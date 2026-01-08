using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting; // 必要に応じて

// =========================================================
// アジェンダ関連のクラス定義
// =========================================================

[System.Serializable]
public class AgendaBlock
{
    // アジェンダブロックの名前 (例: "長時間作業", "メールチェック")
    public string DisplayName;

    // このブロックで使用する WorkObject を特定するためのタグやID
    public string TargetWorkObjectTag; 

    // このブロックでNPCが作業を続けるべき目標時間（秒）
    public float DurationGoalSeconds; 
}

public class WorkAgenda
{
    public Queue<AgendaBlock> AgendaQueue = new Queue<AgendaBlock>();

    // 現在進行中のブロック
    public AgendaBlock CurrentBlock { get; set; }

    // 新しいアジェンダリストを設定し、最初のブロックを開始する
    public void SetNewAgenda(AgendaBlock[] blocks)
    {
        AgendaQueue.Clear();
        foreach (var block in blocks)
        {
            AgendaQueue.Enqueue(block);
        }
        AdvanceToNextBlock();
    }

    // 次のブロックに進む（なければfalseを返す）
    public bool AdvanceToNextBlock()
    {
        if (AgendaQueue.Count > 0)
        {
            CurrentBlock = AgendaQueue.Dequeue();
            return true;
        }
        CurrentBlock = null;
        return false;
    }
}

// =========================================================

[System.Serializable]
public class AIStatConfiguration
{
    [field: SerializeField] public AIStat LinkedStat { get; set; }

    [field: SerializeField] public bool OverrideDefaults { get; set; } = true;
    [field: SerializeField, Range(0f, 1f)] public float Override_InitialValue { get; set; } = 0.5f;
    [field: SerializeField, Range(0f, 1f)] public float Override_DecayRate { get; set; } = 0.005f;
}

public enum Gender { Male, Female }

[RequireComponent(typeof(BaseNavigation))]
public class CommonAIBase : MonoBehaviour
{
    [Header("General")]
    [SerializeField] int HouseholdID = 1;
    [field: SerializeField] protected AIStatConfiguration[] Stats;
    public AIStatConfiguration[] PublicStats => Stats;
    [SerializeField] protected FeedbackUIPanel LinkedUI;

    public Gender NPCGender { get; private set; }

    [Header("Traits")]
    [SerializeField] protected List<Trait> Traits = new List<Trait>();
    public List<Trait> PublicTraits => Traits;

    [Header("Memories")]
    [SerializeField] int LongTermMemoryThreshold = 2;

    // 💡 修正点: Agendaプロパティを親クラスに追加
    public WorkAgenda Agenda { get; set; } = new WorkAgenda();

    protected BaseNavigation Navigation;
    public BaseInteraction CurrentInteractionPublic => CurrentInteraction;

    protected bool StartedPerforming = false;
    public bool PublicStartedPerforming => StartedPerforming;

    public Blackboard IndividualBlackboard { get; protected set; }
    public Blackboard HouseholdBlackboard { get; protected set; }

    protected Dictionary<AIStat, float> DecayRates = new Dictionary<AIStat, float>();
    protected Dictionary<AIStat, AIStatPanel> StatUIPanels = new Dictionary<AIStat, AIStatPanel>();

    protected BaseInteraction CurrentInteraction
    {
        get 
        {
            BaseInteraction interaction = null;
            IndividualBlackboard.TryGetGeneric(EBlackboardKey.Character_FocusObject, out interaction, null);
            return interaction; 
        }
        set 
        {
            BaseInteraction previousInteraction = null;
            IndividualBlackboard.TryGetGeneric(EBlackboardKey.Character_FocusObject, out previousInteraction, null);

            IndividualBlackboard.SetGeneric(EBlackboardKey.Character_FocusObject, value);

            List<GameObject> objectsInUse = null;
            HouseholdBlackboard.TryGetGeneric(EBlackboardKey.Household_ObjectsInUse, out objectsInUse, null);

            if (value != null)
            {
                if (objectsInUse == null)
                    objectsInUse = new List<GameObject>();

                if (!objectsInUse.Contains(value.gameObject))
                {
                    objectsInUse.Add(value.gameObject);
                    HouseholdBlackboard.SetGeneric(EBlackboardKey.Household_ObjectsInUse, objectsInUse);
                }
            }
            else if (objectsInUse != null && previousInteraction != null && previousInteraction.gameObject != null)
            {
                if (objectsInUse.Remove(previousInteraction.gameObject))
                    HouseholdBlackboard.SetGeneric(EBlackboardKey.Household_ObjectsInUse, objectsInUse);
            }
        }
    }

    public void SetGender(Gender gender)
    {
        NPCGender = gender;
        Debug.Log($"{gameObject.name} assigned gender: {NPCGender}");
    }

    protected virtual void Awake()
    {
        Navigation = GetComponent<BaseNavigation>();
        if (Traits == null)
        {
            Traits = new List<Trait>();
        }
    }

    protected virtual void Start()
    {
        HouseholdBlackboard = BlackboardManager.Instance.GetSharedBlackboard(HouseholdID);
        IndividualBlackboard = BlackboardManager.Instance.GetIndividualBlackboard(this);

        IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_ShortTerm, new List<MemoryFragment>());
        IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_LongTerm, new List<MemoryFragment>());

        foreach (var statConfig in Stats)
        {
            var linkedStat = statConfig.LinkedStat;
            if (linkedStat == null) continue;

            // Enable overrides for all stats
            statConfig.OverrideDefaults = true;

            if (linkedStat.name.Contains("Work"))
            {
                // Work stat: Fixed at 0
                statConfig.Override_InitialValue = 0f;
                statConfig.Override_DecayRate = 0f;
            }
            else
            {
                // Other stats: Randomize Decay Rate ONLY (0.0001 to 0.001)
                statConfig.Override_DecayRate = Random.Range(0.00001f, 0.001f);
            }

            float initialValue = statConfig.OverrideDefaults ? statConfig.Override_InitialValue : linkedStat.InitialValue;
            float decayRate = statConfig.OverrideDefaults ? statConfig.Override_DecayRate : linkedStat.DecayRate;

            DecayRates[linkedStat] = decayRate;
            IndividualBlackboard.SetStat(linkedStat, initialValue);

            if (linkedStat.IsVisible)
                StatUIPanels[linkedStat] = LinkedUI.AddStat(linkedStat, initialValue);
        }
    }

    public void AddTrait(Trait trait)
    {
        if (trait != null && Traits != null && !Traits.Contains(trait)) 
        {
            Traits.Add(trait);
            Debug.Log($"[{gameObject.name}] Added Trait: {trait.DisplayName}");
        }
    }

    protected float ApplyTraitsTo(AIStat targetStat, Trait.ETargetType targetType, float currentValue)
    {
        if (Traits == null) return currentValue; 
        
        foreach(var trait in Traits)
        {
            if (trait == null) continue; 
            currentValue = trait.Apply(targetStat, targetType, currentValue);
        }
        return currentValue;
    }

    protected virtual void Update()
    {
        if (CurrentInteraction != null)
        {
            if (Navigation.IsAtDestination && !StartedPerforming)
            {
                StartedPerforming = true;
                CurrentInteraction.Perform(this, OnInteractionFinished);
            }
        }

        foreach(var statConfig in Stats)
        {
            UpdateIndividualStat(statConfig.LinkedStat, -DecayRates[statConfig.LinkedStat] * Time.deltaTime, Trait.ETargetType.DecayRate);
        }

        List<MemoryFragment> recentMemories = IndividualBlackboard.GetGeneric<List<MemoryFragment>>(EBlackboardKey.Memories_ShortTerm);
        bool memoriesChanged = false;

        for(int index = recentMemories.Count - 1; index >= 0; index--)
        {
            if (!recentMemories[index].Tick(Time.deltaTime))
            {
                recentMemories.RemoveAt(index);
                memoriesChanged = true;
            }
        }

        if (memoriesChanged)
            IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_ShortTerm, recentMemories);
    }

    protected virtual void OnInteractionFinished(BaseInteraction interaction)
    {
        interaction.UnlockInteraction(this);
        CurrentInteraction = null;
        Debug.Log($"Finished {interaction.DisplayName}");
    }

    public void UpdateIndividualStat(AIStat linkedStat, float amount, Trait.ETargetType targetType)
    {
        float adjustedAmount = ApplyTraitsTo(linkedStat, targetType, amount);
        float newValue = Mathf.Clamp01(GetStatValue(linkedStat) + adjustedAmount);

        IndividualBlackboard.SetStat(linkedStat, newValue);

        if (linkedStat.IsVisible)
            StatUIPanels[linkedStat].OnStatChanged(newValue);
    }

    public float GetStatValue(AIStat linkedStat)
    {
        return IndividualBlackboard.GetStat(linkedStat);
    }

    public void AddMemories(MemoryFragment[] memoriesToAdd)
    {
        foreach (var memory in memoriesToAdd)
            AddMemory(memory);
    }

    protected void AddMemory(MemoryFragment memoryToAdd)
    {
        List<MemoryFragment> permanentMemories = IndividualBlackboard.GetGeneric<List<MemoryFragment>>(EBlackboardKey.Memories_LongTerm);

        MemoryFragment memoryToCancel = null;
        foreach(var memory in permanentMemories)
        {
            if (memoryToAdd.IsSimilarTo(memory))
                return;
            if (memory.IsCancelledBy(memoryToAdd))
                memoryToCancel = memory;
        }

        if (memoryToCancel != null)
        {
            permanentMemories.Remove(memoryToCancel);
            IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_LongTerm, permanentMemories);
        }

        List<MemoryFragment> recentMemories = IndividualBlackboard.GetGeneric<List<MemoryFragment>>(EBlackboardKey.Memories_ShortTerm);

        MemoryFragment existingRecentMemory = null;
        foreach(var memory in recentMemories)
        {
            if (memoryToAdd.IsSimilarTo(memory))
                existingRecentMemory = memory;
            if (memory.IsCancelledBy(memoryToAdd))
                memoryToCancel = memory;
        }

        if (memoryToCancel != null)
        {
            recentMemories.Remove(memoryToCancel);
            IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_ShortTerm, recentMemories);
        }

        if (existingRecentMemory == null)
        {
            Debug.Log($"Added memory {memoryToAdd.Name}");
            recentMemories.Add(memoryToAdd.Duplicate());
            IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_ShortTerm, recentMemories);
        }
        else
        {
            Debug.Log($"Reinforced memory {memoryToAdd.Name}");
            existingRecentMemory.Reinforce(memoryToAdd);

            if (existingRecentMemory.Occurrences >= LongTermMemoryThreshold)
            {
                permanentMemories.Add(existingRecentMemory);
                recentMemories.Remove(existingRecentMemory);

                IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_ShortTerm, recentMemories);
                IndividualBlackboard.SetGeneric(EBlackboardKey.Memories_LongTerm, permanentMemories);

                Debug.Log($"Memory {existingRecentMemory.Name} became permanent!");
            }
        }
    }
}