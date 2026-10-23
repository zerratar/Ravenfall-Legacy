using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the <see cref="MusicLibrary"/> from Assets/Sounds/Music whenever music is added, removed or moved there,
/// before every build, and from Ravenfall > Rebuild Music Library. Music there is imported to stream from disk.
/// </summary>
/// <remarks>
/// A track's situation comes from, in this order: the folder it is in (Night/, Kyo/, ...), the prompt number its
/// name starts with ("3.Dreamy Lullaby", see docs/music.md), or a situation name its name is or starts with
/// ("War", "Night - Owls"). Anything else plays by day. The library is derived from the files, so it is not
/// versioned; a fresh checkout builds it on the first import.
/// </remarks>
public sealed class MusicLibraryBuilder : AssetPostprocessor, IPreprocessBuildWithReport
{
    private const string MusicFolder = "Assets/Sounds/Music";
    private const string LibraryFolder = "Assets/Sounds/Resources";
    private const string LibraryPath = LibraryFolder + "/" + MusicLibrary.ResourceName + ".asset";
    // LUFS every track is brought to (the quietest Suno tracks are about this)
    private const float MusicLoudness = -17f;

    // tracks from the music packs that fit a situation, where they are
    private static readonly (string path, MusicContext context)[] Extras =
    {
        ("Assets/Tavern Music Free Pack/WAV Tracks/Lute by the Fire 24_48_Tempo_90_LOOP.wav", MusicContext.Tavern),
        ("Assets/DarkFantasyMusicPack/Bewitched.wav", MusicContext.Dungeon),
    };

    // the prompt numbers in docs/music.md
    private static readonly Dictionary<int, MusicContext> ByNumber = new Dictionary<int, MusicContext>
    {
        { 1, MusicContext.Day }, { 2, MusicContext.Day }, { 3, MusicContext.Night }, { 4, MusicContext.Rain },
        { 5, MusicContext.Sailing }, { 6, MusicContext.Kyo }, { 7, MusicContext.Heim }, { 8, MusicContext.Eldara },
        { 9, MusicContext.Atria }, { 10, MusicContext.Ironhill }, { 11, MusicContext.Village }, { 12, MusicContext.Tavern },
        { 13, MusicContext.Onsen }, { 14, MusicContext.Menu }, { 15, MusicContext.Dungeon }, { 16, MusicContext.DungeonBoss },
        { 17, MusicContext.RaidBoss }, { 18, MusicContext.War }, { 19, MusicContext.Arena },
    };

    private static readonly Dictionary<string, MusicContext> Aliases = new Dictionary<string, MusicContext>(StringComparer.OrdinalIgnoreCase)
    {
        { "Game", MusicContext.Day }, { "Kyojin", MusicContext.Kyo }, { "Iceheim", MusicContext.Heim },
        { "BuildMode", MusicContext.Village }, { "Build", MusicContext.Village }, { "Boss", MusicContext.DungeonBoss },
        { "Raid", MusicContext.RaidBoss }, { "Ferry", MusicContext.Sailing }, { "Login", MusicContext.Menu },
    };

    private static bool queued;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => Rebuild();

    [MenuItem("Ravenfall/Rebuild Music Library")]
    private static void RebuildFromMenu()
    {
        Rebuild();
        Debug.Log("Music library rebuilt: " + AssetDatabase.LoadAssetAtPath<MusicLibrary>(LibraryPath)?.Entries.Length + " situations with music.");
    }

    // music is long: it streams from disk instead of sitting in memory, compressed
    private void OnPreprocessAudio()
    {
        if (!IsMusic(assetPath)) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = false;
        importer.loadInBackground = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.6f;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (queued || !(AnyMusic(imported) || AnyMusic(deleted) || AnyMusic(moved) || AnyMusic(movedFrom))) return;
        queued = true;
        // not while assets are being imported
        EditorApplication.delayCall += () =>
        {
            queued = false;
            Rebuild();
        };
    }

    private static bool AnyMusic(string[] paths)
    {
        foreach (var path in paths) if (IsMusic(path)) return true;
        return false;
    }

    private static bool IsMusic(string path)
    {
        if (path.StartsWith(MusicFolder + "/", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".meta")) return true;
        foreach (var (extra, _) in Extras) if (string.Equals(path, extra, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>The library again from the files.</summary>
    public static void Rebuild()
    {
        var byContext = new SortedDictionary<MusicContext, List<AudioClip>>();
        void Add(MusicContext context, AudioClip clip)
        {
            if (!clip) return;
            if (!byContext.TryGetValue(context, out var list)) byContext[context] = list = new List<AudioClip>();
            if (!list.Contains(clip)) list.Add(clip);
        }

        if (AssetDatabase.IsValidFolder(MusicFolder))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MusicFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Add(Classify(path), AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
        }
        foreach (var (path, context) in Extras) Add(context, AssetDatabase.LoadAssetAtPath<AudioClip>(path));

        var entries = new List<MusicLibrary.Entry>();
        foreach (var pair in byContext)
        {
            pair.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            // every track to the loudness of the quietest Suno tracks; only ever down, so nothing clips
            var gains = new float[pair.Value.Count];
            for (var i = 0; i < gains.Length; ++i) gains[i] = Loudness.Gain(AssetDatabase.GetAssetPath(pair.Value[i]), MusicLoudness, 0.25f, 1f);
            entries.Add(new MusicLibrary.Entry { Context = pair.Key, Clips = pair.Value.ToArray(), Gains = gains });
        }

        if (!AssetDatabase.IsValidFolder(LibraryFolder)) AssetDatabase.CreateFolder("Assets/Sounds", "Resources");
        var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(LibraryPath);
        if (!library)
        {
            library = ScriptableObject.CreateInstance<MusicLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.Entries = entries.ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
    }

    /// <summary>Which situation a track under the music folder plays in.</summary>
    public static MusicContext Classify(string path)
    {
        var relative = path.Substring(MusicFolder.Length).TrimStart('/');
        var parts = relative.Split('/');
        if (parts.Length > 1 && TryContext(parts[0], out var byFolder)) return byFolder;

        var name = Path.GetFileNameWithoutExtension(path).Trim();
        var digits = 0;
        while (digits < name.Length && char.IsDigit(name[digits])) digits++;
        if (digits > 0 && digits < name.Length && ".-_ ".IndexOf(name[digits]) >= 0
            && int.TryParse(name.Substring(0, digits), out var number) && ByNumber.TryGetValue(number, out var byNumber))
            return byNumber;

        if (TryContext(name, out var byName)) return byName;
        var cut = name.IndexOfAny(new[] { ' ', '-', '_', '.' });
        if (cut > 0 && TryContext(name.Substring(0, cut), out var byPrefix)) return byPrefix;
        return MusicContext.Day;
    }

    private static bool TryContext(string word, out MusicContext context)
    {
        if (Aliases.TryGetValue(word, out context)) return true;
        // Enum.TryParse takes numbers too; a situation is a name
        if (word.Length == 0 || char.IsDigit(word[0])) return false;
        return Enum.TryParse(word, true, out context) && Enum.IsDefined(typeof(MusicContext), context);
    }
}
