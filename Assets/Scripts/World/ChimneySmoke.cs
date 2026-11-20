using UnityEngine;

/// <summary>
/// Smoke rising from a chimney (or a log house's smoke hole) in a place loaded after play started, such as the tower's
/// towns: <see cref="VillageLife"/> only finds the islands' chimneys when play starts. Put it at the chimney's top; the
/// smoke is the islands' own, with the weather and ambient effects setting, and off in potato mode.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChimneySmoke : MonoBehaviour
{
    [Range(0f, 1f)] public float Strength = 0.5f;

    private ParticleSystem smoke;

    private void Update()
    {
        // VillageLife may not have made its materials yet when the place loads
        if (smoke || !VillageLife.Instance) return;
        smoke = VillageLife.Instance.AddChimneySmoke(transform.position, Strength);
        if (!smoke) enabled = false;
    }

    private void OnDisable()
    {
        if (smoke && VillageLife.Instance) VillageLife.Instance.RemoveSmoke(smoke);
        smoke = null;
    }
}
