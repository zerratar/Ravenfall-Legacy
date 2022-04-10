using System;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private AnimationCurve fadeCurve;
    [SerializeField] private CanvasGroup canvasGroup;

    private float fadeTimer;
    private bool sentMidPoint;
    private bool sentCompleted;
    private bool broken;

    public bool FadeActive = false;

    public Action FadeCompleted;
    public Action FadeHalfWay;

    // Start is called before the first frame update
    void Start()
    {

        fadeTimer = 0;
        try
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null && fadeCurve != null && canvasGroup)
            {
                canvasGroup.alpha = fadeCurve.Evaluate(0);
            }
        }
        catch
        {
            broken = true;
        }
    }

    public void StartFade()
    {
        if (broken || !canvasGroup || canvasGroup == null || fadeCurve == null)
            return;

        gameObject.SetActive(true);
        FadeActive = true;
        sentMidPoint = false;
        sentCompleted = false;
        fadeTimer = 0f;
        canvasGroup.alpha = fadeCurve.Evaluate(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (broken || !canvasGroup || canvasGroup == null || fadeCurve == null || !FadeActive)
        {
            return;
        }

        fadeTimer += Time.deltaTime;
        fadeTimer = Math.Min(fadeTimer, fadeTime);
        var t = fadeTimer / fadeTime;
        canvasGroup.alpha = fadeCurve.Evaluate(t);

        if (t >= 0.5 && !sentMidPoint)
        {
            sentMidPoint = true;
            FadeHalfWay?.Invoke();
        }

        if (t >= 1.0 && !sentCompleted)
        {
            sentCompleted = true;
            FadeActive = false;
            FadeCompleted?.Invoke();
        }
    }
}