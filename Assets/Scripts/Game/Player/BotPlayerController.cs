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

        // if we are stuck, try and resolve the issue
        if (playerController.IsStuck)
        {
            Shinobytes.Debug.LogWarning(playerController.Name + " is stuck. Trying to unstuck.");
            // make sure we only call this once every 5 seconds.
            if (playerController.Unstuck(true, 5f))
            {
                playerController.Movement.AdjustPlayerPositionToNavmesh();
                playerController.IsStuck = false;
            }

            return;
        }

        if (gameManager.Raid.Started && gameManager.Raid.CanJoin(playerController) == RaidJoinResult.CanJoin)
        {
            joiningEvent = true;
            StartCoroutine(JoinRaid());
        }

        if (gameManager.Dungeons.Active)
        {
            var result = gameManager.Dungeons.CanJoin(playerController);
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
        if (playerController.ferryHandler.OnFerry)
        {
            return;
        }

        if (!playerController.Island)
        {
            Shinobytes.Debug.LogWarning(playerController.Name + " is not on an island! Please check scene view to see where this poor guy is.");
            playerController.IsStuck = true;
            // record position, target, island, any details here, record the amount of bots effected, include current task and target
            return;
        }

        var now = Time.time;
        if (now - LastTeleport < 3)
        {
            // dont report if we just got teleported here since current paths does not get flushed out upon teleport.
            return;
        }

        // TODO: if we have a task, and we are on an island that will let us gain exp
        // check if we gained any exp the past 30s, if not then report!

        if (playerController.ActiveSkill != RavenNest.Models.Skill.None)
        {
            // ...
        }

        var movement = playerController.Movement;
        if (movement.HasIncompletePath)
        {
            // if the path is partial, can we check if last point is close enough to do the task or not.
            var distanceToTarget = 9999f;
            if (movement.PathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial && playerController.Target && movement.CurrentPath != null)
            {
                var targetPos = playerController.Target.position;
                var lastCorner = movement.CurrentPath.corners[^1];

                distanceToTarget = Vector3.Distance(lastCorner, targetPos);
            }

            if (distanceToTarget > 2) // distance to target needs to be based on actual target.
            {
                Shinobytes.Debug.LogWarning(playerController.Name + ", island: " + playerController.Island.Identifier + ", has incomplete path: " + movement.PathStatus + ", current task: " + playerController.ActiveSkill);// + ", distance: " + distanceToTarget);
                playerController.IsStuck = true;
                // record position, target, island, any details here, only one record per target is necessary.
                return;
            }
        }

        // alright, we are on an island, lets check if we are stuck or not.
        // of course, if we are resting this should be ignored.
        if (playerController.TimeSinceLastTaskChange > 1f // if we recently changed task, this will spam, so make sure its been more than a second since we changed task.
            && playerController.ferryHandler.State != PlayerFerryState.Embarking
            && !playerController.onsenHandler.InOnsen            
            && (movement.IdleTime >= ExpectedMaxIdleTime(playerController.ActiveSkill) || (movement.IdleTime >= 15 && (GameTime.time - playerController.LastExecutedTaskTime) >= 15))
            && playerController.Target)
        {

            // if we are training Ranged or Magic, we could potentially be standing for a long time
            // so we also have to check "last activation" time of either magic or ranged skill
            var ignored = false;
            if (playerController.ActiveSkill == RavenNest.Models.Skill.Magic || playerController.ActiveSkill == RavenNest.Models.Skill.Ranged)
            {
                ignored = now - playerController.Animations.LastTrigger < 3;
            }

            if (!ignored)
            {
                Shinobytes.Debug.LogWarning(playerController.Name + ", island: " + playerController.Island.Identifier + ", has been idling for much longer than expected for current task: " + playerController.ActiveSkill); //+ ", idleTime: " + movement.IdleTime);
                playerController.IsStuck = true;
                // record position, target, island, any details here, only one record per target is necessary, but we can update amount of unique bots triggered out of bots using same skill.
                return;
            }
        }

        playerController.IsStuck = false;
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
