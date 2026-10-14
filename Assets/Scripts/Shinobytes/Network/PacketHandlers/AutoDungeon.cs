using System;
public class AutoDungeon : ChatBotCommandHandler<string>
{
    public AutoDungeon(
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
            var autoJoinCost = Game.SessionSettings.AutoJoinDungeonCost;
            var l = data.ToLower();
            var before = player.dungeonHandler.AutoJoinCounter;
            if (int.TryParse(data, out var count))
            {
                player.dungeonHandler.AutoJoinCounter = Math.Max(0, count);
                if (before != player.dungeonHandler.AutoJoinCounter)
                    client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_COUNT, player.dungeonHandler.AutoJoinCounter, autoJoinCost);
            }
            else if (l == "count" || l == "status" || l == "stats" || l == "left" || l == "state")
            {
                if (player.dungeonHandler.AutoJoinCounter == int.MaxValue)
                {
                    client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_STATUS_ON);
                }
                else if (player.dungeonHandler.AutoJoinCounter > 0)
                {
                    client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_COUNT, player.dungeonHandler.AutoJoinCounter, autoJoinCost);
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_STATUS_OFF);
                }
            }
            else if (string.IsNullOrEmpty(l) || l == "on")
            {
                player.dungeonHandler.AutoJoinCounter = int.MaxValue;
                //if (before != player.dungeonHandler.AutoJoinCounter)
                client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_ON, autoJoinCost);
            }
            else if (l == "off" || l == "cancel" || l == "stop")
            {
                player.dungeonHandler.AutoJoinCounter = 0;
                //if (before != player.dungeonHandler.AutoJoinCounter)
                client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_OFF);
            }
        }
        catch
        {
            client.SendReply(gm, Localization.MSG_DUNGEON_AUTOJOIN_INVALID, data);
        }
    }
}
