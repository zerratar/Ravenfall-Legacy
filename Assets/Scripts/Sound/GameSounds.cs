using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The world's sounds: what players do (an axe in a tree, a pick on a rock, a line in the water, a hammer at the
/// bench), the place round the camera (wind, birds by day, crickets at night, waves near the sea, the village, the
/// fires) and the animals. Recorded clips from <see cref="SoundLibrary"/>; without a library it stays silent.
/// </summary>
/// <remarks>
/// With hundreds of players at work the actions would be a wall of noise, so they are heard only near the camera,
/// at most a few of a kind at once and not closer together than a tenth of a second, and other players more quietly
/// than the one the camera follows, who is always heard. Loops for the place grow and fade with the time of day, the
/// weather and the island; down in a dungeon they all go quiet. Rain and thunder are the weather's
/// (<see cref="WeatherEffects"/>), with their own volume.
/// </remarks>
public sealed class GameSounds : MonoBehaviour
{
    /// <summary>The setting: the volume of these sounds, 0 to 1.</summary>
    public static float Volume { get; set; } = 0.6f;

    // ---- one-shots
    private const int PoolSize = 20;
    private const int PerKind = 3;
    private const float MinGap = 0.1f;
    private const float Hearing = 45f;
    private const float OthersVolume = 0.55f;

    // ---- the place
    private const float CheckEvery = 0.5f;
    private const float BedFade = 0.35f;
    private const float DungeonDepth = -30f;
    private const float SeaProbeDistance = 20f;
    private const float VillageRadius = 70f;

    // ---- fires and animals
    private const int FireSources = 4;
    private const float FireHearing = 30f;
    private const float ScanEvery = 10f;
    private const float AnimalHearing = 32f;

    private static GameSounds instance;

    private AudioSource[] pool;
    private SoundKind[] poolKind;
    private readonly Dictionary<SoundKind, float> lastPlayed = new Dictionary<SoundKind, float>();
    private readonly List<(SoundKind kind, Vector3 at, float volume, float time, bool observed)> delayed = new List<(SoundKind, Vector3, float, float, bool)>();

    private sealed class Bed
    {
        public SoundKind Kind;
        public AudioSource Source;
        public float Target;
        // the playing loop turned to the common loudness
        public float Gain = 1f;
    }
    private readonly List<Bed> beds = new List<Bed>();
    private Bed wind, birds, crickets, insects, waves, village;

    private AudioSource[] fires;
    private readonly List<Vector3> firePlaces = new List<Vector3>();
    private readonly List<(Transform animal, SoundKind voice, EnemyController enemy)> animals = new List<(Transform, SoundKind, EnemyController)>();
    private AudioSource flat;

    private GameManager game;
    private Ocean ocean;
    private TownHallController townHall;
    private float nextCheck, nextScan, nextAnimal, nextGust;
    private float seaShare;
    private bool wasNight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying || FindAnyObjectByType<GameSounds>()) return;
        var go = new GameObject("Game Sounds") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<GameSounds>();
    }

    private void Awake()
    {
        instance = this;
        pool = new AudioSource[PoolSize];
        poolKind = new SoundKind[PoolSize];
        for (var i = 0; i < PoolSize; ++i) pool[i] = MakeSource("Sound", true);
        fires = new AudioSource[FireSources];
        for (var i = 0; i < FireSources; ++i)
        {
            fires[i] = MakeSource("Fire", true);
            fires[i].loop = true;
            fires[i].maxDistance = FireHearing;
        }
        flat = MakeSource("Flat", false);
        wind = AddBed(SoundKind.Wind);
        birds = AddBed(SoundKind.Birds);
        crickets = AddBed(SoundKind.Crickets);
        insects = AddBed(SoundKind.Insects);
        waves = AddBed(SoundKind.Waves);
        village = AddBed(SoundKind.Village);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private AudioSource MakeSource(string name, bool placed)
    {
        var source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.playOnAwake = false;
        source.spatialBlend = placed ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 6f;
        source.maxDistance = Hearing;
        source.dopplerLevel = 0f;
        return source;
    }

    private Bed AddBed(SoundKind kind)
    {
        var source = MakeSource(kind.ToString(), false);
        source.loop = true;
        source.volume = 0f;
        var bed = new Bed { Kind = kind, Source = source };
        beds.Add(bed);
        return bed;
    }

    // ---- what players do -----------------------------------------------------------------------------

    /// <summary>
    /// A sound at a place, after a delay (to land on the swing's hit). <paramref name="player"/> is who made it: the
    /// player the camera follows is always heard and loudest, others only while few of the kind are playing.
    /// </summary>
    public static void Play(SoundKind kind, Vector3 at, float volume = 1f, float delay = 0f, PlayerController player = null)
    {
        if (!instance || Volume <= 0.001f) return;
        var observed = player && instance.IsObserved(player);
        if (delay > 0f) instance.delayed.Add((kind, at, volume, Time.time + delay, observed));
        else instance.PlayNow(kind, at, volume, observed);
    }

    private bool IsObserved(PlayerController player)
    {
        // the hero is who the stream watches while hero mode runs
        if (HeroController.Active && HeroController.Active.Player == player) return true;
        if (!game) game = GameManager.Instance;
        var watched = game && game.Camera && game.Camera.Observer ? game.Camera.Observer.ObservedPlayer : null;
        return watched == player;
    }

    private void PlayNow(SoundKind kind, Vector3 at, float volume, bool observed)
    {
        var listener = Listener();
        if (!listener || (at - listener.position).sqrMagnitude > Hearing * Hearing) return;

        var now = Time.time;
        if (!observed)
        {
            if (lastPlayed.TryGetValue(kind, out var last) && now - last < MinGap) return;
            var playing = 0;
            for (var i = 0; i < PoolSize; ++i) if (pool[i].isPlaying && poolKind[i] == kind) playing++;
            if (playing >= PerKind) return;
        }

        var clip = SoundLibrary.Pick(kind);
        if (!clip) return;

        // a free source, or the one nearest its end
        var source = pool[0];
        var index = 0;
        var best = float.MaxValue;
        for (var i = 0; i < PoolSize; ++i)
        {
            var s = pool[i];
            if (!s.isPlaying) { source = s; index = i; break; }
            var left = s.clip ? s.clip.length - s.time : 0f;
            if (left < best) { best = left; source = s; index = i; }
        }

        lastPlayed[kind] = now;
        poolKind[index] = kind;
        source.transform.position = at;
        source.clip = clip;
        source.volume = Volume * volume * (observed ? 1f : OthersVolume) * Random.Range(0.85f, 1f);
        source.pitch = Random.Range(0.92f, 1.08f);
        source.Play();
    }

    private static Transform Listener()
    {
        var camera = Camera.main;
        return camera ? camera.transform : null;
    }

    // ---- the place ---------------------------------------------------------------------------------

    private void Update()
    {
        var now = Time.time;
        for (var i = delayed.Count - 1; i >= 0; --i)
        {
            if (delayed[i].time > now) continue;
            var d = delayed[i];
            delayed.RemoveAt(i);
            PlayNow(d.kind, d.at, d.volume, d.observed);
        }

        var library = SoundLibrary.Instance;
        if (!library) return;

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanEvery;
            Scan();
        }
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + CheckEvery;
            SetTargets();
            PlaceFires();
        }

        var fade = Time.unscaledDeltaTime * BedFade;
        foreach (var bed in beds)
        {
            var source = bed.Source;
            var wanted = bed.Target * Volume;
            source.volume = Mathf.MoveTowards(source.volume, wanted * bed.Gain, fade);
            if (wanted > 0.001f && !source.isPlaying)
            {
                source.clip = SoundLibrary.Pick(bed.Kind);
                bed.Gain = SoundLibrary.Gain(source.clip);
                if (source.clip)
                {
                    // each loop starts somewhere in, so two of the same never line up
                    source.time = Random.Range(0f, source.clip.length * 0.8f);
                    source.Play();
                }
            }
            else if (source.volume <= 0.001f && source.isPlaying) source.Stop();
        }

        Animals(now);
        Gusts(now);
    }

    private void SetTargets()
    {
        if (!game) game = GameManager.Instance;
        var listener = Listener();
        var outside = listener && listener.position.y > DungeonDepth;
        var island = Ambience.Island;
        var land = outside && Ambience.HasFocus && !string.IsNullOrEmpty(island);
        var snowy = island == "Heim";
        var night = Ambience.Darkness;
        var day = 1f - night;
        var rain = Weather.Rain;
        var dry = 1f - rain;

        wind.Target = outside ? 0.12f + 0.35f * Weather.Storm + 0.2f * rain + (snowy ? 0.25f : 0f) : 0f;
        birds.Target = land && !snowy ? 0.3f * day * dry * dry : 0f;
        crickets.Target = land && !snowy ? 0.26f * night * dry : 0f;
        insects.Target = land && !snowy ? 0.1f * day * dry : 0f;
        seaShare = outside ? SeaShare() : 0f;
        waves.Target = 0.45f * seaShare;

        if (!townHall) townHall = FindAnyObjectByType<TownHallController>();
        var inVillage = land && island == "Home" && townHall && (Ambience.FocusPoint - townHall.transform.position).sqrMagnitude < VillageRadius * VillageRadius;
        village.Target = inVillage ? 0.2f * day * dry : 0f;

        // a rooster on the home island when the night is over
        var isNight = night > 0.5f;
        if (wasNight && !isNight && inVillage) PlayFlat(SoundKind.Rooster, 0.6f);
        wasNight = isNight;
    }

    /// <summary>How much of the ground round the focus is open water, 0 to 1: a ray down at the focus and around it.</summary>
    private float SeaShare()
    {
        if (!Ambience.HasFocus) return 0f;
        if (!ocean) ocean = FindAnyObjectByType<Ocean>();
        if (!ocean) return 0f;
        var sea = ocean.transform.position.y + 0.2f;
        var focus = Ambience.FocusPoint;
        var water = 0;
        const int probes = 9;
        for (var i = 0; i < probes; ++i)
        {
            var at = focus;
            if (i > 0)
            {
                var angle = (i - 1) * Mathf.PI * 2f / (probes - 1);
                at += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SeaProbeDistance;
            }
            var ground = Physics.Raycast(at + Vector3.up * 40f, Vector3.down, out var hit, 90f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y > sea;
            if (!ground) water++;
        }
        return water / (float)probes;
    }

    // ---- fires and animals --------------------------------------------------------------------------

    /// <summary>Where the fires are (the village's and the torches' lights), and which animals there are.</summary>
    private void Scan()
    {
        firePlaces.Clear();
        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Point || !light.isActiveAndEnabled) continue;
            if (IsFire(light.transform)) firePlaces.Add(light.transform.position);
        }

        animals.Clear();
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
        {
            if (Voice(enemy.name, out var voice)) animals.Add((enemy.transform, voice, enemy));
        }
        foreach (var animator in FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
        {
            if (animator.GetComponentInParent<EnemyController>()) continue;
            if (Voice(animator.name, out var voice)) animals.Add((animator.transform, voice, null));
        }
    }

    private static bool IsFire(Transform t)
    {
        for (var i = 0; t && i < 3; ++i, t = t.parent)
        {
            var name = t.name;
            if (name.IndexOf("Fire", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Torch", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Brazier", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static bool Voice(string name, out SoundKind voice)
    {
        voice = default;
        if (Has(name, "rooster")) voice = SoundKind.Rooster;
        else if (Has(name, "chicken") || Has(name, "hen")) voice = SoundKind.Chicken;
        else if (Has(name, "cow") || Has(name, "bull")) voice = SoundKind.Cow;
        else if (Has(name, "sheep") || Has(name, "lamb")) voice = SoundKind.Sheep;
        else if (Has(name, "pig") || Has(name, "boar")) voice = SoundKind.Pig;
        else if (Has(name, "horse")) voice = SoundKind.Horse;
        else if (Has(name, "dog")) voice = SoundKind.Dog;
        else return false;
        return true;
    }

    /// <summary>A whole word of the name: enemies carry viewers' names, and "Stephen" is not a hen.</summary>
    private static bool Has(string name, string word)
    {
        var at = 0;
        while ((at = name.IndexOf(word, at, System.StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = at == 0 || !char.IsLetter(name[at - 1]);
            var end = at + word.Length;
            var after = end >= name.Length || !char.IsLetter(name[end]);
            if (before && after) return true;
            at = end;
        }
        return false;
    }

    /// <summary>The fire loops on the fires nearest the focus.</summary>
    private void PlaceFires()
    {
        var listener = Listener();
        var outside = listener && listener.position.y > DungeonDepth;
        var focus = Ambience.HasFocus ? Ambience.FocusPoint : listener ? listener.position : Vector3.zero;
        for (var f = 0; f < FireSources; ++f)
        {
            // the f-th nearest fire within hearing
            var bestIndex = -1;
            var bestDistance = FireHearing * FireHearing;
            for (var i = 0; i < firePlaces.Count; ++i)
            {
                var d = (firePlaces[i] - focus).sqrMagnitude;
                if (d >= bestDistance) continue;
                var taken = false;
                for (var g = 0; g < f; ++g) if (fires[g].isPlaying && fires[g].transform.position == firePlaces[i]) taken = true;
                if (taken) continue;
                bestDistance = d;
                bestIndex = i;
            }

            var source = fires[f];
            if (!outside || bestIndex < 0 || Volume <= 0.001f)
            {
                if (source.isPlaying) source.Stop();
                continue;
            }
            source.transform.position = firePlaces[bestIndex];
            if (!source.isPlaying)
            {
                source.clip = SoundLibrary.Pick(SoundKind.Fire);
                if (!source.clip) continue;
                source.time = Random.Range(0f, source.clip.length * 0.8f);
                source.Play();
            }
            source.volume = 0.5f * Volume * SoundLibrary.Gain(source.clip);
        }
    }

    /// <summary>Now and then an animal near the focus calls.</summary>
    private void Animals(float now)
    {
        if (now < nextAnimal || animals.Count == 0) return;
        nextAnimal = now + Random.Range(5f, 12f);
        var focus = Ambience.HasFocus ? Ambience.FocusPoint : Vector3.zero;
        var start = Random.Range(0, animals.Count);
        for (var n = 0; n < animals.Count; ++n)
        {
            var (animal, voice, enemy) = animals[(start + n) % animals.Count];
            if (!animal || !animal.gameObject.activeInHierarchy) continue;
            if (enemy && enemy.Stats != null && enemy.Stats.IsDead) continue;
            if ((animal.position - focus).sqrMagnitude > AnimalHearing * AnimalHearing) continue;
            PlayNow(voice, animal.position, 0.7f, false);
            return;
        }
    }

    /// <summary>A gust now and then when the wind is up.</summary>
    private void Gusts(float now)
    {
        if (wind.Target < 0.3f) { nextGust = now + 10f; return; }
        if (now < nextGust) return;
        nextGust = now + Random.Range(12f, 30f);
        PlayFlat(SoundKind.WindGust, 0.4f * wind.Target);
    }

    private void PlayFlat(SoundKind kind, float volume)
    {
        if (Volume <= 0.001f) return;
        var clip = SoundLibrary.Pick(kind);
        if (!clip) return;
        flat.panStereo = Random.Range(-0.5f, 0.5f);
        flat.pitch = Random.Range(0.95f, 1.05f);
        flat.PlayOneShot(clip, Volume * volume);
    }
}
