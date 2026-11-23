using UnityEditor;

/// <summary>
/// Ravenfall > Mute Chat In Play Mode: for tests in the editor. The live bot is connected in play mode, and a test
/// that sets AdminControlData.NoChatBotMessages from an eval sets it too late: the session's own announcements (and
/// anything said before the eval runs) reach the real Twitch chat. With this on, the flag is set when the scripts load
/// for play mode, before the scene and the sign in, so no chat message leaves the game in that play session.
/// Remembered per machine; off by default.
/// </summary>
[InitializeOnLoad]
public static class MuteChatInPlayMode
{
    private const string Item = "Ravenfall/Mute Chat In Play Mode";
    public const string PrefKey = "Ravenfall.MuteChatInPlayMode";

    static MuteChatInPlayMode()
    {
        // runs after every script load, including the one entering play mode (before the scene's Awake)
        if (EditorPrefs.GetBool(PrefKey, false) && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            AdminControlData.NoChatBotMessages = true;
            UnityEngine.Debug.Log("Chat is muted in this play session (Ravenfall > Mute Chat In Play Mode).");
        }
    }

    [MenuItem(Item)]
    private static void Toggle()
    {
        var on = !EditorPrefs.GetBool(PrefKey, false);
        EditorPrefs.SetBool(PrefKey, on);
        UnityEngine.Debug.Log(on ? "Chat will be muted in play mode." : "Chat is no longer muted in play mode.");
    }

    [MenuItem(Item, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(Item, EditorPrefs.GetBool(PrefKey, false));
        return true;
    }
}
