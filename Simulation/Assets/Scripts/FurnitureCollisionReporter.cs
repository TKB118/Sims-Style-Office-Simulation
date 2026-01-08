using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FurnitureCollisionReporter : MonoBehaviour
{
    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    /// <summary>
    /// 指定したワールド位置に家具を仮想的に置いたとき、何かと干渉するかを返す
    /// </summary>
    public bool IsCollidingAtPosition(Vector3 worldPosition, Quaternion rotation, LayerMask mask, GameObject self)
    {
        // 実際のBoxColliderのサイズをワールドスケールで換算
        Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
        Vector3 halfSize = worldSize / 2f;

        // その位置に置いたと仮定してOverlapBoxを使う
        Collider[] hits = Physics.OverlapBox(
            worldPosition + boxCollider.center, // centerに注意
            halfSize,
            rotation,
            mask
        );

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.isTrigger) continue;
            if (hit.gameObject == self) continue;

            Debug.Log($"[Overlap Detected] {self.name} would hit {hit.name}");
            return true;
        }

        return false;
    }
}
