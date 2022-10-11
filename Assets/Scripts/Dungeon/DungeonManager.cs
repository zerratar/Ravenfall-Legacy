using Shinobytes.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    [SerializeField] private ItemDropList[] itemDropLists;

    [Header("Dungeon Boss Settings")]
    [SerializeField] private float healthScale = 100f;
    [SerializeField] private float equipmentStatsScale = 0.33f;
    [SerializeField] private float combatStatsScale = 0.75f;
    [SerializeField] private float mobsCombatStatsScale = 0.33f;

    private readonly object mutex = new object();

    private readonly List<PlayerController> joinedPlayers = new();
    private readonly List<PlayerController> deadPlayers = new();
    private readonly List<PlayerController> alivePlayers = new();

    private DungeonController currentDungeon;

    private float dungeonStartTimer;
    private float nextDungeonTimer;
    private float notificationTimer;
    private DungeonManagerState state;

    public DungeonNotifications Notifications => dungeonNotifications;
    public bool Active => state >= DungeonManagerState.Active;
    public bool Started => state == DungeonManagerState.Started;
    public DungeonController Dungeon => currentDungeon;
    public bool IsBusy { get; internal set; }

    private bool yieldSpecialReward = false;

    private DungeonBossController generatedBoss;

    public string RequiredCode;

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
    private static bool IsAlive(EnemyController x)
    {
        return x != null && x.gameObject != null && !x.Stats.IsDead && x.Stats.Health.CurrentValue > 0;
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
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        if (dungeons == null || dungeons.Length == 0)
        {
            dungeons = GetComponentsInChildren<DungeonController>();
            //foreach (var dungeon in dungeons)
            //{
            //    dungeon.gameObject.SetActive(false);
            //}
        }

        ScheduleNextDungeon();
    }

    private void ScheduleNextDungeon()
    {
        nextDungeonTimer = UnityEngine.Random.Range(minTimeBetweenDungeons, maxTimeBetweenDungeons);
    }

    internal IReadOnlyList<PlayerController> GetPlayers()
    {
        lock (mutex) return joinedPlayers;
    }

    internal int GetAlivePlayerCount()
    {
        return alivePlayers.Count;

        //lock (mutex)
        //{
        //    if (this.deadPlayers.Count >= this.joinedPlayers.Count)
        //    {
        //        return 0;
        //    }

        //    return joinedPlayers.Count - this.deadPlayers.Count;
        //}
    }
    internal IReadOnlyList<PlayerController> GetAlivePlayers()
    {
        return alivePlayers;

        //lock (mutex)
        //{            
        //    if (this.deadPlayers.Count == this.joinedPlayers.Count)
        //    {
        //        return new List<PlayerController>();
        //    }

        //    return joinedPlayers.Where(x => this.deadPlayers.All(y => y.Id != x.Id)).AsList();
        //}
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateDungeonTimer();
        UpdateDungeonStartTimer();
        UpdateDungeon();
    }

    public void ToggleDungeon()
    {
        StartCoroutine(DoBadThings());
    }

    private IEnumerator DoBadThings()
    {
        //UnityEngine.Debug.LogWarning("Generating 100 dungeons...");
        //var start = DateTime.Now;
        //for (var i = 0; i < 100; ++i)
        //{
        if (this.state == DungeonManagerState.None)
        {
            yield return ActivateDungeon();
        }
        else
        {
            EndDungeonFailed();
            yield return null;
        }
        //}

        //UnityEngine.Debug.LogWarning("Took " + (DateTime.Now - start).TotalSeconds + " seconds.");
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

        if (!Active || !Started)
            return;

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

    public async Task<bool> ActivateDungeon()
    {
        if (gameManager.Events.TryStart(this))
        {
            await Notifications.OnDungeonActivated();

            SelectRandomDungeon();

            if (SpawnDungeonBoss())
            {
                state = DungeonManagerState.Active;
                dungeonStartTimer = timeForDungeonStart;
                nextDungeonTimer = 0f;
                if (gameManager.RequireCodeForDungeonOrRaid)
                {
                    RequiredCode = EventCode.New();
                }
                AnnounceDungeon(RequiredCode);
            }
            else
            {
                gameManager.Events.End(this);
                return false;
            }
        }
        else
        {
            nextDungeonTimer = gameManager.Events.RescheduleTime;
        }
        return true;
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
            return joinedPlayers.Contains(player);
        }
    }

    internal void Remove(PlayerController player)
    {
        lock (mutex)
        {
            joinedPlayers.Remove(player);
            deadPlayers.Remove(player);
        }
    }
    public DungeonJoinResult CanJoin(PlayerController player)
    {
        if (!Active) return DungeonJoinResult.NoActiveDungeon;

        lock (mutex)
        {
            if (joinedPlayers.Contains(player))
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

        lock (mutex)
        {
            if (joinedPlayers.Contains(player)) return;
            joinedPlayers.Add(player);
            alivePlayers.Add(player);

            SpawnEnemies();

            AdjustBossStats();
            AdjustEnemyStats();

            gameManager.EventTriggerSystem.SendInput(player.UserId, "dungeon");
        }
    }

    public void PlayerDied(PlayerController player)
    {
        if (deadPlayers.Contains(player)) return;
        deadPlayers.Add(player);
        alivePlayers.Remove(player);
    }

    public void EndDungeonSuccess()
    {
        //Debug.LogWarning("EndDungeonSuccess");
        // 1. reward all players
        RewardPlayers();
        // 2. show some victory UI
        ResetDungeon();
    }

    public void EndDungeonFailed(bool notifyChat = true)
    {
        var players = GetPlayers();
        foreach (var player in players)
        {
            player.Dungeon.OnExit();
        }

        //Debug.LogWarning("EndDungeonFailed");
        // 1. show sad UI
        ResetDungeon();

        if (notifyChat)
        {
            gameManager.RavenBot.Announce("The dungeon has ended without any surviving players.");
        }
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
                lowestStats * combatStatsScale,
                highestStats * combatStatsScale,
                rngLowEq * equipmentStatsScale,
                rngHighEq * equipmentStatsScale,
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

            Dungeon.RewardItemDrops(joinedPlayers);

            foreach (var player in joinedPlayers)
            {
                Dungeon.AddExperienceReward(player);
            }
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

            //generatedEnemies = null;
            state = DungeonManagerState.None;
            lock (mutex) joinedPlayers.Clear();
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

            currentDungeon = null;

            notificationTimer = notificationUpdate;
            dungeonStartTimer = timeForDungeonStart;

            gameManager.Camera.DisableDungeonCamera();

            Boss = null;

        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
        finally
        {
            ScheduleNextDungeon();
            gameManager.Events.End(this);
        }
    }

    private void UpdateDungeonStartTimer()
    {
        if (!Active || Started) return;
        var timeLeft = TimeSpan.FromSeconds(dungeonStartTimer);
        Notifications.UpdateTimer(timeLeft);

        UpdateTimer(ref dungeonStartTimer, StartDungeon);

        lock (mutex) if (!joinedPlayers.Any()) return;
        UpdateTimer(ref notificationTimer, SendNotification);
    }

    private void SendNotification()
    {
        if (!Active || Started) return;

        var timeLeft = TimeSpan.FromSeconds(dungeonStartTimer);
        var secondsLeft = timeLeft.Seconds;
        var minutesLeft = timeLeft.Minutes;

        gameManager.RavenBot.Broadcast("{minutes}m{seconds}s until dungeon starts.", minutesLeft.ToString("00"), secondsLeft.ToString("00"));
        notificationTimer = notificationUpdate;
    }

    private async void UpdateDungeonTimer()
    {
        if (Started) return;
        if (nextDungeonTimer > 0f)
        {
            nextDungeonTimer -= Time.deltaTime;
            if (nextDungeonTimer <= 0f)
            {
                await ActivateDungeon();
            }
        }
    }

    private void UpdateTimer(ref float timer, Action action)
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                action();
            }
        }
    }

    private void StartDungeon()
    {
        SpawnEnemies();

        Notifications.Hide();

        lock (mutex)
        {
            if (!joinedPlayers.Any())
            {
                ResetDungeon();
                return;
            }
            state = DungeonManagerState.Started;
            currentDungeon.Enter();
        }
    }

    private void AnnounceDungeon(string code)
    {
        //var ioc = gameManager.gameObject.GetComponent<IoCContainer>();
        //var evt = ioc.Resolve<EventTriggerSystem>();
        //evt.TriggerEvent("dungeon", TimeSpan.FromSeconds(10));

        Notifications.SetTimeout(dungeonStartTimer);
        Notifications.SetLevel(Boss.Enemy.Stats.CombatLevel);
        Notifications.ShowDungeonActivated(code);

        var prefix = "";
        if (currentDungeon.Name.Contains("Heroic"))
        {
            prefix = "Heroic ";
        }

        if (currentDungeon.Tier == DungeonTier.Dynamic)
        {
            var bossMesh = Boss.transform.GetChild(0);
            if (bossMesh)
            {
                var n = bossMesh.name.Split('_');
                var type = n[n.Length - 2];
                var bossName = Utility.AddSpacesToSentence(type);
                currentDungeon.Name = DungeonNameGenerator.Generate(bossName);
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
            gameManager.RavenBot.Announce(prefix + currentDungeon.Name + " is available. Type '!dungeon code' to join. Find the code on the stream.");
        }
        else
        {
            gameManager.RavenBot.Announce(prefix + currentDungeon.Name + " is available. Type !dungeon to join.");
        }
    }
    private void SelectRandomDungeon()
    {
        currentDungeon = dungeons.Weighted(x => x.SpawnRate);
        currentDungeon.gameObject.SetActive(true);
        currentDungeon.EnableContainer();
        SpawnEnemies();

        if (itemDropLists != null && itemDropLists.Length > 0)
        {
            currentDungeon.ItemDrops.SetDropList(itemDropLists.Random());
        }
    }

    private void SpawnEnemies()
    {
        if (!this.currentDungeon || this.dungeonEnemyPool.HasLeasedEnemies)
        {
            return;
        }

        foreach (var room in currentDungeon.Rooms)
        {
            SpawnEnemiesInRoom(room);
        }
    }

    private void SpawnEnemiesInRoom(DungeonRoomController room)
    {
        room.ReloadEnemies();

        foreach (var point in room.GetComponentsInChildren<EnemySpawnPoint>())
        {
            var enemy = this.dungeonEnemyPool.Get();
            enemy.SpawnPoint = point;
            enemy.SetPosition(point.transform.position);
            enemy.transform.SetParent(room.EnemyContainer, true);
            enemy.gameObject.SetActive(true);
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
    WrongCode
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
    Started
}