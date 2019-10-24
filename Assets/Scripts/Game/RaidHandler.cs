using System;
using System.Collections.Generic;
using UnityEngine;

public class RaidHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;

    private float raidEnterTime;

    private bool teleported;
    private Vector3 prevPosition;
    private IChunk prevChunk;

    public bool InRaid { get; private set; }

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

        var boss = gameManager.Raid.Boss;
        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, boss.transform.position);
        if (distance <= range)
        {
            if (boss.Enemy.Stats.IsDead)
            {
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Lock();
                return;
            }

            player.Attack(boss.Enemy);
        }
        else
        {
            player.GotoPosition(boss.transform.position);
        }
    }

    public void OnEnter()
    {
        InRaid = true;
        raidEnterTime = Time.time;
        Debug.Log($"{player.PlayerName} entered the raid");

        var boss = gameManager.Raid.Boss;
        if (player.Island != boss.Island)
        {
            teleported = true;
            prevPosition = player.transform.position;
            prevChunk = player.Chunk;
            player.Teleporter.Teleport(boss.Island.SpawnPosition);
        }
    }

    public void OnLeave(bool raidWon, bool raidTimedOut)
    {
        InRaid = false;
        if (raidWon)
        {
            var proc = gameManager.Raid.GetParticipationPercentage(raidEnterTime);

            //var expAmount = 

            var ratio = 549.8123f * proc;

            var raidBossCombatLevel = gameManager.Raid.Boss.Enemy.Stats.CombatLevel;
            var exp = GameMath.CombatExperience(raidBossCombatLevel / 25) * proc;
            //var combatType = player.GetCombatTypeFromArgs(player.GetTaskArguments());
            //if (combatType >= 0)
            //{
            //    if (combatType == 3)
            //    {
            //        var distributedExp = exp / 3f;
            //    }
            //}

            player.AddExp((decimal)exp, Skill.Slayer);

            var woodCuttingRatio = ratio;
            var miningRatio = ratio;
            var fishingRatio = ratio;
            var farmingRatio = ratio;

            player.AddResource(Resource.Currency, (int)(player.Stats.CombatLevel * ratio));
            player.AddResource(Resource.Woodcutting, (int)(player.Stats.Woodcutting.Level * woodCuttingRatio));
            player.AddResource(Resource.Mining, (int)(player.Stats.Mining.Level * miningRatio));
            player.AddResource(Resource.Fishing, (int)(player.Stats.Fishing.Level * fishingRatio));
            player.AddResource(Resource.Farming, (int)(player.Stats.Farming.Level * farmingRatio));

            ++player.Statistics.RaidsWon;

            var itemDrop = gameManager.Raid.Boss.GetComponent<ItemDropHandler>();
            if (itemDrop)
            {
                itemDrop.DropItem(player);
            }
        }

        if (raidTimedOut)
        {
            ++player.Statistics.RaidsLost;
        }

        if (raidTimedOut || raidWon)
        {
            player.Stats.Health.Reset();
        }

        player.attackers.Clear();

        if (teleported)
        {
            teleported = false;
            player.Teleporter.Teleport(prevPosition);
            player.Chunk = prevChunk;
            player.taskTarget = null;
        }

        Debug.Log($"{player.PlayerName} left the raid");
    }
}
