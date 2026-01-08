using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class ObstacleAdderRecursive : MonoBehaviour
{
    public string targetObjectName = "Walls";
    
     
    
    void Start()
    {
        // Start the recursive search from the parent object this script is attached to
        SearchAndAttachNavMeshObstacle(transform);
    }

    void SearchAndAttachNavMeshObstacle(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetObjectName)
            {
                // Attach NavMeshObstacle to all children inside the target object
                AttachNavMeshObstacleToChildren(child);
                return; // Break the recursion when the target object is found
            }
            else
            {
                // Continue searching recursively
                SearchAndAttachNavMeshObstacle(child);
            }
        }
    }

    void AttachNavMeshObstacleToChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Check if the NavMeshObstacle component is already attached
            if (child.gameObject.GetComponent<NavMeshObstacle>() == null)
            {
                // Attach the NavMeshObstacle component

                NavMeshObstacle navMeshObstacle = child.gameObject.AddComponent<NavMeshObstacle>();
                navMeshObstacle.carving = true;
            }
            else
            {
                Debug.LogWarning("NavMeshObstacle component is already attached to the child object: " + child.gameObject.name);
            }
        }
    }   
}
