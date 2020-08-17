using System;
using UnityEngine;

public class TwitchEventManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TwitchSubscriberBoost boost;

    private float saveTimer = 5f;

    public int BitsForMultiplier = 250;
    public float MaxMultiplier = 50f;
    public const float DuartionPerBoost = 25f * 60f;
    public float MaxObserveTime = 30f;
    private float announceTimer;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_MULTIPLIER))
        {
            CurrentBoost.Multiplier = PlayerPrefs.GetFloat(TwitchSubscriberBoost.KEY_MULTIPLIER);
        }

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_ELAPSED))
        {
            CurrentBoost.Elapsed = PlayerPrefs.GetFloat(TwitchSubscriberBoost.KEY_ELAPSED);
        }

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_ACTIVE))
        {
            CurrentBoost.Active = PlayerPrefs.GetInt(TwitchSubscriberBoost.KEY_ACTIVE) == 1;
        }

        if (PlayerPrefs.HasKey(TwitchSubscriberBoost.KEY_LASTSUB))
        {
            CurrentBoost.LastSubscriber = PlayerPrefs.GetString(TwitchSubscriberBoost.KEY_LASTSUB);
        }
    }

    private void Update()
    {
        if (!CurrentBoost.Active)
        {
            return;
        }

        if (CurrentBoost.Elapsed < CurrentBoost.Duration)
        {
            CurrentBoost.Elapsed += Time.deltaTime;
            saveTimer -= Time.deltaTime;

            var timeLeft = CurrentBoost.Duration - CurrentBoost.Elapsed;

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
            CurrentBoost.Active = false;
            CurrentBoost.Multiplier = 1;
            CurrentBoost.Elapsed = CurrentBoost.Duration;
            SaveState();
        }
    }

    private void AnnounceExpMultiplierEnding(float secondsLeft)
    {
        var timeLeft = TimeSpan.FromSeconds(secondsLeft);
        var minutesStr = timeLeft.Minutes > 0 ? timeLeft.Minutes + " mins " : "";
        var secondsStr = timeLeft.Seconds > 0 ? timeLeft.Seconds + " seconds" : "";
        gameManager.Server?.Client?.SendCommand("",
            "message",
            $"The current exp multiplier (x{CurrentBoost.Multiplier}) will end in {minutesStr}{secondsStr}.");
    }

    public void Activate()
    {
        CurrentBoost.Active = CurrentBoost.Elapsed <= CurrentBoost.Duration;
        SaveState();
    }

    public void Reset()
    {
        CurrentBoost.Active = false;
        CurrentBoost.Multiplier = 1;
        CurrentBoost.Elapsed = CurrentBoost.Duration;
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
    }

    public void OnCheer(TwitchCheer cheer)
    {
        var player = gameManager.Players.GetPlayerByUserId(cheer.UserId);
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
        }

        AddCheer(cheer);
    }

    public void OnSubscribe(TwitchSubscription subscribe)
    {
        var player = gameManager.Players.GetPlayerByUserId(subscribe.UserId);
        if (player)
        {
            if (subscribe.ReceiverUserId == null)
            {
                player.IsSubscriber = true;
            }
            else
            {
                var receiver = gameManager.Players.GetPlayerByUserId(subscribe.ReceiverUserId);
                if (receiver)
                {
                    player.IsSubscriber = true;
                }
            }
            gameManager.Camera.ObservePlayer(player, MaxObserveTime);
        }

        var activePlayers = gameManager.Players.GetAllPlayers();
        foreach (var plr in activePlayers)
        {
            plr.Cheer();
        }

        AddSubscriber(subscribe);
    }

    private void AddCheer(TwitchCheer data)
    {
        CurrentBoost.LastCheerer = string.IsNullOrEmpty(data.UserName)
            ? CurrentBoost.LastCheerer
            : data.UserName;

        if (gameManager.Permissions.ExpMultiplierLimit == 0)
        {
            return;
        }

        CurrentBoost.LastCheerAmount = data.Bits;
        CurrentBoost.CheerPot += data.Bits;

        while (CurrentBoost.CheerPot >= BitsForMultiplier)
        {
            AddtoTimer();
            CurrentBoost.CheerPot -= BitsForMultiplier;
        }

        if (totalMultipliersAdded > 0)
        {
            gameManager.Server?.Client?.SendCommand(data.UserName, "message",
                $"You have increased the multiplier by x{totalMultipliersAdded} with your {data.Bits} cheer!! PogChamp We only need {BitsForMultiplier - CurrentBoost.CheerPot} more bits for another multiplier! <3");
        }
        else
        {
            gameManager.Server?.Client?.SendCommand(data.UserName, "message",
                $"We only need {BitsForMultiplier - CurrentBoost.CheerPot} more bits for another multiplier! PogChamp");
        }

        SaveState();
    }

    private void AddSubscriber(TwitchSubscription data)
    {

        CurrentBoost.LastSubscriber = string.IsNullOrEmpty(data.UserName)
            ? CurrentBoost.LastSubscriber
            : data.UserName;

        if (gameManager.Permissions.ExpMultiplierLimit == 0)
        {
            return;
        }

        if (data.Months >= 0)
        {
            AddtoTimer();
        }

        SaveState();
    }

    private void AddtoTimer()
    {
        CurrentBoost.Duration += DuartionPerBoost;
        CurrentBoost.Active = true;
        CurrentBoost.Multiplier = Mathf.Min(CurrentBoost.Multiplier + (UnityEngine.Random.value <= 0.10 ? 10 : 1), gameManager.Permissions.ExpMultiplierLimit);
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
    public float Duration;
}