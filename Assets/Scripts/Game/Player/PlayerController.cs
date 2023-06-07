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

public class PlayerController : MonoBehaviour, IAttackable
{
    private readonly HashSet<Guid> pickedItems = new HashSet<Guid>();
    internal readonly HashSet<string> AttackerNames = new HashSet<string>();
    internal readonly List<IAttackable> Attackers = new List<IAttackable>();

    public GameManager GameManager;

    [SerializeField] private ManualPlayerController manualPlayerController;
    [SerializeField] private HashSet<string> taskArguments = new HashSet<string>();
    [NonSerialized] public string taskArgument;
    [SerializeField] private ChunkManager chunkManager;

    [SerializeField] private HealthBarManager healthBarManager;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private RaidHandler raidHandler;
    [SerializeField] private StreamRaidHandler streamRaidHandler;

    [SerializeField] private ClanHandler clanHandler;

    [SerializeField] private DungeonHandler dungeonHandler;
    [SerializeField] private OnsenHandler onsenHandler;

    [SerializeField] private ArenaHandler arenaHandler;
    [SerializeField] private CombatHandler combatHandler;
    [SerializeField] private DuelHandler duelHandler;
    [SerializeField] private FerryHandler ferryHandler;
    [SerializeField] private TeleportHandler teleportHandler;
    [SerializeField] private EffectHandler effectHandler;
    [SerializeField] private PlayerAnimationController playerAnimations;
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

    private SyntyPlayerAppearance playerAppearance;
    private float actionTimer = 0f;
    private Skill lastTrainedSkill = Skill.Attack;

    private ArenaController arena;
    private DamageCounterManager damageCounterManager;

    private TaskType? lateGotoClosestType = null;

    //private Statistics lastSavedStatistics;

    private float outOfResourcesAlertTimer = 0f;


    private float outOfResourcesAlertTime = 60f;
    private int outOfResourcesAlertCounter;

    internal Transform attackTarget;
    internal object taskTarget;

    public Chunk Chunk;

    public Skills Stats = new Skills();

    public Resources Resources = new Resources();
    //public Statistics Statistics = new Statistics();
    public PlayerEquipment Equipment;
    public Inventory Inventory;


    public string PlayerName;
    public string PlayerNameLowerCase;

    public string PlayerNameHexColor = "#FFFFFF";

    public Guid Id;
    public Guid UserId;

    public string Platform;
    public string PlatformId;

    public float AttackRange = 1.8f;
    public float RangedAttackRange = 15F;
    public float MagicAttackRange = 15f;
    public float HealingRange = 15f;
    public int PatreonTier;

    public EquipmentStats EquipmentStats = new EquipmentStats();

    public bool Controlled { get; private set; }

    public bool IsModerator;
    public bool IsBroadcaster;
    public bool IsSubscriber;
    public bool IsVip;

    //public int BitsCheered;
    //public int GiftedSubs;
    //public int TotalBitsCheered;
    //public int TotalGiftedSubs;
    public User User { get; private set; }
    public int CharacterIndex { get; private set; }

    public DateTime LastActivityUtc;

    public bool IsGameAdmin;
    public bool IsGameModerator;

    public float RegenTime = 10f;
    public float RegenRate = 0.1f;

    public Vector3 TempScale = Vector3.one;

    private float regenTimer;
    private float regenAmount;

    private HealthBar healthBar;
    private bool hasBeenInitialized;

    private ItemController targetDropItem;
    private float monsterTimer;
    private float scaleTimer;
    private TaskType currentTask;

    public string CurrentTaskName;

    private readonly ConcurrentQueue<GameInventoryItem> queuedItemAdd = new ConcurrentQueue<GameInventoryItem>();

    public Transform Target
    {
        get => attackTarget ?? (taskTarget as IAttackable)?.Transform ?? (taskTarget as Transform) ?? (taskTarget as MonoBehaviour)?.transform;
        private set => attackTarget = value;
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
    public StreamRaidHandler StreamRaid => streamRaidHandler;
    public RaidHandler Raid => raidHandler;
    public ClanHandler Clan => clanHandler;
    public ArenaHandler Arena => arenaHandler;
    public DuelHandler Duel => duelHandler;
    public CombatHandler Combat => combatHandler;
    public FerryHandler Ferry => ferryHandler;
    public EffectHandler Effects => effectHandler;
    public TeleportHandler Teleporter => teleportHandler;
    public DungeonHandler Dungeon => dungeonHandler;
    public OnsenHandler Onsen => onsenHandler;

    public bool ItemDropEventActive { get; private set; }

    public Skill ActiveSkill;
    //public int CombatType { get; set; }
    //public int SkillType { get; set; }
    public bool IsDiaperModeEnabled { get; private set; }
    public bool IsBot { get; internal set; }
    public BotPlayerController Bot { get; internal set; }
    public bool IsAfk
    {
        get
        {
            var hours = PlayerSettings.Instance.PlayerAfkHours;

            if (hours.HasValue && hours.Value > 0)
            {
                return hours.Value < (DateTime.UtcNow - LastActivityUtc).TotalHours;
            }

            return false;
        }
    }

    public SkillUpdate LastSailingSaved { get; internal set; }
    public SkillUpdate LastSlayerSaved { get; internal set; }

    public CharacterStateUpdate LastSavedState;

    private bool hasGameManager;

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

    public bool HasTaskArgument(string args) => taskArguments.Contains(args);
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

    void Start()
    {
        if (!Movement) Movement = GetComponent<PlayerMovementController>();
        if (!Movement) Movement = gameObject.AddComponent<PlayerMovementController>();
        if (!onsenHandler) onsenHandler = GetComponent<OnsenHandler>();
        if (!clanHandler) clanHandler = GetComponent<ClanHandler>();
        if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
        if (!Inventory) Inventory = GetComponent<Inventory>();
        if (!GameManager) GameManager = FindObjectOfType<GameManager>();
        if (!chunkManager) chunkManager = GameManager.Chunks; ;
        if (!healthBarManager) healthBarManager = FindObjectOfType<HealthBarManager>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
        //if (!rbody) rbody = GetComponent<Rigidbody>();

        if (!effectHandler) effectHandler = GetComponent<EffectHandler>();
        if (!teleportHandler) teleportHandler = GetComponent<TeleportHandler>();
        if (!ferryHandler) ferryHandler = GetComponent<FerryHandler>();
        if (!raidHandler) raidHandler = GetComponent<RaidHandler>();
        if (!streamRaidHandler) streamRaidHandler = GetComponent<StreamRaidHandler>();
        if (!arenaHandler) arenaHandler = GetComponent<ArenaHandler>();
        if (!duelHandler) duelHandler = GetComponent<DuelHandler>();
        if (!combatHandler) combatHandler = GetComponent<CombatHandler>();

        playerAppearance = GetComponent<SyntyPlayerAppearance>();

        if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
        if (healthBarManager) healthBar = healthBarManager.Add(this);

        this.hitRangeCollider = GetComponent<SphereCollider>();
    }

    void LateUpdate()
    {
        if (GameCache.IsAwaitingGameRestore) return;
        if (Overlay.IsGame)
        {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, euler.z);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        if (Movement.LastDestination != Vector3.zero)
        {
            Gizmos.color = new Color(1, 1, 0, 0.75F);
            Gizmos.DrawLine(transform.position, Movement.LastDestination);
            Gizmos.DrawSphere(Movement.LastDestination, 0.1f);
        }
        if (Movement.NextDestination != Vector3.zero)
        {
            Gizmos.color = new Color(0, 1, 1, 0.75F);
            Gizmos.DrawLine(transform.position, this.Movement.NextDestination);
            Gizmos.DrawSphere(Movement.NextDestination, 0.1f);
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

    void Update()
    {
        if ((IsBot && !Overlay.IsGame) || GameCache.IsAwaitingGameRestore)
        {
            return;
        }

        if (!hasBeenInitialized)
        {
            return;
        }

        var deltaTime = GameTime.deltaTime;
        this.Movement.UpdateIdle(this.Ferry.OnFerry);

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
                ResetFullBodySkin();
            }
        }


        //if (fullbodyPlayerSkinActive)
        //{
        //    HideCombinedMesh();
        //}

        UpdateHealthRegeneration();

        actionTimer -= deltaTime;

        if (Onsen.InOnsen)
            return;

        this.Movement.UpdateMovement();

        if (Controlled)
            return;

        if (streamRaidHandler.InWar) return;
        if (ferryHandler.OnFerry || ferryHandler.Active) return;
        if (dungeonHandler.InDungeon) return;
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
            GotoClosest(t);
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

    private void HideCombinedMesh()
    {
        var visibleMesh = this.Appearance.GetCombinedMesh();
        if (visibleMesh)
            visibleMesh.gameObject.SetActive(false);
    }

    //private void HandleRested()
    //{
    //    // this will be updated from the server every 1s. but to get a smoother transition
    //    // we do this locally until we get the server update.
    //    if (!this.Onsen.InOnsen && Rested.RestedTime > 0)
    //    {
    //        Rested.RestedTime -= GameTime.deltaTime;
    //    }
    //}

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
                var amount = this.Stats.Health.MaxLevel * RegenRate * GameTime.deltaTime;
                regenAmount += amount;
                var add = Mathf.FloorToInt(regenAmount);
                if (add > 0)
                {
                    Stats.Health.CurrentValue = Mathf.Min(this.Stats.Health.MaxLevel, Stats.Health.CurrentValue + add);

                    if (healthBar && healthBar != null)
                    {
                        healthBar.UpdateHealth();
                    }

                    regenAmount -= add;
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
    public GameInventoryItem PickupItem(Item item, bool alertInChat = true)
    {
        if (item == null) return null;

        var itemInstance = Inventory.AddToBackpack(item);
        if (itemInstance == null) return null;

        if (!IsBot)
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
        state.SyncTime = GameSystems.time;
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
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();

        Chunk = null;
        SetDestination(chunkManager.GetStarterChunk().CenterPointWorld);
    }

    public void GotoClosest(TaskType type)
    {
        if (Raid.InRaid || Dungeon.InDungeon)
        {
            return;
        }

        var onFerry = Ferry && Ferry.Active;
        var hasNotReachedFerryDestination = Ferry.Destination && Island != Ferry.Destination;

        if (onFerry || Island == null || hasNotReachedFerryDestination)
        {
            LateGotoClosest(type);
            return;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();

        chunkManager = GameManager.Chunks;
        Island = GameManager.Islands.FindPlayerIsland(this);
        Chunk = chunkManager.GetChunkOfType(this, type);

        hasNotReachedFerryDestination = Ferry.Destination && Island != Ferry.Destination;
        if (Island == null || hasNotReachedFerryDestination)
        {
            LateGotoClosest(type);
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

                var chunks = chunkManager.GetChunksOfType(Island, type);
                var skill = GetSkill(type) ?? GetActiveSkillStat(); // if its null, then its a combat skill

                // but if its still null, we can't do any extra step checks. Not a valid task to skill
                if (skill != null)
                {
                    foreach (var chunk in chunks)
                    {
                        var reqCombat = chunk.GetRequiredCombatLevel();
                        var reqSkill = chunk.GetRequiredSkillLevel();

                        if (Stats.CombatLevel >= reqCombat && skill.Level >= reqSkill)
                        {
                            Chunk = chunk;
                            break;
                        }
                    }
                }

                // still null? well..
                if (Chunk == null)
                {
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
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
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

    private void LateGotoClosest(TaskType type) => lateGotoClosestType = type;

    public void SetTaskArguments(string[] taskArgs)
    {
        if (taskArgs == null || taskArgs.Length == 0 || taskArgs[0] == null)
        {
            return;
        }

        taskArguments = new HashSet<string>(taskArgs.Select(x => x.ToLower()));
        taskArgument = taskArgs[0].ToLower();//taskArguments.First();
        taskTarget = null;

        ActiveSkill = SkillUtilities.ParseSkill(taskArgument);

        UpdateTrainingFlags();

        // in case we change what we train.
        // we don't want shield armor to be added to magic, ranged or healing.
        Inventory.UpdateEquipmentEffect();

        var skillStat = GetActiveSkillStat();
        if (skillStat != null)
            skillStat.ResetExpPerHour();
    }

    internal void UpdateTrainingFlags()
    {
        UseLongRange = HasTaskArgument("ranged") || HasTaskArgument("magic");
        TrainingRanged = HasTaskArgument("ranged");
        TrainingMelee = HasTaskArgument("all") || HasTaskArgument("atk") || HasTaskArgument("att") || HasTaskArgument("def") || HasTaskArgument("str");
        TrainingAll = HasTaskArgument("all");
        TrainingStrength = HasTaskArgument("str");
        TrainingDefense = HasTaskArgument("def");
        TrainingAttack = HasTaskArgument("atk") || HasTaskArgument("att");
        TrainingMagic = HasTaskArgument("magic");
        TrainingHealing = HasTaskArgument("heal") || HasTaskArgument("healing");
        TrainingResourceChangingSkill =
            HasTaskArgument("wood") || HasTaskArgument("farm") || HasTaskArgument("craft")
            || HasTaskArgument("woodcutting") || HasTaskArgument("mining")
            || HasTaskArgument("fishing") || HasTaskArgument("cooking")
            || HasTaskArgument("crafting") || HasTaskArgument("farming");
    }

    public TaskType GetTask() => currentTask;//Chunk?.ChunkType ?? TaskType.None;
    public HashSet<string> GetTaskArguments() => taskArguments;

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
        Inventory.Unequip(item);
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
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword))
            {
                if (reportShieldWarning)
                {
                    GameManager.RavenBot.SendReply(this, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                }
                return;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedMace || item.Type == ItemType.OneHandedSword)
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
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > 0) requirement += item.RequiredAttackLevel + " Attack.";
            if (item.RequiredDefenseLevel > 0) requirement += item.RequiredAttackLevel + " Defense.";
            if (item.RequiredMagicLevel > 0) requirement += item.RequiredAttackLevel + " Magic/Healing.";
            if (item.RequiredRangedLevel > 0) requirement += item.RequiredAttackLevel + " Ranged.";
            if (item.RequiredSlayerLevel > 0) requirement += item.RequiredAttackLevel + " Slayer.";
            GameManager.RavenBot.SendReply(this, "You do not meet the requirements to equip " + item.Name + ". " + requirement);
            return;
        }
    }

    internal async Task<bool> EquipAsync(GameInventoryItem item)
    {
        Equip(item);

        if (IsBot)
        {
            return true;
        }

        if (await GameManager.RavenNest.Players.EquipInventoryItemAsync(Id, item.InstanceId))
        {
            return false;
        }

        return true;
    }

    internal async Task<bool> EquipAsync(Item item)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword))
            {
                GameManager.RavenBot.SendReply(this, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                return false;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedMace || item.Type == ItemType.OneHandedSword)
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
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > 0) requirement += item.RequiredAttackLevel + " Attack.";
            if (item.RequiredDefenseLevel > 0) requirement += item.RequiredAttackLevel + " Defense.";
            if (item.RequiredMagicLevel > 0) requirement += item.RequiredAttackLevel + " Magic/Healing.";
            if (item.RequiredRangedLevel > 0) requirement += item.RequiredAttackLevel + " Ranged.";
            if (item.RequiredSlayerLevel > 0) requirement += item.RequiredAttackLevel + " Slayer.";
            GameManager.RavenBot.SendReply(this, "You do not meet the requirements to equip " + item.Name + ". " + requirement);
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
        if (prepareForCamera)
        {
            LastActivityUtc = DateTime.UtcNow;
        }

        gameObject.name = player.Name;

        GameManager = gm;
        hasGameManager = !!GameManager;

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
                false, false, false, false, player.Identifier);
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
        Resources = player.Resources;

        Raider = raidInfo;

        UpdateUser(user);

        var joinOnsenAfterInitialize = false;
        var joinFerryAfterInitialize = false;

        //if (Raider != null)
        if (player.State != null)
        {
            if (player.State.RestedTime > 0)
            {
                Rested.RestedTime = player.State.RestedTime;
                Rested.ExpBoost = 2;
            }

            var setTask = true;
            if (hasGameManager)
            {
                this.Teleporter.islandManager = this.GameManager.Islands;
                this.Teleporter.player = this;

                if (!string.IsNullOrEmpty(player.State.Island) && player.State.X != null)
                {
                    if (player.State.InDungeon)
                    {
                        var targetIsland = GameManager.Islands.Find(player.State.Island);
                        if (targetIsland)
                        {
                            this.Teleporter.Teleport(targetIsland.SpawnPosition);
                        }
                    }
                    else
                    {
                        var newPosition = new Vector3(
                            (float)player.State.X.Value,
                            (float)player.State.Y.Value,
                            (float)player.State.Z.Value);

                        var targetIsland = GameManager.Islands.FindIsland(newPosition);
                        if (targetIsland)
                        {
                            // a little bit of a ugly hack, but will ensure a player does not have to do !unstuck if terrain has been modified.
                            if (newPosition.y < -4.5f)
                            {
                                newPosition = targetIsland.SpawnPosition;
                            }

                            this.Teleporter.Teleport(newPosition);
                        }
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
                SetTask(player.State.Task, new string[] { player.State.TaskArgument ?? player.State.Task });
            }
        }

        if (GameManager)
            GameManager.NameTags.Add(this);

        Stats.Health.Reset();
        //Inventory.EquipBestItems();
        Equipment.HideEquipments(); // don't show sword on join

        var itemManager = GameManager?.Items;
        if (itemManager == null)
        {
            itemManager = FindObjectOfType<ItemManager>();
        }

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
                Ferry.AddPlayerToFerry();
            }
        }, prepareForCamera, false); // Inventory.Create will update the appearance.
    }

    public void SetTask(TaskType task, string[] arg)
    {
        this.currentTask = task;
        this.CurrentTaskName = task.ToString();
        this.SetTaskArguments(arg);
    }

    public void SetTask(string targetTaskName, string[] args)
    {
        if (string.IsNullOrEmpty(targetTaskName))
        {
            return;
        }

        targetTaskName = targetTaskName.Trim();

        if (!Enum.TryParse<TaskType>(targetTaskName, true, out var type) || type == TaskType.None)
        {
            return;
        }

        var taskArgs = args == null || args.Length == 0f
            ? new[] { type.ToString() }
            : args;

        var a = taskArgs[0];
        var skill = SkillUtilities.ParseSkill(a);
        if (Overlay.IsOverlay || Duel.InDuel)
        {
            currentTask = type;
            this.CurrentTaskName = currentTask.ToString();
            SetTaskArguments(taskArgs);
            return;
        }

        if (Ferry && Ferry.Active)
        {
            Ferry.Disembark();
        }
        var Game = GameManager;
        if (Game.Arena && Game.Arena.HasJoined(this) && !Game.Arena.Leave(this))
        {
            Shinobytes.Debug.Log(PlayerName + " task cannot be done as you're inside the arena.");
            return;
        }

        if (Onsen.InOnsen)
        {
            Game.Onsen.Leave(this);
        }

        if (Game.Arena.HasJoined(this))
        {
            Game.Arena.Leave(this);
        }

        var isCombatSkill = skill.IsCombatSkill();

        if (Raid.InRaid && !isCombatSkill)
        {
            Game.Raid.Leave(this);
        }

        if (Dungeon.InDungeon && !isCombatSkill)
        {
            return;
        }

        currentTask = type;
        CurrentTaskName = currentTask.ToString();
        SetTaskArguments(taskArgs);

        if (Raid.InRaid || Dungeon.InDungeon)
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
            GotoClosest(type);
        }
    }

    public bool Fish(FishingController fishingSpot)
    {
        actionTimer = fishingAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowFishingRod();

        if (lastTrainedSkill != Skill.Fishing)
        {
            lastTrainedSkill = Skill.Fishing;
            playerAnimations.StartFishing();
            return true;
        }

        playerAnimations.Fish();

        LookAt(fishingSpot.LookTransform);

        if (fishingSpot.Fish(this))
        {
            AddExp(Skill.Fishing, GetExpFactor());
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

        if (lastTrainedSkill != Skill.Cooking)
        {
            lastTrainedSkill = Skill.Cooking;
            playerAnimations.StartCooking();
            return true;
        }

        LookAt(craftingStation.transform);

        if (craftingStation.Craft(this))
        {
            var factor = Chunk?.CalculateExpFactor(this) ?? 1d;
            AddExp(Skill.Cooking, factor);//, craftingStation.GetExperience(this));
        }

        return true;
    }

    public bool Craft(CraftingStation craftingStation)
    {
        actionTimer = craftingAnimationTime;
        Movement.Lock();
        InCombat = false;

        Equipment.ShowHammer();
        if (lastTrainedSkill != Skill.Crafting)
        {
            lastTrainedSkill = Skill.Crafting;
            playerAnimations.StartCrafting();
            return true;
        }

        playerAnimations.Craft();

        LookAt(craftingStation.transform);
        if (craftingStation.Craft(this))
        {
            var factor = Chunk?.CalculateExpFactor(this) ?? 1d;
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

        if (lastTrainedSkill != Skill.Mining)
        {
            lastTrainedSkill = Skill.Mining;
            playerAnimations.StartMining();
            return true;
        }

        playerAnimations.Mine();

        LookAt(rock.transform);

        if (rock.Mine(this))
        {
            var factor = Chunk?.CalculateExpFactor(this) ?? 1d;
            AddExp(Skill.Mining, factor);
            //var amount = rock.Resource * Mathf.FloorToInt(Stats.Mining.CurrentValue / 10f);
            //Statistics.TotalOreCollected += (int)amount;
        }
        return true;
    }

    public bool Cut(TreeController tree)
    {
        actionTimer = chompTreeAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowHatchet();

        if (lastTrainedSkill != Skill.Woodcutting)
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

    public bool Farm(FarmController farm)
    {
        actionTimer = rakeAnimationTime;
        InCombat = false;
        Movement.Lock();

        Equipment.ShowRake();
        if (lastTrainedSkill != Skill.Farming)
        {
            lastTrainedSkill = Skill.Farming;
            playerAnimations.StartFarming();
            return true;
        }

        LookAt(farm.transform);

        if (farm.Farm(this))
        {
            var factor = Chunk?.CalculateExpFactor(this) ?? 1d;
            AddExp(Skill.Farming, factor);
            var amount = farm.Resource * Mathf.FloorToInt(Stats.Farming.CurrentValue / 10f);
            //Statistics.TotalWheatCollected += amount;
        }

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

        if (!target.Transform || target.Transform == null)
        {
            return false;
        }

        Target = target.Transform;
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

        transform.LookAt(Target.transform);

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
                return healingAnimationTime;
            case AttackType.Ranged:
                return rangeAnimationTime;
            case AttackType.Magic:
                return magicAnimationTime;
            default:
                return attackAnimationTime;
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

            var maxHeal = GameMath.MaxHit(Stats.Healing.CurrentValue, EquipmentStats.BaseMagicPower);
            var heal = CalculateDamage(target);
            if (!target.Heal(this, heal))
                return true;

            // allow for some variation in gains based on how high you heal.

            var factor = (1 + (heal / maxHeal * 0.2)) *
                ((Raid.InRaid || Dungeon.InDungeon) ? 1.0 : Chunk?.CalculateExpFactor(this) ?? 1.0);
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
    //public IEnumerator HealTarget(IAttackable target, float hitTime)
    //{
    //    yield return new WaitForSeconds(hitTime);
    //    try
    //    {
    //        if (target == null || !target.Transform || target.GetStats().IsDead) yield break;

    //        var maxHeal = GameMath.MaxHit(Stats.Healing.CurrentValue, EquipmentStats.MagicPower);
    //        var heal = CalculateDamage(target);
    //        if (!target.Heal(this, heal))
    //            yield break;

    //        // allow for some variation in gains based on how high you heal.

    //        var factor = (1 + (heal / maxHeal * 0.2)) *
    //            ((Raid.InRaid || Dungeon.InDungeon) ? 1.0 : Chunk?.CalculateExpFactor(this) ?? 1.0);
    //        AddExp(Skill.Healing, factor);
    //    }
    //    catch (Exception exc)
    //    {
    //        Shinobytes.Debug.LogError("Unable to heal target: " + exc.Message);
    //    }
    //    finally
    //    {
    //        InCombat = false;
    //    }
    //}

    public bool DamageEnemy(IAttackable enemy, float hitTime, float startTime)
    {
        var delta = Time.time - startTime;
        if (delta < hitTime) return false;
        if (enemy == null) return true;

        if (TrainingRanged)
        {
            this.Effects.DestroyProjectile();
        }

        var damage = CalculateDamage(enemy);
        if (enemy == null || !enemy.TakeDamage(this, damage))
            return true;

        //Statistics.TotalDamageDone += damage;

        var isPlayer = enemy is PlayerController playerController;
        var enemyController = enemy as EnemyController;
        try
        {
            if (!enemy.GivesExperienceWhenKilled)
                return true;

            // give all attackers exp for the kill, not just the one who gives the killing blow.
            foreach (PlayerController player in enemy.GetAttackers())
            {
                if (player == null || !player || player.isDestroyed)
                    continue;

                //if (isPlayer)
                //{
                //    ++player.Statistics.PlayersKilled;
                //}
                //else
                //{
                //    ++player.Statistics.EnemiesKilled;
                //}

                //var combatExperience = enemy.GetExperience();
                var activeSkill = player.ActiveSkill;
                if (activeSkill.IsCombatSkill())
                {
                    //activeSkill = Skill.Health; // ALL
                    var factor = Dungeon.InDungeon ? 1d : Chunk?.CalculateExpFactor(player) ?? 1d;

                    if (enemyController != null)
                    {
                        factor *= System.Math.Max(1.0d, enemyController.ExpFactor);
                    }

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

    //public IEnumerator DamageEnemy(IAttackable enemy, float hitTime)
    //{
    //    yield return new WaitForSeconds(hitTime);
    //    if (enemy == null)
    //        yield break;

    //    if (TrainingRanged)
    //    {
    //        this.Effects.DestroyProjectile();
    //    }

    //    var damage = CalculateDamage(enemy);
    //    if (enemy == null || !enemy.TakeDamage(this, damage))
    //        yield break;

    //    Statistics.TotalDamageDone += damage;

    //    var isPlayer = enemy is PlayerController playerController;
    //    var enemyController = enemy as EnemyController;
    //    try
    //    {
    //        if (!enemy.GivesExperienceWhenKilled)
    //            yield break;

    //        // give all attackers exp for the kill, not just the one who gives the killing blow.

    //        foreach (PlayerController player in enemy.GetAttackers())
    //        {
    //            if (player == null || !player || player.isDestroyed)
    //            {
    //                continue;
    //            }

    //            if (isPlayer)
    //            {
    //                ++player.Statistics.PlayersKilled;
    //            }
    //            else
    //            {
    //                ++player.Statistics.EnemiesKilled;
    //            }

    //            //var combatExperience = enemy.GetExperience();
    //            var activeSkill = player.ActiveSkill;
    //            if (activeSkill.IsCombatSkill())
    //            {
    //                //activeSkill = Skill.Health; // ALL
    //                var factor = Dungeon.InDungeon ? 1d : Chunk?.CalculateExpFactor(player) ?? 1d;

    //                if (enemyController != null)
    //                {
    //                    factor *= System.Math.Max(1.0d, enemyController.ExpFactor);
    //                }

    //                player.AddExp(activeSkill, factor);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        InCombat = false;
    //    }
    //}

    public bool DamageTree(TreeController tree, float startTime)
    {
        var delta = Time.time - startTime;
        var actionTime = chompTreeAnimationTime / 2f;
        if (delta < actionTime)
            return false;

        var damage = CalculateDamage(tree);
        if (!tree.DoDamage(this, damage))
            return true;

        // give all attackers exp for the kill, not just the one who gives the killing blow.
        foreach (var player in tree.WoodCutters)
        {
            if (player == null || !player || player.isDestroyed)
            {
                continue;
            }

            //++player.Statistics.TotalTreesCutDown;

            var factor = Chunk?.CalculateExpFactor(player) ?? 1d;
            player.AddExp(Skill.Woodcutting, factor);// tree.Experience);
            //var amount = (int)(tree.Resource * Mathf.FloorToInt(player.Stats.Woodcutting.CurrentValue / 10f));
            //player.Statistics.TotalWoodCollected += amount;
        }
        return true;
    }

    public IEnumerator DamageTree(TreeController tree)
    {
        yield return new WaitForSeconds(chompTreeAnimationTime / 2f);

        var damage = CalculateDamage(tree);
        if (!tree.DoDamage(this, damage)) yield break;
        // give all attackers exp for the kill, not just the one who gives the killing blow.
        foreach (var player in tree.WoodCutters)
        {
            if (player == null || !player || player.isDestroyed)
            {
                continue;
            }

            //++player.Statistics.TotalTreesCutDown;

            var factor = Chunk?.CalculateExpFactor(player) ?? 1d;
            player.AddExp(Skill.Woodcutting, factor);// tree.Experience);
            //var amount = (int)(tree.Resource * Mathf.FloorToInt(player.Stats.Woodcutting.CurrentValue / 10f));
            //player.Statistics.TotalWoodCollected += amount;
        }
    }

    #region Manage EXP/Resources

    public double GetTierExpMultiplier()
    {
        var tierMulti = TwitchEventManager.TierExpMultis[GameManager.Permissions.SubscriberTier];
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

    public double GetExpFactor()
    {
        var skill = ActiveSkill;
        if (skill == Skill.Sailing || skill == Skill.Healing) return 1;
        //if (skill == Skill.Health) return 1d / 3d;
        return Chunk?.CalculateExpFactor(this) ?? 1d;
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

        return GameMath.Exp.CalculateExperience(nextLevel, skill, factor, GetExpMultiplier(skill), GetMultiplierFactor());
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
                if (Stats.Attack.AddExp(each, out var a))
                    CelebrateSkillLevelUp(Skill.Attack, a);

                if (Stats.Defense.AddExp(each, out var b))
                    CelebrateSkillLevelUp(Skill.Defense, b);

                if (Stats.Strength.AddExp(each, out var c))
                    CelebrateSkillLevelUp(Skill.Strength, c);

                return;
            }
        }

        if (stat.AddExp(exp, out var atkLvls))
        {
            CelebrateSkillLevelUp(skill, atkLvls);
        }
    }

    public void RemoveResources(RavenNest.Models.Item item)
    {
        if (item.WoodCost > 0)
            RemoveResource(Resource.Woodcutting, item.WoodCost);

        if (item.OreCost > 0)
            RemoveResource(Resource.Mining, item.OreCost);
    }

    public void RemoveResources(RavenNest.Models.Resources item)
    {
        if (item.Wood > 0)
            RemoveResource(Resource.Woodcutting, item.Wood);

        if (item.Fish > 0)
            RemoveResource(Resource.Fishing, item.Fish);

        if (item.Wheat > 0)
            RemoveResource(Resource.Farming, item.Wheat);

        if (item.Ore > 0)
            RemoveResource(Resource.Mining, item.Ore);
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
        if (Effects)
            Effects.LevelUp();
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
                        SetDestination(GetTaskTargetPosition(taskTarget));

                    if (reason == TaskExecutionStatus.InsufficientResources)
                    {
                        outOfResourcesAlertTimer -= GameTime.deltaTime;
                        if (outOfResourcesAlertTimer <= 0f)
                        {
                            Shinobytes.Debug.LogWarning(PlayerName + " is out of resources and won't gain any crafting exp.");

                            var message = lastTrainedSkill == Skill.Cooking
                                ? "You're out of resources, you wont gain any cooking exp. Use !train farming or !train fishing to get some resources."
                                : "You're out of resources, you wont gain any crafting exp. Use !train woodcutting or !train mining to get some resources.";

                            GameManager.RavenBot.SendReply(this, message);
                            outOfResourcesAlertTimer = ++outOfResourcesAlertCounter > 3 ? outOfResourcesAlertTime * 10F : outOfResourcesAlertTime;
                        }
                    }
                    return;
                }

                if (Chunk.ExecuteTask(this, taskTarget))
                {
                    outOfResourcesAlertCounter = 0;
                    outOfResourcesAlertTimer = 0f;
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

        if (Chunk.ExecuteTask(this, taskTarget))
        {
            outOfResourcesAlertCounter = 0;
            outOfResourcesAlertTimer = 0f;
            return;
        }
    }

    private Vector3 GetTaskTargetPosition(object obj)
    {
        if (obj is IAttackable attackable)
        {
            return Movement.JitterTranslate(attackable.Position);
        }
        return Movement.JitterTranslateSlow(obj);
    }

    //private Transform GetTaskTargetTransform(object obj)
    //{
    //    return (obj as IAttackable)?.Transform ?? (obj as Transform) ?? (obj as MonoBehaviour)?.transform;
    //}

    #region Transform Adjustments

    private void LookAt(Transform targetTransform)
    {
        var rot = transform.rotation;
        transform.LookAt(targetTransform);
        transform.rotation = new Quaternion(rot.x, transform.rotation.y, rot.z, rot.w);
    }

    public bool SetDestination(Vector3 position)
    {
        InCombat = Duel.InDuel;
        Movement.SetDestination(position);
        return true;
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
            case TaskType.Fishing: return Stats.Fishing;
            case TaskType.Woodcutting: return Stats.Woodcutting;
            case TaskType.Mining: return Stats.Mining;
        }
        return null;
    }

    internal SkillStat GetActiveSkillStat()
    {
        var skill = ActiveSkill;//GetActiveSkill();
        if (skill == Skill.None) return null;
        return Stats[skill];
    }

    public void ClearAttackers()
    {
        AttackerNames.Clear();
        Attackers.Clear();
    }

    public bool Heal(IAttackable healer, int amount)
    {
        if (!transform || transform == null)
            return false;

        if (Stats == null || Stats.IsDead)
            return false;

        if (!damageCounterManager)
            damageCounterManager = FindObjectOfType<DamageCounterManager>();

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
            damageCounterManager = FindObjectOfType<DamageCounterManager>();
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
        if (!arena) arena = FindObjectOfType<ArenaController>();
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
        if (item.Category == ItemCategory.Resource || item.Category == ItemCategory.Food || item.Category == ItemCategory.Scroll)
            return false;

        if (item.RequiredDefenseLevel > Stats.Defense.Level) return false;
        if (item.RequiredAttackLevel > Stats.Attack.Level) return false;
        if (item.RequiredSlayerLevel > Stats.Slayer.Level) return false;
        if (item.RequiredMagicLevel > Stats.Magic.Level && item.RequiredMagicLevel > Stats.Healing.Level) return false;
        if (item.RequiredRangedLevel > Stats.Ranged.Level) return false;

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
            if (currentEquipment.Type == ItemType.Helmet && currentEquipment.GetTotalStats() == 0)
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
                Shinobytes.Debug.LogError(exc);
            }
        }

        hasQueuedItemAdd = queuedItemAdd.Count > 0;
    }

    public void UpdateEquipmentEffect(List<GameInventoryItem> equipped)
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
            }
        }

        if (GameManager)
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

        Movement.Unlock();

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

    internal void Unstuck()
    {
        if (Raid.InRaid)
        {
            this.SetPosition(this.Island.SpawnPosition);
            return;
        }

        if (Dungeon.InDungeon)
        {
            this.SetPosition(Dungeon.SpawnPosition);
            return;
        }

        if (Duel.InDuel)
        {
            this.Duel.Interrupt();
            return;
        }

        if (Island && Island.AllowRaidWar && !StreamRaid.InWar)
        {
            var homeIsland = GameManager.Islands.All.FirstOrDefault(x => x.Identifier == "home");
            if (homeIsland)
            {
                SetPosition(homeIsland.SpawnPosition);
                Island = homeIsland;
                return;
            }
        }

        if (!Onsen.InOnsen && !Dungeon.InDungeon && Animations.IsMoving && !Ferry.OnFerry)
        {
            var i = Island;
            if (!i) i = GameManager.Islands.All.FirstOrDefault(x => x.Identifier == "home");
            this.SetPosition(i.SpawnPosition);
            return;
        }

        if ((Island && this.GameManager.Islands.FindPlayerIsland(this) != Island) ||
            Island && agent && agent.isActiveAndEnabled && agent.isOnNavMesh && Movement.IdleTime > 2f && !Movement.IsMoving && Animations.IsMoving)
        {
            this.SetPosition(Island.SpawnPosition);
            return;
        }

        Movement.AdjustPlayerPositionToNavmesh();
    }
    public void Destroy()
    {
        if (!isDestroyed)
        {
            GameObject.Destroy(gameObject);
            isDestroyed = true;
        }
    }
}

public class CharacterRestedState
{
    public double ExpBoost;
    public double RestedPercent;
    public double RestedTime;
    public double CombatStatsBoost;
}

public class AsyncPlayerRequest
{
    private readonly Task request;
    private readonly Func<Task> lazyRequest;
    public AsyncPlayerRequest(Func<Task> action)
    {
        lazyRequest = action;
    }
    public AsyncPlayerRequest(Task request)
    {
        this.request = request;
    }

    public AsyncPlayerRequest(Action request)
    {
        this.request = new Task(request);
    }

    public Task Invoke()
    {
        if (lazyRequest != null)
        {
            return lazyRequest.Invoke();
        }

        return request;
    }
}
