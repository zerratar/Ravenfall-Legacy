using UnityEngine;

/// <summary>
/// The streamer plays a character on Ravebridge (phase 6c of docs/ravebridge-design.md): their own character in the
/// stream (saved to the server as always) or their Ravebridge character (6d). The streamer is in Ravebridge, as the
/// hero, or not there at all (the user, 2026-11-03: no playing by hand outside hero mode, no character of theirs left
/// on the island unplayed). Begin takes the character to the island, follows it with the camera and makes it the
/// hero; Stop leaves Ravebridge: hero mode ends and the character goes back where it was (the Ravebridge character is
/// put away). Leaving hero mode by any way (the panel, !hero end) is leaving.
/// </summary>
public static class RavebridgePlay
{
    public static PlayerController Playing { get; private set; }

    // what the character trained before it was played: clicking things (and hero mode) change its task, and Stop
    // gives it back the one it had
    private static TaskType taskBefore;
    private static string argumentBefore;

    public static event System.Action Changed;

    public static bool Begin(PlayerController player)
    {
        var rb = RavebridgeManager.Instance;
        var game = GameManager.Instance;
        if (!rb || !rb.Active || !player || !game) return false;
        if (!rb.Contains(player) && rb.Join(player) != RavebridgeManager.JoinResult.Joined) return false;
        if (Playing && Playing != player) Stop();

        if (Playing != player)
        {
            taskBefore = player.GetTask();
            argumentBefore = player.GetTaskArgument();
        }
        rb.Hero = player;
        rb.Attended = true;
        player.taskTarget = null;
        player.SetDestination(player.Position);

        // the camera first: observing a character takes control away from it
        game.Camera.FollowControlled(player);
        player.EnableControl();
        Playing = player;
        // always as the hero: the wave runs and the bounties are there to start, or not, and the island to roam
        if (!HeroController.Active || HeroController.Active.Player != player) HeroController.Begin(player);
        // the first time: how hero play works (once per streamer)
        Shinobytes.UI.RavebridgeIntro.ShowFirstTime();
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Out of Ravebridge: hero mode ends, the character is given back to automatic play and, with
    /// <paramref name="leave"/>, goes back where it was (RavebridgeManager.Leave, which puts its task back too). The
    /// event leaving or forgetting the character stops without leaving again.
    /// </summary>
    public static void Stop(bool leave = true)
    {
        var player = Playing;
        Playing = null;
        if (HeroController.Active) HeroController.End(false);
        if (player)
        {
            player.RevokeControl();
            player.taskTarget = null;
            var task = taskBefore != TaskType.None ? taskBefore : player.GetTask();
            var argument = taskBefore != TaskType.None ? argumentBefore : player.GetTaskArgument();
            if (task != TaskType.None) player.SetTask(task, argument, true);
            taskBefore = TaskType.None;
        }
        var game = GameManager.Instance;
        if (game && game.Camera) game.Camera.StopFollowingControlled();
        var rb = RavebridgeManager.Instance;
        if (leave && player && rb && rb.Contains(player)) rb.Leave(player);
        Changed?.Invoke();
    }
}
