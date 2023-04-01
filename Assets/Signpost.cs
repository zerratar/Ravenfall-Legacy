using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

public class Signpost : MonoBehaviour
{
    public TaskType Type;
    public IslandController Island;
    public TMPro.TextMeshProUGUI Label;
    public Chunk Chunk;
    private int lastCombatLevel;
    private int lastReqSkillLevel;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }
    public void FindIsland()
    {
        var islands = FindObjectsOfType<IslandController>();
        foreach (var island in islands)
        {
            island.Awake();
            if (island.InsideIsland(this.transform.position))
            {
                this.Island = island;
            }
        }
    }

    public void FindTaskType()
    {
        var txtName = this.name;
        txtName = txtName?.ToLower();
        if (string.IsNullOrEmpty(txtName))
            return;

        var type = TaskType.Fighting;
        if (txtName.Contains("cook") || txtName.Contains("craft"))
        {
            // cooking & crafting
            type = TaskType.Cooking;
        }
        else if (txtName.Contains("farm"))
        {
            type = TaskType.Farming;
        }
        else if (txtName.Contains("min"))
        {
            type = TaskType.Mining;
        }
        else if (txtName.Contains("wood"))
        {
            type = TaskType.Woodcutting;
        }
        else if (txtName.Contains("fish"))
        {
            type = TaskType.Fishing;
        }
        else if (!txtName.Contains("combat"))
        {
            // try figuring it out.

        }

        Type = type;
    }

    public void FindChunk()
    {
        if (!Island) return;
        if (!Label) Label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (!Label) return;

        var chunkManager = FindObjectOfType<ChunkManager>();

        chunkManager.Init(true);

        var chunks = chunkManager.GetChunksOfType(Island, Type);
        if (chunks == null || chunks.Count == 0) return;
        var chunk = chunks.FirstOrDefault();
        this.Chunk = chunk;
        UpdateLevelRequirement();
    }

    [Button("Update Sign")]
    public void Bruteforce()
    {
        for (var i = 0; i < 2; ++i)
        {
            FindIsland();
            FindTaskType();
            FindChunk();
        }
    }

    public void UpdateLevelRequirement()
    {
        if (!Label) return;
        if (Label.fontSize != 0.8f)
        {
            Label.fontSize = 0.8f;
            Label.alignment = TMPro.TextAlignmentOptions.Center;
            Label.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
            Label.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        }
        var reqCombatLevel = Chunk.RequiredCombatLevel;
        if (reqCombatLevel > 1)
        {
            Label.text = "Lv. " + reqCombatLevel;
            this.lastCombatLevel = reqCombatLevel;
            this.name = "Lv." + reqCombatLevel + " Combat";
            return;
        }

        var reqSkillLevel = Chunk.RequiredSkilllevel;
        if (reqSkillLevel > 1)
        {
            Label.text = "Lv. " + reqSkillLevel;
            lastReqSkillLevel = reqSkillLevel;
            this.name = "Lv." + reqSkillLevel + " " + Type;
        }
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    if (!Island) return;
    //    if (!Chunk) return;
    //    if (!Label) return;

    //    UpdateLevelRequirement();
    //}
}
