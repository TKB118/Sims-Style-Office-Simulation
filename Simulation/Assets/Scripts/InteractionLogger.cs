using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

public class InteractionLogger : MonoBehaviour
{
    [SerializeField] private string npcId = "NPC_1";

    private string logFilePath;
    private Vector3 lastPosition;
    private float moveStartTime;
    private string lastObject;
    private float stayStartTime;
    private float hungerBefore;
    public static string StepPrefix = "";


    private void Start()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, StepPrefix + npcId + "_log.csv");

        if (!File.Exists(logFilePath))
            File.WriteAllText(logFilePath, "Timestamp,NPC_ID,From,To,TraveledDistance,StraightDistance,MoveTime,StayDuration,NeedType,NeedDelta\n");

        lastPosition = transform.position;
        moveStartTime = Time.time;
    }

    public void LogDecision(string npcName, string chosenInteraction, float selectedScore, float topScore, string allStatDecayData, float timeToDecision)
    {
        // Define the data fields for the regression analysis
        string logEntry = string.Format("{0},{1},{2},{3:F4},{4:F4},{5:F4},{6:F4}\n",
            System.DateTime.Now.ToString("HH:mm:ss.fff"),
            npcName,
            chosenInteraction,
            selectedScore,
            topScore,
            timeToDecision,
            allStatDecayData // This column will hold all stat values and decay rates
        );

        // Placeholder for your actual file writing logic
        // Example: WriteLogLine(logEntry, "DecisionLog.csv"); 
        
        // NOTE: Ensure your DecisionLog.csv file is created with the header:
        // Time,NPCName,ChosenInteraction,SelectedScore,TopScore,TimeToDecision,AllStatDecayData
    }

    public void OnStartMove(string fromObject)
    {
        lastPosition = transform.position;
        lastObject = fromObject;
        moveStartTime = Time.time;
    }

    public void OnEndMove(string toObject)
    {
        Vector3 currentPosition = transform.position;
        float traveledDistance = Vector3.Distance(lastPosition, currentPosition);
        float straightDistance = Vector3.Distance(GameObject.Find(lastObject).transform.position, GameObject.Find(toObject).transform.position);
        float moveTime = Time.time - moveStartTime;

        Log($"Move,{npcId},{lastObject},{toObject},{traveledDistance},{straightDistance},{moveTime},,,,");
    }

    public void OnStartInteraction(string objName, string needType, float hungerBeforeInteraction)
    {
        stayStartTime = Time.time;
        lastObject = objName;
        hungerBefore = hungerBeforeInteraction;
    }

    public void OnEndInteraction(string objName, string needType, float hungerAfterInteraction)
    {
        float stayDuration = Time.time - stayStartTime;
        float needDelta = hungerBefore - hungerAfterInteraction;

        Log($"Interaction,{npcId},,,,{stayDuration},,{needType},{needDelta}");
    }

    private void Log(string line)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.AppendAllText(logFilePath, timestamp + "," + line + "\n");
    }
}
