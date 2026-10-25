using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Placing, moving, turning and removing decor in build mode. See docs/village-decor-design.md.
/// </summary>
/// <remarks>
/// <para>
/// <b>Placing.</b> Pick a piece in the Decor tray: it follows the mouse, on the half-metre grid (Alt places it
/// freely). R turns it (Shift for small steps). A click places one and keeps the piece in hand for the next; Esc
/// or a right click stops. Trees, bushes and rocks may not stand on buildings or the town hall; flowers and grass go
/// anywhere, and hide under whatever is built on them later.
/// </para>
/// <para>
/// <b>Editing.</b> Click a piece (the island's own trees and flowers too) to select it: Move picks it up (a click
/// puts it down, Esc puts it back), Rotate turns it, Remove takes it away for half its worth.
/// </para>
/// <para>
/// The piece in hand is the real model, never tinted (build mode shows the true look); a ring under it says whether
/// it can stand there.
/// </para>
/// </remarks>
public class VillageDecorTool : MonoBehaviour
{
    private const float Grid = 0.5f;
    private const float TurnStep = 45f;
    private const float FineTurnStep = 15f;

    private VillageBuildMode mode;
    private GameManager gameManager;

    private GameObject ghost;
    private VillageDecorItem ghostItem;
    private float rotation;
    private Vector3 cursor;
    private bool hasCursor;

    // the selected piece's place before a move, to put it back
    private Vector3 moveFrom;
    private Quaternion moveFromRotation;

    /// <summary>The piece in hand from the tray, or null.</summary>
    public string Armed { get; private set; }

    /// <summary>The piece in hand is one of the viewers' gifts: placed for free.</summary>
    public bool ArmedGift { get; private set; }
    public VillageDecorItem Selected { get; private set; }
    public bool Moving { get; private set; }
    public bool Busy { get; private set; }

    /// <summary>Why the piece in hand (or being moved) cannot stand where it is, or null.</summary>
    public string Problem { get; private set; }

    public event Action Changed;
    public event Action<string> Failed;

    private VillageDecor Decor => gameManager && gameManager.Village ? gameManager.Village.Decor : null;

    private void Awake()
    {
        mode = GetComponent<VillageBuildMode>();
        gameManager = FindAnyObjectByType<GameManager>();
    }

    // ---- Starting and stopping -----------------------------------------------------------------

    public void Arm(string piece, bool gift = false)
    {
        var decor = Decor;
        var info = decor != null ? decor.PieceOf(piece) : null;
        var prefab = decor != null ? decor.PrefabFor(piece) : null;
        // a gift is the viewer's: placeable whatever the village has unlocked
        if (info == null || !prefab || Busy || (gift ? info.Gifts <= 0 : !info.Unlocked)) return;

        Stop();
        Armed = piece;
        ArmedGift = gift;
        ghost = MakeGhost(prefab);
        // measured as the placed piece will be
        if (!ghost.TryGetComponent(out ghostItem)) ghostItem = ghost.AddComponent<VillageDecorItem>();
        ghostItem.enabled = false;
        rotation = UnityEngine.Random.Range(0, 8) * TurnStep; // a little variety by default
        hasCursor = false;
        Changed?.Invoke();
    }

    public void Select(VillageDecorItem item)
    {
        Stop();
        Selected = item;
        Changed?.Invoke();
    }

    /// <summary>Drops whatever is in hand, puts a piece being moved back, and lets go of the selection.</summary>
    public void Stop()
    {
        var changed = Armed != null || Selected || Moving;
        if (Moving && Selected) Selected.transform.SetPositionAndRotation(moveFrom, moveFromRotation);
        Moving = false;
        Armed = null;
        ArmedGift = false;
        Selected = null;
        Problem = null;
        if (ghost) Destroy(ghost);
        ghost = null;
        if (mode) mode.HideDecorRing();
        if (changed) Changed?.Invoke();
    }

    public void StartMove()
    {
        if (!Selected || Busy) return;
        Moving = true;
        moveFrom = Selected.transform.position;
        moveFromRotation = Selected.transform.rotation;
        rotation = Selected.transform.eulerAngles.y;
        hasCursor = false;
        Changed?.Invoke();
    }

    // ---- Input, from build mode's Update ---------------------------------------------------------

    /// <summary>Handles the frame's input while placing or moving decor. True when it took the frame.</summary>
    public bool HandleInput()
    {
        if (Armed == null && !Moving)
        {
            if (Selected && BuildInput.KeyDown(KeyCode.Escape))
            {
                EscapeKey.Claim();
                Stop();
                return true;
            }
            return false;
        }

        if (BuildInput.KeyDown(KeyCode.Escape) | BuildInput.RightClicked())
        {
            EscapeKey.Claim();
            if (Moving)
            {
                // put back, keep it selected
                Selected.transform.SetPositionAndRotation(moveFrom, moveFromRotation);
                Moving = false;
                Problem = null;
                mode.HideDecorRing();
                Changed?.Invoke();
            }
            else Stop();
            return true;
        }

        if (BuildInput.KeyDown(KeyCode.R)) Turn(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? FineTurnStep : TurnStep);

        Follow();

        if (Input.GetMouseButtonDown(0) && !Shinobytes.UI.HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement() && hasCursor && !Busy)
        {
            if (Problem != null) Failed?.Invoke(Problem);
            else if (Moving) _ = FinishMoveAsync();
            else _ = PlaceAsync();
        }

        return true;
    }

    /// <summary>Turns the piece in hand or being moved, as the R key does.</summary>
    public void Turn(float degrees)
    {
        rotation = (rotation + degrees) % 360f;
        if (Moving && Selected) Selected.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        if (ghost) ghost.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
    }

    /// <summary>Moves the piece in hand (or being moved) to the mouse and checks the spot.</summary>
    private void Follow()
    {
        if (!mode.TryGetMousePoint(out var point)) return;
        var free = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (!free)
        {
            point.x = Mathf.Round(point.x / Grid) * Grid;
            point.z = Mathf.Round(point.z / Grid) * Grid;
        }

        var ground = PlacementUtility.FindGroundPoint(point);
        cursor = ground;
        hasCursor = true;

        var target = Moving ? Selected ? Selected.gameObject : null : ghost;
        if (!target) return;
        target.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, rotation, 0f));

        var small = Moving ? Selected.Small : (Decor?.PieceOf(Armed)?.Small ?? false);
        var item = Moving ? Selected : ghostItem;
        Problem = mode.ValidateDecor(ground, item.Shape, item.ShapeRotation, small);
        mode.ShowDecorRing(ground, item.Shape, item.ShapeRotation, Problem == null);
    }

    // ---- Server --------------------------------------------------------------------------------

    private async System.Threading.Tasks.Task PlaceAsync()
    {
        var decor = Decor;
        if (decor == null || Armed == null) return;
        Busy = true;
        Changed?.Invoke();
        try
        {
            var gift = ArmedGift;
            var (item, error) = await decor.PlaceAsync(Armed, cursor, rotation, gift);
            if (item == null) Failed?.Invoke(ErrorText(error));
            // the last of a gift placed: nothing left in hand
            else if (gift && (decor.PieceOf(item.Piece)?.Gifts ?? 0) <= 0) Stop();
            // the next one turned differently, as hands place them
            else Turn(UnityEngine.Random.Range(1, 8) * TurnStep);
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    private async System.Threading.Tasks.Task FinishMoveAsync()
    {
        var decor = Decor;
        var item = Selected;
        if (decor == null || !item) return;
        Busy = true;
        Moving = false;
        mode.HideDecorRing();
        Changed?.Invoke();
        try
        {
            var error = await decor.MoveAsync(item, item.transform.position, item.transform.eulerAngles.y, moveFrom, moveFromRotation);
            if (error != null) Failed?.Invoke(ErrorText(error));
        }
        finally
        {
            Busy = false;
            Problem = null;
            Changed?.Invoke();
        }
    }

    /// <summary>Turns the selected piece in place and saves it.</summary>
    public async System.Threading.Tasks.Task RotateSelectedAsync(float degrees)
    {
        var decor = Decor;
        var item = Selected;
        if (decor == null || !item || Busy || Moving) return;
        var before = item.transform.position;
        var beforeRotation = item.transform.rotation;
        item.transform.rotation = Quaternion.Euler(0f, item.transform.eulerAngles.y + degrees, 0f);
        Busy = true;
        Changed?.Invoke();
        try
        {
            var error = await decor.MoveAsync(item, before, item.transform.eulerAngles.y, before, beforeRotation);
            if (error != null) Failed?.Invoke(ErrorText(error));
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Removes the selected piece and says what it gave back.</summary>
    public async System.Threading.Tasks.Task RemoveSelectedAsync()
    {
        var decor = Decor;
        var item = Selected;
        if (decor == null || !item || Busy) return;
        var name = decor.NameOf(item.Piece);
        var gift = item.Gift;
        Busy = true;
        Changed?.Invoke();
        try
        {
            var (result, error) = await decor.RemoveAsync(item);
            if (result == null) { Failed?.Invoke(ErrorText(error)); return; }
            Selected = null;
            var refund = RefundText(result.RefundWood, result.RefundOre, result.RefundFish, result.RefundWheat, result.RefundCoins);
            Failed?.Invoke(gift ? string.Format(Localization.UI_DECOR_GIFT_RETURNED, name) : string.IsNullOrEmpty(refund)
                ? string.Format(Localization.UI_DECOR_REMOVED, name)
                : string.Format(Localization.UI_DECOR_REMOVED_REFUND, name, refund));
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    // ---- Helpers -------------------------------------------------------------------------------

    /// <summary>The real model to carry around: no behaviour, no colliders, and not tinted.</summary>
    private static GameObject MakeGhost(GameObject prefab)
    {
        var copy = Instantiate(prefab);
        copy.name = prefab.name + " (Placing)";
        foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        return copy;
    }

    public static string ErrorText(string error)
    {
        switch (error)
        {
            case "resources": return Localization.UI_DECOR_ERROR_RESOURCES;
            case "locked": return Localization.UI_DECOR_ERROR_LOCKED;
            case "removed": return Localization.UI_DECOR_ERROR_REMOVED;
            case "nogift": return Localization.UI_DECOR_ERROR_NOGIFT;
            default: return Localization.UI_DECOR_ERROR;
        }
    }

    /// <summary>"10 wood, 5 wheat": only what is there, in the road prices' words.</summary>
    public static string RefundText(double wood, double ore, double fish, double wheat, double coins)
    {
        var parts = new System.Collections.Generic.List<string>();
        void Add(double amount, string format) { if (amount > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, format, amount.ToString("N0", CultureInfo.InvariantCulture))); }
        Add(wood, Localization.UI_ROAD_COST_WOOD);
        Add(ore, Localization.UI_ROAD_COST_ORE);
        Add(fish, Localization.UI_ROAD_COST_FISH);
        Add(wheat, Localization.UI_ROAD_COST_WHEAT);
        Add(coins, Localization.UI_ROAD_COST_COINS);
        return string.Join(", ", parts);
    }

    public static string CostText(RavenNest.SDK.Endpoints.DecorPiece piece) =>
        piece == null ? string.Empty : RefundText(piece.Wood, piece.Ore, piece.Fish, piece.Wheat, piece.Coins);
}
