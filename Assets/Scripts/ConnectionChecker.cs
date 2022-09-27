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
        Graphics.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            UpdateUI();
        }
        catch { }
    }

    private void UpdateUI()
    {
        if (!gameManager || gameManager.RavenNest == null ||
            !gameManager.RavenNest.Authenticated || !Graphics)
        {
            return;
        }

        if ((gameManager.RavenNest.WebSocket.IsReady) && Graphics.activeSelf)
        {
            SetVisibility(false);
            return;
        }

        if (Graphics.activeSelf)
        {
            return;
        }

        //if (gameManager.RavenNest.Desynchronized)
        //{
        //    Label.text = "CLIENT CANNOT SYNCHRONIZE WITH SERVER. PLEASE RESTART OR USE !RELOAD";
        //    SetVisibility(true);
        //} else 
        if (!gameManager.RavenNest.SessionStarted && gameManager.RavenNest.BadClientVersion)
        {
            Label.text = "CLIENT IS OUT OF DATE. RESTART RAVENFALL TO UPDATE";
            SetVisibility(true);
        }

        if (!gameManager.RavenNest.SessionStarted || gameManager.RavenNest.WebSocket.IsReady)
        {
            return;
        }

        Label.text = "CONNECTION LOST";
        SetVisibility(true);
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
