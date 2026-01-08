using UnityEngine;
using System.Collections;

public class SimulationController : MonoBehaviour
{
    public FurnitureRandomizer furnitureRandomizer;
    public SimulationNPCController npcController;
    public SimulationStepManager stepManager;
    public TakeScreenshot screenshotTaker; 
    public ProximityCommunicationTracker proximityTracker;
    public bool Randomize = false;


    public float stepDuration = 30f;
    public int maxSteps = 10;

    private int currentStep = 1;

    private void Start()
    {
        StartCoroutine(SimulationLoop());
    }

    public IEnumerator RunOnce()
{
    Debug.Log("[RunOnce] Running simulation for MLAgent evaluation...");

    // 家具はAgentが配置済み

    // NPCをスポーン
    npcController.RespawnNPCs();
    yield return new WaitForSeconds(2f); // 安定化時間

    // 実時間180秒（＝3時間相当）のシミュレーション時間を持つ
    float duration = 180f;
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        yield return null;
    }

    // 評価ログ出力
    stepManager.SaveAllLogs();

    Debug.Log("[RunOnce] Simulation complete.");
}


    private IEnumerator SimulationLoop()
    {
        while (currentStep <= maxSteps)
        {
            Debug.Log($"[Step {currentStep}] Starting simulation step.");

            // === フォルダ作成 ===
            string stepDirName = $"Step_{currentStep}";
            string stepDirPath = System.IO.Path.Combine(Application.persistentDataPath, stepDirName);
            System.IO.Directory.CreateDirectory(stepDirPath);

            // === 各種マネージャーにステップ情報を通知 ===
            string stepPrefix = stepDirPath + "/";
            stepManager.stepPrefix = stepPrefix;
            LayoutEvaluationManager.StepPrefix = stepPrefix;
            TravelDistanceLogger.StepPrefix = stepPrefix;
            VisionSensorLogger.StepPrefix = stepPrefix;
            ProximityCommunicationTracker.StepPrefix = stepPrefix;
            NpcActivityLogger.StepPrefix = stepPrefix; // Added for NPC logs

            // === 1. Cleanup Old NPCs ===
            npcController.DestroyAllNPCs();
            yield return new WaitForSeconds(0.2f); // Wait for destruction

            // === 2. Randomize Furniture (if enabled) ===
            if (Randomize)
            {
                yield return StartCoroutine(furnitureRandomizer.RandomizeFurniturePositions()); 
            }

            // === 3. Reset Interactions (Now safe as NPCs are gone) ===
            ResetAllInteractions();
            yield return null;
            
            // === NPCスポーン前にスクリーンショット撮影 ===
            if (screenshotTaker != null)
            {
                string imgPath = System.IO.Path.Combine(stepDirPath, "FloorPlanImage.png");
                screenshotTaker.Capture(imgPath);
                yield return null;
            }

            // === NPCスポーン ===
            npcController.SpawnNPCs();

            // === シミュレーション待機 ===
            yield return new WaitForSeconds(stepDuration);

            // === ログ保存 ===
            stepManager.SaveAllLogs();

            currentStep++;
        }

        Debug.Log("Simulation complete.");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // エディタでの停止
    #else
        Application.Quit(); // ビルド版での終了
    #endif
    }


    private void ResetAllInteractions()
    {
        int totalCleared = 0;

        foreach (var smartObject in SmartObjectManager.Instance.RegisteredObjects)
        {
            foreach (var interaction in smartObject.Interactions)
            {
                if (interaction is SimpleInteraction simple)
                {
                    simple.ForceClear();
                    totalCleared++;
                }
            }
        }

        Debug.Log($"[SimulationController] Reset {totalCleared} interactions.");
    }


}
