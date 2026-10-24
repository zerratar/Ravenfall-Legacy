using RavenNest.SDK.Endpoints;

/// <summary>A clan's logo, cape image or cape shapes changed on the website: its players are dressed again.</summary>
public class ClanLookChangedEventHandler : GameEventHandler<ClanLook>
{
    public override void Handle(GameManager gameManager, ClanLook data)
    {
        gameManager.ClanLooks.Apply(data);
    }
}
