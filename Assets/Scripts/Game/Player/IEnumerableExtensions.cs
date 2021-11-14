using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = Shinobytes.Debug;
public class ExternalResources
{
    private static ConcurrentDictionary<string, ExternalResource<AudioClip>> audioResources;
    static ExternalResources()
    {
        LoadSoundFilesAsync();
    }

    private async static void LoadSoundFilesAsync()
    {
        audioResources = new ConcurrentDictionary<string, ExternalResource<AudioClip>>();

        string soundsFolder = GetSoundFolder();
        if (!System.IO.Directory.Exists(soundsFolder))
        {
            UnityEngine.Debug.Log("No sounds folder found in: " + soundsFolder + ". Ignoring");
            return;
        }

        var files = System.IO.Directory.GetFiles(soundsFolder, "*.mp3");
        foreach (var file in files)
        {
            await LoadSoundFileAsync(file);
        }
    }

    private static string GetSoundFolder()
    {
        var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
        var soundsFolder = System.IO.Path.Combine(dataFolder.Parent.FullName, @"data\sounds\");
        return soundsFolder;
    }

    private async static Task LoadSoundFileAsync(string file)
    {
        var wasLoadedBefore = false;
        if (audioResources.TryGetValue(file, out var res))
        {
            wasLoadedBefore = true;
            file = res.FullPath;
        }

        using (UnityWebRequest web = UnityWebRequestMultimedia.GetAudioClip("file://" + file, AudioType.MPEG))
        {
            var retries = 0;
            do
            {
                // if the file has been loaded before. Most likely they are 
                // trying to rename the file. lets wait a couple of seconds
                // 
                if (wasLoadedBefore && !System.IO.File.Exists(file))
                {
                    if (++retries >= 5)
                    {
                        return;
                    }

                    await Task.Delay(1000);

                    if (!System.IO.File.Exists(file))
                    {
                        continue;
                    }
                }

                await web.SendWebRequest();
                if (web.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(web);
                    if (clip != null)
                    {
                        audioResources[System.IO.Path.GetFileName(file)] =
                            new ExternalResource<AudioClip>(file, clip);
                    }
                }
                return;
            } while (wasLoadedBefore);
        }
    }

    public static async Task ReloadIfModifiedAsync(string key)
    {
        if (!audioResources.TryGetValue(key, out var resx))
        {
            return;
        }

        if (resx.HasBeenModified)
        {
            await LoadSoundFileAsync(resx.FullPath);
        }
    }

    //public static void ReloadIfModified(string key)
    //{
    //    if (!audioResources.TryGetValue(key, out var resx))
    //    {
    //        return;
    //    }
    //    if (resx.HasBeenModified)
    //    {
    //        LoadSoundFileAsync(resx.FullPath).Wait();
    //    }
    //}
    public static AudioClip GetAudioClip(string key)
    {
        if (!audioResources.TryGetValue(key, out var resx))
        {
            return null;
        }

        return resx.Resource;
    }
}

public class ExternalResource<T>
{
    private readonly DateTime originalWriteTime;
    private readonly DateTime originalCreationTime;
    private readonly long originalSize;
    public ExternalResource(string fullPath, T resource)
    {
        FullPath = fullPath;
        Resource = resource;
        this.originalSize = FileSize;
        this.originalWriteTime = LastWriteTime;
        this.originalCreationTime = CreationTime;
    }
    public T Resource { get; set; }
    public string FullPath { get; set; }
    public long FileSize => new System.IO.FileInfo(FullPath).Length;
    public DateTime CreationTime => new System.IO.FileInfo(FullPath).CreationTime;
    public DateTime LastWriteTime => new System.IO.FileInfo(FullPath).LastWriteTime;
    public bool HasBeenModified
    {
        get
        {
            var fi = new System.IO.FileInfo(FullPath);
            if (!fi.Exists) return true;
            return originalSize != fi.Length || originalWriteTime != fi.LastWriteTime || originalWriteTime != fi.CreationTime;
        }
    }
}
public static class AudioSourceExtensions
{
    private readonly static Dictionary<string, string> audioFileLookup = new Dictionary<string, string>();
    public static IEnumerator LoadAudioClipFromFile(this AudioSource audioSource, string fileName)
    {
        if (!audioFileLookup.TryGetValue(fileName, out var raidMp3Override))
        {
            var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
            var soundsFolder = System.IO.Path.Combine(dataFolder.Parent.FullName, @"data\sounds\");
            if (!System.IO.Directory.Exists(soundsFolder))
            {
                Debug.Log("No sounds folder found in: " + soundsFolder + ". Ignoring");
                yield break;
            }

            raidMp3Override = System.IO.Directory.GetFiles(soundsFolder, fileName).FirstOrDefault();
            if (raidMp3Override == null || !System.IO.File.Exists(raidMp3Override))
            {
                Debug.Log("No " + fileName + " found. Ignoring");
                yield break;
            }
        }

        using (UnityWebRequest web = UnityWebRequestMultimedia.GetAudioClip("file://" + raidMp3Override, AudioType.MPEG))
        {
            yield return web.SendWebRequest();
            if (web.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(web);
                if (clip != null)
                {
                    audioSource.clip = clip;
                }
            }
        }
    }

}

public static class IEnumerableExtensions
{
    public static IReadOnlyList<double> Delta(this IList<double> newValue, IReadOnlyList<double> oldValue)
    {
        if (oldValue == null)
        {
            return new List<double>(newValue.Count);
        }
        if (newValue.Count != oldValue.Count)
        {
            return new List<double>(newValue.Count);
        }

        return newValue.Select((x, i) => x - oldValue[i]).ToList();
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T except)
    {
        return items.Where(x => !x.Equals(except));
    }

    public static int RandomIndex<T>(this IEnumerable<T> items)
    {
        return UnityEngine.Random.Range(0, items.Count());
    }
    public static T Random<T>(this IEnumerable<T> items)
    {
        var selections = items.ToList();
        return selections[UnityEngine.Random.Range(0, selections.Count)];
    }

    public static IReadOnlyList<T> Randomize<T>(this IEnumerable<T> items)
    {
        return items.OrderBy(x => UnityEngine.Random.value).ToList();
    }
    public static T Weighted<T, T2>(this IEnumerable<T> items, Func<T, T2> weight)
       where T2 : struct
    {
        var selections = items.ToArray();

        foreach (var sel in selections)
        {
            var ran = UnityEngine.Random.value;
            var w = weight(sel);

            if (w is float f && ran <= f)
            {
                return sel;
            }

            if (w is int i && ran <= i)
            {
                return sel;
            }

            if (w is short s && ran <= s)
            {
                return sel;
            }

            if (w is decimal de && ran <= (double)de)
            {
                return sel;
            }

            if (w is byte b && ran <= b)
            {
                return sel;
            }

            if (w is double d && ran <= d)
            {
                return sel;
            }
        }

        return selections[UnityEngine.Random.Range(0, selections.Length)];
    }
    public static T RandomizedWeighted<T, T2>(this IEnumerable<T> items, Func<T, T2> weight)
        where T2 : struct
    {
        var selections = items.Randomize();

        foreach (var sel in selections)
        {
            var ran = UnityEngine.Random.value;
            var w = weight(sel);

            if (w is float f && ran <= f)
            {
                return sel;
            }

            if (w is int i && ran <= i)
            {
                return sel;
            }

            if (w is short s && ran <= s)
            {
                return sel;
            }

            if (w is decimal de && ran <= (double)de)
            {
                return sel;
            }

            if (w is byte b && ran <= b)
            {
                return sel;
            }

            if (w is double d && ran <= d)
            {
                return sel;
            }
        }

        return selections[UnityEngine.Random.Range(0, selections.Count)];
    }
}