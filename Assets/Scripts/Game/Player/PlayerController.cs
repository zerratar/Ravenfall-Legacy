using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Assets.Scripts;
using RavenNest.Models;
using RavenNest.SDK;
using UnityEngine;
using UnityEngine.AI;
using Resources = RavenNest.Models.Resources;
public class PlayerController : MonoBehaviour, IAttackable
{
    private static readonly ConcurrentDictionary<string, int> combatTypeArgLookup
        = new ConcurrentDictionary<string, int>();

    private static readonly ConcurrentDictionary<string, int> skillTypeArgLookup
        = new ConcurrentDictionary<string, int>();

    private static readonly ConcurrentDictionary<string, Skill> skillLookup
        = new ConcurrentDictionary<string, Skill>();

    private readonly HashSet<Guid> pickedItems = new HashSet<Guid>();

    internal readonly HashSet<string> AttackerNames = new HashSet<string>();
    internal readonly List<IAttackable> Attackers = new List<IAttackable>();

    [SerializeField] private ManualPlayerController manualPlayerController;
    [SerializeField] private HashSet<string> taskArguments = new HashSet<string>();
    [SerializeField] private string taskArgument;

    [SerializeField] private GameManager gameManager;
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

    private IPlayerAppearance playerAppearance;
    private float actionTimer = 0f;
    private float timeExpTimer = 1f;
    private Skill lastTrainedSkill = Skill.Attack;

    private ArenaController arena;
    private DamageCounterManager damageCounterManager;

    private TaskType? lateGotoClosestType = null;

    private double ExpOverTime;
    private double lastSavedExperienceTotal;

    private Statistics lastSavedStatistics;

    private float outOfResourcesAlertTimer = 0f;
    private float outOfResourcesAlertTime = 60f;
    private int outOfResourcesAlertCounter;

    internal Transform attackTarget;
    internal object taskTarget;

    public IChunk Chunk;

    public Skills Stats = new Skills();

    public Resources Resources = new Resources();
    public Statistics Statistics = new Statistics();
    public PlayerEquipment Equipment;
    public Inventory Inventory;


    public string PlayerName;
    public string PlayerNameLowerCase;

    public string PlayerNameHexColor = "#FFFFFF";

    public string UserId;
    public Guid Id;
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

    public int BitsCheered;
    public int GiftedSubs;
    public int TotalBitsCheered;
    public int TotalGiftedSubs;
    public TwitchPlayerInfo TwitchUser { get; private set; }
    public int CharacterIndex { get; private set; }

    public bool IsGameAdmin;
    public bool IsGameModerator;

    public float RegenTime = 10f;
    public float RegenRate = 0.1f;

    public Vector3 TempScale = Vector3.one;

    private float regenTimer;
    private float regenAmount;

    private Vector3 lastPosition;
    private HealthBar healthBar;
    private bool hasBeenInitialized;

    private ItemController targetDropItem;
    private Vector3 NextDestination;
    private float monsterTimer;
    private float scaleTimer;
    private float loyaltyUpdateTimer = -1f;

    private readonly ConcurrentQueue<Guid> queuedItemAdd = new ConcurrentQueue<Guid>();

    public Transform Target
    {
        get => attackTarget ?? (taskTarget as IAttackable)?.Transform ?? (taskTarget as Transform) ?? (taskTarget as MonoBehaviour)?.transform;
        private set => attackTarget = value;
    }

    public GameManager Game => gameManager;
    public float IdleTime { get; private set; }
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

    internal Vector3 PositionInternal;
    public Vector3 Position => PositionInternal;
    public bool IsMoving => MovementTime > 0.05 || (agent.isActiveAndEnabled && agent.velocity.magnitude >= 0.01);
    public bool IsUpToDate { get; private set; } = true;
    public bool Removed { get; set; }
    public bool IsNPC => PlayerName != null && PlayerName.StartsWith("Player ");
    public bool IsReadyForAction => actionTimer <= 0f;
    public string Name => PlayerName;
    public bool GivesExperienceWhenKilled => false;
    public bool InCombat { get; set; }
    public float HealthBarOffset => 0f;
    public StreamRaidInfo Raider { get; private set; }
    public bool UseLongRange { get; private set; }
    public bool TrainingRanged { get; private set; }
    public bool TrainingMelee { get; private set; }
    public bool TrainingAll { get; private set; }
    public bool TrainingStrength { get; private set; }
    public bool TrainingDefense { get; private set; }
    public bool TrainingAttack { get; private set; }
    public bool TrainingMagic { get; private set; }
    public bool TrainingHealing { get; private set; }
    public bool TrainingResourceChangingSkill { get; private set; }

    public PlayerAnimationController Animations => playerAnimations;

    private IslandController _island;
    private float lastHeal;
    private bool isDestroyed;
    private CombatSkill lastTrainedCombatSkill;
    private bool hasQueuedItemAdd;
    private SphereCollider hitRangeCollider;
    private float MovementTime;
    private Vector3 LastDestination;
    private bool movementIsLocked;
    private Vector3 positionJitter;

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
    public IPlayerAppearance Appearance => playerAppearance
        ?? (playerAppearance = GetComponent<SyntyPlayerAppearance>());
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

    public int CombatType { get; set; }
    public int SkillType { get; set; }
    public bool IsDiaperModeEnabled { get; private set; }
    public bool IsBot { get; internal set; }
    public BotPlayerController Bot { get; internal set; }

    public readonly ConcurrentQueue<AsyncPlayerRequest> RequestQueue = new ConcurrentQueue<AsyncPlayerRequest>();

    private long createdRequests = 0;
    public void UpdateRequestQueue()
    {
        // every 10th request is a "save everything"
        if (createdRequests % 10 == 0)
        {
            EnqueueRequest(() => gameManager.RavenNest.SavePlayerAsync(this));
        }
        //// every 3rd is a state update
        //if (createdRequests % 3 == 0)
        //{
        //    EnqueueRequest(async () => await gameManager.RavenNest.SavePlayerStateAsync(this));
        //}
        // otherwise, save active skill
        else
        {
            EnqueueRequest(() => gameManager.RavenNest.SaveTrainingSkill(this));
        }
    }
    public void EnqueueRequest(Func<Task> action)
    {
        RequestQueue.Enqueue(new AsyncPlayerRequest(action));
        ++createdRequests;
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

    private void EquipBestItems()
    {
        Inventory.EquipBestItems();
    }

    private void UnequipAllArmor()
    {
        Inventory.UnequipArmor();
    }

    private void UnequipAllItems()
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

    internal void AddBitCheer(int bits)
    {
        this.BitsCheered += bits;
        this.TotalBitsCheered += bits;
        gameManager.RavenNest.SendPlayerLoyaltyData(this);
    }

    internal void AddSubscribe(bool gifted)
    {
        if (gifted)
        {
            this.GiftedSubs++;
            this.TotalGiftedSubs++;
        }
        // in case of multiple gifted subs at the same time. we delay this 
        // by 500ms for each time an added subscription
        // and send it as a bulk. instead of having it send 1 per sub
        loyaltyUpdateTimer = 0.5f;
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
        if (monsterTimer > 0 || Appearance.MonsterMesh)
        {
            ResetMonster();
        }

        monsterTimer = time;
        // Pick random monster
        if (availableMonsterMeshes == null || availableMonsterMeshes.Length == 0)
            return false;

        Equipment.HideEquipments();

        Appearance.SetMonsterMesh(availableMonsterMeshes.Random());

        var monsterMesh = Appearance.MonsterMesh;
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
        if (healthBarManager) healthBarManager.Remove(this);
        gameManager.NameTags.Remove(this);
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
        PositionInternal = this.transform.position;
        if (!onsenHandler) onsenHandler = GetComponent<OnsenHandler>();
        if (!clanHandler) clanHandler = GetComponent<ClanHandler>();
        if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
        if (!Inventory) Inventory = GetComponent<Inventory>();
        if (!healthBarManager) healthBarManager = FindObjectOfType<HealthBarManager>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

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

        playerAppearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>();

        if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
        if (healthBarManager) healthBar = healthBarManager.Add(this);

        this.hitRangeCollider = GetComponent<SphereCollider>();

        //if (rbody) rbody.isKinematic = true;

        // add some jitter to help separating players apart a bit.
        var x = UnityEngine.Random.Range(-0.5f, 0.5f);
        var z = UnityEngine.Random.Range(-0.5f, 0.5f);
        this.positionJitter = new Vector3(x, 0, z);
    }

    void LateUpdate()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        var euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, euler.z);
    }

    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        if (LastDestination != Vector3.zero)
        {
            Gizmos.color = new Color(1, 1, 0, 0.75F);
            Gizmos.DrawLine(transform.position, LastDestination);
            Gizmos.DrawSphere(LastDestination, 0.1f);
        }
        if (NextDestination != Vector3.zero)
        {
            Gizmos.color = new Color(0, 1, 1, 0.75F);
            Gizmos.DrawLine(transform.position, this.NextDestination);
            Gizmos.DrawSphere(NextDestination, 0.1f);
        }

        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (agent.pathPending)
            {
                UnityEngine.Debug.LogWarning("Path is being processed");
            }

            if (agent.hasPath)
            {
                UnityEngine.Debug.LogWarning("Path is ready");
            }

            if (agent.isPathStale)
            {
                UnityEngine.Debug.LogWarning("Path is stale");
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
        PositionInternal = this.transform.position;
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        var deltaTime = Time.deltaTime;

        if (loyaltyUpdateTimer > 0 && !IsBot)
        {
            loyaltyUpdateTimer -= deltaTime;
            if (loyaltyUpdateTimer <= 0)
            {
                loyaltyUpdateTimer = -1f;
                gameManager.RavenNest.SendPlayerLoyaltyData(this);
            }
        }

        if (this.Ferry.OnFerry || (this.PositionInternal - lastPosition).magnitude < 0.01f)
        {
            IdleTime += deltaTime;
            MovementTime = 0;
        }
        else
        {
            IdleTime = 0;
            MovementTime += deltaTime;
        }

        //if (Island && agent && 
        //    agent.isActiveAndEnabled && 
        //    agent.isOnNavMesh && 
        //    IdleTime > 2f && 
        //    !IsMoving && 
        //    Animations.IsMoving)
        //{

        //    agent.Warp(Position + Vector3.up * 3f);
        //}

        lastPosition = this.PositionInternal;

        if (!hasBeenInitialized) return;

        HandleRested();
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
            var visibleMesh = this.Appearance.GetCombinedMesh();
            if (visibleMesh)
                visibleMesh.gameObject.SetActive(false);

            if (monsterTimer <= 0)
            {
                ResetMonster();
            }
        }

        UpdateHealthRegeneration();

        actionTimer -= deltaTime;

        if (Onsen.InOnsen)
            return;

        if (!IsMoving && this.Animations.IsMoving)
        {
            this.Animations.StopMoving();
        }
        else if (IsMoving && !this.Animations.IsMoving)
        {
            this.Animations.StartMoving();
        }

        if (Controlled)
            return;

        //if (NextDestination != Vector3.zero)
        //    GotoPosition(NextDestination);

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

        if (Chunk != null)
        {
            DoTask();
            AddTimeExp();
        }
    }

    private void HandleRested()
    {
        // this will be updated from the server every 1s. but to get a smoother transition
        // we do this locally until we get the server update.
        if (!this.Onsen.InOnsen && Rested.RestedTime > 0)
        {
            Rested.RestedTime -= Time.deltaTime;
        }
    }

    private void ResetMonster()
    {
        Appearance.DestroyMonsterMesh();
        animator = gameObject.GetComponent<Animator>();
        Animations.ResetActiveAnimator();
    }

    private void UpdateDropEvent()
    {
        if (!ItemDropEventActive)
            return;

        if (targetDropItem && gameManager.DropEvent.Contains(targetDropItem))
        {
            var island = gameManager.Islands.FindIsland(targetDropItem.transform.position);
            if (!island || !Island || island.name != Island.name)
            {
                targetDropItem = null;
                return;
            }

            if (targetDropItem.transform.position == agent.destination)
                return;

            if (!GotoPosition(targetDropItem.transform.position))
                targetDropItem = null;

            return;
        }

        var availableDropItems = gameManager.DropEvent.GetDropItems();

        targetDropItem = availableDropItems
            .OrderBy(x => Vector3.Distance(x.transform.position, PositionInternal))
            .FirstOrDefault(x => !pickedItems.Contains(x.Id));

        if (targetDropItem == null)
        {
            EndItemDropEvent();
        }
    }

    private void UpdateHealthRegeneration()
    {
        try
        {
            if (this.gameObject == null || this.transform == null)
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
                regenTimer += Time.deltaTime;
            }
            if (regenTimer >= RegenTime)
            {
                var amount = this.Stats.Health.Level * RegenRate * Time.deltaTime;
                regenAmount += amount;
                var add = Mathf.FloorToInt(regenAmount);
                if (add > 0)
                {
                    Stats.Health.CurrentValue = Mathf.Min(this.Stats.Health.Level, Stats.Health.CurrentValue + add);

                    if (healthBar && healthBar != null)
                    {
                        healthBar.UpdateHealth();
                    }

                    regenAmount -= add;
                }

                if (Stats.Health.CurrentValue == Stats.Health.Level)
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

    public bool PickupEventItem(Guid id)
    {
        if (pickedItems.Add(id))
        {
            PickupItem(gameManager.Items.Get(id));
            EndItemDropEvent();
            return true;
        }
        return false;
    }

    public void PickupItem(Item item, string messageStart = "You found")
    {
        if (item == null) return;
        AddItem(item);
        var itemName = item.Name;
        if (item.Category == ItemCategory.StreamerToken)
        {
            itemName = this.gameManager.RavenNest.TwitchDisplayName + " Token";
        }
        if (EquipIfBetter(item))
        {
            if (IsBot) return;
            gameManager.RavenBot.Send(PlayerName, messageStart + " and equipped a {itemName}!", itemName);
            return;
        }
        if (IsBot) return;
        gameManager.RavenBot.Send(PlayerName, messageStart + " a {itemName}!", itemName);
    }

    public PlayerState BuildPlayerState()
    {
        var state = new PlayerState();

        state.CharacterId = this.Id;
        state.SyncTime = Time.time;
        state.UserId = UserId;

        var statistics = Statistics.ToList()
            .Delta(lastSavedStatistics?.ToList())
            .ToArray();

        if (statistics.Any(x => x != 0))
        {
            state.Statistics = statistics;
        }

        state.Experience = Stats.ExperienceList;
        state.Level = Stats.LevelList;

        if (Chunk != null)
        {
            state.CurrentTask = Chunk.ChunkType + ":" + taskArguments.FirstOrDefault();
        }

        return state;
    }

    internal void FailedToSave() => IsUpToDate = false;
    internal void SavedSucceseful()
    {
        IsUpToDate = true;
        lastSavedStatistics = DataMapper.Map<Statistics, Statistics>(Statistics);
        lastSavedExperienceTotal = Stats.TotalExperience;
    }

    public void Cheer() => playerAnimations.ForceCheer();

    public void GotoActiveChunk()
    {
        if (Chunk == null) return;
        GotoPosition(Chunk.CenterPointWorld);
    }

    public void GotoStartingArea()
    {
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();

        Chunk = null;
        GotoPosition(chunkManager.GetStarterChunk().CenterPointWorld);
    }

    public void GotoClosest(TaskType type)
    {
        if (Ferry && Ferry.Active)
        {
            LateGotoClosest(type);
            return;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!chunkManager)
        {
            GameManager.LogError($"No ChunkManager found!");
            return;
        }

        Island = gameManager.Islands.FindPlayerIsland(this);
        Chunk = chunkManager.GetChunkOfType(this, type);

        if (Raid.InRaid || Dungeon.InDungeon)
        {
            return;
        }

        if (Chunk == null)
        {
            var chunks = chunkManager.GetChunksOfType(Island, type);
            if (chunks.Count > 0)
            {
                if (IsBot)
                {
                    return;
                }

                var lowestReq = chunks.OrderBy(x => x.GetRequiredCombatLevel() + x.GetRequiredSkillLevel()).FirstOrDefault();
                var reqCombat = lowestReq.GetRequiredCombatLevel();
                var reqSkill = lowestReq.GetRequiredSkillLevel();

                var arg0 = "";
                var arg1 = "";
                var msg = "";

                if (reqCombat > 1 && reqSkill > 1)
                {
                    msg = "You need to be at least combat level {reqCombat} and skill level {reqSkill} to train this skill on this island.";
                    arg0 = reqCombat.ToString();
                    arg1 = reqSkill.ToString();
                }
                else if (reqCombat > 1)
                {
                    msg = "You need to be at least combat level {reqCombat} to train this skill on this island.";
                    arg0 = reqCombat.ToString();
                }
                else if (reqSkill > 1)
                {
                    msg = "You need to have at least level {reqSkill} {type} to train this skill on this island.";
                    arg0 = reqSkill.ToString();
                    arg1 = type.ToString();
                }


                gameManager.RavenBot.Send(PlayerName, msg, arg0, arg1);
                return;
            }

            gameManager.RavenBot.Send(PlayerName, "You cannot train {type} here.", type);
            GameManager.LogWarning($"{PlayerName}. No suitable chunk found of type '{type}'");
            return;
        }

        playerAnimations.ResetAnimationStates();
        GotoPosition(Chunk.CenterPointWorld);
    }

    public void SetChunk(TaskType type)
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!chunkManager)
        {
            GameManager.LogError($"No ChunkManager found!");
            return;
        }

        Island = gameManager.Islands.FindPlayerIsland(this);
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
        taskArgument = taskArguments.First();
        taskTarget = null;

        CombatType = GetCombatTypeFromArgs(taskArgs);

        if (CombatType != -1)
        {
            lastTrainedCombatSkill = (CombatSkill)CombatType;
        }

        SkillType = GetSkillTypeFromArgs(taskArgs);

        // in case we change what we train.
        // we don't want shield armor to be added to magic, ranged or healing.
        Inventory.UpdateCombatStats();

        UpdateTrainingFlags();
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
            HasTaskArgument("woodcutting") || HasTaskArgument("mining")
            || HasTaskArgument("fishing") || HasTaskArgument("cooking")
            || HasTaskArgument("crafting") || HasTaskArgument("farming");
    }

    public TaskType GetTask() => Chunk?.ChunkType ?? TaskType.None;
    public HashSet<string> GetTaskArguments() => taskArguments;

    internal async Task<GameInventoryItem> CycleEquippedPetAsync()
    {
        var equippedPet = Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        var pets = Inventory.GetInventoryItemsOfType(ItemCategory.Pet, ItemType.Pet);
        if (pets.Count == 0) return null;
        var petToEquip = pets.GroupBy(x => x.Item.Id)
            .OrderBy(x => UnityEngine.Random.value)
            .FirstOrDefault(x => x.Key != equippedPet?.Id)?
            .FirstOrDefault();

        if (petToEquip == null)
        {
            var pet = pets.FirstOrDefault();
            Inventory.Equip(pet.Item);
            return pet;
        }
        if (!IsBot)
        {
            await gameManager.RavenNest.Players.EquipItemAsync(UserId, petToEquip.Item.Id);
        }
        Inventory.Equip(petToEquip.Item);
        return petToEquip;
    }

    internal async Task UnequipAllItemsAsync()
    {
        UnequipAllItems();
        if (!IsBot)
        {
            await gameManager.RavenNest.Players.UnequipAllItemsAsync(UserId);
        }
    }

    internal async Task EquipBestItemsAsync()
    {
        EquipBestItems();
        if (!IsBot)
        {
            await gameManager.RavenNest.Players.EquipBestItemsAsync(UserId);
        }
    }

    internal async Task UnequipAsync(Item item)
    {
        Inventory.Unequip(item);
        if (!IsBot)
        {
            await gameManager.RavenNest.Players.UnequipItemAsync(UserId, item.Id);
        }
    }

    internal async Task<bool> EquipAsync(Item item)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword))
            {
                gameManager.RavenBot.SendMessage(this.TwitchUser.Username, "You cannot equip a shield while having a 2-handed weapon equipped.");
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
                    Inventory.Equip(shield.Item);
                }
            }
        }

        var equipped = Inventory.Equip(item);
        if (!equipped)
        {
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > 0) requirement += item.RequiredAttackLevel + " Attack.";
            if (item.RequiredDefenseLevel > 0) requirement += item.RequiredAttackLevel + " Defense.";
            if (item.RequiredMagicLevel > 0) requirement += item.RequiredAttackLevel + " Magic or Healing.";
            if (item.RequiredRangedLevel > 0) requirement += item.RequiredAttackLevel + " Ranged.";
            if (item.RequiredSlayerLevel > 0) requirement += item.RequiredAttackLevel + " Slayer.";
            gameManager.RavenBot.SendMessage(this.TwitchUser.Username, "You do not meet the requirements to equip " + item.Name + ". " + requirement);
            return false;
        }

        if (IsBot)
        {
            return true;
        }

        if (await gameManager.RavenNest.Players.EquipItemAsync(UserId, item.Id))
        {
            return false;
        }

        return true;
    }

    public void UpdateTwitchUser(TwitchPlayerInfo twitchUser)
    {
        if (twitchUser == null)
        {
            return;
        }

        // you can never not be broadcaster, but if for any reason you were not before, make sure you are now.
        IsBroadcaster = IsBroadcaster || twitchUser.IsBroadcaster;
        IsModerator = twitchUser.IsModerator;
        IsSubscriber = twitchUser.IsSubscriber;
        IsVip = twitchUser.IsVip;
        TwitchUser = twitchUser;
    }

    public void SetPlayer(
        RavenNest.Models.Player player,
        TwitchPlayerInfo twitchUser,
        StreamRaidInfo raidInfo)
    {
        gameObject.name = player.Name;

        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        clanHandler.SetClan(player.Clan, player.ClanRole);

        Definition = player;

        IsGameAdmin = player.IsAdmin;
        IsGameModerator = player.IsModerator;

        Id = player.Id;
        UserId = player.UserId;

        agent.avoidancePriority = UnityEngine.Random.Range(1, 99);

        if (UserId.StartsWith("#") || this.IsBot)
        {
            this.IsBot = true;
            this.Bot = this.gameObject.AddComponent<BotPlayerController>();
        }
        PatreonTier = player.PatreonTier;
        PlayerName = player.Name;
        PlayerNameLowerCase = player.Name.ToLower();
        Stats = new Skills(player.Skills);
        CharacterIndex = player.CharacterIndex;
        Resources = player.Resources;
        Statistics = player.Statistics;
        ExpOverTime = 1d;

        Raider = raidInfo;
        lastSavedStatistics = DataMapper.Map<Statistics, Statistics>(Statistics);

        if (twitchUser == null)
        {
            twitchUser = new TwitchPlayerInfo(
                player.UserId,
                player.UserName,
                player.Name,
                PlayerNameHexColor,
                false, false, false, false, player.Identifier);
        }

        UpdateTwitchUser(twitchUser);

        gameManager.NameTags.Add(this);

        Stats.Health.Reset();
        //Inventory.EquipBestItems();
        Equipment.HideEquipments(); // don't show sword on join

        //if (Raider != null)
        if (player.State != null)
        {
            if (!string.IsNullOrEmpty(player.State.Island) && player.State.X != null)
            {
                if (player.State.InDungeon)
                {
                    var targetIsland = gameManager.Islands.Find(player.State.Island);
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

                    var targetIsland = gameManager.Islands.FindIsland(newPosition);
                    if (targetIsland)
                    {
                        this.Teleporter.Teleport(newPosition);
                    }
                }
            }

            Island = gameManager.Islands.FindPlayerIsland(this);

            if (player.State.InOnsen && onsenHandler)
            {
                // Attach player to the onsen...                
                Game.Onsen.Join(this);
            }

            if (!string.IsNullOrEmpty(player.State.Task))
            {
                SetTask(player.State.Task, new string[] { player.State.TaskArgument });
            }
        }

        Appearance.SetAppearance(player.Appearance, () =>
        {
            Inventory.Create(player.InventoryItems, gameManager.Items.GetItems());

            hasBeenInitialized = true;
        });
    }

    public void SetTask(string targetTaskName, string[] args)
    {
        if (string.IsNullOrEmpty(targetTaskName))
        {
            return;
        }

        if (Ferry && Ferry.Active)
        {
            Ferry.Disembark();
        }

        if (Game.Arena && Game.Arena.HasJoined(this) && !Game.Arena.Leave(this))
        {
            GameManager.Log(PlayerName + " task cannot be done as you're inside the arena.");
            return;
        }

        targetTaskName = targetTaskName.Trim();
        var type = Enum
            .GetValues(typeof(TaskType))
            .Cast<TaskType>()
            .FirstOrDefault(x =>
                x.ToString().Equals(targetTaskName, StringComparison.InvariantCultureIgnoreCase));

        if (type == TaskType.None)
        {
            return;
        }

        var taskArgs = args == null || args.Length == 0f
            ? new[] { type.ToString() }
            : args;

        if (Onsen.InOnsen)
        {
            Game.Onsen.Leave(this);
        }

        if (Duel.InDuel)
        {
            SetTaskArguments(taskArgs);
            return;
        }

        if (Raid.InRaid && !IsCombatTask(taskArgs))
        {
            Game.Raid.Leave(this);
        }

        if (Game.Arena.HasJoined(this))
        {
            Game.Arena.Leave(this);
        }

        if (Dungeon.InDungeon && !IsCombatTask(taskArgs))
        {
            return;
        }

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

    private bool IsCombatTask(string[] taskArgs)
    {
        var ta = new HashSet<string>(taskArgs.Select(x => x.ToLower()));
        var combatSkill = GetCombatTypeFromArgs(ta);
        return combatSkill != -1;
    }

    public bool Fish(FishingController fishingSpot)
    {
        actionTimer = fishingAnimationTime;
        InCombat = false;
        Lock();

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
            AddExp(fishingSpot.Experience, Skill.Fishing);
            var amount = fishingSpot.Resource * Mathf.FloorToInt(Stats.Fishing.CurrentValue / 10f);
            Statistics.TotalFishCollected += (int)amount;
        }

        return true;
    }

    public bool Cook(CraftingStation craftingStation)
    {
        actionTimer = cookingAnimationTime;
        Lock();
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
            AddExp(craftingStation.GetExperience(this), Skill.Cooking);
        }

        return true;
    }

    public bool Craft(CraftingStation craftingStation)
    {
        actionTimer = craftingAnimationTime;
        Lock();
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
            AddExp(craftingStation.GetExperience(this), Skill.Crafting);
        }

        return true;
    }

    public bool Mine(RockController rock)
    {
        actionTimer = mineRockAnimationTime;
        InCombat = false;
        Lock();

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
            AddExp(rock.Experience, Skill.Mining);
            //var amount = rock.Resource * Mathf.FloorToInt(Stats.Mining.CurrentValue / 10f);
            //Statistics.TotalOreCollected += (int)amount;
        }
        return true;
    }

    public bool Cut(TreeController tree)
    {
        actionTimer = chompTreeAnimationTime;
        InCombat = false;
        Lock();

        Equipment.ShowHatchet();

        if (lastTrainedSkill != Skill.Woodcutting)
        {
            lastTrainedSkill = Skill.Woodcutting;
            playerAnimations.StartWoodcutting();
            return true;
        }

        playerAnimations.Chop(0);

        StartCoroutine(DamageTree(tree));

        return true;
    }

    public bool Farm(FarmController farm)
    {
        actionTimer = rakeAnimationTime;
        InCombat = false;
        Lock();

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
            AddExp(farm.Experience, Skill.Farming);
            var amount = farm.Resource * Mathf.FloorToInt(Stats.Farming.CurrentValue / 10f);
            Statistics.TotalWheatCollected += amount;
        }

        return true;
    }

    public bool Attack(PlayerController player)
    {
        if (player == this)
        {
            GameManager.LogError(player.PlayerName + ", You cant fight yourself :o");
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

        Lock();

        Equipment.ShowWeapon(attackType);

        var weapon = Inventory.GetEquipmentOfCategory(ItemCategory.Weapon);
        var weaponAnim = TrainingHealing ? 6 : TrainingMagic ? 4 : TrainingRanged ? 3 : weapon?.GetAnimation() ?? 0;
        var attackAnimation = TrainingHealing || TrainingMagic || TrainingRanged ? 0 : weapon?.GetAnimationCount() ?? 4;

        if (!playerAnimations.IsAttacking() || !lastTrainedSkill.IsCombatSkill())
        {
            //this.GetActiveCombatSkill()
#warning lastTrainedSkill is set to Skill.Attack, it should be set to the actual current combat skill
            lastTrainedSkill = Skill.Attack;
            playerAnimations.StartCombat(weaponAnim, Equipment.HasShield);
            if (!damageOnDraw) return true;
        }

        playerAnimations.Attack(weaponAnim, UnityEngine.Random.Range(0, attackAnimation), Equipment.HasShield);

        transform.LookAt(Target.transform);

        if (TrainingHealing)
        {
            StartCoroutine(HealTarget(target, hitTime));
        }
        else
        {
            StartCoroutine(DamageEnemy(target, hitTime));
        }
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

    public IEnumerator HealTarget(IAttackable target, float hitTime)
    {
        yield return new WaitForSeconds(hitTime);
        try
        {
            if (target == null || !target.Transform || target.GetStats().IsDead) yield break;

            var damage = CalculateDamage(target);
            if (!target.Heal(this, damage))
                yield break;

            if (!target.GivesExperienceWhenKilled)
                yield break;

            var exp = 0d;
            if (damage == 0)
                exp = 2d;
            else
                exp = (double)Mathf.Max(2f, damage / 2f);

            AddExp(exp, Skill.Healing);
        }
        catch (Exception exc)
        {
            GameManager.LogError("Unable to heal target: " + exc);
        }
        finally
        {
            InCombat = false;
        }
    }

    public IEnumerator DamageEnemy(IAttackable enemy, float hitTime)
    {
        yield return new WaitForSeconds(hitTime);
        if (enemy == null)
            yield break;

        if (TrainingRanged)
        {
            this.Effects.DestroyProjectile();
        }

        var damage = CalculateDamage(enemy);
        if (enemy == null || !enemy.TakeDamage(this, damage))
            yield break;

        Statistics.TotalDamageDone += damage;

        var isPlayer = enemy is PlayerController;

        try
        {
            if (!enemy.GivesExperienceWhenKilled)
                yield break;

            // give all attackers exp for the kill, not just the one who gives the killing blow.

            foreach (PlayerController player in enemy.GetAttackers())
            {
                if (isPlayer)
                {
                    ++player.Statistics.PlayersKilled;
                }
                else
                {
                    ++player.Statistics.EnemiesKilled;
                }

                var combatExperience = enemy.GetExperience();

                var activeSkill = GetActiveSkill();
                if (!activeSkill.IsCombatSkill())
                {
                    activeSkill = Skill.Health; // ALL
                }

                player.AddExp(combatExperience, activeSkill);// (CombatSkill)GetCombatTypeFromArgs(player.taskArguments));    
            }
        }
        finally
        {
            InCombat = false;
        }
    }

    public IEnumerator DamageTree(TreeController tree)
    {
        yield return new WaitForSeconds(chompTreeAnimationTime / 2f);

        var damage = CalculateDamage(tree);
        if (!tree.DoDamage(this, damage)) yield break;
        // give all attackers exp for the kill, not just the one who gives the killing blow.
        foreach (var player in tree.WoodCutters)
        {
            ++player.Statistics.TotalTreesCutDown;
            player.AddExp(tree.Experience, Skill.Woodcutting);
            var amount = (int)(tree.Resource * Mathf.FloorToInt(player.Stats.Woodcutting.CurrentValue / 10f));
            player.Statistics.TotalWoodCollected += amount;
        }
    }

    #region Manage EXP/Resources

    public double GetTierExpMultiplier()
    {
        var tierMulti = TwitchEventManager.TierExpMultis[gameManager.Permissions.SubscriberTier];
        var subMulti = (this.IsSubscriber || Game.PlayerBoostRequirement > 0) ? tierMulti : 0;
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

    public double GetExpMultiplier(Skill skill)
    {
        var tierSub = GetTierExpMultiplier();
        var multi = tierSub + gameManager.Village.GetExpBonusBySkill(skill);

        if (gameManager.Boost.Active)
        {
            multi += gameManager.Boost.Multiplier;
        }

        multi = Math.Max(1, multi);
        if (Rested.RestedTime > 0 && Rested.ExpBoost > 1)
            multi *= (float)Rested.ExpBoost;

        return multi * GameMath.ExpScale;
    }

    public void AddExp(double exp, Skill skill)
    {
        exp *= GetExpMultiplier(skill);

        var stat = Stats.GetSkill(skill);
        if (stat == null)
            return;

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

    private void AddTimeExp()
    {
        timeExpTimer -= Time.deltaTime;
        if (timeExpTimer <= 0)
        {
            if (AddExpToActiveSkillStat(ExpOverTime))
            {
                timeExpTimer = 1f;
            }
        }
    }

    public bool AddExpToActiveSkillStat(double experience)
    {
        var skill = GetActiveSkill();

        // !string.IsNullOrEmpty(taskArgument)
        if (skill != Skill.None || skill == Skill.Slayer)
        {
            AddExp(experience, skill);

//#if DEBUG
//            UnityEngine.Debug.LogWarning("Giving " + experience + " to " + skill);
//#endif
            //var combatType = GetCombatTypeFromArg(taskArgument);
            //if (combatType != -1)
            //{
            //    AddCombatExp(experience, (CombatSkill)combatType);
            //}
            //else
            //{
            //    var skill = (TaskSkill)GetSkillTypeFromArgs(taskArguments);
            //    AddExp(experience, skill.AsSkill());
            //}
            return true;
        }
//        else
//        {

//#if DEBUG
//            UnityEngine.Debug.LogWarning("Unable to give exp: " + experience + ", skill is: " + skill);
//#endif
//        }
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
            GameManager.LogError("Cannot do task if we do not know in which chunk we are. :o");
            return;
        }

        if (taskTarget != null)
        {
            var taskCompleted = Chunk.IsTaskCompleted(this, taskTarget);
            if (taskCompleted)
            {
                Unlock();
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
                        GotoPosition(GetTaskTargetPosition(taskTarget));

                    if (reason == TaskExecutionStatus.InsufficientResources)
                    {
                        outOfResourcesAlertTimer -= Time.deltaTime;
                        if (outOfResourcesAlertTimer <= 0f)
                        {
                            GameManager.LogWarning(PlayerName + " is out of resources and won't gain any crafting exp.");

                            var message = lastTrainedSkill == Skill.Cooking
                                ? "You're out of resources, you wont gain any cooking exp. Use !train farming or !train fishing to get some resources."
                                : "You're out of resources, you wont gain any crafting exp. Use !train woodcutting or !train mining to get some resources.";

                            gameManager.RavenBot.SendMessage(PlayerName, message);
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
                GotoPosition(GetTaskTargetPosition(taskTarget));

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
            return attackable.Position + positionJitter;
        }
        return (((obj as Transform) ?? (obj as MonoBehaviour)?.transform)?.position ?? this.PositionInternal) + positionJitter;
    }

    private Transform GetTaskTargetTransform(object obj)
    {
        return (obj as IAttackable)?.Transform ?? (obj as Transform) ?? (obj as MonoBehaviour)?.transform;
    }

    #region Transform Adjustments

    private void LookAt(Transform targetTransform)
    {
        var rot = transform.rotation;
        transform.LookAt(targetTransform);
        transform.rotation = new Quaternion(rot.x, transform.rotation.y, rot.z, rot.w);
    }

    public bool GotoPosition(Vector3 position, bool force = false)
    {
        Unlock();

        playerAnimations.StartMoving();
        InCombat = Duel.InDuel;
        NextDestination = Vector3.zero;

        if ((force || Vector3.Distance(position, LastDestination) >= 1 || Vector3.Distance(agent.destination, position) >= 10) && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.pathPending)
        {
            //if (agent.pathStatus ==  NavMeshPathStatus.)
            agent.SetDestination(position);
            LastDestination = position;
        }
        //else
        //{
        //    NextDestination = position;
        //}

        //if (this.PositionInternal != position)
        //{
        //    MovementTime += Time.deltaTime;
        //}
        return true;
    }

    public void SetPosition(Vector3 position)
    {
        if (agent && agent.enabled)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }
        PositionInternal = position;
        Island = Game.Islands.FindPlayerIsland(this);
    }

    public void Lock()
    {
        if (movementIsLocked)
        {
            return;
        }

        if (agent && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(Transform.position);
                agent.isStopped = true;
            }
            agent.enabled = false;

            if (playerAnimations)
                playerAnimations.StopMoving();
        }
        MovementTime = 0;
        this.movementIsLocked = true;
    }

    public void Unlock()
    {
        if (!movementIsLocked)
        {
            return;
        }

        agent.enabled = true;
        if (agent.isOnNavMesh)
            agent.isStopped = false;

        movementIsLocked = false;
    }
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(string skillName) => Stats.GetSkillByName(skillName);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(Skill skill) => Stats[skill];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(TaskSkill skill) => Stats.GetSkill(skill);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetSkill(int skillIndex) => Stats.GetSkill((TaskSkill)skillIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetCombatSkill(CombatSkill skill) => Stats.GetCombatSkill(skill);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkillStat GetCombatSkill(int skillIndex) => Stats.GetCombatSkill((CombatSkill)skillIndex);


    [Obsolete("Please use GetActiveSkillStat instead.")]
    public SkillStat GetActiveCombatSkillStat()
    {
        var skillIndex = GetCombatTypeFromArgs(taskArguments);
        return Stats.GetCombatSkill((CombatSkill)skillIndex);
    }

    [Obsolete("Please use GetActiveSkillStat instead.")]
    internal SkillStat GetActiveTaskSkillStat()
    {
        var acs = GetActiveCombatSkillStat();
        if (acs != null) return acs;
        var st = GetSkillTypeFromArgs(this.taskArguments);
        if (st >= 0) return GetSkill((TaskSkill)st);
        return null;
    }

    internal SkillStat GetActiveSkillStat()
    {
        var skill = GetActiveSkill();
        if (skill == Skill.None) return null;
        return Stats[skill];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetActiveSkill instead")]
    public int GetCombatTypeFromArgs(params string[] args)
    {
        foreach (var v in args.Select(x => GetCombatTypeFromArg(x.ToLower()))) if (v >= 0) return v;
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetActiveSkill instead")]
    public int GetCombatTypeFromArgs(HashSet<string> args)
    {
        foreach (var v in args.Select(x => GetCombatTypeFromArg(x))) if (v >= 0) return v;
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Skill GetActiveSkill(Skill defaultSkill = Skill.None)
    {
        foreach (var v in taskArguments)
        {
            var skill = GetSkillFromArg(v);
            if (skill != Skill.None)
            {
                return skill;
            }
        }

        return defaultSkill;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetActiveSkill instead")]
    public int GetSkillTypeFromArgs(HashSet<string> args)
    {
        foreach (var v in args.Select(x => GetSkillTypeFromArg(x))) if (v >= 0) return v;
        return -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetActiveSkill instead")]
    public int GetSkillTypeFromArgs(params string[] args)
    {
        foreach (var v in args.Select(x => GetSkillTypeFromArg(x.ToLower()))) if (v >= 0) return v;
        return -1;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Skill GetSkillFromArg(string val)
    {
        if (string.IsNullOrEmpty(val))
            return Skill.None;

        var key = val.ToLower();

        if (skillLookup.TryGetValue(key, out var type))
            return type;

        if (val == "hp" || val == "health" || val == "all")        
            return skillLookup[key] = Skill.Health;

        if (val == "mine") return skillLookup[key] = Skill.Mining;
        if (val == "mage") return skillLookup[key] = Skill.Magic;
        if (val == "atk") return skillLookup[key] = Skill.Attack;

        if (Enum.TryParse<Skill>(val, true, out var skill))
        {
            return skillLookup[key] = skill;
        }

        // do a where -> foreach instead of first or default as first value is not "none"
        // which means we would get wrong skill

        foreach (var s in System.Enum.GetValues(typeof(Skill)).Cast<Skill>().Where(x => x.ToString().ToLower().StartsWith(key)))
        {
            return skillLookup[key] = s;
        }

        return Skill.None;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetSkillFromArg instead")]
    public int GetSkillTypeFromArg(string val)
    {
        if (string.IsNullOrEmpty(val))
            return -1;

        if (skillTypeArgLookup.TryGetValue(val, out var type))
            return type;

        if (StartsWith(val, "wood") || StartsWith(val, "chomp") || StartsWith(val, "chop"))
            return skillTypeArgLookup[val] = 0;
        if (StartsWith(val, "fish") || StartsWith(val, "fist"))
            return skillTypeArgLookup[val] = 1;
        if (StartsWith(val, "craft"))
            return skillTypeArgLookup[val] = 2;
        if (StartsWith(val, "cook"))
            return skillTypeArgLookup[val] = 3;
        if (StartsWith(val, "mine") || StartsWith(val, "mining"))
            return skillTypeArgLookup[val] = 4;
        if (StartsWith(val, "farm"))
            return skillTypeArgLookup[val] = 5;
        if (StartsWith(val, "slay"))
            return skillTypeArgLookup[val] = 6;
        if (StartsWith(val, "sail"))
            return skillTypeArgLookup[val] = 7;

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetSkillFromArg instead")]
    public static int GetCombatTypeFromArg(string val)
    {
        if (string.IsNullOrEmpty(val))
            return -1;

        if (combatTypeArgLookup.TryGetValue(val, out var type))
            return type;
        if (StartsWith(val, "atk") || StartsWith(val, "att"))
            return combatTypeArgLookup[val] = 0;
        if (StartsWith(val, "def"))
            return combatTypeArgLookup[val] = 1;
        if (StartsWith(val, "str"))
            return combatTypeArgLookup[val] = 2;
        if (StartsWith(val, "all") || StartsWith(val, "combat") || StartsWith(val, "health") || StartsWith(val, "hits") || StartsWith(val, "hp"))
            return combatTypeArgLookup[val] = 3;
        if (StartsWith(val, "magic"))
            return combatTypeArgLookup[val] = 4;
        if (StartsWith(val, "ranged"))
            return combatTypeArgLookup[val] = 5;
        if (StartsWith(val, "heal"))
            return combatTypeArgLookup[val] = 6;
        return -1;
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
        if (damage > 0)
        {
            Statistics.TotalDamageTaken += damage;
        }

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
        ++Statistics.DeathCount;
        if (!arena) arena = FindObjectOfType<ArenaController>();
        if (dungeonHandler.InDungeon) dungeonHandler.Died();
        if (arena) arena.Died(this);
        if (duelHandler.InDuel) duelHandler.Died();
        if (streamRaidHandler) streamRaidHandler.Died();

        InCombat = false;
        Animations.Death();
        Lock();
        StartCoroutine(Respawn());
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

    public bool EquipIfBetter(RavenNest.Models.Item item)
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
            Inventory.Equip(item);
            return true;
        }

        return false;
    }

    public void AddItem(RavenNest.Models.Item item, bool updateServer = true)
    {
        if (item == null) return;
        Inventory.Add(item);

        if (IsBot)
        {
            return;
        }

        if (IntegrityCheck.IsCompromised)
        {
            GameManager.LogError("Item add for user: " + UserId + ", item: " + item.Id + ", failed. Integrity compromised");
            return;
        }

        if (!updateServer)
            return;

        queuedItemAdd.Enqueue(item.Id);
        this.hasQueuedItemAdd = true;
        AddQueuedItemAsync();
    }

    private async void AddQueuedItemAsync()
    {
        if (IsBot)
        {
            return;
        }

        if (!Game.RavenNest.Stream.IsReady)
        {
            return;
        }

        if (queuedItemAdd.TryDequeue(out var itemId))
        {
            try
            {
                var result = await gameManager.RavenNest.Players.AddItemAsync(UserId, itemId);
                if (result == AddItemResult.Failed)
                {
                    queuedItemAdd.Enqueue(itemId);
                    GameManager.LogError("Item add for user: " + UserId + ", item: " + itemId + ", result: " + result);
                }
            }
            catch (Exception exc)
            {
                queuedItemAdd.Enqueue(itemId);
                GameManager.LogError(exc.ToString());
            }
        }

        hasQueuedItemAdd = queuedItemAdd.Count > 0;
    }

    public void UpdateCombatStats(List<RavenNest.Models.Item> equipped)
    {
        EquipmentStats.ArmorPower = 0;
        EquipmentStats.WeaponAim = 0;
        EquipmentStats.WeaponPower = 0;
        EquipmentStats.MagicPower = 0;
        EquipmentStats.MagicAim = 0;
        EquipmentStats.RangedPower = 0;
        EquipmentStats.RangedAim = 0;
        foreach (var e in equipped)
        {
            // Ignore shield when training ranged or magic.
            if ((TrainingHealing || TrainingMagic || TrainingRanged) && e.Type == ItemType.Shield)
                continue;

            EquipmentStats.ArmorPower += e.ArmorPower;
            EquipmentStats.WeaponAim += e.WeaponAim;
            EquipmentStats.WeaponPower += e.WeaponPower;
            EquipmentStats.MagicPower += e.MagicPower;
            EquipmentStats.MagicAim += e.MagicAim;
            EquipmentStats.RangedPower += e.RangedPower;
            EquipmentStats.RangedAim += e.RangedAim;
        }

        var op = this.gameManager.Camera.Observer.ObservedPlayer;
        if (op && op.Id == Id)
            this.gameManager.Camera.Observer.ForceUpdate();
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        ClearAttackers();

        if (Island && Island.SpawnPositionTransform)
            transform.position = Island.SpawnPosition;
        else
            transform.position = chunkManager.GetStarterChunk().GetPlayerSpawnPoint();

        MovementTime = 0;
        IdleTime = 0;

        transform.rotation = Quaternion.identity;
        playerAnimations.Revive();

        yield return new WaitForSeconds(2f);

        Stats.Health.Reset();
        Unlock();

        if (Chunk != null && Chunk.Island != Island)
            GotoClosest(Chunk.ChunkType);
    }

    private int CalculateDamage(IAttackable enemy)
    {
        if (enemy == null) return 0;
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
            var homeIsland = Game.Islands.All.FirstOrDefault(x => x.Identifier == "home");
            if (homeIsland)
            {
                agent.Warp(homeIsland.SpawnPosition);
                Island = homeIsland;
                return;
            }
        }

        if (!Onsen.InOnsen && !Dungeon.InDungeon && Animations.IsMoving && !Ferry.OnFerry)
        {
            var i = Island;
            if (!i) i = Game.Islands.All.FirstOrDefault(x => x.Identifier == "home");
            this.SetPosition(i.SpawnPosition);
            return;
        }

        if ((Island && this.Game.Islands.FindPlayerIsland(this) != Island) ||
            Island && agent && agent.isActiveAndEnabled && agent.isOnNavMesh && IdleTime > 2f && !IsMoving && Animations.IsMoving)
        {
            this.SetPosition(Island.SpawnPosition);
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
}

public class CharacterRestedState
{
    public double ExpBoost { get; internal set; }
    public double RestedPercent { get; internal set; }
    public double RestedTime { get; internal set; }
    public double CombatStatsBoost { get; internal set; }
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

    public Task InvokeAsync()
    {
        if (lazyRequest != null)
        {
            return lazyRequest.Invoke();
        }

        return request;
    }
}
