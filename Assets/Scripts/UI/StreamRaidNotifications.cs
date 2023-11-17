using TMPro;
using UnityEngine;

public class StreamRaidNotifications : MonoBehaviour
{
    [SerializeField] private GameObject raidBossAppeared;
    [SerializeField] private PlayerLogoManager logoManager;

    [SerializeField] private GameObject raidersWin;
    [SerializeField] private GameObject defendersWin;

    private AudioSource audioSource;
    private string raidSound = "streamraid.mp3";
    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Start()
    {
        if (!logoManager) logoManager = FindAnyObjectByType<PlayerLogoManager>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("RaidHornVolume", 1f);
        HideRaidInfo();
    }

    public void HideRaidInfo()
    {
        ExternalResources.ReloadIfModifiedAsync(raidSound);
        if (raidBossAppeared) raidBossAppeared.SetActive(false);
        if (raidersWin) raidersWin.SetActive(false);
        if (defendersWin) raidersWin.SetActive(false);
    }

    public void ShowIncomingRaid(string message)
    {
        if (!raidBossAppeared)
        {
            Shinobytes.Debug.LogError("No Raid Message set on Raid Notifications");
            return;
        }

        raidBossAppeared.GetComponentInChildren<TextMeshProUGUI>().text = message;

        if (audioSource)
        {
            var o = ExternalResources.GetAudioClip(raidSound);
            if (o != null) audioSource.clip = o;
            audioSource.Play();
        }

        raidBossAppeared.SetActive(true);
        raidBossAppeared.GetComponent<AutoHideUI>()?.Reset();
    }

    internal void ShowDefendersWon()
    {
        Shinobytes.Debug.Log("ShowDefendersWon");
        if (defendersWin)
        {
            defendersWin.GetComponent<AutoHideUI>()?.Reset();
        }
    }

    internal void ShowRaidersWon()
    {
        Shinobytes.Debug.Log("ShowRaidersWon");
        if (raidersWin)
        {
            raidersWin.GetComponent<AutoHideUI>()?.Reset();
        }
    }
}
