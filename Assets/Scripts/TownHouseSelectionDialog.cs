using System.Collections.Generic;
using UnityEngine;

public class TownHouseSelectionDialog : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TownHouseManager townHouseManager;
    [SerializeField] private RectTransform buildingScrollViewContent;
    [SerializeField] private GameObject townHouseButtonPrefab;
    [SerializeField] private TownHouseRenderManager townHouseRenderManager;

    [SerializeField] private TMPro.TextMeshProUGUI buildingNameLabel;
    [SerializeField] private TMPro.TextMeshProUGUI buildingDescriptionLabel;

    private List<TownHouseButton> instantiatedButtons = new List<TownHouseButton>();
    private TownHouse selectedHouse;
    public TownHouse SelectedHouse
    {
        get => selectedHouse;
        set
        {
            SelectHouse(value);
        }
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!townHouseManager) townHouseManager = gameManager.Village.TownHouses;
        if (!townHouseRenderManager) townHouseRenderManager = FindObjectOfType<TownHouseRenderManager>();

        GenerateTownHouseButtons();
    }

    private void SelectHouse(TownHouse house)
    {
        foreach (var button in instantiatedButtons)
        {
            button.SetOutline(false);
        }

        selectedHouse = house;
        buildingNameLabel.text = house.Name;
        buildingDescriptionLabel.text = house.Description;
    }


    private void GenerateTownHouseButtons()
    {
        foreach (var townHouse in townHouseManager.TownHouses)
        {
            var renderTexture = townHouseRenderManager.CreateHouseRender(townHouse);
            var buttonGameObject = Instantiate(townHouseButtonPrefab, buildingScrollViewContent.transform);
            var button = buttonGameObject.GetComponent<TownHouseButton>();
            button.SetBuilding(townHouse, renderTexture);
            instantiatedButtons.Add(button);
        }

        RebuildTags();
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