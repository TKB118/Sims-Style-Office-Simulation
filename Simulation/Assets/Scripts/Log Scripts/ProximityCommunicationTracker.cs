using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class ProximityCommunicationTracker : MonoBehaviour
{
    public static ProximityCommunicationTracker Instance { get; private set; }

    public static string StepPrefix = "";

    private class CommRecord
    {
        public string ObjectA, ObjectB;
        public string NpcA, NpcB;
        public string ObjectTag;
        public float Duration;
    }

    private List<CommRecord> records = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ReportCommunication(CommonAIBase npcA, CommonAIBase npcB, float duration)
    {
        GameObject objA = npcA.CurrentInteractionPublic?.gameObject;
        GameObject objB = npcB.CurrentInteractionPublic?.gameObject;

        if (objA == null || objB == null)
        {
            //Debug.LogWarning("[Communication] SmartObjectがnullのため記録しません");
            return;
        }

        if (objA.tag != objB.tag)
        {
            //Debug.Log($"[Communication] タグが異なるためスキップ: {objA.tag} vs {objB.tag}");
            return;
        }

        string npcNameA = npcA.name;
        string npcNameB = npcB.name;
        string sortedNpcA = string.Compare(npcNameA, npcNameB) < 0 ? npcNameA : npcNameB;
        string sortedNpcB = string.Compare(npcNameA, npcNameB) < 0 ? npcNameB : npcNameA;

        string objNameA = objA.name;
        string objNameB = objB.name;
        string sortedObjA = string.Compare(objNameA, objNameB) < 0 ? objNameA : objNameB;
        string sortedObjB = string.Compare(objNameA, objNameB) < 0 ? objNameB : objNameA;

        string tagName = objA.tag;

        records.Add(new CommRecord
        {
            ObjectA = sortedObjA,
            ObjectB = sortedObjB,
            NpcA = sortedNpcA,
            NpcB = sortedNpcB,
            ObjectTag = tagName,
            Duration = duration
        });

        //Debug.Log($"[Communication] 記録: {sortedNpcA} ⇄ {sortedNpcB}, Objects: {sortedObjA} ⇄ {sortedObjB}, Tag: {tagName}, Duration: {duration:F2}s");
    }

    public void FinalizeAndSave(int stepIndex)
    {
        if (string.IsNullOrEmpty(StepPrefix))
        {
            Debug.LogWarning("[ProximityCommunicationTracker] StepPrefix が未設定です");
            StepPrefix = Application.persistentDataPath;
        }

        string path = Path.Combine(StepPrefix, $"CommunicationDurations_Step{stepIndex}.csv");
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            writer.WriteLine("ObjectPair,NpcPair,ObjectTag,Duration");
            foreach (var r in records)
            {
                string objPair = $"{r.ObjectA} ⇄ {r.ObjectB}";
                string npcPair = $"{r.NpcA} ⇄ {r.NpcB}";
                writer.WriteLine($"{objPair},{npcPair},{r.ObjectTag},{r.Duration:F2}");
            }
        }

        // === スコア化: タグごとの重みを考慮した累積スコア ===
        Dictionary<string, float> tagWeights = new()
        {
            { "Rest", 1.5f },
            { "Hunger", 1.5f },
            { "Work", 0.5f },
            { "Default", 1.0f }
        };

        Dictionary<string, float> scores = new();

        foreach (var r in records)
        {
            string pairKey = $"{r.ObjectA} ⇄ {r.ObjectB}";
            float weight = tagWeights.ContainsKey(r.ObjectTag) ? tagWeights[r.ObjectTag] : tagWeights["Default"];
            float scoreValue = r.Duration * weight;

            if (!scores.ContainsKey(pairKey))
                scores[pairKey] = 0;
            scores[pairKey] += scoreValue;
        }

        string scorePath = Path.Combine(StepPrefix, $"CommunicationScores_Step{stepIndex}.csv");
        using (var writer = new StreamWriter(scorePath, false, Encoding.UTF8))
        {
            writer.WriteLine("ObjectPair,ObjectTag,TotalScore");
            foreach (var r in scores)
            {
                string tag = "";
                foreach (var rec in records)
                {
                    if ($"{rec.ObjectA} ⇄ {rec.ObjectB}" == r.Key)
                    {
                        tag = rec.ObjectTag;
                        break;
                    }
                }
                writer.WriteLine($"{r.Key},{tag},{r.Value:F2}");
            }
        }
        float layoutTotalScore = 0f;
        foreach (var s in scores.Values)
            layoutTotalScore += s;

        string layoutEvalPath = Path.Combine(StepPrefix, $"LayoutEvaluation_Step{stepIndex}.txt");
        File.WriteAllText(layoutEvalPath, $"LayoutCommunicationScore: {layoutTotalScore:F2}");

        Debug.Log($"[LayoutEvaluation] 総合スコア: {layoutTotalScore:F2}");

        Debug.Log($"[ProximityCommunicationTracker] CSV 書き出し完了: {path}, {scorePath}");
        records.Clear();
    }
}
