using System.IO;
using System.Linq;

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
    public bool? PlayerNamesVisible;
    public bool? LocalBotServerDisabled;
    public bool? AutoKickAfkPlayers;
    public bool? DayNightCycleEnabled;
    public bool? PhysicsEnabled;

    public float? DayNightTime;
    public float? PlayerListSize;
    public float? PlayerListScale;
    public float? PlayerListSpeed;
    public float? RaidHornVolume;
    public float? MusicVolume;
    public float? DPIScale;
    public float? ViewDistance;
    public float? CameraRotationSpeed;

    public int? LocalBotPort;
    public int? PlayerBoostRequirement;
    public int? PlayerCacheExpiryTime;
    public int? ItemDropMessageType;
    public int? PathfindingQualitySettings;

    public double? PlayerAfkHours;
    public string RavenBotServer;

    public bool? DisableDungeons;
    public bool? DisableRaids;
    public bool? AutoAssignVacantHouses;

    public bool? QueryEngineEnabled;
    public bool? QueryEngineAlwaysReturnArray;
    public string QueryEngineApiPrefix;

    public float[] ExpMultiplierAnnouncements;

    public StreamLabelSettings StreamLabels;
    public ObserverTimes PlayerObserveSeconds;
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
        Instance = store.Get();
        SetDefaultValues();
    }

    public static void Save() => store.Save(Instance);

    private static void SetDefaultValues()
    {
        var wasUpdated = false;

        if (Instance.DayNightCycleEnabled == null)
        {
            Instance.DayNightCycleEnabled = true;
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

        if (Instance.CameraRotationSpeed == null)
        {
            Instance.CameraRotationSpeed = OrbitCamera.RotationSpeed;
            wasUpdated = true;
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
            Serialize(sw, "playerNamesVisible", obj.PlayerNamesVisible);
            Serialize(sw, "localBotServerDisabled", obj.LocalBotServerDisabled);
            Serialize(sw, "autoKickAfkPlayers", obj.AutoKickAfkPlayers);
            Serialize(sw, "dayNightCycleEnabled", obj.DayNightCycleEnabled);
            Serialize(sw, "queryEngineEnabled", obj.QueryEngineEnabled);
            Serialize(sw, "queryEngineAlwaysReturnArray", obj.QueryEngineAlwaysReturnArray);
            Serialize(sw, "physicsEnabled", obj.PhysicsEnabled);

            Serialize(sw, "disableRaids", obj.DisableRaids);
            Serialize(sw, "disableDungeons", obj.DisableDungeons);
            Serialize(sw, "autoAssignVacantHouses", obj.AutoAssignVacantHouses);

            // Serialize all nullable float properties
            Serialize(sw, "dayNightTime", obj.DayNightTime);
            Serialize(sw, "playerListSize", obj.PlayerListSize);
            Serialize(sw, "playerListScale", obj.PlayerListScale);
            Serialize(sw, "playerListSpeed", obj.PlayerListSpeed);
            Serialize(sw, "raidHornVolume", obj.RaidHornVolume);
            Serialize(sw, "musicVolume", obj.MusicVolume);
            Serialize(sw, "dpiScale", obj.DPIScale);
            Serialize(sw, "viewDistance", obj.ViewDistance);
            Serialize(sw, "cameraRotationSpeed", obj.CameraRotationSpeed);

            // Serialize all nullable int properties
            Serialize(sw, "playerBoostRequirement", obj.PlayerBoostRequirement);
            Serialize(sw, "playerCacheExpiryTime", obj.PlayerCacheExpiryTime);
            Serialize(sw, "itemDropMessageType", obj.ItemDropMessageType);
            Serialize(sw, "pathfindingQualitySettings", obj.PathfindingQualitySettings);
            Serialize(sw, "localBotPort", obj.LocalBotPort);

            // Serialize all nullable double properties
            Serialize(sw, "playerAfkHours", obj.PlayerAfkHours);

            // Serialize string properties

            Serialize(sw, "ravenBotServer", obj.RavenBotServer);
            Serialize(sw, "queryEngineApiPrefix", obj.QueryEngineApiPrefix);

            // Serialize array
            Serialize(sw, "expMultiplierAnnouncements", obj.ExpMultiplierAnnouncements);

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
