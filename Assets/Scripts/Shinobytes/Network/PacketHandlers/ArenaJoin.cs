using System;

public class ArenaJoin : ChatBotCommandHandler<Empty>
{
    public ArenaJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Empty _, GameMessage gm, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, Localization.MSG_ARENA_UNAVAILABLE_WAR);
                return;
            }

            if (Game.Arena.Island != player.Island)
            {
                client.SendReply(gm, Localization.MSG_ARENA_WRONG_ISLAND);
                return;
            }

            if (player.ferryHandler.OnFerry)
            {
                client.SendReply(gm, Localization.MSG_ARENA_FERRY);
                return;
            }

            if (player.ferryHandler.Active)
            {
                player.ferryHandler.BeginDisembark();
            }

            if (!Game.Arena.CanJoin(player, out var alreadyJoined, out var alreadyStarted)
                || Game.Raid.Started
                || Game.Dungeons.Active
                || Game.Dungeons.Started
                || player.raidHandler.InRaid
                || player.duelHandler.InDuel
                || player.dungeonHandler.InDungeon)
            {
                var errorMessage = "";
                if (Game.Raid.Started)
                {
                    errorMessage = Localization.MSG_RAID_STARTED;
                }
                else if (Game.Dungeons.Active || Game.Dungeons.Started)
                {
                    errorMessage = Localization.MSG_DUNGEON_STARTED;
                }
                else if (player.dungeonHandler.InDungeon)
                {
                    errorMessage = Localization.MSG_IN_DUNGEON;
                }
                else if (player.duelHandler.InDuel)
                {
                    errorMessage = Localization.MSG_IN_DUEL;
                }
                else if (player.raidHandler.InRaid)
                {
                    errorMessage = Localization.MSG_IN_RAID;
                }
                else if (alreadyJoined)
                {
                    errorMessage = Localization.MSG_ARENA_ALREADY_JOINED;
                }
                else if (alreadyStarted)
                {
                    errorMessage = Localization.MSG_ARENA_ALREADY_STARTED;
                }
                else
                {
                    errorMessage = Localization.MSG_ARENA_JOIN_ERROR;
                }

                client.SendReply(gm, errorMessage);
                return;
            }

            Game.Arena.Join(player);

            client.SendReply(gm, Localization.MSG_ARENA_JOIN);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("ArenaJoin.Handle: " + exc);
        }
    }
}