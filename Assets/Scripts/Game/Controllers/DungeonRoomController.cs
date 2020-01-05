using System;
using System.Linq;
using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [SerializeField] private DungeonController dungeon;
    [SerializeField] private Transform cameraPoint;

    public DungeonRoomType RoomType;

    private DungeonRoomState state;
    private DungeonGateController gate;
    private EnemyController[] enemies;

    public DungeonBossController Boss { get; set; }
    public bool InProgress => state == DungeonRoomState.InProgress;
    public bool IsCompleted => state == DungeonRoomState.Completed;
    public Transform CameraPoint => cameraPoint;
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
                enemy.Respawn();
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
        return enemies
            .Where(x => !x.Stats.IsDead)
            .OrderBy(x => x.Attackers.Count)
            .ThenBy(x => Vector3.Distance(x.transform.position, player.transform.position))
            .FirstOrDefault();
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
