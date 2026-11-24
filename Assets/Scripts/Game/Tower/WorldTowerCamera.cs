using UnityEngine;

/// <summary>
///     The tower camera key (Keybinds.Action.TowerCamera, T unless rebound) and the button for it on the tower panel:
///     one press takes the camera from the world to the players in the tower, the next takes it back to what it was
///     watching outside. Both ends are remembered, so the streamer can look out and come back to the same player.
///     <para><b>To the tower</b>: the player the camera last watched there if they are still there, else someone on
///     the climb's part, else someone in the town, else anyone in the tower. With nobody in the tower the key does
///     nothing but say so (the tower panel is not up then either).</para>
///     <para><b>Back to the world</b>: the player it watched outside if they are still outside, or that island's
///     view, else the first player outside the tower, else the islands.</para>
///     <para><b>It stays there</b>: after the key, the camera's own rotation (the timer, Next player) goes round the
///     players on that side only, in the tower or outside it, so the streamer is not carried out again ten seconds
///     later. That ends when the streamer picks someone on the other side (a click in the list, the search, Next
///     island), when the key is pressed again (the other side then), or when nobody is left on that side.</para>
///     <para>It leaves alone what has the camera for a reason: a cutscene (the Cinematic state takes no camera key),
///     the regatta's race camera, build mode, a character played by hand and the hero's view. The camera's own looks
///     at the tower (WorldTowerMoments.cs) keep off for a while after it was used.</para>
/// </summary>
public static class WorldTowerCamera
{
    public enum Result { ToTower, ToWorld, Nobody, Blocked }

    /// <summary>When the key or the button was last used (Time.unscaledTime), for the camera's own looks to keep off.</summary>
    public static float LastUsed { get; private set; } = -1000f;

    /// <summary>The camera's own looks at the tower wait this long after the streamer used the key.</summary>
    public const float QuietSeconds = 30f;

    public static bool UsedLately => Time.unscaledTime - LastUsed < QuietSeconds;

    private enum Side { Any, Tower, World }

    // where the camera's rotation stays since the key was last used
    private static Side side = Side.Any;
    private static int sideCountFrame = -1;
    private static bool sideHasPlayers;

    private static PlayerController lastInTower;
    private static PlayerController lastOutside;
    private static IslandController lastIsland;
    private static bool outsideWasIsland;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        LastUsed = -1000f;
        side = Side.Any;
        sideCountFrame = -1;
        lastInTower = null;
        lastOutside = null;
        lastIsland = null;
        outsideWasIsland = false;
    }

    private static bool In(PlayerController player) => player && !player.isDestroyed && player.towerHandler != null && player.towerHandler.InTower;

    private static bool Outside(PlayerController player) => player && !player.isDestroyed && !In(player);

    /// <summary>The camera is at the tower: on a player in it, or (with nobody watched) standing at one of its places.</summary>
    public static bool AtTower
    {
        get
        {
            var game = GameManager.Instance;
            var camera = game ? game.Camera : null;
            var watched = camera && camera.Observer ? camera.Observer.ObservedPlayer : null;
            if (watched) return In(watched);
            var tower = TowerManager.Instance;
            return tower && tower.CameraInTower;
        }
    }

    /// <summary>Whether the key would do anything now: someone is in the tower, or the camera is there.</summary>
    public static bool Useful
    {
        get
        {
            var tower = TowerManager.Instance;
            return tower && (tower.AllPlayers().Count > 0 || tower.CameraInTower);
        }
    }

    /// <summary>Every frame from the game camera: what it watches now, as the end to come back to.</summary>
    public static void Track(GameCamera camera)
    {
        if (!camera) return;
        var watched = camera.Observer ? camera.Observer.ObservedPlayer : null;
        if (watched)
        {
            if (In(watched))
            {
                lastInTower = watched;
                // the rotation never goes there by itself while it is kept outside: the streamer picked them
                if (side == Side.World) side = Side.Any;
            }
            else
            {
                lastOutside = watched;
                outsideWasIsland = false;
                if (side == Side.Tower) side = Side.Any;
            }
        }
        else if (camera.State == GameCameraType.Island && camera.CurrentlyObservedIsland)
        {
            lastIsland = camera.CurrentlyObservedIsland;
            outsideWasIsland = true;
            if (side == Side.Tower) side = Side.Any;
        }
    }

    /// <summary>
    ///     Whether the camera's rotation may go to this player now (GameCamera.ObserveNextPlayer): anyone, until the key
    ///     was used; then only those on the side it took the camera to, while there is anyone there.
    /// </summary>
    public static bool Follows(PlayerController player)
    {
        if (side == Side.Any) return true;
        if (sideCountFrame != Time.frameCount)
        {
            sideCountFrame = Time.frameCount;
            sideHasPlayers = false;
            var game = GameManager.Instance;
            if (game)
                foreach (var p in game.Players.GetAllPlayers())
                    if (p && !p.isDestroyed && In(p) == (side == Side.Tower)) { sideHasPlayers = true; break; }
        }
        if (!sideHasPlayers)
        {
            side = Side.Any;
            return true;
        }
        return In(player) == (side == Side.Tower);
    }

    /// <summary>The key or the button: to the tower, or back.</summary>
    public static Result Toggle()
    {
        var game = GameManager.Instance;
        var camera = game ? game.Camera : null;
        if (!camera) return Result.Blocked;
        // what has the camera for a reason keeps it
        if (camera.State == GameCameraType.Cinematic || camera.State == GameCameraType.Race || camera.State == GameCameraType.Controlled) return Result.Blocked;
        if (HeroController.Active || (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive)) return Result.Blocked;
        if (TowerCutscene.Playing || RavebridgeReveal.Playing || WorldTower.Playing != WorldTower.Moment.None) return Result.Blocked;

        var tower = TowerManager.Instance;
        if (AtTower)
        {
            // back to what was watched outside
            var done = false;
            if (!outsideWasIsland && Outside(lastOutside)) done = camera.Watch(lastOutside);
            if (!done && outsideWasIsland && lastIsland) done = camera.Watch(lastIsland);
            if (!done && Outside(lastOutside)) done = camera.Watch(lastOutside);
            if (!done)
            {
                foreach (var player in game.Players.GetAllPlayers())
                {
                    if (!Outside(player) || !camera.Watch(player)) continue;
                    done = true;
                    break;
                }
            }
            if (!done && lastIsland) done = camera.Watch(lastIsland);
            // from here the rotation stays outside (set before the islands are asked for, which may go on to a player)
            side = Side.World;
            if (!done) camera.ObserveNextIsland();
            LastUsed = Time.unscaledTime;
            return Result.ToWorld;
        }

        var inside = tower ? tower.AllPlayers() : null;
        if (inside == null || inside.Count == 0)
        {
            Shinobytes.UI.TowerHudView.Word(Localization.UI_TOWER_CAMERA_NOBODY);
            return Result.Nobody;
        }
        PlayerController target = In(lastInTower) ? lastInTower : null;
        if (!target)
        {
            var climbing = tower.Players();
            if (climbing.Count > 0) target = climbing[0];
        }
        if (!target)
        {
            var town = tower.TownPlayers();
            if (town.Count > 0) target = town[0];
        }
        if (!target) target = inside[0];
        if (!camera.Watch(target)) return Result.Blocked;
        side = Side.Tower;
        LastUsed = Time.unscaledTime;
        return Result.ToTower;
    }
}
