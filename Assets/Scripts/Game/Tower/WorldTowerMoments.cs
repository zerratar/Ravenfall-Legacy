using UnityEngine;

/// <summary>
///     The camera's looks at the world tower (docs/world-tower.md, "Camera moments"). The camera that follows a
///     player looks down at the ground and never shows the horizon, so the tower gets short shots of its own:
///     <para><b>A floor falls</b>: after the floor boss's own cutscene, a look at the tower as that floor's section
///     flares up and settles.</para>
///     <para><b>The stream enters</b>: when the tower was empty and someone goes in, a look from the gate up the tower.</para>
///     <para><b>Now and then</b>: while nothing else is going on and nobody has touched mouse or keys for a while, a
///     slow look at the tower from wherever the camera is. Its own setting (Looks at the tower), off in potato mode.</para>
///     <para>None of them hides the game's windows or puts up a title; each is a few seconds. A look never starts
///     over a cutscene, an event, build mode, an open window, or a camera the streamer steers, and it ends at once
///     when the streamer touches anything or something bigger starts. One that cannot start in time is dropped, not
///     queued behind other things. The first two follow the Cutscenes setting.</para>
/// </summary>
public sealed partial class WorldTower
{
    public enum Moment { None, FloorLit, Entering, Idle }

    /// <summary>The look that is playing, if any.</summary>
    public static Moment Playing => Instance ? Instance.moment : Moment.None;

    [Header("Camera moments")]
    [Tooltip("Minutes between the looks while nothing is going on: a time between these two is picked after each.")]
    public float IdleLookMinutesMin = 12f;
    public float IdleLookMinutesMax = 20f;
    [Tooltip("Seconds without mouse or keys before an idle look may start.")]
    public float IdleLookQuietSeconds = 45f;
    [Tooltip("The tower's height above the sea up to its last built section, for aiming.")]
    public float BuiltHeight = 367f;

    public const float FloorLitSeconds = 4.6f, EnteringSeconds = 5f, IdleSeconds = 9f;
    /// <summary>An entering look at most this often: a tower that empties and fills again does not repeat it.</summary>
    public const float EnteringEverySeconds = 900f;

    private Moment moment;
    private float momentTime, momentSeconds;
    private readonly Vector3[] momentPositions = new Vector3[2];
    private readonly Vector3[] momentForwards = new Vector3[2];
    private PlayerController momentPlayer;
    private IslandController momentIsland;
    private int momentFloor;
    private bool momentFlared;
    private float momentQuietFrom;

    // a floor that fell and waits for its look: its section stays as it was until the camera is on it (or the wait is over)
    private int waitingFloor;
    private float waitingUntil;
    private float waitingSince;
    private float nextIdleLook;
    private float nextEntering;
    private float enteringUntil;
    private int lastInside;
    private bool insideKnown;
    private float nextInsideCount;
    private float lastOpening = -1000f;

    /// <summary>For trying a look by hand (the editor's tests): plays it now if the camera is free.</summary>
    public bool PlayNow(Moment kind, int floor = 1) => StartMoment(kind, floor, true);

    private void FloorFell(int floor)
    {
        // the look waits for the boss's own cutscene to end; without cutscenes the section flares at once
        if (!SettingsApplier.Cutscenes || (GameManager.Instance && GameManager.Instance.PotatoMode)) { Flare(floor); return; }
        waitingFloor = floor;
        waitingSince = Time.time;
        waitingUntil = Time.time + 14f;
    }

    /// <summary>Every frame: a look that is playing, and the ones that are due.</summary>
    private void Moments()
    {
        if (moment != Moment.None) { Play(); return; }
        var game = GameManager.Instance;
        if (!game) return;

        if (waitingFloor > 0)
        {
            // the boss's own cutscene comes first, however long it is (within reason): the look follows it
            if (TowerCutscene.Playing && Time.time - waitingSince < 60f) waitingUntil = Mathf.Max(waitingUntil, Time.time + 5f);
            if (Time.time > waitingUntil) { Flare(waitingFloor); waitingFloor = 0; }
            else if (StartMoment(Moment.FloorLit, waitingFloor, false)) waitingFloor = 0;
            return;
        }

        // the stream enters: the tower was empty and someone goes in (to the climb or to a town). Not on the heels
        // of the tower's own opening cutscene, which has just shown it
        var tm = TowerManager.Instance;
        if (tm && Time.time >= nextInsideCount)
        {
            nextInsideCount = Time.time + 0.5f;
            if (tm.Opening) lastOpening = Time.time;
            var inside = tm.AllPlayers().Count;
            if (insideKnown && lastInside == 0 && inside > 0 && Time.time >= nextEntering && Time.time - lastOpening > 60f
                && SettingsApplier.Cutscenes && !game.PotatoMode)
                enteringUntil = Time.time + 6f;
            lastInside = inside;
            insideKnown = true;
        }
        if (enteringUntil > 0f)
        {
            if (Time.time > enteringUntil) enteringUntil = 0f;
            else if (StartMoment(Moment.Entering, CurrentFloor, false)) { enteringUntil = 0f; nextEntering = Time.time + EnteringEverySeconds; }
            return;
        }

        // now and then, while nothing is going on
        if (Time.time < nextIdleLook) return;
        if (!SettingsApplier.TowerLooks) { nextIdleLook = Time.time + 60f; return; }
        if (InputActivity.IdleSeconds < IdleLookQuietSeconds || Busy(game) || !StartMoment(Moment.Idle, CurrentFloor, false))
        {
            nextIdleLook = Time.time + 30f;
            return;
        }
        nextIdleLook = Time.time + Random.Range(IdleLookMinutesMin, IdleLookMinutesMax) * 60f;
    }

    /// <summary>Whether the camera can be had without taking it from anyone or putting a look over something else.</summary>
    private static bool CameraFree(GameManager game)
    {
        var camera = game.Camera;
        if (!camera || !camera.CinematicFree || camera.IsFreeCameraActive || camera.State == GameCameraType.Race) return false;
        if (TowerCutscene.Playing || RavebridgeReveal.Playing) return false;
        // the streamer has just sent the camera to the tower or back (the tower camera key): it stays where they put it
        if (WorldTowerCamera.UsedLately) return false;
        if (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive) return false;
        if (HeroController.Active) return false;
        if (Keybinds.IsTyping) return false;
        if (Shinobytes.UI.SettingsWindow.IsOpen || Shinobytes.UI.ShopWindow.IsOpen || Shinobytes.UI.WhatsNew.IsOpen
            || Shinobytes.UI.RavebridgeIntro.IsOpen || Shinobytes.UI.RavebridgeDialogueView.IsOpen) return false;
        var tm = TowerManager.Instance;
        if (tm && (tm.Opening || tm.Phase == TowerManager.TowerPhase.Countdown || tm.Phase == TowerManager.TowerPhase.Fight)) return false;
        return true;
    }

    /// <summary>Something is going on that an idle look has no business interrupting.</summary>
    private static bool Busy(GameManager game)
    {
        if (game.Raid && (game.Raid.Started || game.Raid.IsEventActive)) return true;
        if (game.Dungeons && (game.Dungeons.Active || game.Dungeons.Started)) return true;
        if (game.Arena && game.Arena.Started) return true;
        if (game.StreamRaid && (game.StreamRaid.Started || game.StreamRaid.IsWar)) return true;
        if (RavebridgeManager.Instance && RavebridgeManager.Instance.Active) return true;
        if (WaveRun.Current != null || Regatta.Running || PirateRaid.Running || TreasureHunt.Running) return true;
        var tm = TowerManager.Instance;
        return tm && tm.Phase != TowerManager.TowerPhase.Closed && tm.Phase != TowerManager.TowerPhase.Open;
    }

    private bool StartMoment(Moment kind, int floor, bool byHand)
    {
        var game = GameManager.Instance;
        if (moment != Moment.None || !game || !view) return false;
        if (!byHand && !CameraFree(game)) return false;
        // where the camera goes back to: who or what it was on
        var observer = game.Camera.Observer;
        momentPlayer = observer ? observer.ObservedPlayer : null;
        momentIsland = game.Camera.CurrentlyObservedIsland;
        var eye = view.transform.position;
        var forward = view.transform.forward;
        if (!game.Camera.StartCinematic()) return false;

        var foot = transform.position;
        var front = -transform.forward;                 // the way the gate faces
        var side = transform.right;
        var top = foot + Vector3.up * BuiltHeight;
        switch (kind)
        {
            case Moment.FloorLit:
            {
                // from out over the sea in front, a little to one side, rising toward the floor that fell
                var height = SectionHeight(floor);
                var aim = foot + Vector3.up * height;
                momentPositions[0] = foot + front * (BuiltHeight * 1.9f) + side * (BuiltHeight * 0.5f) + Vector3.up * (height * 0.35f + 30f);
                momentPositions[1] = foot + front * (BuiltHeight * 1.55f) + side * (BuiltHeight * 0.3f) + Vector3.up * (height * 0.6f + 30f);
                momentForwards[0] = (aim - momentPositions[0]).normalized;
                momentForwards[1] = (aim - momentPositions[1]).normalized;
                momentSeconds = FloorLitSeconds;
                break;
            }
            case Moment.Entering:
            {
                // from the gate, back and up, until the whole tower stands in the picture
                var gate = Gate ? Gate.position : foot + front * 100f;
                momentPositions[0] = gate + front * 70f + Vector3.up * 14f;
                momentPositions[1] = foot + front * (BuiltHeight * 1.5f) + side * (BuiltHeight * 0.18f) + Vector3.up * 70f;
                momentForwards[0] = (gate + Vector3.up * 16f - momentPositions[0]).normalized;
                momentForwards[1] = (foot + Vector3.up * (BuiltHeight * 0.5f) - momentPositions[1]).normalized;
                momentSeconds = EnteringSeconds;
                break;
            }
            default:
            {
                // from where the camera is: it lifts a little and turns to the tower, then drifts toward it. From
                // too far off (the tower's own floors, kilometres out) or from under the ground: from the sea in front
                var away = eye - foot;
                away.y = 0f;
                if (away.magnitude > 2600f || away.magnitude < BuiltHeight * 0.8f || eye.y < -20f)
                {
                    eye = foot + front * (BuiltHeight * 2.3f) + side * (BuiltHeight * 0.6f) + Vector3.up * 40f;
                    forward = (top * 0.5f + foot * 0.5f - eye).normalized;
                }
                var toward = (foot + Vector3.up * (BuiltHeight * 0.45f)) - eye;
                var level = new Vector3(toward.x, 0f, toward.z).normalized;
                momentPositions[0] = eye;
                momentForwards[0] = forward;
                momentPositions[1] = eye + Vector3.up * 16f + level * 24f;
                momentForwards[1] = ((foot + Vector3.up * (BuiltHeight * 0.45f)) - momentPositions[1]).normalized;
                momentSeconds = IdleSeconds;
                break;
            }
        }
        moment = kind;
        momentFloor = floor;
        momentFlared = false;
        momentTime = 0f;
        // one tried by hand (the tests) runs its time whatever the mouse does
        momentQuietFrom = byHand ? float.MaxValue : Time.unscaledTime + 0.4f;
        Place(0f);
        // the camera has jumped, perhaps from kilometres off where the tower is not drawn: what shows is worked out again now
        nextLook = 0f;
        Look();
        return true;
    }

    private float SectionHeight(int floor)
    {
        // the middle of a floor's section: the built sections share the height between them, near enough for aiming
        var floors = 0;
        foreach (var section in Sections) if (section.Floor > floors) floors = section.Floor;
        if (floors <= 0 || floor <= 0) return BuiltHeight * 0.5f;
        return BuiltHeight * (Mathf.Clamp(floor, 1, floors) - 0.5f) / floors;
    }

    private void Play()
    {
        var game = GameManager.Instance;
        momentTime += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        // the floor flares once the camera has settled on it
        if (moment == Moment.FloorLit && !momentFlared && momentTime > 0.9f)
        {
            momentFlared = true;
            Flare(momentFloor);
        }
        var over = momentTime >= momentSeconds || !game || !game.Camera || game.Camera.State != GameCameraType.Cinematic;
        // the streamer touches anything: the camera is theirs again
        if (!over && Time.unscaledTime > momentQuietFrom && (InputActivity.IdleSeconds < 0.2f || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))) over = true;
        // something bigger starts: it gets the camera
        if (!over && game)
        {
            var tm = TowerManager.Instance;
            if (game.Raid.Started || game.Dungeons.Started || game.StreamRaid.IsWar || (tm && (tm.Opening || tm.Phase == TowerManager.TowerPhase.Countdown || tm.Phase == TowerManager.TowerPhase.Fight))) over = true;
        }
        if (over) { EndMoment(true); return; }
        Place(Mathf.Clamp01(momentTime / momentSeconds));
    }

    private void Place(float u)
    {
        if (!view) return;
        var (position, rotation) = TowerCutscene.Evaluate(momentPositions, momentForwards, TowerCutscene.Ease(u));
        view.transform.SetPositionAndRotation(position, rotation);
    }

    private void EndMoment(bool giveBack)
    {
        if (moment == Moment.None) return;
        if (moment == Moment.FloorLit && !momentFlared) Flare(momentFloor);
        moment = Moment.None;
        var game = GameManager.Instance;
        if (!giveBack || !game || !game.Camera) return;
        if (momentPlayer && !momentPlayer.isDestroyed) game.Camera.StopCinematic(momentPlayer);
        else game.Camera.StopCinematic(momentIsland);
        momentPlayer = null;
        momentIsland = null;
    }
}
