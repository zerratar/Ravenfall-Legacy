using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TownHallInfoManager : MonoBehaviour
{

    [SerializeField] private VillageManager villageManager;

    [SerializeField] private TMPro.TextMeshPro lblTier;
    [SerializeField] private TMPro.TextMeshPro lblLevel;
    [SerializeField] private TMPro.TextMeshPro lblSlots;
    [SerializeField] private TMPro.TextMeshPro lblTotalExpBonus;

    public int Tier { get; set; }
    public decimal Experience { get; set; }
    public int Level { get; set; }
    public int Slots { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        if (!villageManager) villageManager = FindObjectOfType<VillageManager>();
        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        var nextLevel = GameMath.LevelToExperience(Level + 1);
        var progress = Mathf.RoundToInt(Mathf.Floor((float)(Experience / nextLevel) * 100f));
        lblLevel.text = "Level: " + Level + " (" + progress + "%)";
        lblSlots.text = "Slots: " + Slots;
        lblTier.text = "Tier: " + Tier;

        var bonuses = villageManager.GetExpBonuses().GroupBy(x => x.SlotType).ToList();
        if (bonuses.Count > 0)
        {
            lblTotalExpBonus.text = "";
            foreach (var bonus in bonuses)
            {
                if (bonus.Key == TownHouseSlotType.Empty)
                    continue;

                var expBonus = 0f;
                var values = bonus.ToList();
                if (values.Count > 0)
                    expBonus = values.Sum(x => x.Bonus) / 100f;

                lblTotalExpBonus.text += bonus.Key + ": " + Mathf.RoundToInt(expBonus * 100f) + "%\r\n";
            }
        }
        else
        {
            lblTotalExpBonus.text = "No active bonus";
        }
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }
    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    internal void Toggle()
    {
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }
}
