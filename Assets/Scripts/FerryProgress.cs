using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FerryProgress : MonoBehaviour
{
    [SerializeField] private FerryController ferry;
    [SerializeField] private TextMeshProUGUI txtETA;
    [SerializeField] private RectTransform ferryImageTransform;
    [SerializeField] private float minX = -400f;
    [SerializeField] private float maxX = 400f;

    private bool goingBack = false;
    private bool triggerFlip = false;
    private float tmpProgress = 0f;
    private string etaFormat;

    private void Awake()
    {
        etaFormat = txtETA.text;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFerryETA();
        UpdateFerryPosition();
    }

    private void UpdateFerryETA()
    {
        var leavingEta = ferry.CurrentLeaveETA;
        var etaSeconds = leavingEta > 0 ? leavingEta : ferry.CurrentPathETA;
        var etaMinutes = 0f;
        if (etaSeconds >= 60f)
        {
            etaMinutes = etaSeconds / 60f;
            etaSeconds = etaSeconds % 60f;
        }
        var eta = etaMinutes.ToString("00") + ":" + etaSeconds.ToString("00");
        var state = leavingEta > 0 ? "Departing in" : "ETA";
        txtETA.text = string.Format(etaFormat, state, eta);
    }

    private void UpdateFerryPosition()
    {
        var progress = ferry.GetProgress();
        if (progress < 1f)
        {
            tmpProgress = progress;
        }

        if (progress >= 1f && !triggerFlip)
        {
            goingBack = !goingBack;
            tmpProgress = 0f;
            triggerFlip = true;
        }

        if (progress < 1f)
        {
            triggerFlip = false;
        }

        ferryImageTransform.localScale = new Vector3(goingBack ? -1 : 1, 1, 1);
        ferryImageTransform.localPosition = new Vector3(
            Mathf.Lerp(minX, maxX, goingBack ? 1f - tmpProgress : tmpProgress), 0, 0);
    }
}
