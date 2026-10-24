using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Item icons, from the website's item images (imgs/items/&lt;item-name&gt;.png, the same pictures the
/// website shows), downloaded once and kept on disk, then in memory for the session.
/// </summary>
/// <remarks>
/// The tavern shelves and the shop window ask for an icon with a callback; it is called at once when
/// the icon is known, or when it arrives. An item without an image is remembered for the session, so
/// it is not asked for again.
/// </remarks>
public static class ItemIcons
{
    private const string BaseUrl = "https://www.ravenfall.stream/imgs/items/";

    private static readonly Dictionary<string, Texture2D> loaded = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, List<Action<Texture2D>>> waiting = new Dictionary<string, List<Action<Texture2D>>>();
    private static readonly HashSet<string> missing = new HashSet<string>();
    private static Runner runner;

    /// <summary>The website's file name for an item: lower case, spaces as dashes, no apostrophes.</summary>
    public static string Slug(string itemName) =>
        string.IsNullOrEmpty(itemName) ? null : itemName.ToLowerInvariant().Replace("'", "").Replace(' ', '-');

    /// <summary>Calls back with the icon (null when there is none), now or once it has been fetched.</summary>
    public static void Get(string itemName, Action<Texture2D> done)
    {
        var slug = Slug(itemName);
        if (slug == null || missing.Contains(slug))
        {
            done?.Invoke(null);
            return;
        }

        if (loaded.TryGetValue(slug, out var texture) && texture)
        {
            done?.Invoke(texture);
            return;
        }

        if (waiting.TryGetValue(slug, out var callbacks))
        {
            if (done != null) callbacks.Add(done);
            return;
        }

        waiting[slug] = new List<Action<Texture2D>>();
        if (done != null) waiting[slug].Add(done);

        var file = CachePath(slug);
        if (File.Exists(file))
        {
            try
            {
                var fromDisk = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = slug };
                if (fromDisk.LoadImage(File.ReadAllBytes(file)))
                {
                    Finish(slug, fromDisk);
                    return;
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Item icon " + slug + " could not be read from the cache: " + exc.Message);
            }
        }

        if (!runner)
        {
            runner = new GameObject("Item Icons") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Runner>();
            UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
        }

        runner.StartCoroutine(Download(slug, file));
    }

    private static IEnumerator Download(string slug, string file)
    {
        using var request = UnityWebRequestTexture.GetTexture(BaseUrl + slug + ".png");
        request.timeout = 20;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            missing.Add(slug);
            Finish(slug, null);
            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(request);
        texture.name = slug;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, request.downloadHandler.data);
        }
        catch (Exception exc)
        {
            // the icon still shows; it is only fetched again next session
            Shinobytes.Debug.LogWarning("Item icon " + slug + " could not be cached: " + exc.Message);
        }

        Finish(slug, texture);
    }

    private static void Finish(string slug, Texture2D texture)
    {
        if (texture) loaded[slug] = texture;
        if (!waiting.TryGetValue(slug, out var callbacks)) return;
        waiting.Remove(slug);
        foreach (var callback in callbacks)
        {
            try
            {
                callback(texture);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Item icon callback failed: " + exc);
            }
        }
    }

    private static string CachePath(string slug) =>
        Path.Combine(Application.persistentDataPath, "item-icons", slug + ".png");

    private sealed class Runner : MonoBehaviour { }
}
