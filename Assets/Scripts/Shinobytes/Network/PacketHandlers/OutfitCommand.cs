using System;
using RavenNest.Models;
using Skill = RavenNest.Models.Skill;

/// <summary>
///     Outfits per skill from chat (docs/outfits-design.md); the bot sends "outfit_command" with what came after !outfit:
///     !outfit (the outfit of what is trained), !outfit save (what is worn becomes it), !outfit pin (and better gear no
///     longer replaces it), !outfit unpin, !outfit (skill) (that skill's outfit goes on now), !outfit auto on|off
///     (change with training) and !outfit upgrade on|off (better gear by itself). The server keeps the outfits and puts
///     them on; the answer brings them back and the gear follows.
/// </summary>
public class OutfitCommand : ChatBotCommandHandler<string>
{
    public OutfitCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var text = (data ?? "").Trim();
        var space = text.IndexOf(' ');
        var action = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var rest = space < 0 ? "" : text.Substring(space + 1).Trim().ToLowerInvariant();
        var outfits = player.Outfits;

        if (!outfits.Active)
        {
            // a bot, or a game the server does not feed: nothing is kept for them
            client.SendReply(gm, Localization.MSG_OUTFIT_FAILED);
            return;
        }

        try
        {
            switch (action)
            {
                case "":
                case "info":
                case "show":
                    Info(player, gm, client);
                    return;

                case "help":
                case "?":
                    client.SendReply(gm, Localization.MSG_OUTFIT_USAGE);
                    return;

                case "save":
                case "keep":
                case "remember":
                    await Save(player, 0, gm, client);
                    return;

                case "pin":
                case "lock":
                    await Save(player, 1, gm, client);
                    return;

                case "unpin":
                case "unlock":
                case "best":
                    await Save(player, 2, gm, client);
                    return;

                case "auto":
                case "follow":
                    if (!OnOff(rest, out var follow))
                    {
                        client.SendReply(gm, outfits.State.Follow ? Localization.MSG_OUTFIT_AUTO_ON : Localization.MSG_OUTFIT_AUTO_OFF);
                        return;
                    }
                    if (!Ok(await Game.RavenNest.Players.OutfitSwitchesAsync(player.Id, follow ? 1 : 0, -1), player, gm, client)) return;
                    client.SendReply(gm, player.Outfits.State.Follow ? Localization.MSG_OUTFIT_AUTO_ON : Localization.MSG_OUTFIT_AUTO_OFF);
                    return;

                case "upgrade":
                case "upgrades":
                    if (!OnOff(rest, out var upgrade))
                    {
                        client.SendReply(gm, outfits.State.Upgrade ? Localization.MSG_OUTFIT_UPGRADE_ON : Localization.MSG_OUTFIT_UPGRADE_OFF);
                        return;
                    }
                    if (!Ok(await Game.RavenNest.Players.OutfitSwitchesAsync(player.Id, -1, upgrade ? 1 : 0), player, gm, client)) return;
                    client.SendReply(gm, player.Outfits.State.Upgrade ? Localization.MSG_OUTFIT_UPGRADE_ON : Localization.MSG_OUTFIT_UPGRADE_OFF);
                    return;
            }

            // anything else is a skill: "!outfit attack", "!outfit mining", "!outfit tank"; "!outfit wear attack" too
            var skill = OutfitRules.ParseSkill(action == "wear" || action == "use" ? rest : text);
            if (skill == Skill.None)
            {
                client.SendReply(gm, Localization.MSG_OUTFIT_USAGE);
                return;
            }
            await Wear(player, skill, gm, client);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!outfit " + text + " for " + player.PlayerName + ": " + exc);
            client.SendReply(gm, Localization.MSG_OUTFIT_FAILED);
        }
    }

    private static bool OnOff(string word, out bool on)
    {
        on = word == "on" || word == "yes" || word == "true" || word == "1";
        return on || word == "off" || word == "no" || word == "false" || word == "0";
    }

    // the answer goes to the player's outfits; false (with a reply) when the server did not do it
    private bool Ok(OutfitResult result, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player || player.Removed) return false;
        if (result == null || result.Status == OutfitStatus.Error || result.Status == OutfitStatus.UnknownSkill)
        {
            client.SendReply(gm, Localization.MSG_OUTFIT_FAILED);
            return false;
        }
        player.Outfits.Apply(result, false);
        return true;
    }

    // !outfit: the outfit of what is trained, its pet, and what is special about it
    private void Info(PlayerController player, GameMessage gm, GameClient client)
    {
        var outfits = player.Outfits;
        var trained = outfits.Trained;
        if (trained == Skill.None)
        {
            client.SendReply(gm, Localization.MSG_OUTFIT_USAGE);
            return;
        }

        var on = outfits.Worn;
        var kept = outfits.State.Find(trained);
        // the outfit that is on is what is worn; another one is what was kept for it
        var gear = on == trained ? outfits.DescribeWorn() : outfits.Describe(trained);
        if (gear == null)
        {
            client.SendReply(gm, Localization.MSG_OUTFIT_NONE_YET, Localization.CombatStyleName(trained));
            return;
        }

        var notes = new System.Collections.Generic.List<string>();
        var pet = on == trained ? player.BattlePets.Active : null;
        if (pet == null && on != trained && kept != null && kept.Pet != Guid.Empty)
        {
            foreach (var p in player.BattlePets.Stable)
            {
                if (p.Id == kept.Pet) pet = p;
            }
        }
        if (pet != null) notes.Add(Localization.OUTFIT_NOTE_PET.Replace("{petName}", pet.Name));
        if (kept != null && kept.PinnedCount > 0) notes.Add(Localization.OUTFIT_NOTE_PINNED.Replace("{count}", kept.PinnedCount.ToString()));
        if (on != trained) notes.Add(Localization.OUTFIT_NOTE_OTHER_ON.Replace("{skillName}", Localization.CombatStyleName(on)));
        if (!outfits.State.Follow) notes.Add(Localization.OUTFIT_NOTE_AUTO_OFF);
        if (!outfits.State.Upgrade) notes.Add(Localization.OUTFIT_NOTE_UPGRADE_OFF);

        if (notes.Count == 0) client.SendReply(gm, Localization.MSG_OUTFIT_INFO, Localization.CombatStyleName(trained), gear);
        else client.SendReply(gm, Localization.MSG_OUTFIT_INFO_NOTES, Localization.CombatStyleName(trained), gear, string.Join(" ", notes));
    }

    // !outfit save, pin, unpin: for the skill that is trained
    private async System.Threading.Tasks.Task Save(PlayerController player, int mode, GameMessage gm, GameClient client)
    {
        var trained = player.Outfits.Trained;
        if (trained == Skill.None)
        {
            client.SendReply(gm, Localization.MSG_OUTFIT_USAGE);
            return;
        }

        var kept = player.Outfits.State.Find(trained);
        var pinnedBefore = kept != null ? kept.PinnedCount : 0;
        if (!Ok(await Game.RavenNest.Players.OutfitSaveAsync(player.Id, (int)trained, mode), player, gm, client)) return;
        var name = Localization.CombatStyleName(trained);
        if (mode == 2)
        {
            client.SendReply(gm, pinnedBefore > 0 ? Localization.MSG_OUTFIT_UNPINNED : Localization.MSG_OUTFIT_UNPINNED_NONE, name);
            return;
        }
        client.SendReply(gm, mode == 1 ? Localization.MSG_OUTFIT_PINNED : Localization.MSG_OUTFIT_SAVED, name, player.Outfits.Describe(trained) ?? player.Outfits.DescribeWorn());
    }

    // !outfit attack: that skill's outfit goes on now, whatever is trained
    private async System.Threading.Tasks.Task Wear(PlayerController player, Skill skill, GameMessage gm, GameClient client)
    {
        var result = await Game.RavenNest.Players.OutfitWearAsync(player.Id, (int)skill);
        if (!Ok(result, player, gm, client)) return;
        var name = Localization.CombatStyleName(skill);
        if (result.Status == OutfitStatus.NoOutfit)
        {
            client.SendReply(gm, Localization.MSG_OUTFIT_NONE_YET, name);
            return;
        }

        var gear = player.Outfits.Describe(skill) ?? player.Outfits.DescribeWorn();
        client.SendReply(gm, result.Changed || result.Pets != null ? Localization.MSG_OUTFIT_WORN : Localization.MSG_OUTFIT_WORN_ALREADY, name, gear);
        // pieces that were gone, and what filled them: said after the outfit itself
        var gone = player.Outfits.GoneText(result, out var goneArgs);
        if (gone != null) client.SendReply(gm, gone, goneArgs);
    }
}
