using UnityEngine;

/// <summary>
///     The places a player can be away from the islands in (docs/tower-design.md, section 8). While away, the player
///     is not on an island, cannot sail, travel, rest or join raids, and gets back what they left when they return.
///     Raids, duels, the arena and Ravebridge happen on the islands, so they are not here.
/// </summary>
public enum AwayPlace
{
    None = 0,
    Dungeon = 1,
    Tower = 2,
}

/// <summary>What a handler for an away place gives the rest of the game.</summary>
public interface IAwayHandler
{
    AwayPlace Place { get; }

    /// <summary>Whether the player is in it now.</summary>
    bool IsAway { get; }

    /// <summary>What the player left behind and the way back.</summary>
    WorldReturn Return { get; }

    /// <summary>Where a stuck player is put back inside the place.</summary>
    Vector3 SpawnPosition { get; }
}

/// <summary>
///     What a player leaves behind when going somewhere away from the islands (a dungeon, the tower), and the way back:
///     the task and its argument, where they stood and on which island, whether they were on a ferry (and where it was
///     going) or resting in the onsen. Taken out of DungeonHandler so every away place returns players the same way.
/// </summary>
public sealed class WorldReturn
{
    public TaskType Task;
    public string TaskArgument;
    public Vector3 Position;
    public IslandController Island;
    public FerryContext Ferry;
    public bool WasResting;

    /// <summary>
    ///     Remembers what the player is doing and takes them off the ferry or out of the onsen. The caller moves them
    ///     to the away place afterwards.
    /// </summary>
    public void Leave(PlayerController player)
    {
        if (player.Island)
        {
            player.Island.RemovePlayer(player);
        }

        Task = player.GetTask();
        TaskArgument = player.GetTaskArgument();

        Ferry = new()
        {
            OnFerry = player.ferryHandler.OnFerry,
            State = player.ferryHandler.State,
            HasDestination = !!player.ferryHandler.Destination,
            Destination = player.ferryHandler.Destination
        };

        if (Ferry.OnFerry)
        {
            player.ferryHandler.RemoveFromFerry();
        }
        else
        {
            Position = player.Position;
        }

        WasResting = player.onsenHandler.InOnsen;

        if (player.onsenHandler.InOnsen)
        {
            Position = player.onsenHandler.EntryPoint;
            player.onsenHandler.Exit();
            player.transform.parent = null;
        }

        Island = player.Island;
    }

    /// <summary>
    ///     Gives the player back what they left: the task, the ferry (or the spot they stood on), the embarking, the walk
    ///     to the task and the onsen. The caller clears its own state first.
    /// </summary>
    public void Back(PlayerController player, string placeName)
    {
        if (Task != TaskType.None)
        {
            player.SetTask(Task, TaskArgument, true);
        }
        player.taskTarget = null;

        if (Ferry.OnFerry)
        {
            player.Movement.Lock();
            player.ferryHandler.AddPlayerToFerry(Ferry.Destination);
            Ferry.HasReturned = true;
        }
        else
        {
            player.teleportHandler.Teleport(ReturnPoint(player, placeName));
        }

        if (Ferry.State == PlayerFerryState.Embarking)
        {
            // if we were embarking, make sure we do that again.
            player.ferryHandler.Embark(Ferry.Destination);
        }
        else
        {
            var currentTask = player.GetTask();
            if (!Ferry.OnFerry && currentTask != TaskType.None)
            {
                player.GotoClosest(currentTask, true);
            }

            if (WasResting)
            {
                player.GameManager.Onsen.Join(player);
            }
        }

        WasResting = false;
    }

    /// <summary>
    /// Where to put the player back: where they stood before, if that is still somewhere to stand. It may not be: they
    /// may have left from the ferry's deck at a dock (over the sea), or the ground there may have changed. The spot
    /// must be on an island, on navmesh at about its height, above the sea, and joined to the island's spawn point by a
    /// path (not a patch of seabed or a hole round a tree); otherwise it is the spawn point of the island they came
    /// from, or of the island nearest the spot.
    /// </summary>
    public Vector3 ReturnPoint(PlayerController player, string placeName)
    {
        var islands = player.GameManager ? player.GameManager.Islands : null;
        var island = islands ? islands.FindIsland(Position) : null;
        var home = Island ? Island : island;
        if (!home && islands) home = islands.Find("Home");
        if (island && NavMeshHelper.SampleNearHeight(Position, 1f, 1.5f, out var ground))
        {
            var ocean = Object.FindAnyObjectByType<Ocean>();
            var dry = !ocean || ground.y > ocean.transform.position.y + 0.3f;
            var path = new UnityEngine.AI.NavMeshPath();
            var joined = UnityEngine.AI.NavMesh.CalculatePath(ground, island.SpawnPosition, NavMeshHelper.AreaMaskAt(ground), path)
                && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
            if (dry && joined) return ground;
        }
        if (home)
        {
            Shinobytes.Debug.LogWarning(player.Name + " could not go back to where they left for " + placeName + " (" + Position + "), so they go to " + home.Identifier + " instead.");
            return home.SpawnPosition;
        }
        return Position;
    }
}
