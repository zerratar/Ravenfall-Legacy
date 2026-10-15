using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The little signs of life about the islands: windmill sails turning, smoke rising from chimneys and forge flues,
/// braziers and the campfire burning, each lighting the ground round it, more after dark, and the village's houses
/// showing who is home (smoke, lit windows, lamplight). With the weather and ambient effects setting, and off in potato
/// mode (<see cref="Ambience"/>).
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
    private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int ModeId = Shader.PropertyToID("_Mode");

    // the village houses' window glass after dark: lit warm from inside (past white, so it blooms), and dark
    private static readonly Color WindowsLit = new Color(2.6f, 1.7f, 0.85f);
    private static readonly Color WindowsDark = new Color(0.1f, 0.1f, 0.12f);

    /// <summary>A fire already burning this close (in metres, across) to a brazier's bowl is its own.</summary>
    private const float OwnFireReach = 1.2f;

    /// <summary>How bright the warm glow round each fire is at night; none by day.</summary>
    private const float GlowAtNight = 0.2f;

    /// <summary>How often the village houses are looked over (who lives where, built, moved, pulled down), seconds.</summary>
    private const float HouseCheckEvery = 2f;

    private readonly List<(Transform sails, Vector3 axis, float speed)> sails = new List<(Transform, Vector3, float)>();
    private readonly List<ParticleSystem> systems = new List<ParticleSystem>();
    private readonly List<(Light light, float strength)> fireLights = new List<(Light, float)>();
    private readonly Dictionary<TownHouseController, Home> homes = new Dictionary<TownHouseController, Home>();
    private Material windowGlow, windowDark;
    private RenderParams glowParams, darkParams;
    private PlayerManager players;
    private readonly HashSet<TownHouseController> housesSeen = new HashSet<TownHouseController>();
    private readonly List<TownHouseController> housesGone = new List<TownHouseController>();
    private Material smokeMaterial, flameMaterial, glowMaterial;
    private float nextHouseCheck;
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
            smokeMaterial.SetFloat(SoftFadeId, 0.4f);
            flameMaterial = MakeMaterial(shader, 6, true, false, 1.6f);
            // drawn additive, soft where it meets the ground and the brazier
            glowMaterial = MakeMaterial(shader, 7, true, false, 0f);
            glowMaterial.SetFloat(SoftFadeId, 0.6f);
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

    /// <summary>A thin column, grey unless told otherwise, widening and thinning out as it rises and the wind takes it.</summary>
    private ParticleSystem AddSmoke(Vector3 at, float strength, Color? colour = null)
    {
        var system = MakeSystem("Smoke", smokeMaterial, at);
        var main = system.main;
        main.maxParticles = 140;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
        var grey = 0.58f + 0.08f * strength;
        var tint = colour ?? new Color(grey, grey, grey + 0.02f);
        main.startColor = new Color(tint.r, tint.g, tint.b, 0.18f + 0.2f * strength);
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
        return system;
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
        AddGlow(at + Vector3.up * (0.35f * scale), scale);
        AddLight(at + Vector3.up * (0.6f * scale), 1.5f + 8f * scale, scale);
    }

    /// <summary>
    /// The fire's own light on the ground and the walls round it, flickering, strong after dark and faint by day. No
    /// shadows, and the renderer clusters its lights (Forward+), so a few dozen of them cost little.
    /// </summary>
    private void AddLight(Vector3 at, float range, float strength)
    {
        var light = new GameObject("Fire Light").AddComponent<Light>();
        light.transform.SetParent(transform, false);
        light.transform.position = at;
        light.type = LightType.Point;
        light.range = range;
        light.useColorTemperature = true;
        light.colorTemperature = 1900f;
        light.shadows = LightShadows.None;
        fireLights.Add((light, strength));
    }

    /// <summary>
    /// A warm glow round a fire after dark, breathing a little: two or three wide soft discs overlapping, fading in
    /// and out. The light a brazier would give, without the cost of a light.
    /// </summary>
    private void AddGlow(Vector3 at, float scale)
    {
        var system = MakeSystem("Glow", glowMaterial, at);
        var main = system.main;
        main.maxParticles = 4;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(2.4f * scale, 3f * scale);
        main.startColor = new Color(1f, 0.5f, 0.18f);
        var emission = system.emission;
        emission.rateOverTime = 1.4f;
        var shape = system.shape;
        shape.enabled = false;
        Fade(system, Color.white, Color.white, 0.35f, 0.65f);
        Begin(system);
    }

    private void AddCampfire(Bounds bounds)
    {
        var at = new Vector3(bounds.center.x, bounds.min.y + 0.12f, bounds.center.z);
        AddFlame(at, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.35f, 1.2f);
        AddSmoke(at + Vector3.up * 0.6f, 0.8f);
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

    /// <summary>A village house, and the signs that someone is home: its chimney's smoke, lit windows, lamplight round it.</summary>
    private sealed class Home
    {
        public TownHouse Type;
        public MeshRenderer Renderer;
        public int Glass = -1;          // the window glass among the renderer's materials
        public ParticleSystem Smoke;
        public Light Lamp;
        public Mesh Mesh;
        public bool Lived;              // its owner is in the game
    }

    /// <summary>
    /// The village's houses show who is home: while a house's owner is in the game its chimney smokes (TownHouse.Chimney
    /// says where on each kind), and after dark its windows glow warm and its lamplight falls on the ground round it.
    /// An empty house is dark at night and cold. The houses come and go as the village is built, moved and pulled down,
    /// and players come and go, so they are looked over now and then.
    /// </summary>
    private void Houses()
    {
        if (!players)
        {
            var game = FindAnyObjectByType<GameManager>();
            if (game) players = game.Players;
        }

        housesSeen.Clear();
        foreach (var house in FindObjectsByType<TownHouseController>(FindObjectsInactive.Exclude))
        {
            var type = house.TownHouse;
            var slot = house.Slot;
            // a copy being placed in build mode has its behaviours off and no plot
            if (!type || !slot || !house.enabled) continue;
            housesSeen.Add(house);

            if (!homes.TryGetValue(house, out var home) || home.Type != type)
            {
                if (home != null) Forget(home);
                home = Find(house, type);
                homes[house] = home;
            }

            var owner = slot.OwnerUserId;
            home.Lived = owner != null && players && players.GetPlayerByUserId(owner.Value);

            if (home.Lived && type.Chimney != Vector3.zero && smokeMaterial)
            {
                var at = house.transform.TransformPoint(type.Chimney);
                if (home.Smoke) home.Smoke.transform.position = at;
                else home.Smoke = AddSmoke(at, 0.35f, type.ChimneySmoke);
            }
            else if (home.Smoke)
            {
                Remove(home.Smoke);
                home.Smoke = null;
            }

            if (home.Renderer)
            {
                var bounds = home.Renderer.bounds;
                home.Lamp.transform.position = new Vector3(bounds.center.x, bounds.min.y + 1.4f, bounds.center.z);
                home.Lamp.range = Mathf.Max(bounds.extents.x, bounds.extents.z) + 5f;
            }
        }

        housesGone.Clear();
        foreach (var house in homes.Keys) if (!house || !housesSeen.Contains(house)) housesGone.Add(house);
        foreach (var house in housesGone)
        {
            Forget(homes[house]);
            homes.Remove(house);
        }
    }

    /// <summary>The house's own mesh and its window glass, and a lamp for it (lit only while someone is home after dark).</summary>
    private Home Find(TownHouseController house, TownHouse type)
    {
        var home = new Home { Type = type };
        foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>())
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh || !filter.sharedMesh.name.Contains("Village_House")) continue;
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; ++i)
            {
                if (!materials[i] || !materials[i].name.StartsWith("Village_StainedGlass")) continue;
                home.Renderer = renderer;
                home.Glass = i;
                MakeWindows(materials[i]);
                break;
            }
            if (home.Renderer) break;
        }

        // the lamplight comes from inside the house, out through its windows onto the ground; without shadows it
        // reaches the ground all round, and the walls, facing away from it, stay unlit
        home.Lamp = new GameObject("Home Lamp").AddComponent<Light>();
        home.Lamp.transform.SetParent(transform, false);
        home.Lamp.type = LightType.Point;
        home.Lamp.useColorTemperature = true;
        home.Lamp.colorTemperature = 2400f;
        home.Lamp.shadows = LightShadows.None;
        home.Lamp.enabled = false;
        return home;
    }

    /// <summary>Puts out a house's signs: no smoke, no lamp.</summary>
    private void Forget(Home home)
    {
        if (home.Smoke) Remove(home.Smoke);
        if (home.Lamp) Destroy(home.Lamp.gameObject);
    }

    /// <summary>
    /// The windows and the lamp for how dark it is: lit warm where someone is home, dark where no one is. The glass is
    /// drawn again over itself (WindowGlow): the houses' own material has no emission wired, so it cannot glow, and a
    /// copy of it per house would be a material per house where two shared ones do.
    /// </summary>
    private void Lamps(bool on, float night, float time)
    {
        var drawWindows = on && night > 0.02f && windowGlow;
        if (drawWindows)
        {
            windowGlow.SetColor(ColorId, WindowsLit * night);
            windowDark.SetColor(ColorId, Color.Lerp(Color.white, WindowsDark, night));
        }

        var i = 0;
        foreach (var home in homes.Values)
        {
            ++i;
            if (drawWindows && home.Renderer && home.Glass >= 0)
            {
                if (!home.Mesh) home.Mesh = home.Renderer.GetComponent<MeshFilter>().sharedMesh;
                if (home.Mesh && home.Glass < home.Mesh.subMeshCount)
                {
                    var draw = home.Lived ? glowParams : darkParams;
                    Graphics.RenderMesh(draw, home.Mesh, home.Glass, home.Renderer.localToWorldMatrix);
                }
            }
            if (!home.Lamp) continue;
            home.Lamp.enabled = on && home.Lived && night > 0.02f && home.Renderer;
            // a lamp, not a fire: it hardly flickers
            if (home.Lamp.enabled) home.Lamp.intensity = 2.4f * night * (0.96f + 0.04f * Mathf.Sin(time * 2.3f + i * 1.9f));
        }
    }

    /// <summary>The two window materials, on the houses' glass texture: the glow added on, the dark multiplied over.</summary>
    private void MakeWindows(Material glass)
    {
        if (windowGlow || !glass) return;
        var shader = Resources.Load<Shader>("WindowGlow");
        if (!shader || !shader.isSupported) return;
        var texture = glass.HasProperty(BaseMapId) ? glass.GetTexture(BaseMapId) : null;
        windowGlow = new Material(shader) { name = "Window Glow" };
        windowGlow.SetTexture(BaseMapId, texture);
        windowGlow.SetFloat(ModeId, 0f);
        windowGlow.SetFloat(SrcBlendId, (float)BlendMode.One);
        windowGlow.SetFloat(DstBlendId, (float)BlendMode.One);
        windowDark = new Material(shader) { name = "Window Dark" };
        windowDark.SetFloat(ModeId, 1f);
        windowDark.SetFloat(SrcBlendId, (float)BlendMode.DstColor);
        windowDark.SetFloat(DstBlendId, (float)BlendMode.Zero);
        glowParams = new RenderParams(windowGlow) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
        darkParams = new RenderParams(windowDark) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
    }

    private void OnDestroy()
    {
        if (windowGlow) Destroy(windowGlow);
        if (windowDark) Destroy(windowDark);
    }

    private void Remove(ParticleSystem system)
    {
        systems.Remove(system);
        Destroy(system.gameObject);
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
        if (Time.unscaledTime >= nextHouseCheck)
        {
            nextHouseCheck = Time.unscaledTime + HouseCheckEvery;
            Houses();
        }
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

        // the fires glow and light the ground round them, faintly by day, strongly after dark (and some under a storm's
        // dark sky), flickering
        var night = Mathf.Max(Ambience.Darkness, (1f - Weather.Sunlight) * 0.6f);
        if (glowMaterial) glowMaterial.SetFloat(IntensityId, GlowAtNight * night);
        var time = Time.time;
        Lamps(on, night, time);
        for (var i = 0; i < fireLights.Count; ++i)
        {
            var (light, strength) = fireLights[i];
            if (!light) continue;
            light.enabled = on;
            // each fire flickers in its own time
            var flicker = 0.8f + 0.12f * Mathf.Sin(time * 9.1f + i * 1.7f) + 0.08f * Mathf.PerlinNoise(time * 4f, i * 0.37f);
            light.intensity = (0.4f + 3.2f * night) * flicker * strength;
        }
    }
}
