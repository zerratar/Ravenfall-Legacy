using Assets.Scripts.UI.Menu;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    public const string SettingsName_PotatoMode = "PotatoMode";
    public const string SettingsName_AutoPotatoMode = "AutoPotatoMode";
    public const string SettingsName_PlayerListSize = "PlayerListSize";
    public const string SettingsName_PlayerListScale = "PlayerListScale";
    public const string SettingsName_RaidHornVolume = "RaidHornVolume";
    public const string SettingsName_MusicVolume = "MusicVolume";

    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider raidHornVolumeSlider = null;
    [SerializeField] private Slider playerListSizeSlider = null;
    [SerializeField] private Slider playerListScaleSlider = null;
    //[SerializeField] private Slider playerObserverScaleSlider = null;
    [SerializeField] private Toggle potatoModeToggle = null;
    [SerializeField] private Toggle autoPotatoModeToggle = null;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private GraphicsSettings graphics;
    [SerializeField] private UISettings ui;
    [SerializeField] private SoundsSettings sounds;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        potatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_PotatoMode, gameManager.PotatoMode && !gameManager.AutoPotatoMode ? 1 : 0) > 0;
        autoPotatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_AutoPotatoMode, gameManager.AutoPotatoMode ? 1 : 0) > 0;
        playerListSizeSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListSize, gameManager.PlayerList.Bottom);
        playerListScaleSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListScale, gameManager.PlayerList.Scale);
        //playerObserverScaleSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerObserverScale, gameManager.PlayerList.Scale);
        raidHornVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_RaidHornVolume, gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_MusicVolume, gameManager.Music.volume);

        ShowSoundSettings();
    }

    protected override void OnChangesApplied()
    {
        PlayerPrefs.SetInt(SettingsName_PotatoMode, potatoModeToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(SettingsName_AutoPotatoMode, autoPotatoModeToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat(SettingsName_PlayerListSize, playerListSizeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_PlayerListScale, playerListScaleSlider.value);
        PlayerPrefs.SetFloat(SettingsName_RaidHornVolume, raidHornVolumeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_MusicVolume, musicVolumeSlider.value);
    }

    public void ShowSoundSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(true);
    }
    public void ShowGraphicsSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(true);
        sounds.gameObject.SetActive(false);
    }
    public void ShowUISettings()
    {
        ui.gameObject.SetActive(true);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
    }
    public void OnPotatoModeChanged()
    {
        gameManager.PotatoMode = potatoModeToggle.isOn;
        gameManager.AutoPotatoMode = autoPotatoModeToggle.isOn;
    }

    public void OnSliderValueChanged()
    {
        if (musicVolumeSlider != null)
        {
            gameManager.Music.volume = musicVolumeSlider.value;
        }

        if (raidHornVolumeSlider != null)
        {
            gameManager.StreamRaid.Notifications.volume = raidHornVolumeSlider.value;
            gameManager.Raid.Notifications.volume = raidHornVolumeSlider.value;
            gameManager.Dungeons.Notifications.volume = raidHornVolumeSlider.value;
        }

        if (playerListSizeSlider != null)
        {
            gameManager.PlayerList.Bottom = playerListSizeSlider.value;
        }

        if (playerListScaleSlider != null)
        {
            gameManager.PlayerList.Scale = playerListScaleSlider.value;
        }
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
}