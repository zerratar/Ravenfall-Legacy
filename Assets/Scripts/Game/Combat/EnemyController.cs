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
    private WalkyWalkyScript animations;
    private DamageCounterManager damageCounterManager;
    private NavMeshAgent agent;
    private Rigidbody rbody;

    public IReadOnlyList<IAttackable> Attackers => attackers.Values.ToList();
    public IReadOnlyDictionary<string, float> Aggro => attackerAggro;
    public bool InCombat { get; private set; }
    public string Name => name;

    void Start()
    {
        //if (!wander) wander = this.GetComponent<WanderScript>();                
        if (!damageCounterManager) damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();
        if (!healthBarManager) healthBarManager = GameObject.FindObjectOfType<HealthBarManager>();
        if (!animations) animations = GetComponent<WalkyWalkyScript>();

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

        rbody = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!HandleFightBack || !InCombat || !Target)
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
            SetDestination(transform.position);
            return;
        }

        var dist = Vector3.Distance(Transform.position, Target.position);
        if (dist <= attackRange)
        {
            // we can reach our target
            Lock();
            AttackTarget(targetPlayer);
        }
        else
        {
            SetDestination(Target.position);
        }
    }

    private void AttackTarget(PlayerController targetPlayer)
    {
        if (!targetPlayer) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            animations.Attack(() =>
            {
                if (Stats.IsDead || !Target || !targetPlayer)
                {
                    Target = null;
                    return;
                }

                var damage = GameMath.CalculateDamage(this, targetPlayer);
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
        if (!agent) return;
        Unlock();
        agent.SetDestination(position);
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

    public bool TakeDamage(IAttackable attacker, int damage)
    {
        if (this == null || !this || gameObject == null || !gameObject)
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

        if (attackers.Count == 0)
        {
            Target = player.transform;
            transform.LookAt(player.transform);
        }

        attackers[attacker.Name] = attacker;

        if (!damageCounterManager)
            damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();

        InCombat = true;

        damageCounterManager.Add(transform, damage);
        Stats.Health.Add(-damage);

        attackerAggro.TryGetValue(attacker.Name, out var aggro);
        var totalAggro = aggro + damage;
        attackerAggro[attacker.Name] = totalAggro;

        if (highestAttackerAggroValue <= totalAggro)
        {
            highestAttackerAggroValue = totalAggro;
            if (attacker.Name != Target?.name)
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

        if (animations) animations.Die();
        if (AutomaticRespawn) Respawn();
        return true;

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
        if (animations) animations.Revive();
        attackers.Clear();
        attackerAggro.Clear();

        transform.position = spawnPoint;
        //transform.rotation = spawnRotation;
        Stats.Health.Reset();
    }

    public void Lock()
    {
        if (agent && agent.enabled)
        {
            agent.SetDestination(transform.position);
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rbody)
        {
            rbody.isKinematic = true;
        }
    }

    public void Unlock()
    {
        if (agent)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        if (rbody)
        {
            rbody.isKinematic = true;
        }
    }

    public void AddAttacker(PlayerController player)
    {
        attackers[player.PlayerName] = player;
    }
}