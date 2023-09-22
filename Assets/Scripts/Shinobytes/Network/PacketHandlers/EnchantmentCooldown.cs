public class EnchantmentCooldown : ChatBotCommandHandler
{
    public EnchantmentCooldown(
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


        if (player.Clan == null || !player.Clan.InClan)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        var result = await Game.RavenNest.Players.GetEnchantmentCooldownAsync(player.Id);
        var now = System.DateTime.UtcNow;
        var cooldownExpires = result.Cooldown.GetValueOrDefault();

        if (cooldownExpires <= now)
        {
            client.SendReply(gm, "You do not have any cooldown.");
            return;
        }

        var clearCost = result.CoinsPerSeconds;
        var cooldown = cooldownExpires - now;
        var cooldownString = GetCooldownString(cooldown);
        var totalCost = clearCost * cooldown.TotalSeconds;
        client.SendReply(gm, "You have to wait {timeLeft} before you can try to enchant something again. If you want to clear this cooldown, use !enchant clear cooldown, this will cost {pricePerSecond} coins per second. ~Total {totalCost} coins",
            cooldownString, clearCost, totalCost);
    }

    private string GetCooldownString(System.TimeSpan cooldown)
    {
        if (cooldown.Hours > 0)
        {
            if (cooldown.Hours == 1)
                return "1 hour";
            return (int)cooldown.TotalHours + " hours";
        }

        if (cooldown.Minutes > 0)
        {
            if (cooldown.Minutes == 1)
                return "1 minute";
            return (int)cooldown.TotalMinutes + " minutes";
        }

        if (cooldown.Seconds > 0)
        {
            if (cooldown.Seconds == 1)
                return "1 second";
            return (int)cooldown.TotalSeconds + " seconds";
        }

        return "a moment";
    }
}
