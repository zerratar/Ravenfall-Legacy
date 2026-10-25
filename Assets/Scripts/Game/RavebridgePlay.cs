using UnityEngine;

/// <summary>
/// The streamer plays a character by hand on Ravebridge (phase 6c of docs/ravebridge-design.md): their own character
/// in the stream (saved to the server as always) or their Ravebridge character (6d). Begin takes the character to the
/// island if it is not there, follows it with the camera and hands it to ManualPlayerController; Stop gives it back
/// to automatic play. Esc stops (RavebridgeView), as does the character leaving or the event ending.
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
        Changed?.Invoke();
        return true;
    }

    public static void Stop()
    {
        if (HeroController.Active) HeroController.End();
        var player = Playing;
        Playing = null;
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
        Changed?.Invoke();
    }
}
