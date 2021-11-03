using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IAttackable
{
    internal readonly HashSet<string> AttackerNames = new HashSet<string>();
    internal readonly List<IAttackable> Attackers = new List<IAttackable>();

    private readonly Dictionary<string, float> attackerAggro = new Dictionary<string, float>();

    [SerializeField] private bool positionLocked = true;
    [SerializeField] private Vector3 spawnPoint;
    [SerializeField] private Quaternion spawnRotation;
    [SerializeField] private float respawnTime = 7F;
    [SerializeField] private HealthBarManager healthBarManager;
    [SerializeField] private float healthBarOffset = 0f;
    [SerializeField] private float attackRange = 2.88f;
    [SerializeField] private double experience;

    [SerializeField] private float attackTimer;
    [SerializeField] private float attackInterval = 0.22f;

    [SerializeField] public EquipmentStats EquipmentStats;
    [SerializeField] public Skills Stats;

    public float AggroRange = 7.5f;

    public bool HandleFightBack = true;
    public bool RotationLocked = false;
    public bool AutomaticRespawn = true;

    private float noDamageDropTargetTimer;
    private float highestAttackerAggroValue;
    private HealthBar healthBar;
    private WalkyWalkyScript movement;
    private DamageCounterManager damageCounterManager;
    //private NavMeshAgent agent;
    private bool isVisbile = true;
    private bool spawnPositionSet;

    private DungeonBossController dungeonBossController;
    private RaidBossController raidBossController;
    private SphereCollider hitRangeCollider;

    private GameManager game;

    public IslandController Island;
    public IReadOnlyDictionary<string, float> Aggro => attackerAggro;
    public bool InCombat { get; private set; }
    public string Name => name;
    public float HealthBarOffset => healthBarOffset;
    public bool IsRaidBoss { get; private set; }
    public bool IsDungeonBoss { get; private set; }

    internal Vector3 PositionInternal;
    private float hitRangeRadius;

    public Vector3 Position => PositionInternal;
    public int GetAttackerCountExcluding(PlayerController player)
    {
        if (AttackerNames.Contains(player.Name))
        {
            return Attackers.Count - 1;
        }
        return Attackers.Count;
    }
    void Start()
    {

        this.Island = GetComponentInParent<IslandController>();

        this.dungeonBossController = GetComponent<DungeonBossController>();
        this.raidBossController = GetComponent<RaidBossController>();

        this.game = FindObjectOfType<GameManager>();

        this.IsDungeonBoss = !!this.dungeonBossController;
        this.IsRaidBoss = !!raidBossController;

        //if (!wander) wander = this.GetComponent<WanderScript>();                
        if (!damageCounterManager) damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();
        if (!healthBarManager) healthBarManager = GameObject.FindObjectOfType<HealthBarManager>();
        if (!movement) movement = GetComponent<WalkyWalkyScript>();

        PositionInternal = this.transform.position;

        if (!spawnPositionSet)
        {
            spawnPoint = Position;
            spawnRotation = transform.rotation;
            spawnPositionSet = true;
        }

        if (game.Graphics && game.Graphics.IsGraphicsEnabled && healthBarManager && !IsRaidBoss && !IsDungeonBoss)
        {
            healthBar = healthBarManager.Add(this);
        }

        //if (game.Graphics.IsGraphicsEnabled && healthBarManager && !IsRaidBoss && !IsDungeonBoss)
        //{
        //    healthBar = healthBarManager.Add(this);
        //}

        if (EquipmentStats == null)
        {
            EquipmentStats = new EquipmentStats();
        }
        if (Stats == null)
        {
            Stats = new Skills();
        }

        this.hitRangeCollider = GetComponent<SphereCollider>();
        if (hitRangeCollider)
        {
            hitRangeRadius = hitRangeCollider.radius;
        }
    }
    public float GetHitRange()
    {
        return hitRangeRadius;
    }
    void Update()
    {
        PositionInternal = this.transform.position;
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        if (!InCombat || !Target)
        {
            return;
        }

        if (noDamageDropTargetTimer >= 0)
        {
            noDamageDropTargetTimer += Time.deltaTime;
        }

        if (noDamageDropTargetTimer >= 5.0f)
        {
            noDamageDropTargetTimer = -1f;
            Target = null;
            SetDestination(spawnPoint);
            return;
        }

        var targetPlayer = Target.GetComponent<PlayerController>();
        if (!targetPlayer) return;

        if (targetPlayer.Stats.IsDead || targetPlayer.Removed)
        {
            Target = null;
            InCombat = false;
            //SetDestination(transform.position);
            Lock();
            return;
        }

        var dist = Vector3.Distance(Position, Target.position);
        if (dist >= (attackRange + targetPlayer.GetAttackRange()))
        {
            Lock();
            return;
        }

        if (dist <= attackRange)
        {
            // we can reach our target
            Lock();

            if (HandleFightBack)
            {
                AttackTarget(targetPlayer);
            }
        }
        else
        {
            SetDestination(Target.position);
        }
    }

    private void AttackTarget(PlayerController targetPlayer)
    {
        if (!targetPlayer) return;
        if (!movement) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            movement.Attack(() =>
            {

                if (!Target && targetPlayer)
                {
                    Target = targetPlayer.transform;
                }

                if (Stats.IsDead || !Target || !targetPlayer)
                {
                    Target = null;
                    return;
                }

                var damage = GameMath.CalculateMeleeDamage(this, targetPlayer);
                if (targetPlayer.TakeDamage(this, (int)damage))
                {
                    Target = null;
                }
            });

            attackTimer = attackInterval;
        }
    }

    private void SetDestination(Vector3 position)
    {
        Unlock();
        movement.SetDestination(position);
    }

    private void OnBecameVisible()
    {
        this.isVisbile = true;
    }
    private void OnBecameInvisible()
    {
        this.isVisbile = false;
    }

    void LateUpdate()
    {
        if (!isVisbile)
        {
            return;
        }

        if (positionLocked)
        {
            transform.position = spawnPoint;
            transform.rotation = spawnRotation;
        }

        if (RotationLocked)
        {
            return;
        }

        if (Target)
        {
            transform.LookAt(Target);

            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, euler.z);
        }
    }

    public Transform Transform => gameObject.transform;

    public double GetExperience()
    {
        return experience;
    }

    public bool GivesExperienceWhenKilled { get; set; } = true;

    public Transform Target { get; private set; }

    public void ClearAttackers()
    {
        AttackerNames.Clear();
        Attackers.Clear();
    }
    public bool Heal(IAttackable attacker, int damage)
    {
        return false;
    }
    public bool Attack(IAttackable attacker)
    {
        if (this == null || !this || gameObject == null || !gameObject)
        {
            return false;
        }

        if (attacker == null)
        {
            return false;
        }

        if (Stats.IsDead)
        {
            return false;
        }

        if (!(attacker is PlayerController player))
        {
            return false;
        }

        if (!player)
        {
            return false;
        }

        try
        {
            if (Attackers.Count == 0)
            {
                Target = player.transform;
                transform.LookAt(player.transform);
            }

            var attackerName = attacker.Name;
            if (AttackerNames.Add(attackerName))
            {
                Attackers.Add(attacker);
            }

            if (!damageCounterManager)
                damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();

            InCombat = true;

            attackerAggro.TryGetValue(attackerName, out var aggro);

            var aggroMultiplier = 1f;

            var totalAggro = aggro;

            attackerAggro[attackerName] = totalAggro;

            if (highestAttackerAggroValue <= totalAggro || Target == null)
            {
                highestAttackerAggroValue = totalAggro;

                Target = attacker.Transform;
            }

            UpdateHealthbar();

            if (!Stats.IsDead)
            {
                // when an enemy has been ignored for some time, it will lose its interest
                // to attack its target. this is to avoid having them forever following
                // a player that has stopped training combat.
                noDamageDropTargetTimer = 0f;
                return false;
            }

            noDamageDropTargetTimer = -1f;
            InCombat = false;
            highestAttackerAggroValue = 0;
            Target = null;
            Unlock();

            if (movement) movement.Die();
            if (AutomaticRespawn) Respawn();
            return true;
        }
        catch (Exception exc)
        {
            GameManager.LogError("Error handling Damage " + exc);
            return false;
        }
    }

    public bool TakeDamage(IAttackable attacker, int damage)
    {
        if (this == null || !this || gameObject == null || !gameObject)
        {
            return false;
        }

        if (attacker == null)
        {
            return false;
        }

        if (Stats.IsDead)
        {
            return false;
        }

        if (!(attacker is PlayerController player))
        {
            return false;
        }

        if (!player)
        {
            return false;
        }


        try
        {
            if (Attackers.Count == 0)
            {
                Target = player.transform;
                transform.LookAt(player.transform);
            }

            if (!damageCounterManager)
                damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();

            InCombat = true;

            damageCounterManager.Add(transform, damage, false, IsDungeonBoss || IsRaidBoss);//IsDungeonBoss || IsRaidBoss || this.Attackers.Count >= 5);
            //dc.Color = player.PlayerNameHexColor;

            Stats.Health.Add(-damage);

            var aggroMultiplier = 1f;
            var sword = player.Inventory.GetMeleeWeapon();
            if (sword != null && sword.Type == RavenNest.Models.ItemType.OneHandedSword)
                aggroMultiplier = 2.5f;

            float aggro = 0;
            if (attacker != null)
            {
                var attackerName = attacker.Name;
                if (AttackerNames.Add(attackerName))
                {
                    Attackers.Add(attacker);
                }

                attackerAggro.TryGetValue(attackerName, out aggro);

                var totalAggro = aggro + (damage * aggroMultiplier);

                attackerAggro[attackerName] = totalAggro;

                if (highestAttackerAggroValue <= totalAggro || Target == null)
                {
                    highestAttackerAggroValue = totalAggro;

                    Target = attacker.Transform;
                }
            }


            if (!Stats.IsDead)
            {
                UpdateHealthbar();

                // when an enemy has been ignored for some time, it will lose its interest
                // to attack its target. this is to avoid having them forever following
                // a player that has stopped training combat.
                noDamageDropTargetTimer = 0f;
                return false;
            }

            noDamageDropTargetTimer = -1f;
            InCombat = false;
            highestAttackerAggroValue = 0;
            Target = null;
            Unlock();

            if (movement) movement.Die();
            if (AutomaticRespawn) Respawn();
            return true;
        }
        catch (Exception exc)
        {
            GameManager.LogError("Error handling Damage " + exc);
            return false;
        }
    }

    private void UpdateHealthbar()
    {
        if (this.IsRaidBoss || this.IsDungeonBoss)
        {
            return;
        }

        if (!game.Graphics.IsGraphicsEnabled)
        {
            return;
        }

        if (!healthBar)
        {
            healthBar = healthBarManager.Add(this);
        }
        if (healthBar)
        {
            healthBar.UpdateHealth();
        }
    }

    internal void SetExperience(double v)
    {
        this.experience = v;
    }

    public IReadOnlyList<IAttackable> GetAttackers()
    {
        return Attackers;
    }

    public EquipmentStats GetEquipmentStats()
    {
        return EquipmentStats;
    }

    public Skills GetStats()
    {
        return Stats ?? new Skills();
    }
    public int GetCombatStyle()
    {
        return 1;
    }

    public void Respawn(float forcedRespawnTime = -1)
    {
        try
        {
            if (!this.gameObject.activeSelf)
            {
                RespawnImpl();
                return;
            }

            StartCoroutine(_Respawn(forcedRespawnTime));
        }
        catch (Exception exc)
        {
            // do nothing right now
        }
    }

    private IEnumerator _Respawn(float forcedRespawnTime = -1)
    {
        yield return new WaitForSeconds(forcedRespawnTime != -1
            ? forcedRespawnTime
            : respawnTime);

        gameObject.SetActive(true);

        yield return null;

        if (movement) movement.Revive();

        RespawnImpl();
    }

    private void RespawnImpl()
    {
        Lock();

        gameObject.SetActive(true);
        if (movement) movement.Revive();

        ClearAttackers();
        attackerAggro.Clear();

        transform.position = spawnPoint;
        //transform.rotation = spawnRotation;
        Stats.Health.Reset();

        Unlock();
    }

    public void Lock()
    {
        if (!movement) movement = GetComponent<WalkyWalkyScript>();
        if (!movement) return;
        movement.Lock();
    }

    public void Unlock()
    {
        if (!movement) movement = GetComponent<WalkyWalkyScript>();
        if (!movement) return;
        movement.Unlock();
    }

    public void AddAttacker(PlayerController player)
    {
        if (AttackerNames.Add(player.PlayerName))
        {
            Attackers.Add(player);
        }
    }
}