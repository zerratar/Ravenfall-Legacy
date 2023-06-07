using Assets.Scripts;
using System;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform progress;

    [SerializeField] private CanvasGroup canvasGroup;

    public IAttackable Target;
    public float YOffset = 0.2f;
    public HealthBarManager Manager;
    private CapsuleCollider capsuleCollider;
    private DungeonBossController dungeonBoss;
    private PlayerController player;
    private bool isPlayer;
    private bool isDungeonBoss;
    private Transform targetTransform;
    private Transform targetCamera;
    private Vector3 targetOffset;
    private bool hasTarget;
    private bool isActive;

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
        if (!targetCamera && Camera.main)
        {
            this.targetCamera = Camera.main.transform;
        }

        if (GameCache.IsAwaitingGameRestore) return;
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
        if (Target == null)
        {
            return;
        }

        var stats = Target.GetStats();
        if (stats.IsDead || !Target.InCombat)
        {
            this.SetActive(false);

            if (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = 0f;

                return;
            }
        }
        else
        {
            this.SetActive(true);
            if (canvasGroup.alpha <= 0f)
            {
                canvasGroup.alpha = 1f;
            }
        }


        if (!targetCamera)
        {
            return;
        }

        if (hasTarget)
        {
            float proc = stats.Health.CurrentValue > 0 ? 100f * ((float)stats.Health.CurrentValue / stats.Health.Level) : 0f;
            this.transform.position = targetTransform.position + targetOffset;
            this.transform.rotation = targetCamera.rotation;
            progress.sizeDelta = new Vector2(proc, 100f);
        }
    }
    private void SetActive(bool value)
    {
        if (this.isActive != value)
        {
            this.isActive = value;
            this.gameObject.SetActive(value);
        }
    }


    internal void SetTarget(IAttackable target)
    {
        Target = target;

        SetActive(true);

        var behaviour = (MonoBehaviour)target;
        this.capsuleCollider = behaviour.GetComponent<CapsuleCollider>();
        this.dungeonBoss = behaviour.GetComponent<DungeonBossController>();
        this.player = behaviour.GetComponent<PlayerController>();
        this.isPlayer = !!player;
        this.isDungeonBoss = !!dungeonBoss;

        this.targetTransform = behaviour.transform;
        this.hasTarget = false;
        if (this.targetTransform)
        {
            Vector3 pos;
            this.hasTarget = true;
            if (isPlayer && capsuleCollider)
            {
                pos = (Vector3.up * (capsuleCollider.height * targetTransform.localScale.y))
                    + (Vector3.up * YOffset);
            }
            else
            {
                pos = (Vector3.up * 2f) + (Target.HealthBarOffset * Vector3.up);
                if (isDungeonBoss)
                {
                    pos += Vector3.up * dungeonBoss.transform.localScale.x * 1.125f;
                }
            }

            this.targetOffset = pos;
        }


        if (canvasGroup && canvasGroup.alpha != 1f)
            canvasGroup.alpha = 1f;
    }
}
