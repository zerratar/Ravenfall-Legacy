
using System.Runtime.CompilerServices;

/// <summary>
/// Class for checking whether or not a player is in a valid state.
/// if not it will report the issue and try to fix it. 
/// This is used an additional measurement to avoid players getting stuck in certain scenarios.
/// </summary>
public class PlayerValidator
{
    /// <summary>
    ///     Validates the player and tries to fix its state if its not.
    /// </summary>
    /// <param name="player"></param>
    /// <returns>true if the player is in a valid state otherwise false.</returns>
    public bool Validate(PlayerController player)
    {
        try
        {
            if (player == null || !player || player.isDestroyed || player.Removed)
            {
                return false;
            }

            // ignore bot for now as they have their own validation in the BotPlayerController.cs
            // todo: move the validation logic from BotPlayerController.cs to here.
            if (player.IsBot)
            {
                return true;
            }

            // ignore if we are in a raid or dungeon for now.
            if (player.raidHandler.InRaid || player.dungeonHandler.InDungeon)
            {
                return true;
            }

            // first off here we want to check if our currently trained skill can be trained properly.
            // if not, we have to log it and report so that I can fix it.
            // since we are not in a raid nor a dungeon, unless we are on the ferry we must be on an island.
            if (player.ferryHandler.OnFerry)
            {
                return true;
            }

            // if we are stuck, try and resolve the issue
            if (player.IsStuck)
            {
                // make sure we only call this once every 5 seconds.
                if (player.Unstuck(true, 5f))
                {
#if DEBUG
                    Shinobytes.Debug.LogWarning("Unstuck attempt for " + player.Name + ".");
#endif
                    player.Movement.AdjustPlayerPositionToNavmesh();
                    player.IsStuck = false;
                }

                return false;
            }

            if (player.Island == null || !player.Island)
            {
#if DEBUG
                Shinobytes.Debug.LogWarning(player.Name + " is not on an island! Please check scene view to see where this poor guy is.");
#endif
                player.IsStuck = true;
                // record position, target, island, any details here, record the amount of bots effected, include current task and target
                return false;
            }

            var now = GameTime.time;
            if (now - player.teleportHandler.LastTeleport < 5)
            {
                // dont report if we just got teleported here since current paths does not get flushed out upon teleport.
                return true;
            }

            // TODO: if we have a task, and we are on an island that will let us gain exp
            // check if we gained any exp the past 30s, if not then report!

            if (player.ActiveSkill != RavenNest.Models.Skill.None)
            {
                // ...
            }

            // check if the player is stuck in a path.
            // if the current path is incomplete:
            var movement = player.Movement;
            if (movement.HasIncompletePath)
            {
                // if the path is partial, can we check if last point is close enough to do the task or not.
                var distanceToTarget = 9999f;
                if (movement.PathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial && player.Target && movement.CurrentPath != null)
                {
                    var targetPos = player.Target.position;
                    var lastCorner = movement.CurrentPath.corners[^1];

                    distanceToTarget = UnityEngine.Vector3.Distance(lastCorner, targetPos);
                }

                if (distanceToTarget > 2) // distance to target needs to be based on actual target.
                {
#if DEBUG
                    Shinobytes.Debug.LogWarning(player.Name + ", island: " + player.Island.Identifier +
                        ", has incomplete path: " + movement.PathStatus + ", current task: " + player.ActiveSkill + ", target: " + player.Target.name);// + ", distance: " + distanceToTarget);
                                                                                                                                                       // can be target being stuck too.
#endif
                    // Try unstucking the target as well if its an EnemyController

                    var enemyController = player.Target.GetComponent<EnemyController>();
                    if (enemyController)
                    {
                        enemyController.Unstuck();
                    }

                    player.IsStuck = true;
                    // record position, target, island, any details here, only one record per target is necessary.
                    return false;
                }
            }

            // if we don't have a path or incomplete path, but been idling longer than expected
            // also, ignore if the player is currently resting.

            var timeSinceLastExecutedTask = GameTime.time - player.LastExecutedTaskTime;
            if (player.TimeSinceLastTaskChange > 1f // if we recently changed task, this will spam, so make sure its been more than a second since we changed task.
                && !player.onsenHandler.InOnsen // we will stand still if we are in the onsen :)
                && player.ferryHandler.State != PlayerFerryState.Embarking // if we are embarking the ferry we will be standing still.
                && ((movement.IdleTime >= ExpectedMaxIdleTime(player.ActiveSkill) && timeSinceLastExecutedTask >= 5) ||
                    (movement.IdleTime >= 15 && timeSinceLastExecutedTask >= 15))
                && player.Target)
            {
                // if we are training Ranged or Magic, we could potentially be standing for a long time
                // so we also have to check "last activation" time of either magic or ranged skill
                var ignored = false;
                if (player.ActiveSkill == RavenNest.Models.Skill.Magic || player.ActiveSkill == RavenNest.Models.Skill.Ranged)
                {
                    ignored = now - player.Animations.LastTrigger < 10;
                }

                if (!ignored)
                {
#if DEBUG
                    Shinobytes.Debug.LogWarning(player.Name + ", island: " + player.Island.Identifier + ", has been idling for much longer than expected for current task: " + player.ActiveSkill + ", idle: " + movement.IdleTime + ", time since last task: " + timeSinceLastExecutedTask);
#endif
                    player.IsStuck = true;
                    // record position, target, island, any details here, only one record per target is necessary, but we can update amount of unique bots triggered out of bots using same skill.
                    return false;
                }
            }

            player.IsStuck = false;

            return true;
        }
        catch
        {
            return false;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ExpectedMaxIdleTime(RavenNest.Models.Skill skill)
    {
        if (IsSkillExpectedToStandStill(skill))
            return float.MaxValue;

        if (skill == RavenNest.Models.Skill.Attack ||
            skill == RavenNest.Models.Skill.Defense ||
            skill == RavenNest.Models.Skill.Strength ||
            skill == RavenNest.Models.Skill.Health)
            return 45f;

        if (skill == RavenNest.Models.Skill.Magic ||
            skill == RavenNest.Models.Skill.Ranged)
            return 120f;

        if (skill == RavenNest.Models.Skill.Woodcutting)
            return 20f;

        if (skill == RavenNest.Models.Skill.Gathering)
            return 10f;

        return 5f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSkillExpectedToStandStill(RavenNest.Models.Skill skill)
    {
        return skill == RavenNest.Models.Skill.None || skill == RavenNest.Models.Skill.Alchemy
            || skill == RavenNest.Models.Skill.Cooking || skill == RavenNest.Models.Skill.Crafting
            || skill == RavenNest.Models.Skill.Fishing || skill == RavenNest.Models.Skill.Farming
            || skill == RavenNest.Models.Skill.Mining || skill == RavenNest.Models.Skill.Healing;
    }
}