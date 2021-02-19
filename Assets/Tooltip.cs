using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipViewer tooltipViewer;

    public string Title;

    [TextArea(3, 10)]
    public string Body;

    private int refIndex;
    private RectTransform rect;
    private bool disabled;

    // Start is called before the first frame update
    void Start()
    {
        rect = this.GetComponent<RectTransform>();
        if (!tooltipViewer) tooltipViewer = FindObjectOfType<TooltipViewer>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (disabled) return;
        this.refIndex = tooltipViewer.Show(rect.position, Title, Body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipViewer.Hide(refIndex);
    }

    internal void Disable()
    {
        this.disabled = true;
    }

    internal void Enable()
    {
        this.disabled = false;
    }
}
