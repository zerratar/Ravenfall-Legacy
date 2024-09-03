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
        var user = gm.Sender;
        var player = PlayerManager.GetPlayer(user);

        if (!string.IsNullOrEmpty(args))
        {
            if (!user.IsBroadcaster && !user.IsModerator)
            {
                if (!player)
                {
                    return;
                }

                if (!player.IsGameAdmin && !player.IsGameModerator)
                {
                    return;
                }
            }

            if (args.ToLower() == "all" || args.ToLower() == "everyone")
            {
                foreach (var p in PlayerManager.GetAllPlayers())
                {
                    p.Unstuck(true, 5);
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

        if (!player)
        {
            return;
        }

        var result = player.Unstuck();
        if (!result)
        {
            client.SendReply(gm, "Unstuck command can only be used once every 30s per character.");
        }
    }
}
