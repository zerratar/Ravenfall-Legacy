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

    /// <summary>Raised when build mode turns on or off.</summary>
    public event Action<bool> ActiveChanged;

    /// <summary>Raised when the selection, armed building or village contents change.</summary>
    public event Action Changed;

    /// <summary>Raised with a short message when an action fails, for the interface to show.</summary>
    public event Action<string> Failed;

    private GameManager gameManager;
    private Camera worldCamera;
    private readonly RaycastHit[] hits = new RaycastHit[32];

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
