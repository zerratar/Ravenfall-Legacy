using System.IO;

public class PlayerSettings
{
    private readonly static JsonStore<PlayerSettings> store;
    public readonly static PlayerSettings Instance;

    public bool? PotatoMode;
    public bool? AutoPotatoMode;
    public bool? PostProcessing;
    public bool? RealTimeDayNightCycle;
    public bool? AlertExpiredStateCacheInChat;
    public bool? PlayerListVisible;
    public bool? ShortcutBarVisible;
    public bool? PlayerNamesVisible;
    public bool? LocalBotServerDisabled;
    public bool? AutoKickAfkPlayers;
    public bool? DayNightCycleEnabled;
    public bool? PhysicsEnabled;
    public bool? EnableIslandCameraOnStart;
    public bool? CanObserveEmptyIslands;


    public float? DayNightTime;
    public float? PlayerListSize;
    public float? PlayerListScale;
    public float? PlayerListSpeed;
    public float? RaidHornVolume;
    public float? MusicVolume;
    public float? DPIScale;
    /// <summary>Scale of all 2D interface, 1 for the default size. See Shinobytes.UI.UiScale.</summary>
    public float? UIScale;
    public float? ViewDistance;
    public bool? GrassEnabled;
    public float? GrassDistance;
    public float? GrassDensity;
    public bool? TiltShiftEnabled;
    public float? TiltShiftStrength;
    public float? TiltShiftBand;
    public float? TiltShiftSoftness;
    public bool? CloudShadowsEnabled;
    public bool? DetailedWater;
    public bool? VolumetricClouds;
    public bool? AmbientOcclusion;
    public float? AmbientOcclusionStrength;
    public bool? AmbientEffects;
    public bool? TreeWind;
    public bool? DungeonLighting;
    public float? NightBrightness;
    public bool? AtmosphericHaze;
    public bool? ChangingWeather;
    public bool? LightningFlashes;
    public float? WeatherVolume;
    public float? SoundEffectsVolume;
    public bool? Bloom;
    public float? CameraRotationSpeed;

    /// <summary>A UnityEngine.FullScreenMode value; null leaves the window as Unity last had it.</summary>
    public int? WindowMode;
    /// <summary>Null keeps the screen's native resolution.</summary>
    public int? ResolutionWidth;
    public int? ResolutionHeight;
    /// <summary>Frames per second to stop at; 0 or null for no limit.</summary>
    public int? FrameRateCap;
    public bool? VSync;
    /// <summary>Everything the game plays, through the audio listener, 0 to 1.</summary>
    public float? MasterVolume;
    /// <summary>Free camera base speed in metres per second.</summary>
    public float? FreeCameraSpeed;
    /// <summary>Multipliers on the cameras' own speeds, 1 for as they come.</summary>
    public float? LookSensitivity;
    public float? ZoomSensitivity;
    public bool? InvertLook;
    public bool? InvertZoom;
    /// <summary>Rebound keys as "action=binding", only the ones that differ from the defaults. See Keybinds.</summary>
    public string[] KeyBindings;
    /// <summary>What the player list rows show on the right: 0 exp per hour, 1 exp left, 2 time to level, 3 nothing.</summary>
    public int? PlayerListExpDisplay;
    public bool? HideLogos;
    /// <summary>The logo key moved off Q (0.9.5.1) and logos hidden by flying the camera down were shown again, once.</summary>
    public bool? LogoKeyMoved;
    public bool? PlayerDetailsVisible;
    /// <summary>The world drawn or a black screen (F9).</summary>
    public bool? GraphicsOff;
    /// <summary>
    /// Physics simulation (F4). The older PhysicsEnabled defaulted to true but was never honoured:
    /// physics always started off. A new field keeps that, rather than switching physics on for
    /// everyone whose file says true.
    /// </summary>
    public bool? SimulatePhysics;
    /// <summary>The language code, see Languages; null or "en" for English.</summary>
    public string Language;

    public int? LocalBotPort;
    public int? PlayerBoostRequirement;
    public int? PlayerCacheExpiryTime;
    /// <summary>Players the server remembers but the state file did not have come back without asking (ServerPlayerRestore).</summary>
    public bool? AlwaysRestoreFromServer;
    public int? ItemDropMessageType;
    public int? PathfindingQualitySettings;
    public int? QualityLevel;
    public int? NoExpGainWarningMinutes;

    public double? PlayerAfkHours;
    public string RavenBotServer;
    public string[] BotMessageFilters;

    public bool? DisableDungeons;
    public bool? DisableRaids;
    public bool? RequireJoinCode;
    public bool? AutoAssignVacantHouses;
    public bool? AnnounceNoExpGain;

    public bool? QueryEngineEnabled;
    public bool? QueryEngineAlwaysReturnArray;
    public string QueryEngineApiPrefix;
    public float[] ExpMultiplierAnnouncements;

    public StreamLabelSettings StreamLabels;
    public ObserverTimes PlayerObserveSeconds;
    public float IslandObserveSeconds;
    public LootSettings Loot;
    public class LootSettings
    {
        public bool IncludeOrigin;

        public static LootSettings Default
        {
            get
            {
                return new LootSettings
                {
                    IncludeOrigin = true
                };
            }
        }
    }

    public class StreamLabelSettings
    {
        public bool Enabled;
        public bool SaveJsonFiles;
        public bool SaveTextFiles;

        public static StreamLabelSettings Default
        {
            get
            {
                return new StreamLabelSettings
                {
                    Enabled = true,
                    SaveJsonFiles = true,
                    SaveTextFiles = true,
                };
            }
        }
    }

    public class ObserverTimes
    {
        public float Default;
        public float Subscriber;
        public float Moderator;
        public float Vip;

        public float Broadcaster;
        public float OnSubcription;
        public float OnCheeredBits;

        public static ObserverTimes DefaultTimes
        {
            get
            {
                return new ObserverTimes
                {
                    Default = 10f,
                    Subscriber = 30f,
                    Moderator = 30f,
                    Vip = 30,
                    Broadcaster = 30,
                    OnCheeredBits = 30,
                    OnSubcription = 30,
                };
            }
        }
    }


    static PlayerSettings()
    {
        store = JsonStore<PlayerSettings>.Create("game-settings", new PlayerSettingsJsonSerializer());
        Instance = store.Get() ?? new PlayerSettings();
        SetDefaultValues();
    }

    public static void Save() => store.Save(Instance);

    private static void SetDefaultValues()
    {
        var wasUpdated = false;

        if (Instance.BotMessageFilters == null)
        {
            Instance.BotMessageFilters = new string[0];
            wasUpdated = true;
        }

        if (Instance.AnnounceNoExpGain == null)
        {
            Instance.AnnounceNoExpGain = false;
            wasUpdated = true;
        }

        if (Instance.NoExpGainWarningMinutes == null)
        {
            Instance.NoExpGainWarningMinutes = 5;
            wasUpdated = true;
        }

        if (Instance.CanObserveEmptyIslands == null)
        {
            Instance.CanObserveEmptyIslands = true;
            wasUpdated = true;
        }

        if (Instance.EnableIslandCameraOnStart == null)
        {
            Instance.EnableIslandCameraOnStart = false;
            wasUpdated = true;
        }

        if (Instance.DayNightCycleEnabled == null)
        {
            Instance.DayNightCycleEnabled = true;
            wasUpdated = true;
        }

        if (Instance.QualityLevel == null)
        {
            Instance.QualityLevel = 1;
            wasUpdated = true;
        }

        if (Instance.DayNightTime == null)
        {
            Instance.DayNightTime = 0.5f;
            wasUpdated = true;
        }

        if (Instance.LocalBotServerDisabled == null)
        {
            Instance.LocalBotServerDisabled = false;
            wasUpdated = true;
        }

        if (Instance.AutoAssignVacantHouses == null)
        {
            Instance.AutoAssignVacantHouses = true;
            wasUpdated = true;
        }

        if (Instance.LocalBotPort == null)
        {
            Instance.LocalBotPort = RavenBotConnection.DefaultLocalBotServerPort;
            wasUpdated = true;
        }

        if (Instance.PhysicsEnabled == null)
        {
            Instance.PhysicsEnabled = true;
            wasUpdated = true;
        }

        if (Instance.DisableDungeons == null)
        {
            Instance.DisableDungeons = false;
            wasUpdated = true;
        }

        if (Instance.DisableRaids == null)
        {
            Instance.DisableRaids = false;
            wasUpdated = true;
        }

        if (Instance.RavenBotServer == null)
        {
            Instance.RavenBotServer = "ravenbot.ravenfall.stream:4041";
            wasUpdated = true;
        }

        if (Instance.Loot == null)
        {
            Instance.Loot = LootSettings.Default;
            wasUpdated = true;
        }

        if (Instance.StreamLabels == null)
        {
            Instance.StreamLabels = StreamLabelSettings.Default;
            wasUpdated = true;
        }

        if (Instance.QueryEngineAlwaysReturnArray == null)
        {
            Instance.QueryEngineAlwaysReturnArray = false;
            wasUpdated = true;
        }

        if (Instance.QueryEngineEnabled == null)
        {
            Instance.QueryEngineEnabled = true;
            wasUpdated = true;
        }

        if (Instance.QueryEngineApiPrefix == null)
        {
            Instance.QueryEngineApiPrefix = "localhost:8888/ravenfall/";
            wasUpdated = true;
        }

        if (Instance.PlayerObserveSeconds == null)
        {
            Instance.PlayerObserveSeconds = ObserverTimes.DefaultTimes;
            wasUpdated = true;
        }

        if (Instance.IslandObserveSeconds == 0)
        {
            Instance.IslandObserveSeconds = 10f;
            wasUpdated = true;
        }

        if (Instance.CameraRotationSpeed == null)
        {
            Instance.CameraRotationSpeed = OrbitCamera.RotationSpeed;
            wasUpdated = true;
        }

        if (Instance.ShortcutBarVisible == null)
        {
            Instance.ShortcutBarVisible = true;
        }

        if (Instance.PlayerListVisible == null)
        {
            Instance.PlayerListVisible = true;
            wasUpdated = true;
        }

        if (Instance.PlayerAfkHours == null)
        {
            Instance.PlayerAfkHours = -1;
            wasUpdated = true;
        }

        if (Instance.ViewDistance == null)
        {
            Instance.ViewDistance = 0.5f;
            wasUpdated = true;
        }

        if (Instance.AutoKickAfkPlayers == null)
        {
            Instance.AutoKickAfkPlayers = false;
            wasUpdated = true;
        }

        if (Instance.ExpMultiplierAnnouncements == null || Instance.ExpMultiplierAnnouncements.Length == 0)
        {
            Instance.ExpMultiplierAnnouncements = TwitchEventManager.AnnouncementTimersSeconds;
            wasUpdated = true;
        }

        if (wasUpdated)
        {
            Save();
        }
    }

    public class PlayerSettingsJsonSerializer : ObjectJsonSerializer<PlayerSettings>
    {
        public override string Serialize(PlayerSettings obj)
        {
            StringWriter sw = new StringWriter();

            sw.WriteLine("{");

            // Serialize all nullable boolean properties
            Serialize(sw, "potatoMode", obj.PotatoMode);
            Serialize(sw, "autoPotatoMode", obj.AutoPotatoMode);
            Serialize(sw, "postProcessing", obj.PostProcessing);
            Serialize(sw, "realTimeDayNightCycle", obj.RealTimeDayNightCycle);
            Serialize(sw, "alertExpiredStateCacheInChat", obj.AlertExpiredStateCacheInChat);
            Serialize(sw, "playerListVisible", obj.PlayerListVisible);
            Serialize(sw, "shortcutBarVisible", obj.ShortcutBarVisible);
            Serialize(sw, "playerNamesVisible", obj.PlayerNamesVisible);

            Serialize(sw, "autoKickAfkPlayers", obj.AutoKickAfkPlayers);
            Serialize(sw, "dayNightCycleEnabled", obj.DayNightCycleEnabled);
            Serialize(sw, "queryEngineEnabled", obj.QueryEngineEnabled);
            Serialize(sw, "queryEngineAlwaysReturnArray", obj.QueryEngineAlwaysReturnArray);
            Serialize(sw, "physicsEnabled", obj.PhysicsEnabled);

            Serialize(sw, "disableRaids", obj.DisableRaids);
            Serialize(sw, "requireJoinCode", obj.RequireJoinCode);
            Serialize(sw, "disableDungeons", obj.DisableDungeons);
            Serialize(sw, "autoAssignVacantHouses", obj.AutoAssignVacantHouses);
            Serialize(sw, "enableIslandCameraOnStart", obj.EnableIslandCameraOnStart);

            Serialize(sw, "noExpGainWarningMinutes", obj.NoExpGainWarningMinutes);
            Serialize(sw, "qualityLevel", obj.QualityLevel);
            Serialize(sw, "islandObserveSeconds", obj.IslandObserveSeconds);
            Serialize(sw, "canObserveEmptyIslands", obj.CanObserveEmptyIslands);
            Serialize(sw, "announceNoExpGain", obj.AnnounceNoExpGain);

            // Serialize all nullable float properties
            Serialize(sw, "dayNightTime", obj.DayNightTime);
            Serialize(sw, "playerListSize", obj.PlayerListSize);
            Serialize(sw, "playerListScale", obj.PlayerListScale);
            Serialize(sw, "playerListSpeed", obj.PlayerListSpeed);
            Serialize(sw, "raidHornVolume", obj.RaidHornVolume);
            Serialize(sw, "musicVolume", obj.MusicVolume);
            Serialize(sw, "dpiScale", obj.DPIScale);
            Serialize(sw, "uiScale", obj.UIScale);
            Serialize(sw, "viewDistance", obj.ViewDistance);
            Serialize(sw, "grassEnabled", obj.GrassEnabled);
            Serialize(sw, "grassDistance", obj.GrassDistance);
            Serialize(sw, "grassDensity", obj.GrassDensity);
            Serialize(sw, "tiltShiftEnabled", obj.TiltShiftEnabled);
            Serialize(sw, "tiltShiftStrength", obj.TiltShiftStrength);
            Serialize(sw, "tiltShiftBand", obj.TiltShiftBand);
            Serialize(sw, "tiltShiftSoftness", obj.TiltShiftSoftness);
            Serialize(sw, "cloudShadowsEnabled", obj.CloudShadowsEnabled);
            Serialize(sw, "detailedWater", obj.DetailedWater);
            Serialize(sw, "volumetricClouds", obj.VolumetricClouds);
            Serialize(sw, "ambientOcclusion", obj.AmbientOcclusion);
            Serialize(sw, "ambientOcclusionStrength", obj.AmbientOcclusionStrength);
            Serialize(sw, "ambientEffects", obj.AmbientEffects);
            Serialize(sw, "treeWind", obj.TreeWind);
            Serialize(sw, "dungeonLighting", obj.DungeonLighting);
            Serialize(sw, "nightBrightness", obj.NightBrightness);
            Serialize(sw, "atmosphericHaze", obj.AtmosphericHaze);
            Serialize(sw, "changingWeather", obj.ChangingWeather);
            Serialize(sw, "lightningFlashes", obj.LightningFlashes);
            Serialize(sw, "weatherVolume", obj.WeatherVolume);
            Serialize(sw, "soundEffectsVolume", obj.SoundEffectsVolume);
            Serialize(sw, "bloom", obj.Bloom);
            Serialize(sw, "cameraRotationSpeed", obj.CameraRotationSpeed);
            Serialize(sw, "masterVolume", obj.MasterVolume);
            Serialize(sw, "freeCameraSpeed", obj.FreeCameraSpeed);
            Serialize(sw, "lookSensitivity", obj.LookSensitivity);
            Serialize(sw, "zoomSensitivity", obj.ZoomSensitivity);
            Serialize(sw, "invertLook", obj.InvertLook);
            Serialize(sw, "invertZoom", obj.InvertZoom);
            Serialize(sw, "vSync", obj.VSync);
            Serialize(sw, "windowMode", obj.WindowMode);
            Serialize(sw, "resolutionWidth", obj.ResolutionWidth);
            Serialize(sw, "resolutionHeight", obj.ResolutionHeight);
            Serialize(sw, "frameRateCap", obj.FrameRateCap);

            // Serialize all nullable int properties
            Serialize(sw, "playerBoostRequirement", obj.PlayerBoostRequirement);
            Serialize(sw, "playerCacheExpiryTime", obj.PlayerCacheExpiryTime);
            Serialize(sw, "alwaysRestoreFromServer", obj.AlwaysRestoreFromServer);
            Serialize(sw, "itemDropMessageType", obj.ItemDropMessageType);
            Serialize(sw, "pathfindingQualitySettings", obj.PathfindingQualitySettings);

            Serialize(sw, "localBotServerDisabled", obj.LocalBotServerDisabled);
            Serialize(sw, "localBotPort", obj.LocalBotPort);

            // Serialize all nullable double properties
            Serialize(sw, "playerAfkHours", obj.PlayerAfkHours);

            // Serialize string properties

            Serialize(sw, "ravenBotServer", obj.RavenBotServer);
            Serialize(sw, "queryEngineApiPrefix", obj.QueryEngineApiPrefix);

            // Serialize array
            Serialize(sw, "expMultiplierAnnouncements", obj.ExpMultiplierAnnouncements);
            // the messages the streamer switched off; these were never written before, so they were lost on restart
            Serialize(sw, "botMessageFilters", obj.BotMessageFilters);
            Serialize(sw, "keyBindings", obj.KeyBindings);
            Serialize(sw, "playerListExpDisplay", obj.PlayerListExpDisplay);
            Serialize(sw, "hideLogos", obj.HideLogos);
            Serialize(sw, "logoKeyMoved", obj.LogoKeyMoved);
            Serialize(sw, "playerDetailsVisible", obj.PlayerDetailsVisible);
            Serialize(sw, "graphicsOff", obj.GraphicsOff);
            Serialize(sw, "simulatePhysics", obj.SimulatePhysics);
            Serialize(sw, "language", obj.Language);
            if (obj.Loot != null) sw.WriteLine("  \"loot\": { \"includeOrigin\": " + obj.Loot.IncludeOrigin.ToString().ToLower() + " },");

            // Serialize custom class instances
            sw.WriteLine("  \"streamLabels\": " + SerializeStreamLabelSettings(obj.StreamLabels) + ",");
            sw.WriteLine("  \"playerObserveSeconds\": " + SerializeObserverTimes(obj.PlayerObserveSeconds));

            sw.WriteLine("}");
            return sw.ToString();
        }

        private string SerializeStreamLabelSettings(StreamLabelSettings settings)
        {
            if (settings == null) return "null";
            return "{ " +
                "\"enabled\": " + settings.Enabled.ToString().ToLower() + ", " +
                "\"saveJsonFiles\": " + settings.SaveJsonFiles.ToString().ToLower() + ", " +
                "\"saveTextFiles\": " + settings.SaveTextFiles.ToString().ToLower() +
                " }";
        }

        private string SerializeObserverTimes(ObserverTimes times)
        {
            if (times == null) return "null";
            return "{ " +
                "\"default\": " + times.Default.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"subscriber\": " + times.Subscriber.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"moderator\": " + times.Moderator.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"vip\": " + times.Vip.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"broadcaster\": " + times.Broadcaster.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"onSubcription\": " + times.OnSubcription.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                "\"onCheeredBits\": " + times.OnCheeredBits.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " }";
        }
    }

}
