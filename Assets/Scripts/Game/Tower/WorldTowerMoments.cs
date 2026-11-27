using RavenNest.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     The camera's looks at the world tower (docs/world-tower.md, "Camera moments"). The camera that follows a
///     player looks down at the ground and never shows the horizon, so the tower gets shots of its own:
///     <para><b>A floor falls</b> (a floor boss, never a mini boss): a cutscene of its own. The boss's fall ends in
///     black; the tower fades in from out over the sea with the whole stack in the picture, the cleared floor's section
///     flares, a band of light with sparks climbs to the next floor's section, which lights up in its own color, and
///     the title says so ("Floor 2 cleared", "Floor 3: The Storm Spire is open"). Then black again while the players
///     reach the new floor's town, and the picture comes back on them, or straight on the town's own first-visit
///     cutscene when it is due. The screen steps aside as for the tower's other cutscenes; Skip, Esc or Space end it
///     (mouse and other keys do not). Potato mode: no particles, the fades and the title stay.</para>
///     <para><b>The stream enters</b>: when the tower was empty and someone goes in, a look from the gate up the
///     tower, faded in and out, with no title and no window hidden.</para>
///     <para><b>Now and then</b>: while nothing else is going on and nobody has touched mouse or keys for a while, a
///     slow look at the tower from wherever the camera is. Its own setting (Looks at the tower), off in potato mode.</para>
///     <para>A look never starts over a cutscene, an event, build mode, an open window, or a camera the streamer
///     steers, and it ends at once when something bigger starts. The last two end when the streamer touches anything.
///     One that cannot start in time is dropped, not queued behind other things (the floor's section lights anyway).
///     The first two follow the Cutscenes setting.</para>
/// </summary>
public sealed partial class WorldTower
{
    public enum Moment { None, FloorLit, Entering, Idle }

    /// <summary>A look is playing or waits for the camera: the tower's group camera does not step in before it (WorldTowerCamera).</summary>
    public static bool LookPending => Instance && (Instance.moment != Moment.None || Instance.waitingFloor > 0 || Instance.enteringUntil > 0f);

    /// <summary>The look that is playing, if any.</summary>
    public static Moment Playing => Instance ? Instance.moment : Moment.None;

    /// <summary>The floor's look has the screen (the windows stepped aside, its title): it is a tower cutscene too (<see cref="TowerCutscene.Playing"/>).</summary>
    public static bool Cinematic => Instance && Instance.screen != null;

    /// <summary>While the floor's look plays or waits to, a new town's fly-over is kept for its end (TowerManager).</summary>
    public static bool HoldsArrival => Instance && (Instance.waitingFloor > 0 || Instance.moment == Moment.FloorLit);

    /// <summary>A floor boss fell (TowerManager.Won, before the server's answer): the floor's look follows its fall.</summary>
    public static void FloorBossFell(int floor)
    {
        if (Instance) Instance.BossFell(floor);
    }

    /// <summary>The server refused that clear: no look, and the light follows what the server says.</summary>
    public static void FloorBossRefused()
    {
        if (Instance) Instance.Refused();
    }

    [Header("Camera moments")]
    [Tooltip("Minutes between the looks while nothing is going on: a time between these two is picked after each.")]
    public float IdleLookMinutesMin = 12f;
    public float IdleLookMinutesMax = 20f;
    [Tooltip("Seconds without mouse or keys before an idle look may start.")]
    public float IdleLookQuietSeconds = 45f;
    [Tooltip("The tower's height above the sea up to its last built section, for aiming.")]
    public float BuiltHeight = 367f;

    /// <summary>The floor's look on the tower (from its fade in to its fade out), the entering look, the idle look.</summary>
    public const float FloorLitSeconds = 6.6f, EnteringSeconds = 5f, IdleSeconds = 9f;
    /// <summary>The fades: the floor's look, the entering look, a skip.</summary>
    public const float FadeSeconds = 0.7f, EnteringFade = 0.5f, SkipFade = 0.4f;
    /// <summary>An entering look at most this often: a tower that empties and fills again does not repeat it.</summary>
    public const float EnteringEverySeconds = 900f;

    // the floor's look from its fade in: the cleared floor flares, the light sets off and reaches the next floor, the title
    private const float FlareAt = 0.9f, ClimbFrom = 1.7f, ClimbTo = 3.5f, TitleAt = 3.7f, TitleIn = 0.9f;
    // while the players are still on their way to the new town: the look holds on the tower this long at most, then
    // waits in the dark this long at most
    private const float LongestShot = 9.5f, LongestBlack = 4f;
    // the title's size: as the floor boss's
    private const int TitleSize = 104;

    private enum Stage { Out, Shot, Leaving, Black }

    private Moment moment;
    private Stage stage;
    private float stageTime, momentTime, momentSeconds, leaveSeconds;
    private readonly Vector3[] momentPositions = new Vector3[2];
    private readonly Vector3[] momentForwards = new Vector3[2];
    private PlayerController momentPlayer;
    private IslandController momentIsland;
    private int momentFloor;
    private bool momentFlared, momentClimbed, momentByHand, skipped;
    private float momentQuietFrom;
    private CutsceneScreen screen;
    private ClimbLight climb;

    // a floor that fell and waits for its look; the next floor's section stays dark until the light reaches it
    private int waitingFloor;
    private float waitingUntil;
    private float waitingSince;
    private bool preFaded;
    private int handledFloor;
    private int holdFloor;
    private int forcedLit;
    private float nextIdleLook;
    private float nextEntering;
    private float enteringUntil;
    private int lastInside;
    private bool insideKnown;
    private float nextInsideCount;
    private float lastOpening = -1000f;

    /// <summary>For trying a look by hand (the editor's tests): plays it now if the camera is free.</summary>
    public bool PlayNow(Moment kind, int floor = 1) => StartMoment(kind, floor, true);

    private void BossFell(int floor)
    {
        // without cutscenes the section flares when the server says the floor is cleared, as before
        if (floor <= 0 || !SettingsApplier.Cutscenes || moment == Moment.FloorLit) return;
        handledFloor = floor;
        waitingFloor = floor;
        holdFloor = floor + 1;
        waitingSince = Time.time;
        waitingUntil = Time.time + 14f;
        preFaded = false;
    }

    private void Refused()
    {
        forcedLit = 0;
        if (waitingFloor <= 0) return;
        waitingFloor = 0;
        holdFloor = 0;
        handledFloor = 0;
        if (preFaded) ScreenFade.To(0f, 0.3f);
        preFaded = false;
    }

    /// <summary>The server says a floor was cleared (Progress): its look already follows the boss's fall, or it flares now.</summary>
    private void FloorFell(int floor)
    {
        if (floor == handledFloor) return;
        Flare(floor);
    }

    /// <summary>Every frame: a look that is playing, and the ones that are due.</summary>
    private void Moments()
    {
        if (moment != Moment.None) { Play(); return; }
        var game = GameManager.Instance;
        if (!game) return;

        if (waitingFloor > 0)
        {
            // the boss's own cutscene comes first, however long it is (within reason): the look follows it, and when
            // it can, the cutscene's last moments go dark, so nothing of the game shows between the two
            if (TowerCutscene.Running && Time.time - waitingSince < 60f)
            {
                waitingUntil = Mathf.Max(waitingUntil, Time.time + 5f);
                var left = TowerCutscene.Remaining;
                if (!preFaded && left <= FadeSeconds && FreeAfterCutscene(game))
                {
                    preFaded = true;
                    ScreenFade.To(1f, Mathf.Max(0.1f, left));
                }
                return;
            }
            if (Time.time > waitingUntil) DropFloorLook();
            else if (StartMoment(Moment.FloorLit, waitingFloor, false)) waitingFloor = 0;
            else if (preFaded)
            {
                preFaded = false;
                ScreenFade.To(0f, 0.3f);
            }
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

    /// <summary>The floor's look could not start in time: dropped, and the floors light without it.</summary>
    private void DropFloorLook()
    {
        Flare(waitingFloor);
        waitingFloor = 0;
        holdFloor = 0;
        if (preFaded) ScreenFade.To(0f, 0.3f);
        preFaded = false;
        // a new town's fly-over kept for the look plays now, if it can
        var tm = TowerManager.Instance;
        if (tm) tm.PlayDueArrival();
    }

    /// <summary>Whether the camera can be had without taking it from anyone or putting a look over something else.</summary>
    private static bool CameraFree(GameManager game)
    {
        var camera = game.Camera;
        if (!camera || !camera.CinematicFree || camera.IsFreeCameraActive || camera.State == GameCameraType.Race) return false;
        if (TowerCutscene.Running || RavebridgeReveal.Playing) return false;
        return Undisturbed(game);
    }

    /// <summary>The camera would be free once the tower cutscene playing now ends (the floor boss's fall).</summary>
    private static bool FreeAfterCutscene(GameManager game)
    {
        var camera = game.Camera;
        if (!camera || game.Raid.Started || game.Dungeons.Started || game.StreamRaid.IsWar || RavebridgeReveal.Playing) return false;
        return Undisturbed(game);
    }

    private static bool Undisturbed(GameManager game)
    {
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

    /// <summary>Something bigger than a look starts: it gets the camera at once.</summary>
    private static bool Bigger(GameManager game)
    {
        var tm = TowerManager.Instance;
        return game.Raid.Started || game.Dungeons.Started || game.StreamRaid.IsWar
            || (tm && (tm.Opening || tm.Phase == TowerManager.TowerPhase.Countdown || tm.Phase == TowerManager.TowerPhase.Fight));
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
                // from out over the sea in front, a little to one side, with the whole stack in the picture (the
                // camera's 60 degrees from the foot to over the last section): it drifts in and rises a little as the
                // light climbs, and the sky over the tower is left for the title
                Vector3 Out(float degrees) => front * Mathf.Cos(degrees * Mathf.Deg2Rad) + side * Mathf.Sin(degrees * Mathf.Deg2Rad);
                momentPositions[0] = foot + Out(15f) * (BuiltHeight * 1.3f) + Vector3.up * 26f;
                momentPositions[1] = foot + Out(8f) * (BuiltHeight * 1.14f) + Vector3.up * 58f;
                momentForwards[0] = (foot + Vector3.up * (BuiltHeight * 0.47f) - momentPositions[0]).normalized;
                momentForwards[1] = (foot + Vector3.up * (BuiltHeight * 0.55f) - momentPositions[1]).normalized;
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
        momentClimbed = false;
        momentByHand = byHand;
        skipped = false;
        momentTime = 0f;
        stageTime = 0f;
        // one tried by hand (the tests) runs its time whatever the mouse does
        momentQuietFrom = byHand ? float.MaxValue : Time.unscaledTime + 0.4f;

        if (kind == Moment.Idle)
        {
            // straight onto the tower, as it always was
            stage = Stage.Shot;
            Place(0f);
            nextLook = 0f;
            Look();
            return true;
        }
        if (kind == Moment.FloorLit)
        {
            // the screen steps aside as for the tower's cutscenes; the title waits for the light
            if (byHand) holdFloor = floor + 1;
            var open = Tower.FloorInfo(floor + 1);
            var line = open != null ? string.Format(Localization.UI_TOWER_FLOOR_OPEN, open.Number, open.Name) : Localization.UI_TOWER_FLOORS_SEALED;
            screen = CutsceneScreen.Show(string.Format(Localization.UI_TOWER_FLOOR_CLEARED, floor), line, CutsceneScreen.Gold, CutsceneScreen.GoldOutline, Skip, TitleSize, top: true);
            screen.SetTitle(0f, 0f);
        }
        // the boss's fall already ended in black: straight onto the tower; else the picture goes dark first, the
        // camera held where it was
        if (preFaded || ScreenFade.Alpha > 0.98f) BeginShot();
        else
        {
            stage = Stage.Out;
            ScreenFade.To(1f, kind == Moment.FloorLit ? FadeSeconds : EnteringFade);
        }
        preFaded = false;
        return true;
    }

    /// <summary>From the dark onto the tower: the camera jumps to the shot, which fades in.</summary>
    private void BeginShot()
    {
        stage = Stage.Shot;
        stageTime = 0f;
        momentTime = 0f;
        Place(0f);
        // the camera has jumped, perhaps from kilometres off where the tower is not drawn: what shows is worked out again now
        nextLook = 0f;
        Look();
        var game = GameManager.Instance;
        if (moment == Moment.FloorLit && !(game && game.PotatoMode)) climb = new ClimbLight(this);
        ScreenFade.To(0f, moment == Moment.FloorLit ? FadeSeconds : EnteringFade);
    }

    private void Play()
    {
        var game = GameManager.Instance;
        var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        stageTime += dt;
        // something bigger starts, or the camera was taken: it gets the camera at once
        if (!game || !game.Camera || game.Camera.State != GameCameraType.Cinematic || Bigger(game))
        {
            EndMoment(true);
            return;
        }

        if (moment == Moment.Idle)
        {
            momentTime += dt;
            // the streamer touches anything: the camera is theirs again
            var touched = Time.unscaledTime > momentQuietFrom && (InputActivity.IdleSeconds < 0.2f || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space));
            if (momentTime >= momentSeconds || touched) { EndMoment(true); return; }
            Place(TowerCutscene.Ease(Mathf.Clamp01(momentTime / momentSeconds)));
            return;
        }

        if (moment == Moment.FloorLit)
        {
            // a cutscene: its Skip button, Esc or Space end it; the mouse and the other keys leave it alone
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) && !Keybinds.IsTyping)
            {
                EscapeKey.Claim();
                Skip();
            }
        }
        // the look from the gate: the streamer touching anything ends it, through the dark
        else if (!skipped && Time.unscaledTime > momentQuietFrom && (InputActivity.IdleSeconds < 0.2f || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
            Skip();
        if (moment == Moment.None) return;

        switch (stage)
        {
            case Stage.Out:
                if (stageTime < (moment == Moment.FloorLit ? FadeSeconds : EnteringFade)) break;
                if (skipped) ToBlack();
                else BeginShot();
                break;
            case Stage.Shot:
            case Stage.Leaving:
                momentTime += dt;
                Place(PathAt(momentTime));
                if (moment == Moment.FloorLit) FloorLight(dt);
                if (stage == Stage.Shot && momentTime >= ShotLength()) Leave(moment == Moment.FloorLit ? FadeSeconds : EnteringFade);
                else if (stage == Stage.Leaving && stageTime >= leaveSeconds) ToBlack();
                break;
            case Stage.Black:
                // the players are still on their way to the new floor's town: the dark waits for them, a while
                var tm = TowerManager.Instance;
                if (moment == Moment.FloorLit && !skipped && tm && tm.MovingToNewTown && stageTime < LongestBlack) break;
                Handover();
                break;
        }
    }

    /// <summary>How long the look stays on the tower: the floor's look a little longer while the players are still on their way.</summary>
    private float ShotLength()
    {
        if (moment != Moment.FloorLit) return momentSeconds;
        var tm = TowerManager.Instance;
        return !skipped && !momentByHand && tm && tm.MovingToNewTown ? LongestShot : momentSeconds;
    }

    /// <summary>Where along the path the camera is after <paramref name="t"/> seconds on the tower.</summary>
    private float PathAt(float t)
    {
        if (moment != Moment.FloorLit) return TowerCutscene.Ease(Mathf.Clamp01(t / momentSeconds));
        // the floor's look drifts on however long it holds, slowing as it goes (it may hold longer than planned)
        return Mathf.Sin(Mathf.Clamp01(t / LongestShot) * Mathf.PI * 0.5f);
    }

    /// <summary>The floor's look on the tower: the flare, the light that climbs, the next floor lit, the title.</summary>
    private void FloorLight(float dt)
    {
        var t = momentTime;
        var next = momentFloor + 1;
        var open = Tower.FloorInfo(next) != null;
        if (!momentFlared && t >= FlareAt)
        {
            momentFlared = true;
            Flare(momentFloor);
            climb?.Burst(SectionBottom(momentFloor) + 6f, SectionTop(momentFloor) - 4f, FloorColor(momentFloor), 1f);
        }
        if (!momentClimbed && t >= ClimbFrom)
        {
            // from the middle of the cleared floor to the middle of the next (or to the foot of a floor not open yet)
            var from = SectionMiddle(momentFloor);
            var to = open ? SectionMiddle(next) : SectionBottom(next) + 4f;
            var k = Mathf.Clamp01((t - ClimbFrom) / (ClimbTo - ClimbFrom));
            var eased = k * k * (3f - 2f * k);
            var y = Mathf.Lerp(from, to, eased);
            climb?.Band(y, Color.Lerp(FloorColor(momentFloor), open ? FloorColor(next) : CutsceneScreen.Gold, eased), dt);
            if (k >= 1f) Climbed(true);
        }
        if (screen != null)
        {
            var alpha = Mathf.Clamp01((t - TitleAt) / TitleIn);
            screen.SetTitle(alpha * alpha * (3f - 2f * alpha), (t - TitleAt) / 4f);
        }
    }

    /// <summary>The light has reached the next floor: it lights up in its own color (or, not open yet, the light spends itself at its foot).</summary>
    private void Climbed(bool show)
    {
        if (momentClimbed) return;
        momentClimbed = true;
        var next = momentFloor + 1;
        var open = Tower.FloorInfo(next) != null;
        holdFloor = 0;
        if (open)
        {
            // lit now, before the server's answer if it has not come yet (Progress lets go once it has)
            if (!momentByHand) forcedLit = next;
            foreach (var section in Sections)
            {
                if (section.Floor != next) continue;
                if (show) section.Quick = Time.time + 2f;
                section.Flash = 1f;
            }
            if (show) climb?.Burst(SectionBottom(next) + 4f, SectionTop(next) - 4f, FloorColor(next), 1.3f);
        }
        else if (show) climb?.Burst(SectionBottom(next) - 2f, SectionBottom(next) + 12f, CutsceneScreen.Gold, 0.6f);
    }

    /// <summary>Skip, Esc or Space (or the streamer touching anything on the look from the gate): through the dark to the end.</summary>
    private void Skip()
    {
        if (moment == Moment.None) return;
        skipped = true;
        switch (stage)
        {
            case Stage.Shot: Leave(moment == Moment.FloorLit ? SkipFade : 0.3f); break;
            case Stage.Black: Handover(); break;
            // fading out: it goes on to the dark, then the end
        }
    }

    private void Leave(float seconds)
    {
        stage = Stage.Leaving;
        stageTime = 0f;
        leaveSeconds = seconds;
        ScreenFade.To(1f, seconds);
    }

    private void ToBlack()
    {
        stage = Stage.Black;
        stageTime = 0f;
        screen?.SetTitle(0f, 1f);
    }

    /// <summary>From the dark: the camera goes back to who or what it was on (or a new town's first-visit cutscene takes it), and the picture comes back.</summary>
    private void Handover()
    {
        var floorLook = moment == Moment.FloorLit;
        Wrap();
        moment = Moment.None;
        var game = GameManager.Instance;
        if (game && game.Camera)
        {
            if (momentPlayer && !momentPlayer.isDestroyed) game.Camera.StopCinematic(momentPlayer);
            else game.Camera.StopCinematic(momentIsland);
        }
        momentPlayer = null;
        momentIsland = null;
        var tm = TowerManager.Instance;
        // the town's own cutscene starts on its first pose: it shows at once; else a moment for the camera to settle
        var arrival = floorLook && tm && tm.PlayDueArrival();
        ScreenFade.To(0f, floorLook ? FadeSeconds : EnteringFade, arrival ? 0f : 0.3f);
    }

    /// <summary>The look stops at once (something bigger starts, or the tower goes): the floors are lit as they should be.</summary>
    private void EndMoment(bool giveBack)
    {
        if (moment == Moment.None) return;
        var kind = moment;
        Wrap();
        moment = Moment.None;
        if (kind != Moment.Idle)
        {
            if (giveBack) ScreenFade.To(0f, 0.25f);
            else ScreenFade.Clear();
        }
        var game = GameManager.Instance;
        if (!giveBack || !game || !game.Camera) return;
        if (momentPlayer && !momentPlayer.isDestroyed) game.Camera.StopCinematic(momentPlayer);
        else game.Camera.StopCinematic(momentIsland);
        momentPlayer = null;
        momentIsland = null;
        if (kind == Moment.FloorLit && TowerManager.Instance) TowerManager.Instance.PlayDueArrival();
    }

    /// <summary>What the floor's look leaves behind, however it ends: the cleared floor flared, the next one lit, the screen back.</summary>
    private void Wrap()
    {
        if (moment == Moment.FloorLit)
        {
            if (!momentFlared)
            {
                momentFlared = true;
                Flare(momentFloor);
            }
            Climbed(false);
        }
        holdFloor = 0;
        climb?.Destroy();
        climb = null;
        screen?.Restore();
        screen = null;
    }

    private void Place(float u)
    {
        if (!view) return;
        var (position, rotation) = TowerCutscene.Evaluate(momentPositions, momentForwards, u);
        view.transform.SetPositionAndRotation(position, rotation);
    }

    // ---- The tower's shape, for aiming and for the light ------------------------------------------------------

    // Heights over the sea where each floor's section begins (the last is the top of the last section) and the wall's
    // radius there, as the tower stands in the world: tools/tower/world/stack-built.json ("base" and "seat" times its
    // "scale" of 1.2). Floor 4's section is the Dark Fortress, not open yet.
    private static readonly float[] Levels = { 0f, 108f, 192f, 266.4f, 367.2f };
    private static readonly float[] Radii = { 96f, 91.7f, 70.3f, 52.3f, 48.7f };

    // the color each floor's section glows in (stack.json's "glow"): the light that climbs takes it on
    private static readonly Color[] Glows =
    {
        new Color(1f, 0.72f, 0.28f), new Color(0.45f, 0.82f, 1f), new Color(0.78f, 0.62f, 1f), new Color(0.4f, 1f, 0.32f),
    };

    private float Scale => BuiltHeight / Levels[Levels.Length - 1];

    private float SectionBottom(int floor) => Levels[Mathf.Clamp(floor - 1, 0, Levels.Length - 1)] * Scale;
    private float SectionTop(int floor) => Levels[Mathf.Clamp(floor, 0, Levels.Length - 1)] * Scale;
    private float SectionMiddle(int floor) => (SectionBottom(floor) + SectionTop(floor)) * 0.5f;

    private static Color FloorColor(int floor) => floor >= 1 && floor <= Glows.Length ? Glows[floor - 1] : CutsceneScreen.Gold;

    /// <summary>The wall's radius at <paramref name="height"/> metres over the sea.</summary>
    private float WallRadius(float height)
    {
        var h = height / Scale;
        for (var i = 1; i < Levels.Length; i++)
            if (h <= Levels[i]) return Mathf.Lerp(Radii[i - 1], Radii[i], Mathf.InverseLerp(Levels[i - 1], Levels[i], h)) * Scale;
        return Radii[Radii.Length - 1] * Scale;
    }

    // ---- The light that climbs --------------------------------------------------------------------------------

    /// <summary>
    ///     The floor's look's particles: bursts of sparks round a section, and a band of light round the wall that
    ///     climbs with sparks flying off it. Emitted by hand where they are wanted, drawn additively, gone with the look.
    /// </summary>
    private sealed class ClimbLight
    {
        private static Material material;
        private static Texture2D dot;

        private readonly WorldTower tower;
        private readonly GameObject root;
        private readonly ParticleSystem glow, sparks;
        private float lastY = float.NaN;

        public ClimbLight(WorldTower tower)
        {
            this.tower = tower;
            root = new GameObject("World Tower Climbing Light");
            SceneManager.MoveGameObjectToScene(root, tower.gameObject.scene);
            glow = Make("Glow", 0f, false);
            sparks = Make("Sparks", 0.55f, true);
        }

        private ParticleSystem Make(string name, float gravity, bool shrink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 30f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            if (shrink)
            {
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));
            }
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 1f;
            ps.Play();
            return ps;
        }

        /// <summary>A burst round the wall between two heights (m over the sea): sparks thrown out and up, and soft light.</summary>
        public void Burst(float low, float high, Color color, float strength)
        {
            var foot = tower.transform.position;
            // sparks thrown out and up, far enough to clear what stands round the wall (the storm's cloud ring)
            var sparksCount = Mathf.RoundToInt(520 * strength);
            for (var i = 0; i < sparksCount; i++)
            {
                var y = Random.Range(low, high);
                var (at, outward) = Round(foot, y, tower.WallRadius(y) + Random.Range(2f, 14f));
                var velocity = outward * Random.Range(14f, 48f) + Vector3.up * Random.Range(6f, 30f);
                Emit(sparks, at, velocity, Random.Range(4f, 7.5f), Random.Range(1.4f, 2.8f), Color.Lerp(color, Color.white, Random.Range(0.15f, 0.65f)));
            }
            // soft light hugging the wall, and a ring of it rolling outward
            var puffs = Mathf.RoundToInt(90 * strength);
            var c = color;
            c.a = 0.6f;
            for (var i = 0; i < puffs; i++)
            {
                var y = Random.Range(low, high);
                var (at, outward) = Round(foot, y, tower.WallRadius(y) + 6f);
                Emit(glow, at, outward * Random.Range(2f, 8f) + Vector3.up * Random.Range(0f, 5f), Random.Range(34f, 58f), Random.Range(0.9f, 1.5f), c);
            }
            var wave = Mathf.RoundToInt(110 * strength);
            var middle = (low + high) * 0.5f;
            c.a = 0.45f;
            for (var i = 0; i < wave; i++)
            {
                var (at, outward) = Round(foot, middle + Random.Range(-4f, 4f), tower.WallRadius(middle) + 8f);
                Emit(glow, at, outward * Random.Range(55f, 70f), Random.Range(26f, 38f), Random.Range(0.9f, 1.1f), c);
            }
        }

        /// <summary>The band at <paramref name="y"/> this frame: soft light round the wall, a trail below it, sparks flying off.</summary>
        public void Band(float y, Color color, float dt)
        {
            var foot = tower.transform.position;
            if (float.IsNaN(lastY)) lastY = y;
            var lights = Mathf.Max(1, Mathf.RoundToInt(1400f * dt));
            var c = color;
            c.a = 0.85f;
            for (var i = 0; i < lights; i++)
            {
                var h = Mathf.Lerp(lastY, y, Random.value);
                var (at, outward) = Round(foot, h, tower.WallRadius(h) + 6f);
                Emit(glow, at, outward * Random.Range(0f, 2f) + Vector3.up * Random.Range(0f, 4f), Random.Range(17f, 27f), Random.Range(0.5f, 0.9f), c);
            }
            // a white-hot edge on top of it
            var hot = Mathf.Max(1, Mathf.RoundToInt(500f * dt));
            var white = Color.Lerp(color, Color.white, 0.6f);
            white.a = 0.9f;
            for (var i = 0; i < hot; i++)
            {
                var h = Mathf.Lerp(lastY, y, Random.value) + 3f;
                var (at, outward) = Round(foot, h, tower.WallRadius(h) + 7f);
                Emit(glow, at, Vector3.up * Random.Range(0f, 2f), Random.Range(8f, 12f), Random.Range(0.25f, 0.4f), white);
            }
            var flying = Mathf.Max(1, Mathf.RoundToInt(700f * dt));
            for (var i = 0; i < flying; i++)
            {
                var h = Mathf.Lerp(lastY, y, Random.value);
                var (at, outward) = Round(foot, h, tower.WallRadius(h) + 5f);
                Emit(sparks, at, outward * Random.Range(3f, 14f) + Vector3.up * Random.Range(12f, 34f), Random.Range(3.5f, 6f), Random.Range(0.9f, 1.8f),
                    Color.Lerp(color, Color.white, Random.Range(0.3f, 0.7f)));
            }
            lastY = y;
        }

        private static (Vector3 At, Vector3 Outward) Round(Vector3 foot, float y, float radius)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            return (foot + outward * radius + Vector3.up * y, outward);
        }

        private static void Emit(ParticleSystem ps, Vector3 at, Vector3 velocity, float size, float life, Color color)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = at,
                velocity = velocity,
                startSize = size,
                startLifetime = life,
                startColor = color,
                applyShapeToPosition = false,
            };
            ps.Emit(p, 1);
        }

        public void Destroy()
        {
            if (root) Object.Destroy(root);
        }

        // additive, soft round light with a bright middle (the hero's trail material with a dot of its own)
        private static Material Material()
        {
            if (material) return material;
            if (!dot)
            {
                const int S = 64;
                dot = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "World Tower Light Dot", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var pixels = new Color32[S * S];
                for (var y = 0; y < S; y++)
                for (var x = 0; x < S; x++)
                {
                    var dx = (x + 0.5f) / S * 2f - 1f;
                    var dy = (y + 0.5f) / S * 2f - 1f;
                    var d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    var a = Mathf.Pow(1f - d, 2.2f) * 0.8f + Mathf.Pow(Mathf.Clamp01(1f - d * 3f), 2f) * 0.2f;
                    pixels[y * S + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(a)));
                }
                dot.SetPixels32(pixels);
                dot.Apply();
            }
            var template = Resources.Load<Material>("HeroTrail");
            material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
            material.name = "World Tower Climbing Light";
            material.hideFlags = HideFlags.DontSave;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", dot);
            material.mainTexture = dot;
            return material;
        }
    }
}
