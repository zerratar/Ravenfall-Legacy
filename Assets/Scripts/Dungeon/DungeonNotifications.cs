using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonNotifications : MonoBehaviour
{
    [SerializeField] private AutoHideUI activatedObject;
    [SerializeField] private AutoHideUI timerObject;
    [SerializeField] private GameProgressBar dungeonBossHealth;

    [SerializeField] private float timeBeforeTimer = 7f;
    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonLevel;
    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonTimer;

    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonActiveTimer;
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayers;
    [SerializeField] private TMPro.TextMeshProUGUI lblEnemies;

    [SerializeField] private GameObject dungeonDetailsObject;

    private float timer = 0;

    private AudioSource audioSource;
    private string lblDungeonActiveTimerFormat;
    private string lblPlayersFormat;
    private string lblEnemiesFormat;
    private float runTime;

    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Awake()
    {
        timer = timeBeforeTimer;

        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("RaidHornVolume", 1f);

        HideInformation();
    }

    private void Update()
    {
        if (activatedObject.gameObject.activeSelf)
        {
            timeBeforeTimer -= Time.deltaTime;
            if (timeBeforeTimer <= 0f)
            {
                StartTimer();
            }
        }
    }

    private void StartTimer()
    {
        timeBeforeTimer = timer;
        activatedObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
        timerObject.gameObject.SetActive(true);
        timerObject.Reset();
    }

    public void ShowDungeonActivated()
    {
        if (audioSource) audioSource.Play();
        timerObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
        activatedObject.gameObject.SetActive(true);
    }

    public void SetLevel(int level)
    {
        lblDungeonLevel.text = $"Lv. <b>{level}";
    }

    public void SetTimeout(float timeout)
    {
        timerObject.Timeout = timeout;
        timerObject.Reset();
    }

    public void SetHealthBarValue(float proc, float maxValue = 1f)
    {
        if (proc > 0f) ShowHealthBar();
        dungeonBossHealth.Progress = proc;
        dungeonBossHealth.MaxValue = maxValue;
        if (proc <= 0f) HideHealthBar();
    }

    public void ShowHealthBar()
    {
        if (!dungeonBossHealth.gameObject.activeSelf)
            dungeonBossHealth.gameObject.SetActive(true);
    }

    public void HideHealthBar()
    {
        if (dungeonBossHealth.gameObject.activeSelf)
            dungeonBossHealth.gameObject.SetActive(false);
    }

    public void UpdateTimer(TimeSpan timeLeft)
    {
        lblDungeonTimer.text = $"{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
    }

    public void Hide()
    {
        activatedObject.gameObject.SetActive(false);
        timerObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
    }

    internal void HideInformation()
    {
        if (!dungeonDetailsObject)
            return;

        dungeonDetailsObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
        runTime = 0;
    }

    internal void UpdateInformation(int alivePlayerCount, int aliveEnemyCount, DungeonBossController boss)
    {
        if (!dungeonDetailsObject) return;
        if (!dungeonDetailsObject.activeSelf)
        {
            dungeonDetailsObject.SetActive(true);

            if (string.IsNullOrEmpty(lblDungeonActiveTimerFormat))
                lblDungeonActiveTimerFormat = lblDungeonActiveTimer.text;

            if (string.IsNullOrEmpty(lblPlayersFormat))
                lblPlayersFormat = lblPlayers.text;

            if (string.IsNullOrEmpty(lblEnemiesFormat))
                lblEnemiesFormat = lblEnemies.text;
        }

        runTime += Time.deltaTime;

        lblDungeonActiveTimer.text = string.Format(lblDungeonActiveTimerFormat, Utility.FormatTime(runTime / 60f / 60f));
        lblPlayers.text = string.Format(lblPlayersFormat, alivePlayerCount);
        lblEnemies.text = string.Format(lblEnemiesFormat, aliveEnemyCount);
    }
}
