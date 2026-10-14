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

        if (player.clanHandler == null || !player.clanHandler.InClan)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_CLAN_SKILL);
            return;
        }

        var result = await Game.RavenNest.Players.GetEnchantmentCooldownAsync(player.Id);
        var now = System.DateTime.UtcNow;
        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_COOLDOWN_UNAVAILABLE);
            return;
        }

        var cooldownExpires = result.Cooldown.GetValueOrDefault();

        if (cooldownExpires <= now)
        {
            client.SendReply(gm, Localization.MSG_ENCHANT_NO_COOLDOWN);
            return;
        }

        var clearCost = result.CoinsPerSeconds;
        var cooldown = cooldownExpires - now;
        var cooldownString = GetCooldownString(cooldown);
        var totalCost = (long)(clearCost * cooldown.TotalSeconds);
        client.SendReply(gm, Localization.MSG_ENCHANT_COOLDOWN_CLEAR_INFO,
            cooldownString, clearCost, totalCost);
    }

    private string GetCooldownString(System.TimeSpan cooldown)
    {
        if (cooldown.Hours > 0)
        {
            if (cooldown.Hours == 1)
                return Localization.MSG_ENCHANT_TIME_ONE_HOUR;
            return string.Format(Localization.MSG_ENCHANT_TIME_HOURS, (int)cooldown.TotalHours);
        }

        if (cooldown.Minutes > 0)
        {
            if (cooldown.Minutes == 1)
                return Localization.MSG_ENCHANT_TIME_ONE_MINUTE;
            return string.Format(Localization.MSG_ENCHANT_TIME_MINUTES, (int)cooldown.TotalMinutes);
        }

        if (cooldown.Seconds > 0)
        {
            if (cooldown.Seconds == 1)
                return Localization.MSG_ENCHANT_TIME_ONE_SECOND;
            return string.Format(Localization.MSG_ENCHANT_TIME_SECONDS, (int)cooldown.TotalSeconds);
        }

        return Localization.MSG_ENCHANT_TIME_MOMENT;
    }
}
