using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts;
using RavenNest.Models;
using RavenNest.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Debug = Shinobytes.Debug;

using UnityEngine.Rendering;
using Shinobytes.Linq;
using UnityEngine.AI;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.CompilerServices;

public class GameManager : MonoBehaviour, IGameManager
{
    private readonly ConcurrentDictionary<GameEventType, IGameEventHandler> gameEventHandlers = new();
    private readonly ConcurrentDictionary<Type, IGameEventHandler> typedGameEventHandlers = new();

    private readonly ConcurrentQueue<GameEvent> gameEventQueue = new ConcurrentQueue<GameEvent>();
    private readonly Queue<PlayerController> playerKickQueue = new Queue<PlayerController>();
    private readonly ConcurrentDictionary<string, LoadingState> loadingStates
        = new ConcurrentDictionary<string, LoadingState>();

    private readonly GameEventManager events = new GameEventManager();
    private IoCContainer ioc;

    private DateTime nextAutoJoinRaid;
    private DateTime nextAutoJoinDungeon;

    private object[] ravenbotArgs;

    private int lastButtonIndex = 0;
    private float stateSaveTime = 1f;
    private float experienceSaveTime = 1f;
    private float experienceSaveInterval = 3f;
    private float stateSaveInterval = 3f;
    private float updateSessionInfoTime = 5f;
    private float sessionUpdateFrequency = 5f;
    private int saveCounter;

    private RavenNest.SDK.UnityLogger logger;
    private bool gameSessionActive;
    private float expBoostTimerUpdate = 0.5f;
    private float streamerRaidTimer;
    private bool streamerRaidWar;
    private string streamerRaid;
    private TextMeshProUGUI exitViewText;
    private string exitViewTextFormat;
    private DateTime lastGameEventRecevied;
    private bool isReloadingScene;
    private float uptimeSaveTimerInterval = 3f;
    private float uptimeSaveTimer = 3f;
    private int spawnedBots;
    private bool potatoMode;
    private bool forcedPotatoMode;
    private ClanManager clanManager;
    private NameTagManager nametagManager;
    private SessionStats sessionStats;
    private bool userTriggeredExit;

    [Header("Default Settings")]
    [SerializeField] private string accessKey;
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private RavenBot ravenBot;
    [SerializeField] private GameSettings settings;

    [SerializeField] private LoginHandler loginHandler;
    [SerializeField] private TavernHandler tavern;
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private Volume postProcessingEffects;

    [Header("UI")]
    [SerializeField] private BATimer boostTimer;
    [SerializeField] private PlayerSearchHandler playerSearchHandler;
    [SerializeField] private GameMenuHandler menuHandler;
    [SerializeField] private SettingsMenuView settingsView;
    [SerializeField] private GameObject gameReloadMessage;
    [SerializeField] private GameObject exitView;
    [SerializeField] private GameObject gameReloadUIPanel;
    [SerializeField] private PlayerDetails playerObserver;
    [SerializeField] private IslandDetails islandDetails;
    [SerializeField] private PlayerList playerList;

    [Header("Managers")]
    [SerializeField] private TwitchEventManager subEventManager;
    [SerializeField] private DropEventManager dropEventManager;
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private VillageManager villageManager;
    [SerializeField] private OnsenManager onsen;
    [SerializeField] private PlayerLogoManager playerLogoManager;
    [SerializeField] private ServerNotificationManager serverNotificationManager;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private RaidManager raidManager;
    [SerializeField] private StreamRaidManager streamRaidManager;
    [SerializeField] private ArenaController arenaController;
    [SerializeField] private ItemManager itemManager;

    [Header("Ferry")]
    [SerializeField] private FerryController ferryController;
    [SerializeField] private FerryProgress ferryProgress;

    [Header("Game Update Banner")]
    [SerializeField] private GameObject goUpdateAvailable;
    [SerializeField] private TMPro.TextMeshProUGUI lblUpdateAvailable;

    [Header("Graphics Settings")]
    public RenderPipelineAsset URP_LowQuality;
    public RenderPipelineAsset URP_DefaultQuality;

    [NonSerialized] public bool NewUpdateAvailable;

    public SessionStats SessionStats => sessionStats;

    public string ServerAddress;

    public bool UsePostProcessingEffects = true;
    public GraphicsToggler Graphics;
    public RavenNestClient RavenNest;
    public ClanManager Clans => clanManager ?? (clanManager = new ClanManager(this));
    public VillageManager Village => villageManager;
    public PlayerLogoManager PlayerLogo => playerLogoManager;
    public ExpBoostEvent Boost => subEventManager.CurrentBoost;
    public TwitchEventManager Twitch => subEventManager;
    public MusicManager Music => musicManager;
    public ChunkManager Chunks => chunkManager;
    public IslandManager Islands => islandManager;
    public PlayerDetails ObservedPlayerDetails => playerObserver;
    public IslandDetails ObservedIslandDetails => islandDetails;

    //public ObservedTarget ObservedTarget => new ObservedTarget(playerObserver.ObservedPlayer, islandDetails);

    public PlayerManager Players => playerManager;
    public ItemManager Items => itemManager;
    public CraftingManager Crafting => craftingManager;
    public ArenaController Arena => arenaController;
    public RaidManager Raid => raidManager;
    public StreamRaidManager StreamRaid => streamRaidManager;
    public DungeonManager Dungeons => dungeonManager;
    public RavenBotConnection RavenBot => ravenBot?.Connection;
    public RavenBot RavenBotController => ravenBot;
    public FerryController Ferry => ferryController;
    public DropEventManager DropEvent => dropEventManager;
    public GameCamera Camera => gameCamera;
    public GameEventManager Events => events;
    public PlayerList PlayerList => playerList;
    public ServerNotificationManager ServerNotifications => serverNotificationManager;

    public OnsenManager Onsen => onsen;
    public TavernHandler Tavern => tavern;
    public Overlay Overlay => overlay;
    public bool IsSaving => saveCounter > 0;

    [NonSerialized] public bool isDebugMenuVisible;

    public StreamLabel uptimeLabel;
    public StreamLabel villageBoostLabel;
    public StreamLabel playerCountLabel;

    public StreamLabel expMultiplierJson;
    public StreamLabel sessionStatsJson;
    public StreamLabel ferryStatsJson;
    public StreamLabel raidStatsJson;
    public StreamLabel dungeonStatsJson;
    public StreamLabel villageStatsJson;

    private SessionSettings sessionSettings = new SessionSettings
    {
        ExpMultiplierLimit = 100,
        AutoRestCost = 500,
        AutoJoinDungeonCost = 5000,
        AutoJoinRaidCost = 3000,
        
    };
    public SessionSettings SessionSettings
    {
        get { return sessionSettings; }
        set
        {
            sessionSettings = value;
            if (value != null)
            {
                AdminControlData.IsAdmin = sessionSettings.IsAdministrator;
            }
        }
    }
    public bool LogoCensor { get; set; }
    public bool AlertExpiredStateCacheInChat { get; set; } = true;
    public bool PlayerNamesVisible { get; set; } = true;
    public PlayerItemDropMessageSettings ItemDropMessageSettings { get; set; }
    public int PlayerBoostRequirement { get; set; } = 0;

    public bool PotatoMode
    {
        get => forcedPotatoMode || potatoMode;
        set => potatoMode = value;
    }

    public float DayNightCycleProgress
    {
        get => dayNightCycle?.CycleProgress ?? 0; set
        {
            if (dayNightCycle)
                dayNightCycle.CycleProgress = value;
        }
    }

    public bool DayNightCycleEnabled
    {
        get => dayNightCycle?.IsEnabled ?? false; set
        {
            if (dayNightCycle)
                dayNightCycle.IsEnabled = value;
        }
    }

    public bool RealtimeDayNightCycle
    {
        get => dayNightCycle?.UseRealTime ?? false; set
        {
            if (dayNightCycle)
                dayNightCycle.UseRealTime = value;
        }
    }
    public bool AutoPotatoMode { get; set; }
    public bool IsLoaded { get; private set; }
    public bool DungeonStartEnabled { get; internal set; } = true;
    public bool RaidStartEnabled { get; internal set; } = true;
    public NameTagManager NameTags => nametagManager;
    public StreamLabels StreamLabels { get; private set; }

    private Overlay overlay;

    public bool RequireCodeForDungeonOrRaid;
    private GameCache.LoadStateResult gameCacheStateFileLoadResult;
    private bool stateFileStatusReported;

    public static bool BatchPlayerAddInProgress;
    private int experienceSaveIndex;
    private bool sessionPlayersCleared;
    private float gameStateTime;

    public RaidStats raidStats;
    public VillageStats villageStats;
    public DungeonStats dungeonStats;
    public FerryStats ferryStats;

    void Awake()
    {
        FreezeChecker.Start();

        if (goUpdateAvailable) goUpdateAvailable.SetActive(false);

        GameTime.deltaTime = Time.deltaTime;

        if (!PlayerSettings.Instance.PhysicsEnabled.GetValueOrDefault())
        {
            GraphicsToggler.DisablePhysics();
        }

        //Physics.autoSimulation = false;
        BatchPlayerAddInProgress = false;

        overlay = FindAnyObjectByType<Overlay>();
        if (!overlay)
        {
            overlay = gameObject.AddComponent<Overlay>();
        }

        if (!settings) settings = GetComponent<GameSettings>();
        this.StreamLabels = new StreamLabels(settings);
        if (gameReloadUIPanel) gameReloadUIPanel.SetActive(false);
        Overlay.CheckIfGame();
        GameSystems.Awake();
        QueryEngineAPI.OnGameManagerAwake(this);
    }

    // Start is called before the first frame update   
    void Start()
    {
        GameTime.deltaTime = Time.deltaTime;
#if DEBUG
        Application.SetStackTraceLogType(LogType.Assert | LogType.Error | LogType.Exception | LogType.Log | LogType.Warning, StackTraceLogType.Full);
#else 
        Application.SetStackTraceLogType(LogType.Log | LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error | LogType.Exception, StackTraceLogType.ScriptOnly);
#endif

        this.SetupStreamLabels();

        GameCache.IsAwaitingGameRestore = false;
        ioc = GetComponent<IoCContainer>();

        if (gameReloadMessage) gameReloadMessage.SetActive(false);
        if (!Graphics) Graphics = FindAnyObjectByType<GraphicsToggler>();
        if (!nametagManager) nametagManager = FindAnyObjectByType<NameTagManager>();
        if (!dayNightCycle) dayNightCycle = GetComponent<DayNightCycle>();
        if (!onsen) onsen = GetComponent<OnsenManager>();
        if (!loginHandler) loginHandler = FindAnyObjectByType<LoginHandler>();
        if (!dropEventManager) dropEventManager = GetComponent<DropEventManager>();
        if (!ferryProgress) ferryProgress = FindAnyObjectByType<FerryProgress>();
        if (!gameCamera) gameCamera = FindAnyObjectByType<GameCamera>();

        if (!playerLogoManager) playerLogoManager = GetComponent<PlayerLogoManager>();
        if (!villageManager) villageManager = FindAnyObjectByType<VillageManager>();

        if (!subEventManager) subEventManager = GetComponent<TwitchEventManager>();
        if (!subEventManager) subEventManager = gameObject.AddComponent<TwitchEventManager>();

        if (!islandManager) islandManager = GetComponent<IslandManager>();
        if (!itemManager) itemManager = GetComponent<ItemManager>();
        if (!playerManager) playerManager = GetComponent<PlayerManager>();
        if (!chunkManager) chunkManager = GetComponent<ChunkManager>();
        if (!craftingManager) craftingManager = GetComponent<CraftingManager>();
        if (!raidManager) raidManager = GetComponent<RaidManager>();
        if (!streamRaidManager) streamRaidManager = GetComponent<StreamRaidManager>();
        if (!arenaController) arenaController = FindAnyObjectByType<ArenaController>();

        if (!ferryController) ferryController = FindAnyObjectByType<FerryController>();
        if (!musicManager) musicManager = GetComponent<MusicManager>();

        RegisterGameEventHandler<ItemAddEventHandler>(GameEventType.ItemAdd);
        RegisterGameEventHandler<ResourceUpdateEventHandler>(GameEventType.ResourceUpdate);
        RegisterGameEventHandler<ServerMessageEventHandler>(GameEventType.ServerMessage);

        RegisterGameEventHandler<SessionSettingsChangedEventHandler>(GameEventType.SessionSettingsChanged);
        RegisterGameEventHandler<VillageInfoEventHandler>(GameEventType.VillageInfo);
        RegisterGameEventHandler<VillageLevelUpEventHandler>(GameEventType.VillageLevelUp);

        RegisterGameEventHandler<ClanLevelChangedEventHandler>(GameEventType.ClanLevelChanged);
        RegisterGameEventHandler<ClanSkillLevelChangedEventHandler>(GameEventType.ClanSkillLevelChanged);

        RegisterGameEventHandler<PlayerRestedUpdateEventHandler>(GameEventType.PlayerRestedUpdate);
        RegisterGameEventHandler<ExpMultiplierEventHandler>(GameEventType.ExpMultiplier);
        RegisterGameEventHandler<GameUpdatedEventHandler>(GameEventType.GameUpdated);

        RegisterGameEventHandler<PlayerUnstuckEventHandler>(GameEventType.Unstuck);
        RegisterGameEventHandler<PlayerTeleportEventHandler>(GameEventType.Teleport);

        RegisterGameEventHandler<PlayerRemoveEventHandler>(GameEventType.PlayerRemove);
        RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
        RegisterGameEventHandler<PlayerExpUpdateEventHandler>(GameEventType.PlayerExpUpdate);
        RegisterGameEventHandler<PlayerJoinArenaEventHandler>(GameEventType.PlayerJoinArena);
        RegisterGameEventHandler<PlayerJoinDungeonEventHandler>(GameEventType.PlayerJoinDungeon);
        RegisterGameEventHandler<PlayerJoinRaidEventHandler>(GameEventType.PlayerJoinRaid);
        RegisterGameEventHandler<PlayerNameUpdateEventHandler>(GameEventType.PlayerNameUpdate);
        RegisterGameEventHandler<PlayerTaskEventHandler>(GameEventType.PlayerTask);
        RegisterGameEventHandler<PlayerTravelEventHandler>(GameEventType.PlayerTravel);

        RegisterGameEventHandler<PlayerBeginRestEventHandler>(GameEventType.PlayerBeginRest);
        RegisterGameEventHandler<PlayerEndRestEventHandler>(GameEventType.PlayerEndRest);


        RegisterGameEventHandler<PlayerStartRaidEventHandler>(GameEventType.PlayerStartRaid);
        RegisterGameEventHandler<PlayerStartDungeonEventHandler>(GameEventType.PlayerStartDungeon);

        RegisterGameEventHandler<StreamerPvPEventHandler>(GameEventType.StreamerPvP);

        RegisterGameEventHandler<StreamerWarRaidEventHandler>(GameEventType.WarRaid);
        RegisterGameEventHandler<StreamerRaidEventHandler>(GameEventType.Raid);
        RegisterGameEventHandler<PlayerAppearanceEventHandler>(GameEventType.PlayerAppearance);
        RegisterGameEventHandler<ItemBuyEventHandler>(GameEventType.ItemBuy);
        RegisterGameEventHandler<ItemSellEventHandler>(GameEventType.ItemSell);

        RegisterGameEventHandler<ItemRemoveEventHandler>(GameEventType.ItemRemove);
        RegisterGameEventHandler<ItemRemoveByCategoryHandler>(GameEventType.ItemRemoveByCategory);

        RegisterGameEventHandler<ItemUnequipEventHandler>(GameEventType.ItemUnEquip);
        RegisterGameEventHandler<ItemEquipEventHandler>(GameEventType.ItemEquip);

        LoadGameSettings();

        if (musicManager)
            musicManager.PlayBackgroundMusic();

        GameSystems.Start();

        gameCacheStateFileLoadResult = GameCache.LoadState();
        if (gameCacheStateFileLoadResult == GameCache.LoadStateResult.PlayersRestored)
        {
            GameCache.IsAwaitingGameRestore = true;
        }
    }

    public PlayerItemDropText AddItems(EventItemReward[] rewards, int dungeonIndex = -1, int raidIndex = -1)
    {
        //DropItem(player);
        // Does unity allow us to use C# 10?
        //Action<string, string[]> Announce = gameManager.RavenBot.Announce;
        var droppedItems = new Dictionary<string, List<string>>();

        foreach (var reward in rewards)
        {
            var rewardItem = Items.Get(reward.ItemId);
            if (rewardItem == null)
            {
                continue;
            }

            var player = Players.GetPlayerById(reward.CharacterId);
            if (!player)
            {
                continue;
            }

            var item = this.itemManager.Get(reward.ItemId);
            var stack = player.Inventory.AddToBackpack(reward.InventoryItemId, item, reward.Amount);

            player.RecordLoot(item, reward.Amount, dungeonIndex, raidIndex);

            player.EquipIfBetter(stack);

            var key = rewardItem.Name;

            if (!droppedItems.TryGetValue(key, out var items))
            {
                droppedItems[key] = (items = new List<string>());
            }

            items.Add(player.PlayerName);
        }
        return new PlayerItemDropText(droppedItems, ItemDropMessageSettings);
    }

    private void SetupStreamLabels()
    {
        uptimeLabel = StreamLabels.RegisterText("uptime", () => Time.realtimeSinceStartup.ToString());

        villageBoostLabel = StreamLabels.RegisterText("village-boost", () => GetVillageBoostString());

        playerCountLabel = StreamLabels.RegisterText("online-player-count", () => playerManager.GetPlayerCount().ToString());

        StreamLabels.RegisterText("level-requirements", () => GetLevelRequirementsString()).Update();
        StreamLabels.Register("level-requirements", () => GetLevelRequirements()).Update();

        expMultiplierJson = StreamLabels.Register("exp-multiplier", () => GetExpMultiplierStats());
        sessionStatsJson = StreamLabels.Register("session", () => GetSessionStats());
        ferryStatsJson = StreamLabels.Register("ferry", () => GetFerryStats());
        raidStatsJson = StreamLabels.Register("raid", () => GetRaidStats());
        dungeonStatsJson = StreamLabels.Register("dungeon", () => GetDungeonStats());
        villageStatsJson = StreamLabels.Register("village", () => GetVillageStats());

        expMultiplierJson.Update();
        sessionStatsJson.Update();
        ferryStatsJson.Update();
        raidStatsJson.Update();
        dungeonStatsJson.Update();
        villageStatsJson.Update();
    }

    private ExpBoostEvent emptySubBoost = new ExpBoostEvent();
    public ExpBoostEvent GetExpMultiplierStats()
    {
        var boost = subEventManager.CurrentBoost;
        if (boost.Active)
        {
            return boost;
        }

        return emptySubBoost;
    }

    public string GetVillageBoostString()
    {
        var bonuses = Village.GetExpBonuses();
        var value = bonuses.Where(x => x.Bonus > 0)
         .GroupBy(x => x.SlotType)
         .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
         .Select(x => $"{x.Key} {x.Value.Sum(y => y.Bonus)}%");

        return string.Join(", ", value);
    }

    public VillageStats GetVillageStats()
    {
        if (this.villageStats == null)
        {
            this.villageStats = new VillageStats();
        }

        villageStats.Name = null;
        villageStats.Level = Village.TownHall.Level;
        villageStats.Tier = Village.TownHall.Tier;
        villageStats.BonusExp = Village.GetExpBonuses();
        villageStats.Boost = GetVillageBoostString();
        return villageStats;
    }

    public RaidStats GetRaidStats()
    {
        if (this.raidStats == null)
        {
            this.raidStats = new RaidStats();
        }

        raidStats.Started = Raid.Started;
        raidStats.PlayersCount = Raid.Raiders.Count;
        raidStats.SecondsLeft = Raid.SecondsLeft;
        raidStats.Counter = Raid.Counter;
        if (Raid.Boss != null)
        {
            var health = Raid.Boss.Enemy.Stats.Health;
            raidStats.BossHealthCurrent = health.CurrentValue;
            raidStats.BossHealthMax = health.MaxLevel;
            raidStats.BossHealthPercent = Raid.Boss.Enemy.Stats.HealthPercent;
            raidStats.BossLevel = Raid.Boss.Enemy.Stats.CombatLevel;
        }

        return raidStats;
    }

    public DungeonStats GetDungeonStats()
    {
        if (this.dungeonStats == null)
        {
            this.dungeonStats = new DungeonStats();
        }

        dungeonStats.Started = Dungeons.Started;
        dungeonStats.PlayersCount = Dungeons.GetPlayers().Count;
        dungeonStats.Counter = Dungeons.Counter;

        if (Dungeons.Boss != null)
        {
            var boss = Dungeons.Boss;
            var health = boss.Enemy.Stats.Health;
            dungeonStats.BossHealthCurrent = health.CurrentValue;
            dungeonStats.BossHealthMax = health.MaxLevel;
            dungeonStats.BossHealthPercent = boss.Enemy.Stats.HealthPercent;
            dungeonStats.BossLevel = boss.Enemy.Stats.CombatLevel;
        }

        return dungeonStats;
    }

    public FerryStats GetFerryStats()
    {
        if (this.ferryStats == null)
        {
            this.ferryStats = new FerryStats();
        }

        ferryStats.Destination = Ferry.GetDestination();
        ferryStats.PlayersCount = Ferry.GetPlayerCount();
        if (Ferry.Captain)
        {
            ferryStats.CaptainName = Ferry.Captain.Name;
            ferryStats.CaptainSailingLevel = Ferry.Captain.Stats.Sailing.Level;
        }

        return ferryStats;
    }

    public SessionStats GetSessionStats()
    {
        if (this.sessionStats == null)
        {
            this.sessionStats = new SessionStats();
        }

        if (RavenNest != null)
        {
            sessionStats.Authenticated = RavenNest.Authenticated;
            sessionStats.SessionStarted = RavenNest.SessionStarted;
            sessionStats.TwitchUserName = RavenNest.TwitchUserName;
        }

        if (playerManager != null)
        {
            sessionStats.OnlinePlayerCount = playerManager.GetPlayerCount();
        }

        sessionStats.GameVersion = GameVersion.GetApplicationVersion();
        sessionStats.LastUpdatedUtc = DateTime.UtcNow;
        sessionStats.RealtimeSinceStartup = Time.realtimeSinceStartup;

        return sessionStats;
    }

    public List<IslandTaskCollection> GetLevelRequirements()
    {
        var items = new List<IslandTaskCollection>();

        Chunks.Init();

        var chunks = this.Chunks.GetChunks();
        var grouped = chunks.GroupBy(x => x.Island.Identifier);
        foreach (var island in grouped.OrderBy(x => x.Value.Sum(y => y.RequiredCombatLevel + y.RequiredSkilllevel)))
        {
            var collection = new IslandTaskCollection();
            collection.Island = island.Key;
            collection.Skills = new List<IslandTask>();
            foreach (var chunk in island.Value.OrderBy(x => x.ChunkType.ToString()))
            {
                collection.Skills.Add(new IslandTask
                {
                    SkillLevelRequirement = chunk.RequiredSkilllevel,
                    CombatLevelRequirement = chunk.RequiredCombatLevel,
                    Name = chunk.ChunkType.ToString(),
                });
            }
            items.Add(collection);
        }

        return items;
    }


    public string GetLevelRequirementsString()
    {
        var sb = new StringBuilder();

        var requirements = GetLevelRequirements();

        foreach (var island in requirements)
        {
            sb.AppendLine(island.Island);

            foreach (var chunk in island.Skills)
            {
                if (chunk.SkillLevelRequirement > 1)
                {
                    sb.AppendLine(chunk.Name + " - Requires Level " + chunk.SkillLevelRequirement);
                }
                else if (chunk.CombatLevelRequirement > 1)
                {
                    sb.AppendLine(chunk.Name + " - Requires Combat Level " + chunk.CombatLevelRequirement);
                }
                else
                {
                    sb.AppendLine(chunk.Name + " - No Requirement");
                }
            }
        }

        return sb.ToString();
    }


    private void LoadGameSettings()
    {
        var settings = PlayerSettings.Instance;
        if (!Overlay.IsGame)
        {
            return;
        }

        TwitchEventManager.AnnouncementTimersSeconds = settings.ExpMultiplierAnnouncements ?? TwitchEventManager.AnnouncementTimersSeconds;

        UsePostProcessingEffects = settings.PostProcessing.GetValueOrDefault();
        AutoPotatoMode = settings.AutoPotatoMode.GetValueOrDefault();
        PotatoMode = settings.PotatoMode.GetValueOrDefault();

        DayNightCycleEnabled = settings.DayNightCycleEnabled.GetValueOrDefault(true);
        DayNightCycleProgress = settings.DayNightTime.GetValueOrDefault(0.5f);

        RealtimeDayNightCycle = settings.RealTimeDayNightCycle.GetValueOrDefault();
        PlayerBoostRequirement = settings.PlayerBoostRequirement.GetValueOrDefault();
        AlertExpiredStateCacheInChat = settings.AlertExpiredStateCacheInChat.GetValueOrDefault();
        ItemDropMessageSettings = (PlayerItemDropMessageSettings)settings.ItemDropMessageType.GetValueOrDefault((int)ItemDropMessageSettings);
        PlayerList.Bottom = settings.PlayerListSize.GetValueOrDefault(PlayerList.Bottom);
        PlayerList.Scale = settings.PlayerListScale.GetValueOrDefault(PlayerList.Scale);
        PlayerList.Speed = settings.PlayerListSpeed.GetValueOrDefault(PlayerList.Speed);

        Raid.Notifications.volume = settings.RaidHornVolume.GetValueOrDefault(Raid.Notifications.volume);
        Music.volume = settings.MusicVolume.GetValueOrDefault(Music.volume);

        IslandObserveCamera.RotationSpeed = OrbitCamera.RotationSpeed = settings.CameraRotationSpeed.GetValueOrDefault(OrbitCamera.RotationSpeed);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
        {
            SettingsMenuView.SetResolutionScale(settings.DPIScale.GetValueOrDefault(1f));
        }
        else
        {
            SettingsMenuView.SetResolutionScale(1);
        }

        settingsView.UpdateSettingsUI();

    }

    internal void OnSessionStart()
    {
        gameCamera.OnSessionStart();

        ravenBot.UpdateSessionInfo();

        if (RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
        {
            RavenBot.SendSessionOwner();
        }
    }

    private void RegisterGameEventHandler<T>(GameEventType type) where T : IGameEventHandler, new()
    {
        var value = new T();
        var targetType = typeof(T);
        gameEventHandlers[type] = value;
        typedGameEventHandlers[targetType] = value;
    }
    private IGameEventHandler GetEventHandler(GameEventType type)
    {
        if (gameEventHandlers.TryGetValue(type, out var handler))
        {
            return handler;
        }

        return null;
    }

    private IGameEventHandler<T> GetEventHandler<T>()
    {
        // a little bit more expensive, but it will do.
        var targetType = typeof(T);
        if (typedGameEventHandlers.TryGetValue(targetType, out var handler) && handler is IGameEventHandler<T> typed)
        {
            return typed;
        }

        foreach (var value in gameEventHandlers.Values)
        {
            var t = value as IGameEventHandler<T>;
            if (t != null)
            {
                typedGameEventHandlers[targetType] = t;
                return t;
            }
        }

        return default;
    }

    public void SaveStateAndShutdownGame(bool activateTempLogin = true)
    {
        if (activateTempLogin)
        {
            if (!loginHandler) return;
            loginHandler.ActivateTempAutoLogin();
        }

        userTriggeredExit = true;

        SaveStateFile();

        OnExit();

#if UNITY_2023_2 || UNITY_2023_2_20
        try
        {
            // die!
            Process.GetCurrentProcess().Kill();
            return;
        }
        catch
        {
            // ignored
        }
#endif

        Application.Quit();
    }

    public void UpdateGame()
    {
        SaveStateAndLoadScene(0);
    }

    public void SaveStateFile()
    {
        GameCache.SavePlayersState(this.playerManager.GetAllPlayers());
    }

    public void SaveStateAndLoadScene(int sceneIndex = 1)
    {
        if (!loginHandler) return;

        isReloadingScene = true;
        loginHandler.ActivateTempAutoLogin();

        SaveStateFile();

        GameCache.IsAwaitingGameRestore = true;

        RavenNest.Dispose();
        RavenBotController.Dispose();

        gameReloadMessage.SetActive(true);

        LoadScene(sceneIndex);
    }

    public void LoadScene(int sceneIndex = 1)
    {
        RavenNest.Dispose();
        RavenBotController.Dispose();
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
    }

    private IEnumerator RestoreGameState(RestorableGameState state)
    {
        GameCache.IsAwaitingGameRestore = false;

        gameReloadUIPanel.SetActive(true);
        try
        {
            if (state.Players == null || state.Players.Count == 0)
                yield break;

            yield return UnityEngine.Resources.UnloadUnusedAssets();

            Shinobytes.Debug.Log("Restoring game state with " + state.Players.Count + " players.");

            var waitTime = 0;
            // if we got disconnected or something
            while ((!RavenNest.Authenticated || !RavenNest.SessionStarted || !RavenNest.Tcp.IsReady) && waitTime < 100)
            {
                yield return new WaitForSeconds(1);
                waitTime++;
            }

            // still not connected? retry later.
            if (!RavenNest.Authenticated || !RavenNest.SessionStarted || !RavenNest.Tcp.IsReady)
            {
                Shinobytes.Debug.LogWarning("No conneciton to server when trying to restore players. Retrying");
                GameCache.SetState(state);
                yield break;
            }

            // RestoreAsync

            yield return Players.RestoreAsync(state.Players);
        }
        finally
        {
            gameReloadUIPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameSystems.Update(!isReloadingScene);
        FreezeChecker.SetCurrentScriptUpdate("GameManager Update");

        if (isReloadingScene)
        {
            return;
        }

        //if (Input.GetKeyDown(KeyCode.L))
        //{
        //    (new StreamerRaidEventHandler()).Handle(this, JsonConvert.SerializeObject(new StreamRaidInfo()
        //    {
        //        RaiderUserId = "72424639",
        //        RaiderUserName = "zerratar",
        //        Players = new List<UserCharacter>
        //        {
        //            new UserCharacter()
        //            {
        //                CharacterId = new Guid("0d4e920d-64a7-4b98-9c78-01dd1913824b"),
        //                UserId ="72424639",
        //                Username = "zerratar"
        //            }
        //        }
        //    }));
        //}

        if (AutoPotatoMode)
        {
            forcedPotatoMode = !Application.isFocused;
        }

        var currentQualityLevel = QualitySettings.GetQualityLevel();
        if (PotatoMode)
        {
            if (currentQualityLevel != 0)
            {
                QualitySettings.SetQualityLevel(0);
            }

            //if (URP_LowQuality)
            //{
            //    GraphicsSettings.defaultRenderPipeline = URP_LowQuality;
            //}

            DisablePostProcessingEffects();
        }
        else
        {
            var targetQualitySettings = PlayerSettings.Instance.QualityLevel.GetValueOrDefault(1);
            if (targetQualitySettings < 0 || targetQualitySettings > QualitySettings.count)
            {
                targetQualitySettings = QualitySettings.count - 1;
            }

            if (currentQualityLevel != targetQualitySettings)
            {
                QualitySettings.SetQualityLevel(targetQualitySettings);
            }

            //if (URP_DefaultQuality)
            //{
            //    GraphicsSettings.defaultRenderPipeline = URP_DefaultQuality;
            //}

            EnablePostProcessingEffects();
        }

        if (nametagManager)
            nametagManager.NameTagsEnabled = PlayerNamesVisible;

        UpdateIntegrityCheck();

        if (Input.GetKeyDown(KeyCode.F11) && RavenBot.UseRemoteBot && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Disconnect(BotConnectionType.Remote);
            return;
        }

        var f12Down = Input.GetKeyDown(KeyCode.F12);
        if (f12Down && Input.GetKey(KeyCode.LeftShift))
        {
            RavenNest.Tcp.Enabled = !RavenNest.Tcp.Enabled;
            return;
        }
        else if (f12Down)
        {
            RavenNest.Tcp.Disconnect();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveStateAndLoadScene();
            return;
        }

        if (UpdateExitView())
        {
            return;
        }

        if (Input.GetKeyUp(KeyCode.Escape) && !menuHandler.Visible && !playerSearchHandler.Visible)
        {
            menuHandler.Show();
        }

        if (Input.GetKeyUp(KeyCode.F4))
        {
            if (GraphicsToggler.SimulationMode == SimulationMode.Script)
            {
                Shinobytes.Debug.Log("Physics Turned ON, press F4 to turn off.");
                GraphicsToggler.EnablePhysics();
            }
            else
            {
                Shinobytes.Debug.Log("Physics Turned OFF, press F4 to turn on.");
                GraphicsToggler.DisablePhysics();
            }
        }

        UpdateApiCommunication();

        if (RavenNest == null || !RavenNest.Authenticated)
        {
            return;
        }

        if (!UpdateGameEvents())
        {
            return;
        }

        //ExpMultiplierChecker.RunAsync(this);

        if (RavenNest.Authenticated &&
            RavenNest.SessionStarted &&
            RavenNest.Tcp.IsReady)
        {
            if (Items.Loaded && GameCache.IsAwaitingGameRestore)
            {
                GameCache.IsAwaitingGameRestore = false;
                var reloadState = GameCache.GetReloadState();
                if (reloadState != null)
                {
                    StartCoroutine(RestoreGameState(reloadState.Value));
                    return;
                }
            }

            if (!stateFileStatusReported && RavenBot.IsConnected && gameCacheStateFileLoadResult == GameCache.LoadStateResult.Expired)
            {
                if (AlertExpiredStateCacheInChat)
                {
                    RavenBot.Announce("Player restore state file has expired. No players has been added back.");
                }

                stateFileStatusReported = true;
            }

            if (!sessionPlayersCleared & gameCacheStateFileLoadResult == GameCache.LoadStateResult.NoPlayersRestored)
            {
                if (AlertExpiredStateCacheInChat)
                {
                    RavenBot.Announce("Player restore state file has expired. No players has been added back.");
                }

                RavenNest.Game.ClearPlayersAsync();

                sessionPlayersCleared = true;
            }
        }


        if (uptimeSaveTimer > 0)
            uptimeSaveTimer -= GameTime.deltaTime;

        if (uptimeSaveTimer <= 0 && PlayerSettings.Instance.StreamLabels.Enabled)
        {
            uptimeSaveTimer = uptimeSaveTimerInterval;
            uptimeLabel.Update();

            /* do it every 3s for now. but would better to keep track on value changes and update only then. */

            expMultiplierJson.Update();
            sessionStatsJson.Update();
            ferryStatsJson.Update();
            raidStatsJson.Update();
            dungeonStatsJson.Update();
            villageStatsJson.Update();
        }

        UpdateExpBoostTimer();

        UpdatePlayerKickQueue();

        HandleKeyDown();

        UpdateChatBotCommunication();
    }

    public void EnablePostProcessingEffects()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        if (!UsePostProcessingEffects)
        {
            DisablePostProcessingEffects();
            return;
        }

        postProcessingEffects.weight = 1;
    }
    public void DisablePostProcessingEffects()
    {
        postProcessingEffects.weight = 0f;
    }

    private void UpdateIntegrityCheck()
    {
        IntegrityCheck.Update();
    }

#if DEBUG
    Stopwatch UpdateGameEvents_stopwatch = new Stopwatch();
    Stopwatch UpdateGameEvents_evtSw = new Stopwatch();
    List<GameEventProfiler> UpdateGameEvents_evts = new List<GameEventProfiler>();

#endif

    private bool UpdateGameEvents()
    {
#if DEBUG
        UpdateGameEvents_stopwatch.Restart();
        var eventsHandled = 0;
#endif

        while (gameEventQueue.TryDequeue(out var ge))
        {
#if DEBUG
            UpdateGameEvents_evtSw.Restart();
#endif
            HandleGameEvent(ge);

#if DEBUG
            UpdateGameEvents_evtSw.Stop();
            UpdateGameEvents_evts.Add(new GameEventProfiler
            {
                Size = ge.Data.Length,
                Type = ge.Type,
                ElapsedMilliseconds = UpdateGameEvents_evtSw.ElapsedMilliseconds
            });
            eventsHandled++;
#endif
        }

#if DEBUG
        UpdateGameEvents_stopwatch.Stop();
        if (UpdateGameEvents_stopwatch.ElapsedMilliseconds > 30)
        {
            Shinobytes.Debug.LogError("UpdateGameEvents took a long time! " + UpdateGameEvents_stopwatch.ElapsedMilliseconds + "ms for " + eventsHandled + " events!\r\n" +
                string.Join("\r\n- ", UpdateGameEvents_evts.Select(x => (GameEventType)x.Type + ": " + x.ElapsedMilliseconds + "ms, " + x.Size + " bytes").ToArray())
            );
        }
#endif
        return true;
    }


    private struct GameEventProfiler
    {
        public int Type;
        public int Size;
        public long ElapsedMilliseconds;
    }


    public bool UpdateExitView()
    {
        if (streamerRaidTimer <= 0)
        {
            return false;
        }

        if (exitViewText && exitView.activeSelf)
        {
            streamerRaidTimer -= GameTime.deltaTime;
            exitViewText.text = string.Format(
                exitViewTextFormat,
                streamerRaid,
                Mathf.FloorToInt(streamerRaidTimer)
            );

            if (streamerRaidTimer <= 0f)
            {
                //Application.Quit();
                SaveStateAndShutdownGame(false);
            }

            return true;
        }
        return false;
    }

    internal void BeginStreamerRaid(string username, bool war)
    {
        exitView.SetActive(true);
        streamerRaidTimer = 15f;
        streamerRaidWar = war;
        streamerRaid = username;

        exitViewText = exitView.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        exitViewTextFormat = exitViewText.text;
    }

    public void SetLoadingState(string key, LoadingState state)
    {
        loadingStates[key] = state;
        IsLoaded = loadingStates.All(x => x.Value == LoadingState.Loaded);
    }

    public void OnAuthenticated()
    {
        //ShowFerryProgress();
    }

    public void ShowFerryProgress()
    {
        ferryProgress.gameObject.SetActive(true);
    }

    internal void SetTimeOfDay(int totalTime, int freezeTime)
    {
        dayNightCycle.SetTimeOfDay(totalTime, freezeTime);
    }
    public void QueueRemovePlayer(PlayerController player)
    {
        playerKickQueue.Enqueue(player);
        player.OnKicked();
    }
    public void RemovePlayer(PlayerController player, bool notifyServer = true)
    {
        var playerName = player?.Name;
        try
        {
            if (player.dungeonHandler.InDungeon)
            {
                dungeonManager.Remove(player);
            }

            if (player.raidHandler.InRaid)
            {
                raidManager.Leave(player);
            }

            if (player.ferryHandler.OnFerry)
            {
                player.ferryHandler.RemoveFromFerry();
            }

            if (notifyServer)
            {
                RavenNest.PlayerRemoveAsync(player);
            }

            player.Island = null;

            player.Removed = true;
            playerList.RemovePlayer(player);
            playerManager.Remove(player);

            if (gameCamera.Observer != null && gameCamera.Observer.ObservedPlayer == player)
            {
                gameCamera.ObservePlayer(null);
            }

            if (player.IsBot)
            {
                return;
            }

            villageBoostLabel.Update();
            playerCountLabel.Update();

            villageStatsJson.Update();
            sessionStatsJson.Update();
            SaveStateFile();
            UpdatePathfindingIterations();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Failed to remove player (" + playerName + "): " + exc.ToString() + "\nPlayer instead queued up for removal");
            if (player != null && !player.isDestroyed)
            {
                QueueRemovePlayer(player);
            }
        }
    }

    public void UpdatePathfindingIterations()
    {
        var i = PlayerSettings.Instance.PathfindingQualitySettings.GetValueOrDefault(1);
        var qMin = SettingsMenuView.PathfindingQualityMin;
        var qMax = SettingsMenuView.PathfindingQualityMin;
        if (i >= 0 && i < qMin.Length && i < qMax.Length)
        {
            var min = qMin[i];
            var max = qMax[i];
            var value = playerManager.GetPlayerCount() * 2;
            NavMesh.pathfindingIterationsPerFrame = Mathf.Min(Mathf.Max(min, value), max);
        }
    }

    public async void SpawnManyBotPlayers(int count)
    {
        BeginBatchedPlayerAdd();

        for (var i = 0; i < count; ++i)
        {
            await SpawnBotPlayer();
            await Task.Delay(10);
        }

        EndBatchedPlayerAdd(true);
    }

    public async Task SpawnBotPlayerOnFerry()
    {
        BotPlayerGenerator.Instance.NextOnFerry = true;
        var playerInfo = await GenerateBotInfoAsync();
        var player = await Players.JoinAsync(null, playerInfo, RavenBot.ActiveClient, false, true, null);
        if (player)
            player.EquipBestItems();
        ++spawnedBots;
    }

    public async Task SpawnBotPlayer()
    {
        var playerInfo = await GenerateBotInfoAsync();
        var player = await Players.JoinAsync(null, playerInfo, RavenBot.ActiveClient, false, true, null);
        if (player)
            player.EquipBestItems();
        ++spawnedBots;
    }

    private async Task<User> GenerateBotInfoAsync()
    {
        var id = Random.Range(10000, 99999);
        var userId = "#" + id;

        while (playerManager.GetPlayerByPlatformId(userId, "system"))
        {
            id = Random.Range(10000, 99999);
            userId = "#" + id;
            await Task.Delay(5);
        }

        var userName = "Bot" + id;
        return new User(Guid.NewGuid(), Guid.NewGuid(), userName, userName, "#ffffff", "system", userId, false, false, false, false, false, false, "1");
    }

    public PlayerController SpawnPlayer(
        RavenNest.Models.Player playerDefinition,
        User streamUser = null,
        StreamRaidInfo raidInfo = null,
        bool isGameRestore = false)
    {
        if (!chunkManager)
        {
            Debug.LogError("No chunk manager available!");
            return null;
        }

        var starter = chunkManager.GetStarterChunk();
        if (starter == null)
        {
            Debug.LogError("No starter chunk available!");
            return null;
        }

        var spawnPoint = starter.GetPlayerSpawnPoint();
        //var startIsland = playerDefinition?.State?.Island;
        //var island = Islands.Find(startIsland);
        //if (island != null)
        //{
        //    spawnPoint = island.SpawnPosition;
        //}

        var vector3 = Random.insideUnitSphere + (Vector3.up * 2f);

        var playerInitiatedJoin = !isGameRestore;
        if (streamUser != null && streamUser.PlatformId != null && streamUser.PlatformId.StartsWith("#"))
            playerInitiatedJoin = false;

        var player = playerManager.Spawn(spawnPoint + vector3,
            playerDefinition, streamUser, raidInfo,
            playerInitiatedJoin);

        if (player == null || !player)
        {
            return null;
        }

        if (!player.ferryHandler.OnFerry)
        {
            player.Movement.AdjustPlayerPositionToNavmesh(0.5f);
        }

        playerList.AddPlayer(player);

        if (!isGameRestore && !BatchPlayerAddInProgress)
        {
            Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());

            playerCountLabel.Update();
            villageBoostLabel.Update();

            if (player && gameCamera)
            {
                if (gameCamera.AllowJoinObserve)
                {
                    gameCamera.ObservePlayer(player);
                }
                // in case we are currently observing islands, and more specifically an island without players
                // then we should observe the island the player is on.
                else if (player.Island && gameCamera.State == GameCameraType.Island &&
                        (gameCamera.CurrentlyObservedIsland == null || gameCamera.CurrentlyObservedIsland.GetPlayerCount() == 0))
                {
                    gameCamera.ObserveIsland(player.Island);
                }

            }
        }

        if (dropEventManager.IsActive)
            player.BeginItemDropEvent();

        UpdatePathfindingIterations();
        return player;
    }

    public void BeginBatchedPlayerAdd()
    {
        GameManager.BatchPlayerAddInProgress = true;
    }

    public void EndBatchedPlayerAdd(bool botsAdded = false)
    {
        GameManager.BatchPlayerAddInProgress = false;

        if (!botsAdded)
        {
            var count = Players.GetPlayerCount();

            Shinobytes.Debug.Log("Finished restoring game state with " + count + " players added back.");

            Village.TownHouses.EnsureAssignPlayerRows(count);
            playerCountLabel.Update();
            villageBoostLabel.Update();

            Village.AssignBestPlayers();

            var player = playerManager.LastAddedPlayer;
            if (player && gameCamera && gameCamera.AllowJoinObserve)
                gameCamera.ObservePlayer(player);
        }
    }

    public void HandleGameEvents(EventList gameEvents)
    {
        lastGameEventRecevied = DateTime.UtcNow;

        foreach (var ge in gameEvents.Events)
        {
            gameEventQueue.Enqueue(ge);
        }
    }

    public void HandleGameEvent(GameEvent gameEvent)
    {
        if (gameEvent == null)
            return;

        var handler = GetEventHandler((GameEventType)gameEvent.Type);
        if (handler != null)
        {
            handler.Handle(this, gameEvent.Data);
            return;
        }
    }

    public void HandleGameEvent<TEventData>(TEventData gameEvent)
    {
        if (gameEvent == null)
            return;

        var handler = GetEventHandler<TEventData>();
        if (handler != null)
        {
            handler.Handle(this, gameEvent);
            return;
        }
    }

    internal async Task<PlayerController> AddPlayerByCharacterIdAsync(Guid characterId, StreamRaidInfo raiderInfo)
    {
        var playerInfo = await RavenNest.PlayerJoinAsync(new PlayerJoinData
        {
            CharacterId = characterId
        });

        if (playerInfo == null || !playerInfo.Success)
        {
            Shinobytes.Debug.LogError("Unable to add character with id: " + characterId + " " + (playerInfo != null ? playerInfo.ErrorMessage : ""));
            return null;
        }
        return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    }

    private void OnApplicationQuit()
    {
        if (!userTriggeredExit)
        {
            OnExit();
        }
    }

    private void OnExit()
    {
        try
        {
            FreezeChecker.Stop();
            if (RavenBotController != null)
            {
                RavenBotController.Dispose();
            }

            StopRavenNestSession();

            QueryEngineAPI.OnExit();

            Shinobytes.Debug.Log("Application ending after " + Time.time + " seconds");
        }
        catch
        {
            // ignored
        }
    }

    private void HandleRavenNestConnection()
    {
        var client = RavenNest;
        if (logger == null)
            logger = new RavenNest.SDK.UnityLogger();

        if (client == null)
        {
            client = new RavenNestClient(logger, this);

            RavenNest = client;

            client.Tcp.OnReconnect(OnReconnectedToServer);

            // after RavenNestClient has been initialized, initialize RavenBot connection.

            ravenBot = new RavenBot(logger, client, this);
        }

        if (client != null)
        {
            if (!string.IsNullOrEmpty(RavenNest.ServerAddress))
                ServerAddress = RavenNest.ServerAddress;
        }
    }

    private void OnReconnectedToServer()
    {
    }

    public async Task<bool> RavenNestLoginAsync(string username, string password)
    {
        if (RavenNest == null) return false;
        if (RavenNest.Authenticated) return true;
        return await RavenNest.LoginAsync(username, password);
    }

    private void RavenNestUpdate()
    {
        if (RavenNest.HasActiveRequest)
        {
            return;
        }

        if (RavenNest.BadClientVersion)
        {
            return;
        }

        if (RavenNest.AwaitingSessionStart)
        {
            return;
        }

        if (RavenNest.Authenticated && !RavenNest.SessionStarted)
        {
            RavenNest.StartSession(Ravenfall.Version, accessKey);
            //if (await ravenNest.StartSessionAsync(Ravenfall.Version, accessKey, false))
            //{
            //    gameSessionActive = true;
            //    lastGameEventRecevied = DateTime.UtcNow;
            //}
        }

        if (!RavenNest.Authenticated || !RavenNest.SessionStarted)
        {
            return;
        }

        if (!gameSessionActive)
        {
            gameSessionActive = true;
            lastGameEventRecevied = DateTime.UtcNow;
        }

        if (updateSessionInfoTime > 0)
        {
            updateSessionInfoTime -= GameTime.deltaTime;
        }

        if (RavenBot.UseRemoteBot && !RavenBot.IsConnectedToRemote && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Connect(BotConnectionType.Remote);

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                ravenBot.UpdateSessionInfo();
            }
        }

        if (RavenNest.Authenticated && RavenNest.SessionStarted)
        {
            var saveIntervalScale = Players.LoadingPlayers ? 5 : 1;

            if (gameStateTime >= 0)
            {
                if (!Players.LoadingPlayers && gameStateTime > stateSaveInterval)
                {
                    gameStateTime = stateSaveInterval;
                }

                gameStateTime -= GameTime.deltaTime;
                if (gameStateTime < 0f)
                {
                    SendGameState();
                    gameStateTime = stateSaveInterval * saveIntervalScale;
                }
            }

            if (experienceSaveTime >= 0)
            {
                // if we are no longer loading any players, we have to make sure our saveTimer is returned back to normal.
                if (!Players.LoadingPlayers && experienceSaveTime > experienceSaveInterval)
                {
                    experienceSaveTime = experienceSaveInterval;
                }

                experienceSaveTime -= GameTime.deltaTime;
                if (experienceSaveTime < 0f)
                {
                    // make sure to save everything every 6th time (every 30s)
                    SavePlayerExperience(experienceSaveIndex % 6 == 0);
                    experienceSaveTime = experienceSaveInterval * saveIntervalScale;
                    experienceSaveIndex++;
                }
            }

            if (stateSaveTime >= 0)
            {
                if (!Players.LoadingPlayers && stateSaveTime > stateSaveInterval)
                {
                    stateSaveTime = stateSaveInterval;
                }

                stateSaveTime -= GameTime.deltaTime;
                if (stateSaveTime < 0f)
                {
                    SavePlayerState();
                    stateSaveTime = stateSaveInterval * saveIntervalScale;
                }
            }
        }

        if (updateSessionInfoTime <= 0f)
        {
            updateSessionInfoTime = sessionUpdateFrequency;

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                ravenBot.UpdateSessionInfo();
            }
        }
    }

    public void StopRavenNestSession()
    {
        if (gameSessionActive)
        {
            SavePlayerExperience(false);
            SavePlayerState();
            //SaveIndividualPlayers();

            RavenNest.Terminate();
        }
    }

    private void SendGameState()
    {
        try
        {
            if (RavenNest == null)
            {
                return;
            }

            //#if UNITY_EDITOR
            //            Shinobytes.Debug.LogWarning("Sending Game State");
            //#endif
            RavenNest.SendGameState();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("GameManager.SendGameState: " + exc);
        }
    }

    private void SavePlayerExperience(bool saveAllSkills = false)
    {
        try
        {
            if (RavenNest == null)
            {
                return;
            }

            var players = playerManager.GetAllRealPlayers();
            if (players.Count == 0) return;
            //#if UNITY_EDITOR
            //            Shinobytes.Debug.LogWarning("Saving " + players.Count + " Player Experience: saveAllSkills=" + saveAllSkills);
            //#endif
            RavenNest.SavePlayerExperience(players, saveAllSkills);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("GameManager.SavePlayerExperience: " + exc);
        }
    }

    private void SavePlayerState()
    {
        try
        {
            if (playerManager == null) return;//not loaded yet?
            var players = playerManager.GetAllRealPlayers();
            if (players == null || players.Count == 0) return;
            //#if UNITY_EDITOR
            //            Shinobytes.Debug.LogWarning("Saving " + players.Count + " Player States");
            //#endif
            RavenNest.SavePlayerState(players);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("GameManager.SavePlayerState: " + exc);
        }
    }

    private void UpdateChatBotCommunication()
    {
        if (RavenBot == null || !RavenBot.IsConnected)
        {
            return;
        }

        if (ravenbotArgs == null)
        {
            ravenbotArgs = new object[] { this, RavenBot, playerManager };
        }

        RavenBot.HandleNextPacket(ravenbotArgs);
    }

    private void HandleKeyDown()
    {
        var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var isControlDown = isShiftDown || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetKeyUp(KeyCode.F2))
        {
            Camera.Observer.ToggleVisibility();
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            LogoCensor = !LogoCensor;
        }

        if (Input.GetKeyUp(KeyCode.I))
        {
            playerList.ToggleExpRate();
        }

        if (isControlDown && Input.GetKeyUp(KeyCode.F))
        {
            playerSearchHandler.Show();
        }


        if (isControlDown && Input.GetKeyUp(KeyCode.S))
        {
            SavePlayerExperience(false);
            SavePlayerState();
        }


        if (SessionSettings.IsAdministrator)
        {
            //if (isControlDown && Input.GetKeyUp(KeyCode.C))
            //{
            //    Twitch.OnCheer(new CheerBitsEvent("twitch", "zerratar", "72424639", "zerratar", "Zerratar", true, true, true, 10));
            //    return;
            //}

            //if (isControlDown && Input.GetKeyUp(KeyCode.X))
            //{
            //    Twitch.OnSubscribe(new UserSubscriptionEvent("twitch", "zerratar", "72424639", "zerratar", "Zerratar", null, true, true, 1, true));
            //    return;
            //}

            if (isControlDown && Input.GetKeyUp(KeyCode.P))
            {
                Raid.ForceStartRaid();
            }

            //if (isControlDown && Input.GetKeyUp(KeyCode.R))
            //{
            //    var players = Players.GetAllGameAdmins();
            //    Dungeons.RewardItemDrops(players);
            //}

            if (isControlDown && Input.GetKeyUp(KeyCode.O))
            {
                Dungeons.ForceActivateDungeon();
            }
        }
    }




    private void OnGUI()
    {
        if (!SessionSettings.IsAdministrator)
        {
            return;
        }

#if !DEBUG
        if (!isDebugMenuVisible)
        {
            return;
        }
#endif

        int buttonWidth = 150;
        int buttonMarginY = 5;
        int buttonHeight = 40;
        int buttonStartY = (Screen.height / 2) - (lastButtonIndex * (buttonHeight / 2));//60;

        Rect GetButtonRect(int i) => new Rect(Screen.width - buttonWidth - 20, buttonStartY + ((buttonHeight + buttonMarginY) * i), buttonWidth, buttonHeight);

        var buttonIndex = 0;

        if (GUI.Button(GetButtonRect(buttonIndex++), AdminControlData.NoChatBotMessages ? "Chat Notifications OFF" : "Chat Notifications ON"))
        {
            AdminControlData.NoChatBotMessages = !AdminControlData.NoChatBotMessages;
        }

        if (AdminControlData.ControlPlayers)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Control Bots"))
            {
                AdminControlData.ControlPlayers = false;
            }
        }
        else
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Control Players"))
            {
                AdminControlData.ControlPlayers = true;
            }
        }

        if (raidManager.Started)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Kill raid boss"))
            {
                var randomPlayer = raidManager.Raiders.Random();
                if (raidManager.Boss && randomPlayer)
                {
                    raidManager.Boss.Enemy.TakeDamage(randomPlayer, raidManager.Boss.Enemy.Stats.Health.CurrentValue);
                }
            }
        }
        else
        {

            if (GUI.Button(GetButtonRect(buttonIndex++), "Start Raid"))
            {
                raidManager.ForceStartRaid(/*Players.GetRandom()*/);
            }

        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Toggle Dungeon"))
        {
            dungeonManager.ToggleDungeon();
        }

        if (dungeonManager.Active)
        {
            if (GUI.Button(GetButtonRect(buttonIndex++), "Advance to next room"))
            {
                var players = dungeonManager.GetPlayers();
                foreach (var enemy in dungeonManager.Dungeon.Room.Enemies)
                {
                    var randomPlayer = players.Random();
                    if (!enemy.Stats.IsDead)
                    {
                        enemy.TakeDamage(randomPlayer, enemy.Stats.Health.MaxLevel);
                    }
                }

                foreach (var player in players)
                {
                    player.ClearTarget();
                }
            }

            if (!dungeonManager.Started)
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Start dungeon now"))
                {
                    dungeonManager.ForceStartDungeon();
                }
            }

            if (GUI.Button(GetButtonRect(buttonIndex++), "Damage Boss (-25% HP)"))
            {
                var randomPlayer = dungeonManager.GetPlayers().Random();
                if (randomPlayer && dungeonManager.Boss)
                {
                    var boss = dungeonManager.Boss.Enemy;
                    boss.TakeDamage(randomPlayer, (int)(boss.Stats.Health.Level * 0.25f));
                }
            }


            if (GUI.Button(GetButtonRect(buttonIndex++), "Kill Boss (-100% HP)"))
            {
                var randomPlayer = dungeonManager.GetPlayers().Random();
                if (randomPlayer && dungeonManager.Boss)
                {
                    var boss = dungeonManager.Boss.Enemy;
                    boss.TakeDamage(randomPlayer, boss.Stats.Health.Level);
                }
            }

        }

        if (!AdminControlData.TeleportationVisible && !AdminControlData.BotTrainingVisible && !AdminControlData.ControlPlayers)
        {
            if (AdminControlData.BotSpawnVisible)
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Level: " + AdminControlData.SpawnBotLevel))
                {
                    AdminControlData.SpawnBotLevel = (SpawnBotLevelStrategy)((((int)AdminControlData.SpawnBotLevel) + 1) % 3);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 1000 Train Rest"))
                {
                    SpawnManyBotPlayers(1000);
                    Debug_AutoRestOn();
                    Debug_TrainRandom();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 1000 Bots"))
                {
                    SpawnManyBotPlayers(1000);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 500 Bots"))
                {
                    SpawnManyBotPlayers(500);
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 300 Bots"))
                {
                    SpawnManyBotPlayers(300);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 200 Bots"))
                {
                    SpawnManyBotPlayers(200);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 100 Bots"))
                {
                    SpawnManyBotPlayers(100);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 10 Bots"))
                {
                    SpawnManyBotPlayers(10);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn a bot"))
                {
                    SpawnBotPlayer();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn a bot on ferry"))
                {
                    SpawnBotPlayerOnFerry();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Remove all except stuck"))
                {
                    var bots = Players.GetAllBots();
                    foreach (var b in bots)
                    {
                        if (b.IsStuck)
                            continue;

                        RemovePlayer(b, false);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.BotSpawnVisible = false;
                }

            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn Bots"))
                {
                    AdminControlData.BotSpawnVisible = true;
                }

                var count = Players.GetPlayerCount(true);

                if (count > 0 && GUI.Button(GetButtonRect(buttonIndex++), "Unstuck All"))
                {
                    foreach (var p in Players.GetAllPlayers())
                    {
                        p.Unstuck(true, 0);
                    }
                }
            }
        }

        if (!AdminControlData.TeleportationVisible && !AdminControlData.BotSpawnVisible && (spawnedBots > 0 || playerManager.GetPlayerCount() > 0))
        {
            if (AdminControlData.BotTrainingVisible)
            {

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Combat"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fighting", (new string[] { "all", "strength", "attack", "defense", "ranged", "magic" }).Random());
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Healing"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fighting", (new string[] { "healing" }).Random());
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Fishing"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("fishing", null);
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Woodcutting"))
                {
                    Debug_TrainWoodcutting();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Mining"))
                {
                    Debug_TrainMining();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Farming"))
                {
                    Debug_TrainFarming();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Cooking"))
                {
                    Debug_TrainCooking();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Alchemy"))
                {
                    Debug_TrainAlchemy();
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Crafting"))
                {
                    Debug_TrainCrafting();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Gathering"))
                {
                    Debug_TrainGathering();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Non Combat"))
                {
                    Debug_TrainNonCombat();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Healing 50/Combat 50"))
                {
                    Debug_TrainHealingCombat();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Random"))
                {
                    Debug_TrainRandom();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Rest"))
                {
                    Debug_Rest();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Auto Rest On"))
                {
                    Debug_AutoRestOn();
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Auto Rest Off"))
                {
                    Debug_AutoRestOff();
                }


                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.BotTrainingVisible = false;
                }
            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Training"))
                {
                    AdminControlData.BotTrainingVisible = true;
                }
            }
        }

        if (!AdminControlData.BotSpawnVisible && !AdminControlData.BotTrainingVisible && (spawnedBots > 0 || playerManager.GetPlayerCount() > 0))
        {
            if (AdminControlData.TeleportationVisible)
            {

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Home"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Home");
                    foreach (var bot in bots) bot.ferryHandler.Embark(island);
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) bot.ferryHandler.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) bot.ferryHandler.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) bot.ferryHandler.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) bot.ferryHandler.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Home"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Home");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Atria"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Atria");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Eldara"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Eldara");
                    foreach (var bot in bots) TeleportBot(bot, island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "<< Back"))
                {
                    AdminControlData.TeleportationVisible = false;
                }
            }
            else
            {
                if (GUI.Button(GetButtonRect(buttonIndex++), "Traveling"))
                {
                    AdminControlData.TeleportationVisible = true;
                }
            }
        }


        //if (GUI.Button(GetButtonRect(buttonIndex++), dungeonManager.Active ? "Stop Dungeon" : "Start Dungeon"))
        //{
        //    dungeonManager.ToggleDungeon();
        //}
        //if (GUI.Button(GetButtonRect(buttonIndex++), "Start Raid"))
        //{
        //    raidManager.StartRaid("admin");
        //}
        //if (this.raidManager.Started)
        //{
        //    if (GUI.Button(GetButtonRect(buttonIndex++), "Join Raid"))
        //    {
        //        var bots = this.playerManager.GetAllBots();
        //        foreach (var bot in bots)
        //        {
        //            Raid.Join(bot);
        //        }
        //    }
        //}
        //if (this.dungeonManager.Active)
        //{
        //    if (GUI.Button(GetButtonRect(buttonIndex++), "Join Dungeon"))
        //    {
        //        var bots = this.playerManager.GetAllBots();
        //        foreach (var bot in bots)
        //        {
        //            dungeonManager.Join(bot);
        //        }
        //    }
        //}
        //if (GUI.Button(GetButtonRect(buttonIndex++), "Start Stop 100 Dungeons"))
        //{
        //    StartCoroutine(ToggleManyDungeons(100));
        //}


        lastButtonIndex = buttonIndex;
    }

    private void Debug_TrainWoodcutting()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("woodcutting", null);
        }
    }

    private void Debug_TrainMining()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("mining", null);
        }
    }

    private void Debug_TrainFarming()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("farming", null);
        }
    }

    private void Debug_TrainCooking()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("Cooking", null);
        }
    }

    private void Debug_TrainAlchemy()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("Alchemy", null);
        }
    }

    private void Debug_TrainCrafting()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("Crafting", null);
        }
    }

    private void Debug_TrainGathering()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask("Gathering", null);
        }
    }

    private void Debug_TrainNonCombat()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.SetTask((new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting", "alchemy", "gathering" }).Random());
        }
    }

    private void Debug_TrainHealingCombat()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        var c = bots.Count / 2;
        var i = 0;
        foreach (var bot in bots)
        {
            var task = "fighting";
            var subTask = (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", }).Random();
            if (i++ <= c)
            {
                subTask = "healing";
            }
            bot.SetTask(task, subTask);
        }
    }

    private void Debug_AutoRestOff()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.onsenHandler.ClearAutoRest();
        }
    }

    private void Debug_AutoRestOn()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            bot.onsenHandler.SetAutoRest(0, 5);
        }
    }

    private void Debug_Rest()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            Onsen.Join(bot);
        }
    }

    private void Debug_TrainRandom()
    {
        var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
        foreach (var bot in bots)
        {
            var s = (new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting", "gathering", "alchemy", "fighting" }).Random();
            if (s == "fighting")
            {
                bot.SetTask(s, (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", "healing" }).Random());
            }
            else
            {
                bot.SetTask(s);
            }
        }
    }

    void TeleportBot(PlayerController player, IslandController island)
    {
        player.teleportHandler.Teleport(island.SpawnPosition);
        if (player.IsBot)
        {
            player.Bot.LastTeleport = Time.time;
        }

        player.ClearTarget();
        if (!string.IsNullOrEmpty(player.CurrentTaskName))
            player.SetTask(player.CurrentTaskName, player.taskArgument, true);
    }


    private IEnumerator ToggleManyDungeons(int count)
    {
        for (var i = 0; i < count; ++i)
        {
            dungeonManager.ToggleDungeon();
            yield return null;
        }
    }

    private void UpdatePlayerKickQueue()
    {
        if (PlayerSettings.Instance.AutoKickAfkPlayers.GetValueOrDefault())
        {
            var players = Players
                .GetAllPlayers()
                .Where(x => x.IsAfk)
                .ToList();

            foreach (var p in players)
            {
                RemovePlayer(p);
            }
        }

        if (playerKickQueue.Count <= 0)// || !Arena || Arena.Started)
        {
            return;
        }

        var player = playerKickQueue.Dequeue();
        //if (player.duelHandler.InDuel)
        //{
        //    playerKickQueue.Enqueue(player);
        //    return;
        //}

        RemovePlayer(player);
    }

    private void UpdateExpBoostTimer()
    {
        if (!boostTimer) return;

        expBoostTimerUpdate -= GameTime.deltaTime;
        if (expBoostTimerUpdate > 0f) return;

        boostTimer.SetActive(subEventManager.CurrentBoost.Active);


        if (subEventManager.CurrentBoost.Active)
        {
            var duration = subEventManager.Duration ?? TimeSpan.Zero;
            var remainingTime = subEventManager.TimeLeft ?? TimeSpan.Zero;
            var secondsLeft = (float)remainingTime.TotalSeconds;
            var secondsLeftInt = Mathf.FloorToInt(secondsLeft);
            var secondsTotal = (float)duration.TotalSeconds;
            var timeLeft = $"{secondsLeftInt} sec";
            if (secondsLeftInt > 3600)
            {
                timeLeft = $"{Mathf.FloorToInt(secondsLeftInt / 3600f)} hours";
                var minutes = secondsLeftInt / 60;
                minutes = (int)(minutes % 60);
                if (minutes > 0)
                {
                    timeLeft += " " + (int)+minutes + "min";
                }
            }
            else if (secondsLeftInt > 60)
                timeLeft = $"{Mathf.FloorToInt(secondsLeftInt / 60f)} mins";
            boostTimer.SetSubscriber(subEventManager.CurrentBoost.EventName, !subEventManager.CurrentBoost.EventName.Contains(" "));
            boostTimer.SetText($"EXP Multiplier x{subEventManager.CurrentBoost.Multiplier} - {timeLeft}");
            boostTimer.SetTime(secondsLeft, secondsTotal);
        }

        expBoostTimerUpdate = 0.5f;
    }

    private void UpdateApiCommunication()
    {
        HandleRavenNestConnection();

        if (RavenNest != null)
        {
            RavenNestUpdate();
        }
    }

    internal void OnUpdateAvailable(string newVersion)
    {
        goUpdateAvailable.SetActive(true);
        lblUpdateAvailable.text = "New update available! <color=green>v" + newVersion;
        NewUpdateAvailable = true;
    }
    internal void AnnounceAutoDungeonJoin(List<PlayerController> players)
    {
        if (players.Count == 0) return;
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));
        if (players.Count == 1)
        {
            var plr = players[0];
            if (plr.dungeonHandler.AutoJoinCounter > 0 && plr.dungeonHandler.AutoJoinCounter < int.MaxValue)
            {
                RavenBot.SendReply(plr, "You have automatically joined the dungeon! You will join {counter} more.", plr.dungeonHandler.AutoJoinCounter.ToString());
            }
            else
            {
                RavenBot.SendReply(plr, "You have automatically joined the dungeon!");
            }
        }
        else if (players.Count < 10)
        {
            RavenBot.Announce("{playerNames} have automatically joined the dungeon!", playerNames);
        }
        else
        {
            RavenBot.Announce("{playerCount} players have automatically joined the dungeon!", players.Count.ToString());
        }
    }

    internal void AnnounceAutoDungeonJoinFailed(List<PlayerController> players)
    {
        if (players.Count == 0) return;

        var playersToReport = players.Where(x => x.LastChatCommandUtc <= x.LastDungeonAutoJoinFailUtc || x.LastDungeonAutoJoinFailUtc <= DateTime.UnixEpoch).ToList();
        if (playersToReport.Count > 0)
        {
            var playerNames = string.Join(", ", playersToReport.Select(x => "@" + x.Name));
            if (playersToReport.Count == 1)
            {
                RavenBot.SendReply(playersToReport[0], "You have failed to join the dungeon. Make sure you have enough coins to automatically join.");
            }
            else if (playersToReport.Count < 10)
            {
                RavenBot.Announce("{playerNames} failed to joined the dungeon. You may not have enough coins.", playerNames);
            }
            else
            {
                RavenBot.Announce("{playerCount} players failed to join the dungeon.", playersToReport.Count.ToString());
            }
        }

        foreach (var plr in players) plr.LastDungeonAutoJoinFailUtc = DateTime.UtcNow;
    }

    internal void AnnounceAutoRaidJoinFailed(List<PlayerController> players)
    {
        if (players.Count == 0) return;

        var playersToReport = players.Where(x => x.LastChatCommandUtc <= x.LastRaidAutoJoinFailUtc || x.LastRaidAutoJoinFailUtc <= DateTime.UnixEpoch).ToList();
        if (playersToReport.Count > 0)
        {
            var playerNames = string.Join(", ", playersToReport.Select(x => "@" + x.Name));
            if (playersToReport.Count == 1)
            {
                RavenBot.SendReply(playersToReport[0], "You have failed to join the raid. Make sure you have enough coins to automatically join.");
            }
            else if (playersToReport.Count < 10)
            {
                RavenBot.Announce("{playerNames} failed to joined the raid. You may not have enough coins.", playerNames);
            }
            else
            {
                RavenBot.Announce("{playerCount} players failed to join the raid.", playersToReport.Count.ToString());
            }
        }

        foreach (var plr in players) plr.LastRaidAutoJoinFailUtc = DateTime.UtcNow;
    }

    internal void AnnounceAutoRaidJoin(List<PlayerController> players)
    {
        if (players.Count == 0) return;
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));
        if (players.Count == 1)
        {
            var plr = players[0];
            if (plr.raidHandler.AutoJoinCounter > 0 && plr.raidHandler.AutoJoinCounter < int.MaxValue)
            {
                RavenBot.SendReply(plr, "You have automatically joined the raid! You will join {counter} more.", plr.raidHandler.AutoJoinCounter);
            }
            else
            {
                RavenBot.SendReply(plr, "You have automatically joined the raid!");
            }
        }
        else if (players.Count < 10)
        {
            RavenBot.Announce("{playerNames} have automatically joined the raid!", playerNames);
        }
        else
        {
            RavenBot.Announce("{playerCount} players have automatically joined the raid!", players.Count.ToString());
        }
    }
}

public class IslandTaskCollection
{
    public string Island { get; set; }

    public List<IslandTask> Skills { get; set; }

}
public class IslandTask
{
    public string Name { get; set; }
    public int SkillLevelRequirement { get; set; }
    public int CombatLevelRequirement { get; set; }
}
public class GameTime
{
    public static float deltaTime;
    public static float time;
    public static DateTime now;
}

public class SessionStats
{
    public bool Authenticated { get; set; }
    public bool SessionStarted { get; set; }
    public string TwitchUserName { get; set; }
    public Version GameVersion { get; set; }
    public float RealtimeSinceStartup { get; set; }
    public int OnlinePlayerCount { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public class VillageStats
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int Tier { get; set; }
    public ICollection<TownHouseExpBonus> BonusExp { get; set; }
    public string Boost { get; set; }
}

public class TownHouseExpBonus
{
    [JsonConverter(typeof(StringEnumConverter))]
    public TownHouseSlotType SlotType { get; set; }
    public float Bonus { get; set; }

    public TownHouseExpBonus() { }
    public TownHouseExpBonus(TownHouseSlotType slotType, float bonus)
    {
        SlotType = slotType;
        Bonus = bonus;
    }
}
public class RaidStats
{
    public int Counter { get; set; }
    public bool Started { get; set; }
    public int PlayersCount { get; set; }
    public float SecondsLeft { get; set; }
    public int BossHealthCurrent { get; set; }
    public int BossHealthMax { get; set; }
    public float BossHealthPercent { get; set; }
    public int BossLevel { get; set; }
}

public class DungeonStats
{
    public int Counter { get; set; }
    public bool Started { get; set; }
    public int PlayersCount { get; set; }
    public int PlayersLeft { get; set; }
    public int EnemiesLeft { get; set; }
    public int BossHealthCurrent { get; set; }
    public int BossHealthMax { get; set; }
    public float BossHealthPercent { get; set; }
    public int BossLevel { get; set; }
    public int ActivatedCount { get; set; }
    public float Runtime { get; set; }
}

public class FerryStats
{
    public string Destination { get; set; }
    public int PlayersCount { get; set; }
    public int CaptainSailingLevel { get; set; }
    public string CaptainName { get; set; }
}

public class ServerTime
{
    public static TimeSpan TimeDelta;
    public static DateTime UtcNow => DateTime.UtcNow + TimeDelta;
}

public class FreezeChecker
{
    private const int interval = 1000;
    private static System.Threading.Thread freezeCheckThread;
    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);
    private static readonly object mutex = new object();

    private static volatile bool isRunning;
    private static DateTime lastChange;

    private static string currentObjectName;
    private static string currentScriptMethodName;
    private static string currentScriptFileName;
    private static Thread currentScriptThread;

    public static void Start()
    {
        if (isRunning)
        {
            // we should clear out stacktraces and other stuff
            Shinobytes.Debug.Log("Restarting freeze checker");
            lock (mutex)
            {
                currentObjectName = null;
                currentScriptFileName = null;
                currentScriptMethodName = null;
                currentScriptThread = null;
                lastChange = DateTime.UtcNow;
            }
        }
        else
        {
            lastChange = DateTime.UtcNow;
            isRunning = true;
            Shinobytes.Debug.Log("Starting freeze checker");
            freezeCheckThread = new(Run);
            freezeCheckThread.IsBackground = true;
            freezeCheckThread.Name = "Freeze Check";
            freezeCheckThread.Start();
        }
    }

    public static void SetCurrentScriptUpdate(string objectName, [CallerMemberName] string scriptMethodName = null, [CallerFilePath] string scriptFile = null)
    {
        lock (mutex)
        {
            lastChange = DateTime.UtcNow;
            currentObjectName = objectName;
            currentScriptMethodName = scriptMethodName;
            currentScriptFileName = scriptFile;
            currentScriptThread = Thread.CurrentThread;
        }
    }

    private static void Run(object obj)
    {
        lastChange = DateTime.UtcNow;
        var updateInterval = interval;
        while (isRunning)
        {
            lock (mutex)
            {
                var timeSinceLastChange = DateTime.UtcNow - lastChange;
                if (timeSinceLastChange > timeout && currentScriptFileName != null)
                {
                    // most likely we have a crash or freeze
                    // we can also try and determine that the crash or freeze happened in the currentScriptUpdate.
                    Debug.LogError("!!Freeze Detected!! Possible Culprit: " + currentScriptFileName + "::" + currentScriptMethodName + " (" + currentObjectName + ")");
                    updateInterval = 30000;
                }
                else
                {
                    updateInterval = interval;
                }
            }
            System.Threading.Thread.Sleep(updateInterval);
        }
        isRunning = false;
    }

    public static void Stop()
    {
        Shinobytes.Debug.Log("Stopping freeze checker...");
        isRunning = false;
    }
}