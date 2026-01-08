using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceAccumulator : MonoBehaviour
{
    private List<SmartObject> smartObjects;
    private Dictionary<string, float> finalDistances = new Dictionary<string, float>();
    private Dictionary<string, List<float>> traveledDistances = new Dictionary<string, List<float>>();

    // Reference to the NotSoSimpleAI script
    private NotSoSimpleAI aiController;
    private Transform currentDestination;
    private float currentTravelDistance = 0.0f;
    private Vector3 lastPosition;
    private bool isMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(smartObjects);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
