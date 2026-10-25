using System;
using System.Linq;

/// <summary>
/// !decor: viewers give the village decor with their loyalty points with the streamer (!decor gift snowman), and
/// see what a piece costs (!decor price snowman). A gift waits in build mode's Decor tab until the streamer places
/// it, for free. See docs/village-decor-design.md, phase 3.
/// </summary>
public class DecorCommand : ChatBotCommandHandler<string>
{
    public DecorCommand(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(string arguments, GameMessage gm, GameClient client)
    {
        var text = (arguments ?? "").Trim();
        var space = text.IndexOf(' ');
        var sub = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var query = space < 0 ? "" : text.Substring(space + 1).Trim();
        var decor = Game.Village ? Game.Village.Decor : null;

        try
        {
            if (decor == null || !decor.Loaded || query.Length == 0 || (sub != "gift" && sub != "give" && sub != "price"))
            {
                client.SendReply(gm, Localization.MSG_DECOR_USAGE);
                return;
            }

            var piece = decor.FindPiece(query, out var candidates);
            if (piece == null)
            {
                if (candidates.Count > 1) client.SendReply(gm, Localization.MSG_DECOR_AMBIGUOUS, string.Join(", ", candidates.Take(6).Select(p => decor.NameOf(p.Key))));
                else client.SendReply(gm, Localization.MSG_DECOR_NOT_FOUND, query);
                return;
            }

            var name = decor.NameOf(piece.Key);
            if (sub == "price")
            {
                client.SendReply(gm, Localization.MSG_DECOR_PRICE, name, piece.Points);
                return;
            }

            if (!piece.Unlocked)
            {
                client.SendReply(gm, Localization.MSG_DECOR_LOCKED, name);
                return;
            }

            var sender = gm.Sender;
            if (sender == null || string.IsNullOrEmpty(sender.PlatformId))
            {
                client.SendReply(gm, Localization.MSG_DECOR_NO_USER);
                return;
            }

            var giver = !string.IsNullOrEmpty(sender.DisplayName) ? sender.DisplayName : sender.Username;
            var result = await decor.GiftAsync(piece.Key, sender.Platform, sender.PlatformId, giver);
            if (result == null)
            {
                client.SendReply(gm, Localization.MSG_DECOR_FAILED);
                return;
            }

            switch (result.Ok ? "ok" : result.Error)
            {
                case "ok":
                    client.SendReply(gm, Localization.MSG_DECOR_GIFTED, name, result.Points, result.PointsLeft);
                    if (VillageBuildMode.Instance) VillageBuildMode.Instance.Announce(string.Format(Localization.UI_DECOR_GIFT_TOAST, giver, name));
                    break;
                case "points":
                    client.SendReply(gm, Localization.MSG_DECOR_POINTS, name, result.Points, result.PointsLeft);
                    break;
                case "locked":
                    client.SendReply(gm, Localization.MSG_DECOR_LOCKED, name);
                    break;
                case "nouser":
                    client.SendReply(gm, Localization.MSG_DECOR_NO_USER);
                    break;
                default:
                    client.SendReply(gm, Localization.MSG_DECOR_FAILED);
                    break;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!decor failed: " + exc);
            client.SendReply(gm, Localization.MSG_DECOR_FAILED);
        }
    }
}
