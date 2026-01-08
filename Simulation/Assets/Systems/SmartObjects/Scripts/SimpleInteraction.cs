using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SimpleInteraction : BaseInteraction
{
    protected class PerformerInfo
    {
        public float ElapsedTime;
        public UnityAction<BaseInteraction> OnCompleted;
    }

    [SerializeField] protected int MaxSimultaneousUsers = 1;

    protected Dictionary<CommonAIBase, PerformerInfo> CurrentPerformers = new Dictionary<CommonAIBase, PerformerInfo> ();
    public int NumCurrentUsers => CurrentPerformers.Count;

    protected List<CommonAIBase> PerformersToCleanup = new List<CommonAIBase>();

    public override bool CanPerform()
    {
        return NumCurrentUsers < MaxSimultaneousUsers;
    }

    public override bool LockInteraction(CommonAIBase performer)
    {
        if (CurrentPerformers.ContainsKey(performer))
        {
            // Already locked by this performer. This is fine (idempotent).
            return true;
        }

        if (NumCurrentUsers >= MaxSimultaneousUsers)
        {
            Debug.LogError($"{performer.name} trying to lock {_DisplayName} which is already at max users");
            return false;
        }

        CurrentPerformers[performer] = null;

        return true;
    }

    public override void PauseInteraction(CommonAIBase performer)
    {
        if (CurrentPerformers.ContainsKey(performer))
        {
            // Keep the key (lock) but clear the info (stop performing)
            CurrentPerformers[performer] = null;
        }
    }

    public override bool IsLockedBy(CommonAIBase performer)
    {
        return CurrentPerformers.ContainsKey(performer);
    }

    public override bool Perform(CommonAIBase performer, UnityAction<BaseInteraction> onCompleted)
    {
        if (!CurrentPerformers.ContainsKey(performer))
        {
            Debug.LogError($"{performer.name} is trying to perform an interaction {_DisplayName} that they have not locked");
            return false;
        }

        // check the interaction type
        if (InteractionType == EInteractionType.Instantaneous)
        {
            if (StatChanges.Length > 0)
                ApplyInteractionEffects(performer, 1f, true);

            OnInteractionCompleted(performer, onCompleted);
        }
        else if (InteractionType == EInteractionType.OverTime || InteractionType == EInteractionType.AfterTime)
        {
            CurrentPerformers[performer] = new PerformerInfo() { ElapsedTime = 0, OnCompleted = onCompleted };
        }

        return true;
    }

    protected void OnInteractionCompleted(CommonAIBase performer, UnityAction<BaseInteraction> onCompleted)
    {
        onCompleted.Invoke(this);

        var ai = performer as NotSoSimpleAI;
        var logger = ai?.GetComponent<InteractionLogger>();

        if (logger != null && StatChanges != null && StatChanges.Length > 0)
        {
            foreach (var statChange in StatChanges)
            {
                string statName = statChange.LinkedStat != null ? statChange.LinkedStat.name : "UnknownStat";
                float proportion = (_Duration > 0f) ? 1f : 1f;
                float delta = statChange.Value * proportion;

                logger.OnEndInteraction(DisplayName, statName, delta);
            }

            logger.OnEndMove(DisplayName);
        }

        if (!PerformersToCleanup.Contains(performer))
        {
            PerformersToCleanup.Add(performer);
            Debug.LogWarning($"{performer.name} did not unlock interaction in their OnCompleted handler for {_DisplayName}");
        }

        
        UnlockInteraction(performer);
    }




    public override bool UnlockInteraction(CommonAIBase performer)
    {
        if (CurrentPerformers.ContainsKey(performer))
        {
            PerformersToCleanup.Add(performer);
            return true;
        }

        Debug.LogError($"{performer.name} is trying to unlock an interaction {_DisplayName} they have not locked");

        return false;
    }

    public void ForceClear()
    {
        if (CurrentPerformers.Count > 0)
            Debug.LogWarning($"[ForceClear] {_DisplayName}: Clearing {CurrentPerformers.Count} performers.");

        CurrentPerformers.Clear();
        PerformersToCleanup.Clear();
    }

    protected virtual void Update()
    {
        var performersSnapshot = new List<CommonAIBase>(CurrentPerformers.Keys);
        foreach (var performer in performersSnapshot)
        {
            if (performer == null || !CurrentPerformers.ContainsKey(performer))
                continue;

            PerformerInfo performerInfo = CurrentPerformers[performer];
            if (performerInfo == null)
                continue;

            float previousElapsedTime = performerInfo.ElapsedTime;
            performerInfo.ElapsedTime = Mathf.Min(performerInfo.ElapsedTime + Time.deltaTime, _Duration);
            bool isFinalTick = performerInfo.ElapsedTime >= _Duration;

            bool shouldFinish = isFinalTick;

            if (InteractionType == EInteractionType.OverTime)
            {
                if (StatChanges.Length > 0)
                {
                    bool keepGoing = ApplyInteractionEffects(performer, (performerInfo.ElapsedTime - previousElapsedTime) / _Duration, isFinalTick);
                    if (!keepGoing) shouldFinish = true;
                }
            }
            else if (InteractionType == EInteractionType.AfterTime)
            {
                if (isFinalTick && StatChanges.Length > 0)
                {
                    ApplyInteractionEffects(performer, 1f, true);
                }
            }

            if (shouldFinish)
                OnInteractionCompleted(performer, performerInfo.OnCompleted);
        }

        foreach (var performer in PerformersToCleanup)
            CurrentPerformers.Remove(performer);
        PerformersToCleanup.Clear();
    }

}
