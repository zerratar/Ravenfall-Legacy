using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Applies the player's settings to the running game. The settings window and
/// <see cref="GameManager"/>'s load both go through here, so a setting does the same thing however
/// it was changed. Every setter writes <see cref="PlayerSettings"/> at once; <see cref="Save"/>
/// writes the file, which the settings window does when it closes.
/// </summary>
public static class SettingsApplier
{
    /// <summary>
    /// The bot messages a streamer can switch off, as the keys <see cref="GameManager.HasMessageFilter"/>
    /// checks. A key the game has never been told about counts as filtered, so every one of these is
    /// set on load. The old menu also listed DungeonJoin and RaidJoin, which nothing checks.
    /// </summary>
    public static readonly string[] BotMessages =
    {
        "ExpMultiplierExpire", "PlayerWelcome", "Disembark", "TrainWarning", "DungeonResult", "RaidResult", "ItemDrop"
    };

    /// <summary>How long players are kept in the saved game state, by the expiry setting's index.</summary>
    public static readonly TimeSpan[] PlayerCacheExpiry =
    {
        TimeSpan.Zero,
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(4),
        TimeSpan.FromHours(8),
        TimeSpan.FromHours(12),
        TimeSpan.FromHours(24),
        TimeSpan.FromHours(36),
        TimeSpan.FromHours(48),
        TimeSpan.FromDays(9999), // forever
    };

    /// <summary>Pathfinding iterations per frame for Low, Normal, High and Ultra navigation quality.</summary>
    public static readonly int[] PathfindingQualityMin = { 50, 100, 100, 100 };
    public static readonly int[] PathfindingQualityMax = { 100, 200, 500, 1000 };

    public static TimeSpan GetPlayerCacheExpiryTime() => PlayerCacheExpiry[Mathf.Clamp(PlayerStateExpiry, 0, PlayerCacheExpiry.Length - 1)];

    public static void Save() => PlayerSettings.Save();

    /// <summary>
    /// What a fresh install has, for the reset buttons. The two volumes come from the scene's audio
    /// sources, so they are read in <see cref="CaptureDefaults"/> before the saved settings apply.
    /// </summary>
    public static class Defaults
    {
        public static float MusicVolume = 0.5f;
        public static float EventVolume = 1f;
        public const float CameraRotation = -5f;     // OrbitCamera.RotationSpeed 5, shown with the slider's sign
        public const int BoostRequirement = 0;
        public static int PlayerStateExpiry => PlayerCacheExpiry.Length - 1;
        public const bool AlertExpiredState = false;
        public const int ItemDropMessages = (int)PlayerItemDropMessageSettings.OneItemPerRow;
        public const int PathfindingQuality = 1;
        public const bool AutoAssignHouses = true;
        public const bool StreamLabels = true;
        public const bool StreamLabelFiles = true;
        public const bool DisableRaids = false;
        public const bool DisableDungeons = false;
        public const int QualityLevel = 1;
        public const bool PotatoMode = false;
        public const bool AutoPotatoMode = false;
        public const bool PostProcessing = false;
        public const bool DayNightCycle = true;
        public const bool RealTimeDayNight = false;
        public const float TimeOfDay = 0.5f;
        public const float ViewDistance = 0.5f;
        public const float ResolutionScale = 1f;
        public const float UiScale = 1f;
        public const bool PlayerNames = true;
        public const bool PlayerList = true;
        public const bool PlayerDetails = true;
        public const bool ShortcutBar = true;
        public const bool BotMessage = true;
        public const bool DevTools = false;
        public const float ObserveSeconds = 10f;
        public const float ObserveSecondsSpecial = 30f;
        public const float IslandObserveSeconds = 10f;
        public const bool ObserveEmptyIslands = true;
        public const int AfkKick = 0;
        public const bool AnnounceNoExp = false;
        public const int NoExpMinutes = 1;           // index into NoExpMinuteOptions, 5 minutes
        public const bool LootOrigin = true;
        public const bool VSync = true;
        public const bool HideLogos = false;
        public const int ExpDisplay = 0;              // exp per hour
        public const bool GraphicsOff = false;
        public const bool Physics = false;            // physics has always started off
        public const int FrameRateCap = 0;
        public const int WindowMode = 0;              // borderless
        public const int Resolution = 0;              // the largest, the monitor's own
        public const float MasterVolume = 1f;
        public const float FreeCameraSpeed = 50f;
        public const float Sensitivity = 1f;
        public const bool InvertLook = false;
        public const bool InvertZoom = false;
        public const string BotServer = "ravenbot.ravenfall.stream:4041";
        public const bool LocalBot = true;
        public const int LocalBotPort = 4040;
        public const bool QueryEngine = true;
        public const string QueryEngineAddress = "localhost:8888/ravenfall/";
        /// <summary>TwitchEventManager's own: 5 min, 3 min, 1 min and when it ends.</summary>
        public static readonly bool[] ExpAnnouncements = { false, true, true, true, false, true };
    }

    /// <summary>Reads the defaults that live in the scene. Call before the saved settings are applied.</summary>
    public static void CaptureDefaults(GameManager game)
    {
        if (game.Music) Defaults.MusicVolume = game.Music.volume;
        if (game.Raid && game.Raid.Notifications) Defaults.EventVolume = game.Raid.Notifications.volume;
    }

    /// <summary>
    /// What the old settings menu used to apply on load on top of <c>GameManager.LoadGameSettings</c>:
    /// view distance, the horn volume for every event, player names and the bot message filters.
    /// </summary>
    public static void ApplyOnLoad(GameManager game)
    {
        var settings = PlayerSettings.Instance;
        ApplyViewDistance(game, settings.ViewDistance.GetValueOrDefault(0.5f));
        ApplyEventVolume(game, settings.RaidHornVolume.GetValueOrDefault(game.Raid.Notifications.volume));
        game.PlayerNamesVisible = settings.PlayerNamesVisible.GetValueOrDefault(game.PlayerNamesVisible);
        ApplyBotMessages(game);
        game.UpdatePathfindingIterations();
        ApplyScreen();
        ApplyFrameRate();
        AudioListener.volume = MasterVolume;
        ApplyCamera();
        ApplyExpAnnouncements();

        var s = PlayerSettings.Instance;
        game.LogoCensor = HideLogos;
        Shinobytes.UI.PlayerListWindow.ExpDisplay = ExpDisplay;
        if (s.PlayerDetailsVisible.HasValue) ApplyPlayerDetails(game, s.PlayerDetailsVisible.Value);
        if (GraphicsOff && game.Graphics && game.Graphics.IsGraphicsEnabled) game.Graphics.ToggleAllGraphics();
    }

    // ---- What the shortcut keys switch -----------------------------------------------------------
    // The keys and the settings page change the same saved setting, so the game starts the way the
    // streamer left it, whichever they used.

    public static bool HideLogos => PlayerSettings.Instance.HideLogos.GetValueOrDefault(Defaults.HideLogos);

    public static void SetHideLogos(GameManager game, bool value)
    {
        PlayerSettings.Instance.HideLogos = value;
        game.LogoCensor = value;
    }

    public static void ToggleHideLogos(GameManager game)
    {
        SetHideLogos(game, !game.LogoCensor);
        Save();
    }

    public static int ExpDisplay => Mathf.Clamp(PlayerSettings.Instance.PlayerListExpDisplay.GetValueOrDefault(Defaults.ExpDisplay), 0, 3);

    public static void SetExpDisplay(GameManager game, int value)
    {
        PlayerSettings.Instance.PlayerListExpDisplay = Mathf.Clamp(value, 0, 3);
        Shinobytes.UI.PlayerListWindow.ExpDisplay = ExpDisplay;
    }

    public static void CycleExpDisplay(GameManager game)
    {
        SetExpDisplay(game, (ExpDisplay + 1) % 4);
        // the old list, still behind the toolkit one, keeps its own cycle in step
        if (game.PlayerList) game.PlayerList.ToggleExpRate();
        Save();
    }

    public static void TogglePlayerDetails(GameManager game)
    {
        SetPlayerDetails(game, !PlayerDetails(game));
        Save();
    }

    private static void ApplyPlayerDetails(GameManager game, bool value)
    {
        if (game.Camera && game.Camera.Observer) game.Camera.Observer.SetVisibility(value);
    }

    public static bool GraphicsOff => PlayerSettings.Instance.GraphicsOff.GetValueOrDefault(Defaults.GraphicsOff);

    public static void SetGraphicsOff(GameManager game, bool off)
    {
        PlayerSettings.Instance.GraphicsOff = off;
        if (game.Graphics && game.Graphics.IsGraphicsEnabled == off) game.Graphics.ToggleAllGraphics();
    }

    public static void ToggleGraphics(GameManager game)
    {
        if (!game.Graphics) return;
        SetGraphicsOff(game, game.Graphics.IsGraphicsEnabled);
        Save();
    }

    public static bool Physics => PlayerSettings.Instance.SimulatePhysics.GetValueOrDefault(Defaults.Physics);

    public static void SetPhysics(bool on)
    {
        PlayerSettings.Instance.SimulatePhysics = on;
        if (on) GraphicsToggler.EnablePhysics();
        else GraphicsToggler.DisablePhysics();
    }

    public static void TogglePhysics()
    {
        SetPhysics(GraphicsToggler.SimulationMode == SimulationMode.Script);
        Save();
    }

    // ---- Display --------------------------------------------------------------------------------

    /// <summary>The window modes offered, in the order shown.</summary>
    public static readonly FullScreenMode[] WindowModes = { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed };

    public static int WindowMode
    {
        get
        {
            var mode = (FullScreenMode)PlayerSettings.Instance.WindowMode.GetValueOrDefault((int)Screen.fullScreenMode);
            if (mode == FullScreenMode.MaximizedWindow) mode = FullScreenMode.Windowed;
            var i = Array.IndexOf(WindowModes, mode);
            return i < 0 ? 0 : i;
        }
    }

    public static void SetWindowMode(int index)
    {
        PlayerSettings.Instance.WindowMode = (int)WindowModes[Mathf.Clamp(index, 0, WindowModes.Length - 1)];
        ApplyScreen();
    }

    private static Vector2Int[] resolutions;

    /// <summary>The monitor's sizes, largest first, each once whatever its refresh rates.</summary>
    public static Vector2Int[] Resolutions
    {
        get
        {
            if (resolutions == null || resolutions.Length == 0)
            {
                resolutions = Screen.resolutions
                    .Select(r => new Vector2Int(r.width, r.height))
                    .Distinct()
                    .OrderByDescending(r => r.x * r.y)
                    .ToArray();
                if (resolutions.Length == 0) resolutions = new[] { new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height) };
            }

            return resolutions;
        }
    }

    /// <summary>Index into <see cref="Resolutions"/>; 0, the largest, when nothing is chosen.</summary>
    public static int ResolutionIndex
    {
        get
        {
            var s = PlayerSettings.Instance;
            if (!s.ResolutionWidth.HasValue || !s.ResolutionHeight.HasValue) return 0;
            var i = Array.IndexOf(Resolutions, new Vector2Int(s.ResolutionWidth.Value, s.ResolutionHeight.Value));
            return i < 0 ? 0 : i;
        }
    }

    public static void SetResolutionIndex(int index)
    {
        var r = Resolutions[Mathf.Clamp(index, 0, Resolutions.Length - 1)];
        PlayerSettings.Instance.ResolutionWidth = r.x;
        PlayerSettings.Instance.ResolutionHeight = r.y;
        ApplyScreen();
    }

    /// <summary>
    /// Window mode and size, once either has been chosen; until then the window stays as Unity
    /// remembers it. Has no effect in the editor game view.
    /// </summary>
    public static void ApplyScreen()
    {
        var s = PlayerSettings.Instance;
        if (!s.WindowMode.HasValue && !s.ResolutionWidth.HasValue) return;
        var mode = (FullScreenMode)s.WindowMode.GetValueOrDefault((int)Screen.fullScreenMode);
        var size = s.ResolutionWidth.HasValue && s.ResolutionHeight.HasValue
            ? new Vector2Int(s.ResolutionWidth.Value, s.ResolutionHeight.Value)
            : Resolutions[0];
        if (Screen.width == size.x && Screen.height == size.y && Screen.fullScreenMode == mode) return;
        Screen.SetResolution(size.x, size.y, mode);
    }

    /// <summary>Frame rate limits offered; 0 is no limit.</summary>
    public static readonly int[] FrameRateCaps = { 0, 30, 60, 90, 120, 144, 165, 240 };

    public static bool VSync => PlayerSettings.Instance.VSync.GetValueOrDefault(Defaults.VSync);

    public static void SetVSync(bool value)
    {
        PlayerSettings.Instance.VSync = value;
        ApplyFrameRate();
    }

    public static int FrameRateCap
    {
        get
        {
            var i = Array.IndexOf(FrameRateCaps, PlayerSettings.Instance.FrameRateCap.GetValueOrDefault(0));
            return i < 0 ? 0 : i;
        }
    }

    public static void SetFrameRateCap(int index)
    {
        PlayerSettings.Instance.FrameRateCap = FrameRateCaps[Mathf.Clamp(index, 0, FrameRateCaps.Length - 1)];
        ApplyFrameRate();
    }

    /// <summary>
    /// Vertical sync and the frame rate limit. Every quality level turns vertical sync on by itself,
    /// so GameManager calls this again after each change of quality level (potato mode, focus).
    /// Unity ignores the frame rate limit while vertical sync is on.
    /// </summary>
    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        var cap = PlayerSettings.Instance.FrameRateCap.GetValueOrDefault(0);
        Application.targetFrameRate = !VSync && cap > 0 ? cap : -1;
    }

    // ---- Master volume --------------------------------------------------------------------------

    public static float MasterVolume => PlayerSettings.Instance.MasterVolume.GetValueOrDefault(Defaults.MasterVolume);

    public static void SetMasterVolume(float value)
    {
        PlayerSettings.Instance.MasterVolume = value;
        AudioListener.volume = value;
    }

    // ---- Camera ---------------------------------------------------------------------------------

    public static float FreeCameraSpeed => PlayerSettings.Instance.FreeCameraSpeed.GetValueOrDefault(Defaults.FreeCameraSpeed);
    public static float LookSensitivity => PlayerSettings.Instance.LookSensitivity.GetValueOrDefault(Defaults.Sensitivity);
    public static float ZoomSensitivity => PlayerSettings.Instance.ZoomSensitivity.GetValueOrDefault(Defaults.Sensitivity);
    public static bool InvertLook => PlayerSettings.Instance.InvertLook.GetValueOrDefault(Defaults.InvertLook);
    public static bool InvertZoom => PlayerSettings.Instance.InvertZoom.GetValueOrDefault(Defaults.InvertZoom);

    public static void SetFreeCameraSpeed(float value) { PlayerSettings.Instance.FreeCameraSpeed = value; ApplyCamera(); }
    public static void SetLookSensitivity(float value) { PlayerSettings.Instance.LookSensitivity = value; ApplyCamera(); }
    public static void SetZoomSensitivity(float value) { PlayerSettings.Instance.ZoomSensitivity = value; ApplyCamera(); }
    public static void SetInvertLook(bool value) { PlayerSettings.Instance.InvertLook = value; ApplyCamera(); }
    public static void SetInvertZoom(bool value) { PlayerSettings.Instance.InvertZoom = value; ApplyCamera(); }

    private static void ApplyCamera()
    {
        CameraSettings.FreeCameraSpeed = FreeCameraSpeed;
        CameraSettings.LookSensitivity = LookSensitivity;
        CameraSettings.ZoomSensitivity = ZoomSensitivity;
        CameraSettings.InvertLook = InvertLook;
        CameraSettings.InvertZoom = InvertZoom;
    }

    // ---- Connection (applies on reload) ---------------------------------------------------------

    public static string BotServer => PlayerSettings.Instance.RavenBotServer ?? string.Empty;

    /// <summary>Empty puts the default back.</summary>
    public static void SetBotServer(string value)
    {
        value = (value ?? string.Empty).Trim();
        PlayerSettings.Instance.RavenBotServer = value.Length == 0 ? Defaults.BotServer : value;
    }

    public static bool LocalBot => !PlayerSettings.Instance.LocalBotServerDisabled.GetValueOrDefault(false);
    public static void SetLocalBot(bool value) => PlayerSettings.Instance.LocalBotServerDisabled = !value;

    public static int LocalBotPort => PlayerSettings.Instance.LocalBotPort.GetValueOrDefault(Defaults.LocalBotPort);
    public static void SetLocalBotPort(int value) => PlayerSettings.Instance.LocalBotPort = Mathf.Clamp(value, 1, 65535);

    public static bool QueryEngine => PlayerSettings.Instance.QueryEngineEnabled.GetValueOrDefault(Defaults.QueryEngine);
    public static void SetQueryEngine(bool value) => PlayerSettings.Instance.QueryEngineEnabled = value;

    public static string QueryEngineAddress => PlayerSettings.Instance.QueryEngineApiPrefix ?? Defaults.QueryEngineAddress;

    public static void SetQueryEngineAddress(string value)
    {
        value = (value ?? string.Empty).Trim();
        PlayerSettings.Instance.QueryEngineApiPrefix = value.Length == 0 ? Defaults.QueryEngineAddress : value;
    }

    // ---- Exp multiplier announcements ------------------------------------------------------------

    /// <summary>The times the bot can warn at, in seconds left, matching the settings choices.</summary>
    public static readonly float[] ExpAnnouncementOptions = { 600f, 300f, 180f, 60f, 30f, 0.3f };

    public static bool ExpAnnouncement(int index)
    {
        var times = PlayerSettings.Instance.ExpMultiplierAnnouncements ?? TwitchEventManager.AnnouncementTimersSeconds;
        var t = ExpAnnouncementOptions[index];
        return times.Any(x => Mathf.Abs(x - t) < 0.01f);
    }

    public static void SetExpAnnouncement(int index, bool on)
    {
        var list = (PlayerSettings.Instance.ExpMultiplierAnnouncements ?? TwitchEventManager.AnnouncementTimersSeconds).ToList();
        var t = ExpAnnouncementOptions[index];
        list.RemoveAll(x => Mathf.Abs(x - t) < 0.01f);
        if (on) list.Add(t);
        // a new array, sorted from the most time left: the announcer walks it in that order
        PlayerSettings.Instance.ExpMultiplierAnnouncements = list.OrderByDescending(x => x).ToArray();
        ApplyExpAnnouncements();
    }

    private static void ApplyExpAnnouncements()
    {
        var times = PlayerSettings.Instance.ExpMultiplierAnnouncements;
        if (times != null) TwitchEventManager.AnnouncementTimersSeconds = times.OrderByDescending(x => x).ToArray();
    }

    // ---- Sound ----------------------------------------------------------------------------------

    public static float MusicVolume(GameManager game) => PlayerSettings.Instance.MusicVolume.GetValueOrDefault(game.Music.volume);

    public static void SetMusicVolume(GameManager game, float value)
    {
        PlayerSettings.Instance.MusicVolume = value;
        game.Music.volume = value;
    }

    public static float EventVolume(GameManager game) => PlayerSettings.Instance.RaidHornVolume.GetValueOrDefault(game.Raid.Notifications.volume);

    /// <summary>Raid horns, dungeon gongs and stream raid alerts share the one volume.</summary>
    public static void SetEventVolume(GameManager game, float value)
    {
        PlayerSettings.Instance.RaidHornVolume = value;
        ApplyEventVolume(game, value);
    }

    private static void ApplyEventVolume(GameManager game, float value)
    {
        if (game.StreamRaid && game.StreamRaid.Notifications) game.StreamRaid.Notifications.volume = value;
        if (game.Raid && game.Raid.Notifications) game.Raid.Notifications.volume = value;
        if (game.Dungeons && game.Dungeons.Notifications) game.Dungeons.Notifications.volume = value;
    }

    // ---- Graphics -------------------------------------------------------------------------------

    public static int QualityLevel => PlayerSettings.Instance.QualityLevel.GetValueOrDefault(1);

    /// <summary>GameManager applies the level every frame, so storing it is enough.</summary>
    public static void SetQualityLevel(int value) => PlayerSettings.Instance.QualityLevel = value;

    public static bool PotatoMode(GameManager game) => PlayerSettings.Instance.PotatoMode.GetValueOrDefault(game.PotatoMode && !game.AutoPotatoMode);

    public static void SetPotatoMode(GameManager game, bool value)
    {
        PlayerSettings.Instance.PotatoMode = value;
        game.PotatoMode = value;
    }

    public static bool AutoPotatoMode(GameManager game) => PlayerSettings.Instance.AutoPotatoMode.GetValueOrDefault(game.AutoPotatoMode);

    public static void SetAutoPotatoMode(GameManager game, bool value)
    {
        PlayerSettings.Instance.AutoPotatoMode = value;
        game.AutoPotatoMode = value;
    }

    public static bool PostProcessing(GameManager game) => PlayerSettings.Instance.PostProcessing.GetValueOrDefault(game.UsePostProcessingEffects);

    public static void SetPostProcessing(GameManager game, bool value)
    {
        PlayerSettings.Instance.PostProcessing = value;
        game.UsePostProcessingEffects = value;
    }

    public static bool DayNightCycle => PlayerSettings.Instance.DayNightCycleEnabled.GetValueOrDefault(true);

    public static void SetDayNightCycle(GameManager game, bool value)
    {
        PlayerSettings.Instance.DayNightCycleEnabled = value;
        game.DayNightCycleEnabled = value;
    }

    public static bool RealTimeDayNight(GameManager game) => PlayerSettings.Instance.RealTimeDayNightCycle.GetValueOrDefault(game.RealtimeDayNightCycle);

    public static void SetRealTimeDayNight(GameManager game, bool value)
    {
        PlayerSettings.Instance.RealTimeDayNightCycle = value;
        game.RealtimeDayNightCycle = value;
    }

    /// <summary>0 to 1; under 0.5 is day, the same split <see cref="DayNightCycle.IsNight"/> uses. Holds while the cycle is off.</summary>
    public static float TimeOfDay => PlayerSettings.Instance.DayNightTime.GetValueOrDefault(0.5f);

    public static void SetTimeOfDay(GameManager game, float value)
    {
        PlayerSettings.Instance.DayNightTime = value;
        game.DayNightCycleProgress = value;
    }

    public static float ViewDistance => PlayerSettings.Instance.ViewDistance.GetValueOrDefault(0.5f);

    public static void SetViewDistance(GameManager game, float value)
    {
        PlayerSettings.Instance.ViewDistance = value;
        ApplyViewDistance(game, value);
    }

    private static void ApplyViewDistance(GameManager game, float value)
    {
        var camera = game.Camera;
        if (camera) camera.FarClipDistance = Mathf.Lerp(camera.MinFarClipDistance, camera.MaxFarClipDistance, value);
    }

    /// <summary>Render resolution scaling only works on DirectX 12, so the setting is hidden elsewhere.</summary>
    public static bool ResolutionScaleSupported => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;

    public static float ResolutionScale => PlayerSettings.Instance.DPIScale.GetValueOrDefault(1f);

    public static void SetResolutionScale(float value)
    {
        PlayerSettings.Instance.DPIScale = value;
        ApplyResolutionScale(value);
    }

    public static void ApplyResolutionScale(float factor)
    {
        if (!ResolutionScaleSupported) return;
        factor = Mathf.Clamp(factor, 0.05f, 1f);
        QualitySettings.resolutionScalingFixedDPIFactor = factor;
        ScalableBufferManager.ResizeBuffers(factor, factor);
    }

    // ---- Interface ------------------------------------------------------------------------------

    public static bool PlayerNames(GameManager game) => PlayerSettings.Instance.PlayerNamesVisible.GetValueOrDefault(game.PlayerNamesVisible);

    public static void SetPlayerNames(GameManager game, bool value)
    {
        PlayerSettings.Instance.PlayerNamesVisible = value;
        game.PlayerNamesVisible = value;
    }

    public static bool PlayerList(GameManager game) => game.PlayerList ? game.PlayerList.IsVisible : PlayerSettings.Instance.PlayerListVisible.GetValueOrDefault(true);

    /// <summary>Through the HUD, so F1, the list's close button and this setting agree. The HUD saves it.</summary>
    public static void SetPlayerList(GameManager game, bool value)
    {
        if (HUDController.Instance) HUDController.Instance.SetPlayerListVisible(value);
        else if (game.PlayerList) game.PlayerList.SetVisibility(value);
    }

    public static bool PlayerDetails(GameManager game) => game.Camera && game.Camera.Observer && game.Camera.Observer.IsVisible;

    public static void SetPlayerDetails(GameManager game, bool value)
    {
        PlayerSettings.Instance.PlayerDetailsVisible = value;
        ApplyPlayerDetails(game, value);
    }

    public static bool ShortcutBar => PlayerSettings.Instance.ShortcutBarVisible.GetValueOrDefault(true);

    public static void SetShortcutBar(bool value) => Shinobytes.UI.ShortcutBarView.SetEnabled(value);

    // ---- Game -----------------------------------------------------------------------------------

    /// <summary>
    /// The slider value for the observer camera's rotation: negative turns left, positive right.
    /// The camera itself uses the opposite sign. The setting stores the camera's value, as
    /// GameManager reads it on load; the old menu stored both signs in turn, so it flipped
    /// direction every time the menu was closed.
    /// </summary>
    public static float CameraRotation => -PlayerSettings.Instance.CameraRotationSpeed.GetValueOrDefault(OrbitCamera.RotationSpeed);

    public static void SetCameraRotation(float sliderValue)
    {
        PlayerSettings.Instance.CameraRotationSpeed = -sliderValue;
        IslandObserveCamera.RotationSpeed = OrbitCamera.RotationSpeed = -sliderValue;
    }

    public static int BoostRequirement(GameManager game) => PlayerSettings.Instance.PlayerBoostRequirement.GetValueOrDefault(game.PlayerBoostRequirement);

    public static void SetBoostRequirement(GameManager game, int value)
    {
        PlayerSettings.Instance.PlayerBoostRequirement = value;
        game.PlayerBoostRequirement = value;
    }

    public static int PlayerStateExpiry => PlayerSettings.Instance.PlayerCacheExpiryTime.GetValueOrDefault(PlayerCacheExpiry.Length - 1);

    public static void SetPlayerStateExpiry(int value) => PlayerSettings.Instance.PlayerCacheExpiryTime = value;

    public static int ItemDropMessages(GameManager game) => PlayerSettings.Instance.ItemDropMessageType.GetValueOrDefault((int)game.ItemDropMessageSettings);

    public static void SetItemDropMessages(GameManager game, int value)
    {
        PlayerSettings.Instance.ItemDropMessageType = value;
        game.ItemDropMessageSettings = (PlayerItemDropMessageSettings)value;
    }

    public static int PathfindingQuality => PlayerSettings.Instance.PathfindingQualitySettings.GetValueOrDefault(1);

    public static void SetPathfindingQuality(GameManager game, int value)
    {
        PlayerSettings.Instance.PathfindingQualitySettings = value;
        game.UpdatePathfindingIterations();
    }

    public static bool AutoAssignHouses => PlayerSettings.Instance.AutoAssignVacantHouses.GetValueOrDefault(true);

    public static void SetAutoAssignHouses(bool value) => PlayerSettings.Instance.AutoAssignVacantHouses = value;

    public static bool StreamLabels => PlayerSettings.Instance.StreamLabels == null || PlayerSettings.Instance.StreamLabels.Enabled;

    public static void SetStreamLabels(bool value)
    {
        if (PlayerSettings.Instance.StreamLabels != null) PlayerSettings.Instance.StreamLabels.Enabled = value;
    }

    public static bool AlertExpiredState(GameManager game) => PlayerSettings.Instance.AlertExpiredStateCacheInChat.GetValueOrDefault(game.AlertExpiredStateCacheInChat);

    public static void SetAlertExpiredState(GameManager game, bool value)
    {
        PlayerSettings.Instance.AlertExpiredStateCacheInChat = value;
        game.AlertExpiredStateCacheInChat = value;
    }

    public static bool DisableRaids => PlayerSettings.Instance.DisableRaids.GetValueOrDefault();

    public static void SetDisableRaids(bool value) => PlayerSettings.Instance.DisableRaids = value;

    public static bool DisableDungeons => PlayerSettings.Instance.DisableDungeons.GetValueOrDefault();

    public static void SetDisableDungeons(bool value) => PlayerSettings.Instance.DisableDungeons = value;

    public static bool StreamLabelTextFiles => PlayerSettings.Instance.StreamLabels == null || PlayerSettings.Instance.StreamLabels.SaveTextFiles;

    public static void SetStreamLabelTextFiles(bool value)
    {
        if (PlayerSettings.Instance.StreamLabels != null) PlayerSettings.Instance.StreamLabels.SaveTextFiles = value;
    }

    public static bool StreamLabelJsonFiles => PlayerSettings.Instance.StreamLabels == null || PlayerSettings.Instance.StreamLabels.SaveJsonFiles;

    public static void SetStreamLabelJsonFiles(bool value)
    {
        if (PlayerSettings.Instance.StreamLabels != null) PlayerSettings.Instance.StreamLabels.SaveJsonFiles = value;
    }

    // ---- Observer camera ------------------------------------------------------------------------
    // GameCamera reads these each time it picks someone or somewhere to watch.

    private static PlayerSettings.ObserverTimes ObserveTimes =>
        PlayerSettings.Instance.PlayerObserveSeconds ?? (PlayerSettings.Instance.PlayerObserveSeconds = PlayerSettings.ObserverTimes.DefaultTimes);

    public static float ObserveSeconds => ObserveTimes.Default;
    public static void SetObserveSeconds(float value) => ObserveTimes.Default = value;
    public static float ObserveSubscriberSeconds => ObserveTimes.Subscriber;
    public static void SetObserveSubscriberSeconds(float value) => ObserveTimes.Subscriber = value;
    public static float ObserveVipSeconds => ObserveTimes.Vip;
    public static void SetObserveVipSeconds(float value) => ObserveTimes.Vip = value;
    public static float ObserveModeratorSeconds => ObserveTimes.Moderator;
    public static void SetObserveModeratorSeconds(float value) => ObserveTimes.Moderator = value;

    /// <summary>A sub or a cheer: the two events share the one time.</summary>
    public static float ObserveEventSeconds => Mathf.Max(ObserveTimes.OnSubcription, ObserveTimes.OnCheeredBits);

    public static void SetObserveEventSeconds(float value)
    {
        ObserveTimes.OnSubcription = value;
        ObserveTimes.OnCheeredBits = value;
    }

    public static float IslandObserveSeconds => PlayerSettings.Instance.IslandObserveSeconds > 0 ? PlayerSettings.Instance.IslandObserveSeconds : Defaults.IslandObserveSeconds;
    public static void SetIslandObserveSeconds(float value) => PlayerSettings.Instance.IslandObserveSeconds = value;
    public static bool ObserveEmptyIslands => PlayerSettings.Instance.CanObserveEmptyIslands.GetValueOrDefault(Defaults.ObserveEmptyIslands);
    public static void SetObserveEmptyIslands(bool value) => PlayerSettings.Instance.CanObserveEmptyIslands = value;

    // ---- Players --------------------------------------------------------------------------------

    /// <summary>Hours without a chat command before a player counts as away; index 0 is never.</summary>
    public static readonly double[] AfkHourOptions = { -1, 1, 2, 4, 8, 12, 24 };

    /// <summary>
    /// One choice for two settings: a player only counts as away when the hours are set, and is only
    /// kicked when kicking is on, so "never" switches both off.
    /// </summary>
    public static int AfkKick
    {
        get
        {
            var s = PlayerSettings.Instance;
            if (!s.AutoKickAfkPlayers.GetValueOrDefault() || !s.PlayerAfkHours.HasValue || s.PlayerAfkHours.Value <= 0) return 0;
            var best = 1;
            for (var i = 1; i < AfkHourOptions.Length; ++i)
                if (Math.Abs(AfkHourOptions[i] - s.PlayerAfkHours.Value) < Math.Abs(AfkHourOptions[best] - s.PlayerAfkHours.Value)) best = i;
            return best;
        }
    }

    public static void SetAfkKick(int index)
    {
        index = Mathf.Clamp(index, 0, AfkHourOptions.Length - 1);
        PlayerSettings.Instance.AutoKickAfkPlayers = index > 0;
        PlayerSettings.Instance.PlayerAfkHours = AfkHourOptions[index];
    }

    public static bool AnnounceNoExp => PlayerSettings.Instance.AnnounceNoExpGain.GetValueOrDefault(Defaults.AnnounceNoExp);
    public static void SetAnnounceNoExp(bool value) => PlayerSettings.Instance.AnnounceNoExpGain = value;

    /// <summary>Minutes without exp before the first warning; later ones wait five times as long.</summary>
    public static readonly int[] NoExpMinuteOptions = { 2, 5, 10, 15, 30 };

    public static int NoExpMinutes
    {
        get
        {
            var minutes = PlayerSettings.Instance.NoExpGainWarningMinutes.GetValueOrDefault(5);
            var best = 0;
            for (var i = 1; i < NoExpMinuteOptions.Length; ++i)
                if (Math.Abs(NoExpMinuteOptions[i] - minutes) < Math.Abs(NoExpMinuteOptions[best] - minutes)) best = i;
            return best;
        }
    }

    public static void SetNoExpMinutes(int index) => PlayerSettings.Instance.NoExpGainWarningMinutes = NoExpMinuteOptions[Mathf.Clamp(index, 0, NoExpMinuteOptions.Length - 1)];

    public static bool LootOrigin => PlayerSettings.Instance.Loot == null || PlayerSettings.Instance.Loot.IncludeOrigin;

    public static void SetLootOrigin(bool value)
    {
        if (PlayerSettings.Instance.Loot == null) PlayerSettings.Instance.Loot = PlayerSettings.LootSettings.Default;
        PlayerSettings.Instance.Loot.IncludeOrigin = value;
    }

    // ---- Bot messages ---------------------------------------------------------------------------

    /// <summary>The saved list holds the messages that are switched off.</summary>
    public static bool BotMessageEnabled(string key)
    {
        var off = PlayerSettings.Instance.BotMessageFilters;
        return off == null || !off.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static void SetBotMessageEnabled(GameManager game, string key, bool enabled)
    {
        var off = (PlayerSettings.Instance.BotMessageFilters ?? Array.Empty<string>())
            .Where(x => !x.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!enabled) off = off.Append(key);
        PlayerSettings.Instance.BotMessageFilters = off.ToArray();
        game.SetMessageFilter(key, enabled);
    }

    private static void ApplyBotMessages(GameManager game)
    {
        foreach (var key in BotMessages)
        {
            game.SetMessageFilter(key, BotMessageEnabled(key));
        }
    }

    // ---- Admin ----------------------------------------------------------------------------------

    public static bool DevTools(GameManager game) => game.isDebugMenuVisible;

    public static void SetDevTools(GameManager game, bool value) => game.isDebugMenuVisible = value;
}
