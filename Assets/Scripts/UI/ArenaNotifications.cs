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

    /// <summary>
    /// Optional UI Toolkit replacement. When assigned, announcements are drawn as text from
    /// Localization instead of the pre-rendered banner images above, which is what makes them
    /// translatable. The old objects stay in the scene and still work if this is cleared.
    /// See docs/ui-toolkit-migration.md.
    /// </summary>
    [SerializeField] private Shinobytes.UI.NotificationScreenView uiToolkitScreen;

    /// <summary>The chat command viewers type to join. Highlighted in the announcement.</summary>
    private const string JoinCommand = "!arena";

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
            activeTimer -= GameTime.deltaTime;
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

        if (uiToolkitScreen != null)
        {
            uiToolkitScreen.Show(Localization.UI_ARENA_STARTED);
            return;
        }

        started.SetActive(true);
    }

    public void ShowActivateArena(int playersRequired)
    {
        activeTimer = 10f;
        DisableAll();

        if (uiToolkitScreen != null)
        {
            uiToolkitScreen.Show(
                Localization.UI_ARENA_NOW_ACTIVE,
                Shinobytes.UI.NotificationScreenView.WithCommand(Localization.UI_ARENA_JOIN, JoinCommand),
                string.Format(activated_format, playersRequired));
            return;
        }

        notificationText.text = string.Format(activated_format, playersRequired);
        activated.SetActive(true);
    }

    public void ShowStartingSoon(int secondsLeft)
    {
        activeTimer = secondsLeft;
        DisableAll();
        //notificationText.text = string.Format(startingSoon_format, secondsLeft);
        var detail = secondsLeft > 0
            ? "You have " + secondsLeft + " seconds left to join!"
            : "Arena will start as soon as all players arrive.";

        if (uiToolkitScreen != null)
        {
            uiToolkitScreen.Show(
                Localization.UI_ARENA_ABOUT_TO_START,
                Shinobytes.UI.NotificationScreenView.WithCommand(Localization.UI_ARENA_JOIN, JoinCommand),
                detail);
            return;
        }

        notificationText.text = detail;
        startingSoon.SetActive(true);
    }

    public void ShowWinner(PlayerController player)
    {
        activeTimer = 3f;
        DisableAll();

        if (uiToolkitScreen != null)
        {
            // The winner's name leads rather than the congratulation. On stream the name is the
            // news, and it is what the winner wants to see and screenshot.
            uiToolkitScreen.Show(player.PlayerName, Localization.UI_ARENA_WINNER, null, rewardStyle: true);
            return;
        }

        winner.SetActive(true);
        winnerNameText.text = player.PlayerName;
    }

    public void ShowDraw()
    {
        activeTimer = 3f;
        DisableAll();

        if (uiToolkitScreen != null)
        {
            uiToolkitScreen.Show(
                Localization.UI_ARENA_DRAW_TITLE,
                Localization.UI_ARENA_DRAW_MESSAGE,
                null,
                rewardStyle: true);
            return;
        }

        draw.SetActive(true);
    }

    private void DisableAll()
    {
        if (uiToolkitScreen != null)
        {
            uiToolkitScreen.Hide();
        }

        notificationText.text = "";
        activated.SetActive(false);
        startingSoon.SetActive(false);
        started.SetActive(false);
        winner.SetActive(false);
        draw.SetActive(false);
    }
}
