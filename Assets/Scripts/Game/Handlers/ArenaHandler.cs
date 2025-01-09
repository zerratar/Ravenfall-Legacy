using Shinobytes.Linq;
using System;
using UnityEngine;

public class ArenaHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ChunkManager chunkManager;

    private Chunk arenaChunk;
    private bool fightStarted;
    private Chunk previousChunk;

    public bool InArena;

    private Chunk ArenaChunk
    {
        get
        {
            if (arenaChunk == null && player && chunkManager)
            {
                arenaChunk = chunkManager.GetChunkOfType(player, TaskType.Arena);
            }
            return arenaChunk;
        }
    }

    public void OnEnter()
    {
        if (!chunkManager)
        {
            Shinobytes.Debug.LogError($"No chunk manager set for {player.PlayerName}.");
            return;
        }

        var arena = chunkManager.GetChunkOfType(player, TaskType.Arena);
        if (arena == null)
        {
            return;
        }

        previousChunk = player.Chunk;

        InArena = true;

        player.SetDestination(arena.CenterPointWorld);
    }

    public void OnFightStart()
    {
        fightStarted = true;
    }

    public void OnKicked()
    {
        OnLeave();
    }

    public void OnLeave()
    {
        fightStarted = false;
        InArena = false;
    }

    public void OnWin(bool wonByDraw)
    {
        //++player.Statistics.ArenaFightsWon;

        player.Stats.Health.Reset();

        CelebrateArenaWin(wonByDraw);
        WalkAwayFromArena();
    }

    public void WalkAwayFromArena()
    {
        if (previousChunk == null || previousChunk.ChunkType == TaskType.Arena)
        {
            player.GotoStartingArea();
            return;
        }

        player.GotoActiveChunk();
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!chunkManager) chunkManager = FindAnyObjectByType<ChunkManager>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
    }

    internal void Interrupt()
    {
        OnLeave();
        gameManager.Arena.Interrupt();
    }

    // Update is called once per frame
    public void Poll()
    {
        if (!InArena)
        {
            return;
        }

        if (gameManager.Arena.InsideArena(player))
        {
            if (!fightStarted)
            {
                return;
            }

            var target = GetTarget();
            if (!target)
            {
                gameManager.Arena.End();
                return;
            }

            if (Vector3.Distance(player.Position, target.Position) > player.AttackRange)
            {
                player.SetDestination(target.Position);
                return;
            }

            if (!player.IsReadyForAction)
            {
                return;
            }

            if (player.Stats.IsDead)
            {
                return;
            }

            player.Attack(target);
        }
        else
        {
            player.SetDestination(ArenaChunk.CenterPointWorld);
        }
    }

    private PlayerController GetTarget()
    {
        var target = player.GetAttackers()
            .WhereOfType(x => x as PlayerController, atk => atk != null && atk && !atk.Stats.IsDead)
            .OrderBy(atk => Vector3.Distance(player.Position, atk.Position))
            .FirstOrDefault();

        if (target && gameManager.Arena.AvailablePlayers.Contains(target))
        {
            return target;
        }

        return gameManager.Arena
            .AvailablePlayers
            .Where(x => x != null && x && x != player)
            .OrderBy(x => Vector3.Distance(player.Position, x.Position))
            .FirstOrDefault();
    }

    private void CelebrateArenaWin(bool wonByDraw)
    {
        //if (wonByDraw)
        //{
        //    Debug.Log("Congratulations, " + player.PlayerName + "! You received a reward as the arena ended in a draw!");
        //}
        //else
        //{
        //    Debug.Log("Congratulations, " + player.PlayerName + "! You won the arena");
        //}
    }

}
