using Assets.Scripts;
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
    private Transform targetTransform;
    private Vector3 oldPos;
    private float oldProc;

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
        if (GameCache.Instance.IsAwaitingGameRestore) return;
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
        var stats = Target.GetStats();
        if (stats.IsDead || !Target.InCombat)
        {
            if (canvasGroup && canvasGroup.alpha > 0f)
                canvasGroup.alpha = 0f;
        }

        var proc = stats.Health.CurrentValue > 0
            ? 100f * ((float)stats.Health.CurrentValue / stats.Health.Level)
            : 0f;

        if (targetTransform)
        {
            var pos = targetTransform.position + (Vector3.up * 2f) + (Target.HealthBarOffset * Vector3.up);
            if (isDungeonBoss)
            {
                pos += Vector3.up * dungeonBoss.transform.localScale.x * 1.125f;
            }

            if ((oldPos - pos).sqrMagnitude > 0.01)
            {
                transform.position = pos;
                progress.sizeDelta = new Vector2(proc, 100f);
            }

            if (oldProc != proc)
                oldPos = pos;
            oldProc = proc;
        }
    }

    internal void SetTarget(IAttackable enemy)
    {
        Target = enemy;
        var behaviour = (MonoBehaviour)enemy;
        this.dungeonBoss = behaviour.GetComponent<DungeonBossController>();
        this.isDungeonBoss = !!dungeonBoss;

        this.targetTransform = behaviour.transform;

        if (canvasGroup && canvasGroup.alpha != 1f)
            canvasGroup.alpha = 1f;
    }
}
