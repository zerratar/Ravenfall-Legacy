using System;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private AnimationCurve fadeCurve;

    private CanvasGroup canvasGroup;
    private float fadeTimer;
    private bool sentMidPoint;
    private bool sentCompleted;

    public bool FadeActive = false;

    public Action FadeCompleted;
    public Action FadeHalfWay;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        fadeTimer = 0;
        canvasGroup.alpha = fadeCurve.Evaluate(0);
    }

    public void StartFade()
    {
        FadeActive = true;
        sentMidPoint = false;
        sentCompleted = false;
        fadeTimer = 0f;
        canvasGroup.alpha = fadeCurve.Evaluate(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!canvasGroup || !FadeActive)
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