using System;
using UnityEngine;

public class BuildSlotIconsManager : MonoBehaviour
{
    [SerializeField] private float yOffset = 3f;

    [SerializeField] private UIButton buttonBuildHouse;
    [SerializeField] private UIButton buttonEraseHouse;
    [SerializeField] private UIButton buttonSwitchPlayer;
    [SerializeField] private UIButton buttonAssignPlayer;

    [SerializeField] private GameObject ownerLogoContainer;
    [SerializeField] private UnityEngine.UI.Image ownerLogoImage;
    [SerializeField] private GameObject ownerLogoLoading;
    [SerializeField] private GameObject ownerDisconnected;

    [SerializeField] private TownHouseSelectionDialog selectBuildingDialog;
    [SerializeField] private TownHousePlayerAssignDialog playerAssignDialog;
    [SerializeField] private GameManager gameManager;

    private TownHouseSlot activeSlot;

    private void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        Hide();
    }

    public void Show(TownHouseSlot slot)
    {
        if (slot == null || !slot)
        {
            Hide();
            return;
        }

        transform.position = slot.transform.position + ((yOffset + slot.IconOffset) * Vector3.up);
        activeSlot = slot;
        switch (slot.SlotType)
        {
            case TownHouseSlotType.Undefined:
            case TownHouseSlotType.Empty:
                SetPlayerLogo(null);
                buttonBuildHouse.gameObject.SetActive(true);
                buttonEraseHouse.gameObject.SetActive(false);
                buttonSwitchPlayer.gameObject.SetActive(false);
                buttonAssignPlayer.gameObject.SetActive(false);
                break;

            default:
                SetPlayerLogo(slot.OwnerUserId);
                buttonBuildHouse.gameObject.SetActive(false);
                buttonEraseHouse.gameObject.SetActive(true);
                buttonSwitchPlayer.gameObject.SetActive(!string.IsNullOrEmpty(slot.OwnerUserId));
                buttonAssignPlayer.gameObject.SetActive(!slot.Owner && string.IsNullOrEmpty(slot.OwnerUserId));
                break;
        }

        HideBuildDialog();
        gameObject.SetActive(true);
    }

    private void SetPlayerLogo(string userId)
    {
        var hasOwner = !string.IsNullOrEmpty(userId);
        ownerLogoContainer.SetActive(hasOwner);
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

    public void Hide()
    {
        HideBuildDialog();
        HidePlayerAssignDialog();
        gameObject.SetActive(false);
        activeSlot = null;
    }

    private void HidePlayerAssignDialog()
    {
        if (playerAssignDialog)
        {
            playerAssignDialog.Hide();
        }
    }

    private void HideBuildDialog()
    {
        if (selectBuildingDialog)
        {
            selectBuildingDialog.Hide();
        }
    }

    public void ShowHouseSelection()
    {
        if (selectBuildingDialog)
        {
            selectBuildingDialog.gameObject.SetActive(true);
        }
    }


    public void ShowAssignPlayerDialog()
    {
        if (!playerAssignDialog)
            return;

        if (!activeSlot)
            return;

        var townHouseController = activeSlot.GetComponentInChildren<TownHouseController>();
        if (!townHouseController)
            return;

        townHouseController.Owner = activeSlot.Owner;
        playerAssignDialog.Show(townHouseController);
    }

    public async void AssignPlayer()
    {
        var newOwner = playerAssignDialog.SelectedPlayer;
        if (!newOwner)
        {
            Hide();
            return;
        }

        if (await gameManager.RavenNest.Village.AssignPlayerAsync(activeSlot.Slot, newOwner.UserId))
        {
            gameManager.Village.TownHouses.SetOwner(activeSlot, newOwner);
            Hide();
            return;
        }


        Hide();
    }

    public void SwitchPlayer()
    {
        Debug.Log("Switch player clicked");
    }

    public async void EraseBuilding()
    {
        Debug.Log("Erase building clicked");
        if (!activeSlot)
        {
            Debug.LogError("Failed to erase house. No active slot selected :o");
            return;
        }

        if (await gameManager.RavenNest.Village.RemoveHouseAsync(activeSlot.Slot))
        {
            gameManager.Village.TownHouses.SetHouse(activeSlot, TownHouseSlotType.Empty);
            Hide();
            return;
        }

        Debug.LogError("Failed to erase house :(");
        Show(activeSlot);
    }

    public async void BuildHouse()
    {
        Debug.Log("Build house clicked");

        if (!selectBuildingDialog.SelectedHouse)
        {
            Hide();
            return;
        }

        if (!activeSlot)
        {
            Debug.LogError("Failed to build house. No active slot selected.");
            return;
        }

        var slot = activeSlot.Slot;
        var slotType = selectBuildingDialog.SelectedHouse.Type;

        if (await gameManager.RavenNest.Village.BuildHouseAsync(slot, (int)slotType))
        {
            gameManager.Village.TownHouses.SetHouse(activeSlot, slotType);
            Hide();
            return;
        }

        Debug.LogError("Failed to build house :(");
        Show(activeSlot);
    }
}