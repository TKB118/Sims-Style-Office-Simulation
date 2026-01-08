using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalyzeDistanceRatio : MonoBehaviour
{
    private List<SmartObject> smartObjects;
    private Dictionary<string, float> actualDistances = new Dictionary<string, float>();
    private Dictionary<string, List<float>> traveledDistances = new Dictionary<string, List<float>>();

    void Start()
    {
        // Retrieve the list of registered SmartObjects from the SmartObjectManager
        smartObjects = SmartObjectManager.Instance.RegisteredObjects;
        CalculateActualDistances();
    }

    private void CalculateActualDistances()
    {
        for (int i = 0; i < smartObjects.Count; i++)
        {
            for (int j = i + 1; j < smartObjects.Count; j++)
            {
                SmartObject objA = smartObjects[i];
                SmartObject objB = smartObjects[j];

                float distance = Vector3.Distance(objA.InteractionPoint, objB.InteractionPoint);
                string key = GetSortedKey(objA.DisplayName, objB.DisplayName);

                if (!actualDistances.ContainsKey(key))
                {
                    actualDistances[key] = distance;
                }
            }
        }
    }

    public void LogTraveledDistance(string key, float distance)
    {
        if (!traveledDistances.ContainsKey(key))
        {
            traveledDistances[key] = new List<float>();
        }
        traveledDistances[key].Add(distance);
    }

    private string GetSortedKey(string nameA, string nameB)
    {
        List<string> names = new List<string> { nameA, nameB };
        names.Sort();
        return string.Join("-", names);
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Scene is ending, analyzing NPC walk distances:");

        foreach (var entry in traveledDistances)
        {
            string key = entry.Key;
            float totalTraveledDistance = 0.0f;

            foreach (float traveledDistance in entry.Value)
            {
                totalTraveledDistance += traveledDistance;
            }

            float averageTraveledDistance = totalTraveledDistance / entry.Value.Count;
            if (actualDistances.ContainsKey(key))
            {
                float actualDistance = actualDistances[key];
                float ratio = averageTraveledDistance / actualDistance;

                Debug.Log($"Combination {key}: Average Traveled Distance = {averageTraveledDistance} units, Actual Distance = {actualDistance} units, Ratio = {ratio}");
            }
            else
            {
                Debug.LogWarning($"Actual distance not found for combination {key}");
            }
        }
    }
}
