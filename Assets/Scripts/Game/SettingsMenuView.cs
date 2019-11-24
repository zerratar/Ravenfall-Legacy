using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    private const string SettingsName_PlayerListSize = "PlayerListSize";
    private const string SettingsName_RaidHornVolume = "RaidHornVolume";
    private const string SettingsName_MusicVolume = "MusicVolume";

    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider raidHornVolumeSlider = null;
    [SerializeField] private Slider playerListSizeSlider = null;
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        playerListSizeSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListSize, gameManager.PlayerList.Bottom);
        raidHornVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_RaidHornVolume, gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_MusicVolume, gameManager.Music.volume);
    }

    protected override void OnChangesApplied()
    {
        PlayerPrefs.SetFloat(SettingsName_PlayerListSize, playerListSizeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_RaidHornVolume, raidHornVolumeSlider.value);
        PlayerPrefs.SetFloat(SettingsName_MusicVolume, musicVolumeSlider.value);
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