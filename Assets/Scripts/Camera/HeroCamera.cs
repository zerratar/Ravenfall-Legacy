using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hero mode's camera (docs/hero-mode-design.md, 7a): top-down like an action RPG (a fixed high angle following the
/// hero, scroll to zoom), or over the shoulder; V switches. The right mouse turns either; it never turns by itself.
/// The cursor stays free in both, as a streamer also reads chat and clicks the HUD. GameCamera.FollowHero turns it on.
/// Sight lines (7e): what stands tall between the camera and the hero (a roof, a wall, a tower) is drawn as its
/// shadow only while it is in the way, so the hero is never lost behind a building.
/// </summary>
[RequireComponent(typeof(Camera))]
public class HeroCamera : MonoBehaviour
{
    public enum View { TopDown, Shoulder }

    private const float MinZoom = 8f, MaxZoom = 28f;
    private const float MinShoulder = 2.5f, MaxShoulder = 10f;

    public View Mode = View.TopDown;

    private Transform target;
    private Camera cam;
    private float yaw;
    private float zoom = 16f;
    private float topPitch = 55f;
    private float shoulderPitch = 12f;
    private float shoulderDistance = 5.5f;
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
        // the right mouse turns the camera and tilts it; the wheel zooms (the other cameras' way round)
        if (!Keybinds.IsTyping && Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 4f;
            var tilt = Input.GetAxis("Mouse Y") * 3f;
            if (Mode == View.TopDown) topPitch = Mathf.Clamp(topPitch - tilt, 35f, 75f);
            else shoulderPitch = Mathf.Clamp(shoulderPitch - tilt, -10f, 50f);
        }
        if (!Shinobytes.UI.DraggableWindow.IsPointerOverWindow && !Shinobytes.UI.HudPointer.IsOverHud())
        {
            var step = CameraSettings.ZoomStep(20f);
            if (Mode == View.TopDown) zoom = Mathf.Clamp(zoom - step, MinZoom, MaxZoom);
            else shoulderDistance = Mathf.Clamp(shoulderDistance - step * 0.4f, MinShoulder, MaxShoulder);
        }

        var dt = Time.unscaledDeltaTime;
        blend = Mathf.MoveTowards(blend, Mode == View.TopDown ? 1f : 0f, dt * 3f);
        focus = Vector3.Lerp(focus, target.position, 1f - Mathf.Exp(-10f * dt));

        var topRotation = Quaternion.Euler(topPitch, yaw, 0f);
        var topPosition = focus + Vector3.up - topRotation * Vector3.forward * zoom;
        var shoulderRotation = Quaternion.Euler(shoulderPitch, yaw, 0f);
        var shoulderPosition = focus + Vector3.up * 2.2f + shoulderRotation * Vector3.right * 0.6f - shoulderRotation * Vector3.forward * shoulderDistance;
        var t = Mathf.SmoothStep(0f, 1f, blend);
        transform.SetPositionAndRotation(Vector3.Lerp(shoulderPosition, topPosition, t), Quaternion.Slerp(shoulderRotation, topRotation, t));
        UpdateSightLine();
    }

    // ---- Sight lines ---------------------------------------------------------------------------------------

    private const float SightInterval = 0.1f;
    private const float SightRadius = 2.5f;
    private const float KeepHidden = 0.4f;

    private static readonly Collider[] sightHits = new Collider[96];
    private readonly Dictionary<Renderer, (UnityEngine.Rendering.ShadowCastingMode Mode, float Until)> hidden = new Dictionary<Renderer, (UnityEngine.Rendering.ShadowCastingMode, float)>();
    private readonly List<Renderer> renderersBuffer = new List<Renderer>();
    private readonly List<Renderer> restore = new List<Renderer>();
    private float nextSight;

    /// <summary>A capsule from the camera to the hero's chest: whatever tall stands in it goes shadow only (a whole roof
    /// or wall section, not just the piece a thin line would touch).</summary>
    private void UpdateSightLine()
    {
        if (Time.unscaledTime < nextSight) return;
        nextSight = Time.unscaledTime + SightInterval;
        var chest = target.position + Vector3.up * 1.2f;
        var from = transform.position;
        var to = chest - from;
        var distance = to.magnitude - 1f;
        if (distance > 0.5f)
        {
            var end = from + to.normalized * distance;
            var count = Physics.OverlapCapsuleNonAlloc(from, end, SightRadius, sightHits, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++) Hide(sightHits[i], target.position.y);
        }

        restore.Clear();
        foreach (var h in hidden) if (Time.unscaledTime > h.Value.Until) restore.Add(h.Key);
        foreach (var r in restore) Show(r);
    }

    private void Hide(Collider collider, float heroY)
    {
        if (!collider || collider is TerrainCollider) return;
        // people and enemies stay, and so does the ground
        if (collider.GetComponentInParent<PlayerController>() || collider.GetComponentInParent<EnemyController>() || collider.GetComponentInParent<IslandNpc>()) return;
        var root = collider.attachedRigidbody ? collider.attachedRigidbody.transform : collider.transform;
        renderersBuffer.Clear();
        root.GetComponentsInChildren(false, renderersBuffer);
        if (renderersBuffer.Count == 0) root.GetComponentsInParent(false, renderersBuffer);
        foreach (var r in renderersBuffer)
        {
            if (!r || r is ParticleSystemRenderer) continue;
            var b = r.bounds;
            // only what is tall enough to hide the hero, and not a whole island's ground
            if (b.max.y < heroY + 1.8f || b.size.x > 80f || b.size.z > 80f) continue;
            if (hidden.TryGetValue(r, out var was))
            {
                hidden[r] = (was.Mode, Time.unscaledTime + KeepHidden);
                continue;
            }
            hidden[r] = (r.shadowCastingMode, Time.unscaledTime + KeepHidden);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    private void Show(Renderer r)
    {
        if (hidden.TryGetValue(r, out var was) && r) r.shadowCastingMode = was.Mode;
        hidden.Remove(r);
    }

    private void OnDisable()
    {
        foreach (var h in hidden) if (h.Key) h.Key.shadowCastingMode = h.Value.Mode;
        hidden.Clear();
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
