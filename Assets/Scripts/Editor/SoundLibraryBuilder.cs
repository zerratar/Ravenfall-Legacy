using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the <see cref="SoundLibrary"/> from the Environment Sounds pack and Assets/Sounds/Effects whenever sounds
/// change there, before every build, and from Ravenfall > Rebuild Sound Library. Sets how they are imported.
/// </summary>
/// <remarks>
/// A pack sound's kind comes from its name ("Wood Chop 1-3" is a wood chop); loop kinds only from the pack's
/// Ambient Loops folder, so a long loop never becomes a one-shot. Our own sounds go in Assets/Sounds/Effects/<Kind>/.
/// At most <see cref="MaxPerKind"/> of a kind are used, so the build only carries what plays. Loops stream from disk
/// in stereo; one-shots are kept in memory, compressed, in mono (they are placed in the world). The library is
/// derived from the files and the pack is not versioned, so neither is the library.
/// </remarks>
public sealed class SoundLibraryBuilder : AssetPostprocessor, IPreprocessBuildWithReport
{
    private const string PackFolder = "Assets/Environment Sounds";
    private const string LoopFolder = PackFolder + "/Ambient Loops";
    private const string OwnFolder = "Assets/Sounds/Effects";
    private const string LibraryFolder = "Assets/Sounds/Resources";
    private const string LibraryPath = LibraryFolder + "/" + SoundLibrary.ResourceName + ".asset";
    private const int MaxPerKind = 8;

    // pack names, most specific first
    private static readonly (string prefix, SoundKind kind)[] Loops =
    {
        ("Rain Light Loop", SoundKind.RainLight), ("Rain Moderate Loop", SoundKind.RainModerate), ("Rain Strong Loop", SoundKind.RainStrong),
        ("Wind Loop", SoundKind.Wind), ("Birds Loop", SoundKind.Birds), ("Crickets Loop", SoundKind.Crickets),
        ("Insects Loop", SoundKind.Insects), ("Waves Loop", SoundKind.Waves), ("Fire Burning Loop", SoundKind.Fire),
        ("Village Amb Loop", SoundKind.Village),
    };

    private static readonly (string prefix, SoundKind kind)[] OneShots =
    {
        ("Wind Gust", SoundKind.WindGust), ("Thunder", SoundKind.Thunder), ("Wood Chop", SoundKind.WoodChop),
        ("Tree Falling", SoundKind.TreeFall), ("Rock Impact", SoundKind.RockImpact), ("Rock Small Debris", SoundKind.RockDebris),
        ("Water Splash 1", SoundKind.WaterSplash), ("Water Splash 2", SoundKind.WaterSplash), ("Water Bubble", SoundKind.Bubbles),
        ("Sizzle", SoundKind.Sizzle), ("Soil Impact", SoundKind.Soil), ("Foliage Foley", SoundKind.Foliage),
        ("Sawing Wood", SoundKind.Saw), ("Nail Wood", SoundKind.Hammer), ("Blacksmithing", SoundKind.Hammer),
        ("Animal Cow", SoundKind.Cow), ("Animal Chicken", SoundKind.Chicken), ("Animal Rooster", SoundKind.Rooster),
        ("Animal Sheep", SoundKind.Sheep), ("Animal Pig", SoundKind.Pig), ("Animal Horse", SoundKind.Horse),
        ("Animal Dog", SoundKind.Dog), ("Bird Chirp", SoundKind.BirdChirp),
    };

    private static bool queued;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => Rebuild();

    [MenuItem("Ravenfall/Rebuild Sound Library")]
    private static void RebuildFromMenu()
    {
        Rebuild();
        Debug.Log("Sound library rebuilt: " + AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath)?.Entries.Length + " kinds of sound.");
    }

    public static bool IsLoop(SoundKind kind) => kind <= SoundKind.Village;

    private void OnPreprocessAudio()
    {
        if (!TryClassify(assetPath, out var kind)) return;
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        if (IsLoop(kind))
        {
            importer.forceToMono = false;
            importer.loadInBackground = true;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.quality = 0.5f;
            settings.preloadAudioData = false;
        }
        else
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.quality = 0.6f;
            settings.preloadAudioData = true;
        }
        importer.defaultSampleSettings = settings;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (queued || !(Any(imported) || Any(deleted) || Any(moved) || Any(movedFrom))) return;
        queued = true;
        EditorApplication.delayCall += () =>
        {
            queued = false;
            Rebuild();
        };
    }

    private static bool Any(string[] paths)
    {
        foreach (var path in paths)
        {
            if (path.EndsWith(".meta")) continue;
            if (path.StartsWith(PackFolder + "/", StringComparison.OrdinalIgnoreCase) || path.StartsWith(OwnFolder + "/", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>The library again from the files.</summary>
    public static void Rebuild()
    {
        var byKind = new SortedDictionary<SoundKind, List<(string path, AudioClip clip)>>();
        foreach (var folder in new[] { PackFolder, OwnFolder })
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!TryClassify(path, out var kind)) continue;
                if (!byKind.TryGetValue(kind, out var list)) byKind[kind] = list = new List<(string, AudioClip)>();
                list.Add((path, null));
            }
        }

        var entries = new List<SoundLibrary.Entry>();
        foreach (var pair in byKind)
        {
            // our own sounds first, then the pack's, in name order, up to the limit
            pair.Value.Sort((a, b) =>
            {
                var own = a.path.StartsWith(OwnFolder).CompareTo(b.path.StartsWith(OwnFolder));
                return own != 0 ? -own : string.CompareOrdinal(a.path, b.path);
            });
            var clips = new List<AudioClip>();
            foreach (var (path, _) in pair.Value)
            {
                if (clips.Count >= MaxPerKind) break;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip) clips.Add(clip);
            }
            entries.Add(new SoundLibrary.Entry { Kind = pair.Key, Clips = clips.ToArray() });
        }

        if (!AssetDatabase.IsValidFolder(LibraryFolder)) AssetDatabase.CreateFolder("Assets/Sounds", "Resources");
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (!library)
        {
            library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.Entries = entries.ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
    }

    /// <summary>Which kind of sound a file is, if it is one the game uses.</summary>
    public static bool TryClassify(string path, out SoundKind kind)
    {
        kind = default;
        if (path.EndsWith(".meta")) return false;
        if (path.StartsWith(OwnFolder + "/", StringComparison.OrdinalIgnoreCase))
        {
            var folder = path.Substring(OwnFolder.Length + 1).Split('/');
            return folder.Length > 1 && Enum.TryParse(folder[0], true, out kind) && Enum.IsDefined(typeof(SoundKind), kind) && !char.IsDigit(folder[0][0]);
        }
        if (!path.StartsWith(PackFolder + "/", StringComparison.OrdinalIgnoreCase)) return false;

        var name = Path.GetFileNameWithoutExtension(path);
        var inLoops = path.StartsWith(LoopFolder + "/", StringComparison.OrdinalIgnoreCase);
        foreach (var (prefix, k) in inLoops ? Loops : OneShots)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                kind = k;
                return true;
            }
        }
        return false;
    }
}
