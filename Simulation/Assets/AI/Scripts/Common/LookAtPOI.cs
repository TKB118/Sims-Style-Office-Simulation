using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPOI : MonoBehaviour
{
    [Header("Head Configuration")]
    [SerializeField] Transform HeadBoneTransform;
    [SerializeField] Vector3 HeadBoneToEyeOffset;

    [Header("Look At Configuration")]
    [SerializeField] float LookAtSpeed = 90.0f;
    [SerializeField] float MaxYawAngle = 75.0f;
    [SerializeField] float MinPitchAngle = -30.0f;
    [SerializeField] float MaxPitchAngle = 45.0f;

    [Header("DEBUG OPTIONS")]
    [SerializeField] bool DEBUG_DrawLookDirection = true;
    [SerializeField] Transform DEBUG_LookTargetToSet;
    [SerializeField] bool DEBUG_UpdateLookTarget = false;

    public Transform CurrentLookTarget;
    Transform PreviousLookTarget;

    float DesiredYawDelta = 0f;
    float DesiredPitchDelta = 0f;
    float CurrentYawDelta = 0f;
    float CurrentPitchDelta = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (DEBUG_UpdateLookTarget)
        {
            DEBUG_UpdateLookTarget = false;
            SetLookTarget(DEBUG_LookTargetToSet);
        }
    }

    private void LateUpdate()
    {
        if (CurrentLookTarget != null)
        {
            Vector3 localSpaceLookDirection = HeadBoneTransform.InverseTransformPoint(CurrentLookTarget.position) - HeadBoneToEyeOffset;

            float yawDelta = Mathf.Atan2(localSpaceLookDirection.x, localSpaceLookDirection.z) * Mathf.Rad2Deg;
            float pitchDelta = Mathf.Atan(localSpaceLookDirection.y / localSpaceLookDirection.z) * Mathf.Rad2Deg;

            // if the target is within look limits then set it as the desired pitch and yaw
            if (Mathf.Abs(yawDelta) <= MaxYawAngle && pitchDelta >= MinPitchAngle && pitchDelta <= MaxPitchAngle) 
            {
                DesiredYawDelta = yawDelta;
                DesiredPitchDelta = pitchDelta;
            }
            else
            {
                // return to looking forwards
                DesiredPitchDelta = DesiredYawDelta = 0f;
            }
        }
        else if (PreviousLookTarget != null)
        {
            // return to looking forwards
            DesiredPitchDelta = DesiredYawDelta = 0f;
        }

        if (!Mathf.Approximately(CurrentYawDelta, DesiredYawDelta) || 
            !Mathf.Approximately(CurrentPitchDelta, DesiredPitchDelta))
        {
            CurrentPitchDelta = Mathf.MoveTowardsAngle(CurrentPitchDelta, DesiredPitchDelta, LookAtSpeed * Time.deltaTime);
            CurrentYawDelta = Mathf.MoveTowardsAngle(CurrentYawDelta, DesiredYawDelta, LookAtSpeed * Time.deltaTime);
        }

        HeadBoneTransform.localRotation *= Quaternion.Euler(-CurrentPitchDelta, CurrentYawDelta, 0f);

        PreviousLookTarget = CurrentLookTarget;

        if (DEBUG_DrawLookDirection)
        {
            Vector3 worldSpaceEyePosition = HeadBoneTransform.TransformPoint(HeadBoneToEyeOffset);
            Debug.DrawLine(worldSpaceEyePosition, worldSpaceEyePosition + HeadBoneTransform.forward * 10f, Color.red);
            // Raycast 視線チェック
            Vector3 eyePosition = HeadBoneTransform.TransformPoint(HeadBoneToEyeOffset);
            Vector3 direction = HeadBoneTransform.forward;

            RaycastHit hit;
            if (Physics.Raycast(eyePosition, direction, out hit, 10f))
            {
                var target = hit.collider.GetComponentInParent<DetectableTarget>();
                if (target != null)
                {
                    var logger = GetComponent<VisionSensorLogger>();
                    if (logger != null)
                    {
                        logger.UpdateVision(target.gameObject, hit.distance);
                    }
                }
            }
        }
    } 

    public void SetLookTarget(Transform newLookTarget)
    {
        CurrentLookTarget = newLookTarget;
    }
    public bool PerformLookRaycast(out RaycastHit hitInfo)
    {
        Vector3 eyePos = HeadBoneTransform.TransformPoint(HeadBoneToEyeOffset);
        Vector3 dir = HeadBoneTransform.forward;

        return Physics.Raycast(eyePos, dir, out hitInfo, 10f);
    }

    public Vector3 GetEyePosition()
    {
        return HeadBoneTransform.TransformPoint(HeadBoneToEyeOffset);
    }

    public Vector3 GetLookDirection()
    {
        return HeadBoneTransform.forward;
    }
}
