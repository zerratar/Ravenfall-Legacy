public class PlayerUnstuck : ChatBotCommandHandler<string>
{
    public PlayerUnstuck(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string args, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            return;
        }

        if (!string.IsNullOrEmpty(args) && args.ToLower() == "all" || args.ToLower() == "everyone")
        {
            foreach (var p in PlayerManager.GetAllPlayers())
            {
                p.Unstuck();
            }

            client.SendReply(gm, "Unstucking all players.");
            return;
        }

        var result = player.Unstuck();
        if (!result)
        {
            client.SendReply(gm, "Unstuck command can only be used once per minute/character.");
        }
    }
}
