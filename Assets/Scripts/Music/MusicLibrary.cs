using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The situations music is chosen for, most specific first in <see cref="MusicManager"/>.</summary>
public enum MusicContext
{
    Day,
    Night,
    Rain,
    Sailing,
    Home,
    Away,
    Ironhill,
    Kyo,
    Heim,
    Atria,
    Eldara,
    Village,
    Tavern,
    Onsen,
    Menu,
    Dungeon,
    DungeonBoss,
    RaidBoss,
    War,
    Arena,
}

/// <summary>
/// The game's music, by situation. Built in the editor from Assets/Sounds/Music (MusicLibraryBuilder) and loaded
/// from Resources at runtime, so adding a track is dropping the file in the folder.
/// </summary>
public sealed class MusicLibrary : ScriptableObject
{
    public const string ResourceName = "MusicLibrary";

    [Serializable]
    public sealed class Entry
    {
        public MusicContext Context;
        public AudioClip[] Clips = Array.Empty<AudioClip>();
        // per clip, to bring the tracks to the same loudness
        public float[] Gains = Array.Empty<float>();
    }

    public Entry[] Entries = Array.Empty<Entry>();

    private Dictionary<MusicContext, AudioClip[]> lookup;

    /// <summary>The tracks for a situation, empty when it has none.</summary>
    public IReadOnlyList<AudioClip> Get(MusicContext context)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<MusicContext, AudioClip[]>();
            foreach (var entry in Entries)
            {
                if (entry != null && entry.Clips != null) lookup[entry.Context] = entry.Clips;
            }
        }
        return lookup.TryGetValue(context, out var clips) ? clips : Array.Empty<AudioClip>();
    }

    private Dictionary<AudioClip, float> gains;

    /// <summary>How much a clip is turned down or up to the common loudness (Loudness, in the editor); 1 if unknown.</summary>
    public float Gain(AudioClip clip)
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

    private void OnValidate()
    {
        lookup = null;
        gains = null;
    }
}
