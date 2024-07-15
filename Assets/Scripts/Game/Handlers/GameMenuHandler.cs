using System;
using System.Collections;
using UnityEngine;

public class GameMenuHandler : MonoBehaviour
{
    [SerializeField] private MenuView playerManagementView;
    [SerializeField] private MenuView settingsView;
    [SerializeField] private MenuView raidView;

    [SerializeField] private MenuView menuView;
    [SerializeField] private MenuView activeMenu;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private LoginHandler loginScreen;

    [SerializeField] private TMPro.TextMeshProUGUI lblVersion;

    [SerializeField] private FadeInOut fadeToBlack;
    [SerializeField] private UnityEngine.UI.Button signOutButton;
    public bool Visible => gameObject.activeSelf;
    private bool IsAuthenticated => gameManager && gameManager.RavenNest != null && gameManager.RavenNest.Authenticated;
    private void Awake()
    {
        if (!fadeToBlack) fadeToBlack = FindAnyObjectByType<FadeInOut>();

        signOutButton.gameObject.SetActive(false);

        if (lblVersion)
        {
            lblVersion.text = "v" + Ravenfall.Version;//Application.version;
        }

        if (!loginScreen) loginScreen = FindAnyObjectByType<LoginHandler>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

        settingsView.Hide(false);

        Hide();
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

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void OpenPlayerLogFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appDataFolder = System.IO.Path.Combine(userProfile, @"AppData\LocalLow\", Application.companyName, Application.productName);
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
        gameObject.SetActive(false);

        if (loginScreen && !IsAuthenticated)
        {
            loginScreen.gameObject.SetActive(true);
        }
    }

    public void ShowRaidMenu()
    {
        ActivateMenu(raidView);
    }

    public void ShowSettingsMenu()
    {
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
            loginScreen.gameObject.SetActive(false);
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