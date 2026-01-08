using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceCalculator : MonoBehaviour
{
    private List<SmartObject> smartObjects;

    void Start()
    {
        // Retrieve the list of registered SmartObjects from the SmartObjectManager
        smartObjects = SmartObjectManager.Instance.RegisteredObjects;
        CalculateDistances();
    }

    private void CalculateDistances()
    {
        for (int i = 0; i < smartObjects.Count; i++)
        {
            for (int j = i + 1; j < smartObjects.Count; j++)
            {
                SmartObject objA = smartObjects[i];
                SmartObject objB = smartObjects[j];

                float distance = Vector3.Distance(objA.InteractionPoint, objB.InteractionPoint);
                Debug.Log($"Distance between {objA.DisplayName} and {objB.DisplayName}: {distance} units");
            }
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Scene is ending, calculating final distances:");
        CalculateDistances();
    }
}
