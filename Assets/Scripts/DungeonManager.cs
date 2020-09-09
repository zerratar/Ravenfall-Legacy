using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonManager : MonoBehaviour, IEvent
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject dungeonBossPrefab;
    [SerializeField] private DungeonNotifications dungeonNotifications;
    [SerializeField] private float minTimeBetweenDungeons = 900;
    [SerializeField] private float maxTimeBetweenDungeons = 2700;
    [SerializeField] private float timeForDungeonStart = 180;
    [SerializeField] private float notificationUpdate = 30;

    private readonly object mutex = new object();

    private readonly List<PlayerController> joinedPlayers
        = new List<PlayerController>();

    private readonly List<PlayerController> deadPlayers
        = new List<PlayerController>();

    private DungeonController[] dungeons;
    private DungeonController currentDungeon;

    private float dungeonStartTimer;
    private float nextDungeonTimer;
    private float notificationTimer;
    private DungeonState state;

    public DungeonNotifications Notifications => dungeonNotifications;
    public bool Active => state >= DungeonState.Active;
    public bool Started => state == DungeonState.Started;

    public DungeonController Dungeon => currentDungeon;

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        dungeons = GetComponentsInChildren<DungeonController>();
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

    // Update is called once per frame
    private void Update()
    {
        UpdateDungeonTimer();
        UpdateDungeonStartTimer();
        UpdateDungeon();
    }

    private void UpdateDungeon()
    {
        if (!Active || !Started)
            return;

        gameManager.Camera.EnableDungeonCamera();

        lock (mutex)
        {
            if (deadPlayers.Count == joinedPlayers.Count || joinedPlayers.All(x => !x || x == null))
            {
                EndDungeonFailed();
                return;
            }
        }
    }

    public void ActivateDungeon()
    {
        if (gameManager.Events.TryStart(this))
        {
            state = DungeonState.Active;
            dungeonStartTimer = timeForDungeonStart;
            nextDungeonTimer = 0f;

            SelectRandomDungeon();
            if (SpawnDungeonBoss())
            {
                AnnounceDungeon();
            }
        }
        else
        {
            nextDungeonTimer = gameManager.Events.RescheduleTime;
        }
    }

    public void ForceStartDungeon()
    {
        if (state != DungeonState.Active)
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

            AdjustBossStats();
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

    private void EndDungeonFailed()
    {
        //Debug.LogWarning("EndDungeonFailed");
        // 1. show sad UI
        ResetDungeon();
    }

    private bool SpawnDungeonBoss()
    {
        if (!dungeonBossPrefab)
        {
            Debug.LogError("NO DUNGEON BOSS PREFAB SET!!!");
            return false;
        }

        var spawnPosition = Dungeon.BossSpawnPoint;

        //var players = GetPlayers();
        var players = gameManager.Players.GetAllPlayers();
        var highestStats = players.Max(x => x.Stats);
        var lowestStats = players.Min(x => x.Stats);
        var rngLowEq = players.Min(x => x.EquipmentStats);
        var rngHighEq = players.Max(x => x.EquipmentStats);

        var bossRoom = Dungeon.BossRoom;
        bossRoom.Boss = Instantiate(dungeonBossPrefab, spawnPosition, Quaternion.identity).GetComponent<DungeonBossController>();
        bossRoom.Boss.Create(lowestStats, highestStats, rngLowEq, rngHighEq);
        return true;
    }

    private void AdjustBossStats()
    {
        lock (mutex)
        {
            var bossRoom = Dungeon.BossRoom;
            if (bossRoom.Boss)
            {
                var highestStats = joinedPlayers.Max(x => x.Stats);
                var lowestStats = joinedPlayers.Min(x => x.Stats);
                var rngLowEq = joinedPlayers.Min(x => x.EquipmentStats);
                var rngHighEq = joinedPlayers.Max(x => x.EquipmentStats);

                bossRoom.Boss.SetStats(lowestStats, highestStats * 0.75f, rngLowEq, rngHighEq);
            }
        }
    }

    private void RewardPlayers()
    {
        lock (mutex)
        {
            foreach (var player in joinedPlayers)
            {
                Dungeon.RewardPlayer(player);
            }
        }
    }

    private void ResetDungeon()
    {
        state = DungeonState.None;
        lock (mutex) joinedPlayers.Clear();
        deadPlayers.Clear();
        if (!currentDungeon) return;
        currentDungeon.ResetRooms();
        currentDungeon = null;
        notificationTimer = notificationUpdate;
        dungeonStartTimer = timeForDungeonStart;

        gameManager.Camera.DisableDungeonCamera();
        ScheduleNextDungeon();

        gameManager.Events.End(this);
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

        gameManager.Server.Client.SendMessage("", $"{minutesLeft:00}m{secondsLeft:00}s until dungeon starts.");
        notificationTimer = notificationUpdate;
    }

    private void UpdateDungeonTimer()
    {
        if (Started) return;
        UpdateTimer(ref nextDungeonTimer, ActivateDungeon);
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
        lock (mutex)
        {
            if (!joinedPlayers.Any())
            {
                ResetDungeon();
                return;
            }
            state = DungeonState.Started;
            currentDungeon.Enter();
        }
    }

    private void AnnounceDungeon()
    {
        Notifications.SetTimeout(dungeonStartTimer);
        Notifications.SetLevel(Dungeon.BossRoom.Boss.Enemy.Stats.CombatLevel);
        Notifications.ShowDungeonActivated();

        // 1. announce dungeon event
        gameManager.Server.Announce(currentDungeon.Name + " is available. Type !dungeon to join.");
    }

    private void SelectRandomDungeon()
    {
        currentDungeon = dungeons.Random();
    }
}
public enum DungeonState
{
    None,
    Active,
    Started
}