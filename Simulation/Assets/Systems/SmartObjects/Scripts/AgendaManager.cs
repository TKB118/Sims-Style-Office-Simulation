using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgendaManager : MonoBehaviour
{
    public static AgendaManager Instance { get; private set; }

    [SerializeField]
    private AgendaBlock[] AvailableAgendaBlocks = new AgendaBlock[]
    {
        
        new AgendaBlock() { DisplayName = "Team Meeting", TargetWorkObjectTag = "MeetingSpace", DurationGoalSeconds = 1800f },
        new AgendaBlock() { DisplayName = "Focused Work", TargetWorkObjectTag = "Desk", DurationGoalSeconds = 3600f },
        new AgendaBlock() { DisplayName = "Briefing", TargetWorkObjectTag = "MeetingSpace", DurationGoalSeconds = 900f },
        new AgendaBlock() { DisplayName = "Paperwork", TargetWorkObjectTag = "Desk", DurationGoalSeconds = 1800f },
        new AgendaBlock() { DisplayName = "Research", TargetWorkObjectTag = "Desk", DurationGoalSeconds = 5400f },
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private List<AgendaBlock> FilteredAgendaBlocks = new List<AgendaBlock>();

    private void Start()
    {
        // Wait a frame to ensure all SmartObjects are registered
        StartCoroutine(InitializeAgenda());
    }

    private IEnumerator InitializeAgenda()
    {
        yield return null; // Wait one frame
        RefreshAvailableBlocks();
    }

    public void RefreshAvailableBlocks()
    {
        FilteredAgendaBlocks.Clear();
        
        // Get all unique tags from registered smart objects
        HashSet<string> availableTags = new HashSet<string>();
        foreach(var obj in SmartObjectManager.Instance.RegisteredObjects)
        {
            if (!string.IsNullOrEmpty(obj.tag))
            {
                availableTags.Add(obj.tag);
            }
        }

        // Filter blocks
        foreach(var block in AvailableAgendaBlocks)
        {
            if (availableTags.Contains(block.TargetWorkObjectTag))
            {
                FilteredAgendaBlocks.Add(block);
            }
        }
        
        Debug.Log($"AgendaManager: Found {availableTags.Count} tags. Filtered agenda blocks from {AvailableAgendaBlocks.Length} to {FilteredAgendaBlocks.Count}.");
    }

    public void AssignRandomAgendaBlock(CommonAIBase npc)
    {
        if (FilteredAgendaBlocks.Count == 0)
        {
            // Try refreshing once if empty
            RefreshAvailableBlocks();
            
            if (FilteredAgendaBlocks.Count == 0)
            {
                Debug.LogWarning("No agenda blocks available (no matching SmartObjects found).");
                return;
            }
        }

        // Pick a random block from FILTERED list
        AgendaBlock randomBlock = FilteredAgendaBlocks[Random.Range(0, FilteredAgendaBlocks.Count)];

        // Create a new WorkAgenda
        WorkAgenda newAgenda = new WorkAgenda();
        
        // Deep copy the block to avoid shared state issues
        AgendaBlock blockCopy = new AgendaBlock()
        {
            DisplayName = randomBlock.DisplayName,
            TargetWorkObjectTag = randomBlock.TargetWorkObjectTag,
            DurationGoalSeconds = randomBlock.DurationGoalSeconds
        };
        
        // Use SetNewAgenda to assign the block
        newAgenda.SetNewAgenda(new AgendaBlock[] { blockCopy });

        npc.Agenda = newAgenda;
        
        // Force the NPC to re-evaluate
        if (npc is NotSoSimpleAI ai)
        {
            ai.ForceNewInteraction();
        }
        
        Debug.Log($"Assigned new random agenda block '{blockCopy.DisplayName}' to {npc.name}");
    }

    // Initial assignment (can be called by Spawner)
    public void AssignAgenda(CommonAIBase npc)
    {
        AssignRandomAgendaBlock(npc);
    }
}
