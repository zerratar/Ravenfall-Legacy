using System;
using TMPro;
using UnityEngine;

public class GameProgressBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image imgBack;
    [SerializeField] private UnityEngine.UI.Image imgFront;

    [SerializeField] private UnityEngine.UI.Image imgFrontBack;
    [SerializeField] private TextMeshProUGUI lblProgress;
    [SerializeField] private Vector2 size;
    [SerializeField] public bool displayProgress = true;

    private float lastProgress = -1f;

    public float MaxValue = 1f;
    public float Progress;

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

        if (Progress > 1)
            Progress = 1;

        if (lastProgress != Progress)
        {
            if (displayProgress && lblProgress)
            {
                if (MaxValue > 1f)
                {
                    lblProgress.text = $"{Math.Round(Progress * MaxValue, 0)} HP ({Math.Round(Progress * 100f, 1)}%)";
                }
                else
                {
                    lblProgress.text = $"{Math.Round(Progress * 100f, 1)}%";
                }
            }

            var y = imgFront.rectTransform.sizeDelta.y;
            imgFront.rectTransform.sizeDelta = new Vector2(size.x * Progress, y);
            lastProgress = Progress;
        }
    }
}
