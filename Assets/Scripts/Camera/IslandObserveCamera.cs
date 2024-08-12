using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandObserveCamera : MonoBehaviour
{
    public static float RotationSpeed = 5f;

    public float zoomChangeSpeed = 20f;
    public float orbitChangeSpeed = -30f;
    public float rotateChangeSpeed = 100f;

    public float MaxZoom = 200f;
    public float MinZoom = 5f;
    public float MaxAngle = 90f;
    public float MinAngle = 5f;

    [Range(1f, 300f)]
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

    private IslandController island;

    public IslandController Island => island;

    private Dictionary<string, IslandSettings> settings = new Dictionary<string, IslandSettings>();
    private float saveTimeout;

    private void Update()
    {
        var scrollValue = Input.mouseScrollDelta.y * GameTime.deltaTime * zoomChangeSpeed;
        var oldDistance = Distance;
        var oldAngle = orbitAngles.x;
        Distance = Mathf.Clamp(Distance + scrollValue, MinZoom, MaxZoom);

        if (Input.GetKey(KeyCode.DownArrow))
        {
            var newAngle = Mathf.Clamp(orbitAngles.x + (GameTime.deltaTime * orbitChangeSpeed), MinAngle, MaxAngle);
            orbitAngles = new Vector2(newAngle, orbitAngles.y);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            var newAngle = Mathf.Clamp(orbitAngles.x + (GameTime.deltaTime * -orbitChangeSpeed), MinAngle, MaxAngle);
            orbitAngles = new Vector2(newAngle, orbitAngles.y);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            orbitAngles = new Vector2(orbitAngles.x, orbitAngles.y + (GameTime.deltaTime * -rotateChangeSpeed));
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            orbitAngles = new Vector2(orbitAngles.x, orbitAngles.y + (GameTime.deltaTime * rotateChangeSpeed));
        }

        if (oldDistance != Distance || oldAngle != orbitAngles.x)
        {
            UpdateIslandSettings();
        }

        if (saveTimeout > 0f)
        {
            saveTimeout -= GameTime.time;
            if (saveTimeout <= 0)
            {
                SaveSettings();
            }
        }
    }

    private void UpdateIslandSettings()
    {
        if (island != null)
        {
            if (settings.TryGetValue(island.Identifier, out var islandSettings))
            {
                islandSettings.Angle = orbitAngles.x;
                islandSettings.Distance = Distance;
                saveTimeout = 5f;
            }
        }
    }

    private void LateUpdate()
    {
        if (!targetTransform || targetTransform == null)
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
        Vector3 targetPoint = island?.CameraPanTarget != null ? island.CameraPanTarget.position : targetTransform.position;
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
    public void ObserveIsland(IslandController island)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log("Observe Island: " + island.name);
#endif
        targetTransform = island.transform;
        focusPoint = targetTransform.position;
        this.island = island;

        // check if there are settings for this island
        // use those values for angle/distance
        if (settings.TryGetValue(island.Identifier, out var islandSettings))
        {
            orbitAngles = new Vector2(islandSettings.Angle, 0f);
            Distance = islandSettings.Distance;
        }
        // otherwise save the distance /angle for this island
        else
        {
            islandSettings = new IslandSettings
            {
                Angle = orbitAngles.x,
                Distance = Distance
            };
            settings[island.Identifier] = islandSettings;
        }
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetString("IslandCameraSettings", Newtonsoft.Json.JsonConvert.SerializeObject(settings));
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("IslandCameraSettings"))
        {
            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, IslandSettings>>(PlayerPrefs.GetString("IslandCameraSettings"));
        }
    }

    internal class IslandSettings
    {
        public float Angle;
        public float Distance;
    }
}
