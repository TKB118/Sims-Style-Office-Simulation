using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;

public static class LayoutEvaluationManager
{
    private static List<Dictionary<string, (float ratio, string evaluation)>> allNPCStats = new();
    private static int expectedNPCCount = -1;
    private static int receivedCount = 0;
    public static string StepPrefix = "";

    public class EvaluationSummary
    {
        public int GoodCount;
        public int AcceptableCount;
        public int BadCount;
        public float TotalAverageScore;
    }

    private static EvaluationSummary latestSummary = new EvaluationSummary();

    public static void SetExpectedNPCCount(int count)
    {
        expectedNPCCount = count;
        receivedCount = 0;
        allNPCStats.Clear();
    }

    public static void SubmitRatioStats(Dictionary<TravelDistanceLogger.SmartObjectPair, (float, float, float, string)> ratioStats)
    {
        var formatted = new Dictionary<string, (float, string)>();

        foreach (var kvp in ratioStats)
        {
            GameObject objA = kvp.Key.ObjectA.gameObject;
            GameObject objB = kvp.Key.ObjectB.gameObject;

            if (!SmartObjectTagger.IsImportantTagPair(objA, objB))
            {
                Debug.Log($"[Ignored Pair] {kvp.Key.ObjectA.name} - {kvp.Key.ObjectB.name} (unimportant tag pair)");
                continue;
            }

            string key = $"{kvp.Key.ObjectA.name}-{kvp.Key.ObjectB.name}";
            formatted[key] = (kvp.Value.Item3, kvp.Value.Item4); // ratio, evaluation
        }

        allNPCStats.Add(formatted);
        receivedCount++;

        if (expectedNPCCount > 0 && receivedCount >= expectedNPCCount)
        {
            Debug.Log("All NPCs submitted their stats. Generating summary...");
            SummarizeAndSaveEvaluation();
        }
    }

    private static void SummarizeAndSaveEvaluation()
    {
        var aggregated = new Dictionary<string, List<float>>();
        int goodCount = 0, acceptableCount = 0, badCount = 0;

        foreach (var npcStats in allNPCStats)
        {
            foreach (var kvp in npcStats)
            {
                if (!aggregated.ContainsKey(kvp.Key))
                    aggregated[kvp.Key] = new List<float>();

                aggregated[kvp.Key].Add(ConvertEvaluationToScore(kvp.Value.evaluation));
            }
        }

        string filePath = Path.Combine(StepPrefix, "LayoutEvaluation.csv");  // ← 修正ポイント
        using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))

        {
            writer.WriteLine("ObjectPair,AverageScore,Evaluation");

            float totalSum = 0f;
            int totalCount = 0;

            foreach (var kvp in aggregated)
            {
                float avg = kvp.Value.Average();
                string eval = ConvertScoreToEvaluation(avg);

                switch (eval)
                {
                    case "Good": goodCount++; break;
                    case "Acceptable": acceptableCount++; break;
                    case "Bad": badCount++; break;
                }

                writer.WriteLine($"{kvp.Key},{avg:F2},{eval}");
                totalSum += avg;
                totalCount++;
            }

            if (totalCount > 0)
            {
                float overallAvg = totalSum / totalCount;
                writer.WriteLine();
                writer.WriteLine($"TotalAverageScore,{overallAvg:F2}");

                // 最新評価を更新
                latestSummary.GoodCount = goodCount;
                latestSummary.AcceptableCount = acceptableCount;
                latestSummary.BadCount = badCount;
                latestSummary.TotalAverageScore = overallAvg;
            }
        }
        Debug.Log($"Layout evaluation saved to: {filePath}");
    }

    public static void ForceSaveIfNeeded()
    {
        if (receivedCount > 0) // 最低1体から受信していれば出力する
        {
            Debug.Log("[LayoutEvaluationManager] Force-saving LayoutEvaluation.");
            SummarizeAndSaveEvaluation();
            expectedNPCCount = -1; // 二重実行防止
        }
    }


    public static int GetBadRatioCount()
    {
        int badCount = 0;

        foreach (var npcStats in allNPCStats)
        {
            foreach (var kvp in npcStats)
            {
                if (kvp.Value.evaluation == "Bad")
                {
                    badCount++;
                }
            }
        }

        return badCount;
    }


    private static float ConvertEvaluationToScore(string evaluation)
    {
        return evaluation switch
        {
            "Good" => 1.0f,
            "Acceptable" => 0.5f,
            "Bad" => 0.0f,
            _ => 0.0f
        };
    }

    private static string ConvertScoreToEvaluation(float avgScore)
    {
        if (avgScore >= 0.85f) return "Good";
        if (avgScore >= 0.4f) return "Acceptable";
        return "Bad";
    }

    public static EvaluationSummary GetLatestEvaluationResults()
    {
        return latestSummary;
    }
}
