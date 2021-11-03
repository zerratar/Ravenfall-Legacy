using System.Collections;
using UnityEngine;

public class DuelHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameManager gameManager;
    private DuelCameraScript duelCamera;

    private float duelRequestTime = 30f;
    private float duelRequestTimer = 0f;
    private DuelState state = DuelState.NotStarted;
    private bool isDuelCameraOwner;

    public bool InDuel => state >= DuelState.StakeInput;
    public PlayerController Opponent { get; private set; }
    public PlayerController Requester { get; private set; }
    public bool HasActiveRequest => Requester;

    private void Awake()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!duelCamera) duelCamera = FindObjectOfType<DuelCameraScript>();
    }

    private void Update()
    {
        if (duelRequestTimer > 0f)
        {
            duelRequestTimer -= Time.deltaTime;
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
                player.GotoPosition(Opponent.Position);
            }
            else
            {
                StartFight();
                Opponent.Duel.StartFight();
                player.Lock();
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
        target.Duel.Request(this);
        Opponent = target;
        state = DuelState.RequestSent;
    }

    private void Request(DuelHandler duelHandler)
    {
        Requester = duelHandler.player;
        Opponent = duelHandler.player;
        duelRequestTimer = duelRequestTime;
        state = DuelState.RequestReceived;
        if (gameManager.RavenBot.Local != null && gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_DUEL_REQ, Opponent.PlayerName);
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
                    gameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_DUEL_REQ_TIMEOUT, Requester.PlayerName);
                }
                else
                {
                    gameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_DUEL_REQ_DECLINE, Requester.PlayerName);
                }
            }

            Reset();
        }
        catch (System.Exception exc)
        {
            GameManager.LogError(exc.ToString());
        }
    }

    public void AcceptDuel()
    {
        if (gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_DUEL_REQ_ACCEPT, Requester.PlayerName);
        }

        Opponent = Requester;
        Requester.Duel.Opponent = player;

        Opponent.Duel.StartDuel();
        StartDuel();

        isDuelCameraOwner = duelCamera.AddTarget(this);
    }

    public void Died()
    {
        if (!InDuel) return;
        Opponent.Duel.WonDuel();
        LostDuel();
    }

    public void StartFight()
    {
        if (!InDuel) return;
        player.Stats.Health.Reset();
        state = DuelState.Fighting;
    }

    private void LostDuel()
    {
        RemoveDuelCamera();
        ++player.Statistics.DuelsLost;
        Reset();
    }

    private void WonDuel()
    {
        RemoveDuelCamera();

        if (gameManager.RavenBot.IsConnected)
        {
            gameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_DUEL_WON, Opponent.PlayerName);
        }

        ++player.Statistics.DuelsWon;
        player.Stats.Health.Reset();
        Reset();
    }

    private void RemoveDuelCamera()
    {
        if (isDuelCameraOwner)
        {
            StartCoroutine(_RemoveDuelCamear());
        }
    }

    private IEnumerator _RemoveDuelCamear()
    {
        yield return new WaitForSeconds(2f);
        duelCamera.RemoveTarget(this);
    }

    private void StartDuel()
    {
        state = DuelState.Started;
        duelRequestTimer = 0f;
        player.Stats.Health.Reset();
    }

    private void Reset()
    {
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