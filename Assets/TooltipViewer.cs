using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TooltipViewer : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lblHeader;
    [SerializeField] private TMPro.TextMeshProUGUI lblBody;

    private volatile int refIndex;
    private RectTransform rect;
    private bool disabled;
    private float visibleTimer = 0f;

    void Start()
    {
        this.rect = GetComponent<RectTransform>();
    }

    internal void Hide(int refIndex)
    {
        Hide();
    }

    private void Hide()
    {
        this.transform.position = new Vector3(9999, 9999);
    }

    private void Update()
    {
        if (visibleTimer > 0)
        {
            visibleTimer -= Time.deltaTime;
            if (visibleTimer <= 0)
            {
                Hide();
            }
        }
    }

    internal int Show(Vector3 position, string title, string body)
    {
        lblHeader.text = title;
        lblBody.text = body;

        if (disabled) return 0;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        StartCoroutine(ShowTooltip(position));
        
        visibleTimer = 5f;

        var ri = ++refIndex;
        return ri;
    }

    private IEnumerator ShowTooltip(Vector3 position)
    {
        if (disabled) yield break;
        // WTF ?? why would I need to do this shit.
        // just to make sure the damn canvas is updated
        for (var i = 0; i < 2; ++i)
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
        this.transform.position = position;
    }

    internal void Disable()
    {
        disabled = true;
        Hide();
    }

    internal void Enable()
    {
        disabled = false;
    }
}
