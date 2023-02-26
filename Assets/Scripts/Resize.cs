using System;
using UnityEngine;

public class Resize : MonoBehaviour
{
    [SerializeField] private float targetWidth;
    [SerializeField] private float targetHeight;
    [SerializeField] private float time = 1F;

    private float timer;
    private RectTransform rect;

    private float startWidth;
    private float startHeight;

    // Start is called before the first frame update
    void Start()
    {
        timer = time;
        rect = GetComponent<RectTransform>();

        startWidth = rect.sizeDelta.x;
        startHeight = rect.sizeDelta.y;

        if (Math.Abs(targetWidth) < 0.001) targetWidth = rect.sizeDelta.x;
        if (Math.Abs(targetHeight) < 0.001) targetHeight = rect.sizeDelta.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer > 0f)
        {
            timer -= GameTime.deltaTime;
        }
        else
        {
            timer = 0f;
        }
        var percent = (time - timer) / time;

        var newWidth = Mathf.Lerp(startWidth, targetWidth, percent);
        var newHeight = Mathf.Lerp(startHeight, targetHeight, percent);

        rect.sizeDelta = new Vector2(newWidth, newHeight);
    }
}
