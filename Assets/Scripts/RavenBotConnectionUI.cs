using UnityEngine;
using Debug = Shinobytes.Debug;
public class RavenBotConnectionUI : MonoBehaviour
{
    [SerializeField] private Sprite botConnectingSprite;
    [SerializeField] private Sprite botDisconnectedSprite;
    [SerializeField] private Sprite botConnectedSprite;
    [SerializeField] private UnityEngine.UI.Image statusImage;
    [SerializeField] private GameManager game;
    private BotState lastState;
    // Start is called before the first frame update
    void Start()
    {
        if (!game) game = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        var ravenBot = game.RavenBotController;
        if (ravenBot == null && lastState == BotState.NotSet)
        {
            statusImage.sprite = botDisconnectedSprite;
            lastState = BotState.Disconnected;
            return;
        }

        if (lastState != ravenBot.State)
        {
            Debug.Log("RavenBot State Changed from: " + lastState + " => " + ravenBot.State);
            switch (ravenBot.State)
            {
                case BotState.Connected:
                    statusImage.sprite = botConnectingSprite;
                    break;
                case BotState.Disconnected:
                    statusImage.sprite = botDisconnectedSprite;
                    break;
                case BotState.Ready:
                    statusImage.sprite = botConnectedSprite;
                    break;
            }
            lastState = ravenBot.State;
        }
    }
}
