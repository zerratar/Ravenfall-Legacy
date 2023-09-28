﻿using System;
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

public class GameManager : MonoBehaviour, IGameManager
{
    [Header("Default Settings")]
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private RavenBot ravenBot;

    [SerializeField] private PlayerDetails playerObserver;
    [SerializeField] private PlayerList playerList;
    [SerializeField] private GameSettings settings;

    [SerializeField] private TwitchEventManager subEventManager;
    [SerializeField] private DropEventManager dropEventManager;

    [SerializeField] private BATimer boostTimer;
    [SerializeField] private PlayerSearchHandler playerSearchHandler;
    [SerializeField] private GameMenuHandler menuHandler;

    [SerializeField] private string accessKey;

    [SerializeField] private FerryProgress ferryProgress;
    [SerializeField] private GameObject exitView;
    [SerializeField] private MusicManager musicManager;

    [SerializeField] private IslandManager islandManager;
    [SerializeField] private DungeonManager dungeonManager;

    [SerializeField] private VillageManager villageManager;
    [SerializeField] private OnsenManager onsen;

    [SerializeField] private PlayerLogoManager playerLogoManager;
    [SerializeField] private ServerNotificationManager serverNotificationManager;
    [SerializeField] private LoginHandler loginHandler;

    [SerializeField] private GameObject gameReloadMessage;
    [SerializeField] private TavernHandler tavern;
    [SerializeField] private DayNightCycle dayNightCycle;

    [SerializeField] private GameObject gameReloadUIPanel;
    [SerializeField] private Volume postProcessingEffects;

    [Header("Game Update Banner")]
    [SerializeField] private GameObject goUpdateAvailable;
    [SerializeField] private TMPro.TextMeshProUGUI lblUpdateAvailable;

    [Header("Graphics Settings")]
    public RenderPipelineAsset URP_LowQuality;
    public RenderPipelineAsset URP_DefaultQuality;

    private readonly ConcurrentDictionary<GameEventType, IGameEventHandler> gameEventHandlers = new();
    private readonly ConcurrentDictionary<Type, IGameEventHandler> typedGameEventHandlers = new();

    private readonly ConcurrentQueue<GameEvent> gameEventQueue = new ConcurrentQueue<GameEvent>();
    private readonly Queue<PlayerController> playerKickQueue = new Queue<PlayerController>();
    private readonly ConcurrentDictionary<string, LoadingState> loadingStates
        = new ConcurrentDictionary<string, LoadingState>();

    private readonly GameEventManager events = new GameEventManager();
    private IoCContainer ioc;

    [SerializeField] private FerryController ferryController;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private RaidManager raidManager;
    [SerializeField] private StreamRaidManager streamRaidManager;
    [SerializeField] private ArenaController arenaController;
    [SerializeField] private ItemManager itemManager;

    private int lastButtonIndex = 0;

    private float stateSaveTime = 1f;
    private float experienceSaveTime = 1f;

    private float experienceSaveInterval = 3f;
    private float stateSaveInterval = 3f;

    private SessionStats sessionStats;
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
    private DateTime dungeonStartTime;
    private DateTime raidStartTime;
    private ClanManager clanManager;

    private readonly TimeSpan dungeonStartCooldown = TimeSpan.FromMinutes(10);
    private readonly TimeSpan raidStartCooldown = TimeSpan.FromMinutes(10);
    private NameTagManager nametagManager;

    public StreamLabel uptimeLabel;
    public StreamLabel villageBoostLabel;
    public StreamLabel playerCountLabel;

    public StreamLabel expMultiplierJson;
    public StreamLabel sessionStatsJson;
    public StreamLabel ferryStatsJson;
    public StreamLabel raidStatsJson;
    public StreamLabel dungeonStatsJson;
    public StreamLabel villageStatsJson;

    private Permissions permissions = new Permissions { ExpMultiplierLimit = 100 };
    public Permissions Permissions
    {
        get { return permissions; }
        set
        {
            permissions = value;
            if (value != null)
            {
                AdminControlData.IsAdmin = permissions.IsAdministrator;
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
    private float lastAutoJoinDungeon;
    private float autoJoinDungeonAnnouncement;
    private float lastAutoJoinRaid;
    private float autoJoinRaidAnnouncement;

    void Awake()
    {
        goUpdateAvailable.SetActive(false);
        GameTime.deltaTime = Time.deltaTime;
        //Physics.autoSimulation = false;
        BatchPlayerAddInProgress = false;
        overlay = gameObject.AddComponent<Overlay>();
        if (!settings) settings = GetComponent<GameSettings>();
        this.StreamLabels = new StreamLabels(settings);
        gameReloadUIPanel.SetActive(false);
        Overlay.CheckIfGame();
        GameSystems.Awake();
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

        gameReloadMessage.SetActive(false);
        if (!Graphics) Graphics = FindObjectOfType<GraphicsToggler>();
        if (!nametagManager) nametagManager = FindObjectOfType<NameTagManager>();
        if (!dayNightCycle) dayNightCycle = GetComponent<DayNightCycle>();
        if (!onsen) onsen = GetComponent<OnsenManager>();
        if (!loginHandler) loginHandler = FindObjectOfType<LoginHandler>();
        if (!dropEventManager) dropEventManager = GetComponent<DropEventManager>();
        if (!ferryProgress) ferryProgress = FindObjectOfType<FerryProgress>();
        if (!gameCamera) gameCamera = FindObjectOfType<GameCamera>();

        if (!playerLogoManager) playerLogoManager = GetComponent<PlayerLogoManager>();
        if (!villageManager) villageManager = FindObjectOfType<VillageManager>();

        if (!subEventManager) subEventManager = GetComponent<TwitchEventManager>();
        if (!subEventManager) subEventManager = gameObject.AddComponent<TwitchEventManager>();

        if (!islandManager) islandManager = GetComponent<IslandManager>();
        if (!itemManager) itemManager = GetComponent<ItemManager>();
        if (!playerManager) playerManager = GetComponent<PlayerManager>();
        if (!chunkManager) chunkManager = GetComponent<ChunkManager>();
        if (!craftingManager) craftingManager = GetComponent<CraftingManager>();
        if (!raidManager) raidManager = GetComponent<RaidManager>();
        if (!streamRaidManager) streamRaidManager = GetComponent<StreamRaidManager>();
        if (!arenaController) arenaController = FindObjectOfType<ArenaController>();

        if (!ferryController) ferryController = FindObjectOfType<FerryController>();
        if (!musicManager) musicManager = GetComponent<MusicManager>();

        RegisterGameEventHandler<ItemAddEventHandler>(GameEventType.ItemAdd);
        RegisterGameEventHandler<ResourceUpdateEventHandler>(GameEventType.ResourceUpdate);
        RegisterGameEventHandler<ServerMessageEventHandler>(GameEventType.ServerMessage);

        RegisterGameEventHandler<PermissionChangedEventHandler>(GameEventType.PermissionChange);
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
        RegisterGameEventHandler<ItemUnequipEventHandler>(GameEventType.ItemUnEquip);
        RegisterGameEventHandler<ItemEquipEventHandler>(GameEventType.ItemEquip);

        LoadGameSettings();

        musicManager.PlayBackgroundMusic();

        gameCacheStateFileLoadResult = GameCache.LoadState();

        GameSystems.Start();
    }

    public PlayerItemDropText AddItems(EventItemReward[] rewards)
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

        villageBoostLabel = StreamLabels.RegisterText("village-boost", () =>
        {
            var bonuses = Village.GetExpBonuses();

            var value = bonuses.Where(x => x.Bonus > 0)
             .GroupBy(x => x.SlotType)
             .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
             .Select(x => $"{x.Key} {x.Value.Sum(y => y.Bonus)}%");


            return string.Join(", ", value);
        });

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
    private ExpBoostEvent GetExpMultiplierStats()
    {
        var boost = subEventManager.CurrentBoost;
        if (boost.Active)
        {
            return boost;
        }

        return emptySubBoost;
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
        Raid.Notifications.volume = settings.RaidHornVolume.GetValueOrDefault(Raid.Notifications.volume);
        Music.volume = settings.MusicVolume.GetValueOrDefault(Music.volume);

        OrbitCamera.RotationSpeed = settings.CameraRotationSpeed.GetValueOrDefault(OrbitCamera.RotationSpeed);
        SettingsMenuView.SetResolutionScale(settings.DPIScale.GetValueOrDefault(1f));
    }

    internal void OnSessionStart()
    {
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

        SaveStateFile();
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

    private IEnumerator RestoreGameState(GameCacheState state)
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
        GameTime.deltaTime = Time.deltaTime;

        if (isReloadingScene)
        {
            return;
        }

        GameSystems.Update();

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

            if (URP_LowQuality)
            {
                GraphicsSettings.renderPipelineAsset = URP_LowQuality;
            }

            DisablePostProcessingEffects();
        }
        else
        {
            if (currentQualityLevel != 1)
            {
                QualitySettings.SetQualityLevel(1);
            }

            if (URP_DefaultQuality)
            {
                GraphicsSettings.renderPipelineAsset = URP_DefaultQuality;
            }

            EnablePostProcessingEffects();
        }

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

        UpdateAutoJoinAnnouncement();

        if (Input.GetKeyUp(KeyCode.Escape) && !menuHandler.Visible && !playerSearchHandler.Visible)
        {
            menuHandler.Show();
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

        ExpMultiplierChecker.RunAsync(this);

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

        if (uptimeSaveTimer <= 0)
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
        if (!UsePostProcessingEffects)
        {
            DisablePostProcessingEffects();
            return;
        }

        postProcessingEffects.weight = 0.625f;
    }
    public void DisablePostProcessingEffects()
    {
        postProcessingEffects.weight = 0f;
    }

    private void UpdateIntegrityCheck()
    {
        IntegrityCheck.Update();
    }

    private bool UpdateGameEvents()
    {
#if DEBUG
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var eventsHandled = 0;
#endif

        while (gameEventQueue.TryDequeue(out var ge))
        {
            HandleGameEvent(ge);

#if DEBUG
            eventsHandled++;
#endif
        }

#if DEBUG
        sw.Stop();
        if (sw.ElapsedMilliseconds > 30)
        {
            Shinobytes.Debug.LogError("UpdateGameEvents took a long time! " + sw.ElapsedMilliseconds + "ms for " + eventsHandled + " events!");
        }
#endif
        return true;
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
            if (player.Dungeon.InDungeon)
            {
                dungeonManager.Remove(player);
            }

            if (player.Raid.InRaid)
            {
                raidManager.Leave(player);
            }

            if (player.Ferry.OnFerry)
            {
                player.Ferry.RemoveFromFerry();
            }

            if (notifyServer)
            {
                RavenNest.PlayerRemoveAsync(player);
            }

            player.Removed = true;
            playerList.RemovePlayer(player);
            playerManager.Remove(player);

            if (gameCamera.Observer != null && gameCamera.Observer.ObservedPlayer == player)
            {
                gameCamera.ObservePlayer(null);
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
        var qualitySettingsIndex = PlayerSettings.Instance.PathfindingQualitySettings.GetValueOrDefault(1);
        var min = SettingsMenuView.PathfindingQualityMin[qualitySettingsIndex];
        var max = SettingsMenuView.PathfindingQualityMax[qualitySettingsIndex];
        var value = playerManager.GetPlayerCount() * 2;
        NavMesh.pathfindingIterationsPerFrame = Mathf.Min(Mathf.Max(min, value), max);
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

        playerList.AddPlayer(player);

        if (!isGameRestore && !BatchPlayerAddInProgress)
        {
            Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());

            playerCountLabel.Update();
            villageBoostLabel.Update();

            if (player && gameCamera && gameCamera.AllowJoinObserve)
                gameCamera.ObservePlayer(player);
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
            Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());
            playerCountLabel.Update();
            villageBoostLabel.Update();

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
        if (RavenBotController != null)
        {
            RavenBotController.Dispose();
        }

        StopRavenNestSession();

        Debug.Log("Application ending after " + Time.time + " seconds");
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

            // after RavenNestClient has been initialized, initialize RavenBot connection.

            ravenBot = new RavenBot(logger, client, this);
        }

        if (client != null)
        {
            if (!string.IsNullOrEmpty(RavenNest.ServerAddress))
                ServerAddress = RavenNest.ServerAddress;
        }
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
            Shinobytes.Debug.LogError(exc);
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
            Shinobytes.Debug.LogError(exc);
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
            Shinobytes.Debug.LogError(exc);
        }
    }

    private void UpdateChatBotCommunication()
    {
        if (RavenBot == null || !RavenBot.IsConnected)
        {
            return;
        }

        RavenBot.HandleNextPacket(this, RavenBot, playerManager);
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


        if (Permissions.IsAdministrator)
        {
            if (isControlDown && Input.GetKeyUp(KeyCode.C))
            {
                Twitch.OnCheer(new CheerBitsEvent("twitch", "zerratar", "72424639", "zerratar", "Zerratar", true, true, true, 10));
                return;
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.X))
            {
                Twitch.OnSubscribe(new UserSubscriptionEvent("twitch", "zerratar", "72424639", "zerratar", "Zerratar", null, true, true, 1, true));
                return;
            }

            //if (isControlDown && Input.GetKeyUp(KeyCode.R))
            //{
            //    Raid.StartRaid();
            //}

            if (isControlDown && Input.GetKeyUp(KeyCode.R))
            {
                var players = Players.GetAllGameAdmins();
                Dungeons.RewardItemDrops(players);
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.O))
            {
                var elapsed = DateTime.UtcNow - dungeonStartTime;
                if (elapsed > dungeonStartCooldown || Permissions.IsAdministrator)
                {
                    dungeonStartTime = DateTime.UtcNow;
                    Dungeons.ActivateDungeon();
                }
                else
                {
                    var timeLeft = dungeonStartCooldown - elapsed;
                    RavenBot.Announce("You have to wait {cooldown} second(s) before you can start another raid.", timeLeft.TotalSeconds.ToString());
                }
            }
        }
    }

#if DEBUG



    private void OnGUI()
    {
        if (!Permissions.IsAdministrator)
        {
            return;
        }

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
                raidManager.StartRaid();
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
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask("woodcutting", null);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Mining"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "mining" }).Random(), null);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Farming"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "farming" }).Random(), null);
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Crafting"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "Crafting" }).Random(), null);
                    }
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Gathering"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        bot.SetTask((new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting" }).Random());
                    }
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Healing 50/Combat 50"))
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

                if (GUI.Button(GetButtonRect(buttonIndex++), "Train Random"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        var s = (new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting", "fighting" }).Random();
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

                if (GUI.Button(GetButtonRect(buttonIndex++), "Rest"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    foreach (var bot in bots)
                    {
                        Onsen.Join(bot);
                    }
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
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }
                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Sail Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Home"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Home");
                    foreach (var bot in bots) bot.Ferry.Embark(island);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Away"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Away");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Ironhill"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Ironhill");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Kyo"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Kyo");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
                }

                if (GUI.Button(GetButtonRect(buttonIndex++), "Teleport Heim"))
                {
                    var bots = AdminControlData.ControlPlayers ? this.playerManager.GetAllPlayers() : this.playerManager.GetAllBots();
                    var island = Islands.Find("Heim");
                    foreach (var bot in bots) bot.Teleporter.Teleport(island.SpawnPosition);
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
#endif

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
        //if (player.Duel.InDuel)
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
    }

    internal void UpdateAutoJoinAnnouncement()
    {
        if (playerAutoJoinList.Count == 0) return;
        var playerNames = string.Join(", ", playerAutoJoinList.Select(x => "@" + x.Name));
        var timeNow = Time.realtimeSinceStartup;
        if (timeNow >= autoJoinDungeonAnnouncement && autoJoinDungeonAnnouncement > 0)
        {
            AnnounceAutoDungeonJoin(playerAutoJoinList);
            playerAutoJoinList.Clear();
            autoJoinDungeonAnnouncement = 0;
            return;
        }

        if (timeNow >= autoJoinRaidAnnouncement && autoJoinRaidAnnouncement > 0)
        {
            AnnounceAutoRaidJoin(playerAutoJoinList);
            playerAutoJoinList.Clear();
            autoJoinRaidAnnouncement = 0;
        }
    }

    internal void AnnounceAutoDungeonJoin(List<PlayerController> players)
    {
        if (players.Count == 0) return;
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));
        if (players.Count == 1)
        {
            var plr = players[0];
            if (plr.Dungeon.AutoJoinCounter > 0 && plr.Dungeon.AutoJoinCounter < int.MaxValue)
            {
                RavenBot.SendReply(plr, "You have automatically joined the dungeon! You will join {counter} more.", plr.Dungeon.AutoJoinCounter.ToString());
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
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));

        if (players.Count == 1)
        {
            if (players[0].LastChatCommandUtc > players[0].LastDungeonAutoJoinFailUtc || players[0].LastDungeonAutoJoinFailUtc <= DateTime.UnixEpoch)
                RavenBot.SendReply(players[0], "You have failed to join the dungeon. Make sure you have enough coins to automatically join.");
        }
        else if (players.Count < 10)
        {
            RavenBot.Announce("{playerNames} failed to joined the dungeon. You may not have enough coins.", playerNames);
        }
        else
        {
            RavenBot.Announce("{playerCount} players failed to join the dungeon.", players.Count.ToString());
        }

        foreach (var plr in players) plr.LastDungeonAutoJoinFailUtc = DateTime.UtcNow;
    }

    internal void AnnounceAutoRaidJoin(List<PlayerController> players)
    {
        if (players.Count == 0) return;
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));
        if (players.Count == 1)
        {
            var plr = players[0];
            if (plr.Raid.AutoJoinCounter > 0 && plr.Raid.AutoJoinCounter < int.MaxValue)
            {
                RavenBot.SendReply(plr, "You have automatically joined the raid! You will join {counter} more.", plr.Raid.AutoJoinCounter);
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

    internal void AnnounceAutoRaidJoinFailed(List<PlayerController> players)
    {
        if (players.Count == 0) return;
        var playerNames = string.Join(", ", players.Select(x => "@" + x.Name));
        if (players.Count == 1)
        {
            if (players[0].LastChatCommandUtc > players[0].LastRaidAutoJoinFailUtc || players[0].LastRaidAutoJoinFailUtc <= DateTime.UnixEpoch)
            {
                RavenBot.SendReply(players[0], "You have failed to join the raid. Make sure you have enough coins to automatically join.");
            }
        }
        else if (players.Count < 10)
        {
            RavenBot.Announce("{playerNames} failed to joined the raid. You may not have enough coins.", playerNames);
        }
        else
        {
            RavenBot.Announce("{playerCount} players failed to join the raid.", players.Count.ToString());
        }

        foreach (var plr in players) plr.LastRaidAutoJoinFailUtc = DateTime.UtcNow;
    }

    internal void OnPlayerAutoJoinedDungeon(PlayerController player)
    {
        lastAutoJoinDungeon = Time.realtimeSinceStartup;
        autoJoinDungeonAnnouncement = lastAutoJoinDungeon + 1.25f;
        playerAutoJoinList.Add(player);
    }

    internal void OnPlayerAutoJoinedRaid(PlayerController player)
    {
        lastAutoJoinRaid = Time.realtimeSinceStartup;
        autoJoinRaidAnnouncement = lastAutoJoinDungeon + 1.25f;
        playerAutoJoinList.Add(player);
    }

    internal async Task HandleRaidAutoJoin(PlayerController ignore = null)
    {
        var autoJoinList = new List<Guid>();
        var autoJoinPlayers = new List<PlayerController>();
        var success = new List<PlayerController>();
        var failed = new List<PlayerController>();
        foreach (var player in Players.GetAllPlayers())
        {
            if (Raid.CanJoin(player) != RaidJoinResult.CanJoin || player.Raid.AutoJoining)
                continue;

            if (ignore != null && ignore == player)
            {
                player.Raid.AutoJoining = true;
                continue;
            }

            if (player.Raid.AutoJoinCounter > 0)
            {
                player.Raid.AutoJoining = true;
                autoJoinList.Add(player.Id);
                autoJoinPlayers.Add(player);
            }
        }

        var result = await RavenNest.Players.AutoJoinRaid(autoJoinList.ToArray());
        if (result == null || result.Length == 0)
        {
            failed = autoJoinPlayers;
        }
        else
        {
            for (var i = 0; i < result.Length; ++i)
            {
                var playerResult = result[i];
                var plr = autoJoinPlayers[i];
                if (playerResult)
                {
                    Raid.Join(plr);
                    success.Add(plr);
                    if (plr.Raid.AutoJoinCounter != int.MaxValue)
                    {
                        plr.Raid.AutoJoinCounter--;
                    }
                    plr.Resources.Coins -= RaidAuto.autoJoinCost;
                }
                else
                {
                    failed.Add(plr);
                }
            }
        }
        foreach (var plr in autoJoinPlayers)
        {
            plr.Raid.AutoJoining = false;
        }

        AnnounceAutoRaidJoin(success);

        if (failed.Count > 0)
        {
            AnnounceAutoRaidJoinFailed(failed);
        }
    }

    internal async Task HandleDungeonAutoJoin(PlayerController ignore = null)
    {
        var autoJoinList = new List<Guid>();
        var autoJoinPlayers = new List<PlayerController>();
        var success = new List<PlayerController>();
        var failed = new List<PlayerController>();

        foreach (var player in Players.GetAllPlayers())
        {
            if (Dungeons.CanJoin(player) != DungeonJoinResult.CanJoin)
                continue;

            if (ignore != null && ignore == player)
            {
                player.Dungeon.AutoJoining = true;
                continue;
            }

            if (player.Dungeon.AutoJoinCounter > 0 || AdminControlData.ControlPlayers)
            {
                player.Dungeon.AutoJoining = true;
                autoJoinList.Add(player.Id);
                autoJoinPlayers.Add(player);
            }
        }

        var result = await RavenNest.Players.AutoJoinDungeon(autoJoinList.ToArray());
        if (result == null || result.Length == 0)
        {
            failed = autoJoinPlayers;
        }
        else
        {
            for (var i = 0; i < result.Length; ++i)
            {
                var playerResult = result[i];
                var plr = autoJoinPlayers[i];
                if (playerResult)
                {
                    Dungeons.Join(plr);
                    success.Add(plr);
                    if (plr.Dungeon.AutoJoinCounter != int.MaxValue)
                    {
                        plr.Dungeon.AutoJoinCounter--;
                    }

                    plr.Resources.Coins -= DungeonAuto.autoJoinCost;
                }
                else
                {
                    failed.Add(plr);
                }
            }
        }
        foreach (var plr in autoJoinPlayers)
        {
            plr.Dungeon.AutoJoining = false;
        }

        AnnounceAutoDungeonJoin(success);

        if (failed.Count > 0)
        {
            AnnounceAutoDungeonJoinFailed(failed);
        }
    }

    private List<PlayerController> playerAutoJoinList = new List<PlayerController>();
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
}

public class TownHouseExpBonus
{
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