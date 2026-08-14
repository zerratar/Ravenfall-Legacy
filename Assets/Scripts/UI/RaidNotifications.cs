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
    [SerializeField] private TextMeshProUGUI lblComeJoinText;

    [SerializeField] private TextMeshProUGUI lbLRaidBossLevel;

    /// <summary>
    /// Optional UI Toolkit replacement for the "A boss appeared" banner. When assigned, the
    /// announcement is drawn as text from Localization instead of the RaidBoss.png image, which is
    /// what makes it translatable.
    ///
    /// <para>
    /// Only the announcement moves. The raid HUD underneath it, boss health, timer and level, is
    /// persistent UI rather than an announcement and stays on uGUI for now.
    /// </para>
    /// </summary>
    [SerializeField] private Shinobytes.UI.NotificationScreenView uiToolkitScreen;

    /// <summary>The chat command viewers type to join. Highlighted in the announcement.</summary>
    private const string JoinCommand = "!raid";

    private AudioSource audioSource;
    private string raidSound = "raid.mp3";
    private string joinStringFormat;
    private Vector3 raidTimerStartPos;

    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Start()
    {

        this.joinStringFormat = lblComeJoinText.text;
        if (!raidTimer) raidTimer = GetComponentInChildren<TextMeshProUGUI>();
        //if (!comeJoinImage) comeJoinImage = raidBossHud.transform.Find("Image").GetComponent<Image>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        raidTimerStartPos = raidTimer.rectTransform.localPosition;
        audioSource.volume = PlayerPrefs.GetFloat(SettingsMenuView.SettingsName_RaidHornVolume, 1f);

        HideRaidInfo();
    }

    public void HideRaidInfo()
    {
        if (uiToolkitScreen != null) uiToolkitScreen.Hide();
        if (raidBossAppeared) raidBossAppeared.SetActive(false);
        if (raidBossHealth) raidBossHud.SetActive(false);

        // This happens after the raid. but it should be fine
        ExternalResources.ReloadIfModifiedAsync(raidSound);
    }

    public void HideRaidJoinInfo()
    {
        lblComeJoinText.enabled = false;
        raidTimer.rectTransform.localPosition = new Vector3(0, 112f);
    }

    private void ShowRaidJoinInfo(string code)
    {
        lblComeJoinText.enabled = true;
        lblComeJoinText.text = String.Format(joinStringFormat, code).Replace("  ", " ");
        raidTimer.rectTransform.localPosition = raidTimerStartPos;
    }

    public void ShowRaidBossAppeared(string code)
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

        // The HUD comes up either way; it is not part of the announcement.
        raidBossHud.SetActive(true);

        if (uiToolkitScreen != null)
        {
            // ShowRaidJoinInfo is still called because it also restores the raid timer's position
            // in the HUD, which is layout rather than announcement. The old join label is then
            // switched off so the join instruction is not shown twice, once in the HUD and once in
            // the announcement.
            ShowRaidJoinInfo(code);
            if (lblComeJoinText) lblComeJoinText.enabled = false;

            var joinLine = string.Format(joinStringFormat, code).Replace("  ", " ");
            if (string.IsNullOrWhiteSpace(joinLine) || joinLine.Trim() == code)
            {
                // The scene's format string is the source of this text, so fall back to the
                // localized wording if it turns out to be empty.
                joinLine = Shinobytes.UI.NotificationScreenView.WithCommand(
                    Localization.UI_RAID_JOIN, JoinCommand);
            }

            uiToolkitScreen.Show(
                Localization.UI_RAID_BOSS_APPEARED,
                joinLine,
                Localization.UI_RAID_REWARD_HINT);
            return;
        }

        raidBossAppeared.SetActive(true);
        ShowRaidJoinInfo(code);

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
