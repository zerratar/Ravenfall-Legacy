using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    private readonly List<PlayerController> joinedPlayers
        = new List<PlayerController>();

    private readonly List<PlayerController> deadPlayers
        = new List<PlayerController>();

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

    //private EnemyController[] generatedEnemies;



    public Vector3 StartingPoint
    {
        get
        {
            if (!this.Dungeon)
            {
                return Vector3.zero;
            }

            if (this.Dungeon.HasStartingPoint && this.Dungeon.HasPredefinedRooms)
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
        return dungeonEnemyPool.HasLeasedEnemies ? dungeonEnemyPool.GetLeasedEnemies().Where(IsAlive).ToList() : new List<EnemyController>();
    }
    internal EnemyController GetNextEnemyTarget(PlayerController player)
    {
        if (Dungeon.HasPredefinedRooms)
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

        EnemyController enemyTarget = null;

        var aliveEnemies = this.GetAliveEnemies();
        if (aliveEnemies.Count > 0)
        {
            enemyTarget = aliveEnemies.GetNextEnemyTargetExceptBoss(player);//GetNextEnemyTarget(player, x => !x.name.Contains("_BOSS_"));
        }

        var boss = Boss;
        if (!enemyTarget && boss)
        {
            return boss.Enemy;
        }

        return enemyTarget;
    }

    public DungeonBossController Boss
    {
        get
        {
            if (!Dungeon)
            {
                return null;
            }

            if (Dungeon.HasPredefinedRooms)
            {
                return Dungeon.BossRoom?.Boss;
            }

            if (!generatedBoss)
            {
                generatedBoss = FindObjectOfType<DungeonBossController>();
            }

            return generatedBoss;
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
        lock (mutex)
        {
            if (this.deadPlayers.Count >= this.joinedPlayers.Count)
            {
                return 0;
            }

            return joinedPlayers.Count - this.deadPlayers.Count;
        }
    }
    internal IReadOnlyList<PlayerController> GetAlivePlayers()
    {
        lock (mutex)
        {
            if (this.deadPlayers.Count == this.joinedPlayers.Count)
            {
                return new List<PlayerController>();
            }

            return joinedPlayers.Where(x => this.deadPlayers.All(y => y.Id != x.Id)).ToList();
        }
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
            ActivateDungeon();
            yield return null;
        }
        else
        {
            ResetDungeon();
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

        return enemies.Where(x => Vector3.Distance(position, x.Position) <= x.AggroRange).ToList();
    }

    private void HideDungeonUI()
    {
        Notifications.HideInformation();
    }
    private void UpdateDungeonUI()
    {
        if (this.Dungeon && !this.Dungeon.HasPredefinedRooms && Started)
        {
            Notifications.UpdateInformation(this.GetAlivePlayerCount(), this.GetAliveEnemyCount(), this.Boss);
        }
    }

    public bool ActivateDungeon()
    {
        if (gameManager.Events.TryStart(this))
        {
            SelectRandomDungeon();

            if (SpawnDungeonBoss())
            {
                state = DungeonManagerState.Active;
                dungeonStartTimer = timeForDungeonStart;
                nextDungeonTimer = 0f;

                AnnounceDungeon();
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

    public bool CanJoin(PlayerController player)
    {
        if (!Active) return false;

        lock (mutex)
        {
            return !joinedPlayers.Contains(player);
        }
    }

    public void Join(PlayerController player)
    {
        if (!Active) return;

        lock (mutex)
        {
            if (joinedPlayers.Contains(player)) return;
            joinedPlayers.Add(player);

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
    }

    public void EndDungeonSuccess()
    {
        //Debug.LogWarning("EndDungeonSuccess");
        // 1. reward all players
        RewardPlayers();
        // 2. show some victory UI
        ResetDungeon();
    }

    public void EndDungeonFailed()
    {
        var players = GetPlayers();
        foreach (var player in players)
        {
            player.Dungeon.OnExit();
        }

        //Debug.LogWarning("EndDungeonFailed");
        // 1. show sad UI
        ResetDungeon();
    }

    private bool SpawnDungeonBoss()
    {
        if (Dungeon.HasPredefinedRooms && !dungeonBossPrefab)
        {
            GameManager.LogError("NO DUNGEON BOSS PREFAB SET!!!");
            return false;
        }

        try
        {
            var players = gameManager.Players.GetAllPlayers();
            var highestStats = players.Max(x => x.Stats);
            var lowestStats = players.Min(x => x.Stats);
            var rngLowEq = players.Min(x => x.EquipmentStats);
            var rngHighEq = players.Max(x => x.EquipmentStats);

            if (Dungeon.HasPredefinedRooms)
            {
                Dungeon.BossRoom.Boss = Instantiate(dungeonBossPrefab, Dungeon.BossSpawnPoint, Quaternion.identity)
                     .GetComponent<DungeonBossController>();
            }

            if (!this.Boss)
            {
                GameManager.LogError("Failed to spawn dungeon boss. Unknown reason");
                return false;
            }

            Boss.Create(
                lowestStats * combatStatsScale,
                highestStats * combatStatsScale,
                rngLowEq * equipmentStatsScale,
                rngHighEq * equipmentStatsScale,
                GetBossHealthScale());

            return true;
        }
        catch (Exception exc)
        {
            GameManager.LogError("Unable to create dungeon boss. Error: " + exc);
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

            foreach (var enemy in enemies)
            {
                var starting = lowestStats;
                var high = lowestStats + highestStats;
                enemy.Stats = Skills.Max(starting, Skills.Lerp(starting, high, lerpAmount) * mobsCombatStatsScale);
                enemy.SetExperience(GameMath.CombatExperience(enemy.Stats.CombatLevel) * 1.5d);
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
            return healthScale * (playerCount / (float)MinPlayerCountForHealthScaling);
        }
    }

    private void RewardPlayers()
    {
        lock (mutex)
        {
            foreach (var player in joinedPlayers)
            {
                Dungeon.RewardPlayer(player, yieldSpecialReward);
            }
        }
    }

    private void ResetDungeon()
    {
        try
        {
            if (!this.gameManager.Tavern.IsActivated)
            {
                gameManager.Camera.ReleaseFreeCamera();
            }

            //generatedEnemies = null;

            dungeonEnemyPool.ReturnAll();

            state = DungeonManagerState.None;
            lock (mutex) joinedPlayers.Clear();
            deadPlayers.Clear();

            HideDungeonUI();

            if (!currentDungeon) return;

            currentDungeon.ResetRooms();

            if (currentDungeon.HasPredefinedRooms)
            {
                currentDungeon.DisableContainer();
            }
            if (!this.Dungeon.HasPredefinedRooms)
            {
                var d = this.Dungeon.GetComponentInChildren<AutoGenerateDungeon>();
                if (d)
                {
                    d.DestroyDungeon();
                }
            }

            currentDungeon = null;

            notificationTimer = notificationUpdate;
            dungeonStartTimer = timeForDungeonStart;

            gameManager.Camera.DisableDungeonCamera();

            Boss = null;

        }
        catch (Exception exc)
        {
            GameManager.LogError(exc.ToString());
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

    private void UpdateDungeonTimer()
    {
        if (Started) return;
        if (nextDungeonTimer > 0f)
        {
            nextDungeonTimer -= Time.deltaTime;
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

    private void AnnounceDungeon()
    {
        var ioc = gameManager.gameObject.GetComponent<IoCContainer>();
        var evt = ioc.Resolve<EventTriggerSystem>();
        evt.TriggerEvent("dungeon", TimeSpan.FromSeconds(10));

        Notifications.SetTimeout(dungeonStartTimer);
        Notifications.SetLevel(Boss.Enemy.Stats.CombatLevel);
        Notifications.ShowDungeonActivated();

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
                currentDungeon.Name = "Legendary Dungeoon";
            }

            SpawnEnemies();
        }

        // 1. announce dungeon event
        gameManager.RavenBot.Announce(currentDungeon.Name + " is available. Type !dungeon to join.");
    }
    private void SelectRandomDungeon()
    {
        currentDungeon = dungeons.Weighted(x => x.SpawnRate);

        // Might not be needed.
        dungeonEnemyPool.ReturnAll();

        if (!currentDungeon.HasPredefinedRooms)
        {
            //UnityEngine.Debug.LogError("Procedural Generated Dungeon Selected.");
            var d = this.Dungeon.GetComponentInChildren<AutoGenerateDungeon>();
            if (d)
            {
                d.GenerateDungeon();
            }

            SpawnEnemies();
        }
        else
        {
            currentDungeon.EnableContainer();
        }

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

        // Spawn the necessary amount of enemies in their right positions.
        // Do this by 1. find out all positions we can spawn enemies
        //            2. Lock movements, warp them and make sure they are set active
        var spawnPoints = currentDungeon.gameObject.GetComponentsInChildren<EnemySpawnPoint>();

        foreach (var point in spawnPoints)
        {
            var enemy = this.dungeonEnemyPool.Lease();
            enemy.transform.position = point.transform.position;
            enemy.gameObject.SetActive(true);
        }

        //this.generatedEnemies = this.currentDungeon
        //    .GetComponentsInChildren<EnemyController>()
        //    .Where(x => x != null && x.gameObject != null && !x.name.Contains("_BOSS_"))
        //    .ToArray();
    }
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