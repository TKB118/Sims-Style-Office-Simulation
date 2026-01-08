using UnityEngine;
using UnityEngine.AI;

public class NavMeshAreaScanner : MonoBehaviour
{
    public Transform floorRoot;
    public float scanResolution = 0.5f;

    void Start()
    {
        if (floorRoot == null)
        {
            Debug.LogError("Floor Root not assigned.");
            return;
        }

        Bounds floorBounds = GetCombinedBounds(floorRoot);

        float minX = floorBounds.min.x;
        float maxX = floorBounds.max.x;
        float minZ = floorBounds.min.z;
        float maxZ = floorBounds.max.z;

        float navMinX = float.MaxValue;
        float navMaxX = float.MinValue;
        float navMinZ = float.MaxValue;
        float navMaxZ = float.MinValue;

        for (float x = minX; x <= maxX; x += scanResolution)
        {
            for (float z = minZ; z <= maxZ; z += scanResolution)
            {
                Vector3 testPoint = new Vector3(x, floorBounds.center.y, z);

                if (NavMesh.SamplePosition(testPoint, out NavMeshHit hit, 0.2f, NavMesh.AllAreas))
                {
                    navMinX = Mathf.Min(navMinX, hit.position.x);
                    navMaxX = Mathf.Max(navMaxX, hit.position.x);
                    navMinZ = Mathf.Min(navMinZ, hit.position.z);
                    navMaxZ = Mathf.Max(navMaxZ, hit.position.z);
                }
            }
        }

        Debug.Log($"[NavMesh Area] X: {navMinX} to {navMaxX}, Z: {navMinZ} to {navMaxZ}");
    }

    private Bounds GetCombinedBounds(Transform parent)
    {
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(parent.position, Vector3.zero);

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        return combined;
    }
}
