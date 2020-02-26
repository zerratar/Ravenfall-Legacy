using UnityEngine;

public class BuildSlotIconsManager : MonoBehaviour
{
    [SerializeField] private float yOffset = 3f;

    [SerializeField] private UIButton buttonBuildHouse;
    [SerializeField] private UIButton buttonEraseHouse;
    [SerializeField] private UIButton buttonSwitchPlayer;
    [SerializeField] private UIButton buttonAssignPlayer;

    [SerializeField] private TownHouseSelectionDialog selectBuildingDialog;
    [SerializeField] private GameManager gameManager;    

    private TownHouseSlot activeSlot;

    private void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        Hide();
    }

    public void Show(TownHouseSlot slot)
    {
        transform.position = slot.transform.position + (yOffset * Vector3.up);
        activeSlot = slot;
        switch (slot.SlotType)
        {
            case -1:
            case 0:
                buttonBuildHouse.gameObject.SetActive(true);
                buttonEraseHouse.gameObject.SetActive(false);
                buttonSwitchPlayer.gameObject.SetActive(false);
                buttonAssignPlayer.gameObject.SetActive(false);
                break;

            default:

                buttonBuildHouse.gameObject.SetActive(false);
                buttonEraseHouse.gameObject.SetActive(true);
                buttonSwitchPlayer.gameObject.SetActive(slot.Owner);
                buttonAssignPlayer.gameObject.SetActive(!slot.Owner);
                break;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (selectBuildingDialog)
        {
            selectBuildingDialog.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
        activeSlot = null;
    }

    public void AssignPlayer()
    {
        Debug.Log("Assign player clicked");
    }

    public void SwitchPlayer()
    {
        Debug.Log("Switch player clicked");
    }

    public void EraseBuilding()
    {
        Debug.Log("Erase building clicked");
        if (activeSlot)
        {
            gameManager.Village.TownHouses.SetHouse(activeSlot, -1);
            Show(activeSlot);
        }
    }

    public void ShowHouseSelection()
    {
        Debug.Log("Build house clicked");

        if (selectBuildingDialog)
        {
            selectBuildingDialog.gameObject.SetActive(true);
        }
    }

    public void BuildHouse()
    {
        if (!selectBuildingDialog.SelectedHouse)
        {
            Hide();
            return;
        }

        if (activeSlot)
        {
            gameManager.Village.TownHouses.SetHouse(activeSlot, 1);
            Show(activeSlot);
        }
    }
}
