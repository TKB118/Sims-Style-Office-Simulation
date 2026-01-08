using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FurnitureRandomizer : MonoBehaviour
{
    public Transform furnitureParent;
    public Transform floorParent;
    public LayerMask collisionMask = 0; // Default: すべてのレイヤーを対象

    public IEnumerator RandomizeFurniturePositions()
    {
        if (furnitureParent == null || floorParent == null)
        {
            Debug.LogError("Missing references for FurnitureRandomizer.");
            yield break;
        }

        Bounds floorBounds = GetCombinedBounds(floorParent);

        foreach (Transform furniture in furnitureParent)
        {
            var reporter = furniture.GetComponent<FurnitureCollisionReporter>();
            if (reporter == null)
            {
                Debug.LogWarning($"{furniture.name} is missing FurnitureCollisionReporter.");
                continue;
            }

            BoxCollider col = furniture.GetComponentInChildren<BoxCollider>();
            if (col == null)
            {
                Debug.LogWarning($"{furniture.name} has no BoxCollider.");
                continue;
            }

            Vector3 size = Vector3.Scale(col.size, col.transform.lossyScale);
            Vector3 halfSize = size / 2f;

            float minX = floorBounds.min.x + halfSize.x;
            float maxX = floorBounds.max.x - halfSize.x;
            float minZ = floorBounds.min.z + halfSize.z;
            float maxZ = floorBounds.max.z - halfSize.z;

            bool placed = false;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                Vector3 worldPos = new Vector3(
                    Random.Range(minX, maxX),
                    furniture.position.y,
                    Random.Range(minZ, maxZ)
                );

                // オーバーラップチェック（他家具・壁などと干渉するか？）
                bool hasCollision = reporter.IsCollidingAtPosition(worldPos, Quaternion.identity, collisionMask, furniture.gameObject);
                if (hasCollision)
                    continue;

                // 干渉なし ➝ 確定配置
                Vector3 localPos = furnitureParent.InverseTransformPoint(worldPos);
                furniture.localPosition = new Vector3(localPos.x, furniture.localPosition.y, localPos.z);

                // 配置したことを物理エンジンに即時通知（次家具の判定で考慮させる）
                Physics.SyncTransforms();
                yield return null;

                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"[Skipped] {furniture.name} could not be placed after 100 attempts.");
            }
        }

        Debug.Log("Furniture positions randomized with collision avoidance.");
    }

    private Bounds GetCombinedBounds(Transform target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(target.position, Vector3.zero);

        Bounds combined = renderers[0].bounds;
        foreach (var r in renderers) combined.Encapsulate(r.bounds);
        return combined;
    }
}
