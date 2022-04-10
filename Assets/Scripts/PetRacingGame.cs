using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class PetRacingGame : MonoBehaviour, ITavernGame
{
    [SerializeField] private TextMeshPro helpText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject playersContainer;
    [SerializeField] private GameObject playerStartA;
    [SerializeField] private GameObject playerStartB;
    [SerializeField] private float startTime = 30f;
    [SerializeField] private int maxPlayers = 30;

    private float startTimer;
    private readonly List<PetRacingPlayer> players = new List<PetRacingPlayer>();
    private TavernGameHighscore highscore;

    private TavernGameState state = TavernGameState.None;
    private string infoTextColor = "<color=#444444>";
    private string infoTextSize = "<size=13>";

    public string GameStartCommand => "racing";
    public TavernGameState State => state;
    public bool IsGameOver => State == TavernGameState.GameOver;
    public bool Started => State != TavernGameState.None;
    public bool Playing => State == TavernGameState.Playing;

    private void Start()
    {
        highscore = new TavernGameHighscore("petracing");
    }

    public void Activate()
    {
        this.helpText.text = "!racing\r\n" + infoTextColor + infoTextSize + "JOIN AND PLAY";
        this.state = TavernGameState.WaitingForPlayers;

        DestroyContenderObjects();
    }

    internal void ResetGame()
    {
        players.Clear();
        DestroyContenderObjects();
        helpText.text = "!racing\r\n" + infoTextColor + infoTextSize + "START GAME";
        state = TavernGameState.None;
    }

    internal void EndGame()
    {
        state = TavernGameState.GameOver;
        helpText.text = "!racing reset\r\n" + infoTextColor + infoTextSize + "END GAME";
    }

    internal void Play(PlayerController player)
    {
        if (Playing || IsGameOver)
        {
            return;
        }

        if (this.players.FirstOrDefault(x => x.Player.UserId == player.UserId) != null)
        {
            gameManager.RavenBot.SendMessage(player.Name, "You're already playing.");
            return;
        }

        var pet = player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Pet);
        if (pet == null)
        {
            gameManager.RavenBot.SendMessage(player.Name, "You need to equip a pet to play.");
            return;
        }

        if (string.IsNullOrEmpty(pet.Item.GenericPrefab))
        {
            gameManager.RavenBot.SendMessage(player.Name, "PET IS BROKEN?!?");
            return;
        }

        var petPrefab = Resources.Load<GameObject>(pet.Item.GenericPrefab);
        var petObj = Instantiate(petPrefab, playersContainer.transform);

        petObj.transform.localScale *= 0.1f;
        petObj.name = player.PlayerName;
        petObj.transform.position = GetRandomPosition();

        var contender = petObj.AddComponent<PetRacingContender>();

        this.players.Add(new PetRacingPlayer { Player = player, Pet = petObj, Contender = contender });
        startTimer = startTime;
    }

    private Vector3 GetRandomPosition()
    {
        var a = playerStartA.transform.position;
        var b = playerStartB.transform.position;
        var y = a.y;
        var z = a.z;
        var x = 0f;
        var d = Mathf.Abs(a.x - b.x);
        var minDistancePerPlayer = d / maxPlayers;
        var maxTries = 30;
        var tries = 0;
        do
        {
            if (a.x > b.x)
                x = UnityEngine.Random.Range(b.x, a.x);
            else
                x = UnityEngine.Random.Range(a.x, b.x);
            var shortestDistance = 9999f;
            foreach (var p in players)
            {
                var px = p.Pet.transform.position.x;
                var v = Mathf.Abs(px - x);
                if (v < shortestDistance)
                    shortestDistance = v;
            }
            if (shortestDistance >= minDistancePerPlayer || ++tries > maxTries)
                break;
        } while (true);
        return new Vector3(x, y, z);
    }

    private void DestroyContenderObjects()
    {
        for (var i = 0; i < playersContainer.transform.childCount; ++i)
        {
            var child = playersContainer.transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private void Update()
    {
        if (this.State == TavernGameState.WaitingForPlayers)
        {
            WaitForPlayers();
            return;
        }

        if (Playing)
        {
            UpdateGame();
        }
    }

    private void UpdateGame()
    {
    }

    private void WaitForPlayers()
    {
        if (this.players.Count <= 1)
            return;

        if (startTimer > 0)
        {
            startTimer -= Time.deltaTime;
            if (startTimer <= 0)
            {
                StartGame();
            }
        }
    }

    private void StartGame()
    {
        state = TavernGameState.Playing;
    }

    private class PetRacingPlayer
    {
        public PlayerController Player { get; set; }
        public PetRacingContender Contender { get; set; }
        public GameObject Pet { get; set; }
    }
}
