﻿using Assets.Scripts.UI.Menu;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    public const string SettingsName_PostProcessing = "PostProcessing";
    public const string SettingsName_PotatoMode = "PotatoMode";
    public const string SettingsName_AutoPotatoMode = "AutoPotatoMode";
    public const string SettingsName_RealTimeDayNightCycle = "RealTimeDayNightCycle";
    public const string SettingsName_PlayerName = "PlayerName";
    public const string SettingsName_PlayerListSize = "PlayerListSize";
    public const string SettingsName_PlayerListScale = "PlayerListScale";
    public const string SettingsName_RaidHornVolume = "RaidHornVolume";
    public const string SettingsName_MusicVolume = "MusicVolume";
    public const string SettingsName_DPIScale = "DPIScale";
    public const string SettingsName_ViewDistance = "ViewDistance";

    public const string SettingsName_PlayerBoostRequirement = "PlayerBoostRequirement";
    public const string SettingsName_PlayerCacheExpiryTime = "PlayerCacheExpiryTime";
    public const string SettingsName_AlertExpiredStateCacheInChat = "AlertExpiredStateCacheInChat";
    public const string SettingsName_ItemDropMessageType = "ItemDropMessageType";

    public const string SettingsName_StreamLabelsEnabled = "StreamLabelsEnabled";

    public const string SettingsName_PathfindingQualitySettings = "PathfindingQualitySettings";


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
    [SerializeField] private Toggle playerNameToggle;
    [SerializeField] private Toggle playerListToggle;
    [SerializeField] private Toggle playerDetailsToggle;

    [Header("Graphics Settings")]
    [SerializeField] private GraphicsSettings graphics;
    [SerializeField] private Toggle potatoModeToggle = null;
    [SerializeField] private Toggle postProcessingToggle = null;
    [SerializeField] private Toggle autoPotatoModeToggle = null;
    [SerializeField] private Toggle dayNightCycleEnabled = null;
    [SerializeField] private Slider dayNightTime = null;
    [SerializeField] private Toggle realtimeDayNightCycle = null;
    [SerializeField] private Slider dpiSlider = null;
    [SerializeField] private Slider viewDistanceSlider = null;
    [SerializeField] private TMPro.TMP_Dropdown qualityLevelDropdown = null;


    [Header("Game Settings")]
    [SerializeField] private GameObject game;
    [SerializeField] private Slider observerCameraRotationSlider = null;
    [SerializeField] private TMPro.TextMeshProUGUI observerCameraRotationLabel = null;
    [SerializeField] private TMPro.TMP_Dropdown boostRequirementDropdown = null;
    [SerializeField] private TMPro.TMP_Dropdown playerCacheExpiryTimeDropdown = null;
    [SerializeField] private TMPro.TMP_Dropdown itemDropMessageDropdown = null;
    [SerializeField] private Toggle autoAssignVacantHousesEnabled = null;
    [SerializeField] private Toggle streamLabelsEnabled = null;
    [SerializeField] private Toggle alertPlayerCacheExpirationToggle = null;
    [SerializeField] private TMPro.TextMeshProUGUI[] itemDropMessageExamples = null;
    [SerializeField] private TMPro.TMP_Dropdown pathfindingQuality = null;

    [SerializeField] private Toggle disableRaidsToggle;
    [SerializeField] private Toggle disableDungeonsToggle;

    [Header("Admin Settings")]
    [SerializeField] private GameObject adminButton;
    [SerializeField] private GameObject admin;
    [SerializeField] private Toggle debugControlsEnabled = null;


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

    public static readonly int[] PathfindingQualityMin = new int[]
    {
        50,  // [0] Low
        100, // [1] Normal
        100, // [2] High
        100, // [3] Ultra
    };

    public static readonly int[] PathfindingQualityMax = new int[]
    {
        100,  // [0] Low
        200,  // [1] Normal
        500,  // [2] High
        1000, // [3] Ultra
    };

    public static TimeSpan GetPlayerCacheExpiryTime()
    {
        return PlayerCacheExpiry[PlayerSettings.Instance.PlayerCacheExpiryTime ?? ^1]; // PlayerPrefs.GetInt(SettingsName_PlayerCacheExpiryTime, 1)
    }

    private void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

        UpdateSettingsUI();
        ShowGameSettings();
    }

    public void UpdateSettingsUI()
    {
        var settings = PlayerSettings.Instance;

        postProcessingToggle.isOn = settings.PostProcessing.GetValueOrDefault(gameManager.UsePostProcessingEffects);
        potatoModeToggle.isOn = settings.PotatoMode.GetValueOrDefault(gameManager.PotatoMode && !gameManager.AutoPotatoMode);
        autoPotatoModeToggle.isOn = settings.AutoPotatoMode.GetValueOrDefault(gameManager.AutoPotatoMode);
        realtimeDayNightCycle.isOn = settings.RealTimeDayNightCycle.GetValueOrDefault(gameManager.RealtimeDayNightCycle);
        playerListSizeSlider.value = settings.PlayerListSize.GetValueOrDefault(gameManager.PlayerList.Bottom);
        playerListScaleSlider.value = settings.PlayerListScale.GetValueOrDefault(gameManager.PlayerList.Scale);

        dpiSlider.value = settings.DPIScale.GetValueOrDefault(1f);

        viewDistanceSlider.value = settings.ViewDistance.GetValueOrDefault(0.5f);

        raidHornVolumeSlider.value = settings.RaidHornVolume.GetValueOrDefault(gameManager.Raid.Notifications.volume);
        musicVolumeSlider.value = settings.MusicVolume.GetValueOrDefault(gameManager.Music.volume);
        
        observerCameraRotationSlider.SetValueWithoutNotify(settings.CameraRotationSpeed.GetValueOrDefault(OrbitCamera.RotationSpeed * -1));

        pathfindingQuality.value = settings.PathfindingQualitySettings.GetValueOrDefault(1);
        playerCacheExpiryTimeDropdown.value = settings.PlayerCacheExpiryTime.GetValueOrDefault(SettingsMenuView.PlayerCacheExpiry.Length - 1);
        boostRequirementDropdown.value = settings.PlayerBoostRequirement.GetValueOrDefault(gameManager.PlayerBoostRequirement);
        qualityLevelDropdown.value = settings.QualityLevel.GetValueOrDefault(1);

        disableRaidsToggle.isOn = settings.DisableRaids.GetValueOrDefault();
        disableDungeonsToggle.isOn = settings.DisableDungeons.GetValueOrDefault();

        alertPlayerCacheExpirationToggle.isOn = settings.AlertExpiredStateCacheInChat.GetValueOrDefault(gameManager.AlertExpiredStateCacheInChat);
        streamLabelsEnabled.isOn = settings.StreamLabels.Enabled;

        autoAssignVacantHousesEnabled.isOn = settings.AutoAssignVacantHouses.GetValueOrDefault(true);
        debugControlsEnabled.isOn = gameManager.isDebugMenuVisible;

        playerNameToggle.isOn = settings.PlayerNamesVisible.GetValueOrDefault(gameManager.PlayerNamesVisible);
        itemDropMessageDropdown.value = settings.ItemDropMessageType.GetValueOrDefault((int)gameManager.ItemDropMessageSettings);

        dayNightCycleEnabled.isOn = settings.DayNightCycleEnabled.GetValueOrDefault(true);
        dayNightTime.value = settings.DayNightTime.GetValueOrDefault(0.5f);

        playerDetailsToggle.isOn = gameManager.Camera.Observer.IsVisible;
        playerListToggle.isOn = gameManager.PlayerList.IsVisible;

        SetViewDistance(viewDistanceSlider.value);

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
        {
            SetResolutionScale(dpiSlider.value);
        }
        else
        {
            dpiSlider.transform.parent.gameObject.SetActive(false);
        }
        ShowItemDropExample();
        UpdateCameraRotationLabelText();
        ApplyCameraRotationSpeed();
        OnSliderValueChanged();
    }

    private void ApplyCameraRotationSpeed()
    {
        // negative value is rotating to the right
        // positive value is rotating to the left
        // but since slider shows left as negative and right as positive
        // we have to invert the value.

        IslandObserveCamera.RotationSpeed = OrbitCamera.RotationSpeed = observerCameraRotationSlider.value * -1;
    }

    private void UpdateCameraRotationLabelText()
    {
        var currentValue = observerCameraRotationSlider.value;
        var displayText = currentValue.ToString();
        if (currentValue == 0)
        {
            displayText = "No rotation";
        }
        if (currentValue > 10)
        {
            displayText += " >>";
        }
        else if (currentValue > 0)
        {
            displayText += " >";
        }
        if (currentValue < -10)
        {
            displayText = "<< " + currentValue;
        }
        else if (currentValue < 0)
        {
            displayText = "< " + currentValue;
        }
        observerCameraRotationLabel.text = displayText;
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

        settings.DayNightCycleEnabled = dayNightCycleEnabled.isOn;
        settings.DayNightTime = dayNightTime.value;

        settings.DisableRaids = disableRaidsToggle.isOn;
        settings.DisableDungeons = disableDungeonsToggle.isOn;

        settings.PotatoMode = potatoModeToggle.isOn;
        settings.PostProcessing = postProcessingToggle.isOn;
        settings.AutoPotatoMode = autoPotatoModeToggle.isOn;
        settings.RealTimeDayNightCycle = realtimeDayNightCycle.isOn;
        settings.PlayerListSize = playerListSizeSlider.value;
        settings.PlayerListScale = playerListScaleSlider.value;
        settings.RaidHornVolume = raidHornVolumeSlider.value;
        settings.MusicVolume = musicVolumeSlider.value;
        settings.CameraRotationSpeed = observerCameraRotationSlider.value;
        settings.DPIScale = dpiSlider.value;
        settings.PlayerBoostRequirement = boostRequirementDropdown.value;
        settings.QualityLevel = qualityLevelDropdown.value;
        settings.PlayerCacheExpiryTime = playerCacheExpiryTimeDropdown.value;
        settings.PathfindingQualitySettings = pathfindingQuality.value;
        settings.AlertExpiredStateCacheInChat = alertPlayerCacheExpirationToggle.isOn;
        settings.StreamLabels.Enabled = streamLabelsEnabled.isOn;
        settings.AutoAssignVacantHouses = autoAssignVacantHousesEnabled.isOn;
        settings.ItemDropMessageType = itemDropMessageDropdown.value;
        settings.CameraRotationSpeed = observerCameraRotationSlider.value * -1;
        settings.PlayerNamesVisible = playerNameToggle.isOn;
        settings.ViewDistance = viewDistanceSlider.value;

        gameManager.PlayerList.SetVisibility(playerListToggle.isOn);
        gameManager.Camera.Observer.SetVisibility(playerDetailsToggle.isOn);
        gameManager.isDebugMenuVisible = debugControlsEnabled.isOn;
        PlayerSettings.Save();
    }

    public void DisconnectFromServer()
    {
        gameManager.RavenNest.Tcp.Disconnect();
    }

    public void ShowSoundSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(true);
        game.gameObject.SetActive(false);
        admin.gameObject.SetActive(false);
    }
    public void ShowGraphicsSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(true);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(false);
        admin.gameObject.SetActive(false);
    }
    public void ShowGameSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(true);
        admin.gameObject.SetActive(false);
    }
    public void ShowUISettings()
    {
        ui.gameObject.SetActive(true);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(false);
        admin.gameObject.SetActive(false);
    }

    public void ShowAdminSettings()
    {
        ui.gameObject.SetActive(false);
        graphics.gameObject.SetActive(false);
        sounds.gameObject.SetActive(false);
        game.gameObject.SetActive(false);
        admin.gameObject.SetActive(true);
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

    public void OnDayNightCycleEnabledChanged()
    {
        gameManager.DayNightCycleEnabled = dayNightCycleEnabled.isOn;
    }

    public void OnPostProcessingEffectsChanged()
    {
        gameManager.UsePostProcessingEffects = postProcessingToggle.isOn;
    }

    public void OnPlayerNamesToggle()
    {
        gameManager.PlayerNamesVisible = playerNameToggle.isOn;
    }

    public void OnPlayerListToggle()
    {
        gameManager.PlayerList.SetVisibility(playerListToggle.isOn);
    }

    public void OnPlayerDetailsToggle()
    {
        gameManager.Camera.Observer.SetVisibility(playerDetailsToggle.isOn);
    }

    public void OnBoostRequirementChanged(int val)
    {
        gameManager.PlayerBoostRequirement = boostRequirementDropdown.value;
    }

    public void OnQualityLevelChanged(int val)
    {
        var settings = PlayerSettings.Instance;
        settings.QualityLevel = qualityLevelDropdown.value;
    }

    public void OnNavigationQualityChanged(int val)
    {
        var settings = PlayerSettings.Instance;
        //gameManager.PlayerBoostRequirement = boostRequirementDropdown.value;
        settings.PathfindingQualitySettings = pathfindingQuality.value;
        gameManager.UpdatePathfindingIterations();
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

    public void SetViewDistance(float value)
    {
        var camera = gameManager.Camera;
        camera.FarClipDistance = Mathf.Lerp(camera.MinFarClipDistance, camera.MaxFarClipDistance, value);
    }

    public void OnSliderValueChanged()
    {
        if (dpiSlider != null)
        {
            UpdateResolutionScale();
        }

        if (dayNightTime != null)
        {
            gameManager.DayNightCycleProgress = dayNightTime.value;
        }

        if (musicVolumeSlider != null)
        {
            gameManager.Music.volume = musicVolumeSlider.value;
        }

        if (viewDistanceSlider != null)
        {
            SetViewDistance(viewDistanceSlider.value);
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

        if (observerCameraRotationSlider != null)
        {
            ApplyCameraRotationSpeed();
            UpdateCameraRotationLabelText();
        }
    }

    private void UpdateResolutionScale()
    {
        SetResolutionScale(dpiSlider.value);
    }


    public static void SetResolutionScale(float factor)
    {
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
            return;

        factor = Mathf.Clamp(factor, 0.05f, 1f);
        QualitySettings.resolutionScalingFixedDPIFactor = factor;
        ScalableBufferManager.ResizeBuffers(factor, factor);
    }

    private void OnEnable()
    {
        if (!gameManager)
        {
            adminButton.SetActive(false);
            return;
        }

        if (gameManager.SessionSettings != null && gameManager.SessionSettings.IsAdministrator)
        {
            adminButton.SetActive(true);
        }
        else
        {
            adminButton.SetActive(false);
        }
    }

    private void OnDisable()
    {

    }
}