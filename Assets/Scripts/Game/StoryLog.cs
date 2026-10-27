using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;
using UnityEngine;

/// <summary>
/// The streamer's quest log (phase 5 of docs/quests-design.md): story lines given to the streamer as the village's
/// leader, the Ironhill runestone and the banana island first. The viewers do the work (gathering, bringing items with
/// !quest give, raids, dungeons, finding things); the streamer talks to the NPC and makes the choices, or lets chat
/// vote (!quest vote 2). This keeps what the server last said, asks again now and then (StreamQuestView drives that),
/// runs the chat votes, words the stories, applies the village perks to experience, and shows the stories in the
/// world: the runestone stirs and wakes, and the banana sign changes when the monkeys have enough.
/// </summary>
public static class StoryLog
{
    public const int NotBegun = 0, Active = 1, Done = 2, GivenUp = 4;
    public const float VoteSeconds = 60f;
    private const float PerkBonus = 0.05f;
    private const float MangroveBonus = 0.10f;

    private const float ActiveRefreshSeconds = 10f;
    private const float IdleRefreshSeconds = 60f;
    private const string RunestonePath = "============ WORLD/Islands/Ironhill Island/Runestone Event";
    private const string BananaSignPath = "============ UI/Canvas3D/unavailable/Text (TMP)";
    private const float RunestoneMarkHeight = 3f;
    private const float BananaMarkHeight = 7f;
    private const float BananaMarkSize = 6f;

    /// <summary>The forge-master at Ironhill's dwarf hold: an IslandNpc with this key, who gives the runestone's story.</summary>
    public const string SmithKey = "ironhill_smith";

    private static bool refreshing;
    private static float nextRefresh;

    public static VillageLogData Latest { get; private set; }
    public static event Action Changed;

    // ---- The chat vote on a choice -------------------------------------------------------------------

    public static string VoteStory { get; private set; }
    public static float VoteEnds { get; private set; }
    private static readonly Dictionary<string, int> votes = new Dictionary<string, int>();

    public static bool Voting => VoteStory != null;

    /// <summary>How many voted for each option (from 1).</summary>
    public static int VotesFor(int option)
    {
        var n = 0;
        foreach (var v in votes.Values) if (v == option) n++;
        return n;
    }

    // ---- State --------------------------------------------------------------------------------------

    public static VillageStoryData Story(string key)
    {
        if (Latest?.Stories == null) return null;
        foreach (var s in Latest.Stories) if (s.Key == key) return s;
        return null;
    }

    public static VillageStoryData ActiveStory
    {
        get
        {
            if (Latest?.Stories == null) return null;
            foreach (var s in Latest.Stories) if (s.State == Active) return s;
            return null;
        }
    }

    public static bool HasUnlock(string key)
    {
        if (Latest?.Unlocks == null) return false;
        foreach (var u in Latest.Unlocks) if (u == key) return true;
        return false;
    }

    public static void Tick(GameManager game, float now)
    {
        if (Voting && Time.unscaledTime >= VoteEnds) _ = EndVoteAsync(game);
        if (now < nextRefresh || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        nextRefresh = now + (ActiveStory != null ? ActiveRefreshSeconds : IdleRefreshSeconds);
        _ = RefreshAsync(game);
    }

    public static void RefreshSoon() => nextRefresh = 0f;

    public static async Task RefreshAsync(GameManager game)
    {
        if (refreshing || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        refreshing = true;
        try
        {
            var log = await game.RavenNest.Quests.GetVillageAsync();
            if (log != null) Set(log);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The village stories could not be loaded: " + exc.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    private static void Set(VillageLogData log)
    {
        Latest = log;
        ApplyWorld();
        Changed?.Invoke();
    }

    // ---- What the streamer does ---------------------------------------------------------------------

    public static async Task<VillageStoryResultData> BeginAsync(GameManager game, string key)
    {
        var result = await game.RavenNest.Quests.BeginStoryAsync(key);
        if (result != null && result.Ok && result.Story != null)
            game.RavenBot.Announce(Localization.MSG_STORY_BEGUN, Name(key), Line(key, 0), Task(result.Story));
        await RefreshAsync(game);
        return result;
    }

    public static async Task<VillageStoryResultData> TalkAsync(GameManager game, string key)
    {
        var result = await game.RavenNest.Quests.TalkStoryAsync(key);
        await RefreshAsync(game);
        return result;
    }

    public static async Task<VillageStoryResultData> ChooseAsync(GameManager game, string key, int option)
    {
        if (VoteStory == key) StopVote();
        var result = await game.RavenNest.Quests.ChooseStoryAsync(key, option);
        await RefreshAsync(game);
        return result;
    }

    public static async Task<VillageStoryResultData> AbandonAsync(GameManager game, string key)
    {
        if (VoteStory == key) StopVote();
        var result = await game.RavenNest.Quests.AbandonStoryAsync(key);
        await RefreshAsync(game);
        return result;
    }

    /// <summary>Lets chat vote on the story's choice for a minute; the most votes wins (the first option on a tie).</summary>
    public static void StartVote(GameManager game, VillageStoryData story)
    {
        if (story == null || story.Kind != "choice" || Voting) return;
        VoteStory = story.Key;
        VoteEnds = Time.unscaledTime + VoteSeconds;
        votes.Clear();
        var options = new List<string>();
        for (var i = 1; i <= story.Options; ++i) options.Add(i + ") " + Option(story.Key, i));
        game.RavenBot.Announce(Localization.MSG_STORY_VOTE, Npc(story.Key), (int)VoteSeconds, story.Options, string.Join(", ", options));
        Changed?.Invoke();
    }

    /// <summary>A viewer's vote (!quest vote 2); a new vote from the same viewer replaces the old one.</summary>
    public static bool Vote(string userId, int option)
    {
        var story = VoteStory != null ? Story(VoteStory) : null;
        if (story == null || option < 1 || option > story.Options || string.IsNullOrEmpty(userId)) return false;
        votes[userId] = option;
        Changed?.Invoke();
        return true;
    }

    private static async Task EndVoteAsync(GameManager game)
    {
        var key = VoteStory;
        var story = Story(key);
        var best = 1;
        var bestVotes = -1;
        for (var i = 1; story != null && i <= story.Options; ++i)
        {
            var n = VotesFor(i);
            if (n > bestVotes) { best = i; bestVotes = n; }
        }
        StopVote();
        if (story == null) return;

        game.RavenBot.Announce(Localization.MSG_STORY_VOTE_RESULT, Option(key, best), Math.Max(0, bestVotes));
        await ChooseAsync(game, key, best);
    }

    private static void StopVote()
    {
        VoteStory = null;
        votes.Clear();
        Changed?.Invoke();
    }

    // ---- Perks ---------------------------------------------------------------------------------------

    /// <summary>
    /// The village perks' share of experience in a skill: 1, or a little more; and on the mangrove island, open to
    /// every level, its own bonus (decided 2026-10-25).
    /// </summary>
    public static float ExpFactor(Skill skill, IslandController island = null)
    {
        var factor = island && island.Island == IslandController.MangroveIsland ? 1f + MangroveBonus : 1f;
        if (Latest?.Unlocks == null || Latest.Unlocks.Length == 0) return factor;
        if (HasUnlock("perk_runestone_1") && IsCombat(skill)) factor += PerkBonus;
        if (HasUnlock("perk_runestone_2") && (skill == Skill.Crafting || skill == Skill.Cooking || skill == Skill.Alchemy)) factor += PerkBonus;
        if (HasUnlock("perk_runestone_3") && IsGathering(skill)) factor += PerkBonus;
        if (HasUnlock("perk_bananas") && (skill == Skill.Fishing || skill == Skill.Gathering)) factor += PerkBonus;
        return factor;
    }

    private static bool IsCombat(Skill s) =>
        s == Skill.Attack || s == Skill.Defense || s == Skill.Strength || s == Skill.Health || s == Skill.Melee
        || s == Skill.Ranged || s == Skill.Magic || s == Skill.Healing || s == Skill.Slayer;

    private static bool IsGathering(Skill s) =>
        s == Skill.Woodcutting || s == Skill.Mining || s == Skill.Fishing || s == Skill.Farming || s == Skill.Gathering;

    // ---- The world -----------------------------------------------------------------------------------

    private static TMPro.TMP_Text runestoneText, bananaSign;
    private static IslandNpc smith;
    private static string runestoneOriginal, bananaOriginal;
    private static ParticleSystem runestoneEffect;
    private static float runestoneRate = -1f;

    /// <summary>The runestone's words and glow by how far its story is, and the banana sign once the monkeys have enough.</summary>
    public static void ApplyWorld()
    {
        try
        {
            if (!smith)
            {
                smith = IslandNpc.Find(SmithKey);
                if (smith)
                {
                    smith.Lines = SmithLines;
                    smith.MarkSource = SmithMark;
                }
            }

            if (!runestoneText)
            {
                var rune = GameObject.Find(RunestonePath);
                if (rune)
                {
                    runestoneText = rune.GetComponentInChildren<TMPro.TMP_Text>(true);
                    runestoneEffect = rune.GetComponentInChildren<ParticleSystem>(true);
                    if (runestoneText) runestoneOriginal = runestoneText.text;
                    if (runestoneEffect) runestoneRate = runestoneEffect.emission.rateOverTimeMultiplier;
                    WorldMark.Attach(rune, RunestoneMarkHeight, RunestoneMark);
                }
            }

            var story = Story("runestone");
            if (runestoneText)
            {
                runestoneText.text = story == null || story.State == NotBegun || story.State == GivenUp ? runestoneOriginal
                    : story.State == Done ? Localization.UI_STORY_RUNESTONE_SIGN_DONE
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_RUNESTONE_SIGN_ACTIVE, story.Step, story.Steps);
            }
            if (runestoneEffect && runestoneRate >= 0f)
            {
                var emission = runestoneEffect.emission;
                var share = story == null ? 0f : story.State == Done ? 1f : story.State == Active ? (float)story.Step / Mathf.Max(1, story.Steps) : 0f;
                emission.rateOverTimeMultiplier = runestoneRate * (1f + share * 2f);
            }

            if (!bananaSign)
            {
                var sign = GameObject.Find(BananaSignPath);
                if (sign)
                {
                    bananaSign = sign.GetComponent<TMPro.TMP_Text>();
                    if (bananaSign) bananaOriginal = bananaSign.text;
                    // the sign is metres tall, made to be read from the overview, so the mark is too
                    WorldMark.Attach(sign, BananaMarkHeight, () => MarkFor("bananas"), BananaMarkSize);
                }
            }
            if (bananaSign) bananaSign.text = HasUnlock("island_mangrove") ? Localization.UI_STORY_BANANAS_SIGN_DONE : bananaOriginal;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The stories could not be shown in the world: " + exc.Message);
        }
    }

    /// <summary>
    /// The mark over a story's place in the world: a gold ! while it can be begun (no other story under way), a gold
    /// ? when the streamer has to talk or choose there, a grey ? while chat works on it.
    /// </summary>
    public static IslandNpc.Mark MarkFor(string key)
    {
        var story = Story(key);
        if (story == null) return IslandNpc.Mark.None;
        if (story.State == Active)
            return story.Kind == "talk" || story.Kind == "choice" ? IslandNpc.Mark.Ready : IslandNpc.Mark.Waiting;
        if (story.State == NotBegun || story.State == GivenUp)
            return ActiveStory == null ? IslandNpc.Mark.Available : IslandNpc.Mark.None;
        return IslandNpc.Mark.None;
    }

    /// <summary>The runestone's own mark: the forge-master gives its story, so the gold ! to begin it is over them.</summary>
    private static IslandNpc.Mark RunestoneMark()
    {
        var key = IsDone("runestone") ? "roadwest" : "runestone";
        var mark = MarkFor(key);
        return key == "runestone" && mark == IslandNpc.Mark.Available && smith ? IslandNpc.Mark.None : mark;
    }

    // ---- The forge-master ----------------------------------------------------------------------------

    public static IslandNpc Smith => smith;

    /// <summary>
    /// Over the forge-master: the gold ! while the runestone's story can begin, and a grey ? while the stream brings
    /// the iron and coal for its bands.
    /// </summary>
    private static IslandNpc.Mark SmithMark()
    {
        var rune = Story("runestone");
        if (rune == null) return IslandNpc.Mark.None;
        if (MarkFor("runestone") == IslandNpc.Mark.Available) return IslandNpc.Mark.Available;
        return rune.State == Active && rune.Kind == "deliver" ? IslandNpc.Mark.Waiting : IslandNpc.Mark.None;
    }

    /// <summary>The forge-master's lines: their own, and twice as often what they think of the stories so far.</summary>
    private static IReadOnlyList<string> SmithLines()
    {
        var lines = new List<string>(Localization.UI_IRONHILL_SMITH_LINES.Split('|'));
        var stage = SmithStage();
        if (!string.IsNullOrEmpty(stage))
        {
            var said = stage.Split('|');
            lines.AddRange(said);
            lines.AddRange(said);
        }
        return lines;
    }

    /// <summary>What the forge-master says about the runestone and the road west, by how far they are.</summary>
    private static string SmithStage()
    {
        var rune = Story("runestone");
        if (rune == null) return null;
        if (rune.State == Active) return Lookup("UI_IRONHILL_SMITH_STEP_" + (rune.Step + 1), null);
        if (rune.State != Done) return Localization.UI_IRONHILL_SMITH_BEGIN;
        var west = Story("roadwest");
        if (west != null && west.State == Active) return Localization.UI_IRONHILL_SMITH_WEST;
        if (west != null && west.State == Done) return Localization.UI_IRONHILL_SMITH_WEST_DONE;
        return Localization.UI_IRONHILL_SMITH_AFTER;
    }

    /// <summary>A click on the forge-master: they say where the story stands, and the window opens on it.</summary>
    public static string SmithClicked()
    {
        if (smith)
        {
            var said = (SmithStage() ?? Localization.UI_IRONHILL_SMITH_LINES).Split('|');
            smith.Say(said[UnityEngine.Random.Range(0, said.Length)]);
        }
        return IsDone("runestone") ? "roadwest" : "runestone";
    }

    // ---- Words ---------------------------------------------------------------------------------------

    public static string Name(string key) => Lookup("UI_STORY_" + key, key);

    /// <summary>Whether the streamer has told a story to its end (the road west begins after the runestone).</summary>
    public static bool IsDone(string key)
    {
        if (Latest?.Stories == null) return false;
        foreach (var s in Latest.Stories) if (s.Key == key && s.State == Done) return true;
        return false;
    }
    public static string Npc(string key) => Lookup("UI_STORY_" + key + "_NPC", key);
    public static string Hook(string key) => Lookup("UI_STORY_" + key + "_HOOK", string.Empty);
    public static string Line(string key, int step) => Lookup("UI_STORY_" + key + "_" + (step + 1), string.Empty);
    public static string Ending(string key) => Lookup("UI_STORY_" + key + "_END", string.Empty);
    public static string Thing(string key) => Lookup("UI_STORY_" + key + "_THING", string.Empty);
    public static string Option(string key, int option) => Lookup("UI_STORY_" + key + "_CHOICE_" + option, option.ToString(CultureInfo.InvariantCulture));
    public static string UnlockName(string key) => Lookup("UI_UNLOCK_" + key, key);

    /// <summary>What the current step asks, with its progress where it counts.</summary>
    public static string Task(VillageStoryData s)
    {
        if (s == null) return string.Empty;
        var amount = Number(s.Amount);
        string text;
        switch (s.Kind)
        {
            case "talk": text = string.Format(Localization.UI_STORY_TASK_TALK, Npc(s.Key)); break;
            case "deliver": text = string.Format(Localization.UI_STORY_TASK_DELIVER, amount, s.Target); break;
            case "find": text = string.Format(Lookup("UI_STORY_" + s.Key + "_FIND", "{0}"), amount); break;
            case "viewers": text = string.Format(Localization.UI_STORY_TASK_VIEWERS, amount); break;
            case "choice": return Localization.UI_STORY_TASK_CHOICE;
            case "kill": text = string.Format(Localization.UI_QUEST_KILL, amount); break;
            case "raid": text = string.Format(s.Amount == 1 ? Localization.UI_QUEST_RAID_ONE : Localization.UI_QUEST_RAID, amount); break;
            case "dungeon": text = string.Format(s.Amount == 1 ? Localization.UI_QUEST_DUNGEON_ONE : Localization.UI_QUEST_DUNGEON, amount); break;
            default: text = s.Kind; break;
        }
        return s.Kind == "talk" ? text : text + " (" + Number(s.Progress) + "/" + amount + ")";
    }

    public static string Unlocks(string[] keys)
    {
        if (keys == null || keys.Length == 0) return Localization.UI_QUEST_NONE;
        var names = new List<string>();
        foreach (var k in keys) names.Add(UnlockName(k));
        return string.Join(", ", names);
    }

    public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Lookup(string name, string fallback)
    {
        var field = typeof(Localization).GetField(name.ToUpperInvariant(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return field?.GetValue(null) as string ?? fallback;
    }
}
