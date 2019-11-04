using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaidManager : MonoBehaviour
{
    [SerializeField] private GameCamera camera;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RaidNotifications notifications;
    [SerializeField] private GameObject raidBossPrefab;

    [SerializeField] private float minTimeBetweenRaids = 600;
    [SerializeField] private float maxTimeBetweenRaids = 3000;
    [SerializeField] private float minHealthToJoin = 0.1f;

    private readonly List<PlayerController> raidingPlayers
        = new List<PlayerController>();

    private readonly object mutex = new object();

    private float nextRaidTimer = 0f;

    private float minTimeoutSeconds = 90f;
    private float maxTimeoutSeconds = 900f;
    private float timeoutTimer = 0f;

    private float raidStartedTime = 0f;
    private float raidEndedTime = 0f;

    public IReadOnlyList<PlayerController> Raiders { get { lock (mutex) return raidingPlayers; } }
    public RaidBossController Boss { get; private set; }
    public RaidNotifications Notifications => notifications;

    //private float lastRaidEndTime = 0f;

    public bool Started => nextRaidTimer < 0f;

    private void Start()
    {
        nextRaidTimer = UnityEngine.Random.Range(minTimeBetweenRaids, maxTimeBetweenRaids);
    }

    public RaidJoinResult CanJoin(PlayerController player)
    {
        if (!Started || !Boss || !Boss.Enemy)
        {
            return RaidJoinResult.NoActiveRaid;
        }

        if (raidingPlayers.Contains(player))
        {
            return RaidJoinResult.AlreadyJoined;
        }

        var currentHealth = (float)Boss.Enemy.Stats.Health.CurrentValue;
        var maxHealth = Boss.Enemy.Stats.Health.Level;
        if (currentHealth / maxHealth < minHealthToJoin)
        {
            return RaidJoinResult.MinHealthReached;
        }

        return RaidJoinResult.CanJoin;
    }

    public void Join(PlayerController player)
    {
        if (!Started)
        {
            return;
        }

        lock (mutex)
        {
            if (!raidingPlayers.Remove(player))
            {
                raidingPlayers.Add(player);
            }
        }

        player.Raid.OnEnter();
    }

    public void Leave(PlayerController player, bool reward = false, bool timeout = false)
    {
        lock (mutex)
        {
            if (raidingPlayers.Remove(player))
            {
                player.Raid.OnLeave(reward, timeout);
            }
        }
    }

    public void StartRaid(string initiator = null)
    {
        gameManager.Music.PlayRaidBossMusic();

        if (!notifications.gameObject.activeSelf) notifications.gameObject.SetActive(true);

        nextRaidTimer = -1f;
        raidStartedTime = Time.time;
        camera.EnableRaidCamera();

        SpawnRaidBoss();

        notifications.ShowRaidBossAppeared();

        gameManager.Server?.Client?.SendCommand(
            "", "raid_start",
            $"A level {Boss.Enemy.Stats.CombatLevel} raid boss has appeared! Help fight him by typing !raid");
    }

    public void EndRaid(bool bossKilled, bool timeout)
    {
        gameManager.Music.PlayBackgroundMusic();

        raidEndedTime = Time.time;
        camera.DisableFocusCamera();
        nextRaidTimer = UnityEngine.Random.Range(minTimeBetweenRaids, maxTimeBetweenRaids);
        notifications.HideRaidInfo();

        lock (mutex)
        {
            var playersToLeave = raidingPlayers.ToList();
            foreach (var player in playersToLeave)
            {
                Leave(player, bossKilled, timeout);
            }
        }

        if (!Boss.RaidBossControlsDestroy || timeout)
        {
            Destroy(Boss.gameObject);
        }
    }

    public float GetParticipationPercentage(float enterTime)
    {
        var participationTime = raidEndedTime - enterTime;
        return participationTime / (raidEndedTime - raidStartedTime);
    }

    // Update is called once per frame
    private void Update()
    {
        var players = playerManager.GetAllPlayers();
        if (!Started && players.Count == 0)
        {
            return;
        }

        if (nextRaidTimer > 0f)
        {
            nextRaidTimer -= Time.deltaTime;
            if (nextRaidTimer <= 0f)
            {
                StartRaid();
            }
        }

        if (notifications && Boss)
        {
            timeoutTimer -= Time.deltaTime;
            if (timeoutTimer <= 0f && Boss.Enemy.Stats.Health.CurrentValue > 0)
            {
                EndRaid(false, true);
                return;
            }

            var proc = (float)Boss.Enemy.Stats.Health.CurrentValue / Boss.Enemy.Stats.Health.Level;
            if (proc < minHealthToJoin)
            {
                notifications.HideRaidJoinInfo();
            }

            notifications.SetRaidBossLevel(Boss.Enemy.Stats.CombatLevel);
            notifications.SetHealthBarValue(proc);
            notifications.UpdateRaidTimer(timeoutTimer);
        }
    }

    private void SpawnRaidBoss()
    {
        if (!raidBossPrefab)
        {
            Debug.LogError("NO RAID BOSS PREFAB SET!!!");
            return;
        }

        var spawnPosition = Vector3.zero;
        if (chunkManager)
        {
            var randomChunk = chunkManager
                .GetChunks()
                .OrderBy(x => UnityEngine.Random.value)
                .FirstOrDefault();

            if (randomChunk != null)
            {
                spawnPosition = randomChunk.CenterPointWorld + (Vector3.up * 3.4f);
            }
        }

        var players = playerManager.GetAllPlayers();
        var highestStats = players.Max(x => x.Stats);
        var lowestStats = players.Min(x => x.Stats);
        var rngLowEq = players.Min(x => x.EquipmentStats);
        var rngHighEq = players.Max(x => x.EquipmentStats);

        Boss = Instantiate(raidBossPrefab, spawnPosition, Quaternion.identity).GetComponent<RaidBossController>();

        Boss.Create(lowestStats, highestStats, rngLowEq, rngHighEq);

        timeoutTimer = Mathf.Min(maxTimeoutSeconds, Mathf.Max(minTimeoutSeconds, Boss.Enemy.Stats.CombatLevel * 0.8249123f));
    }
}

public enum RaidJoinResult
{
    CanJoin,
    MinHealthReached,
    AlreadyJoined,
    NoActiveRaid
}
