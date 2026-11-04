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

        // only the streamer, moderators and game staff aim it at others; anyone else unstucks themselves whatever they add
        var mayTarget = user.IsBroadcaster || user.IsModerator || (player && (player.IsGameAdmin || player.IsGameModerator));
        if (!mayTarget)
        {
            args = null;
        }

        if (!string.IsNullOrEmpty(args))
        {
            args = args.Trim();

            if (args.ToLower() == "all" || args.ToLower() == "everyone")
            {
                foreach (var p in PlayerManager.GetAllPlayers())
                {
                    p.Unstuck(true, 5);
                }

                client.SendReply(gm, Localization.MSG_UNSTUCK_ALL);
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
                    client.SendReply(gm, Localization.MSG_UNSTUCK_TRAINING_SET, count);
                    return;
                }

                client.SendReply(gm, Localization.MSG_UNSTUCK_TRAINING_NONE, count);
                return;
            }

            // a player's name: bots before 0.9.7 sent that player as the sender, so the moderator's !unstuck <player>
            // checked the player's rights and quietly did nothing
            var target = PlayerManager.GetPlayerByName(args.TrimStart('@'));
            if (target)
            {
                target.Unstuck(true, 5);
                client.SendReply(gm, Localization.MSG_UNSTUCK_PLAYER, target.PlayerName);
                return;
            }

            client.SendReply(gm, Localization.MSG_UNSTUCK_NOT_FOUND, args);
            return;
        }

        if (!player)
        {
            return;
        }

        var result = player.Unstuck();
        if (!result)
        {
            client.SendReply(gm, Localization.MSG_UNSTUCK_COOLDOWN);
        }
    }
}
