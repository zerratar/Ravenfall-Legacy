using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using Random = UnityEngine.Random;

public class RaidBossController : MonoBehaviour
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private SphereCollider activateRadiusCollider;
    [SerializeField] private DroneController droneController;

    [SerializeField] private SphereCollider attackRadiusCollider;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float deathTimer = 5f;

    [SerializeField] private Rigidbody rb;

    public bool RaidBossControlsDestroy = false;

    private float attackTimer = 0f;
    private Animator modelAnimator;
    private RaidManager raidManager;
    private GameObject modelObject;
    private PlayerController target;

    private bool activated;
    private bool playingDeathAnimation;
    private IslandController island;

    public EnemyController Enemy => enemyController;

    public IslandController Island => island;

    void Awake()
    {
        if (!enemyController) enemyController = GetComponent<EnemyController>();
        if (!droneController) droneController = GetComponentInChildren<DroneController>();
        if (!rb) rb = GetComponent<Rigidbody>();

        enemyController.GivesExperienceWhenKilled = false;
        enemyController.HandleFightBack = false;

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
        name = "___RAID__BOSS___";
        EnsureRaidManager();
    }

    // Update is called once per frame
    void Update()
    {
        if (droneController)
        {
            rb.isKinematic = true;
        }

        if (activated)
        {
            UpdateAction();
            return;
        }

        if (!raidManager || !raidManager.Started)
        {
            return;
        }

        if (raidManager.Raiders.Count <= 0)
        {
            return;
        }

        if (raidManager.Raiders.Any(WithinActivateRange))
        {
            Activate();
        }
    }

    private void UpdateAction()
    {
        if (enemyController.Stats.IsDead)
        {
            raidManager.Notifications.HideRaidInfo();

            if (!playingDeathAnimation)
            {
                if (animator) animator.SetTrigger("DeathTrigger");
                playingDeathAnimation = true;
                return;
            }

            if (deathTimer <= 0f)
            {
                return;
            }

            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                Die();
            }

            return;
        }

        target = GetAttackableTarget();

        if (!target)
        {
            return;
        }

        if (attackTimer <= 0f)
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
        if (!droneController)
        {
            transform.LookAt(target.transform);
        }
        attackTimer = attackInterval;
        var damage = GameMath.CalculateDamage(enemyController, target);

        if (animator)
        {
            animator.SetInteger("AttackType", Random.Range(0, 3));
            animator.SetTrigger("AttackTrigger");
        }

        if (droneController)
        {
            droneController.FireGuns(target.Transform, attackInterval * 0.9f);
            var random = Random.value;
            if (random >= 0.75)
                droneController.FireMissiles(target.Transform);
            else if (random >= 0.5)
                droneController.FireMortars(target.Transform);
        }

        target.TakeDamage(enemyController, (int)damage);
    }

    private PlayerController GetAttackableTarget()
    {
        if (raidManager.Raiders == null) return null;
        var raiders = raidManager.Raiders.ToList();
        if (raiders.Count == 0) return null;
        try
        {
            return raiders
                .Where(x => x != null && x && !x.Stats.IsDead && Vector3.Distance(x.transform.position, transform.position) <= attackRadiusCollider.radius)
                .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
                .ThenByDescending(x =>
                {
                    enemyController.Aggro.TryGetValue(x.Name, out var aggro);
                    return aggro;
                })
                .ThenBy(x => Random.value)
                .FirstOrDefault();
        }
        catch (Exception exc)
        {
            UnityEngine.Debug.LogError(exc.ToString());
            return null;
        }
    }

    public void IslandEnter(IslandController island)
    {
        this.island = island;
    }

    public void IslandExit()
    {
        island = null;
    }

    public void Activate()
    {
        activated = true;
        attackTimer = attackInterval;
    }

    public void Create(
        Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        EnsureRaidManager();
        var model = models.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (!model)
        {
            Debug.LogError("No available raid boss models??!?!");
            return;
        }

        enemyController.Stats = GenerateCombatStats(rngLowStats ?? new Skills(), rngHighStats ?? new Skills());
        enemyController.EquipmentStats = GenerateEquipmentStats(rngLowEq ?? new EquipmentStats(), rngHighEq ?? new EquipmentStats());


        modelObject = Instantiate(model, transform);
        modelAnimator = modelObject.GetComponent<Animator>();
        if (modelAnimator)
        {
            modelAnimator.enabled = false;
            animator.avatar = modelAnimator.avatar;
        }

        if (!droneController) droneController = GetComponentInChildren<DroneController>();

        if (droneController)
        {
            //enemyController.RotationLocked = true;            
            RaidBossControlsDestroy = true;

            //transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
            modelObject.transform.GetChild(0).localScale *= Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
        }
        else
        {
            transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
            modelObject.transform.localScale = Vector3.one; // take control of the scale of these :D MWOUAHAHHA
        }
    }


    public void Die()
    {
        if (droneController) droneController.Death();
        raidManager.EndRaid(true, false);
    }

    private void EnsureRaidManager()
    {
        if (!raidManager) raidManager = FindObjectOfType<RaidManager>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WithinActivateRange(PlayerController player)
    {
        if (!player || player.Kicked || player.gameObject == null && !ReferenceEquals(player.gameObject, null)) return false;
        return Vector3.Distance(transform.position, player.transform.position) <= activateRadiusCollider.radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WithinAttackRange(PlayerController player)
    {
        if (!player || player.Kicked || player.gameObject == null && !ReferenceEquals(player.gameObject, null)) return false;
        return Vector3.Distance(transform.position, player.transform.position) <= activateRadiusCollider.radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Skills GenerateCombatStats(Skills rngLowStats, Skills rngHighStats)
    {
        var health = Random.Range(rngLowStats.Health.CurrentValue, rngHighStats.Health.CurrentValue) * 100;
        var strength = Random.Range(rngLowStats.Strength.CurrentValue, rngHighStats.Strength.CurrentValue);
        var defense = Random.Range(rngLowStats.Defense.CurrentValue, rngHighStats.Defense.CurrentValue);
        var attack = Random.Range(rngLowStats.Attack.CurrentValue, rngHighStats.Attack.CurrentValue);

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
            ArmorPower = Random.Range(rngLowEq.ArmorPower, rngHighEq.ArmorPower),
            WeaponPower = Random.Range(rngLowEq.WeaponPower, rngHighEq.WeaponPower),
            WeaponAim = Random.Range(rngLowEq.WeaponAim, rngHighEq.WeaponAim)
        };
    }
}
