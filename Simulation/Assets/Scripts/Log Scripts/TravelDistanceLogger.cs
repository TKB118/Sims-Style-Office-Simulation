using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.IO;

public class TravelDistanceLogger : MonoBehaviour
{
    private Vector3 lastPosition;
    private float distanceTraveled = 0f;
    private NavMeshAgent agent;
    private NotSoSimpleAI aiComponent;
    private bool isMoving = false;
    private string logFilePath;

    public static string StepPrefix = ""; // e.g., Application.persistentDataPath + "/Step_1/"
    

    private SmartObject currentSmartObject = null;
    private SmartObject previousSmartObject = null;

    public Dictionary<SmartObjectPair, PairStats> distanceStats = new Dictionary<SmartObjectPair, PairStats>();
    public static Dictionary<SmartObjectPair, float> finalDistances = null;

    public Dictionary<SmartObjectPair, (float AverageDistance, float FinalDistance, float Ratio, string RatioEvaluation)> RatioStats { get; private set; } = new();
    public bool IsCalculationComplete { get; private set; } = false;

    void Start()
    {
        lastPosition = transform.position;
        agent = GetComponent<NavMeshAgent>();
        aiComponent = GetComponent<NotSoSimpleAI>();

        string npcName = gameObject.name;
        string npcId = gameObject.GetInstanceID().ToString();
        logFilePath = Path.Combine(StepPrefix, $"{npcName}_{npcId}_TravelLog.csv");

        if (!File.Exists(logFilePath))
        {
            using var writer = new StreamWriter(logFilePath, false, new System.Text.UTF8Encoding(true));
            writer.WriteLine("From,To,Type,Distance");
        }

        aiComponent.OnNewObjectSelected.AddListener(OnNewDestinationSelected);

        CalculateFinalDistances();
    }

    void Update()
    {
        if (isMoving)
        {
            float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
            distanceTraveled += distanceThisFrame;
            lastPosition = transform.position;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    OnDestinationReached();
                }
            }
        }
    }

    void OnNewDestinationSelected(Transform destinationTransform)
    {
        if (destinationTransform != null)
        {
            isMoving = true;
            lastPosition = transform.position;
            distanceTraveled = 0f;
            currentSmartObject = destinationTransform.GetComponentInParent<SmartObject>();
        }
    }

    void OnDestinationReached()
    {
        string from = previousSmartObject?.name ?? "Start";
        string to = currentSmartObject.name;
        string csvLine = $"{from},{to},Traveled,{distanceTraveled}";
        File.AppendAllText(logFilePath, csvLine + "\n", System.Text.Encoding.UTF8);

        if (previousSmartObject != null && currentSmartObject != null && previousSmartObject != currentSmartObject)
        {
            var pair = new SmartObjectPair(previousSmartObject, currentSmartObject);

            if (!distanceStats.ContainsKey(pair))
            {
                distanceStats[pair] = new PairStats();
            }

            distanceStats[pair].TotalDistance += distanceTraveled;
            distanceStats[pair].TravelCount += 1;

            float avg = distanceStats[pair].TotalDistance / distanceStats[pair].TravelCount;
            File.AppendAllText(logFilePath, $"{from},{to},Total,{distanceStats[pair].TotalDistance}\n", System.Text.Encoding.UTF8);
            File.AppendAllText(logFilePath, $"{from},{to},Average,{avg}\n", System.Text.Encoding.UTF8);
        }

        previousSmartObject = currentSmartObject;
        distanceTraveled = 0f;
        isMoving = false;
    }

    public void Flush()
    {
        Debug.Log($"[TravelDistanceLogger] Flushing for {gameObject.name}");

        foreach (var entry in distanceStats)
        {
            var pair = entry.Key;
            var stats = entry.Value;

            if (stats.TravelCount > 0 && finalDistances.ContainsKey(pair))
            {
                float averageDistance = stats.TotalDistance / stats.TravelCount;
                float finalDistance = finalDistances[pair];
                float ratio = averageDistance / finalDistance;
                string ratioEvaluation = EvaluateRatio(ratio);

                RatioStats[pair] = (averageDistance, finalDistance, ratio, ratioEvaluation);
                Debug.Log($"[Evaluation] {pair.ObjectA.name}-{pair.ObjectB.name} => {ratioEvaluation} ({ratio:F2})");
            }
        }

        IsCalculationComplete = true;
        LayoutEvaluationManager.SubmitRatioStats(this.RatioStats);
    }

    private string EvaluateRatio(float ratio)
    {
        if (ratio < 1) return "Good";
        if (ratio <= 2) return "Good";
        if (ratio <= 3) return "Acceptable";
        return "Bad";
    }

    private void CalculateFinalDistances()
    {
        finalDistances = new Dictionary<SmartObjectPair, float>();
        SmartObject[] smartObjects = FindObjectsOfType<SmartObject>();

        for (int i = 0; i < smartObjects.Length; i++)
        {
            for (int j = i + 1; j < smartObjects.Length; j++)
            {
                SmartObject objA = smartObjects[i];
                SmartObject objB = smartObjects[j];
                float distance = Vector3.Distance(objA.InteractionPoint, objB.InteractionPoint);
                var pair = new SmartObjectPair(objA, objB);
                finalDistances[pair] = distance;
            }
        }
    }

    void OnDestroy()
    {
        if (aiComponent != null)
        {
            aiComponent.OnNewObjectSelected.RemoveListener(OnNewDestinationSelected);
        }
    }

    public class SmartObjectPair
    {
        public SmartObject ObjectA;
        public SmartObject ObjectB;

        public SmartObjectPair(SmartObject a, SmartObject b)
        {
            if (a.GetInstanceID() < b.GetInstanceID()) { ObjectA = a; ObjectB = b; }
            else { ObjectA = b; ObjectB = a; }
        }

        public override bool Equals(object obj)
        {
            return obj is SmartObjectPair other && ObjectA == other.ObjectA && ObjectB == other.ObjectB;
        }

        public override int GetHashCode()
        {
            return ObjectA.GetInstanceID() * 31 + ObjectB.GetInstanceID();
        }
    }

    public class PairStats
    {
        public float TotalDistance = 0f;
        public int TravelCount = 0;
    }
}
