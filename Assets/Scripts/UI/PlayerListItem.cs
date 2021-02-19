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

    private string[] combatNames = { "Atk", "Def", "Str", "All", "Mag", "Ran", "Heal" };
    private string[] skillNames = { "Woo", "Fis", "Cra", "Coo", "Min", "Far" };

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

    public ExpProgressHelpStates ExpProgressHelpState;


    // Start is called before the first frame update
    void Start()
    {
        rectTransform = transform.GetComponent<RectTransform>();
        if (lblPlayerName)
            lblPlayerName.richText = true;
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

            //skillName = ToSentenceCase(skillName);

            var skill = isCombatSkill
                ? TargetPlayer.GetCombatSkill(skillIndex)
                : TargetPlayer.GetSkill(skillIndex);

            SetText(lblSkillLevel, skillName + ": <b>" + skill.Level);

            UpdateSkillProgressBar(skill);
        }

        SetText(lblCombatLevel, "Lv: <b>" + TargetPlayer.Stats.CombatLevel);

        UpdateHealthBar(TargetPlayer.Stats.Health);

        if (TargetPlayer)
        {
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
        pbSkill.Progress = skill.Experience > 0 && nextLevelExp > 0 ? ((float)(skill.Experience / nextLevelExp)) : 0;

        var expPerHour = skill.GetExperiencePerHour();
        var expLeft = nextLevelExp - skill.Experience;

        if (lblExpPerHour)
        {
            SetText(lblExpPerHour, "");
            switch (ExpProgressHelpState)
            {
                case ExpProgressHelpStates.ExpLeft:
                    SetText(lblExpPerHour, FormatValue((long)expLeft) + " xp");
                    break;

                case ExpProgressHelpStates.TimeLeft:
                    {
                        if (expPerHour <= 0 || expLeft <= 0) break;

                        var hours = (double)(expLeft / expPerHour);
                        if (hours < 1)
                        {
                            var minutes = hours * 60d;
                            if (minutes > 1)
                            {
                                SetText(lblExpPerHour, (int)minutes + "m");
                            }
                            else
                            {
                                var seconds = minutes * 60d;
                                SetText(lblExpPerHour, (int)seconds + "s");
                            }
                        }
                        else
                        {
                            var minutes = (int)(hours - (long)hours) * 60d;
                            if (minutes > 1)
                            {
                                SetText(lblExpPerHour, (int)hours + "h " + minutes + "m");
                            }
                            else
                            {
                                SetText(lblExpPerHour, (int)hours + "h");
                            }
                        }
                    }
                    break;

                case ExpProgressHelpStates.ExpPerHour:
                    if (expPerHour <= 0) break;
                    SetText(lblExpPerHour, Utility.FormatValue(expPerHour) + " xp / h");
                    break;
            }
        }
    }

    private void UpdateHealthBar(SkillStat skill)
    {
        var now = skill.CurrentValue;
        var next = skill.Level;
        pbHealth.Progress = (float)now / (float)next;
        oldHealthValue = skill.CurrentValue;
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

        SetText(lblSkillLevel, "");
        SetText(lblCombatLevel, "");
        SetText(lblPlayerName, player.PlayerNameLowerCase);

        TargetPlayer = player;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (lblExpPerHour)
        {
            SetActive(lblExpPerHour.gameObject, ExpProgressHelpState != ExpProgressHelpStates.NotVisible);
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