using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DistanceDataManager : MonoBehaviour
{
    public static DistanceDataManager Instance { get; private set; }

    public Dictionary<string, List<float>> LoggedDistances { get; private set; } = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> CalculatedDistances { get; private set; } = new Dictionary<string, List<float>>();
    public Dictionary<string, float> Ratios { get; private set; } = new Dictionary<string, float>();

    public void AddRatio(string key, float ratio)
    {
        if (!Ratios.ContainsKey(key))
        {
            Ratios[key] = ratio;
        }
        else
        {
            Ratios[key] = ratio; // Overwrite existing ratio if key already exists
        }
    }

    public void SaveDataToFile(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (var entry in LoggedDistances)
            {
                writer.WriteLine($"Logged-{entry.Key}:{string.Join(",", entry.Value)}");
            }
            foreach (var entry in CalculatedDistances)
            {
                writer.WriteLine($"Calculated-{entry.Key}:{string.Join(",", entry.Value)}");
            }
            foreach (var entry in Ratios)
            {
                writer.WriteLine($"Ratio-{entry.Key}:{entry.Value}");
            }
        }
    }

    public void LoadDataFromFile(string path)
    {
        if (!File.Exists(path)) return;

        using (StreamReader reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    string[] keyParts = parts[0].Split('-');
                    if (keyParts.Length != 2) continue;

                    string type = keyParts[0];
                    string key = keyParts[1];

                    if (type == "Logged")
                    {
                        List<float> distances = ParseFloatList(parts[1]);
                        LoggedDistances[key] = distances;
                    }
                    else if (type == "Calculated")
                    {
                        List<float> distances = ParseFloatList(parts[1]);
                        CalculatedDistances[key] = distances;
                    }
                    else if (type == "Ratio")
                    {
                        if (float.TryParse(parts[1], out float ratio))
                        {
                            Ratios[key] = ratio;
                        }
                    }
                }
            }
        }
    }

    private List<float> ParseFloatList(string input)
    {
        List<float> values = new List<float>();
        foreach (string value in input.Split(','))
        {
            if (float.TryParse(value, out float result))
            {
                values.Add(result);
            }
        }
        return values;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddDistance(string key, float distance, bool isLogged)
    {
        var targetDictionary = isLogged ? LoggedDistances : CalculatedDistances;

        if (!targetDictionary.ContainsKey(key))
        {
            targetDictionary[key] = new List<float>();
        }
        targetDictionary[key].Add(distance);
    }

    
}
