using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TicTacToe : MonoBehaviour, ITavernGame
{
    [SerializeField] private TextMeshPro boardTitle;
    [SerializeField] private TextMeshPro boardText;
    [SerializeField] private TextMeshPro helpText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemDropHandler itemDropHandler;

    public bool RewardStreamerTokenOnWin;

    private float tokenWinChance = 0.10f;

    private readonly List<string> players = new List<string>();
    private TavernGameHighscore highscore;

    private TavernGameState state;

    private string[] teamNames = new string[] { "Blue", "Red" };
    private string[] teamColors = new string[] { "#009DFF", "red" };
    private string highscoreTopPlayerColor = "";
    private string infoTextColor = "<color=#444444>";
    private string infoTextSize = "<size=13>";

    private int playerTurn;

    public GameObject Player1;
    public GameObject Player2;
    public Transform GameObjects;
    public Transform[] Placements;
    public int[] Grid = new int[9];
    public GameObject GameBoard;

    public float Player2ZOffset = -1.63f;

    public string GameStartCommand => "tictactoe";

    public TavernGameState State => state;

    public bool IsGameOver => State == TavernGameState.GameOver;
    public bool Started => State != TavernGameState.None;

    void Start()
    {
        highscore = new TavernGameHighscore("tictactoe");
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!itemDropHandler) itemDropHandler = FindObjectOfType<ItemDropHandler>();

        //ResetText.SetActive(false);
        //RedTeamWins.SetActive(false);
        //BlueTeamWins.SetActive(false);
        //NoTeamWins.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!highscore.IsLoaded)
        {
            highscore.Load();
            UpdateLeaderBoard();
        }
        if (Started && Input.GetKeyUp(KeyCode.Delete))
        {
            this.ResetGame();
        }
    }

    public void Activate()
    {
        this.state = TavernGameState.WaitingForPlayers;
        this.helpText.text = "!tictactoe 1-9\r\n" + infoTextColor + infoTextSize + "JOIN AND PLAY";

        //var ioc = gameManager.gameObject.GetComponent<IoCContainer>();
        //var evt = ioc.Resolve<EventTriggerSystem>();
        //evt.TriggerEvent("ttt", TimeSpan.FromSeconds(1));
    }

    public void Play(PlayerController player, int slot)
    {
        int teamIndex;

        if (IsGameOver)
        {
            teamIndex = this.players.IndexOf(player.UserId) % 2;
            PlaceMarker(teamIndex, slot - 1); // the troll is on you now
            return;
        }

        if (!this.players.Contains(player.UserId))
        {
            this.players.Add(player.UserId);
            state = TavernGameState.Playing;
            teamIndex = this.players.IndexOf(player.UserId) % 2;
            gameManager.RavenBot.SendMessage(player.Name, Localization.MSG_TICTACTOE_JOINED, teamNames[teamIndex]);
            UpdateTeamListing();
        }
        else
        {
            teamIndex = this.players.IndexOf(player.UserId) % 2;
            UpdateTeamListing();
        }

        if (IsGameOver || playerTurn != teamIndex)
        {
            var errMessage = IsGameOver
                ? Localization.MSG_TICTACTOE_REJECT_GAMEOVER
                : Localization.MSG_TICTACTOE_REJECT_TURN;

            RejectPlay(player, errMessage);
            return;
        }

        var i = slot - 1;
        if (i < 0 || i > Grid.Length)
        {
            var errMessage = Localization.MSG_TICTACTOE_REJECT_NUMBER;
            RejectPlay(player, errMessage);
            return;
        }

        if (Grid[i] != 0)
        {
            var errMessage = Localization.MSG_TICTACTOE_REJECT_PLAYED;
            RejectPlay(player, errMessage);
            return;
        }

        PlaceMarker(teamIndex, i);

        UpdateTeamListing();

        if (CheckForWin(Grid, out var winner))
        {
            EndGame(winner);
        }
    }

    private void PlaceMarker(int teamIndex, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > Grid.Length)
        {
            return;
        }

        var zOffset = teamIndex == 0 ? 0 : Player2ZOffset;
        var obj = GameObject.Instantiate(teamIndex == 0 ? Player1 : Player2, GameObjects);
        Grid[slotIndex] = teamIndex + 1;
        var transformPosition = Placements[slotIndex].position;
        var targetPosition = new Vector3(transformPosition.x, transformPosition.y + 1, transformPosition.z + zOffset);
        obj.transform.position = targetPosition;
        playerTurn = (++playerTurn) % 2;
    }

    public void ResetGame()
    {
        var toDelete = new List<GameObject>();
        for (var i = 0; i < GameObjects.childCount; ++i)
        {
            toDelete.Add(GameObjects.GetChild(i).transform.gameObject);
        }

        foreach (var delete in toDelete)
        {
            DestroyImmediate(delete);
        }

        for (var i = 0; i < 9; ++i)
            this.Grid[i] = 0;

        this.players.Clear();
        this.playerTurn = 0;
        this.state = TavernGameState.None;
        this.helpText.text = "!tictactoe\r\n" + infoTextColor + infoTextSize + "START GAME";
        UpdateLeaderBoard();
        //this.RedTeamWins.SetActive(false);
        //this.BlueTeamWins.SetActive(false);
        //this.NoTeamWins.SetActive(false);
        //this.ResetText.SetActive(false);
    }

    public bool CheckForWin(int[] grid, out int playerIndex)
    {
        playerIndex = -1;

        if (grid == null || grid.Length != 9) return false;

        var gridFilled = true;

        // horizontal
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (row, col) => row * 3 + col))
        {
            return true;
        }

        // horizontal test
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (row, col) => col * 3 + row))
        {
            return true;
        }

        // diagonal test
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (dir, i) => dir == 0 ? i * 3 + i : 2 + 2 * i, 2))
        {
            return true;
        }

        if (gridFilled)
        {
            playerIndex = 0;
            return true;
        }

        return false;
    }

    private static bool CheckLines(
        int[] grid,
        ref int playerIndex,
        ref bool gridFilled,
        Func<int, int, int> getIndex,
        int aLength = 3,
        int bLength = 3)
    {
        for (var a = 0; a < aLength; ++a)
        {
            var lastPlayer = 0;
            var playerWinRow = true;
            for (var b = 0; b < bLength; ++b)
            {
                var index = getIndex(a, b);
                if (grid[index] == 0) gridFilled = false;
                if (b == 0) lastPlayer = grid[index];
                else if (grid[index] != lastPlayer)
                {
                    playerWinRow = false;
                }
            }

            if (playerWinRow && lastPlayer > 0)
            {
                playerIndex = lastPlayer;
                return true;
            }
        }
        return false;
    }

    private void EndGame(int result)
    {
        if (result == 0)
        {
            gameManager.RavenBot.Announce(Localization.MSG_TICTACTOE_END_DRAW);
        }
        else
        {
            var winningTeam = teamNames[result - 1];
            gameManager.RavenBot.Announce(Localization.MSG_TICTACTOE_WIN, winningTeam);
            RewardWinningTeam(result - 1);
        }

        UpdateWinBoard(result);

        this.state = TavernGameState.GameOver;
        this.helpText.text = "!tictactoe reset\r\n" + infoTextColor + infoTextSize + "END GAME";
    }

    private async void RewardWinningTeam(int teamIndex)
    {
        var winners = GetTeamPlayers()[teamIndex];
        var tokenName = gameManager.RavenNest.TwitchDisplayName + " Token";
        if (!RewardStreamerTokenOnWin) return;
        foreach (var winner in winners)
        {
            var player = gameManager.Players.GetPlayerByUserId(winner);
            if (player == null)
                continue;

            if (UnityEngine.Random.value > tokenWinChance)
                continue;

            var result = await gameManager.RavenNest.Players.AddTokensAsync(player.UserId, 1);
            if (result)
            {
                player.Inventory.AddStreamerTokens(1);
                gameManager.RavenBot.SendMessage(player.Name, Localization.MSG_TICTACTOE_WON_TOKEN, tokenName);
            }
        }
    }

    private void UpdateTeamListing()
    {
        if (state == TavernGameState.Playing)
        {
            boardTitle.text = "<color=" + teamColors[playerTurn] + ">Team " + teamNames[playerTurn] + "s turn";
        }
        else
        {
            boardTitle.text = "Players";
        }

        boardText.text = "";
        var teamPlayers = GetTeamPlayers();

        foreach (var team in teamPlayers)
        {
            var teamName = teamNames[team.Key];
            var teamColor = teamColors[team.Key];
            foreach (var userId in team.Value)
            {
                var player = gameManager.Players.GetPlayerByUserId(userId);
                if (player == null)
                {
                    boardText.text += "<color=" + teamColor + ">" + userId + " (DC)\r\n";
                }
                else
                {
                    boardText.text += "<color=" + teamColor + ">" + player.Name + "\r\n";
                }
            }
        }
    }

    private void UpdateWinBoard(int result)
    {
        var teamIndex = result - 1;
        boardTitle.text = "Result";
        boardText.text = result == 0 ? "Game ended in a DRAW" : "<color=" + teamColors[teamIndex] + ">" + teamNames[teamIndex] + " wins!\r\n";

        if (teamIndex == -1) return;
        var teamPlayers = GetTeamPlayers();
        var team = teamPlayers[teamIndex];
        var teamColor = teamColors[teamIndex];
        foreach (var userId in team)
        {
            TavernGameHighscoreItem item;
            var player = gameManager.Players.GetPlayerByUserId(userId);
            if (player == null)
            {
                boardText.text += "<color=" + teamColor + ">" + userId + " (DC)\r\n";
                item = highscore.Get(userId);
            }
            else
            {
                boardText.text += "<color=" + teamColor + ">" + player.Name + "\r\n";
                item = highscore.Get(userId, player.Name);
            }
            ++item.Score;
        }
        highscore.Save();
    }

    private void UpdateLeaderBoard()
    {
        boardTitle.text = "Leaderboard";
        boardText.text = "";
        var placement = 1;
        foreach (var item in highscore.GetTop(10))
        {
            var color = placement == 1 ? highscoreTopPlayerColor : "";
            var number = placement + ". ";
            var name = (item.UserName ?? item.UserId) + " ";
            var score = item.Score + " wins";
            boardText.text += color + number + name + score + "\r\n";
            ++placement;
        }
    }

    private void RejectPlay(PlayerController player, string message)
    {
        gameManager.RavenBot.SendMessage(player.Name, message);
        Shinobytes.Debug.Log(message);
    }

    private Dictionary<int, List<string>> GetTeamPlayers()
    {
        var teamPlayers = new Dictionary<int, List<string>>();
        for (var i = 0; i < players.Count; ++i)
        {
            var team = i % 2;
            if (!teamPlayers.TryGetValue(team, out var list))
            {
                teamPlayers[team] = list = new List<string>();
            }

            list.Add(players[i]);
        }

        return teamPlayers;
    }
}

public enum GameResult : int
{
    Blue = 0,
    Red = 1,
    Draw = 2
}