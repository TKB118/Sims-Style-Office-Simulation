using System.Collections.Generic;
using UnityEngine;

public class AverageActualDistanceRatio : MonoBehaviour
{
    private string dataPath;

    void Start()
    {
        dataPath = Application.persistentDataPath + "/distanceData.txt";
        Debug.Log(Application.persistentDataPath);
        DistanceDataManager.Instance.LoadDataFromFile(dataPath);
    }

    void OnApplicationQuit()
    {
        OnSceneEnd();

        // Log the data path before saving
        Debug.Log($"Saving distance data to: {dataPath}");
        DistanceDataManager.Instance.SaveDataToFile(dataPath);
        Debug.Log("Distance data saved to file on application quit.");
    }


    private void OnSceneEnd()
    {
        var loggedData = DistanceDataManager.Instance.LoggedDistances;
        var calculatedData = DistanceDataManager.Instance.CalculatedDistances;

        foreach (var entry in loggedData)
        {
            string key = entry.Key;

            if (!loggedData.TryGetValue(key, out List<float> loggedDistances) || loggedDistances.Count == 0)
            {
                Debug.LogWarning($"No logged distances found for {key}");
                continue;
            }

            float loggedAverage = CalculateAverage(loggedDistances);

            if (!calculatedData.TryGetValue(key, out List<float> calculatedDistances) || calculatedDistances.Count == 0)
            {
                Debug.LogWarning($"No calculated distances found for {key}");
                continue;
            }

            float calculatedAverage = CalculateAverage(calculatedDistances);
            float ratio = loggedAverage / calculatedAverage;

            // Save the ratio to DistanceDataManager
            DistanceDataManager.Instance.AddRatio(key, ratio);

            Debug.Log($"[Application Quit] Route: {key}, Logged Average: {loggedAverage}, Calculated Average: {calculatedAverage}, Ratio: {ratio}");
        }
    }


    private float CalculateAverage(List<float> distances)
    {
        if (distances.Count == 0) return 0.0f;

        float total = 0.0f;
        foreach (float distance in distances)
        {
            total += distance;
        }
        return total / distances.Count;
    }
}
