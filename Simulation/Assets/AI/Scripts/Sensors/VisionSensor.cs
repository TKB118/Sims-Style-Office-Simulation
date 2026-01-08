using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseAI))]
public class VisionSensor : MonoBehaviour
{
    [SerializeField] LayerMask DetectionMask = ~0;

    BaseAI LinkedAI;
    LocalDetectableTargetManager TargetManager;
    LookAtPOI lookComponent;
    VisionSensorLogger logger;

    // Start is called before the first frame update
    void Start()
    {
        LinkedAI = GetComponent<BaseAI>();
        TargetManager = GetComponent<LocalDetectableTargetManager>();
        lookComponent = GetComponent<LookAtPOI>();
        logger = GetComponent<VisionSensorLogger>();
    }

    // Update is called once per frame
    void Update()
    {
        if (lookComponent.PerformLookRaycast(out RaycastHit hit))
        {
            var target = hit.collider.GetComponentInParent<DetectableTarget>();
            if (target != null)
            {
                logger.UpdateVision(target.gameObject, hit.distance);
            }
        }

    }
}
