using UnityEngine;

public class FerryHandler : MonoBehaviour
{
    private PlayerFerryState state;

    private PlayerController player;
    private FerryController ferry;
    private GameManager gameManager;
    private IslandController destination;

    public bool OnFerry => state == PlayerFerryState.Embarked || player.transform.parent;
    public bool Active => state > PlayerFerryState.None;
    public bool Embarking => state == PlayerFerryState.Embarking;
    public bool Disembarking => state == PlayerFerryState.Disembarking;
    public bool IsCaptain => OnFerry && ferry.IsCaptainPosition(transform.parent);
    public IslandController Destination => destination;
    public decimal Experience => 25;

    private float expTime = 2.5f;
    private float expTimer = 2.5f;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        ferry = FindObjectOfType<FerryController>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        player.Animations.SetCaptainState(IsCaptain);

        if (OnFerry)
        {
            expTimer -= Time.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(Experience, Skill.Sailing);
            }

            if (ferry.state == FerryState.Docked
                && ferry.Island && destination
                && destination == ferry.Island)
            {
                Disembark(ferry.Island);
            }
        }

        if (Embarking && !OnFerry)
        {
            if (!player.Island) return;
            if (!player.Island.DockingArea) return;
            if (!player.Island.DockingArea.OnDock(player))
            {
                player.GotoPosition(player.Island.DockingArea.DockPosition);
            }
            else
            {
                player.Lock();

                if (ferry.Docked && ferry.Island == player.Island)
                {
                    AddPlayerToFerry();
                }
            }
        }
        else if (Disembarking)
        {
            if (!OnFerry)
            {
                RemovePlayerFromFerry(destination);
                return;
            }

            if (!ferry.Island)
            {
                return;
            }

            if (!ferry.Island.DockingArea) return;

            Disembark();
        }
    }

    private void Disembark()
    {
        if (!ferry.Island.DockingArea.OnDock(player))
        {
            if (ferry.Docked && destination)
            {
                // we have reached our target destination
                if (destination == ferry.Island)
                {
                    // disembark!
                    RemovePlayerFromFerry(destination);
                }

            }
            else if (ferry.Docked && !destination)
            {
                RemovePlayerFromFerry(ferry.Island);
            }
        }
        else
        {
            RemovePlayerFromFerry(ferry.Island);
        }
    }

    public void Embark(IslandController destination = null)
    {
        player.Animations.ResetAnimationStates();
        state = PlayerFerryState.Embarking;
        this.destination = destination;
    }

    public void Disembark(IslandController destination = null)
    {
        state = PlayerFerryState.Disembarking;
        this.destination = destination;
    }

    private void RemovePlayerFromFerry(IslandController island)
    {
        state = PlayerFerryState.None;
        var onFerry = OnFerry;
        if (onFerry)
        {
            player.transform.SetParent(null);
            player.transform.position = island.DockingArea.DockPosition;
        }

        player.Island = island ?? gameManager.Islands.FindPlayerIsland(player);

        player.Unlock();
        if (player.Chunk != null)
        {
            player.GotoClosest(player.Chunk.ChunkType);
        }

        if (onFerry)
        {
            gameManager.Server?.Client?.SendCommand(
                player.PlayerName,
                "ferry_success",
                $"You have disembarked the ferry.");
        }
    }

    public void AddPlayerToFerry()
    {
        var ferryPoint = ferry.GetNextPlayerPoint();
        if (ferryPoint)
        {
            state = PlayerFerryState.Embarked;
            player.transform.SetParent(ferryPoint);
            player.transform.localPosition = Vector3.zero;
            player.transform.rotation = ferryPoint.rotation;
            player.Island = null;
        }
    }

    public enum PlayerFerryState
    {
        None,
        Embarking,
        Embarked,
        Fishing,
        Disembarking
    }
}
