using Assets.Scripts;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DungeonBossController : MonoBehaviour
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private SphereCollider activateRadiusCollider;
    [SerializeField] private SphereCollider attackRadiusCollider;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float deathTimer = 5f;

    private float attackTimer = 0f;
    private Animator modelAnimator;
    private DungeonManager dungeonManager;
    private GameObject modelObject;
    private PlayerController target;

    private bool playingDeathAnimation;
    private DungeonRoomController room;
    private bool destroyed;

    public EnemyController Enemy => enemyController;

    void Awake()
    {
        if (!room) room = GetComponentInParent<DungeonRoomController>();
        if (!dungeonManager) dungeonManager = FindObjectOfType<DungeonManager>();
        if (!enemyController) enemyController = GetComponent<EnemyController>();

        enemyController.GivesExperienceWhenKilled = false;
        //enemyController.HandleFightBack = t;


        if (!attackRadiusCollider || !activateRadiusCollider)
        {
            var colliders = GetComponents<SphereCollider>();
            if (colliders.Length == 2)
            {
                attackRadiusCollider = colliders[0];
                activateRadiusCollider = colliders[1];
            }
            else
            {
                Debug.LogError("Blerp");
            }
        }

        name = "___DUNGEON__BOSS___";
    }

    // Update is called once per frame
    void Update()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        if (destroyed)
        {
            return;
        }

        if (!dungeonManager || !dungeonManager.Started)
        {
            if (dungeonManager && !dungeonManager.Active)
                Die();

            return;
        }

        if (UpdateAction())
        {
            HandleAttack();
        }

        var proc = (float)Enemy.Stats.Health.CurrentValue / Enemy.Stats.Health.Level;

        dungeonManager.Notifications.SetHealthBarValue(proc, Enemy.Stats.Health.Level);
    }

    private bool UpdateAction()
    {
        if (enemyController.Stats.IsDead)
        {
            if (!playingDeathAnimation)
            {
                if (animator) animator.SetTrigger("DeathTrigger");
                playingDeathAnimation = true;
                return false;
            }

            if (deathTimer <= 0f)
            {
                return false;
            }

            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                Die();
            }

            return false;
        }
        return true;
    }

    private void HandleAttack()
    {
        target = GetAttackableTarget();

        if (!target)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (destroyed)
        {
            return;
        }

        attackTimer = attackInterval;
        var damage = GameMath.CalculateMeleeDamage(enemyController, target);

        if (animator)
        {
            animator.SetInteger("AttackType", UnityEngine.Random.Range(0, 4));
            animator.SetTrigger("AttackTrigger");
        }

        target.TakeDamage(enemyController, (int)damage);
    }

    private PlayerController GetAttackableTarget()
    {
        var players = dungeonManager.GetPlayers();
        if (players.Count == 0) return null;
        try
        {
            return players
                .Where(x => x != null && x && !x.Stats.IsDead && Vector3.Distance(x.transform.position, transform.position) <= attackRadiusCollider.radius)
                .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
                .ThenByDescending(x =>
                {
                    enemyController.Aggro.TryGetValue(x.Name, out var aggro);
                    return aggro;
                })
                .ThenBy(x => UnityEngine.Random.value)
                .FirstOrDefault();
        }
        catch (Exception exc)
        {
            UnityEngine.Debug.LogError(exc.ToString());
            return null;
        }
    }

    public void Create(
        Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        var model = models.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (!model)
        {
            Debug.LogError("No available dungeon boss models??!?!");
            return;
        }

        modelObject = Instantiate(model, transform);
        modelAnimator = modelObject.GetComponent<Animator>();
        if (modelAnimator)
        {
            modelAnimator.enabled = false;
            animator.avatar = modelAnimator.avatar;
        }

        SetStats(rngLowStats, rngHighStats, rngLowEq, rngHighEq);
    }

    public void SetStats(Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        enemyController.Stats = GenerateCombatStats(rngLowStats ?? new Skills(), rngHighStats ?? new Skills());
        enemyController.EquipmentStats = GenerateEquipmentStats(rngLowEq ?? new EquipmentStats(), rngHighEq ?? new EquipmentStats());
        transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
        modelObject.transform.localScale = Vector3.one;
    }

    public void Die()
    {
        dungeonManager.Notifications.SetHealthBarValue(0);
        if (destroyed) return;
        destroyed = true;
        Destroy(this.gameObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Skills GenerateCombatStats(Skills rngLowStats, Skills rngHighStats)
    {
        var health = Math.Max(100, (int)(UnityEngine.Random.Range(rngLowStats.Health.CurrentValue, rngHighStats.Health.CurrentValue) * 100));
        var strength = Math.Max(1, (int)(UnityEngine.Random.Range(rngLowStats.Strength.CurrentValue, rngHighStats.Strength.CurrentValue)));
        var defense = Math.Max(1, (int)(UnityEngine.Random.Range(rngLowStats.Defense.CurrentValue, rngHighStats.Defense.CurrentValue)));
        var attack = Math.Max(1, (int)(UnityEngine.Random.Range(rngLowStats.Attack.CurrentValue, rngHighStats.Attack.CurrentValue)));

        return new Skills
        {
            Attack = new SkillStat
            {
                CurrentValue = attack,
                Level = attack,
            },
            Defense = new SkillStat
            {
                CurrentValue = defense,
                Level = defense
            },
            Strength = new SkillStat
            {
                CurrentValue = strength,
                Level = strength
            },
            Health = new SkillStat
            {
                CurrentValue = health,
                Level = health
            },
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static EquipmentStats GenerateEquipmentStats(EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        return new EquipmentStats
        {
            ArmorPower = UnityEngine.Random.Range(rngLowEq.ArmorPower, rngHighEq.ArmorPower),
            WeaponPower = UnityEngine.Random.Range(rngLowEq.WeaponPower, rngHighEq.WeaponPower),
            WeaponAim = UnityEngine.Random.Range(rngLowEq.WeaponAim, rngHighEq.WeaponAim)
        };
    }
}
