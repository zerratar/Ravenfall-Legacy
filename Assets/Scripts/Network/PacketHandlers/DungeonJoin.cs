using System;

public class DungeonJoin : PacketHandler<Player>
{
    public DungeonJoin(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(data);
            if (player == null)
            {
                client.SendMessage(data.Username,
                    "You are not currently playing, use the !join to start playing.");
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendMessage(data.Username,
                    "Dungeon is unavailable during wars. Please wait for it to be over.");
                return;
            }

            if (player.Ferry.OnFerry)
            {
                client.SendMessage(data.Username,
                    "You cannot join the dungeon while on the ferry. Please !disembark before you can join.");
                return;
            }


            if (player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (!Game.Dungeons.Active)
            {
                client.SendMessage(data.Username, "No active dungeons available, sorry.");
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendMessage(data.Username, "The dungeon has already started. You will have to wait for the next one!");
                return;
            }

            if (Game.Dungeons.CanJoin(player))
            {
                Game.Dungeons.Join(player);
                client.SendMessage(data.Username, "You have joined the dungeon. Good luck!");
            }
            else
            {
                client.SendMessage(data.Username, "You have already joined the dungeon.");
            }

        }
        catch (Exception exc)
        {
            Game.LogError(exc.ToString());
        }
    }
}
