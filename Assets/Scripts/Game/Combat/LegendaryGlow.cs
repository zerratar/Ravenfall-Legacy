using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     A Legendary's glow (docs/legendary-design.md, section 6): from awakening rank 3 a light and a soft halo of motes in
///     the Legendary's own color on the worn weapon or shield, brighter at rank 4, full at rank 5. Its own setting
///     (Legendary glow), off in potato mode and with graphics off. Checked once a second for every player, like the
///     weapons' element effects (WeaponElementFx).
/// </summary>
public sealed class LegendaryGlow : MonoBehaviour
{
    private const float RefreshSeconds = 1f;
    private static float refreshTimer;

    private GameObject root;
    private Light glowLight;
    private Color color;
    private float strength = -1f;
    private float breath;

    /// <summary>The setting on, not in potato mode, with graphics on.</summary>
    public static bool Enabled
    {
        get
        {
            var game = GameManager.Instance;
            return SettingsApplier.LegendaryGlow && GraphicsToggler.GraphicsEnabled && !(game && game.PotatoMode);
        }
    }

    /// <summary>Once a frame (GameManager): every second, every player's worn Legendaries glow as their rank says.</summary>
    public static void Update(float deltaTime)
    {
        refreshTimer += deltaTime;
        if (refreshTimer < RefreshSeconds) return;
        refreshTimer = 0f;
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        var on = Enabled;
        foreach (var player in game.Players.GetAllPlayers()) Refresh(player, on);
    }

    public static void Refresh(PlayerController player, bool on)
    {
        if (!player || player.Equipment == null) return;
        var eq = player.Equipment;
        Apply(eq.MeleeWeaponObject, player, on);
        Apply(eq.BowObject, player, on);
        Apply(eq.StaffObject, player, on);
        Apply(eq.ShieldObject, player, on);
    }

    private static void Apply(ItemController item, PlayerController player, bool on)
    {
        if (!item) return;
        var amount = 0f;
        var tint = Color.white;
        var worn = player.EquipmentStats?.Legendaries;
        if (on && worn != null)
        {
            foreach (var w in worn)
            {
                // the object must be this Legendary's (a weapon shown from before an equip shows nothing)
                if (item.ItemId != w.Info.ItemId) continue;
                amount = Legendary.Glow(w.Rank);
                ColorUtility.TryParseHtmlString(w.Info.Color, out tint);
            }
        }

        var glow = item.GetComponent<LegendaryGlow>();
        if (amount <= 0f)
        {
            if (glow) glow.Set(tint, 0f);
            return;
        }
        if (!glow) glow = item.gameObject.AddComponent<LegendaryGlow>();
        glow.Set(tint, amount);
    }

    private void Set(Color value, float amount)
    {
        if (amount <= 0f)
        {
            if (root) root.SetActive(false);
            strength = 0f;
            return;
        }
        if (root && Mathf.Abs(amount - strength) < 0.01f && value == color)
        {
            if (!root.activeSelf) root.SetActive(true);
            return;
        }
        color = value;
        strength = amount;
        if (root) Destroy(root);
        Build();
    }

    private void Update()
    {
        if (!glowLight || !root || !root.activeSelf) return;
        // a slow breath, not a flicker
        breath += Time.deltaTime;
        glowLight.intensity = Intensity * (0.85f + 0.15f * Mathf.Sin(breath * 2.2f));
    }

    private float Intensity => 0.8f + 2.4f * strength;

    private void OnDestroy()
    {
        if (root) Destroy(root);
    }

    private void Build()
    {
        // over the model: the biggest renderer's box, as the element effects do
        Renderer biggest = null;
        var size = 0f;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || !r.enabled) continue;
            var s = r.bounds.size.sqrMagnitude;
            if (s > size) { size = s; biggest = r; }
        }
        var holder = biggest ? biggest.transform : transform;
        Mesh mesh = null;
        if (biggest is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
        else if (biggest && biggest.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
        var bounds = mesh ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.3f);

        root = new GameObject("Legendary glow");
        root.layer = gameObject.layer;
        root.transform.SetParent(holder, false);
        root.transform.localPosition = bounds.center;
        root.transform.localRotation = Quaternion.identity;
        // weapon models are often scaled up many times: undone, so the box and speeds are in metres
        var lossy = holder.lossyScale;
        var undo = new Vector3(Safe(lossy.x), Safe(lossy.y), Safe(lossy.z));
        root.transform.localScale = new Vector3(1f / undo.x, 1f / undo.y, 1f / undo.z);

        glowLight = root.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = color;
        glowLight.range = 1.5f + 2f * strength;
        glowLight.intensity = Intensity;
        glowLight.shadows = LightShadows.None;

        var ps = root.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 120;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f + 0.1f * strength, 0.2f + 0.18f * strength);
        main.gravityModifier = -0.04f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.35f), color);
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.Max(Vector3.Scale(bounds.size, undo) * 0.85f, Vector3.one * 0.03f);
        var emission = ps.emission;
        // a faint halo at rank 3, motes that rise off it at rank 5
        emission.rateOverTime = 10f + 60f * strength * strength;
        var colors = ps.colorOverLifetime;
        colors.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f + 0.5f * strength, 0.2f), new GradientAlphaKey(0f, 1f) });
        colors.color = new ParticleSystem.MinMaxGradient(g);
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = WeaponElementFx.Material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        ps.Play();
    }

    private static float Safe(float v) => Mathf.Abs(v) < 1e-4f ? 1f : Mathf.Abs(v);
}
