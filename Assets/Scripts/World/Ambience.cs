using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The islands' weather and life, round the observed player: snow falling on Iceheim, cherry petals drifting over
/// Kyojin, butterflies over the meadows and gulls over the shore by day, and fireflies over the grass at night. On
/// unless the setting turns it off; potato mode turns it off as well (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// Five particle systems made here, moved with the focus (the observed player, or where the camera looks), each
/// emitting only on the islands it belongs to; their particles live in the world, so they stay where they fell while
/// the camera moves on. A few hundred to a couple of thousand small quads, with a shader that needs no textures.
/// </remarks>
public sealed class Ambience : MonoBehaviour
{
    private static readonly int LightId = Shader.PropertyToID("_AmbienceLight");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int LitId = Shader.PropertyToID("_Lit");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private const float IslandCheckEvery = 0.5f;

    /// <summary>The setting: weather and ambient effects on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>Where to be instead of round the observed player, for tools and tests; null for the usual.</summary>
    public static Transform Focus { get; set; }

    /// <summary>How dark it is, 0 by day to 1 at night, as the effects see it.</summary>
    public static float Darkness { get; private set; }

    /// <summary>Where the effects are: on the ground round the observed player, or where the camera looks.</summary>
    public static Vector3 FocusPoint { get; private set; }

    /// <summary>There is a focus just now (not in a menu, a loading screen).</summary>
    public static bool HasFocus { get; private set; }

    /// <summary>The island the focus is on, by its identifier; null over the sea.</summary>
    public static string Island { get; private set; }

    private ParticleSystem snow, petals, fireflies, butterflies, gulls;
    private GameManager game;
    private DayNightCycle dayNight;
    private float nextIslandCheck;
    private Ocean ocean;
    private ParticleSystem.Particle[] particles;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying || FindAnyObjectByType<Ambience>()) return;
        var go = new GameObject("Ambience") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<Ambience>();
    }

    private void Awake()
    {
        var shader = Resources.Load<Shader>("AmbienceParticle");
        if (!shader || !shader.isSupported)
        {
            enabled = false;
            return;
        }
        snow = Make("Snow", shader, 0, false, true);
        petals = Make("Petals", shader, 1, false, true);
        fireflies = Make("Fireflies", shader, 2, true, false);
        butterflies = Make("Butterflies", shader, 3, false, true);
        gulls = Make("Gulls", shader, 4, false, true);
        SetUpSnow(snow);
        SetUpPetals(petals);
        SetUpFireflies(fireflies);
        SetUpButterflies(butterflies);
        SetUpGulls(gulls);
    }

    private ParticleSystem Make(string name, Shader shader, int shape, bool additive, bool lit)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var material = new Material(shader) { name = name };
        material.SetFloat(ShapeId, shape);
        material.SetFloat(SrcBlendId, additive ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
        material.SetFloat(DstBlendId, additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat(LitId, lit ? 1f : 0f);
        material.SetFloat(IntensityId, additive ? 2.4f : 1f);
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingFudge = 10f;

        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = true;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        var box = system.shape;
        box.enabled = true;
        box.shapeType = ParticleSystemShapeType.Box;
        return system;
    }

    private static void SetUpSnow(ParticleSystem system)
    {
        var main = system.main;
        main.maxParticles = 4000;
        main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 18f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.gravityModifier = 0f;
        var box = system.shape;
        box.scale = new Vector3(90f, 1f, 90f);
        box.position = new Vector3(0f, 22f, 0f);
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, -1.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.2f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        Fade(system, 0.08f, 0.9f);
    }

    private static void SetUpPetals(ParticleSystem system)
    {
        var main = system.main;
        main.maxParticles = 900;
        main.startLifetime = new ParticleSystem.MinMaxCurve(16f, 22f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var pinks = new Gradient();
        pinks.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.72f, 0.82f), 0f), new GradientColorKey(new Color(0.98f, 0.55f, 0.72f), 0.5f), new GradientColorKey(new Color(1f, 0.86f, 0.9f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(pinks) { mode = ParticleSystemGradientMode.RandomColor };
        var box = system.shape;
        box.scale = new Vector3(70f, 6f, 70f);
        box.position = new Vector3(0f, 10f, 0f);
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.7f, -0.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
        var spin = system.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.9f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.3f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        Fade(system, 0.1f, 0.85f);
    }

    private static void SetUpFireflies(ParticleSystem system)
    {
        var main = system.main;
        main.maxParticles = 400;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.86f, 0.3f), new Color(0.7f, 1f, 0.28f));
        var box = system.shape;
        box.scale = new Vector3(60f, 2.5f, 60f);
        box.position = new Vector3(0f, 1.5f, 0f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.25f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        // they glow and fade a few times over their life
        var colour = system.colorOverLifetime;
        colour.enabled = true;
        var blink = new Gradient();
        blink.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.2f, 0.35f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0.3f, 0.75f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        colour.color = blink;
    }

    private static void SetUpButterflies(ParticleSystem system)
    {
        var main = system.main;
        main.maxParticles = 80;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.3f, 0.42f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.24f, 0.34f);
        main.startSizeZ = 1f;
        var colours = new Gradient();
        colours.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.62f, 0.18f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.35f), new GradientColorKey(new Color(1f, 0.88f, 0.3f), 0.65f), new GradientColorKey(new Color(0.45f, 0.65f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };
        var box = system.shape;
        box.scale = new Vector3(50f, 1.5f, 50f);
        box.position = new Vector3(0f, 1.2f, 0f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 1.4f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.4f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        Beat(system, 70);
        Fade(system, 0.08f, 0.9f);
    }

    private static void SetUpGulls(ParticleSystem system)
    {
        var main = system.main;
        main.maxParticles = 12;
        main.startLifetime = new ParticleSystem.MinMaxCurve(25f, 40f);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(1.6f, 2.1f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.8f, 1.05f);
        main.startSizeZ = 1f;
        main.startColor = new Color(0.97f, 0.97f, 0.95f);
        var box = system.shape;
        box.scale = new Vector3(70f, 6f, 70f);
        box.position = new Vector3(0f, 22f, 0f);
        // round and round over the shore, drifting with the wind
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        // the three orbital curves must be in the same mode, or Unity says so every frame
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.radial = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocity.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        Beat(system, 45);
        Fade(system, 0.06f, 0.92f);
    }

    /// <summary>
    /// Wings that beat: the quad is squeezed across and opened again, this many times over a particle's life. The
    /// particles are born at different times, so they beat out of step.
    /// </summary>
    private static void Beat(ParticleSystem system, int beats)
    {
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = true;
        var keys = new Keyframe[beats * 2 + 1];
        for (var i = 0; i < keys.Length; ++i) keys[i] = new Keyframe(i / (float)(keys.Length - 1), i % 2 == 0 ? 1f : 0.25f);
        var curve = new AnimationCurve(keys);
        for (var i = 0; i < curve.length; ++i) curve.SmoothTangents(i, 0f);
        size.x = new ParticleSystem.MinMaxCurve(1f, curve);
        size.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
        size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
    }

    /// <summary>In and out over the given parts of a particle's life, so none pops.</summary>
    private static void Fade(ParticleSystem system, float fadeIn, float fadeOut)
    {
        var colour = system.colorOverLifetime;
        colour.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOut), new GradientAlphaKey(0f, 1f) });
        colour.color = fade;
    }

    private void Update()
    {
        if (!game) game = FindAnyObjectByType<GameManager>();
        if (!dayNight) dayNight = FindAnyObjectByType<DayNightCycle>();

        // the focus is found whether or not these are on, as the weather's rain falls round it too
        HasFocus = TryFocus(out var focus);
        if (HasFocus)
        {
            // on the ground, wherever the focus is (the camera's aim falls on the sea's plane, under a hilltop)
            var terrain = Terrain.activeTerrain;
            if (terrain) focus.y = Mathf.Max(focus.y, terrain.SampleHeight(focus) + terrain.transform.position.y);
            FocusPoint = focus;
            if (Time.unscaledTime >= nextIslandCheck)
            {
                nextIslandCheck = Time.unscaledTime + IslandCheckEvery;
                var found = game && game.Islands ? game.Islands.FindIsland(focus) : null;
                Island = found ? found.Identifier : null;
            }
        }
        var night = Darkness = Night();

        // the flakes, petals and raindrops are lit as the world is: by the sun and the sky now (the rain falls with
        // these off too)
        var sun = RenderSettings.sun;
        var light = RenderSettings.ambientSkyColor * 0.8f;
        if (sun && sun.isActiveAndEnabled)
        {
            var colour = sun.color;
            if (sun.useColorTemperature) colour *= Mathf.CorrelatedColorTemperatureToRGB(sun.colorTemperature);
            light += colour * Mathf.Clamp01(sun.intensity) * 0.45f;
        }
        Shader.SetGlobalVector(LightId, new Vector4(Mathf.Min(light.r, 1.1f), Mathf.Min(light.g, 1.1f), Mathf.Min(light.b, 1.1f), 1f));

        if (!Enabled || Suppressed || !HasFocus)
        {
            Emit(snow, 0f);
            Emit(petals, 0f);
            Emit(fireflies, 0f);
            Emit(butterflies, 0f);
            Emit(gulls, 0f);
            return;
        }

        transform.position = focus;
        var island = Island;
        var snowy = island == "Heim";
        // on Iceheim the weather's rain falls as a snowstorm
        Emit(snow, snowy ? 260f * (1f + 3f * Weather.Rain) : 0f);
        Emit(petals, island == "Kyo" ? 22f : 0f);
        // nothing flies in the rain
        var dry = 1f - Weather.Rain;
        Emit(fireflies, island != null && !snowy ? 45f * night * dry : 0f);
        KeepOverLand(fireflies);
        // butterflies over the meadows and gulls over the shore by day
        var day = 1f - night;
        Emit(butterflies, island != null && !snowy ? 3.5f * day * dry * dry : 0f);
        Emit(gulls, island != null ? 0.35f * day * dry : 0f);
    }

    /// <summary>How much it is night, 0 by day to 1 in the dark, from the cycle's light.</summary>
    private float Night()
    {
        if (!dayNight || !dayNight.DayLight || !dayNight.NightLight || !RenderSettings.sun) return 0f;
        // the cycle's own daylight, not the sun's brightness, which the weather dims too
        var day = dayNight.Daylight;
        // dark below 15 % of the day's light, none above 55 %
        var t = Mathf.Clamp01((day - 0.15f) / 0.4f);
        return 1f - t * t * (3f - 2f * t);
    }

    /// <summary>The observed player, or the point on the water's plane the camera looks at.</summary>
    private static bool TryFocus(out Vector3 focus)
    {
        if (Focus)
        {
            focus = Focus.position;
            return true;
        }
        if (TiltShift.FocusPoint.HasValue)
        {
            focus = TiltShift.FocusPoint.Value;
            return true;
        }
        var camera = Camera.main;
        focus = default;
        if (!camera) return false;
        var ray = new Ray(camera.transform.position, camera.transform.forward);
        if (Mathf.Abs(ray.direction.y) < 0.01f) return false;
        var t = (0f - ray.origin.y) / ray.direction.y;
        focus = t > 0f && t < 400f ? ray.GetPoint(t) : camera.transform.position + camera.transform.forward * 30f;
        return true;
    }

    /// <summary>
    /// Puts out the particles just emitted over open water: the box they come from is centred on the focus, so near
    /// a shore half the fireflies were over the sea. Only the newest are looked at, a raycast each, about ten a check.
    /// </summary>
    private void KeepOverLand(ParticleSystem system)
    {
        if (!system || system.particleCount == 0) return;
        if (!ocean) ocean = FindAnyObjectByType<Ocean>();
        if (!ocean) return;
        var sea = ocean.transform.position.y + 0.2f;
        if (particles == null || particles.Length < system.main.maxParticles) particles = new ParticleSystem.Particle[system.main.maxParticles];
        var count = system.GetParticles(particles);
        var changed = false;
        for (var i = 0; i < count; ++i)
        {
            // checked once, in their first quarter second
            if (particles[i].startLifetime - particles[i].remainingLifetime > 0.25f) continue;
            var at = particles[i].position;
            var land = Physics.Raycast(at + Vector3.up * 20f, Vector3.down, out var hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y > sea;
            if (land) continue;
            particles[i].remainingLifetime = 0f;
            changed = true;
        }
        if (changed) system.SetParticles(particles, count);
    }

    private static void Emit(ParticleSystem system, float rate)
    {
        if (!system) return;
        var emission = system.emission;
        emission.rateOverTime = rate;
        if (rate > 0f && !system.isPlaying) system.Play();
    }
}
