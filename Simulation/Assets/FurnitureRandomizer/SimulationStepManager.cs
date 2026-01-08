using UnityEngine;
using System.IO;

public class SimulationStepManager : MonoBehaviour
{
    public string stepPrefix = "Step1_";

    public void SaveAllLogs()
    {
        string stepDir = Path.Combine(Application.persistentDataPath, stepPrefix);
        Directory.CreateDirectory(stepDir);

        LayoutEvaluationManager.StepPrefix = stepDir + "/";
        TravelDistanceLogger.StepPrefix = stepDir + "/";
        VisionSensorLogger.StepPrefix = stepDir + "/";
        ProximityCommunicationTracker.StepPrefix = stepDir + "/"; 


        foreach (var hgd in FindObjectsOfType<HeatmapGridData>())
        {
            hgd.SaveCSV(Path.Combine(stepDir, "HeatmapData.csv"));
            hgd.SaveHeatmapImage(Path.Combine(stepDir, "HeatmapImage.png"));
        }

        foreach (var vs in FindObjectsOfType<VisionSensorLogger>())
            vs.ForceLogFinalView();

        foreach (var td in FindObjectsOfType<TravelDistanceLogger>())
            td.Flush();

     
        if (ProximityCommunicationTracker.Instance != null)
        {
            int stepIndex = GetCurrentStepIndex();
            ProximityCommunicationTracker.Instance.FinalizeAndSave(stepIndex);
        }

        LayoutEvaluationManager.ForceSaveIfNeeded();

        Debug.Log($"All logs saved to: {stepDir}");
    }

    private int GetCurrentStepIndex()
    {
        string folder = new DirectoryInfo(stepPrefix).Name;
        if (folder.StartsWith("Step_") && int.TryParse(folder.Substring(5), out int result))
            return result;
        return 0;
    }



}
