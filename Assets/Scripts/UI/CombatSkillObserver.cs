using UnityEngine;

public class CombatSkillObserver : MonoBehaviour
{
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private StatObserver attack;
    [SerializeField] private StatObserver defense;
    [SerializeField] private StatObserver strength;
    [SerializeField] private StatObserver health;
    [SerializeField] private StatObserver magic;
    [SerializeField] private StatObserver ranged;
    [SerializeField] private StatObserver healing;

    private float nextUpdate = 0f;
    private float updateInterval = 1f;

    public void Update()
    {
        if (!observedPlayer)
            return;

        if (nextUpdate > 0)
        {
            nextUpdate -= GameTime.deltaTime;
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
        attack.Observe(observedPlayer?.Stats?.Attack);
        defense.Observe(observedPlayer?.Stats?.Defense);
        strength.Observe(observedPlayer?.Stats?.Strength);
        health.Observe(observedPlayer?.Stats?.Health);
        magic.Observe(observedPlayer?.Stats?.Magic);
        ranged.Observe(observedPlayer?.Stats?.Ranged);
        healing?.Observe(observedPlayer?.Stats?.Healing);
    }
}