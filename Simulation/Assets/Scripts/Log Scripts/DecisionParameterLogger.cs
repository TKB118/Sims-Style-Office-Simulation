using UnityEngine;
using System.IO;
using System;

public class DecisionParameterLogger : MonoBehaviour
{
    private const string FILE_NAME = "AI_DecisionLog.csv";
    private string fullPath;

    void Awake()
    {
        fullPath = Path.Combine(Application.dataPath, FILE_NAME);
        InitializeLogFile();
    }

    private void InitializeLogFile()
    {
        if (!File.Exists(fullPath))
        {
            // Define the header columns for the regression analysis
            string header = "Time,NPCName,ChosenInteraction,SelectedScore,TopScore,TimeToDecision,AllStatDecayData\n";
            File.WriteAllText(fullPath, header);
            Debug.Log($"Initialized new Decision Log file at: {fullPath}");
        }
    }

    /// <summary>
    /// Logs the AI's internal state parameters at the moment a decision is made.
    /// </summary>
    public void LogDecision(string npcName, string chosenInteraction, float selectedScore, float topScore, string allStatDecayData, float timeToDecision)
    {
        // Format the log entry string
        string logEntry = string.Format("{0},{1},{2},{3:F4},{4:F4},{5:F4},{6:F4}\n",
            DateTime.Now.ToString("HH:mm:ss.fff"),
            npcName,
            chosenInteraction.Replace(',', ' '), // Prevent commas in the interaction name from breaking the CSV
            selectedScore,
            topScore,
            timeToDecision,
            allStatDecayData
        );

        // Append the entry to the dedicated log file
        try
        {
            File.AppendAllText(fullPath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to decision parameter log: {e.Message}");
        }
    }
}