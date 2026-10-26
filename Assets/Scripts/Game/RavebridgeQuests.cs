using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// Ravebridge's island quests (phase 6e of docs/ravebridge-design.md): RuneScape tributes (the cook's big day, the
/// restless ghost, goblin diplomacy) and quests of its own (the windmill that won't, mending the old bridge), for the
/// character the streamer plays by hand. They run in the game from StreamingAssets/Ravebridge/quests.json: steps in
/// order, each one talking to an NPC, collecting quest items from what the character does on the island (a skill's
/// action, or a kill), bringing them back, or defending Ravebridge: holding it for a number of waves in hero mode
/// (WaveRun, started by talking to the NPC; once the quest is done the NPC offers runs for the best score). Quest items and progress belong to the character: in the Ravebridge
/// character's file, or for the streamer's own characters in states/ravebridge/quests-(character id).json. Finishing
/// a quest gives experience (and coins and items to the Ravebridge character); finishing all of them makes Duke
/// Ravacio give the streamer the title "of Ravebridge" (api/quests/village/ravebridge-done).
/// Bounties are quests that ask for loot from the Ravebridge bag (a "loot" step: ten goblin ears, a soul shard) and
/// pay in Ravebridge coins, loot and experience; they come round again after a cooldown, and do not count toward the
/// Duke's title.
/// </summary>
public static class RavebridgeQuests
{
    // ---- The data -----------------------------------------------------------------------------------------

    public class Text : Dictionary<string, string>
    {
        public string Get() => TryGetValue(Languages.Current, out var t) && !string.IsNullOrEmpty(t) ? t : TryGetValue("en", out var en) ? en : string.Empty;
    }

    public class TextList : Dictionary<string, List<string>>
    {
        public IReadOnlyList<string> Get() => TryGetValue(Languages.Current, out var t) && t != null && t.Count > 0 ? t : TryGetValue("en", out var en) ? en : (IReadOnlyList<string>)Array.Empty<string>();
    }

    public class Npc { public Text name; public TextList lines; }

    public class Step
    {
        public string type, npc, item, kill, skill, requires;
        public int amount = 1;
        public float chance = 1f;
        public Dictionary<string, int> items, gives, uses;
        /// <summary>A loot step's: what it wants from the Ravebridge bag (RavebridgeLoot keys) and how many.</summary>
        public Dictionary<string, int> loot;
        public Text say, hint, blocked;
    }

    /// <summary>
    /// What finishing gives: experience; coins and items (the Ravebridge character's own); and into the Ravebridge bag,
    /// for any character, Ravebridge coins and loot.
    /// </summary>
    public class Reward
    {
        public Dictionary<string, double> exp;
        public long coins;
        public Dictionary<string, int> items;
        public long ravebridgeCoins;
        public Dictionary<string, int> loot;
    }

    /// <summary>A quest; a bounty comes round again <see cref="cooldown"/> minutes after it is done.</summary>
    public class Quest { public string key; public Text name; public List<Step> steps; public Reward reward; public bool bounty; public float cooldown; }
    public class DukeText { public Text progress, done; }
    public class Book { public Dictionary<string, Npc> npcs; public Dictionary<string, Text> items; public List<Quest> quests; public DukeText duke; }

    /// <summary>
    /// One character's Ravebridge: the step each quest is on (-1 when done), its quest items, its quest points, and its
    /// Ravebridge bag (the island's coins and the loot hero mode's enemies dropped, RavebridgeLoot).
    /// </summary>
    [Serializable]
    public class State
    {
        public Dictionary<string, int> Steps = new Dictionary<string, int>();
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public int Points;
        public bool Titled;
        public long Coins;
        public Dictionary<string, long> Loot = new Dictionary<string, long>();

        /// <summary>Hero mode upgrades bought from the shopkeeper, by shop entry, and their levels (RavebridgeShop).</summary>
        public Dictionary<string, int> Upgrades = new Dictionary<string, int>();

        /// <summary>Bounties: how often each was done, and when it comes round again (UTC).</summary>
        public Dictionary<string, int> Bounties = new Dictionary<string, int>();
        public Dictionary<string, DateTime> BountyReadyAt = new Dictionary<string, DateTime>();
    }

    private static Book book;
    private static readonly Dictionary<Guid, State> ownStates = new Dictionary<Guid, State>();
    private static readonly Dictionary<Skill, double> lastExperience = new Dictionary<Skill, double>();
    private static PlayerController tracked;
    private static EnemyController lastEnemy;

    /// <summary>Something found or a quest done: a short line for the Ravebridge bar.</summary>
    public static event Action<string> Notice;

    public static Book Data
    {
        get
        {
            if (book != null) return book;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "Ravebridge", "quests.json");
                book = JsonConvert.DeserializeObject<Book>(File.ReadAllText(path));
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("The Ravebridge quests could not be read: " + exc.Message);
                book = new Book { npcs = new Dictionary<string, Npc>(), items = new Dictionary<string, Text>(), quests = new List<Quest>(), duke = new DukeText() };
            }
            IslandNpc.LinesFor = key => book.npcs != null && book.npcs.TryGetValue(key, out var n) && n.lines != null ? n.lines.Get() : null;
            return book;
        }
    }

    public static string NpcName(string key) => Data.npcs != null && Data.npcs.TryGetValue(key, out var n) && n.name != null ? n.name.Get() : key;
    public static string ItemName(string key) => Data.items != null && Data.items.TryGetValue(key, out var t) ? t.Get() : key;
    /// <summary>The story quests (the Duke's title is for all of them); bounties are not counted.</summary>
    public static int QuestCount => Data.quests?.Count(q => !q.bounty) ?? 0;

    // ---- A character's progress -------------------------------------------------------------------------

    private static string Folder => Path.Combine(Shinobytes.IO.Path.GameFolder, "states", "ravebridge");

    public static State StateOf(PlayerController player)
    {
        if (!player) return new State();
        if (player.IsLocal)
        {
            var data = RavebridgeCharacter.Current;
            if (data == null) return new State();
            return data.Quest ??= new State();
        }
        if (ownStates.TryGetValue(player.Id, out var state)) return state;
        try
        {
            var path = Path.Combine(Folder, "quests-" + player.Id.ToString("N") + ".json");
            state = File.Exists(path) ? JsonConvert.DeserializeObject<State>(File.ReadAllText(path)) : null;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Ravebridge quests of " + player.Name + " could not be read: " + exc.Message);
        }
        state ??= new State();
        state.Steps ??= new Dictionary<string, int>();
        state.Items ??= new Dictionary<string, int>();
        state.Loot ??= new Dictionary<string, long>();
        state.Upgrades ??= new Dictionary<string, int>();
        state.Bounties ??= new Dictionary<string, int>();
        state.BountyReadyAt ??= new Dictionary<string, DateTime>();
        ownStates[player.Id] = state;
        return state;
    }

    public static void Save(PlayerController player)
    {
        if (!player) return;
        if (player.IsLocal) { RavebridgeCharacter.Save(); return; }
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "quests-" + player.Id.ToString("N") + ".json"), JsonConvert.SerializeObject(StateOf(player), Formatting.Indented));
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Ravebridge quests of " + player.Name + " could not be saved: " + exc.Message);
        }
    }

    public static int StepOf(State state, string quest) => state.Steps.TryGetValue(quest, out var step) ? step : 0;
    public static bool IsDone(State state, string quest) => StepOf(state, quest) < 0;
    private static int Count(State state, string item) => state.Items.TryGetValue(item, out var n) ? n : 0;
    private static bool Has(State state, Dictionary<string, int> items) => items == null || items.All(i => Count(state, i.Key) >= i.Value);

    private static void Use(State state, Dictionary<string, int> items)
    {
        if (items == null) return;
        foreach (var i in items) state.Items[i.Key] = Math.Max(0, Count(state, i.Key) - i.Value);
    }

    private static void Add(State state, Dictionary<string, int> items)
    {
        if (items == null) return;
        foreach (var i in items) state.Items[i.Key] = Count(state, i.Key) + i.Value;
    }

    // ---- Talking ------------------------------------------------------------------------------------------

    public struct Option
    {
        public string Label;
        public Action Pick;

        /// <summary>The talk ends once it is picked (it opened something else: the shop).</summary>
        public bool Closes;
    }

    public class Reply
    {
        public string Npc;
        public string Line;
        public readonly List<Option> Options = new List<Option>();
    }

    /// <summary>What an NPC says to the character, and what it can answer.</summary>
    public static Reply Talk(PlayerController player, string npc)
    {
        var state = StateOf(player);
        var reply = new Reply { Npc = NpcName(npc) };
        if (npc == "duke") return Duke(player, state, reply);

        // collect steps whose items are already in hand are behind us
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at <= 0 || at >= quest.steps.Count || quest.steps[at].type != "collect") continue;
            if (Count(state, quest.steps[at].item) >= quest.steps[at].amount) Advance(player, quest);
        }

        Quest waiting = null;
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at < 0 || at >= quest.steps.Count) continue;
            var step = quest.steps[at];
            if (step.npc != npc) continue;

            // a bounty not offered yet: not before the quest it follows, and not again before its cooldown is over
            if (quest.bounty && at == 0)
            {
                if (step.requires != null && !IsDone(state, step.requires)) continue;
                if (BountyWait(state, quest) > 0f) { waiting ??= quest; continue; }
            }

            if (step.type == "loot")
            {
                var q = quest;
                if (HasLoot(player, step.loot))
                {
                    reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_HAVE, LootList(step.loot));
                    reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_GIVE, Pick = () => { TakeLoot(player, step.loot); Say(npc, step.say?.Get()); Advance(player, q); } });
                }
                else reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_STILL_NEED, LootList(step.loot, player));
                reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
                return reply;
            }

            if (step.type == "talk")
            {
                if (step.requires != null && !IsDone(state, step.requires))
                {
                    reply.Line = step.blocked?.Get() ?? Localization.UI_RAVEBRIDGE_Q_NOT_YET;
                    return reply;
                }
                if (!Has(state, step.uses))
                {
                    reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_NEED, Items(step.uses));
                    return reply;
                }
                reply.Line = step.say?.Get();
                var q = quest;
                reply.Options.Add(new Option
                {
                    Label = at == 0 ? string.Format(quest.bounty ? Localization.UI_RAVEBRIDGE_Q_BOUNTY_ACCEPT : Localization.UI_RAVEBRIDGE_Q_ACCEPT, quest.name.Get()) : Localization.UI_RAVEBRIDGE_Q_CONTINUE,
                    Pick = () => { Use(state, step.uses); Add(state, step.gives); Advance(player, q); },
                });
                reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
                return reply;
            }

            if (step.type == "defend")
            {
                if (WaveRun.Current)
                {
                    reply.Line = Localization.UI_WAVE_RUNNING;
                    reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
                    return reply;
                }
                reply.Line = step.say?.Get();
                var q = quest;
                var goal = step.amount;
                reply.Options.Add(new Option
                {
                    Label = string.Format(Localization.UI_WAVE_START, goal),
                    Pick = () => Defend(player, goal, run =>
                    {
                        var now = StateOf(player);
                        if (!run.Abandoned && run.Cleared >= goal && StepOf(now, q.key) < q.steps.Count && StepOf(now, q.key) >= 0 && q.steps[StepOf(now, q.key)].type == "defend") Advance(player, q);
                        else if (!run.Abandoned) Notice?.Invoke(string.Format(Localization.UI_WAVE_SHORT, run.Cleared, goal));
                    }),
                });
                reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
                return reply;
            }

            if (step.type == "bring")
            {
                if (Has(state, step.items))
                {
                    reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_HAVE, Items(step.items));
                    var q = quest;
                    reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_GIVE, Pick = () => { Use(state, step.items); Say(npc, step.say?.Get()); Advance(player, q); } });
                }
                else reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_STILL_NEED, Items(step.items, state));
                reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
                return reply;
            }
        }

        // collecting something for this NPC: a reminder of what
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at <= 0 || at >= quest.steps.Count || quest.steps[at].type != "collect") continue;
            var giver = quest.steps.Take(at).LastOrDefault(s => s.npc != null)?.npc;
            if (giver != npc) continue;
            var step = quest.steps[at];
            reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_REMIND, step.hint?.Get(), Count(state, step.item), step.amount);
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
            return reply;
        }

        // a bounty done not long ago: when there will be more
        if (waiting != null && !(npc == "captain" && IsDone(state, "defend")))
        {
            reply.Line = string.Format(Localization.UI_RAVEBRIDGE_Q_BOUNTY_WAIT, Mathf.CeilToInt(BountyWait(state, waiting)));
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
            return reply;
        }

        // the captain, once the line has held: runs for the best score
        if (npc == "captain" && IsDone(state, "defend") && !WaveRun.Current)
        {
            reply.Line = string.Format(Localization.UI_WAVE_AGAIN, WaveRun.BestOf(player));
            var board = WaveRun.Board?.Entries;
            if (board != null && board.Length > 0)
            {
                reply.Line += "\n\n" + Localization.UI_WAVE_LEADERBOARD;
                for (var i = 0; i < board.Length && i < 3; i++) reply.Line += "\n" + string.Format(Localization.UI_WAVE_LEADERBOARD_ROW, i + 1, board[i].Streamer, board[i].Waves);
            }
            reply.Options.Add(new Option { Label = Localization.UI_WAVE_ENDLESS, Pick = () => Defend(player, 0, null) });
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
            return reply;
        }

        // the shopkeeper: the cart finally came
        if (npc == "shopkeeper")
        {
            var said = Data.npcs.TryGetValue(npc, out var keeper) && keeper.lines != null ? keeper.lines.Get() : null;
            reply.Line = said != null && said.Count > 0 ? said[UnityEngine.Random.Range(0, said.Count)] : "...";
            reply.Options.Add(new Option { Label = Localization.UI_RBSHOP_OPEN, Pick = () => RavebridgeShop.Open(player), Closes = true });
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
            return reply;
        }

        // the miller grinds wheat once the mill turns, for anyone
        if (npc == "miller" && IsDone(state, "windmill") && Count(state, "wheat") > 0)
        {
            reply.Line = Localization.UI_RAVEBRIDGE_Q_GRIND;
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_CONTINUE, Pick = () => { state.Items["wheat"]--; state.Items["flour"] = Count(state, "flour") + 1; Save(player); } });
            reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
            return reply;
        }

        var lines = Data.npcs.TryGetValue(npc, out var n) && n.lines != null ? n.lines.Get() : null;
        reply.Line = lines != null && lines.Count > 0 ? lines[UnityEngine.Random.Range(0, lines.Count)] : "...";
        reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
        return reply;
    }

    /// <summary>A wave run for the character played by hand, in hero mode (started if it is not on yet).</summary>
    private static void Defend(PlayerController player, int goal, Action<WaveRun> finished)
    {
        if (!player || !player.Controlled) return;
        if (!HeroController.Active || HeroController.Active.Player != player)
        {
            if (!HeroController.Begin(player)) return;
        }
        if (!WaveRun.Begin(player, goal, finished)) Notice?.Invoke(Localization.UI_WAVE_CANT);
    }

    private static Reply Duke(PlayerController player, State state, Reply reply)
    {
        var done = Data.quests.Count(q => !q.bounty && IsDone(state, q.key));
        if (done >= QuestCount && QuestCount > 0)
        {
            reply.Line = Data.duke?.done?.Get();
            if (!state.Titled)
            {
                state.Titled = true;
                Save(player);
                _ = GiveTitleAsync(player);
            }
        }
        else reply.Line = string.Format(Data.duke?.progress?.Get() ?? "{0}/{1}", done, QuestCount);
        reply.Options.Add(new Option { Label = Localization.UI_RAVEBRIDGE_Q_BYE, Pick = null });
        return reply;
    }

    private static async System.Threading.Tasks.Task GiveTitleAsync(PlayerController player)
    {
        var game = GameManager.Instance;
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        try
        {
            var result = await game.RavenNest.Quests.RavebridgeDoneAsync();
            if (result != null && result.Ok) game.RavenBot?.Announce(Localization.MSG_RAVEBRIDGE_TITLE, player.Name);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The Ravebridge title could not be given: " + exc.Message);
        }
    }

    private static string Items(Dictionary<string, int> items, State have = null)
    {
        if (items == null) return string.Empty;
        return string.Join(", ", items.Select(i => have == null
            ? (i.Value > 1 ? i.Value + " x " : string.Empty) + ItemName(i.Key)
            : ItemName(i.Key) + " (" + Math.Min(Count(have, i.Key), i.Value) + "/" + i.Value + ")"));
    }

    // ---- Loot steps (bounties) --------------------------------------------------------------------------------

    /// <summary>Minutes before a bounty comes round again, 0 when it is ready.</summary>
    private static float BountyWait(State state, Quest quest)
    {
        if (state.BountyReadyAt == null || !state.BountyReadyAt.TryGetValue(quest.key, out var ready)) return 0f;
        return Mathf.Max(0f, (float)(ready - DateTime.UtcNow).TotalMinutes);
    }

    private static bool HasLoot(PlayerController player, Dictionary<string, int> loot)
        => loot == null || loot.All(l => RavebridgeLoot.Count(player, l.Key) >= l.Value);

    private static void TakeLoot(PlayerController player, Dictionary<string, int> loot)
    {
        if (loot == null) return;
        foreach (var l in loot) RavebridgeLoot.Remove(player, RavebridgeLoot.Find(l.Key), l.Value);
    }

    private static string LootName(string key) => RavebridgeLoot.Find(key)?.Name ?? key;

    private static string LootList(Dictionary<string, int> loot, PlayerController have = null)
    {
        if (loot == null) return string.Empty;
        return string.Join(", ", loot.Select(l => have == null
            ? (l.Value > 1 ? l.Value + " x " : string.Empty) + LootName(l.Key)
            : LootName(l.Key) + " (" + Math.Min(RavebridgeLoot.Count(have, l.Key), l.Value) + "/" + l.Value + ")"));
    }

    /// <summary>The loot steps under way, for the hero HUD's tracker: the quest, who to bring it to, and each thing's count.</summary>
    public static List<(string Quest, string Npc, List<(RavebridgeLoot.Item Item, long Have, int Need)> Loot)> LootSteps(PlayerController player)
    {
        var list = new List<(string, string, List<(RavebridgeLoot.Item, long, int)>)>();
        if (!player) return list;
        var state = StateOf(player);
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at <= 0 || at >= quest.steps.Count || quest.steps[at].type != "loot" || quest.steps[at].loot == null) continue;
            var step = quest.steps[at];
            var rows = new List<(RavebridgeLoot.Item, long, int)>();
            foreach (var l in step.loot)
            {
                var item = RavebridgeLoot.Find(l.Key);
                if (item != null) rows.Add((item, RavebridgeLoot.Count(player, l.Key), l.Value));
            }
            list.Add((quest.name.Get(), NpcName(step.npc), rows));
        }
        return list;
    }

    /// <summary>
    /// Loot came into the bag: a loot step that now has all it wants says so, once (the bar's notice), pointing back
    /// to the one who asked.
    /// </summary>
    public static void LootAdded(PlayerController player, string key, long before)
    {
        if (!player || key == null) return;
        var state = StateOf(player);
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at <= 0 || at >= quest.steps.Count || quest.steps[at].type != "loot") continue;
            var step = quest.steps[at];
            if (step.loot == null || !step.loot.TryGetValue(key, out var need) || before >= need) continue;
            if (HasLoot(player, step.loot)) Notice?.Invoke(string.Format(Localization.UI_RAVEBRIDGE_Q_LOOT_READY, quest.name.Get(), NpcName(step.npc)));
        }
    }

    private static void Say(string npc, string line)
    {
        var who = IslandNpc.Find(npc);
        if (who) who.Say(line);
    }

    /// <summary>The next step, skipping collect steps whose items the character already has; the end of the quest.</summary>
    private static void Advance(PlayerController player, Quest quest)
    {
        var state = StateOf(player);
        var at = StepOf(state, quest.key) + 1;
        while (at < quest.steps.Count && quest.steps[at].type == "collect" && Count(state, quest.steps[at].item) >= quest.steps[at].amount) at++;
        if (at >= quest.steps.Count) { Complete(player, quest); return; }
        state.Steps[quest.key] = at;
        if (at == 1) Notice?.Invoke(string.Format(quest.bounty ? Localization.UI_RAVEBRIDGE_Q_BOUNTY_STARTED : Localization.UI_RAVEBRIDGE_Q_STARTED, quest.name.Get()));
        Save(player);
    }

    private static void Complete(PlayerController player, Quest quest)
    {
        var state = StateOf(player);
        var reward = quest.reward;
        if (quest.bounty)
        {
            state.Steps[quest.key] = 0;
            state.Bounties[quest.key] = (state.Bounties.TryGetValue(quest.key, out var done) ? done : 0) + 1;
            state.BountyReadyAt[quest.key] = DateTime.UtcNow.AddMinutes(quest.cooldown);
        }
        else
        {
            state.Steps[quest.key] = -1;
            state.Points++;
        }
        if (reward?.exp != null)
        {
            foreach (var e in reward.exp)
            {
                if (!Enum.TryParse<Skill>(e.Key, out var skill)) continue;
                var stat = player.Stats.GetSkill(skill);
                if (stat != null) stat.AddExp(e.Value, out _);
            }
        }
        // the Ravebridge character's coins and items are its own; the streamer's characters keep the server's
        if (player.IsLocal && reward != null)
        {
            player.Resources.Coins += reward.coins;
            if (reward.items != null) foreach (var i in reward.items) RavebridgeCharacter.Give(player, i.Key, i.Value);
        }
        // the Ravebridge bag's, for anyone
        if (reward != null)
        {
            if (reward.ravebridgeCoins > 0) RavebridgeLoot.Pick(player, null, reward.ravebridgeCoins);
            if (reward.loot != null) foreach (var l in reward.loot) RavebridgeLoot.Pick(player, RavebridgeLoot.Find(l.Key), l.Value);
        }
        Save(player);
        if (quest.bounty)
        {
            Notice?.Invoke(string.Format(Localization.UI_RAVEBRIDGE_Q_BOUNTY_DONE, quest.name.Get(), reward?.ravebridgeCoins ?? 0));
            return;
        }
        var line = string.Format(Localization.UI_RAVEBRIDGE_Q_DONE, quest.name.Get(), state.Points, QuestCount);
        Notice?.Invoke(line);
        GameManager.Instance?.RavenBot?.Announce(Localization.MSG_RAVEBRIDGE_QUEST_DONE, player.Name, quest.name.Get(), state.Points, QuestCount);
    }

    // ---- Collecting -------------------------------------------------------------------------------------

    /// <summary>Every frame from RavebridgeManager: what the character played by hand does feeds its collect steps.</summary>
    public static void Tick()
    {
        var player = RavebridgePlay.Playing;
        if (player != tracked)
        {
            tracked = player;
            lastExperience.Clear();
            lastEnemy = null;
        }
        if (!player || !player.InRavebridge) return;

        foreach (var skill in CollectSkills)
        {
            var stat = player.Stats[skill];
            if (stat == null) continue;
            var exp = stat.Experience;
            if (lastExperience.TryGetValue(skill, out var before) && exp > before) Collect(player, skill.ToString(), null);
            lastExperience[skill] = exp;
        }

        var enemy = player.taskTarget as EnemyController;
        if (enemy) lastEnemy = enemy;
        if (lastEnemy && lastEnemy.Stats.IsDead)
        {
            Collect(player, null, lastEnemy.name.ToLowerInvariant());
            lastEnemy = null;
        }
    }

    private static readonly Skill[] CollectSkills = { Skill.Woodcutting, Skill.Mining, Skill.Fishing, Skill.Farming, Skill.Gathering };

    private static void Collect(PlayerController player, string skill, string killed)
    {
        var state = StateOf(player);
        foreach (var quest in Data.quests)
        {
            var at = StepOf(state, quest.key);
            if (at <= 0 || at >= quest.steps.Count) continue;
            var step = quest.steps[at];
            if (step.type != "collect") continue;
            var matches = skill != null ? step.skill == skill : step.kill != null && killed != null && killed.Contains(step.kill);
            if (!matches || UnityEngine.Random.value > step.chance) continue;

            state.Items[step.item] = Count(state, step.item) + 1;
            Notice?.Invoke(string.Format(Localization.UI_RAVEBRIDGE_Q_FOUND, ItemName(step.item), Math.Min(Count(state, step.item), step.amount), step.amount));
            if (Count(state, step.item) >= step.amount) Advance(player, quest);
            else Save(player);
        }
    }
}
