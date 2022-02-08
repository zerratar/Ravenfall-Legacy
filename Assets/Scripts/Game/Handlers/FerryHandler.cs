using UnityEngine;

public class FerryHandler : MonoBehaviour
{
    private PlayerFerryState state;

    private PlayerController player;
    private FerryController ferry;
    private GameManager gameManager;
    private IslandController destination;

    public bool OnFerry => state == PlayerFerryState.Embarked || isOnFerry;//player.transform.parent?.GetComponentInParent<FerryController>();
    public bool Active => state > PlayerFerryState.None;
    public bool Embarking => state == PlayerFerryState.Embarking;
    public bool Disembarking => state == PlayerFerryState.Disembarking;
    public bool IsCaptain => OnFerry && ferry.IsCaptainPosition(transform.parent);
    public IslandController Destination => destination;

    private float expTime = 2.5f;
    private float expTimer = 2.5f;
    private Transform lastFerryPoint;
    private bool hasReferences;
    private bool isOnFerry;

    private void Start()
    {
        EnsureReferences();
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (hasReferences) return;
        if (!ferry) ferry = FindObjectOfType<FerryController>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!player) player = GetComponent<PlayerController>();
        hasReferences = player && gameManager && ferry;
    }

    private void Update()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        EnsureReferences();

        if (OnFerry)
        {
            player.Animations.SetCaptainState(IsCaptain);

            expTimer -= Time.deltaTime;
            if (expTimer <= 0f)
            {
                expTimer = expTime;
                player.AddExp(Skill.Sailing);
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
                player.GotoPosition(player.Island.DockingArea.DockPosition, Time.frameCount % 30 == 0);
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

    public void Cancel()
    {
        EnsureReferences();

        state = PlayerFerryState.None;
        player.GotoPosition(player.Position);
        this.destination = null;
    }

    public void Embark(IslandController destination = null)
    {
        EnsureReferences();

        player.Animations.ResetAnimationStates();
        player.taskTarget = null;
        state = PlayerFerryState.Embarking;
        this.destination = destination;

        if (player.Onsen.InOnsen)
        {
            gameManager.Onsen.Leave(player);
        }
    }

    public void Disembark(IslandController destination = null)
    {
        EnsureReferences();

        state = PlayerFerryState.Disembarking;
        this.destination = destination;
    }

    public bool RemoveFromFerry()
    {
        var onFerry = OnFerry;
        isOnFerry = false;
        if (onFerry)
        {
            player.transform.SetParent(null);

            if (this.IsCaptain)
            {
                // if we remove the captain we need to assign a new player
                ferry.AssignBestCaptain();
            }
            return true;
        }

        return false;
    }

    private void RemovePlayerFromFerry(IslandController island)
    {
        state = PlayerFerryState.None;

        var onFerry = OnFerry;

        player.Animations.SetCaptainState(false);

        if (RemoveFromFerry())
        {
            player.transform.position = island.DockingArea.DockPosition;
        }
        isOnFerry = false;
        player.Island = island ?? gameManager.Islands.FindPlayerIsland(player);
        player.taskTarget = null;
        //player.Unlock();

        var task = player.GetTask();
        if (task != TaskType.None)
        {
            player.GotoClosest(task);
        }
    }

    private void LateUpdate()
    {
        if (state == PlayerFerryState.Embarked && lastFerryPoint && transform.parent == lastFerryPoint)
        {
            player.transform.localPosition = Vector3.zero;
            player.transform.rotation = lastFerryPoint.rotation;
        }
    }

    public void AddPlayerToFerry(bool canBeCaptain = true)
    {
        EnsureReferences();
        if (!ferry) return;

        // re-arrange players if this player should be the captain.

        var currentCaptain = ferry.Captain;
        if (currentCaptain)
        {
            if (player.Stats.Sailing.CurrentValue > currentCaptain.Stats.Sailing.CurrentValue)
            {
                currentCaptain.Ferry.AddPlayerToFerry(false);
            }
        }

        lastFerryPoint = ferry.GetNextPlayerPoint(canBeCaptain);
        if (lastFerryPoint)
        {
            state = PlayerFerryState.Embarked;
            player.transform.SetParent(lastFerryPoint);
            player.transform.localPosition = Vector3.zero;
            player.transform.rotation = lastFerryPoint.rotation;
            player.Island = null;

            if (ferry.IsCaptainPosition(lastFerryPoint))
            {
                ferry.SetCaptain(this.player);
            }
            this.isOnFerry = true;
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
