using System.Linq;
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
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
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
            target = targetPlayers
                // force check if object has not been destroyed.
                .Where(x => x && x != null && x.gameObject && x.gameObject != null)
                .OrderBy(x => Mathf.Abs(x.Stats.CombatLevel - player.Stats.CombatLevel))
                .ThenBy(x => Vector3.Distance(x.transform.position, player.transform.position))
                .ThenBy(x => x.GetAttackers().Count)
                .FirstOrDefault();
        }

        if (!target)
        {
            gameManager.StreamRaid.CheckForWarEnd();
            return;
        }

        var range = player.GetAttackRange();
        var distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= range)
        {
            if (target.Stats.IsDead)
            {
                target = null;
                return;
            }

            if (!player.IsReadyForAction)
            {
                player.Lock();
                return;
            }

            player.Attack(target);
        }
        else
        {
            player.GotoPosition(target.transform.position);
        }
    }

    public void OnEnter()
    {
        player.taskTarget = null;
        player.Arena.Interrupt();
        player.Duel.Interrupt();
        InWar = true;
    }

    public void OnExit()
    {
        InWar = false;
        if (!player.Stats.IsDead)
        {
            player.Stats.Health.Reset();
        }
        player.attackers.Clear();
    }

    internal void Died()
    {
        InWar = false;
        gameManager.StreamRaid.OnPlayerDied(player);
    }
}
