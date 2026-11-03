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
    private readonly List<GameObject> scenery = new List<GameObject>();
    private readonly List<EnemyController> respawning = new List<EnemyController>();
    private bool sceneryHidden;
    private float unusedSince = -1f;
    private float nextPresenceCheck;

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
        // the scenery cows become real ones before the fighting areas count their enemies (Chunk.Start)
        foreach (var i in FindObjectsByType<IslandController>(FindObjectsInactive.Include))
            if (i.Island == IslandController.RavebridgeIsland) SceneryAnimals.MakeFightable(i);
    }

    public bool Contains(PlayerController player) => player && visits.ContainsKey(player.Id);

    /// <summary>
    /// One of the streamer's own characters: the broadcaster's, or the session owner's (a character restored after a
    /// restart comes back without the broadcaster flag).
    /// </summary>
    public static bool IsStreamers(PlayerController player)
    {
        if (!player || player.IsLocal) return false;
        if (player.IsBroadcaster) return true;
        var game = GameManager.Instance;
        return game && game.RavenNest != null && player.UserId != System.Guid.Empty && player.UserId == game.RavenNest.UserId;
    }

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
    /// <param name="ask">
    /// The "Who plays?" window opens at once and carries the opening for chat too (how to join), so no banner (the
    /// user, 2026-11-03: one window, not a banner and then the window). False where the streamer is played anyway or
    /// is away (HeroStart): the banner says it, a window would only flash or sit over the stream.
    /// </param>
    public StartResult Begin(bool forced, string reason = null, bool now = false, bool ask = true)
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

        ShowIsland();
        Active = true;
        Forced = forced;
        ForcedReason = reason;
        Attended = !forced;
        StartedAt = Time.time;
        pendingForcedReason = null;
        Hero = null;

        var line = forced ? (string.IsNullOrEmpty(reason) ? Localization.MSG_RAVEBRIDGE_FORCED : reason) : Localization.MSG_RAVEBRIDGE_STARTED;
        game.RavenBot?.Announce(line);
        // the chat line without the bot's routing tags (%[category:...]%%[tags:...]%), for the window or the banner
        var readable = MessageUtilities.TryExtractTags(MessageUtilities.TryExtractCategory(line, out _), out _);
        OpeningLine = readable.Replace("!ravebridge", "{command}");
        Ask = ask;
        var banner = ask ? null : FindAnyObjectByType<Shinobytes.UI.NotificationScreenView>(FindObjectsInactive.Include);
        if (banner)
        {
            // the join line only when the announcement does not already say the command
            var said = readable.Contains("!ravebridge");
            var join = said ? null : Shinobytes.UI.NotificationScreenView.WithCommand(Localization.UI_RAVEBRIDGE_JOIN, "!ravebridge");
            if (said) readable = Shinobytes.UI.NotificationScreenView.WithCommand(OpeningLine, "!ravebridge");
            banner.Show(Localization.UI_RAVEBRIDGE_TITLE, readable, join);
            StartCoroutine(HideLater(banner, BannerSeconds));
        }

        Changed?.Invoke();
        return StartResult.Started;
    }

    /// <summary>How long the opening banner shows (only when Begin was not to ask who plays).</summary>
    public const float BannerSeconds = 8f;

    /// <summary>How it opened, for the "Who plays?" window: the announcement with {command} where !ravebridge goes.</summary>
    public string OpeningLine { get; private set; }

    /// <summary>The "Who plays?" window opens with the event (Begin's ask).</summary>
    public bool Ask { get; private set; }

    private static System.Collections.IEnumerator HideLater(Shinobytes.UI.NotificationScreenView banner, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (banner) banner.Hide();
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
        if (IsStreamers(player)) Attended = true;

        // in through the castle gate, a little apart from each other
        var spawn = Island.SpawnPosition + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f));
        // onto the navmesh: a few spots by the gate posts are off it, and players put there stood stuck
        if (UnityEngine.AI.NavMesh.SamplePosition(spawn, out var ground, 3f, NavMeshHelper.AreaMaskAt(spawn))) spawn = ground.position;
        else spawn = Island.SpawnPosition;
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
            if (RavebridgePlay.Playing == player) RavebridgePlay.Stop(false);
            RavebridgeCharacter.Despawn();
            Changed?.Invoke();
            return;
        }

        if (Hero == player) Hero = null;
        if (RavebridgePlay.Playing == player) RavebridgePlay.Stop(false);

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
        if (RavebridgePlay.Playing == player) RavebridgePlay.Stop(false);
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
        UpdatePresence();

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
                if (IsStreamers(player) && Contains(player)) { Hero = player; break; }
            }
        }
    }

    // ---- There only while used ------------------------------------------------------------------------

    /// <summary>How long nobody uses Ravebridge before its contents hide.</summary>
    private const float HideAfter = 3f;

    /// <summary>Whether the island's contents are hidden (nobody uses Ravebridge).</summary>
    public bool IslandHidden => sceneryHidden;

    /// <summary>
    /// The island's contents come back: the event opens, or someone is on the island anyway (a character restored
    /// there after a crash). Switching them on takes a moment (about a quarter of a second in the editor).
    /// </summary>
    public void ShowIsland()
    {
        if (!sceneryHidden) return;
        sceneryHidden = false;
        foreach (var part in scenery) if (part) part.SetActive(true);
        scenery.Clear();
        // switching off stopped their respawn timers
        foreach (var enemy in respawning) if (enemy && enemy.Stats.IsDead) enemy.Respawn(1f);
        respawning.Clear();
    }

    /// <summary>
    /// The island's contents hide while nobody uses Ravebridge (the user, 2026-11-03): a third of the world's
    /// renderers, its enemies, animals, NPCs and resource fields, measured at 1.5 to 3 ms of the main thread a frame in
    /// the editor though nobody was near. The island itself stays (its sphere, its navmesh, the world's terrain tiles
    /// parented under it, which reach past Ravebridge), and so does what is slow to switch back on and cheap to keep
    /// (see Keep). Everything switched off that draws or registers itself (resource fields, the NPC list) lets go and
    /// comes back when switched on.
    /// </summary>
    private void HideIsland()
    {
        if (sceneryHidden || !Island) return;
        scenery.Clear();
        CollectScenery(Island.transform, 0);
        // the dead still on their feet wait for a respawn timer (a coroutine), which switching off stops
        respawning.Clear();
        foreach (var part in scenery)
            foreach (var enemy in part.GetComponentsInChildren<EnemyController>(false))
                if (enemy.Stats != null && enemy.Stats.IsDead) respawning.Add(enemy);
        foreach (var part in scenery) part.SetActive(false);
        sceneryHidden = true;
    }

    /// <summary>
    /// What stays on while the island hides: the ground (terrains), its grass, the roads and the river. They cost next to
    /// nothing far away, and switching them on is slow: the roads rebuild their meshes and the grass grows again, which
    /// froze the game for seconds when the event opened.
    /// </summary>
    private static bool Keep(Component c) => c is Terrain || c is GrassField || c is WorldRoad || c is RiverWater;

    private static readonly List<Component> componentsBuffer = new List<Component>();

    /// <summary>The parts to switch off: whole groups, or inside a group that holds something kept, its other parts.</summary>
    private void CollectScenery(Transform parent, int depth)
    {
        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeSelf) continue;
            componentsBuffer.Clear();
            child.GetComponents(componentsBuffer);
            if (componentsBuffer.Exists(Keep)) continue;
            componentsBuffer.Clear();
            child.GetComponentsInChildren(true, componentsBuffer);
            if (!componentsBuffer.Exists(Keep)) scenery.Add(child.gameObject);
            else if (depth < 4) CollectScenery(child, depth + 1);
        }
    }

    private void UpdatePresence()
    {
        if (Time.time < nextPresenceCheck || !game.IsLoaded || !Island) return;
        nextPresenceCheck = Time.time + 0.5f;
        var used = Active;
        if (!used)
        {
            foreach (var player in game.Players.GetAllPlayers())
            {
                if (player && (player.Island == Island || Contains(player))) { used = true; break; }
            }
        }
        if (used)
        {
            unusedSince = -1f;
            ShowIsland();
            return;
        }
        if (unusedSince < 0f) unusedSince = Time.time;
        else if (Time.time - unusedSince >= HideAfter) HideIsland();
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
