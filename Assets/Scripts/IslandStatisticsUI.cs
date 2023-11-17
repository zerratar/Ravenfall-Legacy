using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Skill = RavenNest.Models.Skill;
public class IslandStatisticsUI : MonoBehaviour
{

    [SerializeField] private TMPro.TextMeshProUGUI lblName;
    [SerializeField] private TMPro.TextMeshProUGUI lblKills;
    [SerializeField] private TMPro.TextMeshProUGUI lblTrees;
    [SerializeField] private TMPro.TextMeshProUGUI lblMining;
    [SerializeField] private TMPro.TextMeshProUGUI lblFishing;
    [SerializeField] private TMPro.TextMeshProUGUI lblCrafting;
    [SerializeField] private TMPro.TextMeshProUGUI lblFarming;
    [SerializeField] private TMPro.TextMeshProUGUI lblHealing;

    private GameManager gameManager;

    public readonly static ExpGainStatisticsData Data = new ExpGainStatisticsData();

    // Start is called before the first frame update
    void Start()
    {
        ClearValues();

        Data.Reset();

        if (!Application.isEditor)
        {
            this.gameObject.SetActive(false);
            return;
        }

        gameManager = FindAnyObjectByType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameManager) return;
        var cameraPosition = gameManager.Camera.transform.position;
        var currentIsland = gameManager.Islands.FindIsland(cameraPosition);
        if (!currentIsland)
        {
            ClearValues();
            return;
        }

        lblName.text = currentIsland.Identifier;

        if (Data.HasModified)
        {
            var islandData = Data[currentIsland.Identifier];

            Data.PlayerCount = currentIsland.GetPlayers().Count;

            UpdateLabel(lblHealing, islandData[Skill.Healing]);
            UpdateLabel(lblKills, islandData[Skill.Health]);
            UpdateLabel(lblMining, islandData[Skill.Mining]);
            UpdateLabel(lblTrees, islandData[Skill.Woodcutting]);
            UpdateLabel(lblFishing, islandData[Skill.Fishing]);
            UpdateLabel(lblFarming, islandData[Skill.Farming]);
            UpdateLabel(lblCrafting, islandData[Skill.Crafting]);
        }
    }

    private void UpdateLabel(TextMeshProUGUI lbl, ExpGainStatisticsData.SkillExpGainStatisticsData data)
    {
        lbl.text = (data.TicksPerSeconds / Data.PlayerCount) + " (" + System.Math.Round(data.AvgTicksPerSeconds, 2) + ") " + lbl.gameObject.name + " ticks per second";  //" per second";
    }

    private void ClearValues()
    {
        lblName.text = "";
        lblKills.text = "";
        lblTrees.text = "";
        lblMining.text = "";
        lblFishing.text = "";
        lblCrafting.text = "";
        lblFarming.text = "";
        lblHealing.text = "";
    }
}

public class ExpGainStatisticsData
{
    private bool hasModified;

    public readonly SkillExpGainStatisticsData Sailing = new SkillExpGainStatisticsData
    {
        Skill = Skill.Sailing
    };


    public readonly SkillExpGainStatisticsData Slayer = new SkillExpGainStatisticsData
    {
        Skill = Skill.Slayer
    };

    public readonly Dictionary<string, IslandExpGainStatisticsData> IslandData
        = new Dictionary<string, IslandExpGainStatisticsData>();

    internal void Reset()
    {
        Sailing.Reset();
        Slayer.Reset();
        IslandData.Clear();
    }

    public bool HasModified
    {
        get
        {
            var modified = hasModified;
            hasModified = false;
            return modified;
        }
    }

    public int PlayerCount { get; internal set; }

    public IslandExpGainStatisticsData this[string island]
    {
        get
        {
            if (!IslandData.TryGetValue(island, out var data))
            {
                IslandData[island] = (data = new IslandExpGainStatisticsData());
            }

            return data;
        }
    }

    internal void ExpTick(IslandController island, Skill skill)
    {
        // NOTE: Slayer EXP is tricky! The actual ticks are few
        //       therefor the min/max range of exp gains needs to be adjusted specifically for those.
        if (skill == Skill.Sailing)
        {
            Sailing.Increment();
        }
        else if (skill == Skill.Slayer)
        {
            Slayer.Increment();
        }
        else if (island)
        {
            if (!IslandData.TryGetValue(island.Identifier, out var data))
            {
                IslandData[island.Identifier] = (data = new IslandExpGainStatisticsData()
                {
                    IslandIdentifier = island.Identifier
                });
            }
            data[skill].Increment();
        }

        hasModified = true;
    }

    public class IslandExpGainStatisticsData
    {
        public string IslandIdentifier;
        private Dictionary<Skill, SkillExpGainStatisticsData> data = new Dictionary<Skill, SkillExpGainStatisticsData>();

        public SkillExpGainStatisticsData this[Skill skill]
        {
            get
            {
                if (data.TryGetValue(skill, out var v)) return v;
                return data[skill] = new SkillExpGainStatisticsData
                {
                    Skill = skill
                };
            }
            set
            {
                data[skill] = value;
            }
        }
    }

    public class SkillExpGainStatisticsData
    {
        public Skill Skill;
        public float TickStart;
        public int TotalTicks;

        public float TicksPerSeconds = 0f;
        public float AvgTicksPerSeconds = 0f;
        private int ticks;
        private float refreshRate = 30f;
        private float lastTick;

        private float totalTicksPerSecondsSamples = 0f;
        private float firstStart = 0f;

        public void Reset()
        {
            TotalTicks = 0;
            TickStart = 0;
            TicksPerSeconds = 0;
        }

        public void Increment()
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (firstStart == 0)
            {
                firstStart = now;
            }
            ++ticks;
            if (++TotalTicks == 1 || now - TickStart >= refreshRate)
            {
                TickStart = now;
                ticks = 0;

                AvgTicksPerSeconds = totalTicksPerSecondsSamples / (now - firstStart);
            }

            lastTick = now;
            var elapsed = (lastTick - TickStart);
            if (elapsed > 0)
            {
                TicksPerSeconds = ticks / (lastTick - TickStart);
            }
            totalTicksPerSecondsSamples += TicksPerSeconds;
        }
    }
}
