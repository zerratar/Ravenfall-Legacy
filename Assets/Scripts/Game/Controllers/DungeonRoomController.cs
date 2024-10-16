﻿using System;
using System.Collections.Generic;
using Shinobytes.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [SerializeField] private DungeonController dungeon;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform enemyContainer;

    public int Width = 1;
    public int Height = 1;

    public bool BossConnector;

    public DungeonRoomType RoomType;
    public DungeonRoomShape RoomShape;
    public DungeonRoomDirection RoomDirection;

    public float RoomRotation = 0;

    private DungeonRoomState state;
    private DungeonGateController gate;
    private EnemyController[] enemies;

    private float stuckCheckTime;
    private int stuckCheckAliveCount;
    private int stuckCheckAlivePlayerCount;
    private long stuckCheckEnemyHealth;
    private long stuckCheckPlayerHealth;

    public DungeonBossController Boss { get; set; }
    public bool InProgress => state == DungeonRoomState.InProgress;
    public bool IsCompleted => state == DungeonRoomState.Completed;
    public Transform CameraPoint => cameraPoint;
    public EnemyController[] Enemies => enemies;
    public Transform ExitPoint => exitPoint;

    [Button("Assign Room to spawn points")]
    private void FixMissingSpawnPointRoom()
    {
        var spawnPoints = GetComponentsInChildren<EnemySpawnPoint>();
        foreach (var spawnPoint in spawnPoints)
        {
            spawnPoint.Room = this;
        }
    }


    [Button("Assign Enemy Container")]
    private void AssignEnemyContainer()
    {
        this.enemyContainer = transform.Find("Enemies");
    }


    // Start is called before the first frame update
    void Start()
    {
        if (!cameraPoint)
            cameraPoint = transform.Find("CameraPoint");

        if (!dungeon)
            dungeon = GetComponentInParent<DungeonController>();

        if (!gate)
        {
            var eventContainer = transform.Find("Event Objects");
            if (eventContainer)
            {
                gate = eventContainer.GetComponentInChildren<DungeonGateController>();
            }
        }

        if (!enemyContainer) this.enemyContainer = transform.Find("Enemies");
        ReloadEnemies();
    }

    public Transform EnemyContainer => enemyContainer;
    public void ReloadEnemies()
    {
        if (!enemyContainer)
            return;

        enemies = enemyContainer.GetComponentsInChildren<EnemyController>();
    }

    internal void ResetRoom()
    {
        stuckCheckTime = 0;
        if (gate)
        {
            gate.Close();
        }

        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                enemy.Respawn(0.1f);
            }
        }
    }

    private void Update()
    {
        if (!InProgress)
            return;

        if (RoomType == DungeonRoomType.Room)
        {
            if (enemies.All(x => x.Stats.IsDead || x.IsUnreachable))
            {
                dungeon.NextRoom();
            }
            else
            {
                var aliveEnemyCount = 0;
                var totalEnemyHealth = 0L;
                foreach (var e in enemies)
                {
                    if (e.Stats.IsDead)
                    {
                        continue;
                    }
                    aliveEnemyCount++;
                    totalEnemyHealth += e.Stats.Health.CurrentValue;
                }

                var alivePlayerCount = dungeon.DungeonManager.GetAlivePlayerCount();
                var healCount = 0;
                var totalPlayerHealth = 0L;
                // if the alive player count is the same, check if those players are training healing or not
                if (alivePlayerCount == stuckCheckAlivePlayerCount)
                {
                    foreach (var plr in dungeon.DungeonManager.GetAlivePlayers())
                    {
                        if (plr.TrainingHealing)
                        {
                            healCount++;
                        }

                        totalPlayerHealth += plr.Stats.Health.CurrentValue;
                    }
                }

                if (totalPlayerHealth != stuckCheckPlayerHealth || healCount == alivePlayerCount || stuckCheckAliveCount != aliveEnemyCount || stuckCheckEnemyHealth != totalEnemyHealth || stuckCheckAlivePlayerCount != alivePlayerCount)
                {
                    stuckCheckTime = GameTime.time;
                    stuckCheckAliveCount = aliveEnemyCount;
                    stuckCheckAlivePlayerCount = alivePlayerCount;
                    stuckCheckEnemyHealth = totalEnemyHealth;
                    stuckCheckPlayerHealth = totalPlayerHealth;
                }
                else if (stuckCheckTime > 0 && GameTime.time - stuckCheckTime > 10f)
                {
                    // forcibly kill all enemies.
                    Shinobytes.Debug.LogWarning("[" + dungeon.Name + "] Dungeon room " + name + " is stuck. Forcibly killing all enemies to proceed to the next room.");

                    var players = dungeon.DungeonManager.GetPlayers();
                    foreach (var enemy in Enemies)
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
            }
        }

        if (RoomType == DungeonRoomType.Boss)
        {
            if (Boss && Boss.Enemy.Stats.IsDead)
            {
                Boss = null;
                dungeon.NextRoom();
            }
        }
    }

    public EnemyController GetNextEnemyTarget(PlayerController player)
    {
        if (enemies == null || enemies.Length == 0)
            return null;

        return enemies.GetNextEnemyTarget(player);
    }

    public void Enter()
    {
        state = DungeonRoomState.InProgress;

        if (gate && RoomType == DungeonRoomType.Start)
            gate.Open();
    }

    public void Exit()
    {
        state = DungeonRoomState.Completed;


        var alivePlayers = dungeon.DungeonManager.GetAlivePlayers();
        foreach (var player in alivePlayers)
        {
            player.ClearTarget();
        }


        if (gate)
            gate.Open();
    }
}

public static class TargetExtensions
{
    public static EnemyController GetNextEnemyTarget(this EnemyController[] enemies, PlayerController player, Func<EnemyController, bool> filter = null)
    {
        if (enemies == null || enemies.Length == 0)
            return null;

        // normally we would just get the next possible enemy ordered by the distance. However,
        // since healers are now attacked by enemies in dungeons.
        // We have to always focus on enemies that are attacking a healer.

        EnemyController target = null;
        float smallestDistance = float.MaxValue;

        for (var i = 0; i < enemies.Length; ++i)
        {
            var enemy = enemies[i];
            if (enemy.IsUnreachable || enemy.Stats.IsDead)
            {
                continue;
            }

            if (filter != null && !filter(enemy))
            {
                continue;
            }

            var distance = Vector3.Distance(enemy.Position, player.Movement.Position) + UnityEngine.Random.Range(0, 25f);

            // we gotta protect our healers first hand. so we add 20 distance to enemies not targeting healers
            if (!enemy.HasValidTarget || !enemy.TargetPlayer.TrainingHealing)
            {
                distance += 20;
            }
            if (distance < smallestDistance)
            {
                target = enemy;
                smallestDistance = distance;
            }
        }

        return target;
    }
}

public enum DungeonRoomShape
{
    Normal,
    L
}

public enum DungeonRoomDirection
{
    RightToLeft,
    RightToTop,
    RightToBottom,

    BottomToLeft,
    BottomToTop,
    BottomToRight,

    LeftToRight,
    LeftToTop,
    LeftToBottom,

    TopToLeft,
    TopToRight,
    TopToBottom
}

public enum DungeonRoomState
{
    None,
    InProgress,
    Completed
}

public enum DungeonRoomType
{
    Start,
    Room,
    Boss
}
