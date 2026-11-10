using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     Weapon effects you can see (docs/combat-v2-design.md, "Weapon effects"): a weapon that turns at least
///     <see cref="MinShare"/> of its hit into an element (its own stats and the gems in it) gives off that element:
///     embers rising off fire, frost motes drifting from ice, sparks jumping off lightning and venom dripping from poison.
///     More of the element, more of the effect. Only the drawn weapon of the style in use is visible, as before. The
///     Weapon effects setting turns them off, and so does potato mode.
/// </summary>
public sealed class WeaponElementFx : MonoBehaviour
{
    /// <summary>Weapons with less of an element than this stay plain (the themed sets' lower tiers, a chipped gem).</summary>
    public const double MinShare = 0.15;

    private const float RefreshSeconds = 1f;

    private static Material material;
    private static Texture2D dot;
    private static float refreshTimer;

    private ParticleSystem system;
    private Element element = Element.Physical;
    private float strength = -1f;

    /// <summary>The setting on and not in potato mode, with graphics on.</summary>
    public static bool Enabled
    {
        get
        {
            var game = GameManager.Instance;
            return SettingsApplier.WeaponEffects && GraphicsToggler.GraphicsEnabled && !(game && game.PotatoMode);
        }
    }

    /// <summary>Once a frame (GameManager): every second, every player's weapons get the effect they should have.</summary>
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

    /// <summary>A player's weapons: an effect on each one with enough of an element, none on the rest.</summary>
    public static void Refresh(PlayerController player, bool on)
    {
        if (!player || player.Equipment == null) return;
        var extras = on && CombatRules.Enabled ? player.EquipmentStats?.WeaponExtras : null;
        Refresh(player.Equipment.MeleeWeaponObject, extras, AttackStyle.Melee);
        Refresh(player.Equipment.BowObject, extras, AttackStyle.Ranged);
        Refresh(player.Equipment.StaffObject, extras, AttackStyle.Magic);
    }

    private static void Refresh(ItemController item, GearStats[] extras, AttackStyle style)
    {
        if (!item) return;
        var gear = extras != null && (int)style < extras.Length ? extras[(int)style] : null;
        var best = Element.Physical;
        var share = 0.0;
        if (gear != null)
        {
            for (var i = 1; i < Elements.All.Length; ++i)
            {
                var e = Elements.All[i];
                var s = gear[GearStats.DamageOf(e)];
                if (s > share)
                {
                    share = s;
                    best = e;
                }
            }
        }

        var fx = item.GetComponent<WeaponElementFx>();
        if (share < MinShare)
        {
            if (fx) fx.Set(Element.Physical, 0f);
            return;
        }

        if (!fx) fx = item.gameObject.AddComponent<WeaponElementFx>();
        // 15% a faint effect, all of the hit a strong one
        fx.Set(best, Mathf.Clamp01((float)((share - MinShare) / (1 - MinShare))) * 0.75f + 0.25f);
    }

    private void Set(Element value, float amount)
    {
        if (value == Element.Physical)
        {
            if (system) system.gameObject.SetActive(false);
            element = Element.Physical;
            return;
        }

        if (system && value == element && Mathf.Abs(amount - strength) < 0.05f)
        {
            if (!system.gameObject.activeSelf) system.gameObject.SetActive(true);
            return;
        }

        element = value;
        strength = amount;
        if (system) Destroy(system.gameObject);
        system = Build(transform, value, amount);
    }

    private void OnDestroy()
    {
        if (system) Destroy(system.gameObject);
    }

    // ---- the effect -----------------------------------------------------------------------------------------------

    private static ParticleSystem Build(Transform weapon, Element element, float amount)
    {
        // over the blade: the biggest model's own box (a weapon look sits over the base model, which hides inside it;
        // the mesh need not be readable for its bounds)
        Mesh mesh = null;
        var holder = weapon;
        var biggest = 0f;
        foreach (var r in weapon.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || !r.enabled) continue;
            var m = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
            if (!m) continue;
            var size = r.bounds.size.sqrMagnitude;
            if (size <= biggest) continue;
            biggest = size;
            mesh = m;
            holder = r.transform;
        }
        var bounds = mesh ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.3f);

        var go = new GameObject("Weapon " + element);
        go.layer = weapon.gameObject.layer;
        go.transform.SetParent(holder, false);
        go.transform.localPosition = bounds.center;
        go.transform.localRotation = Quaternion.identity;
        // weapon models are often scaled up many times (a pickle at 26): the effect undoes that, so its box, speeds
        // and noise are in metres
        var lossy = holder.lossyScale;
        var undo = new Vector3(Safe(lossy.x), Safe(lossy.y), Safe(lossy.z));
        go.transform.localScale = new Vector3(1f / undo.x, 1f / undo.y, 1f / undo.z);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // the trail stays behind a swing
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; // with the undone scale: metres
        main.maxParticles = 160;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.Max(Vector3.Scale(bounds.size, undo) * 0.9f, Vector3.one * 0.02f);

        var emission = ps.emission;
        var colors = ps.colorOverLifetime;
        colors.enabled = true;
        var sizes = ps.sizeOverLifetime;
        sizes.enabled = true;
        var noise = ps.noise;

        switch (element)
        {
            case Element.Fire:
                // embers: rise, flicker and go from yellow to deep red
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.gravityModifier = -0.12f;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.5f, 0.12f));
                emission.rateOverTime = 70f * amount;
                colors.color = Fade(new Color(1f, 0.9f, 0.5f), new Color(1f, 0.25f, 0.05f));
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
                noise.enabled = true;
                noise.strength = 0.18f;
                noise.frequency = 2.5f;
                break;
            case Element.Ice:
                // frost: pale motes drifting down slowly, a little cold mist
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.08f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                main.gravityModifier = 0.03f;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.97f, 1f), new Color(0.55f, 0.82f, 1f));
                emission.rateOverTime = 45f * amount;
                colors.color = Fade(new Color(0.9f, 0.98f, 1f), new Color(0.5f, 0.8f, 1f), 0.25f);
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
                break;
            case Element.Lightning:
                // sparks: short bright jumps in every direction, white to violet
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
                main.gravityModifier = 0f;
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.8f, 0.65f, 1f));
                emission.rateOverTime = 60f * amount;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 12, 0, 0.3f) { probability = 0.5f + 0.5f * amount } });
                colors.color = Fade(Color.white, new Color(0.65f, 0.5f, 1f), 0f);
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
                shape.randomDirectionAmount = 1f;
                break;
            case Element.Poison:
                // venom: green drops that gather and fall
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
                main.gravityModifier = 0.45f;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 1f, 0.3f), new Color(0.3f, 0.8f, 0.15f));
                emission.rateOverTime = 40f * amount;
                colors.color = Fade(new Color(0.6f, 1f, 0.35f), new Color(0.25f, 0.6f, 0.1f), 0.1f);
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 0.3f, 1f));
                break;
        }

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Material;
        renderer.renderMode = element == Element.Lightning ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (element == Element.Lightning)
        {
            // sparks are streaks along where they jump
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 4f;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        ps.Play();
        return ps;
    }

    private static float Safe(float v) => Mathf.Abs(v) < 1e-4f ? 1f : Mathf.Abs(v);

    // the colour from start to end, faded in quickly and out at the end
    private static ParticleSystem.MinMaxGradient Fade(Color start, Color end, float fadeIn = 0.15f)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(fadeIn > 0 ? 0f : 1f, 0f), new GradientAlphaKey(1f, Mathf.Max(0.01f, fadeIn)), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    // hero mode's additive particle material with a soft dot
    private static Material Material
    {
        get
        {
            if (material) return material;
            var template = Resources.Load<Material>("HeroTrail");
            material = template ? new Material(template) : new Material(Shader.Find("Sprites/Default"));
            material.name = "Weapon element fx";
            material.mainTexture = Dot;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Dot);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            return material;
        }
    }

    private static Texture2D Dot
    {
        get
        {
            if (dot) return dot;
            const int Size = 32;
            dot = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Weapon fx dot", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; ++y)
            {
                for (var x = 0; x < Size; ++x)
                {
                    var dx = (x + 0.5f) / Size * 2f - 1f;
                    var dy = (y + 0.5f) / Size * 2f - 1f;
                    var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            dot.SetPixels32(pixels);
            dot.Apply();
            return dot;
        }
    }
}
