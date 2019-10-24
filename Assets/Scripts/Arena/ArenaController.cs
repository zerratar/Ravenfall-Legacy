using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArenaController : MonoBehaviour
{
    private readonly List<PlayerController> joinedPlayers = new List<PlayerController>();
    private readonly List<PlayerController> deadPlayers = new List<PlayerController>();

    [SerializeField] private SphereCollider fightArea;
    [SerializeField] private ArenaGateController gate;
    [SerializeField] private ArenaState state = ArenaState.NotStarted;
    [SerializeField] private float arenaStartTime = 60f; // takes 1 min before it starts.
    [SerializeField] private ArenaNotifications notifications;
    [SerializeField] private GameCamera gameCamera;

    private float arenaStartTimer;
    private bool arenaCountdownStarted;
    private IslandController island;

    public IslandController Island => island;

    public bool Started => state == ArenaState.Started;
    public IReadOnlyList<PlayerController> JoinedPlayers => joinedPlayers;
    public IReadOnlyList<PlayerController> AvailablePlayers => joinedPlayers.Except(deadPlayers).Where(InsideArena).ToList();

    // Start is called before the first frame update
    void Start()
    {
        arenaStartTimer = arenaStartTime;
        if (!gameCamera) gameCamera = GameObject.FindObjectOfType<GameCamera>();
        island = GetComponentInParent<IslandController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == ArenaState.WaitingForStart)
        {
            if (arenaStartTimer > 0)
                arenaStartTimer -= Time.deltaTime;

            RemoveKickedPlayers();

            if (joinedPlayers.Count < 2)
            {
                state = ArenaState.NotStarted;
                return;
            }

            notifications.ShowStartingSoon((int)arenaStartTimer);

            if (arenaStartTimer <= 0f)
            {
                if (JoinedPlayers.All(InsideArena))
                {
                    BeginArenaFight();
                }
            }
        }

        if (state == ArenaState.Started && AvailablePlayers.Count == 0)
        {
            End();
        }
    }

    private void RemoveKickedPlayers()
    {
        var playersToKick = JoinedPlayers.Where(x => x.gameObject == null).ToList();
        foreach (var player in playersToKick)
        {
            player.Arena.OnKicked();
            joinedPlayers.Remove(player);
        }
    }

    public bool InsideArena(PlayerController target) => InsideArena(target.transform);
    public bool InsideArena(Transform target)
    {
        if (!fightArea) fightArea = GetComponent<SphereCollider>();
        if (!fightArea) return false;
        //var capsuleCollider = target.GetComponent<CapsuleCollider>();
        //var insideArena = capsuleCollider && fightArea.bounds.Intersects(capsuleCollider.bounds);

        var dist = Vector3.Distance(fightArea.transform.position, target.position);
        return dist <= fightArea.radius;
    }

    public void BeginCountdown()
    {
        if (state != ArenaState.NotStarted)
        {
            return;
        }

        if (JoinedPlayers.Count < 2)
        {
            notifications.ShowActivateArena(2 - JoinedPlayers.Count);
            //Debug.Log("Arena is not ready yet, we need one more player to start the countdown!");
            return;
        }

        //Debug.Log("Arena countdown begins!");
        state = ArenaState.WaitingForStart;
        arenaStartTimer = arenaStartTime;
        OpenGate();
    }

    public void BeginArenaFight()
    {
        StartCoroutine(_StartArenaFight());
    }

    public void End()
    {
        if (gameCamera) gameCamera.DisableFocusCamera();
        if (AvailablePlayers.Count != 1)
        {
            Debug.Log("It ended in a draw! Reward them all!");

            notifications.ShowDraw();

            foreach (var player in JoinedPlayers)
            {
                player.Arena.OnWin(true);
                player.Arena.OnLeave();
            }
        }
        else
        {
            // do something awesome for the winning player
            var winner = AvailablePlayers.First();
            winner.Arena.OnWin(false);
            notifications.ShowWinner(winner);

            foreach (var player in JoinedPlayers)
            {
                player.Arena.OnLeave();
            }
        }

        ResetState();
    }

    private void ResetState()
    {
        Debug.Log("Arena has been reset");
        joinedPlayers.Clear();
        deadPlayers.Clear();
        state = ArenaState.NotStarted;
        OpenGate();
    }

    public void CloseGate()
    {
        StartCoroutine(_CloseGate());
    }

    public void OpenGate()
    {
        StartCoroutine(_OpenGate());
    }

    public bool CanJoin(PlayerController player, out bool alreadyJoined, out bool alreadyStarted)
    {
        alreadyJoined = JoinedPlayers.FirstOrDefault(x => x.PlayerName.Equals(player.PlayerName));
        alreadyStarted = state >= ArenaState.Started;

        if (state >= ArenaState.Started)
        {
            return false;
        }

        if (alreadyJoined)
        {
            return false;
        }

        if (player.Stats.IsDead)
        {
            return false;
        }

        return true;
    }

    public void Interrupt()
    {
        ResetState();
    }

    public bool HasJoined(PlayerController player)
    {
        return joinedPlayers.FirstOrDefault(x => x == player);
    }

    public bool Leave(PlayerController player)
    {
        if (CanJoin(player, out var alreadyJoined, out var alreadyStarted))
        {
            return false;
        }

        if (alreadyStarted)
        {
            return false;
        }

        if (alreadyJoined)
        {
            joinedPlayers.Remove(player);
            player.Arena.OnLeave();


            return true;
        }

        return false;
    }

    public void Join(PlayerController player)
    {
        if (!CanJoin(player, out var _, out var _))
        {
            return;
        }

        Debug.Log($"{player.PlayerName} joined the arena!");

        if (!joinedPlayers.Remove(player))
        {
            joinedPlayers.Add(player);
        }

        player.Arena.OnEnter();

        BeginCountdown();
    }

    public void Died(PlayerController player)
    {
        if (!joinedPlayers.Contains(player))
        {
            return;
        }

        deadPlayers.Add(player);
        player.Arena.OnLeave();
    }

    private IEnumerator _CloseGate()
    {
        yield return new WaitForSeconds(1f);
        if (gate.State == ArenaGateState.Open)
        {
            gate.Close();
        }
    }

    private IEnumerator _StartArenaFight()
    {
        notifications.ShowStartArena();
        if (gameCamera) gameCamera.EnableArenaCamera();
        yield return new WaitForSeconds(4);

        foreach (var player in joinedPlayers)
        {
            player.Arena.OnFightStart();
        }

        state = ArenaState.Started;
        CloseGate();
        Debug.Log("Arena fight begins!");
    }

    private IEnumerator _OpenGate()
    {
        yield return new WaitForSeconds(0.5f);
        if (gate.State == ArenaGateState.Closed)
        {
            gate.Open();
        }
    }

}

public enum ArenaState
{
    NotStarted,
    WaitingForStart,
    Started,
    WaitingForFinish,
    Finished
}
