using System;
using System.Linq;
using UnityEngine;

public class ArenaHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ChunkManager chunkManager;

    private IChunk arenaChunk;
    private bool fightStarted;
    private IChunk previousChunk;

    public bool InArena { get; private set; }

    private IChunk ArenaChunk
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
            Debug.LogError($"No chunk manager set for {player.PlayerName}.");
            return;
        }

        var arena = chunkManager.GetChunkOfType(player, TaskType.Arena);
        if (arena == null)
        {
            return;
        }

        previousChunk = player.Chunk;

        InArena = true;

        player.GotoPosition(arena.CenterPointWorld);
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
        ++player.Statistics.ArenaFightsWon;

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
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    internal void Interrupt()
    {
        OnLeave();
        gameManager.Arena.Interrupt();
    }

    // Update is called once per frame
    private void Update()
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

            if (Vector3.Distance(player.transform.position, target.transform.position) > player.AttackRange)
            {
                player.GotoPosition(target.transform.position);
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
            player.GotoPosition(ArenaChunk.CenterPointWorld);
        }
    }

    private PlayerController GetTarget()
    {
        var target = player.GetAttackers()
            .OfType<PlayerController>()
            .Where(atk => atk != null && atk && !atk.Stats.IsDead)
            .OrderBy(atk => Vector3.Distance(player.transform.position, atk.transform.position))
            .FirstOrDefault();

        if (target && gameManager.Arena.AvailablePlayers.Contains(target))
        {
            return target;
        }
        
        return gameManager.Arena
            .AvailablePlayers
            .Where(x => x != null && x)
            .Except(player)
            .OrderBy(x => Vector3.Distance(player.transform.position, x.transform.position))
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
