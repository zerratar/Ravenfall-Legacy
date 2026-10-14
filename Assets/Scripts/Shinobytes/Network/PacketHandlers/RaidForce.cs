using RavenNest.Models;
using System.Threading;

public class RaidForce : ChatBotCommandHandler
{
    private int scrollActive;
    public RaidForce(
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

            if (PlayerSettings.Instance.DisableRaids.GetValueOrDefault())
            {
                client.SendReply(gm, Localization.MSG_RAID_FORCE_DISABLED);
                return;
            }

            if (Game.Raid.IsBusy)
            {
                client.SendReply(gm, Localization.MSG_RAID_FORCE_BUSY);
                return;
            }

            if (Game.Raid.IsEventActive)
            {
                client.SendReply(gm, Localization.MSG_RAID_FORCE_ALREADY_ACTIVE);
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
                client.SendReply(gm, Localization.MSG_RAID_FORCE_DUNGEON_BUSY);
                return;
            }

            if (Game.Dungeons.Active)
            {
                client.SendReply(gm, Localization.MSG_RAID_START_FAILED_DUNGEON);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, Localization.MSG_RAID_START_FAILED_WAR);
                return;
            }

            if (Game.Raid && !Game.Raid.Started && !Game.Raid.Boss)
            {
                if (Game.Events.IsActive)
                {
                    client.SendReply(gm, Localization.MSG_RAID_FORCE_TRY_LATER);
                    return;
                }

                //if (Game.Events.IsEventCooldownActive)
                //{
                //    client.SendReply(gm, "Raid cannot be started so quickly after eachother. Please wait {seconds} seconds.", UnityEngine.Mathf.RoundToInt(Game.Events.EventCooldownTimeLeft));
                //    return;
                //}

                Game.Raid.IsBusy = true;
                var result = await Game.RavenNest.Game.ActivateRaidAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Raid);

                    if (!Game.Raid.StartRaid(plr, req =>
                    {
                        if (Game.Raid.CanJoin(plr, string.Empty) == RaidJoinResult.CanJoin)
                        {
                            client.SendReply(gm, Localization.MSG_RAID_FORCE_USED_JOINED);
                            Game.Raid.Join(plr);
                        }
                        else
                        {
                            client.SendReply(gm, Localization.MSG_RAID_FORCE_USED);
                        }

                        Game.Raid.Announce();
                    }))
                    {
                        var raidScroll = Game.Items.Find(x => x.Name.Contains("raid", System.StringComparison.OrdinalIgnoreCase) && x.Type == ItemType.Scroll);
                        if (raidScroll != null)
                        {
                            client.SendReply(gm, Localization.MSG_RAID_FORCE_FAILED_REFUND);
                            await Game.RavenNest.Players.AddItemAsync(plr.Id, raidScroll.Id);
                            plr.Inventory.AddToBackpack(raidScroll);
                        }
                    }
                }
                else
                {
                    switch (result)
                    {
                        case ScrollUseResult.InsufficientScrolls:
                            client.SendReply(gm, Localization.MSG_RAID_FORCE_NO_SCROLLS);
                            return;
                        case ScrollUseResult.Error:
                            client.SendReply(gm, Localization.MSG_EVENT_SCROLL_SERVER_ERROR);
                            return;
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