using System;
using System.Collections.Generic;
using System.Globalization;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !quest: a player's personal quests (phase 1 of docs/quests-design.md). With nothing after it, the quests taken
/// with their progress, today's three offers and this week's quest; !quest accept 2 (or weekly) takes one, and
/// !quest abandon 1 gives up a taken one. Progress is counted on the server; in the tavern the quest giver speaks too.
/// </summary>
public class QuestCommand : ChatBotCommandHandler<string>
{
    public QuestCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string arguments, GameMessage gm, GameClient client)
    {
        _ = QuestChat.HandleAsync(Game, PlayerManager, arguments, gm, client);
    }
}

/// <summary>What !quest says and does, for the new command and for older bots' quest_info.</summary>
public static class QuestChat
{
    public static async System.Threading.Tasks.Task HandleAsync(GameManager game, PlayerManager players, string arguments, GameMessage gm, GameClient client)
    {
        var player = players.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var parts = (arguments ?? "").Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var argument = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";

        try
        {
            var api = game.RavenNest.Quests;
            switch (sub)
            {
                case "accept":
                case "take":
                case "start":
                    {
                        int slot;
                        if (argument == "weekly" || argument == "w" || argument == "week") slot = -1;
                        else if (int.TryParse(argument, out var number) && number >= 1 && number <= 3) slot = number - 1;
                        else { client.SendReply(gm, Localization.MSG_QUEST_USAGE); return; }

                        var result = await api.AcceptAsync(player.Id, slot);
                        if (result == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }
                        if (result.Ok)
                        {
                            client.SendReply(gm, Localization.MSG_QUEST_ACCEPTED, Text(result.Quest), Reward(result.Quest));
                            Say(game, player, Localization.UI_TAVERN_QUEST_ACCEPTED);
                        }
                        else client.SendReply(gm, ErrorText(result.Error));
                        return;
                    }

                case "abandon":
                case "drop":
                case "cancel":
                    {
                        var list = await api.GetAsync(player.Id);
                        var active = list?.Active ?? Array.Empty<QuestData>();
                        if (!int.TryParse(argument, out var number) || number < 1 || number > active.Length)
                        {
                            client.SendReply(gm, Localization.MSG_QUEST_ABANDON_USAGE);
                            return;
                        }

                        var quest = active[number - 1];
                        var result = await api.AbandonAsync(player.Id, quest.Id);
                        client.SendReply(gm, result != null && result.Ok ? Localization.MSG_QUEST_ABANDONED : Localization.MSG_QUEST_FAILED, Text(quest));
                        return;
                    }

                default:
                    await List(game, player, gm, client);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!quest failed: " + exc);
            client.SendReply(gm, Localization.MSG_QUEST_FAILED);
        }
    }

    /// <summary>The quests taken, today's offers and the weekly quest, in one message.</summary>
    private static async System.Threading.Tasks.Task List(GameManager game, PlayerController player, GameMessage gm, GameClient client)
    {
        var list = await game.RavenNest.Quests.GetAsync(player.Id);
        if (list == null)
        {
            client.SendReply(gm, Localization.MSG_QUEST_FAILED);
            return;
        }

        var active = new List<string>();
        for (var i = 0; i < (list.Active?.Length ?? 0); ++i)
        {
            var q = list.Active[i];
            active.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_ACTIVE_ENTRY, i + 1, Text(q), Number(q.Progress), Number(q.Amount)));
        }

        var offers = new List<string>();
        foreach (var q in list.Offers ?? Array.Empty<QuestData>())
        {
            offers.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_OFFER_ENTRY, q.Slot + 1, Text(q), Number(q.RewardCoins)));
        }

        var weekly = list.Weekly == null ? Localization.UI_QUEST_NONE
            : list.Weekly.State == 0 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_WEEKLY_OFFER, Text(list.Weekly), Number(list.Weekly.RewardCoins))
            : list.Weekly.State == 1 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_WEEKLY_ACTIVE, Text(list.Weekly), Number(list.Weekly.Progress), Number(list.Weekly.Amount))
            : list.Weekly.State == 2 ? Localization.UI_QUEST_WEEKLY_DONE
            : Localization.UI_QUEST_NONE;

        client.SendReply(gm, Localization.MSG_QUEST_LIST,
            active.Count > 0 ? string.Join(", ", active) : Localization.UI_QUEST_NONE,
            offers.Count > 0 ? string.Join(", ", offers) : Localization.UI_QUEST_NO_OFFERS,
            weekly,
            list.Streak);

        Say(game, player, offers.Count > 0 ? Localization.UI_TAVERN_QUEST_OFFERS : Localization.UI_TAVERN_QUEST_REPLY);
    }

    /// <summary>"Chop 120 wood", from the quest's objective, skill and amount.</summary>
    public static string Text(QuestData q)
    {
        if (q == null) return string.Empty;
        var amount = Number(q.Amount);
        switch (q.Objective)
        {
            case "gather":
                switch ((Skill)q.Skill)
                {
                    case Skill.Woodcutting: return string.Format(Localization.UI_QUEST_GATHER_WOOD, amount);
                    case Skill.Mining: return string.Format(Localization.UI_QUEST_GATHER_ORE, amount);
                    case Skill.Fishing: return string.Format(Localization.UI_QUEST_GATHER_FISH, amount);
                    case Skill.Farming: return string.Format(Localization.UI_QUEST_GATHER_WHEAT, amount);
                    default: return string.Format(Localization.UI_QUEST_GATHER_HERBS, amount);
                }
            case "exp": return string.Format(Localization.UI_QUEST_EXP, amount, Localization.SkillName((Skill)q.Skill));
            case "craft": return string.Format(Localization.UI_QUEST_CRAFT, amount);
            case "cook": return string.Format(Localization.UI_QUEST_COOK, amount);
            case "brew": return string.Format(Localization.UI_QUEST_BREW, amount);
            case "kill": return string.Format(Localization.UI_QUEST_KILL, amount);
            case "raid": return string.Format(q.Amount == 1 ? Localization.UI_QUEST_RAID_ONE : Localization.UI_QUEST_RAID, amount);
            case "dungeon": return string.Format(q.Amount == 1 ? Localization.UI_QUEST_DUNGEON_ONE : Localization.UI_QUEST_DUNGEON, amount);
            default: return q.Key;
        }
    }

    /// <summary>"1,200 coins and 3,400 Woodcutting exp".</summary>
    public static string Reward(QuestData q) =>
        string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_REWARD, Number(q.RewardCoins), Number((long)q.RewardExp), Localization.SkillName((Skill)q.RewardSkill));

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string ErrorText(string error)
    {
        switch (error)
        {
            case "full": return Localization.MSG_QUEST_FULL;
            case "taken": return Localization.MSG_QUEST_TAKEN;
            case "done": return Localization.MSG_QUEST_DONE;
            case "expired": return Localization.MSG_QUEST_EXPIRED;
            default: return Localization.MSG_QUEST_FAILED;
        }
    }

    /// <summary>In the tavern, the quest giver (or the barkeep) says it to the player as well.</summary>
    private static void Say(GameManager game, PlayerController player, string line)
    {
        if (!game.Tavern || !game.Tavern.IsActivated) return;
        var npc = TavernNpc.Find(TavernNpc.NpcRole.QuestGiver)
            ?? TavernNpc.Find(TavernNpc.NpcRole.Barkeep)
            ?? TavernNpc.Find(TavernNpc.NpcRole.GhostBarkeep);
        if (npc) npc.Say(string.Format(line, player.PlayerName));
    }
}
