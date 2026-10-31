using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject all2d;
    [SerializeField] private GameObject playerList;
    [SerializeField] private Canvas GameUICanvas;
    [SerializeField] private GameManager gameManager;

    /// <summary>On every UI Toolkit document's root while F1 hides everything: the HUD's bars step aside (ravenfall-hud.uss).</summary>
    private const string HiddenClass = "rf-hud-hidden";

    private UIState state;
    private float nextMark;

    public static HUDController Instance { get; private set; }

    /// <summary>What F1 shows: everything, everything but the player list, or nothing.</summary>
    public UIState State => state;

    private void Awake()
    {
        Instance = this;
        //GameUICanvas.worldCamera = Camera.main;
        //GameUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
    }

    private void Start()
    {
        // In Start rather than Awake, so the player list's own Awake has run and keeps the hidden state.
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        var playerListVisible = PlayerSettings.Instance.PlayerListVisible.GetValueOrDefault(true);
        if (!playerListVisible)
        {
            SetState(UIState.NoPlayerList);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Shows or hides the player list and remembers it. The list's close button and the UI settings use
    /// this, and F1 cycles through the same states, so all three agree.
    /// </summary>
    public void SetPlayerListVisible(bool visible)
    {
        if (visible) SetState(UIState.Everything);
        else if (state == UIState.Everything) SetState(UIState.NoPlayerList);
    }

    /// <summary>What F1 does: everything, then no player list, then nothing.</summary>
    public void CycleState()
    {
        SetState((UIState)(((int)state + 1) % 3));
    }

    /// <summary>
    /// The player list is a UI Toolkit window now, shown and hidden through its PlayerList facade. The old
    /// uGUI object stays active, because the facade on it forwards to the window.
    /// </summary>
    private void ShowPlayerList(bool visible)
    {
        if (gameManager && gameManager.PlayerList) gameManager.PlayerList.SetVisibility(visible);
        else if (playerList) playerList.SetActive(visible);
    }

    private void SetState(UIState newState)
    {
        state = newState;

        var wasPlayerListVisible = PlayerSettings.Instance.PlayerListVisible;
        var playerListVisible = true;
        if (state == UIState.Everything)
        {
            all2d.SetActive(true);
            ShowPlayerList(true);
        }
        else if (state == UIState.NoPlayerList)
        {
            ShowPlayerList(false);
            playerListVisible = false;
        }
        else
        {
            all2d.SetActive(false);
            ShowPlayerList(false);
            playerListVisible = false;
        }

        MarkDocuments();

        if (wasPlayerListVisible != playerListVisible)
        {
            PlayerSettings.Instance.PlayerListVisible = playerListVisible;
            PlayerSettings.Save();
        }
    }

    /// <summary>
    /// The UI Toolkit HUD (the village bar, the stream quest, the event bars, the details windows) is not under all2d:
    /// its documents get a class instead. Again every second while everything is hidden, as a document built later or
    /// rebuilt (a live reload, a view binding again) starts without it.
    /// </summary>
    private void MarkDocuments()
    {
        nextMark = Time.unscaledTime + 1f;
        foreach (var doc in FindObjectsByType<UIDocument>())
        {
            var root = doc.rootVisualElement;
            if (root != null) root.EnableInClassList(HiddenClass, state == UIState.Nothing);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Keybinds.Pressed(Keybinds.Action.ToggleHud))
        {
            CycleState();
        }
        else if (state == UIState.Nothing && Time.unscaledTime >= nextMark)
        {
            MarkDocuments();
        }
    }

    public enum UIState
    {
        Everything,
        NoPlayerList,
        Nothing
    }
}

