using UnityEngine;

/// <summary>
///     A door between two rooms of a floor's route (docs/tower-design.md section 22): shut and sealed with ghostfire while
///     the room before it still has monsters, swung open when they have fallen. While it is shut it cuts the floor's
///     navmesh (a carving obstacle), so nobody walks through it; the seal's glow says from above that the way is closed.
/// </summary>
public sealed class TowerGate : MonoBehaviour
{
    /// <summary>The door leaves, turned about their hinge (their own pivot) by <see cref="OpenYaw"/> when open.</summary>
    public Transform[] Leaves = new Transform[0];

    /// <summary>Degrees each leaf turns when open (one per leaf; inward is positive for the left leaf).</summary>
    public float[] OpenYaw = new float[0];

    /// <summary>The ghostfire across the doorway: shrinks away as the door opens.</summary>
    public GameObject Seal;

    public Light SealLight;

    public float Seconds = 1.8f;

    public bool IsOpen { get; private set; }

    private Quaternion[] closed;
    private float sealLight;
    private Vector3 sealScale = Vector3.one;
    private float shown;   // 0 shut, 1 open: where the leaves are now
    private UnityEngine.AI.NavMeshObstacle block;
    private bool ready;

    private void Ready()
    {
        if (ready) return;
        ready = true;
        closed = new Quaternion[Leaves.Length];
        for (var i = 0; i < Leaves.Length; i++) closed[i] = Leaves[i] ? Leaves[i].localRotation : Quaternion.identity;
        if (SealLight) sealLight = SealLight.intensity;
        if (Seal) sealScale = Seal.transform.localScale;
        block = GetComponent<UnityEngine.AI.NavMeshObstacle>();
    }

    private void Awake() => Ready();

    /// <summary>Open (the room before it is cleared, or the boss is called); <paramref name="instant"/> skips the swing.</summary>
    public void Open(bool instant = false)
    {
        Ready();
        if (IsOpen && !instant) return;
        IsOpen = true;
        if (block) block.enabled = false;
        if (instant) Show(1f);
        else GameSounds.Play(SoundKind.RockDebris, transform.position + Vector3.up, 0.55f);
    }

    /// <summary>Shut again (the floor refilled for a new climb).</summary>
    public void Close(bool instant = true)
    {
        Ready();
        IsOpen = false;
        if (block) block.enabled = true;
        if (instant) Show(0f);
    }

    private void Update()
    {
        var target = IsOpen ? 1f : 0f;
        if (Mathf.Approximately(shown, target)) return;
        Show(Mathf.MoveTowards(shown, target, Time.deltaTime / Mathf.Max(0.05f, Seconds)));
    }

    private void Show(float t)
    {
        shown = t;
        var eased = t * t * (3f - 2f * t);
        for (var i = 0; i < Leaves.Length; i++)
        {
            if (!Leaves[i]) continue;
            var yaw = i < OpenYaw.Length ? OpenYaw[i] : 90f;
            Leaves[i].localRotation = closed[i] * Quaternion.Euler(0f, yaw * eased, 0f);
        }
        // the seal goes first: gone by the time the leaves are a third open
        var seal = Mathf.Clamp01(1f - t * 3f);
        if (Seal)
        {
            Seal.SetActive(seal > 0.01f);
            Seal.transform.localScale = new Vector3(sealScale.x, sealScale.y * seal, sealScale.z);
        }
        if (SealLight)
        {
            SealLight.enabled = seal > 0.01f;
            SealLight.intensity = sealLight * seal;
        }
    }
}
