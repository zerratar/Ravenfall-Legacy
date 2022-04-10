using Assets.Scripts.UI.Menu;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    public const string SettingsName_PostProcessing = "PostProcessing";
    public const string SettingsName_PotatoMode = "PotatoMode";
    public const string SettingsName_AutoPotatoMode = "AutoPotatoMode";
    public const string SettingsName_RealTimeDayNightCycle = "RealTimeDayNightCycle";
    public const string SettingsName_PlayerListSize = "PlayerListSize";
    public const string SettingsName_PlayerListScale = "PlayerListScale";
    public const string SettingsName_RaidHornVolume = "RaidHornVolume";
    public const string SettingsName_MusicVolume = "MusicVolume";
    public const string SettingsName_DPIScale = "DPIScale";

    public const string SettingsName_PlayerBoostRequirement = "PlayerBoostRequirement";
    public const string SettingsName_PlayerCacheExpiryTime = "PlayerCacheExpiryTime";
    public const string SettingsName_AlertExpiredStateCacheInChat = "AlertExpiredStateCacheInChat";
    public const string SettingsName_ItemDropMessageType = "ItemDropMessageType";

    //[SerializeField] private Slider playerObserverScaleSlider = null;

    [SerializeField] private GameManager gameManager;


    [Header("Sounds Settings")]
    [SerializeField] private SoundsSettings sounds;
    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider raidHornVolumeSlider = null;

    [Header("UI Settings")]
    [SerializeField] private UISettings ui;
    [SerializeField] private Slider playerListSizeSlider = null;
    [SerializeField] private Slider playerListScaleSlider = null;

    [Header("Graphics Settings")]
    [SerializeField] private GraphicsSettings graphics;
    [SerializeField] private Toggle potatoModeToggle = null;
    [SerializeField] private Toggle postProcessingToggle = null;
    [SerializeField] private Toggle autoPotatoModeToggle = null;
    [SerializeField] private Toggle realtimeDayNightCycle = null;
    [SerializeField] private Slider dpiSlider = null;

    [Header("Game Settings")]
    [SerializeField] private GameObject game;
    [SerializeField] private TMPro.TMP_Dropdown boostRequirementDropdown = null;
    [SerializeField] private TMPro.TMP_Dropdown playerCacheExpiryTimeDropdown = null;
    [SerializeField] private TMPro.TMP_Dropdown itemDropMessageDropdown = null;
    [SerializeField] private Toggle alertPlayerCacheExpirationToggle = null;
    [SerializeField] private TMPro.TextMeshProUGUI[] itemDropMessageExamples = null;

    public static readonly TimeSpan[] PlayerCacheExpiry = new TimeSpan[]
    {
        TimeSpan.Zero,          // [0]
        TimeSpan.FromHours(2),  // [1]
        TimeSpan.FromHours(4),  // [2]
        TimeSpan.FromHours(8),  // [3]
        TimeSpan.FromHours(12), // [4]
        TimeSpan.FromHours(24), // [5]
        TimeSpan.FromHours(36), // [6]
        TimeSpan.FromHours(48), // [7]
        TimeSpan.FromDays(9999) // [8]
    };

    public static TimeSpan GetPlayerCacheExpiryTime()
    {
        return PlayerCacheExpiry[PlayerSettings.Instance.PlayerCacheExpiryTime ?? 2]; // PlayerPrefs.GetInt(SettingsName_PlayerCacheExpiryTime, 1)
    }

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        var settings = PlayerSettings.Instance;

        postProcessingToggle.isOn = settings.PostProcessing.GetValueOrDefault(gameManager.UsePostProcessingEffects);
        potatoModeToggle.isOn = settings.PotatoMode.GetValueOrDefault(gameManager.PotatoMode && !gameManager.AutoPotatoMode);
        autoPotatoModeToggle.isOn = settings.AutoPotatoMode.GetValueOrDefault(gameManager.AutoPotatoMode);
        realtimeDayNightCycle.isOn = settings.RealTimeDayNightCycle.GetValueOrDefault(gameManager.RealtimeDayNightCycle);
        playerListSizeSlider.value = settings.PlayerListSize.GetValueOrDefault(gameManager.PlayerList.Bottom);
        playerListScaleSlider.value = settings.PlayerListScale.GetValueOrDefault(gameManager.PlayerList.Scale);
        dpiSlider.value = settings.DPIScale.GetValueOrDefault(1f);
        raidHornVolumeSlider.value = settings.RaidHornVolume.GetValueOrDefault(gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = settings.MusicVolume.GetValueOrDefault(gameManager.Music.volume);
        playerCacheExpiryTimeDropdown.value = settings.PlayerCacheExpiryTime.GetValueOrDefault(1);
        boostRequirementDropdown.value = settings.PlayerBoostRequirement.GetValueOrDefault(gameManager.PlayerBoostRequirement);
        alertPlayerCacheExpirationToggle.isOn = settings.AlertExpiredStateCacheInChat.GetValueOrDefault(gameManager.AlertExpiredStateCacheInChat);
        itemDropMessageDropdown.value = settings.ItemDropMessageType.GetValueOrDefault((int)gameManager.ItemDropMessageSettings);

        //postProcessingToggle.isOn = PlayerPrefs.GetInt(SettingsName_PostProcessing, gameManager.UsePostProcessingEffects ? 1 : 0) > 0;
        //potatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_PotatoMode, gameManager.PotatoMode && !gameManager.AutoPotatoMode ? 1 : 0) > 0;
        //autoPotatoModeToggle.isOn = PlayerPrefs.GetInt(SettingsName_AutoPotatoMode, gameManager.AutoPotatoMode ? 1 : 0) > 0;
        //realtimeDayNightCycle.isOn = PlayerPrefs.GetInt(SettingsName_RealTimeDayNightCycle, gameManager.RealtimeDayNightCycle ? 1 : 0) > 0;
        //playerListSizeSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListSize, gameManager.PlayerList.Bottom);
        //playerListScaleSlider.value = PlayerPrefs.GetFloat(SettingsName_PlayerListScale, gameManager.PlayerList.Scale);
        //dpiSlider.value = PlayerPrefs.GetFloat(SettingsName_DPIScale, 1f);
        //raidHornVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_RaidHornVolume, gameManager.Raid.Notifications.volume);
        //musicVolumeSlider.value = PlayerPrefs.GetFloat(SettingsName_MusicVolume, gameManager.Music.volume);
        //playerCacheExpiryTimeDropdown.value = PlayerPrefs.GetInt(SettingsName_PlayerCacheExpiryTime, 1);
        //boostRequirementDropdown.value = PlayerPrefs.GetInt(SettingsName_PlayerBoostRequirement, gameManager.PlayerBoostRequirement);
        //alertPlayerCacheExpirationToggle.isOn = PlayerPrefs.GetInt(SettingsName_AlertExpiredStateCacheInChat, gameManager.AlertExpiredStateCacheInChat ? 1 : 0) == 1;
        //itemDropMessageDropdown.value = PlayerPrefs.GetInt(SettingsName_ItemDropMessageType, (int)gameManager.ItemDropMessageSettings);

        //QualitySettings.resolutionScalingFixedDPIFactor = dpiSlider.value;
        SetResolutionScale(dpiSlider.value);
        ShowSoundSettings();

        ShowItemDropExample();
    }

    private void ShowItemDropExample()
    {
        if (itemDropMessageExamples == null)
            return;

        var index = itemDropMessageDropdown.value;
        if (index >= itemDropMessageExamples.Length)
            return;

        for (var i = 0; i < itemDropMessageExamples.Length; ++i)
        {
            itemDropMessageExamples[i].gameObject.SetActive(i == index);
        }
    }

    protected override void OnChangesApplied()
    {
        var settings = PlayerSettings.Instance;

        settings.PotatoMode = potatoModeToggle.isOn;
        settings.PostProcessing = postProcessingToggle.isOn;
        settings.AutoPotatoMode = autoPotatoModeToggle.isOn;
        settings.RealTimeDayNightCycle = realtimeDayNightCycle.isOn;
        settings.PlayerListSize = playerListSizeSlider.value;
        settings.PlayerListScale = playerListScaleSlider.value;
        settings.RaidHornVolume = raidHornVolumeSlider.value;
        settings.MusicVolume = musicVolumeSlider.value;
        settings.DPIScale = dpiSlider.value;
        settings.PlayerBoostRequirement = boostRequirementDropdown.value;
        settings.PlayerCacheExpiryTime = playerCacheExpiryTimeDropdown.value;
        settings.AlertExpiredStateCacheInChat = alertPlayerCacheExpirationToggle.isOn;
        settings.ItemDropMessageType = itemDropMessageDropdown.value;

        PlayerSettings.Save();

        //PlayerPrefs.SetInt(SettingsName_PotatoMode, potatoModeToggle.isOn ? 1 : 0);
        //PlayerPrefs.SetInt(SettingsName_PostProcessing, postProcessingToggle.isOn ? 1 : 0);
        //PlayerPrefs.SetInt(SettingsName_AutoPotatoMode, autoPotatoModeToggle.isOn ? 1 : 0);
        //PlayerPrefs.SetInt(SettingsName_RealTimeDayNightCycle, realtimeDayNightCycle.isOn ? 1 : 0);
        //PlayerPrefs.SetFloat(SettingsName_PlayerListSize, playerListSizeSlider.value);
        //PlayerPrefs.SetFloat(SettingsName_PlayerListScale, playerListScaleSlider.value);
        //PlayerPrefs.SetFloat(SettingsName_RaidHornVolume, raidHornVolumeSlider.value);
        //PlayerPrefs.SetFloat(SettingsName_MusicVolume, musicVolumeSlider.value);
        //PlayerPrefs.SetFloat(SettingsName_DPIScale, dpiSlider.value);
        //PlayerPrefs.SetInt(SettingsName_PlayerBoostRequirement, boostRequirementDropdown.value);
        //PlayerPrefs.SetInt(SettingsName_PlayerCacheExpiryTime, playerCacheExpiryTimeDropdown.value);
        //PlayerPrefs.SetInt(SettingsName_AlertExpiredStateCacheInChat, alertPlayerCacheExpirationToggle.isOn ? 1 : 0);
        //PlayerPrefs.SetInt(SettingsName_ItemDropMessageType, itemDropMessageDropdown.value);
        //PlayerPrefs.Save();

    }

    public void DisconnectFromServer()
    {
        gameManager.RavenNest.Stream.Reconnect();
    }

    public void ShowSoundSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(true);
        game.gameObject.SetActive(false);
    }
    public void ShowGraphicsSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(true);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(false);
    }
    public void ShowGameSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(true);
    }
    public void ShowUISettings()
    {
        ui.gameObject.SetActive(true);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(false);
    }
    public void OnPotatoModeChanged()
    {
        gameManager.PotatoMode = potatoModeToggle.isOn;
        gameManager.AutoPotatoMode = autoPotatoModeToggle.isOn;
    }

    public void OnRealTimeDayNightCycleChanged()
    {
        gameManager.RealtimeDayNightCycle = realtimeDayNightCycle.isOn;
    }
    public void OnPostProcessingEffectsChanged()
    {
        gameManager.UsePostProcessingEffects = postProcessingToggle.isOn;
    }
    public void OnBoostRequirementChanged(int val)
    {
        gameManager.PlayerBoostRequirement = boostRequirementDropdown.value;
    }

    public void OnItemDropMessageChanged()
    {
        gameManager.ItemDropMessageSettings = (PlayerItemDropMessageSettings)itemDropMessageDropdown.value;
        ShowItemDropExample();
    }

    public void OnAlertExpiryCacheFileChanged()
    {
        gameManager.AlertExpiredStateCacheInChat = alertPlayerCacheExpirationToggle.isOn;
    }

    public void OnSliderValueChanged()
    {
        if (dpiSlider != null)
        {
            UpdateResolutionScale();
        }

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

    private void UpdateResolutionScale()
    {
        SetResolutionScale(dpiSlider.value);
    }

    public static void SetResolutionScale(float factor)
    {
        QualitySettings.resolutionScalingFixedDPIFactor = factor;
        ScalableBufferManager.ResizeBuffers(factor, factor);
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
}