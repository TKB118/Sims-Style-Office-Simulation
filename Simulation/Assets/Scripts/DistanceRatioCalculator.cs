using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceRatioCalculator : MonoBehaviour
{
    private List<SmartObject> smartObjects;
    private Dictionary<string, float> finalDistances = new Dictionary<string, float>();
    private Dictionary<string, (float totalDistance, int travelCount)> traveledDistances = new Dictionary<string, (float, int)>();

    // Reference to the NotSoSimpleAI script
    private NotSoSimpleAI aiController;
    private Transform currentDestination;
    private float currentTravelDistance = 0.0f;
    private Vector3 lastPosition;
    private bool isMoving = false;

    void Start()
    {
        // Retrieve the list of registered SmartObjects from the SmartObjectManager
        smartObjects = SmartObjectManager.Instance.RegisteredObjects;
        lastPosition = transform.position;

        aiController = GetComponent<NotSoSimpleAI>();
        if (aiController != null)
        {
            aiController.OnNewObjectSelected.AddListener(OnNewDestinationSelected);
        }
        else
        {
            Debug.LogError("NotSoSimpleAI component not found on the NPC!");
        }
    }

    void Update()
    {
        if (isMoving)
        {
            float distanceThisFrame = Vector3.Distance(lastPosition, transform.position);
            currentTravelDistance += distanceThisFrame;
            lastPosition = transform.position;
        }
    }

    private void CalculateFinalDistances()
    {
        for (int i = 0; i < smartObjects.Count; i++)
        {
            for (int j = i + 1; j < smartObjects.Count; j++)
            {
                SmartObject objA = smartObjects[i];
                SmartObject objB = smartObjects[j];

                float distance = Vector3.Distance(objA.InteractionPoint, objB.InteractionPoint);
                string key = GetSortedKey(objA.DisplayName, objB.DisplayName);
                finalDistances[key] = distance;
                
                Debug.Log($"Distance between {objA.DisplayName} and {objB.DisplayName}: {distance} units");
            }
        }
    }

    void OnNewDestinationSelected(Transform newDestination)
    {
        if (newDestination != null)
        {
            if (currentDestination != null && currentDestination.name == newDestination.name)
            {
                return; // Skip logging if the destination is the same
            }

            // Log the distance traveled to the previous destination only if fully completed
            if (isMoving)
            {
                string key = GetSortedKey(currentDestination.name, newDestination.name);
                if (!traveledDistances.ContainsKey(key))
                {
                    traveledDistances[key] = (0.0f, 0); // Initialize with 0 distance and 0 travels
                }

                // Add to the total distance and increment the travel counter
                traveledDistances[key] = (
                    traveledDistances[key].totalDistance + currentTravelDistance,
                    traveledDistances[key].travelCount + 1
                );

                currentTravelDistance = 0.0f; // Reset the current travel distance
            }

            // Update current destination and reset tracking
            currentDestination = newDestination;
            isMoving = true;
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Scene is ending, calculating final distances and ratios:");
        CalculateFinalDistances();

        foreach (var entry in traveledDistances)
        {
            string key = entry.Key;
            float totalTraveledDistance = entry.Value.totalDistance;
            int travelCount = entry.Value.travelCount;

            if (travelCount > 0)
            {
                float averageTraveledDistance = totalTraveledDistance / travelCount;

                if (finalDistances.ContainsKey(key))
                {
                    float finalDistance = finalDistances[key];
                    float ratio = averageTraveledDistance / finalDistance;
                    Debug.Log($"Ratio of average traveled distance to actual distance between {key}: {ratio}");
                }
                else
                {
                    Debug.LogWarning($"Final distance for {key} not found.");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (aiController != null)
        {
            aiController.OnNewObjectSelected.RemoveListener(OnNewDestinationSelected);
        }
    }

    private string GetSortedKey(string nameA, string nameB)
    {
        List<string> names = new List<string> { nameA, nameB };
        names.Sort();
        return string.Join("-", names);
    }
}
