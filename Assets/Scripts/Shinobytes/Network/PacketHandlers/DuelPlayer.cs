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
            client.SendReply(gm, Localization.MSG_DUEL_DURING_WAR);
            return;
        }

        if (!player2)
        {
            client.SendReply(gm, Localization.MSG_DUEL_PLAYER_NOT_FOUND, target.Username);
            return;
        }

        if (player1.ferryHandler.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_DUEL_FAILED_FERRY);
            return;
        }

        if (player2.ferryHandler.OnFerry)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_FERRY, player2.PlayerName);
            return;
        }

        if (player1.Island != player2.Island)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_OTHER_ISLAND, player2.PlayerName);
            return;
        }

        if (player1 == player2)
        {
            client.SendReply(gm, Localization.MSG_DUEL_SELF);
            return;
        }

        if (player1.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_DUEL_FAILED_IN_DUEL);
            return;
        }

        if (player1.arenaHandler.InArena)
        {
            client.SendReply(gm, Localization.MSG_DUEL_FAILED_IN_ARENA);
            return;
        }

        if (player1.raidHandler.InRaid)
        {
            client.SendReply(gm, Localization.MSG_DUEL_FAILED_IN_RAID);
            return;
        }

        if (player1.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, Localization.MSG_DUEL_FAILED_IN_DUNGEON);
            return;
        }

        if (player2.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_IN_DUEL, player2.PlayerName);
            return;
        }

        if (player2.arenaHandler.InArena)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_IN_ARENA, player2.PlayerName);
            return;
        }

        if (player2.raidHandler.InRaid)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_IN_RAID, player2.PlayerName);
            return;
        }

        if (player2.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_IN_DUNGEON, player2.PlayerName);
            return;
        }

        if (player2.duelHandler.HasActiveRequest)
        {
            client.SendReply(gm, Localization.MSG_DUEL_TARGET_HAS_REQUEST, player2.PlayerName);
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