using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Tag_Wander : MonoBehaviour
{
    public Transform Bed;
    public Transform TV;
    public Transform Toilet;
    public Transform Kitchen;
    public Transform Sofa;

    private NavMeshAgent agent;
    public float wanderTimer = 10f;
    private float timer;
    private List<Transform> destinations;

    private Vector3 lastPosition;
    private float totalDistanceMoved;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;

        // Initialize the list of destinations
        destinations = new List<Transform> { Bed, TV, Toilet, Kitchen, Sofa };

        lastPosition = transform.position;
        totalDistanceMoved = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= wanderTimer)
        {
            SetRandomDestination();
            timer = 0;
        }

        // Track distance moved
        TrackDistanceMoved();
    }

    void SetRandomDestination()
    {
        if (destinations.Count == 0)
            return;

        Transform newDestination = null;
        do
        {
            newDestination = destinations[Random.Range(0, destinations.Count)];
        } while (newDestination == null || newDestination.position == agent.destination);

        agent.SetDestination(newDestination.position);
    }

    void TrackDistanceMoved()
    {
        float distanceTravelled = Vector3.Distance(transform.position, lastPosition);
        totalDistanceMoved += distanceTravelled;
        lastPosition = transform.position;
    }

    void OnDestroy()
    {
        
        Debug.Log("Total distance moved: " + totalDistanceMoved + " meters");
    }
}

