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

    public PlayerController SelectedPlayer
    {
        get => selectedPlayer;
        set => SetSelectedPlayer(value);
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
        lblPlayerName.text = townHouseController.Owner?.Name;

        UpdatePlayerList();
        RebuildTags();
    }

    public void UpdatePlayerList()
    {
        var players = gameManager.Players.GetAllPlayers();

        SetPlayerLogo(townHouse.Owner?.UserId);

        for (var i = 0; i < playerListContent.childCount; ++i)
        {
            Destroy(playerListContent.GetChild(i).gameObject);
        }

        foreach (var player in players)
        {
            if (townHouse.Owner && townHouse.Owner.UserId == player.UserId)
                continue;

            var go = Instantiate(playerRowPrefab, playerListContent);
            var playerRow = go.GetComponent<AssignPlayerRow>();

            playerRow.SetPlayer(
                player,
                townHouseManager.IsHouseOwner(player),
                townHouse);
        }
    }

    private void SetPlayerLogo(string userId)
    {
        Debug.Log($"TownHousePlayerAssignDialog::SetPlayerLogo({userId})");

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