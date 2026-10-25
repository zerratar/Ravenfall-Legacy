using UnityEngine;

/// <summary>
/// Keys and clicks for build mode: none while a text field has the keyboard, and a right click only when it was a
/// click, not the camera turning.
/// </summary>
public static class BuildInput
{
    private const float ClickPixels = 8f;
    private const float ClickSeconds = 0.4f;

    private static Vector3 rightDownAt;
    private static float rightDownTime;
    private static int rightFrame = -1;
    private static bool rightClicked;

    /// <summary>The key went down this frame, and nobody is typing (the build tray's filter, a settings field).</summary>
    public static bool KeyDown(KeyCode key) => !Keybinds.IsTyping && Input.GetKeyDown(key);

    /// <summary>
    /// The right button was let go this frame where it went down, a moment ago: a click. The camera turns while the
    /// right button is held, so taking the press as a click dropped the decor in hand on every turn (October 2026).
    /// Called every frame by whoever uses it, so the press is seen.
    /// </summary>
    public static bool RightClicked()
    {
        if (rightFrame != Time.frameCount)
        {
            rightFrame = Time.frameCount;
            if (Input.GetMouseButtonDown(1))
            {
                rightDownAt = Input.mousePosition;
                rightDownTime = Time.unscaledTime;
            }
            rightClicked = Input.GetMouseButtonUp(1)
                && (Input.mousePosition - rightDownAt).sqrMagnitude <= ClickPixels * ClickPixels
                && Time.unscaledTime - rightDownTime <= ClickSeconds;
        }
        return rightClicked;
    }
}
