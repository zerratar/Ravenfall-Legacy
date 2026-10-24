using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// The pet race on the tavern's table: viewers with a pet equipped join with !race, their pets line up at the start,
/// and once two have joined a countdown runs. Then the pets run for the checkered line, each at its own changing pace
/// with now and then a sprint or a stumble, so anyone can win. The first across wins a point on the race leaderboard;
/// the result is announced and stays on the board for a while before the race clears itself for the next one.
/// The table sits at the back of the tavern, too far off to follow from where the camera comes in, so when a race is
/// about to start the camera glides over to watch from behind the start line, and back again afterwards unless it
/// was moved in the meantime.
/// </summary>
/// <remarks>
/// It was only ever half built: pets joined and stood at the line, but no race was ever run (found 2026-10-24).
/// </remarks>
public class PetRacingGame : MonoBehaviour, ITavernGame
{
    [SerializeField] private TextMeshPro helpText;
    [SerializeField] private TextMeshPro leaderboardTitle;
    [SerializeField] private TextMeshPro leaderboardText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private GameObject playersContainer;
    [SerializeField] private GameObject playerStartA;
    [SerializeField] private GameObject playerStartB;
    [SerializeField] private Transform finishLine;
    [SerializeField] private float startTime = 30f;
    [SerializeField] private int maxPlayers = 30;

    // how long a race takes for a pet running at its usual pace, in seconds
    private const float RaceSeconds = 15f;
    // after the winner, how long the rest have to finish before the race is called
    private const float FinishGraceSeconds = 8f;
    // how long the result stays on the board before the race clears itself
    private const float ResultSeconds = 30f;
    // the pets' size on the table, against their size beside a player
    private const float PetScale = 0.1f;
    // where the camera watches from, against the race (picked by eye in the tavern, 2026-10-24): behind the start
    // line, looking down the track at the finish, with the board's sign above it
    private static readonly Vector3 WatchPosition = new Vector3(4.875f, 2.202f, -2.372f);
    private static readonly Vector3 WatchAngles = new Vector3(39.09f, 181.53f, 0f);
    private const float WatchGlideSeconds = 1.5f;

    private float startTimer;
    private float resultTimer;
    private float raceTime;
    private float winnerTime = -1f;
    private readonly List<PetRacingPlayer> players = new List<PetRacingPlayer>();
    private readonly List<PetRacingPlayer> finished = new List<PetRacingPlayer>();
    private TavernGameHighscore highscore;

    private bool watching;
    private float glide = -1f;
    private Vector3 glideFromPosition, glideToPosition, returnPosition;
    private Quaternion glideFromRotation, glideToRotation, returnRotation;

    private TavernGameState state = TavernGameState.None;
    private string infoTextColor = "<color=#444444>";
    private string infoTextSize = "<size=13>";

    public string GameStartCommand => Localization.PETRACE_START_COMMAND;
    public TavernGameState State => state;
    public bool IsGameOver => State == TavernGameState.GameOver;
    public bool Started => State != TavernGameState.None;
    public bool Playing => State == TavernGameState.Playing;

    private void Start()
    {
        FindReferences();

        // the pets left on the table in the scene are only there to show how it looks
        if (state == TavernGameState.None)
        {
            DestroyContenderObjects();
            ShowIdle();
        }
        UpdateLeaderboard();
    }

    /// <summary>
    /// The race's table is switched off in the scene until the first !race, which activates the race in the same frame
    /// as it is switched on, before Start has run: so everything it needs is looked up here as well.
    /// </summary>
    private void FindReferences()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!gameCamera) gameCamera = FindAnyObjectByType<GameCamera>();
        if (!finishLine) finishLine = transform.Find("Board/Finish");
        highscore ??= new TavernGameHighscore("petracing");
    }

    public void Activate()
    {
        FindReferences();
        players.Clear();
        finished.Clear();
        DestroyContenderObjects();
        startTimer = startTime;
        state = TavernGameState.WaitingForPlayers;
        ShowWaiting();
    }

    internal void ResetGame()
    {
        StopWatching();
        players.Clear();
        finished.Clear();
        DestroyContenderObjects();
        state = TavernGameState.None;
        ShowIdle();
    }

    internal void EndGame()
    {
        state = TavernGameState.GameOver;
        resultTimer = ResultSeconds;
    }

    internal void Play(PlayerController player)
    {
        if (Playing || IsGameOver)
        {
            gameManager.RavenBot.SendReply(player, Localization.PETRACE_RUNNING);
            return;
        }

        if (players.Any(x => x.Player && x.Player.UserId == player.UserId))
        {
            gameManager.RavenBot.SendReply(player, Localization.PETRACE_ALREADY_PLAYING);
            return;
        }

        if (players.Count >= maxPlayers)
        {
            gameManager.RavenBot.SendReply(player, Localization.PETRACE_FULL);
            return;
        }

        var pet = player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Pet);
        if (pet == null)
        {
            gameManager.RavenBot.SendReply(player, Localization.PETRACE_NO_PET);
            return;
        }

        var petPrefab = string.IsNullOrEmpty(pet.Item.GenericPrefab) ? null : Resources.Load<GameObject>(pet.Item.GenericPrefab);
        if (!petPrefab)
        {
            Shinobytes.Debug.LogWarning("Pet race: no model for the pet " + pet.Item.Name + " (" + pet.Item.GenericPrefab + ")");
            gameManager.RavenBot.SendReply(player, Localization.PETRACE_NO_PET);
            return;
        }

        var petObj = Instantiate(petPrefab, playersContainer.transform);
        petObj.transform.localScale *= PetScale;
        petObj.name = player.PlayerName;
        petObj.transform.rotation = Quaternion.LookRotation(RaceDirection(), Vector3.up);
        var contender = petObj.AddComponent<PetRacingContender>();

        players.Add(new PetRacingPlayer
        {
            Player = player,
            Name = player.PlayerName,
            UserId = player.UserId,
            PetName = pet.Item.Name,
            Pet = petObj,
            Contender = contender,
            Animator = petObj.GetComponentInChildren<Animator>(),
            Seed = UnityEngine.Random.value * 100f,
            Pace = UnityEngine.Random.Range(0.92f, 1.08f),
        });

        LineUp();

        // the countdown starts once there is someone to race against, and the camera comes over to watch
        if (players.Count == 2)
        {
            startTimer = startTime;
            Watch();
        }
        gameManager.RavenBot.SendReply(player, Localization.PETRACE_JOINED, pet.Item.Name);
        ShowWaiting();
    }

    /// <summary>The way the pets run: from the start line to the finish, flat on the table.</summary>
    private Vector3 RaceDirection()
    {
        var start = (playerStartA.transform.position + playerStartB.transform.position) * 0.5f;
        var end = finishLine ? finishLine.position : start + transform.right;
        var d = end - start;
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.right;
    }

    private float RaceLength()
    {
        var start = (playerStartA.transform.position + playerStartB.transform.position) * 0.5f;
        if (!finishLine) return 1.5f;
        return Mathf.Max(0.1f, Vector3.Dot(finishLine.position - start, RaceDirection()));
    }

    /// <summary>Spreads the pets evenly across the start line, in the order they joined.</summary>
    private void LineUp()
    {
        var a = playerStartA.transform.position;
        var b = playerStartB.transform.position;
        var count = players.Count;
        for (var i = 0; i < count; ++i)
        {
            var t = (i + 1f) / (count + 1f);
            var p = players[i];
            p.Start = Vector3.Lerp(a, b, t);
            p.Distance = 0f;
            if (p.Pet) p.Pet.transform.position = p.Start;
        }
    }

    private void DestroyContenderObjects()
    {
        for (var i = playersContainer.transform.childCount - 1; i >= 0; --i)
        {
            Destroy(playersContainer.transform.GetChild(i).gameObject);
        }
    }

    /// <summary>Glides the tavern camera over to watch the race, remembering where it was.</summary>
    private void Watch()
    {
        if (watching || !gameCamera) return;
        var cam = gameCamera.transform;
        returnPosition = cam.position;
        returnRotation = cam.rotation;
        GlideTo(transform.TransformPoint(WatchPosition), transform.rotation * Quaternion.Euler(WatchAngles));
        watching = true;
    }

    /// <summary>Takes the camera back to where it was before the race, unless it has been moved since.</summary>
    private void StopWatching()
    {
        if (!watching) return;
        watching = false;
        if (!gameCamera) return;
        var moved = (gameCamera.transform.position - transform.TransformPoint(WatchPosition)).sqrMagnitude > 0.01f;
        if (moved) glide = -1f;
        else GlideTo(returnPosition, returnRotation);
    }

    private void GlideTo(Vector3 position, Quaternion rotation)
    {
        var cam = gameCamera.transform;
        glideFromPosition = cam.position;
        glideFromRotation = cam.rotation;
        glideToPosition = position;
        glideToRotation = rotation;
        glide = 0f;
    }

    private void Glide()
    {
        if (glide < 0f || !gameCamera) return;
        glide = Mathf.Min(1f, glide + Time.unscaledDeltaTime / WatchGlideSeconds);
        var t = glide * glide * (3f - 2f * glide);
        gameCamera.transform.SetPositionAndRotation(Vector3.Lerp(glideFromPosition, glideToPosition, t), Quaternion.Slerp(glideFromRotation, glideToRotation, t));
        if (glide >= 1f) glide = -1f;
    }

    private void Update()
    {
        Glide();
        switch (state)
        {
            case TavernGameState.WaitingForPlayers:
                WaitForPlayers();
                break;
            case TavernGameState.Playing:
                UpdateGame();
                break;
            case TavernGameState.GameOver:
                resultTimer -= GameTime.deltaTime;
                if (resultTimer <= 0f) ResetGame();
                break;
        }
    }

    private void WaitForPlayers()
    {
        // pets of players who left are taken off the line
        if (players.RemoveAll(x => !x.Player || !x.Pet) > 0)
        {
            DestroyLeftovers();
            LineUp();
            ShowWaiting();
        }

        if (players.Count <= 1)
            return;

        var before = Mathf.CeilToInt(startTimer);
        startTimer -= GameTime.deltaTime;
        if (startTimer <= 0f)
        {
            StartGame();
            return;
        }

        if (Mathf.CeilToInt(startTimer) != before) ShowWaiting();
    }

    private void StartGame()
    {
        state = TavernGameState.Playing;
        raceTime = 0f;
        winnerTime = -1f;
        finished.Clear();
        LineUp();
        gameManager.RavenBot.Announce(Localization.PETRACE_STARTED, players.Count);
        helpText.text = "THEY'RE OFF!\r\n" + infoTextColor + infoTextSize + players.Count + " PETS";
    }

    private void UpdateGame()
    {
        var dt = GameTime.deltaTime;
        raceTime += dt;
        var length = RaceLength();
        var direction = RaceDirection();
        var usual = length / RaceSeconds;

        foreach (var p in players)
        {
            if (p.Finished || !p.Pet) continue;

            // a pace that drifts up and down, its own for each pet, with now and then a sprint or a stumble
            var drift = Mathf.PerlinNoise(raceTime * 0.7f, p.Seed) * 0.9f + 0.55f;
            if (p.BurstLeft > 0f)
            {
                p.BurstLeft -= dt;
            }
            else if (UnityEngine.Random.value < dt * 0.25f)
            {
                p.BurstLeft = UnityEngine.Random.Range(0.6f, 1.4f);
                p.Burst = UnityEngine.Random.value < 0.6f ? 1.9f : 0.15f;
            }
            var speed = usual * p.Pace * drift * (p.BurstLeft > 0f ? p.Burst : 1f);

            p.Distance = Mathf.Min(length, p.Distance + speed * dt);
            p.Pet.transform.position = p.Start + direction * p.Distance;
            SetRunning(p, speed / usual);

            if (p.Distance >= length)
            {
                p.Finished = true;
                p.Time = raceTime;
                finished.Add(p);
                SetRunning(p, 0f);
                if (finished.Count == 1)
                {
                    winnerTime = raceTime;
                    gameManager.RavenBot.Announce(Localization.PETRACE_WIN, p.Name, p.PetName);
                }
            }
        }

        if (finished.Count >= players.Count || (winnerTime >= 0f && raceTime - winnerTime >= FinishGraceSeconds))
        {
            Finish();
        }
    }

    private static void SetRunning(PetRacingPlayer p, float pace)
    {
        if (!p.Animator) return;
        p.Animator.SetFloat("Run", pace > 0.05f ? 1f : 0f);
        p.Animator.SetFloat("Walk", pace > 0.05f ? 1f : 0f);
        p.Animator.speed = pace > 0.05f ? Mathf.Clamp(pace, 0.6f, 2f) : 1f;
    }

    private void Finish()
    {
        // the ones still running are placed by how far they got
        foreach (var p in players.Where(x => !x.Finished).OrderByDescending(x => x.Distance))
        {
            finished.Add(p);
            SetRunning(p, 0f);
        }

        if (finished.Count > 0)
        {
            var winner = finished[0];
            highscore.Load();
            var item = highscore.Get(winner.UserId, winner.Name);
            ++item.Score;
            highscore.Save();
            UpdateLeaderboard();
        }

        helpText.text = (finished.Count > 0 ? finished[0].Name + " WINS!" : "RACE OVER") + "\r\n" + infoTextColor + infoTextSize
            + (finished.Count > 1 ? "2ND " + finished[1].Name : "");
        EndGame();
    }

    private void DestroyLeftovers()
    {
        for (var i = playersContainer.transform.childCount - 1; i >= 0; --i)
        {
            var child = playersContainer.transform.GetChild(i).gameObject;
            if (!players.Any(x => x.Pet == child)) Destroy(child);
        }
    }

    /// <summary>The race leaderboard on the wall: the ten pets' owners with the most wins.</summary>
    private void UpdateLeaderboard()
    {
        if (!leaderboardText) return;
        highscore ??= new TavernGameHighscore("petracing");
        highscore.Load();
        if (leaderboardTitle) leaderboardTitle.text = "Race wins";
        var text = "";
        var placement = 1;
        foreach (var item in highscore.GetTop(10))
        {
            text += placement + ". " + item.UserName + " " + item.Score + (item.Score == 1 ? " win" : " wins") + "\r\n";
            ++placement;
        }
        leaderboardText.text = placement == 1 ? "No races won yet" : text;
    }

    private void ShowIdle()
    {
        if (!helpText) return;
        helpText.text = "!" + Localization.PETRACE_START_COMMAND + "\r\n" + infoTextColor + infoTextSize + "PET RACE";
    }

    private void ShowWaiting()
    {
        if (!helpText) return;
        var line = players.Count <= 1
            ? "NEEDS ONE MORE PET"
            : players.Count + " PETS - " + Mathf.Max(0, Mathf.CeilToInt(startTimer)) + "s";
        helpText.text = "!" + Localization.PETRACE_START_COMMAND + "\r\n" + infoTextColor + infoTextSize + line;
    }

    private class PetRacingPlayer
    {
        public PlayerController Player { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public string PetName { get; set; }
        public PetRacingContender Contender { get; set; }
        public GameObject Pet { get; set; }
        public Animator Animator { get; set; }
        public Vector3 Start { get; set; }
        public float Distance { get; set; }
        public float Seed { get; set; }
        public float Pace { get; set; }
        public float Burst { get; set; }
        public float BurstLeft { get; set; }
        public bool Finished { get; set; }
        public float Time { get; set; }
    }
}
