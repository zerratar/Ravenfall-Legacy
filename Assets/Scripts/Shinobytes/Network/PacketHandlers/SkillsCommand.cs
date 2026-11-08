using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The skill trees from chat (combat v2, docs/combat-skills-design.md, section 6). The bot sends "skills_command" with
/// the action first: !tree (points and loadout), !tree Might (a tree's nodes), !tree cleave (one node),
/// !learn cleave, !loadout (the loadout), !loadout 1 cleave, !loadout clear 3, !respec (the price) and !respec confirm.
/// The server saves every change and answers with the build, which the character then follows.
/// </summary>
public class SkillsCommand : ChatBotCommandHandler<string>
{
    public SkillsCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        if (!CombatRules.Enabled)
        {
            client.SendReply(gm, Localization.MSG_SKILLS_DISABLED);
            return;
        }

        var text = (data ?? "").Trim();
        var space = text.IndexOf(' ');
        var action = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var rest = space < 0 ? "" : text.Substring(space + 1).Trim();

        try
        {
            switch (action)
            {
                case "":
                    await Show(player, gm, client);
                    return;
                case "learn":
                    await Learn(player, rest, gm, client);
                    return;
                case "loadout":
                    await Loadout(player, rest, gm, client);
                    return;
                case "respec":
                    await Respec(player, rest, gm, client);
                    return;
                default:
                    Describe(player, text, gm, client);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("SkillsCommand: " + exc);
            client.SendReply(gm, Localization.MSG_SKILLS_ERROR);
        }
    }

    private async Task Show(PlayerController player, GameMessage gm, GameClient client)
    {
        var info = await Game.RavenNest.Skills.GetAsync(player.Id);
        if (info == null) { client.SendReply(gm, Localization.MSG_SKILLS_ERROR); return; }
        player.ApplySkillBuild(info.Build);
        client.SendReply(gm, Localization.MSG_SKILLS_SHOW, info.Earned - info.Spent, info.Earned, LoadoutText(player));
    }

    private async Task Learn(PlayerController player, string node, GameMessage gm, GameClient client)
    {
        if (string.IsNullOrWhiteSpace(node)) { client.SendReply(gm, Localization.MSG_SKILLS_LEARN_USAGE); return; }
        var result = await Game.RavenNest.Skills.LearnAsync(player.Id, node);
        if (!Answered(player, result, gm, client)) return;
        var learned = SkillTrees.Find(node);
        var build = player.SkillBuild;
        var rank = learned != null && learned.MaxRank > 1 ? " (" + build.Rank(learned.Id) + "/" + learned.MaxRank + ")" : "";
        client.SendReply(gm, Localization.MSG_SKILLS_LEARNED, (learned?.Name ?? node) + rank, result.Info.Earned - result.Info.Spent);
    }

    private async Task Loadout(PlayerController player, string args, GameMessage gm, GameClient client)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            client.SendReply(gm, Localization.MSG_SKILLS_LOADOUT, LoadoutText(player));
            return;
        }

        var parts = args.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        SkillBuildResult result;
        if (parts[0].Equals("clear", StringComparison.OrdinalIgnoreCase) && parts.Length == 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cleared))
        {
            result = await Game.RavenNest.Skills.LoadoutAsync(player.Id, cleared, null);
        }
        else if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slot))
        {
            result = await Game.RavenNest.Skills.LoadoutAsync(player.Id, slot, parts[1]);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_SKILLS_LOADOUT_USAGE);
            return;
        }

        if (!Answered(player, result, gm, client)) return;
        client.SendReply(gm, Localization.MSG_SKILLS_LOADOUT, LoadoutText(player));
    }

    private async Task Respec(PlayerController player, string args, GameMessage gm, GameClient client)
    {
        if (!args.Equals("confirm", StringComparison.OrdinalIgnoreCase))
        {
            var info = await Game.RavenNest.Skills.GetAsync(player.Id);
            if (info == null) { client.SendReply(gm, Localization.MSG_SKILLS_ERROR); return; }
            if (info.Spent == 0) { client.SendReply(gm, Localization.MSG_SKILLS_RESPEC_NOTHING); return; }
            var price = info.FreeRespecs > 0 ? Localization.MSG_SKILLS_RESPEC_FREE
                : info.TomesOfForgetting > 0 ? Localization.MSG_SKILLS_RESPEC_TOME
                : string.Format(Localization.MSG_SKILLS_RESPEC_COINS, Utility.FormatAmount(info.RespecCost));
            client.SendReply(gm, Localization.MSG_SKILLS_RESPEC_CONFIRM, info.Spent, price);
            return;
        }

        if (player.raidHandler.InRaid || player.IsAway || player.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_SKILLS_RESPEC_BUSY);
            return;
        }

        var spent = player.SkillBuild.Spent;
        var result = await Game.RavenNest.Skills.RespecAsync(player.Id);
        if (!Answered(player, result, gm, client)) return;
        client.SendReply(gm, Localization.MSG_SKILLS_RESPEC_DONE, spent, result.Info.Earned);
    }

    /// <summary>!skills Might lists a tree; !skills cleave explains one node.</summary>
    private void Describe(PlayerController player, string text, GameMessage gm, GameClient client)
    {
        var tree = Enum.GetValues(typeof(SkillTree)).Cast<SkillTree>()
            .FirstOrDefault(t => t.ToString().Equals(text, StringComparison.OrdinalIgnoreCase) || SkillTrees.SkillOf(t).ToString().Equals(text, StringComparison.OrdinalIgnoreCase));
        if (tree.ToString().Equals(text, StringComparison.OrdinalIgnoreCase) || SkillTrees.SkillOf(tree).ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
        {
            var build = player.SkillBuild;
            var nodes = SkillTrees.Of(tree).Select(n =>
                n.Name + (n.MaxRank > 1 ? " " + build.Rank(n.Id) + "/" + n.MaxRank : build.Has(n.Id) ? " (learned)" : "")
                + (n.Tier > 0 ? " [" + n.RequiredLevel + "]" : ""));
            client.SendReply(gm, Localization.MSG_SKILLS_TREE, tree, SkillTrees.SkillOf(tree), string.Join(", ", nodes));
            return;
        }

        var node = SkillTrees.Find(text);
        if (node == null)
        {
            client.SendReply(gm, Localization.MSG_SKILLS_UNKNOWN, text);
            return;
        }

        var kind = node.IsActive ? string.Format(Localization.MSG_SKILLS_KIND_ACTIVE, node.Active.Cooldown) : node.MaxRank > 1 ? string.Format(Localization.MSG_SKILLS_KIND_RANKS, node.MaxRank) : Localization.MSG_SKILLS_KIND_PASSIVE;
        // the path: one of the skills linked below it (none on the first tier)
        var path = node.Requires.Length > 0 ? string.Format(Localization.MSG_SKILLS_PATH, string.Join(" or ", node.Requires.Select(r => SkillTrees.Get(r)?.Name ?? r))) : "";
        // the numbers: a passive's per rank and its total (yours now and at the last rank), an active's effects
        var effect = node.EffectText();
        var total = node.TotalText(player.SkillBuild?.Rank(node.Id) ?? 0);
        if (!string.IsNullOrEmpty(total)) effect += " (" + total + ")";
        if (effect.Length > 0) effect = char.ToUpperInvariant(effect[0]) + effect.Substring(1);
        client.SendReply(gm, Localization.MSG_SKILLS_INFO, node.Name, node.Tree, kind, node.Description, effect, SkillTrees.SkillOf(node.Tree), node.RequiredLevel, node.Cost, path);
    }

    private static bool Answered(PlayerController player, SkillBuildResult result, GameMessage gm, GameClient client)
    {
        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_SKILLS_ERROR);
            return false;
        }

        if (result.Info != null) player.ApplySkillBuild(result.Info.Build);
        if (!result.Success)
        {
            client.SendReply(gm, Localization.MSG_SKILLS_FAILED, result.Message ?? "");
            return false;
        }
        return true;
    }

    private static string LoadoutText(PlayerController player)
    {
        var build = player.SkillBuild;
        var slots = Enumerable.Range(0, build.LoadoutSlots).Select(i =>
        {
            var id = i < build.Loadout.Count ? build.Loadout[i] : null;
            var node = SkillTrees.Get(id);
            if (node == null) return (i + 1) + " -";
            var left = player.SkillRunner.CooldownLeft(node.Id);
            return (i + 1) + " " + node.Name + (left > 0.5f ? " (" + Mathf.CeilToInt(left) + "s)" : "");
        });
        return string.Join(", ", slots);
    }
}
