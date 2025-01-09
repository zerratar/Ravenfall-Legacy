using Assets.Scripts;
using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RaidManager : MonoBehaviour, IEvent
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

    private DateTime raidStartedTime;
    private DateTime raidEndedTime;

    public IReadOnlyList<PlayerController> Raiders { get { lock (mutex) return raidingPlayers; } }
    public RaidBossController Boss { get; private set; }
    public RaidNotifications Notifications => notifications;

    //private float lastRaidEndTime = 0f;

    public int Counter => raidIndex;

    public float SecondsLeft => timeoutTimer;
    public float SecondsUntilNextRaid => nextRaidTimer;
    public bool Started => nextRaidTimer < 0f;

    public bool IsBusy { get; internal set; }
    public bool HasBeenAnnounced { get; private set; }
    public PlayerController Initiator { get; private set; }

    public string EventName => "Raid " + Boss?.name ?? "Raid";
    public bool IsEventActive => Started || (Boss != null && Boss.Enemy && !Boss.Enemy.Stats.IsDead);

    public string RequiredCode;

    private RaidDifficultySystem difficultySystem = new RaidDifficultySystem();

    private int raidIndex = 0;

    private int isProcessingRewardQueue;

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

        lock (mutex)
        {
            if (raidingPlayers.Contains(player))
            {
                return RaidJoinResult.AlreadyJoined;
            }
        }

        var currentHealth = (float)Boss.Enemy.Stats.Health.CurrentValue;
        var maxHealth = Boss.Enemy.Stats.Health.MaxLevel;
        if (currentHealth / maxHealth < minHealthToJoin)
        {
            return RaidJoinResult.MinHealthReached;
        }

        return RaidJoinResult.CanJoin;
    }

    public RaidJoinResult CanJoin(PlayerController player, string code)
    {
        var canJoin = CanJoin(player);
        if (canJoin == RaidJoinResult.CanJoin && !string.IsNullOrEmpty(this.RequiredCode) && code != this.RequiredCode)
        {
            return RaidJoinResult.WrongCode;
        }

        return canJoin;
    }


    internal int GetPlayerCount()
    {
        return raidingPlayers.Count;
    }

    public void Join(PlayerController player)
    {
        if (!Started)
        {
            return;
        }

        if (!player || player == null)
        {
            return;
        }

        lock (mutex)
        {
            var bossHealth = Boss.Enemy.Stats.Health;
            if (raidingPlayers.Count == 0 || bossHealth.CurrentValue == bossHealth.MaxLevel)
            {
                // reset the start time until someone joins or boss health is the same.
                raidStartedTime = DateTime.Now;
            }

            player.InterruptAction();

            if (!raidingPlayers.Contains(player))
            {
                raidingPlayers.Add(player);
            }
        }

        player.EnsureComponents();
        player.raidHandler.OnEnter();

        // whenever a player joins, we take the sum of all combat skills and equipment
        // this needs to be recorded for every raid that occurs
        difficultySystem.Track(raidIndex, player);
        gameManager.raidStatsJson.Update();
    }

    public void Leave(PlayerController player, bool reward = false, bool timeout = false)
    {
        if (!player || player == null)
        {
            return;
        }

        lock (mutex)
        {
            if (raidingPlayers.Remove(player))
            {
                player.EnsureComponents();
                player.raidHandler.OnLeave(reward, timeout);
            }
        }

        // if user chose to leave themselves, update the raidstatsjson
        if (!reward && !timeout)
        {
            gameManager.raidStatsJson.Update();
        }
    }

    private bool StartRaid(bool hasInitiator, PlayerController? initiator = null, Action<string> onActivated = null)
    {
        if (gameManager.Events.TryStart(this, hasInitiator))
        {
            this.HasBeenAnnounced = false;

            if (gameManager.RequireCodeForDungeonOrRaid)
                RequiredCode = EventCode.New();

            var raidBossSpawned = SpawnRaidBoss();
            if (!raidBossSpawned)
            {
                Shinobytes.Debug.LogWarning("Raid boss failed to spawn. Raid will be cancelled.");
                gameManager.Events.End(this);
                return false;
            }

            Initiator = initiator;

            notifications.OnBeforeRaidStart();
            gameManager.Music.PlayRaidBossMusic();

            if (!notifications.gameObject.activeSelf)
                notifications.gameObject.SetActive(true);

            nextRaidTimer = -1f;
            raidStartedTime = DateTime.Now;
            camera.EnableRaidCamera();

            notifications.ShowRaidBossAppeared(RequiredCode);

            gameManager.raidStatsJson.Update();

            if (onActivated != null)
            {
                onActivated(RequiredCode);
            }
            else
            {
                Announce();
            }

            AutoJoinPlayers();

            return true;
        }
        else if (initiator != null)
        {
            gameManager.RavenBot?.Announce(Localization.MSG_RAID_START_ERROR);
        }
        else
        {
            if (gameManager.Events.IsEventCooldownActive)
            {
                Shinobytes.Debug.LogWarning("Raid could not be started. There is an active cooldown. " + gameManager.Events.EventCooldownTimeLeft + " seconds left.");
            }
            else
            {
                Shinobytes.Debug.LogWarning("Raid could not be started.");
            }
        }

        nextRaidTimer = gameManager.Events.RescheduleTime;
        return false;
    }


    private void AutoJoinPlayers()
    {
        var playersToJoin = new List<PlayerController>();
        var autoJoinCost = gameManager.SessionSettings.AutoJoinRaidCost;
        foreach (var player in gameManager.Players.GetAllPlayers())
        {
            if (/*player.PatreonTier <= 0 ||*/ player.IsBot ||
                CanJoin(player) != RaidJoinResult.CanJoin || Initiator == player
                || player.raidHandler.AutoJoinCounter <= 0
                || autoJoinCost > player.Resources.Coins)
            {
                continue;
            }

            if (player.raidHandler.AutoJoinCounter != int.MaxValue)
            {
                player.raidHandler.AutoJoinCounter--;
            }

            player.Resources.Coins -= autoJoinCost;
            player.raidHandler.AutoJoinCount++;

            playersToJoin.Add(player);
            Join(player);
        }

        if (playersToJoin.Count > 0)
        {
            gameManager.AnnounceAutoRaidJoin(playersToJoin);
        }
    }


    public bool ForceStartRaid()
    {
        return StartRaid(true, null, null);
    }

    public bool StartRaid(PlayerController initiator = null, Action<string> onActivated = null)
    {
        return StartRaid(initiator != null, initiator, onActivated);
    }

    public void Announce()
    {
        if (gameManager.RequireCodeForDungeonOrRaid)
        {
            gameManager.RavenBot?.Announce(Localization.MSG_RAID_START_CODE, Boss.Enemy.Stats.CombatLevel.ToString());
        }
        else
        {
            gameManager.RavenBot?.Announce(Localization.MSG_RAID_START, Boss.Enemy.Stats.CombatLevel.ToString());
        }
        this.HasBeenAnnounced = true;
    }

    public void EndRaid(bool bossKilled, bool timeout)
    {
        try
        {
            if (!bossKilled && timeout)
            {
                gameManager.RavenBot.Announce("Oh no! The raid boss was not killed in time. No rewards will be given.");
                Shinobytes.Debug.Log("Raid boss was not killed in time.");
            }
            else
            {
                Shinobytes.Debug.Log("Raid boss was slain!");
            }

            gameManager.Music.PlayBackgroundMusic();

            camera.DisableFocusCamera();

            notifications.HideRaidInfo();

            RemovePlayers(bossKilled, timeout);

            if (Boss != null && Boss.gameObject != null)
            {
                Destroy(Boss.gameObject);
            }

            gameManager.Ferry.AssignBestCaptain();
            gameManager.raidStatsJson.Update();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error when attempting to end raid: " + exc);
        }
        finally
        {
            gameManager.Events.End(this);

            nextRaidTimer = UnityEngine.Random.Range(minTimeBetweenRaids, maxTimeBetweenRaids);
            raidEndedTime = DateTime.Now;

            raidIndex++;
            difficultySystem.Next();

            Boss = null;
            RequiredCode = null;
        }
    }

    private void RemovePlayers(bool bossKilled, bool timeout)
    {
        try
        {
            lock (mutex)
            {
                var playersToLeave = raidingPlayers.ToList();
                if (bossKilled)
                {
                    RewardItemDrops(playersToLeave);
                }

                foreach (var player in playersToLeave)
                {
                    Leave(player, bossKilled, timeout);
                }
            }

            gameManager.raidStatsJson.Update();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error when attempting to remove players from raid: " + exc);
        }
    }

    private Queue<Func<Task>> rewardQueue = new Queue<Func<Task>>();

    public async void RewardItemDrops(List<PlayerController> players)
    {
        // only players within at least 20% participation time will have chance for item drop.
        if (players.Count(x => !x.IsBot) == 0)
        {
            return;
        }

        var playersInRaid = players.Where(x => x.raidHandler.GetParticipationPercentage() >= 0.2);

        var playersToBeRewarded = playersInRaid.OrderByDescending(x => x.raidHandler.GetParticipationPercentage()).Select(x => x.Id).ToArray();

        await RewardPlayersAsync(playersToBeRewarded);
    }

    public async Task RewardPlayersAsync(Guid[] playersToBeRewarded, int retryCount = 0)
    {
        if (playersToBeRewarded == null || playersToBeRewarded.Length == 0)
        {
            return;
        }

        if (retryCount > 0)
        {
            if (retryCount > 1000)
            {
                return;
            }

            await Task.Delay((int)MathF.Min(retryCount * 1000, 10000));
        }

        // make sure we retry later when we have server connection.
        if (!gameManager.RavenNest.Tcp.IsReady)
        {
            rewardQueue.Enqueue(() => RewardPlayersAsync(playersToBeRewarded, retryCount));
            return;
        }

        var rewards = await gameManager.RavenNest.Game.GetRaidRewardsAsync(playersToBeRewarded);
        if (rewards == null)
        {
            // it could be that we are offline, or temporary issue saving. Lets enqueue it for later.
            rewardQueue.Enqueue(() => RewardPlayersAsync(playersToBeRewarded, retryCount + 1));
            if (retryCount == 0)
            {
                gameManager.RavenBot.Announce("Victorious!! Raid boss was slain but unfortunately the connection to the server has been broken, rewards will be distributed later.");
            }
            return;
        }

        AddItems(rewards);
    }

    private void AddItems(EventItemReward[] rewards)
    {
        var result = gameManager.AddItems(rewards, raidIndex: raidIndex);
        if (result.Count > 0)
        {
            gameManager.RavenBot.Announce("Victorious!! The raid boss was slain and yielded {itemCount} item treasures!", result.Count.ToString());
        }
        else
        {
            gameManager.RavenBot.Announce("Victorious!! The raid boss was slain but did not yield any treasure.");
        }

        foreach (var itemDrop in result.Messages)
        {
            gameManager.RavenBot.Announce(itemDrop);
        }
    }


    public float GetParticipationPercentage(DateTime enterTime)
    {
        var participationTime = raidEndedTime - enterTime;
        return (float)(participationTime.TotalSeconds / (raidEndedTime - raidStartedTime).TotalSeconds);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Overlay.IsOverlay || PlayerSettings.Instance.DisableRaids.GetValueOrDefault())
        {
            return;
        }

        if (GameCache.IsAwaitingGameRestore)
        {
            rewardQueue.Clear();
            return;
        }

        var playerCount = playerManager.GetPlayerCount(true);

        if (!Started && playerCount == 0 && !Boss)
        {
            // try force ending the event if it's still active
            //gameManager.Events.End(this);
            return;
        }

        if (rewardQueue.Count > 0 && Interlocked.CompareExchange(ref isProcessingRewardQueue, 1, 0) == 0)
        {
            ProcessRewardQueueAsync();
        }

        if (nextRaidTimer > 0f)
        {
            nextRaidTimer -= GameTime.deltaTime;
            if (nextRaidTimer <= 0f)
            {
                StartRaid();
            }
        }

        if (Started && playerCount == 0)
        {
            EndRaid(false, false);
            return;
        }

        if (!Boss && Started)
        {
            EndRaid(true, false);
            return;
        }

        if (notifications && Boss)
        {
            if (Boss.Enemy.Stats.Health.CurrentValue <= 0)
            {
                EndRaid(true, false);
                return;
            }

            timeoutTimer -= GameTime.deltaTime;
            if (timeoutTimer <= 0f && Boss.Enemy.Stats.Health.CurrentValue > 0)
            {
                EndRaid(false, true);
                return;
            }

            var proc = (float)Boss.Enemy.Stats.Health.CurrentValue / Boss.Enemy.Stats.Health.MaxLevel;
            if (proc < minHealthToJoin)
            {
                notifications.HideRaidJoinInfo();
            }

            notifications.SetRaidBossLevel(Boss.Enemy.Stats.CombatLevel);
            notifications.SetHealthBarValue(proc, Boss.Enemy.Stats.Health.Level);
            notifications.UpdateRaidTimer(timeoutTimer);
        }
    }

    private async Task ProcessRewardQueueAsync()
    {
        try
        {
            if (rewardQueue.TryDequeue(out var addItems))
            {
                rewardQueue.Clear();
                await addItems();
            }
        }
        catch
        {
            rewardQueue.Clear();
        }
        finally
        {
            Interlocked.Exchange(ref isProcessingRewardQueue, 0);
        }
    }

    private bool SpawnRaidBoss()
    {
        try
        {
            if (!raidBossPrefab)
            {
                Shinobytes.Debug.LogError("NO RAID BOSS PREFAB SET!!!");
                return false;
            }

            var spawnPosition = Vector3.zero;
            if (chunkManager)
            {
                var randomChunk = chunkManager
                    .GetChunks()
                    .Where(x => x.Type != TaskType.Alchemy && x.Type != TaskType.Cooking && x.Type != TaskType.Crafting && x.Type != TaskType.Arena)
                    .OrderBy(x => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (randomChunk != null)
                {
                    spawnPosition = randomChunk.CenterPointWorld + (Vector3.up * 3.4f);
                    randomChunk.Island.Statistics.RaidBossesSpawned++;
                }
            }

            var difficulty = difficultySystem.GetDifficulty(raidIndex);
            if (difficulty == null)
            {
                return false;
            }

            Boss = Instantiate(raidBossPrefab, spawnPosition, Quaternion.identity).GetComponent<RaidBossController>();
            //Boss.Create(lowestStats, highestStats, rngLowEq, rngHighEq);
            Boss.Create(difficulty.BossSkills, difficulty.BossEquipmentStats);

            Boss.UnlockMovement();

            timeoutTimer = Mathf.Min(maxTimeoutSeconds, Mathf.Max(minTimeoutSeconds, Boss.Enemy.Stats.CombatLevel * 0.8249123f));
            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Failed to spawn raid boss: " + exc);
            return false;
        }
    }

}

public enum RaidJoinResult
{
    CanJoin,
    MinHealthReached,
    AlreadyJoined,
    NoActiveRaid,
    WrongCode
}
