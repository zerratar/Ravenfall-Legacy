using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using Skill = RavenNest.Models.Skill;
public class PlayerDetails : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private CombatSkillObserver combatSkillObserver;
    [SerializeField] private ClanSkillObserver clanSkillObserver;
    [SerializeField] private SkillStatsObserver skillStatsObserver;
    [SerializeField] private ResourcesObserver resourcesObserver;
    [SerializeField] private TextMeshProUGUI lblObserving;
    [SerializeField] private TextMeshProUGUI lblPlayername;
    [SerializeField] private TextMeshProUGUI lblPlayerlevel;
    [SerializeField] private TextMeshProUGUI lblClanName;
    [SerializeField] private CanvasGroup canvasGroup;



    [SerializeField] private GameObject timeforlevelPanel;
    [SerializeField] private TextMeshProUGUI lblTimeForLevel;
    [SerializeField] private TextMeshProUGUI lblLevelUpTimeLabel;

    [SerializeField] private GameObject restedPanel;
    [SerializeField] private TextMeshProUGUI lblRestedAmount;

    [SerializeField] private TextMeshProUGUI lblTraining;
    [SerializeField] private TextMeshProUGUI lblTrainingSkill;

    [SerializeField] private AttributeStatsManager attributeStatsManager;
    [SerializeField] private EquipmentSlotManager equipmentSlotManager;

    [SerializeField] private GameObject subscriberBadge;
    [SerializeField] private GameObject vipBadge;
    [SerializeField] private GameObject modBadge;
    [SerializeField] private GameObject broadcasterBadge;
    [SerializeField] private GameObject devBadge;

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Dragscript dragscript;
    [SerializeField] private TooltipViewer playerDetailsTooltip;
    public PlayerController ObservedPlayer => observedPlayer;

    public static bool IsMoving { get; private set; }
    public static bool IsExpanded { get; private set; }

    private float observedPlayerTimeout;
    private RectTransform rectTransform;
    private bool visible = true;
    private ExpGainState CurrentExpGainState;
    private string defaultTimeToLevelUpLabelText;

    void Start()
    {
        if (!dragscript) dragscript = GetComponent<Dragscript>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        playerInventory.Hide();

        // the text "Time left for next level" that is shown as smaller text
        // above the actual time left, keep track on this one so we can change it
        // to "Recommended island" when needed
        defaultTimeToLevelUpLabelText = lblLevelUpTimeLabel.text;
    }

    public void ToggleVisibility()
    {
        this.visible = !this.visible;
        if (this.visible)
        {
            canvasGroup.alpha = 1;
        }
        else
        {
            canvasGroup.alpha = 0;
            playerDetailsTooltip.Hide(0);
        }
    }

    public void ToggleInventory()
    {
        IsExpanded = playerInventory.ToggleVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        if (!visible)
        {
            return;
        }

        if (!observedPlayer || observedPlayer.isDestroyed || observedPlayer.gameObject == null || observedPlayer.Removed)
        {
            if (gameManager.Players.GetPlayerCount() == 0)
            {
                Observe(null, 0);
            }
            else
            {
                gameManager.Camera.ObserveNextPlayer();
            }
            return;
        }

        if (canvasGroup.alpha != 1)
            canvasGroup.alpha = 1;

        IsMoving = dragscript.IsDragging;

        if (observedPlayer.clanHandler.InClan)
            SetText(lblClanName, "<" + observedPlayer.clanHandler.ClanInfo.Name + ">");
        else
            SetText(lblClanName, "");

        SetActive(subscriberBadge, observedPlayer.IsSubscriber);
        SetActive(vipBadge, observedPlayer.IsVip);
        SetActive(modBadge, observedPlayer.IsModerator);

        SetActive(devBadge, observedPlayer.IsGameAdmin);

        SetActive(broadcasterBadge, observedPlayer.IsBroadcaster);

        observedPlayerTimeout -= GameTime.deltaTime;
        SetText(lblObserving, $"({Mathf.FloorToInt(observedPlayerTimeout) + 1}s)");
        SetText(lblPlayerlevel, $"LV : <b>{observedPlayer.Stats.CombatLevel}");

        var isRested = observedPlayer.Rested.RestedTime > 0;
        SetActive(restedPanel, isRested);
        if (isRested)
        {
            var time = Utility.FormatTime(observedPlayer.Rested.RestedTime / 60f / 60f, false);
            if (observedPlayer.onsenHandler.InOnsen)
            {
                SetText(lblRestedAmount, "<color=#FFDE00>" + time + "</color>");
                // increasing time
            }
            else
            {
                SetText(lblRestedAmount, time);
                // decreasing time
            }
        }
        var playerTask = observedPlayer.GetTask();
        var isTrainingSomething = playerTask != TaskType.None || !string.IsNullOrEmpty(observedPlayer.GetTaskArgument());
        SetActive(lblTraining.gameObject, isTrainingSomething);
        SetActive(lblTrainingSkill.gameObject, isTrainingSomething);

        if (observedPlayer.ferryHandler && observedPlayer.ferryHandler.OnFerry)
        {
            lblTrainingSkill.text = "Sailing";
            lblTimeForLevel.text = GetTimeLeftForLevelFormatted(observedPlayer.Stats.Sailing);
        }
        else if (isTrainingSomething)
        {
            var activeSkill = observedPlayer.GetActiveSkillStat();
            if (activeSkill != null)
            {
                var trainingAll = activeSkill == observedPlayer.GetSkill(Skill.Health);
                var name = trainingAll ? "All" : activeSkill.Name;

                var recommendedIsland = IslandManager.GetSuitableIsland(activeSkill.Level);
                //var currentIsland = observedPlayer.Island?.Island ?? RavenNest.Models.Island.Ferry;
                //var onRecommendedIsland = currentIsland != RavenNest.Models.Island.Ferry && recommendedIsland != currentIsland;

                if (CurrentExpGainState == ExpGainState.LevelTooHigh)
                {
                    SetActive(timeforlevelPanel, true);
                    lblTrainingSkill.text = "<color=red>" + name + "\r\n<size=14>Level too high</size></color>";
                    lblLevelUpTimeLabel.text = "Recommended Island";
                    lblTimeForLevel.text = $"!sail <b>{recommendedIsland}</b>";
                }
                else if (CurrentExpGainState == ExpGainState.LevelTooLow)
                {
                    SetActive(timeforlevelPanel, true);
                    lblTrainingSkill.text = "<color=red>" + name + "\r\n<size=14>Level too low</size></color>";
                    lblLevelUpTimeLabel.text = "Recommended Island";
                    lblTimeForLevel.text = $"!sail <b>{recommendedIsland}</b>";
                }
                else if (observedPlayer.Chunk == null)
                {
                    if (!observedPlayer.Island || observedPlayer.Island == null)
                    {
                        SetActive(timeforlevelPanel, false);
                        lblTrainingSkill.text = "<color=red>" + name + "\r\n<size=14>Ineligible</size></color>";
                    }
                    else
                    {
                        SetActive(timeforlevelPanel, false);
                        lblTrainingSkill.text = "<color=red>" + name + "\r\n<size=14>Level too low</size></color>";
                    }
                }
                else
                {
                    lblLevelUpTimeLabel.text = defaultTimeToLevelUpLabelText;
                    SetActive(timeforlevelPanel, true);
                    lblTrainingSkill.text = name;
                    lblTimeForLevel.text = trainingAll ? "N/A" : GetTimeLeftForLevelFormatted();
                }
            }
        }
        else
        {
            SetActive(timeforlevelPanel, false);
        }

        if (observedPlayer.CharacterIndex > 0)
            SetText(lblPlayername, $"{observedPlayer.PlayerName} #{observedPlayer.CharacterIndex}");
        else
            SetText(lblPlayername, $"{observedPlayer.PlayerName}");

        if (observedPlayerTimeout < 0)
            gameManager.Camera.ObserveNextPlayer();
    }

    private string GetTimeLeftForLevelFormatted()
    {
        var s = observedPlayer.ActiveSkill;
        if (s == Skill.None) return "";

        var skill = observedPlayer.Stats[s];
        return GetTimeLeftForLevelFormatted(skill);
    }

    internal void SetExpGainState(ExpGainState state)
    {
        this.CurrentExpGainState = state;
    }

    private string GetTimeLeftForLevelFormatted(SkillStat skill)
    {
        //var f = observedPlayer.GetExpFactor();
        //var expPerTick = ObservedPlayer.GetExperience(s, f);
        //var estimatedExpPerHour = expPerTick * GameMath.Exp.GetTicksPerMinute(s) * 60;
        //var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
        //var expPerHour = System.Math.Min(estimatedExpPerHour, skill.GetExperiencePerHour());
        //var expLeft = nextLevelExp - skill.Experience;

        if (skill.Level >= GameMath.MaxLevel)
        {
            return "<color=yellow>Max level</color>";
        }

        var recommendedIsland = IslandManager.GetSuitableIsland(skill.Level);
        var currentIsland = observedPlayer.Island?.Island ?? RavenNest.Models.Island.Ferry;
        var onRecommendedIsland = currentIsland != RavenNest.Models.Island.Ferry && recommendedIsland != currentIsland;

        switch (CurrentExpGainState)
        {
            case ExpGainState.LevelTooLow:
                return $"<color=red>Your level is too low</color>\n<size=14>Recommended Island:{recommendedIsland}</size>";

            case ExpGainState.LevelTooHigh:
                return $"<color=red>Your level is too high</color>\n<size=14>Recommended Island:{recommendedIsland}</size>";
        }

        var timeLeft = skill.GetEstimatedTimeToLevelUp() - DateTime.UtcNow;
        var hoursLeft = timeLeft.TotalHours;

        var result = "";

        if (hoursLeft <= 0)
            result = "<color=red>Unknown</color>";

        else if (timeLeft.Days >= 365 * 10_000)
            result = "<color=red>When hell freezes over</color>";
        else if (timeLeft.Days >= 365 * 1000)
            result = "<color=red>Unreasonably long</color>";
        else if (timeLeft.Days >= 365)
            result = "<color=red>Way too long</color>";
        else if (timeLeft.Days > 21)
            result = "<color=orange>" + (int)(timeLeft.Days / 7) + " weeks</color>";
        else if (timeLeft.Days > 0)
            result = "<color=yellow>" + timeLeft.Days + " days, " + timeLeft.Hours + " hours</color>";
        else if (timeLeft.Hours > 0)
            result = timeLeft.Hours + " hours, " + timeLeft.Minutes + " mins";
        else if (timeLeft.Minutes > 0)
            result = timeLeft.Minutes + " mins, " + timeLeft.Seconds + " secs";
        else
            result = timeLeft.Seconds + " seconds";
        if (!onRecommendedIsland)
            result += $"\n<size=14>Recommended Island: {recommendedIsland}</size>";

        return result;
    }

    public void Observe(PlayerController player, float timeout)
    {
        if (!player || player == null)
            playerDetailsTooltip.Disable();
        else
            playerDetailsTooltip.Enable();

        if (observedPlayer)
        {
            observedPlayer.IsObserved = false;
        }

        observedPlayer = player;

        if (player)
        {
            player.IsObserved = true;
        }

        ForceUpdate();

        this.gameObject.SetActive(player != null);
        observedPlayerTimeout = timeout;
    }

    public void ForceUpdate()
    {
        //if (GameManager.BatchPlayerAddInProgress)
        //{
        //    return;
        //}

        attributeStatsManager.Observe(observedPlayer);
        equipmentSlotManager.Observe(observedPlayer);
        clanSkillObserver.Observe(observedPlayer);
        resourcesObserver.Observe(observedPlayer);
        combatSkillObserver.Observe(observedPlayer);
        skillStatsObserver.Observe(observedPlayer);

        gameManager.Overlay.SendObservePlayer(observedPlayer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetText(TextMeshProUGUI obj, string value)
    {
        if (!obj) return;
        if (obj.text != value)
            obj.text = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetActive(GameObject obj, bool value)
    {
        if (!obj) return;
        if (obj.activeSelf != value)
            obj.SetActive(value);
    }

}