using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RavenBotConnectionUI : MonoBehaviour
{
    [SerializeField] private Sprite botConnectingSprite;
    [SerializeField] private Sprite botDisconnectedSprite;
    [SerializeField] private Sprite botConnectedSprite;
    [SerializeField] private UnityEngine.UI.Image statusImage;
    [SerializeField] private RavenBot ravenBot;
    private BotState lastState;
    // Start is called before the first frame update
    void Start()
    {
        if (!ravenBot) ravenBot = FindObjectOfType<RavenBot>();
    }

    // Update is called once per frame
    void Update()
    {        
        if (lastState != ravenBot.State)
        {
            UnityEngine.Debug.Log("RavenBot State Changed from: " + lastState + " => " + ravenBot.State);
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
