using System.Collections;
using UnityEngine;
using System.IO;

public class FurnitureReplacerTest : MonoBehaviour
{
    public GameObject layoutRoot;
    public float loopInterval = 15.0f;
    public Vector3 duplicateOffset = new Vector3(12f, 0, 0);
    public int maxSteps = 5;
    


    private int currentStep = 0;

    private void Start()
    {
        StartCoroutine(RunEvaluationLoop());
    }

    private IEnumerator RunEvaluationLoop()
    {
        while (currentStep < maxSteps)
        {
            GameObject newLayout = DuplicateAndRandomize();
            if (newLayout != null)
            {
                SetupEvaluationForStep(newLayout, currentStep);
            }

            currentStep++;
            yield return new WaitForSeconds(loopInterval);
        }
    }

    private GameObject DuplicateAndRandomize()
    {
        if (layoutRoot == null)
        {
            Debug.LogError("Layout root not assigned.");
            return null;
        }

        Vector3 newPosition = layoutRoot.transform.position + duplicateOffset * (currentStep + 1);
        GameObject duplicatedLayout = Instantiate(layoutRoot, newPosition, layoutRoot.transform.rotation);
        duplicatedLayout.name = $"{layoutRoot.name}_Clone_{currentStep}";

        FurnitureReplacer replacer = duplicatedLayout.AddComponent<FurnitureReplacer>();
        replacer.layoutRoot = duplicatedLayout;
        replacer.duplicateInterval = 0; // Disable internal loop
        replacer.enabled = false; // We call replacement manually

        replacer.Invoke("DuplicateLayoutWithFullCollisionAvoidance", 0);

        return duplicatedLayout;
    }

    private void SetupEvaluationForStep(GameObject layout, int step)
    {
        // Remove existing NPCs
        foreach (var npc in GameObject.FindGameObjectsWithTag("NPC"))
        {
            Destroy(npc);
        }

        // Reset file prefix for logs
        string prefix = $"Step_{step}_";
        TravelDistanceLogger.StepPrefix = prefix;
        InteractionLogger.StepPrefix = prefix;
        LayoutEvaluationManager.StepPrefix = prefix;

        // Spawn new NPCs for the new layout (assumes a SpawnNPC manager exists)
        var spawner = FindObjectOfType<SpawnNPC>();
        if (spawner != null)
        {
            spawner.SpawnAt(layout.transform);
        }
        else
        {
            Debug.LogWarning("SpawnNPC manager not found.");
        }
    }
}