using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TownHousePlayerAssignDialog : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TownHouseManager townHouseManager;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Transform playerListContent;

    [SerializeField] private UnityEngine.UI.Image ownerLogoImage;
    [SerializeField] private GameObject noOwnerAssigned;
    [SerializeField] private GameObject ownerLogoLoading;
    [SerializeField] private GameObject ownerDisconnected;

    [SerializeField] private TMPro.TextMeshProUGUI lblBuildingName;
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayerName;

    private PlayerController selectedPlayer;
    private TownHouseController townHouse;
    private int lastPlayerCount;

    private List<AssignPlayerRow> instantiatedPlayerRows = new List<AssignPlayerRow>();

    public PlayerController SelectedPlayer
    {
        get => selectedPlayer;
        set => SetSelectedPlayer(value);
    }

    private void Update()
    {
        if (Time.frameCount % 10 == 0)
        {
            var players = gameManager.Players.GetAllPlayers();
            if (lastPlayerCount != players.Count)
            {
                UpdatePlayerList();
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show(TownHouseController townHouseController)
    {
        townHouse = townHouseController;
        gameObject.SetActive(true);
        lblBuildingName.text = townHouse.TownHouse.Name;
        lblPlayerName.text = townHouseController.Slot.PlayerName;

        UpdatePlayerList();
        RebuildTags();
    }

    public void UpdatePlayerList()
    {
        var players = gameManager.Players.GetAllPlayers();

        lastPlayerCount = players.Count;

        SetPlayerLogo(townHouse.Slot.OwnerUserId);

        //var playerRows = EnsureAndGetAssingPlayerRows(lastPlayerCount);

        EnsureAssingPlayerRows(lastPlayerCount);

        foreach (var row in instantiatedPlayerRows)
        {
            row.gameObject.SetActive(false);
        }

        var targetPlayers = players.OrderByDescending(x => GetExpBonus(x, townHouse)).ToArray();
        for (int i = 0; i < targetPlayers.Length; i++)
        {
            var player = targetPlayers[i];
            if (i >= instantiatedPlayerRows.Count)
            {
                UnityEngine.Debug.LogError("Unable to assign all player slot rows properly.");
                break;
            }

            var playerRow = this.instantiatedPlayerRows[i++];

            //var go = Instantiate(playerRowPrefab, playerListContent);
            //var playerRow = go.GetComponent<AssignPlayerRow>();

            if (townHouse.Slot.OwnerUserId == player.UserId)
            {
                playerRow.gameObject.SetActive(false);
            }
            else
            {
                playerRow.gameObject.SetActive(true);
                playerRow.SetPlayer(
                    player,
                    townHouseManager.IsHouseOwner(player),
                    townHouse);
                UnityEngine.Debug.Log("Set Player Row with player: " + player.Name);
            }
        }
    }

    public void EnsureAssingPlayerRows(int count)
    {
        var result = new List<AssignPlayerRow>();
        if (count < this.instantiatedPlayerRows.Count)
        {
            return;
        }
        var delta = count - this.instantiatedPlayerRows.Count;
        for (var i = 0; i < delta; ++i)
        {
            var go = Instantiate(playerRowPrefab, playerListContent);
            var playerRow = go.GetComponent<AssignPlayerRow>();
            instantiatedPlayerRows.Add(playerRow);
        }
    }
    private IReadOnlyList<AssignPlayerRow> EnsureAndGetAssingPlayerRows(int count)
    {
        if (count <= 0) { return new AssignPlayerRow[0]; }
        var result = new List<AssignPlayerRow>();
        var delta = this.instantiatedPlayerRows.Count - count;

        if (delta >= 0)
        {
            for (var i = 0; i < instantiatedPlayerRows.Count; ++i)
            {
                if (i >= count)
                {
                    instantiatedPlayerRows[i].gameObject.SetActive(false);
                }
                else
                {
                    instantiatedPlayerRows[i].gameObject.SetActive(true);
                    result.Add(instantiatedPlayerRows[i]);
                }
            }
        }
        else
        {
            for (var i = 0; i < count; ++i)
            {
                if (i < instantiatedPlayerRows.Count)
                {
                    instantiatedPlayerRows[i].gameObject.SetActive(true);
                    result.Add(instantiatedPlayerRows[i]);
                }
                else
                {
                    var go = Instantiate(playerRowPrefab, playerListContent);
                    var playerRow = go.GetComponent<AssignPlayerRow>();
                    instantiatedPlayerRows.Add(playerRow);
                    result.Add(playerRow);
                }
            }
        }

        return result;
    }

    public float GetExpBonus(PlayerController player, TownHouseController dialogHouse)
    {
        var existingBonus = 0;
        if (dialogHouse.Slot.PlayerSkills != null)
        {
            var existingSkill = GameMath.GetSkillByHouseType(dialogHouse.Slot.PlayerSkills, dialogHouse.TownHouse.Type);
            existingBonus = (int)GameMath.CalculateHouseExpBonus(existingSkill);
        }

        var skill = GameMath.GetSkillByHouseType(player.Stats, dialogHouse.TownHouse.Type);
        var playerBonus = (int)GameMath.CalculateHouseExpBonus(skill);
        var bonusPlus = playerBonus - existingBonus;

        return bonusPlus;
    }

    private void SetPlayerLogo(string userId)
    {
        var hasOwner = !string.IsNullOrEmpty(userId);
        noOwnerAssigned.SetActive(!hasOwner);
        ownerLogoLoading.SetActive(false);
        ownerLogoImage.gameObject.SetActive(false);
        if (hasOwner)
        {
            ownerLogoLoading.gameObject.SetActive(true);
            gameManager.PlayerLogo.GetLogo(userId, logo =>
            {
                var owner = gameManager.Players.GetPlayerByUserId(userId);
                ownerDisconnected.SetActive(!owner);
                ownerLogoLoading.gameObject.SetActive(false);
                ownerLogoImage.gameObject.SetActive(true);
                ownerLogoImage.color = owner ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                if (logo == null) return;
                ownerLogoImage.sprite = logo;
            });
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!townHouseManager) townHouseManager = gameManager.Village.TownHouses;
    }

    private void SetSelectedPlayer(PlayerController value)
    {
        selectedPlayer = value;
        if (value != null)
        {
            lblPlayerName.text = value.Name;
            SetPlayerLogo(value.UserId);
        }
    }

    private void RebuildTags(Transform parent = null)
    {
        Transform t = parent;
        if (!parent) t = transform;
        for (var i = 0; i < t.childCount; ++i)
        {
            var transform = t.GetChild(i);
            transform.tag = "BuildingDialog";
            RebuildTags(transform);
        }
    }
}