using UnityEngine;

/// <summary>
/// The notice while the game waits for the server to store the players before it closes. Drawn with IMGUI so it
/// needs nothing from the scene and shows even while the rest of the UI is shutting down.
/// </summary>
public sealed class ExitSavingOverlay : MonoBehaviour
{
    private string message;
    private GUIStyle style;
    private Texture2D background;

    public static ExitSavingOverlay Show(string message)
    {
        var go = new GameObject("Exit Saving Overlay");
        DontDestroyOnLoad(go);
        var overlay = go.AddComponent<ExitSavingOverlay>();
        overlay.message = message;
        return overlay;
    }

    private void OnGUI()
    {
        if (style == null)
        {
            background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            background.Apply();
            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(18, Screen.height / 36),
                normal = { textColor = Color.white, background = background }
            };
        }

        GUI.depth = -1000;
        var height = style.fontSize * 3f;
        GUI.Label(new Rect(0, (Screen.height - height) / 2f, Screen.width, height), message, style);
    }

    private void OnDestroy()
    {
        if (background) Destroy(background);
    }
}
