public class TownStats : ChatBotCommandHandler
{
    public TownStats(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var townHall = Game.Village.TownHall;
        var experience = Game.Village.TownHall.Experience;
        var level = Game.Village.TownHall.Level;
        var nextLevel = level + 1;
        var nextLevelExperience = GameMath.ExperienceForLevel(nextLevel);
        var remainingExp = nextLevelExperience - experience;
        client.SendReply(gm, "Village is level {townHallLevel}, it needs {remainingExp} xp to level up.", level, remainingExp);
    }
}
