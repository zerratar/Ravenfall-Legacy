using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Assets.Scripts;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.AI;
using Resources = RavenNest.Models.Resources;
using Debug = Shinobytes.Debug;
using RavenNest.Models.TcpApi;
using Sirenix.OdinInspector;

public class PlayerController : MonoBehaviour, IAttackable, IPollable
{
    private readonly HashSet<Guid> pickedItems = new HashSet<Guid>();
    internal readonly HashSet<string> AttackerNames = new HashSet<string>();
    internal readonly List<IAttackable> Attackers = new List<IAttackable>();

    public GameManager GameManager;

    [SerializeField] public ManualPlayerController manualPlayerController;
    [NonSerialized] public string taskArgument;
    [SerializeField] public ChunkManager chunkManager;

    [SerializeField] public HealthBarManager healthBarManager;
    [SerializeField] public NavMeshAgent agent;
    [SerializeField] public Animator animator;
    [SerializeField] public RaidHandler raidHandler;
    [SerializeField] public StreamRaidHandler streamRaidHandler;

    [SerializeField] public ClanHandler clanHandler;
    [SerializeField] public DungeonHandler dungeonHandler;
    [SerializeField] public OnsenHandler onsenHandler;

    [SerializeField] public ArenaHandler arenaHandler;
    [SerializeField] public DuelHandler duelHandler;
    [SerializeField] public FerryHandler ferryHandler;
    [SerializeField] public TeleportHandler teleportHandler;
    [SerializeField] public EffectHandler effectHandler;
    [SerializeField] public PlayerAnimationController playerAnimations;
    //[SerializeField] private Rigidbody rbody;

    [SerializeField] private float attackAnimationTime = 1.5f;
    [SerializeField] private float rangeAnimationTime = 1.5f;
    [SerializeField] private float healingAnimationTime = 3f;
    [SerializeField] private float magicAnimationTime = 1.5f;
    [SerializeField] private float chompTreeAnimationTime = 2f;
    [SerializeField] private float rakeAnimationTime = 3f;
    [SerializeField] private float fishingAnimationTime = 3f;
    [SerializeField] private float craftingAnimationTime = 3f;
    [SerializeField] private float cookingAnimationTime = 3f;
    [SerializeField] private float mineRockAnimationTime = 2f;
    [SerializeField] private float respawnTime = 4f;

    [SerializeField] private GameObject[] availableMonsterMeshes;

    [NonSerialized] public Transform _transform;
    private StatsModifiers playerStatsModifiers = new StatsModifiers();
    private ConcurrentDictionary<StatusEffectType, StatusEffect> statusEffects = new ConcurrentDictionary<StatusEffectType, StatusEffect>();

    private SyntyPlayerAppearance playerAppearance;
    private float actionTimer = 0f;
    private Skill lastTrainedSkill = Skill.Attack;

    private ArenaController arena;
    private DamageCounterManager damageCounterManager;

    private TaskType? lateGotoClosestType = null;
    private bool lateGotoClosestSilent;
    internal Transform attackTarget;
    internal object taskTarget;

    [NonSerialized] public Chunk Chunk;

    [NonSerialized] public Skills Stats = new Skills();

    [NonSerialized] public Resources Resources = new Resources();
    //public Statistics Statistics = new Statistics();
    [NonSerialized] public PlayerEquipment Equipment;
    [NonSerialized] public Inventory Inventory;

    [NonSerialized] public string PlayerName;
    [NonSerialized] public string PlayerNameLowerCase;

    [NonSerialized] public string PlayerNameHexColor = "#FFFFFF";

    [NonSerialized] public Guid Id;
    [NonSerialized] public Guid UserId;

    [NonSerialized] public string Platform;
    [NonSerialized] public string PlatformId;

    [NonSerialized] public float TimeSinceLastTaskChange = 9999f;

    public float AttackRange = 1.8f;
    public float RangedAttackRange = 15F;
    public float MagicAttackRange = 15f;
    public float HealingRange = 15f;
    public int PatreonTier;

    public EquipmentStats EquipmentStats = new EquipmentStats();
    public PlayerLootManager Loot = new PlayerLootManager();
    public bool Controlled { get; private set; }

    [NonSerialized] public bool IsModerator;
    [NonSerialized] public bool IsBroadcaster;
    [NonSerialized] public bool IsSubscriber;
    [NonSerialized] public bool IsVip;

    //public int BitsCheered;
    //public int GiftedSubs;
    //public int TotalBitsCheered;
    //public int TotalGiftedSubs;
    public User User { get; private set; }
    public int CharacterIndex { get; private set; }
    public string Identifier { get; private set; }

    [NonSerialized] public DateTime LastChatCommandUtc;

    [NonSerialized] public bool IsGameAdmin;
    [NonSerialized] public bool IsGameModerator;

    public float RegenTime = 10f;
    public float RegenRate = 0.1f;

    [NonSerialized] public Vector3 TempScale = Vector3.one;

    private float regenTimer;
    private float regenAmount;

    private HealthBar healthBar;
    private bool hasBeenInitialized;

    private ItemController targetDropItem;
    private float monsterTimer;
    private float scaleTimer;
    private TaskType currentTask;

    [NonSerialized] public string CurrentTaskName;

    private readonly ConcurrentQueue<GameInventoryItem> queuedItemAdd = new ConcurrentQueue<GameInventoryItem>();

    public Transform Target
    {
        get => attackTarget ?? (taskTarget as IAttackable)?.Transform ?? (taskTarget as Transform) ?? (taskTarget as MonoBehaviour)?.transform;
        private set => attackTarget = value;
    }
    public IAttackable CombatTarget
    {
        get => (attackTarget as IAttackable) ?? (taskTarget as IAttackable);
    }
    public CharacterRestedState Rested { get; private set; } = new CharacterRestedState();
    public RavenNest.Models.Player Definition { get; private set; }

    private Transform cachedTransform;
    public Transform Transform
    {
        get
        {
            if (isDestroyed)
            {
                return null;
            }

            try
            {
                if (cachedTransform == null)
                {
                    cachedTransform = this.transform;
                }

                return cachedTransform; //gameObject != null && gameObject && transform != null && transform && gameObject.transform != null ? gameObject.transform : null;
            }
            catch { return null; }
        }
    }

    [SerializeField] internal PlayerMovementController Movement;
    public Vector3 Position => Movement.Position;

    [NonSerialized] public bool IsUpToDate = true;
    [NonSerialized] public bool Removed;
    public bool IsNPC => IsBot || PlayerName != null && PlayerName.StartsWith("Player ");
    public bool IsReadyForAction => actionTimer <= 0f;
    public string Name => PlayerName;
    public bool GivesExperienceWhenKilled => false;
    public bool InCombat { get; set; }
    public float HealthBarOffset => 0f;
    [NonSerialized] public StreamRaidInfo Raider;
    [NonSerialized] public bool UseLongRange;
    [NonSerialized] public bool TrainingRanged;
    [NonSerialized] public bool TrainingMelee;
    [NonSerialized] public bool TrainingAll;
    [NonSerialized] public bool TrainingStrength;
    [NonSerialized] public bool TrainingDefense;
    [NonSerialized] public bool TrainingAttack;
    [NonSerialized] public bool TrainingMagic;
    [NonSerialized] public bool TrainingHealing;
    [NonSerialized] public bool TrainingResourceChangingSkill;

    public PlayerAnimationController Animations => playerAnimations;

    private IslandController _island;
    private float lastHeal;

    [NonSerialized] public bool isDestroyed;

    private bool hasQueuedItemAdd;
    private SphereCollider hitRangeCollider;

    public IslandController Island
    {
        get => _island;
        set
        {
            if (_island && _island != value)
            {
                _island.RemovePlayer(this);
            }
            else if (value && value != _island)
            {
                value.AddPlayer(this);
                //if (!value.AllowRaidWar)
                //IslandHistory.Add(value);
            }
            _island = value;
        }
    }

    public SyntyPlayerAppearance Appearance => playerAppearance ?? (playerAppearance = GetComponent<SyntyPlayerAppearance>());
    //public StreamRaidHandler StreamRaid => streamRaidHandler;
    //public RaidHandler Raid => raidHandler;
    //public ClanHandler Clan => clanHandler;
    //public ArenaHandler Arena => arenaHandler;
    //public DuelHandler Duel => duelHandler;
    //public CombatHandler Combat => combatHandler;
    //public FerryHandler Ferry => ferryHandler;
    //public EffectHandler Effects => effectHandler;
    //public TeleportHandler Teleporter => teleportHandler;
    //public DungeonHandler Dungeon => dungeonHandler;
    //public OnsenHandler Onsen => onsenHandler;

    [NonSerialized] public bool ItemDropEventActive;
    [NonSerialized] public Skill ActiveSkill;
    [NonSerialized] public bool IsDiaperModeEnabled;
    [NonSerialized] public bool IsBot;
    [NonSerialized] public BotPlayerController Bot;

    public bool IsAfk
    {
        get
        {
            var hours = PlayerSettings.Instance.PlayerAfkHours;

            if (hours.HasValue && hours.Value > 0)
            {
                return hours.Value < (DateTime.UtcNow - LastChatCommandUtc).TotalHours;
            }

            return false;
        }
    }

    public TimeSpan TimeSinceLastChatCommandUtc => DateTime.UtcNow - LastChatCommandUtc;

    [NonSerialized] public SkillUpdate LastSailingSaved;
    [NonSerialized] public SkillUpdate LastSlayerSaved;
    [NonSerialized] public CharacterStateUpdate LastSavedState;
    [NonSerialized] public DateTime LastSavedStateTime;

    private ScheduledAction activeScheduledAction;
    private float healTimer;
    private float healDuration;
    private int statusEffectCount;
    private float lastHealTick;
    private float lastUnstuckUsed;
    private bool componentsInitialized;

    private bool hasGameManager;

    internal string FullBodySkinPath;
    private PlayerSessionStats sessionStats = new PlayerSessionStats();
    public ScheduledAction ScheduledAction => activeScheduledAction;

    public PlayerSessionStats SessionStats => sessionStats;
    public DateTime LastDungeonAutoJoinFailUtc { get; internal set; }
    public DateTime LastRaidAutoJoinFailUtc { get; internal set; }
    public GameInventoryItem LastEnchantedItem { get; internal set; }
    public DateTime LastEnchantedItemExpire { get; internal set; }
    public PlayerSkinObject ActiveFullBodySkin { get; private set; }
    public Skill? RaidCombatStyle { get; set; }
    public Skill? DungeonCombatStyle { get; set; }
    public int AutoTrainTargetLevel { get; set; }

    internal void InterruptAction()
    {
        if (activeScheduledAction != null)
        {
            var schedule = activeScheduledAction;

            activeScheduledAction = null;

            schedule.Interrupt();
        }
    }

    internal async void BeginInterruptableAction<TState>(
        TState state,
        Func<TState, Task> action,
        Action<TState> onInterrupt,
        double actionLengthSeconds,
        string description = null,
        object tag = null)
        where TState : class
    {
        InterruptAction();

        if (actionLengthSeconds <= 0)
        {
            // execute immediately without interruption.
            await action(state);
            return;
        }

        // create a timed action
        this.activeScheduledAction = new ScheduledAction<TState>(
            state, action, onInterrupt, actionLengthSeconds, description, tag);
    }

    internal void ClearTarget()
    {
        this.attackTarget = null;
        this.taskTarget = null;
    }

    internal void ToggleDiaperMode()
    {
        IsDiaperModeEnabled = !IsDiaperModeEnabled;
        if (IsDiaperModeEnabled)
        {
            UnequipAllArmor();
        }
        else
        {
            EquipBestItems();
        }
    }

    public void EquipBestItems()
    {
        Inventory.EquipBestItems();
    }

    public void UnequipAllArmor()
    {
        Inventory.UnequipArmor();
    }

    public void UnequipAllItems()
    {
        Inventory.UnequipAll();
    }

    public float GetHitRange()
    {
        if (hitRangeCollider)
        {
            return hitRangeCollider.radius;
        }
        return 0;
    }
    public float GetAttackRange()
    {
        if (TrainingHealing) return HealingRange;
        if (TrainingMagic) return MagicAttackRange;
        if (TrainingRanged) return RangedAttackRange;
        return AttackRange;
    }

    public bool HasTaskArgument(string args) => !string.IsNullOrEmpty(taskArgument) && taskArgument.Equals(args, StringComparison.OrdinalIgnoreCase);
    internal void SetScale(float scale)
    {
        scaleTimer = 300f;
        transform.localScale = Vector3.one * scale;
        TempScale = Vector3.one * scale;
        //foreach (var item in Equipment.EquippedItems)
        //    item.transform.localScale = Vector3.one;
    }

    internal bool TurnIntoMonster(float time)
    {
        if (monsterTimer > 0 || Appearance.FullBodySkinMesh)
        {
            ResetFullBodySkin();
        }

        monsterTimer = time;
        // Pick random monster
        if (availableMonsterMeshes == null || availableMonsterMeshes.Length == 0)
            return false;

        return ApplyPlayerFullBodySkin(availableMonsterMeshes.Random());
    }

    // We could add things like "Turn into werewolf during fullmoon" if you have a specific treat
    // on your charater. And it would limit the player in certain combat related skills. but give boost to different things.

    internal bool ApplyPlayerFullBodySkin(GameObject meshSkinObject)
    {
        if (Appearance.FullBodySkinMesh)
        {
            ResetFullBodySkin();
        }

        Equipment.HideEquipments();
        Appearance.SetFullBodySkinMesh(meshSkinObject);

        var monsterMesh = Appearance.FullBodySkinMesh;
        if (!monsterMesh)
            return false;

        var controller = animator.runtimeAnimatorController;

        if (!monsterMesh.GetComponent<AnimationEventController>())
            monsterMesh.AddComponent<AnimationEventController>();

        animator = monsterMesh.GetComponent<Animator>();
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = controller;
        Animations.SetActiveAnimator(animator);
        return true;
    }

    internal void ApplyPlayerFullBodySkin(PlayerSkinObject skin)
    {
        this.ActiveFullBodySkin = skin;
        this.ApplyPlayerFullBodySkin(ActiveFullBodySkin.SkinMeshObject);
    }

    internal void RemoveFullBodySkin()
    {
        this.ActiveFullBodySkin = null;
        this.ResetFullBodySkin();
    }

    internal void SetRestedState(PlayerRestedUpdate data)
    {
        Rested.CombatStatsBoost = data.StatsBoost;
        Rested.ExpBoost = data.ExpBoost;
        Rested.RestedPercent = data.RestedPercent;
        Rested.RestedTime = data.RestedTime;
    }

    /// <summary>
    /// Called whenever the player is removed from the PlayerManager
    /// </summary>
    public void OnRemoved()
    {
        Removed = true;
        if (healthBarManager) healthBarManager.Remove(this);

        GameManager.NameTags.Remove(this);
    }
    public void UpdateCharacterAppearance()
    {
        Inventory.UpdateAppearance();
    }
    public void BeginItemDropEvent()
    {
        ItemDropEventActive = true;
    }

    public void EndItemDropEvent()
    {
        ItemDropEventActive = false;
        targetDropItem = null;
    }

    void Awake()
    {
        this._transform = this.transform;
        // Initialize handlers that are no longer monobehaviours
        this.dungeonHandler = new DungeonHandler(this, FindObjectOfType<DungeonManager>());
    }

    void Start()
    {
        playerAppearance = GetComponent<SyntyPlayerAppearance>();
        this.hitRangeCollider = GetComponent<SphereCollider>();
        EnsureComponents();

        if (healthBarManager) healthBar = healthBarManager.Add(this);
    }

    public void EnsureComponents()
    {
        if (componentsInitialized) return;
        try
        {
            if (!Movement) Movement = GetComponent<PlayerMovementController>();
            if (!Movement) Movement = gameObject.AddComponent<PlayerMovementController>();
            if (!onsenHandler) onsenHandler = GetComponent<OnsenHandler>();
            if (!clanHandler) clanHandler = GetComponent<ClanHandler>();
            if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
            if (!Inventory) Inventory = GetComponent<Inventory>();
            if (!GameManager) GameManager = FindAnyObjectByType<GameManager>();
            if (!chunkManager) chunkManager = GameManager.Chunks; ;
            if (!healthBarManager) healthBarManager = FindAnyObjectByType<HealthBarManager>();
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
            if (!effectHandler) effectHandler = GetComponent<EffectHandler>();
            if (!teleportHandler) teleportHandler = GetComponent<TeleportHandler>();
            if (!ferryHandler) ferryHandler = GetComponent<FerryHandler>();
            if (!raidHandler) raidHandler = GetComponent<RaidHandler>();
            if (!streamRaidHandler) streamRaidHandler = GetComponent<StreamRaidHandler>();
            if (!arenaHandler) arenaHandler = GetComponent<ArenaHandler>();
            if (!duelHandler) duelHandler = GetComponent<DuelHandler>();
            if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
            if (!playerAppearance) playerAppearance = GetComponent<SyntyPlayerAppearance>();
            if (!this.hitRangeCollider) this.hitRangeCollider = GetComponent<SphereCollider>();
            this.componentsInitialized = true;
        }
        catch { }
    }

    public void LatePoll()
    {
        if (GameCache.IsAwaitingGameRestore || !Overlay.IsGame) return;

        // we would like to avoid doing this every late update as it will be a bit expensive
        // but it cant be helped. Rotate the player to make sure they are standing straight!
        var euler = _transform.rotation.eulerAngles;
        _transform.rotation = Quaternion.Euler(0, euler.y, euler.z);

        //// if the player is not in a raid, dungeon, or arena, we should check if the player are in a "no-go" zone
        //// aka, under the map, or in a place they should not be. If so, we should teleport them to the island spawn position
        //if (!raidHandler.InRaid && !dungeonHandler.InDungeon && !arenaHandler.InArena)
        //{
        //    //if (transform.position.y < -100)
        //    //{
        //    //    teleportHandler.TeleportToIslandSpawn();
        //    //}
        //}
    }

    void OnDrawGizmosSelected()
    {
        if (Movement.Destination != Vector3.zero)
        {
            Gizmos.color = new Color(0, 1, 1, 0.75F);
            Gizmos.DrawLine(transform.position, this.Movement.Destination);
            Gizmos.DrawSphere(Movement.Destination, 0.1f);
        }

        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (agent.pathPending)
            {
                Debug.LogWarning("Path is being processed");
            }

            if (agent.hasPath)
            {
                Debug.LogWarning("Path is ready");
            }

            if (agent.isPathStale)
            {
                Debug.LogWarning("Path is stale");
            }
        }

        if (agent && agent.isActiveAndEnabled && agent.destination != Vector3.zero)
        {
            Gizmos.color = new Color(1, 0, 1, 0.75F);
            Gizmos.DrawSphere(agent.destination, 0.1f);
            Vector3 last = transform.position;
            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(last, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                last = corner;
            }
        }
    }

    public void Poll()
    {
        if ((IsBot && !Overlay.IsGame) || GameCache.IsAwaitingGameRestore)
        {
            return;
        }

        if (!hasBeenInitialized)
        {
            return;
        }

        if (IsBot && Bot)
        {
            Bot.Poll();
        }

        Animations.Poll();
        Movement.Poll();
        onsenHandler.Poll();
        ferryHandler.Poll();
        manualPlayerController.Poll();
        duelHandler.Poll();
        arenaHandler.Poll();
        raidHandler.Poll();
        streamRaidHandler.Poll();

        UpdateActiveEffects();

        var schedule = activeScheduledAction;
        if (schedule != null)
        {
            if (schedule.Interrupted)
            {
                activeScheduledAction = null;
            }

            if (schedule.CanInvoke())
            {
                activeScheduledAction = null;
                schedule.InvokeAsync();
            }
        }

        var deltaTime = GameTime.deltaTime;

        TimeSinceLastTaskChange += deltaTime;

        this.Movement.UpdateIdle(this.ferryHandler.OnFerry);

        //HandleRested();

        if (hasQueuedItemAdd)
        {
            AddQueuedItemAsync();
        }
        if (scaleTimer > 0)
        {
            //foreach (var item in Equipment.EquippedItems)
            //    item.transform.localScale = Vector3.one;

            scaleTimer -= deltaTime;
            if (scaleTimer <= 0)
            {
                transform.localScale = Vector3.one;
                TempScale = Vector3.one;
            }
            else if (scaleTimer < 1)
            {
                transform.localScale = Vector3.Lerp(TempScale, Vector3.one, 1f - scaleTimer);
            }
        }

        if (monsterTimer > 0)
        {
            monsterTimer -= deltaTime;

            HideCombinedMesh();

            if (monsterTimer <= 0)
            {
                // if we have an active full body skin, this is what we should go back to use.
                if (ActiveFullBodySkin != null)
                {
                    ApplyPlayerFullBodySkin(ActiveFullBodySkin);
                }
                else
                {
                    ResetFullBodySkin();
                }
            }
        }


        //if (fullbodyPlayerSkinActive)
        //{
        //    HideCombinedMesh();
        //}

        UpdateHealthRegeneration();

        actionTimer -= deltaTime;

        if (onsenHandler.InOnsen)
            return;

        this.Movement.UpdateMovement();

        if (Controlled)
            return;

        if (streamRaidHandler.InWar) return;
        if (ferryHandler.OnFerry || ferryHandler.Active) return;

        if (dungeonHandler.InDungeon)
        {
            dungeonHandler.Update();
            return;
        }

        if (duelHandler.InDuel) return;
        if (arenaHandler.InArena) return;
        if (raidHandler.InRaid) return;
        if (Stats.IsDead) return;

        if (ItemDropEventActive)
        {
            UpdateDropEvent();
            return;
        }

        if (lateGotoClosestType != null)
        {
            var t = lateGotoClosestType.Value;
            lateGotoClosestType = null;
            GotoClosest(t, lateGotoClosestSilent);
            lateGotoClosestSilent = false;
        }

        if (Chunk != null)
        {
            DoTask();
        }

        //if (currentTask != TaskType.None)
        //{
        //    AddTimeExp();
        //}
    }

    private void UpdateActiveEffects()
    {
        this.playerStatsModifiers.Reset();

        // clearing the effects first will just reset the player state, so if the
        // active effect is still active later then it will be reapplied. Its just a simple way to avoid having to keep track on when to toggle on or off.
        if (statusEffectCount > 0)
        {
            var effects = statusEffects.Values.ToList();
            foreach (var fx in effects)
            {
                if (fx.Expired)
                {
                    RemoveEffect(fx);
                    continue;
                }

                if (UpdateEffect(fx))
                {
                    RemoveEffect(fx);
                }
            }
        }

        playerAnimations.CastSpeedMultiplier = playerStatsModifiers.CastSpeedMultiplier;
        playerAnimations.AttackSpeedMultiplier = playerStatsModifiers.AttackSpeedMultiplier;
        Movement.MovementSpeedMultiplier = playerAnimations.MovementSpeedMultiplier = playerStatsModifiers.MovementSpeedMultiplier;
    }

    private bool UpdateEffect(StatusEffect fx)
    {
        var now = DateTime.UtcNow;
        //var elapsed = now - fx.LastUpdateUtc;
        var timeDuration = fx.Duration;//fx.Effect.ExpiresUtc - fx.Effect.StartUtc;

        fx.LastUpdateUtc = now;

        // set effect, if healing over time then heal if elapsed time is >= 1 second. This is not perfect since it wont always heal the full amount
        // but its better than nothing.
        var effect = fx.Effect;
        try
        {
            switch (effect.Type)
            {
                case StatusEffectType.HealOverTime:
                    var secondsLeft = fx.TimeLeft;
                    healTimer = (float)secondsLeft;
                    healDuration = (float)timeDuration;
                    return RegenerateHealth(ref healTimer, healDuration, effect.Amount);

                case StatusEffectType.Heal:
                    // one time use, heal this player!
                    this.Heal((int)(this.Stats.Health.MaxLevel * effect.Amount));
                    return true;

                case StatusEffectType.IncreasedStrength:
                    this.playerStatsModifiers.StrengthMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedDefense:
                    this.playerStatsModifiers.DefenseMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedAttackPower:
                    this.playerStatsModifiers.AttackPowerMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedMagicPower:
                    this.playerStatsModifiers.MagicPowerMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedHealingPower:
                    this.playerStatsModifiers.HealingPowerMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedRangedPower:
                    this.playerStatsModifiers.RangedPowerMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedExperienceGain:
                    this.playerStatsModifiers.ExpMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedCastSpeed:
                    this.playerStatsModifiers.CastSpeedMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedMovementSpeed:
                    this.playerStatsModifiers.MovementSpeedMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedDodge:
                    this.playerStatsModifiers.DodgeChance += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedHitChance:
                    this.playerStatsModifiers.HitChanceMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreasedAttackSpeed:
                    this.playerStatsModifiers.AttackSpeedMultiplier += effect.Amount;
                    return false;

                case StatusEffectType.IncreaseCriticalHit:
                    this.playerStatsModifiers.CriticalHitChance += effect.Amount;
                    return false;

                case StatusEffectType.AttackAttributePoison:
                    this.playerStatsModifiers.AttackAttributePoisonEffect += effect.Amount;
                    return false;

                case StatusEffectType.AttackAttributeBleeding:
                    this.playerStatsModifiers.AttackAttributeBleedingEffect += effect.Amount;
                    return false;

                case StatusEffectType.AttackAttributeBurning:
                    this.playerStatsModifiers.AttackAttributeBurningEffect += effect.Amount;
                    return false;

                case StatusEffectType.AttackAttributeHealthSteal:
                    this.playerStatsModifiers.AttackAttributeHealthStealEffect += effect.Amount;
                    return false;

                case StatusEffectType.Poison:
                    this.playerStatsModifiers.PoisonEffect += effect.Amount;
                    return false;

                case StatusEffectType.Bleeding:
                    this.playerStatsModifiers.BleedingEffect += effect.Amount;
                    return false;

                case StatusEffectType.Burning:
                    this.playerStatsModifiers.BurningEffect += effect.Amount;
                    return false;

                case StatusEffectType.Damage:
                    this.TakeDamage(null, (int)(this.Stats.Health.MaxLevel * effect.Amount));
                    return true;

                case StatusEffectType.ReducedHitChance:
                    this.playerStatsModifiers.HitChanceMultiplier -= effect.Amount;
                    return false;

                case StatusEffectType.ReducedMovementSpeed:
                    this.playerStatsModifiers.MovementSpeedMultiplier -= effect.Amount;
                    return false;

                case StatusEffectType.ReducedAttackSpeed:
                    this.playerStatsModifiers.AttackSpeedMultiplier -= effect.Amount;
                    return false;

                case StatusEffectType.ReducedCastSpeed:
                    this.playerStatsModifiers.CastSpeedMultiplier -= effect.Amount;
                    return false;
            }
            return true;
        }
        finally
        {
            fx.TimeLeft -= Time.deltaTime;
        }
    }

    private bool RegenerateHealth(ref float healTimer, float healDuration, float effectAmount)
    {
        if (healTimer <= 0)
        {
            return true;
        }

        var fullAmount = this.Stats.Health.MaxLevel * effectAmount;
        var healAmount = 0f;
        if (lastHealTick <= 0)
        {
            lastHealTick = healTimer;
        }

        var finished = false;
        if (healTimer < 1)
        {
            var percentLeft = healTimer / healDuration;
            healAmount = fullAmount * percentLeft;
            finished = true;
        }
        else
        {
            var delta = lastHealTick - healTimer;
            if (delta > 1.5f)
            {
                var percent = delta / healDuration;
                healAmount = fullAmount * percent;
            }
        }

        var amount = Mathf.FloorToInt(healAmount);
        if (amount > 0)
        {
            this.Heal(amount);
            lastHealTick = healTimer;
        }

        return finished;
    }

    private void RemoveEffect(StatusEffect fx)
    {
        // if we have visual effects bound to this, remove it.
        // then delete it from the activeEffects dict
        statusEffects.TryRemove(fx.Effect.Type, out _);
        statusEffectCount = statusEffects.Count;
        lastHealTick = -1;
    }

    private void HideCombinedMesh()
    {
        var visibleMesh = this.Appearance.GetCombinedMesh();
        if (visibleMesh)
            visibleMesh.gameObject.SetActive(false);
    }

    private void ResetFullBodySkin()
    {
        Appearance.DestroyFullBodySkinMesh();
        animator = gameObject.GetComponent<Animator>();
        Animations.ResetActiveAnimator();
    }

    private void UpdateDropEvent()
    {
        if (!ItemDropEventActive)
            return;

        if (targetDropItem && GameManager.DropEvent.Contains(targetDropItem))
        {
            var island = GameManager.Islands.FindIsland(targetDropItem.transform.position);
            if (!island || !Island || island.name != Island.name)
            {
                targetDropItem = null;
                return;
            }

            if (targetDropItem.transform.position == agent.destination)
                return;

            if (!SetDestination(targetDropItem.transform.position))
                targetDropItem = null;

            return;
        }

        var availableDropItems = GameManager.DropEvent.GetDropItems();

        targetDropItem = availableDropItems
            .OrderBy(x => Vector3.Distance(x.transform.position, Movement.Position))
            .FirstOrDefault(x => !pickedItems.Contains(x.ItemId));

        if (targetDropItem == null)
        {
            EndItemDropEvent();
        }
    }

    private void UpdateHealthRegeneration()
    {
        try
        {
            if (isDestroyed || Removed)
            {
                // player removed.
                return;
            }
        }
        catch
        {
            // ignored
            return;
        }

        try
        {
            if ((Chunk?.ChunkType != TaskType.Fighting) && !InCombat)
            {
                regenTimer += GameTime.deltaTime;
            }

            if (regenTimer >= RegenTime)
            {
                var oldValue = Stats.Health.CurrentValue;

                var amount = this.Stats.Health.MaxLevel * RegenRate * GameTime.deltaTime;
                regenAmount += amount;
                var add = Mathf.FloorToInt(regenAmount);
                if (add > 0)
                {
                    var newValue = Mathf.Min(this.Stats.Health.MaxLevel, Stats.Health.CurrentValue + add);
                    Stats.Health.CurrentValue = newValue;
                    regenAmount -= add;
                    if (healthBar && healthBar != null && oldValue != newValue)
                    {
                        healthBar.UpdateHealth();
                    }
                }

                if (Stats.Health.CurrentValue == Stats.Health.MaxLevel)
                {
                    regenTimer = 0;
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    public bool PickupItemById(Guid id)
    {
        if (pickedItems.Add(id))
        {
            PickupItem(GameManager.Items.Get(id));
            EndItemDropEvent();
            return true;
        }
        return false;
    }


    /// <summary>
    ///     Adds an item to the player inventory and updates the server with newly added item.
    /// </summary>
    /// <param name="item">Item to add</param>
    /// <param name="alertInChat">Whether or not player should be alerted in Twitch Chat</param>
    /// <returns></returns>
    public GameInventoryItem PickupItem(Item item, bool alertInChat = true, bool addToServer = true)
    {
        if (item == null) return null;

        var itemInstance = Inventory.AddToBackpack(item);
        if (itemInstance == null) return null;

        if (!IsBot && addToServer)
        {
            EnqueueItemAdd(itemInstance);
        }

        var wasEquipped = EquipIfBetter(itemInstance);
        if (alertInChat && !IsBot)
        {
            GameManager.RavenBot.SendReply(this, wasEquipped
                ? "You found and equipped a {itemName}!"
                : "You found a {itemName}!", item.Name);
        }

        return itemInstance;
    }

    public PlayerState BuildPlayerState()
    {
        var state = new PlayerState();
        state.PlayerId = this.Id;

        state.SyncTime = GameTime.time;
        state.Experience = Stats.GetExperienceList();
        state.Level = Stats.GetLevelList();

        return state;
    }

    public void Cheer() => playerAnimations.ForceCheer();

    public void GotoActiveChunk()
    {
        if (Chunk == null) return;
        SetDestination(Chunk.CenterPointWorld);
    }

    public void GotoStartingArea()
    {
        if (!chunkManager) chunkManager = FindAnyObjectByType<ChunkManager>();
        Chunk = null;
        SetDestination(chunkManager.GetStarterChunk().CenterPointWorld);
    }

    public void GotoClosest(TaskType type, bool silent = false)
    {
        if (raidHandler.InRaid || dungeonHandler.InDungeon)
        {
            return;
        }

        var onFerry = ferryHandler && ferryHandler.Active;
        var hasNotReachedFerryDestination = ferryHandler.Destination && Island != ferryHandler.Destination;

        if (onFerry || Island == null || hasNotReachedFerryDestination)
        {
            LateGotoClosest(type, silent);
            return;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();

        chunkManager = GameManager.Chunks;
        Island = GameManager.Islands.FindPlayerIsland(this);
        Chunk = chunkManager.GetChunkOfType(this, type);

        hasNotReachedFerryDestination = ferryHandler.Destination && Island != ferryHandler.Destination;
        if (Island == null || hasNotReachedFerryDestination)
        {
            LateGotoClosest(type, silent);
            return;
        }

        try
        {
            if (Chunk == null)
            {
                if (IsBot)
                {
                    return;
                }

                ChunkManager.StrictLevelRequirements = false;

                var chunks = chunkManager.GetChunksOfType(Island, type);
                var skill = GetSkill(type) ?? GetActiveSkillStat(); // if its null, then its a combat skill

                // but if its still null, we can't do any extra step checks. Not a valid task to skill
                if (skill != null)
                {
                    foreach (var chunk in chunks)
                    {
                        var reqCombat = chunk.GetRequiredCombatLevel();
                        var reqSkill = chunk.GetRequiredSkillLevel();

                        if ((reqCombat > 1 && Stats.CombatLevel >= reqCombat) || (reqSkill > 1 && skill.Level >= reqSkill))
                        {
                            Chunk = chunk;
                            break;
                        }
                    }
                }

                // still null? well..
                if (Chunk == null)
                {
                    if (silent)
                    {
                        return;
                    }

                    if (chunks.Count > 0)
                    {
                        var lowestReq = chunks.Lowest(x => x.GetRequiredCombatLevel() + x.GetRequiredSkillLevel());//chunks.OrderBy(x => x.GetRequiredCombatLevel() + x.GetRequiredSkillLevel()).FirstOrDefault();
                        var reqCombat = lowestReq.GetRequiredCombatLevel();
                        var reqSkill = lowestReq.GetRequiredSkillLevel();

                        var arg0 = "";
                        var arg1 = "";
                        var msg = "";

                        if (ChunkManager.StrictLevelRequirements && reqCombat > 1 && reqSkill > 1)
                        {
                            msg = Localization.NOT_HIGH_ENOUGH_SKILL_AND_COMBAT;
                            arg0 = reqCombat.ToString();
                            arg1 = reqSkill.ToString();
                        }
                        else if (reqCombat > 1)
                        {
                            if (ChunkManager.StrictLevelRequirements)
                            {
                                msg = Localization.NOT_HIGH_ENOUGH_COMBAT;
                            }
                            else
                            {
                                msg = Localization.NOT_HIGH_ENOUGH_SKILL_OR_COMBAT;
                            }

                            arg0 = type.ToString();
                            arg1 = reqCombat.ToString();

                        }
                        else if (reqSkill > 1)
                        {
                            msg = Localization.NOT_HIGH_ENOUGH_SKILL;
                            arg0 = reqSkill.ToString();
                            arg1 = type.ToString();

                            if (type == TaskType.Fighting)
                            {
                                arg1 = ActiveSkill.ToString();
                            }
                        }

                        GameManager.RavenBot.SendReply(this, msg, arg0, arg1);
                        return;
                    }

                    GameManager.RavenBot.SendReply(this, Localization.CANT_TRAIN_HERE, type.ToString());
#if UNITY_EDITOR
                    Shinobytes.Debug.LogWarning($"{PlayerName}. No suitable chunk found of type '{type}'");
#endif
                    return;
                }
            }

        }
        finally
        {
            playerAnimations.ResetAnimationStates();
            taskTarget = null;
            Movement.Lock();
        }

        SetDestination(Chunk.CenterPointWorld);
    }

    public void SetChunk(TaskType type)
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!chunkManager) chunkManager = FindAnyObjectByType<ChunkManager>();
        if (!chunkManager)
        {
            Shinobytes.Debug.LogError($"No ChunkManager found!");
            return;
        }

        Island = GameManager.Islands.FindPlayerIsland(this);
        Chunk = chunkManager.GetChunkOfType(this, type);
        playerAnimations.ResetAnimationStates();
    }

    internal void RevokeControl()
    {
        this.Controlled = false;
        manualPlayerController.Active = false;
        manualPlayerController.enabled = false;
    }

    internal void EnableControl()
    {
        this.Controlled = true;
        manualPlayerController.enabled = true;
        manualPlayerController.Active = true;
    }

    internal void ToggleControl()
    {
        if (this.Controlled)
        {
            RevokeControl();
        }
        else
        {
            EnableControl();
        }
    }

    private void LateGotoClosest(TaskType type, bool silent = false)
    {
        lateGotoClosestType = type;
        lateGotoClosestSilent = silent;
    }

    private void SetTaskArgument(string taskArg)
    {
        if (taskArg == null || taskArg.Length == 0)
        {
            if (string.IsNullOrEmpty(CurrentTaskName))
                return;

            taskArg = CurrentTaskName;
        }

        taskArgument = taskArg.ToLower();//taskArguments.First();
        taskTarget = null;

        // Try to get the active skill based on the task argument.
        UpdateTrainingFlags();

        // in case we change what we train.
        // we don't want shield armor to be added to magic, ranged or healing.
        Inventory.UpdateEquipmentEffect();

        var skillStat = GetActiveSkillStat();
        if (skillStat != null)
            skillStat.ResetExperiencePerHour();
    }

    internal void UpdateTrainingFlags()
    {
        TrainingRanged = ActiveSkill == Skill.Ranged || HasTaskArgument("ranged");
        TrainingMagic = ActiveSkill == Skill.Magic || HasTaskArgument("magic");
        TrainingHealing = ActiveSkill == Skill.Healing || HasTaskArgument("heal") || HasTaskArgument("healing");
        UseLongRange = TrainingRanged || TrainingMagic || TrainingHealing;
        TrainingAll = ActiveSkill == Skill.Melee || ActiveSkill == Skill.Health || HasTaskArgument("all");
        TrainingStrength = ActiveSkill == Skill.Strength || HasTaskArgument("str");
        TrainingDefense = ActiveSkill == Skill.Defense || HasTaskArgument("def");
        TrainingAttack = ActiveSkill == Skill.Attack || HasTaskArgument("atk") || HasTaskArgument("att");

        TrainingMelee = TrainingAll || TrainingStrength || TrainingDefense || TrainingAttack;

        TrainingResourceChangingSkill =
            ActiveSkill == Skill.Woodcutting || ActiveSkill == Skill.Farming || ActiveSkill == Skill.Crafting ||
            ActiveSkill == Skill.Fishing || ActiveSkill == Skill.Gathering || ActiveSkill == Skill.Cooking || ActiveSkill == Skill.Mining ||
            HasTaskArgument("wood") || HasTaskArgument("farm") || HasTaskArgument("craft")
            || HasTaskArgument("woodcutting") || HasTaskArgument("mining")
            || HasTaskArgument("fishing") || HasTaskArgument("cooking")
            || HasTaskArgument("crafting") || HasTaskArgument("farming");
    }

    public TaskType GetTask() => currentTask;//Chunk?.ChunkType ?? TaskType.None;
    public string GetTaskArgument() => taskArgument;

    internal async Task<GameInventoryItem> CycleEquippedPetAsync()
    {
        var equippedPet = Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        var pets = Inventory.GetInventoryItemsOfType(ItemCategory.Pet, ItemType.Pet);
        if (pets.Count == 0) return null;

        var equippedPetId = equippedPet?.ItemId ?? Guid.Empty;

        var petToEquip = pets
            .Where(x => x.Item.Id != equippedPetId)
            .DistinctBy(x => x.Item.Id)
            .Random();

        if (petToEquip == null)
        {
            var pet = pets.FirstOrDefault();
            Inventory.Equip(pet);
            return pet;
        }

        if (!IsBot)
        {
            await GameManager.RavenNest.Players.EquipInventoryItemAsync(Id, petToEquip.InstanceId);
        }
        Inventory.Equip(petToEquip);
        return petToEquip;
    }

    internal async Task UnequipAllItemsAsync()
    {
        UnequipAllItems();
        if (!IsBot)
        {
            await GameManager.RavenNest.Players.UnequipAllItemsAsync(Id);
        }
    }

    internal async Task UnequipAsync(GameInventoryItem item)
    {
        Inventory.Unequip(item, true);
        Inventory.UpdateEquipmentEffect();

        if (!IsBot)
        {
            await GameManager.RavenNest.Players.UnequipInventoryItemAsync(Id, item.InstanceId);
        }
    }

    internal void Unequip(GameInventoryItem item)
    {
        Inventory.Unequip(item);
    }

    public void Equip(GameInventoryItem item, bool reportShieldWarning = true)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword || thw.Type == ItemType.TwoHandedSpear))
            {
                if (reportShieldWarning)
                {
                    GameManager.RavenBot.SendReply(this, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                }
                return;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedSword)
        {
            var eqShield = Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
            if (eqShield == null)
            {
                var shields = Inventory.GetInventoryItemsOfType(ItemCategory.Armor, ItemType.Shield);
                var shield = shields.OrderByDescending(Inventory.GetItemValue)
                    .FirstOrDefault(Inventory.CanEquipItem);

                if (shield != null)
                {
                    Inventory.Equip(shield);
                }
            }
        }

        var equipped = Inventory.Equip(item);
        if (!equipped)
        {
            if (!item.IsEquippableType)
            {
                GameManager.RavenBot.SendReply(this, "{itemName} can't be equipped.", item.Name);
                return;
            }

            var reqLevels = new List<string>();
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
            if (item.RequiredDefenseLevel > Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
            if (item.RequiredMagicLevel > Stats.Magic.Level || item.RequiredMagicLevel > Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
            if (item.RequiredRangedLevel > Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
            if (item.RequiredSlayerLevel > Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
            if (reqLevels.Count > 0)
            {
                GameManager.RavenBot.SendReply(this, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
            }
            return;
        }
    }

    public void AnnounceLevelToLowToEquip(GameInventoryItem item)
    {

        if (!item.IsEquippableType)
        {
            GameManager.RavenBot.SendReply(this, "{itemName} can't be equipped.", item.Name);
            return;
        }

        var reqLevels = new List<string>();
        var requirement = "You require level ";
        if (item.RequiredAttackLevel > Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
        if (item.RequiredDefenseLevel > Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
        if (item.RequiredMagicLevel > Stats.Magic.Level || item.RequiredMagicLevel > Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
        if (item.RequiredRangedLevel > Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
        if (item.RequiredSlayerLevel > Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
        if (reqLevels.Count > 0)
        {
            GameManager.RavenBot.SendReply(this, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
        }
    }

    internal async Task<bool> EquipAsync(GameInventoryItem item)
    {
        if (IsBot)
        {
            return true;
        }

        if (await GameManager.RavenNest.Players.EquipInventoryItemAsync(Id, item.InstanceId))
        {
            Equip(item);

            return true;
        }

        return false;
    }

    internal async Task<bool> EquipAsync(Item item)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword || thw.Type == ItemType.TwoHandedSpear))
            {
                GameManager.RavenBot.SendReply(this, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                return false;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedSword)
        {
            var eqShield = Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
            if (eqShield == null)
            {
                var shields = Inventory.GetInventoryItemsOfType(ItemCategory.Armor, ItemType.Shield);
                var shield = shields.OrderByDescending(Inventory.GetItemValue)
                    .FirstOrDefault(Inventory.CanEquipItem);

                if (shield != null)
                {
                    Inventory.Equip(shield);
                }
            }
        }

        var equipped = Inventory.EquipByItemId(item.Id);
        if (!equipped)
        {
            var reqLevels = new List<string>();
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
            if (item.RequiredDefenseLevel > Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
            if (item.RequiredMagicLevel > Stats.Magic.Level || item.RequiredMagicLevel > Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
            if (item.RequiredRangedLevel > Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
            if (item.RequiredSlayerLevel > Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
            if (reqLevels.Count > 0)
            {
                GameManager.RavenBot.SendReply(this, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
            }
            return false;
        }

        if (IsBot)
        {
            return true;
        }

        if (await GameManager.RavenNest.Players.EquipItemAsync(Id, item.Id))
        {
            return false;
        }

        return true;
    }

    public void UpdateUser(User user)
    {
        if (user == null)
        {
            return;
        }

        if (user.Id == Guid.Empty)
        {
            user.Id = UserId;
        }

        // you can never not be broadcaster, but if for any reason you were not before, make sure you are now.
        IsBroadcaster = IsBroadcaster || user.IsBroadcaster;
        IsModerator = user.IsModerator;
        IsSubscriber = user.IsSubscriber;
        IsVip = user.IsVip;
        User = user;

        if (!string.IsNullOrEmpty(user.Platform))
        {
            Platform = user.Platform;
        }

        if (!string.IsNullOrEmpty(user.PlatformId))
        {
            PlatformId = user.PlatformId;
        }

        if (Id != Guid.Empty)
        {
            user.CharacterId = Id;
        }
    }

    public void SetPlayer(
        Player player,
        User user,
        StreamRaidInfo raidInfo,
        GameManager gm,
        bool prepareForCamera)
    {
        EnsureComponents();

        if (prepareForCamera)
        {
            LastChatCommandUtc = DateTime.UtcNow;
        }

        gameObject.name = player.Name;

        GameManager = gm;

        if (!GameManager && Overlay.IsOverlay)
        {
            GameManager = FindAnyObjectByType<GameManager>();
        }

        hasGameManager = !!GameManager;

        if (clanHandler && GameManager)
            clanHandler.SetClan(player.Clan, player.ClanRole, GameManager, GameManager.PlayerLogo);

        Definition = player;

        IsGameAdmin = player.IsAdmin;
        IsGameModerator = player.IsModerator;

        Id = player.Id;
        UserId = player.UserId;

        if (user == null)
        {
            user = new User(
                player.UserId,
                player.Id,
                player.UserName,
                player.Name,
                PlayerNameHexColor,
                "ravenfall",
                player.Id.ToString(),
                false, false, false, false,
                player.IsAdmin,
                player.IsModerator,
                player.Identifier);
        }
        if (user.Id == Guid.Empty)
        {
            user.Id = UserId;
        }

        PlatformId = user.PlatformId;
        Platform = user.Platform;

        if ((!string.IsNullOrEmpty(user.PlatformId) && user.PlatformId[0] == '#') || this.IsBot)
        {
            this.IsBot = true;
            this.Bot = this.gameObject.GetComponent<BotPlayerController>() ?? this.gameObject.AddComponent<BotPlayerController>();
            this.Bot.playerController = this;
        }

        Movement.SetAvoidancePriority(UnityEngine.Random.Range(1, 99));

        PatreonTier = player.PatreonTier;
        PlayerName = player.Name;
        PlayerNameLowerCase = player.Name.ToLower();
        Stats = new Skills(player.Skills);
        CharacterIndex = player.CharacterIndex;
        Identifier = player.Identifier;
        Resources = player.Resources;

        Raider = raidInfo;

        UpdateUser(user);

        var joinOnsenAfterInitialize = false;
        var joinFerryAfterInitialize = false;

        ClearTask();

        FullBodySkinPath = player.FullBodySkin;

        //if (Raider != null)
        if (player.State != null && Overlay.IsGame)
        {
            if (player.State.RestedTime > 0)
            {
                Rested.RestedTime = player.State.RestedTime;
                Rested.ExpBoost = 2;
            }

            AutoTrainTargetLevel = player.State.AutoTrainTargetLevel;
            if (player.State.DungeonCombatStyle != null)
            {
                DungeonCombatStyle = (Skill)player.State.DungeonCombatStyle.Value;
            }

            if (player.State.RaidCombatStyle != null)
            {
                RaidCombatStyle = (Skill)player.State.RaidCombatStyle.Value;
            }

            Rested.AutoRestTarget = player.State.AutoRestTarget;
            Rested.AutoRestStart = player.State.AutoRestStart;

            dungeonHandler.AutoJoinCounter = player.State.AutoJoinDungeonCounter;
            raidHandler.AutoJoinCounter = player.State.AutoJoinRaidCounter;

            var setTask = true;
            if (hasGameManager && Overlay.IsGame)
            {
                this.teleportHandler.islandManager = this.GameManager.Islands;
                this.teleportHandler.player = this;

                if (!string.IsNullOrEmpty(player.State.Island) && player.State.X != null)
                {
                    if (player.State.InDungeon)
                    {
                        var targetIsland = GameManager.Islands.Find(player.State.Island);
                        if (targetIsland)
                        {
                            this.teleportHandler.Teleport(targetIsland.SpawnPosition);
                        }
                    }
                    else
                    {
                        var newPosition = new Vector3(
                            (float)player.State.X.Value,
                            (float)player.State.Y.Value,
                            (float)player.State.Z.Value);

                        newPosition = PlacementUtility.FindGroundPoint(newPosition);

                        var targetIsland = GameManager.Islands.FindIsland(newPosition);
                        if (targetIsland)
                        {
                            // check if we are under the water surface with this new position.
                            // if so, teleport to the spawn position instead.
                            if (newPosition.y < -3.5f)
                            {
                                newPosition = targetIsland.SpawnPosition;
                            }
                        }

                        this.teleportHandler.Teleport(newPosition);
                    }
                }

                // could it be? are we on the ferry??
                // ... Let scheck?

                if (string.IsNullOrEmpty(player.State.Island) && (
                    Mathf.Abs((float)player.State.X.GetValueOrDefault()) > 0.01 ||
                    Mathf.Abs((float)player.State.Y.GetValueOrDefault()) > 0.01 ||
                    Mathf.Abs((float)player.State.Z.GetValueOrDefault()) > 0.01))
                {
                    //setTask = false;
                    // most likely on the ferry, bub!
                    joinFerryAfterInitialize = true;
                }
                else
                {
                    Island = GameManager.Islands.FindPlayerIsland(this);

                    if (player.State.InOnsen && onsenHandler)
                    {
                        //setTask = false;
                        joinOnsenAfterInitialize = true;
                        // Attach player to the onsen...
                    }
                }
            }

            if (setTask && !string.IsNullOrEmpty(player.State.Task))
            {
                SetTask(player.State.Task, player.State.TaskArgument ?? player.State.Task);
            }
        }

        if (GameManager && GameManager.NameTags)
            GameManager.NameTags.Add(this);

        Stats.Health.Reset();
        //Inventory.EquipBestItems();
        Equipment.HideEquipments(); // don't show sword on join

        var itemManager = GameManager?.Items;
        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        ApplyStatusEffects(player.StatusEffects);



        this.Appearance.player = this;
        this.Appearance.gameManager = GameManager;
        Appearance.SetAppearance(player.Appearance, () =>
        {
            Inventory.Create(player.InventoryItems);
            hasBeenInitialized = true;
            if (joinOnsenAfterInitialize)
            {
                GameManager.Onsen.Join(this);
            }

            if (joinFerryAfterInitialize)
            {
                Movement.Lock();
                ferryHandler.AddPlayerToFerry();
            }

            // check if the player object has a full body skin assigned or not
            // this will override any item based skins.
            if (!string.IsNullOrEmpty(FullBodySkinPath))
            {
                PlayerSkinObject skin = itemManager.GetSkin(FullBodySkinPath);
                if (skin != null)
                {
                    ApplyPlayerFullBodySkin(skin);
                }
            }
        }, prepareForCamera, false); // Inventory.Create will update the appearance.
    }

    public void SetTask(TaskType task, string arg = null, bool silent = false)
    {
        // if our active scheduled action is to brew|craft|cook etc
        // and we set a new task that is the same as the target action
        // do not interrupt, otherwise interrupt.

        if (activeScheduledAction != null &&
            activeScheduledAction.Tag != null &&
            (TaskType)activeScheduledAction.Tag != task)
        {
            InterruptAction();
        }

        SetTask(task.ToString(), arg, silent);
    }

    public void SetTask(string targetTaskName, string args = null, bool silent = false)
    {
        if (string.IsNullOrEmpty(targetTaskName))
        {
            return;
        }

        targetTaskName = targetTaskName.Trim();
        var t = targetTaskName.ToLower();

        // this needs to be fixed in the bot, but lets allow it for now.
        if (t.StartsWith("brewing"))
        {
            targetTaskName = "Alchemy";
        }

        if (!Enum.TryParse<TaskType>(targetTaskName, true, out var type) || type == TaskType.None)
        {
            var score = int.MaxValue;

            foreach (var tt in Enum.GetValues(typeof(TaskType)).OfType<TaskType>())
            {
                var s = ItemResolver.LevenshteinDistance(targetTaskName.ToLower(), tt.ToString().ToLower());
                if (s < score)
                {
                    score = s;
                    type = tt;
                }
            }

            if (type == TaskType.None || score > 3)
            {
                if (score > 3)
                {
                    if (!silent)
                    {
                        GameManager.RavenBot.SendReply(this, "{skillName} is not a trainable skill. Did you mean {suggestion}?", targetTaskName, type.ToString());
                    }
                }
                return;
            }
        }

        TimeSinceLastTaskChange = 0f;

        if (string.IsNullOrEmpty(args))
        {
            args = targetTaskName.ToLower();
        }

        var skill = Skill.None;
        if (type != TaskType.Fighting)
        {
            skill = SkillUtilities.ParseSkill(targetTaskName);
        }
        else if (!string.IsNullOrEmpty(args))
        {
            skill = SkillUtilities.ParseSkill(args);
        }

        if (Overlay.IsOverlay || duelHandler.InDuel)
        {
            currentTask = type;
            CurrentTaskName = currentTask.ToString();
            SetTaskArgument(args);
            return;
        }

        if (ferryHandler && ferryHandler.Active)
        {
            ferryHandler.BeginDisembark();
        }
        var Game = GameManager;
        if (Game.Arena && Game.Arena.HasJoined(this) && !Game.Arena.Leave(this))
        {
            if (!silent)
            {
                Shinobytes.Debug.Log(PlayerName + " task cannot be done as you're inside the arena.");
            }
            return;
        }

        if (onsenHandler.InOnsen)
        {
            Game.Onsen.Leave(this);
        }

        if (Game.Arena.HasJoined(this))
        {
            Game.Arena.Leave(this);
        }

        var isCombatSkill = skill.IsCombatSkill();

        if (raidHandler.InRaid && !isCombatSkill)
        {
            Game.Raid.Leave(this);
        }

        if (dungeonHandler.InDungeon && !isCombatSkill)
        {
            return;
        }

        if (ActiveSkill.IsCombatSkill() && !isCombatSkill)
        {
            Equipment.HideEquipments();
        }

        if (isCombatSkill)
        {
            if (raidHandler.InRaid)
            {
                RaidCombatStyle = skill;
            }

            if (dungeonHandler.InDungeon)
            {
                DungeonCombatStyle = skill;
            }
        }

        ActiveSkill = skill;
        currentTask = type;
        CurrentTaskName = currentTask.ToString();
        SetTaskArgument(args);

        if (raidHandler.InRaid || dungeonHandler.InDungeon)
        {
            return;
        }

        // training healing is enough if you stay in place.
        if (TrainingHealing)
        {
            SetChunk(type);
        }
        else
        {
            GotoClosest(type, silent);
        }
    }

    public void SetTaskBySkillSilently(Skill skill)
    {
        ActiveSkill = skill;
        currentTask = skill.GetTaskType();
        CurrentTaskName = currentTask.ToString();
        SetTaskArgument(skill.ToString());
    }

    public void SetTaskBySkillSilently(Skill skill, int targetLevel)
    {
        SetTaskBySkillSilently(skill);
        AutoTrainTargetLevel = targetLevel;
    }

    [NonSerialized] public ExpGainState CurrentExpGainState;
    internal bool IsObserved;

    private void SetExpGainState(ExpGainState state)
    {
        if (state != CurrentExpGainState)
        {
            CurrentExpGainState = state;
        }

        if (IsObserved && GameManager.ObservedPlayerDetails)
        {
            GameManager.ObservedPlayerDetails.SetExpGainState(state);
        }
    }

    public void ClearTask()
    {
        // if we are fighting, we have to stop fighting.
        var combatTarget = CombatTarget;
        if (combatTarget != null)
        {
            var enemy = combatTarget as EnemyController;
            if (enemy)
            {
                enemy.RemoveAttacker(this);
            }
        }

        currentTask = TaskType.None;
        CurrentTaskName = null;
        taskArgument = null;//taskArguments.First();
        taskTarget = null;

        ActiveSkill = Skill.None;

        UpdateTrainingFlags();

        if (Inventory)
        {
            // in case we change what we train.
            // we don't want shield armor to be added to magic, ranged or healing.
            Inventory.UpdateEquipmentEffect();
        }
    }

    public bool Fish(FishingController fishingSpot)
    {
        actionTimer = fishingAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowFishingRod();
        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Fishing || currentAnimState != PlayerAnimationState.Fishing)
        {
            lastTrainedSkill = Skill.Fishing;
            playerAnimations.StartFishing();
            return true;
        }

        playerAnimations.Fish();

        LookAt(fishingSpot.LookTransform);

        if (fishingSpot.Fish(this))
        {
            Island.Statistics.FishCaught++;

            AddExp(Skill.Fishing, GetExpFactor(out var state));

            SetExpGainState(state);
            //var amount = fishingSpot.Resource * Mathf.FloorToInt(Stats.Fishing.CurrentValue / 10f);
            //Statistics.TotalFishCollected += (int)amount;
        }

        return true;
    }

    public bool Cook(CraftingStation craftingStation)
    {
        actionTimer = cookingAnimationTime;
        Movement.Lock();
        InCombat = false;

        Equipment.HideEquipments();

        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Cooking || currentAnimState != PlayerAnimationState.Cooking)
        {
            lastTrainedSkill = Skill.Cooking;
            playerAnimations.StartCooking();
            return true;
        }

        LookAt(craftingStation.transform);

        if (craftingStation.Craft(this) && Chunk != null)
        {
            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            if (factor > 0)
            {
                AddExp(Skill.Cooking, factor);//, craftingStation.GetExperience(this));
            }
        }

        return true;
    }

    public bool Brew(CraftingStation craftingStation)
    {
        actionTimer = craftingAnimationTime;
        Movement.Lock();
        InCombat = false;

        //Equipment.ShowHammer();
        Equipment.HideEquipments();
        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Alchemy || currentAnimState != PlayerAnimationState.Brewing)
        {
            lastTrainedSkill = Skill.Alchemy;
            playerAnimations.StartBrewing();
            return true;
        }

        playerAnimations.Brew();
        //playerAnimations.StartBrewing();

        LookAt(craftingStation.transform);
        if (craftingStation.Craft(this) && Chunk != null)
        {
            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            if (factor > 0)
            {
                AddExp(Skill.Alchemy, factor);//, craftingStation.GetExperience(this));
            }
        }

        return true;
    }

    public bool Craft(CraftingStation craftingStation)
    {
        actionTimer = craftingAnimationTime;
        Movement.Lock();
        InCombat = false;

        Equipment.ShowHammer();
        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Crafting || currentAnimState != PlayerAnimationState.Crafting)
        {
            lastTrainedSkill = Skill.Crafting;
            playerAnimations.StartCrafting();
            return true;
        }

        playerAnimations.Craft();

        LookAt(craftingStation.transform);
        if (craftingStation.Craft(this) && Chunk != null)
        {
            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            AddExp(Skill.Crafting, factor);//, craftingStation.GetExperience(this));
        }

        return true;
    }

    public bool Mine(RockController rock)
    {
        actionTimer = mineRockAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowPickAxe();

        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Mining || currentAnimState != PlayerAnimationState.Mining)
        {
            lastTrainedSkill = Skill.Mining;
            playerAnimations.StartMining();
            return true;
        }

        playerAnimations.Mine();

        LookAt(rock.transform);

        if (rock.Mine(this) && Chunk != null)
        {
            Island.Statistics.RocksMined++;

            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            AddExp(Skill.Mining, factor);
            //var amount = rock.Resource * Mathf.FloorToInt(Stats.Mining.CurrentValue / 10f);
            //Statistics.TotalOreCollected += (int)amount;
        }
        return true;
    }
    public bool Farm(FarmController farm)
    {
        actionTimer = rakeAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowRake();
        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Farming || currentAnimState != PlayerAnimationState.Farming)
        {
            lastTrainedSkill = Skill.Farming;
            playerAnimations.StartFarming();
            return true;
        }

        LookAt(farm.transform);

        if (farm.Farm(this) && Chunk != null)
        {
            Island.Statistics.CropsHarvested++;

            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            AddExp(Skill.Farming, factor);

            //var amount = farm.Resource * Mathf.FloorToInt(Stats.Farming.MaxLevel / 10f);
            //Statistics.TotalWheatCollected += amount;
        }

        return true;
    }

    public bool Cut(TreeController tree)
    {
        actionTimer = chompTreeAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowHatchet();

        var currentAnimState = playerAnimations.State;
        if (lastTrainedSkill != Skill.Woodcutting || currentAnimState != PlayerAnimationState.Woodcutting)
        {
            lastTrainedSkill = Skill.Woodcutting;
            playerAnimations.StartWoodcutting();
            return true;
        }

        playerAnimations.Chop(0);

        //StartCoroutine(DamageTree(tree));
        var startTime = Time.time;

        ActionSystem.Run(() => DamageTree(tree, startTime));

        return true;
    }

    public bool Gather(GatherController gather)
    {
        actionTimer = rakeAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.HideEquipments();
        lastTrainedSkill = Skill.Gathering;
        playerAnimations.Gather(gather.PlayKneelingAnimation);

        LookAt(gather.transform);

        var startTime = Time.time;

        ActionSystem.Run(() => Gather(gather, startTime));

        return true;
    }

    public bool Attack(PlayerController player)
    {
        if (player == this)
        {
            Shinobytes.Debug.LogError(player.PlayerName + ", You cant fight yourself :o");
            return false;
        }
        if (player == null || !player)
        {
            return false;
        }

        return AttackEntity(player, true);
    }
    public bool Attack(EnemyController enemy)
    {
        if (enemy == null || !enemy) return false;
        return AttackEntity(enemy);
    }

    public bool Heal(PlayerController target)
    {
        if (target == null || !target) return false;
        return AttackEntity(target);
    }

    private bool AttackEntity(IAttackable target, bool damageOnDraw = false)
    {
        if (target == null)
        {
            return false;
        }

        if (!this || this.isDestroyed)
        {
            return false;
        }
        var targetTransform = target.Transform;
        if (!targetTransform || target.Transform == null)
        {
            return false;
        }

        if (this.Stats.IsDead)
        {
            return true;
        }

        Target = targetTransform;
        var attackType = GetAttackType();
        InCombat = true;
        regenTimer = 0f;
        actionTimer = GetAttackAnimationTime(attackType);

        var hitTime = actionTimer / 2;

        if (TrainingHealing)
        {
            if (Time.time - this.lastHeal < hitTime)
            {
                return true;
            }

            this.lastHeal = Time.time;
        }

        Movement.Lock();

        Equipment.ShowWeapon(attackType);

        var weapon = Inventory.GetEquipmentOfCategory(ItemCategory.Weapon);
        var weaponAnim = TrainingHealing ? 6 : TrainingMagic ? 4 : TrainingRanged ? 3 : weapon?.GetAnimation() ?? 0;
        var attackAnimation = TrainingHealing || TrainingMagic || TrainingRanged ? 0 : weapon?.GetAnimationCount() ?? 4;

        if (!playerAnimations.IsAttacking() || !lastTrainedSkill.IsCombatSkill())
        {
            lastTrainedSkill = ActiveSkill.IsCombatSkill() ? ActiveSkill : Skill.Attack;
            playerAnimations.StartCombat(weaponAnim, Equipment.HasShield);
            if (!damageOnDraw) return true;
        }

        playerAnimations.Attack(weaponAnim, UnityEngine.Random.Range(0, attackAnimation), Equipment.HasShield);

        this._transform.LookAt(Target);

        var startTime = Time.time;

        if (TrainingHealing)
        {
            ActionSystem.Run(() => HealTarget(target, hitTime, startTime));
            return true;
            //StartCoroutine(HealTarget(target, hitTime, startTime));
        }

        ActionSystem.Run(() => DamageEnemy(target, hitTime, startTime));
        //StartCoroutine(DamageEnemy(target, hitTime, startTime));
        return true;
    }

    private float GetAttackAnimationTime(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Healing:
                return healingAnimationTime / playerStatsModifiers.CastSpeedMultiplier;
            case AttackType.Ranged:
                return rangeAnimationTime / playerStatsModifiers.AttackSpeedMultiplier;
            case AttackType.Magic:
                return magicAnimationTime / playerStatsModifiers.CastSpeedMultiplier;
            default:
                return attackAnimationTime / playerStatsModifiers.AttackSpeedMultiplier;
        }
    }

    public AttackType GetAttackType()
    {
        if (TrainingHealing) return AttackType.Healing;
        if (TrainingRanged) return AttackType.Ranged;
        if (TrainingMagic) return AttackType.Magic;
        return AttackType.Melee;
    }

    public bool HealTarget(IAttackable target, float hitTime, float startTime)
    {
        var delta = Time.time - startTime;
        if (delta < hitTime) return false;
        try
        {
            if (target == null || !target.Transform || target.GetStats().IsDead)
                return true;

            var maxHeal = GameMath.MaxHit(Stats.Healing.MaxLevel, EquipmentStats.BaseMagicPower);
            var heal = CalculateDamage(target);
            if (!target.Heal(heal))
                return true;

            sessionStats.AddHealingDealt(heal);

            // allow for some variation in gains based on how high you heal.
            var state = ExpGainState.FullGain;
            var factor = (1 + (heal / maxHeal * 0.2)) *
                ((raidHandler.InRaid || dungeonHandler.InDungeon) ? 1.0 : Chunk?.CalculateExpFactor(this, out state) ?? 1.0);

            SetExpGainState(state);

            if (AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > Stats.Healing.Level)
                AddExp(Skill.Healing, factor);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to heal target: " + exc.Message);
        }
        finally
        {
            InCombat = false;
        }
        return true;
    }

    public bool DamageEnemy(IAttackable enemy, float hitTime, float startTime)
    {
        var delta = Time.time - startTime;
        if (delta < hitTime) return false;
        if (enemy == null) return true;

        if (TrainingRanged)
        {
            this.effectHandler.DestroyProjectile();
        }

        var damage = CalculateDamage(enemy);

        sessionStats.AddDamageDealt(damage);

        if (enemy == null || !enemy.TakeDamage(this, damage))
            return true;

        sessionStats.IncrementEnemiesKilled();
        //Statistics.TotalDamageDone += damage;

        var isPlayer = enemy is PlayerController playerController;
        var enemyController = enemy as EnemyController;

        try
        {
            var isMonster = enemyController != null;
            if (isMonster && Island)
                Island.Statistics.MonstersDefeated++;

            if (!enemy.GivesExperienceWhenKilled)
                return true;

            // give all attackers exp for the kill, not just the one who gives the killing blow.
            foreach (PlayerController player in enemy.GetAttackers())
            {
                if (player == null || !player || player.isDestroyed)
                    continue;

                //var combatExperience = enemy.GetExperience();
                var activeSkill = player.ActiveSkill;
                if (activeSkill.IsCombatSkill())
                {
                    //activeSkill = Skill.Health; // ALL
                    var state = ExpGainState.FullGain;
                    var factor = dungeonHandler.InDungeon ? 1d : Chunk?.CalculateExpFactor(player, out state) ?? 1d;

                    SetExpGainState(state);

                    if (isMonster)
                    {
                        factor *= System.Math.Max(1.0d, enemyController.ExpFactor);
                    }

                    if (player.AutoTrainTargetLevel <= 0 || player.AutoTrainTargetLevel > player.GetSkill(activeSkill).Level)
                        player.AddExp(activeSkill, factor);
                }
            }
        }
        finally
        {
            InCombat = false;
        }
        return true;
    }

    private bool Gather(GatherController gather, float startTime)
    {
        var delta = Time.time - startTime;
        var actionTime = chompTreeAnimationTime / 2f;
        if (delta < actionTime)
            return false;

        if (!gather.Gather(this))
            return false;

        sessionStats.IncrementGather();

        if (Island)
            Island.Statistics.ItemsGathered++;

        foreach (var player in gather.Gatherers)
        {
            if (player == null || !player || player.isDestroyed)
            {
                continue;
            }

            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            if (AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > Stats.Gathering.Level)
                player.AddExp(Skill.Gathering, factor);
        }

        return true;
    }

    public bool DamageTree(TreeController tree, float startTime)
    {
        var delta = Time.time - startTime;
        var actionTime = chompTreeAnimationTime / 2f;
        if (delta < actionTime)
            return false;

        var damage = CalculateDamage(tree);
        if (!tree.DoDamage(this, damage))
            return true;

        sessionStats.IncrementTreeCutDown();

        if (Island)
            Island.Statistics.TreesCutDown++;

        // give all attackers exp for the kill, not just the one who gives the killing blow.
        foreach (var player in tree.WoodCutters)
        {
            if (player == null || !player || player.isDestroyed)
            {
                continue;
            }

            //++player.Statistics.TotalTreesCutDown;

            var factor = Chunk.CalculateExpFactor(this, out var state);

            SetExpGainState(state);

            if (player.AutoTrainTargetLevel <= 0 || player.AutoTrainTargetLevel > player.Stats.Woodcutting.Level)
                player.AddExp(Skill.Woodcutting, factor);// tree.Experience);
            //var amount = (int)(tree.Resource * Mathf.FloorToInt(player.Stats.Woodcutting.CurrentValue / 10f));
            //player.Statistics.TotalWoodCollected += amount;
        }
        return true;
    }

    #region Manage EXP/Resources

    public double GetTierExpMultiplier()
    {
        var tierMulti = TwitchEventManager.TierExpMultis[GameManager.SessionSettings.SubscriberTier];
        var subMulti = (this.IsSubscriber || GameManager.PlayerBoostRequirement > 0) ? tierMulti : 0;
        var multi = subMulti;
        if (PatreonTier > 0)
        {
            var patreonMulti = TwitchEventManager.TierExpMultis[PatreonTier];
            if (patreonMulti > multi)
            {
                return patreonMulti;
            }
        }
        return multi;
    }

    //public double GetExpMultiplier(Skill skill)
    //{
    //    var tierSub = GetTierExpMultiplier();
    //    var multi = tierSub + gameManager.Village.GetExpBonusBySkill(skill);

    //    if (gameManager.Boost.Active)
    //    {
    //        multi += gameManager.Boost.Multiplier;
    //    }

    //    multi = Math.Max(1, multi);
    //    if (Rested.RestedTime > 0 && Rested.ExpBoost > 1)
    //        multi *= (float)Rested.ExpBoost;

    //    return multi * GameMath.ExpScale;
    //}

    public double GetExpMultiplier(Skill skill)
    {
        var tierSub = GetTierExpMultiplier();
        var multi = (float)tierSub;
        var boost = GameManager.Boost;
        if (boost.Active)
            multi += boost.Multiplier;

        multi += GameManager.Village.GetExpBonusBySkill(skill);
        multi = Math.Max(1, multi);
        if (Rested.ExpBoost > 1 && Rested.RestedTime > 0)
        {
            var rexp = (float)Rested.ExpBoost;
            multi = Mathf.Max(rexp * multi, rexp);
        }

        return multi;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetMultiplierFactor() => 1;

    public double GetExpFactor(out ExpGainState state)
    {
        var skill = ActiveSkill;
        state = ExpGainState.FullGain;
        if (skill == Skill.Sailing || skill == Skill.Healing) return 1;
        //if (skill == Skill.Health) return 1d / 3d;
        return Chunk?.CalculateExpFactor(this, out state) ?? 1d;
    }

    public double GetExperience(Skill skill, double factor)
    {
        // check if we are training all. If so, take the avg level + 1
        // we can't take the min level as if you have super high str and low the rest.
        // exp gain shouldnt be super low. Don't want to punish those players
        // we can't take the max level either as it would mean that you can focus leveling on
        // one skill first and then gain the rest super quickly. using avg will still
        // benefit the player but not as much that it can be abused.
        // It will be more beneficial if the player have similar level on each skill (ATK,DEF,STR)
        int nextLevel = skill == Skill.Health
            ? ((int)((GetSkill(Skill.Attack).Level + GetSkill(Skill.Defense).Level + GetSkill(Skill.Strength).Level) / 3f)) + 1
            : GetSkill(skill).Level + 1;

        var xp = GameMath.Exp.CalculateExperience(nextLevel, skill, factor, GetExpMultiplier(skill), GetMultiplierFactor());

        return xp * Mathf.Max(1, playerStatsModifiers.ExpMultiplier);
    }

    public void AddExp(Skill skill, double factor = 1)
    {
        //exp *= GetExpMultiplier(skill);

        var stat = Stats.GetSkill(skill);
        if (stat == null)
            return;

        var exp = GetExperience(skill, factor);

        //if (!isTimeExp && Application.isEditor)
        //{
        //    var expTickSkill = skill.IsCombatSkill() && skill != Skill.Healing ? Skill.Health : skill;
        //    IslandStatisticsUI.Data.ExpTick(this.Island, expTickSkill);
        //}

        if (skill.IsCombatSkill())
        {
            if (Stats.Health.AddExp(exp / 3d, out var hpLevels))
                CelebrateSkillLevelUp(Skill.Health, hpLevels);

            if (skill == Skill.Health)
            {
                var each = exp / 3d;
                var left = 3d;

                if (AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > Stats.Attack.Level)
                {
                    if (Stats.Attack.AddExp(each, out var a))
                        CelebrateSkillLevelUp(Skill.Attack, a);
                }
                else
                {
                    each = exp / --left;
                }

                if (AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > Stats.Defense.Level)
                {
                    if (Stats.Defense.AddExp(each, out var b))
                        CelebrateSkillLevelUp(Skill.Defense, b);
                }
                else
                {
                    each = exp / --left;
                }

                if (AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > Stats.Strength.Level)
                    if (Stats.Strength.AddExp(each, out var c))
                        CelebrateSkillLevelUp(Skill.Strength, c);

                return;
            }
        }

        if (stat.Type == Skill.Slayer || stat.Type == Skill.Sailing || stat.Type == Skill.Health ||
            AutoTrainTargetLevel <= 0 || AutoTrainTargetLevel > stat.Level)
        {
            if (stat.AddExp(exp, out var atkLvls))
            {
                CelebrateSkillLevelUp(skill, atkLvls);
            }
        }
    }

    public void RemoveResource(Resource resource, double amount)
    {
        switch (resource)
        {
            case Resource.Mining:
                Resources.Ore -= amount;
                break;
            case Resource.Woodcutting:
                Resources.Wood -= amount;
                break;
            case Resource.Fishing:
                Resources.Fish -= amount;
                break;
            case Resource.Farming:
                Resources.Wheat -= amount;
                break;
            case Resource.Currency:
                Resources.Coins -= amount;
                break;
        }
    }

    public void AddResource(Resource resource, double amount, bool allowMultiplier = true)
    {
        amount = Math.Max(1, amount);

        switch (resource)
        {
            case Resource.Mining:
                Resources.Ore += amount;
                break;
            case Resource.Woodcutting:
                Resources.Wood += amount;
                break;
            case Resource.Fishing:
                Resources.Fish += amount;
                break;
            case Resource.Farming:
                Resources.Wheat += amount;
                break;
            case Resource.Currency:
                Resources.Coins += amount;
                break;
        }
    }

    //private void AddTimeExp()
    //{
    //    timeExpTimer -= GameTime.deltaTime;
    //    if (timeExpTimer <= 0)
    //    {
    //        if (AddExpToActiveSkillStat(ExpOverTime, true))
    //        {
    //            timeExpTimer = 1f;
    //        }
    //    }
    //}

    /// <summary>
    ///     Adds experience based on the given factor to the currently trained skill.
    /// </summary>
    /// <param name="factor"></param>
    /// <returns></returns>
    public bool AddExp(double factor = 1)
    {
        var skill = ActiveSkill;
        if (skill != Skill.None)
        {
            AddExp(skill, factor);
            return true;
        }
        return false;
    }

    //AsSkill
    private void CelebrateSkillLevelUp(Skill skill, int levelCount)
    {
        CelebrateLevelUp();// skill.ToString(), levelCount);
    }

    private void CelebrateLevelUp()//(string skillName, int levelCount)
    {
        if (playerAnimations)
            playerAnimations.Cheer();
        //gameManager.PlayerLevelUp(this, GetSkill(skillName));
        if (effectHandler)
            effectHandler.LevelUp();
    }

    #endregion

    private void DoTask()
    {
        if (Chunk == null)
        {
            Shinobytes.Debug.LogError("Cannot do task if we do not know in which chunk we are. :o");
            return;
        }

        if (taskTarget != null)
        {
            var taskCompleted = Chunk.IsTaskCompleted(this, taskTarget);
            if (taskCompleted)
            {
                Movement.Unlock();
            }
            else
            {
                if (!Chunk.CanExecuteTask(this, taskTarget, out var reason))
                {
                    if (reason == TaskExecutionStatus.InvalidTarget)
                    {
                        taskTarget = Chunk.GetTaskTarget(this);
                        if (taskTarget == null)
                            return;

                        Chunk.TargetAcquired(this, taskTarget);
                        return;
                    }

                    if (reason == TaskExecutionStatus.OutOfRange)
                    {
                        if (!SetDestination(GetTaskTargetPosition(taskTarget)) && Movement.PathStatus == NavMeshPathStatus.PathInvalid)
                        {
                            // path is invalid, this target is therefor invalid.
                            Chunk.SetTargetInvalid(taskTarget);

                            // turn off local avoidance as well.
                            Movement.DisableLocalAvoidance();

                            taskTarget = null;
                        }
                    }

                    return;
                }

                if (Chunk.ExecuteTask(this, taskTarget))
                {
                    return;
                }
            }
        }

        taskTarget = Chunk.GetTaskTarget(this);
        if (taskTarget == null)
        {
            return;
        }

        Chunk.TargetAcquired(this, taskTarget);

        if (!Chunk.CanExecuteTask(this, taskTarget, out var exeTaskReason))
        {
            if (exeTaskReason == TaskExecutionStatus.OutOfRange)
                SetDestination(GetTaskTargetPosition(taskTarget));

            return;
        }

        Chunk.ExecuteTask(this, taskTarget);
    }

    private Vector3 GetTaskTargetPosition(object obj)
    {
        if (obj is IAttackable attackable)
        {
            return attackable.Position;
        }

        if (obj is Transform t)
        {
            return t.position;
        }

        if (obj is MonoBehaviour b)
        {
            return b.transform.position;
        }

        return Position;
    }

    //private Transform GetTaskTargetTransform(object obj)
    //{
    //    return (obj as IAttackable)?.Transform ?? (obj as Transform) ?? (obj as MonoBehaviour)?.transform;
    //}

    #region Transform Adjustments

    private void LookAt(Transform targetTransform)
    {
        var rotBefore = transform.eulerAngles;
        transform.LookAt(targetTransform);

        var rotAfter = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(rotAfter.x, rotBefore.y, rotAfter.z);
    }

    public bool SetDestination(Vector3 position, bool adjustToNavmesh = false)
    {
        InCombat = duelHandler.InDuel || attackTarget;
        return Movement.SetDestination(position, adjustToNavmesh);
    }

    public void SetPosition(Vector3 position, bool adjustToNavmesh = true)
    {
        this.Movement.SetPosition(position, adjustToNavmesh);
        this.Island = GameManager.Islands.FindPlayerIsland(this);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(string skillName) => Stats.GetSkillByName(skillName);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(Skill skill) => Stats[skill];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(TaskType type)
    {
        switch (type)
        {
            case TaskType.Cooking: return Stats.Cooking;
            case TaskType.Crafting: return Stats.Crafting;
            case TaskType.Farming: return Stats.Farming;
            case TaskType.Gathering: return Stats.Gathering;
            case TaskType.Alchemy: return Stats.Alchemy;
            case TaskType.Fishing: return Stats.Fishing;
            case TaskType.Woodcutting: return Stats.Woodcutting;
            case TaskType.Mining: return Stats.Mining;
        }
        return null;
    }

    internal SkillStat GetActiveSkillStat()
    {
        if (string.IsNullOrEmpty(taskArgument)) return null;
        var skill = ActiveSkill;//GetActiveSkill();
        if (skill == Skill.None) return null;
        return Stats[skill];
    }

    public void ClearAttackers()
    {
        AttackerNames.Clear();
        Attackers.Clear();
    }

    public bool Heal(int amount)
    {
        if (!transform || transform == null)
            return false;

        if (Stats == null || Stats.IsDead)
            return false;

        if (!damageCounterManager)
            damageCounterManager = FindAnyObjectByType<DamageCounterManager>();

        if (damageCounterManager)
            damageCounterManager.Add(transform, amount, true);

        if (Stats.Health != null)
            Stats.Health.Add(amount);

        if (healthBar != null && healthBar) healthBar.UpdateHealth();
        return true;
    }

    public bool TakeDamage(IAttackable attacker, int damage)
    {
        if (Stats.IsDead)
        {
            return false;
        }

        InCombat = true;

        if (attacker != null)
        {
            if (AttackerNames.Add(attacker.Name))
            {
                Attackers.Add(attacker);
            }
        }

        if (!damageCounterManager)
        {
            damageCounterManager = FindAnyObjectByType<DamageCounterManager>();
        }

        if (damageCounterManager)
        {
            damageCounterManager.Add(transform, damage);
        }

        Stats.Health.Add(-damage);
        //if (damage > 0)
        //{
        //    Statistics.TotalDamageTaken += damage;
        //}

        if (healthBar) healthBar.UpdateHealth();

        if (Stats.IsDead)
        {
            Die();
            return true;
        }

        return false;
    }

    public void Die()
    {
        if (!arena) arena = FindAnyObjectByType<ArenaController>();
        if (dungeonHandler.InDungeon) dungeonHandler.Died();
        if (arena) arena.Died(this);
        if (duelHandler.InDuel) duelHandler.Died();
        if (streamRaidHandler) streamRaidHandler.Died();

        InCombat = false;
        Animations.Death();
        Movement.Lock();
        StartCoroutine(Respawn());
    }

    public void OnKicked()
    {
        if (arena) arena.Died(this);
        if (duelHandler.InDuel) duelHandler.Died();
        if (streamRaidHandler) streamRaidHandler.Died();
    }

    public IReadOnlyList<IAttackable> GetAttackers()
    {
        return Attackers;
    }

    public Skills GetStats()
    {
        return Stats;
    }
    public EquipmentStats GetEquipmentStats()
    {
        return EquipmentStats;
    }
    public int GetCombatStyle() => 1;

    public double GetExperience() => 0;

    public bool EquipIfBetter(GameInventoryItem item)
    {
        if (!item.CanBeEquipped())
            return false;

        var currentEquipment = Inventory.GetEquipmentOfType(item.Category, item.Type);
        if (currentEquipment == null)
        {
            Inventory.Equip(item);
            return true;
        }

        if (currentEquipment.GetTotalStats() < item.GetTotalStats())
        {
            // cosmetic ones should not be replaced.
            // maybe we should find a good way to mark an item as cosmetic?
            if (currentEquipment.Category == ItemCategory.Skin || currentEquipment.Category == ItemCategory.Cosmetic || currentEquipment.GetTotalStats() == 0)
            //(currentEquipment.Type == ItemType.Helmet || currentEquipment.Type == ItemType.Hat || currentEquipment.Type == ItemType.Mask))
            {
                return false;
            }

            return Inventory.Equip(item);
        }

        return false;
    }

    private void EnqueueItemAdd(GameInventoryItem itemInstance)
    {
        queuedItemAdd.Enqueue(itemInstance);
        this.hasQueuedItemAdd = true;
        AddQueuedItemAsync();
    }
    private async void AddQueuedItemAsync()
    {
        if (IsBot)
        {
            return;
        }

        if (!GameManager.RavenNest.Tcp.IsReady)
        {
            return;
        }

        if (queuedItemAdd.TryDequeue(out var item))
        {
            try
            {
                var result = await GameManager.RavenNest.Players.AddItemAsync(Id, item);

                if (result == null || result.Result == AddItemResult.Failed)
                {
                    // this needs to give a better response.
                    // empty guid? are we Okay not to retry?
                    // if player is not in the client. Ignore this
                    if (GameManager.Players.Contains(Id))
                    {
                        queuedItemAdd.Enqueue(item);
                        //Shinobytes.Debug.LogError("Item add for user: " + UserId + ", item: " + item + ", result: " + result);
                    }
                }
                else if (result.Result == AddItemResult.Added || result.Result == AddItemResult.AddedAndEquipped)
                {
                    item.InstanceId = result.InstanceId;
                }
                else
                {
                    Shinobytes.Debug.LogError("Failed to add item (" + item.Item.Name + " x " + item.Amount + ") to user with ID '" + UserId + "' (" + Name + "), server returned: " + result.Result + " (" + result.Message + ")");
                }
            }
            catch (Exception exc)
            {
                queuedItemAdd.Enqueue(item);
                Shinobytes.Debug.LogError("PlayerController.AddQueuedItemAsync: " + exc);
            }
        }

        hasQueuedItemAdd = queuedItemAdd.Count > 0;
    }

    public void UpdateEquipmentEffect()
    {
        if (Overlay.IsOverlay && (!Inventory || Inventory.Equipped == null))
        {
            return;
        }

        UpdateEquipmentEffect(Inventory.Equipped);
    }

    public void UpdateEquipmentEffect(IReadOnlyList<GameInventoryItem> equipped)
    {
        EquipmentStats.BaseArmorPower = 0;
        EquipmentStats.BaseWeaponAim = 0;
        EquipmentStats.BaseWeaponPower = 0;
        EquipmentStats.BaseMagicPower = 0;
        EquipmentStats.BaseMagicAim = 0;
        EquipmentStats.BaseRangedPower = 0;
        EquipmentStats.BaseRangedAim = 0;

        EquipmentStats.ArmorPowerBonus = 0;
        EquipmentStats.WeaponAimBonus = 0;
        EquipmentStats.WeaponPowerBonus = 0;
        EquipmentStats.MagicPowerBonus = 0;
        EquipmentStats.MagicAimBonus = 0;
        EquipmentStats.RangedPowerBonus = 0;
        EquipmentStats.RangedAimBonus = 0;

        foreach (var s in Stats.SkillList)
        {
            s.Bonus = 0;
            if (s == this.Stats.Health)
            {
                continue;
            }

            s.CurrentValue = Mathf.FloorToInt(s.Level + (float)s.Bonus);
            s.MaxLevel = Mathf.FloorToInt(s.Level + s.Bonus);
        }

        foreach (var e in equipped)
        {
            // Ignore shield when training ranged or magic.
            // potentially, allow shields for mages. see whats the best option here...

            if ((TrainingHealing || TrainingMagic || TrainingRanged) && e.Item.Type == ItemType.Shield)
                continue;

            var stats = e.GetItemStats();

            EquipmentStats.BaseArmorPower += stats.ArmorPower;
            EquipmentStats.BaseWeaponAim += stats.WeaponAim;
            EquipmentStats.BaseWeaponPower += stats.WeaponPower;

            EquipmentStats.BaseMagicPower += stats.MagicPower;
            EquipmentStats.BaseMagicAim += stats.MagicAim;

            EquipmentStats.BaseRangedPower += stats.RangedPower;
            EquipmentStats.BaseRangedAim += stats.RangedAim;

            EquipmentStats.ArmorPowerBonus += stats.ArmorPower.Bonus;
            EquipmentStats.WeaponAimBonus += stats.WeaponAim.Bonus;
            EquipmentStats.WeaponPowerBonus += stats.WeaponPower.Bonus;

            EquipmentStats.MagicPowerBonus += stats.MagicPower.Bonus;
            EquipmentStats.MagicAimBonus += stats.MagicAim.Bonus;

            EquipmentStats.RangedPowerBonus += stats.RangedPower.Bonus;
            EquipmentStats.RangedAimBonus += stats.RangedAim.Bonus;

            var skillBonus = e.GetSkillBonuses();
            foreach (var sb in skillBonus)
            {
                sb.Skill.Bonus += (float)sb.Bonus;
                if (sb.Skill == this.Stats.Health)
                {
                    continue;
                }
                sb.Skill.CurrentValue = Mathf.FloorToInt(sb.Skill.Level + (float)sb.Skill.Bonus);
                sb.Skill.MaxLevel = Mathf.FloorToInt(sb.Skill.Level + sb.Skill.Bonus);
            }
        }

        if (GameManager && !Overlay.IsOverlay)
        {
            var op = this.GameManager.Camera.Observer.ObservedPlayer;
            if (op && op.Id == Id)
            {
                this.GameManager.Camera.Observer.ForceUpdate();
            }
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        ClearAttackers();

        if (Island && Island.SpawnPositionTransform)
            transform.position = Island.SpawnPosition;
        else
            transform.position = chunkManager.GetStarterChunk().GetPlayerSpawnPoint();

        Movement.ResetTimers();

        transform.rotation = Quaternion.identity;
        playerAnimations.Revive();

        yield return new WaitForSeconds(2f);

        Stats.Health.Reset();

        Movement.Unlock(true);

        if (Chunk != null && Chunk.Island != Island)
            GotoClosest(Chunk.ChunkType);
    }

    private int CalculateDamage(IAttackable enemy)
    {
        if (this == null || enemy == null) return 0;
        if (TrainingHealing)
            return (int)GameMath.CalculateHealing(this, enemy);

        if (TrainingMagic)
            return (int)GameMath.CalculateMagicDamage(this, enemy);

        if (TrainingRanged)
            return (int)GameMath.CalculateRangedDamage(this, enemy);

        return (int)GameMath.CalculateMeleeDamage(this, enemy);
    }

    private int CalculateDamage(TreeController enemy)
    {
        return (int)GameMath.CalculateSkillDamage(Stats.Woodcutting, enemy.Level);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool StartsWith(string str, string arg) => str.IndexOf(arg) == 0;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    internal bool Unstuck(bool forceUnstuck = false)
    {
        var now = Time.realtimeSinceStartup;
        if (!forceUnstuck && now - lastUnstuckUsed < 30)
        {
            return false;
        }

        try
        {
            // if we are in a raid, teleport to the spawnposition of the island the player is on.
            // this could potentially be improved by teleporting to the spawnposition of the raid instead
            if (raidHandler.InRaid)
            {
                this.SetPosition(this.Island.SpawnPosition);
                return true;
            }

            // if we are in a dungeon, teleport to the spawnposition of the dungeon
            if (dungeonHandler.InDungeon)
            {
                this.SetPosition(dungeonHandler.SpawnPosition);
                return true;
            }

            // if we are in a duel, interrupt the duel
            if (duelHandler.InDuel)
            {
                this.duelHandler.Interrupt();
                this.SetPosition(this.Island.SpawnPosition);
                return true;
            }

            // if the player is stuck on the war island, teleport to home
            if (Island && Island.AllowRaidWar && !streamRaidHandler.InWar)
            {
                var homeIsland = GameManager.Islands.All.FirstOrDefault(x => x.Identifier == "home");
                if (homeIsland)
                {
                    SetPosition(homeIsland.SpawnPosition);
                    Island = homeIsland;
                    return true;
                }
            }

            if (!Island || ferryHandler.OnFerry)
            {
                this.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                this.ferryHandler.AddPlayerToFerry();
                return true;
            }

            if (onsenHandler.InOnsen)
            {
                // leave and join onsen
                GameManager.Onsen.Leave(this);
                GameManager.Onsen.Join(this);
                return true;
            }

            //if (string.IsNullOrEmpty(CurrentTaskName) && Stats.CombatLevel > 3)
            //{
            //    SetTask(TaskType.Fighting, "all");
            //}

            // if we are not in an onsen, dungeon and not on the ferry but we are playing a moving animation
            // then we are teleported to the spawnposition of the current island we are on.
            // if no island can be detected, then telepor to home island.
            // (this could be improved to take closest island into consideration)
            if (!onsenHandler.InOnsen)
            {
                var i = Island;
                if (!i) i = this.GameManager.Islands.FindPlayerIsland(this);
                if (!i) i = GameManager.Islands.All.OrderBy(x => Vector3.Distance(x.SpawnPositionTransform.position, this.transform.position)).FirstOrDefault();
                if (!i) i = GameManager.Islands.All.FirstOrDefault(x => x.Identifier == "home");
                this.SetPosition(i.SpawnPosition);
                return true;
            }

            Movement.AdjustPlayerPositionToNavmesh();
            return true;
        }
        finally
        {
            lastUnstuckUsed = Time.realtimeSinceStartup;
        }
    }

    public void Destroy()
    {
        if (!isDestroyed)
        {
            GameObject.Destroy(gameObject);
            isDestroyed = true;
        }
    }

    public int ApplyInstantHealEffect(CharacterStatusEffect effect)
    {
        var healAmount = Mathf.FloorToInt(effect.Amount * this.Stats.Health.MaxLevel);
        this.Heal(healAmount);
        return healAmount;
    }

    public int ApplyInstantDamageEffect(CharacterStatusEffect effect)
    {
        var damage = Mathf.FloorToInt(effect.Amount * this.Stats.Health.MaxLevel);
        this.TakeDamage(null, damage);
        return damage;
    }

    public void ApplyStatusEffects(IReadOnlyList<CharacterStatusEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            return;
        }

        foreach (var fx in effects)
        {
            ApplyStatusEffect(fx);
        }

        UpdateEquipmentEffect();
    }

    public void ApplyStatusEffect(CharacterStatusEffect effect)
    {
        this.statusEffects[effect.Type] = new StatusEffect { Effect = effect };
        statusEffectCount = statusEffects.Count;
    }

    internal IReadOnlyList<StatusEffect> GetStatusEffects()
    {
        return statusEffects.Values.AsList();
    }

    public StatsModifiers GetModifiers() => playerStatsModifiers;

    public void RecordLoot(Item item, int amount, int dungeonIndex, int raidIndex)
    {
        var utcNow = DateTime.UtcNow;
        var gameTime = GameTime.time;
        var record = new PlayerLootRecord
        {
            Time = utcNow,
            ItemName = item.Name,
            Amount = amount,
            DungeonIndex = dungeonIndex,
            RaidIndex = raidIndex,
        };

        Loot.Add(record);
    }
}
