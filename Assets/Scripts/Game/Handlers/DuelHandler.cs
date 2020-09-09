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
            var distance = Vector3.Distance(player.transform.position, Opponent.transform.position);
            if (distance > player.AttackRange)
            {
                player.GotoPosition(Opponent.transform.position);
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
        if (gameManager.Server.Client != null && gameManager.Server.Client.Connected)
        {
            gameManager.Server.Client.SendCommand(
                player.PlayerName,
                "duel_alert",
                $"A duel request received from {Opponent.PlayerName}, reply with !duel accept or !duel decline");
        }
    }

    public void DeclineDuel(bool timedOut = false)
    {
        if ((gameManager.Server?.Client?.Connected).GetValueOrDefault())
        {
            if (timedOut)
            {
                gameManager.Server.Client.SendCommand(
                    player.PlayerName,
                    "duel_declined",
                    $"The duel request from {Requester.PlayerName} has timed out and automatically declined.");

            }
            else
            {
                gameManager.Server.Client.SendCommand(
                    player.PlayerName,
                    "duel_declined",
                    $"Duel with {Requester.PlayerName} was declined.");
            }
        }

        Reset();
    }

    public void AcceptDuel()
    {
        if (gameManager.Server.Client != null && gameManager.Server.Client.Connected)
        {
            gameManager.Server.Client.SendCommand(
                player.PlayerName,
                "duel_accepted",
                $"You have accepted the duel against {Requester.PlayerName}. May the best fighter win!");
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

        if (gameManager.Server.Client != null && gameManager.Server.Client.Connected)
        {
            gameManager.Server.Client.SendCommand(player.PlayerName, "duel_result", $"You won the duel against {Opponent.PlayerName}!");
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