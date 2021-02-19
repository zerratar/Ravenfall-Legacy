public class ClanSkillLevelChangedEventHandler : GameEventHandler<ClanSkillLevelChanged>
{
    protected override void Handle(GameManager gameManager, ClanSkillLevelChanged data)
    {
        gameManager.Clans.UpdateClanSkill(data);
    }
}