using TMPro;
using UnityEngine;

public class BATimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI subscriber;
    [SerializeField] private GameProgressBar timeProgressBar;

    private string lastSubber;

    private void Awake()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!subscriber) subscriber = GetComponentInChildren<TextMeshProUGUI>();
        if (!timeProgressBar) timeProgressBar = GetComponentInChildren<GameProgressBar>();
    }

    public void SetSubscriber(string subber)
    {
        if (!subscriber || lastSubber == subber) return;
        lastSubber = subber;
        subscriber.text = "Last subscriber: " + SanitizeText(subber);
    }

    public void SetText(string text)
    {
        if (!label) return;
        label.text = text;
    }

    public void SetTime(float elapsed, float total)
    {
        if (!timeProgressBar) return;
        timeProgressBar.displayProgress = false;
        timeProgressBar.Progress = 1f - (elapsed / total);
    }

    public void SetActive(bool currentBoostActive)
    {
        gameObject.SetActive(currentBoostActive);
    }

    private static string SanitizeText(string text)
    {
        // TODO: Fix me, so we can ensure Unity doesnt crash :S
        return text;
    }
}