using RavenNest.Models;

public class PlayerStartRaidEventHandler : GameEventHandler<RavenNest.Models.PlayerId>
{
    public override async void Handle(GameManager Game, RavenNest.Models.PlayerId data)
    {
        var player = Game.Players.GetPlayerById(data.Id);
        if (!player) return;

        try
        {
            if (Game.StreamRaid.IsWar || Game.Dungeons.IsBusy || Game.Raid.IsBusy || Game.Raid.Started)
            {
                return;
            }

            if (Game.Raid && !Game.Raid.Started && !Game.Raid.Boss)
            {
                if (Game.Events.IsActive)
                {
                    return;
                }

                Game.Raid.IsBusy = true;
                var result = await Game.RavenNest.Game.ActivateRaidAsync(player);
                if (result == ScrollUseResult.Success)
                {
                    var scrollsLeft = player.Inventory.RemoveScroll(ScrollType.Raid);
                    if (!await Game.Raid.StartRaid(player.Name))
                    {
                        var raidScroll = Game.Items.Find(x => x.Name.Contains("raid", System.StringComparison.OrdinalIgnoreCase) && x.Type == ItemType.Scroll);
                        if (raidScroll != null)
                        {
                            await Game.RavenNest.Players.AddItemAsync(player.Id, raidScroll.Id);
                            player.Inventory.AddToBackpack(raidScroll);
                        }
                    }
                }
            }
        }
        catch { }
        finally
        {
            Game.Raid.IsBusy = false;
        }
    }
}