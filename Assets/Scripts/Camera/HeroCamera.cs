using UnityEngine;

/// <summary>
/// Hero mode's camera (docs/hero-mode-design.md, 7a): top-down like an action RPG (a fixed high angle following the
/// hero, scroll to zoom), or over the shoulder; V switches. The right mouse turns either; it never turns by itself.
/// The cursor stays free in both, as a streamer also reads chat and clicks the HUD. GameCamera.FollowHero turns it on.
/// </summary>
[RequireComponent(typeof(Camera))]
public class HeroCamera : MonoBehaviour
{
    public enum View { TopDown, Shoulder }

    private const float TopDownPitch = 55f;
    private const float ShoulderPitch = 12f;
    private const float MinZoom = 8f, MaxZoom = 28f;

    public View Mode = View.TopDown;

    private Transform target;
    private Camera cam;
    private float yaw;
    private float zoom = 16f;
    private Vector3 focus;
    private float blend = 1f; // 0 shoulder, 1 top-down: switching glides

    /// <summary>Where the camera looks along the ground: WASD walks relative to it.</summary>
    public Vector3 Forward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
    public Vector3 Right => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

    public void Follow(PlayerController player)
    {
        if (!cam) cam = GetComponent<Camera>();
        target = player ? player.transform : null;
        if (!target) return;
        focus = target.position;
        yaw = target.eulerAngles.y;
        blend = Mode == View.TopDown ? 1f : 0f;
    }

    public void Toggle() => Mode = Mode == View.TopDown ? View.Shoulder : View.TopDown;

    private void LateUpdate()
    {
        if (!target) return;
        var typing = Keybinds.IsTyping;
        if (!typing && Input.GetMouseButton(1)) yaw += Input.GetAxis("Mouse X") * 4f;
        if (!Shinobytes.UI.DraggableWindow.IsPointerOverWindow) zoom = Mathf.Clamp(zoom + CameraSettings.ZoomStep(20f), MinZoom, MaxZoom);

        var dt = Time.unscaledDeltaTime;
        blend = Mathf.MoveTowards(blend, Mode == View.TopDown ? 1f : 0f, dt * 3f);
        focus = Vector3.Lerp(focus, target.position, 1f - Mathf.Exp(-10f * dt));

        var topRotation = Quaternion.Euler(TopDownPitch, yaw, 0f);
        var topPosition = focus + Vector3.up - topRotation * Vector3.forward * zoom;
        var shoulderRotation = Quaternion.Euler(ShoulderPitch, yaw, 0f);
        var shoulderPosition = focus + Vector3.up * 2.2f + shoulderRotation * Vector3.right * 0.6f - shoulderRotation * Vector3.forward * 5.5f;
        var t = Mathf.SmoothStep(0f, 1f, blend);
        transform.SetPositionAndRotation(Vector3.Lerp(shoulderPosition, topPosition, t), Quaternion.Slerp(shoulderRotation, topRotation, t));
    }

    /// <summary>
    /// Where the hero aims: the ground under the mouse from above, or what the middle of the screen points at from
    /// behind the hero.
    /// </summary>
    public bool Aim(out Vector3 point)
    {
        point = default;
        if (!target) return false;
        if (!cam) cam = GetComponent<Camera>();
        if (Mode == View.TopDown)
        {
            // no mouse over the game (another window has it): no aim
            var mouse = Input.mousePosition;
            if (float.IsInfinity(mouse.x) || float.IsNaN(mouse.x) || !cam.pixelRect.Contains(mouse)) return false;
            var ray = cam.ScreenPointToRay(mouse);
            var ground = new Plane(Vector3.up, target.position);
            if (!ground.Raycast(ray, out var distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        var forward = new Ray(cam.transform.position, cam.transform.forward);
        var plane = new Plane(Vector3.up, target.position);
        point = plane.Raycast(forward, out var d) && d < 40f ? forward.GetPoint(d) : target.position + Forward * 12f;
        return true;
    }
}
