using Newtonsoft.Json.Schema;
using System;
using System.Collections.Concurrent;
using UnityEngine;

public class TwitchEventManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TwitchSubscriberBoost boost;

    public static readonly float[] TierExpMultis = new float[10]
    {
        0f, 2f, 3f, 5f, 5f, 5f, 5f, 5f, 5f, 5f
    };

    private int BitsForMultiplier = 100;

    private int SubMultiplierAdd = 5;
    private int BitsMultiplierAdd = 1;
    private float MaxObserveTime = 30f;
    private float announceTimer;
    private int limitOverride = -1;

    public DateTime LastUpdated;

    public int ExpMultiplierLimit => limitOverride > 0 ? limitOverride : gameManager.Permissions.ExpMultiplierLimit;
    public TimeSpan MaxBoostTime => TimeSpan.FromHours(TierExpMultis[gameManager.Permissions.SubscriberTier] - 1f);
    public TimeSpan? TimeLeft => CurrentBoost?.TimeLeft;
    public TimeSpan? Duration => CurrentBoost?.Duration;
    public DateTime? EndTime => CurrentBoost?.EndTime;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    internal void SetExpMultiplierLimit(string name, int expMultiplier)
    {
        limitOverride = expMultiplier;
        gameManager.RavenBot?.SendMessage(name, Localization.MSG_MULTIPLIER_LIMIT, expMultiplier.ToString());
    }

    private void Update()
    {
        if (CurrentBoost == null || !CurrentBoost.Active)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (CurrentBoost?.Multiplier <= 1 || now >= CurrentBoost.EndTime)
        {
            ResetMultiplier();
            return;
        }

        CurrentBoost.Elapsed = now - CurrentBoost.StartTime;
        CurrentBoost.TimeLeft = CurrentBoost.Duration - CurrentBoost.Elapsed;

        var timeLeft = CurrentBoost.Duration - CurrentBoost.Elapsed;
        var timeLeftSeconds = (float)timeLeft.TotalSeconds;
        if (timeLeftSeconds <= 180f)
        {
            announceTimer -= Time.deltaTime;
            if (announceTimer <= 0f)
            {
                if (timeLeftSeconds > 0)
                {
                    AnnounceExpMultiplierEnding(timeLeftSeconds);
                }
                announceTimer = timeLeftSeconds < 30F ? 10F : 30f;
            }
        }
    }

    private void AnnounceExpMultiplierEnding(float secondsLeft)
    {
        var timeLeft = TimeSpan.FromSeconds(secondsLeft);
        var minutesStr = timeLeft.Minutes > 0 ? timeLeft.Minutes + " mins " : "";
        var secondsStr = timeLeft.Seconds > 0 ? timeLeft.Seconds + " seconds" : "";
        if (timeLeft.Seconds >= 10)
        {
            gameManager.RavenBot.SendMessage("", Localization.MSG_MULTIPLIER_ENDS,
                CurrentBoost.Multiplier.ToString(),
                minutesStr,
                secondsStr);
        }
    }

    public void ResetMultiplier()
    {
        if (CurrentBoost.Multiplier > 1)
        {
            Shinobytes.Debug.Log("Global Exp Multiplier have been reset.");
        }

        boost = new TwitchSubscriberBoost();
        //if (CurrentBoost.Active || CurrentBoost.BoostTime > TimeSpan.Zero)
        //{
        //    CurrentBoost.BoostTime = TimeSpan.Zero;
        //    CurrentBoost.EndTime = DateTime.MinValue;
        //    CurrentBoost.StartTime = DateTime.MinValue;
        //    CurrentBoost.Active = false;
        //    CurrentBoost.Multiplier = 0;
        //    CurrentBoost.Elapsed = Duration;
        //}
    }

    public void OnCheer(TwitchCheer cheer)
    {
        try
        {
            var player = gameManager.Players.GetPlayerByUserId(cheer.UserId);
            if (!player) player = gameManager.Players.GetPlayerByName(cheer.UserName);
            if (player)
            {
                var subscriberMultiplier = player.IsSubscriber ? 2f : 1f;
                var observeTime = Mathf.Min(MaxObserveTime, (cheer.Bits / (float)BitsForMultiplier)
                    * MaxObserveTime
                    * subscriberMultiplier);

                if (observeTime >= 1)
                {
                    gameManager.Camera.ObservePlayer(player, observeTime);
                }

                player.Cheer();
                //player.AddBitCheer(cheer.Bits);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error resolving twitch cheer: " + exc);
        }
        //AddCheer(cheer, CurrentBoost.Active);
    }

    public void OnSubscribe(TwitchSubscription subscribe)
    {
        try
        {
            var player = gameManager.Players.GetPlayerByUserId(subscribe.UserId);
            if (!player) player = gameManager.Players.GetPlayerByName(subscribe.UserName);

            var receiverName = player?.Name;

            if (subscribe.ReceiverUserId != null)
            {
                var receiver = gameManager.Players.GetPlayerByUserId(subscribe.ReceiverUserId);
                if (receiver)
                {
                    receiver.IsSubscriber = true;
                    receiverName = receiver.Name;
                }
            }

            if (player)
            {
                if (subscribe.ReceiverUserId == null)
                {
                    player.IsSubscriber = true;
                }

                gameManager.Camera.ObservePlayer(player, MaxObserveTime);
            }

            if (!string.IsNullOrEmpty(receiverName))
            {
                gameManager.RavenBot?.Send(receiverName, Localization.MSG_SUB_CREW,
                        gameManager.RavenNest.TwitchDisplayName,
                        TierExpMultis[gameManager.Permissions.SubscriberTier]);
            }

            var activePlayers = gameManager.Players.GetAllPlayers();
            foreach (var plr in activePlayers)
            {
                plr.Cheer();
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error resolving twitch subscription: " + exc);
        }
    }

    internal void SetExpMultiplier(
        string eventName,
        int multiplier,
        DateTime startTime,
        DateTime endTime)
    {
        LastUpdated = DateTime.UtcNow;
        var multi = Mathf.Min(multiplier, ExpMultiplierLimit);
        if (multi <= 1)
        {
            ResetMultiplier();
            return;
        }

        var now = DateTime.UtcNow;
        CurrentBoost.EndTime = endTime;
        CurrentBoost.Multiplier = multi;
        CurrentBoost.StartTime = startTime;
        CurrentBoost.Elapsed = now - startTime;
        CurrentBoost.Duration = endTime - startTime;
        CurrentBoost.TimeLeft = CurrentBoost.Duration - CurrentBoost.Elapsed;
        CurrentBoost.LastSubscriber = eventName;
        CurrentBoost.Active = true;
    }

    public void SetExpMultiplier(string sender, int amount)
    {
        //CurrentBoost.LastSubscriber = sender;
        //CurrentBoost.Elapsed = 0f;
        //CurrentBoost.Active = true;
        //CurrentBoost.Multiplier = amount;
        //CurrentBoost.StartTime = DateTime.UtcNow;
        //CurrentBoost.EndTime = DateTime.UtcNow.AddMinutes(30);
        //gameManager.RavenBot?.Send(sender, Localization.MSG_MULTIPLIER_SET, amount);
    }

    public TwitchSubscriberBoost CurrentBoost
        => boost = boost ?? new TwitchSubscriberBoost();
}

[Serializable]
public class TwitchSubscriberBoost
{
    public string LastSubscriber;
    public string LastCheerer;

    public int LastCheerAmount;
    public int CheerPot;

    public bool Active;
    public float Multiplier = 1f;
    public TimeSpan Elapsed;
    public TimeSpan Duration;
    public TimeSpan TimeLeft;
    public DateTime EndTime;
    public DateTime StartTime;
}