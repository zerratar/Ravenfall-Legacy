using System;

public class AutoRaid : ChatBotCommandHandler<string>
{
    public AutoRaid(
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
            //if (player.PatreonTier <= 0)
            //{
            //    client.SendReply(gm, "You need to be a mithril patron or higher to use this command. https://www.patreon.com/ravenfall");
            //    return;
            //}

            var autoJoinCost = Game.SessionSettings.AutoJoinRaidCost;
            var l = data.ToLower();
            var before = player.raidHandler.AutoJoinCounter;
            if (int.TryParse(data, out var count))
            {
                player.raidHandler.AutoJoinCounter = Math.Max(0, count);
                if (before != player.raidHandler.AutoJoinCounter)
                    client.SendReply(gm, "You will automatically join the next {autoJoinCounter} raids, until you use !raid join off. It will cost {cost} each time you automatically join a raid.", player.raidHandler.AutoJoinCounter, autoJoinCost);
            }
            else if (l == "count" || l == "status" || l == "stats" || l == "left" || l == "state")
            {
                if (player.raidHandler.AutoJoinCounter == int.MaxValue)
                {
                    client.SendReply(gm, "You will automatically join raids until you use !raid join off.");
                }
                else if (player.raidHandler.AutoJoinCounter > 0)
                {
                    client.SendReply(gm, "You will automatically join the next {autoJoinCounter} raids, until you use !raid join off. It will cost {cost} each time you automatically join a raid.", player.raidHandler.AutoJoinCounter, autoJoinCost);
                }
                else
                {
                    client.SendReply(gm, "You have are not set to automatically join any raids.");
                }
            }
            else if (l == "on" || l == "auto")
            {
                player.raidHandler.AutoJoinCounter = int.MaxValue;
                //if (before != player.raidHandler.AutoJoinCounter)
                client.SendReply(gm, "You will automatically join raids until you use !raid join off. It will cost {cost} each time you automatically join a raid.", autoJoinCost);
            }
            else if (l == "off" || l == "cancel" || l == "stop")
            {
                player.raidHandler.AutoJoinCounter = 0;
                //if (before != player.raidHandler.AutoJoinCounter)
                client.SendReply(gm, "You will no longer automatically join raids", data);
            }
        }
        catch
        {
            client.SendReply(gm, "{query} is not a valid value for raid auto join settings.", data);
        }
    }
}
