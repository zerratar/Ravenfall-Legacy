using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// What the <see cref="Weather"/> does that is not light and cloud: rain falling round the focus (<see cref="Ambience.FocusPoint"/>)
/// with ripples where it lands, and the lightning of a storm. Lightning flashes in the cloud far off as a storm comes
/// in, upwind, nearer and more often as it builds, then strikes the ground as a bolt with a flash over the land; the
/// lightning flashes setting turns the light of it off (<see cref="LightningFlashes"/>) for those who would rather not
/// see flashing light; the thunder still rolls. Thunder comes after its flash, later the farther off it struck, so a
/// storm can be heard coming (<see cref="WeatherSounds"/>), and rain hisses as it falls. Nothing falls in a dungeon, and on Iceheim the rain falls as snow (<see cref="Ambience"/>).
/// </summary>
/// <remarks>
/// The rain is one particle system of streaks in a box above the focus, up to about six thousand in a downpour, and
/// the ripples a second one, placed one by one on whatever is under them (the ground, a roof, the sea). A bolt is a
/// ribbon mesh made afresh for each strike, drawn after the clouds (<see cref="SkyClouds.Overlay"/>) so they do not
/// hide it.
/// </remarks>
public sealed class WeatherEffects : MonoBehaviour
{
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int LitId = Shader.PropertyToID("_Lit");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int SoftFadeId = Shader.PropertyToID("_SoftFade");
    private static readonly int NearFadeId = Shader.PropertyToID("_NearFade");

    private const float RainArea = 56f;       // metres across the box the rain falls in
    private const float RainHeight = 26f;     // how high above the focus it starts
    private const float FallSpeed = 22f;      // metres a second
    private const float DropsInDownpour = 5000f;
    private const float RipplesInDownpour = 900f;
    private const float RippleReach = 24f;    // metres round the focus
    private const float CloudBase = SkyClouds.Floor + 30f;

    /// <summary>The setting: lightning flashes on or off.</summary>
    public static bool LightningFlashes { get; set; } = true;

    /// <summary>The setting: how loud the rain and the thunder are, 0 to 1.</summary>
    public static float Volume { get; set; } = 0.6f;

    private const float SpeedOfSound = 343f;

    private ParticleSystem rain, ripples;
    private Mesh boltMesh;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<Color> colours = new List<Color>();
    private readonly List<int> triangles = new List<int>();
    private Light strikeLight;
    private Material rainMaterial, rippleMaterial, boltMaterial;
    private Ocean ocean;
    private AudioSource rainSound;
    // recorded rain crossfades between two sources when it changes from light to moderate to strong
    private AudioSource rainSoundOut;
    private SoundKind? rainTier;
    private readonly AudioSource[] thunderSounds = new AudioSource[3];
    private AudioClip[] nearThunder, farThunder;
    private readonly List<(float at, bool near, float volume, float pan)> thunderToCome = new List<(float, bool, float, float)>();
    private float rippleDebt;

    // the lightning now: when it began, how bright, where; and when the next comes
    private float flashStart = -100f, flashPeak, nextLightning = -1f;
    private Vector3 flashAt;
    private bool flashBolt;
    private readonly List<Vector3> path = new List<Vector3>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("Weather Effects") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<WeatherEffects>();
    }

    private void Start()
    {
        var shader = Resources.Load<Shader>("AmbienceParticle");
        if (!shader || !shader.isSupported)
        {
            enabled = false;
            return;
        }
        rainMaterial = MakeMaterial(shader, 7, false, true, 1.15f);
        rainMaterial.SetFloat(NearFadeId, 6f);
        rippleMaterial = MakeMaterial(shader, 9, false, true, 1f);
        rippleMaterial.SetFloat(SoftFadeId, 0.05f);
        boltMaterial = MakeMaterial(shader, 8, true, false, 3f);
        // the bolt draws over the clouds, so it hides itself behind whatever is nearer, by the scene's depth
        boltMaterial.SetFloat(SoftFadeId, 2f);
        ocean = FindAnyObjectByType<Ocean>();

        rain = MakeRain();
        ripples = MakeRipples();
        rainSound = MakeSound("Rain Sound");
        rainSound.loop = true;
        rainSoundOut = MakeSound("Rain Sound");
        rainSoundOut.loop = true;
        for (var i = 0; i < thunderSounds.Length; ++i)
        {
            thunderSounds[i] = MakeSound("Thunder Sound");
            // far thunder is recorded thunder with its crack taken off
            thunderSounds[i].gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 22000f;
        }
        strikeLight = new GameObject("Lightning Light").AddComponent<Light>();
        strikeLight.transform.SetParent(transform, false);
        strikeLight.type = LightType.Point;
        strikeLight.color = new Color(0.8f, 0.86f, 1f);
        strikeLight.shadows = LightShadows.None;
        strikeLight.enabled = false;
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

    private ParticleSystem MakeSystem(string name, Material material, ParticleSystemRenderMode mode)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = mode;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = true;
        main.startSpeed = 0f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        return system;
    }

    /// <summary>Streaks, stretched along their fall, slanting with the wind.</summary>
    private ParticleSystem MakeRain()
    {
        var system = MakeSystem("Rain", rainMaterial, ParticleSystemRenderMode.Stretch);
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 1f;
        var main = system.main;
        main.maxParticles = 7000;
        main.startLifetime = (RainHeight + 4f) / FallSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.04f);
        main.startColor = new Color(0.78f, 0.82f, 0.9f, 0.32f);
        var emission = system.emission;
        emission.rateOverTime = 0f;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(RainArea, 0.1f, RainArea);
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // all three in the same mode (two constants), as Unity wants; the wind sets x and z as it blows
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-FallSpeed * 1.05f, -FallSpeed * 0.95f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        system.Play();
        return system;
    }

    /// <summary>Rings spreading where drops land, lying flat; placed one by one, as they need the ground under them.</summary>
    private ParticleSystem MakeRipples()
    {
        var system = MakeSystem("Ripples", rippleMaterial, ParticleSystemRenderMode.HorizontalBillboard);
        var main = system.main;
        main.maxParticles = 1200;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startColor = new Color(0.82f, 0.86f, 0.92f, 0.5f);
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.15f, 1f, 1f));
        var colour = system.colorOverLifetime;
        colour.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
        colour.color = fade;
        system.Play();
        return system;
    }

    private void Update()
    {
        var camera = Camera.main;
        var outside = camera && camera.transform.position.y > -30f && Ambience.HasFocus;
        var focus = Ambience.FocusPoint;
        var wet = outside && Weather.Changing && Ambience.Island != "Heim" ? Weather.Rain : 0f;

        // the rain starts upwind, so it lands round the focus
        var windSpeed = 2f + 5f * Weather.Storm;
        var wind = SkyClouds.Wind.normalized * windSpeed;
        var fall = RainHeight / FallSpeed;
        var emission = rain.emission;
        emission.rateOverTime = DropsInDownpour * wet;
        if (wet > 0f)
        {
            rain.transform.position = focus + new Vector3(-wind.x * fall, RainHeight, -wind.y * fall);
            var velocity = rain.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(wind.x * 0.85f, wind.x * 1.15f);
            velocity.z = new ParticleSystem.MinMaxCurve(wind.y * 0.85f, wind.y * 1.15f);
            Ripple(focus, wet);
        }

        Lightning(outside, focus);
        Sounds(outside, wet);
    }

    private void Ripple(Vector3 focus, float wet)
    {
        rippleDebt += RipplesInDownpour * wet * Time.deltaTime;
        var count = Mathf.Min((int)rippleDebt, 60);
        rippleDebt -= count;
        var sea = ocean ? ocean.transform.position.y : -1000f;
        var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
        for (var i = 0; i < count; ++i)
        {
            var offset = Random.insideUnitCircle * RippleReach;
            var origin = new Vector3(focus.x + offset.x, focus.y + 40f, focus.z + offset.y);
            float y;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 90f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) y = Mathf.Max(hit.point.y, sea);
            else if (sea > -1000f) y = sea;
            else continue;
            emit.position = new Vector3(origin.x, y + 0.04f, origin.z);
            ripples.Emit(emit, 1);
        }
    }


    // ---- sound -------------------------------------------------------------------------------------

    private AudioSource MakeSound(string name)
    {
        var source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    private void Sounds(bool outside, float wet)
    {
        // the rain, rising and falling with it: recorded (light, moderate or strong) when the sounds are there,
        // otherwise the hiss made the first time it rains
        if (SoundLibrary.Instance && SoundLibrary.Instance.Get(SoundKind.RainLight).Count > 0) RecordedRain(wet);
        else
        {
            var rainVolume = Volume * wet * 0.45f;
            if (rainVolume > 0.001f && !rainSound.clip) rainSound.clip = WeatherSounds.Rain(7);
            rainSound.volume = Mathf.MoveTowards(rainSound.volume, rainVolume, Time.deltaTime * 0.25f);
            if (rainSound.volume > 0.001f && !rainSound.isPlaying && rainSound.clip) rainSound.Play();
            else if (rainSound.volume <= 0.001f && rainSound.isPlaying) rainSound.Stop();
        }

        // thunder, once its sound has come; in a dungeon none is heard
        if (!outside) thunderToCome.Clear();
        for (var i = thunderToCome.Count - 1; i >= 0; --i)
        {
            var (at, near, volume, pan) = thunderToCome[i];
            if (Time.time < at) continue;
            thunderToCome.RemoveAt(i);
            if (Volume <= 0.001f) continue;
            var recorded = SoundLibrary.Instance ? SoundLibrary.Instance.Get(SoundKind.Thunder) : null;
            if (recorded != null && recorded.Count > 0)
            {
                if (nearThunder == null || nearThunder.Length != recorded.Count)
                {
                    nearThunder = new AudioClip[recorded.Count];
                    for (var c = 0; c < recorded.Count; ++c) nearThunder[c] = recorded[c];
                    farThunder = nearThunder;
                }
            }
            else if (nearThunder == null)
            {
                nearThunder = new[] { WeatherSounds.Thunder(true, 11), WeatherSounds.Thunder(true, 12), WeatherSounds.Thunder(true, 13) };
                farThunder = new[] { WeatherSounds.Thunder(false, 21), WeatherSounds.Thunder(false, 22), WeatherSounds.Thunder(false, 23) };
            }
            // the quietest of the sources playing gives way, so a new crack is never lost under an old roll
            var source = thunderSounds[0];
            foreach (var s in thunderSounds)
            {
                if (!s.isPlaying) { source = s; break; }
                if (s.volume < source.volume) source = s;
            }
            var clips = near ? nearThunder : farThunder;
            source.clip = clips[Random.Range(0, clips.Length)];
            source.volume = Volume * volume;
            source.panStereo = pan;
            source.pitch = near ? Random.Range(0.9f, 1.08f) : Random.Range(0.78f, 0.92f);
            var muffle = source.GetComponent<AudioLowPassFilter>();
            if (muffle) muffle.cutoffFrequency = near ? 22000f : 900f;
            source.Play();
        }
    }

    /// <summary>
    /// Recorded rain: the light, moderate or strong loop for how hard it rains, a new loop crossfading in when it
    /// changes, each started somewhere in so two showers never sound alike.
    /// </summary>
    private void RecordedRain(float wet)
    {
        var rainVolume = Volume * wet * 0.6f;
        var tier = wet < 0.4f ? SoundKind.RainLight : wet < 0.75f ? SoundKind.RainModerate : SoundKind.RainStrong;
        if (SoundLibrary.Instance.Get(tier).Count == 0) tier = SoundKind.RainLight;
        if (rainVolume > 0.001f && tier != rainTier)
        {
            var clip = SoundLibrary.Pick(tier);
            if (clip)
            {
                (rainSound, rainSoundOut) = (rainSoundOut, rainSound);
                rainSound.clip = clip;
                rainSound.volume = 0f;
                rainSound.time = Random.Range(0f, clip.length * 0.8f);
                rainSound.Play();
                rainTier = tier;
            }
        }

        var fade = Time.deltaTime * 0.25f;
        rainSound.volume = Mathf.MoveTowards(rainSound.volume, rainVolume, fade);
        rainSoundOut.volume = Mathf.MoveTowards(rainSoundOut.volume, 0f, fade);
        if (rainSoundOut.isPlaying && rainSoundOut.volume <= 0.001f) rainSoundOut.Stop();
        if (rainSound.isPlaying && rainSound.volume <= 0.001f && rainVolume <= 0.001f)
        {
            rainSound.Stop();
            rainTier = null;
        }
    }

    // ---- lightning ----------------------------------------------------------------------------------

    private void Lightning(bool outside, Vector3 focus)
    {
        var storm = Weather.Changing && outside ? Weather.Storm : 0f;
        var time = Time.time;
        if (storm < 0.05f) nextLightning = -1f;
        else
        {
            // far off and now and then as the storm comes in, near and often in the thick of it
            if (nextLightning < 0f) nextLightning = time + Random.Range(4f, 12f);
            if (time >= nextLightning)
            {
                Strike(focus, storm);
                var mean = Mathf.Lerp(45f, 7f, storm);
                nextLightning = time + Mathf.Max(2.5f, -Mathf.Log(Mathf.Max(Random.value, 0.01f)) * mean);
            }
        }

        // the flash: a stroke, a dim moment, a second stroke, then it dies away
        var since = time - flashStart;
        var level = since < 0.07f ? 1f : since < 0.12f ? 0.2f : since < 0.2f ? 0.8f : Mathf.Max(0f, 0.8f * (1f - (since - 0.2f) / 0.3f));
        if (since > 0.5f || storm <= 0f || !LightningFlashes) level = 0f;
        Weather.Flash = flashPeak * level;
        SkyClouds.Flash = level > 0f ? new Vector4(flashAt.x, CloudBase + 60f, flashAt.z, (flashBolt ? 0.45f : 1.1f) * level) : Vector4.zero;
        strikeLight.enabled = flashBolt && level > 0f && strikeLight.range > 0f;
        if (strikeLight.enabled) strikeLight.intensity = 40f * level;

        var showBolt = flashBolt && level > 0.1f;
        if (showBolt) boltMaterial.SetFloat(IntensityId, 3f * level);
        var shown = SkyClouds.Overlay.Contains((boltMesh, boltMaterial));
        if (showBolt && !shown) SkyClouds.Overlay.Add((boltMesh, boltMaterial));
        else if (!showBolt && shown) SkyClouds.Overlay.Remove((boltMesh, boltMaterial));
    }

    private void Strike(Vector3 focus, float storm)
    {
        // where: upwind, as the storm comes with the wind, nearer the stormier
        var near = storm * storm;
        var distance = Mathf.Lerp(4500f, 160f, near) * Random.Range(0.6f, 1.4f);
        var upwind = -SkyClouds.Wind.normalized;
        var side = new Vector2(-upwind.y, upwind.x);
        var across = Random.Range(-0.8f, 0.8f);
        var dir = (upwind * (1f - Mathf.Abs(across)) + side * across).normalized;
        flashAt = new Vector3(focus.x + dir.x * distance, 0f, focus.z + dir.y * distance);

        flashStart = Time.time;
        flashBolt = storm > 0.5f && distance < 2600f && Random.value < storm * 0.85f;
        // the land lights up most for a strike close by; far off, the flash is mostly in the cloud
        flashPeak = (flashBolt ? 0.6f : 0.3f) / (1f + (distance / 700f) * (distance / 700f));
        if (flashBolt) Bolt(distance);

        // its thunder, when the sound gets here: near, a crack and a roll; far, a rumble
        var camera = Camera.main;
        var pan = 0f;
        if (camera)
        {
            var toward = new Vector3(flashAt.x - focus.x, 0f, flashAt.z - focus.z).normalized;
            pan = Vector3.Dot(camera.transform.right, toward) * 0.5f;
        }
        var loudness = Mathf.Clamp(1.2f / (1f + distance / 900f), 0.12f, 1f);
        thunderToCome.Add((Time.time + Mathf.Min(distance / SpeedOfSound, 14f), distance < 900f, loudness, pan));
    }

    /// <summary>A jagged bolt from the cloud's base down to whatever it strikes, with a branch or two off it.</summary>
    private void Bolt(float distance)
    {
        var ground = flashAt.y;
        var origin = new Vector3(flashAt.x, CloudBase + 200f, flashAt.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, 600f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) ground = hit.point.y;
        else if (ocean) ground = ocean.transform.position.y;
        var bottom = new Vector3(flashAt.x, ground, flashAt.z);
        var top = bottom + new Vector3(Random.Range(-60f, 60f), SkyClouds.Floor + 50f - ground, Random.Range(-60f, 60f));

        // a ribbon facing the camera, drawn after the clouds (SkyClouds.Overlay) so they do not hide it; a bolt far
        // off is drawn wider, so it does not thin to nothing on the screen
        var eye = Camera.main ? Camera.main.transform.position : bottom;
        var width = Mathf.Max(1.4f, distance * 0.0025f);
        vertices.Clear();
        uvs.Clear();
        triangles.Clear();
        Jag(top, bottom, 6, 0.24f);
        Ribbon(eye, width);
        var main = new List<Vector3>(path);
        var branches = Random.Range(1, 4);
        for (var b = 0; b < branches; ++b)
        {
            var from = main[Random.Range(main.Count / 4, main.Count * 2 / 3)];
            var drop = (from.y - ground) * Random.Range(0.25f, 0.5f);
            var to = from + new Vector3(Random.Range(-1f, 1f) * drop * 0.45f, -drop, Random.Range(-1f, 1f) * drop * 0.45f);
            Jag(from, to, 4, 0.28f);
            Ribbon(eye, width * 0.5f);
        }
        if (!boltMesh)
        {
            boltMesh = new Mesh { name = "Lightning" };
            boltMesh.MarkDynamic();
        }
        boltMesh.Clear();
        boltMesh.SetVertices(vertices);
        boltMesh.SetUVs(0, uvs);
        boltMesh.SetColors(Colours());
        boltMesh.SetTriangles(triangles, 0);
        boltMesh.bounds = new Bounds((top + bottom) * 0.5f, new Vector3(800f, CloudBase + 400f, 800f));

        strikeLight.transform.position = bottom + Vector3.up * 25f;
        strikeLight.range = distance < 500f ? 260f : 0f;
    }

    /// <summary>
    /// The bolt's colour on each of its vertices, fading out as it goes up into the cloud, as the cloud hides it there;
    /// how bright it is goes in the material.
    /// </summary>
    private List<Color> Colours()
    {
        colours.Clear();
        foreach (var v in vertices) colours.Add(new Color(0.85f, 0.9f, 1f, Mathf.Clamp01((SkyClouds.Floor + 50f - v.y) / 70f)));
        return colours;
    }

    /// <summary>The path from a to b broken into 2^levels pieces, each midpoint pushed aside by a share of its length.</summary>
    private void Jag(Vector3 a, Vector3 b, int levels, float rough)
    {
        path.Clear();
        path.Add(a);
        path.Add(b);
        for (var level = 0; level < levels; ++level)
        {
            for (var i = path.Count - 1; i > 0; --i)
            {
                var p = path[i - 1];
                var q = path[i];
                var length = Vector3.Distance(p, q);
                var mid = (p + q) * 0.5f + new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)) * (length * rough);
                path.Insert(i, mid);
            }
        }
    }

    /// <summary>The path as a strip of quads turned to face the eye, v across it (the stroke shape's).</summary>
    private void Ribbon(Vector3 eye, float width)
    {
        var start = vertices.Count;
        for (var i = 0; i < path.Count; ++i)
        {
            var along = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
            var side = Vector3.Cross(along, path[i] - eye).normalized * (width * 0.5f * Mathf.Lerp(1f, 0.6f, i / (float)(path.Count - 1)));
            vertices.Add(path[i] - side);
            vertices.Add(path[i] + side);
            var u = i / (float)(path.Count - 1);
            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, 1f));
            if (i == 0) continue;
            var k = start + i * 2;
            triangles.Add(k - 2); triangles.Add(k - 1); triangles.Add(k);
            triangles.Add(k - 1); triangles.Add(k + 1); triangles.Add(k);
        }
    }
}
