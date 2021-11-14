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
            try
            {
                if (player == null || player.gameObject == null)
                {
                    return;
                }

                var bar = healthBars.FirstOrDefault(x =>
                    x != null
                    && x.Target != null
                    && ((x.Target == player || x.Target.Name == player.Name)
                    || x.Target.Transform != null
                    && x.Target.Transform
                    && x.Target.Transform.GetInstanceID() == player.GetInstanceID()));

                if (bar)
                {
                    Destroy(bar.gameObject);
                    healthBars.Remove(bar);
                }
            }
            catch (System.Exception exc)
            {
#if DEBUG
                Shinobytes.Debug.Log("Warning: Unable to remove healthbar properly. This can be ignored. ");
#endif
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
            var hb = healthBars.FirstOrDefault(x => x.Target == enemy);
            if (hb)
            {
                Shinobytes.Debug.LogWarning($"{enemyName} already have an assigned health bar.");
                hb.gameObject.SetActive(true);
                return hb;
            }
        }

        var healthBar = Instantiate(healthBarPrefab, transform);
        if (!healthBar)
        {
            Shinobytes.Debug.LogError($"Failed to add health bar for {enemyName}");
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
