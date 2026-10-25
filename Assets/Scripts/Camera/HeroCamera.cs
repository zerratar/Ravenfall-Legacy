using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hero mode's camera (docs/hero-mode-design.md, 7a): top-down like an action RPG (a fixed high angle following the
/// hero, scroll to zoom), or over the shoulder; V switches. The right mouse turns either; it never turns by itself.
/// The cursor stays free in both, as a streamer also reads chat and clicks the HUD. GameCamera.FollowHero turns it on.
/// Sight lines (7e): what stands tall in the line between the camera and the hero (a roof, a wall, a tower) is drawn
/// as its shadow only while it is in the way, so the hero is never lost behind a building.
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
        var position = Vector3.Lerp(shoulderPosition, topPosition, t);
        // a hit's shake: a little noise that dies away
        if (Time.unscaledTime < shakeUntil)
        {
            var fade = (shakeUntil - Time.unscaledTime) / Mathf.Max(0.01f, shakeLength);
            var n = Time.unscaledTime * 38f;
            position += (transform.right * (Mathf.PerlinNoise(n, 0.3f) - 0.5f) + transform.up * (Mathf.PerlinNoise(0.7f, n) - 0.5f)) * shakeStrength * 2f * fade;
        }
        transform.SetPositionAndRotation(position, Quaternion.Slerp(shoulderRotation, topRotation, t));
        UpdateSightLine();
    }

    private float shakeUntil;
    private float shakeLength;
    private float shakeStrength;

    /// <summary>Shakes the camera a little for a while (a hit landing).</summary>
    public void Shake(float strength, float seconds)
    {
        if (Time.unscaledTime < shakeUntil && strength < shakeStrength) return;
        shakeStrength = strength;
        shakeLength = seconds;
        shakeUntil = Time.unscaledTime + seconds;
    }

    // ---- Sight lines ---------------------------------------------------------------------------------------

    private const float SightInterval = 0.1f;
    private const float RayRadius = 0.3f;
    private const float RayMargin = 0.6f;   // how far round a ray a piece may reach and still be hidden
    private const float GroupSize = 16f;    // a piece hides with its parent while the parent is smaller than this
    private const float KeepHidden = 0.4f;

    private static readonly RaycastHit[] sightHits = new RaycastHit[32];
    private static readonly float[] SightHeights = { 1.8f, 1.2f, 0.35f }; // the hero's head, chest and feet
    private readonly Dictionary<Renderer, (UnityEngine.Rendering.ShadowCastingMode Mode, float Until)> hidden = new Dictionary<Renderer, (UnityEngine.Rendering.ShadowCastingMode, float)>();
    private readonly Dictionary<Transform, float> groupSizes = new Dictionary<Transform, float>();
    private readonly List<Renderer> renderersBuffer = new List<Renderer>();
    private readonly List<Renderer> restore = new List<Renderer>();
    private float nextSight;

    /// <summary>
    /// Three thin rays from the camera to the hero's head, chest and feet: what they hit (and the rest of its small
    /// group, a roof with its tiles, a wall walk with its planks) goes shadow only, where it crosses a ray. Things off to
    /// the side (stairs, bushes) stay.
    /// </summary>
    private void UpdateSightLine()
    {
        if (Time.unscaledTime < nextSight) return;
        nextSight = Time.unscaledTime + SightInterval;
        var from = transform.position;
        foreach (var height in SightHeights)
        {
            var to = target.position + Vector3.up * height - from;
            var distance = to.magnitude - 0.8f;
            if (distance < 0.5f) continue;
            var ray = new Ray(from, to.normalized);
            var count = Physics.SphereCastNonAlloc(ray, RayRadius, sightHits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++) Hide(sightHits[i].collider, ray, distance, target.position.y);
        }

        restore.Clear();
        foreach (var h in hidden) if (Time.unscaledTime > h.Value.Until) restore.Add(h.Key);
        foreach (var r in restore) Show(r);
    }

    private void Hide(Collider collider, Ray ray, float distance, float heroY)
    {
        if (!collider || collider is TerrainCollider) return;
        // people and enemies stay
        if (collider.GetComponentInParent<PlayerController>() || collider.GetComponentInParent<EnemyController>() || collider.GetComponentInParent<IslandNpc>()) return;

        // the piece's group: up through its parents while they stay small
        var group = collider.attachedRigidbody ? collider.attachedRigidbody.transform : collider.transform;
        for (var up = 0; up < 3; up++)
        {
            var parent = group.parent;
            if (!parent || parent.GetComponent<IslandController>() || parent.GetComponent<Chunk>() || SizeOf(parent) > GroupSize) break;
            group = parent;
        }

        renderersBuffer.Clear();
        group.GetComponentsInChildren(false, renderersBuffer);
        foreach (var r in renderersBuffer)
        {
            if (!r || r is ParticleSystemRenderer) continue;
            var b = r.bounds;
            // only what is tall enough to hide the hero, not a whole island's ground, and only where it crosses the ray
            if (b.max.y < heroY + 1.6f || b.size.x > 80f || b.size.z > 80f) continue;
            b.Expand(RayMargin * 2f);
            if (!b.IntersectRay(ray, out var at) || at > distance) continue;
            if (hidden.TryGetValue(r, out var was))
            {
                hidden[r] = (was.Mode, Time.unscaledTime + KeepHidden);
                continue;
            }
            hidden[r] = (r.shadowCastingMode, Time.unscaledTime + KeepHidden);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    /// <summary>How big a group is (its renderers' bounds, the larger side), worked out once.</summary>
    private float SizeOf(Transform t)
    {
        if (groupSizes.TryGetValue(t, out var size)) return size;
        size = 0f;
        var first = true;
        var bounds = new Bounds();
        foreach (var r in t.GetComponentsInChildren<Renderer>(false))
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
            if (bounds.size.x > 200f || bounds.size.z > 200f) break;
        }
        if (!first) size = Mathf.Max(bounds.size.x, bounds.size.z);
        groupSizes[t] = size;
        return size;
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
