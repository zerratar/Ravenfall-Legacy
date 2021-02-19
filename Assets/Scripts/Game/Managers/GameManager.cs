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

    [SerializeField] private PlayerLogoManager playerLogoManager;
    [SerializeField] private ServerNotificationManager serverNotificationManager;
    [SerializeField] private LoginHandler loginHandler;

    [SerializeField] private GameObject gameReloadMessage;

    [SerializeField] private TavernHandler tavern;

    [SerializeField] private DayNightCycle dayNightCycle;

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

    public string ServerAddress;

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
    public FerryController Ferry => ferryController;
    public DropEventManager DropEvent => dropEventManager;
    public GameCamera Camera => gameCamera;
    public GameEventManager Events => events;
    public PlayerList PlayerList => playerList;
    public ServerNotificationManager ServerNotifications => serverNotificationManager;

    public EventTriggerSystem EventTriggerSystem => ioc.Resolve<EventTriggerSystem>();

    public TavernHandler Tavern => tavern;

    public bool IsSaving => saveCounter > 0;

    private float saveTimer = 5f;
    private float saveFrequency = 10f;
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

    public Permissions Permissions { get; set; } = new Permissions();
    public bool LogoCensor { get; set; }

    public bool PotatoMode
    {
        get => forcedPotatoMode || potatoMode;
        set => potatoMode = value;
    }

    public bool AutoPotatoMode { get; set; }
    public bool IsLoaded => loadingStates.All(x => x.Value == LoadingState.Loaded);
    public bool DungeonStartEnabled { get; internal set; } = true;
    public bool RaidStartEnabled { get; internal set; } = true;

    // Start is called before the first frame update   
    void Start()
    {
        ioc = GetComponent<IoCContainer>();
        AutoPotatoMode = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_AutoPotatoMode, 0) > 0;
        PotatoMode = PlayerPrefs.GetInt(SettingsMenuView.SettingsName_PotatoMode, 0) > 0;

        gameReloadMessage.SetActive(false);
        if (!dayNightCycle) dayNightCycle = GetComponent<DayNightCycle>();

        if (!loginHandler) loginHandler = FindObjectOfType<LoginHandler>();
        if (!dropEventManager) dropEventManager = GetComponent<DropEventManager>();
        if (!ferryProgress) ferryProgress = FindObjectOfType<FerryProgress>();
        if (!gameCamera) gameCamera = FindObjectOfType<GameCamera>();

        if (!playerLogoManager) playerLogoManager = GetComponent<PlayerLogoManager>();
        if (!villageManager) villageManager = FindObjectOfType<VillageManager>();

        if (!settings) settings = GetComponent<GameSettings>();
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
        musicManager.PlayBackgroundMusic();

        this.EventTriggerSystem.SourceTripped += OnSourceTripped;
    }

    internal void OnSessionStart()
    {
        commandServer.SendSessionOwner(this.RavenNest.TwitchUserId, this.RavenNest.TwitchUserName, this.RavenNest.SessionId);
    }

    private void OnSourceTripped(object sender, EventTriggerSystem.SysEventStats e)
    {
        if (!ravenNest.Authenticated && ravenNest.Stream.IsReady)
        {
            return;
        }
        ravenNest.Stream.UpdatePlayerEventStatsAsync(e);
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

    public void ReloadGame()
    {
        if (!loginHandler) return;
        isReloadingScene = true;
        loginHandler.ActivateTempAutoLogin();
        var gc = GameCache.Instance;

        gc.SetPlayersState(this.playerManager.GetAllPlayers());
        gc.BuildState();

        ravenNest.Stream.Close();
        RavenBot.Stop();

        gameReloadMessage.SetActive(true);

        SceneManager.LoadScene(1);
    }

    private IEnumerator RestoreGameState(GameCacheState state)
    {
        if (state.Players == null || state.Players.Count == 0)
            yield break;

        yield return UnityEngine.Resources.UnloadUnusedAssets();

        try
        {
            var players = state.Players?.Select(x => x.Definition.Id)?.ToArray();
            if (players != null)
                ravenNest.Game.AttachPlayersAsync(players);
        }
        catch (Exception exc)
        {
            LogError("Failed to attach players on restore. " + exc.ToString());
        }

        yield return new WaitForSeconds(0.5f);

        foreach (var player in state.Players)
        {
            var instance = SpawnPlayer(player.Definition, player.TwitchUser);
            if (!instance || instance == null)
            {
                continue;
            }

            yield return new WaitForEndOfFrame();

            instance.Lock();

            yield return new WaitForEndOfFrame();

            instance.Inventory.EquipAll();

            yield return new WaitForEndOfFrame();

            if (!player.ResetPosition)
            {
                instance.transform.position = player.Position;
                instance.Island = islandManager.FindPlayerIsland(instance);
            }

            yield return new WaitForEndOfFrame();

            if (player.TrainingTaskArg != null && player.TrainingTaskArg.Length > 0)
            {
                instance.SetTaskArguments(player.TrainingTaskArg);
                instance.GotoClosest(player.TrainingTask);
            }

            yield return new WaitForEndOfFrame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isReloadingScene)
        {
            return;
        }

        if (AutoPotatoMode)
        {
            forcedPotatoMode = !Application.isFocused;
        }

        //if (Time.frameCount % 300 == 0)
        //{
        //    System.GC.Collect();
        //}

        UpdateIntegrityCheck();

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
            menuHandler.Show();
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

        if (ravenNest.Authenticated && ravenNest.SessionStarted && ravenNest.Stream.IsReady)
        {
            var reloadState = GameCache.Instance.GetReloadState();
            if (reloadState != null)
            {
                StartCoroutine(RestoreGameState(reloadState));
                return;
            }
        }

        if (uptimeSaveTimer > 0)
            uptimeSaveTimer -= Time.deltaTime;

        if (uptimeSaveTimer <= 0)
        {
            uptimeSaveTimer = uptimeSaveTimerInterval;
            SaveGameStat("uptime", Time.realtimeSinceStartup);
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
            streamerRaidTimer -= Time.deltaTime;
            exitViewText.text = string.Format(
                exitViewTextFormat,
                streamerRaid,
                Mathf.FloorToInt(streamerRaidTimer)
            );

            if (streamerRaidTimer <= 0f)
            {
                Application.Quit();
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

    public void Log(string message)
    {
        //Debug.Log(message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }
    public void QueueRemovePlayer(PlayerController player)
    {
        playerKickQueue.Enqueue(player);
    }

    public void RemovePlayer(PlayerController player)
    {
        if (player.Dungeon.InDungeon)
        {
            dungeonManager.Remove(player);
        }

        if (player.Raid.InRaid)
        {
            raidManager.Leave(player);
        }


        ravenNest.PlayerRemoveAsync(player);

        player.Kicked = true;
        playerList.RemovePlayer(player);
        playerManager.Remove(player);

        UpdateVillageBoostText();
    }

    public PlayerController SpawnPlayer(
        RavenNest.Models.Player playerDefinition,
        Player streamUser = null,
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
        PlayerJoined(player);

        UpdateVillageBoostText();

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
            return null;
        }

        return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    }

    internal async Task<PlayerController> AddPlayerByUserIdAsync(string userId, StreamRaidInfo raiderInfo)
    {
        var playerInfo = await RavenNest.PlayerJoinAsync(new PlayerJoinData
        {
            UserId = userId,
            UserName = "",
            Identifier = "1"
        });

        if (playerInfo == null || !playerInfo.Success)
        {
            return null;
        }

        return SpawnPlayer(playerInfo.Player, raidInfo: raiderInfo);
    }

    private void OnApplicationQuit()
    {
        this.EventTriggerSystem.Dispose();

        StopRavenNestSession();

        Debug.Log("Application ending after " + Time.time + " seconds");

        RavenBot.Stop();
        SaveEmptyGameStats();
    }

    private void HandleRavenNestConnection()
    {
        var client = ravenNest;
        if (logger == null)
            logger = new RavenNest.SDK.UnityLogger();

        if (client == null)
        {
            client = new RavenNestClient(logger, this,
            new ProductionRavenNestStreamSettings()
            //new StagingRavenNestStreamSettings()
            //new LocalRavenNestStreamSettings()
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

        if (saveTimer > 0)
        {
            saveTimer -= Time.deltaTime;
        }

        if (saveTimer <= 0f)
        {
            saveTimer = saveFrequency;
            await SavePlayersAsync();
        }

        if (savingPlayersTime > 0)
        {
            savingPlayersTime -= Time.deltaTime;
        }
    }

    private async void StopRavenNestSession()
    {
        if (gameSessionActive)
        {
            await SavePlayersAsync();
            await ravenNest.EndSessionAsync();
            Debug.Log("Saving complete!");
        }
    }

    private async Task SavePlayersAsync()
    {
        try
        {
            if (savingPlayersTime > 0) return;
            var players = playerManager.GetAllPlayers().ToList();
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
                    await Task.Delay(5);
                }
            }
            catch (Exception exc)
            {
                Debug.LogError(exc.ToString());
            }

            //if (failedToSave.Count > 0)
            //{
            //    await SavePlayersUsingHTTP(failedToSave);
            //}
        }
        catch (Exception exc)
        {
            Debug.LogError(exc.ToString());
        }

        savingPlayersTime = 0;
    }

    private async Task SavePlayersUsingHTTP(IReadOnlyList<PlayerController> players)
    {
        Debug.LogWarning($"Fallbacking to HTTP Endpoint Saving {players.Count} players...");
        var states = players
            .Select(x => x.BuildPlayerState())
            .ToArray();

        // fall back to HTTPS Post Save
        var batchSize = 20;
        for (var i = 0; i < states.Length;)
        {
            var toUpdate = states.Skip(i * batchSize).Take(batchSize).ToArray();
            var remaining = states.Length - i;
            i += remaining < batchSize ? remaining : batchSize;

            var result = await ravenNest.Players.UpdateManyAsync(toUpdate);
            if (result == null)
            {
                Debug.LogWarning($"Saving gave null result. Data may not have been saved.");
                continue;
            }

            for (var playerIndex = 0; playerIndex < result.Length; ++playerIndex)
            {
                if (players.Count <= playerIndex)
                {
                    Debug.LogWarning($"Player at index {playerIndex} did not exist ingame. Skipping");
                    continue;
                }

                var playerResult = new { Player = players[playerIndex], Successeful = result[playerIndex] };
                if (playerResult.Successeful)
                {
                    playerResult.Player.SavedSucceseful();
                }
                else
                {
                    playerResult.Player.FailedToSave();
                    Debug.LogWarning($"{playerResult.Player.Name} was not saved. In another session?");
                }
            }

            await Task.Delay(1000);
        }
    }

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

        if (Permissions.IsAdministrator && isControlDown && Input.GetKeyUp(KeyCode.C))
        {
            Twitch.OnCheer(new TwitchCheer("72424639", "zerratar", "Zerratar", true, true, true, 10));
        }

        if (Permissions.IsAdministrator && isControlDown && Input.GetKeyUp(KeyCode.X))
        {
            Twitch.OnSubscribe(new TwitchSubscription("72424639", "zerratar", "Zerratar", null, true, true, 1, true));
        }

        if (Permissions.IsAdministrator && isControlDown && Input.GetKeyUp(KeyCode.O))
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

        if (Permissions.IsAdministrator && isControlDown && Input.GetKeyUp(KeyCode.R))
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

        if (Permissions.IsAdministrator || Application.isEditor)
        {

            if (isControlDown && Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                var adminPlayer = this.Players.GetAllPlayers().FirstOrDefault(x => x.IsGameAdmin);
                if (adminPlayer != null)
                {
                    var st = itemManager.GetItems().FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
                    if (st != null)
                    {
                        adminPlayer.PickupItem(st);
                    }
                }
            }


            if (isControlDown && Input.GetKeyDown(KeyCode.Delete))
            {
                subEventManager.Reset();
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.A))
            {
                subEventManager.Activate();
            }


            //if (isControlDown && Input.GetKeyUp(KeyCode.C))
            //{
            //    Twitch.OnSubscribe(new TwitchSubscription(null, null, null, null, false, false, -1, true));
            //}
        }
    }

    private void UpdatePlayerKickQueue()
    {
        if (playerKickQueue.Count <= 0 || !Arena || Arena.Started)
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
                timeLeft = $"{Mathf.FloorToInt(secondsLeft / 3600f)} hours";
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

    public void UpdateVillageBoostText()
    {
        var bonuses = Village.GetExpBonuses();
        var bonusString = string.Join(", ", bonuses.GroupBy(x => x.SlotType)
            .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
            .Select(x => $"{x.Key} {x.Sum(y => y.Bonus)}%"));


        SaveGameStat("village-boost", bonusString);
    }

    private void SaveEmptyGameStats()
    {
        SaveGameStat("online-player-count", 0);
        //SaveGameStat("last-joined-name", "");
        //SaveGameStat("last-level-up-name", "");
        //SaveGameStat("last-level-up-skill", "");
    }

    public void PlayerJoined(PlayerController player)
    {
        if (!player) return;
        //SaveGameStat("last-joined-name", player.PlayerName);
        SaveGameStat("online-player-count", playerManager.GetPlayerCount());
    }

    public void PlayerLevelUp(PlayerController player, SkillStat skill)
    {
        if (!player) return;
        //SaveGameStat("last-level-up-name", player.PlayerName);
        //SaveGameStat("last-level-up-skill", skill?.ToString());
    }

    private void SaveGameStat<T>(string name, T value)
    {
        try
        {
            if (!System.IO.Directory.Exists(settings.StreamLabelsFolder))
                System.IO.Directory.CreateDirectory(settings.StreamLabelsFolder);

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(settings.StreamLabelsFolder, name + ".txt"),
                value.ToString());
        }
        catch
        {
            // Ignore: since we do not want this to interrupt any execution of the script.
        }
    }

    internal void UpdateServerTime(DateTime timeUtc)
    {
        //if (Permissions.IsAdministrator)
        //    return;

        //var f0 = lastServerTimeUpdateFloat;
        //var dt0 = lastServerTimeUpdateDateTime;
        //var st0 = serverTime;
        //var now = DateTime.UtcNow;

        //var f1 = lastServerTimeUpdateFloat = Time.realtimeSinceStartup;
        //var dt1 = lastServerTimeUpdateDateTime = DateTime.UtcNow;
        //var st1 = serverTime = timeUtc;
        //var margin = 3600;

        //if (f0 > 0.0001f && dt0 != DateTime.MinValue && st0 != DateTime.MinValue)
        //{
        //    var nowDelta = now - timeUtc;
        //    if (nowDelta > TimeSpan.FromSeconds(margin))
        //    {
        //        var fd = f1 - f0;
        //        var dtd = dt1 - dt0;
        //        var std = st1 - st0;

        //        var us = (long)fd;
        //        var cs = (long)dtd.TotalSeconds;
        //        var ss = (long)std.TotalSeconds;

        //        if (Math.Abs(cs - ss) > margin || Math.Abs(us - cs) > margin || Math.Abs(us - ss) > margin)
        //        {
        //            RavenNest.Stream.SyncTimeAsync(nowDelta, now, timeUtc);
        //            RavenNest.Desynchronized = true;
        //            // Things will not be saved properly.
        //        }
        //    }
        //}

    }
}