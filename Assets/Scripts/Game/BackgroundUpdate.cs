using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Downloads a new version while the stream goes on (the user, 2026-11-04: the stream sat on the update screen while
/// half a gigabyte came down), so Update now only restarts into the patcher. It starts when the server says a newer
/// version is out (GameManager.OnUpdateAvailable), with the setting on (on by default). The file lands where the
/// update screen would have put it (update/), noted as complete in update/ready.json; the update screen then starts the
/// patcher at once, or takes over a download still running (TakeOver). UpdateDownloadView shows it all in the bottom
/// left corner.
/// </summary>
public static class BackgroundUpdate
{
    public enum Phase { None, Downloading, Ready, Failed }

    /// <summary>What the download is doing. None also when the setting is off: the card then only offers Update now.</summary>
    public static Phase State { get; private set; }

    /// <summary>The version the server announced (and the one being downloaded).</summary>
    public static string Version { get; private set; }

    /// <summary>How far the download is, or null before it reports.</summary>
    public static DownloadProgress Progress => handler?.GetDownloadProgress();

    /// <summary>
    /// With "Install updates automatically" on (off by default), when the countdown to the restart runs out (unscaled
    /// time), or a negative number while none runs: it starts once the download is ready and nothing is under way.
    /// </summary>
    public static float AutoUpdateAt { get; private set; } = -1f;

    /// <summary>The automatic restart waits for a raid, a dungeon, Ravebridge, the regatta or the pirates to finish.</summary>
    public static bool AutoUpdateWaiting { get; private set; }

    /// <summary>The warning chat gets before the automatic restart.</summary>
    public const float AutoUpdateWarning = 60f;

    private static string autoCancelledFor;

    /// <summary>Lets the editor try it (downloads into the project folder's update/); off otherwise in the editor.</summary>
    public static bool AllowInEditor;

    private static GameUpdateHandler handler;
    private static Task<bool> download;

    /// <summary>The server says a newer version is out.</summary>
    public static void Announced(string version)
    {
        if (string.IsNullOrEmpty(version)) return;
        if (Version != version && State != Phase.Downloading) State = Phase.None;
        Version = version;
        if (SettingsApplier.DownloadUpdatesEarly) Begin();
    }

    /// <summary>The setting was turned on with an update already announced: start now.</summary>
    public static void SettingTurnedOn()
    {
        if (!string.IsNullOrEmpty(Version) && (State == Phase.None || State == Phase.Failed)) Begin();
    }

    /// <summary>
    /// The automatic restart, checked by the card a few times a second: once the download is ready and nothing is
    /// under way, chat is told and a minute later the game restarts into the update.
    /// </summary>
    public static void Tick()
    {
        var auto = State == Phase.Ready && SettingsApplier.InstallUpdatesAutomatically && autoCancelledFor != Version;
        if (!auto)
        {
            AutoUpdateAt = -1f;
            AutoUpdateWaiting = false;
            return;
        }

        if (AutoUpdateAt < 0f)
        {
            AutoUpdateWaiting = Busy();
            if (AutoUpdateWaiting) return;
            AutoUpdateAt = Time.unscaledTime + AutoUpdateWarning;
            var gm = GameManager.Instance;
            if (gm && gm.RavenBot != null) gm.RavenBot.Announce(Localization.MSG_UPDATE_AUTO_RESTART, Version);
            return;
        }

        if (Time.unscaledTime >= AutoUpdateAt)
        {
            AutoUpdateAt = -1f;
            autoCancelledFor = Version; // once: should the restart not happen, it is not tried again
            UpdateNow();
        }
    }

    /// <summary>Not now: no automatic restart for this version (Update now still works).</summary>
    public static void CancelAuto()
    {
        autoCancelledFor = Version;
        AutoUpdateAt = -1f;
    }

    /// <summary>Something chat is in the middle of, which a restart would cut short.</summary>
    private static bool Busy()
    {
        var gm = GameManager.Instance;
        if (!gm) return true;
        if (gm.Raid && gm.Raid.IsEventActive) return true;
        if (gm.Dungeons && gm.Dungeons.Active) return true;
        if (RavebridgeManager.Instance && RavebridgeManager.Instance.Active) return true;
        return RavebridgeReveal.Playing || WaveRun.Current != null || Regatta.Running || PirateRaid.Running;
    }

    /// <summary>
    /// The stream was first on the newest version, or an Early Bird (UpdateHonourEventHandler): the card celebrates it
    /// for a while. Null once that is over.
    /// </summary>
    public static RavenNest.Models.UpdateHonour Honour => honour != null && Time.unscaledTime < honourUntil ? honour : null;

    private static RavenNest.Models.UpdateHonour honour;
    private static float honourUntil;
    private const float HonourSeconds = 90f;

    public static void Honoured(RavenNest.Models.UpdateHonour data)
    {
        honour = data;
        honourUntil = Time.unscaledTime + HonourSeconds;
    }

    /// <summary>A failed download, tried again from the card.</summary>
    public static void Retry()
    {
        if (State == Phase.Failed) Begin();
    }

    /// <summary>
    /// Restarts into the update: the players are saved and the update screen opens, which starts the patcher at once
    /// when the download is done (or finishes it, or downloads it there).
    /// </summary>
    public static void UpdateNow()
    {
        var gm = GameManager.Instance;
        if (gm) gm.UpdateGame();
    }

    /// <summary>
    /// For the update screen: a download still running in the background, with its handler for the progress; it
    /// finishes there. Null when none runs.
    /// </summary>
    public static (GameUpdateHandler Handler, Task<bool> Download) TakeOver()
    {
        if (State != Phase.Downloading || download == null) return (null, null);
        return (handler, download);
    }

    private static void Begin()
    {
        if (State == Phase.Downloading || State == Phase.Ready) return;
        if (Application.isEditor && !AllowInEditor) return;
        State = Phase.Downloading;
        download = Run();
    }

    private static async Task<bool> Run()
    {
        try
        {
            handler = new GameUpdateHandler(UpdateHost());
            var update = await handler.DownloadUpdateInfoAsync();
            if (update == null || update.GetVersion() <= GameVersion.GetApplicationVersion())
            {
                // nothing newer after all, or the server could not be asked: the card falls back to Update now
                State = update == null ? Phase.Failed : Phase.None;
                return false;
            }

            Version = update.Version;
            if (handler.IsDownloaded(update) || await handler.DownloadAsync(update))
            {
                State = Phase.Ready;
                Shinobytes.Debug.Log("Update " + update.Version + " is downloaded and ready.");
                return true;
            }

            State = Phase.Failed;
            return false;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The background update download failed: " + exc.Message);
            State = Phase.Failed;
            return false;
        }
    }

    /// <summary>The server the game signed in to (the local one with -localserver), as the update screen asks it.</summary>
    private static string UpdateHost()
    {
        var api = GameManager.Instance && GameManager.Instance.RavenNest != null ? GameManager.Instance.RavenNest.ServerAddress : null;
        if (string.IsNullOrEmpty(api)) return "https://www.ravenfall.stream/";
        api = api.TrimEnd('/');
        if (api.EndsWith("/api", StringComparison.OrdinalIgnoreCase)) api = api.Substring(0, api.Length - 4);
        return api + "/";
    }
}
