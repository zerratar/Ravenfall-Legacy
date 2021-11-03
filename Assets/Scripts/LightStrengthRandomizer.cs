using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightStrengthRandomizer : MonoBehaviour
{
    [SerializeField] private new Light light;

    private float lightStrengthMin = 0.3f;
    private float lightStrengthMax = 1.0f;

    private float updateIntervalMin = 0.175f;
    private float updateIntervalMax = 0.33f;
    private float nextUpdate = 0.25f;

    private float smoothTime = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        if (!light)
            this.light = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        nextUpdate -= Time.deltaTime;
        if (nextUpdate <= 0)
        {
            nextUpdate = 10000;
            StartCoroutine(SetLightIntensity());
        }
    }

    private IEnumerator SetLightIntensity()
    {
        var targetIntensity = Random.Range(lightStrengthMin, lightStrengthMax);
        var intensityStart = light.intensity;
        var time = 0f;
        while (time < smoothTime)
        {
            yield return null;
            time += Time.deltaTime;
            var progress = time / smoothTime;
            if (progress > 1) progress = 1f;
            light.intensity = Mathf.Lerp(intensityStart, targetIntensity, progress);
            if (progress >= 1)
                break;
        }

        nextUpdate = Random.Range(updateIntervalMin, updateIntervalMax);
    }
}
