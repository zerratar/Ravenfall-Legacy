using Assets.Scripts;
using UnityEngine;

public class TownHallInfoManager : MonoBehaviour
{
    [SerializeField] private VillageManager villageManager;
    [SerializeField] private TMPro.TextMeshPro lblTier;
    [SerializeField] private TMPro.TextMeshPro lblLevel;
    [SerializeField] private TMPro.TextMeshPro lblSlots;
    [SerializeField] private TMPro.TextMeshPro lblTotalExpBonus;

    public int Tier { get; set; }
    public double Experience { get; set; }
    public int Level { get; set; }
    public int Slots { get; set; }
    public int UsedSlots { get; set; }
    // Start is called before the first frame update
    void Start()
    {
        if (!villageManager) villageManager = FindObjectOfType<VillageManager>();
        Hide();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameSystems.frameCount % 4 == 0)
        {
            var nextLevel = Level + 1;
            var nextLevelExperience = GameMath.ExperienceForLevel(nextLevel);
            var progress = Experience > 0 && nextLevel > 0 ? System.Math.Round((float)(Experience / nextLevelExperience) * 100f, 2) : 0;

            lblLevel.text = "Level: " + Level + " (" + progress + "%)";
            lblSlots.text = "Slots: " + UsedSlots + "/" + Slots;
            lblTier.text = "Tier: " + Tier;
        }

        //if (Time.frameCount % 30 == 0)
        //{
        //    UpdateExpBonusTexts();
        //}
    }

    public void UpdateExpBonusTexts()
    {
        var bonuses = villageManager.GetGroupedExpBonuses();
        if (bonuses.Count > 0)
        {
            lblTotalExpBonus.text = "";
            foreach (var bonus in bonuses)
            {
                if (bonus.SlotType == TownHouseSlotType.Empty || bonus.SlotType == TownHouseSlotType.Undefined)
                    continue;

                if (bonus.Bonus > 0)
                {
                    lblTotalExpBonus.text += bonus.SlotType + ": " + Mathf.RoundToInt(bonus.Bonus) + "%\r\n";
                }
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
