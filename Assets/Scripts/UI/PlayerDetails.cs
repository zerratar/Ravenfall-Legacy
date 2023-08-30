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

    void Start()
    {
        if (!dragscript) dragscript = GetComponent<Dragscript>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        playerInventory.Hide();
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

        if (observedPlayer.Clan.InClan)
            SetText(lblClanName, "<" + observedPlayer.Clan.ClanInfo.Name + ">");
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
            if (observedPlayer.Onsen.InOnsen)
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

        if (isTrainingSomething)
        {
            var activeSkill = observedPlayer.GetActiveSkillStat();
            if (activeSkill != null)
            {
                var trainingAll = activeSkill == observedPlayer.GetSkill(Skill.Health);
                var name = trainingAll ? "All" : activeSkill.Name;
                if (playerTask == TaskType.None || observedPlayer.Chunk == null)
                {
                    SetActive(timeforlevelPanel, false);
                    lblTrainingSkill.text = "<color=red>" + name + "\r\n<size=14>Ineligible</size></color>";
                }
                else
                {
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

        //var f = observedPlayer.GetExpFactor();
        //var expPerTick = ObservedPlayer.GetExperience(s, f);
        //var estimatedExpPerHour = expPerTick * GameMath.Exp.GetTicksPerMinute(s) * 60;
        //var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
        //var expPerHour = System.Math.Min(estimatedExpPerHour, skill.GetExperiencePerHour());
        //var expLeft = nextLevelExp - skill.Experience;

        var timeLeft = skill.GetEstimatedTimeToLevelUp() - DateTime.UtcNow;// GetEstimatedTimeForLevelUp(expPerHour, skill.Level, skill.Experience);
        var hoursLeft = timeLeft.TotalHours;
        if (hoursLeft <= 0)
            return "<color=red>Unknown</color>";

        if (timeLeft.Days >= 365 * 10_000)
        {
            return "<color=red>When hell freezes over</color>";
        }
        if (timeLeft.Days >= 365 * 1000)
        {
            return "<color=red>Unreasonably long</color>";
        }
        if (timeLeft.Days >= 365)
        {
            return "<color=red>Way too long</color>";
        }
        if (timeLeft.Days > 21)
        {
            return "<color=orange>" + (int)(timeLeft.Days / 7) + " weeks</color>";
        }
        if (timeLeft.Days > 0)
        {
            return "<color=yellow>" + timeLeft.Days + " days, " + timeLeft.Hours + " hours</color>";
        }
        if (timeLeft.Hours > 0)
        {
            return timeLeft.Hours + " hours, " + timeLeft.Minutes + " mins";
        }
        if (timeLeft.Minutes > 0)
        {
            return timeLeft.Minutes + " mins, " + timeLeft.Seconds + " secs";
        }
        return timeLeft.Seconds + " seconds";
    }

    public void Observe(PlayerController player, float timeout)
    {
        if (!player || player == null)
            playerDetailsTooltip.Disable();
        else
            playerDetailsTooltip.Enable();

        observedPlayer = player;

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