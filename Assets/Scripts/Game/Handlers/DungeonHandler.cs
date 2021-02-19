using System.Linq;
using UnityEngine;

public class DungeonHandler : MonoBehaviour
{
    [SerializeField] private DungeonManager dungeon;
    [SerializeField] private PlayerController player;

    private Vector3 previousPosition;
    private EnemyController enemyTarget;
    private PlayerController healTarget;

    public bool InDungeon { get; private set; }

    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!dungeon) dungeon = FindObjectOfType<DungeonManager>();
    }

    private void Update()
    {
        if (!InDungeon || !dungeon.Active)
            return;

        var room = dungeon.Dungeon.Room;
        var roomType = room.RoomType;

        if (roomType == DungeonRoomType.Start)
            return;

        if (player.TrainingHealing)
        {
            if (healTarget && healTarget.Stats.IsDead)
                healTarget = null;

            healTarget = dungeon.GetPlayers()
                .OrderByDescending(x => x.Stats.Health.Level - x.Stats.Health.CurrentValue)
                .FirstOrDefault();

            if (!healTarget)
                return;

            HealTarget();
            return;
        }

        if (enemyTarget && enemyTarget.Stats.IsDead)
            enemyTarget = null;

        if (roomType == DungeonRoomType.Room && !enemyTarget)
            enemyTarget = room.GetNextEnemyTarget(player);

        if (roomType == DungeonRoomType.Boss && !enemyTarget && room.Boss)
            enemyTarget = room.Boss.Enemy;

        if (!enemyTarget)
            return;

        AttackTarget();
    }

    public void OnEnter()
    {
        if (!dungeon || dungeon == null || dungeon.Dungeon == null || !dungeon.Dungeon)
            return;

        if (!player || player == null)
            return;

        var startingPoint = dungeon.Dungeon.StartingPoint;
        if (this.player.Ferry.OnFerry)
        {
            if (player.Ferry.Destination)
            {
                previousPosition = player.Ferry.Destination.SpawnPosition;
            }
            else
            {
                var chunk = player.Game.Chunks.GetStarterChunk();
                previousPosition = chunk.GetPlayerSpawnPoint();
            }
            player.transform.parent = null;
        }
        else
        {
            previousPosition = player.transform.position;
        }

        player.Teleporter.Teleport(startingPoint);
        player.Stats.Health.Reset();
        InDungeon = true;
    }

    public void OnExit()
    {
        if (!InDungeon)
            return;

        player.Teleporter.Teleport(previousPosition);
        InDungeon = false;
        enemyTarget = null;
        healTarget = null;
    }

    public void Died()
    {
        healTarget = null;
        enemyTarget = null;
        dungeon.PlayerDied(this.player);
        OnExit();
    }

    private void HealTarget()
    {
        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, healTarget.transform.position);
        if (distance <= range)
        {
            if (healTarget.Stats.IsDead)
            {
                healTarget = null;
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Lock();
                return;
            }
            
            if (player == null || !player || healTarget == null || !healTarget)
                return;

            player.Heal(healTarget);
        }
        else
        {
            player.GotoPosition(healTarget.transform.position);
        }
    }

    private void AttackTarget()
    {
        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, enemyTarget.transform.position);
        if (distance <= range)
        {
            if (enemyTarget.Stats.IsDead)
            {
                enemyTarget = null;
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Lock();
                return;
            }

            player.Attack(enemyTarget);
        }
        else
        {
            player.GotoPosition(enemyTarget.transform.position);
        }
    }
}