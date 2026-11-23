using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The server found something odd for a character while gathering (an Old Boot, a Pet Rock; docs/easter-eggs.md). The
/// inventory feed brings the item; this shows "Found: Old Boot!" over the character and says it in chat, at most once
/// every few minutes for the whole stream so a busy stream is not flooded (the word over the head always shows).
/// </summary>
public class OddFindEventHandler : GameEventHandler<OddFind>
{
    /// <summary>The least time between two odd find lines in chat, for the whole stream.</summary>
    public const float ChatEverySeconds = 600f;

    private static float lastChat = -ChatEverySeconds;

    public override void Handle(GameManager gameManager, OddFind data)
    {
        if (data == null || string.IsNullOrEmpty(data.Item)) return;
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player) return;

        SillySurprises.Say(player.transform, string.Format(Localization.UI_FLOAT_ODD_FIND, data.Item), SillySurprises.FindColor, 2.2f, always: true);

        var line = OddThings.FoundLine(data.Item, player.PlayerName);
        if (string.IsNullOrEmpty(line) || Time.unscaledTime - lastChat < ChatEverySeconds) return;
        lastChat = Time.unscaledTime;
        gameManager.RavenBot?.Announce(Localization.MSG_ODD_FIND, line);
    }
}
