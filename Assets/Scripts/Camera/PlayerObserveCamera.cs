using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObserveCamera : MonoBehaviour
{
    public bool CanControlPlayer;

    [SerializeField] private float positionOffsetY = 5f;
    [SerializeField] private float viewOffsetY = 5f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private RawImage image;

    private PlayerController player;
    private int playerObserveLayer;


    public bool HasTarget => player != null;

    [Header("Preview controls")]
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float minZoom = 0.35f;
    [SerializeField] private float maxZoom = 2.5f;

    /// <summary>The camera's angle around the player, relative to straight in front of them.</summary>
    private float yaw;
    private float targetYaw;

    // Set from the details preview: tilt up or down, distance as a share of the usual, and a
    // sideways and upwards shift of the whole view, in player heights. Each eases to its target.
    private float pitch;
    private float targetPitch;
    private float zoom = 1f;
    private float targetZoom = 1f;
    private Vector2 pan;
    private Vector2 targetPan;

    /// <summary>
    /// Turns the camera around the observed player. Used by the turn buttons and dragging on the
    /// details preview; the view goes back to the front whenever a different player is observed.
    /// </summary>
    public void Rotate(float degrees)
    {
        targetYaw += degrees;
    }

    /// <summary>Tilts the camera over or under the player, within limits.</summary>
    public void Tilt(float degrees)
    {
        targetPitch = Mathf.Clamp(targetPitch + degrees, minPitch, maxPitch);
    }

    /// <summary>Moves the camera closer (below 1) or further away (above 1), by a factor.</summary>
    public void Zoom(float factor)
    {
        targetZoom = Mathf.Clamp(targetZoom * factor, minZoom, maxZoom);
    }

    /// <summary>Shifts the view sideways and up, in player heights, keeping the player in reach.</summary>
    public void Pan(Vector2 amount)
    {
        targetPan = Vector2.ClampMagnitude(targetPan + amount, 1.5f);
    }

    /// <summary>Back to the front view of the player, as when they were first observed.</summary>
    public void ResetView()
    {
        targetYaw = Mathf.Round(yaw / 360f) * 360f;
        targetPitch = 0f;
        targetZoom = 1f;
        targetPan = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (!player)
        {
            if (image) image.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        if (CanControlPlayer && Keybinds.Pressed(Keybinds.Action.ControlPlayer))
        {
            player.ToggleControl();
            if (player.Controlled)
            {
                Shinobytes.Debug.Log(player.Name + " is now being controlled.");
            }
            else
            {
                Shinobytes.Debug.Log(player.Name + " is no longer being controlled.");
            }
        }

    }

    public void LateUpdate()
    {
        if (!player)
        {
            return;
        }

        if (player.Controlled)
        {
            return;
        }

        // Eases toward the requested view so a turn button swings the camera rather than cutting.
        var ease = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
        yaw = Mathf.Lerp(yaw, targetYaw, ease);
        pitch = Mathf.Lerp(pitch, targetPitch, ease);
        zoom = Mathf.Lerp(zoom, targetZoom, ease);
        pan = Vector2.Lerp(pan, targetPan, ease);

        var t = player.transform;
        var playerScale = t.localScale.y;
        var direction = Quaternion.Euler(0f, yaw, 0f) * t.forward;

        // Orbits the point the camera looks at: the usual offset from it, tilted about the
        // sideways axis and scaled by the zoom. The pan then shifts camera and target together.
        var target = t.position + new Vector3(0, viewOffsetY, 0);
        var offset = direction * (playerScale * distance) + (positionOffsetY - viewOffsetY) * Vector3.up;
        var side = Vector3.Cross(Vector3.up, direction).normalized;
        offset = Quaternion.AngleAxis(pitch, side) * offset * zoom;
        var shift = (side * pan.x + Vector3.up * pan.y) * (playerScale * distance);

        transform.position = target + offset + shift;
        transform.LookAt(target + shift);
    }

    public void ObservePlayer(PlayerController player)
    {
        if (this.player)
        {
            this.player.RevokeControl();
            ResetPlayerLayer();
        }

        if (this.player != player)
        {
            yaw = targetYaw = 0f;
            pitch = targetPitch = 0f;
            zoom = targetZoom = 1f;
            pan = targetPan = Vector2.zero;
        }

        this.player = player;
        if (!this.player)
        {
            return;
        }

        if (image) image.gameObject.SetActive(true);
        gameObject.SetActive(true);
        UpdatePlayerLayer();
    }
    public void UpdatePlayerLayer()
    {
        if (!player) return;
        if (playerObserveLayer <= 0)
            playerObserveLayer = LayerMask.NameToLayer("PlayerObserve");
        SetLayerRecursive(player.gameObject, playerObserveLayer);
    }

    private void ResetPlayerLayer()
    {
        if (!player) return;
        SetLayerRecursive(player.gameObject, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        for (var i = 0; i < go.transform.childCount; ++i)
        {
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
