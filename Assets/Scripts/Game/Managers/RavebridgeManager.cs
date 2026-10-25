using System;
using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The Ravebridge event (phase 6b of docs/ravebridge-design.md): the streamer starts it where the stream has the
/// island_ravebridge unlock (or a world event forces it, unlocked or not), viewers join with !ravebridge and are taken
/// to the island, keep training there, and are brought back where they were when they leave or the streamer ends it.
/// One event at a time (GameEventManager). A forced event nobody looks after ends by itself after half an hour.
/// Viewers can help the streamer's character on purpose: !ravebridge follow fights its fights, !ravebridge heal heals it.
/// </summary>
public class RavebridgeManager : MonoBehaviour, IEvent
{
    public const string Unlock = "island_ravebridge";
    private const float ForcedTimeout = 30f * 60f;
    private const float UprisingRollInterval = 10f * 60f;
    private const int UprisingChance = 36; // one in 36 rolls: about every six hours of streaming

    public enum Help { None, Follow, Heal }

    private sealed class Visit
    {
        public PrevPlayerState Before;
        public Help Help;
        public float NextHelp;
    }

    private readonly Dictionary<Guid, Visit> visits = new Dictionary<Guid, Visit>();
    private readonly List<PlayerController> leaving = new List<PlayerController>();

    private GameManager game;
    private IslandController island;
    private float nextUprisingRoll;
    private string pendingForcedReason;

    public static RavebridgeManager Instance { get; private set; }

    public string EventName => "Ravebridge";
    public bool IsEventActive => Active;
    public bool Active { get; private set; }

    /// <summary>Pulled here by a world event rather than started by the streamer.</summary>
    public bool Forced { get; private set; }
    public string ForcedReason { get; private set; }
    public float StartedAt { get; private set; }

    /// <summary>The streamer took part (played, joined or ended a timer): a forced event no longer ends by itself.</summary>
    public bool Attended { get; set; }

    /// <summary>The character the streamer plays here, by hand (6c) or their own character on the island.</summary>
    public PlayerController Hero { get; set; }

    public int PlayerCount => visits.Count;
    public float SecondsLeft => Forced && !Attended ? Mathf.Max(0f, ForcedTimeout - (Time.time - StartedAt)) : -1f;

    public static event Action Changed;

    public IslandController Island
    {
        get
        {
            if (!island && game && game.Islands != null) island = game.Islands.Get(IslandController.RavebridgeIsland);
            return island;
        }
    }

    /// <summary>Whether this stream may start Ravebridge itself (forced events go anyway).</summary>
    public static bool Unlocked => StoryLog.HasUnlock(Unlock);

    private void Awake()
    {
        Instance = this;
        game = GetComponent<GameManager>();
        nextUprisingRoll = Time.time + UprisingRollInterval;
    }

    public bool Contains(PlayerController player) => player && visits.ContainsKey(player.Id);

    public Help HelpOf(PlayerController player) => player && visits.TryGetValue(player.Id, out var v) ? v.Help : Help.None;

    /// <summary>Where a player on Ravebridge came from: what saves send, so a crash brings them home.</summary>
    public bool TryGetReturn(PlayerController player, out IslandController from, out Vector3 position)
    {
        from = null;
        position = default;
        if (!player || !visits.TryGetValue(player.Id, out var v) || v.Before == null) return false;
        from = v.Before.Island;
        position = v.Before.Position;
        return from;
    }

    // ---- Starting and ending ---------------------------------------------------------------------------

    public enum StartResult { Started, AlreadyActive, Locked, Busy, NoIsland }

    /// <summary>
    /// The streamer (or a moderator) starts it; a forced start comes from a world event and waits out the events'
    /// cooldown, unless now (the debug panel's button).
    /// </summary>
    public StartResult Begin(bool forced, string reason = null, bool now = false)
    {
        if (Active) return StartResult.AlreadyActive;
        if (!Island) return StartResult.NoIsland;
        if (!forced && !Unlocked) return StartResult.Locked;
        if (game.StreamRaid && game.StreamRaid.Started) return StartResult.Busy;
        if (!game.Events.TryStart(this, !forced || now))
        {
            // a forced event waits for the running one to end
            if (forced) pendingForcedReason = reason ?? string.Empty;
            return StartResult.Busy;
        }

        Active = true;
        Forced = forced;
        ForcedReason = reason;
        Attended = !forced;
        StartedAt = Time.time;
        pendingForcedReason = null;
        Hero = null;

        var line = forced ? (string.IsNullOrEmpty(reason) ? Localization.MSG_RAVEBRIDGE_FORCED : reason) : Localization.MSG_RAVEBRIDGE_STARTED;
        game.RavenBot?.Announce(line);
        var banner = FindAnyObjectByType<Shinobytes.UI.NotificationScreenView>(FindObjectsInactive.Include);
        if (banner)
        {
            // the chat line without the bot's routing tags (%[category:...]%%[tags:...]%)
            var readable = MessageUtilities.TryExtractTags(MessageUtilities.TryExtractCategory(line, out _), out _);
            banner.Show(Localization.UI_RAVEBRIDGE_TITLE, readable, Shinobytes.UI.NotificationScreenView.WithCommand(Localization.UI_RAVEBRIDGE_JOIN, "!ravebridge"));
        }

        Changed?.Invoke();
        return StartResult.Started;
    }

    /// <summary>Everyone goes back where they came from.</summary>
    public void End()
    {
        if (!Active) return;
        leaving.Clear();
        foreach (var player in game.Players.GetAllPlayers())
        {
            if (Contains(player)) leaving.Add(player);
        }
        foreach (var player in leaving) Leave(player);
        visits.Clear();
        Active = false;
        Forced = false;
        Hero = null;
        game.Events.End(this);
        game.RavenBot?.Announce(Localization.MSG_RAVEBRIDGE_ENDED);
        Changed?.Invoke();
    }

    // ---- Joining and leaving ---------------------------------------------------------------------------

    public enum JoinResult { Joined, NotActive, AlreadyThere, Busy, Dead }

    public JoinResult Join(PlayerController player)
    {
        if (!Active || !Island) return JoinResult.NotActive;
        if (Contains(player)) return JoinResult.AlreadyThere;
        if (player.Stats.IsDead) return JoinResult.Dead;
        if (player.dungeonHandler.InDungeon || player.raidHandler.InRaid || player.streamRaidHandler.InWar
            || player.arenaHandler.InArena || player.duelHandler.InDuel)
        {
            return JoinResult.Busy;
        }

        visits[player.Id] = new Visit { Before = new PrevPlayerState(player) };
        player.InRavebridge = true;
        if (player.IsBroadcaster) Attended = true;

        // in through the castle gate, a little apart from each other
        var spawn = Island.SpawnPosition + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f));
        player.teleportHandler.Teleport(spawn);
        player.Island = Island;
        Island.AddPlayer(player);
        var task = player.GetTask();
        if (task != TaskType.None) player.SetTask(task, player.GetTaskArgument(), true);

        Changed?.Invoke();
        return JoinResult.Joined;
    }

    /// <summary>Back where they were: their island and spot (or the ferry), their task, the onsen.</summary>
    public void Leave(PlayerController player)
    {
        if (!player || !visits.TryGetValue(player.Id, out var visit)) return;
        visits.Remove(player.Id);
        player.InRavebridge = false;

        // the Ravebridge character never leaves the island: it is saved and put away
        if (player.IsLocal)
        {
            if (Hero == player) Hero = null;
            if (RavebridgePlay.Playing == player) RavebridgePlay.Stop();
            RavebridgeCharacter.Despawn();
            Changed?.Invoke();
            return;
        }

        if (Hero == player) Hero = null;
        if (RavebridgePlay.Playing == player) RavebridgePlay.Stop();

        var before = visit.Before;
        player.taskTarget = null;
        if (before.Ferry.OnFerry)
        {
            player.Movement.Lock();
            player.ferryHandler.AddPlayerToFerry(before.Ferry.Destination);
        }
        else if (before.Island)
        {
            player.teleportHandler.Teleport(before.Position);
        }
        else
        {
            var home = game.Islands.Find("home");
            if (home) player.teleportHandler.Teleport(home.SpawnPosition);
        }

        var task = before.TrainingTask ?? TaskType.None;
        if (task != TaskType.None)
        {
            player.SetTask(task, before.TrainingTaskArgument, true);
            if (!before.Ferry.OnFerry) player.GotoClosest(task, true);
        }
        if (before.Ferry.State == PlayerFerryState.Embarking) player.ferryHandler.Embark(before.Ferry.Destination);
        if (before.Resting) game.Onsen.Join(player);
        Changed?.Invoke();
    }

    /// <summary>The player left the game: nothing to bring back, only forget them.</summary>
    public void Forget(PlayerController player)
    {
        if (!player) return;
        if (RavebridgePlay.Playing == player) RavebridgePlay.Stop();
        visits.Remove(player.Id);
        player.InRavebridge = false;
        if (Hero == player) Hero = null;
        Changed?.Invoke();
    }

    public bool SetHelp(PlayerController player, Help help)
    {
        if (!player || !visits.TryGetValue(player.Id, out var visit)) return false;
        visit.Help = help;
        if (help == Help.Heal) player.SetTask(TaskType.Fighting, "healing", true);
        else if (help == Help.None)
        {
            var task = player.GetTask();
            if (task != TaskType.None) player.SetTask(task, player.GetTaskArgument(), true);
        }
        return true;
    }

    // ---- Every frame -----------------------------------------------------------------------------------

    private void Update()
    {
        if (!game || game.Players == null) return;

        RavebridgeCharacter.Tick();
        RavebridgeQuests.Tick();

        if (!Active)
        {
            if (pendingForcedReason != null && !game.Events.IsActive) Begin(true, pendingForcedReason.Length > 0 ? pendingForcedReason : null);
            RollUprising();
            return;
        }

        // a forced event nobody looks after ends by itself
        if (Forced && !Attended && Time.time - StartedAt > ForcedTimeout)
        {
            End();
            return;
        }

        if (!Hero || !Contains(Hero))
        {
            Hero = null;
            foreach (var player in game.Players.GetAllPlayers())
            {
                if (player.IsBroadcaster && Contains(player)) { Hero = player; break; }
            }
        }
    }

    /// <summary>The goblins of Ravebridge rise, now and then, in any stream with players: a forced event.</summary>
    private void RollUprising()
    {
        if (Time.time < nextUprisingRoll) return;
        nextUprisingRoll = Time.time + UprisingRollInterval;
        if (game.Events.IsActive || game.Players.GetPlayerCount() < 3) return;
        if (UnityEngine.Random.Range(0, UprisingChance) != 0) return;
        Begin(true, Localization.MSG_RAVEBRIDGE_UPRISING);
    }

    /// <summary>
    /// A helper's turn, from PlayerController.Update: true when the helper acted (the automatic task waits). Followers
    /// fight what the streamer's character fights and stay close otherwise; healers heal it when it is hurt.
    /// </summary>
    public bool UpdateHelper(PlayerController player)
    {
        if (!visits.TryGetValue(player.Id, out var visit) || visit.Help == Help.None) return false;
        var hero = Hero;
        if (!hero || hero == player || hero.Stats.IsDead || player.Stats.IsDead) return false;
        if (Time.time < visit.NextHelp) return true;
        visit.NextHelp = Time.time + 0.25f;

        var range = player.GetAttackRange();
        if (visit.Help == Help.Heal && hero.Stats.HealthPercent < 0.9f)
        {
            if (Vector3.Distance(player.Position, hero.Position) > range) { player.SetDestination(hero.Position); return true; }
            if (player.IsReadyForAction) player.Heal(hero);
            return true;
        }

        var enemy = hero.Target ? hero.Target.GetComponent<EnemyController>() : null;
        if (visit.Help == Help.Follow && enemy && !enemy.Stats.IsDead)
        {
            if (Vector3.Distance(player.Position, enemy.Position) > range) { player.SetDestination(enemy.Position); return true; }
            if (player.IsReadyForAction) player.Attack(enemy);
            return true;
        }

        // nothing to do: stay close
        if (Vector3.Distance(player.Position, hero.Position) > 6f) player.SetDestination(hero.Position);
        return true;
    }
}
