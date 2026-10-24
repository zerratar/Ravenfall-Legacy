using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Downloads player and clan logos (profile pictures, clan logos on capes) and keeps them for an hour.
/// </summary>
/// <remarks>
/// <para>
/// Hardened after capes kept showing the Ravenfall logo (reported on stream, October 2026):
/// </para>
/// <list type="bullet">
/// <item>Logos are kept by their address. They were kept by user id, so a clan owner's profile picture and their clan
/// logo took each other's place, whichever came first.</item>
/// <item>A logo that could not be downloaded, or that the server only had a placeholder for (X-Logo-Fallback), is
/// tried again later, every cape waiting on it gets it when it arrives, and the plain image is only shown meanwhile.
/// A failed download used to leave the cape on the plain image until the player joined again.</item>
/// <item>One download per address at a time: everyone asking for it while it downloads waits for that one.</item>
/// </list>
/// </remarks>
public class PlayerLogoManager : MonoBehaviour
{
    //public const string TwitchLogoUrl = "https://www.ravenfall.stream/api/twitch/clan-logo/";

    private static readonly TimeSpan LifeSpan = TimeSpan.FromHours(1);
    private const float FirstRetry = 30f;
    private const float LongestRetry = 600f;
    // callbacks kept per address while it is retried; a cape asks again on every change of gear
    private const int MaxWaiting = 1000;

    private readonly Dictionary<string, Logo> logos = new Dictionary<string, Logo>();
    // callbacks waiting for an address that is downloading or will be tried again
    private readonly Dictionary<string, List<Action<Sprite>>> waiting = new Dictionary<string, List<Action<Sprite>>>();
    private readonly HashSet<string> downloading = new HashSet<string>();
    private readonly Dictionary<string, float> retryDelay = new Dictionary<string, float>();
    private readonly HashSet<string> reported = new HashSet<string>();

    [SerializeField] private Sprite replacementLogo;

    private GameManager gameManager;
    private const string logoPath = "players/logo/";
    private const string clanLogoPath = "clan/logo/";
    public string LogoUrl => ServerAddress + logoPath;
    public string ClanLogoUrl => ServerAddress + clanLogoPath;

    private string ServerAddress => gameManager?.ServerAddress ?? "https://www.ravenfall.stream/api/";

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    /// <summary>
    /// A clan's logo address as the game can download it: the clan's own when it has one (made absolute when the
    /// server gave a path), otherwise the owner's from the clan logo endpoint.
    /// </summary>
    public string ResolveClanLogoUrl(string clanLogo, string owner)
    {
        if (!string.IsNullOrWhiteSpace(clanLogo))
        {
            var logo = clanLogo.Trim();
            if (logo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || logo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return logo;
            }

            // a path on the server ("/api/twitch/logo/..."): the server's host in front
            if (Uri.TryCreate(ServerAddress, UriKind.Absolute, out var server) && Uri.TryCreate(server, logo, out var absolute))
            {
                return absolute.ToString();
            }
        }

        return string.IsNullOrWhiteSpace(owner) ? null : ClanLogoUrl + owner;
    }

    /// <summary>A clan's own cape image; the image id makes a new upload a new address, past the hour's cache.</summary>
    public string ClanCapeUrl(Guid clanId, Guid imageId) => ServerAddress + "clan/cape/" + clanId + "?v=" + imageId.ToString("N");

    // the default capes made so far, by the logo they were made from
    private readonly Dictionary<Texture, RenderTexture> defaultCapes = new Dictionary<Texture, RenderTexture>();
    private const int MaxDefaultCapes = 256;
    public const int CapeWidth = 512;
    public const int CapeHeight = 1024;
    // the server's cloth colour behind uploaded cape images, so both kinds of cape look alike
    private static readonly Color32 CapeCloth = new Color32(0x2B, 0x2A, 0x33, 0xFF);

    /// <summary>
    /// The cape for a clan without its own cape image: the logo on a cloth colour, in the 1:2 layout the clan capes
    /// show (docs/clan-images-design.md). The logo sits on the upper back, inside what even the short cape shows.
    /// </summary>
    public Texture DefaultCape(Sprite logo)
    {
        var source = logo ? logo.texture : (replacementLogo ? replacementLogo.texture : null);
        if (!source) return null;
        if (defaultCapes.TryGetValue(source, out var made) && made && made.IsCreated()) return made;

        if (defaultCapes.Count >= MaxDefaultCapes)
        {
            foreach (var old in defaultCapes.Values) if (old) old.Release();
            defaultCapes.Clear();
        }

        var cape = new RenderTexture(CapeWidth, CapeHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = "Default clan cape",
            useMipMap = true,
            autoGenerateMips = true,
            wrapMode = TextureWrapMode.Clamp,
        };
        cape.Create();

        var previous = RenderTexture.active;
        RenderTexture.active = cape;
        GL.Clear(true, true, QualitySettings.activeColorSpace == ColorSpace.Linear ? ((Color)CapeCloth).linear : (Color)CapeCloth);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, CapeWidth, CapeHeight, 0);
        // 384 px wide, centred, its middle 300 px down: v 0.52 to 0.89 of the cape
        const float size = 384f;
        Graphics.DrawTexture(new Rect((CapeWidth - size) / 2f, 300f - size / 2f, size, size), source);
        GL.PopMatrix();
        RenderTexture.active = previous;

        defaultCapes[source] = cape;
        return cape;
    }

    public void GetLogo(Guid userId, string url, Action<Sprite> onLogoDownloaded)
    {
        if (onLogoDownloaded == null)
            return;

        Get(string.IsNullOrWhiteSpace(url) ? LogoUrl + userId : url, onLogoDownloaded);
    }

    public void GetLogo(Guid userId, Action<Sprite> onLogoDownloaded)
    {
        if (onLogoDownloaded == null)
            return;

        Get(LogoUrl + userId, onLogoDownloaded);
    }

    public Sprite GetLogo(Guid raiderUserId)
    {
        if (gameManager && gameManager.LogoCensor)
            return replacementLogo;

        var url = LogoUrl + raiderUserId;
        if (TryGetLogo(url, out var logo))
            return logo.Sprite;

        Get(url, null);
        return null;
    }

    private void Get(string url, Action<Sprite> onLogoDownloaded)
    {
        if (gameManager && gameManager.LogoCensor)
        {
            onLogoDownloaded?.Invoke(replacementLogo);
            return;
        }

        if (TryGetLogo(url, out var logo))
        {
            onLogoDownloaded?.Invoke(logo.Sprite);
            // a placeholder for now: the real logo follows when the retry gets it
            if (logo.Placeholder) Wait(url, onLogoDownloaded);
            return;
        }

        Wait(url, onLogoDownloaded);

        // waiting for a retry: the plain image until then
        if (retryDelay.ContainsKey(url))
        {
            onLogoDownloaded?.Invoke(replacementLogo);
            return;
        }

        if (downloading.Add(url))
        {
            StartCoroutine(Download(url));
        }
    }

    private void Wait(string url, Action<Sprite> callback)
    {
        if (callback == null) return;
        if (!waiting.TryGetValue(url, out var list)) waiting[url] = list = new List<Action<Sprite>>();
        if (list.Count >= MaxWaiting) list.RemoveAt(0);
        list.Add(callback);
    }

    internal async void ClearCache()
    {
        if (!gameManager) return;

        if (gameManager.RavenNest.Authenticated)
        {
            await gameManager.RavenNest.ClearLogoAsync(gameManager.RavenNest.TwitchUserId);
        }

        logos.Clear();
        retryDelay.Clear();

        foreach (var player in this.gameManager.Players.GetAllPlayers())
        {
            player.Appearance.UpdateClanCape();
        }
    }

    private bool TryGetLogo(string url, out Logo logo)
    {
        if (url != null && logos.TryGetValue(url, out var found) && found != null && !found.Expired && found.Sprite != null)
        {
            logo = found;
            return true;
        }

        logo = null;
        return false;
    }

    private IEnumerator Download(string url)
    {
        Sprite sprite = null;
        var placeholder = false;
        string error = null;

        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (IsError(www.result))
            {
                error = www.error;
            }
            else
            {
                try
                {
                    var texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    sprite = SpriteFromTexture2D(texture);
                    placeholder = string.Equals(www.GetResponseHeader("X-Logo-Fallback"), "true", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception exc)
                {
                    error = exc.Message;
                }
            }
        }

        downloading.Remove(url);

        if (sprite != null)
        {
            // a placeholder is shown, but asked for again later: the real logo may be there by then
            logos[url] = new Logo(sprite, placeholder ? TimeSpan.FromSeconds(NextDelay(url)) : LifeSpan, placeholder);
            if (placeholder)
            {
                ScheduleRetry(url, "the server has no logo yet");
            }
            else
            {
                retryDelay.Remove(url);
            }

            Deliver(url, sprite, keepWaiting: placeholder);
            yield break;
        }

        ScheduleRetry(url, error);
        Deliver(url, replacementLogo, keepWaiting: true);
    }

    private float NextDelay(string url)
    {
        return retryDelay.TryGetValue(url, out var delay) ? delay : FirstRetry;
    }

    private void ScheduleRetry(string url, string reason)
    {
        var delay = NextDelay(url);
        retryDelay[url] = Mathf.Min(delay * 2f, LongestRetry);
        if (reported.Add(url))
        {
            Shinobytes.Debug.LogWarning("Could not get the logo " + url + " (" + reason + "); the plain image is shown and it is tried again in " + delay + " s.");
        }

        StartCoroutine(RetryAfter(url, delay));
    }

    private IEnumerator RetryAfter(string url, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (!waiting.ContainsKey(url) || downloading.Contains(url))
        {
            retryDelay.Remove(url);
            yield break; // nobody needs it any more, or it is already coming
        }

        downloading.Add(url);
        yield return Download(url);
    }

    /// <summary>Gives the sprite to everyone waiting for the address; they keep waiting when a better one may follow.</summary>
    private void Deliver(string url, Sprite sprite, bool keepWaiting)
    {
        if (!waiting.TryGetValue(url, out var list))
        {
            return;
        }

        if (!keepWaiting)
        {
            waiting.Remove(url);
        }

        foreach (var callback in list.ToArray())
        {
            try
            {
                callback(sprite);
            }
            catch (Exception)
            {
                // its window or player is gone: it does not get the next one either
                list.Remove(callback);
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
        public Logo(Sprite sprite, TimeSpan lifeSpan, bool placeholder = false)
        {
            Sprite = sprite;
            Placeholder = placeholder;
            Expires = DateTime.UtcNow.Add(lifeSpan);
        }
        public Sprite Sprite { get; }
        /// <summary>The server's stand-in: shown, but asked for again.</summary>
        public bool Placeholder { get; }
        public DateTime Expires { get; }
        public bool Expired => DateTime.UtcNow > Expires;
    }
}
