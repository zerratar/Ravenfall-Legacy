using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaidNotifications : MonoBehaviour
{
    [SerializeField] private GameObject raidBossAppeared;
    [SerializeField] private GameObject raidBossHud;
    [SerializeField] private GameProgressBar raidBossHealth;
    [SerializeField] private TextMeshProUGUI raidTimer;
    [SerializeField] private Image comeJoinImage;

    [SerializeField] private TextMeshProUGUI lbLRaidBossLevel;

    private AudioSource audioSource;
    private string raidSound = "raid.mp3";
    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Start()
    {
        if (!raidTimer) raidTimer = GetComponentInChildren<TextMeshProUGUI>();
        if (!comeJoinImage) comeJoinImage = raidBossHud.transform.Find("Image").GetComponent<Image>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        audioSource.volume = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_RaidHornVolume, 1f);

        HideRaidInfo();
    }

    public void HideRaidInfo()
    {
        if (raidBossAppeared) raidBossAppeared.SetActive(false);
        if (raidBossHealth) raidBossHud.SetActive(false);
        
        // This happens after the raid. but it should be fine
        ExternalResources.ReloadIfModifiedAsync(raidSound);
    }

    public void HideRaidJoinInfo()
    {
        comeJoinImage.enabled = false;
        raidTimer.rectTransform.localPosition = new Vector3(0, 15f);
    }

    private void ShowRaidJoinInfo()
    {
        comeJoinImage.enabled = true;
        raidTimer.rectTransform.localPosition = new Vector3(0, 120);
    }

    public void ShowRaidBossAppeared()
    {
        if (!raidBossAppeared)
        {
            Shinobytes.Debug.LogError("No Raid Boss Message set on Raid Notifications");
            return;
        }

        if (audioSource)
        {
            var o = ExternalResources.GetAudioClip(raidSound);
            if (o != null) audioSource.clip = o;
            audioSource.Play();
        }

        raidBossAppeared.SetActive(true);
        raidBossHud.SetActive(true);

        ShowRaidJoinInfo();

        var autoHide = raidBossAppeared.GetComponent<AutoHideUI>();
        if (autoHide)
        {
            autoHide.Reset();
        }
    }

    public void SetHealthBarValue(float proc, float maxValue = 1f)
    {
        raidBossHealth.MaxValue = maxValue;
        raidBossHealth.Progress = proc;
    }

    public void UpdateRaidTimer(float timeoutTimer)
    {
        if (!raidTimer)
        {
            return;
        }

        raidTimer.text = $"{Mathf.FloorToInt(timeoutTimer)} seconds left";
    }

    public void SetRaidBossLevel(int combatLevel)
    {
        if (lbLRaidBossLevel) lbLRaidBossLevel.text = $"Lv: <b>{combatLevel}";
    }

    internal void OnBeforeRaidStart()
    {
        ExternalResources.ReloadIfModifiedAsync(raidSound);
    }
}
