using UnityEngine;
using System.Collections; // コルーチン用
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq; 

public class NpcActivityLogger : MonoBehaviour
{
    public static string StepPrefix = ""; // Added for simulation loop support
    private string currentLogFilePath;
    private string logDirectory;
    private CommonAIBase AIBase;

    // 状態管理
    private bool isInteracting = false; 
    private string currentStatName = ""; 
    private string currentLocation = ""; 
    private float interactionStartTime = 0f;

    // 基本情報
    private string npcName;
    private string npcId;

    // キャッシュ用変数 (初期値)
    private string cachedTraitNames = "NoTrait"; // 複数対応のため複数形に
    private string cachedDecayId = "NoStats";
    private bool attributesCaptured = false;

    void Start()
    {
        AIBase = GetComponent<CommonAIBase>();
        if (AIBase == null)
        {
            Debug.LogError($"[{gameObject.name}] CommonAIBase missing.");
            enabled = false;
            return;
        }

        string basePath = !string.IsNullOrEmpty(StepPrefix) ? StepPrefix : Application.persistentDataPath; 
        logDirectory = basePath; 
        if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

        npcName = gameObject.name;
        npcId = gameObject.GetInstanceID().ToString();

        // 1. 仮ファイル名でログ記録開始
        string tempFileName = $"{npcName}_{npcId}_TEMP.csv";
        currentLogFilePath = Path.Combine(logDirectory, tempFileName);

        try
        {
            if (File.Exists(currentLogFilePath)) File.Delete(currentLogFilePath);
            string header = "Timestamp,State,StatName,Location,Duration";
            File.AppendAllText(currentLogFilePath, header + "\n", System.Text.Encoding.UTF8);
        }
        catch (IOException) { }

        // 2. 属性情報の遅延取得を開始 (コルーチン)
        StartCoroutine(CaptureAttributesDelayed());
    }

    // 1秒待ってから属性を取得するコルーチン
    IEnumerator CaptureAttributesDelayed()
    {
        // 提案通り1秒待機 (Traitの割り振りを待つ)
        yield return new WaitForSeconds(1.0f);

        CaptureAttributes();
    }

    private void CaptureAttributes()
    {
        try
        {
            // --- Trait の取得 (複数対応 & None除外) ---
            if (AIBase.PublicTraits != null)
            {
                // LINQを使ってフィルタリング
                // 1. nullでない
                // 2. 名前が "None" でない (インスペクター上の空スロット対策)
                // 3. 名前が空文字でない
                var validTraits = AIBase.PublicTraits
                    .Where(t => t != null && t.name != "None" && !string.IsNullOrEmpty(t.name))
                    .Select(t => t.name)
                    .ToList();

                if (validTraits.Count > 0)
                {
                    // 複数のTraitがある場合、ハイフンでつなぐ (例: Diligent-Social)
                    cachedTraitNames = string.Join("-", validTraits);
                    Debug.Log($"[{npcName}] Traits Captured: {cachedTraitNames}");
                }
                else
                {
                    Debug.LogWarning($"[{npcName}] No valid traits found after waiting.");
                }
            }

            // --- Stats の取得 ---
            var decayRates = new List<string>();
            if (AIBase.PublicStats != null)
            {
                foreach (var statConfig in AIBase.PublicStats)
                {
                    if (statConfig != null && statConfig.LinkedStat != null)
                    {
                        string rawName = statConfig.LinkedStat.name;
                        // 名前整形 (AIStat_Bladder -> B)
                        string cleanName = rawName.Replace("AIStat_", "").Replace("Stat_", "");
                        string statInitial = cleanName.Substring(0, 1).ToUpper();

                        // 値取得 (Override_DecayRate)
                        float overrideVal = statConfig.Override_DecayRate;
                        string rateValue = (overrideVal * 10000000).ToString("0000");

                        decayRates.Add($"{statInitial}{rateValue}");
                    }
                }
            }

            if (decayRates.Count > 0)
            {
                cachedDecayId = string.Join("_", decayRates);
                Debug.Log($"[{npcName}] Stats Captured: {cachedDecayId}");
            }

            attributesCaptured = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{npcName}] CaptureAttributes Failed: {e.Message}");
        }
    }

    void Update()
    {
        if (string.IsNullOrEmpty(currentLogFilePath) || AIBase == null) return;
        CheckAiState();
    }

    void OnDestroy()
    {
        FinalizeLogFile();
    }

    private void FinalizeLogFile()
    {
        if (string.IsNullOrEmpty(currentLogFilePath) || !File.Exists(currentLogFilePath)) return;

        // キャッシュしておいた情報を使ってリネーム
        // 例: NPC_33_Diligent-Social_W0050_H0100_Log.csv
        string finalFileName = $"{npcName}_{cachedTraitNames}_{cachedDecayId}_Log.csv";
        string finalPath = Path.Combine(logDirectory, finalFileName);

        try
        {
            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(currentLogFilePath, finalPath);
            Debug.Log($"[{npcName}] Log finalized and renamed to: {finalFileName}");
        }
        catch (IOException e)
        {
            Debug.LogError($"[{npcName}] Failed to rename log: {e.Message}");
        }
    }

    // --- ログ監視ロジック ---
    private void CheckAiState()
    {
        bool aiHasActiveInteraction = (AIBase.CurrentInteractionPublic != null);

        if (aiHasActiveInteraction && !isInteracting)
        {
            isInteracting = true;
            interactionStartTime = Time.time;
            GetInteractionDetails();
            WriteLog("Start", currentStatName, currentLocation, 0f);
        }
        else if (!aiHasActiveInteraction && isInteracting)
        {
            isInteracting = false;
            float duration = Time.time - interactionStartTime;
            WriteLog("End", currentStatName, currentLocation, duration);
            currentStatName = "";
            currentLocation = "";
        }
    }

    private void GetInteractionDetails()
    {
        currentStatName = "Unknown";
        currentLocation = "Unknown"; 

        if (AIBase.CurrentInteractionPublic == null) return;
        
        if (AIBase.CurrentInteractionPublic.gameObject != null)
        {
            currentLocation = AIBase.CurrentInteractionPublic.gameObject.name;
        }
        
        var simpleInt = AIBase.CurrentInteractionPublic.gameObject.GetComponent<SimpleInteraction>();
        
        if (simpleInt == null) return;

        if (simpleInt.StatChanges != null && simpleInt.StatChanges.Length > 0)
        {
            var statChange = simpleInt.StatChanges[0];
            if (statChange.LinkedStat != null) 
            {
                currentStatName = statChange.LinkedStat.name;
            } 
        }
    }

    private void WriteLog(string state, string statName, string location, float duration)
    {
        if (string.IsNullOrEmpty(currentLogFilePath)) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string durationStr = (state == "End") ? duration.ToString("F2") : "";
        string line = $"{timestamp},{state},{statName},{location},{durationStr}";
        try
        {
            File.AppendAllText(currentLogFilePath, line + "\n", System.Text.Encoding.UTF8);
        }
        catch (IOException) { }
    }
}