using System;
using UnityEngine;

/// <summary>
/// One of the regatta Kraken's tentacles (docs/banana-island.md, "Pickups, hazards and meddling"): Pirate Realm's
/// tentacle, a chain of joints from the root to the tip. It waves, more towards the tip, and curls over towards its
/// own -X (the regatta turns that towards the boat it holds). The joints are set by tools/mangrove/regatta-props.cs.
/// </summary>
public sealed class KrakenTentacle : MonoBehaviour
{
    public Transform[] Joints;

    /// <summary>0 still, 1 thrashing.</summary>
    [NonSerialized] public float Wave = 1f;
    /// <summary>0 straight up, 1 bent right over.</summary>
    [NonSerialized] public float Curl;

    private Quaternion[] rest;
    private float seed;

    private void Awake()
    {
        if (Joints == null) Joints = Array.Empty<Transform>();
        rest = new Quaternion[Joints.Length];
        for (var i = 0; i < Joints.Length; ++i) rest[i] = Joints[i].localRotation;
        seed = UnityEngine.Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        var count = Joints.Length;
        if (count == 0) return;
        var t = Time.time * 3.2f + seed;
        for (var i = 0; i < count; ++i)
        {
            // the root barely moves, the tip whips; the wave runs up the tentacle
            var along = count > 1 ? i / (float)(count - 1) : 1f;
            var flex = Mathf.Lerp(0.15f, 1f, along * along);
            var sway = Mathf.Sin(t - i * 0.55f) * 38f * flex * Wave;
            var twist = Mathf.Sin(t * 0.8f - i * 0.45f + seed) * 30f * flex * Wave;
            var bend = Curl * 18f * Mathf.Lerp(0.4f, 1.2f, along);
            Joints[i].localRotation = rest[i] * Quaternion.Euler(0f, twist, sway + bend);
        }
    }
}
