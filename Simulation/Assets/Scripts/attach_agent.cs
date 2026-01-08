using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class attach_agent : MonoBehaviour
{
    public string targetObjectName = "Agents";
    public float agentRadius = 0.3f;
    public float agentHeight = 1.8f;
    

    void Start()
    {
        // Start the recursive search from the parent object this script is attached to
        SearchAndAttachNavMeshAgent(transform);
    }

    void SearchAndAttachNavMeshAgent(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetObjectName)
            {
                // Attach NavMeshAgent to all children inside the target object
                AttachNavMeshAgentToChildren(child);
                return; // Break the recursion when the target object is found
            }
            else
            {
                // Continue searching recursively
                SearchAndAttachNavMeshAgent(child);
            }
        }
    }

    void AttachNavMeshAgentToChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Check if the NavMeshAgent component is already attached
            if (child.gameObject.GetComponent<NavMeshAgent>() == null)
            {
                // Attach the NavMeshAgent component
                NavMeshAgent agent = child.gameObject.AddComponent<NavMeshAgent>();

                // Set the specific settings for the NavMeshAgent
                agent.radius = agentRadius;
                agent.height = agentHeight;
                
            }
            else
            {
                Debug.LogWarning("NavMeshAgent component is already attached to the child object: " + child.gameObject.name);
            }
        }
    }
}
