using System;

public class RaidAuto : ChatBotCommandHandler<string>
{
    public static DateTime lastCostUpdate = DateTime.UnixEpoch;
    public static int autoJoinCost = 3000;

    public RaidAuto(
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
                var res = await Game.RavenNest.Players.GetAutoJoinRaidCostAsync();
                if (autoJoinCost != 0)
                {
                    autoJoinCost = res;
                    lastCostUpdate = DateTime.UtcNow;
                }
            }

            var l = data.ToLower();
            var before = player.Raid.AutoJoinCounter;
            if (int.TryParse(data, out var count))
            {
                player.Raid.AutoJoinCounter = Math.Max(0, count);
                if (before != player.Raid.AutoJoinCounter)
                    client.SendReply(gm, "You will automatically join the next {autoJoinCounter} raids, until you use !raid join off or run out of coins. Each time your character automatically joins a raid it will cost {autoJoinCost} coins.", player.Raid.AutoJoinCounter, autoJoinCost);
            }
            else if (l == "count" || l == "status" || l == "stats" || l == "left" || l == "state")
            {
                if (player.Raid.AutoJoinCounter == int.MaxValue)
                {
                    client.SendReply(gm, "You will automatically join raids until you use !raid join off or run out of coins. Each time your character automatically joins a raid it will cost {autoJoinCost} coins.", autoJoinCost);
                }
                else if (player.Raid.AutoJoinCounter > 0)
                {
                    client.SendReply(gm, "You will automatically join the next {autoJoinCounter} raids, until you use !raid join off or run out of coins. Each time your character automatically joins a raid it will cost {autoJoinCost} coins.", player.Raid.AutoJoinCounter, autoJoinCost);
                }
                else
                {
                    client.SendReply(gm, "You have are not set to automatically join any raids.");
                }
            }
            else if (l == "on" || l == "auto")
            {
                player.Raid.AutoJoinCounter = int.MaxValue;
                //if (before != player.Raid.AutoJoinCounter)
                client.SendReply(gm, "You will automatically join raids until you use !raid join off or run out of coins. Each time your character automatically joins a raid it will cost {autoJoinCost} coins.", autoJoinCost);
            }
            else if (l == "off" || l == "cancel" || l == "stop")
            {
                player.Raid.AutoJoinCounter = 0;
                //if (before != player.Raid.AutoJoinCounter)
                client.SendReply(gm, "You will no longer automatically join raids", data);
            }
        }
        catch
        {
            client.SendReply(gm, "{query} is not a valid value for raid auto join settings.", data);
        }
    }
}
