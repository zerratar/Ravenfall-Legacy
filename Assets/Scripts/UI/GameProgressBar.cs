using System;
using TMPro;
using UnityEngine;

public class GameProgressBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image imgBack;
    [SerializeField] private UnityEngine.UI.Image imgFront;
    [SerializeField] private TextMeshProUGUI lblProgress;
    [SerializeField] private Vector2 size;
    [SerializeField] public bool displayProgress = true;

    private float lastProgress = -1f;

    public float progress;
    
    // Start is called before the first frame update
    void Start()
    {
        if (Mathf.Abs(size.x) <= 0.00001f)
        {
            size = imgBack.rectTransform.sizeDelta;
        }
        if (Mathf.Abs(size.x) <= 0.00001f)
        {
            size = GetComponent<RectTransform>().sizeDelta;
        }

        if (Mathf.Abs(size.x) <= 0.00001f)
        {
            size = GetComponent<RectTransform>().rect.size;
        }

        if (displayProgress && !lblProgress) lblProgress = GetComponentInChildren<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!imgBack || !imgFront)
        {
            return;
        }

        if (lastProgress != progress)
        {
            if (displayProgress && lblProgress)
            {
                lblProgress.text = $"{Math.Round(progress * 100f, 1)}%";
            }

            var y = imgFront.rectTransform.sizeDelta.y;
            imgFront.rectTransform.sizeDelta = new Vector2(size.x * progress, y);
            lastProgress = progress;
        }
    }
}
