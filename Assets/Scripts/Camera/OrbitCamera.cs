using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    public static float RotationSpeed = 5f;

    [Range(1f, 30f)]
    public float Distance = 5f;

    [Min(0f)]
    public float FocusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    private float focusCentering = 0.5f;

    [SerializeField]
    private Transform targetTransform;

    [SerializeField]
    private Vector2 orbitAxis = Vector2.up;

    [SerializeField]
    private Vector2 orbitAngles = new Vector2(45f, 0f);

    private Vector3 focusPoint;

    public Transform TargetTransform
    {
        get => targetTransform;
        set
        {
            targetTransform = value;
            if (value)
            {
                focusPoint = targetTransform.position;
            }
        }
    }

    private void LateUpdate()
    {
        if (!TargetTransform || TargetTransform == null)
        {
            return;
        }

        UpdateFocusPoint();
        UpdateRotation();
        Quaternion lookRotation = Quaternion.Euler(orbitAngles);
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * Distance;
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private void UpdateRotation()
    {
        orbitAngles += RotationSpeed * Time.unscaledDeltaTime * orbitAxis;
    }

    private void UpdateFocusPoint()
    {
        Vector3 targetPoint = TargetTransform.position;
        if (FocusRadius > 0f)
        {

            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }

            if (distance > FocusRadius)
            {
                t = Mathf.Min(t, FocusRadius / distance);
            }

            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }
}
