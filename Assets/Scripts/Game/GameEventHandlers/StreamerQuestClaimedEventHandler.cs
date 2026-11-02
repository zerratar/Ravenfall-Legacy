using RavenNest.SDK.Endpoints;
using RavenNest.Models;

/// <summary>
/// A viewer finished one of the streamer's bounties and got its reward (the server paid it from what was set aside;
/// the coins come with a ResourceUpdate, the items through the inventory feed). Chat hears who, and how many are left.
/// </summary>
public class StreamerQuestClaimedEventHandler : GameEventHandler<StreamerQuestClaimed>
{
    public override void Handle(GameManager gameManager, StreamerQuestClaimed data)
    {
        var quest = data?.Quest;
        if (quest == null)
        {
            return;
        }

        StreamerQuests.RefreshSoon();
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        var name = player ? player.PlayerName : "?";
        gameManager.RavenBot.Announce(Localization.MSG_STREAMER_BOUNTY_CLAIMED, name, StreamerQuests.Title(quest),
            StreamerQuests.Reward(data.Coins, quest.RewardItemName, data.Items, data.Loyalty), quest.Winners - quest.Claimed, quest.Winners);
    }
}
