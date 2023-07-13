using RavenNest.Models;
using System;

public class GameUpdatedEventHandler : GameEventHandler<GameUpdatedRequest>
{
    private bool newVersionAnnounced;

    public override void Handle(GameManager gameManager, GameUpdatedRequest data)
    {
        Version expectedVersion = GetExpectedVersion(data);
        Version appVersion = GameVersion.GetApplicationVersion();

        // we are the expected version, do nothing.
        if (appVersion >= expectedVersion)
        {
            newVersionAnnounced = false;
            return;
        }

        gameManager.OnUpdateAvailable(data.ExpectedVersion);

        if (data.UpdateRequired)
        {
            if (!newVersionAnnounced)
            {
                newVersionAnnounced = true;
                gameManager.RavenBot.Announce("Attention! An essential update to Ravenfall, v{version}, is available. This update contains critical changes. To avoid any game disruption, restart your game immediately to apply the update. Thank you!", data.ExpectedVersion);
            }

            gameManager.SaveStateAndLoadScene();
            return;
        }

        if (!newVersionAnnounced)
        {
            newVersionAnnounced = true;
            gameManager.RavenBot.Announce("Good news! Ravenfall v{version} is out now! For optimal gameplay and new features, restart your game to update. Enjoy!", data.ExpectedVersion);
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
