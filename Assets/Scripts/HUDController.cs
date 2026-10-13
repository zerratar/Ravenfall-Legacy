using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject all2d;
    [SerializeField] private GameObject playerList;
    [SerializeField] private Canvas GameUICanvas;
    [SerializeField] private GameManager gameManager;

    private UIState state;

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

        if (wasPlayerListVisible != playerListVisible)
        {
            PlayerSettings.Instance.PlayerListVisible = playerListVisible;
            PlayerSettings.Save();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CycleState();
        }
    }

    public enum UIState
    {
        Everything,
        NoPlayerList,
        Nothing
    }
}

