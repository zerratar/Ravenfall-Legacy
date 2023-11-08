using Assets.Scripts;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IAttackable
{
    internal readonly HashSet<string> AttackerNames = new HashSet<string>();
    internal readonly List<IAttackable> Attackers = new List<IAttackable>();
    private readonly Dictionary<string, float> attackerAggro = new Dictionary<string, float>();
    private static RuntimeAnimatorController humanoidEnemy;

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
    [SerializeField] public DungeonRoomController DungeonRoom;

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
    private DungeonController dungeon;
    private SphereCollider hitRangeCollider;

    private GameManager game;

    public IslandController Island;
    public IReadOnlyDictionary<string, float> Aggro => attackerAggro;
    public bool InCombat { get; private set; }
    public string Name => name;
    public float HealthBarOffset => healthBarOffset;
    public bool IsRaidBoss { get; private set; }
    public bool IsDungeonBoss { get; private set; }
    public bool IsDungeonEnemy;
    internal Vector3 PositionInternal;

    private float hitRangeRadius;

    public bool Removed;

    public Vector3 Position => PositionInternal;

    float showDebugInfoTime;

    public PlayerController TargetPlayer;
    private StatsModifiers statsModifiers = new StatsModifiers();
    public StatsModifiers GetModifiers() => statsModifiers;


    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    [Button("Assign Dependencies")]
    public void AssignDependencies()
    {
        if (!this.game) this.game = FindObjectOfType<GameManager>();
        if (!damageCounterManager) damageCounterManager = GameObject.FindObjectOfType<DamageCounterManager>();
        if (!healthBarManager) healthBarManager = GameObject.FindObjectOfType<HealthBarManager>();
        if (!movement) movement = GetComponent<EnemyMovementController>();

        if (!this.Island)
        {
            var im = GameObject.FindObjectOfType<IslandManager>();
            this.Island = im.FindIsland(this.transform.position);
        }
    }

    [Button("Rename")]
    public void Rename()
    {
        this.name = "Enemy Lv. " + Stats.CombatLevel;
    }

    [Button("Make into enemy")]
    public void Create()
    {
        AdjustPlacement();

        this.HandleFightBack = true;
        this.healthBarOffset = 0.25f;
        this.attackRange = 5f;
        this.attackTimer = 0f;
        this.attackInterval = 0.75f;
        this.ExpFactor = 1f;
        this.AggroRange = 7.5f;
        this.AutomaticRespawn = true;

        this.gameObject.EnsureComponent<SphereCollider>(x =>
        {
            x.center = new Vector3(0.01979065f, 1.021983f, 0f);
            x.radius = 1.796585f;
        });

        this.gameObject.EnsureComponent<CapsuleCollider>(x =>
        {
            x.center = new Vector3(0, 0.84f, 0);
            x.radius = 0.24f;
            x.height = 1.89f;
        });

        this.gameObject.EnsureComponent<Animator>(x =>
        {
            // set runtime controller to Humanoid Enemy. Unfortunately its not in our resources folder so we cant.
            //x.runtimeAnimatorController = 
            // but! we can find another enemycontroller that has one!

            // default to pick humanoid Enemy, as 90%+ of the enemies are humanoids.
            if (!humanoidEnemy)
            {
                foreach (var enemy in FindObjectsOfType<EnemyController>())
                {
                    var animator = enemy.GetComponent<Animator>();
                    if (!animator) continue;
                    var controller = animator.runtimeAnimatorController;
                    if (controller && controller.name == "Humanoid Enemy")
                    {
                        humanoidEnemy = controller;
                        break;
                    }
                }
            }
            if (!x.runtimeAnimatorController)
                x.runtimeAnimatorController = humanoidEnemy;
        });

        var nma = this.gameObject.EnsureComponent<NavMeshAgent>(x =>
        {
            x.speed = 3.5f;
            x.angularSpeed = 120;
            x.acceleration = 8f;
            x.stoppingDistance = 0;
            x.autoBraking = true;
            x.radius = 0.5f;
            x.height = 2f;
            x.avoidancePriority = 50;
            x.baseOffset = -0.125f;
        });

        this.gameObject.EnsureComponent<EnemyMovementController>(x =>
        {
            x.minDestinationDistance = 1f;
            x.attackAnimationLength = 0.5f;
            x.deathAnimationLength = 1f;
            x.enemyController = this;
            x.navMeshAgent = nma;
        });

        AssignDependencies();

        // reload all chunks. We may not have the latest one
        this.game.Chunks.Init(true);

        // after finding island, lets compare to other islands with fighting chunks.
        var fightingAreas = this.game.Chunks.GetChunksOfType(TaskType.Fighting).OrderBy(x => x.RequiredCombatLevel + x.RequiredSkilllevel).ToArray();

        // get previous chunk, and not the one owned by this enemy.
        var thisChunk = GetComponentInParent<Chunk>();
        var thisIndex = System.Array.IndexOf(fightingAreas, thisChunk);
        if (thisIndex > 1)
        {
            var strongestEnemies = new List<EnemyController>();
            var levelRequirements = new List<int>();
            for (var i = 0; i < thisIndex; i++)
            {
                var chunk = fightingAreas[i];
                levelRequirements.Add(chunk.RequiredCombatLevel + chunk.RequiredSkilllevel);
                var enemies = chunk.gameObject.GetComponentsInChildren<EnemyController>(true);
                strongestEnemies.Add(enemies.OrderByDescending(x => x.Stats.CombatLevel).FirstOrDefault());
            }

            if (strongestEnemies.Count < 1)
            {
                // didnt work.
                return;
            }

            // lets simplify it, take first and last, delta divided by the count
            var last = strongestEnemies[^1];
            var first = strongestEnemies[0];
            var statsDelta = last.Stats - first.Stats;
            var eqDelta = last.EquipmentStats - first.EquipmentStats;
            var statsAvg = statsDelta / strongestEnemies.Count;
            var eqAvg = eqDelta / strongestEnemies.Count;

            var avgLevelReqJump = levelRequirements.Average();
            var thisLevelReqJump = (thisChunk.RequiredCombatLevel + thisChunk.RequiredSkilllevel) - levelRequirements[^1];

            var boostLvReq = Mathf.Max(1f, (float)thisLevelReqJump / (float)avgLevelReqJump);

            var levelJumpInc = Mathf.Max(1f, 1f - boostLvReq) * 0.25f;

            //this.Stats = ... get stats
            //last.Stats.CopyTo(this.Stats);
            //this.Stats.TakeBestOf(last.Stats);
            this.Stats = new Skills();
            this.Stats += last.Stats + (last.Stats * levelJumpInc);
            this.Stats += (statsAvg * boostLvReq);

            this.EquipmentStats = new EquipmentStats();
            this.EquipmentStats += last.EquipmentStats;
            this.EquipmentStats += (eqAvg * boostLvReq);
        }
        if (this.name.Contains("_") || this.name.Contains(" Lv. ")) // only prefab names should be replaced. so we don't accidently rename Named enemies.
            this.Rename();
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
        try
        {
            showDebugInfoTime = 1f;
            if (this.Target)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, this.Target.position);
            }

            if (this.movement && this.movement.isMovingInternal)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(this.transform.position, this.movement.Destination);
            }
        }
        catch (System.Exception exc)
        {
            // ignored
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
        this.dungeon = GetComponentInParent<DungeonController>();

        if (dungeon && !DungeonRoom)
        {
            DungeonRoom = GetComponentInParent<DungeonRoomController>();
        }

        if (!this.game)
            this.game = FindObjectOfType<GameManager>();

        this.IsDungeonBoss = !!this.dungeonBossController;
        this.IsRaidBoss = !!raidBossController;

        if (IsDungeonBoss || Island == null || dungeon || DungeonRoom)
        {
            IsDungeonEnemy = true;
        }

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

        if ((!InCombat || !Target) && !IsDungeonEnemy)
        {
            return;
        }

        if (!IsDungeonBoss)
        {
            if (noDamageDropTargetTimer >= 0)
            {
                noDamageDropTargetTimer += GameTime.deltaTime;
            }

            if (noDamageDropTargetTimer >= 5.0f)
            {
                noDamageDropTargetTimer = -1f;
                ClearTarget();
                SetDestination(spawnPoint);
                return;
            }

            if (!TargetPlayer && Target)
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
        else if (!HasValidTarget)
        {
            ClearTarget();
            InCombat = false;
            Lock();
            return;
        }

        var dist = Vector3.Distance(Position, Target.position);
        //if (dist >= (attackRange + TargetPlayer.GetAttackRange()) && !IsDungeonEnemy)
        //{
        //    Lock();
        //    return;
        //}

        if (dist <= attackRange * 1.05f) // add some margin
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

    public bool HasValidTarget
    {
        get
        {
            return TargetPlayer && !TargetPlayer.Stats.IsDead && !TargetPlayer.isDestroyed && !TargetPlayer.Removed && (this.Island == TargetPlayer.Island || !IsDungeonEnemy);
        }
    }

    public void AttackTarget(PlayerController targetPlayer)
    {
        if (!targetPlayer) return;
        if (!movement) return;
        attackTimer -= GameTime.deltaTime;
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

    public void SetTarget(PlayerController targetPlayer)
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

    public bool SetDestination(Vector3 position)
    {
        Unlock();
        return movement.SetDestination(position);
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
    public bool IsUnreachable { get; set; }

    public EnemySpawnPoint SpawnPoint;

    public void ClearAttackers()
    {
        AttackerNames.Clear();
        Attackers.Clear();
    }
    public bool Heal(int damage)
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
                if (IsRaidBoss)
                {
                    raidBossController.Activate();
                }

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

    public void RemoveAttacker(PlayerController player)
    {
        if (AttackerNames.Remove(player.PlayerName))
        {
            Attackers.Remove(player);

            if (Attackers.Count == 0 || TargetPlayer == player)
            {
                TargetPlayer = null;
                Target = null;
            }
        }
    }

    public void AddAttacker(PlayerController player)
    {
        if (AttackerNames.Add(player.PlayerName))
        {
            Attackers.Add(player);
        }
    }

}