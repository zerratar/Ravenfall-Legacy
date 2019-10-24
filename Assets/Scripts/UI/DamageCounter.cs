using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageCounter : MonoBehaviour
{
    private float offsetY = 2.75f;

    [SerializeField] private TextMeshProUGUI labelBack;
    [SerializeField] private TextMeshProUGUI labelFront;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float FadeoutOffsetY = 0f;

    private float FadeoutTimer = 2f;

    public float TargetFadeoutOffsetY = 3f;
    public float FadeoutDuration = 2f;
    public Transform Target;
    public int Damage = 0;

    void Start()
    {
        FadeoutTimer = FadeoutDuration;
    }

    // Update is called once per frame
    void Update()
    {
        if (FadeoutTimer <= 0f)
        {
            return;
        }

        transform.LookAt(Camera.main.transform);

        FadeoutTimer -= Time.deltaTime;

        if (FadeoutTimer <= 0f)
        {
            canvasGroup.alpha = 0;
            FadeoutTimer = FadeoutDuration;
            DestroyImmediate(gameObject);
            return;
        }

        labelBack.text = Damage.ToString();
        labelFront.text = Damage.ToString();

        var fadeoutProgress = FadeoutTimer / FadeoutDuration;
        var proc = 1f - fadeoutProgress;

        if (FadeoutTimer <= FadeoutDuration / 2f)
        {
            canvasGroup.alpha = FadeoutTimer / (FadeoutDuration / 2f);
        }

        FadeoutOffsetY = Mathf.Lerp(0, TargetFadeoutOffsetY, proc);

        if (Target)
        {
            transform.position = Target.transform.position + (Vector3.up * (offsetY + FadeoutOffsetY));
        }
    }
}
