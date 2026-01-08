using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class generate_navmesh : MonoBehaviour
{
    public string targetObjectName = "Floors";
    public int agentTypeID = 0; // Example agent type ID
    public float agentRadius = 0.5f;
    public float agentHeight = 2.0f;
    public float maxSlope = 57.0f;
    public float stepHeight = 0.23f;
    public int tileSize = 32;
    public float voxelSize = 0.01f;
    public float minRegionArea = 2.0f;
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize = new Vector3(50, 10, 50);

    void Start()
    {
        // Start the recursive search from the parent object this script is attached to
        SearchAndAttachNavMeshSurface(transform);
    }

    void SearchAndAttachNavMeshSurface(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetObjectName)
            {
                // Attach NavMeshSurface to all children inside the target object
                AttachNavMeshSurfaceToChildren(child);
                return; // Break the recursion when the target object is found
            }
            else
            {
                // Continue searching recursively
                SearchAndAttachNavMeshSurface(child);
            }
        }
    }

    void AttachNavMeshSurfaceToChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Check if the NavMeshSurface component is already attached
            if (child.gameObject.GetComponent<NavMeshSurface>() == null)
            {
                // Attach the NavMeshSurface component
                NavMeshSurface navMeshSurface = child.gameObject.AddComponent<NavMeshSurface>();
                CustomizeNavMeshSurface(navMeshSurface);

                // Optionally, bake the NavMesh for each child
                navMeshSurface.BuildNavMesh();
            }
            else
            {
                Debug.LogWarning("NavMeshSurface component is already attached to the child object: " + child.gameObject.name);
            }
        }
    }

    void CustomizeNavMeshSurface(NavMeshSurface navMeshSurface)
    {
        navMeshSurface.agentTypeID = agentTypeID;
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.layerMask = LayerMask.GetMask("Default");

        // Customize NavMeshBuildSettings
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeID);
        settings.agentRadius = agentRadius;
        settings.agentHeight = agentHeight;
        settings.agentSlope = maxSlope;
        settings.agentClimb = stepHeight;
        settings.overrideTileSize = true;
        settings.tileSize = tileSize;
        settings.overrideVoxelSize = true;
        settings.voxelSize = voxelSize;
        settings.minRegionArea = minRegionArea;

        // Apply the settings to the NavMeshSurface
        navMeshSurface.overrideTileSize = settings.overrideTileSize;
        navMeshSurface.tileSize = settings.tileSize;
        navMeshSurface.overrideVoxelSize = settings.overrideVoxelSize;
        navMeshSurface.voxelSize = settings.voxelSize;

        navMeshSurface.center = boundsCenter;
        navMeshSurface.size = boundsSize;
    }
}
