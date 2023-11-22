using System.Collections;
using UnityEngine;

public class BotPlayerController : MonoBehaviour
{
    private GameManager gameManager;
    public PlayerController playerController;
    private bool initialized;
    private bool joiningEvent;

    private void Start()
    {
        if (!this.gameManager) this.gameManager = FindAnyObjectByType<GameManager>();
        if (!this.playerController) this.playerController = GetComponent<PlayerController>();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (joiningEvent)
        {
            return;
        }

        if (playerController.raidHandler.InRaid || playerController.dungeonHandler.InDungeon)
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
        if (!playerController) playerController = GetComponent<PlayerController>();
        gameManager.Dungeons.Join(playerController);
        joiningEvent = false;
    }
    private IEnumerator JoinRaid()
    {
        yield return new WaitForSeconds(0.5f + UnityEngine.Random.value);
        if (!playerController) playerController = GetComponent<PlayerController>();
        gameManager.Raid.Join(playerController);
        joiningEvent = false;
    }
}
