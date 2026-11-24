using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     The tower camera (docs/tower-design.md section 23; the owner, 2026-11-24: "once tower is started its tower
///     camera. it doesnt even have to observe individual players in tower ... but we should be able with T toggle to
///     outside camera and it will stay at the side work as normal until toggled again"). While anyone is in the tower
///     the stream has two sides, the tower and the world, and the camera keeps to the one the streamer is on:
///     <para><b>The tower side</b> is a group camera (<see cref="GameCameraType.Tower"/>): it watches no single
///     player, it stands over the middle of the group with the orbit camera's slow turn, as the dungeon's and the
///     raid's cameras do. Where: a boss fight (the guardian and the group together); else the climb's part while
///     anyone is on it; else the town or the busiest open part, whichever holds more. On a part of rooms it is the
///     room most of them are in (the biggest clump), so the cutaway walls go on working: they follow what the orbit
///     camera looks at. It pulls back as the group spreads, unless the streamer has just zoomed by hand.</para>
///     <para><b>The world side</b> is the game's camera as it always was: the rotation among the players outside
///     the tower, the islands, the free camera, clicks.</para>
///     <para><b>Which side</b>, in this order: (1) the tower camera key (<see cref="Keybinds.Action.TowerCamera"/>, T
///     unless rebound) or the tower panel's button flips it, and from a player followed in the tower it goes back to
///     the group; (2) a player the streamer picks (a click in the list, the search, Next player) is followed as
///     always, and the side is theirs; picking an island is the world side; (3) when the tower was empty and someone
///     goes in, or the game starts with players already in it, the side is the tower, after the entering look if one
///     plays, and never out of the streamer's hands (the free camera, a played character); (4) when the last player
///     leaves, the side is the world and the camera goes back to who or what it watched there. What takes the camera
///     for a while (a raid, a dungeon, the arena, the regatta, a cutscene, the looks at the world tower, a cheer's
///     moment on a player) gives it back to the side the streamer was on; so does the end of a followed player's
///     time on the tower side, where the group camera stands in for the rotation.</para>
///     <para>It leaves alone what has the camera for a reason when the key is pressed: a cutscene (the Cinematic
///     state takes no camera key), the regatta's race camera, build mode, a character played by hand and the hero's
///     view. The camera's own looks at the world tower (WorldTowerMoments.cs) keep off for a while after the key.</para>
/// </summary>
public static class WorldTowerCamera
{
    public enum Result { ToTower, ToWorld, Nobody, Blocked }

    public enum Side { World, Tower }

    /// <summary>The side the streamer is on. The world while nobody is in the tower.</summary>
    public static Side Current { get; private set; } = Side.World;

    /// <summary>When the key or the button was last used (Time.unscaledTime), for the camera's own looks to keep off.</summary>
    public static float LastUsed { get; private set; } = -1000f;

    /// <summary>The camera's own looks at the tower wait this long after the streamer used the key.</summary>
    public const float QuietSeconds = 30f;

    public static bool UsedLately => Time.unscaledTime - LastUsed < QuietSeconds;

    // how near each other players have to be to be one clump, and how far the group camera stands off at the least
    // and how far at the most: further than the orbit camera goes for one player (30 m), for a party spread over a field
    private const float ClumpRadius = 22f, NearestZoom = 16f, FarthestZoom = 46f;

    private static PlayerController lastInTower;
    private static PlayerController lastOutside;
    private static IslandController lastIsland;
    private static bool outsideWasIsland;
    private static PlayerController lastWatched;
    private static bool watchedFromOutside;
    private static IslandController lastIslandSeen;

    private static int inside;
    private static int lastInside;
    private static float nextCount;
    private static bool wantGroup;
    private static float wantFrom;
    private static bool wasHeld;
    private static bool wasGroup;
    private static float lastInput = -1000f;

    private static Transform target;
    private static Vector3 middle;
    private static Vector3 velocity;
    private static float radius;
    private static float nextGather;
    private static float savedZoom = -1f;
    private static float savedMaxZoom = -1f;
    private static float setZoom;
    private static float handZoomUntil;
    private static readonly List<PlayerController> group = new List<PlayerController>();
    private static readonly Dictionary<TowerFloor, int> onFloor = new Dictionary<TowerFloor, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Current = Side.World;
        LastUsed = -1000f;
        lastInTower = lastOutside = lastWatched = null;
        lastIsland = lastIslandSeen = null;
        outsideWasIsland = false;
        inside = lastInside = 0;
        nextCount = nextGather = 0f;
        wantGroup = wasHeld = wasGroup = false;
        wantFrom = 0f;
        lastInput = -1000f;
        target = null;
        savedZoom = -1f;
        savedMaxZoom = -1f;
        handZoomUntil = 0f;
        group.Clear();
        onFloor.Clear();
    }

    private static bool In(PlayerController player) => player && !player.isDestroyed && player.towerHandler != null && player.towerHandler.InTower;

    private static bool Outside(PlayerController player) => player && !player.isDestroyed && !In(player);

    private static PlayerController WatchedBy(GameCamera camera) => camera && camera.Observer ? camera.Observer.ObservedPlayer : null;

    /// <summary>The group camera is on: the camera is in the tower and watches no single player.</summary>
    public static bool Group
    {
        get
        {
            var game = GameManager.Instance;
            return game && game.Camera && game.Camera.State == GameCameraType.Tower;
        }
    }

    /// <summary>The camera is at the tower: the group camera, a player followed in it, or (with nobody watched) standing at one of its places.</summary>
    public static bool AtTower
    {
        get
        {
            var game = GameManager.Instance;
            var camera = game ? game.Camera : null;
            if (!camera) return false;
            if (camera.State == GameCameraType.Tower) return true;
            var watched = WatchedBy(camera);
            if (watched) return In(watched);
            var tower = TowerManager.Instance;
            return tower && tower.CameraInTower;
        }
    }

    /// <summary>How many players are in the tower (counted a few times a second).</summary>
    public static int Inside => inside;

    /// <summary>What the group camera looks at: the middle of the group, moved a few times a second and eased between.</summary>
    public static Transform Target
    {
        get
        {
            if (!target)
            {
                var go = new GameObject("Tower Camera Target") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                target = go.transform;
            }
            return target;
        }
    }

    // the streamer has just clicked or pressed something: a change of who is watched is their pick
    private static bool Picked => Time.unscaledTime - lastInput < 0.6f;

    private static bool Held(GameCameraType state) =>
        state == GameCameraType.Raid || state == GameCameraType.Dungeon || state == GameCameraType.Arena
        || state == GameCameraType.Race || state == GameCameraType.Cinematic || state == GameCameraType.Controlled;

    private static void Count(GameManager game)
    {
        nextCount = Time.unscaledTime + 0.25f;
        var n = 0;
        if (TowerManager.Instance)
            foreach (var p in game.Players.GetAllPlayers())
                if (In(p)) n++;
        inside = n;
    }

    /// <summary>
    ///     Every frame from the game camera, whatever has it: the climb starting and ending, the camera handed back
    ///     after something else had it, and the group camera's target while it is on.
    /// </summary>
    public static void Tick(GameCamera camera)
    {
        var game = GameManager.Instance;
        if (!camera || !game) return;
        var now = Time.unscaledTime;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || Input.anyKeyDown) lastInput = now;
        if (now >= nextCount) Count(game);

        var state = camera.State;
        var groupNow = state == GameCameraType.Tower;
        // the streamer's own zoom comes back with the camera they zoomed
        if (wasGroup && !groupNow) RestoreZoom(camera);
        wasGroup = groupNow;

        // the climb starts (or the game starts with players in the tower): the tower's side
        if (inside > 0 && lastInside == 0)
        {
            Current = Side.Tower;
            wantGroup = true;
            // (a moment for the entering look to be asked for first: it is noticed twice a second)
            wantFrom = now + 0.8f;
        }
        // the last player leaves: the world's side, and the camera goes back out
        if (inside == 0 && lastInside > 0)
        {
            wantGroup = false;
            if (Current == Side.Tower)
            {
                Current = Side.World;
                if (groupNow) GoOutside(camera);
            }
        }
        lastInside = inside;

        // something that had the camera has let go of it: back to the group camera on the tower's side
        var held = Held(state);
        if (wasHeld && !held && Current == Side.Tower && inside > 0 && !groupNow) wantGroup = true;
        wasHeld = held;

        if (wantGroup)
        {
            if (Current != Side.Tower || inside == 0 || groupNow) wantGroup = false;
            // never out of the streamer's hands (a free camera they are flying), never under a look that is about to play
            else if (now >= wantFrom && !held && !(camera.IsFreeCameraActive && InputActivity.IdleSeconds < 30f) && !WorldTower.LookPending && camera.EnableTowerCamera(Aim(game, true)))
                wantGroup = false;
        }

        if (camera.State == GameCameraType.Tower) Drive(camera, game);
    }

    /// <summary>After the cutscene check in the game camera's update: what is watched now, as the end to come back to, and whose side a pick is.</summary>
    public static void Track(GameCamera camera)
    {
        if (!camera) return;
        var watched = WatchedBy(camera);
        if (watched != lastWatched)
        {
            lastWatched = watched;
            watchedFromOutside = watched && !In(watched);
            // the streamer picked them (a click, the search, Next player): the side is theirs
            if (watched && Picked && inside > 0)
            {
                Current = In(watched) ? Side.Tower : Side.World;
                wantGroup = false;
            }
        }
        // the followed player changed sides under the camera (walked into the tower, or left it): the camera stays on
        // the side the streamer is on
        if (watched && inside > 0 && watchedFromOutside != !In(watched))
        {
            watchedFromOutside = !In(watched);
            if (Current == Side.World && In(watched)) { lastInTower = watched; GoOutside(camera); return; }
            if (Current == Side.Tower && !In(watched) && camera.EnableTowerCamera(Aim(GameManager.Instance, true))) return;
        }
        if (watched)
        {
            if (In(watched)) lastInTower = watched;
            else
            {
                lastOutside = watched;
                outsideWasIsland = false;
            }
        }
        else if (camera.State == GameCameraType.Island && camera.CurrentlyObservedIsland)
        {
            var island = camera.CurrentlyObservedIsland;
            if (island != lastIslandSeen)
            {
                lastIslandSeen = island;
                if (Picked && inside > 0)
                {
                    Current = Side.World;
                    wantGroup = false;
                }
            }
            lastIsland = island;
            outsideWasIsland = true;
        }
    }

    /// <summary>
    ///     Whether the camera's rotation may go to this player (GameCamera.ObserveNextPlayer): while anyone is in the
    ///     tower, only those on the side the streamer is on.
    /// </summary>
    public static bool Follows(PlayerController player)
    {
        if (inside == 0) return true;
        return In(player) == (Current == Side.Tower);
    }

    /// <summary>
    ///     A followed player's time is up (GameCamera's rotation timer): on the tower's side the group camera takes
    ///     over where the rotation would go on to the next player. False for the world's side: rotate as always.
    /// </summary>
    public static bool RotationEnds(GameCamera camera)
    {
        if (Current != Side.Tower || inside == 0 || !camera) return false;
        return camera.EnableTowerCamera(Aim(GameManager.Instance, true));
    }

    /// <summary>
    ///     Something that had the camera gives it back (a cutscene ending, a raid's or a dungeon's camera, the race):
    ///     on the tower's side it goes straight to the group camera, with no frame of anything else between. False on
    ///     the world's side, and while a look at the world tower waits to play.
    /// </summary>
    public static bool TakesBack(GameCamera camera)
    {
        if (Current != Side.Tower || inside == 0 || !camera || WorldTower.LookPending) return false;
        if (!camera.EnableTowerCamera(Aim(GameManager.Instance, true), true)) return false;
        wantGroup = false;
        return true;
    }

    /// <summary>What a click on a player does (the tests pick this way): the camera follows them, and the side is theirs.</summary>
    public static bool Pick(PlayerController player)
    {
        var game = GameManager.Instance;
        if (!player || !game || !game.Camera) return false;
        lastInput = Time.unscaledTime;
        game.Camera.ObservePlayer(player);
        return true;
    }

    /// <summary>The key or the button: out of the tower to the world, or (back) to the tower's group camera.</summary>
    public static Result Toggle()
    {
        var game = GameManager.Instance;
        var camera = game ? game.Camera : null;
        if (!camera) return Result.Blocked;
        // what has the camera for a reason keeps it
        if (camera.State == GameCameraType.Cinematic || camera.State == GameCameraType.Race || camera.State == GameCameraType.Controlled) return Result.Blocked;
        if (HeroController.Active || (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive)) return Result.Blocked;
        if (TowerCutscene.Playing || RavebridgeReveal.Playing || WorldTower.Playing != WorldTower.Moment.None) return Result.Blocked;
        Count(game);

        if (camera.State == GameCameraType.Tower)
        {
            Current = Side.World;
            wantGroup = false;
            GoOutside(camera);
            LastUsed = Time.unscaledTime;
            return Result.ToWorld;
        }

        if (inside == 0)
        {
            Shinobytes.UI.TowerHudView.Word(Localization.UI_TOWER_CAMERA_NOBODY);
            return Result.Nobody;
        }
        // from the world, or from a player followed in the tower: the group camera
        if (!camera.EnableTowerCamera(Aim(game, true))) return Result.Blocked;
        Current = Side.Tower;
        wantGroup = false;
        LastUsed = Time.unscaledTime;
        return Result.ToTower;
    }

    // back to what was watched outside: that player if still outside, or that island's view, else anyone outside, else the islands
    private static void GoOutside(GameCamera camera)
    {
        RestoreZoom(camera);
        var game = GameManager.Instance;
        var done = false;
        if (!outsideWasIsland && Outside(lastOutside)) done = camera.Watch(lastOutside);
        if (!done && outsideWasIsland && lastIsland) done = camera.Watch(lastIsland);
        if (!done && Outside(lastOutside)) done = camera.Watch(lastOutside);
        if (!done && game)
        {
            foreach (var player in game.Players.GetAllPlayers())
            {
                if (!Outside(player) || !camera.Watch(player)) continue;
                done = true;
                break;
            }
        }
        if (!done && lastIsland) done = camera.Watch(lastIsland);
        if (!done) camera.ObserveNextIsland();
    }

    private static void RestoreZoom(GameCamera camera)
    {
        if (savedZoom <= 0f) return;
        var orbit = camera.GetComponent<OrbitCamera>();
        if (orbit)
        {
            if (savedMaxZoom > 0f) orbit.MaxZoom = savedMaxZoom;
            orbit.Distance = Mathf.Min(savedZoom, orbit.MaxZoom);
        }
        savedZoom = -1f;
    }

    // ---- the group ----

    /// <summary>The target put on the group as it is now (at once when <paramref name="snap"/>), for the camera to be pointed at.</summary>
    private static Transform Aim(GameManager game, bool snap)
    {
        var t = Target;
        if (Gather(game, out var boss)) Middle(boss);
        if (snap)
        {
            t.position = middle;
            velocity = Vector3.zero;
        }
        nextGather = Time.unscaledTime + 0.2f;
        return t;
    }

    /// <summary>
    ///     Who the group camera shows: a boss fight's players; else those on the climb's part; else the town's or the
    ///     busiest open part's, whichever are more (the town on a tie). False when nobody is in the tower.
    /// </summary>
    private static bool Gather(GameManager game, out EnemyController boss)
    {
        boss = null;
        group.Clear();
        var tower = TowerManager.Instance;
        if (!tower || !game) return false;

        var fight = tower.Phase == TowerManager.TowerPhase.Fight ? tower.FightArea : null;
        var climb = fight ?? tower.PushArea;
        var climbFloor = climb != null ? climb.Floor : null;
        onFloor.Clear();
        var town = 0;
        var onClimb = 0;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!In(p)) continue;
            var floor = p.towerHandler.Floor;
            if (!floor) { town++; continue; }
            if (floor == climbFloor) onClimb++;
            onFloor[floor] = onFloor.TryGetValue(floor, out var n) ? n + 1 : 1;
        }

        TowerFloor where = null;
        var inTown = false;
        if (onClimb > 0) where = climbFloor;
        else
        {
            var most = 0;
            foreach (var pair in onFloor)
                if (pair.Value > most) { most = pair.Value; where = pair.Key; }
            if (town >= most) { inTown = town > 0; where = null; }
        }
        if (!where && !inTown) return false;

        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!In(p)) continue;
            var floor = p.towerHandler.Floor;
            if (inTown ? !floor : floor == where) group.Add(p);
        }
        if (fight != null && where == climbFloor) boss = tower.FightBoss;
        return group.Count > 0;
    }

    /// <summary>
    ///     The middle of the biggest clump of the group (the player with the most others near, and the mean of those),
    ///     so a party split over two rooms is watched where most of it is and never from inside the wall between; a
    ///     guardian that is fought is taken into the picture.
    /// </summary>
    private static void Middle(EnemyController boss)
    {
        var count = group.Count;
        var best = 0;
        var bestNear = -1;
        // (a big crowd: every third is asked, which finds the clump as well)
        var stride = count > 90 ? 3 : 1;
        for (var i = 0; i < count; i += stride)
        {
            var a = group[i].transform.position;
            var near = 0;
            for (var j = 0; j < count; j++)
                if ((group[j].transform.position - a).sqrMagnitude < ClumpRadius * ClumpRadius) near++;
            if (near > bestNear) { bestNear = near; best = i; }
        }
        var centre = group[best].transform.position;
        var sum = Vector3.zero;
        var members = 0;
        for (var j = 0; j < count; j++)
        {
            var p = group[j].transform.position;
            if ((p - centre).sqrMagnitude >= ClumpRadius * ClumpRadius) continue;
            sum += p;
            members++;
        }
        var mid = members > 0 ? sum / members : centre;
        var reach = 0f;
        for (var j = 0; j < count; j++)
        {
            var d = group[j].transform.position - mid;
            d.y = 0f;
            if (d.sqrMagnitude < ClumpRadius * ClumpRadius * 2.25f) reach = Mathf.Max(reach, d.magnitude);
        }
        if (boss)
        {
            // the guardian and the group together: the middle leans toward it, and the picture holds both
            var to = boss.transform.position - mid;
            to.y = 0f;
            var lean = Mathf.Min(to.magnitude * 0.4f, 12f);
            if (to.sqrMagnitude > 0.01f) mid += to.normalized * lean;
            reach = Mathf.Max(reach, to.magnitude - lean + 4f);
        }
        middle = mid;
        radius = reach;
    }

    private static void Drive(GameCamera camera, GameManager game)
    {
        var now = Time.unscaledTime;
        var t = Target;
        if (now >= nextGather)
        {
            nextGather = now + 0.2f;
            if (Gather(game, out var boss)) Middle(boss);
        }
        var orbit = camera.GetComponent<OrbitCamera>();
        // another place (the town after a part, the next part): a cut, not a flight through the walls
        if ((middle - t.position).sqrMagnitude > 45f * 45f)
        {
            t.position = middle;
            velocity = Vector3.zero;
            if (orbit) orbit.TargetTransform = t;
        }
        else t.position = Vector3.SmoothDamp(t.position, middle, ref velocity, 0.6f, 40f, Time.unscaledDeltaTime);

        if (!orbit) return;
        if (savedZoom <= 0f)
        {
            savedZoom = orbit.Distance;
            savedMaxZoom = orbit.MaxZoom;
            orbit.MaxZoom = Mathf.Max(orbit.MaxZoom, FarthestZoom);
            setZoom = orbit.Distance;
            handZoomUntil = 0f;
        }
        // the streamer zoomed by hand: theirs for a while
        if (Mathf.Abs(orbit.Distance - setZoom) > 0.01f) handZoomUntil = now + 25f;
        if (now >= handZoomUntil)
        {
            var wanted = Mathf.Clamp(radius * 1.25f + 12f, NearestZoom, FarthestZoom);
            orbit.Distance = Mathf.MoveTowards(orbit.Distance, wanted, 6f * Time.unscaledDeltaTime);
        }
        setZoom = orbit.Distance;
    }
}
