using Assets.Scripts;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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
    //[SerializeField] private double experience;

    [SerializeField] private float attackTimer;
    [SerializeField] private float attackInterval = 0.22f;

    [SerializeField] public EquipmentStats EquipmentStats;
    [SerializeField] public Skills Stats;

    public double ExpFactor = 1d;

    public float AggroRange = 7.5f;
    public bool HandleFightBack = true;
    public bool RotationLocked = false;

    public bool AutomaticRespawn = true;

    private float noDamageDropTargetTimer;
    private float highestAttackerAggroValue;
    private HealthBar healthBar;
    private EnemyMovementController movement;
    private DamageCounterManager damageCounterManager;

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

    public bool Removed;

    public Vector3 Position => PositionInternal;

    float showDebugInfoTime;

    public PlayerController TargetPlayer;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    public int GetAttackerCountExcluding(PlayerController player)
    {
        if (AttackerNames.Contains(player.Name))
        {
            return Attackers.Count - 1;
        }
        return Attackers.Count;
    }

    public void OnDrawGizmosSelected()
    {
        showDebugInfoTime = 1f;
        if (this.Target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.transform.position, this.Target.position);
        }

        if (this.movement.isMovingInternal)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(this.transform.position, this.movement.Destination);
        }
    }

    void OnGUI()
    {
        if (showDebugInfoTime > 0f)
        {
            showDebugInfoTime -= Time.deltaTime;
            var w = 300f;
            var h = 20f;
            var x = (Screen.width / 2f) - (w / 2f);
            var y = (Screen.height / 2f) - (h / 2f);
            var spacing = 5;

            if (this.Target)
            {
                GUI.Label(new Rect(x, y, w, h), "Target: " + Target.name);
                y += h + spacing;
            }
            else if (InCombat)
            {
                GUI.Label(new Rect(x, y, w, h), "In combat, no target");
                y += h + spacing;
            }
            else
            {
                GUI.Label(new Rect(x, y, w, h), "Not in combat");
                y += h + spacing;
            }

            GUI.Label(new Rect(x, y, w, h), "Destination dist: " + Vector3.Distance(transform.position, movement.Destination));
            y += h + spacing;

            GUI.Label(new Rect(x, y, w, h), "Destination Changed: " + movement.DestinationChangeTime);
            y += h + spacing;

        }
    }

    void OnDestroy()
    {
        Removed = true;
        attackerAggro.Clear();
        Attackers.Clear();
        AttackerNames.Clear();

        if (healthBar)
        {
            healthBarManager.Remove(this.healthBar);
            healthBar = null;
            healthBarManager = null;
        }

        EquipmentStats = null;
        Stats = null;
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
        if (!movement) movement = GetComponent<EnemyMovementController>();

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
        if (GameCache.IsAwaitingGameRestore)
        {
            return;
        }


        PositionInternal = this.transform.position;

        //if (SpawnPoint && Mathf.Abs(SpawnPoint.transform.position.y - PositionInternal.y) >= 5)
        //{
        //    SetPosition(SpawnPoint.transform.position);
        //    //this.transform.position = ;
        //}

        if ((!InCombat || !Target) && !IsDungeonBoss)
        {
            return;
        }

        if (!IsDungeonBoss)
        {
            if (noDamageDropTargetTimer >= 0)
            {
                noDamageDropTargetTimer += Time.deltaTime;
            }

            if (noDamageDropTargetTimer >= 5.0f)
            {
                noDamageDropTargetTimer = -1f;
                ClearTarget();
                SetDestination(spawnPoint);
                return;
            }

            if (!TargetPlayer)
            {
                TargetPlayer = Target.GetComponent<PlayerController>();
                if (!TargetPlayer)
                    return;
            }
        }

        if (IsDungeonBoss)
        {
            if (!dungeonBossController.UpdateAction())
            {
                return;
            }

            SetTarget(dungeonBossController.GetAttackableTarget());

            if (!TargetPlayer)
            {
                return;
            }
            //dungeonBossController.UpdateHealthBar();
        }
        else
        {
            if (TargetPlayer.Stats.IsDead || TargetPlayer.isDestroyed || TargetPlayer.Removed || this.Island != TargetPlayer.Island)
            {
                ClearTarget();
                InCombat = false;
                Lock();
                return;
            }
        }

        var dist = Vector3.Distance(Position, Target.position);
        if (dist >= (attackRange + TargetPlayer.GetAttackRange()) && !IsDungeonBoss)
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
                AttackTarget(TargetPlayer);
            }
        }
        else
        {
            SetDestination(Target.position);
        }
    }

    public void AttackTarget(PlayerController targetPlayer)
    {
        if (!targetPlayer) return;
        if (!movement) return;
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            movement.Attack(() =>
            {
                if (this == null)
                {
                    return;
                }

                if (!Target && targetPlayer)
                {
                    SetTarget(targetPlayer);
                }

                if (Stats.IsDead || !Target || !targetPlayer)
                {
                    ClearTarget();
                    return;
                }

                var damage = GameMath.CalculateMeleeDamage(this, targetPlayer);
                if (targetPlayer.TakeDamage(this, (int)damage))
                {
                    ClearTarget();
                }
            });

            attackTimer = attackInterval;
        }
    }

    private void SetTarget(PlayerController targetPlayer)
    {
        TargetPlayer = targetPlayer;
        if (targetPlayer)
        {
            Target = targetPlayer.transform;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearTarget()
    {
        TargetPlayer = null;
        Target = null;
    }

    public void SetDestination(Vector3 position)
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

    public bool GivesExperienceWhenKilled { get; set; } = true;

    public Transform Target { get; private set; }
    public EnemySpawnPoint SpawnPoint;

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
                SetTarget(player);
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

            //var aggroMultiplier = 1f;

            var totalAggro = aggro;

            attackerAggro[attackerName] = totalAggro;

            if (highestAttackerAggroValue <= totalAggro || Target == null)
            {
                highestAttackerAggroValue = totalAggro;
                SetTarget(attacker as PlayerController);
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

            ClearTarget();

            Unlock();

            if (movement) movement.Die();
            if (AutomaticRespawn) Respawn();
            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error handling Damage: " + exc.Message);
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
                SetTarget(player);
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
                    SetTarget(attacker as PlayerController);
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
            ClearTarget();
            Unlock();

            if (movement) movement.Die();
            if (AutomaticRespawn) Respawn();
            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error handling Damage: " + exc.Message);
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
        return Stats ?? (Stats = new Skills());
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
        ClearAttackers();
        attackerAggro.Clear();
        transform.position = spawnPoint;
        //transform.rotation = spawnRotation;
        Stats.Health.Reset();
        if (movement) movement.Revive();
        Unlock();
    }

    internal void ResetState()
    {
        ClearAttackers();
        attackerAggro.Clear();
        Stats.Health.Reset();
        ClearTarget();
        if (movement) movement.ResetVisibility();
    }

    public void Lock()
    {
        if (!movement) movement = GetComponent<EnemyMovementController>();
        if (!movement) return;
        movement.Lock();
    }

    public void Unlock()
    {
        if (!movement) movement = GetComponent<EnemyMovementController>();
        if (!movement) return;
        movement.Unlock();
    }

    internal void SetPosition(Vector3 position)
    {
        if (!movement) movement = GetComponent<EnemyMovementController>();
        if (!movement) return;
        movement.SetPosition(position);
    }

    public void AddAttacker(PlayerController player)
    {
        if (AttackerNames.Add(player.PlayerName))
        {
            Attackers.Add(player);
        }
    }

}