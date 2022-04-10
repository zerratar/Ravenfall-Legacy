using System;
using System.Collections.Generic;
using Shinobytes.Linq;
using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [SerializeField] private DungeonController dungeon;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private Transform exitPoint;

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

    public DungeonBossController Boss { get; set; }
    public bool InProgress => state == DungeonRoomState.InProgress;
    public bool IsCompleted => state == DungeonRoomState.Completed;
    public Transform CameraPoint => cameraPoint;

    public Transform ExitPoint => exitPoint;

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

        var enemyContainer = transform.Find("Enemies");
        if (!enemyContainer)
            return;

        enemies = enemyContainer.GetComponentsInChildren<EnemyController>();
    }

    internal void ResetRoom()
    {
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
            if (enemies.All(x => x.Stats.IsDead))
            {
                dungeon.NextRoom();
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

        if (gate)
            gate.Open();
    }
}

public static class TargetExtensions
{

    public static EnemyController GetNextEnemyTargetExceptBoss(this IReadOnlyList<EnemyController> enemies, PlayerController player)
    {
        if (enemies == null || enemies.Count == 0)
            return null;

        var maxDist = float.MaxValue;
        EnemyController target = null;
        for (var i = 0; i < enemies.Count; ++i)
        {
            var x = enemies[i];
            if (x.Stats.Health.CurrentValue <= 0)
            {
                continue;
            }

            var dist = Vector3.Distance(x.PositionInternal, player.PositionInternal);
            if (dist < maxDist)
            {
                maxDist = dist;
                target = x;
            }
        }

        return target;
    }

    public static EnemyController GetNextEnemyTargetExceptBoss(this EnemyController[] enemies, PlayerController player)
    {
        if (enemies == null || enemies.Length == 0)
            return null;

        var maxDist = float.MaxValue;
        EnemyController target = null;
        for (var i = 0; i < enemies.Length; ++i)
        {
            var x = enemies[i];
            if (x.Stats.Health.CurrentValue <= 0)
            {
                continue;
            }

            var dist = Vector3.Distance(x.PositionInternal, player.PositionInternal);
            if (dist < maxDist)
            {
                maxDist = dist;
                target = x;
            }
        }

        return target;
    }
    public static EnemyController GetNextEnemyTarget(this EnemyController[] enemies, PlayerController player, Func<EnemyController, bool> filter = null)
    {
        if (enemies == null || enemies.Length == 0)
            return null;

        return
            enemies
            .Where(x => !x.Stats.IsDead && (filter == null || filter(x)))
            //.OrderBy(x => x.GetAttackerCountExcluding(player))
            .OrderBy(x => Vector3.Distance(x.PositionInternal, player.PositionInternal))
            .FirstOrDefault();
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
