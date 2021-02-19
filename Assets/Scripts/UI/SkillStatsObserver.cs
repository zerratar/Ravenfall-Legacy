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

    private float nextUpdate = 0f;
    private float updateInterval = 1f;

    private void Update()
    {
        if (!observedPlayer) return;

        if (nextUpdate > 0)
        {
            nextUpdate -= Time.deltaTime;
            if (nextUpdate <= 0)
            {
                UpdateStats();
                nextUpdate = updateInterval;
            }
        }
    }

    public void Observe(PlayerController player)
    {
        observedPlayer = player;
        UpdateStats();
        nextUpdate = 0.1f;
    }

    private void UpdateStats()
    {
        woodcutting.Observe(observedPlayer?.Stats?.Woodcutting);
        fishing.Observe(observedPlayer?.Stats?.Fishing);
        mining.Observe(observedPlayer?.Stats?.Mining);
        crafting.Observe(observedPlayer?.Stats?.Crafting);
        cooking.Observe(observedPlayer?.Stats?.Cooking);
        farming.Observe(observedPlayer?.Stats?.Farming);
        slayer.Observe(observedPlayer?.Stats?.Slayer);
        sailing.Observe(observedPlayer?.Stats?.Sailing);
    }
}