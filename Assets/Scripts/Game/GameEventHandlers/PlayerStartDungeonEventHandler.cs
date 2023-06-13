using RavenNest.Models;

public class PlayerStartDungeonEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override async void Handle(GameManager gameManager, RavenNest.Models.PlayerId data)
    {
        var player = gameManager.Players.GetPlayerById(data.Id);
        if (!player) return;

        try
        {
            if (gameManager.StreamRaid.IsWar||gameManager.Dungeons.IsBusy|| gameManager.Raid.IsBusy|| gameManager.Raid.Started)
            {
                return;
            }

            if (gameManager.Dungeons && !gameManager.Dungeons.Started)
            {
                if (gameManager.Events.IsActive)
                {
                    return;
                }

                gameManager.Dungeons.IsBusy = true;
                var result = await gameManager.RavenNest.Game.ActivateDungeonAsync(player);
                if (result == ScrollUseResult.Success)
                {
                    await ExternalResources.ReloadIfModifiedAsync("dungeon.mp3");

                    if (await gameManager.Dungeons.ActivateDungeon())
                    {
                        player.Inventory.RemoveScroll(ScrollType.Dungeon);
                    }
                    else
                    {
                        var dungeonScroll = gameManager.Items.Find(x => x.Name.ToLower().Contains("dungeon") && x.Category == ItemCategory.Scroll);
                        if (dungeonScroll != null)
                        {
                            await gameManager.RavenNest.Players.AddItemAsync(player.Id, dungeonScroll.Id);
                        }
                    }
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
        finally
        {
            gameManager.Dungeons.IsBusy = false;
        }
    }
}
