using System;
using UnityEngine;

public class TownHallManager : MonoBehaviour
{
    [SerializeField] private TownHallController[] townHallBuildings;
    [SerializeField] private TownHallInfoManager info;

    [Tooltip("The ground the biggest town hall tier covers, in tier space (x and z, metres): its building, " +
             "plaza and resource yard. Plots keep off all of it, and a moved hall must find room for all of " +
             "it, so an upgrade never lands on a house.")]
    [SerializeField] private Rect area = new Rect(-23.6f, -16.1f, 49.2f, 45.3f);

    private int tier = -1;
    private int level;
    private double exp;

    private long wood;
    private long coins;
    private long ore;
    private long fish;
    private long wheat;

    private TownHallController activeTownHall;

    private bool plazaCaptured;
    private Vector3 plazaPosition;
    private Quaternion plazaRotation;

    void Start()
    {
        if (!info) info = FindAnyObjectByType<TownHallInfoManager>();

        if (townHallBuildings == null || townHallBuildings.Length == 0)
            townHallBuildings = GetComponentsInChildren<TownHallController>();

        if (townHallBuildings.Length > 0)
            activeTownHall = townHallBuildings[0];
    }

    /// <summary>
    /// The town hall building currently standing, for its position and size. The manager itself
    /// sits at the world origin, so anything measuring distance to the town hall must use this.
    /// </summary>
    public Transform ActiveHall
    {
        get
        {
            if (activeTownHall && activeTownHall.gameObject.activeInHierarchy) return activeTownHall.transform;
            if (townHallBuildings != null)
            {
                foreach (var hall in townHallBuildings)
                {
                    if (hall && hall.gameObject.activeInHierarchy) return hall.transform;
                }
            }

            return activeTownHall ? activeTownHall.transform : transform;
        }
    }

    /// <summary>Where the scene put the town hall: the middle of the village, whether or not the hall was moved.</summary>
    public Vector3 Plaza
    {
        get { CapturePlaza(); return plazaPosition; }
    }

    /// <summary>Where the town hall stands now, the same for every tier.</summary>
    public Vector3 HallPosition => townHallBuildings != null && townHallBuildings.Length > 0 ? townHallBuildings[0].transform.position : transform.position;

    /// <summary>The town hall's turn around the vertical, in degrees.</summary>
    public float HallRotation => townHallBuildings != null && townHallBuildings.Length > 0 ? townHallBuildings[0].transform.eulerAngles.y : 0f;

    /// <summary>Whether the streamer moved the town hall off the plaza.</summary>
    public bool IsMoved { get; private set; }

    public Rect Area => area;

    /// <summary>
    /// The tier the village has bought, from the server. Null when the server does not sell tiers yet,
    /// and the tier then follows the village level as it always did.
    /// </summary>
    public int? BoughtTier { get; private set; }
    public int MaxTier { get; private set; } = 4;

    /// <summary>What the next tier needs: village level and resources.</summary>
    public int NextLevel { get; private set; }
    public int NextWood { get; private set; }
    public int NextOre { get; private set; }
    public int NextFish { get; private set; }
    public int NextWheat { get; private set; }

    /// <summary>Whether there is a tier to buy.</summary>
    public bool HasNextTier => BoughtTier.HasValue && BoughtTier.Value < MaxTier;

    /// <summary>Whether the village can buy the next tier now.</summary>
    public bool CanUpgrade => HasNextTier && level >= NextLevel && wood >= NextWood && ore >= NextOre && fish >= NextFish && wheat >= NextWheat;

    public double Experience => exp;
    public int Level => level;
    public int Tier => tier;

    public long Coins => coins;
    public long Wood => wood;
    public long Wheat => wheat;
    public long Fish => fish;
    public long Ore => ore;

    //public decimal ExperienceToLevel => GameMath.VillageLevelToExperience(Level + 1);

    public void SetTierByLevel(int level)
    {
        if (this.level != level)
        {
            SetLevel(level);
            // A bought tier stays until the next one is bought; levelling up only makes it buyable.
            SetTier(BoughtTier ?? GetTownHallTier(level));
            if (info) info.MakeDirty();
        }
    }

    /// <summary>The tier the village has bought and what the next one needs, from the server.</summary>
    public void SetBoughtTier(int tier, int maxTier, int nextLevel, int nextWood, int nextOre, int nextFish, int nextWheat)
    {
        BoughtTier = tier;
        MaxTier = maxTier;
        NextLevel = nextLevel;
        NextWood = nextWood;
        NextOre = nextOre;
        NextFish = nextFish;
        NextWheat = nextWheat;
        SetTier(tier);
        if (info) info.MakeDirty();
    }

    /// <summary>Takes resources spent on an upgrade off the village's stock, until the server's next update.</summary>
    public void Spend(long wood, long ore, long fish, long wheat)
    {
        SetResources(coins, this.ore - ore, this.wood - wood, this.fish - fish, this.wheat - wheat);
    }

    public void SetTier(int tier)
    {
        if (this.tier == tier)
        {
            return;
        }


        this.tier = tier;

        foreach (var townHall in townHallBuildings)
            townHall.gameObject.SetActive(false);

        if (tier >= townHallBuildings.Length)
            tier = townHallBuildings.Length - 1;

        activeTownHall = townHallBuildings[tier];
        activeTownHall.gameObject.SetActive(true);

        // The new tier's resource piles only knew the resources from when it was last shown.
        activeTownHall.ResourcesUpdated();

        if (info)
        {
            info.Tier = tier;
            info.MakeDirty();
        }
    }

    /// <summary>Houses built, for the town resource bar.</summary>
    public int UsedSlots { get; private set; }

    /// <summary>House slots the village has at its level.</summary>
    public int SlotCount { get; private set; }

    internal void SetSlotCount(int usedCount, int count)
    {
        UsedSlots = usedCount;
        SlotCount = count;
        if (info)
        {
            info.UsedSlots = usedCount;
            info.Slots = count;
            info.MakeDirty();
        }
    }

    internal void OpenVillageDialog()
    {
        if (info)
        {
            info.MakeDirty();
            info.Toggle();
            var infoPos = activeTownHall.InfoTransform;
            info.transform.SetPositionAndRotation(infoPos.position, infoPos.rotation);
        }
    }

    private void SetLevel(int level)
    {
        this.level = level;
        if (info)
        {
            info.Level = level;
            info.MakeDirty();
        }
    }

    internal void SetExp(double experience)
    {
        this.exp = experience;
        if (info)
        {
            info.Experience = experience;
            info.MakeDirty();
        }
    }

    // ---- Placement ---------------------------------------------------------------------------

    /// <summary>
    /// Remembers where the scene put the town hall, so a moved hall can always go back. All tiers
    /// stand at the same spot, so the first one speaks for them.
    /// </summary>
    private void CapturePlaza()
    {
        if (plazaCaptured || townHallBuildings == null || townHallBuildings.Length == 0) return;
        plazaPosition = townHallBuildings[0].transform.position;
        plazaRotation = townHallBuildings[0].transform.rotation;
        plazaCaptured = true;
    }

    /// <summary>
    /// Stands every tier at a spot on the ground, turned by <paramref name="rotation"/> degrees, without
    /// recording it as a placement. Build mode uses this while the hall follows the mouse.
    /// </summary>
    public void MoveTo(Vector3 position, float rotation)
    {
        CapturePlaza();
        position.y = GroundHeight(position);
        var turn = Quaternion.Euler(0f, rotation, 0f);
        foreach (var hall in townHallBuildings)
        {
            if (hall) hall.transform.SetPositionAndRotation(position, turn);
        }
    }

    /// <summary>Moves the town hall to where the streamer placed it.</summary>
    public void ApplyPlacement(float x, float z, float rotation)
    {
        MoveTo(new Vector3(x, 0f, z), rotation);
        IsMoved = true;
    }

    /// <summary>Puts the town hall back on the plaza.</summary>
    public void ResetPlacement()
    {
        CapturePlaza();
        foreach (var hall in townHallBuildings)
        {
            if (hall) hall.transform.SetPositionAndRotation(plazaPosition, plazaRotation);
        }

        IsMoved = false;
    }

    /// <summary>The four corners of the town hall area, on the ground plane, for a hall standing at a spot.</summary>
    public Vector3[] AreaCorners(Vector3 position, float rotation)
    {
        var turn = Quaternion.Euler(0f, rotation, 0f);
        return new[]
        {
            position + turn * new Vector3(area.xMin, 0f, area.yMin),
            position + turn * new Vector3(area.xMin, 0f, area.yMax),
            position + turn * new Vector3(area.xMax, 0f, area.yMax),
            position + turn * new Vector3(area.xMax, 0f, area.yMin),
        };
    }

    /// <summary>
    /// Whether a round footprint of <paramref name="radius"/> at <paramref name="point"/> reaches into the
    /// town hall area of a hall standing at <paramref name="position"/>, turned by <paramref name="rotation"/>.
    /// </summary>
    public bool AreaOverlaps(Vector3 position, float rotation, Vector3 point, float radius)
    {
        var local = Quaternion.Euler(0f, -rotation, 0f) * (point - position);
        var dx = Mathf.Max(area.xMin - local.x, 0f, local.x - area.xMax);
        var dz = Mathf.Max(area.yMin - local.z, 0f, local.z - area.yMax);
        return dx * dx + dz * dz < radius * radius;
    }

    /// <summary>
    /// Height of the terrain under a point. Taken from the terrain itself rather than by casting down,
    /// which would land on the hall's own floors while it is being moved and lift it a little every frame.
    /// </summary>
    public float GroundHeight(Vector3 point)
    {
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (!terrain || !terrain.terrainData) continue;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
            return terrain.SampleHeight(point) + origin.y;
        }

        return PlacementUtility.FindGroundPoint(point).y;
    }

    private int GetTownHallTier(int level)
    {
        // < 10: tier 0 -- 10: Tier 1
        // < 30: tier 1 -- 30: Tier 2
        // < 50: tier 2 -- 50: Tier 3

        if (level < 10) return 0;
        if (level < 30) return 1;
        if (level < 80) return 2;
        if (level < 150) return 3;
        if (level < 200) return 4;
        return 4;

        //var tier = 1;
        //for (; tier < 4; ++tier)
        //{
        //    if ((level - 20 * tier) <= 0)
        //    {
        //        break;
        //    }
        //}

        //return tier;
    }

    internal void SetResources(long coins, long ore, long wood, long fish, long wheat)
    {
        var modified = false;
        if (this.coins != coins)
        {
            this.coins = coins;
            modified = true;
        }

        if (this.ore != ore)
        {
            this.ore = ore;
            modified = true;
        }

        if (this.wood != wood)
        {
            this.wood = wood;
            modified = true;
        }

        if (this.fish != fish)
        {
            this.fish = fish;
            modified = true;
        }

        if (this.wheat != wheat)
        {
            this.wheat = wheat;
            modified = true;
        }

        if (modified)
        {
            activeTownHall.ResourcesUpdated();
        }
    }
}
