using UnityEngine;

/// <summary>
/// The tavern by lamplight: while the camera is inside, the light is a warm indoor one, the same whatever the time
/// of day and the weather outside. The Halloween theme freezes the time at night for its spooky windows, and the
/// moonlit night that suits the islands left the room near black (found on stream, October 2026); it gets a purple
/// tint, just as bright: the tavern is always properly lit.
/// </summary>
/// <remarks>
/// Works like <see cref="DungeonMood"/>: after the day and night cycle has set the light each frame, easing over
/// about a second.
/// </remarks>
public sealed class TavernMood : MonoBehaviour
{
    private static readonly Color SkyAmbient = new Color(0.52f, 0.44f, 0.35f);
    private static readonly Color EquatorAmbient = new Color(0.47f, 0.38f, 0.28f);
    private static readonly Color GroundAmbient = new Color(0.26f, 0.20f, 0.14f);
    private const float Sun = 0.85f;
    private const float SunTemperature = 4300f;

    // Halloween: a little purple, as bright as any other night in the tavern
    private static readonly Color SpookySkyAmbient = new Color(0.50f, 0.42f, 0.52f);
    private static readonly Color SpookyEquatorAmbient = new Color(0.47f, 0.37f, 0.42f);
    private static readonly Color SpookyGroundAmbient = new Color(0.26f, 0.19f, 0.22f);
    private const float SpookySun = 0.85f;

    private GameManager game;
    private float blend;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("Tavern Mood") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<TavernMood>();
    }

    private void LateUpdate()
    {
        if (!game) game = FindAnyObjectByType<GameManager>();
        var tavern = game ? game.Tavern : null;
        var inside = tavern && tavern.IsActivated;
        blend = Mathf.MoveTowards(blend, inside ? 1f : 0f, Time.unscaledDeltaTime * 1.2f);
        if (blend <= 0f) return;

        var spooky = tavern && tavern.IsSpookyTheme;
        var t = blend * blend * (3f - 2f * blend);
        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, spooky ? SpookySkyAmbient : SkyAmbient, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, spooky ? SpookyEquatorAmbient : EquatorAmbient, t);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, spooky ? SpookyGroundAmbient : GroundAmbient, t);
        var sun = RenderSettings.sun;
        if (sun)
        {
            sun.intensity = Mathf.Lerp(sun.intensity, spooky ? SpookySun : Sun, t);
            sun.colorTemperature = Mathf.Lerp(sun.colorTemperature, SunTemperature, t);
            sun.color = Color.Lerp(sun.color, Color.white, t);
        }
    }
}
