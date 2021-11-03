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

    private float saveTimer = 5f;
    public float Duration => CurrentBoost != null && CurrentBoost.Active && EndTime > DateTime.MinValue
        ? (float)(EndTime - CurrentBoost.StartTime).TotalSeconds
        : 1800f;

    public DateTime EndTime => CurrentBoost.EndTime.Add(CurrentBoost.BoostTime);

    private int BitsForMultiplier = 100;

    private int SubMultiplierAdd = 5;
    private int BitsMultiplierAdd = 1;
    private float MaxObserveTime = 30f;
    private float announceTimer;
    private int limitOverride = -1;
    public int ExpMultiplierLimit => limitOverride > 0 ? limitOverride : gameManager.Permissions.ExpMultiplierLimit;
    public TimeSpan MaxBoostTime => TimeSpan.FromHours(TierExpMultis[gameManager.Permissions.SubscriberTier] - 1f);

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_MULTIPLIER))
            CurrentBoost.Multiplier = PlayerPrefs.GetFloat(TwitchSubscriberBoost.KEY_MULTIPLIER);

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_BOOST_TIME))
        {
            var val = PlayerPrefs.GetString(TwitchSubscriberBoost.KEY_BOOST_TIME);
            if (long.TryParse(val, out var seconds))
                CurrentBoost.BoostTime = TimeSpan.FromSeconds(seconds);
        }

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_ELAPSED))
            CurrentBoost.Elapsed = PlayerPrefs.GetFloat(TwitchSubscriberBoost.KEY_ELAPSED);

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_ACTIVE))
            CurrentBoost.Active = PlayerPrefs.GetInt(TwitchSubscriberBoost.KEY_ACTIVE) == 1;

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_LASTSUB))
            CurrentBoost.LastSubscriber = PlayerPrefs.GetString(TwitchSubscriberBoost.KEY_LASTSUB);

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_LASTCHEER))
            CurrentBoost.LastCheerer = PlayerPrefs.GetString(TwitchSubscriberBoost.KEY_LASTCHEER);

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_LASTCHEER_AMOUNT))
            CurrentBoost.LastCheerAmount = PlayerPrefs.GetInt(TwitchSubscriberBoost.KEY_LASTCHEER_AMOUNT);

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_CHEER_POT))
            CurrentBoost.CheerPot = PlayerPrefs.GetInt(TwitchSubscriberBoost.KEY_CHEER_POT);
    }

    internal void SetExpMultiplierLimit(string name, int expMultiplier)
    {
        limitOverride = expMultiplier;
        gameManager.RavenBot?.SendMessage(name, Localization.MSG_MULTIPLIER_LIMIT, expMultiplier.ToString());
    }

    private void Update()
    {
        if (!CurrentBoost.Active)
        {
            return;
        }

        if (CurrentBoost.Multiplier > 1 && EndTime > DateTime.MinValue && DateTime.UtcNow <= EndTime)
        {
            CurrentBoost.Elapsed += Time.deltaTime;
        }
        else if (CurrentBoost.Multiplier > 1 && CurrentBoost.Elapsed < Duration)
        {
            CurrentBoost.Elapsed += Time.deltaTime;
            saveTimer -= Time.deltaTime;

            var timeLeft = Duration - CurrentBoost.Elapsed;
            if (timeLeft < 180f)
            {
                announceTimer -= Time.deltaTime;
                if (announceTimer <= 0f)
                {
                    AnnounceExpMultiplierEnding(timeLeft);
                    announceTimer = timeLeft < 30F ? 10F : 30f;
                }
            }

            if (saveTimer <= 0f)
            {
                SaveState();
            }
        }
        else
        {
            this.Reset();
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

    public void Activate()
    {
        CurrentBoost.Active = CurrentBoost.Elapsed <= Duration;
        SaveState();
    }

    public void Reset()
    {
        CurrentBoost.BoostTime = TimeSpan.Zero;
        CurrentBoost.EndTime = DateTime.MinValue;
        CurrentBoost.StartTime = DateTime.MinValue;
        CurrentBoost.Active = false;
        CurrentBoost.Multiplier = 0;
        CurrentBoost.Elapsed = Duration;
        SaveState();
    }

    private void SaveState()
    {
        PlayerPrefs.SetFloat(TwitchSubscriberBoost.KEY_MULTIPLIER, CurrentBoost.Multiplier);
        PlayerPrefs.SetFloat(TwitchSubscriberBoost.KEY_ELAPSED, CurrentBoost.Elapsed);
        PlayerPrefs.SetString(TwitchSubscriberBoost.KEY_LASTSUB, CurrentBoost.LastSubscriber);
        PlayerPrefs.SetString(TwitchSubscriberBoost.KEY_LASTCHEER, CurrentBoost.LastCheerer);
        PlayerPrefs.SetInt(TwitchSubscriberBoost.KEY_LASTCHEER_AMOUNT, CurrentBoost.LastCheerAmount);
        PlayerPrefs.SetInt(TwitchSubscriberBoost.KEY_CHEER_POT, CurrentBoost.CheerPot);
        PlayerPrefs.SetInt(TwitchSubscriberBoost.KEY_ACTIVE, CurrentBoost.Active ? 1 : 0);
        PlayerPrefs.SetString(TwitchSubscriberBoost.KEY_BOOST_TIME, ((long)(CurrentBoost.BoostTime.TotalSeconds)).ToString());
        saveTimer = 5f;
    }

    public void OnCheer(TwitchCheer cheer)
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
            player.AddBitCheer(cheer.Bits);
        }
        else
        {
            gameManager.RavenNest.Stream.SendPlayerLoyaltyData(cheer);
        }

        AddCheer(cheer, CurrentBoost.Active);
    }

    public void OnSubscribe(TwitchSubscription subscribe)
    {
        PlayerController receiver = null;
        var player = gameManager.Players.GetPlayerByUserId(subscribe.UserId);
        if (!player) player = gameManager.Players.GetPlayerByName(subscribe.UserName);

        var receiverName = player?.Name;

        if (subscribe.ReceiverUserId != null)
        {
            receiver = gameManager.Players.GetPlayerByUserId(subscribe.ReceiverUserId);
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

            player.AddSubscribe(subscribe.ReceiverUserId != null);
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

        if (!player)
        {
            gameManager.RavenNest.Stream.SendPlayerLoyaltyData(subscribe);
        }

        AddSubscriber(subscribe, CurrentBoost.Active);
    }


    private void AddCheer(TwitchCheer data, bool increaseMultiplier = true)
    {
        if (increaseMultiplier)
        {
            CurrentBoost.LastCheerer = string.IsNullOrEmpty(data.UserName)
                ? CurrentBoost.LastCheerer
                : data.UserName;

            CurrentBoost.LastCheerAmount = data.Bits;
            CurrentBoost.CheerPot += data.Bits;

            var totalMultipliersAdded = 0;
            var maxed = false;
            var cheerPot = CurrentBoost.CheerPot;
            while (cheerPot >= BitsForMultiplier)
            {
                cheerPot -= BitsForMultiplier;
                totalMultipliersAdded += BitsMultiplierAdd;
                CurrentBoost.BoostTime = CurrentBoost.BoostTime.Add(TimeSpan.FromMinutes(BitsMultiplierAdd));
                if (CurrentBoost.BoostTime > MaxBoostTime)
                {
                    maxed = true;
                    CurrentBoost.BoostTime = MaxBoostTime;
                }
            }
            CurrentBoost.CheerPot = cheerPot;

            if (maxed)
            {
                //gameManager.Server.Send(data.UserName, "The multiplier timer cannot be boosted any further. But thank you for the bitties <3");
            }
            else if (totalMultipliersAdded > 0)
            {
                gameManager.RavenBot.Send(data.UserName, Localization.MSG_BIT_CHEER_INCREASE,
                    (int)CurrentBoost.BoostTime.TotalMinutes,
                    data.Bits,
                    BitsForMultiplier - CurrentBoost.CheerPot);
            }
            else
            {
                gameManager.RavenBot.Send(data.UserName, Localization.MSG_BIT_CHEER_LEFT,
                    BitsForMultiplier - CurrentBoost.CheerPot);
            }
        }

        SaveState();
    }

    private void AddSubscriber(TwitchSubscription data, bool increaseMultiplier = true)
    {
        if (increaseMultiplier)
        {
            CurrentBoost.LastSubscriber = string.IsNullOrEmpty(data.UserName)
               ? CurrentBoost.LastSubscriber
               : data.UserName;

            CurrentBoost.BoostTime = CurrentBoost.BoostTime.Add(TimeSpan.FromMinutes(SubMultiplierAdd));
            if (CurrentBoost.BoostTime > MaxBoostTime)
                CurrentBoost.BoostTime = MaxBoostTime;
        }

        SaveState();
    }

    public void IncreaseExpMultiplier(string sender, int amount)
    {
        if (ExpMultiplierLimit == 0)
        {
            return;
        }

        var oldMultiplier = CurrentBoost.Multiplier;
        CurrentBoost.LastSubscriber = sender;
        CurrentBoost.Elapsed = 0f;
        CurrentBoost.Active = true;
        CurrentBoost.Multiplier = Mathf.Min(
            CurrentBoost.Multiplier + amount, ExpMultiplierLimit);
        var added = CurrentBoost.Multiplier - oldMultiplier;
        if (added > 0)
            gameManager.RavenBot?.Send(sender, Localization.MSG_MULTIPLIER_INCREASE, added, CurrentBoost.Multiplier);
        else
            gameManager.RavenBot?.Send(sender, Localization.MSG_MULTIPLIER_RESET);
        SaveState();
    }

    internal void SetExpMultiplier(
        string eventName,
        int multiplier,
        DateTime startTime,
        DateTime endTime)
    {
        var multi = Mathf.Min(multiplier, ExpMultiplierLimit);
        if (multi <= 1)
        {
            Reset();
            return;
        }

        if (CurrentBoost.Active)
        {
            // get the current boost
            // get the current end time
            // if the current end time is less than new end time
            // then set new end time to new end time

            // if the current end time is more than the new end time
            // subtract new endtime from existing endtime and set as boost.

            if (EndTime <= endTime)
            {
                CurrentBoost.EndTime = endTime;
                CurrentBoost.BoostTime = TimeSpan.Zero;
            }
            else
            {
                CurrentBoost.BoostTime = EndTime - endTime;
                CurrentBoost.EndTime = endTime;
            }
        }
        else
        {
            CurrentBoost.EndTime = endTime;
        }

        CurrentBoost.LastSubscriber = eventName;
        CurrentBoost.Elapsed = (float)(DateTime.UtcNow - startTime).TotalSeconds;
        CurrentBoost.Active = true;

        if (CurrentBoost.Multiplier < multiplier)
        {
            CurrentBoost.Multiplier = multiplier;
        }


        CurrentBoost.StartTime = startTime;


        SaveState();
    }

    public void SetExpMultiplier(string sender, int amount)
    {
        CurrentBoost.LastSubscriber = sender;
        CurrentBoost.Elapsed = 0f;
        CurrentBoost.Active = true;
        CurrentBoost.Multiplier = amount;
        CurrentBoost.StartTime = DateTime.UtcNow;
        CurrentBoost.EndTime = DateTime.UtcNow.AddMinutes(30);

        SaveState();

        gameManager.RavenBot?.Send(sender, Localization.MSG_MULTIPLIER_SET, amount);
    }

    public TwitchSubscriberBoost CurrentBoost
        => boost = boost ?? new TwitchSubscriberBoost();
}

[Serializable]
public class TwitchSubscriberBoost
{
    public const string KEY_MULTIPLIER = "boost_multiplier";
    public const string KEY_ELAPSED = "boost_elapsed";
    public const string KEY_ACTIVE = "boost_active";
    public const string KEY_LASTSUB = "boost_lastsub";

    public const string KEY_BOOST_TIME = "boost_time";

    public const string KEY_LASTCHEER = "boost_lastcheer";
    public const string KEY_LASTCHEER_AMOUNT = "boost_lastcheer_amount";
    public const string KEY_CHEER_POT = "boost_cheerpot";

    public string LastSubscriber;
    public string LastCheerer;

    public int LastCheerAmount;
    public int CheerPot;

    public bool Active;
    public float Multiplier = 1f;
    public float Elapsed;
    public DateTime EndTime;
    public DateTime StartTime;
    public TimeSpan BoostTime;
}