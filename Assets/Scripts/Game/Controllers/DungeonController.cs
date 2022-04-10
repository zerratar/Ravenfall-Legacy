using Shinobytes.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] private GameObject dungeonContainer;
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ItemDropHandler itemDropHandler;
    [SerializeField] private Transform startingPoint;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private int itemRewardCount = 1;
    [SerializeField] private GameObject background;

    public string Name;

    public int Level = 1;
    public DungeonTier Tier;
    public DungeonDifficulity Difficulity;

    [Range(0.01f, 1f)]
    public float SpawnRate = 0.5f;

    private DungeonRoomController[] rooms;
    private DungeonRoomController currentRoom;

    private int currentRoomIndex = 0;
    private int bossCombatLevel;

    public Vector3 BossSpawnPoint => bossSpawnPoint.position;
    public Vector3 StartingPoint => startingPoint.position;

    public ItemDropHandler ItemDrops => itemDropHandler;

    public DungeonRoomController BossRoom => rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Boss);
    public DungeonRoomController Room => currentRoom;

    // Start is called before the first frame update
    public bool HasPredefinedRooms => (rooms != null && rooms.Length > 0) || (!Application.isPlaying && (rooms = GetComponentsInChildren<DungeonRoomController>()).Length > 0);

    public Transform CameraPoint => activeCameraPoint;

    private Transform activeCameraPoint;
    private float lastCameraChange;
    private Transform[] cameraTargets;

    private bool UsePlayerCamera = true;

    public bool HasStartingPoint => !!startingPoint;

    void Start()
    {
        itemDropHandler = GetComponent<ItemDropHandler>();

        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!dungeonManager) dungeonManager = FindObjectOfType<DungeonManager>();

        rooms = GetComponentsInChildren<DungeonRoomController>();

        if (rooms.Length > 0)
        {
            currentRoom = rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Start);
            currentRoomIndex = Array.IndexOf(rooms, currentRoom);
        }

        if (dungeonContainer)
        {
            dungeonContainer.SetActive(false);
        }
    }

    public void Update()
    {
        if (this.dungeonManager.Dungeon != this || !this.dungeonManager.Started)
        {
            return;
        }

        if (background)
        {
            if (gameManager.PotatoMode)
            {
                if (background.activeSelf)
                {
                    background.SetActive(false);
                }
            }
            else if (!background.activeSelf)
            {
                background.SetActive(true);
            }
        }

        if (HasPredefinedRooms)
        {
            this.activeCameraPoint = Room.CameraPoint;
        }
        else
        {

            if (this.dungeonManager.Started && (!dungeonManager.Boss || dungeonManager.Boss.Enemy.Stats.IsDead))
            {
                Exit();
                return;
            }

            if (UsePlayerCamera && Input.GetKeyDown(KeyCode.Tab))
            {
                ObserveNextPlayer();
                this.lastCameraChange = Time.time;
                return;
            }

            // DO NOT DO THIS EVERY UPDATE!!!!!!
            if (Time.time - lastCameraChange >= 10)
            {

                // 1. Use the center room emitter to place target camera transform
                // 2. Find out which camera transforms/rooms that has players in them
                // 3. Toggle between these transforms every 5s

                // var players = dungeonManager.GetPlayers();
                // get the room that has most players closest to it.
                // means, we have to weight the room with most players with smallest distance numbers.
                // First, get all rooms. Sort the list by distance  to each room.
                // compare each room where same player or player index has smaller distance than the other one.
                // take the room with most players with shorterst distance.
                // voila. Active Camera Point Found.

                // this is low though, better alternatives?
                // first player joined that is not dead / room?

                if (UsePlayerCamera)
                {
                    ObserveNextPlayer();
                }
                else
                {
                    if (cameraTargets != null && cameraTargets.Length > 0)
                    {
                        var players = dungeonManager.GetAlivePlayers();
                        if (players.Count > 0)
                        {

                            var maxDistance = float.MaxValue;
                            this.activeCameraPoint = null;

                            foreach (var cam in cameraTargets)
                            {
                                // get average distance to camera target
                                var avgDistance = players.Sum(x => Vector3.Distance(cam.position, x.transform.position)) / players.Count;
                                if (avgDistance < maxDistance)
                                {
                                    maxDistance = avgDistance;
                                    this.activeCameraPoint = cam;
                                }
                            }
                        }
                        else
                        {
                            this.activeCameraPoint = cameraTargets.FirstOrDefault();
                        }
                    }
                    else
                    {
                        this.activeCameraPoint = this.transform;
                    }
                }

                this.lastCameraChange = Time.time;
            }
        }
    }

    private void ObserveNextPlayer()
    {
        if (!dungeonManager || dungeonManager == null)
        {
            return;
        }

        try
        {
            var players = dungeonManager.GetAlivePlayers();
            if (players != null && players.Count > 0)
            {
                this.activeCameraPoint = players.Random().transform;
            }
            else
            {
                this.activeCameraPoint = dungeonManager.Boss?.transform;
            }
        }
        catch
        {
            // ignored
        }
    }

    public void ResetRooms()
    {
        currentRoomIndex = 0;
        foreach (var room in rooms)
        {
            room.ResetRoom();
        }
    }

    public void NextRoom()
    {
        currentRoom.Exit();
        currentRoomIndex = (currentRoomIndex + 1) % rooms.Length;
        if (currentRoomIndex == 0)
        {
            Exit();
            return;
        }

        currentRoom = rooms[currentRoomIndex];
        currentRoom.Enter();
    }

    public void Enter()
    {

        if (this.HasPredefinedRooms && !currentRoom)
        {
            Shinobytes.Debug.LogError("No starting room was found!");
            return;
        }

        if (HasPredefinedRooms && dungeonContainer)
        {
            dungeonContainer.SetActive(true);
        }

        bossCombatLevel = this.dungeonManager.Boss.Enemy.Stats.CombatLevel;


        if (!startingPoint)
        {
            startingPoint = GetComponentInChildren<DungeonStartingPoint>()?.transform;
        }

        TeleportPlayers();

        if (this.HasPredefinedRooms)
        {
            currentRoom.Enter();
            NextRoom(); // Skip the starting room so we can get going!
        }
        else
        {
            this.cameraTargets = GetComponentsInChildren<RoomCenter>().SelectArray(x => x.transform);
            // This is an automatic generated dungeon. No need to do anything atm?
        }
    }

    private void TeleportPlayers()
    {
        var joinedPlayers = dungeonManager.GetPlayers();
        foreach (var player in joinedPlayers)
        {
            player.Dungeon.OnEnter();
        }

        gameManager.Ferry.AssignBestCaptain();
    }

    private void Exit()
    {
        var players = dungeonManager.GetPlayers();
        foreach (var player in players)
        {
            player.Dungeon.OnExit();
        }

        dungeonManager.EndDungeonSuccess();
        HideDungeon(2);
    }

    public IEnumerator HideDungeon(float afterSeconds)
    {
        yield return new WaitForSeconds(afterSeconds);
        if (!dungeonManager.Started && dungeonContainer)
        {
            dungeonContainer.SetActive(false);
        }
    }

    public void AddExperienceReward(PlayerController player)
    {
        var factor = Math.Min(50, Math.Max(bossCombatLevel / player.Stats.CombatLevel, 10d));
        player.AddExp(Skill.Slayer, factor);
        player.AddExp(Math.Max(5, factor * 0.5));
    }

    public void RewardItemDrops(IReadOnlyList<PlayerController> joinedPlayers)
    {
        if (!ItemDrops) return;
        var collection = ItemDrops.DropItems(joinedPlayers, DropType.Guaranteed);
        gameManager.RavenBot.Announce("Victorious!! The dungeon boss was slain and yielded " + collection.Count + " item treasures!");
        foreach (var msg in collection.Messages)
        {
            gameManager.RavenBot.Announce(msg);
        }
    }

    internal void DisableContainer()
    {
        dungeonContainer.SetActive(false);
    }
    internal void EnableContainer()
    {
        dungeonContainer.SetActive(true);
        if (rooms == null || rooms.Length == 0)
        {
            return;
        }

        foreach (var room in rooms)
        {
            room.ResetRoom();
        }
    }

}