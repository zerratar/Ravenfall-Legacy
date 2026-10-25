using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The game's sounds besides the music: loops for the weather and the world, one-shots for what happens.</summary>
public enum SoundKind
{
    // loops
    RainLight,
    RainModerate,
    RainStrong,
    Wind,
    Birds,
    Crickets,
    Insects,
    Waves,
    Fire,
    Village,
    // one-shots
    WindGust,
    Thunder,
    WoodChop,
    TreeFall,
    RockImpact,
    RockDebris,
    WaterSplash,
    Bubbles,
    Sizzle,
    Soil,
    Foliage,
    Saw,
    Hammer,
    Cow,
    Chicken,
    Rooster,
    Sheep,
    Pig,
    Horse,
    Dog,
    BirdChirp,
    // hero mode's fights (from Gatebound's packs, in Assets/Sounds/Effects)
    Swing,
    Slash,
    HeavySlash,
    BowShot,
    ArrowHit,
    Dodge,
    Heal,
    Zap,
    HolyCast,
    GoblinAttack,
    GoblinHurt,
    GoblinDeath,
    HeroStruck,
}

/// <summary>
/// The sounds by kind. Built in the editor from the Environment Sounds pack and Assets/Sounds/Effects
/// (SoundLibraryBuilder) and loaded from Resources at runtime.
/// </summary>
public sealed class SoundLibrary : ScriptableObject
{
    public const string ResourceName = "SoundLibrary";

    [Serializable]
    public sealed class Entry
    {
        public SoundKind Kind;
        public AudioClip[] Clips = Array.Empty<AudioClip>();
        // per clip, to bring the loops to the same loudness (the pack's lie between -31 and -41 LUFS)
        public float[] Gains = Array.Empty<float>();
    }

    public Entry[] Entries = Array.Empty<Entry>();

    private Dictionary<SoundKind, AudioClip[]> lookup;

    private static SoundLibrary loaded;
    private static bool tried;

    /// <summary>The library, or null when there is none (no sounds imported).</summary>
    public static SoundLibrary Instance
    {
        get
        {
            if (!tried)
            {
                tried = true;
                loaded = Resources.Load<SoundLibrary>(ResourceName);
            }
            return loaded;
        }
    }

    /// <summary>The clips of a kind, empty when it has none.</summary>
    public IReadOnlyList<AudioClip> Get(SoundKind kind)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<SoundKind, AudioClip[]>();
            foreach (var entry in Entries)
            {
                if (entry != null && entry.Clips != null) lookup[entry.Kind] = entry.Clips;
            }
        }
        return lookup.TryGetValue(kind, out var clips) ? clips : Array.Empty<AudioClip>();
    }

    /// <summary>A random clip of a kind, or null.</summary>
    public static AudioClip Pick(SoundKind kind)
    {
        var library = Instance;
        if (!library) return null;
        var clips = library.Get(kind);
        return clips.Count == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Count)];
    }

    private Dictionary<AudioClip, float> gains;

    /// <summary>How much a clip is turned down or up to the common loudness (Loudness, in the editor); 1 if unknown.</summary>
    public float GainOf(AudioClip clip)
    {
        if (!clip) return 1f;
        if (gains == null)
        {
            gains = new Dictionary<AudioClip, float>();
            foreach (var entry in Entries)
            {
                if (entry?.Clips == null) continue;
                for (var i = 0; i < entry.Clips.Length; ++i)
                {
                    if (entry.Clips[i] && entry.Gains != null && i < entry.Gains.Length) gains[entry.Clips[i]] = entry.Gains[i];
                }
            }
        }
        return gains.TryGetValue(clip, out var gain) ? gain : 1f;
    }

    /// <summary>A clip's gain in the library, 1 without one.</summary>
    public static float Gain(AudioClip clip) => Instance ? Instance.GainOf(clip) : 1f;

    private void OnValidate()
    {
        lookup = null;
        gains = null;
    }
}
