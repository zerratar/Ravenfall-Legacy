public class DuelPlayer : ChatBotCommandHandler<User>
{
    public DuelPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User target, GameMessage gm, GameClient client)
    {
        var player1 = PlayerManager.GetPlayer(gm.Sender);
        var player2 = PlayerManager.GetPlayer(target);

        if (Game.StreamRaid.IsWar)
        {
            client.SendReply(gm, "Unable to duel someone during a war. Please wait for it to be over.");
            return;
        }

        if (!player2)
        {
            client.SendReply(gm, "No player with the name {targetPlayerName} is currently playing.", target.Username);
            return;
        }

        if (player1.ferryHandler.OnFerry)
        {
            client.SendReply(gm, "You cannot duel another player while on the ferry.");
            return;
        }

        if (player2.ferryHandler.OnFerry)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as they are on the ferry.", player2.PlayerName);
            return;
        }

        if (player1.Island != player2.Island)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as they are not on the same island.", player2.PlayerName);
            return;
        }

        if (player1 == player2)
        {
            client.SendReply(gm, "You cannot duel yourself.");
            return;
        }

        if (player1.duelHandler.InDuel)
        {
            client.SendReply(gm, "You cannot duel another player as you are already in a duel.");
            return;
        }

        if (player1.arenaHandler.InArena)
        {
            client.SendReply(gm, "You cannot duel another player as you are participating in the Arena.");
            return;
        }

        if (player1.raidHandler.InRaid)
        {
            client.SendReply(gm, "You cannot duel another player as you are participating in a Raid.");
            return;
        }

        if (player1.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, "You cannot duel another player as you are participating in a Dungeon.");
            return;
        }

        if (player2.duelHandler.InDuel)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as he or she is participating in a duel.", player2.PlayerName);
            return;
        }

        if (player2.arenaHandler.InArena)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as he or she is participating in the Arena.", player2.PlayerName);
            return;
        }

        if (player2.raidHandler.InRaid)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as he or she is participating in a Raid.", player2.PlayerName);
            return;
        }

        if (player2.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as he or she is participating in a Dungeon.", player2.PlayerName);
            return;
        }

        if (player2.duelHandler.HasActiveRequest)
        {
            client.SendReply(gm, "You cannot duel {targetPlayerName} as he or she already have an active duel request.", player2.PlayerName);
            return;
        }

        if (player1.duelHandler.HasActiveRequest)
        {
            if (player1.duelHandler.Requester == player2)
            {
                player1.duelHandler.AcceptDuel();
                return;
            }

            player1.duelHandler.DeclineDuel();
        }

        player1.duelHandler.RequestDuel(player2);
    }
}