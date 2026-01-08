using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    private List<NotSoSimpleAI> npcList = new List<NotSoSimpleAI>();

    void Awake()
    {
        npcList.AddRange(FindObjectsOfType<NotSoSimpleAI>());
    }

    public void ResetNPCs()
    {
        foreach (var npc in npcList)
        {
            npc.transform.position = npc.SpawnPoint; // SpawnPointは個別で設定しておく
            npc.ResetState(); // 欲求や内部状態のリセット
        }

        Debug.Log("NPCs reset to spawn positions.");
    }

    public void StartSimulation()
    {
        foreach (var npc in npcList)
        {
            npc.ForceNewInteraction(); // 強制的に新しい行動を開始
        }

        Debug.Log("NPCs started new simulation cycle.");
    }

    private List<GameObject> activeNPCs = new List<GameObject>();

    public void SpawnNPCsAt(Transform layoutRoot, GameObject npcPrefab, int count)
    {
        ClearNPCs(); // 事前に全削除

        Vector3 spawnPos = layoutRoot.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            Vector3 finalPos = spawnPos + offset;

            GameObject npc = Instantiate(npcPrefab, finalPos, Quaternion.identity);
            activeNPCs.Add(npc);

            var ai = npc.GetComponent<NotSoSimpleAI>();
            if (ai != null)
            {
                ai.SpawnPoint = finalPos;
            }
        }
    }

    public void ClearNPCs()
    {
        var allNPCs = FindObjectsOfType<NotSoSimpleAI>();
        foreach (var npc in allNPCs)
        {
            Destroy(npc.gameObject);
        }

        activeNPCs.Clear();
    }


}
