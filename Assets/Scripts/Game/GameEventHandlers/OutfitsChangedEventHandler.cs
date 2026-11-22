using RavenNest.Models;

/// <summary>
/// A character's outfits per skill were changed on the server from somewhere else (the website's Equipment tab): the
/// game takes them as they are now, and the pet that went out with an outfit (docs/outfits-design.md). Gear that was
/// put on comes as inventory changes, like every other equipment change.
/// </summary>
public class OutfitsChangedEventHandler : GameEventHandler<OutfitsChanged>
{
    public override void Handle(GameManager gameManager, OutfitsChanged data)
    {
        if (data == null) return;
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (player) player.Outfits.Apply(new OutfitResult { Outfits = data.Outfits ?? string.Empty, Pets = data.Pets }, false);
    }
}
