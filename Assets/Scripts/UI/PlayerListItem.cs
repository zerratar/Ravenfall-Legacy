using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    //[SerializeField] private UnityEngine.UI.Text lblSkillIcon;
    [SerializeField] private GameProgressBar pbHealth;
    [SerializeField] private GameProgressBar pbSkill;

    [SerializeField] private TextMeshProUGUI lblCombatLevel;
    [SerializeField] private TextMeshProUGUI lblSkillLevel;
    [SerializeField] private TextMeshProUGUI lblExpPerHour;
    [SerializeField] private TextMeshProUGUI lblPlayerName;

    [SerializeField] private string[] combatNames = { "Atk", "Def", "Str", "All", "Mag", "Ran" };
    [SerializeField] private string[] skillNames = { "Woo", "Fis", "Coo", "Cra", "Min", "Far" };

    public bool ExpPerHourVisible = false;

    private RectTransform rectTransform;

    private bool isRotatingSkill = false;
    private bool isCombatSkill = false;

    private float lastSkillCheckTimer = 5f;
    private float lastSkillCheckTime = 5f;

    private int skillIndex = -1;

    //private char[] combatIcons = { '\uf71d', '\uf132', '\uf6de' };
    //private char[] skillIcons = { '\uf724', '\uf578', '\uf6e3', '\uf7eb', '\uf6fd' };

    private GameCamera gameCamera;
    private bool hasSkill;
    private int oldHealthValue;

    public PlayerController TargetPlayer { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = transform.GetComponent<RectTransform>();
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

        if (skillIndex == -1)
        {
            SetText(lblSkillLevel, "");
            SetText(lblCombatLevel, "");
            SetActive(pbSkill.gameObject, false);
        }
        else if (hasSkill)
        {
            SetActive(pbSkill.gameObject, true);

            var skillName = isCombatSkill
                ? combatNames[skillIndex]
                : skillNames[skillIndex];

            skillName = ToSentenceCase(skillName);

            var skill = isCombatSkill
                ? TargetPlayer.GetCombatSkill(skillIndex)
                : TargetPlayer.GetSkill(skillIndex);

            SetText(lblSkillLevel, $"{skillName}: <b>{skill.Level}");

            UpdateSkillProgressBar(skill);
        }

        SetText(lblCombatLevel, $"Lv: <b>{TargetPlayer.Stats.CombatLevel}");

        UpdateHealthBar(TargetPlayer.Stats.Health);
    }


    private string ToSentenceCase(string str)
    {
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    private void UpdateSkillProgressBar(SkillStat skill)
    {
        var thisLevelExp = GameMath.LevelToExperience(skill.Level);
        var nextLevelExp = GameMath.LevelToExperience(skill.Level + 1);
        var now = skill.Experience - thisLevelExp;
        var next = nextLevelExp - thisLevelExp;
        pbSkill.progress = (float)now / (float)next;

        var expPerHour = skill.GetExperiencePerHour();
        if (lblExpPerHour && ExpPerHourVisible)
        {
            SetText(lblExpPerHour, Utility.FormatValue(expPerHour) + " xp / h");
        }
    }

    private void UpdateHealthBar(SkillStat skill)
    {
        var now = skill.CurrentValue;
        var next = skill.Level;
        pbHealth.progress = (float)now / (float)next;
        oldHealthValue = skill.CurrentValue;
    }

    public void UpdatePlayerInfo(PlayerController player, GameCamera gameCamera)
    {
        this.gameCamera = gameCamera;

        SetText(lblSkillLevel, "");
        SetText(lblCombatLevel, "");
        SetText(lblPlayerName, player.PlayerName.ToLower());

        TargetPlayer = player;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (lblExpPerHour)
        {
            SetActive(lblExpPerHour.gameObject, ExpPerHourVisible);
        }

        if (!TargetPlayer)
        {
            UpdateLabels();
            return;
        }

        //progressBarHealth.offsetMax = new Vector2(-this.width, progressBarHealth.offsetMin.y);
        //progressBarSkill.offsetMax = new Vector2(-this.width, progressBarSkill.offsetMin.y);

        var taskArgs = TargetPlayer.GetTaskArguments();
        hasSkill = taskArgs != null && taskArgs.Length > 0;

        if (hasSkill)
        {
            skillIndex = -1;
            isCombatSkill = false;
            isRotatingSkill = false;

            var si = TargetPlayer.GetCombatTypeFromArgs(taskArgs);
            if (si != -1)
            {
                isCombatSkill = true;
                var combatSkillIndex = skillIndex = TargetPlayer.GetCombatTypeFromArgs(taskArgs);
                if (combatSkillIndex == 3)
                {
                    skillIndex = 0;
                    isRotatingSkill = true;
                }
            }
            else
            {
                skillIndex = TargetPlayer.GetSkillTypeFromArgs(taskArgs);
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
    private static void SetActive(GameObject gameObject, bool value)
    {
        if (gameObject.activeSelf != value)
        {
            gameObject.SetActive(value);
        }
    }
}