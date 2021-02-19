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
    private readonly ConcurrentDictionary<string, IAttackable> attackers
        = new ConcurrentDictionary<string, IAttackable>();

    private readonly ConcurrentDictionary<string, float> attackerAggro
        = new ConcurrentDictionary<string, float>();

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

    public bool HandleFightBack = true;
    public bool RotationLocked = false;
    public bool AutomaticRespawn = true;

    private float noDamageDropTargetTimer;
    private float highestAttackerAggroValue;
    private HealthBar healthBar;
    private WalkyWalkyScript movement;
    private DamageCounterManager damageCounterManager;
    //private NavMeshAgent agent;

    public IReadOnlyList<IAttackable> Attackers => attackers.Values.ToList();
    public IReadOnlyDictionary<string, float> Aggro => attackerAggro;
    public bool InCombat { get; private set; }
    public string Name => name;

    public float HealthBarOffset => healthBarOffset;

    void Start()
    {
        //if (!wander) wander = this.GetComponent<WanderScript>();                
        if (!damageCounterManager) damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();
        if (!healthBarManager) healthBarManager = GameObject.FindObjectOfType<HealthBarManager>();
        if (!movement) movement = GetComponent<WalkyWalkyScript>();

        spawnPoint = transform.position;
        spawnRotation = transform.rotation;

        if (healthBarManager && !GetComponent<RaidBossController>())
        {
            healthBar = healthBarManager.Add(this);
        }

        if (EquipmentStats == null)
        {
            EquipmentStats = new EquipmentStats();
        }
        if (Stats == null)
        {
            Stats = new Skills();
        }
    }

    void Update()
    {
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

        if (targetPlayer.Stats.IsDead)
        {
            Target = null;
            InCombat = false;
            //SetDestination(transform.position);
            Lock();
            return;
        }

        var dist = Vector3.Distance(Transform.position, Target.position);
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

    void LateUpdate()
    {
        if (positionLocked)
        {
            transform.position = spawnPoint;
            transform.rotation = spawnRotation;
        }

        if (RotationLocked) return;
        if (Target)
        {
            transform.LookAt(Target);
        }

        var euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, euler.z);
    }

    public Transform Transform => gameObject.transform;

    public decimal GetExperience()
    {
        return (decimal)experience;
    }

    public bool GivesExperienceWhenKilled { get; set; } = true;

    public Transform Target { get; private set; }

    public bool Heal(IAttackable attacker, int damage)
    {
        return false;
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
            if (attackers.Count == 0)
            {
                Target = player.transform;
                transform.LookAt(player.transform);
            }

            var attackerName = attacker.Name;

            attackers[attackerName] = attacker;

            if (!damageCounterManager)
                damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();

            InCombat = true;

            var dc = damageCounterManager.Add(transform, damage);
            //dc.Color = player.PlayerNameHexColor;

            Stats.Health.Add(-damage);

            attackerAggro.TryGetValue(attackerName, out var aggro);

            var aggroMultiplier = 1f;
            var sword = player.Inventory.GetMeleeWeapon();
            if (sword != null && sword.Type == RavenNest.Models.ItemType.OneHandedSword)
                aggroMultiplier = 2.5f;

            var totalAggro = aggro + (damage * aggroMultiplier);

            attackerAggro[attackerName] = totalAggro;

            if (highestAttackerAggroValue <= totalAggro)
            {
                highestAttackerAggroValue = totalAggro;
                if (attackerName != Target?.name)
                {
                    Target = attacker.Transform;
                }
            }

            if (healthBar)
            {
                healthBar.UpdateHealth();
            }

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
        catch
        {
            return false;
        }
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

    public void Respawn()
    {
        StartCoroutine(_Respawn());
    }

    private IEnumerator _Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        if (movement) movement.Revive();
        attackers.Clear();
        attackerAggro.Clear();

        transform.position = spawnPoint;
        //transform.rotation = spawnRotation;
        Stats.Health.Reset();
    }

    public void Lock()
    {
        movement.Lock();
    }

    public void Unlock()
    {
        movement.Unlock();
    }

    public void AddAttacker(PlayerController player)
    {
        attackers[player.PlayerName] = player;
    }
}