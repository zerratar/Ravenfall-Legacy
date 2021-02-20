using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunestoneEvent : MonoBehaviour
{
    [SerializeField] private ParticleSystem effect;
    [SerializeField] private GameCamera camera;
    [SerializeField] private SphereCollider sphereCollider;
    [SerializeField] private TMPro.TextMeshPro eventText;
    [SerializeField] private float textFadeDuration = 0.5f;

    private bool inTrigger;

    // Start is called before the first frame update
    void Start()
    {
        if (!camera) camera = FindObjectOfType<GameCamera>();
        if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
        if (!eventText) eventText = GetComponentInChildren<TMPro.TextMeshPro>();

        HideEvent();
    }

    // Update is called once per frame
    void Update()
    {
        var cp = camera.transform.position;
        var rc = transform.position + sphereCollider.center;
        if (Vector3.Distance(cp, rc) < sphereCollider.radius)
        {
            if (!inTrigger)
            {
                inTrigger = true;
                ShowEvent();
            }
        }
        else if (inTrigger)
        {
            inTrigger = false;
            HideEvent();
        }
    }

    private void HideEvent()
    {
        if (effect.isPlaying)
            effect.Stop();

        if (!eventText)
            return;

        FadeText(0, textFadeDuration);
    }

    private void ShowEvent()
    {
        if (!effect.isPlaying)
            effect.Play();

        if (!eventText)
            return;

        FadeText(1, textFadeDuration);
    }

    private void FadeText(float targetOpacity, float duration)
    {
        if (!eventText) return;
        StartCoroutine(SetTextOpacity(targetOpacity, duration));
    }

    private IEnumerator SetTextOpacity(float targetOpacity, float duration)
    {
        float progress = 0f;
        float start = eventText.color.a;
        while (progress < duration)
        {
            yield return null;

            progress += Time.deltaTime;
            var opacity = Mathf.Lerp(start, targetOpacity, progress / duration);
            eventText.color = new Color(1f, 1f, 1f, opacity);
        }
    }
}
