using RavenNest.Models;
using Shinobytes.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] private GameObject dungeonContainer;
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private GameManager gameManager;
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

    [Range(1f, 10f)] public float MobsDifficultyScale = 1f;
    [Range(1f, 10f)] public float BossCombatScale = 1f;
    [Range(1f, 10f)] public float BossHealthScale = 1f;

    private DungeonRoomController[] rooms;
    private DungeonRoomController currentRoom;

    private int currentRoomIndex = 0;
    private int bossCombatLevel;

    public Vector3 BossSpawnPoint => bossSpawnPoint.position;
    public Vector3 StartingPoint => startingPoint.position;

    public DungeonRoomController BossRoom => Rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Boss);
    public DungeonRoomController Room => currentRoom;
    public DungeonRoomController[] Rooms => ((rooms == null || rooms.Length == 0) ? (rooms = GetRooms()) : rooms);

    public DungeonManager DungeonManager => dungeonManager;
    public Transform CameraPoint => activeCameraPoint;

    private Transform activeCameraPoint;
    public bool HasStartingPoint => !!startingPoint;
    void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!dungeonManager) dungeonManager = FindAnyObjectByType<DungeonManager>();

        rooms = GetRooms();

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

        if (currentRoom && currentRoom.RoomType == DungeonRoomType.Boss && dungeonManager.Boss)
        {
            this.activeCameraPoint = dungeonManager.Boss?.transform;
        }
        else
        {
            this.activeCameraPoint = Room.CameraPoint;
        }

        if (this.dungeonManager.Started && (!dungeonManager.Boss || dungeonManager.Boss.Enemy.Stats.IsDead))
        {
            Exit();
            return;
        }
    }

    private DungeonRoomController[] GetRooms()
    {
        var r = GetComponentsInChildren<DungeonRoomController>(true);
        if (r.Length > 0)
        {
            currentRoom = r.FirstOrDefault(x => x.RoomType == DungeonRoomType.Start);
            currentRoomIndex = Array.IndexOf(r, currentRoom);
        }
        return r;
    }

    public void ResetRooms()
    {
        currentRoomIndex = 0;
        foreach (var room in Rooms)
        {
            room.ResetRoom();
        }
    }

    public int CurrentRoomIndex => currentRoomIndex;

    public void NextRoom()
    {
        currentRoom.Exit();
        currentRoomIndex = (currentRoomIndex + 1) % Rooms.Length;
        if (currentRoomIndex == 0)
        {
            Exit();
            return;
        }

        currentRoom = Rooms[currentRoomIndex];
        currentRoom.Enter();
    }

    public void Enter()
    {

        if (!currentRoom)
        {
            Shinobytes.Debug.LogError("No starting room was found!");
            return;
        }

        if (dungeonContainer)
        {
            dungeonContainer.SetActive(true);
        }

        bossCombatLevel = this.dungeonManager.Boss.Enemy.Stats.CombatLevel;


        if (!startingPoint)
        {
            startingPoint = GetComponentInChildren<DungeonStartingPoint>()?.transform;
        }

        TeleportPlayers();

        currentRoom.Enter();
        NextRoom(); // Skip the starting room so we can get going!
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
        var slayerFactor = Mathf.Max(bossCombatLevel / 200, 70) * 0.125 * Math.Max(1, gameManager.SessionSettings.DungeonExpFactor);
        player.AddExp(Skill.Slayer, slayerFactor);

        var skillFactor = Math.Max(5, slayerFactor * 0.5);
        if (player.Dungeon.Ferry.HasReturned && !player.Dungeon.Ferry.HasDestination)
        {
            player.AddExp(Skill.Sailing, skillFactor);
            return;
        }

        player.AddExp(skillFactor);
    }



    internal void DisableContainer()
    {
        dungeonContainer.SetActive(false);
    }
    internal void EnableContainer()
    {
        dungeonContainer.SetActive(true);
        if (Rooms == null || Rooms.Length == 0)
        {
            return;
        }

        foreach (var room in Rooms)
        {
            room.ResetRoom();
        }
    }

}