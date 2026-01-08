using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[RequireComponent(typeof(BaseNavigation))]
public class NotSoSimpleAI : CommonAIBase
{
    [SerializeField] protected float DefaultInteractionScore = 0f;
    [SerializeField] protected float PickInteractionInterval = 2f;
    [SerializeField] protected int InteractionPickSize = 5;
    [SerializeField] bool AvoidInUseObjects = true;
    [SerializeField] protected float AgendaPriorityScore = 0.6f;
    
    [Header("Regression Logging")]
    [SerializeField] private DecisionParameterLogger parameterLogger; 
    
    [SerializeField] public UnityEvent<Transform> OnNewObjectSelected = new();

    // 現在の作業ブロックの経過時間を追跡
    protected float CurrentBlockElapsedTime = 0f;
    // 中断した作業オブジェクトを格納
    protected BaseInteraction InterruptedInteraction = null;

    protected float TimeUntilNextInteractionPicked = -1f;

    private InteractionLogger interactionLogger;
    private string lastObjectName = "Start";
    private UnityEngine.AI.NavMeshAgent agent;

    // Debugging State
    private string currentLogicState = "Initializing";
    private string currentPriority = "None";
    private bool InCriticalMode = false;

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        interactionLogger = GetComponent<InteractionLogger>();
    }

    protected override void Update()
    {
        base.Update();

        if (agent != null && agent.velocity.magnitude > 0.1f)
        {
            var heatmap = FindObjectOfType<HeatmapGridData>();
            if (heatmap != null)
                heatmap.RegisterStay(transform.position, Time.deltaTime);
        }

        // 1. Check for Critical Interruptions (ALWAYS ACTIVE)
        if (CurrentInteraction != null && CheckCriticalNeeds())
        {
            // If we are currently doing something that ISN'T a critical need fix, interrupt it.
            // We assume "Work" is not a critical need (handled in CheckCriticalNeeds).
            
            // Check if the current interaction is actually fixing the critical need.
            // If we are already fixing a critical need, do NOT interrupt.
            bool isFixingCritical = IsInteractionFixingCritical(CurrentInteraction);

            if (!isFixingCritical)
            {
                // Check if it's a work object (Agenda Target)
                bool isWorkObject = Agenda.CurrentBlock != null && 
                                    !string.IsNullOrEmpty(Agenda.CurrentBlock.TargetWorkObjectTag) &&
                                    CurrentInteraction.gameObject.CompareTag(Agenda.CurrentBlock.TargetWorkObjectTag);

                // ONLY interrupt if it is a work object. Allow other activities (like Lunch) to finish.
                if (isWorkObject)
                {
                    Debug.LogWarning($"{gameObject.name} interrupted {CurrentInteraction.DisplayName} for CRITICAL NEED.");
                    InterruptedInteraction = CurrentInteraction;
                    CurrentInteraction.PauseInteraction(this); // Keep locked, stop performing

                    CurrentInteraction = null;
                    StartedPerforming = false;
                    TimeUntilNextInteractionPicked = 0f; // Force immediate repick
                }
            }
        }

        // 2. Interaction Loop
        if (CurrentInteraction == null)
        {
            currentLogicState = "Thinking";
            TimeUntilNextInteractionPicked -= Time.deltaTime;

            if (TimeUntilNextInteractionPicked <= 0)
            {
                TimeUntilNextInteractionPicked = PickInteractionInterval;
                PickBestInteraction();
            }
        }
        else
        {
            // We have a target interaction.
            
            // Check Agenda Progress
            bool hasValidTag = Agenda.CurrentBlock != null && !string.IsNullOrEmpty(Agenda.CurrentBlock.TargetWorkObjectTag);
            if (StartedPerforming && hasValidTag && CurrentInteraction.gameObject.CompareTag(Agenda.CurrentBlock.TargetWorkObjectTag))
            {
                CurrentBlockElapsedTime += Time.deltaTime;
                if (CurrentBlockElapsedTime >= Agenda.CurrentBlock.DurationGoalSeconds)
                {
                    Debug.Log($"{gameObject.name} finished agenda block: {Agenda.CurrentBlock.DisplayName}");
                    CurrentBlockElapsedTime = 0f;
                    
                    // If no next block, check if work is done
                    if (!Agenda.AdvanceToNextBlock())
                    {
                        // 💡 FIX: Keep working until Work stat is full (>= 0.95)
                        float workVal = GetWorkStatValue();
                        if (workVal < 0.95f)
                        {
                            AgendaManager.Instance.AssignRandomAgendaBlock(this);
                        }
                        else
                        {
                            Debug.Log($"{gameObject.name} Work Complete (Stat: {workVal:F2}). No new agenda assigned.");
                            Agenda.CurrentBlock = null; // Ensure clear
                        }
                    }
                    
                    // Finish naturally
                    CurrentInteraction.UnlockInteraction(this);
                    CurrentInteraction = null;
                    StartedPerforming = false;
                    TimeUntilNextInteractionPicked = 0f;
                    return;
                }
            }

            // Movement & Performance Logic
            if (!StartedPerforming)
            {
                currentLogicState = "Moving to " + CurrentInteraction.DisplayName;
                
                // 💡 FIX: Strict Distance Check to prevent magic replenishment
                float dist = Vector3.Distance(transform.position, CurrentInteraction.transform.position); // Assuming Interaction is on the object
                // Better: Use the interaction point if available, or the object position.
                // SmartObject stores InteractionPoint. We need to find the SmartObject.
                SmartObject targetSO = FindSmartObjectForInteraction(CurrentInteraction);
                Vector3 targetPos = targetSO != null ? targetSO.InteractionPoint : CurrentInteraction.transform.position;
                
                if (Vector3.Distance(transform.position, targetPos) <= 1.5f) // 1.5f tolerance
                {
                    currentLogicState = "Performing " + CurrentInteraction.DisplayName;
                    CurrentInteraction.Perform(this, OnInteractionFinished);
                    StartedPerforming = true;
                }
                else
                {
                    // Ensure we are moving
                    if (agent.destination != targetPos)
                    {
                        Navigation.SetDestination(targetPos);
                    }
                }
            }
            else
            {
                currentLogicState = "Performing " + CurrentInteraction.DisplayName;
            }
        }
    }

    // ================================================================================================
    // LOGIC CORE: Explicit Priority System
    // ================================================================================================

    void PickBestInteraction()
    {
        // 1. CRITICAL NEEDS (Highest Priority)
        if (CheckCriticalNeeds())
        {
            currentPriority = "Critical Need";
            if (PickCriticalInteraction()) return;
            
            // 💡 FIX: If we are critical but can't find an interaction (e.g. all toilets full),
            // Try to return to interrupted work first (if we still hold the lock).
            if (InterruptedInteraction != null && IsLockedByMe(InterruptedInteraction))
            {
                //Debug.LogWarning($"{gameObject.name} Critical but no interaction found. Returning to interrupted work: {InterruptedInteraction.DisplayName}");
                currentPriority = "Critical Return to Work";
                SetInteractionAndMove(InterruptedInteraction);
                return;
            }

            // Fallback to Wander
            Debug.LogWarning($"{gameObject.name} is Critical but found no interaction. Wandering...");
            currentPriority = "Critical Wander";
            Wander();
            return;
        }

        // 2. AGENDA (Base Priority)
        // Only do agenda if NOT in critical mode
        if (!InCriticalMode && Agenda.CurrentBlock != null)
        {
            currentPriority = "Agenda: " + Agenda.CurrentBlock.DisplayName;
            if (PickAgendaInteraction()) return;
        }
        
        // 3. COMFORT (Fallback)
        currentPriority = "Comfort";
        if (PickComfortInteraction()) return;

        // 4. WANDER (Last Resort - Prevent Freezing)
        currentPriority = "Wandering";
        Wander();
    }

    private bool PickCriticalInteraction()
    {
        AIStat criticalStat = GetMostCriticalStat();
        if (criticalStat == null) return false;

        return PickInteractionTargetingStat(criticalStat);
    }

    private bool PickAgendaInteraction()
    {
        if (Agenda.CurrentBlock == null || string.IsNullOrEmpty(Agenda.CurrentBlock.TargetWorkObjectTag)) return false;

        // 1. Get all objects matching the tag
        var candidates = GetAllInteractionsWithTag(Agenda.CurrentBlock.TargetWorkObjectTag);
        
        // 2. Sort by distance (nearest first)
        candidates.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        // 3. Try to lock the best one
        foreach (var candidate in candidates)
        {
            if (SetInteractionAndMove(candidate))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool PickComfortInteraction()
    {
        // Gather all interactions
        List<BaseInteraction> allInteractions = new List<BaseInteraction>();
        foreach(var so in SmartObjectManager.Instance.RegisteredObjects)
        {
            foreach(var interaction in so.Interactions)
            {
                if (interaction.CanPerform())
                {
                    if (AvoidInUseObjects && IsObjectInUse(interaction.gameObject) && !IsLockedByMe(interaction)) continue;
                    allInteractions.Add(interaction);
                }
            }
        }

        // Sort by Score
        allInteractions.Sort((a, b) => ScoreInteraction(b).CompareTo(ScoreInteraction(a)));

        // Take top N
        int count = Mathf.Min(InteractionPickSize, allInteractions.Count);
        for(int i=0; i<count; i++)
        {
            if (SetInteractionAndMove(allInteractions[i])) return true;
        }
        return false;
    }

    private bool PickInteractionTargetingStat(AIStat stat)
    {
        List<BaseInteraction> candidates = new List<BaseInteraction>();

        foreach (var so in SmartObjectManager.Instance.RegisteredObjects)
        {
            foreach (var interaction in so.Interactions)
            {
                if (!interaction.CanPerform()) continue;
                if (AvoidInUseObjects && IsObjectInUse(interaction.gameObject) && !IsLockedByMe(interaction)) continue;

                if (interaction.StatChanges.Any(sc => sc.LinkedStat == stat && sc.Value > 0))
                {
                    candidates.Add(interaction);
                }
            }
        }

        // Sort by gain amount (descending)
        candidates.Sort((a, b) => 
        {
            float gainA = a.StatChanges.First(sc => sc.LinkedStat == stat).Value;
            float gainB = b.StatChanges.First(sc => sc.LinkedStat == stat).Value;
            return gainB.CompareTo(gainA);
        });

        foreach (var candidate in candidates)
        {
            if (SetInteractionAndMove(candidate)) return true;
        }

        return false;
    }

    private void Wander()
    {
        // Pick a random point on NavMesh
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, 10f, 1))
        {
            Navigation.SetDestination(hit.position);
            currentLogicState = "Wandering";
            if (interactionLogger != null) interactionLogger.OnStartMove("Wander");
        }
    }

    private AIStat GetMostCriticalStat()
    {
        AIStat worstStat = null;
        float worstVal = 1.0f;

        foreach(var statConfig in Stats)
        {
            if (statConfig.LinkedStat.name.Contains("Work")) continue;
            
            float val = GetStatValue(statConfig.LinkedStat);
            if (val < 0.2f && val < worstVal)
            {
                worstVal = val;
                worstStat = statConfig.LinkedStat;
            }
        }
        return worstStat;
    }

    private List<BaseInteraction> GetAllInteractionsWithTag(string tag)
    {
        List<BaseInteraction> list = new List<BaseInteraction>();
        foreach (var smartObject in SmartObjectManager.Instance.RegisteredObjects)
        {
            if (!smartObject.CompareTag(tag)) continue;

            foreach (var interaction in smartObject.Interactions)
            {
                if (!interaction.CanPerform()) continue;
                // If I am locked by me, I can use it even if it's "in use" (by me)
                if (AvoidInUseObjects && IsObjectInUse(interaction.gameObject) && !IsLockedByMe(interaction)) continue;
                list.Add(interaction);
            }
        }
        return list;
    }

    private bool CheckCriticalNeeds()
    {
        // Hysteresis Logic
        if (InCriticalMode)
        {
            // Exit Critical Mode only if ALL stats are above 0.5
            bool allGood = true;
            foreach(var statConfig in Stats)
            {
                if (statConfig.LinkedStat.name.Contains("Work")) continue;
                if (GetStatValue(statConfig.LinkedStat) < 0.5f)
                {
                    allGood = false;
                    break;
                }
            }

            if (allGood)
            {
                InCriticalMode = false;
                Debug.Log($"{gameObject.name} exiting Critical Mode (All stats > 0.5)");
                return false;
            }
            return true; // Still in critical mode
        }
        else
        {
            // Enter Critical Mode if ANY stat is below 0.2
            if (GetMostCriticalStat() != null)
            {
                InCriticalMode = true;
                Debug.Log($"{gameObject.name} entering Critical Mode (Stat < 0.2)");
                return true;
            }
            return false;
        }
    }

    private bool IsInteractionFixingCritical(BaseInteraction interaction)
    {
        if (interaction == null) return false;
        
        // Fix for Oscillation: Check if this interaction fixes ANY critical stat, not just the "most" critical one.
        foreach(var statConfig in Stats)
        {
            if (statConfig.LinkedStat.name.Contains("Work")) continue;
            
            float val = GetStatValue(statConfig.LinkedStat);
            if (val < 0.2f)
            {
                // This stat is critical. Does the interaction fix it?
                if (interaction.StatChanges.Any(sc => sc.LinkedStat == statConfig.LinkedStat && sc.Value > 0))
                {
                    return true;
                }
            }
        }
        return false;
    }

    float ScoreInteraction(BaseInteraction interaction)
    {
        // Simplified scoring for Comfort
        float score = 0f;
        foreach (var change in interaction.StatChanges)
        {
            // Simple: Value * (1 - CurrentValue) -> High gain for low stats
            float current = GetStatValue(change.LinkedStat);
            score += change.Value * (1.0f - current);
        }
        return score;
    }
    
    // Helper to check if I am the one using the object
    private bool IsLockedByMe(BaseInteraction interaction)
    {
        return interaction.IsLockedBy(this);
    }

    private bool IsObjectInUse(GameObject obj)
    {
        List<GameObject> objectsInUse = null;
        HouseholdBlackboard.TryGetGeneric(EBlackboardKey.Household_ObjectsInUse, out objectsInUse, null);
        return AvoidInUseObjects && objectsInUse != null && objectsInUse.Contains(obj);
    }

    private bool SetInteractionAndMove(BaseInteraction selectedInteraction)
    {
        if (selectedInteraction.LockInteraction(this))
        {
            CurrentInteraction = selectedInteraction;
            StartedPerforming = false;

            SmartObject targetObject = FindSmartObjectForInteraction(selectedInteraction);
            if (targetObject != null)
            {
                OnNewObjectSelected.Invoke(targetObject.LookAtPoint);
                // Just set destination, don't check result immediately as path might be pending
                Navigation.SetDestination(targetObject.InteractionPoint); 
                
                //Debug.Log($"{gameObject.name} locked {selectedInteraction.DisplayName}. Moving to {targetObject.InteractionPoint}.");

                if (interactionLogger != null)
                    interactionLogger.OnStartMove(lastObjectName);
            }
            else
            {
                Debug.LogError($"{gameObject.name} locked {selectedInteraction.DisplayName} but could not find parent SmartObject!");
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} failed to lock {selectedInteraction.DisplayName}");
            return false;
        }
    }

    private SmartObject FindSmartObjectForInteraction(BaseInteraction interaction)
    {
        foreach (var smartObject in SmartObjectManager.Instance.RegisteredObjects)
        {
            if (smartObject.Interactions.Contains(interaction))
                return smartObject;
        }
        return null;
    }

    public void NotifyInteractionEnd(string objectName, string statType, float afterValue)
    {
        if (interactionLogger != null)
        {
            interactionLogger.OnEndInteraction(objectName, statType, afterValue);
            interactionLogger.OnEndMove(objectName);
        }
        lastObjectName = objectName;
    }

    public Vector3 SpawnPoint { get; set; } 

    public void ResetState()
    {
        if (IndividualBlackboard == null)
            IndividualBlackboard = BlackboardManager.Instance.GetIndividualBlackboard(this);
        if (HouseholdBlackboard == null)
            HouseholdBlackboard = BlackboardManager.Instance.GetSharedBlackboard(1);

        if (IndividualBlackboard == null || HouseholdBlackboard == null)
            return;

        // 💡 FIX: Unlock the interaction before clearing it!
        if (CurrentInteraction != null)
        {
            CurrentInteraction.UnlockInteraction(this);
        }

        CurrentInteraction = null;
        StartedPerforming = false;
        TimeUntilNextInteractionPicked = 0f;
        InterruptedInteraction = null;
        CurrentBlockElapsedTime = 0f;
        
        HouseholdBlackboard.SetGeneric(EBlackboardKey.Household_ObjectsInUse, new List<GameObject>());

        Debug.Log($"{gameObject.name} state reset. Interaction unlocked.");
    }

    private IEnumerator DelayedResetState()
    {
        yield return new WaitForSeconds(0.2f);
        ResetState();
    }

    public void ForceNewInteraction()
    {
        TimeUntilNextInteractionPicked = 0f; 
    }

    private float GetWorkStatValue()
    {
        foreach(var statConfig in Stats)
        {
            if (statConfig.LinkedStat.name.Contains("Work"))
            {
                return GetStatValue(statConfig.LinkedStat);
            }
        }
        return 0f; // Default if not found
    }
}