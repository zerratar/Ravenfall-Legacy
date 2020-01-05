using System;
using System.Linq;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private ItemDropHandler itemDropHandler;
    [SerializeField] private Transform startingPoint;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private int itemRewardCount = 1;

    public string Name;

    private DungeonRoomController[] rooms;
    private DungeonRoomController currentRoom;

    private int currentRoomIndex = 0;
    private int bossCombatLevel;

    public Vector3 BossSpawnPoint => bossSpawnPoint.position;
    public Vector3 StartingPoint => startingPoint.position;
    public DungeonRoomController Room => currentRoom;
    public ItemDropHandler ItemDrops => itemDropHandler;
    public DungeonRoomController BossRoom => rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Boss);


    // Start is called before the first frame update
    void Start()
    {
        if (!itemDropHandler) itemDropHandler = GetComponent<ItemDropHandler>();
        if (!dungeonManager) dungeonManager = FindObjectOfType<DungeonManager>();

        rooms = GetComponentsInChildren<DungeonRoomController>();
        currentRoom = rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Start);
        currentRoomIndex = Array.IndexOf(rooms, currentRoom);
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
        if (!currentRoom)
        {
            Debug.LogError("No starting room was found!");
            return;
        }

        bossCombatLevel = BossRoom.Boss.Enemy.Stats.CombatLevel;
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
    }

    private void Exit()
    {
        var players = dungeonManager.GetPlayers();
        foreach (var player in players)
        {
            player.Dungeon.OnExit();
        }

        dungeonManager.EndDungeonSuccess();
    }

    internal void RewardPlayer(PlayerController player)
    {
        if (!ItemDrops) return;

        var exp = GameMath.CombatExperience(bossCombatLevel / 5);
        var yieldExp = exp / 2m;

        player.AddExp(yieldExp, Skill.Slayer);

        if (!player.AddExpToCurrentSkill(yieldExp))
            player.AddExp(yieldExp, Skill.Slayer);

        for (var i = 0; i < itemRewardCount; ++i)
            ItemDrops.DropItem(player, true);
    }
}