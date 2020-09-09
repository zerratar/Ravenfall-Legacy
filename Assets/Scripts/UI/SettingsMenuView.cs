using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    public const string SettingsName_PotatoMode = "PotatoMode";
    public const string SettingsName_AutoPotatoMode = "AutoPotatoMode";
    public const string SettingsName_PlayerListSize = "PlayerListSize";
    public const string SettingsName_RaidHornVolume = "RaidHornVolume";
    public const string SettingsName_MusicVolume = "MusicVolume";

    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider raidHornVolumeSlider = null;
    [SerializeField] private Slider playerListSizeSlider = null;
    [SerializeField] private Toggle potatoModeToggle = null;
    [SerializeField] private Toggle autoPotatoModeToggle = null;
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        potatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_PotatoMode, gameManager.PotatoMode && !gameManager.AutoPotatoMode ? 1 : 0) > 0;
        autoPotatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_AutoPotatoMode, gameManager.AutoPotatoMode ? 1 : 0) > 0;
        playerListSizeSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListSize, gameManager.PlayerList.Bottom);
        raidHornVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_RaidHornVolume, gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_MusicVolume, gameManager.Music.volume);
    }

    protected override void OnChangesApplied()
    {
        PlayerPrefs.SetInt(SettingsName_PotatoMode, potatoModeToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(SettingsName_AutoPotatoMode, autoPotatoModeToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat(SettingsName_PlayerListSize, playerListSizeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_RaidHornVolume, raidHornVolumeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_MusicVolume, musicVolumeSlider.value);
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
            gameManager.Raid.Notifications.volume = raidHornVolumeSlider.value;
            gameManager.Dungeons.Notifications.volume = raidHornVolumeSlider.value;
        }

        if (playerListSizeSlider != null)
        {
            gameManager.PlayerList.Bottom = playerListSizeSlider.value;
        }
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
}