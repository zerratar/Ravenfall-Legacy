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
    private bool buttonsGenerated;
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
        TryGenerateButtons();
    }

    private void OnEnable()
    {
        // Retried every time the dialog opens.
        //
        // This object ships active in the scene, so Start runs during scene load, well before the
        // village has arrived from the server. Reading gameManager.Village.TownHouses that early
        // throws, and because Start never runs twice the button list stayed empty for the rest of
        // the session: the dialog would open with nothing in it. Generating on demand means the
        // first open after the village exists succeeds.
        TryGenerateButtons();
    }

    /// <summary>
    /// Builds the building buttons once the data needed to build them exists. Safe to call
    /// repeatedly; it does nothing after it has succeeded.
    /// </summary>
    private void TryGenerateButtons()
    {
        if (buttonsGenerated)
        {
            return;
        }

        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!townHouseRenderManager) townHouseRenderManager = FindAnyObjectByType<TownHouseRenderManager>();

        if (!townHouseManager)
        {
            // Village is populated from the server, so this is null until a session is running.
            townHouseManager = gameManager != null && gameManager.Village != null
                ? gameManager.Village.TownHouses
                : null;
        }

        if (!townHouseManager || townHouseManager.TownHouses == null || !townHouseRenderManager)
        {
            Shinobytes.Debug.LogWarning("Building selection has nothing to show yet."
                + " townHouseManager=" + (townHouseManager ? "ok" : "missing")
                + ", renderManager=" + (townHouseRenderManager ? "ok" : "missing")
                + ". Will retry the next time the dialog opens.");
            return;
        }

        GenerateTownHouseButtons();
        buttonsGenerated = true;
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