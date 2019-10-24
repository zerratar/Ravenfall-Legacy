using UnityEngine;

public class GameMenuHandler : MonoBehaviour
{
    [SerializeField] private MenuView playerManagementView;
    [SerializeField] private MenuView settingsView;
    [SerializeField] private MenuView raidView;

    [SerializeField] private MenuView menuView;
    [SerializeField] private MenuView activeMenu;

    public bool Visible => gameObject.activeSelf;

    private void Awake()
    {
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