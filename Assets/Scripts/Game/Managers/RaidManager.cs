﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaidManager : MonoBehaviour, IEvent
{
    [SerializeField] private GameCamera camera;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RaidNotifications notifications;
    [SerializeField] private GameObject raidBossPrefab;

    [SerializeField] private float minTimeBetweenRaids = 600;
    [SerializeField] private float maxTimeBetweenRaids = 3000;
    [SerializeField] private float minHealthToJoin = 0.1f;

    private readonly List<PlayerController> raidingPlayers
        = new List<PlayerController>();

    private readonly object mutex = new object();

    private float nextRaidTimer = 0f;

    private float minTimeoutSeconds = 90f;
    private float maxTimeoutSeconds = 900f;
    private float timeoutTimer = 0f;

    private float raidStartedTime = 0f;
    private float raidEndedTime = 0f;

    public IReadOnlyList<PlayerController> Raiders { get { lock (mutex) return raidingPlayers; } }
    public RaidBossController Boss { get; private set; }
    public RaidNotifications Notifications => notifications;

    //private float lastRaidEndTime = 0f;

    public bool Started => nextRaidTimer < 0f;

    public bool IsBusy { get; internal set; }

    private void Start()
    {
        nextRaidTimer = UnityEngine.Random.Range(minTimeBetweenRaids, maxTimeBetweenRaids);
    }

    public RaidJoinResult CanJoin(PlayerController player)
    {
        if (!Started || !Boss || !Boss.Enemy)
        {
            return RaidJoinResult.NoActiveRaid;
        }

        lock (mutex)
        {
            if (raidingPlayers.Contains(player))
            {
                return RaidJoinResult.AlreadyJoined;
            }
        }

        var currentHealth = (float)Boss.Enemy.Stats.Health.CurrentValue;
        var maxHealth = Boss.Enemy.Stats.Health.Level;
        if (currentHealth / maxHealth < minHealthToJoin)
        {
            return RaidJoinResult.MinHealthReached;
        }

        return RaidJoinResult.CanJoin;
    }

    public void Join(PlayerController player)
    {
        if (!Started)
        {
            return;
        }

        lock (mutex)
        {
            var bossHealth = Boss.Enemy.Stats.Health;
            if (raidingPlayers.Count == 0 || bossHealth.CurrentValue == bossHealth.Level)
            {
                // reset the start time until someone joins or boss health is the same.
                raidStartedTime = Time.time;
            }

            if (!raidingPlayers.Remove(player))
            {
                raidingPlayers.Add(player);
            }
        }

        player.Raid.OnEnter();

        //gameManager.EventTriggerSystem.SendInput(player.UserId, "raid");
    }

    public void Leave(PlayerController player, bool reward = false, bool timeout = false)
    {
        lock (mutex)
        {
            if (raidingPlayers.Remove(player))
            {
                player.Raid.OnLeave(reward, timeout);
            }
        }
    }

    public void StartRaid(string initiator = null)
    {
        if (gameManager.Events.TryStart(this))
        {
            notifications.OnBeforeRaidStart();

            gameManager.Music.PlayRaidBossMusic();

            if (!notifications.gameObject.activeSelf) notifications.gameObject.SetActive(true);

            nextRaidTimer = -1f;
            raidStartedTime = Time.time;
            camera.EnableRaidCamera();

            SpawnRaidBoss();

            notifications.ShowRaidBossAppeared();

            gameManager.RavenBot?.Announce(Localization.MSG_RAID_START, Boss.Enemy.Stats.CombatLevel.ToString());

            //var ioc = gameManager.gameObject.GetComponent<IoCContainer>();
            //if (ioc)
            //{
            //    var evt = ioc.Resolve<EventTriggerSystem>();
            //    if (evt != null)
            //    {
            //        evt.TriggerEvent("raid", TimeSpan.FromSeconds(10));
            //    }
            //}
            return;
        }
        else if (!string.IsNullOrEmpty(initiator))
        {
            gameManager.RavenBot?.Announce(Localization.MSG_RAID_START_ERROR);
        }

        nextRaidTimer = gameManager.Events.RescheduleTime;
    }

    public void EndRaid(bool bossKilled, bool timeout)
    {
        gameManager.Music.PlayBackgroundMusic();

        raidEndedTime = Time.time;
        camera.DisableFocusCamera();
        ScheduleNextRaid();
        notifications.HideRaidInfo();
        var itemDrop = gameManager.Raid.Boss.GetComponent<ItemDropHandler>();

        lock (mutex)
        {
            var playersToLeave = raidingPlayers.ToList();
            if(itemDrop != null && bossKilled) //Only when is killed should players be rewarded
                itemDrop.DropItemForPlayers(playersToLeave, DropType.StandardGuaranteed);
            foreach (var player in playersToLeave)
            {
                Leave(player, bossKilled, timeout);
            }
        }

        Destroy(Boss.gameObject);
        Boss = null;

        gameManager.Events.End(this);
    }

    private void ScheduleNextRaid()
    {
        nextRaidTimer = UnityEngine.Random.Range(minTimeBetweenRaids, maxTimeBetweenRaids);
    }

    public float GetParticipationPercentage(float enterTime)
    {
        var participationTime = raidEndedTime - enterTime;
        return participationTime / (raidEndedTime - raidStartedTime);
    }

    // Update is called once per frame
    private void Update()
    {
        var players = playerManager.GetAllPlayers();
        if (!Started && players.Count == 0)
        {
            return;
        }

        if (nextRaidTimer > 0f)
        {
            nextRaidTimer -= Time.deltaTime;
            if (nextRaidTimer <= 0f)
            {
                StartRaid();
            }
        }

        if (Started && players.Count == 0)
        {
            EndRaid(false, true);
            return;
        }

        if (!Boss && Started)
        {
            EndRaid(true, true);
            return;
        }

        if (notifications && Boss)
        {
            if (Boss.Enemy.Stats.Health.CurrentValue <= 0)
            {
                EndRaid(true, true);
                return;
            }

            timeoutTimer -= Time.deltaTime;
            if (timeoutTimer <= 0f && Boss.Enemy.Stats.Health.CurrentValue > 0)
            {
                EndRaid(false, true);
                return;
            }

            var proc = (float)Boss.Enemy.Stats.Health.CurrentValue / Boss.Enemy.Stats.Health.Level;
            if (proc < minHealthToJoin)
            {
                notifications.HideRaidJoinInfo();
            }

            notifications.SetRaidBossLevel(Boss.Enemy.Stats.CombatLevel);
            notifications.SetHealthBarValue(proc, Boss.Enemy.Stats.Health.Level);
            notifications.UpdateRaidTimer(timeoutTimer);
        }
    }

    private void SpawnRaidBoss()
    {
        if (!raidBossPrefab)
        {
            Shinobytes.Debug.LogError("NO RAID BOSS PREFAB SET!!!");
            return;
        }

        var spawnPosition = Vector3.zero;
        if (chunkManager)
        {
            var randomChunk = chunkManager
                .GetChunks()
                .OrderBy(x => UnityEngine.Random.value)
                .FirstOrDefault();

            if (randomChunk != null)
            {
                spawnPosition = randomChunk.CenterPointWorld + (Vector3.up * 3.4f);
            }
        }

        var players = playerManager.GetAllPlayers();
        var highestStats = players.Max(x => x.Stats);
        var lowestStats = players.Min(x => x.Stats);
        var rngLowEq = players.Min(x => x.EquipmentStats);
        var rngHighEq = players.Max(x => x.EquipmentStats);

        Boss = Instantiate(raidBossPrefab, spawnPosition, Quaternion.identity).GetComponent<RaidBossController>();

        Boss.Create(lowestStats, highestStats, rngLowEq, rngHighEq * 0.75f);

        timeoutTimer = Mathf.Min(maxTimeoutSeconds, Mathf.Max(minTimeoutSeconds, Boss.Enemy.Stats.CombatLevel * 0.8249123f));

        Boss.Enemy.Unlock();
    }
}

public enum RaidJoinResult
{
    CanJoin,
    MinHealthReached,
    AlreadyJoined,
    NoActiveRaid
}
