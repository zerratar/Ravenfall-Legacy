using Assets.Scripts;
using System;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

using Random = UnityEngine.Random;

public class RaidBossController : MonoBehaviour
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private SphereCollider activateRadiusCollider;

    [SerializeField] private SphereCollider attackRadiusCollider;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float deathTimer = 5f;
    [SerializeField] private int healthMultiplier = 100;

    [SerializeField] private Rigidbody rb;
    public bool RaidBossControlsDestroy = false;
    public ItemDropHandler ItemDrops;

    private float attackTimer = 0f;
    private Animator modelAnimator;
    private RaidManager raidManager;
    private GameObject modelObject;
    private PlayerController target;

    private bool activated;
    private bool playingDeathAnimation;
    private IslandController island;
    private GameManager gameManager;
    public Transform _transform;

    public EnemyController Enemy => enemyController;

    public IslandController Island => island;

    void Awake()
    {
        this._transform = transform;
        if (!enemyController) enemyController = GetComponent<EnemyController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!ItemDrops) ItemDrops = GetComponent<ItemDropHandler>();

        enemyController.GivesExperienceWhenKilled = false;
        //enemyController.HandleFightBack = false;

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
                Shinobytes.Debug.LogError("Blerp");
            }
        }
        name = "Raid Boss";
        EnsureRaidManager();

        this.gameManager = FindAnyObjectByType<GameManager>();

        AssignIsland();

        //this.Enemy.Unlock();
    }

    public void UnlockMovement()
    {
        enemyController.AdjustPlacement();
        enemyController.Unlock(true);
    }

    private void AssignIsland()
    {
        if (!gameManager)
        {
            return;
        }

        if (!island)
        {
            var newIsland = gameManager.Islands.FindIsland(this._transform.position);
            if (newIsland)
                IslandEnter(newIsland);
        }

        if (!enemyController.Island)
        {
            enemyController.Island = island;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameCache.IsAwaitingGameRestore) return;

        AssignIsland();

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

            deathTimer -= GameTime.deltaTime;
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

        if (Vector3.Distance(target.Position, enemyController.Position) <= attackRadiusCollider.radius)
        {
            if (attackTimer <= 0f)
            {
                return;
            }

            Enemy.Lock();

            attackTimer -= GameTime.deltaTime;
            if (attackTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            Enemy.SetDestination(target.Position);
        }
    }

    private void Attack()
    {
        attackTimer = attackInterval;
        var damage = GameMath.CalculateMeleeDamage(enemyController, target);

        if (animator)
        {
            animator.SetInteger("AttackType", Random.Range(0, 4));
            animator.SetTrigger("AttackTrigger");
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
                .Where(x => x != null && x && !x.Stats.IsDead)
                .OrderByDescending(x =>
                {
                    enemyController.Aggro.TryGetValue(x.Name, out var aggro);
                    return aggro + Random.value;
                })
                .FirstOrDefault();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("RaidBossController.GetAttackableTarget: " + exc);
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
        if (activated) return;
        activated = true;
        attackTimer = attackInterval;
        Enemy.Unlock();
    }

    public void Create(Skills stats, EquipmentStats equipmentStats)
    {
        EnsureRaidManager();
        var model = models.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (!model)
        {
            Shinobytes.Debug.LogError("No available raid boss models??!?!");
            return;
        }

        enemyController.Stats = stats;
        enemyController.EquipmentStats = equipmentStats;

        modelObject = Instantiate(model, transform);
        modelAnimator = modelObject.GetComponent<Animator>();
        if (modelAnimator)
        {
            modelAnimator.enabled = false;
            animator.avatar = modelAnimator.avatar;
        }

        transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
        modelObject.transform.localScale = Vector3.one; // take control of the scale of these :D MWOUAHAHHA
        Enemy.Unlock();
    }

    public void Create(Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        var characterStats = GenerateCombatStats(rngLowStats ?? new Skills(), rngHighStats ?? new Skills());
        var equipmentStats = GenerateEquipmentStats(rngLowEq ?? new EquipmentStats(), rngHighEq ?? new EquipmentStats());

        Create(characterStats, equipmentStats);
    }

    public void Die()
    {
        raidManager.EndRaid(true, false);
    }

    private void EnsureRaidManager()
    {
        if (!raidManager) raidManager = FindAnyObjectByType<RaidManager>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WithinActivateRange(PlayerController player)
    {
        if (!player || player.isDestroyed || player.Removed || player.gameObject == null && !ReferenceEquals(player.gameObject, null)) return false;
        return Vector3.Distance(enemyController.Position, player.Position) <= activateRadiusCollider.radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WithinAttackRange(PlayerController player)
    {
        if (!player || player.isDestroyed || player.Removed || player.gameObject == null && !ReferenceEquals(player.gameObject, null)) return false;
        return Vector3.Distance(enemyController.Position, player.Position) <= activateRadiusCollider.radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Skills GenerateCombatStats(Skills rngLowStats, Skills rngHighStats)
    {
        var health = Math.Max(healthMultiplier, Random.Range(rngLowStats.Health.CurrentValue, rngHighStats.Health.CurrentValue) * healthMultiplier);
        var strength = Math.Max(1, Random.Range(rngLowStats.Strength.MaxLevel, rngHighStats.Strength.MaxLevel));
        var defense = Math.Max(1, Random.Range(rngLowStats.Defense.MaxLevel, rngHighStats.Defense.MaxLevel));
        var attack = Math.Max(1, Random.Range(rngLowStats.Attack.MaxLevel, rngHighStats.Attack.MaxLevel));
        var magic = Math.Max(1, Random.Range(rngLowStats.Magic.MaxLevel, rngHighStats.Magic.MaxLevel));
        var ranged = Math.Max(1, Random.Range(rngLowStats.Ranged.MaxLevel, rngHighStats.Ranged.MaxLevel));

        return new Skills
        {
            Attack = new(attack),
            Defense = new(defense),
            Strength = new(strength),
            Health = new(health),
            Magic = new(magic),
            Ranged = new(ranged)
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EquipmentStats GenerateEquipmentStats(EquipmentStats rngLowEq, EquipmentStats rngHighEq)
    {
        return new EquipmentStats
        {
            BaseArmorPower = Random.Range(rngLowEq.BaseArmorPower, rngHighEq.BaseArmorPower),
            BaseWeaponPower = Random.Range(rngLowEq.BaseWeaponPower, rngHighEq.BaseWeaponPower),
            BaseWeaponAim = Random.Range(rngLowEq.BaseWeaponAim, rngHighEq.BaseWeaponAim)
        };
    }
}
