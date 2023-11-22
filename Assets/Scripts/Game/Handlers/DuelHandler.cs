using System.Collections;
using UnityEngine;

public class DuelHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameManager gameManager;
    private DuelCameraScript duelCamera;

    private float duelRequestTime = 30f;
    private float duelRequestTimer = 0f;
    private bool wasInOnsen;
    private DuelState state = DuelState.NotStarted;
    private bool isDuelCameraOwner;

    public bool InDuel => state >= DuelState.StakeInput;
    public PlayerController Opponent { get; private set; }
    public PlayerController Requester { get; private set; }
    public bool HasActiveRequest => Requester;

    private void Awake()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!duelCamera) duelCamera = FindAnyObjectByType<DuelCameraScript>();
    }

    private void Update()
    {
        if (duelRequestTimer > 0f)
        {
            duelRequestTimer -= GameTime.deltaTime;
            if (duelRequestTimer <= 0f)
            {
                DeclineDuel(true);
            }
        }

        if (!InDuel)
        {
            return;
        }

        if (state == DuelState.Started)
        {
            var distance = Vector3.Distance(player.Position, Opponent.Position);
            if (distance > player.AttackRange)
            {
                player.SetDestination(Opponent.Position);
            }
            else
            {
                StartFight();
                Opponent.duelHandler.StartFight();
                player.Movement.Lock();
            }
        }

        if (state == DuelState.Fighting)
        {
            if (!player.IsReadyForAction)
            {
                return;
            }

            if (player.Stats.IsDead)
            {
                return;
            }

            if (Opponent.Stats.IsDead)
            {
                return;
            }

            player.Attack(Opponent);
        }
    }

    public void Interrupt()
    {
        RemoveDuelCamera();
        Reset();
    }

    public void RequestDuel(PlayerController target)
    {
        target.duelHandler.Request(this);
        Opponent = target;
        state = DuelState.RequestSent;
    }

    private void Request(DuelHandler duelHandler)
    {
        Requester = duelHandler.player;
        Opponent = duelHandler.player;
        duelRequestTimer = duelRequestTime;
        state = DuelState.RequestReceived;
        if (gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendReply(player, Localization.MSG_DUEL_REQ, Opponent.PlayerName);
        }
    }

    public void DeclineDuel(bool timedOut = false)
    {
        try
        {
            if ((gameManager.RavenBot?.IsConnected).GetValueOrDefault())
            {
                if (timedOut)
                {
                    gameManager.RavenBot.SendReply(player, Localization.MSG_DUEL_REQ_TIMEOUT, Requester.PlayerName);
                }
                else
                {
                    gameManager.RavenBot.SendReply(player, Localization.MSG_DUEL_REQ_DECLINE, Requester.PlayerName);
                }
            }

            Reset();
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("DuelHandler.DeclineDuel: " + exc);
        }
    }

    public void AcceptDuel()
    {
        if (gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendReply(player, Localization.MSG_DUEL_REQ_ACCEPT, Requester.PlayerName);
        }

        Opponent = Requester;
        Requester.duelHandler.Opponent = player;

        Opponent.duelHandler.StartDuel();
        StartDuel();

        isDuelCameraOwner = duelCamera.AddTarget(this);
    }

    public void Died()
    {
        if (!InDuel) return;
        Opponent.duelHandler.WonDuel();
        LostDuel();
    }

    public void StartFight()
    {
        if (!InDuel) return;
        if (player.onsenHandler.InOnsen)
        {
            player.onsenHandler.Exit();
        }

        player.Stats.Health.Reset();
        state = DuelState.Fighting;
    }

    private void LostDuel()
    {
        RemoveDuelCamera();

        if (wasInOnsen)
        {
            player.GameManager.Onsen.Join(player);
        }

        //++player.Statistics.DuelsLost;
        Reset();
    }

    private void WonDuel()
    {

        RemoveDuelCamera();

        if (gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendReply(player, Localization.MSG_DUEL_WON, Opponent.PlayerName);
        }

        //++player.Statistics.DuelsWon;
        player.Stats.Health.Reset();

        if (wasInOnsen)
        {
            player.GameManager.Onsen.Join(player);
        }

        Reset();
    }

    private void RemoveDuelCamera()
    {
        if (isDuelCameraOwner)
        {
            StartCoroutine(_RemoveDuelCamera());
        }
    }

    private IEnumerator _RemoveDuelCamera()
    {
        yield return new WaitForSeconds(2f);
        duelCamera.RemoveTarget(this);
    }

    private void StartDuel()
    {
        wasInOnsen = player.onsenHandler.InOnsen;
        if (wasInOnsen)
            player.onsenHandler.Exit();

        state = DuelState.Started;
        duelRequestTimer = 0f;
        player.Stats.Health.Reset();
    }

    private void Reset()
    {
        wasInOnsen = false;
        state = DuelState.NotStarted;
        Opponent = null;
        Requester = null;
    }
}

public enum DuelState
{
    NotStarted,
    RequestReceived,
    RequestSent,
    StakeInput,
    Started,
    Fighting,
}