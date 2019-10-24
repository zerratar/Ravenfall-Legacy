using UnityEngine;

public class SkillStatsObserver : MonoBehaviour
{
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private StatObserver woodcutting;
    [SerializeField] private StatObserver fishing;
    [SerializeField] private StatObserver mining;
    [SerializeField] private StatObserver crafting;
    [SerializeField] private StatObserver cooking;
    [SerializeField] private StatObserver farming;
    [SerializeField] private StatObserver slayer;
    [SerializeField] private StatObserver sailing;

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
        woodcutting.Observe(player?.Stats?.Woodcutting);
        fishing.Observe(player?.Stats?.Fishing);
        mining.Observe(player?.Stats?.Mining);
        crafting.Observe(player?.Stats?.Crafting);
        cooking.Observe(player?.Stats?.Cooking);
        farming.Observe(player?.Stats?.Farming);
        slayer.Observe(player?.Stats?.Slayer);
        sailing.Observe(player?.Stats?.Sailing);
    }
}