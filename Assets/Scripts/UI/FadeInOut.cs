using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private FadeState fadeState = FadeState.Out;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private bool forever = true;

    private CanvasGroup canvasGroup;
    private float fadeTimer;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        fadeTimer = fadeTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (!canvasGroup)
        {
            return;
        }

        if (fadeState == FadeState.Out)
        {
            if (canvasGroup.alpha > 0f)
            {
                fadeTimer -= Time.deltaTime;
                canvasGroup.alpha = fadeTimer / fadeTime;
            }
            else if (forever)
            {
                fadeTimer = fadeTime;
                fadeState = FadeState.In;
            }
        }
        else
        {
            if (canvasGroup.alpha < 1f)
            {
                fadeTimer -= Time.deltaTime;
                canvasGroup.alpha = (fadeTime - fadeTimer) / fadeTime;
            }
            else if (forever)
            {
                fadeTimer = fadeTime;
                fadeState = FadeState.Out;
            }
        }
    }
}

public enum FadeState
{
    In,
    Out
}
