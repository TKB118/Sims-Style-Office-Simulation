using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections;

public class FurniturePlacementAgent : Agent
{
    [SerializeField] private FurnitureReplacer furnitureReplacer;
    [SerializeField] private float floorWidth = 10f;
    [SerializeField] private float floorDepth = 10f;
    [SerializeField] private HeatmapGridData heatmapData;
    [SerializeField] private float simulationDuration = 10f; // NPCシミュレーションを待つ秒数
    private int furnitureCount;
    private Vector3[] currentPositions;
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private int npcCount = 3;
    [SerializeField] private NPCManager npcManager;

    private bool isSimulating = false;

    public override void Initialize()
    {
        Transform furnitureParent = furnitureReplacer.layoutRoot.transform.Find(furnitureReplacer.furnitureParentName);
        furnitureCount = furnitureParent.childCount;
        currentPositions = new Vector3[furnitureCount];
    }

    private GameObject previousLayoutRoot = null;
    

    public override void OnEpisodeBegin()
    {
        // 1. 古いNPC削除
        npcManager.ClearNPCs();

        // 2. 古いSmartObjectを無効化（前回レイアウトがあれば）
        if (previousLayoutRoot != null)
        {
            var oldSmartObjects = previousLayoutRoot.GetComponentsInChildren<SmartObject>();
            foreach (var so in oldSmartObjects)
            {
                so.enabled = false; // SmartObjectスクリプトを無効化
            }
        }

        // 3. 新レイアウトを複製し、現在のRootに設定
        GameObject newLayout = furnitureReplacer.DuplicateLayoutAndReturnRoot();
        previousLayoutRoot = newLayout;

        // 4. SmartObjectManagerに登録（古いものはすでに無効なので登録対象外）
        var manager = newLayout.GetComponent<SmartObjectManager>();
        var smartObjects = newLayout.GetComponentsInChildren<SmartObject>();
        foreach (var so in smartObjects)
        {
            manager.RegisterSmartObject(so);
        }

        // 5. NPCを新レイアウト位置にスポーン
        npcManager.SpawnNPCsAt(newLayout.transform, npcPrefab, npcCount);
    }




    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var pos in currentPositions)
        {
            sensor.AddObservation(pos.x / (floorWidth / 2f));
            sensor.AddObservation(pos.z / (floorDepth / 2f));
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isSimulating) return; // 連打防止

        var actionArray = actions.ContinuousActions;
        int index = 0;

        for (int i = 0; i < furnitureCount; i++)
        {
            float normX = Mathf.Clamp(actionArray[index++], -1f, 1f);
            float normZ = Mathf.Clamp(actionArray[index++], -1f, 1f);

            float x = normX * (floorWidth / 2f);
            float z = normZ * (floorDepth / 2f);

            currentPositions[i] = new Vector3(x, 0f, z);
        }

        furnitureReplacer.ApplyFurnitureLayout(currentPositions);

        // NPCシミュレーションを非同期で開始し、評価完了後に報酬を与える
        StartCoroutine(RunSimulationAndEvaluate());
    }

    private IEnumerator RunSimulationAndEvaluate()
    {
        isSimulating = true;

        npcManager.StartSimulation();

        yield return new WaitForSeconds(simulationDuration);

        var evaluationResults = LayoutEvaluationManager.GetLatestEvaluationResults();
        int badRatioCount = LayoutEvaluationManager.GetBadRatioCount();

        // 動線効率 Good評価
        float goodReward = evaluationResults.GoodCount * 1.0f;

        // 滞在時間バランス
        float heatmapStdDev = heatmapData.GetStayTimeStdDev();
        float heatmapReward = Mathf.Clamp01(1f - (heatmapStdDev / 10f)) * 5.0f;

        // Bad評価による減点
        float badPenalty = evaluationResults.BadCount * -1.0f;

        // 距離比率Badによる減点
        float ratioPenalty = badRatioCount * -0.5f;

        // 合計報酬
        float totalReward = goodReward + heatmapReward + badPenalty + ratioPenalty;

        // デバッグログ出力
        Debug.Log($"[Reward Breakdown] GoodReward: {goodReward}, HeatmapReward: {heatmapReward}, BadPenalty: {badPenalty}, RatioPenalty: {ratioPenalty}, TotalReward: {totalReward}");

        AddReward(totalReward);

        EndEpisode();

        isSimulating = false;
    }
    
    
}
