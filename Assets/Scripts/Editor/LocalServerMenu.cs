using UnityEditor;

/// <summary>
/// Ravenfall > Use Local RavenNest Server: points the game at a RavenNest running on this machine instead
/// of production, for testing server changes before they are deployed. Remembered per machine.
/// </summary>
public static class LocalServerMenu
{
    private const string Item = "Ravenfall/Use Local RavenNest Server";

    [MenuItem(Item)]
    private static void Toggle()
    {
        var on = !EditorPrefs.GetBool(RavenNest.SDK.Endpoints.LocalDevServer.PrefKey, false);
        EditorPrefs.SetBool(RavenNest.SDK.Endpoints.LocalDevServer.PrefKey, on);
        UnityEngine.Debug.Log(on ? "The game will use the local RavenNest server (http://localhost)." : "The game will use the production server.");
    }

    [MenuItem(Item, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(Item, EditorPrefs.GetBool(RavenNest.SDK.Endpoints.LocalDevServer.PrefKey, false));
        return true;
    }
}
