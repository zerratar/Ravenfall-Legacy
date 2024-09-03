using Assets.Scripts;
using RavenNest.Models;
using Shinobytes.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class DungeonManager : MonoBehaviour, IEvent
{
    private const int MinPlayerCountForHealthScaling = 10;

    [SerializeField] private DungeonController[] dungeons;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject dungeonBossPrefab;
    [SerializeField] private DungeonNotifications dungeonNotifications;
    [SerializeField] private float minTimeBetweenDungeons = 900;
    [SerializeField] private float maxTimeBetweenDungeons = 2700;
    [SerializeField] private float timeForDungeonStart = 120;
    [SerializeField] private float notificationUpdate = 30;
    [SerializeField] private EnemyPool dungeonEnemyPool;

    [Header("Dungeon Boss Settings")]
    [SerializeField] private float healthScale = 100f;
    [SerializeField] private float equipmentStatsScale = 0.33f;
    [SerializeField] private float combatStatsScale = 0.75f;
    [SerializeField] private float mobsCombatStatsScale = 0.33f;

    private float lastDungeonRewardTime;

    private readonly object mutex = new object();

    private readonly HashSet<Guid> joinedPlayerId = new();
    private readonly List<PlayerController> joinedPlayers = new();
    private readonly List<PlayerController> deadPlayers = new();
    private readonly List<PlayerController> alivePlayers = new();

    private DungeonController currentDungeon;

    private float dungeonStartTimer;
    private float nextDungeonTimer;
    private float notificationTimer;
    private DateTime startedTime;

    public PlayerController Initiator { get; private set; }

    private DungeonManagerState state;
    public float SecondsUntilStart => dungeonStartTimer;
    public float SecondsUntilNext => nextDungeonTimer;
    public DungeonNotifications Notifications => dungeonNotifications;
    public bool Active => state == DungeonManagerState.Active || state == DungeonManagerState.Started;
    public bool Started => state == DungeonManagerState.Started;
    public DungeonController Dungeon => currentDungeon;
    public bool IsBusy { get; internal set; }
    public TimeSpan Elapsed => DateTime.UtcNow - startedTime;

    private bool yieldSpecialReward = false;

    private int dungeonIndex;
    private DungeonBossController generatedBoss;

    public string RequiredCode;

    public int Counter => dungeonIndex;

    private Queue<Func<Task>> rewardQueue = new Queue<Func<Task>>();

    public bool HasBeenAnnounced { get; private set; }

    public string EventName => Dungeon?.Name ?? "Dungeon";

    public bool IsEventActive => Active && Dungeon != null && Dungeon?.BossRoom?.Boss != null;

    private int isProcessingRewardQueue;
    private float adjustStatsTimer;

    public Vector3 StartingPoint
    {
        get
        {
            if (!this.Dungeon)
            {
                return Vector3.zero;
            }

            if (this.Dungeon.HasStartingPoint)
            {
                return this.Dungeon.StartingPoint;
            }

            var dsp = this.Dungeon.GetComponentInChildren<DungeonStartingPoint>();
            if (!dsp)
            {
                return Vector3.zero;
            }

            return dsp.transform.position + dsp.Offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAlive(EnemyController x)
    {
        return !x.Stats.IsDead && x.Stats.Health.CurrentValue > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEnemyCount()
    {
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies().Count : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAliveEnemyCount()
    {
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies().Count(IsAlive) : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAliveEnemies()
    {
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies().Any(IsAlive) : false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<EnemyController> GetAliveEnemies()
    {
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies().AsList(IsAlive) : new List<EnemyController>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<EnemyController> GetEnemies()
    {
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies() : new List<EnemyController>();
    }

    internal EnemyController GetNextEnemyTarget(PlayerController player)
    {
        var room = Dungeon.Room;
        var roomType = room.RoomType;

        if (roomType == DungeonRoomType.Start)
            return null;

        if (roomType == DungeonRoomType.Boss && room.Boss)
            return room.Boss.Enemy;

        if (roomType == DungeonRoomType.Room)
            return room.GetNextEnemyTarget(player);

        return null;
    }

    public DungeonBossController Boss
    {
        get
        {
            if (!Dungeon)
            {
                return null;
            }

            return Dungeon.BossRoom?.Boss;
        }
        set
        {
            if (Dungeon && Dungeon.BossRoom)
                Dungeon.BossRoom.Boss = value;

            generatedBoss = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

        if (dungeons == null || dungeons.Length == 0)
        {
            dungeons = GetComponentsInChildren<DungeonController>(true);
        }

        ScheduleNextDungeon();
    }

    private void ScheduleNextDungeon()
    {
        nextDungeonTimer = UnityEngine.Random.Range(minTimeBetweenDungeons, maxTimeBetweenDungeons);

    }

    internal int GetPlayerCount()
    {
        return joinedPlayers.Count;
    }

    internal IReadOnlyList<PlayerController> GetPlayers()
    {
        lock (mutex) return joinedPlayers;
    }

    internal int GetAlivePlayerCount()
    {
        return alivePlayers.Count;
    }

    internal int GetDeadPlayerCount()
    {
        return deadPlayers.Count;
    }

    internal IReadOnlyList<PlayerController> GetAlivePlayers()
    {
        return alivePlayers;
    }

    // Update is called once per frame
    private void Update()
    {
        if (PlayerSettings.Instance.DisableDungeons.GetValueOrDefault())
        {
            return;
        }

        if (GameCache.IsAwaitingGameRestore)
        {
            rewardQueue.Clear();
            return;
        }

        if (rewardQueue.Count > 0 && Interlocked.CompareExchange(ref isProcessingRewardQueue, 1, 0) == 0)
        {
            ProcessRewardQueueAsync();
        }

        if (adjustStatsTimer > 0)
        {
            adjustStatsTimer -= GameTime.deltaTime;
            if (adjustStatsTimer <= 0)
            {
                AdjustEnemyStats();
                AdjustBossStats();
            }
        }

        UpdateDungeonTimer();
        UpdateDungeonStartTimer();
        UpdateDungeon();
    }

    private async Task ProcessRewardQueueAsync()
    {
        try
        {
            if (rewardQueue.TryDequeue(out var addItems))
            {
                rewardQueue.Clear();
                await addItems();
            }
        }
        catch
        {
            rewardQueue.Clear();
        }
        finally
        {
            Interlocked.Exchange(ref isProcessingRewardQueue, 0);
        }
    }


    public void ToggleDungeon()
    {
        StartCoroutine(DoBadThings());
    }

    private IEnumerator DoBadThings()
    {
        //Shinobytes.Debug.LogWarning("Generating 100 dungeons...");
        //var start = DateTime.Now;
        //for (var i = 0; i < 100; ++i)
        //{
        if (this.state == DungeonManagerState.None)
        {
            yield return ActivateDungeon(gameManager.Players.GetRandom());
        }
        else
        {
            EndDungeonFailed();
            yield return null;
        }
        //}

        //Shinobytes.Debug.LogWarning("Took " + (DateTime.Now - start).TotalSeconds + " seconds.");
    }

    private void UpdateDungeon()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !gameManager.Tavern.IsActivated)
        {
            if (gameManager.Camera.ForcedFreeCamera)
            {
                gameManager.Camera.ReleaseFreeCamera();
            }
        }

        if (!Started)
        {
            return;
        }

        if (!Active)
        {
            // try force ending the event if it's still active
            gameManager.Events.End(this);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var modifier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl);
            if (gameManager.Camera.ForcedFreeCamera)
            {
                gameManager.Camera.ReleaseFreeCamera();
            }
            else if (modifier)
            {
                gameManager.Camera.ForceFreeCamera(false);
            }
        }

        gameManager.Camera.EnableDungeonCamera();

        UpdateDungeonUI();

        lock (mutex)
        {
            if (deadPlayers.Count == joinedPlayers.Count || joinedPlayers.All(x => !x || x == null))
            {
                EndDungeonFailed();
                return;
            }

            foreach (var alive in alivePlayers)
            {
                if (!alive.TrainingHealing)
                {
                    continue;
                }

                if (alive.Attackers.Count > 0)
                {
                    continue;
                }

                var enemy = GetNextEnemyTarget(alive);
                if (enemy != null && !enemy.TargetPlayer)
                {
                    enemy.SetTarget(alive);
                }
            }
        }
    }

    internal IReadOnlyList<EnemyController> GetEnemiesNear(Vector3 position)
    {
        var enemies = GetAliveEnemies();
        if (enemies.Count == 0)
        {
            return null;
        }

        return enemies.AsList(x => Vector3.Distance(position, x.Position) <= x.AggroRange);
    }

    private void HideDungeonUI()
    {
        Notifications.HideInformation();
    }

    private void UpdateDungeonUI()
    {
        if (this.Dungeon && Started)
        {
            Notifications.UpdateInformation(this.GetAlivePlayerCount(), this.GetAliveEnemyCount(), this.Boss);
        }
    }

    public async Task<bool> ActivateDungeon(PlayerController initiator = null, Action<string> onActivated = null)
    {
        try
        {
            if (gameManager.Events.TryStart(this, initiator != null))
            {

                HasBeenAnnounced = false;

                await Notifications.OnDungeonActivated();

                SelectRandomDungeon();

                if (SpawnDungeonBoss())
                {
                    Shinobytes.Debug.Log($"Dungeon #{(dungeonIndex + 1)} has been activated: " + Dungeon.Name);
                    Initiator = initiator;
                    state = DungeonManagerState.Active;
                    dungeonStartTimer = timeForDungeonStart;
                    nextDungeonTimer = 0f;
                    if (gameManager.RequireCodeForDungeonOrRaid)
                    {
                        RequiredCode = EventCode.New();
                    }

                    if (onActivated != null)
                    {
                        onActivated(RequiredCode);
                    }
                    else
                    {
                        AnnounceDungeon(RequiredCode);
                    }
                }
                else
                {
                    gameManager.Events.End(this);
                    return false;
                }

                gameManager.dungeonStatsJson.Update();
            }
            else
            {
                if (gameManager.Events.IsEventCooldownActive)
                {
                    Shinobytes.Debug.LogWarning("Dungeon could not be started. There is an active cooldown. " + gameManager.Events.EventCooldownTimeLeft + " seconds left.");
                }
                else
                {
                    Shinobytes.Debug.LogWarning("Dungeon could not be started.");
                }

                nextDungeonTimer = gameManager.Events.RescheduleTime;
            }
            return true;
        }
        catch (Exception exc)
        {
            gameManager.Events.End(this);
            Shinobytes.Debug.LogError("Error trying to activate dungeon: " + exc);
            return false;
        }
    }

    public void ForceStartDungeon()
    {
        if (state != DungeonManagerState.Active)
            return;

        notificationTimer = 0f;
        nextDungeonTimer = 0f;
        dungeonStartTimer = 0f;
        Notifications.Hide();
        StartDungeon();
    }
    internal bool JoinedDungeon(PlayerController player)
    {
        lock (mutex)
        {
            return joinedPlayerId.Contains(player.Id);
        }
    }

    internal void Remove(PlayerController player)
    {
        lock (mutex)
        {
            joinedPlayerId.Remove(player.Id);
            joinedPlayers.Remove(player);
            deadPlayers.Remove(player);
            player.dungeonHandler.Clear();
        }
    }
    public DungeonJoinResult CanJoin(PlayerController player)
    {
        if (!Active) return DungeonJoinResult.NoActiveDungeon;

        if (!player || player == null)
        {
            return DungeonJoinResult.Error;
        }

        lock (mutex)
        {
            if (joinedPlayerId.Contains(player.Id))
                return DungeonJoinResult.AlreadyJoined;

            return DungeonJoinResult.CanJoin;
        }
    }

    public DungeonJoinResult CanJoin(PlayerController player, string code)
    {
        var result = CanJoin(player);
        if (result == DungeonJoinResult.CanJoin && !string.IsNullOrEmpty(this.RequiredCode) && code != this.RequiredCode)
        {
            return DungeonJoinResult.WrongCode;
        }
        return result;
    }

    public void Join(PlayerController player)
    {
        if (!Active) return;
        if (!player || player == null)
        {
            return;
        }

        player.dungeonHandler.AutoJoining = false;

        lock (mutex)
        {
            if (joinedPlayerId.Contains(player.Id)) return;

            // don't interrupt until we are teleported to the dungeon.
            //player.InterruptAction();

            joinedPlayerId.Add(player.Id);
            joinedPlayers.Add(player);
            alivePlayers.Add(player);

            SpawnEnemies();

            adjustStatsTimer = 0.5f;
        }
    }

    public void PlayerDied(PlayerController player)
    {
        alivePlayers.Remove(player);
        if (deadPlayers.Contains(player)) return;
        deadPlayers.Add(player);
    }

    public void EndDungeonSuccess()
    {
        Shinobytes.Debug.Log("Dungeon ended in a success.");
        var players = GetPlayers();

        foreach (var player in players)
        {
            player.dungeonHandler.OnExit();
        }

        dungeonIndex++;

        //Debug.LogWarning("EndDungeonSuccess");
        // 1. reward all players
        RewardPlayers();

        // 2. show some victory UI
        ResetDungeon();

        gameManager.dungeonStatsJson.Update();
    }

    public void EndDungeonFailed(bool notifyChat = true)
    {
        if (notifyChat)
        {
            Shinobytes.Debug.Log("The dungeon has ended without any surviving players.");
        }
        else
        {
            Shinobytes.Debug.Log("The dungeon was stopped by a user.");
        }

        var players = GetPlayers();
        foreach (var player in players)
        {
            player.dungeonHandler.OnExit();
        }

        dungeonIndex++;

        //Debug.LogWarning("EndDungeonFailed");
        // 1. show sad UI
        ResetDungeon();

        if (notifyChat)
        {
            gameManager.RavenBot.Announce("The dungeon has ended without any surviving players.");
        }

        gameManager.dungeonStatsJson.Update();
    }

    private bool SpawnDungeonBoss()
    {
        if (!dungeonBossPrefab)
        {
            Shinobytes.Debug.LogError("NO DUNGEON BOSS PREFAB SET!!!");
            return false;
        }

        try
        {
            var players = gameManager.Players.GetAllPlayers();
            var highestStats = players.Max(x => x.Stats);
            var lowestStats = players.Min(x => x.Stats);
            var rngLowEq = players.Min(x => x.EquipmentStats);
            var rngHighEq = players.Max(x => x.EquipmentStats);

            Dungeon.BossRoom.Boss =
                  Instantiate(dungeonBossPrefab, Dungeon.BossSpawnPoint, Quaternion.identity)
                 .GetComponent<DungeonBossController>();

            if (!this.Boss)
            {
                Shinobytes.Debug.LogError("Failed to spawn dungeon boss. Unknown reason");
                return false;
            }

            Boss.Create(
                lowestStats * combatStatsScale * Dungeon.BossCombatScale,
                highestStats * combatStatsScale * Dungeon.BossCombatScale,
                rngLowEq * equipmentStatsScale * Dungeon.BossCombatScale,
                rngHighEq * equipmentStatsScale * Dungeon.BossCombatScale,
                GetBossHealthScale());

            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to create dungeon boss. Error: " + exc.Message);
            return false;
        }
    }

    private void AdjustEnemyStats()
    {
        var enemies = this.dungeonEnemyPool.GetLeasedEnemies();

        if (enemies.Count == 0)
        {
            return;
        }

        lock (mutex)
        {
            //generatedEnemies = generatedEnemies.Where(x => x != null && x.gameObject != null && x.transform != null).ToArray();

            var highestStats = joinedPlayers.Max(x => x.Stats);
            var lowestStats = joinedPlayers.Min(x => x.Stats);
            var rngLowEq = joinedPlayers.Min(x => x.EquipmentStats);
            var rngHighEq = joinedPlayers.Max(x => x.EquipmentStats);

            var avgCombatLevel = joinedPlayers.Sum(x => x.Stats.CombatLevel) / joinedPlayers.Count;
            var lerpAmount = avgCombatLevel / highestStats.CombatLevel;

            var avgDefense = joinedPlayers.Sum(x => x.Stats.Defense.Level) / joinedPlayers.Count;

            foreach (var enemy in enemies)
            {
                var starting = lowestStats;
                var high = lowestStats + highestStats;
                enemy.Stats = Skills.Max(starting, Skills.Lerp(starting, high, lerpAmount) * mobsCombatStatsScale * Dungeon.MobsDifficultyScale);

                // ugly hax, but will at least make enemies possible to kill.
                if (enemy.Stats.Defense.CurrentValue > avgDefense)
                {
                    enemy.Stats.Defense.CurrentValue = avgDefense;
                }
            }
        }
    }

    private void AdjustBossStats()
    {
        lock (mutex)
        {
            if (!this.Boss)
            {
                return;
            }

            var highestStats = joinedPlayers.Max(x => x.Stats);
            var lowestStats = joinedPlayers.Min(x => x.Stats);
            var rngLowEq = joinedPlayers.Min(x => x.EquipmentStats);
            var rngHighEq = joinedPlayers.Max(x => x.EquipmentStats);

            Boss.SetStats(
                lowestStats * combatStatsScale * Dungeon.BossCombatScale,
                highestStats * combatStatsScale * Dungeon.BossCombatScale,
                rngLowEq * equipmentStatsScale * Dungeon.BossCombatScale,
                rngHighEq * equipmentStatsScale * Dungeon.BossCombatScale,
                GetBossHealthScale());
        }
    }

    private float GetBossHealthScale()
    {
        lock (mutex)
        {
            var playerCount = Mathf.Max(MinPlayerCountForHealthScaling, joinedPlayers.Count);
            return healthScale * (playerCount / (float)MinPlayerCountForHealthScaling) * Dungeon.BossHealthScale;
        }
    }
    private void RewardPlayers()
    {
        lock (mutex)
        {
            var timeSinceLastReward = Time.time - lastDungeonRewardTime;
            if (timeSinceLastReward >= 5 || lastDungeonRewardTime <= 0)
            {
                RewardItemDrops(joinedPlayers);

                foreach (var player in joinedPlayers)
                {
                    Dungeon.AddExperienceReward(player);
                }
            }
            else
            {
#if DEBUG
                Shinobytes.Debug.LogWarning("Dungeon ended very quickly. We will be skipping the reward this time.");
#endif
            }
            lastDungeonRewardTime = Time.time;
        }
    }

    public async void RewardItemDrops(IReadOnlyList<PlayerController> joinedPlayers)
    {
        var playersToBeRewarded = joinedPlayers.Select(x => x.Id).ToArray();
        await RewardPlayersAsync(Dungeon.Tier, playersToBeRewarded);
    }

    public async Task RewardPlayersAsync(DungeonTier tier, Guid[] playersToBeRewarded, int retryCount = 0)
    {
        if (playersToBeRewarded == null || playersToBeRewarded.Length == 0)
        {
            return;
        }

        if (retryCount > 0)
        {
            if (retryCount > 1000)
            {
                return;
            }

            if (retryCount > 30)
            {
                await Task.Delay(60_000);
            }
            else
            {
                await Task.Delay((int)MathF.Min(retryCount * 1000, 10000));
            }
        }

        // make sure we retry later when we have server connection.
        if (!gameManager.RavenNest.Tcp.IsReady)
        {
            rewardQueue.Enqueue(() => RewardPlayersAsync(tier, playersToBeRewarded, retryCount));
            return;
        }

        var rewards = await gameManager.RavenNest.Game.GetDungeonRewardsAsync(tier, playersToBeRewarded);
        if (rewards == null)
        {
            // it could be that we are offline, or temporary issue saving. Lets enqueue it for later.
            rewardQueue.Enqueue(() => RewardPlayersAsync(tier, playersToBeRewarded, retryCount + 1));
            if (retryCount == 0)
            {
                gameManager.RavenBot.Announce("Victorious!! Dungeon boss was slain but unfortunately the connection to the server has been broken, rewards will be distributed later.");
            }
            return;
        }

        AddItems(rewards);
    }

    private void AddItems(EventItemReward[] rewards)
    {
        var result = gameManager.AddItems(rewards, dungeonIndex: dungeonIndex);

        if (result.Count > 0)
        {
            gameManager.RavenBot.Announce("Victorious!! The dungeon boss was slain and yielded " + result.Count + " item treasures!");
        }
        else
        {
            gameManager.RavenBot.Announce("Victorious!! The dungeon boss was slain but did not yield any treasure.");
        }

        foreach (var msg in result.Messages)
        {
            gameManager.RavenBot.Announce(msg);
        }
    }



    private void ResetDungeon()
    {
        try
        {
            RequiredCode = null;

            if (!this.gameManager.Tavern.IsActivated)
            {
                gameManager.Camera.ReleaseFreeCamera();
            }

            state = DungeonManagerState.None;

            lock (mutex)
            {
                joinedPlayers.Clear();
                joinedPlayerId.Clear();
            }

            alivePlayers.Clear();
            deadPlayers.Clear();

            if (currentDungeon)
            {
                currentDungeon.ResetRooms();
            }

            dungeonEnemyPool.ReturnAll();

            HideDungeonUI();

            if (!currentDungeon) return;

            currentDungeon.DisableContainer();
            currentDungeon.gameObject.SetActive(false);

            notificationTimer = notificationUpdate;
            dungeonStartTimer = timeForDungeonStart;

            gameManager.Camera.DisableDungeonCamera();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Reset Dungeon Err: " + exc);
        }
        finally
        {
            ScheduleNextDungeon();

            state = DungeonManagerState.None;
            currentDungeon = null;
            Boss = null;
            gameManager.Events.End(this);
        }
    }

    private void UpdateDungeonStartTimer()
    {
        if (!Active || Started) return;
        var timeLeft = TimeSpan.FromSeconds(dungeonStartTimer);
        Notifications.UpdateTimer(timeLeft);

        UpdateTimer(ref dungeonStartTimer, StartDungeon);

        lock (mutex) if (joinedPlayers.Count == 0) return;
        UpdateTimer(ref notificationTimer, SendNotification);
    }

    private void SendNotification()
    {
        if (!Active || Started) return;

        var timeLeft = TimeSpan.FromSeconds(dungeonStartTimer);
        var secondsLeft = timeLeft.Seconds;
        var minutesLeft = timeLeft.Minutes;
        if (minutesLeft > 0)
        {
            gameManager.RavenBot.Announce("{minutes}m{seconds}s until dungeon starts.", minutesLeft.ToString("00"), secondsLeft.ToString("00"));
        }
        else
        {
            gameManager.RavenBot.Announce("{seconds}s until dungeon starts.", secondsLeft.ToString("00"));
        }
        notificationTimer = notificationUpdate;
    }

    private void UpdateDungeonTimer()
    {
        if (Started) return;
        if (nextDungeonTimer > 0f)
        {
            nextDungeonTimer -= GameTime.deltaTime;
            if (nextDungeonTimer <= 0f)
            {
                ActivateDungeon();
            }
        }
    }

    private void UpdateTimer(ref float timer, Action action)
    {
        if (timer > 0f)
        {
            timer -= GameTime.deltaTime;
            if (timer <= 0f)
            {
                action();
            }
        }
    }

    private void StartDungeon()
    {
        Shinobytes.Debug.Log("Starting dungeon: " + currentDungeon.Name);
        SpawnEnemies();

        Notifications.Hide();

        lock (mutex)
        {
            if (joinedPlayers.Count == 0)
            {
                ResetDungeon();
                return;
            }
            startedTime = DateTime.UtcNow;
            state = DungeonManagerState.Started;
            currentDungeon.Enter();
        }

        gameManager.dungeonStatsJson.Update();
    }

    public void AnnounceDungeon(string code)
    {
        //var ioc = gameManager.gameObject.GetComponent<IoCContainer>();
        //var evt = ioc.Resolve<EventTriggerSystem>();
        //evt.TriggerEvent("dungeon", TimeSpan.FromSeconds(10));

        Notifications.SetTimeout(dungeonStartTimer);
        Notifications.SetLevel(Boss.Enemy.Stats.CombatLevel);
        Notifications.ShowDungeonActivated(code);

        var bossMesh = Boss.transform.GetChild(0);
        if (bossMesh)
        {
            var n = bossMesh.name.Split('_');
            var type = n[n.Length - 2];
            Boss.name = Utility.AddSpacesToSentence(type);
        }

        if (currentDungeon.Tier == DungeonTier.Dynamic)
        {
            if (bossMesh)
            {
                currentDungeon.Name = DungeonNameGenerator.Generate(Boss.name);
            }
            else
            {
                currentDungeon.Name = "Legendary Dungeon";
            }

            SpawnEnemies();
        }

        // 1. announce dungeon event
        if (gameManager.RequireCodeForDungeonOrRaid)
        {
            gameManager.RavenBot.Announce(currentDungeon.Name + " is available. Type '!dungeon code' to join. Find the code on the stream.");
        }
        else
        {
            gameManager.RavenBot.Announce(currentDungeon.Name + " is available. Type !dungeon to join.");
        }

        HasBeenAnnounced = true;

        gameManager.dungeonStatsJson.Update();
    }

    private void SelectRandomDungeon()
    {
        if (dungeons == null || dungeons.Length == 0)
        {
            dungeons = GetComponentsInChildren<DungeonController>(true);
        }

        if (dungeons.Length == 0)
        {
            throw new Exception("Unable to select a dungeon as no dungeon could be found.");
        }

        currentDungeon = dungeons.Weighted(x => x.SpawnRate);
        currentDungeon.gameObject.SetActive(true);
        currentDungeon.EnableContainer();
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        if (!this.currentDungeon || this.dungeonEnemyPool.HasLeasedEnemies)
        {
            return;
        }

        dungeonEnemyPool.ReturnAll();

        foreach (var room in currentDungeon.Rooms)
        {
            if (room.gameObject.activeInHierarchy)
            {
                SpawnEnemiesInRoom(room);
            }
        }
    }

    private void SpawnEnemiesInRoom(DungeonRoomController room)
    {
        foreach (var point in room.GetComponentsInChildren<EnemySpawnPoint>())
        {
            var enemy = this.dungeonEnemyPool.Get();
            enemy.SpawnPoint = point;

            if (enemy.IsUnreachable)
            {
                Shinobytes.Debug.LogWarning(enemy.name + " in room: " + room.name + ", in dungeon: " + currentDungeon.Name + ", was unreachable in previous run.");
            }

            enemy.IsUnreachable = false; // reset unreachable flag.
            enemy.gameObject.SetActive(true);
            enemy.Lock();
            enemy.SetPosition(point.transform.position);
            enemy.transform.SetParent(room.EnemyContainer, true);


            // check if this will actually spawn in a room.

        }

        room.ReloadEnemies();
        room.ResetRoom();
    }
}

public enum DungeonJoinResult
{
    CanJoin,
    NoActiveDungeon,
    AlreadyJoined,
    WrongCode,
    Error
}

public class DungeonNameGenerator
{
    private readonly static string[] nameFormats = new string[] {
        "{0} of the {1} {2}",
        "The {1} {2} {0}",
    };

    private readonly static string[] elements = new string[]
    {
        "Twisting",
        "Burning",
        "Northern",
        "Freezing",
        "Nameless",
        "Southern",
        "Voiceless",
        "Screaming",
        "Haunting",
        "Wandering",
        "Chaotic",
        "Perished",
        "Unrelented",
    };

    private readonly static string[] types = new string[]
    {
        "Crypt",
        "Burrows",
        "Catacombs",
        "Caverns",
        "Pits",
        "Labyrinth",
        "Dungeon",
        "Chambers",
        "Vault"
    };
    public static string Generate(string bossName)
    {
        // For nostalgia!
        if (UnityEngine.Random.value < 0.1f)
        {
            return "Luna's Tickle Basement";
        }

        return string.Format(nameFormats.Random(), types.Random(), elements.Random(), bossName);
    }
}

public enum DungeonManagerState
{
    None,
    Active,
    Started,
    //RewardingPlayers
}