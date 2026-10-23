using System.Collections;
using UnityEngine;

/// <summary>
/// The sound goes quiet before the game closes. While Unity tears the game down it puts the master volume back to
/// full before every playing sound has stopped, so the music and the world came out at 100 % for a moment at every
/// exit, loud on stream. Silenced and paused first, there is nothing left to hear.
/// </summary>
public static class AudioQuiet
{
    // a pause left over from the last exit (or play session in the editor) must not mute this one
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => AudioListener.pause = false;

    /// <summary>Fades everything out over <paramref name="seconds"/>, then silences it.</summary>
    public static IEnumerator FadeOut(float seconds)
    {
        var start = AudioListener.volume;
        for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            AudioListener.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        Silence();
    }

    /// <summary>Nothing more is heard: the master volume at zero and every sound paused.</summary>
    public static void Silence()
    {
        AudioListener.volume = 0f;
        AudioListener.pause = true;
    }

#if UNITY_EDITOR
    // the editor keeps the listener's volume after play mode; give it back for previewing clips
    [UnityEditor.InitializeOnLoadMethod]
    private static void WatchPlayMode()
    {
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state != UnityEditor.PlayModeStateChange.EnteredEditMode) return;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        };
    }
#endif
}
