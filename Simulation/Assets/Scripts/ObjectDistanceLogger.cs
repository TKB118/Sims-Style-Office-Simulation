using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDistanceLogger : MonoBehaviour
{
    private List<SmartObject> smartObjects;
    private Dictionary<string, float> finalDistances = new Dictionary<string, float>();
    private Dictionary<string, (float totalDistance, int travelCount)> traveledDistances = new Dictionary<string, (float, int)>();

    private Transform currentDestination;
    private float currentTravelDistance = 0.0f;
    private Vector3 lastPosition;
    private bool isMoving = false;

    void Start()
    {
        // Retrieve the list of registered SmartObjects
        smartObjects = SmartObjectManager.Instance.RegisteredObjects;

        lastPosition = transform.position;
        CalculateFinalDistances();
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

    public void OnNewDestinationSelected(Transform newDestination)
    {
        if (newDestination != null)
        {
            if (currentDestination != null && currentDestination.name == newDestination.name)
            {
                return; // Skip logging if the destination is the same
            }

            // Log the traveled distance to the previous destination
            if (isMoving && currentDestination != null)
            {
                string key = GetSortedKey(currentDestination.name, newDestination.name);
                if (!traveledDistances.ContainsKey(key))
                {
                    traveledDistances[key] = (0.0f, 0); // Initialize with 0 distance and 0 travels
                }

                traveledDistances[key] = (
                    traveledDistances[key].totalDistance + currentTravelDistance,
                    traveledDistances[key].travelCount + 1
                );

                Debug.Log($"Traveled distance between {currentDestination.name} and {newDestination.name}: {currentTravelDistance} units");

                currentTravelDistance = 0.0f; // Reset the current travel distance
            }

            // Update the current destination and start tracking
            currentDestination = newDestination;
            isMoving = true;
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

                Debug.Log($"Calculated distance between {objA.DisplayName} and {objB.DisplayName}: {distance} units");
            }
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Calculating average distances and ratios:");
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
                    Debug.Log($"Average traveled distance to actual distance ratio for {key}: {ratio}");
                }
                else
                {
                    Debug.LogWarning($"Final distance for {key} not found.");
                }
            }
        }
    }

    private string GetSortedKey(string nameA, string nameB)
    {
        List<string> names = new List<string> { nameA, nameB };
        names.Sort();
        return string.Join("-", names);
    }
}
