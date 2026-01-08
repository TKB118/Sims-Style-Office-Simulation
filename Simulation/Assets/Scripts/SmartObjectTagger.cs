using System.Collections.Generic;
using UnityEngine;

public enum ObjectFunctionTag
{
    Unknown,
    WorkObject,
    LeisureObject,
    EnergyObject,
    HygieneObject,
    TechObject,
    HungerObject
}

public static class SmartObjectTagger
{
    public static ObjectFunctionTag GetFunctionalTag(GameObject obj)
    {
        var interaction = obj.GetComponent<SimpleInteraction>();
        if (interaction == null || interaction.StatChanges == null || interaction.StatChanges.Length == 0)
            return ObjectFunctionTag.Unknown;

        foreach (var statChange in interaction.StatChanges)
        {
            if (statChange == null || statChange.LinkedStat == null || string.IsNullOrEmpty(statChange.LinkedStat.name))
                continue;

            string statName = statChange.LinkedStat.name.ToLower();


            if (statName.Contains("work")) return ObjectFunctionTag.WorkObject;
            if (statName.Contains("fun")) return ObjectFunctionTag.LeisureObject;
            if (statName.Contains("energy")) return ObjectFunctionTag.EnergyObject;
            if (statName.Contains("hygiene")) return ObjectFunctionTag.HygieneObject;
            if (statName.Contains("tech")) return ObjectFunctionTag.TechObject;
            if (statName.Contains("hunger")) return ObjectFunctionTag.HungerObject;
        }

        return ObjectFunctionTag.Unknown;
    }

    public static bool IsImportantTagPair(GameObject objA, GameObject objB)
    {
        var tagA = GetFunctionalTag(objA);
        var tagB = GetFunctionalTag(objB);

        var importantPairs = new HashSet<(ObjectFunctionTag, ObjectFunctionTag)>
        {
            (ObjectFunctionTag.WorkObject, ObjectFunctionTag.TechObject),
            (ObjectFunctionTag.WorkObject, ObjectFunctionTag.EnergyObject),
            (ObjectFunctionTag.WorkObject, ObjectFunctionTag.LeisureObject),
            (ObjectFunctionTag.LeisureObject, ObjectFunctionTag.HungerObject),
            (ObjectFunctionTag.HygieneObject, ObjectFunctionTag.EnergyObject)
        };

        return importantPairs.Contains((tagA, tagB)) || importantPairs.Contains((tagB, tagA));
    }

}
