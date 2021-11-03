using System;

public class ArenaJoin : PacketHandler<TwitchPlayerInfo>
{
    public ArenaJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(data);
            if (player == null)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendMessage(data.Username, Localization.MSG_ARENA_UNAVAILABLE_WAR);
                return;
            }

            if (Game.Arena.Island != player.Island)
            {
                client.SendMessage(data.Username, Localization.MSG_ARENA_WRONG_ISLAND);
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendMessage(data.Username, Localization.MSG_ARENA_FERRY);
                return;
            }

            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (!Game.Arena.CanJoin(player, out var alreadyJoined, out var alreadyStarted)
                || Game.Raid.Started
                || Game.Dungeons.Active
                || Game.Dungeons.Started
                || player.Raid.InRaid
                || player.Duel.InDuel
                || player.Dungeon.InDungeon)
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
                else if (player.Dungeon.InDungeon)
                {
                    errorMessage = Localization.MSG_IN_DUNGEON;
                }
                else if (player.Duel.InDuel)
                {
                    errorMessage = Localization.MSG_IN_DUEL;
                }
                else if (player.Raid.InRaid)
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
             
                client.SendMessage(data.Username, errorMessage);
                return;
            }

            Game.Arena.Join(player);

            client.SendMessage(player.PlayerName, Localization.MSG_ARENA_JOIN);
        }
        catch (Exception exc)
        {
            GameManager.LogError(exc.ToString());
        }
    }
}