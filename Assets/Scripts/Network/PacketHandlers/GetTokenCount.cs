using System.Linq;
public class GetTokenCount : PacketHandler<Player>
{
    public GetTokenCount(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {        //token_count
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var streamerTokenCount = GetPlayerTokenCount(player);

        var tokenName = $"{Game.RavenNest.TwitchDisplayName} Token";
        client.SendFormat(data.Username, Localization.MSG_TOKENS, (int)streamerTokenCount, tokenName);
    }

    private decimal GetPlayerTokenCount(PlayerController player)
    {
        var streamerTokens = player.Inventory
            .GetInventoryItemsOfCategory(RavenNest.Models.ItemCategory.StreamerToken)
            .Where(x => x.Tag == null || x.Tag == Game.RavenNest.TwitchUserId)
            .ToList();

        var streamerTokenCount = 0m;
        if (streamerTokens.Count > 0)
        {
            streamerTokenCount = streamerTokens.Sum(x => x.Amount);
        }

        return streamerTokenCount;
    }
}