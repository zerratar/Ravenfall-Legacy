using System;
using UnityEngine;

/// <summary>
///     The tower's cutscenes (the user, 2026-11-17; Ravebridge's rising was a hit): the first time a stream opens the
///     tower, the camera comes in over Mossgate and rises up the tower into the haze ("The Tower opens its gate"); the
///     first time a stream reaches a new band's town (after the band's last boss), a fly-over of the town with its name.
///     The camera glides along a smooth path through the town's poses (<see cref="TowerTown.Cutscene"/>), easing in and
///     out, and the name shows over the last seconds. The screen steps aside as for Ravebridge (CutsceneScreen); Esc,
///     Space or the Skip button go to the name. Nothing in the world waits for it: players walk into the town meanwhile.
/// </summary>
public sealed class TowerCutscene : MonoBehaviour
{
    /// <summary>How long the name shows at the end, and how long it takes to come and go.</summary>
    public const float TitleSeconds = 4f, TitleIn = 1f, TitleOut = 0.8f;

    public static bool Playing => current;
    private static TowerCutscene current;

    private Vector3[] positions;
    private Vector3[] forwards;
    private float seconds;
    private float time;
    private bool finished;
    private bool boomDone;
    private Transform cam;
    private CutsceneScreen screen;
    private AudioSource sound;
    private Func<PlayerController> after;
    private Action done;

    /// <summary>
    ///     Plays a path through <paramref name="poses"/> (two at least) over <paramref name="seconds"/>, the name over its
    ///     last seconds; at the end the camera goes to the player <paramref name="after"/> gives (or the islands) and
    ///     <paramref name="done"/> runs. False when it cannot play: the camera is not free (a raid, a dungeon, a war, a
    ///     character played by hand) or another cutscene is on.
    /// </summary>
    public static bool Play(MonoBehaviour host, Transform[] poses, float seconds, string name, string line, Color color, Color outline,
        Func<PlayerController> after, Action done)
    {
        if (current || RavebridgeReveal.Playing || !host || poses == null || poses.Length < 2) return false;
        var game = GameManager.Instance;
        if (!game || !game.Camera || !game.Camera.StartCinematic()) return false;
        current = host.gameObject.AddComponent<TowerCutscene>();
        current.Begin(poses, seconds, name, line, color, outline, after, done);
        return true;
    }

    /// <summary>Straight to the end (the tower closed meanwhile).</summary>
    public static void Stop()
    {
        if (current) current.Finish();
    }

    private void Begin(Transform[] poses, float seconds, string name, string line, Color color, Color outline, Func<PlayerController> after, Action done)
    {
        // copied: the town may go while it plays
        positions = new Vector3[poses.Length];
        forwards = new Vector3[poses.Length];
        for (var i = 0; i < poses.Length; i++)
        {
            positions[i] = poses[i].position;
            forwards[i] = poses[i].forward;
        }
        this.seconds = Mathf.Max(TitleSeconds + 2f, seconds);
        this.after = after;
        this.done = done;
        cam = GameManager.Instance.Camera.transform;
        screen = CutsceneScreen.Show(name, line, color, outline, SkipNow);
        sound = gameObject.AddComponent<AudioSource>();
        sound.spatialBlend = 0f;
        sound.playOnAwake = false;
        // the thunder pitched down: a deep boom as the name comes, as for Ravebridge
        sound.pitch = 0.55f;
        Place();
    }

    private void LateUpdate()
    {
        if (finished) return;
        time += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) && !Keybinds.IsTyping)
        {
            EscapeKey.Claim();
            SkipNow();
        }
        Place();

        var start = seconds - TitleSeconds;
        var alpha = Mathf.Clamp01((time - start) / TitleIn) * Mathf.Clamp01((seconds - time) / TitleOut);
        screen?.SetTitle(alpha, (time - start) / TitleSeconds);
        if (!boomDone && time >= start)
        {
            boomDone = true;
            var props = RavebridgeRevealProps.Get();
            if (props && props.Boom) sound.PlayOneShot(props.Boom, GameSounds.Volume * 0.6f);
        }
        if (time >= seconds) Finish();
    }

    private void Place()
    {
        if (!cam) return;
        var (position, rotation) = Evaluate(positions, forwards, Ease(Mathf.Clamp01(time / seconds)));
        cam.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Gentle at both ends: the camera sets off and settles on the last pose, where the name shows.</summary>
    public static float Ease(float x) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * x);

    /// <summary>
    ///     The camera at <paramref name="u"/> (0 to 1) along the path through the poses: a Catmull-Rom curve through the
    ///     positions, at an even pace over the distance, and one through the directions they look.
    /// </summary>
    public static (Vector3 Position, Quaternion Rotation) Evaluate(Vector3[] positions, Vector3[] forwards, float u)
    {
        var n = positions.Length;
        if (n == 1) return (positions[0], Quaternion.LookRotation(forwards[0]));
        // the distance along the path, so a long leg does not rush and a short one does not crawl
        var total = 0f;
        for (var i = 1; i < n; i++) total += Vector3.Distance(positions[i - 1], positions[i]);
        var at = Mathf.Clamp01(u) * total;
        var segment = 0;
        var t = 0f;
        for (var i = 1; i < n; i++)
        {
            var length = Vector3.Distance(positions[i - 1], positions[i]);
            if (at <= length || i == n - 1)
            {
                segment = i - 1;
                t = length > 0f ? Mathf.Clamp01(at / length) : 1f;
                break;
            }
            at -= length;
        }
        var position = CatmullRom(positions, segment, t);
        var forward = CatmullRom(forwards, segment, t);
        if (forward.sqrMagnitude < 1e-6f) forward = forwards[segment + 1];
        return (position, Quaternion.LookRotation(forward.normalized, Vector3.up));
    }

    private static Vector3 CatmullRom(Vector3[] p, int segment, float t)
    {
        var n = p.Length;
        var p1 = p[segment];
        var p2 = p[segment + 1];
        // past the ends: the line carried on
        var p0 = segment > 0 ? p[segment - 1] : 2f * p1 - p2;
        var p3 = segment + 2 < n ? p[segment + 2] : 2f * p2 - p1;
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    private void SkipNow()
    {
        if (finished) return;
        // the name for a moment, then the end
        time = Mathf.Max(time, seconds - TitleIn - 1.2f);
        boomDone = true;
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        screen?.Restore();
        screen = null;
        PlayerController player = null;
        try { player = after?.Invoke(); }
        catch (Exception exc) { Shinobytes.Debug.LogError("TowerCutscene: " + exc.Message); }
        var game = GameManager.Instance;
        if (game && game.Camera) game.Camera.StopCinematic(player);
        if (current == this) current = null;
        var callback = done;
        done = null;
        Destroy(this);
        // the boom rings out
        if (sound) Destroy(sound, 6f);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        // play mode stopped or the game shut down in the middle
        if (finished) return;
        finished = true;
        screen?.Restore();
        if (current == this) current = null;
    }
}
