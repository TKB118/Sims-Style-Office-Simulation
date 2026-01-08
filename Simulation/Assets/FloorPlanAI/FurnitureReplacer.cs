using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FurnitureReplacer : MonoBehaviour
{
    public GameObject layoutRoot;
    public float duplicateInterval = 10.0f;
    public Vector3 duplicateOffset = new Vector3(10f, 0, 0);
    public string furnitureParentName = "Furniture";
    public string floorParentName = "Floors";
    public string wallsParentName = "Walls";

    private int duplicateCount = 0;

    void Start()
    {
        StartCoroutine(DuplicateAndRandomizeLoop());
    }

    private IEnumerator DuplicateAndRandomizeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(duplicateInterval);
            DuplicateLayoutWithCollisionAvoidance();
        }
    }

    public GameObject DuplicateLayoutAndReturnRoot()
    {
        if (layoutRoot == null)
        {
            Debug.LogError("Layout root is not assigned.");
            return null;
        }

        Vector3 newPosition = layoutRoot.transform.position + duplicateOffset * (duplicateCount + 1);

        GameObject duplicatedLayout = Instantiate(layoutRoot, newPosition, layoutRoot.transform.rotation);
        duplicatedLayout.name = $"{layoutRoot.name}_Clone_{duplicateCount}";

        duplicateCount++;

        Debug.Log($"Duplicated layout #{duplicateCount}: {duplicatedLayout.name}");

        return duplicatedLayout;
    }

    public void DeregisterOldSmartObjects(SmartObjectManager manager, GameObject currentLayoutRoot)
    {
        var allSmartObjects = FindObjectsOfType<SmartObject>();

        foreach (var so in allSmartObjects)
        {
            if (!so.transform.IsChildOf(currentLayoutRoot.transform))
            {
                manager.DeregisterSmartObject(so);
            }
        }

        Debug.Log("Old SmartObjects deregistered.");
    }



    private void DuplicateLayoutWithCollisionAvoidance()
    {
        if (layoutRoot == null)
        {
            Debug.LogError("Layout root is not assigned.");
            return;
        }

        Vector3 newPosition = layoutRoot.transform.position + duplicateOffset * (duplicateCount + 1);

        GameObject duplicatedLayout = Instantiate(layoutRoot, newPosition, layoutRoot.transform.rotation);
        duplicatedLayout.name = $"{layoutRoot.name}_Clone_{duplicateCount}";

        Transform furnitureParent = duplicatedLayout.transform.Find(furnitureParentName);
        Transform floorParent = duplicatedLayout.transform.Find(floorParentName);
        Transform wallsParent = duplicatedLayout.transform.Find(wallsParentName);

        if (furnitureParent == null || floorParent == null || wallsParent == null)
        {
            Debug.LogError($"Furniture, Floors or Walls not found in duplicated layout.");
            return;
        }

        Bounds floorBounds = GetCombinedWorldBounds(floorParent);
        List<Bounds> wallBoundsList = GetAllWorldBounds(wallsParent);

        List<Bounds> placedFurnitureBounds = new List<Bounds>();

        foreach (Transform furniture in furnitureParent)
        {
            Bounds furnitureBounds = GetCombinedWorldBounds(furniture);
            Vector3 furnitureSizeWithMargin = furnitureBounds.size + new Vector3(0.6f, 0f, 0.6f); // 0.3m余白

            float halfWidth = furnitureSizeWithMargin.x / 2f;
            float halfDepth = furnitureSizeWithMargin.z / 2f;

            float minX = floorBounds.min.x + halfWidth;
            float maxX = floorBounds.max.x - halfWidth;
            float minZ = floorBounds.min.z + halfDepth;
            float maxZ = floorBounds.max.z - halfDepth;

            if (minX > maxX || minZ > maxZ)
            {
                Debug.LogWarning($"Furniture {furniture.name} is too big for the floor bounds. Skipping placement.");
                continue;
            }

            const int maxAttempts = 100;
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 randomWorldPos = new Vector3(
                    Random.Range(minX, maxX),
                    furniture.position.y,
                    Random.Range(minZ, maxZ)
                );

                // NavMesh有効範囲か確認
                if (!NavMesh.SamplePosition(randomWorldPos, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
                    continue;

                Bounds simulatedBounds = new Bounds(randomWorldPos, furnitureSizeWithMargin);

                // 壁と干渉チェック
                bool intersectsWall = false;
                foreach (Bounds wallBounds in wallBoundsList)
                {
                    if (simulatedBounds.Intersects(wallBounds))
                    {
                        intersectsWall = true;
                        break;
                    }
                }
                if (intersectsWall) continue;

                // 家具同士干渉チェック
                bool intersectsFurniture = false;
                foreach (Bounds existingFurniture in placedFurnitureBounds)
                {
                    if (simulatedBounds.Intersects(existingFurniture))
                    {
                        intersectsFurniture = true;
                        break;
                    }
                }
                if (intersectsFurniture) continue;

                // OK → 設置
                Vector3 localPos = furnitureParent.InverseTransformPoint(randomWorldPos);
                furniture.localPosition = new Vector3(localPos.x, furniture.localPosition.y, localPos.z);

                placedFurnitureBounds.Add(simulatedBounds);

                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"Failed to place furniture {furniture.name} after {maxAttempts} attempts.");
            }
        }

        Debug.Log($"Duplicated layout #{duplicateCount} with NavMesh-valid, collision-free furniture placement.");
        duplicateCount++;
    }

    private Bounds GetCombinedWorldBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(target.position, Vector3.zero);

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds;
    }

    private List<Bounds> GetAllWorldBounds(Transform parent)
    {
        List<Bounds> boundsList = new List<Bounds>();
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            boundsList.Add(r.bounds);
        }
        return boundsList;
    }

    public void ApplyFurnitureLayout(Vector3[] positions)
    {
        Transform furnitureParent = layoutRoot.transform.Find(furnitureParentName);
        Transform floorParent = layoutRoot.transform.Find(floorParentName);
        Transform wallsParent = layoutRoot.transform.Find(wallsParentName);

        if (furnitureParent == null || floorParent == null || wallsParent == null)
        {
            Debug.LogError($"Furniture, Floors or Walls not found in layout.");
            return;
        }

        Bounds floorBounds = GetCombinedWorldBounds(floorParent);
        List<Bounds> wallBoundsList = GetAllWorldBounds(wallsParent);

        List<Bounds> placedFurnitureBounds = new List<Bounds>();
        int index = 0;

        foreach (Transform furniture in furnitureParent)
        {
            if (index >= positions.Length)
            {
                Debug.LogWarning("Not enough positions provided for all furniture.");
                break;
            }

            Vector3 targetPos = positions[index++];
            Bounds furnitureBounds = GetCombinedWorldBounds(furniture);

            // 衝突チェック
            Bounds simulatedBounds = new Bounds(targetPos, furnitureBounds.size);

            bool intersectsWall = wallBoundsList.Exists(wallBounds => simulatedBounds.Intersects(wallBounds));
            bool intersectsFurniture = placedFurnitureBounds.Exists(existingBounds => simulatedBounds.Intersects(existingBounds));

            if (intersectsWall || intersectsFurniture)
            {
                Debug.LogWarning($"Position {targetPos} is invalid for {furniture.name}. Skipping placement.");
                continue;
            }

            Vector3 localPos = furnitureParent.InverseTransformPoint(targetPos);
            furniture.localPosition = new Vector3(localPos.x, furniture.localPosition.y, localPos.z);
            placedFurnitureBounds.Add(simulatedBounds);
        }

        Debug.Log("Furniture layout applied from ML-Agents action.");
    }

}
