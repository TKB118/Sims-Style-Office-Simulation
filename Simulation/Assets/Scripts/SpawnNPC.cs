using System.Collections;
using UnityEngine;

public class SpawnNPC : MonoBehaviour
{
    public GameObject prefabToPlace; // Prefab to be placed
    public int numberOfPrefabsToPlace = 1; // Number of prefabs to place per floor child
    public float spawnDelay = 1f; // Delay in seconds between NPC spawns

    // Start is called before the first frame update
    void Start()
    {
        LayoutEvaluationManager.SetExpectedNPCCount(numberOfPrefabsToPlace);
        StartCoroutine(StartSpawning(0.5f));
    }

    private IEnumerator StartSpawning(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        for (int i = 0; i < numberOfPrefabsToPlace; i++)
        {
            Vector3 centerPosition = transform.position;

            GameObject spawnedPrefab = Instantiate(prefabToPlace, centerPosition, Quaternion.identity);

            // ここでSpawnPointを設定
            var aiComponent = spawnedPrefab.GetComponent<NotSoSimpleAI>();
            if (aiComponent != null)
            {
                aiComponent.SpawnPoint = centerPosition;
            }

            Debug.Log($"Placed prefab {i + 1} at the position: {centerPosition}");

            yield return new WaitForSeconds(spawnDelay);
        }
    }
        public void SpawnAt(Transform layoutRoot)
    {
        // NPCをスポーンする基準位置を決める
        Vector3 spawnPosition = layoutRoot.position;

        for (int i = 0; i < numberOfPrefabsToPlace; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            Vector3 finalPosition = spawnPosition + offset;

            GameObject spawnedPrefab = Instantiate(prefabToPlace, finalPosition, Quaternion.identity);

            // NotSoSimpleAIにSpawnPointを設定
            var aiComponent = spawnedPrefab.GetComponent<NotSoSimpleAI>();
            if (aiComponent != null)
            {
                aiComponent.SpawnPoint = finalPosition;
            }

            Debug.Log($"Spawned NPC {i + 1} at {finalPosition} for layout {layoutRoot.name}");
        }
    }

}
