using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[ExecuteInEditMode]
public class SmartObjectAutoTagger : MonoBehaviour
{
    void Start()
    {
        ApplyTagBasedOnStat();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyTagBasedOnStat();
    }
#endif

    private void ApplyTagBasedOnStat()
    {
        var interaction = GetComponent<SimpleInteraction>();
        if (interaction == null || interaction.StatChanges == null) return;

        foreach (var statChange in interaction.StatChanges)
        {
            if (statChange?.LinkedStat == null) continue;

            string fullStatName = statChange.LinkedStat.name;

            string extracted = ExtractStatTag(fullStatName);
            if (string.IsNullOrEmpty(extracted)) continue;

#if UNITY_EDITOR
            if (IsTagDefined(extracted))
            {
                if (gameObject.tag != extracted)
                {
                    gameObject.tag = extracted;
                    Debug.Log($"[SmartObjectAutoTagger] Tag '{extracted}' set on '{gameObject.name}'");
                }
                return;
            }
            else
            {
                Debug.LogWarning($"[SmartObjectAutoTagger] Tag '{extracted}' is not defined in TagManager.");
            }
#endif
        }
    }

    private string ExtractStatTag(string statName)
    {
        if (statName.StartsWith("AIStat_") && statName.Length > 7)
        {
            return statName.Substring(7); // "AIStat_XXX" → "XXX"
        }
        return null;
    }

#if UNITY_EDITOR
    private bool IsTagDefined(string tag)
    {
        foreach (var definedTag in InternalEditorUtility.tags)
        {
            if (definedTag == tag)
                return true;
        }
        return false;
    }
#endif
}
