using TMPro;
using UnityEngine;

public class BATimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI subscriber;
    [SerializeField] private GameProgressBar timeProgressBar;

    [Tooltip("Optional UI Toolkit pill. When assigned, every call is forwarded to it and this uGUI banner stays hidden.")]
    [SerializeField] private Shinobytes.UI.XpBoostView toolkitView;

    private string lastSubber;
    private bool isActive;
    private float multiplier = 1f;

    private void Awake()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!subscriber) subscriber = GetComponentInChildren<TextMeshProUGUI>();
        if (!timeProgressBar) timeProgressBar = GetComponentInChildren<GameProgressBar>();
        this.isActive = gameObject.activeInHierarchy;
    }

    public void SetSubscriber(string subber, bool byUser)
    {
        if (toolkitView)
        {
            toolkitView.SetSubscriber(SanitizeText(subber), byUser);
            return;
        }

        if (!subscriber || lastSubber == subber) return;
        lastSubber = subber;
        subscriber.text =
            (byUser ? "Thanks to " : "")
             + SanitizeText(subber);
    }

    public void SetMultiplier(float value)
    {
        multiplier = value;
    }

    public void SetText(string text)
    {
        if (toolkitView) return;
        if (!label) return;
        label.text = text;
    }

    public void SetTime(float timeLeftSeconds, float totalDurationSeconds)
    {
        if (toolkitView)
        {
            toolkitView.SetBoost(multiplier, timeLeftSeconds, totalDurationSeconds);
            return;
        }

        if (!timeProgressBar) return;
        timeProgressBar.displayProgress = false;
        timeProgressBar.Progress = timeLeftSeconds / totalDurationSeconds;
    }

    public void SetActive(bool currentBoostActive)
    {
        if (toolkitView)
        {
            toolkitView.SetActive(currentBoostActive);

            // The legacy banner never shows while the pill is in charge. Its methods still run while
            // the GameObject is inactive, which is all the forwarding needs.
            if (gameObject.activeSelf) gameObject.SetActive(false);
            isActive = false;
            return;
        }

        if (isActive == currentBoostActive)
        {
            return;
        }

        isActive = currentBoostActive;
        gameObject.SetActive(currentBoostActive);
    }

    private static string SanitizeText(string text)
    {
        // TODO: Fix me, so we can ensure Unity doesnt crash :S
        return text;
    }
}