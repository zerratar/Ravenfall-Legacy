using UnityEngine;

/// <summary>
///     A tower place's extra light and flame (ghostfire, candle flames, fill lights, moonbeams): what costs frames
///     without changing what happens. Its children are shown unless potato mode is on (the user: what costs FPS follows
///     potato mode); the place's key lights are not under it and always shine.
/// </summary>
public sealed class TowerFx : MonoBehaviour
{
    private GameManager game;
    private bool? shown;
    private float nextCheck;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 1f;
        if (!game) game = GameManager.Instance;
        var on = !(game && game.PotatoMode);
        if (shown == on) return;
        shown = on;
        foreach (Transform child in transform) child.gameObject.SetActive(on);
    }
}
