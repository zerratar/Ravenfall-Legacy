using System;
using UnityEngine;

public class TwitchEventManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TwitchSubscriberBoost boost;

    public static float[] AnnouncementTimersSeconds = new float[] {
        300, // first at 5 mins left
        180, // second at 3 mins left
        60,  // third when there is 1 minute left
        0.3f // let everyone know the multiplier has expired.
    };

    private bool[] announced = new bool[AnnouncementTimersSeconds.Length];

    public static readonly float[] TierExpMultis = new float[10]
    {
        0f, 2f, 3f, 5f, 5f, 5f, 5f, 5f, 5f, 5f
    };

    private int BitsForMultiplier = 100;

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
        //gameManager.RavenBot?.SendMessage(name, Localization.MSG_MULTIPLIER_LIMIT, expMultiplier.ToString());
    }

    private void Update()
    {
        if (announced.Length != AnnouncementTimersSeconds.Length)
        {
            announced = new bool[AnnouncementTimersSeconds.Length];
        }

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

        for (var i = 0; i < AnnouncementTimersSeconds.Length; i++)
        {
            if (announced[i]) // this has already been announced. Continue.
            {
                continue;
            }

            var time = AnnouncementTimersSeconds[i];
            if (timeLeftSeconds <= time)
            {
                AnnounceExpMultiplierEnding(timeLeftSeconds);
                announced[i] = true;
                break;
            }
        }
    }

    private void AnnounceExpMultiplierEnding(float secondsLeft)
    {
        var timeLeft = TimeSpan.FromSeconds(secondsLeft);
        var minutesStr = timeLeft.Minutes > 0 ? timeLeft.Minutes + " mins " : "";
        var secondsStr = timeLeft.Seconds > 0 ? timeLeft.Seconds + " seconds" : "";

        if (timeLeft.Seconds >= 1)
        {
            gameManager.RavenBot.Announce(Localization.MSG_MULTIPLIER_ENDS, CurrentBoost.Multiplier.ToString(), minutesStr, secondsStr);
        }
        else
        {
            gameManager.RavenBot.Announce(Localization.MSG_MULTIPLIER_ENDED);
        }
    }

    public void ResetMultiplier()
    {
        if (CurrentBoost.Multiplier > 1)
        {
            Shinobytes.Debug.Log("Global Exp Multiplier have been reset.");
        }

        announced = new bool[AnnouncementTimersSeconds.Length];
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

    public void OnCheer(CheerBitsEvent cheer)
    {
        try
        {
            var player = gameManager.Players.GetPlayerByPlatformId(cheer.UserId, cheer.Platform);
            if (!player) player = gameManager.Players.GetPlayerByName(cheer.UserName);
            if (player)
            {
                //var subscriberMultiplier = player.IsSubscriber ? 2f : 1f;
                //var observeTime = Mathf.Min(MaxObserveTime, (cheer.Bits / (float)BitsForMultiplier)
                //    * MaxObserveTime
                //    * subscriberMultiplier);
                //if (observeTime >= 1)
                //{
                //    gameManager.Camera.ObservePlayer(player, ObserveEvent.CheeredBits, cheer.Bits);
                //}

                gameManager.Camera.ObservePlayer(player, ObserveEvent.Bits, cheer.Bits);

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

    public void OnSubscribe(UserSubscriptionEvent subscribe)
    {
        try
        {
            var player = gameManager.Players.GetPlayerByPlatformId(subscribe.UserId, subscribe.Platform);
            if (!player) player = gameManager.Players.GetPlayerByName(subscribe.UserName);

            var receiverName = player?.Name;

            if (subscribe.ReceiverUserId != null)
            {
                var receiver = gameManager.Players.GetPlayerByPlatformId(subscribe.ReceiverUserId, subscribe.Platform);
                if (receiver)
                {
                    receiver.IsSubscriber = true;
                    receiverName = receiver.Name;

                    gameManager.RavenBot?.SendReply(receiver, 
                        Localization.MSG_SUB_CREW,
                        gameManager.RavenNest.TwitchDisplayName,
                        TierExpMultis[gameManager.Permissions.SubscriberTier]);
                }
            }

            if (player)
            {
                if (subscribe.ReceiverUserId == null)
                {
                    player.IsSubscriber = true;
                }

                gameManager.Camera.ObservePlayer(player, ObserveEvent.Subscription);
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
        //if (multiplier != CurrentBoost.Multiplier || endTime != CurrentBoost.EndTime)
        //{
        //    Shinobytes.Debug.Log($"Updating Exp Multiplier. (Old Multi: " + CurrentBoost.Multiplier + ", Old EndTime: " + CurrentBoost.EndTime + $")\n{{\"multiplier\": \"{multiplier}\", \"name\": \"{eventName}\", \"start-time\": \"{startTime}\", \"end-time\": \"{endTime}\"}}\nCurrent UTC Time is: {DateTime.UtcNow}");
        //}

        LastUpdated = DateTime.UtcNow;
        var multi = Mathf.Min(multiplier, ExpMultiplierLimit);
        if (multi <= 1)
        {
            ResetMultiplier();
            return;
        }

        if (multiplier != CurrentBoost.Multiplier)
        {
            announced = new bool[AnnouncementTimersSeconds.Length];
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