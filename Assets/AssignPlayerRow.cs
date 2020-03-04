using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignPlayerRow : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lblPlayerName;
    [SerializeField] private TMPro.TextMeshProUGUI lblSkill;
    [SerializeField] private TMPro.TextMeshProUGUI lblBonus;

    private TownHousePlayerAssignDialog playerAssignDialog;
    private PlayerController player;

    private void Start()
    {
        if (!playerAssignDialog) playerAssignDialog = FindObjectOfType<TownHousePlayerAssignDialog>();
    }

    public void SetPlayer(PlayerController player, TownHouseController townHouse)
    {
        this.player = player;
        var existingBonus = 0;
        if (townHouse.Owner)
        {
            var existingSkill = GameMath.GetSkillByHouseType(player.Stats, townHouse.TownHouse.Type);
            existingBonus = (int)GameMath.CalculateHouseExpBonus(existingSkill);
        }

        var skill = GameMath.GetSkillByHouseType(player.Stats, townHouse.TownHouse.Type);
        var playerBonus = (int)GameMath.CalculateHouseExpBonus(skill);
        var bonusPlus = playerBonus - existingBonus;

        lblPlayerName.text = player.Name;
        lblSkill.text = skill.Name + " Lv. " + skill.CurrentValue;

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
