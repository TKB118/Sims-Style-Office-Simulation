using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // <-- ADDED for trait randomization

public class SimulationNPCController : MonoBehaviour
{
    [SerializeField]
    public GameObject npcPrefab;

    [SerializeField]
    public Transform spawnPoint;      
    
    [SerializeField]   // スポーン位置
    public int npcCount = 5;

    [SerializeField]
    public float spawnInterval = 1f;     // 各NPCのスポーン間隔

    private NotSoSimpleAI ai;

    // --- NEW FIELDS FOR TRAIT MANAGEMENT ---
    [Header("NPC Customization")]
    public List<Trait> AvailableTraits = new List<Trait>(); // Drag your Trait assets here in the Inspector
    [Range(1, 5)]
    public int MaxTraitsPerNPC = 1; // Maximum number of unique traits to assign per NPC
    // ----------------------------------------

    private readonly List<GameObject> spawnedNPCs = new();


    private void IgnitionAgenda(NotSoSimpleAI ai)
    {
        if (ai == null) return;

        if (AgendaManager.Instance != null)
        {
            AgendaManager.Instance.AssignAgenda(ai);
            // Debug.Log($"[Ignition] Agenda assigned to {ai.name}");
        }
        else
        {
            Debug.LogWarning("AgendaManager Instance not found. Cannot assign agenda.");
        }
    }

    /// <summary>
    /// シーン開始時に存在する全てのNPCにアジェンダを割り当てます。
    /// </summary>
    private IEnumerator IgnitionAllExistingNPCs()
    {
        // 他の初期化待ち（必要であれば）
        yield return new WaitForEndOfFrame();

        var allNPCs = FindObjectsOfType<NotSoSimpleAI>();
        foreach (var npc in allNPCs)
        {
            IgnitionAgenda(npc);
        }
    }


    public void RespawnNPCs()
    {
        Debug.Log($"[RespawnNPCs] Spawning {npcCount} NPCs");
        DestroyAllNPCs();
        SpawnNPCs();
    }

    public void DestroyAllNPCs()
    {
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null) Destroy(npc);
        }
        spawnedNPCs.Clear();
    }

    public void SpawnNPCs()
    {
        StartCoroutine(SpawnNPCsSequentially());
    }


    private IEnumerator SpawnNPCsSequentially()
    {
        for (int i = 0; i < npcCount; i++)
        {
            if (npcPrefab == null || spawnPoint == null)
            {
                Debug.LogError("[SimulationNPCController] npcPrefab or spawnPoint is null!");
                yield break;
            }

            GameObject npc = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
            npc.name = $"NPC_{i}";
            spawnedNPCs.Add(npc);

            Debug.Log($"[Spawned] {npc.name} at {spawnPoint.position}");

            yield return null;

            var ai = npc.GetComponent<NotSoSimpleAI>();
            if (ai != null)
            {
                ai.SpawnPoint = spawnPoint.position;

                // 1. ASSIGN GENDER
                AssignRandomGender(ai); 
                
                // 2. ASSIGN TRAITS
                AssignRandomTraits(ai);

                ai.ResetState();
                IgnitionAgenda(ai);
            }
            else
            {
                Debug.LogWarning($"[Missing AI] {npc.name} does not have NotSoSimpleAI component.");
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log($"[SimulationNPCController] Spawned {npcCount} NPCs sequentially.");
    }

    // --- Helper function for Gender (uses the globally defined Gender enum) ---
    private void AssignRandomGender(NotSoSimpleAI ai)
    {
        // Note: The Gender enum is defined outside the class in CommonAIBase.cs, 
        // allowing it to be referenced directly here (assuming same namespace).
        var possibleGenders = System.Enum.GetValues(typeof(Gender));
        Gender randomGender = 
            (Gender)possibleGenders.GetValue(Random.Range(0, possibleGenders.Length));
        
        ai.SetGender(randomGender);
    }
    
    // --- Helper function for Traits (IMPLEMENTED) ---
    private void AssignRandomTraits(NotSoSimpleAI ai)
    {
        if (AvailableTraits == null || AvailableTraits.Count == 0)
        {
            Debug.LogWarning($"[{ai.name}] No AvailableTraits found to assign.");
            return;
        }

        // Determine the number of traits to assign (from 1 up to MaxTraitsPerNPC)
        // Ensure we don't try to assign more traits than are available.
        int maxPossible = Mathf.Min(AvailableTraits.Count, MaxTraitsPerNPC);
        int numTraitsToAssign = Random.Range(1, maxPossible + 1);
        
        // Create a temporary list of traits to pick from (to avoid duplicates)
        List<Trait> traitsPool = AvailableTraits.ToList();
        
        for (int j = 0; j < numTraitsToAssign && traitsPool.Count > 0; j++)
        {
            int randomIndex = Random.Range(0, traitsPool.Count);
            Trait selectedTrait = traitsPool[randomIndex];
            
            // Assign the trait using the method implemented on CommonAIBase
            ai.AddTrait(selectedTrait); 
            
            // Remove the selected trait from the pool to avoid duplicates
            traitsPool.RemoveAt(randomIndex);
        }
    }


    public List<GameObject> GetNPCs() => spawnedNPCs;
}