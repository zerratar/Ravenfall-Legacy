using TMPro;
using UnityEngine;

public class ConnectionChecker : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public GameObject Graphics;
    public TextMeshProUGUI Label;
    private float lastUpdate;

    // Start is called before the first frame update
    void Start()
    {
        if (!Graphics) return;
        // straight away: SetVisibility waits a quarter second between changes, counted from zero,
        // so the first hide was skipped and a banner saved as shown stayed up until login
        Graphics.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (!gameManager || gameManager.RavenNest == null || !gameManager.RavenNest.Authenticated || !Graphics)
            {
                return;
            }

            if (gameManager.RavenNest.Connected && Graphics.activeSelf)
            {
                SetVisibility(false);
                return;
            }

            if (Graphics.activeSelf)
            {
                return;
            }

            if (!gameManager.RavenNest.SessionStarted && gameManager.RavenNest.BadClientVersion)
            {
                Label.text = "CLIENT IS OUT OF DATE. RESTART RAVENFALL TO UPDATE";
                SetVisibility(true);
            }

            if (!gameManager.RavenNest.SessionStarted || gameManager.RavenNest.Connected)
            {
                return;
            }

            Label.text = "CONNECTION LOST - DO NOT RESTART YOUR GAME TO AVOID DATA LOSS";
            SetVisibility(true);
        }
        catch { }
    }

    private void SetVisibility(bool value)
    {
        if (Time.time - lastUpdate > 0.25)
        {
            Graphics.SetActive(value);
            lastUpdate = Time.time;
        }
    }
}
