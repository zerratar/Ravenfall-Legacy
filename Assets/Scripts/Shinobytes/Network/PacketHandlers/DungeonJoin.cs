using System;

public class DungeonJoin : PacketHandler<EventJoinRequest>
{
    public DungeonJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(EventJoinRequest data, GameClient client)
    {
        try
        {
            var username = data.Player.Username;
            var player = PlayerManager.GetPlayer(data.Player);
            if (player == null)
            {
                client.SendMessage(username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendMessage(username, "Dungeon is unavailable during wars. Please wait for it to be over.");
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendMessage(username, "You cannot join the dungeon while on the ferry. Please !disembark before you can join.");
                return;
            }


            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (!Game.Dungeons.Active)
            {
                client.SendMessage(username, "No active dungeons available, sorry.");
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendMessage(username, "The dungeon has already started. You will have to wait for the next one!");
                return;
            }
            var result = Game.Dungeons.CanJoin(player, data.Code);

            switch (result)
            {
                case DungeonJoinResult.CanJoin:
                    Game.Dungeons.Join(player);
                    client.SendMessage(username, "You have joined the dungeon. Good luck!");
                    break;
                case DungeonJoinResult.AlreadyJoined:
                    client.SendMessage(username, "You have already joined the dungeon.");
                    break;
                case DungeonJoinResult.WrongCode:
                    client.SendMessage(username, "You have used incorrect command for joining the dungeon. Use !dungeon [word seen on stream] to join.");
                    break;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }
}
