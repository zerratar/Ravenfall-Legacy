using System;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform progress;

    [SerializeField] private CanvasGroup canvasGroup;

    public IAttackable Target;

    public HealthBarManager Manager;
    private DungeonBossController dungeonBoss;
    private bool isDungeonBoss;
    // Start is called before the first frame update
    void Start()
    {
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (!progress)
        {
            progress = gameObject.transform.GetChild(0).GetComponent<RectTransform>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Target == null || !((MonoBehaviour)Target))
        {
            if (Manager)
            {
                Manager.Remove(this);
            }
            Destroy(gameObject);
            return;
        }

        //transform.position = Target.Transform.position + (Vector3.up * 2f);

        UpdateHealth();
    }

    // This is being called whenever health is actually changed.
    public void UpdateHealth()
    {
        if (canvasGroup)
            canvasGroup.alpha = 1f;

        var stats = Target.GetStats();
        if (stats.IsDead || !Target.InCombat)
        {
            if (canvasGroup)
                canvasGroup.alpha = 0f;
        }

        var proc = stats.Health.CurrentValue > 0
            ? 100f * ((float)stats.Health.CurrentValue / stats.Health.Level)
            : 0f;

        var tar = (MonoBehaviour)Target;
        if (tar)
        {
            var targetTransform = tar.transform;
            progress.sizeDelta = new Vector2(proc, 100f);
            transform.position = targetTransform.position + (Vector3.up * 2f) + (Target.HealthBarOffset * Vector3.up);
            if (isDungeonBoss)
            {
                transform.position += Vector3.up * dungeonBoss.transform.localScale.x * 1.125f;
            }
        }
    }

    internal void SetTarget(IAttackable enemy)
    {
        Target = enemy;
        var behaviour = (MonoBehaviour)enemy;
        this.dungeonBoss = behaviour.GetComponent<DungeonBossController>();
        this.isDungeonBoss = !!dungeonBoss;
    }
}
