using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceLogger : MonoBehaviour
{
    private Vector3 lastPosition;
    private Transform currentDestination;
    private bool isMoving = false;
    private HashSet<string> loggedKeys = new HashSet<string>();
    private List<Transform> visitedDestinations = new List<Transform>();
    private List<float> traveledDistances = new List<float>();
    private Dictionary<string, List<float>> distanceDictionary = new Dictionary<string, List<float>>();
    
    private float currentTravelDistance = 0.0f;

    // Reference to the NotSoSimpleAI script
    private NotSoSimpleAI aiController;

    void Start()
    {
        aiController = GetComponent<NotSoSimpleAI>();
        if (aiController != null)
        {
            aiController.OnNewObjectSelected.AddListener(OnNewDestinationSelected);
        }
        else
        {
            Debug.LogError("NotSoSimpleAI component not found on the NPC!");
        }
        lastPosition = transform.position;
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

    void OnNewDestinationSelected(Transform newDestination)
    {
        if (newDestination != null)
        {
            if (currentDestination != null && currentDestination.name == newDestination.name)
            {
                return; // Skip logging if the destination is the same
            }

            // Log the distance traveled to the previous destination
            if (isMoving)
            {
                traveledDistances.Add(currentTravelDistance);
                visitedDestinations.Add(currentDestination);

                string key = GetSortedKey(currentDestination.name, newDestination.name);
                if (!distanceDictionary.ContainsKey(key))
                {
                    distanceDictionary[key] = new List<float>();
                }
                distanceDictionary[key].Add(currentTravelDistance);

                currentTravelDistance = 0.0f;
            }

            // Update current destination and reset tracking
            currentDestination = newDestination;
            isMoving = true;
        }
    }

    void OnApplicationQuit()
    {
        // Log the average traveled distance for each unique combination of visited destinations
        if (distanceDictionary.Count > 0)
        {
            Debug.Log("Scene is ending, calculating average distances between all visited destinations:");

            foreach (var entry in distanceDictionary)
            {
                float totalDistance = 0.0f;
                foreach (float distance in entry.Value)
                {
                    totalDistance += distance;
                }
                float averageDistance = totalDistance / entry.Value.Count;
                if (averageDistance > 0.0f)
                {
                    Debug.Log($"Average traveled distance between {entry.Key}: {averageDistance} units");
                }
            }
        }
        else
        {
            Debug.Log("Not enough destinations were visited to calculate distances.");
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
