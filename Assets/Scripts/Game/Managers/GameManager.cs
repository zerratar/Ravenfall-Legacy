using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts;
using Newtonsoft.Json;
using RavenNest.Models;
using RavenNest.SDK;
using RavenNest.SDK.Endpoints;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using RavenNestPlayer = RavenNest.Models.Player;
using Debug = Shinobytes.Debug;

public class GameManager : MonoBehaviour, IGameManager
{
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private RavenBot commandServer;

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



    private readonly ConcurrentDictionary<GameEventType, IGameEventHandler> gameEventHandlers
    = new ConcurrentDictionary<GameEventType, IGameEventHandler>();

    private readonly ConcurrentQueue<GameEvent> gameEventQueue = new ConcurrentQueue<GameEvent>();
    private readonly Queue<PlayerController> playerKickQueue = new Queue<PlayerController>();
    private readonly ConcurrentDictionary<string, LoadingState> loadingStates
        = new ConcurrentDictionary<string, LoadingState>();

    private readonly GameEventManager events = new GameEventManager();
    private IoCContainer ioc;
    private FerryController ferryController;
    private ChunkManager chunkManager;
    private PlayerManager playerManager;
    private CraftingManager craftingManager;
    private RaidManager raidManager;
    private StreamRaidManager streamRaidManager;
    private ArenaController arenaController;
    private ItemManager itemManager;
    private GraphicsToggler graphics;

    private int lastButtonIndex = 0;
    private float playerRequestTime = 0.5f;
    private float playerRequestInterval = 0.5f;

    public string ServerAddress;

    public GraphicsToggler Graphics => graphics;
    public IRavenNestClient RavenNest => ravenNest;
    public ClanManager Clans => clanManager ?? (clanManager = new ClanManager(this));

    public VillageManager Village => villageManager;
    public PlayerLogoManager PlayerLogo => playerLogoManager;
    public TwitchSubscriberBoost Boost => subEventManager.CurrentBoost;
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
    public RavenBotConnection RavenBot => commandServer?.Connection;
    public RavenBot RavenBotController => commandServer;
    public FerryController Ferry => ferryController;
    public DropEventManager DropEvent => dropEventManager;
    public GameCamera Camera => gameCamera;
    public GameEventManager Events => events;
    public PlayerList PlayerList => playerList;
    public ServerNotificationManager ServerNotifications => serverNotificationManager;

    private EventTriggerSystem eventTriggerSystem;
    public EventTriggerSystem EventTriggerSystem => eventTriggerSystem ?? (eventTriggerSystem = ioc.Resolve<EventTriggerSystem>());
    public OnsenManager Onsen => onsen;
    public TavernHandler Tavern => tavern;
    public Overlay Overlay => overlay;
    public bool IsSaving => saveCounter > 0;

    private float updateSessionInfoTime = 5f;
    private float saveFrequency = 5f;
    private int saveCounter;

    private RavenNestClient ravenNest;
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
    private float savingPlayersTime = 0f;
    private float savingPlayersTimeDuration = 60000f;
    private float uptimeSaveTimerInterval = 5f;
    private float uptimeSaveTimer = 5f;

    private bool potatoMode;
    private bool forcedPotatoMode;
    private DateTime dungeonStartTime;
    private DateTime raidStartTime;
    private ClanManager clanManager;

    private float lastServerTimeUpdateFloat;
    private DateTime lastServerTimeUpdateDateTime;
    private DateTime serverTime;

    private readonly TimeSpan dungeonStartCooldown = TimeSpan.FromMinutes(10);
    private readonly TimeSpan raidStartCooldown = TimeSpan.FromMinutes(10);
    private NameTagManager nametagManager;

    public StreamLabel uptimeLabel;
    public StreamLabel villageBoostLabel;
    public StreamLabel playerCountLabel;

    public int PlayerBoostRequirement { get; set; } = 0;

    public Permissions Permissions { get; set; } = new Permissions();
    public bool LogoCensor { get; set; }

    public bool PotatoMode
    {
        get => forcedPotatoMode || potatoMode;
        set => potatoMode = value;
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
    private bool expMultiplierUpdating;
    private DateTime waitBeforeExpMultiplierCheck;

    public bool RequireCodeForDungeonOrRaid;
    private GameCache.LoadStateResult gameCacheStateFileLoadResult;
    private bool stateFileStatusReported;

    void Awake()
    {
        overlay = gameObject.AddComponent<Overlay>();
        if (!settings) settings = GetComponent<GameSettings>();
        this.StreamLabels = new StreamLabels(settings);
        gameReloadUIPanel.SetActive(false);
    }

    // Start is called before the first frame update   
    void Start()
    {

#if DEBUG
        Application.SetStackTraceLogType(LogType.Assert | LogType.Error | LogType.Exception | LogType.Log | LogType.Warning, StackTraceLogType.Full);
#else 
        Application.SetStackTraceLogType(LogType.Log | LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error | LogType.Exception, StackTraceLogType.ScriptOnly);
#endif

        this.SetupStreamLabels();

        GameCache.Instance.IsAwaitingGameRestore = false;
        ioc = GetComponent<IoCContainer>();

        gameReloadMessage.SetActive(false);
        if (!graphics) graphics = FindObjectOfType<GraphicsToggler>();
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

        if (!commandServer) commandServer = GetComponent<RavenBot>();
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
        RegisterGameEventHandler<ServerTimeEventHandler>(GameEventType.ServerTime);

        RegisterGameEventHandler<PlayerRestedUpdateEventHandler>(GameEventType.PlayerRestedUpdate);

        RegisterGameEventHandler<PubSubTokenReceivedEventHandler>(GameEventType.PubSubToken);

        RegisterGameEventHandler<ExpMultiplierEventHandler>(GameEventType.ExpMultiplier);

        RegisterGameEventHandler<PlayerRemoveEventHandler>(GameEventType.PlayerRemove);
        RegisterGameEventHandler<PlayerAddEventHandler>(GameEventType.PlayerAdd);
        RegisterGameEventHandler<PlayerExpUpdateEventHandler>(GameEventType.PlayerExpUpdate);
        RegisterGameEventHandler<PlayerJoinArenaEventHandler>(GameEventType.PlayerJoinArena);
        RegisterGameEventHandler<PlayerJoinDungeonEventHandler>(GameEventType.PlayerJoinDungeon);
        RegisterGameEventHandler<PlayerJoinRaidEventHandler>(GameEventType.PlayerJoinRaid);
        RegisterGameEventHandler<PlayerNameUpdateEventHandler>(GameEventType.PlayerNameUpdate);
        RegisterGameEventHandler<PlayerTaskEventHandler>(GameEventType.PlayerTask);


        RegisterGameEventHandler<StreamerWarRaidEventHandler>(GameEventType.WarRaid);
        RegisterGameEventHandler<StreamerRaidEventHandler>(GameEventType.Raid);
        RegisterGameEventHandler<PlayerAppearanceEventHandler>(GameEventType.PlayerAppearance);
        RegisterGameEventHandler<ItemBuyEventHandler>(GameEventType.ItemBuy);
        RegisterGameEventHandler<ItemSellEventHandler>(GameEventType.ItemSell);

        commandServer.Initialize(this);

        LoadGameSettings();

        musicManager.PlayBackgroundMusic();

        this.EventTriggerSystem.SourceTripped += OnSourceTripped;

        gameCacheStateFileLoadResult = GameCache.Instance.LoadState();
    }

    private void OnSourceTripped(object sender, EventTriggerSystem.SysEventStats e)
    {
        if (!ravenNest.Authenticated || !ravenNest.Stream.IsReady)
        {
            return;
        }

        ravenNest.Stream.UpdatePlayerEventStatsAsync(e);
    }

    private void SetupStreamLabels()
    {
        uptimeLabel = StreamLabels.Register("uptime", () => Time.realtimeSinceStartup.ToString());
        villageBoostLabel = StreamLabels.Register("village-boost", () =>
        {
            var bonuses = Village.GetExpBonuses();
            return string.Join(", ", bonuses.Where(x => x.Bonus > 0).GroupBy(x => x.SlotType)
                .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
                .Select(x => $"{x.Key} {x.Sum(y => y.Bonus)}%"));
        });
        playerCountLabel = StreamLabels.Register("online-player-count", () => playerManager.GetPlayerCount().ToString());
    }

    private void LoadGameSettings()
    {
        AutoPotatoMode = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_AutoPotatoMode, 0) > 0;
        PotatoMode = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_PotatoMode, 0) > 0;
        RealtimeDayNightCycle = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_RealTimeDayNightCycle, 0) > 0;
        PlayerBoostRequirement = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_PlayerBoostRequirement);

        PlayerList.Bottom = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_PlayerListSize, PlayerList.Bottom);
        PlayerList.Scale = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_PlayerListScale, PlayerList.Scale);

        Raid.Notifications.volume = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_RaidHornVolume, Raid.Notifications.volume);
        Music.volume = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_MusicVolume, Music.volume);

        SettingsMenuView.SetResolutionScale(PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_DPIScale, 1f));
    }

    internal void OnSessionStart()
    {
        commandServer.UpdateSessionInfo();

        if (RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
        {
            RavenBot.SendSessionOwner(this.RavenNest.TwitchUserId, this.RavenNest.TwitchUserName, this.RavenNest.SessionId);
        }
    }

    private void RegisterGameEventHandler<T>(GameEventType type) where T : IGameEventHandler, new()
    {
        gameEventHandlers[type] = new T();
    }
    private IGameEventHandler GetEventHandler(GameEventType type)
    {
        if (gameEventHandlers.TryGetValue(type, out var handler))
        {
            return handler;
        }

        return null;
    }

    public void SaveStateAndShutdownGame(bool activateTempLogin = true)
    {
        if (activateTempLogin)
        {
            if (!loginHandler) return;
            loginHandler.ActivateTempAutoLogin();
        }

        SavePlayerStates();
        Application.Quit();
    }
    public void SavePlayerStates()
    {
        GameCache.Instance.SavePlayersState(this.playerManager.GetAllPlayers());

        //var gc = GameCache.Instance;
        //var players = ;

        //gc.SetPlayersState(players);
        //gc.BuildState();
        //gc.SaveState();
    }

    public void ReloadScene()
    {
        ravenNest.Stream.Close();
        RavenBot.Stop();
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void ReloadGame()
    {
        if (!loginHandler) return;

        isReloadingScene = true;
        loginHandler.ActivateTempAutoLogin();

        SavePlayerStates();

        GameCache.Instance.IsAwaitingGameRestore = true;

        ravenNest.Stream.Close();
        RavenBot.Stop();

        gameReloadMessage.SetActive(true);

        ReloadScene();
    }

    private IEnumerator RestoreGameState(GameCacheState state)
    {
        GameCache.Instance.IsAwaitingGameRestore = false;

        gameReloadUIPanel.SetActive(true);
        if (state.Players == null || state.Players.Count == 0)
            yield break;

        yield return UnityEngine.Resources.UnloadUnusedAssets();

        Shinobytes.Debug.Log("Restoring game state with " + state.Players.Count + " players.");

        var waitTime = 0;
        // if we got disconnected or something
        while ((!RavenNest.Authenticated || !RavenNest.SessionStarted || !RavenNest.Stream.IsReady) && waitTime < 100)
        {
            yield return new WaitForSeconds(1);
            waitTime++;
        }

        // still not connected? retry later.
        if (!RavenNest.Authenticated || !RavenNest.SessionStarted || !RavenNest.Stream.IsReady)
        {
            Shinobytes.Debug.LogWarning("No conneciton to server when trying to restore players. Retrying");
            GameCache.Instance.SetState(state);
            yield break;
        }

        // RestoreAsync

        yield return Players.RestoreAsync(state.Players);
        gameReloadUIPanel.SetActive(false);
        //foreach (var player in state.Players)
        //{
        //    if (player.TwitchUser == null || string.IsNullOrEmpty(player.TwitchUser.UserId))
        //    {
        //        Debug.LogError("Unable to restore character, TwitchUser is null or missing UserId.");
        //        yield return null;
        //    }


        //    yield return Players.JoinAsync(player.TwitchUser, RavenBot.ActiveClient, false, false, player.CharacterId);
        //}

        //SavePlayerStates();
    }

    // Update is called once per frame
    void Update()
    {
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
        }
        else
        {
            if (currentQualityLevel != 1)
            {
                QualitySettings.SetQualityLevel(1);
            }
        }

        UpdateIntegrityCheck();

        if (Input.GetKeyDown(KeyCode.F11) && RavenBot.UseRemoteBot && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Disconnect(BotConnectionType.Remote);
            return;
        }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            RavenNest.Stream.Reconnect();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            ReloadGame();
            return;
        }

        if (UpdateExitView())
        {
            return;
        }

        if (Input.GetKeyUp(KeyCode.Escape) && !menuHandler.Visible && !playerSearchHandler.Visible)
        {
            menuHandler.Show(true);
        }

        UpdateApiCommunication();

        if (ravenNest == null || !ravenNest.Authenticated)
        {
            return;
        }

        if (!UpdateGameEvents())
        {
            return;
        }

        if (GameCache.Instance.IsAwaitingGameRestore
            && ravenNest.Authenticated
            && ravenNest.SessionStarted
            && ravenNest.Stream.IsReady
            && Items.Loaded)
        {
            GameCache.Instance.IsAwaitingGameRestore = false;
            var reloadState = GameCache.Instance.GetReloadState();
            if (reloadState != null)
            {
                StartCoroutine(RestoreGameState(reloadState.Value));
                return;
            }
        }
        else if (
             ravenNest.Authenticated &&
             ravenNest.SessionStarted &&
             RavenBot.IsConnected &&
            !stateFileStatusReported && gameCacheStateFileLoadResult == GameCache.LoadStateResult.Expired)
        {
            RavenBot.Announce("Player restore state file has expired. No players has been added back.");
            stateFileStatusReported = true;
        }

        if (uptimeSaveTimer > 0)
            uptimeSaveTimer -= Time.deltaTime;

        if (uptimeSaveTimer <= 0)
        {
            uptimeSaveTimer = uptimeSaveTimerInterval;
            uptimeLabel.Update();
        }

        UpdateExpBoostTimer();

        UpdatePlayerKickQueue();

        HandleKeyDown();

        UpdateChatBotCommunication();
    }

    private void UpdateIntegrityCheck()
    {
        IntegrityCheck.Update();
    }

    private bool UpdateGameEvents()
    {
        if (gameEventQueue.TryDequeue(out var ge))
        {
            HandleGameEvent(ge);
        }

        // update exp multiplier
        UpdateExpMultiplierAsync();

        return true;
    }

    private async void UpdateExpMultiplierAsync()
    {
        try
        {
            if (expMultiplierUpdating)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (waitBeforeExpMultiplierCheck > now)
            {
                return;
            }

            var expMultiplierSinceLastUpdate = now - Twitch.LastUpdated;
            if (expMultiplierSinceLastUpdate.TotalSeconds >= 5)
            {
                expMultiplierUpdating = true;
                var multiplier = await RavenNest.Game.GetExpMultiplierAsync();
                Twitch.SetExpMultiplier(
                    multiplier.EventName,
                    multiplier.Multiplier,
                    multiplier.StartTime,
                    multiplier.EndTime);
            }
        }
        catch
        {
            // ignore, but wait some time before we try this again.
            waitBeforeExpMultiplierCheck = DateTime.UtcNow.AddSeconds(10);
        }
        finally { expMultiplierUpdating = false; }
    }

    public bool UpdateExitView()
    {
        if (streamerRaidTimer <= 0)
        {
            return false;
        }

        if (exitViewText && exitView.activeSelf)
        {
            streamerRaidTimer -= Time.deltaTime;
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
    }
    public void RemovePlayer(PlayerController player, bool notifyServer = true)
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
            ravenNest.PlayerRemoveAsync(player);
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
        SavePlayerStates();
    }

    public async void SpawnManyBotPlayers(int count)
    {
        for (var i = 0; i < count; ++i)
        {
            await SpawnBotPlayer();
            await Task.Delay(10);
        }
    }

    public async Task SpawnBotPlayer()
    {
        var id = Random.Range(10000, 99999);

        var userId = "#" + id;

        while (playerManager.GetPlayerByUserId(userId))
        {
            id = Random.Range(10000, 99999);
            userId = "#" + id;
            await Task.Delay(5);
        }

        var userName = "Bot" + id;

        //var uid = 1000 + (botSpawnIndex++);
        var playerInfo = new TwitchPlayerInfo(userId, userName, userName, "#ffffff", false, false, false, false, "1");
        var player = await Players.JoinAsync(playerInfo, RavenBot.ActiveClient, false, true);
        if (player)
        {
            await player.EquipBestItemsAsync();
        }
    }
    public PlayerController SpawnPlayer(
        RavenNest.Models.Player playerDefinition,
        TwitchPlayerInfo streamUser = null,
        StreamRaidInfo raidInfo = null)
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

        var vector3 = Random.insideUnitSphere * 1f;
        var player = playerManager.Spawn(spawnPoint + vector3, playerDefinition, streamUser, raidInfo);

        if (!player)
        {
            Debug.LogError("Can't spawn player, player is already playing.");
            return null;
        }

        playerList.AddPlayer(player);

        Village.TownHouses.EnsureAssignPlayerRows(Players.GetPlayerCount());

        playerCountLabel.Update();
        villageBoostLabel.Update();

        if (player && gameCamera && gameCamera.AllowJoinObserve)
            gameCamera.ObservePlayer(player);

        if (dropEventManager.IsActive)
            player.BeginItemDropEvent();

        return player;
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

    //internal async Task<PlayerController> AddPlayerByUserIdAsync(string userId, StreamRaidInfo raiderInfo)
    //{
    //    var playerInfo = await RavenNest.PlayerJoinAsync(new PlayerJoinData
    //    {
    //        UserId = userId,
    //        UserName = "",
    //        Identifier = "1"
    //    });

    //    if (playerInfo == null || !playerInfo.Success)
    //    {

    //        return null;
    //    }

    //    return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    //}

    private void OnApplicationQuit()
    {
        RavenBot.Stop();

        //this.EventTriggerSystem.Dispose();

        StopRavenNestSession();

        Debug.Log("Application ending after " + Time.time + " seconds");
    }

    private void HandleRavenNestConnection()
    {
        var client = ravenNest;
        if (logger == null)
            logger = new RavenNest.SDK.UnityLogger();

        if (client == null)
        {
            client = new RavenNestClient(logger, this,
            //new ProductionRavenNestStreamSettings()
            //new StagingRavenNestStreamSettings()
            new LocalRavenNestStreamSettings()
            //new UnsecureLocalRavenNestStreamSettings()
            );

            ravenNest = client;
        }

        if (client != null)
        {
            ravenNest.Update();
            ServerAddress = ravenNest.ServerAddress;
        }
    }

    public async Task<bool> RavenNestLoginAsync(string username, string password)
    {
        if (ravenNest == null) return false;
        if (ravenNest.Authenticated) return true;
        return await ravenNest.LoginAsync(username, password);
    }

    private async void RavenNestUpdate()
    {
        if (ravenNest.HasActiveRequest)
        {
            return;
        }

        if (ravenNest.BadClientVersion)
        {
            return;
        }

        if (ravenNest.Authenticated && !ravenNest.SessionStarted)
        {
            if (await ravenNest.StartSessionAsync(Application.version, accessKey, false))
            {
                gameSessionActive = true;
                lastGameEventRecevied = DateTime.UtcNow;
            }
        }

        if (!ravenNest.Authenticated || !ravenNest.SessionStarted)
        {
            return;
        }

        if (updateSessionInfoTime > 0)
        {
            updateSessionInfoTime -= Time.deltaTime;
        }

        if (RavenBot.UseRemoteBot && !RavenBot.IsConnectedToRemote && !RavenBot.IsConnectedToLocal)
        {
            RavenBot.Connect(BotConnectionType.Remote);

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                commandServer.UpdateSessionInfo();
            }
        }


        if (RavenNest.Authenticated && RavenNest.SessionStarted)
        {
            if (playerRequestTime >= 0)
            {
                playerRequestTime -= Time.deltaTime;
                if (playerRequestTime < 0f)
                {
                    await SendPlayerRequestsAsync();
                    playerRequestTime = playerRequestInterval;
                }
            }
        }

        if (updateSessionInfoTime <= 0f)
        {
            updateSessionInfoTime = saveFrequency;
            //await SavePlayersAsync();

            if (RavenNest.Authenticated && RavenNest.SessionStarted && RavenBot.UseRemoteBot && RavenBot.IsConnectedToRemote)
            {
                commandServer.UpdateSessionInfo();
            }
        }

        if (savingPlayersTime > 0)
        {
            savingPlayersTime -= Time.deltaTime;
        }
    }

    public async void StopRavenNestSession()
    {
        if (gameSessionActive)
        {
            await SavePlayersAsync();
            await ravenNest.EndSessionAsync();
            Debug.Log("Saving complete!");
        }
    }

    private async Task SendPlayerRequestsAsync()
    {
        try
        {
            var players = playerManager.GetAllPlayers().ToList();

            foreach (var player in players)
            {
                if (player.RequestQueue.TryDequeue(out var req))
                {
                    await req.InvokeAsync();
                }

                player.UpdateRequestQueue();
            }

        }
        catch (Exception exc)
        {
            Debug.LogError(exc);
        }
    }

    private async Task SavePlayersAsync()
    {
        try
        {
            if (savingPlayersTime > 0) return;
            var players = playerManager.GetAllPlayers();
            var failedToSave = new List<PlayerController>();
            if (players.Count == 0) return;
            //Debug.Log($"Saving {players.Count} players...");
            try
            {
                savingPlayersTime = savingPlayersTimeDuration;
                // Save using websocket
                foreach (var player in players)
                {
                    await ravenNest.SavePlayerAsync(player);
                    //if (await ravenNest.SavePlayerAsync(player))
                    //{
                    //    player.SavedSucceseful();
                    //}
                    //else
                    //{
                    //    player.FailedToSave();
                    //    failedToSave.Add(player);
                    //}
                    //await Task.Delay(5);
                }
            }
            catch (Exception exc)
            {
                Debug.LogError(exc);
            }

            //if (failedToSave.Count > 0)
            //{
            //    await SavePlayersUsingHTTP(failedToSave);
            //}
        }
        catch (Exception exc)
        {
            Debug.LogError(exc);
        }

        savingPlayersTime = 0;
    }

    //private async Task SavePlayersUsingHTTP(IReadOnlyList<PlayerController> players)
    //{
    //    Debug.LogWarning($"Fallbacking to HTTP Endpoint Saving {players.Count} players...");
    //    var states = players
    //        .Select(x => x.BuildPlayerState())
    //        .ToArray();

    //    // fall back to HTTPS Post Save
    //    var batchSize = 20;
    //    for (var i = 0; i < states.Length;)
    //    {
    //        var toUpdate = states.Skip(i * batchSize).Take(batchSize).ToArray();
    //        var remaining = states.Length - i;
    //        i += remaining < batchSize ? remaining : batchSize;

    //        var result = await ravenNest.Players.UpdateManyAsync(toUpdate);
    //        if (result == null)
    //        {
    //            Debug.LogWarning($"Saving gave null result. Data may not have been saved.");
    //            continue;
    //        }

    //        for (var playerIndex = 0; playerIndex < result.Length; ++playerIndex)
    //        {
    //            if (players.Count <= playerIndex)
    //            {
    //                Debug.LogWarning($"Player at index {playerIndex} did not exist ingame. Skipping");
    //                continue;
    //            }

    //            var playerResult = new { Player = players[playerIndex], Successeful = result[playerIndex] };
    //            if (playerResult.Successeful)
    //            {
    //                playerResult.Player.SavedSucceseful();
    //            }
    //            else
    //            {
    //                playerResult.Player.FailedToSave();
    //                Debug.LogWarning($"{playerResult.Player.Name} was not saved. In another session?");
    //            }
    //        }

    //        await Task.Delay(1000);
    //    }
    //}

    private void UpdateChatBotCommunication()
    {
        if (RavenBot == null || !RavenBot.IsBound)
        {
            return;
        }

        RavenBot.HandleNextPacket(this, RavenBot, playerManager);
    }

    //private IslandController lastIslandToggle = null;
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
            SavePlayersAsync();
        }

        if (Permissions.IsAdministrator)
        {
            if (isControlDown && Input.GetKeyUp(KeyCode.C))
            {
                Twitch.OnCheer(new TwitchCheer("72424639", "zerratar", "Zerratar", true, true, true, 10));
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.X))
            {
                Twitch.OnSubscribe(new TwitchSubscription("72424639", "zerratar", "Zerratar", null, true, true, 1, true));
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
                    RavenBot.Announce("You have to wait {cooldown} seconds before you can start another raid.", timeLeft.TotalSeconds.ToString());
                }
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.R))
            {
                var elapsed = DateTime.UtcNow - raidStartTime;
                if (elapsed > raidStartCooldown || Permissions.IsAdministrator)
                {
                    raidStartTime = DateTime.UtcNow;
                    raidManager.StartRaid("streamer");
                }
                else
                {
                    var timeLeft = raidStartCooldown - elapsed;
                    RavenBot.Announce("You have to wait {cooldown} seconds before you can start another raid.", timeLeft.TotalSeconds.ToString());
                }
            }

            //if (isControlDown && Input.GetKeyUp(KeyCode.Y))
            //{
            //    RavenNest.EnqueueLoyaltyUpdate(new TwitchCheer("72424639", "zerratar", "zerratar", true, true, true, 200));
            //}

            //if (isControlDown && Input.GetKeyUp(KeyCode.T))
            //{
            //    RavenNest.EnqueueLoyaltyUpdate(new TwitchSubscription("72424639", "zerratar", "zerratar", "72424639", true, true, 1, false));
            //}
        }

        //if (Permissions.IsAdministrator || Application.isEditor)
        //{
        //    //if (isControlDown && Input.GetKeyDown(KeyCode.KeypadPlus))
        //    //{
        //    //    var adminPlayer = this.Players.GetAllPlayers().FirstOrDefault(x => x.IsGameAdmin);
        //    //    if (adminPlayer != null)
        //    //    {
        //    //        var st = itemManager.GetItems().FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
        //    //        if (st != null)
        //    //        {
        //    //            adminPlayer.PickupItem(st);
        //    //        }
        //    //    }
        //    //}
        //    //if (isControlDown && Input.GetKeyDown(KeyCode.Delete))
        //    //{
        //    //    subEventManager.Reset();
        //    //}
        //    //if (isControlDown && Input.GetKeyUp(KeyCode.A))
        //    //{
        //    //    subEventManager.Activate();
        //    //}
        //    //if (isControlDown && Input.GetKeyUp(KeyCode.C))
        //    //{
        //    //    Twitch.OnSubscribe(new TwitchSubscription(null, null, null, null, false, false, -1, true));
        //    //}
        //}
    }

#if DEBUG
    private void OnGUI()
    {
        return;

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
        if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn 99 Bots"))
        {
            SpawnManyBotPlayers(99);
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Spawn a bot"))
        {
            SpawnBotPlayer();
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Combat"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask("fighting", new string[] { (new string[] { "all", "strength", "attack", "defense", "ranged", "magic" }).Random() });
            }
        }
        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Healing"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask("fighting", new string[] { (new string[] { "healing" }).Random() });
            }
        }
        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Fishing"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask("fishing", new string[0]);
            }
        }
        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Woodcutting"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask("woodcutting", new string[0]);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Mining"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask((new string[] { "mining" }).Random(), new string[0]);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Farming"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask((new string[] { "farming" }).Random(), new string[0]);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Train Crafting"))
        {
            var bots = this.playerManager.GetAllBots();
            foreach (var bot in bots)
            {
                bot.SetTask((new string[] { "Crafting" }).Random(), new string[0]);
            }
        }

        //if (GUI.Button(GetButtonRect(buttonIndex++), "Train Gathering"))
        //{
        //    var bots = this.playerManager.GetAllBots();
        //    foreach (var bot in bots)
        //    {
        //        bot.SetTask((new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting" }).Random(), new string[0]);
        //    }
        //}

        //if (GUI.Button(GetButtonRect(buttonIndex++), "Train Random"))
        //{
        //    var bots = this.playerManager.GetAllBots();
        //    foreach (var bot in bots)
        //    {

        //        var s = (new string[] { "fishing", "mining", "farming", "crafting", "cooking", "woodcutting", "fighting" }).Random();
        //        if (s == "fighting")
        //        {
        //            bot.SetTask(s, new string[] { (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", "healing" }).Random() });
        //        }
        //        else
        //        {
        //            bot.SetTask(s, new string[0]);
        //        }
        //    }
        //}

        //if (GUI.Button(GetButtonRect(buttonIndex++), "Healing 50/Combat 50"))
        //{
        //    var bots = this.playerManager.GetAllBots();
        //    var c = bots.Count / 2;
        //    var i = 0;
        //    foreach (var bot in bots)
        //    {
        //        var task = "fighting";
        //        var subTask = (new string[] { "all", "strength", "attack", "defense", "ranged", "magic", }).Random();
        //        if (i++ <= c)
        //        {
        //            subTask = "healing";
        //        }
        //        bot.SetTask(task, new string[] { subTask });
        //    }
        //}

        if (GUI.Button(GetButtonRect(buttonIndex++), "Away"))
        {
            var bots = this.playerManager.GetAllBots();

            var island = Islands.Find("Away");

            foreach (var bot in bots)
            {
                //bot.Ferry.Embark(island);
                bot.Teleporter.Teleport(island.SpawnPosition);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Ironhill"))
        {
            var bots = this.playerManager.GetAllBots();

            var island = Islands.Find("Ironhill");

            foreach (var bot in bots)
            {
                //bot.Ferry.Embark(island);
                bot.Teleporter.Teleport(island.SpawnPosition);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Kyo"))
        {
            var bots = this.playerManager.GetAllBots();

            var island = Islands.Find("Kyo");

            foreach (var bot in bots)
            {
                //bot.Ferry.Embark(island);
                bot.Teleporter.Teleport(island.SpawnPosition);
            }
        }

        if (GUI.Button(GetButtonRect(buttonIndex++), "Heim"))
        {
            var bots = this.playerManager.GetAllBots();

            var island = Islands.Find("Heim");

            foreach (var bot in bots)
            {
                //bot.Ferry.Embark(island);
                bot.Teleporter.Teleport(island.SpawnPosition);
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
        if (playerKickQueue.Count <= 0)// || !Arena || Arena.Started)
        {
            return;
        }

        var player = playerKickQueue.Dequeue();
        if (player.Duel.InDuel)
        {
            playerKickQueue.Enqueue(player);
            return;
        }

        RemovePlayer(player);
    }

    private void UpdateExpBoostTimer()
    {
        if (!boostTimer) return;

        expBoostTimerUpdate -= Time.deltaTime;
        if (expBoostTimerUpdate > 0f) return;

        boostTimer.SetActive(subEventManager.CurrentBoost.Active);


        if (subEventManager.CurrentBoost.Active)
        {
            var secondsLeft = Mathf.FloorToInt(subEventManager.Duration - subEventManager.CurrentBoost.Elapsed);
            var timeLeft = $"{secondsLeft} sec";
            if (secondsLeft > 3600)
            {
                timeLeft = $"{Mathf.FloorToInt(secondsLeft / 3600f)} hours";
                var minutes = secondsLeft / 60;
                minutes = (int)(minutes % 60);
                if (minutes > 0)
                {
                    timeLeft += " " + (int)+minutes + "min";
                }
            }
            else if (secondsLeft > 60)
                timeLeft = $"{Mathf.FloorToInt(secondsLeft / 60f)} mins";
            boostTimer.SetSubscriber(subEventManager.CurrentBoost.LastSubscriber, !subEventManager.CurrentBoost.LastSubscriber.Contains(" "));
            boostTimer.SetText(
                $"EXP Multiplier x{subEventManager.CurrentBoost.Multiplier} - {timeLeft}");
            boostTimer.SetTime(subEventManager.CurrentBoost.Elapsed, subEventManager.Duration);
        }

        expBoostTimerUpdate = 0.5f;
    }

    private void UpdateApiCommunication()
    {
        HandleRavenNestConnection();

        if (ravenNest != null)
        {
            RavenNestUpdate();
        }
    }
    internal void UpdateServerTime(DateTime timeUtc)
    {
        if (Permissions.IsAdministrator)
            return;

        var f0 = lastServerTimeUpdateFloat;
        var dt0 = lastServerTimeUpdateDateTime;
        var st0 = serverTime;
        var now = DateTime.UtcNow;

        var f1 = lastServerTimeUpdateFloat = Time.realtimeSinceStartup;
        var dt1 = lastServerTimeUpdateDateTime = DateTime.UtcNow;
        var st1 = serverTime = timeUtc;
        var margin = 3600;

        if (f0 > 0.0001f && dt0 != DateTime.MinValue && st0 != DateTime.MinValue)
        {
            var nowDelta = now - timeUtc;
            if (nowDelta > TimeSpan.FromSeconds(margin))
            {
                var fd = f1 - f0;
                var dtd = dt1 - dt0;
                var std = st1 - st0;

                var us = (long)fd;
                var cs = (long)dtd.TotalSeconds;
                var ss = (long)std.TotalSeconds;

                if (Math.Abs(cs - ss) > margin || Math.Abs(us - cs) > margin || Math.Abs(us - ss) > margin)
                {
                    RavenNest.Stream.SyncTimeAsync(nowDelta, now, timeUtc);
                    //RavenNest.Desynchronized = true;

                    // Things will not be saved properly.
                }
            }
        }

    }
}