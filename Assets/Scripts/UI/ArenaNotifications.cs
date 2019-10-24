using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArenaNotifications : MonoBehaviour
{
    [SerializeField] private GameObject activated;
    [SerializeField] private GameObject startingSoon;
    [SerializeField] private GameObject started;
    [SerializeField] private GameObject winner;
    [SerializeField] private GameObject draw;

    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private TextMeshProUGUI winnerNameText;

    [SerializeField]
    private string activated_format =
        "<color=#df4639>{0} <color=#ffffff>more player required to start.";

    [SerializeField]
    private string startingSoon_format =
        "<color=#df4639>{0} <color=#ffffff>seconds until it starts.";

    private string action;
    private float activeTimer;

    void Start()
    {
        DisableAll();
    }

    void Update()
    {
        if (activeTimer >= 0f)
        {
            activeTimer -= Time.deltaTime;
            if (activeTimer > -100f && activeTimer <= 0f)
            {
                activeTimer = float.MinValue;
                DisableAll();
            }
        }
    }

    public void ShowStartArena()
    {
        activeTimer = 3f;
        DisableAll();
        started.SetActive(true);
    }

    public void ShowActivateArena(int playersRequired)
    {
        activeTimer = 10f;
        DisableAll();
        notificationText.text = string.Format(activated_format, playersRequired);
        activated.SetActive(true);
    }

    public void ShowStartingSoon(int secondsLeft)
    {
        activeTimer = secondsLeft;
        DisableAll();
        notificationText.text = string.Format(startingSoon_format, secondsLeft);
        startingSoon.SetActive(true);
    }

    public void ShowWinner(PlayerController player)
    {
        activeTimer = 3f;
        DisableAll();
        winner.SetActive(true);
        winnerNameText.text = player.PlayerName;
    }

    public void ShowDraw()
    {
        activeTimer = 3f;
        DisableAll();
        draw.SetActive(true);
    }

    private void DisableAll()
    {
        notificationText.text = "";
        activated.SetActive(false);
        startingSoon.SetActive(false);
        started.SetActive(false);
        winner.SetActive(false);
        draw.SetActive(false);
    }
}
