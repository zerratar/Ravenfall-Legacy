using System.Collections.Generic;
using UnityEngine;
using RavenNest.Models;

public class IslandDetails : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("Island")]
    [SerializeField] private TMPro.TextMeshProUGUI islandName;
    [SerializeField] private TMPro.TextMeshProUGUI requiredLevel;
    [SerializeField] private GameObject onsenAvailable;
    [SerializeField] private TMPro.TextMeshProUGUI playerCount;


    /*
        Skill Training Distribution
     */
    [Header("Skill Training Distribution")]
    [SerializeField] private TMPro.TextMeshProUGUI attack;
    [SerializeField] private TMPro.TextMeshProUGUI defense;
    [SerializeField] private TMPro.TextMeshProUGUI strength;
    [SerializeField] private TMPro.TextMeshProUGUI magic;
    [SerializeField] private TMPro.TextMeshProUGUI ranged;
    [SerializeField] private TMPro.TextMeshProUGUI healing;
    [SerializeField] private TMPro.TextMeshProUGUI alchemy;
    [SerializeField] private TMPro.TextMeshProUGUI woodcutting;
    [SerializeField] private TMPro.TextMeshProUGUI fishing;
    [SerializeField] private TMPro.TextMeshProUGUI mining;
    [SerializeField] private TMPro.TextMeshProUGUI crafting;
    [SerializeField] private TMPro.TextMeshProUGUI cooking;
    [SerializeField] private TMPro.TextMeshProUGUI farming;
    [SerializeField] private TMPro.TextMeshProUGUI gathering;

    /*
        Statistics
     */

    [Header("Statistics")]
    [SerializeField] private TMPro.TextMeshProUGUI monstersDefeated;
    [SerializeField] private TMPro.TextMeshProUGUI playersKilled;
    [SerializeField] private TMPro.TextMeshProUGUI raidBosses;
    [SerializeField] private TMPro.TextMeshProUGUI itemsGathered;
    [SerializeField] private TMPro.TextMeshProUGUI treesCutDown;
    [SerializeField] private TMPro.TextMeshProUGUI rocksMined;
    [SerializeField] private TMPro.TextMeshProUGUI fishCaught;
    [SerializeField] private TMPro.TextMeshProUGUI cropsHarvested;
    [SerializeField] private TMPro.TextMeshProUGUI foodCooked;
    [SerializeField] private TMPro.TextMeshProUGUI itemsCrafted;
    [SerializeField] private TMPro.TextMeshProUGUI potionsBrewed;

    private IslandController target;
    private float timeout;
    private Dictionary<RavenNest.Models.Skill, int> skillCounter = new Dictionary<RavenNest.Models.Skill, int>();
    private string playerCountFormat;
    private string requiredLevelFormat;

    private void Awake()
    {
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (target == null)
        {
            this.gameObject.SetActive(false);
            return;
        }

        timeout -= GameTime.deltaTime;

        this.UpdateUI();
    }

    internal void Observe(IslandController island, float timer)
    {
        if (island == null)
        {
            this.target = null;
            this.timeout = 0;
            return;
        }
        this.gameObject.SetActive(true);
        this.target = island;
        this.timeout = timer;
        this.UpdateUI();
    }

    private void UpdateUI()
    {

        if (string.IsNullOrEmpty(requiredLevelFormat))
        {
            requiredLevelFormat = requiredLevel.text;
        }

        if (string.IsNullOrEmpty(playerCountFormat))
        {
            playerCountFormat = playerCount.text;
        }

        var players = target.GetPlayers();
        islandName.text = target.name;
        requiredLevel.text = string.Format(requiredLevelFormat, target.LevelRequirement);
        onsenAvailable.SetActive(gameManager.Onsen.RestingAreaAvailable(target));
        playerCount.text = string.Format(playerCountFormat, players.Count);

        foreach (var skill in SkillUtilities.Skills)
        {
            skillCounter[skill] = 0;
        }

        foreach (var player in players)
        {
            var taskType = player.GetTask();
            var activeSkill = player.GetActiveSkillStat();
            if (taskType == TaskType.Fighting)
            {
                if (activeSkill.Type == Skill.Health || activeSkill.Type == Skill.Melee)
                {
                    skillCounter[Skill.Attack]++;
                    skillCounter[Skill.Defense]++;
                    skillCounter[Skill.Strength]++;
                    continue;
                }
            }

            skillCounter[activeSkill.Type]++;
        }

        // update skill training distribution ui
        attack.text = skillCounter[Skill.Attack].ToString();
        defense.text = skillCounter[Skill.Defense].ToString();
        strength.text = skillCounter[Skill.Strength].ToString();
        magic.text = skillCounter[Skill.Magic].ToString();
        ranged.text = skillCounter[Skill.Ranged].ToString();
        healing.text = skillCounter[Skill.Health].ToString();
        alchemy.text = skillCounter[Skill.Alchemy].ToString();
        woodcutting.text = skillCounter[Skill.Woodcutting].ToString();
        fishing.text = skillCounter[Skill.Fishing].ToString();
        mining.text = skillCounter[Skill.Mining].ToString();
        crafting.text = skillCounter[Skill.Crafting].ToString();
        cooking.text = skillCounter[Skill.Cooking].ToString();
        farming.text = skillCounter[Skill.Farming].ToString();
        gathering.text = skillCounter[Skill.Gathering].ToString();

        // update the statistics
        var statistics = target.Statistics;
        monstersDefeated.text = statistics.MonstersDefeated.ToString();
        playersKilled.text = statistics.PlayersKilled.ToString();
        raidBosses.text = statistics.RaidBossesSpawned.ToString();
        itemsGathered.text = statistics.ItemsGathered.ToString();
        treesCutDown.text = statistics.TreesCutDown.ToString();
        rocksMined.text = statistics.RocksMined.ToString();
        fishCaught.text = statistics.FishCaught.ToString();
        cropsHarvested.text = statistics.CropsHarvested.ToString();
        foodCooked.text = statistics.FoodCooked.ToString();
        itemsCrafted.text = statistics.ItemsCrafted.ToString();
        potionsBrewed.text = statistics.PotionsBrewed.ToString();
    }
}