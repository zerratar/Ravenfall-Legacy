using UnityEngine;

/// <summary>
/// Shows what is under it only after dark: the Count at Ravebridge's manor sleeps by day and stands by his coffin at
/// night. Looked at once a second, from the day cycle's own daylight; with no cycle (or with it off) it stays as the
/// scene left it. Outside play nothing changes, so the editor shows it as placed.
/// </summary>
public sealed class NightOnly : MonoBehaviour
{
    // up below this much of the day's light, as dark as the torches start to reach (NightLights)
    private const float DarkBelow = 0.3f;
    private const float CheckEvery = 1f;

    private DayNightCycle dayNight;
    private float nextCheck;
    private bool? shown;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckEvery;
        if (!dayNight) dayNight = FindAnyObjectByType<DayNightCycle>();
        if (!dayNight || !dayNight.isActiveAndEnabled) return;

        var night = dayNight.Daylight < DarkBelow;
        if (shown == night) return;
        shown = night;
        foreach (Transform child in transform) child.gameObject.SetActive(night);
    }
}
