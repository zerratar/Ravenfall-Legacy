using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = Shinobytes.Debug;
public class RavenBotConnectionUI : MonoBehaviour
{
    [SerializeField] private Sprite botConnectingSprite;
    [SerializeField] private Sprite botDisconnectedSprite;
    [SerializeField] private Sprite botConnectedSprite;
    [SerializeField] private UnityEngine.UI.Image statusImage;
    [SerializeField] private UnityEngine.UI.Image twitchDisconnected;
    [SerializeField] private bool showBotStatusWhenReady = false;
    [SerializeField] private GameManager game;
    private BotState lastState;
    private bool joinedChannel;
    // Start is called before the first frame update
    void Start()
    {
        joinedChannel = false;
        Enable(twitchDisconnected);
        if (!game) game = FindAnyObjectByType<GameManager>();
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

        if (joinedChannel != ravenBot.HasJoinedChannel)
        {
            twitchDisconnected.gameObject.SetActive(!ravenBot.HasJoinedChannel);
            joinedChannel = ravenBot.HasJoinedChannel;
        }

        if (lastState != ravenBot.State)
        {
            Debug.Log("RavenBot State Changed from: " + lastState + " => " + ravenBot.State);
            switch (ravenBot.State)
            {
                case BotState.Connected:
                    Enable(statusImage);
                    statusImage.sprite = botConnectingSprite;
                    break;
                case BotState.Disconnected:
                    Enable(statusImage);
                    statusImage.sprite = botDisconnectedSprite;
                    break;
                case BotState.Ready:

                    SetEnabled(statusImage, showBotStatusWhenReady);
                    statusImage.sprite = botConnectedSprite;
                    break;
            }
            lastState = ravenBot.State;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetEnabled(Behaviour obj, bool enabled)
    {
        if (enabled)
        {
            Enable(obj);
        }
        else
        {
            Disable(obj);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Enable(Behaviour obj)
    {
        var go = obj.gameObject;
        if (!go.activeInHierarchy)
        {
            go.gameObject.SetActive(true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Disable(Behaviour obj)
    {
        var go = obj.gameObject;
        if (go.activeInHierarchy)
        {
            go.gameObject.SetActive(false);
        }
    }
}
