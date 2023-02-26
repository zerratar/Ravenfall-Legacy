using Assets.Scripts;
using System;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DungeonBossController : MonoBehaviour
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private SphereCollider activateRadiusCollider;
    [SerializeField] private SphereCollider attackRadiusCollider;
    [SerializeField] private float deathTimer = 5f;
    [SerializeField] private float hitRange = 3f;

    private Animator modelAnimator;
    private DungeonManager dungeonManager;
    private GameObject modelObject;
    private PlayerController target;

    private bool playingDeathAnimation;
    private DungeonRoomController room;
    private bool destroyed;

    public EnemyController Enemy => enemyController;

    private void OnDestroy()
    {
        dungeonManager.Boss = null;
        models = null;
        enemyController = null;
        animator = null;
        modelAnimator = null;
        dungeonManager = null;
        modelObject = null;
        target = null;
        room = null;
        destroyed = true;
    }

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
                Shinobytes.Debug.LogError("Blerp");
            }
        }

        name = "___DUNGEON__BOSS___";
        this.Enemy.Lock();
        this.transform.SetParent(null);

    }

    // Update is called once per frame
    void Update()
    {
        if (GameCache.IsAwaitingGameRestore) return;
        if (destroyed)
        {
            return;
        }
        //if (!Enemy.InCombat)
        //{
        //    this.Enemy.Lock();
        //}
        if (!dungeonManager || !dungeonManager.Started)
        {
            if (dungeonManager && !dungeonManager.Active)
                Die();

            return;
        }

        //if (UpdateAction())
        //{
        //    HandleAttack();
        //}

        UpdateHealthBar();
    }

    public void UpdateHealthBar()
    {
        var proc = (float)Enemy.Stats.Health.CurrentValue / Enemy.Stats.Health.Level;

        dungeonManager.Notifications.SetHealthBarValue(proc, Enemy.Stats.Health.Level);
    }

    public bool UpdateAction()
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

            deathTimer -= GameTime.deltaTime;
            if (deathTimer <= 0f)
            {
                Die();
            }

            return false;
        }
        return true;
    }

    //private void HandleAttack()
    //{
    //    target = GetAttackableTarget();
    //    attackTimer -= Time.deltaTime;

    //    if (!target)
    //    {
    //        return;
    //    }

    //    var dist = Vector3.Distance(Enemy.Position, target.Position);
    //    if (dist >= hitRange)
    //    {
    //        Enemy.SetDestination(target.Position);
    //        return;
    //    }
        
    //    if (attackTimer <= 0f)
    //    {
    //        Enemy.Lock();
    //        Attack();
    //    }
    //}

    //private void Attack()
    //{
    //    if (destroyed)
    //    {
    //        return;
    //    }

    //    attackTimer = attackInterval;

    //    if (this == null || enemyController == null)
    //    {
    //        return;
    //    }

    //    var damage = GameMath.CalculateMeleeDamage(enemyController, target);

    //    if (animator)
    //    {
    //        animator.SetInteger("AttackType", UnityEngine.Random.Range(0, 4));
    //        animator.SetTrigger("AttackTrigger");
    //    }

    //    target.TakeDamage(enemyController, (int)damage);
    //}

    public PlayerController GetAttackableTarget()
    {
        var players = dungeonManager.GetAlivePlayers();
        if (players.Count == 0) return null;
        var hasAliveEnemies = dungeonManager.HasAliveEnemies();
        if (hasAliveEnemies) return null;

        try
        {
            if ((players.Count == 1 || players.All(x => x.TrainingHealing)))
            {
                return players.FirstOrDefault(x => x != null && x && !x.Stats.IsDead);
            }

            return players
                .Where(x => x != null && x && !x.Stats.IsDead)
                .OrderByDescending(x =>
                {
                    enemyController.Aggro.TryGetValue(x.Name, out var aggro);
                    return aggro + UnityEngine.Random.value;
                })
                .FirstOrDefault();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
            return null;
        }
    }

    public void Create(
        Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq, float healthScale = 100f)
    {
        var model = models.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (!model)
        {
            Shinobytes.Debug.LogError("No available dungeon boss models??!?!");
            return;
        }

        modelObject = Instantiate(model, transform);
        modelAnimator = modelObject.GetComponent<Animator>();
        if (modelAnimator)
        {
            modelAnimator.enabled = false;
            animator.avatar = modelAnimator.avatar;
        }

        SetStats(rngLowStats, rngHighStats, rngLowEq, rngHighEq, healthScale);

        this.Enemy.Lock();
    }

    public void SetStats(Skills rngLowStats, Skills rngHighStats, EquipmentStats rngLowEq, EquipmentStats rngHighEq, float healthScale = 100f)
    {
        enemyController.Stats = GenerateCombatStats(rngLowStats ?? new Skills(), rngHighStats ?? new Skills(), healthScale);
        enemyController.EquipmentStats = GenerateEquipmentStats(rngLowEq ?? new EquipmentStats(), rngHighEq ?? new EquipmentStats());
        transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Min(3.5f, enemyController.Stats.CombatLevel * 0.003f));
        modelObject.transform.localScale = Vector3.one;

        this.Enemy.Lock();
    }

    public void Die()
    {
        dungeonManager.Notifications.SetHealthBarValue(0);
        if (destroyed) return;
        destroyed = true;
        Destroy(this.gameObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Skills GenerateCombatStats(Skills rngLowStats, Skills rngHighStats, float healthScale = 100f)
    {
        var skills = Skills.Random(rngLowStats, rngHighStats);

        skills.Health.Level = skills.Health.CurrentValue = (int)(skills.Health.Level * healthScale);
        return skills;
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
