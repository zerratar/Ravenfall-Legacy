using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// Village build mode: entering and leaving it, selecting plots, and building, demolishing and
/// assigning residents. The interface lives in <c>Shinobytes.UI.BuildModeView</c>; this class owns
/// the state and talks to the server.
/// </summary>
/// <remarks>
/// Phase 1 of docs/village-build-mode-design.md: a real mode on top of today's village, with the
/// same server calls the old icon bar made. While this component exists it owns village clicks.
/// <c>TownHouseManager</c> stops handling them, so a misclick while flying around can no longer
/// start construction outside build mode.
/// </remarks>
public class VillageBuildMode : MonoBehaviour
{
    [Tooltip("Where the camera goes on entering build mode, relative to the town hall.")]
    [SerializeField] private Vector3 overviewOffset = new Vector3(0f, 30f, -28f);

    [Header("Placement")]
    [Tooltip("Grid cell size in metres. Positions snap to it.")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Degrees per press of R. Hold Alt for quarter turns.")]
    [SerializeField] private float rotationStep = 15f;

    [Tooltip("How far from the plaza buildings may stand, in metres, for each town hall tier (1 to 5). The village grows with its hall; the last covers the whole island.")]
    [SerializeField] private float[] areaByTier = { 45f, 60f, 75f, 95f, 150f };

    // 3 m since the houses have foundations reaching 3.2 m down (tools/village-rebuild/house-foundations.cs): at 2 m,
    // much of the island was refused, around the bridge for one
    [Tooltip("Largest height difference across a footprint before the ground counts as too steep.")]
    [SerializeField] private float maxSlope = 3f;

    [Tooltip("Gap kept between buildings, in metres.")]
    [SerializeField] private float spacing = 0.5f;

    [Tooltip("Largest height difference across the town hall area before the ground counts as too steep.")]
    [SerializeField] private float maxHallSlope = 4f;

    public static VillageBuildMode Instance { get; private set; }

    /// <summary>Whether build mode is on.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The plot the streamer has selected, if any.</summary>
    public TownHouseSlot Selected { get; private set; }

    /// <summary>
    /// A building type picked from the tray with no plot selected. Clicking an empty plot builds it.
    /// </summary>
    public TownHouseSlotType? Armed { get; private set; }

    /// <summary>
    /// While a building is armed: the empty plot under the mouse it would snap to, if any.
    /// Otherwise it would go where the ghost stands, on a free plot moved there.
    /// </summary>
    public TownHouseSlot SnapTarget { get; private set; }

    /// <summary>True while a server call is in flight; the interface disables its actions.</summary>
    public bool Busy { get; private set; }

    /// <summary>The plot being moved, if any.</summary>
    public TownHouseSlot Moving { get; private set; }

    /// <summary>Whether the town hall is selected (its panel shows Move, Rotate and Reset position).</summary>
    public bool HallSelected { get; private set; }

    /// <summary>Whether the town hall is following the mouse.</summary>
    public bool MovingHall { get; private set; }

    /// <summary>Whether a plot or the town hall is being moved.</summary>
    public bool IsMoving => Moving || MovingHall;

    /// <summary>Whether the plot or town hall being moved can be dropped where it is.</summary>
    public bool MoveValid { get; private set; }

    /// <summary>Why the current spot is not valid, for the hint line.</summary>
    public string MoveProblem { get; private set; }

    /// <summary>Raised when build mode turns on or off.</summary>
    public event Action<bool> ActiveChanged;

    /// <summary>Raised when the selection, armed building or village contents change.</summary>
    public event Action Changed;

    /// <summary>Raised with a short message when an action fails, for the interface to show.</summary>
    public event Action<string> Failed;

    private GameManager gameManager;
    private Camera worldCamera;
    private readonly RaycastHit[] hits = new RaycastHit[32];

    private PlacementOverlay overlay;
    private VillageGrid villageGrid;
    private VillageRoadTool roadTool;

    /// <summary>Drawing and editing roads, which shares this mode's input.</summary>
    public VillageRoadTool Roads => roadTool ? roadTool : (roadTool = GetComponent<VillageRoadTool>() ?? gameObject.AddComponent<VillageRoadTool>());
    private GameObject ghost;
    private Footprint ghostFootprint = Footprint.Default;
    private float oceanLevel = float.NaN;
    private Vector3 moveStartPosition;
    private Quaternion moveStartRotation;
    private float moveRotation;
    private Vector3 lastSnapped = new Vector3(float.NaN, 0f, 0f);
    private float lastRotation = float.NaN;
    private readonly Dictionary<TownHouseSlot, float> radiusCache = new Dictionary<TownHouseSlot, float>();
    private readonly Dictionary<TownHouseSlot, Footprint> footprintCache = new Dictionary<TownHouseSlot, Footprint>();

    // The plots in the way of the placement last checked, for the overlay's rings and the message.
    private readonly List<TownHouseSlot> blockers = new List<TownHouseSlot>();
    // the town hall's area is in the way too: outlined red like a building
    private bool hallBlocks;
    private readonly List<(Vector3, float, Vector2)> blockerRings = new List<(Vector3, float, Vector2)>();
    private bool previewing;

    /// <summary>The buildings in the way of the placement last checked.</summary>
    public IReadOnlyList<TownHouseSlot> Blockers => blockers;
    private Vector3 hallStartPosition;
    private float hallStartRotation;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        gameManager = GameManager.Instance ? GameManager.Instance : FindAnyObjectByType<GameManager>();
    }

    // ---- Entering and leaving ------------------------------------------------------------

    public bool CanEnter => gameManager && gameManager.IsLoaded && gameManager.Village != null && gameManager.Village.TownHouses;

    public void Toggle()
    {
        if (IsActive) Exit(); else Enter();
    }

    public void Enter()
    {
        if (IsActive || !CanEnter)
        {
            return;
        }

        // Forcing the free camera stops the automatic tour from pulling the camera to a player
        // in the middle of building.
        gameManager.Camera.ForceFreeCamera(false);
        StartCoroutine(FlyToVillage());

        IsActive = true;
        Selected = null;
        Armed = null;
        ShowVillageGrid();
        ActiveChanged?.Invoke(true);
        Changed?.Invoke();
    }

    /// <summary>
    /// The grid over everywhere a building may stand: the same circle round the plaza that
    /// <see cref="Validate"/> keeps buildings inside, with its edge drawn, and the ring the next
    /// town hall tier opens shown dimmer beyond it.
    /// </summary>
    private void ShowVillageGrid()
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall) return;

        if (float.IsNaN(oceanLevel)) oceanLevel = FindOceanLevel();
        if (!villageGrid) villageGrid = VillageGrid.Create();
        villageGrid.Show(townHall.Plaza, AreaRadius, NextAreaRadius, cellSize, townHall.GroundHeight,
            float.IsNegativeInfinity(oceanLevel) ? float.NegativeInfinity : oceanLevel);
    }

    public void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        EndPreview();
        CancelMove();
        CancelHallMove();
        SetArmed(null);
        if (villageGrid) villageGrid.Hide();
        Roads.CancelDrawing();
        Roads.Deselect();
        IsActive = false;
        Selected = null;
        HallSelected = false;
        gameManager.Camera.ReleaseFreeCamera();
        ActiveChanged?.Invoke(false);
        Changed?.Invoke();
    }

    /// <summary>
    /// Eases the camera over the town hall. Waits first: switching to the free camera starts a
    /// coroutine in GameCamera that puts the camera back where it was after a frame and a physics
    /// step, and moving the camera before that just flickered it.
    /// </summary>
    private System.Collections.IEnumerator FlyToVillage()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;

        var townHall = gameManager.Village.TownHall;
        var cam = Camera.main;
        if (!townHall || !cam || !IsActive)
        {
            yield break;
        }

        // The manager sits at the world origin; the building is what the camera should look at.
        var centre = townHall.ActiveHall.position;
        var fromPosition = cam.transform.position;
        var fromRotation = cam.transform.rotation;
        var toPosition = centre + overviewOffset;
        var toRotation = Quaternion.LookRotation(centre - toPosition);

        const float duration = 0.6f;
        for (var t = 0f; t < 1f && IsActive; t += Time.unscaledDeltaTime / duration)
        {
            var e = 1f - (1f - t) * (1f - t) * (1f - t);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, e), Quaternion.Slerp(fromRotation, toRotation, e));
            yield return null;
        }

        if (IsActive) cam.transform.SetPositionAndRotation(toPosition, toRotation);
    }

    // ---- Selection -----------------------------------------------------------------------

    public IReadOnlyList<TownHouseSlot> Plots
    {
        get
        {
            var houses = gameManager && gameManager.Village != null ? gameManager.Village.TownHouses : null;
            if (!houses || houses.Slots == null)
            {
                return Array.Empty<TownHouseSlot>();
            }

            var count = Mathf.Min(houses.SlotCount, houses.Slots.Length);
            var list = new List<TownHouseSlot>(count);
            for (var i = 0; i < count; ++i)
            {
                if (houses.Slots[i]) list.Add(houses.Slots[i]);
            }

            return list;
        }
    }

    public IReadOnlyList<TownHouse> Catalogue
    {
        get
        {
            var houses = gameManager && gameManager.Village != null ? gameManager.Village.TownHouses : null;
            return houses && houses.TownHouses != null ? houses.TownHouses : Array.Empty<TownHouse>();
        }
    }

    public void Select(TownHouseSlot slot)
    {
        Roads.Deselect();
        Roads.CancelDrawing();
        if (!IsActive)
        {
            return;
        }

        // A building armed from the tray goes straight onto an empty plot.
        if (Armed.HasValue && slot && IsEmpty(slot))
        {
            var type = Armed.Value;
            SetArmed(null);
            _ = BuildAsync(slot, type);
            return;
        }

        Selected = slot;
        HallSelected = false;
        SetArmed(null);
        Changed?.Invoke();
    }

    /// <summary>Selects the town hall, for moving or turning it.</summary>
    public void SelectHall()
    {
        Roads.Deselect();
        Roads.CancelDrawing();
        if (!IsActive)
        {
            return;
        }

        Selected = null;
        HallSelected = true;
        SetArmed(null);
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        EndPreview();
        Roads.Deselect();
        Selected = null;
        HallSelected = false;
        SetArmed(null);
        Changed?.Invoke();
    }

    /// <summary>
    /// A card was picked in the tray. With an empty plot selected it builds there; with a built
    /// plot selected it changes the building; with nothing selected it arms the card.
    /// </summary>
    public void Choose(TownHouseSlotType type)
    {
        Roads.Deselect();
        Roads.CancelDrawing();
        if (!IsActive || Busy)
        {
            return;
        }

        if (Selected)
        {
            _ = BuildAsync(Selected, type);
            return;
        }

        if (Armed != type && PlotCounts().free == 0)
        {
            Failed?.Invoke(Localization.UI_BUILD_NO_FREE_PLOTS);
            return;
        }

        SetArmed(Armed == type ? (TownHouseSlotType?)null : type);
        Changed?.Invoke();
    }

    /// <summary>Arms or disarms a building, creating or removing the ghost that follows the mouse.</summary>
    private void SetArmed(TownHouseSlotType? type)
    {
        if (Armed == type)
        {
            return;
        }

        Armed = type;
        SnapTarget = null;
        lastSnapped = new Vector3(float.NaN, 0f, 0f);
        lastRotation = float.NaN;

        if (ghost) Destroy(ghost);
        ghost = null;
        if (overlay) overlay.Hide();

        if (!type.HasValue)
        {
            return;
        }

        var house = Catalogue.FirstOrDefault(x => x && x.Type == type.Value);
        if (!house || !house.Prefab)
        {
            return;
        }

        ghost = PlacementOverlay.MakeGhost(house.Prefab);
        ghostFootprint = Footprint.Measure(ghost, useRenderers: true);
        moveRotation = 0f;
        if (!overlay) overlay = PlacementOverlay.Create(cellSize);
    }

    public static bool IsEmpty(TownHouseSlot slot)
    {
        return slot.SlotType == TownHouseSlotType.Empty || slot.SlotType == TownHouseSlotType.Undefined;
    }

    // ---- Input ---------------------------------------------------------------------------

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        if (!gameManager.Camera.IsFreeCameraActive && !gameManager.Camera.ForcedFreeCamera)
        {
            // Something else took the camera (a raid, a dungeon). Leave build mode cleanly.
            Exit();
            return;
        }

        if (Roads.HandleInput())
        {
            return;
        }

        if (Moving)
        {
            UpdateMove();
            return;
        }

        if (MovingHall)
        {
            UpdateHallMove();
            return;
        }

        if (Armed.HasValue && ghost)
        {
            UpdatePlacing();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            if (Armed.HasValue || Selected || HallSelected) ClearSelection();
            else Exit();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            var slot = PlotUnderMouse(out var hall);
            var road = Roads.RoadUnderMouse();
            var again = (slot && slot == Selected) || (hall && HallSelected);
            if (road != null && (again || (!slot && !hall))) SelectRoad(road);
            else if (slot) Select(slot);
            else if (hall) SelectHall();
            else if (Selected || HallSelected || Armed.HasValue || Roads.Selected != null) ClearSelection();
        }
    }

    /// <summary>
    /// The plot under the mouse. When the town hall is the nearest thing hit, returns none and sets
    /// <paramref name="hall"/>.
    /// </summary>
    private TownHouseSlot PlotUnderMouse(out bool hall)
    {
        hall = false;
        if (!worldCamera || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        if (!worldCamera) return null;

        var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        var count = Physics.RaycastNonAlloc(ray, hits, 2000f);
        TownHouseSlot best = null;
        var bestDistance = float.MaxValue;
        var hallDistance = float.MaxValue;
        for (var i = 0; i < count; ++i)
        {
            var hit = hits[i];
            var slot = hit.collider.GetComponentInParent<TownHouseSlot>();
            if (slot && hit.distance < bestDistance)
            {
                best = slot;
                bestDistance = hit.distance;
            }
            else if (!slot && hit.distance < hallDistance && hit.collider.GetComponent<TownHallController>())
            {
                hallDistance = hit.distance;
            }
        }

        if (hallDistance < bestDistance)
        {
            hall = true;
            return null;
        }

        return best;
    }

    private TownHouseSlot PlotUnderMouse() => PlotUnderMouse(out _);

    // ---- Moving --------------------------------------------------------------------------

    /// <summary>Picks a plot up. It follows the mouse until it is placed or the move is cancelled.</summary>
    public void StartMove(TownHouseSlot slot)
    {
        if (!IsActive || Busy || !slot)
        {
            return;
        }

        CancelMove();
        CancelHallMove();
        Moving = slot;
        Selected = slot;
        HallSelected = false;
        Armed = null;
        moveStartPosition = slot.transform.position;
        moveStartRotation = slot.transform.rotation;
        moveRotation = Mathf.Round(slot.transform.eulerAngles.y / rotationStep) * rotationStep;
        lastSnapped = new Vector3(float.NaN, 0f, 0f);
        lastRotation = float.NaN;
        radiusCache.Clear();
        footprintCache.Clear();

        if (!overlay) overlay = PlacementOverlay.Create(cellSize);
        Changed?.Invoke();
    }

    /// <summary>Puts the plot back where it was before the move started.</summary>
    public void CancelMove()
    {
        if (!Moving)
        {
            return;
        }

        Moving.transform.SetPositionAndRotation(moveStartPosition, moveStartRotation);
        TownHouseSlot.SyncColliders();
        Moving = null;
        MoveProblem = null;
        if (overlay) overlay.Hide();
        Changed?.Invoke();
    }

    private void UpdateMove()
    {
        // Saving: the plot stays where it was dropped until the server answers.
        if (Busy)
        {
            return;
        }

        // Esc only: the right mouse button turns the camera, which is exactly what someone does
        // while lining a building up.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            CancelMove();
            return;
        }

        ReadRotationKeys();

        if (!TryGetMousePoint(out var point))
        {
            return;
        }

        var snapped = new Vector3(Mathf.Round(point.x / cellSize) * cellSize, point.y, Mathf.Round(point.z / cellSize) * cellSize);
        if (snapped.x != lastSnapped.x || snapped.z != lastSnapped.z || moveRotation != lastRotation)
        {
            lastSnapped = snapped;
            lastRotation = moveRotation;

            var ground = PlacementUtility.FindGroundPoint(snapped);
            Moving.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, moveRotation, 0f));

            MoveProblem = Validate(Moving, FootprintOf(Moving), ground, moveRotation);
            MoveValid = MoveProblem == null;
            Changed?.Invoke();

            if (overlay)
            {
                ShowFootprint(FootprintOf(Moving), Moving.transform.position, moveRotation, MoveValid);
                RingBlockers();
            }
        }

        if (overlay)
        {
            ShowFootprint(FootprintOf(Moving), Moving.transform.position, moveRotation, MoveValid);
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            if (MoveValid) _ = CommitMoveAsync();
            else Failed?.Invoke(MoveProblem);
        }
    }

    /// <summary>
    /// Where the mouse points on the village. The ray is cut against a horizontal plane at the
    /// plaza's height rather than against the scene, so trees, houses and characters under the
    /// cursor do not make the building jump; the ground height is found straight down from there.
    /// The manager itself sits at the world origin, eight metres below the village, and a plane at
    /// its height put the building metres away from the cursor.
    /// </summary>
    public bool TryGetMousePoint(out Vector3 point)
    {
        point = default;
        if (!worldCamera || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        var townHall = gameManager.Village.TownHall;
        if (!worldCamera || !townHall) return false;

        var plane = new Plane(Vector3.up, townHall.Plaza);
        var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out var distance)) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    /// <summary>Null when the plot can stand here, otherwise the reason it cannot.</summary>
    private string Validate(TownHouseSlot slot, Footprint footprint, Vector3 position, float rotation, bool checkArea = true)
    {
        blockers.Clear();
        hallBlocks = false;
        var townHall = gameManager.Village.TownHall;
        // Measured from the plaza, the middle of the village, so moving the town hall does not move
        // the area buildings may stand in.
        var centre = townHall ? townHall.Plaza : position;

        // Inside the village: the circle the town hall's tier allows. A building already outside it
        // (the area was larger before tiers set it) may stay, but only moves to somewhere inside.
        if (checkArea && (Flat(position - centre).magnitude > AreaRadius || (villageGrid && !villageGrid.IsOnVillageLand(position))))
        {
            return Localization.UI_BUILD_PLACE_OUTSIDE;
        }

        // Not overlapping the town hall: its footprint, not a circle around it, so buildings can
        // stand right beside it.
        if (OverlapsTownHall(footprint, position, rotation))
        {
            hallBlocks = true;
            return Localization.UI_BUILD_PLACE_TOWN_HALL;
        }

        // Clear of every other plot, built or not. All of them are collected, so the message can
        // name them and the overlay ring them: a streamer used to move buildings one by one to find
        // the one in the way.
        foreach (var other in Plots)
        {
            if (other == slot) continue;
            if (Footprint.Overlaps(footprint.Grown(spacing / 2f), position, rotation,
                    FootprintOf(other).Grown(spacing / 2f), other.transform.position, other.transform.eulerAngles.y))
            {
                blockers.Add(other);
            }
        }

        if (blockers.Count > 0)
        {
            return WithBlockers(Localization.UI_BUILD_PLACE_OVERLAP);
        }

        // On ground, not water, and not too steep.
        var low = float.MaxValue;
        var high = float.MinValue;
        var corners = footprint.Corners(position, rotation);
        for (var i = 0; i < 5; ++i)
        {
            var sample = i == 4 ? footprint.Centre(position, rotation) : corners[i];
            if (IsWater(sample)) return Localization.UI_BUILD_PLACE_WATER;
            var y = PlacementUtility.FindGroundPoint(sample).y;
            low = Mathf.Min(low, y);
            high = Mathf.Max(high, y);
        }

        if (high - low > maxSlope)
        {
            return Localization.UI_BUILD_PLACE_STEEP;
        }

        return null;
    }

    private bool IsWater(Vector3 point)
    {
        // The sea has no collider, so the ground under it is the seabed; it counts as water when
        // that ground is below the sea's surface. Lakes have colliders, handled below.
        if (float.IsNaN(oceanLevel)) oceanLevel = FindOceanLevel();
        if (!float.IsNegativeInfinity(oceanLevel) && PlacementUtility.FindGroundPoint(point).y < oceanLevel + 0.2f)
        {
            return true;
        }

        var ray = new Ray(point + Vector3.up * 30f, Vector3.down);
        var hits = Physics.RaycastAll(ray, 80f);
        RaycastHit? first = null;
        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger || hit.collider.GetComponentInParent<TownHouseSlot>()) continue;
            if (first == null || hit.distance < first.Value.distance) first = hit;
        }

        if (first == null) return false;
        var name = first.Value.collider.name;
        return name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("ocean", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Half the width of a plot's footprint, from its colliders, or a sensible default.</summary>
    private float RadiusOf(TownHouseSlot slot)
    {
        if (radiusCache.TryGetValue(slot, out var cached)) return cached;

        var radius = 2.5f;
        var colliders = slot.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            // Measured with the plot unrotated, so a turned building does not grow its footprint.
            var rotation = slot.transform.rotation;
            slot.transform.rotation = Quaternion.identity;
            var bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Length; ++i) bounds.Encapsulate(colliders[i].bounds);
            slot.transform.rotation = rotation;
            radius = Mathf.Max(2f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.85f);
        }

        radiusCache[slot] = radius;
        return radius;
    }

    /// <summary>
    /// Whether a footprint at <paramref name="position"/>, turned by <paramref name="rotation"/>, reaches into the
    /// town hall area: the ground the biggest tier covers (building, plaza and yard), turned with the
    /// hall, so an upgrade never lands on a house.
    /// </summary>
    private bool OverlapsTownHall(Footprint footprint, Vector3 position, float rotation)
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall) return false;
        return Footprint.Overlaps(HallFootprint(townHall), townHall.HallPosition, townHall.HallRotation, footprint, position, rotation);
    }

    /// <summary>How far from the plaza buildings may stand with a town hall of <paramref name="tier"/> (0 to 4).</summary>
    public float AreaRadiusFor(int tier)
    {
        if (areaByTier == null || areaByTier.Length == 0) return float.MaxValue;
        return areaByTier[Mathf.Clamp(tier, 0, areaByTier.Length - 1)];
    }

    /// <summary>How far from the plaza buildings may stand now.</summary>
    public float AreaRadius
    {
        get
        {
            var townHall = gameManager.Village.TownHall;
            return AreaRadiusFor(townHall ? townHall.Tier : 0);
        }
    }

    /// <summary>The area the next town hall tier opens, or the current one at the last tier.</summary>
    public float NextAreaRadius
    {
        get
        {
            var townHall = gameManager.Village.TownHall;
            return AreaRadiusFor(townHall ? townHall.Tier + 1 : 1);
        }
    }

    /// <summary>The town hall tier, shown 1 to 5, that opens the next ring; 0 at the last tier.</summary>
    public int NextAreaTier
    {
        get
        {
            var townHall = gameManager.Village.TownHall;
            var tier = townHall ? townHall.Tier : 0;
            return NextAreaRadius > AreaRadius ? tier + 2 : 0;
        }
    }

    /// <summary>A plot standing outside the area its town hall allows, from before tiers set it.</summary>
    public bool IsOutsideArea(TownHouseSlot slot)
    {
        var townHall = gameManager.Village.TownHall;
        if (!slot || !townHall) return false;
        return Flat(slot.transform.position - townHall.Plaza).magnitude > AreaRadius;
    }

    /// <summary>
    /// Where the next tier's ring is labelled: half way across it, on the side the camera looks at,
    /// so the label is in view.
    /// </summary>
    public bool TryGetNextAreaLabelPoint(Camera camera, out Vector3 point)
    {
        point = default;
        var townHall = gameManager.Village.TownHall;
        if (!townHall || !camera || NextAreaTier == 0) return false;
        var centre = townHall.Plaza;
        var look = Flat(camera.transform.position + camera.transform.forward * 60f - centre);
        if (look.sqrMagnitude < 1f) look = Vector3.forward;
        var flat = centre + look.normalized * ((AreaRadius + NextAreaRadius) / 2f);
        point = new Vector3(flat.x, townHall.GroundHeight(flat), flat.z);
        return true;
    }

    /// <summary>Picks a road for editing; the building selection goes.</summary>
    public void SelectRoad(VillageRoads.Road road)
    {
        EndPreview();
        Selected = null;
        HallSelected = false;
        SetArmed(null);
        Roads.Select(road);
        Changed?.Invoke();
    }

    /// <summary>Starts drawing a road of <paramref name="type"/>; the building selection goes.</summary>
    public void ChooseRoad(int type)
    {
        EndPreview();
        Selected = null;
        HallSelected = false;
        SetArmed(null);
        Roads.StartDrawing(type);
        Changed?.Invoke();
    }

    /// <summary>The town hall's area, the room its biggest tier needs, as a footprint.</summary>
    private static Footprint HallFootprint(TownHallManager townHall)
    {
        var area = townHall.Area;
        return new Footprint(new Vector2(area.width / 2f, area.height / 2f), area.center);
    }

    /// <summary>A plot's rectangle, measured once from its building and kept until something changes.</summary>
    private Footprint FootprintOf(TownHouseSlot slot)
    {
        if (!slot) return Footprint.Default;
        if (footprintCache.TryGetValue(slot, out var cached)) return cached;
        var footprint = Footprint.Measure(slot.gameObject);
        footprintCache[slot] = footprint;
        return footprint;
    }

    private void ShowFootprint(Footprint footprint, Vector3 position, float rotation, bool valid)
    {
        overlay.Show(footprint.Centre(position, rotation), rotation, footprint.Half.x, footprint.Half.y, valid);
    }

    /// <summary>Height of the sea surface: the largest water renderer in the scene.</summary>
    private static float FindOceanLevel()
    {
        Renderer best = null;
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r.gameObject.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!best || r.bounds.size.x * r.bounds.size.z > best.bounds.size.x * best.bounds.size.z) best = r;
        }

        // Anything smaller than a few hundred metres across is a lake or a fountain, not the sea.
        return best && best.bounds.size.x > 500f ? best.bounds.center.y : float.NegativeInfinity;
    }

    /// <summary>Turns the building or town hall being moved, or the one being placed, as the R key does.</summary>
    public void RotateMove(float degrees)
    {
        if (Busy || (!IsMoving && !Armed.HasValue)) return;
        moveRotation = Mathf.Repeat(moveRotation + degrees, 360f);
    }

    private void ReadRotationKeys()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var step = alt ? 90f : rotationStep;
        moveRotation = Mathf.Repeat(moveRotation + (shift ? -step : step), 360f);
    }

    // ---- Moving the town hall ------------------------------------------------------------------

    /// <summary>Picks the town hall up. It follows the mouse until it is placed or the move is cancelled.</summary>
    public void StartHallMove()
    {
        var townHall = gameManager.Village.TownHall;
        if (!IsActive || Busy || !townHall)
        {
            return;
        }

        CancelMove();
        CancelHallMove();
        MovingHall = true;
        HallSelected = true;
        Selected = null;
        Armed = null;
        hallStartPosition = townHall.HallPosition;
        hallStartRotation = townHall.HallRotation;
        moveRotation = Mathf.Repeat(Mathf.Round(hallStartRotation / rotationStep) * rotationStep, 360f);
        lastSnapped = new Vector3(float.NaN, 0f, 0f);
        lastRotation = float.NaN;
        radiusCache.Clear();
        footprintCache.Clear();

        if (!overlay) overlay = PlacementOverlay.Create(cellSize);
        Changed?.Invoke();
    }

    /// <summary>Puts the town hall back where it was before the move started.</summary>
    public void CancelHallMove()
    {
        if (!MovingHall)
        {
            return;
        }

        var townHall = gameManager.Village.TownHall;
        if (townHall) townHall.MoveTo(hallStartPosition, hallStartRotation);
        MovingHall = false;
        MoveProblem = null;
        if (overlay) overlay.Hide();
        Changed?.Invoke();
    }

    private void UpdateHallMove()
    {
        var townHall = gameManager.Village.TownHall;
        if (Busy || !townHall)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            CancelHallMove();
            return;
        }

        ReadRotationKeys();

        if (!TryGetMousePoint(out var point))
        {
            return;
        }

        var snapped = new Vector3(Mathf.Round(point.x / cellSize) * cellSize, point.y, Mathf.Round(point.z / cellSize) * cellSize);
        if (snapped.x != lastSnapped.x || snapped.z != lastSnapped.z || moveRotation != lastRotation)
        {
            lastSnapped = snapped;
            lastRotation = moveRotation;
            townHall.MoveTo(snapped, moveRotation);
            MoveProblem = ValidateHall(townHall.HallPosition, moveRotation);
            MoveValid = MoveProblem == null;
            Changed?.Invoke();
            if (overlay) ShowHallArea(townHall, moveRotation, MoveValid);
        }

        if (overlay)
        {
            var area = townHall.Area;
            var centre = townHall.HallPosition + Quaternion.Euler(0f, moveRotation, 0f) * new Vector3(area.center.x, 0f, area.center.y);
            overlay.Show(centre, moveRotation, area.width / 2f, area.height / 2f, MoveValid);
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            if (MoveValid) _ = CommitHallMoveAsync();
            else Failed?.Invoke(MoveProblem);
        }
    }

    /// <summary>
    /// Null when the town hall can stand here, otherwise the reason it cannot. The whole town hall area
    /// (the biggest tier's ground) must fit: inside the village, clear of every plot, on dry and level
    /// enough ground.
    /// </summary>
    private string ValidateHall(Vector3 position, float rotation)
    {
        blockers.Clear();
        hallBlocks = false;
        var townHall = gameManager.Village.TownHall;
        var plaza = townHall.Plaza;

        var corners = townHall.AreaCorners(position, rotation);
        foreach (var corner in corners)
        {
            if (Flat(corner - plaza).magnitude > AreaRadius)
            {
                return Localization.UI_BUILD_HALL_OUTSIDE;
            }
        }

        foreach (var plot in Plots)
        {
            if (Footprint.Overlaps(HallFootprint(townHall), position, rotation,
                    FootprintOf(plot).Grown(spacing), plot.transform.position, plot.transform.eulerAngles.y))
            {
                blockers.Add(plot);
            }
        }

        if (blockers.Count > 0)
        {
            return WithBlockers(Localization.UI_BUILD_HALL_PLOT);
        }

        // Corners, edge middles and centre: dry ground, and not too uneven for the hall to sit on.
        var low = float.MaxValue;
        var high = float.MinValue;
        for (var i = 0; i < 9; ++i)
        {
            var sample = i < 4 ? corners[i]
                : i < 8 ? (corners[i - 4] + corners[(i - 3) % 4]) / 2f
                : (corners[0] + corners[2]) / 2f;
            if (IsWater(sample)) return Localization.UI_BUILD_PLACE_WATER;
            var y = townHall.GroundHeight(sample);
            low = Mathf.Min(low, y);
            high = Mathf.Max(high, y);
        }

        if (high - low > maxHallSlope)
        {
            return Localization.UI_BUILD_PLACE_STEEP;
        }

        return null;
    }

    private async Task CommitHallMoveAsync()
    {
        var townHall = gameManager.Village.TownHall;
        var position = townHall.HallPosition;
        var rotation = moveRotation;

        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.PlaceTownHallAsync(position.x, position.z, rotation))
            {
                Failed?.Invoke(gameManager.Village.TownHallPlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                CancelHallMove();
                return;
            }

            townHall.ApplyPlacement(position.x, position.z, rotation);
            MovingHall = false;
            MoveProblem = null;
            if (overlay) overlay.Hide();
        });
    }

    // ---- Previewing a turn -----------------------------------------------------------------------

    /// <summary>
    /// Shows where the selected building or the town hall would stand turned by
    /// <paramref name="degrees"/>, green or red, with the buildings in the way ringed. The Rotate
    /// tile shows it while the pointer is over it, so a turn can be judged before it is made, as a
    /// move can.
    /// </summary>
    public string PreviewRotation(float degrees)
    {
        if (!IsActive || Busy || IsMoving) return null;

        var townHall = gameManager.Village.TownHall;
        if (!overlay) overlay = PlacementOverlay.Create(cellSize);
        radiusCache.Clear();
        footprintCache.Clear();
        string problem = null;

        if (HallSelected && townHall)
        {
            var rotation = Mathf.Repeat(Mathf.Round(townHall.HallRotation / rotationStep) * rotationStep + degrees, 360f);
            problem = ValidateHall(townHall.HallPosition, rotation);
            ShowHallArea(townHall, rotation, problem == null, townHall.HallPosition);
        }
        else if (Selected)
        {
            var rotation = Mathf.Repeat(Mathf.Round(Selected.transform.eulerAngles.y / rotationStep) * rotationStep + degrees, 360f);
            problem = Validate(Selected, FootprintOf(Selected), Selected.transform.position, rotation);
            ShowFootprint(FootprintOf(Selected), Selected.transform.position, rotation, problem == null);
            RingBlockers();
        }
        else
        {
            return null;
        }

        previewing = true;
        return problem;
    }

    /// <summary>Takes the turn preview away, unless a move has the overlay now.</summary>
    public void EndPreview()
    {
        if (!previewing) return;
        previewing = false;
        if (!IsMoving && overlay) overlay.Hide();
    }

    private void ShowHallArea(TownHallManager townHall, float rotation, bool valid, Vector3? at = null)
    {
        var position = at ?? townHall.HallPosition;
        var area = townHall.Area;
        var centre = position + Quaternion.Euler(0f, rotation, 0f) * new Vector3(area.center.x, 0f, area.center.y);
        overlay.Show(centre, rotation, area.width / 2f, area.height / 2f, valid);
        RingBlockers();
    }

    private void RingBlockers()
    {
        if (!overlay) return;
        blockerRings.Clear();
        foreach (var plot in blockers)
        {
            // a little outside the footprint, so the outline is not hidden under the walls
            if (!plot) continue;
            var footprint = FootprintOf(plot).Grown(0.6f);
            var rotation = plot.transform.eulerAngles.y;
            blockerRings.Add((footprint.Centre(plot.transform.position, rotation), rotation, footprint.Half));
        }

        var townHall = gameManager.Village.TownHall;
        if (hallBlocks && townHall)
        {
            var hall = HallFootprint(townHall).Grown(0.3f);
            blockerRings.Add((hall.Centre(townHall.HallPosition, townHall.HallRotation), townHall.HallRotation, hall.Half));
        }
        overlay.ShowBlockers(blockerRings);
    }

    /// <summary>The problem, and which buildings are in the way: "In the way: Sawmill, Farm".</summary>
    private string WithBlockers(string problem)
    {
        var names = new List<string>();
        foreach (var plot in blockers)
        {
            var name = IsEmpty(plot) ? Localization.UI_BUILD_EMPTY_PLOT : Localization.HouseName(plot.SlotType);
            if (!names.Contains(name)) names.Add(name);
        }

        if (names.Count == 0) return problem;
        var shown = names.Count > 3 ? string.Join(", ", names.GetRange(0, 3)) + " " + string.Format(Localization.UI_BUILD_BLOCKED_MORE, names.Count - 3) : string.Join(", ", names);
        return problem + ". " + string.Format(Localization.UI_BUILD_BLOCKED_BY, shown);
    }

    /// <summary>Turns the town hall where it stands, after checking the turned area still fits.</summary>
    public async Task RotateHallInPlaceAsync(float degrees)
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall || Busy || IsMoving) return;

        radiusCache.Clear();
        footprintCache.Clear();
        var position = townHall.HallPosition;
        var rotation = Mathf.Repeat(Mathf.Round(townHall.HallRotation / rotationStep) * rotationStep + degrees, 360f);
        var problem = ValidateHall(position, rotation);
        if (problem != null)
        {
            Failed?.Invoke(problem);
            return;
        }

        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.PlaceTownHallAsync(position.x, position.z, rotation))
            {
                Failed?.Invoke(gameManager.Village.TownHallPlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                return;
            }

            townHall.ApplyPlacement(position.x, position.z, rotation);
        });
    }

    /// <summary>
    /// Buys the next town hall tier. The server checks the level and the resources and spends them; the
    /// new tier appears where the hall stands.
    /// </summary>
    public async Task UpgradeHallAsync()
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall || Busy || IsMoving || !townHall.CanUpgrade) return;

        long wood = townHall.NextWood, ore = townHall.NextOre, fish = townHall.NextFish, wheat = townHall.NextWheat;
        await Run(async () =>
        {
            var state = await gameManager.RavenNest.Village.UpgradeTownHallAsync();
            if (state == null || !state.Tier.HasValue)
            {
                Failed?.Invoke(Localization.UI_BUILD_HALL_UPGRADE_FAILED);
                return;
            }

            townHall.Spend(wood, ore, fish, wheat);
            gameManager.Village.ApplyTownHallTier(state);
            // a bigger hall, a bigger village: the grid grows to the new room
            if (IsActive) ShowVillageGrid();
        });
    }

    /// <summary>Puts a moved town hall back on the plaza.</summary>
    public async Task ResetHallPlacementAsync()
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall || Busy || !townHall.IsMoved) return;

        // The plaza must be clear too: plots may have been placed there after the hall moved away.
        radiusCache.Clear();
        footprintCache.Clear();
        var plaza = townHall.Plaza;
        foreach (var plot in Plots)
        {
            if (Footprint.Overlaps(HallFootprint(townHall), plaza, 0f,
                    FootprintOf(plot).Grown(spacing), plot.transform.position, plot.transform.eulerAngles.y))
            {
                Failed?.Invoke(Localization.UI_BUILD_HALL_PLOT);
                return;
            }
        }

        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.ResetTownHallPlacementAsync())
            {
                Failed?.Invoke(gameManager.Village.TownHallPlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                return;
            }

            townHall.ResetPlacement();
        });
    }

    // ---- Placing a new building --------------------------------------------------------------

    /// <summary>
    /// A building is armed: its ghost follows the mouse. Over an empty plot it snaps there and
    /// building uses that plot as it is. Anywhere else valid, a free plot is moved to the ghost and
    /// built on, so the streamer places buildings freely while the plot limit still holds.
    /// </summary>
    private void UpdatePlacing()
    {
        if (Busy) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            ClearSelection();
            return;
        }

        ReadRotationKeys();

        if (!TryGetMousePoint(out var point))
        {
            return;
        }

        var snapped = new Vector3(Mathf.Round(point.x / cellSize) * cellSize, point.y, Mathf.Round(point.z / cellSize) * cellSize);
        var target = EmptyPlotNear(point);

        if (target != SnapTarget || snapped.x != lastSnapped.x || snapped.z != lastSnapped.z || moveRotation != lastRotation)
        {
            SnapTarget = target;
            lastSnapped = snapped;
            lastRotation = moveRotation;

            if (target)
            {
                ghost.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
                MoveProblem = null;
                blockers.Clear();
                hallBlocks = false;
            }
            else
            {
                var ground = PlacementUtility.FindGroundPoint(snapped);
                ghost.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, moveRotation, 0f));
                MoveProblem = Validate(null, ghostFootprint, ground, moveRotation);
            }

            MoveValid = MoveProblem == null;
            PlacementOverlay.TintGhost(ghost, MoveValid);
            Changed?.Invoke();
            RingBlockers();
        }

        if (overlay)
        {
            ShowFootprint(ghostFootprint, ghost.transform.position, ghost.transform.eulerAngles.y, MoveValid);
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            if (!MoveValid)
            {
                Failed?.Invoke(MoveProblem);
                return;
            }

            _ = PlaceNewAsync(Armed.Value, SnapTarget, ghost.transform.position, ghost.transform.eulerAngles.y);
        }
    }

    /// <summary>The empty plot whose footprint the point falls inside, if any.</summary>
    private TownHouseSlot EmptyPlotNear(Vector3 point)
    {
        TownHouseSlot best = null;
        var bestDistance = float.MaxValue;
        foreach (var plot in Plots)
        {
            if (!IsEmpty(plot)) continue;
            var distance = Flat(point - plot.transform.position).magnitude;
            if (distance < RadiusOf(plot) && distance < bestDistance)
            {
                best = plot;
                bestDistance = distance;
            }
        }

        return best;
    }

    private async Task PlaceNewAsync(TownHouseSlotType type, TownHouseSlot target, Vector3 position, float rotation)
    {
        // Snapped onto a plot: build there, no move needed.
        if (target)
        {
            SetArmed(null);
            await BuildAsync(target, type);
            return;
        }

        // Anywhere else: take the nearest free plot to where the building is going.
        TownHouseSlot carrier = null;
        var nearest = float.MaxValue;
        foreach (var plot in Plots)
        {
            if (!IsEmpty(plot)) continue;
            var distance = Flat(plot.transform.position - position).magnitude;
            if (distance < nearest)
            {
                carrier = plot;
                nearest = distance;
            }
        }

        if (!carrier)
        {
            Failed?.Invoke(Localization.UI_BUILD_NO_FREE_PLOTS);
            return;
        }

        SetArmed(null);
        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.PlaceHouseAsync(carrier.Slot, position.x, position.z, rotation))
            {
                Failed?.Invoke(gameManager.Village.PlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                return;
            }

            carrier.ApplyPlacement(position.x, position.z, rotation);
            radiusCache.Remove(carrier);
        });

        if (carrier.IsMoved)
        {
            await BuildAsync(carrier, type);
        }
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private async Task CommitMoveAsync()
    {
        var slot = Moving;
        var position = slot.transform.position;
        var rotation = moveRotation;

        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.PlaceHouseAsync(slot.Slot, position.x, position.z, rotation))
            {
                Failed?.Invoke(gameManager.Village.PlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                CancelMove();
                return;
            }

            slot.ApplyPlacement(position.x, position.z, rotation);
            Moving = null;
            MoveProblem = null;
            if (overlay) overlay.Hide();
        });
    }

    /// <summary>
    /// Turns a plot where it stands, by <paramref name="degrees"/>, after checking the turned
    /// footprint still fits. The Rotate tile on the building panel uses this.
    /// </summary>
    public async Task RotateInPlaceAsync(TownHouseSlot slot, float degrees)
    {
        if (!slot || Busy || Moving) return;

        radiusCache.Clear();
        footprintCache.Clear();
        var position = slot.transform.position;
        var rotation = Mathf.Repeat(Mathf.Round(slot.transform.eulerAngles.y / rotationStep) * rotationStep + degrees, 360f);
        var problem = Validate(slot, FootprintOf(slot), position, rotation);
        if (problem != null)
        {
            Failed?.Invoke(problem);
            return;
        }

        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.PlaceHouseAsync(slot.Slot, position.x, position.z, rotation))
            {
                Failed?.Invoke(gameManager.Village.PlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                return;
            }

            slot.ApplyPlacement(position.x, position.z, rotation);
        });
    }

    /// <summary>How many plots the village has, and how many of them are empty.</summary>
    public (int free, int total) PlotCounts()
    {
        var plots = Plots;
        var free = 0;
        foreach (var plot in plots) if (IsEmpty(plot)) free++;
        return (free, plots.Count);
    }

    /// <summary>Puts a moved plot back on its original position.</summary>
    public async Task ResetPlacementAsync(TownHouseSlot slot)
    {
        if (!slot || Busy || !slot.IsMoved) return;
        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.ResetPlacementAsync(slot.Slot))
            {
                Failed?.Invoke(gameManager.Village.PlacementSupported
                    ? Localization.UI_BUILD_PLACE_REJECTED
                    : Localization.UI_BUILD_PLACE_UNSUPPORTED);
                return;
            }

            slot.ResetPlacement();
        });
    }

    // ---- Actions -------------------------------------------------------------------------

    // ---- Settling plots ---------------------------------------------------------------------

    // The ground a plot needs whatever is built on it later: the biggest house type, about 10 by 10 m.
    private static readonly Footprint HouseRoom = new Footprint(new Vector2(5f, 5f), Vector2.zero);

    /// <summary>
    /// Moves plots with no stored position off spots that are taken. The default layout changed:
    /// existing villages keep their houses where the old layout put them (the server stores those
    /// positions), and a plot that opens later at its new default could land on one of them, or on
    /// the town hall or in water. Such a plot goes to the nearest free spot instead. An empty one
    /// only for this session; one with a building has the spot saved, so the building stays put.
    /// Being outside the village area is not a reason: those plots stay, marked, like any other.
    /// </summary>
    public void SettlePlots()
    {
        if (IsMoving || Busy) return;
        var village = gameManager ? gameManager.Village : null;
        if (!village || !village.TownHall) return;
        var houses = village.TownHouses;
        var slots = houses.Slots;
        if (slots == null) return;

        var count = Mathf.Min(houses.SlotCount, slots.Length);
        for (var i = 0; i < count; ++i)
        {
            var slot = slots[i];
            if (!slot || !slot.gameObject.activeInHierarchy || slot.IsMoved) continue;

            var room = IsEmpty(slot) ? HouseRoom : FootprintOf(slot);
            var position = slot.transform.position;
            var rotation = slot.transform.eulerAngles.y;
            // a nudged plot first tries its own spot again: it may be free now
            if (slot.IsNudged)
            {
                var original = slot.DefaultPosition;
                if (Validate(slot, room, original, rotation, false) == null)
                {
                    slot.ResetPlacement();
                    continue;
                }
            }

            if (Validate(slot, room, position, rotation, false) == null) continue;
            if (!TryFindFreeSpot(slot, room, position, out var spot, out var yaw)) continue;

            slot.Nudge(spot.x, spot.z, yaw);
            if (!IsEmpty(slot)) SaveNudge(slot);
        }

        blockers.Clear();
        hallBlocks = false;
    }

    /// <summary>The nearest spot round <paramref name="from"/> where the plot fits, facing the plaza.</summary>
    private bool TryFindFreeSpot(TownHouseSlot slot, Footprint room, Vector3 from, out Vector3 spot, out float yaw)
    {
        var plaza = gameManager.Village.TownHall.Plaza;
        for (var r = 2f; r <= 40f; r += 2f)
        {
            var steps = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / 2f));
            for (var k = 0; k < steps; ++k)
            {
                var a = k * Mathf.PI * 2f / steps;
                var flat = from + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
                var candidate = PlacementUtility.FindGroundPoint(new Vector3(flat.x, from.y + 20f, flat.z));
                // the front (local -Z) to the plaza, as the default layout has them
                var facing = Mathf.Round(Mathf.Atan2(candidate.x - plaza.x, candidate.z - plaza.z) * Mathf.Rad2Deg / rotationStep) * rotationStep;
                if (Validate(slot, room, candidate, facing, false) != null) continue;
                spot = candidate;
                yaw = facing;
                return true;
            }
        }

        spot = default;
        yaw = 0f;
        return false;
    }

    /// <summary>Stores a nudged plot's spot, so the building on it stays there.</summary>
    private async void SaveNudge(TownHouseSlot slot)
    {
        if (!gameManager.Village.PlacementSupported || !slot.IsNudged) return;
        var position = slot.transform.position;
        var rotation = slot.transform.eulerAngles.y;
        try
        {
            if (await gameManager.RavenNest.Village.PlaceHouseAsync(slot.Slot, position.x, position.z, rotation))
            {
                slot.ApplyPlacement(position.x, position.z, rotation);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Could not save the spot of plot " + slot.Slot + ": " + exc.Message);
        }
    }

    // ---- Actions -------------------------------------------------------------------------

    /// <summary>Builds <paramref name="type"/> on the plot, or replaces what is there.</summary>
    public async Task BuildAsync(TownHouseSlot slot, TownHouseSlotType type)
    {
        if (!slot || Busy) return;
        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.BuildHouseAsync(slot.Slot, (int)type))
            {
                Failed?.Invoke("The server did not accept the building. Try again in a moment.");
                return;
            }

            // The server keeps the resident when the building changes, so the client does too.
            // TownHouseManager.SetHouse would clear it.
            var prefab = Catalogue.FirstOrDefault(x => x.Type == type);
            slot.SetHouse(new VillageHouseInfo { Slot = slot.Slot, Type = (int)type, OwnerUserId = slot.OwnerUserId }, prefab);
            // built on a plot that was moved off a taken spot: keep the building where it is
            if (slot.IsNudged) SaveNudge(slot);
            Selected = slot;
        });
    }

    public async Task DemolishAsync(TownHouseSlot slot)
    {
        if (!slot || Busy || IsEmpty(slot)) return;
        await Run(async () =>
        {
            if (!await gameManager.RavenNest.Village.RemoveHouseAsync(slot.Slot))
            {
                Failed?.Invoke("The server did not accept the demolition. Try again in a moment.");
                return;
            }

            gameManager.Village.TownHouses.SetHouse(slot, TownHouseSlotType.Empty);
        });
    }

    public async Task AssignAsync(TownHouseSlot slot, PlayerController player)
    {
        if (!slot || !player || Busy) return;
        await Run(async () =>
        {
            // Bots and test players are not real users, so the server has nothing to store; the
            // old icon bar assigned them locally only, and so does this.
            var local = player.IsBot || (player.PlatformId != null && player.PlatformId.StartsWith("#"));
            if (!local && !await gameManager.RavenNest.Village.AssignPlayerAsync(slot.Slot, player.Id))
            {
                Failed?.Invoke("The server did not accept the resident. Try again in a moment.");
                return;
            }

            gameManager.Village.TownHouses.SetOwner(slot, player);
            gameManager.villageBoostLabel.Update();
        });
    }

    private async Task Run(Func<Task> action)
    {
        Busy = true;
        Changed?.Invoke();
        try
        {
            await action();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Build mode action failed: " + exc);
            Failed?.Invoke("Something went wrong. Check the log for details.");
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    // ---- Queries for the interface -------------------------------------------------------

    /// <summary>The experience bonus, in percent, a player would give living in a building of this type.</summary>
    public static float BonusFor(PlayerController player, TownHouseSlotType type)
    {
        if (!player || player.Stats == null) return 0f;
        var skill = GameMath.GetSkillByHouseType(player.Stats, type);
        return skill != null ? GameMath.CalculateHouseExpBonus(skill) : 0f;
    }

    /// <summary>Players ranked by the bonus they would give in a building of this type, best first.</summary>
    public IReadOnlyList<PlayerController> RankResidents(TownHouseSlotType type)
    {
        var players = gameManager.Players.GetAllPlayers();
        return players.Where(x => x).OrderByDescending(x => BonusFor(x, type)).ToList();
    }

    /// <summary>The plot a player already lives in, if any. Assigning them elsewhere moves them.</summary>
    public TownHouseSlot HomeOf(PlayerController player)
    {
        var houses = gameManager.Village.TownHouses;
        return houses && player ? houses.FindSlotByOwner(player) : null;
    }

    /// <summary>The current total bonus per building type.</summary>
    public IReadOnlyDictionary<TownHouseSlotType, float> BonusByType()
    {
        var result = new Dictionary<TownHouseSlotType, float>();
        foreach (var bonus in gameManager.Village.GetGroupedExpBonuses())
        {
            if (bonus.Bonus > 0) result[bonus.SlotType] = bonus.Bonus;
        }

        return result;
    }
}
