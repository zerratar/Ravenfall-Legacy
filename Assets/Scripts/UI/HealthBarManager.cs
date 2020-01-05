using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    private readonly object mutex = new object();
    private readonly List<HealthBar> healthBars = new List<HealthBar>();

    [SerializeField] private GameObject healthBarPrefab;

    public void Remove(HealthBar hb)
    {
        lock (mutex)
        {
            healthBars.Remove(hb);
        }
    }

    public void Remove(PlayerController player)
    {
        lock (mutex)
        {
            var bar = healthBars.FirstOrDefault(x => x.Target == player);
            if (bar)
            {
                Destroy(bar.gameObject);
                healthBars.Remove(bar);
            }
        }
    }

    public HealthBar Add(IAttackable enemy)
    {
        if (!healthBarPrefab) return null;
        if (enemy == null) return null;

        var enemyName = ((MonoBehaviour)enemy).name;

        lock (mutex)
        {
            if (healthBars.Any(x => x.Target == enemy))
            {
                Debug.LogWarning($"{enemyName} already have an assigned health bar.");
                return null;
            }
        }

        var healthBar = Instantiate(healthBarPrefab, transform);
        if (!healthBar)
        {
            Debug.LogError($"Failed to add health bar for {enemyName}");
            return null;
        }

        var barComponent = healthBar.GetComponent<HealthBar>();
        barComponent.Manager = this;
        barComponent.SetTarget(enemy);

        lock (mutex)
        {
            healthBars.Add(barComponent);
        }

        return barComponent;
    }
}
