using System;
using System.Collections;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RaidHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;

    public float RaidEnterTime;

    private bool teleported;
    private IslandController previousIsland;
    private Vector3 prevPosition;
    private string[] prevTaskArgs;
    private Chunk prevChunk;
    private bool wasResting;

    private FerryState ferryState;

    public bool InRaid { get; private set; }
    public IslandController PreviousIsland => previousIsland;
    public Vector3 PreviousPosition => prevPosition;
    // Start is called before the first frame update
    void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!InRaid || !gameManager.Raid.Started || !gameManager.Raid.Boss)
        {
            return;
        }

        var target = gameManager.Raid.Boss.transform;
        PlayerController healTarget = null;
        if (player.TrainingHealing)
        {
            var raiders = gameManager.Raid.Raiders;
            if (raiders != null)
            {
                healTarget = raiders
                    .Where(x => x != null && x.Stats != null && !x.Stats.IsDead && x.Raid.InRaid)
                    .OrderByDescending(x =>
                        x.Stats.Health.Level - x.Stats.Health.CurrentValue)
                    .FirstOrDefault();

                if (healTarget && healTarget != null)
                {
                    target = healTarget.transform;
                }
                else
                {
                    healTarget = this.player;
                    target = this.player.transform;
                }
            }
        }

        if (!target)
        {
            return;
        }

        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, target.position);
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
            player.SetDestination(target.position);
        }
    }

    public void OnEnter()
    {
        InRaid = true;
        RaidEnterTime = Time.time;
#if DEBUG
        Shinobytes.Debug.Log($"{player.PlayerName} entered the raid");
#endif
        prevTaskArgs = player.GetTaskArguments().ToArray();
        if (!gameManager || !gameManager.Raid || !gameManager.Raid.Boss)
        {
            StartCoroutine(WaitForStart());
            return;
        }

        wasResting = player.Onsen.InOnsen;

        ferryState = new()
        { 
            OnFerry = player.Ferry.OnFerry,
            HasDestination = !!player.Ferry.Destination
        };

        if (wasResting)
        {
            player.GameManager.Onsen.Leave(player);
        }

        if (ferryState.OnFerry)
        {
            player.Ferry.RemoveFromFerry();
        }

        var boss = gameManager.Raid.Boss;
        if (player.Island != boss.Island)
        {
            teleported = true;
            prevPosition = player.transform.position;
            previousIsland = player.Island;
            prevChunk = player.Chunk;
            player.Teleporter.Teleport(boss.Island.SpawnPosition);
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
            prevPosition = player.transform.position;
            prevChunk = player.Chunk;
            player.Teleporter.Teleport(boss.Island.SpawnPosition);
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

            var proc = gameManager.Raid.GetParticipationPercentage(RaidEnterTime);
            var raidBossCombatLevel = (double)gameManager.Raid.Boss.Enemy.Stats.CombatLevel;
            //var exp = GameMath.CombatExperience(raidBossCombatLevel / 15) * proc;
            //var yieldExp = exp / 2d;

            var factor = Math.Min(50, Math.Max(raidBossCombatLevel / player.Stats.CombatLevel, 10d)) * proc;
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

        //if (raidTimedOut)
        //{
        //    ++player.Statistics.RaidsLost;
        //}

        if (raidTimedOut || raidWon)
        {
            player.Stats.Health.Reset();
        }

        player.ClearAttackers();

        if (teleported)
        {
            teleported = false;
            player.Teleporter.Teleport(prevPosition);
            player.Chunk = prevChunk;
            if (prevChunk != null)
            {
                player.SetTask(prevChunk.ChunkType, prevTaskArgs);
            }
            player.taskTarget = null;
        }

        if (wasResting)
        {
            player.GameManager.Onsen.Join(player);
        }
        else if (ferryState.OnFerry)
        {
            player.Movement.Lock();
            player.Ferry.AddPlayerToFerry();
            ferryState.HasReturned = true;
        }

        wasResting = false;

#if DEBUG
        Shinobytes.Debug.Log($"{player.PlayerName} left the raid");
#endif
    }


    public struct FerryState
    {
        public bool OnFerry;
        public bool HasDestination;
        public bool HasReturned;
    }

}
