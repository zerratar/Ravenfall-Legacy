using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// How loud a sound file is (integrated loudness, EBU R128, in LUFS), measured with ffmpeg, so the music and the sound
/// loops can be levelled: the Suno tracks lie between -17 and -14.6, the old raid and war music at -13.3, and the
/// pack's loops anywhere from -31 (birds) to -41 (rain). Each file is measured once; the results are kept in
/// Library/RavenfallLoudness.txt by path, size and date. Without ffmpeg nothing is measured and nothing levelled.
/// </summary>
/// <remarks>
/// ffmpeg is looked for in the RAVENFALL_FFMPEG variable, on the PATH and in C:\Tools\ffmpeg\bin.
/// </remarks>
public static class Loudness
{
    private const string CachePath = "Library/RavenfallLoudness.txt";
    private static Dictionary<string, (long size, long ticks, float lufs)> cache;
    private static string ffmpeg;
    private static bool looked;

    /// <summary>The gain that brings a file to <paramref name="target"/> LUFS, between <paramref name="min"/> and <paramref name="max"/>; 1 when it cannot be measured.</summary>
    public static float Gain(string assetPath, float target, float min, float max)
    {
        var lufs = Measure(assetPath);
        if (float.IsNaN(lufs)) return 1f;
        return Mathf.Clamp(Mathf.Pow(10f, (target - lufs) / 20f), min, max);
    }

    /// <summary>The file's integrated loudness in LUFS, or NaN.</summary>
    public static float Measure(string assetPath)
    {
        Load();
        var full = Path.GetFullPath(assetPath);
        if (!File.Exists(full)) return float.NaN;
        var info = new FileInfo(full);
        if (cache.TryGetValue(assetPath, out var known) && known.size == info.Length && known.ticks == info.LastWriteTimeUtc.Ticks) return known.lufs;

        var exe = Ffmpeg();
        if (exe == null) return float.NaN;
        var lufs = Run(exe, full);
        if (float.IsNaN(lufs)) return lufs;
        cache[assetPath] = (info.Length, info.LastWriteTimeUtc.Ticks, lufs);
        Save();
        return lufs;
    }

    private static float Run(string exe, string file)
    {
        try
        {
            var start = new ProcessStartInfo(exe, "-hide_banner -nostats -i \"" + file + "\" -af ebur128=framelog=quiet -f null -")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(start);
            var output = process.StandardError.ReadToEnd();
            process.WaitForExit(60000);
            // the summary at the end: "Integrated loudness:" then " I: -16.8 LUFS"
            var at = output.LastIndexOf("Integrated loudness:", StringComparison.Ordinal);
            if (at < 0) return float.NaN;
            var i = output.IndexOf("I:", at, StringComparison.Ordinal);
            var end = output.IndexOf("LUFS", i, StringComparison.Ordinal);
            if (i < 0 || end < 0) return float.NaN;
            var text = output.Substring(i + 2, end - i - 2).Trim();
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > -70f ? value : float.NaN;
        }
        catch (Exception exc)
        {
            UnityEngine.Debug.LogWarning("Could not measure the loudness of " + file + ": " + exc.Message);
            return float.NaN;
        }
    }

    private static string Ffmpeg()
    {
        if (looked) return ffmpeg;
        looked = true;
        var candidates = new List<string>();
        var set = Environment.GetEnvironmentVariable("RAVENFALL_FFMPEG");
        if (!string.IsNullOrEmpty(set)) candidates.Add(set);
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(dir)) candidates.Add(Path.Combine(dir.Trim(), "ffmpeg.exe"));
        }
        candidates.Add(@"C:\Tools\ffmpeg\bin\ffmpeg.exe");
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return ffmpeg = candidate;
        }
        return ffmpeg = null;
    }

    private static void Load()
    {
        if (cache != null) return;
        cache = new Dictionary<string, (long, long, float)>();
        if (!File.Exists(CachePath)) return;
        foreach (var line in File.ReadAllLines(CachePath))
        {
            var parts = line.Split('\t');
            if (parts.Length == 4 && long.TryParse(parts[1], out var size) && long.TryParse(parts[2], out var ticks)
                && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lufs))
                cache[parts[0]] = (size, ticks, lufs);
        }
    }

    private static void Save()
    {
        var lines = new List<string>();
        foreach (var pair in cache)
            lines.Add(pair.Key + "\t" + pair.Value.size + "\t" + pair.Value.ticks + "\t" + pair.Value.lufs.ToString(CultureInfo.InvariantCulture));
        File.WriteAllLines(CachePath, lines);
    }
}
