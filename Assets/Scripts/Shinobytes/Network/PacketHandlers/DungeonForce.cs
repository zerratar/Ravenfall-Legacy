using RavenNest.Models;

public class DungeonForce : ChatBotCommandHandler
{
    public DungeonForce(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        try
        {
            var plr = PlayerManager.GetPlayer(gm.Sender);
            if (!plr)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }


            if (PlayerSettings.Instance.DisableDungeons.GetValueOrDefault())
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_DISABLED);
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_BUSY);
                return;
            }

            if (Game.Dungeons.Active)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_ALREADY_ACTIVE);
                return;
            }

            if (Game.Raid.IsBusy)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_RAID_BUSY);
                return;
            }

            if (Game.Raid.IsEventActive)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_START_FAILED_RAID);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, Localization.MSG_DUNGEON_START_FAILED_WAR);
                return;
            }

            if (Game.Dungeons && !Game.Dungeons.Started)
            {
                if (Game.Events.IsActive)
                {
                    client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_TRY_LATER);
                    return;
                }

                //if (Game.Events.IsEventCooldownActive)
                //{
                //    client.SendReply(gm, "Dungeon cannot be started so quickly after eachother. Please wait {seconds} seconds.", UnityEngine.Mathf.RoundToInt(Game.Events.EventCooldownTimeLeft));
                //    return;
                //}

                Game.Dungeons.IsBusy = true;
                var result = await Game.RavenNest.Game.ActivateDungeonAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    await ExternalResources.ReloadIfModifiedAsync("dungeon.mp3");

                    if (!await Game.Dungeons.ActivateDungeon(plr, req =>
                    {
                        var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Dungeon);

                        if (Game.Dungeons.CanJoin(plr, string.Empty) == DungeonJoinResult.CanJoin)
                        {
                            client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_USED_JOINED);
                            Game.Dungeons.Join(plr);
                        }
                        else
                        {
                            client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_USED);
                        }

                        Game.Dungeons.AnnounceDungeon(req);
                    }))
                    {
                        client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_FAILED);
                        var dungeonScroll = Game.Items.Find(x => x.Name.ToLower().Contains("dungeon") && x.Category == ItemCategory.Scroll);
                        if (dungeonScroll != null)
                        {
                            await Game.RavenNest.Players.AddItemAsync(plr.Id, dungeonScroll.Id);
                        }
                    }
                }
                else
                {
                    switch (result)
                    {
                        case ScrollUseResult.InsufficientScrolls:
                            client.SendReply(gm, Localization.MSG_DUNGEON_FORCE_NO_SCROLLS);
                            return;
                        case ScrollUseResult.Error:
                            client.SendReply(gm, Localization.MSG_EVENT_SCROLL_SERVER_ERROR);
                            return;
                    }
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("DungeonForce.Handle: " + exc);
        }
        finally
        {
            Game.Dungeons.IsBusy = false;
        }
    }
}
