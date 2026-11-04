using RavenNest.SDK.Endpoints;
using RavenNest.Models;

/// <summary>
/// The stream started on the newest release soon after it came out (the server's UpdateHonourManager): the first
/// stream in the world on it, an Early Bird (within ten minutes), or both. Chat hears it, the update card in the bottom
/// left corner celebrates for a while (UpdateDownloadView), and any new title can be worn with !title.
/// </summary>
public class UpdateHonourEventHandler : GameEventHandler<UpdateHonour>
{
    public override void Handle(GameManager gameManager, UpdateHonour data)
    {
        if (data == null || (!data.First && !data.Early))
        {
            return;
        }

        if (data.First) gameManager.RavenBot.Announce(Localization.MSG_UPDATE_FIRST, data.Version, data.Minutes);
        else gameManager.RavenBot.Announce(Localization.MSG_UPDATE_EARLY, data.Version, data.Minutes);

        var titles = data.NewTitles ?? System.Array.Empty<string>();
        if (titles.Length > 0)
        {
            gameManager.RavenBot.Announce(Localization.MSG_UPDATE_NEW_TITLES, string.Join(", ", System.Linq.Enumerable.Select(titles, QuestChat.TitleText)));
        }

        BackgroundUpdate.Honoured(data);
    }
}
