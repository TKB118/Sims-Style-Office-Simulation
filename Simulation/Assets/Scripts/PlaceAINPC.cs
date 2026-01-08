using System.Collections;
using UnityEngine;

public class PlaceAINPC : MonoBehaviour
{
    public GameObject prefabToPlace; // Prefab to be placed
    public string targetObjectName = "Floors"; // Name of the target object to search
    public int numberOfPrefabsToPlace = 1; // Number of prefabs to place per floor child

    void Start()
    {
        StartCoroutine(DelayedPrefabPlacement(0.5f));
    }

    IEnumerator DelayedPrefabPlacement(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);
        yield return SearchAndPlacePrefab(transform);
    }

    IEnumerator SearchAndPlacePrefab(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(targetObjectName, System.StringComparison.OrdinalIgnoreCase))
            {
                foreach (Transform floorChild in child)
                {
                    yield return PlacePrefabsOnFloorChild(floorChild);
                }
            }
            else
            {
                yield return SearchAndPlacePrefab(child);
            }
        }
    }

    IEnumerator PlacePrefabsOnFloorChild(Transform floorChild)
    {
        Vector3 centerPosition = floorChild.GetComponent<Renderer>()?.bounds.center ?? floorChild.position;

        for (int i = 0; i < numberOfPrefabsToPlace; i++)
        {
            Instantiate(prefabToPlace, centerPosition, Quaternion.identity);

            Debug.Log($"Placed prefab {i + 1} at the center of floor child: {floorChild.name}");

            yield return new WaitForSeconds(1f); // Delay between placing each prefab
        }
    }
}
