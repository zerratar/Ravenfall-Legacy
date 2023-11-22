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
        var sender = gm.Sender;
        var player = PlayerManager.GetPlayer(sender);

        if (!string.IsNullOrEmpty(args))
        {
            if (!sender.IsBroadcaster && !sender.IsModerator && !sender.IsGameModerator && !sender.IsGameAdministrator)
            {
                if (!player)
                {
                    return;
                }
            }

            if (args.ToLower() == "all" || args.ToLower() == "everyone")
            {

                foreach (var p in PlayerManager.GetAllPlayers())
                {
                    p.Unstuck();
                }

                client.SendReply(gm, "Unstucking all players.");
                return;
            }

            if (args.ToLower() == "training")
            {
                var count = 0;
                foreach (var p in PlayerManager.GetAllPlayers())
                {
                    if (string.IsNullOrEmpty(p.CurrentTaskName) && !p.dungeonHandler.InDungeon && !p.raidHandler.InRaid && !p.duelHandler.InDuel && !p.ferryHandler.OnFerry && !p.ferryHandler.Embarking && p.Stats.CombatLevel > 3)
                    {
                        count++;
                        p.SetTask(TaskType.Fighting, "all");
                    }
                }

                if (count > 0)
                {
                    client.SendReply(gm, "{playerCount} players set to train All.", count);
                    return;
                }

                client.SendReply(gm, "No players eligible for setting training to All.", count);
                return;
            }

        }

        var result = player.Unstuck();
        if (!result)
        {
            client.SendReply(gm, "Unstuck command can only be used once per minute/character.");
        }
    }
}
