﻿using Shinobytes.Linq;
using System;
using UnityEngine;

public class DungeonHandler
{

    private EnemyController enemyTarget;
    private PlayerController healTarget;

    private TaskType previousTask;
    private string previousTaskArgument;

    private float waitForDungeon;

    private Vector3 previousPosition;
    private IslandController previousIsland;

    public bool ReturnedToOnsen;

    public FerryState Ferry;

    private bool wasResting;
    private float autoJoinRequestTimeout;

    public bool AutoJoining;

    private PlayerController player;
    private DungeonManager dungeon;

    public IslandController PreviousIsland => previousIsland;
    public Vector3 PreviousPosition => previousPosition;
    public bool InDungeon { get; private set; }
    public int AutoJoinCounter { get; set; }
    public bool Joined => dungeon != null && dungeon.JoinedDungeon(this.player);

    //private void Start()
    //{
    //    if (!player) player = GetComponent<PlayerController>();
    //    if (!dungeon) dungeon = FindObjectOfType<DungeonManager>();
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

        if (AutoJoining)
        {
            return;
        }

        if (!InDungeon && !dungeon.Started)
        {
            if (AutoJoinCounter > 0)
            {
                // try join the dungeon if possible, and dont auto join if server is not responding.
                AutoJoining = true;
                RequestAutoJoinAsync();
            }
            else
            {
                return;
            }
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

        AutoJoining = false;

        if (player.TrainingHealing)
        {
            if (!healTarget || healTarget.Stats.IsDead || healTarget.Id == player.Id || !healTarget.Dungeon.InDungeon)
                healTarget = null;

            if (!healTarget || healTarget == null || healTarget.Id == player.Id || healTarget.TrainingHealing || healTarget.Stats.HealthPercent > 0.9 || !healTarget.Dungeon.InDungeon)
            {
                var players = dungeon.GetAlivePlayers();//GetPlayers();
                var healthDif = 100f;
                var healthSmol = int.MaxValue;
                for (var i = 0; i < players.Count; ++i)
                {
                    var x = players[i];

                    if (x.Stats.Health.CurrentValue <= 0 || x.Id == player.Id || !x.Dungeon.InDungeon || Mathf.Abs(player.transform.position.y - x.transform.position.y) > 10)
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

    private async void RequestAutoJoinAsync()
    {
        if (AutoJoining)
        {
            return;
        }

        try
        {
            if (autoJoinRequestTimeout > 0)
            {
                autoJoinRequestTimeout -= Time.deltaTime;
                return;
            }

            if (AutoJoinCounter == 0 || player.GameManager.Dungeons.CanJoin(player) != DungeonJoinResult.CanJoin)
                return;

            var result = await player.GameManager.RavenNest.Players.AutoJoinDungeon(player.Id);
            if (result)
            {
                player.GameManager.Dungeons.Join(player);
                if (AutoJoinCounter != int.MaxValue)
                {
                    AutoJoinCounter--;
                }

                player.GameManager.OnPlayerAutoJoinedDungeon(player);

                //if (AutoJoinCounter > 0 && AutoJoinCounter != int.MaxValue)
                //{
                //    player.GameManager.RavenBot.SendReply(player, "You've automatically joined the dungeon. You will join {autoJoinLeft} more.", AutoJoinCounter);
                //}
                //else if (AutoJoinCounter == int.MaxValue)
                //{
                //    player.GameManager.RavenBot.SendReply(player, "You've automatically joined the dungeon.");
                //}
                //else
                //{
                //    player.GameManager.RavenBot.SendReply(player, "You've automatically joined the dungeon. You will no longer automatically join any dungeon.");
                //}
            }
            else
            {
                AutoJoinCounter = 0;
                player.GameManager.RavenBot.SendReply(player, "You've failed to automatically joined the Dungeon. You either do not have enough coins or server did not respond.");
            }
        }
        catch
        {
            // do not cancel auto-join but we don't want to retry again too quickly asking the server. Set a cooldown
            autoJoinRequestTimeout = 2f;
        }
        finally
        {
            AutoJoining = false;
        }
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
        this.previousTask = this.player.GetTask();
        this.previousTaskArgument = this.player.GetTaskArgument();

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

        player.InterruptAction();
        player.Teleporter.Teleport(startingPoint);
        player.Stats.Health.Reset();

        this.player.Island = null;
        this.player.taskTarget = null;
    }

    public void OnExit()
    {
        if (!InDungeon)
            return;

        Clear();

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


        player.taskTarget = null;
        player.attackTarget = null;

        if (previousTask != TaskType.None)
        {
            this.player.SetTask(previousTask, previousTaskArgument);
        }

        var currentTask = player.GetTask();
        if (!Ferry.OnFerry && currentTask != TaskType.None)
        {
            player.GotoClosest(currentTask);
        }

        if (wasResting)
        {
            player.GameManager.Onsen.Join(player);
            ReturnedToOnsen = true;
        }

        wasResting = false;
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

        var distance = Vector3.Distance(player.transform.position, enemyTarget.transform.position);
        if (distance <= range)
        {
            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            // Aggro more enemies that are close to the one being attacked if it doesnt have an attacker.
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

            player.Attack(enemyTarget);
        }
        else
        {
            player.SetDestination(enemyTarget.transform.position);

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

    public struct FerryState
    {
        public bool OnFerry;
        public bool HasDestination;
        public bool HasReturned;
    }

}