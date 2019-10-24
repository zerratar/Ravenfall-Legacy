using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulse : MonoBehaviour
{
    private RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        var half = new Vector3(0.5f, 0.5f, 0.5f);
        var quart = new Vector3(0.25f, 0.25f, 0.25f);
        rectTransform.localScale = half * Mathf.Sin(Time.time / 2f) + Vector3.one;
    }
}
