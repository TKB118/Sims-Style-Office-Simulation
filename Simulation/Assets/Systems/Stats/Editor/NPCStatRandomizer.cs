using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NPCStatRandomizer : EditorWindow
{
    [MenuItem("Tools/Randomize NPC Stats")]
    public static void RandomizeStats()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;

            CommonAIBase aiBase = prefab.GetComponent<CommonAIBase>();
            if (aiBase != null)
            {
                Undo.RecordObject(aiBase, "Randomize NPC Stats");
                bool modified = false;

                if (aiBase.PublicStats != null)
                {
                    foreach (var statConfig in aiBase.PublicStats)
                    {
                        if (statConfig.LinkedStat == null) continue;

                        statConfig.OverrideDefaults = true;

                        if (statConfig.LinkedStat.name.Contains("Work"))
                        {
                            statConfig.Override_InitialValue = 0f;
                            statConfig.Override_DecayRate = 0f;
                        }
                        else
                        {
                            statConfig.Override_InitialValue = Random.Range(0f, 1f);
                            statConfig.Override_DecayRate = Random.Range(0.0001f, 0.001f);
                        }
                        modified = true;
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(aiBase);
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Randomized stats for {count} NPC prefabs.");
    }
}
