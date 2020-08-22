public class SetExpMultiplier : PacketHandler<SetExpMultiplierRequest>
{
    public SetExpMultiplier(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(SetExpMultiplierRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (player == null || !player || !player.IsGameAdmin)
        {
            return;
        }

        Game.Twitch.SetExpMultiplier(player.Name, data.ExpMultiplier);
    }
}

public class DuelPlayer : PacketHandler<DuelPlayerRequest>
{
    public DuelPlayer(
        GameManager game,
        GameServer server,
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
            client.SendCommand(data.playerA?.Username, "duel_failed",
                "Unable to duel someone during a war. Please wait for it to be over.");
            return;
        }

        if (!player2)
        {
            client.SendCommand(data.playerA?.Username,
                "duel_failed", $"No player with the name {data.playerB?.Username} is currently playing.");
            return;
        }

        if (player1.Ferry.OnFerry)
        {
            client.SendCommand(data.playerA?.Username, "duel_failed", $"You cannot duel another player while on the ferry.");
            return;
        }

        if (player2.Ferry.OnFerry)
        {
            client.SendCommand(data.playerA?.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as they are on the ferry.");
            return;
        }

        if (player1.Island != player2.Island)
        {
            client.SendCommand(data.playerA?.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as they not on the same island.");
            return;
        }

        if (player1 == player2)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", "You cannot duel yourself.");
            return;
        }

        if (player1.Duel.InDuel)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", "You cannot duel another player as you are already in a duel.");
            return;
        }

        if (player1.Arena.InArena)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", "You cannot duel another player as you are participating in the Arena.");
            return;
        }

        if (player1.Raid.InRaid)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", "You cannot duel another player as you are participating in a Raid.");
            return;
        }

        if (player1.Dungeon.InDungeon)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", "You cannot duel another player as you are participating in a Dungeon.");
            return;
        }

        if (player2.Duel.InDuel)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as he or she is participating in a duel.");
            return;
        }

        if (player2.Arena.InArena)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as he or she is participating in the Arena.");
            return;
        }

        if (player2.Raid.InRaid)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as he or she is participating in a Raid.");
            return;
        }

        if (player2.Dungeon.InDungeon)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as he or she is participating in a Dungeon.");
            return;
        }

        if (player2.Duel.HasActiveRequest)
        {
            client.SendCommand(data.playerA.Username, "duel_failed", $"You cannot duel {player2.PlayerName} as he or she already have an active duel request.");
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