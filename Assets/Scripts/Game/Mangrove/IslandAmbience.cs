using UnityEngine;

/// <summary>
/// The banana island's life in the air and water (docs/banana-island.md): birds wheeling over it and fish in the lagoon
/// all day, fireflies in the stilt village after dark. Off in potato mode (they cost a little and change the look) and
/// while the camera is far away; looked at once a second.
/// </summary>
public sealed class IslandAmbience : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] always;
    [SerializeField] private ParticleSystem[] nightOnly;
    [SerializeField] private float seenWithin = 350f;

    private const float DarkBelow = 0.3f;
    private DayNightCycle dayNight;
    private float nextCheck;
    private bool? dayShown, nightShown;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 1f;

        var game = GameManager.Instance;
        var cam = Camera.main;
        var near = cam && (cam.transform.position - transform.position).sqrMagnitude < seenWithin * seenWithin;
        var on = near && !(game && game.PotatoMode);
        if (!dayNight) dayNight = FindAnyObjectByType<DayNightCycle>();
        var night = dayNight && dayNight.isActiveAndEnabled && dayNight.Daylight < DarkBelow;

        Show(always, on, ref dayShown);
        Show(nightOnly, on && night, ref nightShown);
    }

    private static void Show(ParticleSystem[] systems, bool on, ref bool? shown)
    {
        if (shown == on || systems == null) return;
        shown = on;
        foreach (var ps in systems)
        {
            if (!ps) continue;
            if (on)
            {
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }
    }
}
