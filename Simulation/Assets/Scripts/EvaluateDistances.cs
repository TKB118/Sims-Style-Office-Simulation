using UnityEngine;
using System.Collections;

public class EvaluateDistances : MonoBehaviour
{
    private TravelDistanceLogger distanceTracker;

    void Start()
    {
        distanceTracker = GetComponent<TravelDistanceLogger>();

        if (distanceTracker == null)
        {
            Debug.LogError("TravelDistanceLogger component not found on this GameObject.");
            return;
        }

        StartCoroutine(WaitForCalculation());
    }

    public IEnumerator WaitForCalculation()
    {
        while (!distanceTracker.IsCalculationComplete)
        {
            yield return null; // Wait until calculations are done
        }
        EvaluateFloorPlan();
    }

    public void EvaluateFloorPlan()
    {
        if (distanceTracker.RatioStats == null || distanceTracker.RatioStats.Count == 0 || !Application.isEditor)
        {
            Debug.LogWarning("No data available or ratios have not been calculated yet to evaluate the floor plan.");
            return;
        }

        foreach (var entry in distanceTracker.RatioStats)
        {
            var pair = entry.Key;
            var stats = entry.Value;
            float averageDistance = stats.AverageDistance;

            string evaluation;

            if (averageDistance < 20)
            {
                evaluation = "Good";
            }
            else if (averageDistance >= 20 && averageDistance <= 50)
            {
                evaluation = "Acceptable";
            }
            else
            {
                evaluation = "Bad";
            }

            Debug.Log($"Evaluation for {pair.ObjectA.name}-{pair.ObjectB.name}: {evaluation}. Average Distance: {averageDistance} units.");
        }
    }
}
