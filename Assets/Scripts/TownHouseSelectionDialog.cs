using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TownHouseSelectionDialog : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TownHouseManager townHouseManager;
    [SerializeField] private RectTransform buildingScrollViewContent;
    [SerializeField] private ScrollRect buildingScrollRect;
    [SerializeField] private GameObject townHouseButtonPrefab;
    [SerializeField] private TownHouseRenderManager townHouseRenderManager;

    [SerializeField] private TownHouseScrollButton chevronLeft;
    [SerializeField] private TownHouseScrollButton chevronRight;

    [SerializeField] private TMPro.TextMeshProUGUI buildingNameLabel;
    [SerializeField] private TMPro.TextMeshProUGUI buildingDescriptionLabel;
    [SerializeField] private float scrollSpeed = 0.1f;


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

    void Update()
    {
        if (chevronLeft && chevronLeft.IsPointerDown)
        {
            ScrollLeft();
        }

        if (chevronRight && chevronRight.IsPointerDown)
        {
            ScrollRight();
        }
    }

    private void SelectHouse(TownHouse house)
    {
        foreach (var button in instantiatedButtons)
        {
            button.SetOutline(false);
        }

        selectedHouse = house;
        buildingNameLabel.text = house.Name;
        buildingDescriptionLabel.text = string.Format(house.Description, (int)GameMath.MaxExpBonusPerSlot);
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

    public void ScrollLeft()
    {
        var newScroll = buildingScrollRect.horizontalNormalizedPosition - scrollSpeed * Time.deltaTime;
        buildingScrollRect.horizontalNormalizedPosition = Mathf.Max(0, newScroll);
    }

    public void ScrollRight()
    {
        var newScroll = buildingScrollRect.horizontalNormalizedPosition + scrollSpeed * Time.deltaTime;
        buildingScrollRect.horizontalNormalizedPosition = Mathf.Min(1, newScroll);
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