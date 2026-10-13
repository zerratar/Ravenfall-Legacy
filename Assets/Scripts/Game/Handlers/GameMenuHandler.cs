using System;
using System.Collections;
using UnityEngine;

public class GameMenuHandler : MonoBehaviour
{
    [SerializeField] private MenuView playerManagementView;
    [SerializeField] private MenuView settingsView;
    [Tooltip("The UI Toolkit settings window. When set it replaces the uGUI settings view.")]
    [SerializeField] private Shinobytes.UI.SettingsWindow toolkitSettings;
    [Tooltip("The UI Toolkit game menu. When set it replaces the uGUI menu panel.")]
    [SerializeField] private Shinobytes.UI.GameMenuWindow toolkitMenu;
    [SerializeField] private MenuView raidView;

    [SerializeField] private MenuView menuView;
    [SerializeField] private MenuView activeMenu;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private LoginHandler loginScreen;

    [SerializeField] private TMPro.TextMeshProUGUI lblVersion;

    [SerializeField] private FadeInOut fadeToBlack;
    [SerializeField] private UnityEngine.UI.Button signOutButton;

    public bool Visible => toolkitMenu ? toolkitMenu.IsOpen : gameObject.activeSelf;
    private int shownFrame = -1;
    private int hiddenFrame = -1;

    /// <summary>
    /// The menu closed during this frame. Esc closes it, and GameManager, which opens it on Esc,
    /// must not take the same key press as a reason to open it again.
    /// </summary>
    public bool ClosedThisFrame => hiddenFrame == Time.frameCount;
    private bool IsAuthenticated => gameManager && gameManager.RavenNest != null && gameManager.RavenNest.Authenticated;
    private void Awake()
    {
        if (!fadeToBlack) fadeToBlack = FindAnyObjectByType<FadeInOut>();

        signOutButton.gameObject.SetActive(false);

        UpdateDetailsLabel();

        if (!loginScreen) loginScreen = FindAnyObjectByType<LoginHandler>(FindObjectsInactive.Include);
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (!toolkitSettings) toolkitSettings = FindAnyObjectByType<Shinobytes.UI.SettingsWindow>(FindObjectsInactive.Include);
        if (!toolkitMenu) toolkitMenu = FindAnyObjectByType<Shinobytes.UI.GameMenuWindow>(FindObjectsInactive.Include);

        settingsView.Hide(false);

        Hide();
    }

    private void UpdateDetailsLabel()
    {
        if (lblVersion)
        {

            lblVersion.text =
                "Game Version\t<b>" + Ravenfall.Version + "</b>\n" +
                "Graphics API\t<b>" + SystemInfo.graphicsDeviceType + "</b>\n" +
                "Graphics Memory\t<b>" + SystemInfo.graphicsMemorySize + "MB</b>\n" +
                "System Memory\t<b>" + SystemInfo.systemMemorySize + "MB</b>\n" +
                "Operating System\t<b>" + SystemInfo.operatingSystem + "</b>\n" +
                "Uptime\t\t<b>" + Utility.FormatTime(TimeSpan.FromSeconds(Time.time)) + "</b>";
        }
    }

    public void ResetUIPositions()
    {
        // Legacy uGUI windows.
        foreach (var ds in GameObject.FindObjectsByType<Dragscript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ds.ResetPosition();
        }

        // UI Toolkit windows. Handled separately because their positions live in preferences keyed
        // by window id rather than on a component, so there is nothing to find in the scene. Both
        // run while the migration is in progress; this call is harmless once no toolkit windows
        // exist yet, and keeps working when the last Dragscript is gone.
        Shinobytes.UI.DraggableWindow.ResetAllPositions();
    }

    private void Update()
    {
        //if (activeMenu != menuView)
        //{
        //    if (!activeMenu.Visible)
        //    {
        //        ShowMenu();
        //    }
        //    return;
        //}

        //if (!activeMenu.Visible)
        //{
        //    ShowMenu();
        //}

        UpdateDetailsLabel();

        // Not in the frame the menu came back in: the settings window closes on that same Esc and
        // reopens the menu, which would otherwise shut again straight away.
        if (Input.GetKeyUp(KeyCode.Escape) && Time.frameCount != shownFrame)
        {
            Hide();
        }
    }

    public void OpenPlayerLogFolder()
    {
        //var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        //var appDataFolder = System.IO.Path.Combine(userProfile, @"AppData\LocalLow\", Application.companyName, Application.productName);
        var appDataFolder = UnityEngine.Application.persistentDataPath;
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            appDataFolder = appDataFolder.Replace("\\", "/");
        System.Diagnostics.Process.Start(appDataFolder);
    }

    public void Back()
    {
        settingsView.Hide();
        ShowMenu();
    }

    public void ClearClanFlagCache()
    {
        gameManager.PlayerLogo.ClearCache();
    }

    public void ShowMenu()
    {
        ActivateMenu(menuView);
    }

    public void Show()
    {
        shownFrame = Time.frameCount;

        if (toolkitMenu)
        {
            // The uGUI panel stays switched off; the toolkit menu handles its own Esc and buttons.
            if (loginScreen && !IsAuthenticated) loginScreen.HideView();
            gameObject.SetActive(false);
            toolkitMenu.Open();
            return;
        }

        gameObject.SetActive(true);
        signOutButton.gameObject.SetActive(gameManager.RavenNest.Authenticated);

        ActivateMenu(menuView);

        //if (activeMenu)
        //{
        //    activeMenu.gameObject.SetActive(false);
        //}
    }

    public void Hide()
    {
        if (Visible) hiddenFrame = Time.frameCount;
        if (toolkitMenu) toolkitMenu.Close();
        gameObject.SetActive(false);

        if (loginScreen && !IsAuthenticated)
        {
            loginScreen.ShowView();
        }
    }

    public void ShowRaidMenu()
    {
        ActivateMenu(raidView);
    }

    public void ShowSettingsMenu()
    {
        if (toolkitSettings)
        {
            // The menu steps aside while settings are open and comes back when they close. (The
            // uGUI one also draws above every UI Toolkit panel, so it could not stay up anyway.)
            if (toolkitMenu) toolkitMenu.Close();
            gameObject.SetActive(false);
            toolkitSettings.Open(Show);
            return;
        }

        ActivateMenu(settingsView);
    }

    public void ShowPlayerMenu()
    {
        ActivateMenu(playerManagementView);
    }

    private void ActivateMenu(MenuView menuView)
    {
        if (loginScreen && !IsAuthenticated)
        {
            loginScreen.HideView();
        }

        if (!Visible)
        {
            Show();
        }

        StartCoroutine(ToggleMenu(activeMenu, menuView));
    }

    private IEnumerator ToggleMenu(MenuView hide, MenuView show)
    {
        if (hide)
        {
            yield return null;
            hide.Hide();
        }

        yield return null;

        show.Show();

        activeMenu = show;
    }

    public void Logout()
    {
        //fadeToBlack.StartFade();
        gameManager.SaveStateFile();
        loginScreen.ClearPassword();
        gameManager.LoadScene();
    }

    public void Exit()
    {
        gameManager.SaveStateAndShutdownGame(false);
    }
}