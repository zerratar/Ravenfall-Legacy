public class ClanSkillLevelChangedEventHandler : GameEventHandler<ClanSkillLevelChanged>
{
    public override void Handle(GameManager gameManager, ClanSkillLevelChanged data)
    {
        gameManager.Clans.UpdateClanSkill(data);
    }
}