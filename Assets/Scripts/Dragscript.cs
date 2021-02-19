
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

        this.transform.localPosition = new Vector3(lpx, lpy, lp.z);
    }

    private Vector2 Size => PlayerDetails.IsExpanded ? new Vector2(size.x, screenH) : size;

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
        var eight = size.y / 8f;
        var bX0 = -(screenW / 2);
        var bX1 = (screenW - size.x) + bX0;
        var bY0 = -eight;
        var bY1 = screenH - size.y - eight;


        if (newPos.x < bX0)
            newPos.x = bX0;
        if (newPos.x > bX1)
            newPos.x = bX1;

        if (PlayerDetails.IsExpanded)
        {
            newPos.y = bY1;
        }
        else
        {
            if (newPos.y < bY0)
                newPos.y = bY0;
            if (newPos.y > bY1)
                newPos.y = bY1;
        }

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

        UnityEngine.PlayerPrefs.SetFloat(keyX, pos.x);
        UnityEngine.PlayerPrefs.SetFloat(keyY, pos.y);

    }


    #endregion

}