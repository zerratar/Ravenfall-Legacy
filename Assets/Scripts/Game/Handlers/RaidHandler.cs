using System;
using System.Collections;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Skill = RavenNest.Models.Skill;

public class RaidHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;

    public DateTime RaidEnterTime;

    private bool teleported;
    private IslandController previousIsland;
    private Vector3 prevPosition;
    private string[] prevTaskArgs;
    private bool wasResting;

    private FerryContext ferryState;
    private TaskType previousTask;
    private string previousTaskArgument;

    private Transform _transform;

    public bool InRaid { get; private set; }
    public IslandController PreviousIsland => previousIsland;
    public Vector3 PreviousPosition => prevPosition;

    public int AutoJoinCounter { get; set; }
    public long AutoJoinCount { get; set; }
    //public double AutoJoinCost { get; set; } = 3000;

    private bool started;
    // Start is called before the first frame update
    void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        started = true;
    }

    void Awake()
    {
        this._transform = this.transform;
    }

    // Update is called once per frame
    public void Poll()
    {
        if (!started)
        {
            return;
        }

        if (Overlay.IsOverlay || !gameManager.Raid.Started || !gameManager.Raid.Boss)
        {
            return;
        }

        if (!InRaid)
        {
            return;
        }

        var target = gameManager.Raid.Boss._transform;
        PlayerController healTarget = null;
        if (player.TrainingHealing)
        {
            var raiders = gameManager.Raid.Raiders;
            if (raiders != null)
            {
                healTarget = raiders
                    .Where(x => x != null && x.Stats != null && !x.Stats.IsDead && x.raidHandler.InRaid)
                    .OrderByDescending(x =>
                        x.Stats.Health.MaxLevel - x.Stats.Health.CurrentValue)
                    .FirstOrDefault();

                if (healTarget && healTarget != null)
                {
                    target = healTarget.Transform;
                }
                else
                {
                    healTarget = this.player;
                    target = this._transform;
                }
            }
        }

        if (!target)
        {
            return;
        }

        var range = player.GetAttackRange();
        var distance = Vector3.Distance(_transform.position, target.position);
        if (distance <= range)
        {
            if (!player.TrainingHealing && gameManager.Raid.Boss.Enemy.Stats.IsDead)
            {
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            if (player.TrainingHealing)
            {
                player.Heal(healTarget);
            }
            else
            {
                player.Attack(gameManager.Raid.Boss.Enemy);
            }
        }
        else
        {
            if (!player.SetDestination(target.position))
            {
                player.Movement.DisableLocalAvoidance();
            }
        }
    }

    public void OnEnter()
    {
        InRaid = true;
        RaidEnterTime = DateTime.Now;
        //#if DEBUG
        //        Shinobytes.Debug.Log($"{player.PlayerName} entered the raid");
        //#endif

        wasResting = player.onsenHandler.InOnsen;

        ferryState = new()
        {
            OnFerry = player.ferryHandler.OnFerry,
            State = player.ferryHandler.State,
            HasDestination = !!player.ferryHandler.Destination,
            Destination = player.ferryHandler.Destination
        };

        if (wasResting)
        {
            player.onsenHandler.Exit();
        }

        if (ferryState.OnFerry)
        {
            player.ferryHandler.RemoveFromFerry();
        }

        this.previousTask = this.player.GetTask();
        this.previousTaskArgument = this.player.GetTaskArgument();

        if (!gameManager || !gameManager.Raid || !gameManager.Raid.Boss)
        {
            StartCoroutine(WaitForStart());
            return;
        }

        var boss = gameManager.Raid.Boss;
        if (player.Island != boss.Island)
        {
            teleported = true;
            prevPosition = _transform.position;
            previousIsland = player.Island;
            player.teleportHandler.Teleport(boss.Island.SpawnPosition);
        }

        if (player.RaidSkill != null)
        {
            this.player.SetTaskBySkillSilently(player.RaidSkill.Value);
        }
    }

    private IEnumerator WaitForStart()
    {
        var tries = 0;
        var maxTries = 10000;
        while (!gameManager || !gameManager.Raid || !gameManager.Raid.Boss)
        {
            yield return null;
            yield return new WaitForSeconds(0.1f);
            if (++tries >= maxTries)
            {
                yield break;
            }
        }

        var boss = gameManager.Raid.Boss;

        if (player.Island != boss.Island)
        {
            teleported = true;
            prevPosition = _transform.position;
            previousIsland = player.Island;
            player.teleportHandler.Teleport(boss.Island.SpawnPosition);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetParticipationPercentage()
    {
        return gameManager.Raid.GetParticipationPercentage(RaidEnterTime);
    }

    public void OnLeave(bool raidWon, bool raidTimedOut)
    {
        InRaid = false;

        if (raidWon)
        {
            //++player.Statistics.RaidsWon;

            var proc = gameManager.Raid.GetParticipationPercentage(RaidEnterTime) * Math.Max(1, gameManager.SessionSettings.RaidExpFactor);
            var raidBossCombatLevel = gameManager.Raid.Boss.Enemy.Stats.CombatLevel;
            var factor = Mathf.Max(raidBossCombatLevel / 300, 40) * 0.125 * proc;
            player.AddExp(Skill.Slayer, factor);

            var expFactor = Math.Max(5 * proc, factor * 0.5);

            // only if you don't have a destination is it considered as sailing for exp
            if (ferryState.OnFerry && !ferryState.HasDestination)
            {
                player.AddExp(Skill.Sailing, expFactor);
            }
            else
            {
                player.AddExp(expFactor);
            }
        }

        if (raidTimedOut || raidWon)
        {
            player.Stats.Health.Reset();
        }

        player.ClearAttackers();
        player.attackTarget = null;
        player.taskTarget = null;

        if (teleported)
        {
            teleported = false;

            if (!ferryState.OnFerry) // do not teleport back if we were on the ferry. the code below will handle it.
            {
                player.teleportHandler.Teleport(prevPosition);
            }
        }

        player.taskTarget = null;

        if (previousTask != TaskType.None)
        {
            this.player.SetTask(previousTask, previousTaskArgument, true);
        }

        if (wasResting)
        {
            player.GameManager.Onsen.Join(player);
        }
        else if (ferryState.OnFerry)
        {
            player.InCombat = false;
            player.ClearAttackers();
            player.Movement.Lock();
            player.ferryHandler.AddPlayerToFerry(ferryState.Destination);
            ferryState.HasReturned = true;
        }

        if (ferryState.State == PlayerFerryState.Embarking)
        {
            // if we were embarking, make sure we do that again.
            player.ferryHandler.Embark(ferryState.Destination);
        }
        else if (!ferryState.OnFerry && !wasResting)
        {
            var currentTask = player.GetTask();
            if (currentTask != TaskType.None)
            {
                player.GotoClosest(currentTask, true);
            }
        }
        wasResting = false;

        //#if DEBUG
        //        Shinobytes.Debug.Log($"{player.PlayerName} left the raid");
        //#endif
    }

    public bool SetSkill(RavenNest.Models.Skill? value)
    {
        player.RaidSkill = value;

        if (InRaid && player.RaidSkill != null)
        {
            this.player.SetTaskBySkillSilently(player.RaidSkill.Value);
            return true;
        }

        return false;
    }
}
