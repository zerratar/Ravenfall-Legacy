using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     One wall piece of an indoor floor (<see cref="TowerCutaway"/>): its full height and its low stub. Placed at the
///     wall's middle, forward along the wall's face (either way), right along its length.
/// </summary>
public sealed class TowerWallPiece : MonoBehaviour
{
    /// <summary>The whole wall (and what hangs on it: banners, torches, windows).</summary>
    public GameObject Full;

    /// <summary>What is left of it while it is cut: a broken stub a metre high.</summary>
    public GameObject Low;

    public float HalfLength = 2.5f;

    private Renderer[] full;
    private UnityEngine.Rendering.ShadowCastingMode[] modes;
    private Renderer[] low;
    private Renderer[] flames;
    private bool cut;
    private bool ready;

    private void Ready()
    {
        if (ready) return;
        ready = true;
        var list = new List<Renderer>();
        var fire = new List<Renderer>();
        if (Full) foreach (var r in Full.GetComponentsInChildren<Renderer>(true)) (r is ParticleSystemRenderer ? fire : list).Add(r);
        full = list.ToArray();
        flames = fire.ToArray();
        modes = new UnityEngine.Rendering.ShadowCastingMode[full.Length];
        for (var i = 0; i < full.Length; i++) modes[i] = full[i].shadowCastingMode;
        low = Low ? Low.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        foreach (var r in low) r.enabled = false;
    }

    private void Awake() => Ready();

    public void SetCut(bool on)
    {
        Ready();
        if (on == cut) return;
        cut = on;
        // the full wall keeps its shadow on the floor: the room's light does not change as the camera turns
        for (var i = 0; i < full.Length; i++)
        {
            if (!full[i]) continue;
            full[i].shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : modes[i];
        }
        foreach (var r in low) if (r) r.enabled = on;
        // a torch's flames would hang in the air where the wall was
        foreach (var r in flames) if (r) r.enabled = !on;
    }
}
