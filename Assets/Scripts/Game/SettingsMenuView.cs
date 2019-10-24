using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider raidHornVolumeSlider = null;
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        raidHornVolumeSlider.value = PlayerPrefs.GetFloat("RaidHornVolume", gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", gameManager.Music.volume);
    }

    protected override void OnChangesApplied()
    {
        PlayerPrefs.SetFloat("RaidHornVolume", raidHornVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
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
    }
}