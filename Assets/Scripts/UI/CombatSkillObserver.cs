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
        attack.Observe(player?.Stats?.Attack);
        defense.Observe(player?.Stats?.Defense);
        strength.Observe(player?.Stats?.Strength);
        health.Observe(player?.Stats?.Health);
        magic.Observe(player?.Stats?.Magic);
        ranged.Observe(player?.Stats?.Ranged);
    }
}