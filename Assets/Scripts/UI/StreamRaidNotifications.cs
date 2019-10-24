using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StreamRaidNotifications : MonoBehaviour
{
    [SerializeField] private GameObject raidBossAppeared;
    [SerializeField] private PlayerLogoManager logoManager;

    [SerializeField] private GameObject raidersWin;
    [SerializeField] private GameObject defendersWin;

    private AudioSource audioSource;

    private void Start()
    {
        if (!logoManager) logoManager = FindObjectOfType<PlayerLogoManager>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        HideRaidInfo();
    }

    public void HideRaidInfo()
    {
        if (raidBossAppeared) raidBossAppeared.SetActive(false);
        if (raidersWin) raidersWin.SetActive(false);
        if (defendersWin) raidersWin.SetActive(false);
    }

    public void ShowIncomingRaid(string message)
    {
        if (!raidBossAppeared)
        {
            Debug.LogError("No Raid Message set on Raid Notifications");
            return;
        }

        raidBossAppeared.GetComponentInChildren<TextMeshProUGUI>().text = message;

        if (audioSource) audioSource.Play();

        raidBossAppeared.SetActive(true);
        raidBossAppeared.GetComponent<AutoHideUI>()?.Reset();
    }

    internal void ShowDefendersWon()
    {
        Debug.Log("ShowDefendersWon");
        if (defendersWin)
        {
            defendersWin.GetComponent<AutoHideUI>()?.Reset();

        }
    }

    internal void ShowRaidersWon()
    {
        Debug.Log("ShowRaidersWon");
        if (raidersWin)
        {
            raidersWin.GetComponent<AutoHideUI>()?.Reset();
        }
    }
}
