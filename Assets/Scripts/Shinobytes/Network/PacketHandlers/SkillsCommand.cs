using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The skill trees from chat (combat v2, docs/combat-skills-design.md, section 6), Taming's Beastmaster among them. The bot sends "skills_command" with
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
        var free = result.Info.Earned - result.Info.Spent;
        if (learned != null && learned.IsActive)
        {
            // an active is only used from the loadout: say where it went, or that it is not in it (a full loadout left
            // it out without a word), when it is used and what it needs equipped
            var slot = build.Loadout.FindIndex(x => string.Equals(x, learned.Id, StringComparison.OrdinalIgnoreCase));
            var needs = SkillTrees.NeedsText(learned.Active.Needs);
            // a weapon is equipped; a battle pet (Beastmaster's commands) is out
            var needsText = learned.Active.Needs == SkillGear.Pet ? Localization.MSG_SKILLS_LEARNED_NEEDS_PET : needs.Length > 0 ? string.Format(Localization.MSG_SKILLS_LEARNED_NEEDS, needs) : "";
            if (slot < 0) client.SendReply(gm, Localization.MSG_SKILLS_LEARNED_FULL + needsText, learned.Name, free);
            else client.SendReply(gm, Localization.MSG_SKILLS_LEARNED_ACTIVE + needsText, learned.Name, slot + 1, SkillTrees.UseText(build.RuleOf(learned)), free);
            return;
        }
        client.SendReply(gm, Localization.MSG_SKILLS_LEARNED, (learned?.Name ?? node) + rank, free);
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
        // a tree by its name, its skill's name (as shown: Vitality, and Health as it was called), or the start of
        // either ("hunt" is the Hunter tree, not Hunter's Pace) when no skill has that exact name
        bool IsTree(SkillTree t, bool start) =>
            start ? t.ToString().StartsWith(text, StringComparison.OrdinalIgnoreCase)
                : t.ToString().Equals(text, StringComparison.OrdinalIgnoreCase) || SkillTrees.SkillOf(t).ToString().Equals(text, StringComparison.OrdinalIgnoreCase)
                    || SkillTrees.SkillName(SkillTrees.SkillOf(t)).Equals(text, StringComparison.OrdinalIgnoreCase);
        var trees = Enum.GetValues(typeof(SkillTree)).Cast<SkillTree>().ToList();
        var matches = SkillTrees.Matches(text);
        var exactSkill = matches.Count == 1 && (matches[0].Name.Equals(text, StringComparison.OrdinalIgnoreCase) || matches[0].Id.Equals(text.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase));
        var treeIndex = trees.FindIndex(t => IsTree(t, false));
        if (treeIndex < 0 && !exactSkill && text.Length >= 3) treeIndex = trees.FindIndex(t => IsTree(t, true));
        if (treeIndex >= 0)
        {
            var tree = trees[treeIndex];
            var build = player.SkillBuild;
            var skillName = SkillTrees.SkillName(SkillTrees.SkillOf(tree));
            var nodes = SkillTrees.Of(tree).Select(n =>
                n.Name + (n.MaxRank > 1 ? " " + build.Rank(n.Id) + "/" + n.MaxRank : build.Has(n.Id) ? " (learned)" : n.IsActive ? " (active)" : "")
                + (n.Tier > 0 ? " [" + n.RequiredLevel + "]" : ""));
            // the Vitality tree is named after its skill: once, not "Vitality (Vitality)"
            if (tree.ToString() == skillName) client.SendReply(gm, Localization.MSG_SKILLS_TREE_OWN_NAME, tree, string.Join(", ", nodes), skillName);
            else client.SendReply(gm, Localization.MSG_SKILLS_TREE, tree, skillName, string.Join(", ", nodes));
            return;
        }

        var node = matches.Count == 1 ? matches[0] : null;
        if (node == null)
        {
            // several skills it could mean: say which; none: the trees
            if (matches.Count > 1) client.SendReply(gm, Localization.MSG_SKILLS_FAILED, SkillTrees.NotFoundText(text));
            else client.SendReply(gm, Localization.MSG_SKILLS_UNKNOWN, text);
            return;
        }

        // an active: how often, when the character uses it (the player's rule from the website, or the skill's own) and
        // what must be equipped; a skill learned for the wrong weapon was never used and nothing said why
        var kind = node.MaxRank > 1 ? string.Format(Localization.MSG_SKILLS_KIND_RANKS, node.MaxRank) : Localization.MSG_SKILLS_KIND_PASSIVE;
        if (node.IsActive)
        {
            kind = string.Format(Localization.MSG_SKILLS_KIND_ACTIVE, node.Active.Cooldown, SkillTrees.UseText(player.SkillBuild != null ? player.SkillBuild.RuleOf(node) : node.Active.Use));
            var needs = SkillTrees.NeedsText(node.Active.Needs);
            if (node.Active.Needs == SkillGear.Pet) kind += Localization.MSG_SKILLS_KIND_NEEDS_PET;
            else if (needs.Length > 0) kind += string.Format(Localization.MSG_SKILLS_KIND_NEEDS, needs);
        }
        // the path: one of the skills linked below it (none on the first tier)
        var path = node.Requires.Length > 0 ? string.Format(Localization.MSG_SKILLS_PATH, string.Join(" or ", node.Requires.Select(r => SkillTrees.Get(r)?.Name ?? r))) : "";
        // the numbers: a passive's per rank and its total (yours now and at the last rank), an active's effects
        var effect = node.EffectText();
        var total = node.TotalText(player.SkillBuild?.Rank(node.Id) ?? 0);
        if (!string.IsNullOrEmpty(total)) effect += " (" + total + ")";
        // nothing to add for the skills without numbers (Last Stand, Battle Rhythm): no ". ." after the description
        if (effect.Length > 0) effect = " " + char.ToUpperInvariant(effect[0]) + effect.Substring(1) + ".";
        var cost = string.Format(node.IsActive || node.MaxRank <= 1 ? Localization.MSG_SKILLS_COST_ACTIVE : Localization.MSG_SKILLS_COST_RANK, node.Cost);
        client.SendReply(gm, Localization.MSG_SKILLS_INFO, node.Name, node.Tree, kind, node.Description, effect, SkillTrees.SkillName(SkillTrees.SkillOf(node.Tree)), node.RequiredLevel, cost, path);
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
            if (node == null) return (i + 1) + " " + Localization.MSG_SKILLS_LOADOUT_EMPTY;
            var left = player.SkillRunner.CooldownLeft(node.Id);
            // a skill whose weapon is not equipped is never used: say so where the player looks
            var needs = node.Active != null && !player.SkillRunner.HasGear(node.Active.Needs) ? string.Format(Localization.MSG_SKILLS_LOADOUT_NEEDS, SkillTrees.NeedsText(node.Active.Needs)) : "";
            return (i + 1) + " " + node.Name + needs + (left > 0.5f ? " (" + Mathf.CeilToInt(left) + "s)" : "");
        });
        return string.Join(", ", slots);
    }
}
