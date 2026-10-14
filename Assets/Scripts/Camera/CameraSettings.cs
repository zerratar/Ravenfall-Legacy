using UnityEngine;

/// <summary>
/// The player's camera settings, read by <see cref="FreeCamera"/>, <see cref="OrbitCamera"/> and
/// <see cref="IslandObserveCamera"/>. Set from PlayerSettings by <see cref="SettingsApplier"/>.
/// </summary>
public static class CameraSettings
{
    /// <summary>Free camera base speed; null keeps the camera's own (50).</summary>
    public static float? FreeCameraSpeed;

    /// <summary>Multiplier on mouse look in the free camera.</summary>
    public static float LookSensitivity = 1f;

    /// <summary>Multiplier on scroll wheel zoom in the player and island cameras.</summary>
    public static float ZoomSensitivity = 1f;

    public static bool InvertLook;
    public static bool InvertZoom;

    /// <summary>
    /// How far one frame's scrolling zooms. A wheel tick arrives in one frame, so this deliberately
    /// does not scale with frame time: the cameras used to multiply by it, which made each tick zoom
    /// less the higher the frame rate. A sixtieth keeps the feel the cameras had at 60 frames.
    /// </summary>
    public static float ZoomStep(float zoomChangeSpeed)
    {
        return Input.mouseScrollDelta.y * zoomChangeSpeed * (1f / 60f) * ZoomSensitivity * (InvertZoom ? -1f : 1f);
    }
}
