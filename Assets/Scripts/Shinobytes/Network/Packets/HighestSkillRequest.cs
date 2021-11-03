public class HighestSkillRequest
{
    public HighestSkillRequest(TwitchPlayerInfo player, string skill)
    {
        Player = player;
        Skill = skill;
    }

    public TwitchPlayerInfo Player { get; }
    public string Skill { get; }
}
