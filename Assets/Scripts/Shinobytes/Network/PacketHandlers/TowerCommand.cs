using System;
using System.Globalization;
using RavenNest.Combat;

/// <summary>
/// The tower from chat (docs/tower-design.md; the words and commands as built: docs/tower-worlds-proposal.md sections
/// 12 and 13). The bot sends "tower_command" with the action first: !tower (to the part the climb is on, or the town
/// when everyone waits there between parts), !tower leave, !tower info, !tower town (the floor's town, Mossgate first),
/// !tower talk elm (one of the town's people: their quest chain, or a rumour), !tower map (the floor map on screen for a
/// while), and the choices for everyone, which the streamer or a moderator makes at once and anyone in the tower can
/// vote for (section 16): !tower boss (an attempt on the part's guardian), !tower climb (from the town up to the next
/// part), !tower all part 3 (everyone on the tower's parts goes to a part already reached) and !tower close (everyone
/// out for now). !tower vote climb is the same as !tower climb.
/// A place typed alone takes only the player who types it, the streamer too, to a part the stream has cleared, to
/// train there: !tower part 7 (a part of the floor they are on), !tower floor 2 (that floor's latest part),
/// !tower floor 2 part 7, or short !tower 2-7; !tower train 7 is !tower part 7. !tower part or !tower floor alone is
/// !tower: back to the climb. With all in front (or everyone, group) it is everyone on the tower's parts who goes, as
/// !tower part 7 did before; !tower all alone brings them back to the climb.
/// </summary>
public class TowerCommand : ChatBotCommandHandler<string>
{
    public TowerCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var tower = TowerManager.Instance;
        if (!tower)
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }

        var text = (data ?? "").Trim().ToLowerInvariant();
        // !tower vote climb: the same as !tower climb
        if (text.StartsWith("vote ")) text = text.Substring(5).Trim();
        var space = text.IndexOf(' ');
        var action = space < 0 ? text : text.Substring(0, space);
        var user = gm.Sender;
        var player = PlayerManager.GetPlayer(user);
        var canForce = user.IsBroadcaster || user.IsModerator || (player && (player.IsGameAdmin || player.IsGameModerator));

        // the streamer closed the tower (settings): nobody goes in; those inside can still leave and look
        // (the streamer and moderators can still close it, which sends out anyone left inside)
        var closing = canForce && (action == "close" || action == "quit" || action == "end" || action == "stop");
        if (!SettingsApplier.TowerOpen && !closing && action != "leave" && action != "exit" && action != "info" && action != "status")
        {
            var inside = player && player.towerHandler != null && player.towerHandler.InTower;
            client.SendReply(gm, inside ? Localization.MSG_TOWER_CLOSED_INSIDE : Localization.MSG_TOWER_CLOSED_HERE);
            return;
        }

        try
        {
            switch (action)
            {
                case "leave":
                case "exit":
                    Leave(tower, player, gm, client);
                    return;
                case "boss":
                case "challenge":
                    Boss(tower, player, canForce, gm, client);
                    return;
                case "climb":
                case "up":
                case "continue":
                case "next":
                    Climb(tower, player, canForce, gm, client);
                    return;
                case "close":
                case "quit":
                case "end":
                case "stop":
                    Close(tower, player, canForce, gm, client);
                    return;
                case "info":
                case "status":
                    Info(tower, gm, client);
                    return;
                case "help":
                case "commands":
                case "?":
                    client.SendReply(gm, Localization.MSG_TOWER_HELP);
                    return;
                case "floor":
                case "part":
                case "train":
                    // !tower part 7, !tower floor 2, !tower floor 2 part 7: this player goes to a part the stream has
                    // cleared, to train there. Alone: back to the climb, as !tower
                    if (space < 0) { Join(tower, player, gm, client); return; }
                    Visit(tower, player, (action == "floor" ? "floor " : "part ") + text.Substring(space + 1).Trim(), gm, client);
                    return;
                case "all":
                case "everyone":
                case "group":
                    // !tower all part 7, !tower all 2-7: everyone on the tower's parts goes there (votes but for the
                    // streamer); alone: everyone back to the climb's part
                    MoveAll(tower, player, space < 0 ? "" : text.Substring(space + 1).Trim(), canForce, gm, client);
                    return;
                case "map":
                    Map(tower, player, gm, client);
                    return;
                case "town":
                    // !tower town, or !tower town rimeholt
                    Town(tower, player, space < 0 ? null : RavenNest.Combat.Tower.Town(text.Substring(space + 1).Trim()), space < 0 ? null : text.Substring(space + 1).Trim(), gm, client);
                    return;
                case "rest":
                case "sleep":
                    Rest(tower, player, gm, client);
                    return;
                case "talk":
                case "speak":
                    Talk(player, space < 0 ? "" : text.Substring(space + 1).Trim(), gm, client);
                    return;
                default:
                    // !tower mossgate, !tower rimeholt: that town
                    var named = RavenNest.Combat.Tower.Town(action);
                    if (named != null)
                    {
                        Town(tower, player, named, action, gm, client);
                        return;
                    }
                    // !tower 2-7, !tower 2 7, !tower 7: a place the short way
                    if (text.Length > 0 && char.IsDigit(text[0]))
                    {
                        Visit(tower, player, text, gm, client);
                        return;
                    }
                    Join(tower, player, gm, client);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerCommand: " + exc);
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
        }
    }

    private static void Join(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var floor = Tower.Get(tower.StreamFloor);
        var result = tower.Join(player);
        switch (result)
        {
            case TowerManager.JoinResult.Joined:
            case TowerManager.JoinResult.Elsewhere:
                // the part they are on now, or on their way to
                var at = tower.AreaOf(player);
                var here = at != null ? at.Step : tower.StreamFloor;
                foreach (var a in tower.Areas) if (at == null && a.Waits(player)) here = a.Step;
                var current = Tower.Get(here);
                // a character far below the floor: said, with the way out (levels are only brought down to a floor,
                // never up to it)
                var warning = Warning(player, current);
                if (result == TowerManager.JoinResult.Elsewhere)
                    // no place for the climb's part (potato mode keeps one part open): where everyone trains
                    client.SendReply(gm, Localization.MSG_TOWER_JOINED_TRAINING + warning, TowerWords.Floor(here), TowerWords.Part(here), current != null ? current.Name : "", TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                else if (tower.Phase == TowerManager.TowerPhase.Countdown)
                    client.SendReply(gm, Localization.MSG_TOWER_JOINED_ATTEMPT + warning, TowerWords.Floor(here), TowerWords.Part(here), ((int)tower.PhaseSecondsLeft).ToString(CultureInfo.InvariantCulture));
                else
                    client.SendReply(gm, Localization.MSG_TOWER_JOINED + warning, TowerWords.Floor(here), TowerWords.Part(here), current != null ? current.Name : "", Tower.SyncLevel(here), tower.QuestText());
                return;
            case TowerManager.JoinResult.Loading:
                client.SendReply(gm, Localization.MSG_TOWER_LOADING, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), floor != null ? floor.Name : "");
                return;
            case TowerManager.JoinResult.ToTown:
                client.SendReply(gm, Localization.MSG_TOWER_TOWN_ARRIVED, tower.CurrentTown != null ? tower.CurrentTown.Name : "", TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                return;
            case TowerManager.JoinResult.AlreadyIn:
                Info(tower, gm, client);
                return;
            case TowerManager.JoinResult.FightUnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_FIGHT_UNDER_WAY, UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(tower.PhaseSecondsLeft / 60f)).ToString(CultureInfo.InvariantCulture));
                return;
            case TowerManager.JoinResult.Busy:
                client.SendReply(gm, Localization.MSG_TOWER_CANNOT_JOIN);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
        }
    }

    // a character far below a part: said, with the way out
    private static string Warning(PlayerController player, TowerFloorDefinition part) =>
        part != null && player && player.Stats != null && player.Stats.CombatLevel < part.Level * 0.6f
            ? string.Format(Localization.MSG_TOWER_JOIN_WARNING, part.Level, player.Stats.CombatLevel) : "";

    private static void Town(TowerManager tower, PlayerController player, RavenNest.Combat.TowerTownDefinition wanted, string asked, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (wanted == null && !string.IsNullOrEmpty(asked))
        {
            client.SendReply(gm, Localization.MSG_TOWER_TOWN_UNKNOWN, asked, TownNames(tower));
            return;
        }

        var open = tower.CurrentTown;
        var result = tower.JoinTown(player, wanted);
        var town = wanted ?? tower.CurrentTown;
        switch (result)
        {
            case TowerManager.TownJoinResult.Locked:
                // the town of a floor further up: the floor below it has to be cleared first
                client.SendReply(gm, Localization.MSG_TOWER_TOWN_LOCKED, wanted.Name, Math.Max(1, wanted.Band - 1));
                return;
            case TowerManager.TownJoinResult.OtherOpen:
                client.SendReply(gm, Localization.MSG_TOWER_TOWN_OTHER_OPEN, open != null ? open.Name : "", wanted != null ? wanted.Name : "", wanted != null ? wanted.Key : "");
                return;
            case TowerManager.TownJoinResult.Joined:
            case TowerManager.TownJoinResult.Loading:
                client.SendReply(gm, Localization.MSG_TOWER_TOWN_JOINED, town.Name, People(town));
                return;
            case TowerManager.TownJoinResult.AlreadyIn:
                client.SendReply(gm, Localization.MSG_TOWER_TOWN_HERE, town.Name, People(town));
                return;
            case TowerManager.TownJoinResult.NoTown:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
            default:
                client.SendReply(gm, player.towerHandler != null && player.towerHandler.InBossFight ? Localization.MSG_TOWER_TOWN_FIGHTING : Localization.MSG_TOWER_CANNOT_JOIN);
                return;
        }
    }

    /// <summary>
    ///     !tower rest, and !rest or !onsen in the tower: to the town's resting place (into the town first from a floor),
    ///     rested as in the Onsen.
    /// </summary>
    internal static void Rest(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var handler = player.towerHandler;
        var town = tower.CurrentTown;
        if (handler != null && handler.InTown)
        {
            switch (handler.StartResting())
            {
                case TowerHandler.RestResult.Resting:
                    client.SendReply(gm, Localization.MSG_TOWER_REST, town != null ? town.Name : "");
                    return;
                case TowerHandler.RestResult.AlreadyResting:
                    client.SendReply(gm, Localization.MSG_TOWER_REST_ALREADY);
                    return;
                default:
                    client.SendReply(gm, Localization.MSG_TOWER_REST_FULL);
                    return;
            }
        }

        switch (tower.JoinTown(player))
        {
            case TowerManager.TownJoinResult.Joined:
            case TowerManager.TownJoinResult.Loading:
                if (handler != null)
                {
                    // into the town (now, or once it has loaded), then to the resting place
                    if (handler.InTown) handler.StartResting();
                    else handler.RestOnArrival = true;
                }
                client.SendReply(gm, Localization.MSG_TOWER_REST, town != null ? town.Name : "");
                return;
            case TowerManager.TownJoinResult.NoTown:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
            default:
                client.SendReply(gm, handler != null && handler.InBossFight ? Localization.MSG_TOWER_TOWN_FIGHTING : Localization.MSG_TOWER_CANNOT_JOIN);
                return;
        }
    }

    /// <summary>The towns the stream has reached, for chat: "Mossgate (!tower town mossgate), Rimeholt (...)".</summary>
    private static string TownNames(TowerManager tower)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var t in RavenNest.Combat.Tower.Towns) if (tower.TownUnlocked(t)) parts.Add(t.Name + " (!tower town " + t.Key + ")");
        return string.Join(", ", parts);
    }

    /// <summary>"Warden Elm (!tower talk elm), Bramble (!tower talk bramble), ..."</summary>
    private static string People(TowerTownDefinition town)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var n in town.People) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_TOWN_PERSON, n.Name, n.Role, n.Key));
        return string.Join(", ", parts);
    }

    private async void Talk(PlayerController player, string who, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }
        var tower = TowerManager.Instance;
        var town = TowerTown.Loaded;
        if (!tower || player.towerHandler == null || !player.towerHandler.InTown || !town || town.Town == null)
        {
            client.SendReply(gm, Localization.MSG_TOWER_TALK_NOT_IN_TOWN, tower ? tower.CurrentTown?.Name ?? "" : "");
            return;
        }

        var definition = town.Town;
        TowerNpc npc = null;
        if (!string.IsNullOrEmpty(who))
        {
            foreach (var n in definition.People)
                if (string.Equals(n.Key, who, StringComparison.OrdinalIgnoreCase) || n.Name.IndexOf(who, StringComparison.OrdinalIgnoreCase) >= 0) { npc = n; break; }
        }
        if (npc == null)
        {
            client.SendReply(gm, Localization.MSG_TOWER_TALK_WHO, definition.Name, People(definition));
            return;
        }

        try
        {
            await QuestChat.TalkAsync(Game, player, npc, gm, client);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerCommand.Talk: " + exc);
            client.SendReply(gm, Localization.MSG_QUEST_FAILED);
        }
    }

    private static void Leave(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player || player.towerHandler == null || !player.towerHandler.InTower)
        {
            // waiting to be let in (the floor loading, the opening cutscene): taken back
            if (player && tower.CancelWaiting(player)) client.SendReply(gm, Localization.MSG_TOWER_LEFT_WAITING);
            else client.SendReply(gm, Localization.MSG_TOWER_NOT_IN);
            return;
        }
        tower.Leave(player);
        client.SendReply(gm, Localization.MSG_TOWER_LEFT);
    }

    private static void Boss(TowerManager tower, PlayerController player, bool canForce, GameMessage gm, GameClient client)
    {
        var floor = Tower.Get(tower.StreamFloor);
        switch (tower.CallBoss(player, canForce, out var needed))
        {
            case TowerManager.BossCallResult.Started:
                return; // the countdown is announced
            case TowerManager.BossCallResult.Voted:
                // the first voice is announced to everyone (the vote opened); a later one gets where it stands
                if (!tower.VoteJustOpened) client.SendReply(gm, Localization.MSG_TOWER_BOSS_VOTE, TowerWords.BossKind(tower.StreamFloor), TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), needed, tower.VoteSecondsLeft("boss"));
                return;
            case TowerManager.BossCallResult.NotOpen:
                if (tower.AllCleared) client.SendReply(gm, Localization.MSG_TOWER_ALL_CLEARED);
                else client.SendReply(gm, Localization.MSG_TOWER_BOSS_LOCKED, TowerWords.BossKind(tower.StreamFloor), TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), tower.QuestText());
                return;
            case TowerManager.BossCallResult.UnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_UNDER_WAY);
                return;
            case TowerManager.BossCallResult.Regrouping:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_REGROUPING, ((int)tower.RetrySecondsLeft).ToString(CultureInfo.InvariantCulture));
                return;
            case TowerManager.BossCallResult.NotInTower:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_ENTER_FIRST);
                return;
            default:
                // nobody on the climb's part: on a cleared part to train, in the town between parts, or not in the tower
                if (tower.FightingPlayers().Count > 0) client.SendReply(gm, Localization.MSG_TOWER_BOSS_ELSEWHERE, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                else if (tower.TownPlayers().Count > 0) client.SendReply(gm, Localization.MSG_TOWER_BOSS_IN_TOWN, tower.CurrentTown != null ? tower.CurrentTown.Name : "the town", TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                else client.SendReply(gm, Localization.MSG_TOWER_BOSS_NOBODY, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), floor != null ? floor.Boss.Name : "");
                return;
        }
    }

    private static void Climb(TowerManager tower, PlayerController player, bool canForce, GameMessage gm, GameClient client)
    {
        var floor = Tower.Get(tower.StreamFloor);
        var town = tower.CurrentTown;
        switch (tower.Climb(player, canForce, out var needed))
        {
            case TowerManager.ClimbResult.Climbing:
                client.SendReply(gm, Localization.MSG_TOWER_CLIMB_STARTED, town != null ? town.Name : "", TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), floor != null ? floor.Name : "");
                return;
            case TowerManager.ClimbResult.Voted:
                if (!tower.VoteJustOpened) client.SendReply(gm, Localization.MSG_TOWER_CLIMB_VOTE, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor), needed, tower.VoteSecondsLeft("climb"));
                return;
            case TowerManager.ClimbResult.NotInTower:
                client.SendReply(gm, Localization.MSG_TOWER_ENTER_TO_VOTE);
                return;
            case TowerManager.ClimbResult.NobodyInTown:
                client.SendReply(gm, Localization.MSG_TOWER_CLIMB_NOBODY, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                return;
            case TowerManager.ClimbResult.FightUnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_FIGHT_UNDER_WAY, UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(tower.PhaseSecondsLeft / 60f)).ToString(CultureInfo.InvariantCulture));
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
        }
    }

    private static void Close(TowerManager tower, PlayerController player, bool canForce, GameMessage gm, GameClient client)
    {
        switch (tower.Close(player, canForce, out var needed))
        {
            case TowerManager.CloseResult.Closed:
                client.SendReply(gm, Localization.MSG_TOWER_CLOSED, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor));
                return;
            case TowerManager.CloseResult.Voted:
                if (!tower.VoteJustOpened) client.SendReply(gm, Localization.MSG_TOWER_CLOSE_VOTE, needed, tower.VoteSecondsLeft("close"));
                return;
            case TowerManager.CloseResult.NotInTower:
                client.SendReply(gm, Localization.MSG_TOWER_ENTER_TO_VOTE);
                return;
            case TowerManager.CloseResult.FightUnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_CLOSE_FIGHTING);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_CLOSE_EMPTY);
                return;
        }
    }

    /// <summary>
    ///     A place as chat types it ("part 7", "floor 2", "floor 2 part 7", "2-7", "7") to the step of a part the stream
    ///     has reached; false after replying when it is none, does not exist or is not reached. A part alone is one of
    ///     the floor <paramref name="player"/> is on (the part or the town they are in), else of the stream's floor; a
    ///     floor alone is its latest part reached.
    /// </summary>
    private static bool TryPlace(TowerManager tower, PlayerController player, string arg, GameMessage gm, GameClient client, out int step)
    {
        step = 0;
        var streamFloor = TowerWords.Floor(tower.StreamFloor);
        var streamPart = TowerWords.Part(tower.StreamFloor);
        if (!Tower.TryParsePlace(arg, out var floorNumber, out var part))
        {
            client.SendReply(gm, Localization.MSG_TOWER_PLACE_UNKNOWN, arg);
            return false;
        }
        if (floorNumber == 0)
        {
            var on = player ? tower.AreaOf(player) : null;
            var town = player && player.towerHandler != null && player.towerHandler.InTown && TowerTown.Loaded ? TowerTown.Loaded.Town : null;
            floorNumber = on != null ? TowerWords.Floor(on.Step) : town != null ? town.Band : streamFloor;
        }
        if (Tower.FloorInfo(floorNumber) == null || part > Tower.PartsIn(floorNumber))
        {
            // further up than the tower goes for now is not said (no spoilers): it is just not reached
            var beyond = floorNumber > Tower.FloorCount;
            client.SendReply(gm, beyond ? Localization.MSG_TOWER_FLOOR_LOCKED : Localization.MSG_TOWER_PLACE_UNKNOWN,
                beyond ? new object[] { string.Format(Localization.UI_TOWER_FLOOR_ONLY, floorNumber), streamFloor, streamPart } : new object[] { arg });
            return false;
        }
        step = part == 0 ? Tower.LatestStep(floorNumber, tower.StreamFloor) : Tower.StepOf(floorNumber, part);
        if (step == 0 || step > tower.StreamFloor)
        {
            client.SendReply(gm, Localization.MSG_TOWER_FLOOR_LOCKED,
                part == 0 ? string.Format(Localization.UI_TOWER_FLOOR_ONLY, floorNumber) : string.Format(Localization.UI_TOWER_PLACE, floorNumber, part), streamFloor, streamPart);
            return false;
        }
        return true;
    }

    /// <summary>The parts that are open now, for chat: "floor 1, part 6 (the climb, !tower), floor 1, part 3 (!tower 1-3)".</summary>
    private static string OpenParts(TowerManager tower)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var a in tower.Areas)
        {
            if (a.Unloading || a.Closing) continue;
            parts.Add(string.Format(CultureInfo.InvariantCulture, tower.IsClimb(a) ? Localization.UI_TOWER_OPEN_CLIMB : Localization.UI_TOWER_OPEN_PART,
                TowerWords.Floor(a.Step), TowerWords.Part(a.Step), Tower.Short(a.Step)));
        }
        return string.Join(", ", parts);
    }

    /// <summary>
    ///     Only this player goes to a part the stream has cleared, to train there (the streamer's command does the same
    ///     as a viewer's). The climb's own part is !tower. When as many parts are open as fit, the reply names them and
    ///     the player stays where they are.
    /// </summary>
    private static void Visit(TowerManager tower, PlayerController player, string arg, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }
        if (tower.State == null)
        {
            tower.RefreshState();
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        if (!TryPlace(tower, player, arg, gm, client, out var step)) return;
        // the part the climb is on: the same as !tower
        if (step == tower.StreamFloor)
        {
            Join(tower, player, gm, client);
            return;
        }
        var def = Tower.Get(step);
        var f = TowerWords.Floor(step);
        var p = TowerWords.Part(step);
        var climbFloor = TowerWords.Floor(tower.StreamFloor);
        var climbPart = TowerWords.Part(tower.StreamFloor);
        switch (tower.Visit(player, step, out _))
        {
            case TowerManager.VisitResult.Joined:
                client.SendReply(gm, Localization.MSG_TOWER_TRIP + Warning(player, def), f, p, def.Name, Tower.SyncLevel(step), climbFloor, climbPart);
                return;
            case TowerManager.VisitResult.Loading:
                client.SendReply(gm, Localization.MSG_TOWER_TRIP_LOADING + Warning(player, def), f, p, def.Name, climbFloor, climbPart);
                return;
            case TowerManager.VisitResult.Here:
                client.SendReply(gm, Localization.MSG_TOWER_TRIP_HERE, f, p, def.Name, climbFloor, climbPart);
                return;
            case TowerManager.VisitResult.Full:
                if (tower.AreaLimit <= 1) client.SendReply(gm, Localization.MSG_TOWER_AREAS_FULL_POTATO, OpenParts(tower), Tower.Short(step));
                else client.SendReply(gm, Localization.MSG_TOWER_AREAS_FULL, tower.AreaLimit, OpenParts(tower));
                return;
            case TowerManager.VisitResult.Locked:
                client.SendReply(gm, Localization.MSG_TOWER_FLOOR_LOCKED, string.Format(Localization.UI_TOWER_PLACE, f, p), climbFloor, climbPart);
                return;
            case TowerManager.VisitResult.Busy:
                if (player.towerHandler != null && player.towerHandler.InBossFight) client.SendReply(gm, Localization.MSG_TOWER_TOWN_FIGHTING);
                else if (player.towerHandler != null && player.towerHandler.InTower) client.SendReply(gm, Localization.MSG_TOWER_FLOOR_BUSY);
                else client.SendReply(gm, Localization.MSG_TOWER_CANNOT_JOIN);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
        }
    }

    /// <summary>
    ///     !tower all part 7: everyone on the tower's parts goes to a part the stream has reached (<paramref name="arg"/>
    ///     as <see cref="TryPlace"/> takes it), or with nothing back to the part the climb is on. The streamer or a
    ///     moderator at once, anyone else in the tower by vote.
    /// </summary>
    private static void MoveAll(TowerManager tower, PlayerController player, string arg, bool canForce, GameMessage gm, GameClient client)
    {
        if (tower.State == null)
        {
            tower.RefreshState();
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var streamFloor = TowerWords.Floor(tower.StreamFloor);
        var streamPart = TowerWords.Part(tower.StreamFloor);
        var number = tower.StreamFloor;
        if (!string.IsNullOrEmpty(arg) && !TryPlace(tower, player, arg, gm, client, out number)) return;
        var def = Tower.Get(number);
        var f = TowerWords.Floor(number);
        var p = TowerWords.Part(number);
        switch (tower.RequestMove(player, number, canForce, out var needed))
        {
            case TowerManager.MoveResult.Voted:
                if (!tower.VoteJustOpened) client.SendReply(gm, Localization.MSG_TOWER_FLOOR_VOTE, f, p, def.Name, needed, tower.VoteSecondsLeft(TowerManager.PartVote(number)));
                return;
            case TowerManager.MoveResult.NotInTower:
                client.SendReply(gm, Localization.MSG_TOWER_ENTER_TO_VOTE);
                return;
            case TowerManager.MoveResult.Moved:
                if (number == tower.StreamFloor) client.SendReply(gm, Localization.MSG_TOWER_FLOOR_BACK, f, p, def.Name);
                else client.SendReply(gm, Localization.MSG_TOWER_FLOOR_TRAIN, f, p, def.Name, streamFloor, streamPart);
                return;
            case TowerManager.MoveResult.Same:
                client.SendReply(gm, Localization.MSG_TOWER_FLOOR_SAME, f, p, def.Name);
                return;
            case TowerManager.MoveResult.Nobody:
                client.SendReply(gm, Localization.MSG_TOWER_MOVE_NOBODY, number == tower.StreamFloor ? "" : Tower.Short(number));
                return;
            case TowerManager.MoveResult.Locked:
                client.SendReply(gm, Localization.MSG_TOWER_FLOOR_LOCKED, string.Format(Localization.UI_TOWER_PLACE, f, p), streamFloor, streamPart);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_FLOOR_BUSY);
                return;
        }
    }

    /// <summary>
    ///     !tower map: the floor map (the tower panel's, which the streamer opens with its button) on screen for a
    ///     while, for viewers; at most once a minute.
    /// </summary>
    private static void Map(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (tower.State == null)
        {
            tower.RefreshState();
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var wait = Shinobytes.UI.TowerHudView.MapWaitSeconds;
        if (wait > 0)
        {
            client.SendReply(gm, Localization.MSG_TOWER_MAP_WAIT, wait);
            return;
        }
        // the floor the player is on, else the stream's
        var on = player ? tower.AreaOf(player) : null;
        var floor = on != null ? TowerWords.Floor(on.Step) : TowerWords.Floor(tower.StreamFloor);
        if (!Shinobytes.UI.TowerHudView.ShowMapFor(floor, Shinobytes.UI.TowerHudView.MapChatSeconds))
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var info = Tower.FloorInfo(floor);
        client.SendReply(gm, Localization.MSG_TOWER_MAP_SHOWN, floor, info != null ? info.Name : "", (int)Shinobytes.UI.TowerHudView.MapChatSeconds);
    }

    private static void Info(TowerManager tower, GameMessage gm, GameClient client)
    {
        if (tower.State == null)
        {
            tower.RefreshState();
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var floor = Tower.Get(tower.StreamFloor);
        if (floor == null)
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var kind = TowerWords.BossKind(floor.Number);
        var boss = tower.AllCleared ? Localization.MSG_TOWER_INFO_ALL_CLEARED
            : tower.State.BossOpen ? string.Format(Localization.MSG_TOWER_INFO_BOSS_OPEN, kind, floor.Boss.Name)
            : string.Format(Localization.MSG_TOWER_INFO_BOSS_LOCKED, kind, floor.Boss.Name);
        // the parts players train on beside the climb, with how many are on each
        var open = new System.Collections.Generic.List<string>();
        foreach (var a in tower.Areas)
        {
            if (a.Unloading || a.Closing || tower.IsClimb(a)) continue;
            open.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_TOWER_OPEN_COUNT, TowerWords.Floor(a.Step), TowerWords.Part(a.Step), tower.CountOn(a)));
        }
        if (open.Count > 0) boss += string.Format(Localization.MSG_TOWER_INFO_OPEN, string.Join(", ", open));
        client.SendReply(gm, Localization.MSG_TOWER_INFO, floor.Floor, Tower.FloorCount, floor.Part, Tower.PartsIn(floor.Floor), floor.Name, floor.Level, Tower.SyncLevel(floor.Number), tower.QuestText(), boss, tower.AllPlayers().Count);
    }
}
