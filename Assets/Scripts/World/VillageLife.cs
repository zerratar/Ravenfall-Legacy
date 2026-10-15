using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The little signs of life about the islands: windmill sails turning, smoke rising from chimneys and forge flues,
/// braziers burning, and the campfire with a flickering light of its own after dark. With the weather and ambient
/// effects setting, and off in potato mode (<see cref="Ambience"/>).
/// </summary>
/// <remarks>
/// Found by their kit names when play starts; nothing in the scene is changed. A sail turns about the axis its mesh
/// is thinnest along. A brazier that already has a fire of its own (a particle system over its bowl) is left alone.
/// Each smoke or flame is a small particle system, paused while it is out of sight.
/// </remarks>
public sealed class VillageLife : MonoBehaviour
{
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int LitId = Shader.PropertyToID("_Lit");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    /// <summary>A fire already burning this close (in metres, across) to a brazier's bowl is its own.</summary>
    private const float OwnFireReach = 1.2f;

    private readonly List<(Transform sails, Vector3 axis, float speed)> sails = new List<(Transform, Vector3, float)>();
    private readonly List<ParticleSystem> systems = new List<ParticleSystem>();
    private readonly List<Light> fireLights = new List<Light>();
    private Material smokeMaterial, flameMaterial;
    private bool running = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("Village Life") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<VillageLife>();
    }

    private void Start()
    {
        var shader = Resources.Load<Shader>("AmbienceParticle");
        if (shader && shader.isSupported)
        {
            smokeMaterial = MakeMaterial(shader, 5, false, true, 1f);
            flameMaterial = MakeMaterial(shader, 6, true, false, 1.6f);
        }

        var fires = new List<Vector3>();
        foreach (var system in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include)) fires.Add(system.transform.position);

        var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var renderer in renderers)
        {
            var name = renderer.name;
            var bounds = renderer.bounds;
            if (name.Contains("Windmill") && name.Contains("Blades")) AddSails(renderer);
            else if (!smokeMaterial) continue;
            else if (name.StartsWith("SM_Bld_Chimney") || name.StartsWith("SM_Prop_Workbench_Forge_01_Flue")) AddSmoke(Top(bounds, 0f), 0.5f);
            else if (name.StartsWith("SM_Prop_Camp_Fireplace_01")) AddCampfire(bounds);
            else if ((name.Contains("Brazier") || name.Contains("BottleTorch")) && !name.Contains("_Insert"))
            {
                var bowl = Top(bounds, Mathf.Min(0.1f, bounds.size.y * 0.1f));
                if (Burning(fires, bowl) || Covered(renderers, bowl)) continue;
                // the same brazier can be in the scene more than once (a level of detail, a static copy)
                fires.Add(bowl);
                AddFlame(bowl, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.4f, name.Contains("BottleTorch") ? 0.5f : 1f);
            }
        }
    }

    private static Vector3 Top(Bounds bounds, float down) => new Vector3(bounds.center.x, bounds.max.y - down, bounds.center.z);

    /// <summary>Something sits on it (a statue on a pedestal that is a brazier by name): it is not lit.</summary>
    private static bool Covered(MeshRenderer[] renderers, Vector3 bowl)
    {
        foreach (var other in renderers)
        {
            var bounds = other.bounds;
            if (bounds.min.y < bowl.y - 0.25f || bounds.min.y > bowl.y + 0.5f) continue;
            if (bowl.x < bounds.min.x || bowl.x > bounds.max.x || bowl.z < bounds.min.z || bowl.z > bounds.max.z) continue;
            if (!other.name.Contains("Brazier")) return true;
        }
        return false;
    }

    private static bool Burning(List<Vector3> fires, Vector3 at)
    {
        foreach (var fire in fires)
        {
            var dx = fire.x - at.x;
            var dz = fire.z - at.z;
            if (dx * dx + dz * dz < OwnFireReach * OwnFireReach && Mathf.Abs(fire.y - at.y) < 2f) return true;
        }
        return false;
    }

    private static Material MakeMaterial(Shader shader, int shape, bool additive, bool lit, float intensity)
    {
        var material = new Material(shader);
        material.SetFloat(ShapeId, shape);
        material.SetFloat(SrcBlendId, additive ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
        material.SetFloat(DstBlendId, additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat(LitId, lit ? 1f : 0f);
        material.SetFloat(IntensityId, intensity);
        return material;
    }

    /// <summary>Sails turn about the axis their mesh is thinnest along, through their own centre.</summary>
    private void AddSails(MeshRenderer renderer)
    {
        var filter = renderer.GetComponent<MeshFilter>();
        if (!filter || !filter.sharedMesh) return;
        var size = filter.sharedMesh.bounds.size;
        var axis = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
        sails.Add((renderer.transform, axis, Random.Range(18f, 26f)));
    }

    /// <summary>A thin grey column, widening and thinning out as it rises and the wind takes it.</summary>
    private void AddSmoke(Vector3 at, float strength)
    {
        var system = MakeSystem("Smoke", smokeMaterial, at);
        var main = system.main;
        main.maxParticles = 140;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
        var grey = 0.58f + 0.08f * strength;
        main.startColor = new Color(grey, grey, grey + 0.02f, 0.18f + 0.2f * strength);
        var emission = system.emission;
        emission.rateOverTime = 9f + 6f * strength;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var wind = SkyClouds.Wind.normalized;
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, wind.x * 0.45f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, wind.y * 0.45f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.15f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 4f));
        Fade(system, Color.white, Color.white, 0.1f, 0.4f);
        Begin(system);
    }

    /// <summary>Flames licking up from a bowl of coals: white-yellow at the root, red as they die.</summary>
    private void AddFlame(Vector3 at, float radius, float scale)
    {
        var system = MakeSystem("Flame", flameMaterial, at);
        var main = system.main;
        main.maxParticles = Mathf.CeilToInt(40 * scale);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * scale + 0.2f, 0.9f * scale + 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f * scale, 0.75f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.2f), new Color(1f, 0.78f, 0.34f));
        var emission = system.emission;
        emission.rateOverTime = 36f * scale;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.03f, radius);
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 1f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));
        Fade(system, new Color(1f, 0.95f, 0.75f), new Color(0.9f, 0.25f, 0.08f), 0.12f, 0.55f);
        Begin(system);
    }

    private void AddCampfire(Bounds bounds)
    {
        var at = new Vector3(bounds.center.x, bounds.min.y + 0.12f, bounds.center.z);
        AddFlame(at, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.35f, 1.2f);
        AddSmoke(at + Vector3.up * 0.6f, 0.8f);

        var light = new GameObject("Fire Light").AddComponent<Light>();
        light.transform.SetParent(transform, false);
        light.transform.position = at + Vector3.up * 0.7f;
        light.type = LightType.Point;
        light.range = 8f;
        light.useColorTemperature = true;
        light.colorTemperature = 1900f;
        light.shadows = LightShadows.None;
        fireLights.Add(light);
    }

    private ParticleSystem MakeSystem(string name, Material material, Vector3 at)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = at;
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = true;
        main.prewarm = true;
        main.cullingMode = ParticleSystemCullingMode.Pause;
        return system;
    }

    private void Begin(ParticleSystem system)
    {
        systems.Add(system);
        if (running) system.Play();
    }

    private static void Fade(ParticleSystem system, Color from, Color to, float fadeIn, float fadeOut)
    {
        var colour = system.colorOverLifetime;
        colour.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(0.6f, fadeOut), new GradientAlphaKey(0f, 1f) });
        colour.color = fade;
    }

    private void Update()
    {
        var on = Ambience.Enabled && !Ambience.Suppressed;
        if (on != running)
        {
            running = on;
            foreach (var system in systems)
            {
                if (on) system.Play();
                else system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (on)
        {
            var dt = Time.deltaTime;
            foreach (var (transform, axis, speed) in sails) if (transform) transform.Rotate(axis, speed * dt, Space.Self);
        }

        // the campfire lights the ground round it, more after dark, flickering
        var night = Ambience.Darkness;
        var time = Time.time;
        for (var i = 0; i < fireLights.Count; ++i)
        {
            var light = fireLights[i];
            if (!light) continue;
            light.enabled = on;
            var flicker = 0.8f + 0.12f * Mathf.Sin(time * 9.1f + i) + 0.08f * Mathf.PerlinNoise(time * 4f, i);
            light.intensity = (0.6f + 2.6f * night) * flicker;
        }
    }
}
