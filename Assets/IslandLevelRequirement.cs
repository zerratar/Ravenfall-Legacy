using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class IslandLevelRequirement : MonoBehaviour
{
    [SerializeField] private GameObject levelReqBlock;
    [SerializeField] private TMPro.TextMeshPro lblRequirements;

    private IslandController island;
    private List<IslandTask> levelRequirements;

    // Start is called before the first frame update
    void Start()
    {
        levelReqBlock.SetActive(false);
        this.island = GetComponentInParent<IslandController>();
        this.levelRequirements = GetLevelRequirements();

        var sb = new StringBuilder();

        foreach (var chunk in levelRequirements)
        {
            if (chunk.SkillLevelRequirement > 1)
            {
                sb.AppendLine("<b>"+chunk.Name + "</b> - Lv. " + chunk.SkillLevelRequirement);
            }
            else if (chunk.CombatLevelRequirement > 1)
            {
                sb.AppendLine("<b>"+chunk.Name + "</b> - Combat Lv. " + chunk.CombatLevelRequirement);
            }
            else
            {
                sb.AppendLine("<b>"+chunk.Name + "</b> - No Requirement");
            }
        }

        lblRequirements.text = sb.ToString();
    }

    public void ToggleLevelRequirements()
    {
        levelReqBlock.SetActive(!levelReqBlock.activeSelf);
    }
    public List<IslandTask> GetLevelRequirements()
    {
        var game = FindObjectOfType<GameManager>();
        game.Chunks.Init();
        var skills = new List<IslandTask>();
        var chunks = island.GetComponentsInChildren<Chunk>();
        foreach(var chunk in chunks)
        {
            skills.Add(new IslandTask
            {
                SkillLevelRequirement = chunk.RequiredSkilllevel,
                CombatLevelRequirement = chunk.RequiredCombatLevel,
                Name = chunk.ChunkType.ToString(),
            });
        }
        return skills;
    }
}
