using System.Collections.Generic;
using UnityEngine;
using RavenNest.Models;
using UnityEngine.UI;

public class IslandDetails : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("Island")]
    [SerializeField] private TMPro.TextMeshProUGUI islandName;
    [SerializeField] private TMPro.TextMeshProUGUI requiredLevel;
    [SerializeField] private GameObject onsenAvailable;
    [SerializeField] private TMPro.TextMeshProUGUI playerCount;
    [SerializeField] private TMPro.TextMeshProUGUI lblObserverTime;
    [SerializeField] private Image imgObserverTimePg;


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
    private float timeoutLength;
    private Dictionary<RavenNest.Models.Skill, int> skillCounter = new Dictionary<RavenNest.Models.Skill, int>();
    private string playerCountFormat;
    private string requiredLevelFormat;
    private string observerTimeoutFormat;

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

        island.RebuildPlayerList();

        this.gameObject.SetActive(true);
        this.target = island;
        this.timeout = timer;
        this.timeoutLength = timer;
        this.UpdateUI();
    }

    private void UpdateUI()
    {
        try
        {
            if (string.IsNullOrEmpty(requiredLevelFormat))
            {
                requiredLevelFormat = requiredLevel.text;
            }

            if (string.IsNullOrEmpty(playerCountFormat))
            {
                playerCountFormat = playerCount.text;
            }

            if (string.IsNullOrEmpty(observerTimeoutFormat))
            {
                observerTimeoutFormat = lblObserverTime.text;
            }

            if (target == null)
            {
                return;
            }

            var players = target.GetPlayers();

            islandName.text = target.Identifier;
            imgObserverTimePg.fillAmount = timeout / timeoutLength;

            lblObserverTime.text = string.Format(observerTimeoutFormat, Mathf.RoundToInt(timeout));
            requiredLevel.text = string.Format(requiredLevelFormat, target.LevelRequirement);
            onsenAvailable.SetActive(gameManager.Onsen.RestingAreaAvailable(target));
            playerCount.text = string.Format(playerCountFormat, players.Count);

            foreach (var skill in SkillUtilities.Skills)
            {
                skillCounter[skill] = 0;
            }

            foreach (var player in players)
            {
                if (player == null || player.isDestroyed || !player)
                {
                    continue;
                }

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
            attack.text = Utility.FormatAmount(skillCounter[Skill.Attack]);
            defense.text = Utility.FormatAmount(skillCounter[Skill.Defense]);
            strength.text = Utility.FormatAmount(skillCounter[Skill.Strength]);
            magic.text = Utility.FormatAmount(skillCounter[Skill.Magic]);
            ranged.text = Utility.FormatAmount(skillCounter[Skill.Ranged]);
            healing.text = Utility.FormatAmount(skillCounter[Skill.Health]);
            alchemy.text = Utility.FormatAmount(skillCounter[Skill.Alchemy]);
            woodcutting.text = Utility.FormatAmount(skillCounter[Skill.Woodcutting]);
            fishing.text = Utility.FormatAmount(skillCounter[Skill.Fishing]);
            mining.text = Utility.FormatAmount(skillCounter[Skill.Mining]);
            crafting.text = Utility.FormatAmount(skillCounter[Skill.Crafting]);
            cooking.text = Utility.FormatAmount(skillCounter[Skill.Cooking]);
            farming.text = Utility.FormatAmount(skillCounter[Skill.Farming]);
            gathering.text = Utility.FormatAmount(skillCounter[Skill.Gathering]);

            // update the statistics
            var statistics = target.Statistics;
            monstersDefeated.text = Utility.FormatAmount(statistics.MonstersDefeated);
            playersKilled.text = Utility.FormatAmount(statistics.PlayersKilled);
            raidBosses.text = Utility.FormatAmount(statistics.RaidBossesSpawned);
            itemsGathered.text = Utility.FormatAmount(statistics.ItemsGathered);
            treesCutDown.text = Utility.FormatAmount(statistics.TreesCutDown);
            rocksMined.text = Utility.FormatAmount(statistics.RocksMined);
            fishCaught.text = Utility.FormatAmount(statistics.FishCaught);
            cropsHarvested.text = Utility.FormatAmount(statistics.CropsHarvested);
            foodCooked.text = Utility.FormatAmount(statistics.FoodCooked);
            itemsCrafted.text = Utility.FormatAmount(statistics.ItemsCrafted);
            potionsBrewed.text = Utility.FormatAmount(statistics.PotionsBrewed);
        }
        catch
        {
        }
    }
}