using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesObserver : MonoBehaviour
{
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private ResourceObserver wood;
    [SerializeField] private ResourceObserver fish;
    [SerializeField] private ResourceObserver ore;
    [SerializeField] private ResourceObserver wheat;
    [SerializeField] private ResourceObserver coin;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!observedPlayer)
        {
            return;
        }
    }

    public void Observe(PlayerController player)
    {
        observedPlayer = player;
        wood.Observe(() => player?.Resources?.Wood);
        fish.Observe(() => player?.Resources?.Fish);
        ore.Observe(() => player?.Resources?.Ore);
        wheat.Observe(() => player?.Resources?.Wheat);
        coin.Observe(() => player?.Resources?.Coins);
    }
}
