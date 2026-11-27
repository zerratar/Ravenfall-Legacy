using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
///     Black over the whole screen that fades in and out (the world tower's looks, WorldTowerMoments): a UI Toolkit
///     panel of its own, drawn over every other one, so the game's windows go dark with the picture and nothing shows
///     through while it is black. Clicks go through it (the cutscene's Skip button works under it), and
///     <see cref="CutsceneScreen"/> leaves it alone when it hides the screen, so a fade can carry on from one cutscene
///     into the next. Without the game's UI (no panel to copy) there is no fade: the cuts are hard.
/// </summary>
public sealed class ScreenFade : MonoBehaviour
{
    private static ScreenFade instance;

    private UIDocument document;
    private PanelSettings panel;
    private VisualElement black;
    private float alpha, from, to, start, seconds, delay;

    /// <summary>How dark the screen is: 0 clear, 1 black.</summary>
    public static float Alpha => instance ? instance.alpha : 0f;

    /// <summary>The panel is this fade's own (CutsceneScreen does not hide it).</summary>
    public static bool Owns(UIDocument doc) => instance && doc && doc == instance.document;

    /// <summary>Fades to <paramref name="target"/> (1 black, 0 clear) over <paramref name="seconds"/>, after <paramref name="wait"/> seconds.</summary>
    public static void To(float target, float seconds, float wait = 0f)
    {
        target = Mathf.Clamp01(target);
        if (!instance && (target <= 0f || !Create())) return;
        instance.from = instance.alpha;
        instance.to = target;
        instance.start = Time.unscaledTime;
        instance.seconds = Mathf.Max(0f, seconds);
        instance.delay = Mathf.Max(0f, wait);
        instance.Step();
    }

    /// <summary>Clear at once.</summary>
    public static void Clear()
    {
        if (!instance) return;
        instance.alpha = instance.from = instance.to = 0f;
        instance.Apply();
    }

    private static bool Create()
    {
        // the game's own panel is copied (its scale and theme), drawn over everything else
        UIDocument host = null;
        var order = 0f;
        foreach (var doc in FindObjectsByType<UIDocument>())
        {
            if (!doc.panelSettings) continue;
            order = Mathf.Max(order, doc.panelSettings.sortingOrder);
            var root = doc.rootVisualElement;
            if (!host && root != null && root.Q<VisualElement>("conn-layer") != null) host = doc;
        }
        if (!host) return false;
        var go = new GameObject("Screen Fade");
        go.SetActive(false);
        var fade = go.AddComponent<ScreenFade>();
        fade.panel = Instantiate(host.panelSettings);
        fade.panel.name = "Screen Fade";
        fade.panel.hideFlags = HideFlags.DontSave;
        fade.panel.sortingOrder = order + 100f;
        fade.panel.clearColor = false;
        fade.document = go.AddComponent<UIDocument>();
        fade.document.panelSettings = fade.panel;
        go.SetActive(true);
        var top = fade.document.rootVisualElement;
        if (top == null)
        {
            Destroy(go);
            return false;
        }
        top.pickingMode = PickingMode.Ignore;
        top.style.position = Position.Absolute;
        top.style.left = 0; top.style.right = 0; top.style.top = 0; top.style.bottom = 0;
        fade.black = new VisualElement { name = "screen-fade", pickingMode = PickingMode.Ignore };
        fade.black.style.position = Position.Absolute;
        fade.black.style.left = 0; fade.black.style.right = 0; fade.black.style.top = 0; fade.black.style.bottom = 0;
        fade.black.style.backgroundColor = Color.black;
        top.Add(fade.black);
        instance = fade;
        fade.Apply();
        return true;
    }

    private void Update() => Step();

    private void Step()
    {
        var t = Time.unscaledTime - start - delay;
        if (t < 0f) return;
        var k = seconds > 0f ? Mathf.Clamp01(t / seconds) : 1f;
        // gentle at both ends
        k = k * k * (3f - 2f * k);
        var a = Mathf.Lerp(from, to, k);
        if (Mathf.Approximately(a, alpha) && black != null && black.style.opacity.value == a) return;
        alpha = a;
        Apply();
    }

    private void Apply()
    {
        if (black == null) return;
        black.style.opacity = alpha;
        black.style.display = alpha > 0.001f ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (panel) Destroy(panel);
    }
}
