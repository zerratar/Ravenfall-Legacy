using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenNest.Models;
using RavenNest.SDK;
using RavenNest.SDK.Endpoints;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using RavenNestPlayer = RavenNest.Models.Player;

public class GameManager : MonoBehaviour, IGameManager
{
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private CommandServer commandServer;

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

    private readonly ConcurrentQueue<GameEvent> gameEventQueue = new ConcurrentQueue<GameEvent>();
    private readonly Queue<PlayerController> playerKickQueue = new Queue<PlayerController>();
    private readonly ConcurrentDictionary<string, LoadingState> loadingStates
        = new ConcurrentDictionary<string, LoadingState>();

    [SerializeField] private IslandManager islandManager;
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
    public GameServer Server => commandServer?.Server;
    public FerryController Ferry => ferryController;
    public DropEventManager DropEvent => dropEventManager;
    public GameCamera Camera => gameCamera;

    public bool IsSaving => saveCounter > 0;

    private float spawnTimer = 10f;

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

    public Permissions Permissions { get; set; } = new Permissions();
    public bool LogoCensor { get; set; }

    public bool IsLoaded => loadingStates.All(x => x.Value == LoadingState.Loaded);

    // Start is called before the first frame update   
    void Start()
    {
        if (!dropEventManager) dropEventManager = GetComponent<DropEventManager>();
        if (!ferryProgress) ferryProgress = FindObjectOfType<FerryProgress>();
        if (!gameCamera) gameCamera = FindObjectOfType<GameCamera>();

        if (!settings) settings = GetComponent<GameSettings>();
        if (!subEventManager) subEventManager = GetComponent<TwitchEventManager>();
        if (!subEventManager) subEventManager = gameObject.AddComponent<TwitchEventManager>();

        if (!commandServer) commandServer = GetComponent<CommandServer>();

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


        commandServer.StartServer(this);

        musicManager.PlayBackgroundMusic();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateIntegrityCheck();

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

        UpdateGameEvents();

        UpdateExpBoostTimer();

        UpdatePlayerKickQueue();

        HandleKeyDown();

        UpdateChatBotCommunication();
    }

    private void UpdateIntegrityCheck()
    {
        IntegrityCheck.Update();
    }

    private void UpdateGameEvents()
    {
        if (gameEventQueue.TryDequeue(out var ge))
        {
            HandleGameEvent(ge);
        }
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

    public void Log(string message)
    {
        Debug.Log(message);
    }

    public void OnAuthenticated()
    {
        ShowFerryProgress();
    }

    public void ShowFerryProgress()
    {
        ferryProgress.gameObject.SetActive(true);
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
        player.Kicked = true;
        playerList.RemovePlayer(player);
        playerManager.Remove(player);
    }

    public PlayerController SpawnPlayer(
        RavenNest.Models.Player playerDef,
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
        var vector3 = Random.insideUnitSphere * 1f;
        var player = playerManager.Spawn(spawnPoint + vector3, playerDef, streamUser, raidInfo);
        if (!player)
        {
            Debug.LogError("Can't spawn player, player is already playing.");
        }

        playerList.AddPlayer(player);

        PlayerJoined(player);

        if (player && gameCamera && gameCamera.AllowJoinObserve) gameCamera.ObservePlayer(player);

        if (dropEventManager.IsActive)
        {
            player.BeginItemDropEvent();
        }

        return player;
    }

    public void HandleGameEvents(EventList gameEvents)
    {
        foreach (var ge in gameEvents.Events)
        {
            gameEventQueue.Enqueue(ge);
        }
    }

    public void HandleGameEvent(GameEvent gameEvent)
    {
        if (gameEvent == null)
            return;

        switch ((GameEventType)gameEvent.Type)
        {
            case GameEventType.PermissionChange:
                {
                    Debug.LogWarning("User Permission Update received: " + gameEvent.Data);
                    Permissions = JsonConvert.DeserializeObject<Permissions>(gameEvent.Data);
                    break;
                }
            case GameEventType.ServerMessage:
                {
                    Debug.LogWarning("Server message received: " + gameEvent.Data);
                    var serverMessage = JsonConvert.DeserializeObject<ServerMessage>(gameEvent.Data);
                    OnServerMessage(serverMessage);
                    break;
                }
            case GameEventType.PlayerRemove:
                {
                    Debug.LogWarning("Player remove received: " + gameEvent.Data);
                    var playerInfo = JsonConvert.DeserializeObject<PlayerRemove>(gameEvent.Data);
                    OnPlayerRemove(playerInfo);
                    break;
                }
            case GameEventType.WarRaid:
                {
                    Debug.LogWarning("War raid received: " + gameEvent.Data);
                    var raidInfo = JsonConvert.DeserializeObject<StreamRaidInfo>(gameEvent.Data);
                    OnStreamerRaid(raidInfo, true);
                    break;
                }
            case GameEventType.Raid:
                {
                    Debug.LogWarning("Raid received: " + gameEvent.Data);
                    var raidInfo = JsonConvert.DeserializeObject<StreamRaidInfo>(gameEvent.Data);
                    OnStreamerRaid(raidInfo, false);
                    return;
                }
            case GameEventType.PlayerAppearance:
                {
                    Debug.LogWarning("Player appearance received: " + gameEvent.Data);
                    var appearanceData = JsonConvert.DeserializeObject<AppearanceUpdate>(gameEvent.Data);
                    var player = playerManager.GetPlayerByUserId(appearanceData.UserId);
                    if (player)
                    {
                        player.Appearance.TryUpdate(appearanceData.Values);
                    }
                    return;
                }
            case GameEventType.ItemBuy:
                {
                    Debug.LogWarning("Item buy received: " + gameEvent.Data);
                    var data = JsonConvert.DeserializeObject<ItemTradeUpdate>(gameEvent.Data);
                    var player = playerManager.GetPlayerByUserId(data.BuyerId);
                    if (player)
                    {
                        var coinsBefore = player.Resources.Coins;
                        player.RemoveResource(Resource.Currency, data.Cost);
                        var coinsChange = coinsBefore - player.Resources.Coins;
                        Debug.Log("Coins change after item buy: " + coinsChange);
                    }
                    return;
                }
            case GameEventType.ItemSell:
                {
                    Debug.LogWarning("Item sell received: " + gameEvent.Data);
                    var data = JsonConvert.DeserializeObject<ItemTradeUpdate>(gameEvent.Data);
                    var player = playerManager.GetPlayerByUserId(data.SellerId);
                    if (player)
                    {
                        var coinsBefore = player.Resources.Coins;
                        player.AddResource(Resource.Currency, data.Cost, false);
                        var coinsChange = coinsBefore - player.Resources.Coins;
                        Debug.Log("Coins change after item sell: " + coinsChange);
                    }
                    return;
                }
        }
    }

    private void OnServerMessage(ServerMessage serverMessage)
    {
        Debug.Log("Server Message: " + serverMessage.Message);
    }

    private void OnPlayerRemove(PlayerRemove playerInfo)
    {
        var player = Players.GetPlayerByUserId(playerInfo.UserId);
        if (!player)
        {
            Debug.Log($"Received Player Remove ({playerInfo.UserId}) but the player is not in this game. Reason: " + playerInfo.Reason);
            return;
        }

        QueueRemovePlayer(player);
        Debug.Log($"{player.PlayerName} removed from the game. Reason: " + playerInfo.Reason);
    }

    private async void OnStreamerRaid(StreamRaidInfo raidInfo, bool raidWar)
    {
        var players = Players.GetAllPlayers();
        if (raidInfo.Players.Count == 0)
        {
            Server.Client?.SendCommand("", "message", raidInfo.RaiderUserName + " raided but without any players. Kappa");
            return;
        }

        // only one active raid war at a time.
        raidWar = raidWar && !StreamRaid.Started && !StreamRaid.IsWar;
        StreamRaid.AnnounceRaid(raidInfo, raidWar && players.Count > 0);

        if (players.Count == 0 && raidWar)
        {
            Server.Client?.SendCommand("", "message",
                raidInfo.RaiderUserName + " raided with intent of war but we don't have any players. FeelsBadMan");
        }

        StreamRaid.ClearTeams();

        if (!StreamRaid.Started && raidWar)
        {
            foreach (var player in players)
            {
                if (raidInfo.Players.Contains(player.UserId)) continue;
                StreamRaid.AddToStreamerTeam(player);
            }
        }

        var raiders = new List<PlayerController>();
        foreach (var user in raidInfo.Players)
        {
            var existingPlayer = Players.GetPlayerByUserId(user);
            if (existingPlayer)
            {
                RemovePlayer(existingPlayer);
            }

            var player = await AddPlayerByUserIdAsync(user, raidInfo);
            if (player && raidWar)
            {
                raiders.Add(player);
            }
        }

        if (raidWar)
        {
            StartCoroutine(StartRaidWar(raiders));
        }
    }

    private IEnumerator StartRaidWar(IReadOnlyList<PlayerController> raiders)
    {
        yield return new WaitForEndOfFrame();
        foreach (var raider in raiders)
        {
            StreamRaid.AddToRaiderTeam(raider);
            yield return new WaitForEndOfFrame();
        }

        StreamRaid.StartRaidWar();
    }

    private async Task<PlayerController> AddPlayerByUserIdAsync(string userId, StreamRaidInfo raiderInfo)
    {
        var playerInfo = await RavenNest.PlayerJoinAsync(userId, "");
        if (playerInfo == null)
        {
            return null;
        }

        return SpawnPlayer(playerInfo, raidInfo: raiderInfo);
    }

    private void OnApplicationQuit()
    {
        StopRavenNestSession();

        Debug.Log("Application ending after " + Time.time + " seconds");
        var client = Server.Client;
        if (client == null) return;

        Server.Stop();
        SaveEmptyGameStats();
    }

    private void HandleRavenNestConnection()
    {
        var client = ravenNest;
        if (logger == null)
            logger = new RavenNest.SDK.UnityLogger();

        if (client == null)
        {
            client = new RavenNestClient(
                logger,
                this,
            new RavenNestStreamSettings()
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

            if (IntegrityCheck.IsCompromised)
            {
                return;
            }

            await SavePlayersAsync();
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
        if (Interlocked.CompareExchange(ref saveCounter, 1, 0) == 1)
        {
            return;
        }

        Debug.Log("Saving all active players!!");
        try
        {
            var players = playerManager
                .GetAllPlayers();
            var states = players
                .Select(x => x.BuildPlayerState())
                .ToArray();

            var result = await ravenNest.Players.UpdateManyAsync(states);
            if (result.All(x => x))
            {
                Debug.Log("All players saved successfully!");

                foreach (var player in players.Select((res, index) => new { Result = res, Player = players[index] }).Where(x => x.Result))
                {
                    player.Player.SavedSucceseful();
                }
            }
            else
            {
                var successCount = result.Count(x => x);
                var totalCount = result.Length;

                Debug.LogWarning($"{successCount} out of {totalCount} players saved successfully!");
            }
        }
        catch (Exception exc)
        {
            Debug.LogError(exc.ToString());
        }
        finally
        {
            Interlocked.Decrement(ref saveCounter);
        }
    }
    private void UpdateChatBotCommunication()
    {
        if (Server == null || !Server.IsBound)
        {
            return;
        }

        Server.HandleNextPacket(this, Server, playerManager);
    }

    //private IslandController lastIslandToggle = null;
    private void HandleKeyDown()
    {
        var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var isControlDown = isShiftDown || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

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


        if (Input.GetKeyUp(KeyCode.Space))
        {
            var player = Players.FindPlayers("zerratar").FirstOrDefault();
            if (player)
            {
                var def = JsonConvert.DeserializeObject<RavenNestPlayer>(JsonConvert.SerializeObject(player.Definition));
                def.UserId = UnityEngine.Random.Range(12345, 99999).ToString();
                def.Name += def.UserId;
                def.UserName += def.UserId;
                var p = SpawnPlayer(def);
                if (raidManager.Started)
                {
                    raidManager.Join(p);
                }
            }
        }

        if (isControlDown && Input.GetKeyUp(KeyCode.R))
        {
            raidManager.StartRaid();
        }

        if (Permissions.IsAdministrator || Application.isEditor)
        {
            if (isControlDown && Input.GetKeyDown(KeyCode.Delete))
            {
                subEventManager.Reset();
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.A))
            {
                subEventManager.Activate();
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.X))
            {
                Twitch.OnSubscribe(new TwitchSubscription(null, null, null, null, 1, true));
            }

            if (isControlDown && Input.GetKeyUp(KeyCode.C))
            {
                Twitch.OnSubscribe(new TwitchSubscription(null, null, null, null, -1, true));
            }
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
            boostTimer.SetSubscriber(subEventManager.CurrentBoost.LastSubscriber);
            boostTimer.SetText(
                $"EXP Multiplier x{subEventManager.CurrentBoost.Multiplier} - {Mathf.FloorToInt(subEventManager.Duration - subEventManager.CurrentBoost.Elapsed)}s left");
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

    private void SaveEmptyGameStats()
    {
        SaveGameStat("online-player-count", 0);
        SaveGameStat("last-joined-name", "");
        SaveGameStat("last-level-up-name", "");
        SaveGameStat("last-level-up-skill", "");
    }

    public void PlayerJoined(PlayerController player)
    {
        SaveGameStat("last-joined-name", player.PlayerName);
        SaveGameStat("online-player-count", playerManager.GetPlayerCount());
    }

    public void PlayerLevelUp(PlayerController player, SkillStat skill)
    {
        SaveGameStat("last-level-up-name", player.PlayerName);
        SaveGameStat("last-level-up-skill", skill?.ToString());
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
}