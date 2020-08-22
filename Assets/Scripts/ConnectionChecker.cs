using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConnectionChecker : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public GameObject Graphics;
    public TextMeshProUGUI Label;

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

        if (gameManager.RavenNest.Stream.IsReady && Graphics.activeSelf)
        {
            Graphics.SetActive(false);
            return;
        }

        if (Graphics.activeSelf)
        {
            return;
        }

        if (!gameManager.RavenNest.SessionStarted && gameManager.RavenNest.BadClientVersion)
        {
            Label.text = "CLIENT IS OUT OF DATE. RESTART RAVENFALL TO UPDATE";
            Graphics.SetActive(true);
        }

        if (!gameManager.RavenNest.SessionStarted || gameManager.RavenNest.Stream.IsReady)
        {
            return;
        }

        Label.text = "CONNECTION LOST";
        Graphics.SetActive(true);
    }
}
