using System;
using System.Threading.Tasks;
using UnityEngine;
public class DungeonNotifications : MonoBehaviour
{
    [SerializeField] private AutoHideUI activatedObject;
    [SerializeField] private AutoHideUI timerObject;
    [SerializeField] private GameProgressBar dungeonBossHealth;

    [SerializeField] private float timeBeforeTimer = 7f;
    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonLevel;
    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonTimer;

    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonJoin;
    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonJoin2;


    [SerializeField] private TMPro.TextMeshProUGUI lblDungeonActiveTimer;
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayers;
    [SerializeField] private TMPro.TextMeshProUGUI lblEnemies;

    [SerializeField] private GameObject dungeonDetailsObject;

    [Tooltip("Optional UI Toolkit panel. When assigned it shows the dungeon instead of the uGUI objects above, which stay hidden. The horn, stream labels and dungeon stats work either way. Clear it to bring the old UI back.")]
    [SerializeField] private Shinobytes.UI.DungeonHudView hudView;


    private string lblDungeonJoinFormat;
    private string lblDungeonJoin2Format;

    private float timer = 0;
    private int dungeonCount;
    private AudioSource audioSource;
    private string lblDungeonActiveTimerFormat;
    private string lblPlayersFormat;
    private string lblEnemiesFormat;
    private float runTime;

    private string dungeonSound = "dungeon.mp3";

    private GameManager gameManager;

    private StreamLabel amountOfDungeonsRunLabel;
    private StreamLabel playersLeftLabel;
    private StreamLabel enemiesLeftLabel;
    private StreamLabel runTimeLabel;
    private StreamLabel bossHealthLabel;
    private StreamLabel bossLevelLabel;

    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Awake()
    {
        this.lblDungeonJoinFormat = lblDungeonJoin.text;
        this.lblDungeonJoin2Format = lblDungeonJoin2.text;

        timer = timeBeforeTimer;

        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("RaidHornVolume", 1f);
        HideInformation();
    }

    private void Start()
    {
        this.gameManager = FindAnyObjectByType<GameManager>();
    }
    private void Update()
    {
        if (hudView) return;

        if (activatedObject.gameObject.activeSelf)
        {
            timeBeforeTimer -= GameTime.deltaTime;
            if (timeBeforeTimer <= 0f)
            {
                StartTimer();
            }
        }
    }

    private void StartTimer()
    {
        ExternalResources.ReloadIfModifiedAsync(dungeonSound);
        timeBeforeTimer = timer;
        activatedObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
        timerObject.gameObject.SetActive(true);
        timerObject.Reset();
    }

    public void ShowDungeonActivated(string code)
    {
        lblDungeonJoin.text = string.Format(lblDungeonJoinFormat, code).Replace("  ", " ");
        lblDungeonJoin2.text = string.Format(lblDungeonJoin2Format, code).Replace("  ", " ");

        if (audioSource)
        {
            var o = ExternalResources.GetAudioClip(dungeonSound);
            if (o != null) audioSource.clip = o;
            audioSource.Play();
        }
        ++dungeonCount;

        if (hudView)
        {
            hudView.Announce(code);
            return;
        }

        timerObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
        activatedObject.gameObject.SetActive(true);
    }

    public void SetLevel(int level)
    {
        if (hudView) return;
        lblDungeonLevel.text = $"Lv. <b>{level}";
    }

    public void SetTimeout(float timeout)
    {
        if (hudView) return;
        timerObject.Timeout = timeout;
        timerObject.Reset();
    }

    public void SetHealthBarValue(float proc, float maxValue = 1f)
    {
        if (hudView) return;
        if (proc > 0f) ShowHealthBar();
        dungeonBossHealth.Progress = proc;
        dungeonBossHealth.MaxValue = maxValue;
        if (proc <= 0f) HideHealthBar();
    }

    public void ShowHealthBar()
    {
        if (hudView) return;
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
        if (hudView) return;
        lblDungeonTimer.text = $"{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
    }

    public void Hide()
    {
        ExternalResources.ReloadIfModifiedAsync(dungeonSound);

        activatedObject.gameObject.SetActive(false);
        timerObject.gameObject.SetActive(false);
        dungeonBossHealth.gameObject.SetActive(false);
    }

    internal void HideInformation()
    {
        Hide();

        if (dungeonDetailsObject)
        {
            dungeonDetailsObject.SetActive(false);
        }

        runTime = 0;
    }

    internal void UpdateInformation(int alivePlayerCount, int aliveEnemyCount, DungeonBossController boss)
    {
        if (!dungeonDetailsObject) return;

        // With the UI Toolkit panel in use the details object stays hidden, but the run time, stream
        // labels and dungeon stats below are still kept up to date.
        if (!hudView && !dungeonDetailsObject.activeSelf)
        {
            dungeonDetailsObject.SetActive(true);

            if (string.IsNullOrEmpty(lblDungeonActiveTimerFormat))
                lblDungeonActiveTimerFormat = lblDungeonActiveTimer.text;

            if (string.IsNullOrEmpty(lblPlayersFormat))
                lblPlayersFormat = lblPlayers.text;

            if (string.IsNullOrEmpty(lblEnemiesFormat))
                lblEnemiesFormat = lblEnemies.text;
        }

        runTime += GameTime.deltaTime;

        if (!hudView)
        {
            lblDungeonActiveTimer.text = string.Format(lblDungeonActiveTimerFormat, Utility.FormatTime(runTime / 60f / 60f));
            lblPlayers.text = string.Format(lblPlayersFormat, alivePlayerCount);
            lblEnemies.text = string.Format(lblEnemiesFormat, aliveEnemyCount);
        }

        var dungeonStats = gameManager.GetDungeonStats();

        dungeonStats.ActivatedCount = this.dungeonCount;
        dungeonStats.PlayersLeft = alivePlayerCount;
        dungeonStats.EnemiesLeft = aliveEnemyCount;
        dungeonStats.Runtime = runTime;

        if (this.amountOfDungeonsRunLabel == null)
            this.amountOfDungeonsRunLabel = gameManager.StreamLabels.RegisterText("dungeon-count", () => this.dungeonCount.ToString());
        this.amountOfDungeonsRunLabel.Update();

        if (this.playersLeftLabel == null)
            this.playersLeftLabel = gameManager.StreamLabels.RegisterText("dungeon-players-left", () => alivePlayerCount.ToString());
        this.playersLeftLabel.Update();


        if (this.enemiesLeftLabel == null)
            this.enemiesLeftLabel = gameManager.StreamLabels.RegisterText("dungeon-enemies-left", () => aliveEnemyCount.ToString());
        this.enemiesLeftLabel.Update();

        if (this.runTimeLabel == null)
            this.runTimeLabel = gameManager.StreamLabels.RegisterText("dungeon-run-time", () => runTime.ToString());
        if (GameSystems.frameCount % 30 == 0)
        {
            this.runTimeLabel.Update();
        }

        if (this.bossHealthLabel == null)
            this.bossHealthLabel = gameManager.StreamLabels.RegisterText("dungeon-boss-health", () =>
            {
                if (boss && !boss.Enemy.Stats.IsDead)
                {
                    return Math.Round(boss.Enemy.Stats.HealthPercent * 100f, 2) + "%";
                }

                return "Dead";
            });
        this.bossHealthLabel.Update();

        if (this.bossLevelLabel == null)
            this.bossLevelLabel = gameManager.StreamLabels.RegisterText("dungeon-boss-level", () =>
            {
                if (boss && !boss.Enemy.Stats.IsDead)
                {
                    return boss.Enemy.Stats.CombatLevel.ToString();
                }

                return "";
            });
        this.bossLevelLabel.Update();
    }

    internal async Task OnDungeonActivated()
    {
        await ExternalResources.ReloadIfModifiedAsync(dungeonSound);
    }
}
