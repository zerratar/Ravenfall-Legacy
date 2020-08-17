using UnityEngine;

public class DungeonHandler : MonoBehaviour
{
    [SerializeField] private DungeonManager dungeon;
    [SerializeField] private PlayerController player;

    private Vector3 previousPosition;
    private EnemyController enemyTarget;

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
        var startingPoint = dungeon.Dungeon.StartingPoint;
        previousPosition = player.transform.position;
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
    }

    public void Died()
    {
        InDungeon = false;
        enemyTarget = null;
        dungeon.PlayerDied(this.player);
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