using System;

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
        if (gm == null || gm.Sender == null)
        {
            Shinobytes.Debug.LogError("ClearEnchantmentCooldown: GameMessage or Sender is null.");
            return;
        }

        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player || !player.clanHandler)
        {
            Shinobytes.Debug.LogError("ClearEnchantmentCooldown: player or clanHandler is null (" + gm.Sender.DisplayName + ")");
            return;
        }

        if (player.clanHandler == null || !player.clanHandler.InClan)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        try
        {
            var result = await Game.RavenNest.Players.ClearEnchantmentCooldownAsync(player.Id);
            if (result != null && result.Success)
            {
                var totalCost = result.TotalCost;
                client.SendReply(gm, "You have cleared your enchantment cooldown for a total of {totalCost} coins", totalCost);
                return;
            }
            else
            {
                if (result == null) Shinobytes.Debug.LogWarning("ClearEnchantmentCooldown(" + player.Id + "): server call returned null.");
                client.SendReply(gm, "Unable to clear the cooldown, either you don't have a cooldown active or you don't have enough coins.");
                return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("ClearEnchantmentCooldown: An error occurred while clearing enchantment cooldown: " + exc);
            client.SendReply(gm, "An error occurred while trying to clear your enchantment cooldown. Please try again later.");
        }
    }
}
