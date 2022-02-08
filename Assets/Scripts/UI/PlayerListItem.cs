using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private GameProgressBar pbHealth;
    [SerializeField] private GameProgressBar pbSkill;
    [SerializeField] private RectTransform bg;
    [SerializeField] private GameObject restedObject;
    [SerializeField] private TextMeshProUGUI lblRestedTime;

    [SerializeField] private TextMeshProUGUI lblCombatLevel;
    [SerializeField] private TextMeshProUGUI lblSkillLevel;
    [SerializeField] private TextMeshProUGUI lblExpPerHour;
    [SerializeField] private TextMeshProUGUI lblPlayerName;

    private string[] combatNames = { "Atk", "Def", "Str", "All", "Mag", "Ran", "Heal" };
    private string[] skillNames = { "Woo", "Fis", "Cra", "Coo", "Min", "Far" };

    private RectTransform rectTransform;
    private bool isRotatingSkill = false;
    private bool isCombatSkill = false;

    private float lastSkillCheckTimer = 5f;
    private float lastSkillCheckTime = 5f;

    private int skillIndex = -1;

    private GameCamera gameCamera;
    private bool hasSkill;
    private int oldHealthValue;
    private int oldHealthLevel;

    public PlayerController TargetPlayer;

    public ExpProgressHelpStates ExpProgressHelpState;
    public float ExpPerHourUpdate;

    private SkillStat lastSkillTrained;
    private int lastSkillTrainedLevel;
    private Guid playerId;
    private int lastCombatLevel;
    private GameObject lblExpPerHourObj;
    private GameObject pbSkillObj;

    //private int lastCombatLevel;

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = transform.GetComponent<RectTransform>();
        if (lblPlayerName)
            lblPlayerName.richText = true;

        lblExpPerHourObj = lblExpPerHour.gameObject;
        pbSkillObj = pbSkill.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRotatingSkill)
        {
            lastSkillCheckTimer -= Time.deltaTime;
            if (lastSkillCheckTimer <= 0)
            {
                skillIndex = (skillIndex + 1) % 3;
                lastSkillCheckTimer = lastSkillCheckTime;
            }
        }

        UpdateUI();
    }
    private void UpdateLabels()
    {
        if (!TargetPlayer)
        {
            SetText(lblSkillLevel, "");
            SetText(lblCombatLevel, "");
            SetText(lblPlayerName, "bad player");
            return;
        }

        var combatLevel = TargetPlayer.Stats.CombatLevel;
        if (combatLevel != lastCombatLevel)
        {
            SetText(lblCombatLevel, "Lv:<b> " + combatLevel);
            lastCombatLevel = combatLevel;
        }
        if (skillIndex == -1)
        {
            SetText(lblSkillLevel, "");
            //SetText(lblCombatLevel, "");
            SetActive(pbSkillObj, false);
        }
        else if (hasSkill)
        {
            SetActive(pbSkillObj, true);
            try
            {
                string skillName = null;
                SkillStat skill = null;
                if (isCombatSkill)
                {
                    if (skillIndex < combatNames.Length)
                    {
                        skillName = combatNames[skillIndex];
                        skill = TargetPlayer.GetCombatSkill(skillIndex);
                    }
                }
                else
                {
                    if (skillIndex < skillNames.Length)
                    {
                        skillName = skillNames[skillIndex];
                        skill = TargetPlayer.GetSkill(skillIndex);
                    }
                }
                if (skill != null)
                {
                    SetText(lblSkillLevel, skillName + ":<b> " + skill.Level);
                    UpdateSkillProgressBar(skill);
                    this.lastSkillTrained = skill;
                    this.lastSkillTrainedLevel = skill.Level;

                }
            }
            catch (System.Exception exc)
            {
                Shinobytes.Debug.LogError(exc.ToString());
            }
        }

        UpdateHealthBar(TargetPlayer.Stats.Health);

        if (this.playerId != TargetPlayer.Id)
        {
            this.playerId = TargetPlayer.Id;

            var playerName = TargetPlayer.PlayerNameLowerCase;
            if (TargetPlayer.CharacterIndex > 0)
                playerName += " <color=#ff444444>" + TargetPlayer.CharacterIndex;

            if (!TargetPlayer.IsUpToDate)
            {
                SetText(lblPlayerName, playerName, Color.red);
                return;
            }

            SetText(lblPlayerName, playerName);
        }
    }

    private void UpdateSkillProgressBar(SkillStat skill)
    {
        var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
        var expLeft = nextLevelExp - skill.Experience;

        pbSkill.Progress = skill.Experience > 0 && nextLevelExp > 0 ? ((float)(skill.Experience / nextLevelExp)) : 0;

        if (lblExpPerHour)
        {
            var now = Time.time;
            var sinceLastUpdate = Time.time - ExpPerHourUpdate;
            var minTime = 1f;//ExpProgressHelpState == ExpProgressHelpStates.TimeLeft ? 1f : 5f;
            if (sinceLastUpdate < minTime)
                return;

            var s = TargetPlayer.GetActiveSkill();
            var f = TargetPlayer.GetExpFactor();
            var expPerTick = TargetPlayer.GetExperience(s, f);
            var estimatedExpPerHour = expPerTick * GameMath.Exp.GetTicksPerMinute(s) * 60;
            var expPerHour = System.Math.Min(estimatedExpPerHour, skill.GetExperiencePerHour());

            SetText(lblExpPerHour, "");
            switch (ExpProgressHelpState)
            {
                case ExpProgressHelpStates.ExpLeft:
                    SetText(lblExpPerHour, FormatValue((long)expLeft) + " xp");
                    break;

                case ExpProgressHelpStates.TimeLeft:
                    {
                        if (expPerHour <= 0 || expLeft <= 0) return;
                        var hours = (double)(expLeft / expPerHour);
                        var text = Utility.FormatDayTime(hours);
                        SetText(lblExpPerHour, text);
                    }
                    break;

                case ExpProgressHelpStates.ExpPerHour:
                    if (expPerHour <= 0) return;
                    SetText(lblExpPerHour, Utility.FormatValue(expPerHour) + " xp / h");
                    break;
            }
            ExpPerHourUpdate = now;
        }
    }
    private void UpdateHealthBar(SkillStat skill)
    {
        var now = skill.CurrentValue;
        var next = skill.Level;
        if (oldHealthValue != now || oldHealthLevel != skill.Level)
        {
            pbHealth.Progress = (float)now / (float)next;
            oldHealthValue = skill.CurrentValue;
            oldHealthLevel = skill.Level;
        }
    }
    private static string FormatValue(long num)
    {
        var str = num.ToString();
        if (str.Length <= 3) return str;
        for (var i = str.Length - 3; i >= 0; i -= 3)
            str = str.Insert(i, " ");
        return str;
    }

    public void UpdatePlayerInfo(PlayerController player, GameCamera gameCamera)
    {
        this.gameCamera = gameCamera;

        if (!lblExpPerHourObj) lblExpPerHourObj = lblExpPerHour.gameObject;
        if (!pbSkillObj) pbSkillObj = pbSkill.gameObject;

        SetText(lblSkillLevel, "");
        SetText(lblPlayerName, player.PlayerNameLowerCase);

        TargetPlayer = player;

        var combatLevel = player.Stats.CombatLevel;
        if (combatLevel != lastCombatLevel)
        {
            SetText(lblCombatLevel, "Lv:<b> " + combatLevel);
            lastCombatLevel = combatLevel;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (lblExpPerHour)
        {
            SetActive(lblExpPerHourObj, ExpProgressHelpState != ExpProgressHelpStates.NotVisible);
        }

        if (!TargetPlayer)
        {
            UpdateLabels();
            return;
        }

        if (TargetPlayer.Rested.RestedTime > 0)
        {
            SetActive(restedObject, true);

            var time = Utility.FormatTime(TargetPlayer.Rested.RestedTime / 60f / 60f, false);

            if (TargetPlayer.Onsen.InOnsen)
            {
                SetText(lblRestedTime, "<color=#FFDE00>" + time);
                // increasing time
            }
            else
            {
                SetText(lblRestedTime, time);
                // decreasing time
            }
        }
        else
        {
            SetActive(restedObject, false);
        }

        //progressBarHealth.offsetMax = new Vector2(-this.width, progressBarHealth.offsetMin.y);
        //progressBarSkill.offsetMax = new Vector2(-this.width, progressBarSkill.offsetMin.y);

        var taskArgs = TargetPlayer.GetTaskArguments();
        hasSkill = taskArgs != null && taskArgs.Count > 0;

        if (hasSkill)
        {
            skillIndex = -1;
            isCombatSkill = false;
            isRotatingSkill = false;

            var si = TargetPlayer.CombatType;
            if (si != -1)
            {
                isCombatSkill = true;
                skillIndex = si;
                if (si == 3)
                {
                    skillIndex = 0;
                    isRotatingSkill = true;
                }
            }
            else
            {
                skillIndex = TargetPlayer.SkillType;
            }
        }

        UpdateLabels();
    }

    public void ObservePlayer()
    {
        if (gameCamera && TargetPlayer)
        {
            gameCamera.ObservePlayer(TargetPlayer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label.text != value)
        {
            label.text = value;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetText(TextMeshProUGUI label, string value, Color color)
    {
        if (color == default) color = Color.white;
        var newValue = color != Color.white ? "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + value : value;
        if (label.text != newValue)
        {
            label.text = newValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetActive(GameObject gameObject, bool value)
    {
        if (gameObject.activeSelf != value)
        {
            gameObject.SetActive(value);
        }
    }
}

public enum ExpProgressHelpStates : int
{
    NotVisible,
    ExpPerHour,
    ExpLeft,
    TimeLeft,
}