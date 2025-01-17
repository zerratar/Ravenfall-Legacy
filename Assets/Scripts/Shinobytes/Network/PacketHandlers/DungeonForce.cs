﻿using RavenNest.Models;

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
                client.SendReply(gm, "Dungeons has been disabled in this stream by the broadcaster.");
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
                client.SendReply(gm, "Someone just used a dungeon scroll.");
                return;
            }

            if (Game.Dungeons.Active)
            {
                client.SendReply(gm, "There is already an active dungeon.");
                return;
            }

            if (Game.Raid.IsBusy)
            {
                client.SendReply(gm, "Someone tried to use a raid scroll. Please wait before using a dungeon scroll.");
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
                    client.SendReply(gm, "Dungeon cannot be started right now. Please try again later");
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
                            client.SendReply(gm, "You have used a Dungeon Scroll and joined the dungeon. Good luck!");
                            Game.Dungeons.Join(plr);
                        }
                        else
                        {
                            client.SendReply(gm, "You have used a Dungeon Scroll.");
                        }

                        Game.Dungeons.AnnounceDungeon(req);
                    }))
                    {
                        client.SendReply(gm, "Dungeon could not be started. Try again later");
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
                            client.SendReply(gm, "You do not have any Dungeon Scrolls! Redeem them under streamer loyalty on the website.");
                            return;
                        case ScrollUseResult.Error:
                            client.SendReply(gm, "Server was not able to give back a valid response. Uh oh.. BUG!");
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
