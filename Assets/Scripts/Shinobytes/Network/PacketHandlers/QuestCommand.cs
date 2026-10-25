using System;
using System.Collections.Generic;
using System.Globalization;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !quest: a player's personal quests (phases 1 and 3 of docs/quests-design.md). With nothing after it, the quests
/// taken with their progress, today's three offers and this week's quest, then the seasonal quest and the chain under
/// way; !quest accept 2 (or weekly, or seasonal) takes one, and !quest abandon 1 gives up a taken one. The chains:
/// !quest chains, !quest begin cat, !quest deliver, !quest answer 2, !quest abandon chain. Titles and achievements:
/// !quest title (also !title) and !quest achievements (also !achievements). Progress is counted on the server; in the
/// tavern the quest giver speaks too. !quest stream is the stream quest (phase 2): what it is, and start and stop for
/// the streamer and the moderators.
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
        var parts = (arguments ?? "").Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var argument = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";

        // the stream quest is everyone's, playing or not
        if (sub == "stream" || sub == "s")
        {
            await StreamAsync(game, argument, parts.Length > 2 ? parts[2] : "", gm, client);
            return;
        }

        var player = players.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

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
                        else if (argument == "seasonal" || argument == "season" || argument == "halloween" || argument == "christmas") slot = -2;
                        else if (int.TryParse(argument, out var number) && number >= 1 && number <= 3) slot = number - 1;
                        else if (!string.IsNullOrEmpty(argument)) { await BeginChainAsync(game, player, argument, gm, client); return; }
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

                case "chains":
                case "chain":
                case "stories":
                    await ChainsAsync(game, player, gm, client);
                    return;

                case "begin":
                    await BeginChainAsync(game, player, argument, gm, client);
                    return;

                case "deliver":
                case "give":
                case "handin":
                    await DeliverAsync(game, player, gm, client);
                    return;

                case "answer":
                case "say":
                    await AnswerAsync(game, player, argument, gm, client);
                    return;

                case "abandon":
                case "drop":
                case "cancel":
                    {
                        var list = await api.GetAsync(player.Id);
                        if (argument == "chain" || argument == "story" || argument == "c")
                        {
                            if (list?.Chain == null) { client.SendReply(gm, Localization.MSG_QUEST_NO_CHAIN); return; }
                            var dropped = await api.AbandonAsync(player.Id, list.Chain.Id);
                            client.SendReply(gm, dropped != null && dropped.Ok ? Localization.MSG_QUEST_ABANDONED : Localization.MSG_QUEST_FAILED, ChainName(list.Chain.Key));
                            return;
                        }

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

                case "title":
                case "titles":
                    await TitleAsync(game, player, argument, gm, client);
                    return;

                case "achievements":
                case "achievement":
                case "ach":
                    await AchievementsAsync(game, player, gm, client);
                    return;

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

    /// <summary>
    /// !quest stream: how the stream quest goes, or today's offers; !quest stream start 2 and !quest stream stop for
    /// the streamer and the moderators (the quest window does the same, see StreamQuestView).
    /// </summary>
    private static async System.Threading.Tasks.Task StreamAsync(GameManager game, string action, string argument, GameMessage gm, GameClient client)
    {
        try
        {
            var user = gm.Sender;
            var allowed = user.IsBroadcaster || user.IsModerator || user.IsGameModerator || user.IsGameAdministrator;
            switch (action)
            {
                case "start":
                case "begin":
                    {
                        if (!allowed) { client.SendReply(gm, Localization.MSG_STREAM_QUEST_NOT_ALLOWED); return; }
                        if (!int.TryParse(argument, out var number) || number < 1 || number > 3) { client.SendReply(gm, Localization.MSG_STREAM_QUEST_USAGE); return; }
                        var result = await StreamQuests.StartAsync(game, number - 1);
                        if (result == null) client.SendReply(gm, Localization.MSG_QUEST_FAILED);
                        else if (!result.Ok && result.Error == "running") client.SendReply(gm, Localization.MSG_STREAM_QUEST_RUNNING, StreamQuests.Text(result.Quest));
                        else if (!result.Ok) client.SendReply(gm, Localization.MSG_QUEST_FAILED);
                        // started: StreamQuests tells the whole chat
                        return;
                    }

                case "stop":
                case "end":
                    {
                        if (!allowed) { client.SendReply(gm, Localization.MSG_STREAM_QUEST_NOT_ALLOWED); return; }
                        var result = await StreamQuests.StopAsync(game);
                        if (result == null || (!result.Ok && result.Error != "none")) client.SendReply(gm, Localization.MSG_QUEST_FAILED);
                        else if (!result.Ok) client.SendReply(gm, Localization.MSG_STREAM_QUEST_NONE);
                        // stopped: the server's StreamQuestEnded tells the whole chat
                        return;
                    }

                case "":
                case "info":
                    break;

                default:
                    client.SendReply(gm, Localization.MSG_STREAM_QUEST_USAGE);
                    return;
            }

            await StreamQuests.RefreshAsync(game);
            var state = StreamQuests.Latest;
            if (state == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }

            var q = StreamQuests.Active;
            if (q != null)
            {
                client.SendReply(gm, Localization.MSG_STREAM_QUEST_INFO, StreamQuests.Text(q), Number(q.Progress), Number(q.Amount),
                    StreamQuests.MinutesLeft(q), q.Helpers, StreamQuests.Reward(q));
                return;
            }

            var offers = new List<string>();
            foreach (var o in state.Offers ?? Array.Empty<StreamQuestData>())
            {
                offers.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_OFFER_ENTRY, o.Slot + 1, StreamQuests.Text(o), o.Minutes));
            }
            client.SendReply(gm, Localization.MSG_STREAM_QUEST_OFFERS, offers.Count > 0 ? string.Join(", ", offers) : Localization.UI_QUEST_NO_OFFERS);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!quest stream failed: " + exc);
            client.SendReply(gm, Localization.MSG_QUEST_FAILED);
        }
    }

    /// <summary>The quests taken, today's offers and the weekly quest, in one message; then the seasonal quest and the chain.</summary>
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

        var seasonal = list.Seasonal;
        if (seasonal != null && seasonal.State == 0)
            client.SendReply(gm, Localization.MSG_QUEST_SEASONAL_OFFER, SeasonOf(seasonal), Text(seasonal), Reward(seasonal));
        else if (seasonal != null && seasonal.State == 1)
            client.SendReply(gm, Localization.MSG_QUEST_SEASONAL_ACTIVE, SeasonOf(seasonal), Text(seasonal), Number(seasonal.Progress), Number(seasonal.Amount));

        if (list.Chain != null)
        {
            client.SendReply(gm, Localization.MSG_QUEST_CHAIN_STEP, ChainName(list.Chain.Key), list.Chain.Step + 1, list.Chain.Steps, Story(list.Chain.Key, list.Chain.Step), Task(list.Chain));
            Say(game, player, Story(list.Chain.Key, list.Chain.Step));
            return;
        }

        Say(game, player, offers.Count > 0 ? Localization.UI_TAVERN_QUEST_OFFERS : Localization.UI_TAVERN_QUEST_REPLY);
    }

    // ---- Chains ----------------------------------------------------------------------------------------------

    private static async System.Threading.Tasks.Task ChainsAsync(GameManager game, PlayerController player, GameMessage gm, GameClient client)
    {
        var list = await game.RavenNest.Quests.GetAsync(player.Id);
        if (list?.Chains == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }

        var entries = new List<string>();
        foreach (var c in list.Chains)
        {
            var state = c.State == 2 ? Localization.UI_QUEST_CHAIN_DONE
                : c.State == 1 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_CHAIN_ACTIVE, c.Step + 1, c.Steps)
                : c.State == 5 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_CHAIN_NOTNOW, Localization.DecorEvent(c.Event))
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_CHAIN_OPEN, c.Steps);
            entries.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_CHAIN_ENTRY, c.Key, ChainName(c.Key), state));
        }
        client.SendReply(gm, Localization.MSG_QUEST_CHAINS, string.Join(", ", entries));
    }

    private static async System.Threading.Tasks.Task BeginChainAsync(GameManager game, PlayerController player, string argument, GameMessage gm, GameClient client)
    {
        var api = game.RavenNest.Quests;
        var list = await api.GetAsync(player.Id);
        var key = FindChain(list?.Chains, argument);
        if (key == null) { client.SendReply(gm, Localization.MSG_QUEST_CHAIN_UNKNOWN); return; }

        var result = await api.BeginChainAsync(player.Id, key);
        if (result == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }
        if (result.Ok)
        {
            var q = result.Quest;
            client.SendReply(gm, Localization.MSG_QUEST_CHAIN_BEGUN, ChainName(key), Story(key, 0), Task(q));
            Say(game, player, Story(key, 0));
            return;
        }

        switch (result.Error)
        {
            case "busy": client.SendReply(gm, Localization.MSG_QUEST_CHAIN_BUSY, ChainName(result.Quest?.Key)); break;
            case "taken": client.SendReply(gm, Localization.MSG_QUEST_CHAIN_STEP, ChainName(key), result.Quest.Step + 1, result.Quest.Steps, Story(key, result.Quest.Step), Task(result.Quest)); break;
            case "done": client.SendReply(gm, Localization.MSG_QUEST_CHAIN_FINISHED, ChainName(key)); break;
            case "notnow":
                {
                    var ev = list?.Chains != null ? Array.Find(list.Chains, c => c.Key == key)?.Event : null;
                    client.SendReply(gm, Localization.MSG_QUEST_CHAIN_NOTNOW, ChainName(key), Localization.DecorEvent(ev));
                    break;
                }
            default: client.SendReply(gm, Localization.MSG_QUEST_FAILED); break;
        }
    }

    private static async System.Threading.Tasks.Task DeliverAsync(GameManager game, PlayerController player, GameMessage gm, GameClient client)
    {
        var result = await game.RavenNest.Quests.DeliverAsync(player.Id);
        if (result == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }
        var q = result.Quest;
        if (result.Ok)
        {
            // a finished step is told by the QuestCompleted event, with the next one
            if (q.Progress < q.Amount) client.SendReply(gm, Localization.MSG_QUEST_DELIVERED, Number(result.Delivered), q.Target, Number(q.Progress), Number(q.Amount));
            return;
        }

        switch (result.Error)
        {
            case "unknown" when q == null: client.SendReply(gm, Localization.MSG_QUEST_NO_CHAIN); break;
            case "nothing": client.SendReply(gm, Localization.MSG_QUEST_DELIVER_NOTHING, q.Target, Task(q)); break;
            case "notstep": client.SendReply(gm, Localization.MSG_QUEST_NOT_DELIVER, Task(q)); break;
            default: client.SendReply(gm, Localization.MSG_QUEST_FAILED); break;
        }
    }

    private static async System.Threading.Tasks.Task AnswerAsync(GameManager game, PlayerController player, string argument, GameMessage gm, GameClient client)
    {
        if (!int.TryParse(argument, out var answer)) { client.SendReply(gm, Localization.MSG_QUEST_ANSWER_USAGE); return; }
        var result = await game.RavenNest.Quests.AnswerAsync(player.Id, answer);
        if (result == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }
        if (result.Ok) return; // the QuestCompleted event tells the next step

        var q = result.Quest;
        switch (result.Error)
        {
            case "wrong":
                client.SendReply(gm, Localization.MSG_QUEST_WRONG, Lookup("UI_CHAIN_" + q.Key + "_WRONG", Localization.UI_QUEST_WRONG_DEFAULT));
                break;
            case "notstep": client.SendReply(gm, Localization.MSG_QUEST_NOT_ANSWER, Task(q)); break;
            case "unknown" when q == null: client.SendReply(gm, Localization.MSG_QUEST_NO_CHAIN); break;
            default: client.SendReply(gm, Localization.MSG_QUEST_ANSWER_USAGE); break;
        }
    }

    /// <summary>A chain by its key, the start of its key or name, or its number in !quest chains.</summary>
    private static string FindChain(QuestChainData[] chains, string argument)
    {
        if (chains == null || string.IsNullOrEmpty(argument)) return null;
        if (int.TryParse(argument, out var number)) return number >= 1 && number <= chains.Length ? chains[number - 1].Key : null;
        foreach (var c in chains) if (c.Key == argument) return c.Key;
        foreach (var c in chains)
        {
            if (c.Key.StartsWith(argument, StringComparison.OrdinalIgnoreCase)) return c.Key;
            if (ChainName(c.Key).IndexOf(argument, StringComparison.OrdinalIgnoreCase) >= 0) return c.Key;
        }
        return null;
    }

    // ---- Titles and achievements ---------------------------------------------------------------------------

    private static async System.Threading.Tasks.Task TitleAsync(GameManager game, PlayerController player, string argument, GameMessage gm, GameClient client)
    {
        var api = game.RavenNest.Quests;
        var list = await api.GetAchievementsAsync(player.Id);
        if (list == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }
        var titles = list.Titles ?? Array.Empty<string>();

        if (argument == "off" || argument == "none" || argument == "remove")
        {
            var removed = await api.SetTitleAsync(player.Id, "none");
            if (removed != null && removed.Ok) QuestTitles.Set(player.Id, null);
            client.SendReply(gm, removed != null && removed.Ok ? Localization.MSG_TITLE_REMOVED : Localization.MSG_QUEST_FAILED);
            return;
        }

        if (int.TryParse(argument, out var number))
        {
            if (number < 1 || number > titles.Length) { client.SendReply(gm, titles.Length == 0 ? Localization.MSG_TITLE_NONE : Localization.MSG_TITLE_USAGE); return; }
            var title = titles[number - 1];
            var result = await api.SetTitleAsync(player.Id, title);
            if (result != null && result.Ok) QuestTitles.Set(player.Id, title);
            client.SendReply(gm, result != null && result.Ok ? Localization.MSG_TITLE_SET : Localization.MSG_QUEST_FAILED, TitleText(title));
            return;
        }

        if (titles.Length == 0) { client.SendReply(gm, Localization.MSG_TITLE_NONE); return; }
        var entries = new List<string>();
        for (var i = 0; i < titles.Length; ++i)
        {
            entries.Add(string.Format(CultureInfo.InvariantCulture, titles[i] == list.Title ? Localization.UI_TITLE_ENTRY_WORN : Localization.UI_TITLE_ENTRY, i + 1, TitleText(titles[i])));
        }
        client.SendReply(gm, Localization.MSG_TITLE_LIST, string.Join(", ", entries));
    }

    private static async System.Threading.Tasks.Task AchievementsAsync(GameManager game, PlayerController player, GameMessage gm, GameClient client)
    {
        var list = await game.RavenNest.Quests.GetAchievementsAsync(player.Id);
        if (list?.Achievements == null) { client.SendReply(gm, Localization.MSG_QUEST_FAILED); return; }

        var reached = new List<string>();
        var next = new List<(double share, string text)>();
        foreach (var a in list.Achievements)
        {
            if (a.Tier > 0) reached.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ACHIEVEMENT_REACHED_ENTRY, AchievementName(a.Key), Medal(a.Tier)));
            if (a.Targets != null && a.Tier < a.Targets.Length && a.Progress > 0)
            {
                var target = a.Targets[a.Tier];
                next.Add(((double)a.Progress / target, string.Format(CultureInfo.InvariantCulture, Localization.UI_ACHIEVEMENT_NEXT_ENTRY, AchievementName(a.Key), Number(a.Progress), Number(target))));
            }
        }

        next.Sort((x, y) => y.share.CompareTo(x.share));
        var closest = new List<string>();
        for (var i = 0; i < next.Count && i < 3; ++i) closest.Add(next[i].text);
        client.SendReply(gm, Localization.MSG_ACHIEVEMENTS,
            reached.Count > 0 ? string.Join(", ", reached) : Localization.UI_QUEST_NONE,
            closest.Count > 0 ? string.Join(", ", closest) : Localization.UI_QUEST_NONE);
    }

    // ---- Words -------------------------------------------------------------------------------------------------

    /// <summary>"Chop 120 wood", from the quest's objective, skill and amount; a chain step's own kinds too.</summary>
    public static string Text(QuestData q)
    {
        if (q == null) return string.Empty;
        var amount = Number(q.Amount);
        if (q.Kind == 2)
        {
            var seasonal = Lookup("UI_QUEST_" + q.Key, null);
            if (seasonal != null) return string.Format(seasonal, amount);
        }

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
            case "deliver": return string.Format(Localization.UI_QUEST_DELIVER, amount, q.Target);
            case "visit": return string.Format(Localization.UI_QUEST_VISIT, q.Target);
            case "answer": return string.Format(Localization.UI_QUEST_ANSWER, q.Options);
            default: return q.Key;
        }
    }

    /// <summary>A chain step's task with its progress when it counts: "Bring 10 Raw Sprat (4/10)".</summary>
    public static string Task(QuestData q)
    {
        if (q == null) return string.Empty;
        var text = Text(q);
        return q.Amount > 1 && q.Progress > 0 ? text + " (" + Number(q.Progress) + "/" + Number(q.Amount) + ")" : text;
    }

    /// <summary>"1,200 coins and 3,400 Woodcutting exp".</summary>
    public static string Reward(QuestData q) =>
        string.Format(CultureInfo.InvariantCulture, Localization.UI_QUEST_REWARD, Number(q.RewardCoins), Number((long)q.RewardExp), Localization.SkillName((Skill)q.RewardSkill));

    public static string ChainName(string key) => Lookup("UI_CHAIN_" + key, key);

    /// <summary>A chain step's story line (from 0), as the quest giver tells it.</summary>
    public static string Story(string key, int step) => Lookup("UI_CHAIN_" + key + "_" + (step + 1), string.Empty);

    public static string Ending(string key) => Lookup("UI_CHAIN_" + key + "_END", string.Empty);

    /// <summary>"the Lumberjack": a title's words from its key ("woodcutter_2", "cat").</summary>
    public static string TitleText(string key) => string.IsNullOrEmpty(key) ? string.Empty : Lookup("UI_TITLE_" + key, key);

    public static string AchievementName(string key) => Lookup("UI_ACHIEVEMENT_" + key, key);

    public static string Medal(int tier) => tier == 3 ? Localization.UI_MEDAL_GOLD : tier == 2 ? Localization.UI_MEDAL_SILVER : Localization.UI_MEDAL_BRONZE;

    /// <summary>The event a seasonal quest belongs to, from its key ("halloween_kill").</summary>
    public static string SeasonOf(QuestData q)
    {
        var key = q?.Key ?? string.Empty;
        var cut = key.IndexOf('_');
        return Localization.DecorEvent(cut > 0 ? key.Substring(0, cut) : key);
    }

    /// <summary>A string of the current language by its name in Localization, in capitals.</summary>
    private static string Lookup(string name, string fallback)
    {
        var field = typeof(Localization).GetField(name.ToUpperInvariant(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return field?.GetValue(null) as string ?? fallback;
    }

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string ErrorText(string error)
    {
        switch (error)
        {
            case "full": return Localization.MSG_QUEST_FULL;
            case "taken": return Localization.MSG_QUEST_TAKEN;
            case "done": return Localization.MSG_QUEST_DONE;
            case "expired": return Localization.MSG_QUEST_EXPIRED;
            case "notnow": return Localization.MSG_QUEST_NO_SEASON;
            default: return Localization.MSG_QUEST_FAILED;
        }
    }

    /// <summary>In the tavern, the quest giver (or the barkeep) says it to the player as well.</summary>
    public static void Say(GameManager game, PlayerController player, string line)
    {
        if (string.IsNullOrEmpty(line) || !game.Tavern || !game.Tavern.IsActivated) return;
        var npc = TavernNpc.Find(TavernNpc.NpcRole.QuestGiver)
            ?? TavernNpc.Find(TavernNpc.NpcRole.Barkeep)
            ?? TavernNpc.Find(TavernNpc.NpcRole.GhostBarkeep);
        if (npc) npc.Say(string.Format(line, player.PlayerName));
    }
}
