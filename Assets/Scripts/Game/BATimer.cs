using TMPro;
using UnityEngine;

public class BATimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI subscriber;
    [SerializeField] private GameProgressBar timeProgressBar;

    private string lastSubber;
    private bool isActive;

    private void Awake()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!subscriber) subscriber = GetComponentInChildren<TextMeshProUGUI>();
        if (!timeProgressBar) timeProgressBar = GetComponentInChildren<GameProgressBar>();
        this.isActive = gameObject.activeInHierarchy;
    }

    public void SetSubscriber(string subber, bool byUser)
    {
        if (!subscriber || lastSubber == subber) return;
        lastSubber = subber;
        subscriber.text =
            (byUser ? "Thanks to " : "")
             + SanitizeText(subber);
    }

    public void SetText(string text)
    {
        if (!label) return;
        label.text = text;
    }

    public void SetTime(float timeLeftSeconds, float totalDurationSeconds)
    {
        if (!timeProgressBar) return;
        timeProgressBar.displayProgress = false;
        timeProgressBar.Progress = timeLeftSeconds / totalDurationSeconds;
    }

    public void SetActive(bool currentBoostActive)
    {
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