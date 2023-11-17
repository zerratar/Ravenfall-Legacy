using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignPlayerRow : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayerName;
    [SerializeField] private TMPro.TextMeshProUGUI lblSkill;
    [SerializeField] private TMPro.TextMeshProUGUI lblBonus;
    [SerializeField] private GameObject lblHouseOwner;

    private TownHousePlayerAssignDialog playerAssignDialog;
    private PlayerController player;

    private void Start()
    {
        if (!playerAssignDialog) playerAssignDialog = FindAnyObjectByType<TownHousePlayerAssignDialog>();
    }

    public void SetPlayer(PlayerController player, bool isHouseOwner, TownHouseController dialogHouse)
    {
        this.player = player;
        var existingBonus = 0;

        lblHouseOwner.SetActive(isHouseOwner);

        if (dialogHouse.Slot.PlayerSkills != null)
        {
            var existingSkill = GameMath.GetSkillByHouseType(dialogHouse.Slot.PlayerSkills, dialogHouse.TownHouse.Type);

            //  (stats.Attack.Level+stats.Defense.Level+stats.Strength.Level)/3 for combat?

            existingBonus = (int)GameMath.CalculateHouseExpBonus(existingSkill);
        }

        var skill = GameMath.GetSkillByHouseType(player.Stats, dialogHouse.TownHouse.Type);
        var playerBonus = (int)GameMath.CalculateHouseExpBonus(skill);
        var bonusPlus = playerBonus - existingBonus;

        lblPlayerName.text = player.Name;
        lblSkill.text = skill.Name + " Lv. " + skill.Level;

        if (bonusPlus == 0)
        {
            lblBonus.text = $"<color=#FFFFFF>{playerBonus}% (+0%)";
            return;
        }

        if (bonusPlus > 0)
        {
            lblBonus.text = $"<color=#FFFFFF>{playerBonus}% <color=#0BFF00>(+{bonusPlus}%)";
        }
        else
        {
            lblBonus.text = $"<color=#FF0B00>{playerBonus}% (-{bonusPlus}%)";
        }
    }

    public void OnClick()
    {
        playerAssignDialog.SelectedPlayer = player;
    }
}