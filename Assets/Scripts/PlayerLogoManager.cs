using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerLogoManager : MonoBehaviour
{
    public const string TwitchLogoUrl = "https://www.ravenfall.stream/api/twitch/logo/";

    private readonly ConcurrentDictionary<string, Logo> userLogos = new ConcurrentDictionary<string, Logo>();

    [SerializeField] private Sprite replacementLogo;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void GetLogo(string userId, string url, Action<Sprite> onLogoDownloaded)
    {
        //ClearCache();
        if (userId == null || onLogoDownloaded == null)
            return;

        if (gameManager.LogoCensor)
        {
            onLogoDownloaded(replacementLogo);
            return;
        }

        if (userLogos.TryGetValue(userId, out var sprite) && !sprite.Expired)
        {
            onLogoDownloaded(sprite.Sprite);
            return;
        }


        StartCoroutine(DownloadTexture(userId, url, onLogoDownloaded));
    }

    public void GetLogo(string userId, Action<Sprite> onLogoDownloaded)
    {
        //ClearCache();
        if (userId == null || onLogoDownloaded == null)
            return;

        if (gameManager.LogoCensor)
        {
            onLogoDownloaded(replacementLogo);
            return;
        }

        if (userLogos.TryGetValue(userId, out var sprite) && !sprite.Expired)
        {
            onLogoDownloaded(sprite.Sprite);
            return;
        }

        StartCoroutine(DownloadTexture(userId, TwitchLogoUrl + userId, onLogoDownloaded));
    }

    public Sprite GetLogo(string raiderUserId)
    {
        //ClearCache();
        if (gameManager.LogoCensor)
            return replacementLogo;

        if (userLogos.TryGetValue(raiderUserId, out var sprite) && !sprite.Expired)
            return sprite.Sprite;

        StartCoroutine(DownloadTexture(raiderUserId, TwitchLogoUrl + raiderUserId));
        return null;
    }

    //private void ClearCache()
    //{
    //    var keysToRemove = new HashSet<string>();
    //    foreach (var item in userLogos)
    //    {
    //        if (item.Value.Expired)
    //        {
    //            keysToRemove.Add(item.Key);
    //        }
    //    }

    //    foreach (var key in keysToRemove)
    //    {
    //        userLogos.TryRemove(key, out _);
    //    }
    //}

    private IEnumerator DownloadTexture(string userId, string url, Action<Sprite> onLogoDownloaded = null)
    {
        if (userLogos.ContainsKey(userId))
        {
            yield break;
        }

        userLogos[userId] = null;

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

        yield return www.SendWebRequest();

        if (IsError(www.result))
        {
            Debug.Log(www.error);
            userLogos.TryRemove(userId, out _);
        }
        else
        {
            Texture2D webTexture = ((DownloadHandlerTexture)www.downloadHandler).texture as Texture2D;
            Sprite webSprite = SpriteFromTexture2D(webTexture);
            userLogos[userId] = new Logo(webSprite);
            onLogoDownloaded?.Invoke(webSprite);
        }
    }

    private bool IsError(UnityWebRequest.Result result)
    {
        return result == UnityWebRequest.Result.ConnectionError ||
               result == UnityWebRequest.Result.DataProcessingError ||
               result == UnityWebRequest.Result.ProtocolError;
    }

    private Sprite SpriteFromTexture2D(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
    }

    private class Logo
    {
        private static readonly TimeSpan LifeSpan = TimeSpan.FromHours(1);
        public Logo(Sprite sprite)
        {
            Sprite = sprite;
            Created = DateTime.UtcNow;
            Expires = Created.Add(LifeSpan);
        }
        public Sprite Sprite { get; }
        public DateTime Created { get; }
        public DateTime Expires { get; }
        public bool Expired => DateTime.UtcNow > Expires;
    }
}
