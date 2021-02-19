using System;
using System.Collections;
using System.Linq;
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
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!itemDropHandler) itemDropHandler = GetComponent<ItemDropHandler>();
        if (!dungeonManager) dungeonManager = FindObjectOfType<DungeonManager>();

        rooms = GetComponentsInChildren<DungeonRoomController>();
        currentRoom = rooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Start);
        currentRoomIndex = Array.IndexOf(rooms, currentRoom);
        dungeonContainer.SetActive(false);
    }

    public void Update()
    {
        if (!background) return;
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
        dungeonContainer.SetActive(true);
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
        HideDungeon(2);
    }

    private IEnumerator HideDungeon(float afterSeconds)
    {
        yield return new WaitForSeconds(afterSeconds);
        if (!dungeonManager.Started)
        {
            dungeonContainer.SetActive(false);
        }
    }

    internal void RewardPlayer(PlayerController player, bool generateMagicAttributes)
    {
        if (!ItemDrops) return;

        var exp = GameMath.CombatExperience(bossCombatLevel / 5);
        var yieldExp = exp / 2m;

        player.AddExp(yieldExp, Skill.Slayer);

        if (!player.AddExpToCurrentSkill(yieldExp))
            player.AddExp(yieldExp, Skill.Slayer);

        var type = generateMagicAttributes ? DropType.MagicRewardGuaranteed : DropType.StandardGuaranteed;
        for (var i = 0; i < itemRewardCount; ++i)
        {
            ItemDrops.DropItem(player, type);
        }
    }
}