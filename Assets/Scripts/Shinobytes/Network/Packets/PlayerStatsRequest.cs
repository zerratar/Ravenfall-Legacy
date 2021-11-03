public class PlayerStatsRequest : IBotRequest
{
    public TwitchPlayerInfo Player { get; }
    public string Skill { get; }

    public PlayerStatsRequest(TwitchPlayerInfo player, string skill)
    {
        Player = player;
        Skill = skill;
    }
}
