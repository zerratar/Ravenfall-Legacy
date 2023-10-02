using System;
using System.Collections.Generic;

public class GetStatusEffects : ChatBotCommandHandler<string>
{
    public GetStatusEffects(GameManager game, RavenBotConnection server, PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var statusEffects = player.GetStatusEffects();
        if (statusEffects.Count == 0)
        {
            client.SendReply(gm, "You do not have any active status effects.");
            return;
        }

        client.SendReply(gm, FormatStatusEffects(statusEffects));
    }

    private string FormatStatusEffects(IReadOnlyList<StatusEffect> effects)
    {
        var now = DateTime.UtcNow;
        var strings = new List<string>();
        foreach (var fx in effects)
        {
            if (fx.Expired)
            {
                continue;
            }

            var timeLeft = TimeSpan.FromSeconds(fx.TimeLeft); //now - fx.ExpiresUtc;
            var proc = UnityEngine.Mathf.FloorToInt(fx.Amount * 100);
            strings.Add($"{Utility.AddSpacesToSentence(fx.Type.ToString())} +{proc}% ({timeLeft:hh\\:mm\\:ss})");
        }

        return string.Join(", ", strings);
    }
}
