using System;

public class DungeonAuto : ChatBotCommandHandler<string>
{
    private DateTime lastCostUpdate = DateTime.MinValue;
    private int autoJoinCost = 5000;

    public DungeonAuto(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }
        try
        {
            if (DateTime.UtcNow - lastCostUpdate > TimeSpan.FromMinutes(5))
            {
                autoJoinCost = await Game.RavenNest.Players.GetAutoJoinDungeonCostAsync();
                lastCostUpdate = DateTime.UtcNow;
            }

            var before = player.Dungeon.AutoJoinCounter;
            if (int.TryParse(data, out var count))
            {
                player.Dungeon.AutoJoinCounter = Math.Max(0, count);
                if (before != player.Dungeon.AutoJoinCounter)
                    client.SendReply(gm, "You will automatically join the next " + player.Dungeon.AutoJoinCounter + " dungeons, until you use !dungeon join off or run out of coins. Each time your character automatically joins a dungeon it will cost " + autoJoinCost + " coins.", data);
            }
            else if (data.ToLower() == "on")
            {
                player.Dungeon.AutoJoinCounter = int.MaxValue;
                if (before != player.Dungeon.AutoJoinCounter)
                    client.SendReply(gm, "You will automatically join dungeons until you use !dungeon join off or run out of coins. Each time your character automatically joins a dungeon it will cost " + autoJoinCost + " coins.", data);
            }
            else
            {
                player.Dungeon.AutoJoinCounter = 0;
                if (before != player.Dungeon.AutoJoinCounter)
                    client.SendReply(gm, "You will no longer automatically join dungeons", data);
            }
        }
        catch
        {
            client.SendReply(gm, "{query} is not a valid value for dungeon auto join settings", data);
        }
    }
}
