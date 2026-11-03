using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Ravebridge rising from the sea (the user, 2026-11-03): the first time a stream opens Ravebridge (RavebridgeManager,
/// when it asks who plays), a cutscene. The island's contents start deep under the sea floor and rise on an eased
/// curve while the camera watches from a distance and swings round, shaking as hard as the island moves; mist and
/// spray burst up all round its coast, dust rolls off the land once it breaks the surface, and a ring of mist rolls out
/// as it settles. Then its name, and the camera stays on the island. Esc or Space skips to the end.
/// The UI and the walking characters (animals, enemies, the NPCs, who stand on the navmesh and would not rise with the
/// ground) are put away while it plays; nobody joins until it is over (RavebridgeManager.Revealing). The grass is drawn
/// where it will be and does not rise, but from this far it is not drawn at all.
/// </summary>
public sealed class RavebridgeReveal : MonoBehaviour
{
    private const float Depth = 45f;                         // under the sea floor, the tallest tower too
    private const float Calm = 1.5f, Rise = 11f, Settle = 2f, Hold = 3.5f;
    private const float Total = Calm + Rise + Settle + Hold;
    private const float TitleIn = 1f, TitleOut = 0.8f;

    public static bool Playing => current;
    private static RavebridgeReveal current;

    private readonly List<(Transform Part, Vector3 At)> parts = new List<(Transform, Vector3)>();
    private readonly List<GameObject> walkers = new List<GameObject>();
    private readonly List<(VisualElement Element, StyleEnum<DisplayStyle> Display)> hiddenUi = new List<(VisualElement, StyleEnum<DisplayStyle>)>();
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private Action done;
    private IslandController island;
    private Rect land;          // x and z
    private Vector3 gate;       // the castle gate, where the eye ends up (taken before the island sinks)
    private float sea;
    private float time;
    private bool burstDone;
    private bool finished;
    private Transform cam;
    private GameObject effects;
    private ParticleSystem mist, spray, dust, ring;
    private Material soft;
    private AudioSource rumble, deepRumble, cascade, waves, oneShots, boom;
    private float nextDebris;
    private MusicManager music;
    private float musicVolume = -1f;
    private bool boomDone;
    private static Texture2D puff;
    private VisualElement title;

    /// <summary>Starts the cutscene; false when the camera is not free for it (a raid, a dungeon, a war).</summary>
    public static bool Play(MonoBehaviour host, IslandController island, Action done)
    {
        if (current || !island || !host) return false;
        var game = GameManager.Instance;
        if (!game || !game.Camera || !game.Camera.StartCinematic()) return false;
        current = host.gameObject.AddComponent<RavebridgeReveal>();
        current.Begin(island, done);
        return true;
    }

    /// <summary>Straight to its name and the end (Esc, or the event ended meanwhile).</summary>
    public static void Skip()
    {
        if (current) current.SkipNow();
    }

    private void Begin(IslandController island, Action done)
    {
        this.island = island;
        this.done = done;
        sea = SeaChart.Sea;
        cam = GameManager.Instance.Camera.transform;
        land = LandOf(island);
        gate = island.SpawnPosition;

        foreach (Transform child in island.transform)
        {
            // the world's terrain tiles parented here reach past Ravebridge
            if (child.GetComponent<Terrain>()) continue;
            parts.Add((child, child.position));
        }
        var seen = new HashSet<GameObject>();
        foreach (var (part, _) in parts)
        {
            foreach (var agent in part.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(false))
                if (seen.Add(agent.gameObject)) walkers.Add(agent.gameObject);
        }
        foreach (var w in walkers) w.SetActive(false);
        SetOffset(-Depth);

        MakeEffects();
        MakeSound();
        HideUi();
        Place(0f);
    }

    private void LateUpdate()
    {
        if (finished) return;
        time += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) && !Keybinds.IsTyping)
        {
            EscapeKey.Claim();
            SkipNow();
        }

        var x = Mathf.Clamp01((time - Calm) / Rise);
        var rising = x > 0f && x < 1f;
        SetOffset(-Depth * (1f - Smoother(x)));
        // how fast it moves, 0 to 1 (the curve's slope over its steepest)
        var speed = rising ? 30f * x * x * (1f - x) * (1f - x) / 1.875f : 0f;
        var settle = Mathf.Clamp01((time - Calm - Rise) / Settle);
        var settling = time > Calm + Rise && settle < 1f;

        var offset = -Depth * (1f - Smoother(x));
        var above = Mathf.Clamp01((offset + 30f) / 20f); // the land near or over the surface
        Rate(mist, 420f * speed + (settling ? 80f * (1f - settle) : 0f));
        Rate(spray, 650f * speed);
        Rate(dust, 260f * Mathf.Max(speed * above, settling ? 0.5f * (1f - settle) : 0f));
        if (dust) dust.transform.position = new Vector3(land.center.x, sea + offset + 8f, land.center.y);
        if (!burstDone && time >= Calm + Rise - 0.3f)
        {
            burstDone = true;
            if (ring) ring.Emit(500);
            SplashSound();
        }

        var shake = 0.12f + 1.7f * speed + (settling ? 0.7f * (1f - settle) : 0f);
        Place(shake);
        UpdateSound(speed, above, settle, settling);
        UpdateTitle();
        if (time >= Total) Finish();
    }

    /// <summary>The camera: far off to the south east and low over the water, swinging round and rising as the island does.</summary>
    private void Place(float shake)
    {
        if (!cam) return;
        var half = Mathf.Max(land.width, land.height) * 0.5f;
        var u = Smoother(Mathf.Clamp01(time / (Calm + Rise + Settle)));
        // from the south: the castle on the left of the river, the desert town on the right
        var yaw = Mathf.Lerp(208f, 174f, u) * Mathf.Deg2Rad;
        var distance = half * Mathf.Lerp(2.2f, 1.45f, u);
        var height = Mathf.Lerp(14f, 80f, u);
        var centre = new Vector3(land.center.x, sea, land.center.y);
        var position = centre + new Vector3(Mathf.Sin(yaw) * distance, height, Mathf.Cos(yaw) * distance);
        // the eye drifts to the castle gate as the island comes up
        var target = Vector3.Lerp(centre, new Vector3(gate.x, sea, gate.z), 0.6f * u) + Vector3.up * Mathf.Lerp(-6f, 8f, u);

        var t = Time.unscaledTime * 9f;
        position += new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, 5.1f) - 0.5f) * (2f * shake);
        var roll = (Mathf.PerlinNoise(t * 0.7f, 9.3f) - 0.5f) * shake * 1.2f;
        cam.SetPositionAndRotation(position, Quaternion.LookRotation(target - position) * Quaternion.Euler(0f, 0f, roll));
    }

    private void SetOffset(float y)
    {
        foreach (var (part, at) in parts)
            if (part) part.position = at + Vector3.up * y;
    }

    private void SkipNow()
    {
        if (finished) return;
        // its name for a moment, then the end
        time = Mathf.Max(time, Total - TitleIn - 1.2f);
        burstDone = true;
        if (mist) mist.Clear();
        if (spray) spray.Clear();
        if (dust) dust.Clear();
        if (ring) ring.Clear();
        boomDone = true;
        nextDebris = float.MaxValue;
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        SetOffset(0f);
        foreach (var w in walkers) if (w) w.SetActive(true);
        walkers.Clear();
        ShowUi();
        RestoreMusic();
        if (effects) Destroy(effects);
        if (soft) Destroy(soft);
        var game = GameManager.Instance;
        if (game && game.Camera) game.Camera.StopCinematic(island);
        if (current == this) current = null;
        var callback = done;
        done = null;
        Destroy(this);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        // play mode stopped or the game shut down in the middle: everything back where it belongs
        if (finished) return;
        finished = true;
        SetOffset(0f);
        foreach (var w in walkers) if (w) w.SetActive(true);
        ShowUi();
        RestoreMusic();
        if (effects) Destroy(effects);
        if (current == this) current = null;
    }

    private void RestoreMusic()
    {
        if (music && musicVolume >= 0f) music.volume = musicVolume;
        musicVolume = -1f;
    }

    private static float Smoother(float x) => x * x * x * (x * (x * 6f - 15f) + 10f);

    private static void Rate(ParticleSystem system, float rate)
    {
        if (!system) return;
        var emission = system.emission;
        emission.rateOverTime = rate;
    }

    /// <summary>A soft, cloudy puff: a round falloff broken up by noise.</summary>
    private static Texture2D Puff()
    {
        if (puff) return puff;
        const int Size = 128;
        puff = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Reveal Puff", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var dx = (x + 0.5f) / Size * 2f - 1f;
                var dy = (y + 0.5f) / Size * 2f - 1f;
                var falloff = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 1.5f);
                var noise = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.07f + 7.7f);
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(falloff * noise * 1.4f)));
            }
        }
        puff.SetPixels32(pixels);
        puff.Apply();
        return puff;
    }

    /// <summary>The island's land, found with rays down over its sphere before it sinks: the hits above the sea.</summary>
    private static Rect LandOf(IslandController island)
    {
        var centre = island.Centre;
        var radius = Mathf.Max(30f, island.Radius);
        const int Samples = 48;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        for (var i = 0; i < Samples; i++)
        {
            for (var j = 0; j < Samples; j++)
            {
                var x = centre.x - radius + (i + 0.5f) * 2f * radius / Samples;
                var z = centre.z - radius + (j + 0.5f) * 2f * radius / Samples;
                if (!Physics.Raycast(new Vector3(x, centre.y + 400f, z), Vector3.down, out var hit, 800f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y < SeaChart.Sea + 0.3f) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
            }
        }
        return minX > maxX ? new Rect(centre.x - radius * 0.6f, centre.z - radius * 0.6f, radius * 1.2f, radius * 1.2f) : Rect.MinMaxRect(minX, minZ, maxX, maxZ);
    }

    // ---- The mist, the spray and the dust ---------------------------------------------------------------

    private void MakeEffects()
    {
        var props = RavebridgeRevealProps.Get();
        effects = new GameObject("Ravebridge Reveal");
        // the soft circle is a low-poly shape: fine small, a stack of hexagons as clouds this big
        if (props && props.Mist)
        {
            soft = new Material(props.Mist) { name = "Reveal Mist" };
            soft.SetTexture("_Albedo_Map", Puff());
        }
        var coast = new Vector3(land.center.x, sea + 0.5f, land.center.y);
        // the particles are unlit: by night they would glow, so they take the moon's dim blue instead
        var cycle = FindAnyObjectByType<DayNightCycle>();
        var night = cycle && cycle.IsEnabled ? 1f - cycle.Daylight : 0f;
        Color Lit(Color c) => new Color(c.r * Mathf.Lerp(1f, 0.45f, night), c.g * Mathf.Lerp(1f, 0.5f, night), c.b * Mathf.Lerp(1f, 0.65f, night), c.a);
        // the coast: the edges of the land's box, pointing up
        var edge = new Vector3(land.width * 0.95f, land.height * 0.95f, 0.1f);

        // low along the coast, not a wall in front of the island
        mist = MakeSystem("Mist", soft, 3000, coast);
        Shape(mist, ParticleSystemShapeType.BoxEdge, edge);
        var main = mist.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(10f, 22f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        // the soft circle's material doubles its colour: half of white is white
        main.startColor = Lit(new Color(0.5f, 0.51f, 0.53f, 0.4f));
        main.gravityModifier = 0.05f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        Fade(mist, 0.15f);
        Grow(mist, 0.6f, 1.6f);

        spray = MakeSystem("Spray", props ? props.Spray : null, 4000, coast);
        Shape(spray, ParticleSystemShapeType.BoxEdge, edge);
        main = spray.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 32f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
        main.startColor = Lit(new Color(0.85f, 0.95f, 1f, 0.9f));
        main.gravityModifier = 2.2f;
        var shape = spray.shape;
        shape.randomDirectionAmount = 0.25f;
        Fade(spray, 0.05f);

        dust = MakeSystem("Dust", soft ? soft : props ? props.Dust : null, 2000, coast);
        Shape(dust, ParticleSystemShapeType.Box, new Vector3(land.width * 0.85f, land.height * 0.85f, 6f));
        main = dust.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(20f, 45f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startColor = Lit(new Color(0.33f, 0.28f, 0.21f, 0.4f));
        main.gravityModifier = -0.02f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        Fade(dust, 0.2f);
        Grow(dust, 0.7f, 1.8f);

        // as it settles: a ring of mist rolling out over the sea
        ring = MakeSystem("Ring", soft, 1000, coast);
        Shape(ring, ParticleSystemShapeType.BoxEdge, edge);
        main = ring.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(18f, 34f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startColor = Lit(new Color(0.5f, 0.51f, 0.53f, 0.5f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var velocity = ring.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.radial = new ParticleSystem.MinMaxCurve(18f, 30f);
        // every axis in the same mode as the radial (two constants), or the module refuses them
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, 3f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        Fade(ring, 0.1f);
        Grow(ring, 0.8f, 2f);
    }

    private ParticleSystem MakeSystem(string name, Material material, int most, Vector3 at)
    {
        var go = new GameObject(name);
        go.transform.SetParent(effects.transform, false);
        go.transform.position = at;
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 5f;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = most;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (material) renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 4f;
        system.Play();
        return system;
    }

    /// <summary>A box shape turned so it points up (a box emits along its own z).</summary>
    private static void Shape(ParticleSystem system, ParticleSystemShapeType type, Vector3 scale)
    {
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = type;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.scale = scale;
    }

    private static void Fade(ParticleSystem system, float fadeIn)
    {
        var colour = system.colorOverLifetime;
        colour.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colour.color = gradient;
    }

    private static void Grow(ParticleSystem system, float from, float to)
    {
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    // ---- The sound -----------------------------------------------------------------------------------

    // the clips' own loudness differs a lot (the debris is quiet, the thunder loud): each is brought near the same
    private const float RumbleGain = 1f, DeepGain = 0.75f, CascadeGain = 1.1f, WavesGain = 1.6f, BoomGain = 0.8f, SplashGain = 1.2f, DebrisGain = 1f;

    private void MakeSound()
    {
        // the music steps back for the rumble and comes back under the name
        music = FindAnyObjectByType<MusicManager>();
        if (music) musicVolume = music.volume;
        var props = RavebridgeRevealProps.Get();
        if (!props) return;
        rumble = Source(props.Rumble, 1f);
        deepRumble = Source(props.DeepRumble, 0.7f);
        cascade = Source(props.Cascade, 1f);
        waves = Source(props.Waves, 1f);
        oneShots = Source(null, 1f);
        // the thunder pitched down: a deep boom, not weather
        boom = Source(null, 0.6f);
        nextDebris = Calm + 2f;
    }

    /// <summary>Heard as a cutscene is, the same everywhere (not from a place); the game's sound volume applies.</summary>
    private AudioSource Source(AudioClip clip, float pitch)
    {
        var source = effects.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.pitch = pitch;
        source.volume = 0f;
        if (clip)
        {
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
        return source;
    }

    private void UpdateSound(float speed, float above, float settle, bool settling)
    {
        if (music && musicVolume >= 0f)
        {
            var duck = Mathf.Min(Mathf.Clamp01(time), Mathf.Clamp01((Total - time) / 1.5f));
            music.volume = musicVolume * Mathf.Lerp(1f, 0.3f, duck);
        }
        var props = RavebridgeRevealProps.Get();
        if (!props || !effects) return;
        var volume = GameSounds.Volume;
        var start = Mathf.Clamp01(time / Calm);                                        // the rumble comes up from nothing
        var after = Mathf.Clamp01(1f - (time - Calm - Rise - Settle) / (Hold * 0.6f));  // and dies away under the name
        var skipped = time >= Total - TitleIn - 1.2f && speed <= 0f && !settling ? 0f : 1f;
        var quake = Mathf.Max(speed, settling ? 0.5f * (1f - settle) : 0f);
        if (rumble) rumble.volume = volume * RumbleGain * start * after * skipped * (0.35f + 0.65f * quake);
        if (deepRumble) deepRumble.volume = volume * DeepGain * skipped * quake;
        if (cascade) cascade.volume = volume * CascadeGain * skipped * Mathf.Max(speed * above, settling ? 0.6f * (1f - settle) : 0f);
        if (waves) waves.volume = volume * WavesGain * 0.35f * Mathf.Clamp01(Total - time);

        if (!boomDone && time >= Calm)
        {
            boomDone = true;
            if (props.Boom && boom) boom.PlayOneShot(props.Boom, volume * BoomGain);
        }
        // rocks cracking and sliding while it moves fast
        if (time >= nextDebris && speed > 0.25f && props.Debris != null && props.Debris.Length > 0)
        {
            nextDebris = time + UnityEngine.Random.Range(0.7f, 1.6f);
            var clip = props.Debris[UnityEngine.Random.Range(0, props.Debris.Length)];
            if (clip) oneShots.PlayOneShot(clip, volume * DebrisGain * UnityEngine.Random.Range(0.6f, 1f));
        }
    }

    private void SplashSound()
    {
        var props = RavebridgeRevealProps.Get();
        if (props && props.Splash && oneShots) oneShots.PlayOneShot(props.Splash, GameSounds.Volume * SplashGain);
    }

    // ---- The screen ----------------------------------------------------------------------------------

    /// <summary>Everything on screen steps aside (UI Toolkit panels and the old canvases); its name shows at the end.</summary>
    private void HideUi()
    {
        UIDocument host = null;
        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;
            if (!host && root.Q<VisualElement>("conn-layer") != null) host = doc;
            foreach (var child in root.Children())
            {
                hiddenUi.Add((child, child.style.display));
                child.style.display = DisplayStyle.None;
            }
        }
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.enabled || !canvas.isRootCanvas) continue;
            canvas.enabled = false;
            hiddenCanvases.Add(canvas);
        }

        if (!host) return;
        title = new VisualElement { pickingMode = PickingMode.Ignore };
        title.style.position = Position.Absolute;
        title.style.left = 0; title.style.right = 0; title.style.top = 0; title.style.bottom = 0;
        title.style.alignItems = Align.Center;
        title.style.justifyContent = Justify.Center;
        title.style.opacity = 0f;
        var shadow = new TextShadow { offset = new Vector2(0f, 4f), blurRadius = 14f, color = new Color(0f, 0f, 0f, 0.85f) };
        var name = new Label(Localization.UI_RAVEBRIDGE_TITLE) { pickingMode = PickingMode.Ignore };
        name.AddToClassList("rf-font-eb");
        name.style.fontSize = 120;
        name.style.color = new Color(1f, 0.84f, 0.36f);
        name.style.textShadow = shadow;
        name.style.unityTextOutlineWidth = 1.5f;
        name.style.unityTextOutlineColor = new Color(0.25f, 0.15f, 0.02f);
        var line = new Label(Localization.UI_RAVEBRIDGE_RISEN) { pickingMode = PickingMode.Ignore };
        line.AddToClassList("rf-font-b");
        line.style.fontSize = 38;
        line.style.marginTop = -6;
        line.style.color = new Color(0.97f, 0.94f, 0.86f);
        line.style.textShadow = shadow;
        title.Add(name);
        title.Add(line);
        host.rootVisualElement.Add(title);

        // the only thing on screen besides the island: a Skip button in the corner (the user, 2026-11-03), Esc too
        var gold = new Color(0.84f, 0.67f, 0.16f);
        var skip = new Button(SkipNow) { name = "reveal-skip" };
        skip.style.position = Position.Absolute;
        skip.style.right = 36; skip.style.bottom = 32;
        skip.style.flexDirection = FlexDirection.Row;
        skip.style.alignItems = Align.Center;
        skip.style.paddingLeft = 18; skip.style.paddingRight = 10; skip.style.paddingTop = 8; skip.style.paddingBottom = 8;
        skip.style.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        skip.style.borderLeftWidth = skip.style.borderRightWidth = skip.style.borderTopWidth = skip.style.borderBottomWidth = 2;
        skip.style.borderLeftColor = skip.style.borderRightColor = skip.style.borderTopColor = skip.style.borderBottomColor = gold;
        skip.style.borderTopLeftRadius = skip.style.borderTopRightRadius = skip.style.borderBottomLeftRadius = skip.style.borderBottomRightRadius = 10;
        var word = new Label(Localization.UI_CUTSCENE_SKIP) { pickingMode = PickingMode.Ignore };
        word.AddToClassList("rf-font-eb");
        word.style.fontSize = 20;
        word.style.color = new Color(0.97f, 0.94f, 0.86f);
        word.style.marginRight = 12;
        var key = new Label("Esc") { pickingMode = PickingMode.Ignore };
        key.AddToClassList("rf-font-b");
        key.style.fontSize = 15;
        key.style.color = new Color(0.97f, 0.94f, 0.86f);
        key.style.paddingLeft = 8; key.style.paddingRight = 8; key.style.paddingTop = 2; key.style.paddingBottom = 3;
        key.style.backgroundColor = new Color(0.16f, 0.14f, 0.11f);
        key.style.borderLeftWidth = key.style.borderRightWidth = key.style.borderTopWidth = key.style.borderBottomWidth = 1;
        key.style.borderLeftColor = key.style.borderRightColor = key.style.borderTopColor = key.style.borderBottomColor = gold;
        key.style.borderTopLeftRadius = key.style.borderTopRightRadius = key.style.borderBottomLeftRadius = key.style.borderBottomRightRadius = 5;
        skip.Add(word);
        skip.Add(key);
        skip.RegisterCallback<MouseEnterEvent>(_ => skip.style.backgroundColor = new Color(0.2f, 0.16f, 0.06f, 0.95f));
        skip.RegisterCallback<MouseLeaveEvent>(_ => skip.style.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.92f));
        host.rootVisualElement.Add(skip);
    }

    private void UpdateTitle()
    {
        if (title == null) return;
        var start = Total - Hold - 0.6f;
        var alpha = Mathf.Clamp01((time - start) / TitleIn) * Mathf.Clamp01((Total - time) / TitleOut);
        title.style.opacity = alpha;
        title.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.94f, 1f, Mathf.Clamp01((time - start) / (TitleIn + Hold))));
    }

    private void ShowUi()
    {
        foreach (var (element, display) in hiddenUi) element.style.display = display;
        hiddenUi.Clear();
        foreach (var canvas in hiddenCanvases) if (canvas) canvas.enabled = true;
        hiddenCanvases.Clear();
        if (title != null)
        {
            var parent = title.parent;
            title.RemoveFromHierarchy();
            parent?.Q<Button>("reveal-skip")?.RemoveFromHierarchy();
            title = null;
        }
    }
}
