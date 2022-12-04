﻿public class PlayerSettings
{
    private readonly static JsonStore<PlayerSettings> store;
    public readonly static PlayerSettings Instance;

    public bool? PotatoMode;
    public bool? AutoPotatoMode;
    public bool? PostProcessing;
    public bool? RealTimeDayNightCycle;
    public bool? AlertExpiredStateCacheInChat;
    public bool? PlayerListVisible;

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