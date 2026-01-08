using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class HeatmapGridData : MonoBehaviour
{
    public float cellSize = 1f; // 1m単位分割
    public Gradient heatmapGradient;
    public float maxStayTime = 60f;

    private Vector2 areaSize;
    private float[,] stayTimes;
    private int cellsX, cellsY;

    void Start()
    {
        // === 面積自動検出 ===
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Vector3 size = rend.bounds.size;
            areaSize = new Vector2(size.x, size.z);
        }
        else
        {
            Debug.LogError("HeatmapGridData: Renderer not found. Please attach to a floor object.");
            areaSize = new Vector2(8, 10); // フォールバック
        }

        cellsX = Mathf.CeilToInt(areaSize.x / cellSize);
        cellsY = Mathf.CeilToInt(areaSize.y / cellSize);
        stayTimes = new float[cellsX, cellsY];
    }

    public void RegisterStay(Vector3 worldPos, float deltaTime)
    {
        Vector3 local = worldPos - transform.position;
        int x = Mathf.FloorToInt((local.x + areaSize.x / 2) / cellSize);
        int y = Mathf.FloorToInt((local.z + areaSize.y / 2) / cellSize);

        if (x >= 0 && x < cellsX && y >= 0 && y < cellsY)
        {
            stayTimes[x, y] += deltaTime;
        }
    }

    public void SaveCSV(string filePath)
    {
        using StreamWriter writer = new StreamWriter(filePath)
        {
            AutoFlush = true
        };
        writer.WriteLine("X,Y,StayTime");
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                writer.WriteLine($"{x},{y},{stayTimes[x, y]:F2}");
            }
        }
    }

    public void SaveHeatmapImage(string filePath)
    {
        Texture2D texture = new Texture2D(cellsX, cellsY);

        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                float normalizedStay = Mathf.Clamp01(stayTimes[x, y] / maxStayTime);
                Color color = heatmapGradient.Evaluate(normalizedStay);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);
        Debug.Log($"Heatmap Image saved: {filePath}");
    }

    public float GetStayTimeStdDev()
    {
        List<float> allStayTimes = new List<float>();

        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                allStayTimes.Add(stayTimes[x, y]);
            }
        }

        float avg = allStayTimes.Average();
        float variance = allStayTimes.Select(t => Mathf.Pow(t - avg, 2)).Average();
        float stdDev = Mathf.Sqrt(variance);

        return stdDev;
    }


    void OnApplicationQuit()
    {
        string csvPath = Path.Combine(Application.persistentDataPath, "HeatmapData.csv");
        string imgPath = Path.Combine(Application.persistentDataPath, "HeatmapImage.png");

        SaveCSV(csvPath);
        SaveHeatmapImage(imgPath);

        Debug.Log($"Heatmap CSV + Image saved to {Application.persistentDataPath}");
    }
}