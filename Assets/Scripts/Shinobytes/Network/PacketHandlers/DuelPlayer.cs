public class DuelPlayer : PacketHandler<DuelPlayerRequest>
{
    public DuelPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(DuelPlayerRequest data, GameClient client)
    {
        var player1 = PlayerManager.GetPlayer(data.playerA);
        var player2 = PlayerManager.GetPlayer(data.playerB);

        if (Game.StreamRaid.IsWar)
        {
            client.SendMessage(data.playerA?.Username, "Unable to duel someone during a war. Please wait for it to be over.");
            return;
        }

        if (!player2)
        {
            client.SendMessage(data.playerA?.Username, "No player with the name {targetPlayerName} is currently playing.", data.playerB?.Username);
            return;
        }

        if (player1.Ferry.OnFerry)
        {
            client.SendMessage(data.playerA?.Username, "You cannot duel another player while on the ferry.");
            return;
        }

        if (player2.Ferry.OnFerry)
        {
            client.SendMessage(data.playerA?.Username, "You cannot duel {targetPlayerName} as they are on the ferry.", player2.PlayerName);
            return;
        }

        if (player1.Island != player2.Island)
        {
            client.SendMessage(data.playerA?.Username, "You cannot duel {targetPlayerName} as they are not on the same island.", player2.PlayerName);
            return;
        }

        if (player1 == player2)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel yourself.");
            return;
        }

        if (player1.Duel.InDuel)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel another player as you are already in a duel.");
            return;
        }

        if (player1.Arena.InArena)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel another player as you are participating in the Arena.");
            return;
        }

        if (player1.Raid.InRaid)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel another player as you are participating in a Raid.");
            return;
        }

        if (player1.Dungeon.InDungeon)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel another player as you are participating in a Dungeon.");
            return;
        }

        if (player2.Duel.InDuel)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel {targetPlayerName} as he or she is participating in a duel.", player2.PlayerName);
            return;
        }

        if (player2.Arena.InArena)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel {targetPlayerName} as he or she is participating in the Arena.", player2.PlayerName);
            return;
        }

        if (player2.Raid.InRaid)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel {targetPlayerName} as he or she is participating in a Raid.", player2.PlayerName);
            return;
        }

        if (player2.Dungeon.InDungeon)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel {targetPlayerName} as he or she is participating in a Dungeon.", player2.PlayerName);
            return;
        }

        if (player2.Duel.HasActiveRequest)
        {
            client.SendMessage(data.playerA.Username, "You cannot duel {targetPlayerName} as he or she already have an active duel request.", player2.PlayerName);
            return;
        }

        if (player1.Duel.HasActiveRequest)
        {
            if (player1.Duel.Requester == player2)
            {
                player1.Duel.AcceptDuel();
                return;
            }

            player1.Duel.DeclineDuel();
        }

        player1.Duel.RequestDuel(player2);
    }
}