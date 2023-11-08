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

    public float? DayNightTime;
    public float? PlayerListSize;
    public float? PlayerListScale;
    public float? RaidHornVolume;
    public float? MusicVolume;
    public float? DPIScale;
    public float? CameraRotationSpeed;

    public int? PlayerBoostRequirement;
    public int? PlayerCacheExpiryTime;
    public int? ItemDropMessageType;
    public int? PathfindingQualitySettings;

    public double? PlayerAfkHours;
    public string RavenBotServer;
    public string QueryEngineApiPrefix;
    public bool? QueryEngineEnabled;

    public float[] ExpMultiplierAnnouncements;

    public ObserverTimes PlayerObserveSeconds;

    static PlayerSettings()
    {
        store = JsonStore<PlayerSettings>.Create("game-settings");
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

        if (Instance.RavenBotServer == null)
        {
            Instance.RavenBotServer = "ravenbot.ravenfall.stream:4041";
            wasUpdated = true;
        }

        if (Instance.QueryEngineEnabled == null)
        {
            Instance.QueryEngineEnabled = true;
            wasUpdated = false;
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
}
