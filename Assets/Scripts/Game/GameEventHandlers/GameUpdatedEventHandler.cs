using RavenNest.Models;
using System;
using System.Collections.Generic;

public class GameUpdatedEventHandler : GameEventHandler<GameUpdatedRequest>
{
    private HashSet<string> versionAnnounced = new HashSet<string>();
    private DateTime lastAnnounced = DateTime.UnixEpoch;
    public override void Handle(GameManager gameManager, GameUpdatedRequest data)
    {
        Version expectedVersion = GetExpectedVersion(data);
        Version appVersion = GameVersion.GetApplicationVersion();

        // we are the expected version, do nothing.
        if (appVersion >= expectedVersion)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (lastAnnounced > DateTime.UnixEpoch && now - lastAnnounced > TimeSpan.FromHours(3))
        {
            versionAnnounced.Clear();
        }

        gameManager.OnUpdateAvailable(data.ExpectedVersion);

        if (data.UpdateRequired)
        {
            if (versionAnnounced.Add(data.ExpectedVersion))
            {
                gameManager.RavenBot.Announce("Attention! An essential update to Ravenfall, v{version}, is available. This update contains critical changes. To avoid any game disruption, game will attempt to automatically restart to apply the update.", data.ExpectedVersion);
                lastAnnounced = DateTime.UtcNow;
            }

            gameManager.SaveStateAndLoadScene(0);
            return;
        }

        if (versionAnnounced.Add(data.ExpectedVersion))
        {
            gameManager.RavenBot.Announce("Good news! Ravenfall v{version} is out now! For optimal gameplay and new features, restart your game to update. Enjoy!", data.ExpectedVersion);
            lastAnnounced = DateTime.UtcNow;
        }
    }

    private Version GetExpectedVersion(GameUpdatedRequest data)
    {
        if (string.IsNullOrEmpty(data.ExpectedVersion))
        {
            return new Version();
        }

        GameVersion.TryParse(data.ExpectedVersion, out var newVersion);

        return newVersion ?? new Version();
    }
}
