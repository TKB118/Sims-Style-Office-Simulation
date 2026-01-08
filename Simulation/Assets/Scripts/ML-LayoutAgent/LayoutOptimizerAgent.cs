using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class LayoutOptimizerAgent : Agent
{
    public Transform[] furnitureObjects;   // 配置対象の家具
    public Transform floorArea;           // 配置範囲（床オブジェクト）
    public SimulationController simulationController; // シミュレーション統括クラス

    private Vector2 floorMin, floorMax;
    private Vector3[] initialPositions;

    public override void Initialize()
    {
        // 床の範囲計算
        Renderer floorRenderer = floorArea.GetComponent<Renderer>();
        Bounds bounds = floorRenderer.bounds;
        floorMin = new Vector2(bounds.min.x, bounds.min.z);
        floorMax = new Vector2(bounds.max.x, bounds.max.z);

        // 初期位置記録
        initialPositions = new Vector3[furnitureObjects.Length];
        for (int i = 0; i < furnitureObjects.Length; i++)
        {
            initialPositions[i] = furnitureObjects[i].position;
        }
    }

    public override void OnEpisodeBegin()
    {
        // 家具位置をリセット
        for (int i = 0; i < furnitureObjects.Length; i++)
        {
            furnitureObjects[i].position = initialPositions[i];
        }

        RequestDecision(); // 初回のみ強制的に決定を促す
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // 家具ごとの現在位置（正規化）
        foreach (var obj in furnitureObjects)
        {
            Vector3 pos = obj.position;
            float normX = Mathf.InverseLerp(floorMin.x, floorMax.x, pos.x);
            float normZ = Mathf.InverseLerp(floorMin.y, floorMax.y, pos.z);
            sensor.AddObservation(normX);
            sensor.AddObservation(normZ);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived called");
        // 家具の新しい位置を反映
        var actionArray = actions.ContinuousActions;
        for (int i = 0; i < furnitureObjects.Length; i++)
        {
            float normX = Mathf.Clamp01(actionArray[i * 2]);
            float normZ = Mathf.Clamp01(actionArray[i * 2 + 1]);

            float x = Mathf.Lerp(floorMin.x, floorMax.x, normX);
            float z = Mathf.Lerp(floorMin.y, floorMax.y, normZ);
            Vector3 newPos = new Vector3(x, furnitureObjects[i].position.y, z);
            furnitureObjects[i].position = newPos;
        }

        // 評価シミュレーションを非同期で実行
        StartCoroutine(RunAndEvaluate());
    }

    private IEnumerator RunAndEvaluate()
    {
        Debug.Log("[LayoutOptimizerAgent] Starting RunOnce simulation...");

        yield return StartCoroutine(simulationController.RunOnce());

        Debug.Log("[LayoutOptimizerAgent] Simulation complete. Reading reward...");

        float reward = ReadEvaluationScore();
        SetReward(reward);

        EndEpisode();
    }

    private float ReadEvaluationScore()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "LayoutEvaluation.csv");
        if (System.IO.File.Exists(path))
        {
            var lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (line.StartsWith("TotalAverageScore"))
                {
                    var parts = line.Split(',');
                    if (float.TryParse(parts[1], out float score))
                        return score;
                }
            }
        }
        return 0f;
    }
}
