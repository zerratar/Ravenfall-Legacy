using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Dragscript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 size;
    private CanvasScaler canvasScaler;
    private Canvas canvas;
    private Vector3 lastMousePos;
    private Vector3 lastUiPos;

    private float scaleX;
    private float scaleY;
    private float screenW;
    private float screenH;
    private string keyX;
    private string keyY;
    private Vector3 originalPosition;

    public bool IsDragging { get; private set; }

    void Start()
    {
        size = GetComponent<RectTransform>().sizeDelta;
        canvasScaler = GetComponentInParent<CanvasScaler>();
        canvas = canvasScaler.GetComponent<Canvas>();
        var refW = canvasScaler.referenceResolution.x;
        var refH = canvasScaler.referenceResolution.y;

        this.screenW = refW;
        this.screenH = refH;
        this.scaleX = Screen.width / refW;
        this.scaleY = Screen.height / refH;

        var n = this.name;
        this.keyX = "__" + n + "_win_x";
        this.keyY = "__" + n + "_win_y";

        var lp = transform.localPosition;
        var lpx = lp.x;
        var lpy = lp.y;
        if (PlayerPrefs.HasKey(keyX))
            lpx = PlayerPrefs.GetFloat(keyX);

        if (PlayerPrefs.HasKey(keyY))
            lpy = PlayerPrefs.GetFloat(keyY);

        this.originalPosition = this.transform.position;

        this.transform.localPosition = new Vector3(lpx, lpy, lp.z);
    }
    public void ResetPosition()
    {
        this.transform.position = this.originalPosition;

        PlayerPrefs.SetFloat(keyX, originalPosition.x);
        PlayerPrefs.SetFloat(keyY, originalPosition.y);
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
        var newPos = lastUiPos + posDelta;

        var rectTransform = GetComponent<RectTransform>();
        var canvasRectTransform = canvas.GetComponent<RectTransform>();

        // Calculate the boundaries
        var canvasSize = canvasRectTransform.sizeDelta;
        var halfSize = rectTransform.sizeDelta / 2f;

        var minX = -canvasSize.x / 2f + halfSize.x;
        var maxX = canvasSize.x / 2f - halfSize.x;
        var minY = -canvasSize.y / 2f + halfSize.y;
        var maxY = canvasSize.y / 2f - halfSize.y;

        // Clamp the new position within the boundaries
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

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
    }

    #endregion
}