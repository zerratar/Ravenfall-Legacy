using UnityEngine;

/// <summary>
/// A gentle bob for something floating in a silly world scene (Quackers in Kyo's hot spring, King Clucksworth's crown):
/// up and down and a slow turn. Only runs while it is shown (the Silly surprises setting) and on screen.
/// </summary>
public sealed class SillyBob : MonoBehaviour
{
    public float Height = 0.04f;
    public float Speed = 1.6f;
    public float Turn = 6f;

    private Vector3 start;
    private Quaternion startRotation;
    private Renderer[] renderers;
    private float phase;

    private void Awake()
    {
        start = transform.localPosition;
        startRotation = transform.localRotation;
        renderers = GetComponentsInChildren<Renderer>();
        phase = Random.value * 10f;
    }

    private void Update()
    {
        var seen = false;
        foreach (var r in renderers) if (r && r.isVisible) { seen = true; break; }
        if (!seen) return;
        var t = Time.time * Speed + phase;
        transform.localPosition = start + Vector3.up * Mathf.Sin(t) * Height;
        transform.localRotation = startRotation * Quaternion.Euler(Mathf.Sin(t * 0.7f) * 4f, Mathf.Sin(t * 0.21f) * Turn * 3f, Mathf.Cos(t * 0.9f) * 4f);
    }
}
