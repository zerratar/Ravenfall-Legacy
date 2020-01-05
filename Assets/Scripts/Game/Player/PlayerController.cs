using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK;
using UnityEngine;
using UnityEngine.AI;
using Resources = RavenNest.Models.Resources;

public interface IPlayerController { }

public class PlayerController : MonoBehaviour, IAttackable, IPlayerController
{
    private readonly HashSet<Guid> pickedItems = new HashSet<Guid>();

    internal readonly ConcurrentDictionary<string, IAttackable> attackers
        = new ConcurrentDictionary<string, IAttackable>();

    [SerializeField] private string[] taskArguments;

    [SerializeField] private GameManager gameManager;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private NameTagManager nameTagManager;
    [SerializeField] private HealthBarManager healthBarManager;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [SerializeField] private RaidHandler raidHandler;
    [SerializeField] private StreamRaidHandler streamRaidHandler;

    [SerializeField] private DungeonHandler dungeonHandler;
    [SerializeField] private ArenaHandler arenaHandler;
    [SerializeField] private CombatHandler combatHandler;
    [SerializeField] private DuelHandler duelHandler;
    [SerializeField] private FerryHandler ferryHandler;
    [SerializeField] private TeleportHandler teleportHandler;
    [SerializeField] private EffectHandler effectHandler;

    [SerializeField] private PlayerAnimationController playerAnimations;

    [SerializeField] private Rigidbody rbody;
    [SerializeField] private float attackAnimationTime = 1.5f;
    [SerializeField] private float rangeAnimationTime = 1.5f;
    [SerializeField] private float magicAnimationTime = 1.5f;
    [SerializeField] private float chompTreeAnimationTime = 2f;
    [SerializeField] private float rakeAnimationTime = 3f;
    [SerializeField] private float fishingAnimationTime = 3f;
    [SerializeField] private float craftingAnimationTime = 3f;
    [SerializeField] private float cookingAnimationTime = 3f;
    [SerializeField] private float mineRockAnimationTime = 2f;

    [SerializeField] private float respawnTime = 4f;

    private IPlayerAppearance playerAppearance;
    private float actionTimer = 0f;
    private float timeExpTimer = 1f;
    private Skill lastTrainedSkill = Skill.Fighting;
    private ArenaController arena;
    private DamageCounterManager damageCounterManager;

    private TaskType? lateGotoClosestType = null;

    private decimal ExpOverTime;

    private decimal lastSavedExperienceTotal;
    private Statistics lastSavedStatistics;

    private float outOfResourcesAlertTimer = 0f;
    private float outOfResourcesAlertTime = 60f;
    private int outOfResourcesAlertCounter;

    private Transform attackTarget;

    internal Transform taskTarget;
    public IChunk Chunk;

    public Skills Stats = new Skills();
    public Resources Resources = new Resources();
    public Statistics Statistics = new Statistics();
    public PlayerEquipment Equipment;
    public Inventory Inventory;

    public string PlayerName;
    public string PlayerNameHexColor = "#FFFFFF";

    public string UserId;
    public float AttackRange = 1.8f;
    public float RangedAttackRange = 15F;
    public float MagicAttackRange = 15f;

    public EquipmentStats EquipmentStats = new EquipmentStats();

    public bool IsModerator;
    public bool IsBroadcaster;
    public bool IsSubscriber;
    public bool IsVip;

    public bool IsGameAdmin;
    public bool IsGameModerator;

    public float RegenTime = 10f;
    public float RegenRate = 0.1f;
    private float regenTimer;
    private float regenAmount;

    private int saving;
    private HealthBar healthBar;
    private bool hasBeenInitialized;

    private ItemController targetDropItem;

    public Transform Target
    {
        get => attackTarget ?? taskTarget;
        private set => attackTarget = value;
    }
    public RavenNest.Models.Player Definition { get; private set; }
    public Transform Transform => gameObject.transform;
    public bool IsMoving => agent.isActiveAndEnabled && agent.remainingDistance > 0;
    public bool Kicked { get; set; }
    public bool IsNPC => PlayerName != null && PlayerName.StartsWith("Player ");
    public bool IsReadyForAction => actionTimer <= 0f;
    public string Name => PlayerName;
    public bool GivesExperienceWhenKilled => false;
    public bool InCombat { get; private set; }

    public StreamRaidInfo Raider { get; private set; }
    public bool UseLongRange => HasTaskArgument("ranged") || HasTaskArgument("magic");
    public bool TrainingRanged => HasTaskArgument("ranged");
    public bool TrainingMagic => HasTaskArgument("magic");

    public PlayerAnimationController Animations => playerAnimations;
    public IslandController Island { get; set; }
    public IPlayerAppearance Appearance => playerAppearance
        ?? (playerAppearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>()
        ?? GetComponent<PlayerAppearance>());
    public StreamRaidHandler StreamRaid => streamRaidHandler;
    public RaidHandler Raid => raidHandler;
    public ArenaHandler Arena => arenaHandler;
    public DuelHandler Duel => duelHandler;
    public CombatHandler Combat => combatHandler;
    public FerryHandler Ferry => ferryHandler;
    public EffectHandler Effects => effectHandler;
    public TeleportHandler Teleporter => teleportHandler;

    public DungeonHandler Dungeon => dungeonHandler;

    public bool ItemDropEventActive { get; private set; }

    public float GetAttackRange()
    {
        if (TrainingMagic) return MagicAttackRange;
        if (TrainingRanged) return RangedAttackRange;
        return AttackRange;
    }
    public bool HasTaskArgument(string args) =>
        GetTaskArguments().Any(x => x.Equals(args, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Called whenever the player is removed from the PlayerManager
    /// </summary>
    public void OnRemoved()
    {
        if (healthBarManager) healthBarManager.Remove(this);
        if (nameTagManager) nameTagManager.Remove(this);
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
        if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
        if (!Inventory) Inventory = GetComponent<Inventory>();
        if (!healthBarManager) healthBarManager = FindObjectOfType<HealthBarManager>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        if (!Equipment) Equipment = GetComponent<PlayerEquipment>();
        if (!rbody) rbody = GetComponent<Rigidbody>();

        if (!effectHandler) effectHandler = GetComponent<EffectHandler>();
        if (!teleportHandler) teleportHandler = GetComponent<TeleportHandler>();
        if (!ferryHandler) ferryHandler = GetComponent<FerryHandler>();
        if (!raidHandler) raidHandler = GetComponent<RaidHandler>();
        if (!streamRaidHandler) streamRaidHandler = GetComponent<StreamRaidHandler>();
        if (!arenaHandler) arenaHandler = GetComponent<ArenaHandler>();
        if (!duelHandler) duelHandler = GetComponent<DuelHandler>();
        if (!combatHandler) combatHandler = GetComponent<CombatHandler>();

        playerAppearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>() ?? GetComponent<PlayerAppearance>();

        if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
        if (healthBarManager) healthBar = healthBarManager.Add(this);

        if (rbody) rbody.isKinematic = true;
    }

    void LateUpdate()
    {
        var euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, euler.z);
    }

    void Update()
    {
        if (!hasBeenInitialized) return;

        SavePlayerState();
        actionTimer -= Time.deltaTime;

        if (streamRaidHandler.InWar) return;
        if (ferryHandler.OnFerry || ferryHandler.Active) return;

        UpdateHealthRegeneration();

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
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
            .FirstOrDefault(x => !pickedItems.Contains(x.Id));

        if (targetDropItem == null)
        {
            EndItemDropEvent();
        }
    }

    private void UpdateHealthRegeneration()
    {
        if ((Chunk == null || Chunk.ChunkType != TaskType.Fighting) && !InCombat)
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
                healthBar.UpdateHealth();
                regenAmount -= add;
            }

            if (Stats.Health.CurrentValue == Stats.Health.Level)
            {
                regenTimer = 0;
            }
        }
    }

    private void SavePlayerState()
    {
        if (IntegrityCheck.IsCompromised)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref saving, 1, 0) == 1)
        {
            return;
        }

        gameManager.RavenNest.SavePlayerAsync(this).ContinueWith(result =>
        {
            Interlocked.Exchange(ref saving, 0);
            return true;
        });
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

    public void PickupItem(Item item)
    {
        AddItem(item);

        if (EquipIfBetter(item))
        {
            gameManager.Server.Client.SendCommand(PlayerName, "item_pickup", $"You found and equipped a {item.Name}!");
            return;
        }
        gameManager.Server.Client.SendCommand(PlayerName, "item_pickup", $"You found a {item.Name}!");
    }

    public PlayerState BuildPlayerState()
    {
        var state = new PlayerState();

        state.UserId = UserId;

        var statistics = Statistics.ToList()
            .Delta(lastSavedStatistics?.ToList())
            .ToArray();

        if (statistics.Any(x => x != 0))
        {
            state.Statistics = statistics;
        }

        if (Stats.TotalExperience > lastSavedExperienceTotal)
        {
            state.Experience = Stats.ExperienceList;
        }

        if (Chunk != null)
        {
            state.CurrentTask = Chunk.ChunkType + ":" + taskArguments.FirstOrDefault();
        }

        return state;
    }

    internal void SavedSucceseful()
    {
        lastSavedStatistics = DataMapper.Map<Statistics, Statistics>(Statistics);
        lastSavedExperienceTotal = Stats.TotalExperience;
    }

    public void Cheer()
    {
        playerAnimations.ForceCheer();
    }

    public void GotoActiveChunk()
    {
        if (Chunk == null)
        {
            return;
        }

        GotoPosition(Chunk.CenterPointWorld);
    }

    public void GotoStartingArea()
    {
        if (!chunkManager) chunkManager = FindObjectOfType<ChunkManager>();
        if (!chunkManager)
        {
            Debug.LogError("No ChunkManager found!");
            return;
        }

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
            Debug.LogError($"No ChunkManager found!");
            return;
        }

        Island = gameManager.Islands.FindPlayerIsland(this);
        Chunk = chunkManager.GetChunkOfType(this, type);
        if (Chunk == null)
        {
            var chunks = chunkManager.GetChunksOfType(Island, type);
            if (chunks.Count > 0)
            {
                var lowestReq = chunks.OrderBy(x => x.GetRequiredCombatLevel() + x.GetRequiredkillLevel()).FirstOrDefault();

                var reqCombat = lowestReq.GetRequiredCombatLevel();
                var reqSkill = lowestReq.GetRequiredkillLevel();

                gameManager.Server.Client.SendCommand(
                    PlayerName,
                    "train_failed",

                    reqCombat > 1
                    ? $"You need to be at least combat level {lowestReq.GetRequiredCombatLevel()} to train this skill on this island."
                    : reqSkill > 1
                        ? $"You need have level {lowestReq.GetRequiredkillLevel()} {type} to train this skill on this island."
                        : $"You need to be at least combat level {lowestReq.GetRequiredCombatLevel()} and skill level {lowestReq.GetRequiredkillLevel()} to train this skill on this island.");
                return;
            }

            Debug.LogWarning($"{PlayerName}. No suitable chunk found of type '{type}'");
            return;
        }

        playerAnimations.ResetAnimationStates();

        GotoPosition(Chunk.CenterPointWorld);
    }

    private void LateGotoClosest(TaskType type) => lateGotoClosestType = type;

    public void SetTaskArguments(string[] taskArgs)
    {
        taskArguments = taskArgs;
        taskTarget = null;
    }

    public TaskType GetTask() => Chunk?.ChunkType ?? TaskType.None;
    public string[] GetTaskArguments() => taskArguments.ToArray();

    internal async Task<GameInventoryItem> CycleEquippedPetAsync()
    {
        var equippedPet = Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        var pets = Inventory.GetInventoryItemsOfType(ItemCategory.Pet, ItemType.Pet);
        if (pets.Count == 0) return null;
        var petToEquip = pets.GroupBy(x => x.Item.Id)
            .OrderBy(x => UnityEngine.Random.value)
            .FirstOrDefault(x => x.Key != equippedPet?.Id)
            .FirstOrDefault();

        await gameManager.RavenNest.Players.EquipItemAsync(UserId, petToEquip.Item.Id);
        Inventory.Equip(petToEquip.Item);
        return petToEquip;
    }

    public void SetPlayer(
        RavenNest.Models.Player player,
        Player streamUser,
        StreamRaidInfo raidInfo)
    {
        gameObject.name = player.Name;

        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (Appearance != null)
        {
            Appearance.SetAppearance(player.Appearance);
        }

        Definition = player;

        IsGameAdmin = player.IsAdmin;
        IsGameModerator = player.IsModerator;

        UserId = player.UserId;
        PlayerName = player.Name;
        Stats = new Skills(player.Skills);
        Resources = player.Resources;
        Statistics = player.Statistics;
        ExpOverTime = 1m;

        Raider = raidInfo;

        lastSavedStatistics = DataMapper.Map<Statistics, Statistics>(Statistics);

        if (streamUser != null)
        {
            IsModerator = streamUser.IsModerator;
            IsBroadcaster = streamUser.IsBroadcaster;
            IsSubscriber = streamUser.IsSubscriber;
            IsVip = streamUser.IsVip;
        }

        Island = gameManager.Islands.FindPlayerIsland(this);
        Inventory.Create(player.InventoryItems, gameManager.Items.GetItems());
        lastSavedExperienceTotal = Stats.TotalExperience;

        if (!nameTagManager)
            nameTagManager = GameObject.Find("NameTags").GetComponent<NameTagManager>();

        if (nameTagManager)
            nameTagManager.Add(this);

        Stats.Health.Reset();
        Inventory.EquipBestItems();
        Equipment.HideEquipments(); // don't show sword on join
        hasBeenInitialized = true;
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
            var amount = rock.Resource * Mathf.FloorToInt(Stats.Mining.CurrentValue / 10f);
            Statistics.TotalOreCollected += (int)amount;
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

    public AttackType GetAttackType()
    {
        if (TrainingRanged) return AttackType.Ranged;
        if (TrainingMagic) return AttackType.Magic;
        return AttackType.Melee;
    }

    public bool Attack(PlayerController player)
    {
        if (player == this)
        {
            Debug.LogError(player.PlayerName + ", You cant fight yourself :o");
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

    private bool AttackEntity(IAttackable target, bool damageOnDraw = false)
    {
        var attackType = GetAttackType();
        InCombat = true;
        regenTimer = 0f;
        actionTimer = GetAttackAnimationTime(attackType);
        Target = target.Transform;

        var hitTime = actionTimer / 2;

        Lock();

        Equipment.ShowWeapon(attackType);
        var weapon = Inventory.GetEquipmentOfCategory(ItemCategory.Weapon);
        var weaponAnim = TrainingMagic ? 4 : TrainingRanged ? 3 : weapon?.Type.GetAnimation() ?? 0;
        var attackAnimation = TrainingMagic || TrainingRanged ? 0 : weapon?.Type.GetAnimationCount() ?? 4;

        if (!playerAnimations.IsAttacking() || lastTrainedSkill != Skill.Fighting)
        {
            lastTrainedSkill = Skill.Fighting;
            playerAnimations.StartCombat(weaponAnim);
            if (!damageOnDraw) return true;
        }

        playerAnimations.Attack(weaponAnim, UnityEngine.Random.Range(0, attackAnimation));

        transform.LookAt(Target.transform);
        StartCoroutine(DamageEnemy(target, hitTime));
        return true;
    }

    private float GetAttackAnimationTime(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Ranged:
                return rangeAnimationTime;
            case AttackType.Magic:
                return magicAnimationTime;
            default:
                return attackAnimationTime;
        }
    }

    public IEnumerator DamageEnemy(IAttackable enemy, float hitTime)
    {
        yield return new WaitForSeconds(hitTime);
        if (enemy == null)
        {
            yield break;
        }

        var damage = CalculateDamage(enemy);
        if (enemy == null || !enemy.TakeDamage(this, damage))
        {
            yield break;
        }

        Statistics.TotalDamageDone += damage;

        var isPlayer = enemy is PlayerController;

        try
        {
            if (!enemy.GivesExperienceWhenKilled)
            {
                yield break;
            }

            // give all attackers exp for the kill, not just the one who gives the killing blow.

            foreach (PlayerController player in enemy.GetAttackers())
            {
                //if (!(attacker is PlayerController player))
                //{
                //    continue;
                //}

                if (isPlayer)
                {
                    ++player.Statistics.PlayersKilled;
                }
                else
                {
                    ++player.Statistics.EnemiesKilled;
                }

                var combatExperience = enemy.GetExperience(); //GameMath.CombatExperience(enemy.GetStats());                
                player.AddCombatExp(combatExperience,
                    player.taskArguments != null
                        ? GetCombatTypeFromArgs(player.taskArguments)
                        : Mathf.FloorToInt(UnityEngine.Random.value * 3));
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
            player.AddResource(Resource.Woodcutting, amount);
        }
    }

    #region Manage EXP/Resources

    public void AddCombatExp(decimal exp, int skill)
    {
        if (gameManager.Boost.Active)
        {
            exp *= (decimal)gameManager.Boost.Multiplier;
        }

        switch (skill)
        {
            case 0:
                if (Stats.Attack.AddExp(exp, out var atkLvls))
                {
                    CelebrateCombatSkillLevelUp(CombatSkill.Attack, atkLvls);
                    Inventory.EquipBestItems();
                }
                break;
            case 1:
                if (Stats.Defense.AddExp(exp, out var defLvls))
                {
                    CelebrateCombatSkillLevelUp(CombatSkill.Defense, defLvls);
                    Inventory.EquipBestItems();
                }
                break;
            case 2:
                if (Stats.Strength.AddExp(exp, out var strLvls))
                    CelebrateCombatSkillLevelUp(CombatSkill.Strength, strLvls);
                break;
            // all / controlled
            case 3:
                {
                    var each = exp / 3;
                    if (Stats.Attack.AddExp(each, out var a))
                    {
                        CelebrateCombatSkillLevelUp(CombatSkill.Attack, a);
                        Inventory.EquipBestItems();
                    }

                    if (Stats.Defense.AddExp(each, out var b))
                    {
                        CelebrateCombatSkillLevelUp(CombatSkill.Defense, b);
                        Inventory.EquipBestItems();
                    }
                    if (Stats.Strength.AddExp(each, out var c))
                        CelebrateCombatSkillLevelUp(CombatSkill.Strength, c);
                }
                break;
            case 4:
                {
                    if (Stats.Magic.AddExp(exp, out var lvls))
                    {
                        CelebrateCombatSkillLevelUp(CombatSkill.Magic, lvls);
                    }
                }
                break;
            case 5:
                {
                    if (Stats.Ranged.AddExp(exp, out var lvls))
                    {
                        CelebrateCombatSkillLevelUp(CombatSkill.Ranged, lvls);
                    }
                }
                break;
        }

        if (Stats.Health.AddExp(exp / 3, out var hpLevels))
        {
            CelebrateCombatSkillLevelUp(CombatSkill.Health, hpLevels);
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

    public void RemoveResource(Resource resource, decimal amount)
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

    public void AddResource(Resource resource, decimal amount, bool allowMultiplier = true)
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

    public void AddExp(decimal exp, Skill skill)
    {
        if (gameManager.Boost.Active)
        {
            exp *= (decimal)gameManager.Boost.Multiplier;
        }


        switch (skill)
        {
            case Skill.Woodcutting:
                {
                    if (Stats.Woodcutting.AddExp(exp, out var a))
                        CelebrateSkillLevelUp(skill, a);
                }
                break;

            case Skill.Fishing:
                {
                    if (Stats.Fishing.AddExp(exp, out var a))
                        CelebrateSkillLevelUp(skill, a);
                }
                break;

            case Skill.Crafting:
                {
                    if (Stats.Crafting.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                }
                break;

            case Skill.Cooking:
                {
                    if (Stats.Cooking.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                }
                break;
            case Skill.Mining:
                {
                    if (Stats.Mining.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                    break;
                }
            case Skill.Farming:
                {
                    if (Stats.Farming.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                    break;
                }
            case Skill.Slayer:
                {
                    if (Stats.Slayer.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                    break;
                }
            case Skill.Sailing:
                {
                    if (Stats.Sailing.AddExp(exp, out var c))
                        CelebrateSkillLevelUp(skill, c);
                    break;
                }
        }
    }

    private void AddTimeExp()
    {
        timeExpTimer -= Time.deltaTime;
        if (timeExpTimer <= 0)
        {
            if (AddExpToCurrentSkill(ExpOverTime))
                timeExpTimer = 1f;
        }
    }

    public bool AddExpToCurrentSkill(decimal experience)
    {
        if (taskArguments != null && taskArguments.Length > 0)
        {
            var combatType = GetCombatTypeFromArg(taskArguments[0]);
            if (combatType != -1)
            {
                AddCombatExp(experience, combatType);
            }
            else
            {
                var skill = (Skill)GetSkillTypeFromArgs(taskArguments);
                AddExp(experience, skill);
            }
            return true;
        }
        return false;
    }

    private void CelebrateCombatSkillLevelUp(CombatSkill skill, int levelCount)
    {
        CelebrateLevelUp(skill.ToString(), levelCount);
    }

    private void CelebrateSkillLevelUp(Skill skill, int levelCount)
    {
        CelebrateLevelUp(skill.ToString(), levelCount);
    }

    private void CelebrateLevelUp(string skillName, int levelCount)
    {
        playerAnimations.Cheer();
        gameManager.PlayerLevelUp(this, GetSkill(skillName));
        Effects.LevelUp();
    }

    #endregion

    private void DoTask()
    {
        if (Chunk == null)
        {
            Debug.LogError("Cannot do task if we do not know in which chunk we are. :o");
            return;
        }

        if (taskTarget)
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
                        if (!taskTarget)
                        {
                            return;
                        }

                        Chunk.TargetAcquired(this, taskTarget);
                        return;
                    }

                    if (reason == TaskExecutionStatus.OutOfRange)
                    {
                        GotoPosition(taskTarget.transform.position);
                    }

                    if (reason == TaskExecutionStatus.InsufficientResources)
                    {
                        outOfResourcesAlertTimer -= Time.deltaTime;
                        if (outOfResourcesAlertTimer <= 0f)
                        {
                            Debug.LogWarning(PlayerName + " is out of resources and won't gain any crafting exp.");

                            var message = lastTrainedSkill == Skill.Cooking
                                ? "You're out of resources, you wont gain any cooking exp. Use !train farming or !train fishing to get some resources."
                                : "You're out of resources, you wont gain any crafting exp. Use !train woodcutting or !train mining to get some resources.";

                            gameManager.Server.Client.SendCommand(PlayerName, "train_failed", message);
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
        if (!taskTarget)
        {
            return;
        }

        Chunk.TargetAcquired(this, taskTarget);

        if (!Chunk.CanExecuteTask(this, taskTarget, out var exeTaskReason))
        {
            if (exeTaskReason == TaskExecutionStatus.OutOfRange)
                GotoPosition(taskTarget.transform.position);
        }
    }

    #region Transform Adjustments

    private void LookAt(Transform targetTransform)
    {
        var rot = transform.rotation;
        transform.LookAt(targetTransform);
        transform.rotation = new Quaternion(rot.x, transform.rotation.y, rot.z, rot.w);
    }

    public bool GotoPosition(Vector3 position)
    {
        Unlock();
        playerAnimations.StartMoving();
        InCombat = Duel.InDuel;
        agent.SetDestination(position);
        return true;
    }

    public void Lock()
    {
        if (playerAnimations)
            playerAnimations.StopMoving();

        if (agent && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            agent.SetDestination(transform.position);
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rbody)
        {
            rbody.isKinematic = true;
            //rbody.detectCollisions = false;
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
            //rbody.detectCollisions = true;
        }
    }
    #endregion

    public SkillStat GetSkill(string skillName)
    {
        var skillField = Stats.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.FieldType == typeof(SkillStat))
            .FirstOrDefault(x => x.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skillField != null)
        {
            return skillField.GetValue(Stats) as SkillStat;
        }

        return null;
    }

    public SkillStat GetSkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: return Stats.Woodcutting;
            case 1: return Stats.Fishing;
            case 2: return Stats.Crafting;
            case 3: return Stats.Cooking;
            case 4: return Stats.Mining;
            case 5: return Stats.Farming;
            case 6: return Stats.Slayer;
            case 7: return Stats.Sailing;
        }
        return null;
    }

    public SkillStat GetCombatSkill(CombatSkill skill)
    {
        switch (skill)
        {
            case CombatSkill.Attack: return Stats.Attack;
            case CombatSkill.Defense: return Stats.Defense;
            case CombatSkill.Strength: return Stats.Strength;
            case CombatSkill.Health: return Stats.Health;
            case CombatSkill.Magic: return Stats.Magic;
            case CombatSkill.Ranged: return Stats.Ranged;
        }
        return null;
    }

    public SkillStat GetCombatSkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: return Stats.Attack;
            case 1: return Stats.Defense;
            case 2: return Stats.Strength;
            case 3: return Stats.Health;
            case 4: return Stats.Magic;
            case 5: return Stats.Ranged;
        }
        return null;
    }

    public int GetCombatTypeFromArgs(params string[] args)
    {
        foreach (var val in args)
        {
            var value = GetCombatTypeFromArg(val);
            if (value != -1) return value;
        }

        return -1;
    }

    public static int GetCombatTypeFromArg(string val)
    {
        if (StartsWith(val, "atk") || StartsWith(val, "att")) return 0;
        if (StartsWith(val, "def")) return 1;
        if (StartsWith(val, "str")) return 2;
        if (StartsWith(val, "all") || StartsWith(val, "combat") || StartsWith(val, "health") || StartsWith(val, "hits") || StartsWith(val, "hp")) return 3;
        if (StartsWith(val, "magic")) return 4;
        if (StartsWith(val, "ranged")) return 5;
        return -1;
    }

    public int GetSkillTypeFromArgs(params string[] args)
    {
        foreach (var val in args)
        {
            if (StartsWith(val, "wood") || StartsWith(val, "chomp") || StartsWith(val, "chop")) return 0;
            if (StartsWith(val, "fish") || StartsWith(val, "fist")) return 1;
            if (StartsWith(val, "craft")) return 2;
            if (StartsWith(val, "cook")) return 3;
            if (StartsWith(val, "mine") || StartsWith(val, "mining")) return 4;
            if (StartsWith(val, "farm")) return 5;
            if (StartsWith(val, "slay")) return 6;
            if (StartsWith(val, "sail")) return 7;
        }

        return -1;
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
            attackers[attacker.Name] = attacker;
        }

        if (!damageCounterManager)
            damageCounterManager = FindObjectOfType<DamageCounterManager>();


        damageCounterManager.Add(transform, damage);
        Stats.Health.Add(-damage);
        Statistics.TotalDamageTaken += damage;

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
        return attackers.Values.Where(x => x != null).ToList();
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

    public decimal GetExperience() => 0;

    public bool EquipIfBetter(RavenNest.Models.Item item)
    {
        if (item.Category == ItemCategory.Resource || item.Category == ItemCategory.Food)
        {
            return false;
        }

        if (item.RequiredDefenseLevel > Stats.Defense.Level)
        {
            return false;
        }

        if (item.RequiredAttackLevel > Stats.Attack.Level)
        {
            return false;
        }

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

    public async void AddItem(RavenNest.Models.Item item, bool updateServer = true)
    {
        Inventory.Add(item);

        if (IntegrityCheck.IsCompromised)
        {
            Debug.LogError("Item add for user: " + UserId + ", item: " + item.Id + ", failed. Integrity compromised");
            return;
        }

        if (!updateServer)
        {
            return;
        }

        var result = await gameManager.RavenNest.Players.AddItemAsync(UserId, item.Id);
        if (result == AddItemResult.Failed)
        {
            Debug.LogError("Item add for user: " + UserId + ", item: " + item.Id + ", result: " + result);
        }
        //else
        //{
        //    Debug.Log("Item add for user: " + UserId + ", item: " + item.Id + ", result: " + result);
        //}
    }

    public void UpdateCombatStats(List<RavenNest.Models.Item> equipped)
    {
        EquipmentStats.ArmorPower = 0;
        EquipmentStats.WeaponAim = 0;
        EquipmentStats.WeaponPower = 0;
        foreach (var e in equipped)
        {
            EquipmentStats.ArmorPower += e.ArmorPower;
            EquipmentStats.WeaponAim += e.WeaponAim;
            EquipmentStats.WeaponPower += e.WeaponPower;
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        attackers.Clear();

        if (Island && Island.SpawnPositionTransform)
        {
            transform.position = Island.SpawnPosition;
        }
        else
        {
            transform.position = chunkManager.GetStarterChunk().GetPlayerSpawnPoint();
        }

        transform.rotation = Quaternion.identity;
        playerAnimations.Revive();

        yield return new WaitForSeconds(2f);

        Stats.Health.Reset();
        Unlock();

        if (Chunk != null && Chunk.Island != Island)
        {
            GotoClosest(Chunk.ChunkType);
        }
    }

    private int CalculateDamage(IAttackable enemy)
    {
        if (enemy == null) return 0;
        if (TrainingMagic)
        {
            return (int)GameMath.CalculateMagicDamage(this, enemy);
        }

        if (TrainingRanged)
        {
            return (int)GameMath.CalculateRangedDamage(this, enemy);
        }

        return (int)GameMath.CalculateDamage(this, enemy);
    }

    private int CalculateDamage(TreeController enemy)
    {
        return (int)GameMath.CalculateSkillDamage(Stats.Woodcutting, enemy.Level);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool StartsWith(string str, string arg) => str.StartsWith(arg, StringComparison.OrdinalIgnoreCase);
}
