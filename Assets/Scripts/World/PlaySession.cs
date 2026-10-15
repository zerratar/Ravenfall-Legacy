using System;
using UnityEngine;

/// <summary>
/// Tells the effects that hook the render pipeline at the start of play (tilt shift, the clouds and their shadows,
/// ambient occlusion) when the session ends, so they let go and put back what they changed.
/// </summary>
/// <remarks>
/// Leaving play mode in the editor does not reload the scripts, so a handler added from a RuntimeInitializeOnLoadMethod
/// stays subscribed and goes on running in edit mode: it set the sun's cookie and switched the scene's cloud meshes off
/// there, where a save would keep it, and made a hidden volume each time play stopped. In a build this is the quit.
/// </remarks>
public static class PlaySession
{
    /// <summary>Once, as play stops (or the game quits); cleared after.</summary>
    public static event Action Ending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Begin()
    {
        Ending = null;
        Application.quitting -= End;
        Application.quitting += End;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= Changed;
        UnityEditor.EditorApplication.playModeStateChanged += Changed;
#endif
    }

#if UNITY_EDITOR
    private static void Changed(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) End();
    }
#endif

    private static void End()
    {
        var ending = Ending;
        Ending = null;
        ending?.Invoke();
    }
}
