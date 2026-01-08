using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableTarget : MonoBehaviour
{
    [SerializeField] bool LocalTargetManagerOnly = false;

    void Start()
    {
        if (!LocalTargetManagerOnly)
        {
            if (DetectableTargetManager.Instance != null)
            {
                DetectableTargetManager.Instance.Register(this);
            }
            else
            {
                //Debug.LogWarning($"[DetectableTarget] No DetectableTargetManager instance found when registering: {gameObject.name}");
            }
        }
    }

    void OnDestroy()
    {
        if (!LocalTargetManagerOnly && DetectableTargetManager.Instance != null)
        {
            DetectableTargetManager.Instance.Deregister(this);
        }
    }
}
