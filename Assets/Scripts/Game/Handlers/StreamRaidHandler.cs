using System;
using UnityEngine;

public class StreamRaidHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;

    private PlayerController target;

    public bool InWar { get; set; }

    void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
    }

    public void Poll()
    {
        if (!InWar || !gameManager.StreamRaid.Started)
        {
            return;
        }

        if (target && (target.Stats.IsDead || !gameManager.StreamRaid.InRaid(target)))
        {
            target = null;
        }

        if (!target)
        {
            var targetPlayers = gameManager.StreamRaid.GetOpposingTeamPlayers(player);

            // Single pass instead of Where + OrderBy + ThenBy + ThenBy + FirstOrDefault. The old
            // form sorted the whole opposing team, computing all three keys for every player, just
            // to take one element. Keys are compared in the same priority order, and strict "<"
            // keeps the first element among equals so ties resolve as the stable sort did.
            // Squared distance is used rather than Vector3.Distance: sqrt is monotonic, so both the
            // ordering and the ties are identical, without the per-candidate square root.
            var myCombatLevel = player.Stats.CombatLevel;
            var myPosition = player.Position;
            var bestLevelDiff = 0;
            var bestSqrDistance = 0f;
            var bestAttackers = 0;

            for (var i = 0; i < targetPlayers.Count; i++)
            {
                var candidate = targetPlayers[i];
                // force check if object has not been destroyed.
                if (!candidate || candidate == null || !candidate.gameObject || candidate.gameObject == null)
                {
                    continue;
                }

                var levelDiff = Mathf.Abs(candidate.Stats.CombatLevel - myCombatLevel);
                var sqrDistance = (candidate.Position - myPosition).sqrMagnitude;
                var attackers = candidate.GetAttackers().Count;

                if (target == null
                    || levelDiff < bestLevelDiff
                    || (levelDiff == bestLevelDiff && sqrDistance < bestSqrDistance)
                    || (levelDiff == bestLevelDiff && sqrDistance == bestSqrDistance && attackers < bestAttackers))
                {
                    target = candidate;
                    bestLevelDiff = levelDiff;
                    bestSqrDistance = sqrDistance;
                    bestAttackers = attackers;
                }
            }
        }

        if (!target)
        {
            gameManager.StreamRaid.CheckForWarEnd();
            return;
        }

        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, target.Position);
        if (distance <= range)
        {
            if (target.Stats.IsDead)
            {
                target = null;
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Movement.Lock();
                return;
            }

            player.Attack(target);
        }
        else
        {
            player.SetDestination(target.Position);
        }
    }

    public void OnEnter()
    {
        player.taskTarget = null;
        player.arenaHandler.Interrupt();
        player.duelHandler.Interrupt();
        InWar = true;
    }

    public void OnExit()
    {
        InWar = false;
        if (!player.Stats.IsDead)
        {
            player.Stats.Health.Reset();
        }
        player.ClearAttackers();
        player.taskTarget = null;
        player.attackTarget = null;
    }

    internal void Died()
    {
        InWar = false;
        gameManager.StreamRaid.OnPlayerDied(player);
    }
}
