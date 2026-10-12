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
    [SerializeField] private Vector3 overviewOffset = new Vector3(0f, 42f, -38f);

    [Header("Placement")]
    [Tooltip("Grid cell size in metres. Positions snap to it.")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Degrees per press of R. Hold Alt for quarter turns.")]
    [SerializeField] private float rotationStep = 15f;

    [Tooltip("How far past the outermost original plot buildings may go, in metres.")]
    [SerializeField] private float villageMargin = 12f;

    [Tooltip("Largest height difference across a footprint before the ground counts as too steep.")]
    [SerializeField] private float maxSlope = 2f;

    [Tooltip("Gap kept between buildings, in metres.")]
    [SerializeField] private float spacing = 0.5f;

    public static VillageBuildMode Instance { get; private set; }

    /// <summary>Whether build mode is on.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The plot the streamer has selected, if any.</summary>
    public TownHouseSlot Selected { get; private set; }

    /// <summary>
    /// A building type picked from the tray with no plot selected. Clicking an empty plot builds it.
    /// </summary>
    public TownHouseSlotType? Armed { get; private set; }

    /// <summary>True while a server call is in flight; the interface disables its actions.</summary>
    public bool Busy { get; private set; }

    /// <summary>The plot being moved, if any.</summary>
    public TownHouseSlot Moving { get; private set; }

    /// <summary>Whether the plot being moved can be dropped where it is.</summary>
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
    private Vector3 moveStartPosition;
    private Quaternion moveStartRotation;
    private float moveRotation;
    private Vector3 lastSnapped = new Vector3(float.NaN, 0f, 0f);
    private float lastRotation = float.NaN;
    private readonly Dictionary<TownHouseSlot, float> radiusCache = new Dictionary<TownHouseSlot, float>();

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
        FlyToVillage();

        IsActive = true;
        Selected = null;
        Armed = null;
        ActiveChanged?.Invoke(true);
        Changed?.Invoke();
    }

    public void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        CancelMove();
        IsActive = false;
        Selected = null;
        Armed = null;
        gameManager.Camera.ReleaseFreeCamera();
        ActiveChanged?.Invoke(false);
        Changed?.Invoke();
    }

    private void FlyToVillage()
    {
        var townHall = gameManager.Village.TownHall;
        var cam = Camera.main;
        if (!townHall || !cam)
        {
            return;
        }

        var centre = townHall.transform.position;
        cam.transform.position = centre + overviewOffset;
        cam.transform.LookAt(centre);
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
        if (!IsActive)
        {
            return;
        }

        // A building armed from the tray goes straight onto an empty plot.
        if (Armed.HasValue && slot && IsEmpty(slot))
        {
            var type = Armed.Value;
            Armed = null;
            _ = BuildAsync(slot, type);
            return;
        }

        Selected = slot;
        Armed = null;
        Changed?.Invoke();
    }

    public void ClearSelection()
    {
        Selected = null;
        Armed = null;
        Changed?.Invoke();
    }

    /// <summary>
    /// A card was picked in the tray. With an empty plot selected it builds there; with a built
    /// plot selected it changes the building; with nothing selected it arms the card.
    /// </summary>
    public void Choose(TownHouseSlotType type)
    {
        if (!IsActive || Busy)
        {
            return;
        }

        if (Selected)
        {
            _ = BuildAsync(Selected, type);
            return;
        }

        Armed = Armed == type ? (TownHouseSlotType?)null : type;
        Changed?.Invoke();
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

        if (Moving)
        {
            UpdateMove();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Armed.HasValue || Selected) ClearSelection();
            else Exit();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            var slot = PlotUnderMouse();
            if (slot) Select(slot);
            else if (Selected || Armed.HasValue) ClearSelection();
        }
    }

    private TownHouseSlot PlotUnderMouse()
    {
        if (!worldCamera || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        if (!worldCamera) return null;

        var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        var count = Physics.RaycastNonAlloc(ray, hits, 2000f);
        TownHouseSlot best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < count; ++i)
        {
            var hit = hits[i];
            var slot = hit.collider.GetComponentInParent<TownHouseSlot>();
            if (slot && hit.distance < bestDistance)
            {
                best = slot;
                bestDistance = hit.distance;
            }
        }

        return best;
    }

    // ---- Moving --------------------------------------------------------------------------

    /// <summary>Picks a plot up. It follows the mouse until it is placed or the move is cancelled.</summary>
    public void StartMove(TownHouseSlot slot)
    {
        if (!IsActive || Busy || !slot)
        {
            return;
        }

        CancelMove();
        Moving = slot;
        Selected = slot;
        Armed = null;
        moveStartPosition = slot.transform.position;
        moveStartRotation = slot.transform.rotation;
        moveRotation = Mathf.Round(slot.transform.eulerAngles.y / rotationStep) * rotationStep;
        lastSnapped = new Vector3(float.NaN, 0f, 0f);
        lastRotation = float.NaN;
        radiusCache.Clear();

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

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelMove();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var step = alt ? 90f : rotationStep;
            moveRotation = Mathf.Repeat(moveRotation + (shift ? -step : step), 360f);
        }

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

            MoveProblem = Validate(Moving, ground, moveRotation);
            MoveValid = MoveProblem == null;
            Changed?.Invoke();
        }

        if (overlay)
        {
            overlay.Show(Moving.transform.position, moveRotation, RadiusOf(Moving), MoveValid);
        }

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement())
        {
            if (MoveValid) _ = CommitMoveAsync();
            else Failed?.Invoke(MoveProblem);
        }
    }

    /// <summary>
    /// Where the mouse points on the village. The ray is cut against a horizontal plane at the
    /// town hall's height rather than against the scene, so trees, houses and characters under the
    /// cursor do not make the building jump; the ground height is found straight down from there.
    /// </summary>
    private bool TryGetMousePoint(out Vector3 point)
    {
        point = default;
        if (!worldCamera || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        var townHall = gameManager.Village.TownHall;
        if (!worldCamera || !townHall) return false;

        var plane = new Plane(Vector3.up, townHall.transform.position);
        var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out var distance)) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    /// <summary>Null when the plot can stand here, otherwise the reason it cannot.</summary>
    private string Validate(TownHouseSlot slot, Vector3 position, float rotation)
    {
        var townHall = gameManager.Village.TownHall;
        var centre = townHall ? townHall.transform.position : position;
        var radius = RadiusOf(slot);

        // Inside the village: no further out than the outermost original plot, plus a margin.
        var reach = 0f;
        var allSlots = gameManager.Village.TownHouses.Slots;
        foreach (var other in allSlots)
        {
            if (other) reach = Mathf.Max(reach, Flat(other.DefaultPosition - centre).magnitude);
        }

        if (Flat(position - centre).magnitude > reach + villageMargin)
        {
            return Localization.UI_BUILD_PLACE_OUTSIDE;
        }

        // Clear of the town hall.
        var hallRadius = TownHallRadius();
        if (hallRadius > 0f && Flat(position - centre).magnitude < hallRadius + radius + spacing)
        {
            return Localization.UI_BUILD_PLACE_TOWN_HALL;
        }

        // Clear of every other plot, built or not.
        foreach (var other in Plots)
        {
            if (other == slot) continue;
            if (Flat(position - other.transform.position).magnitude < radius + RadiusOf(other) + spacing)
            {
                return Localization.UI_BUILD_PLACE_OVERLAP;
            }
        }

        // On ground, not water, and not too steep.
        var low = float.MaxValue;
        var high = float.MinValue;
        var turn = Quaternion.Euler(0f, rotation, 0f);
        for (var i = 0; i < 5; ++i)
        {
            var offset = i == 4 ? Vector3.zero : turn * new Vector3((i & 1) == 0 ? -radius : radius, 0f, (i & 2) == 0 ? -radius : radius);
            var sample = position + offset;
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

    private static bool IsWater(Vector3 point)
    {
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

    private float TownHallRadius()
    {
        var townHall = gameManager.Village.TownHall;
        if (!townHall) return 0f;
        var renderers = townHall.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; ++i) bounds.Encapsulate(renderers[i].bounds);
        return Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f;
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
        var position = slot.transform.position;
        var rotation = Mathf.Repeat(Mathf.Round(slot.transform.eulerAngles.y / rotationStep) * rotationStep + degrees, 360f);
        var problem = Validate(slot, position, rotation);
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
