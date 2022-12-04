using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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
            Shinobytes.Debug.Log("No sounds folder found in: " + soundsFolder + ". Ignoring");
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
                if (wasLoadedBefore && !Shinobytes.IO.File.Exists(file))
                {
                    if (++retries >= 5)
                    {
                        return;
                    }

                    await Task.Delay(1000);

                    if (!Shinobytes.IO.File.Exists(file))
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
