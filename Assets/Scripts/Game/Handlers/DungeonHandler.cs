using Shinobytes.Linq;
using System;
using Tessera;
using UnityEngine;

public class DungeonHandler : IAwayHandler
{

    private EnemyController enemyTarget;
    private PlayerController healTarget;

    private float waitForDungeon;

    // what the player left for the dungeon, and the way back (shared with the tower)
    private readonly WorldReturn back = new WorldReturn();

    private PlayerController player;
    private DungeonManager dungeon;

    public IslandController PreviousIsland => back.Island;
    public Vector3 PreviousPosition => back.Position;
    public FerryContext Ferry => back.Ferry;
    public bool InDungeon;

    public AwayPlace Place => AwayPlace.Dungeon;
    public bool IsAway => InDungeon;
    public WorldReturn Return => back;
    public int AutoJoinCounter { get; set; }
    public long AutoJoinCount { get; set; }
    //public int AutoJoinCost { get; set; } = 5000;

    public bool Joined => dungeon != null && dungeon.JoinedDungeon(this.player);

    //private void Start()
    //{
    //    if (!player) player = GetComponent<PlayerController>();
    //    if (!dungeon) dungeon = FindAnyObjectByType<DungeonManager>();
    //}

    public DungeonHandler(PlayerController player, DungeonManager dungeon)
    {
        this.player = player;
        this.dungeon = dungeon;
    }

    public void Update()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        if (waitForDungeon > 0f)
        {
            waitForDungeon -= GameTime.deltaTime;
            OnEnter();
            return;
        }

        if (!dungeon.Active)
        {
            return;
        }

        if (!dungeon.Started)
        {
            return;
        }

        if (!InDungeon)
        {
            return;
        }

        if (player.TrainingHealing)
        {
            if (!healTarget || healTarget.Stats.IsDead || healTarget.Id == player.Id || !healTarget.dungeonHandler.InDungeon)
                healTarget = null;

            if (!healTarget || healTarget == null || healTarget.Id == player.Id || healTarget.TrainingHealing || healTarget.Stats.HealthPercent > 0.9 || !healTarget.dungeonHandler.InDungeon)
            {
                var players = dungeon.GetAlivePlayers();
                var healthDif = 100f;
                var healthSmol = int.MaxValue;
                for (var i = 0; i < players.Count; ++i)
                {
                    var x = players[i];

                    if (x.Stats.Health.CurrentValue <= 0 || x.Id == player.Id || !x.dungeonHandler.InDungeon || Mathf.Abs(player.transform.position.y - x.transform.position.y) > 10)
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

        var room = dungeon.Dungeon.Room;
        var roomType = room.RoomType;
        if (roomType == DungeonRoomType.Start)
            return;

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
                    var distA = Vector3.Distance(possibleTarget.transform.position, player.Movement.Position);
                    var distB = Vector3.Distance(enemyTarget.transform.position, player.Movement.Position);
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
        back.Leave(player);

        player.InterruptAction();
        player.teleportHandler.Teleport(startingPoint);
        player.Stats.Health.Reset();

        this.player.Island = null;
        this.player.taskTarget = null;

        if (player.DungeonSkill != null)
        {
            this.player.SetTaskBySkillSilently(player.DungeonSkill.Value);
        }
    }

    public void OnExit()
    {
        if (!InDungeon || !player || !player.Movement)
            return;

        player.Movement.EnableLocalAvoidance();

        Clear();

        back.Back(player, "the dungeon");
    }

    public void Died()
    {
        dungeon.PlayerDied(this.player);
        OnExit();

        healTarget = null;
        enemyTarget = null;
        player.ClearTarget();
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
        var distance = Vector3.Distance(player.transform.position, healTarget.Position);
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
            if (!player.SetDestination(healTarget.Position))
            {
                player.Movement.DisableLocalAvoidance();
            }

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

        var targetEnemyPosition = enemyTarget._transform.position;

        var distance = Vector3.Distance(player._transform.position, targetEnemyPosition);
        if (distance <= range)
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            // Aggro more enemies that are close to the one being attacked if it doesnt have an attacker.

            var enemies = dungeon.GetEnemies();
            for (var i = 0; i < enemies.Count; ++i)
            {
                var enemy = enemies[i];
                if (enemy.Stats.IsDead || enemy.Attackers.Count > 0 || enemy == enemyTarget)
                {
                    continue;
                }

                if (Vector3.Distance(targetEnemyPosition, enemy._transform.position) > enemy.AggroRange)
                {
                    continue;
                }

                enemy.Attack(this.player);
            }


            player.Attack(enemyTarget);
        }
        else
        {
            if (!player.SetDestination(targetEnemyPosition))
            {
                player.Movement.DisableLocalAvoidance();

                // unreachable enemy in dungeon
                enemyTarget.IsUnreachable = true;
                enemyTarget.TakeDamage(player, enemyTarget.Stats.Health.MaxValue);
            }

            enemyTarget = null;
        }
    }

    /// <summary>
    /// Clears the dungeon state
    /// </summary>
    internal void Clear()
    {
        enemyTarget = null;
        healTarget = null;
        InDungeon = false;
        player.attackTarget = null;
        player.taskTarget = null;
    }

    public bool SetSkill(RavenNest.Models.Skill? value)
    {
        player.DungeonSkill = value;

        if (InDungeon && player.DungeonSkill != null)
        {
            this.player.SetTaskBySkillSilently(player.DungeonSkill.Value);
            return true;
        }

        return false;
    }
}

public struct FerryContext
{
    public bool OnFerry;
    public bool HasDestination;
    public bool HasReturned;
    public PlayerFerryState State;
    public IslandController Destination;
}