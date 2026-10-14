using System;
using UnityEngine;

/// <summary>
/// The entry point for the game menu (Esc or the cog) and settings. The menu itself is the UI Toolkit
/// <see cref="Shinobytes.UI.GameMenuWindow"/>, settings the <see cref="Shinobytes.UI.SettingsWindow"/>;
/// this keeps the login screen, log out and exit where they always were. The object it sits on is
/// what is left of the old uGUI menu and stays switched off.
/// </summary>
public class GameMenuHandler : MonoBehaviour
{
    [SerializeField] private Shinobytes.UI.SettingsWindow toolkitSettings;
    [SerializeField] private Shinobytes.UI.GameMenuWindow toolkitMenu;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private LoginHandler loginScreen;

    public bool Visible => toolkitMenu && toolkitMenu.IsOpen;
    private int hiddenFrame = -1;

    /// <summary>
    /// The menu closed during this frame. Esc closes it, and GameManager, which opens it on Esc,
    /// must not take the same key press as a reason to open it again.
    /// </summary>
    public bool ClosedThisFrame => hiddenFrame == Time.frameCount;
    private bool IsAuthenticated => GameManager && GameManager.RavenNest != null && GameManager.RavenNest.Authenticated;

    /// <summary>The object stays switched off, so Awake never runs; references are found on first use.</summary>
    private GameManager GameManager => gameManager ? gameManager : (gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include));
    private LoginHandler LoginScreen => loginScreen ? loginScreen : (loginScreen = FindAnyObjectByType<LoginHandler>(FindObjectsInactive.Include));
    private Shinobytes.UI.GameMenuWindow Menu => toolkitMenu ? toolkitMenu : (toolkitMenu = FindAnyObjectByType<Shinobytes.UI.GameMenuWindow>(FindObjectsInactive.Include));
    private Shinobytes.UI.SettingsWindow Settings => toolkitSettings ? toolkitSettings : (toolkitSettings = FindAnyObjectByType<Shinobytes.UI.SettingsWindow>(FindObjectsInactive.Include));

    public void ResetUIPositions()
    {
        // Legacy uGUI windows.
        foreach (var ds in GameObject.FindObjectsByType<Dragscript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ds.ResetPosition();
        }

        // UI Toolkit windows. Handled separately because their positions live in preferences keyed
        // by window id rather than on a component, so there is nothing to find in the scene.
        Shinobytes.UI.DraggableWindow.ResetAllPositions();
    }

    public void OpenPlayerLogFolder()
    {
        var appDataFolder = UnityEngine.Application.persistentDataPath;
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            appDataFolder = appDataFolder.Replace("\\", "/");
        System.Diagnostics.Process.Start(appDataFolder);
    }

    public void ClearClanFlagCache()
    {
        GameManager.PlayerLogo.ClearCache();
    }

    public void Show()
    {
        if (LoginScreen && !IsAuthenticated) LoginScreen.HideView();
        if (Menu) Menu.Open();
    }

    public void Hide()
    {
        if (Visible) hiddenFrame = Time.frameCount;
        if (Menu) Menu.Close();

        if (LoginScreen && !IsAuthenticated)
        {
            LoginScreen.ShowView();
        }
    }

    public void ShowSettingsMenu()
    {
        // The menu steps aside while settings are open and comes back when they close.
        if (Menu) Menu.Close();
        if (Settings) Settings.Open(Show);
    }

    public void Logout()
    {
        GameManager.SaveStateFile();
        LoginScreen.ClearPassword();
        GameManager.LoadScene();
    }

    public void Exit()
    {
        GameManager.SaveStateAndShutdownGame(false);
    }
}
