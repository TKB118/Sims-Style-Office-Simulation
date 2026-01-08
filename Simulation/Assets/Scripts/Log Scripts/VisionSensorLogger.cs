using System;
using System.IO;
using UnityEngine;
using System.Text;

public class VisionSensorLogger : MonoBehaviour
{
    private string visionLogPath;
    private GameObject currentTarget;
    private float viewStartTime;

    public static string StepPrefix = ""; // <- 必ず末尾に '/' をつけて設定する

    void Start()
    {
        string npcName = gameObject.name;
        string npcId = gameObject.GetInstanceID().ToString();
        visionLogPath = Path.Combine(StepPrefix, $"VisionLog_{npcName}_{npcId}.csv");

        if (!File.Exists(visionLogPath))
        {
            var utf8bom = new UTF8Encoding(true);
            File.WriteAllText(visionLogPath, "Timestamp,NPC_ID,TargetName,ViewDuration(s),Distance\n", utf8bom);
        }
    }

    public void UpdateVision(GameObject newTarget, float distance)
    {
        float currentTime = Time.time;

        if (currentTarget != null && newTarget != currentTarget)
        {
            float duration = currentTime - viewStartTime;
            WriteLog(currentTarget, duration);
        }

        if (newTarget != currentTarget)
        {
            currentTarget = newTarget;
            viewStartTime = currentTime;
        }
    }

    private void WriteLog(GameObject target, float duration)
    {
        if (string.IsNullOrEmpty(visionLogPath)) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string npcId = gameObject.GetInstanceID().ToString();

        float distance = Vector3.Distance(transform.position, target.transform.position);
        string line = $"{timestamp},{npcId},{target.name},{duration:F2},{distance:F2}";

        File.AppendAllText(visionLogPath, line + "\n", Encoding.UTF8);
    }

    public void ForceLogFinalView()
    {
        if (currentTarget != null)
        {
            float duration = Time.time - viewStartTime;
            WriteLog(currentTarget, duration);
        }
    }

    void OnApplicationQuit()
    {
        ForceLogFinalView();
    }
}
