using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Makes a uGUI window draggable, remembers where it was left, and keeps it on screen.
/// </summary>
/// <remarks>
/// Four bugs are fixed here, which between them account for the windows people kept losing. Kept as
/// a repair rather than a rewrite because three windows still depend on it, and saved positions from
/// the old version have to keep working. The UI Toolkit equivalent is
/// <see cref="Shinobytes.UI.DraggableWindow"/>; this exists until the last of those three is ported.
///
/// <list type="number">
/// <item>
/// <b>Clamping only happened while dragging.</b> A saved position was applied on load untouched, so
/// a window placed near the right edge of a wide screen was off screen after relaunching on a
/// narrow one, with no way back short of clearing preferences. Now clamped on load as well.
/// </item>
/// <item>
/// <b>Nothing reacted to the game window being resized.</b> Same outcome without needing a restart.
/// Now re-checked whenever the canvas rect changes.
/// </item>
/// <item>
/// <b>The clamp read <c>sizeDelta</c> from the canvas.</b> For a stretched canvas that is (0,0), so
/// the bounds collapsed and, with min above max, <see cref="Mathf.Clamp"/> returned the max: the
/// window snapped to a fixed wrong spot instead of staying where it was dropped. It now reads
/// <c>rect.size</c>, which is the resolved size.
/// </item>
/// <item>
/// <b>Reset mixed coordinate spaces.</b> It restored <c>transform.position</c> (world) but wrote
/// that value into keys that are read back as <c>localPosition</c>, so resetting a window could move
/// it somewhere new on the next launch rather than back to its default. Everything is local now.
/// </item>
/// </list>
/// </remarks>
public class Dragscript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// How much of the window must stay inside the canvas. Without an allowance a window larger than
    /// the canvas has an empty valid range and cannot be positioned at all.
    /// </summary>
    private const float MinVisible = 64f;

    private Canvas canvas;
    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;

    private Vector3 lastMousePos;
    private Vector3 lastUiPos;

    private string keyX;
    private string keyY;

    /// <summary>The position the scene author gave this window, in local space.</summary>
    private Vector3 defaultLocalPosition;

    private Vector2 lastCanvasSize;

    public bool IsDragging { get; private set; }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        var canvasScaler = GetComponentInParent<CanvasScaler>();
        canvas = canvasScaler != null ? canvasScaler.GetComponent<Canvas>() : GetComponentInParent<Canvas>();
        canvasRectTransform = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        var n = this.name;
        this.keyX = "__" + n + "_win_x";
        this.keyY = "__" + n + "_win_y";

        // Captured before anything saved is applied, so this really is the authored default.
        this.defaultLocalPosition = transform.localPosition;

        var target = defaultLocalPosition;
        if (PlayerPrefs.HasKey(keyX)) target.x = PlayerPrefs.GetFloat(keyX);
        if (PlayerPrefs.HasKey(keyY)) target.y = PlayerPrefs.GetFloat(keyY);

        // Clamped rather than trusted. This is the fix for windows that came back off screen.
        transform.localPosition = Clamp(target);
        lastCanvasSize = CanvasSize();
    }

    private void Update()
    {
        if (IsDragging)
        {
            return;
        }

        // Cheap enough to poll: two floats compared per frame, and there is no resize event on
        // RectTransform to subscribe to.
        var size = CanvasSize();
        if (size == lastCanvasSize)
        {
            return;
        }

        lastCanvasSize = size;
        transform.localPosition = Clamp(transform.localPosition);
    }

    /// <summary>
    /// Returns the resolved canvas size, or zero when the canvas is not resolvable yet.
    /// </summary>
    private Vector2 CanvasSize()
    {
        return canvasRectTransform != null ? canvasRectTransform.rect.size : Vector2.zero;
    }

    /// <summary>
    /// Forces a local position inside the canvas.
    /// </summary>
    private Vector3 Clamp(Vector3 localPosition)
    {
        var canvasSize = CanvasSize();
        if (canvasSize.x <= 0 || canvasSize.y <= 0 || rectTransform == null)
        {
            // Nothing sensible to clamp against; leaving it alone beats moving it to a guess.
            return localPosition;
        }

        var size = rectTransform.rect.size;
        var halfX = Mathf.Min(size.x * 0.5f, Mathf.Max(0f, canvasSize.x * 0.5f - MinVisible));
        var halfY = Mathf.Min(size.y * 0.5f, Mathf.Max(0f, canvasSize.y * 0.5f - MinVisible));

        var minX = -canvasSize.x * 0.5f + halfX;
        var maxX = canvasSize.x * 0.5f - halfX;
        var minY = -canvasSize.y * 0.5f + halfY;
        var maxY = canvasSize.y * 0.5f - halfY;

        localPosition.x = Mathf.Clamp(localPosition.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        localPosition.y = Mathf.Clamp(localPosition.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        return localPosition;
    }

    /// <summary>
    /// Returns the window to its authored position and forgets the saved one.
    /// </summary>
    public void ResetPosition()
    {
        transform.localPosition = Clamp(defaultLocalPosition);

        // Deleted rather than overwritten with the default. Writing the default back means a later
        // change to the scene's layout would still be overridden by this stale value.
        PlayerPrefs.DeleteKey(keyX);
        PlayerPrefs.DeleteKey(keyY);
        PlayerPrefs.Save();
    }

    #region IBeginDragHandler implementation
    public void OnBeginDrag(PointerEventData eventData)
    {
        lastMousePos = eventData.position;
        lastUiPos = transform.localPosition;
        IsDragging = true;
    }
    #endregion

    #region IDragHandler implementation

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        var mousePos = eventData.position;
        var posDelta = (Vector3)mousePos - lastMousePos;

        // Screen pixels are not canvas units when a CanvasScaler is in play, and the old code
        // applied the raw pixel delta to a local position. On any resolution where the scale factor
        // was not exactly 1 the window drifted away from the cursor as you dragged, faster the
        // further you went. The original had unused scaleX and scaleY fields, so this was known
        // about and never wired up. Dividing by the resolved scale factor makes the window track
        // the pointer exactly.
        var scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        posDelta /= scaleFactor;

        var newPos = Clamp(lastUiPos + posDelta);

        transform.localPosition = newPos;
        lastUiPos = newPos;
        lastMousePos = mousePos;
    }

    #endregion

    #region IEndDragHandler implementation

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        var pos = transform.localPosition;

        PlayerPrefs.SetFloat(keyX, pos.x);
        PlayerPrefs.SetFloat(keyY, pos.y);
        PlayerPrefs.Save();
    }

    #endregion
}
