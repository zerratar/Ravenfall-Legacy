﻿using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ExternalResources
{
    private static ConcurrentDictionary<string, ExternalResource<AudioClip>> audioResources;
    private static ConcurrentDictionary<string, ExternalResource<Sprite>> spriteResources;

    static ExternalResources()
    {
        LoadSoundFilesAsync();
        LoadSpriteFilesAsync();
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

    private async static void LoadSpriteFilesAsync()
    {
        spriteResources = new ConcurrentDictionary<string, ExternalResource<Sprite>>();

        string spriteFolder = GetSpriteFolder();
        if (!System.IO.Directory.Exists(spriteFolder))
        {
            Shinobytes.Debug.Log("No sprite folder found in: " + spriteFolder + ". Ignoring");
            return;
        }

        var files = System.IO.Directory.GetFiles(spriteFolder, "*.png");
        foreach (var file in files)
        {
            await LoadSpriteFileAsync(file);
        }
    }

    private static string GetSpriteFolder()
    {
        var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
        return System.IO.Path.Combine(dataFolder.Parent.FullName, @"data\sprites\");
    }

    private static string GetSoundFolder()
    {
        var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
        return System.IO.Path.Combine(dataFolder.Parent.FullName, @"data\sounds\");
    }

    private async static Task LoadSpriteFileAsync(string file)
    {
        var wasLoadedBefore = false;
        if (spriteResources.TryGetValue(file, out var res))
        {
            wasLoadedBefore = true;
            file = res.FullPath;
        }

        using (UnityWebRequest web = UnityWebRequestTexture.GetTexture("file://" + file))
        {
            var retries = 0;
            do
            {
                // if the file has been loaded before. Most likely they are 
                // trying to rename the file. lets wait a couple of seconds
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
                    var name = System.IO.Path.GetFileName(file);
                    var tex = DownloadHandlerTexture.GetContent(web);
                    if (tex != null)
                    {
                        tex.name = name;

                        if (spriteResources.TryGetValue(name, out var existing) && existing.Resource)
                        {
                            GameObject.Destroy(existing.Resource);
                        }

                        var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                        sprite.name = name;
                        spriteResources[name] = new ExternalResource<Sprite>(file, sprite);
                    }
                }
                return;
            } while (wasLoadedBefore);
        }
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
                    var name = System.IO.Path.GetFileName(file);
                    var clip = DownloadHandlerAudioClip.GetContent(web);
                    if (clip != null)
                    {
                        clip.name = name;

                        if (audioResources.TryGetValue(name, out var existing) && existing.Resource)
                        {
                            GameObject.Destroy(existing.Resource);
                        }

                        audioResources[name] = new ExternalResource<AudioClip>(file, clip);
                    }
                }
                return;
            } while (wasLoadedBefore);
        }
    }

    public static async Task ReloadIfModifiedAsync(string key)
    {
        try
        {
            if (!audioResources.TryGetValue(key, out var resx))
            {
                return;
            }

            if (resx.HasBeenModified)
            {

#if UNITY_EDITOR
                Shinobytes.Debug.LogWarning("Reloading External Resource as it has been modified. Key: " + key);
#endif
                await LoadSoundFileAsync(resx.FullPath);
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Reloading External Resources failed. Key: " + key + ", Exception: " + exc);
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

    public static bool TryGetSprite(string key, out Sprite sprite)
    {
        sprite = null;
        if (spriteResources.TryGetValue(key, out var resx))
        {
            sprite = resx.Resource;
        }

        return sprite != null;
    }

    public static Sprite GetSprite(string key)
    {
        if (!spriteResources.TryGetValue(key, out var resx))
        {
            return null;
        }

        return resx.Resource;
    }

    public static AudioClip GetAudioClip(string key)
    {
        if (!audioResources.TryGetValue(key, out var resx))
        {
            return null;
        }

        return resx.Resource;
    }
}
