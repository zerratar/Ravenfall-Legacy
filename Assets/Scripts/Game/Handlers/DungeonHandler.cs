using Shinobytes.Linq;
using UnityEngine;

public class DungeonHandler : MonoBehaviour
{
    [SerializeField] private DungeonManager dungeon;
    [SerializeField] private PlayerController player;


    private EnemyController enemyTarget;
    private PlayerController healTarget;
    private string[] previousTaskArgs;
    private float waitForDungeon;

    private Vector3 previousPosition;
    private IslandController previousIsland;

    public bool ReturnedToOnsen;

    public FerryState Ferry;

    private bool wasResting;

    public IslandController PreviousIsland => previousIsland;
    public Vector3 PreviousPosition => previousPosition;
    public bool InDungeon { get; private set; }

    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!dungeon) dungeon = FindObjectOfType<DungeonManager>();
    }

    private void Update()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        if (waitForDungeon > 0f)
        {
            waitForDungeon -= Time.deltaTime;
            OnEnter();
            return;
        }

        if (!InDungeon || !dungeon.Active)
            return;

        if (player.TrainingHealing)
        {
            if (!healTarget || healTarget.Stats.IsDead || healTarget.Id == player.Id || !healTarget.Dungeon.InDungeon)
                healTarget = null;

            if (!healTarget || healTarget == null || healTarget.Id == player.Id || healTarget.TrainingHealing || healTarget.Stats.HealthPercent > 0.9 || !healTarget.Dungeon.InDungeon)
            {
                var players = dungeon.GetPlayers();
                var healthDif = 100f;
                var healthSmol = int.MaxValue;
                for (var i = 0; i < players.Count; ++i)
                {
                    var x = players[i];

                    if (x.Stats.Health.CurrentValue <= 0 || x.Id == player.Id || !x.Dungeon.InDungeon)
                    {
                        continue;
                    }

                    var hp = x.Stats.HealthPercent;
                    if (hp < healthDif || (x.Stats.Health.CurrentValue < healthSmol && hp < 1))
                    {
                        healthSmol = x.Stats.Health.CurrentValue;
                        healthDif = hp;
                        healTarget = x;
                    }
                }

                if (!healTarget)
                {
                    healTarget = this.player;
                }
            }

            if (!healTarget || healTarget.Stats.IsDead)
            {
                healTarget = null;
                return;
            }

            HealTarget();
            return;
        }

        if (enemyTarget && enemyTarget.Stats.IsDead)
            enemyTarget = null;

        if (dungeon.Dungeon.HasPredefinedRooms)
        {
            var room = dungeon.Dungeon.Room;
            var roomType = room.RoomType;
            if (roomType == DungeonRoomType.Start)
                return;
        }

        if (!enemyTarget)
        {
            var possibleTarget = dungeon.GetNextEnemyTarget(player);

            if (possibleTarget)
            {
                if (!enemyTarget)
                {
                    enemyTarget = possibleTarget;
                }
                else if (possibleTarget != enemyTarget)
                {
                    var distA = Vector3.Distance(possibleTarget.PositionInternal, player.Movement.Position);
                    var distB = Vector3.Distance(enemyTarget.PositionInternal, player.Movement.Position);
                    if (distA < distB)
                    {
                        enemyTarget = possibleTarget;
                    }
                }
            }
        }

        if (!enemyTarget)
            return;

        AttackTarget();
    }

    public void OnEnter()
    {        
        ReturnedToOnsen = false;

        enemyTarget = null;
        healTarget = null;

        if (!dungeon || dungeon == null || dungeon.Dungeon == null || !dungeon.Dungeon)
        {
            if (waitForDungeon > 0)
            {
                return;
            }

            waitForDungeon = 3f;
            return;
        }

        if (!player || player == null)
        {
            // Player left the game. Ignore.
            return;
        }

        waitForDungeon = 0;
        InDungeon = true;
        var startingPoint = dungeon.StartingPoint;

        this.previousTaskArgs = this.player.GetTaskArguments().ToArray();

        Ferry = new()
        {
            OnFerry = player.Ferry.OnFerry,
            HasDestination = !!player.Ferry.Destination
        };

        if (Ferry.OnFerry)
        {
            //var wasCaptainOfFerry = this.player.Ferry.IsCaptain;
            //if (player.Ferry.Destination)
            //{
            //    previousPosition = player.Ferry.Destination.SpawnPosition;
            //}
            //else
            //{
            //    var chunk = player.GameManager.Chunks.GetStarterChunk();
            //    previousPosition = chunk.GetPlayerSpawnPoint();
            //}

            player.Ferry.RemoveFromFerry();
        }
        else
        {
            previousPosition = player.Position;
        }

        wasResting = this.player.Onsen.InOnsen;

        if (player.Onsen.InOnsen)
        {
            //previousPosition = player.Game.Onsen.
            previousPosition = player.Onsen.EntryPoint;
            player.GameManager.Onsen.Leave(player);
            player.transform.parent = null;
        }

        previousIsland = this.player.Island;

        player.Teleporter.Teleport(startingPoint);
        player.Stats.Health.Reset();

        this.player.Island = null;
        this.player.taskTarget = null;
    }

    public void OnExit()
    {
        if (!InDungeon)
            return;

        enemyTarget = null;
        healTarget = null;

        InDungeon = false;

        if (Ferry.OnFerry)
        {
            player.Movement.Lock();
            player.Ferry.AddPlayerToFerry();
            Ferry.HasReturned = true;
        }
        else
        {
            player.Teleporter.Teleport(previousPosition);
        }

        if (wasResting)
        {
            player.GameManager.Onsen.Join(player);
            ReturnedToOnsen = true;
        }

        player.taskTarget = null;
        player.attackTarget = null;

        if (previousTaskArgs != null && previousTaskArgs.Length > 0)
        {
            this.player.SetTaskArguments(previousTaskArgs);
        }

        var currentTask = player.GetTask();
        if (!Ferry.OnFerry && currentTask != TaskType.None)
        {
            player.GotoClosest(currentTask);
        }

        wasResting = false;
    }

    public void Died()
    {
        healTarget = null;
        enemyTarget = null;
        dungeon.PlayerDied(this.player);
        OnExit();
    }

    public Vector3 SpawnPosition
    {
        get
        {
            return this.dungeon.StartingPoint;
        }
    }
    private void HealTarget()
    {
        if (!healTarget)
        {
            healTarget = null;
            return;
        }

        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, healTarget.Position);
        if (distance <= range)
        {
            if (healTarget.Stats.IsDead)
            {
                healTarget = null;
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            if (player == null || !player || healTarget == null || !healTarget)
                return;

            player.Heal(healTarget);
        }
        else
        {
            player.SetDestination(healTarget.Position);
            healTarget = null;
        }
    }

    private void AttackTarget()
    {
        var range = player.GetAttackRange();
        if (!enemyTarget || enemyTarget.Stats.IsDead)
        {
            enemyTarget = null;
            return;
        }

        var distance = Vector3.Distance(transform.position, enemyTarget.transform.position);
        if (distance <= range)
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            // Aggro more enemies that are close to the one being attacked if it doesnt have an attacker.
            //if (!dungeon.Dungeon.HasPredefinedRooms)
            {
                var enemies = dungeon.GetEnemiesNear(enemyTarget.transform.position);
                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy.Attackers.Count > 0)
                        {
                            continue;
                        }

                        enemy.Attack(this.player);
                    }
                }
            }

            player.Attack(enemyTarget);
        }
        else
        {
            player.SetDestination(enemyTarget.transform.position);

            enemyTarget = null;
        }
    }


    public struct FerryState
    {
        public bool OnFerry;
        public bool HasDestination;
        public bool HasReturned;
    }

}