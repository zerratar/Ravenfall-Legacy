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

    private void OnValidate() => lookup = null;
}
