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

    public bool Visible => gameObject.activeSelf;
    private bool IsAuthenticated => gameManager && gameManager.RavenNest != null && gameManager.RavenNest.Authenticated;
    private void Awake()
    {
        if (lblVersion)
        {
            lblVersion.text = "v" + Application.version;
        }

        if (!loginScreen) loginScreen = FindObjectOfType<LoginHandler>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        Hide();
    }

    private void Update()
    {
        if (activeMenu != menuView)
        {
            if (!activeMenu.Visible)
            {
                ShowMenu();
            }

            return;
        }

        if (!activeMenu.Visible)
        {
            ShowMenu();
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void ShowMenu()
    {
        ActivateMenu(menuView);
    }

    public void Show()
    {
        gameObject.SetActive(true);
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

        if (activeMenu)
        {
            activeMenu.Hide();
        }

        menuView.Show();

        activeMenu = menuView;
    }

    public void Exit()
    {
        Application.Quit();
    }
}