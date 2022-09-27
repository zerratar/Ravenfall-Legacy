using System.Collections;
using UnityEngine;

public class BotPlayerController : MonoBehaviour
{
    private GameManager gameManager;
    public PlayerController playerController;
    private bool joiningEvent;

    private void Start()
    {
        this.gameManager = FindObjectOfType<GameManager>();
        this.playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (joiningEvent)
        {
            return;
        }

        if (!this.playerController)
        {
            return;
        }

        if (!gameManager)
        {
            return;
        }

        if (gameManager.Raid.Started && gameManager.Raid.CanJoin(playerController) == RaidJoinResult.CanJoin)
        {
            joiningEvent = true;
            StartCoroutine(JoinRaid());
        }

        if (gameManager.Dungeons.Active)
        {
            var result = gameManager.Dungeons.CanJoin(playerController);
            if (result == DungeonJoinResult.CanJoin)
            {
                joiningEvent = true;
                StartCoroutine(JoinDungeon());
            }
        }
    }

    private IEnumerator JoinDungeon()
    {
        yield return new WaitForSeconds(0.5f + UnityEngine.Random.value);
        gameManager.Dungeons.Join(playerController);
        joiningEvent = false;
    }
    private IEnumerator JoinRaid()
    {
        yield return new WaitForSeconds(0.5f + UnityEngine.Random.value);
        gameManager.Raid.Join(playerController);
        joiningEvent = false;
    }
}
