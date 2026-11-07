using RavenNest.Models;

/// <summary>
/// A character's skill trees changed on the server (the website's Skill trees tab, or chat): the character follows the
/// build at once, passives and loadout (combat v2, docs/combat-skills-design.md).
/// </summary>
public class SkillBuildChangedEventHandler : GameEventHandler<SkillBuildInfo>
{
    public override void Handle(GameManager gameManager, SkillBuildInfo data)
    {
        if (data == null) return;
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (player) player.ApplySkillBuild(data.Build);
    }
}
