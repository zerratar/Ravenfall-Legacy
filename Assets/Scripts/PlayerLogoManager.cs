using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerLogoManager : MonoBehaviour
{
    //public const string TwitchLogoUrl = "https://www.ravenfall.stream/api/twitch/clan-logo/";

    private readonly ConcurrentDictionary<Guid, Logo> userLogos = new ConcurrentDictionary<Guid, Logo>();

    [SerializeField] private Sprite replacementLogo;

    private GameManager gameManager;
    private const string logoPath = "players/logo/";
    private const string clanLogoPath = "clan/logo/";
    public string LogoUrl => (gameManager?.ServerAddress ?? "https://www.ravenfall.stream/api/") + logoPath;
    public string ClanLogoUrl => (gameManager?.ServerAddress ?? "https://www.ravenfall.stream/api/") + clanLogoPath;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void GetLogo(Guid userId, string url, Action<Sprite> onLogoDownloaded)
    {
        if (userId == null || onLogoDownloaded == null)
            return;

        if (gameManager && gameManager.LogoCensor)
        {
            onLogoDownloaded(replacementLogo);
            return;
        }

        if (TryGetLogo(userId, out var sprite))
        {
            onLogoDownloaded(sprite.Sprite);
            return;
        }


        StartCoroutine(DownloadTexture(userId, url, onLogoDownloaded));
    }

    public void GetLogo(Guid userId, Action<Sprite> onLogoDownloaded)
    {
        if (userId == null || onLogoDownloaded == null)
            return;

        if (gameManager && gameManager.LogoCensor)
        {
            onLogoDownloaded(replacementLogo);
            return;
        }

        if (TryGetLogo(userId, out var sprite))
        {
            onLogoDownloaded(sprite.Sprite);
            return;
        }

        StartCoroutine(DownloadTexture(userId, LogoUrl + userId, onLogoDownloaded));
    }

    public Sprite GetLogo(Guid raiderUserId)
    {
        //ClearCache();
        if (gameManager && gameManager.LogoCensor)
            return replacementLogo;

        if (TryGetLogo(raiderUserId, out var sprite))
            return sprite.Sprite;

        StartCoroutine(DownloadTexture(raiderUserId, LogoUrl + raiderUserId));
        return null;
    }

    internal async void ClearCache()
    {
        if (!gameManager) return;

        if (gameManager.RavenNest.Authenticated)
        {
            await gameManager.RavenNest.ClearLogoAsync(gameManager.RavenNest.TwitchUserId);
        }

        userLogos.Clear();

        foreach (var player in this.gameManager.Players.GetAllPlayers())
        {
            player.Appearance.UpdateClanCape();
        }
    }

    private bool TryGetLogo(Guid userId, out Logo logo)
    {
        if (userLogos.TryGetValue(userId, out var sprite) && sprite != null && !sprite.Expired && sprite.Sprite != null)
        {
            logo = sprite;
            return true;
        }
        logo = null;
        return false;
    }

    private IEnumerator DownloadTexture(Guid userId, string url, Action<Sprite> onLogoDownloaded = null)
    {
        if (userLogos.ContainsKey(userId))
        {
            if (TryGetLogo(userId, out var logo))
            {
                onLogoDownloaded?.Invoke(logo.Sprite);
                yield break;
            }
        }

        userLogos[userId] = null;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

        yield return www.SendWebRequest();

        if (IsError(www.result))
        {
            Shinobytes.Debug.Log(www.error);
            userLogos.TryRemove(userId, out _);
            onLogoDownloaded?.Invoke(replacementLogo);
        }
        else
        {
            try
            {
                Texture2D webTexture = ((DownloadHandlerTexture)www.downloadHandler).texture as Texture2D;
                Sprite webSprite = SpriteFromTexture2D(webTexture);
                userLogos[userId] = new Logo(webSprite);
                onLogoDownloaded?.Invoke(webSprite);
            }
            catch
            {
                onLogoDownloaded?.Invoke(replacementLogo);
            }
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
