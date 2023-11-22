public class ClearEnchantmentCooldown : ChatBotCommandHandler
{
    public ClearEnchantmentCooldown(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            return;
        }

        if (player.clanHandler == null || !player.clanHandler.InClan)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        var result = await Game.RavenNest.Players.ClearEnchantmentCooldownAsync(player.Id);
        if (result.Success)
        {
            var totalCost = result.TotalCost;
            client.SendReply(gm, "You have cleared your enchantment cooldown for a total of {totalCost} coins", totalCost);
            return;
        }
        else
        {
            client.SendReply(gm, "Unable to clear the cooldown, either you don't have a cooldown active or you don't have enough coins.");
            return;
        }
    }
}
