using RavenNest.Models;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BotPlayerController : MonoBehaviour
{
    private GameManager gameManager;
    public PlayerController playerController;
    private bool initialized;
    private bool joiningEvent;
    internal float LastTeleport;

    private void Start()
    {
        if (!this.gameManager) this.gameManager = FindAnyObjectByType<GameManager>();
        if (!this.playerController) this.playerController = GetComponent<PlayerController>();
        initialized = true;
    }

    public void Poll()
    {
        if (!initialized)
        {
            return;
        }

        if (Overlay.IsOverlay)
        {
            return;
        }

        if (joiningEvent)
        {
            return;
        }

        if (playerController.raidHandler.InRaid || playerController.dungeonHandler.InDungeon)
        {
            return;
        }

        var player = playerController;
        // if we are stuck, try and resolve the issue
        if (player.IsStuck)
        {
            // make sure we only call this once every 5 seconds.
            if (player.Unstuck(true, 5f))
            {
                Shinobytes.Debug.LogWarning("Unstuck attempt for " + player.Name + ".");
                player.Movement.AdjustPlayerPositionToNavmesh();
                player.IsStuck = false;
            }

            return;
        }

        if (gameManager.Raid.Started && gameManager.Raid.CanJoin(player) == RaidJoinResult.CanJoin)
        {
            joiningEvent = true;
            StartCoroutine(JoinRaid());
        }

        if (gameManager.Dungeons.Active)
        {
            var result = gameManager.Dungeons.CanJoin(player);
            if (result == DungeonJoinResult.CanJoin)
            {
                joiningEvent = true;
                StartCoroutine(JoinDungeon());
            }
        }

        // try identifying issues in game using the bot player controller
        // first off here we want to check if our currently trained skill can be trained properly.
        // if not, we have to log it and report so that I can fix it.
        // since we are not in a raid nor a dungeon, unless we are on the ferry we must be on an island.
        if (player.ferryHandler.OnFerry)
        {
            return;
        }

        if (!player.Island)
        {
            Shinobytes.Debug.LogWarning(player.Name + " is not on an island! Please check scene view to see where this poor guy is.");
            player.IsStuck = true;
            // record position, target, island, any details here, record the amount of bots effected, include current task and target
            return;
        }

        var now = Time.time;
        if (now - LastTeleport < 5)
        {
            // dont report if we just got teleported here since current paths does not get flushed out upon teleport.
            return;
        }

        var movement = player.Movement;
        if (movement.HasIncompletePath)
        {
            // if the path is partial, can we check if last point is close enough to do the task or not.
            var distanceToTarget = 9999f;
            if (movement.PathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial && player.Target && movement.CurrentPath != null)
            {
                var targetPos = player.Target.position;
                var lastCorner = movement.CurrentPath.corners[^1];

                distanceToTarget = Vector3.Distance(lastCorner, targetPos);
            }

            if (distanceToTarget > 2) // distance to target needs to be based on actual target.
            {
                Shinobytes.Debug.LogWarning(player.Name + ", island: " + player.Island.Identifier +
                    ", has incomplete path: " + movement.PathStatus + ", current task: " + player.ActiveSkill);// + ", distance: " + distanceToTarget);
                player.IsStuck = true;
                // record position, target, island, any details here, only one record per target is necessary.
                return;
            }
        }

        // alright, we are on an island, lets check if we are stuck or not.
        // of course, if we are resting this should be ignored.
        if (player.TimeSinceLastTaskChange > 1f // if we recently changed task, this will spam, so make sure its been more than a second since we changed task.
            && player.ferryHandler.State != PlayerFerryState.Embarking
            && !player.onsenHandler.InOnsen
            && (movement.IdleTime >= ExpectedMaxIdleTime(player.ActiveSkill) || (movement.IdleTime >= 30 && (GameTime.time - player.LastExecutedTaskTime) >= 30))
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
                Shinobytes.Debug.LogWarning(player.Name + ", island: " + player.Island.Identifier +
                    ", has been idling for much longer than expected for current task: " + player.ActiveSkill);
                player.IsStuck = true;
                // record position, target, island, any details here, only one record per target is necessary, but we can update amount of unique bots triggered out of bots using same skill.
                return;
            }
        }

        player.IsStuck = false;
        // after recording details, store it for later, let bots run wild for 5-10 minutes then teleport them to next island, and continue recording issues until all islands been tested.
        // then generate a report
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
            return 60f;

        if (skill == RavenNest.Models.Skill.Magic ||
            skill == RavenNest.Models.Skill.Ranged)
            return 180f;

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

    private IEnumerator JoinDungeon()
    {
        yield return new WaitForSeconds(0.5f + UnityEngine.Random.value);
        if (!playerController) playerController = GetComponent<PlayerController>();
        gameManager.Dungeons.Join(playerController);
        joiningEvent = false;
    }
    private IEnumerator JoinRaid()
    {
        yield return new WaitForSeconds(0.5f + UnityEngine.Random.value);
        if (!playerController) playerController = GetComponent<PlayerController>();
        gameManager.Raid.Join(playerController);
        joiningEvent = false;
    }

}
