using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Drawing and editing roads in build mode.
/// </summary>
/// <remarks>
/// <para>
/// <b>Drawing.</b> Pick a road type in the tray, then click to place points; the road curves
/// through them. Points snap to the 1 m grid and onto the points of other roads close by, so roads
/// join; Alt places them freely, Shift keeps the direction to 15 degree steps. Backspace takes the
/// last point back, Enter, a right click or a click on the last point finishes, Esc stops. The
/// road panel shows the length and cost as it grows, red when the village cannot pay.
/// </para>
/// <para>
/// <b>Editing.</b> Click a road to select it. Drag a point to move it, Ctrl click the road to add
/// a point, right click a point to take it away, pick another type in the panel. Nothing is spent
/// until Apply: the panel shows what the change costs, or gives back, first. Cancel or Esc puts the
/// road back as it was. A road under a building is reached by clicking the building again.
/// </para>
/// <para>
/// Roads through a building or the town hall are drawn red while build mode is on. They are never
/// moved or removed for it: the streamer decides.
/// </para>
/// </remarks>
public class VillageRoadTool : MonoBehaviour
{
    private const float HandlePixels = 16f;
    private const float AngleStep = 15f;
    private const float JoinDistance = 1.2f;
    private const int HandleRingPoints = 24;

    private VillageBuildMode mode;
    private GameManager gameManager;

    // drawing
    private readonly List<Vector2> drawPoints = new List<Vector2>();
    private VillageRoads.Road preview;
    private Vector2 cursor;
    private bool hasCursor;

    // editing: the selected road's points and type as they are being changed, and as saved
    private List<Vector2> savedPoints;
    private int savedType;
    private int dragIndex = -1;

    private readonly List<LineRenderer> handles = new List<LineRenderer>();
    private Material handleMaterial;

    /// <summary>The road type being drawn, or null.</summary>
    public int? DrawType { get; private set; }
    public VillageRoads.Road Selected { get; private set; }
    public bool Busy { get; private set; }
    public bool Drawing => DrawType.HasValue;

    /// <summary>The road being drawn: its length so far and whether the village can pay for it.</summary>
    public float DrawLength { get; private set; }
    public bool DrawAffordable { get; private set; } = true;
    public int DrawPointCount => drawPoints.Count;

    /// <summary>The selected road has changes that are not saved yet.</summary>
    public bool Dirty => Selected != null && (Selected.Type != savedType || !SamePoints(Selected.Points, savedPoints));

    /// <summary>The selected road's length as saved, and as it would be after Apply.</summary>
    public float SavedLength => savedPoints != null ? VillageRoads.PaidLength(savedPoints) : 0f;
    public float EditLength => Selected != null ? VillageRoads.PaidLength(Selected.Points) : 0f;
    public int SavedType => savedType;

    public event Action Changed;
    public event Action<string> Failed;

    private VillageRoads Roads => gameManager && gameManager.Village ? gameManager.Village.Roads : null;

    private void Awake()
    {
        mode = GetComponent<VillageBuildMode>();
        gameManager = FindAnyObjectByType<GameManager>();
        // a building moved, built or removed: which roads run through one may have changed
        if (mode) mode.Changed += () => { if (mode.IsActive && !mode.IsMoving) RefreshFlags(true); };
    }

    // ---- Drawing ---------------------------------------------------------------------------------

    public void StartDrawing(int type)
    {
        var roads = Roads;
        var info = roads != null ? roads.TypeOf(type) : null;
        if (info == null || !info.Unlocked || Busy) return;

        Deselect();
        DrawType = type;
        drawPoints.Clear();
        hasCursor = false;
        UpdatePreview();
        Changed?.Invoke();
    }

    public void CancelDrawing()
    {
        if (!Drawing) return;
        DrawType = null;
        drawPoints.Clear();
        HidePreview();
        Changed?.Invoke();
    }

    /// <summary>Another type for the road being drawn; the points stay.</summary>
    public void SetDrawType(int type)
    {
        var info = Roads != null ? Roads.TypeOf(type) : null;
        if (!Drawing || info == null || !info.Unlocked || DrawType == type) return;
        DrawType = type;
        UpdatePreview();
    }

    /// <summary>Takes the last placed point back.</summary>
    public void UndoPoint()
    {
        if (!Drawing || drawPoints.Count == 0) return;
        drawPoints.RemoveAt(drawPoints.Count - 1);
        UpdatePreview();
    }

    /// <summary>Builds the road drawn so far, from the panel's Finish.</summary>
    public void Finish()
    {
        if (!Drawing || Busy) return;
        if (drawPoints.Count >= 2) _ = FinishDrawing();
        else Failed?.Invoke(Localization.UI_ROAD_TWO_POINTS);
    }

    // ---- Editing ---------------------------------------------------------------------------------

    public void Select(VillageRoads.Road road)
    {
        if (road == null || Busy) return;
        CancelDrawing();
        // the same road again keeps its changes, and what it was before them: what is saved is
        // only ever what the server has
        if (Selected == road) return;
        if (Selected != null) Revert();
        Selected = road;
        savedPoints = new List<Vector2>(road.Points);
        savedType = road.Type;
        DrawHandles();
        Changed?.Invoke();
    }

    /// <summary>Lets go of the road. Changes not applied are put back.</summary>
    public void Deselect()
    {
        if (Selected == null) return;
        Revert();
        Selected = null;
        savedPoints = null;
        dragIndex = -1;
        HideHandles();
        Changed?.Invoke();
    }

    /// <summary>Another type for the selected road, shown at once and saved with Apply.</summary>
    public void SetType(int type)
    {
        var road = Selected;
        var roads = Roads;
        if (road == null || roads == null || Busy || road.Type == type) return;
        var info = roads.TypeOf(type);
        if (info == null || !info.Unlocked) return;

        road.Type = type;
        roads.Rebuild(road);
        Changed?.Invoke();
    }

    /// <summary>Puts the selected road back as it is saved.</summary>
    public void CancelEdit()
    {
        if (!Dirty) return;
        Revert();
        DrawHandles();
        Changed?.Invoke();
    }

    /// <summary>Saves the changes: the server charges the difference or gives it back.</summary>
    public async void Apply()
    {
        var road = Selected;
        var roads = Roads;
        if (road == null || roads == null || Busy || !Dirty) return;
        if (!EditAffordable())
        {
            Failed?.Invoke(ErrorText("resources"));
            return;
        }

        Busy = true;
        Changed?.Invoke();
        try
        {
            var error = await roads.ChangeAsync(road, road.Type, new List<Vector2>(road.Points), savedPoints, savedType);
            if (error != null)
            {
                Failed?.Invoke(ErrorText(error));
            }

            // saved, or put back by ChangeAsync on failure: either way, this is now the saved road
            savedPoints = new List<Vector2>(road.Points);
            savedType = road.Type;
            if (Selected == road) DrawHandles();
            RefreshFlags(true);
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    public async void RemoveSelected()
    {
        var road = Selected;
        var roads = Roads;
        if (road == null || roads == null || Busy) return;

        Busy = true;
        Changed?.Invoke();
        try
        {
            var error = await roads.RemoveAsync(road);
            if (error != null) Failed?.Invoke(ErrorText(error));
            else
            {
                Selected = null;
                savedPoints = null;
                HideHandles();
            }
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// What Apply would cost: positive amounts to pay, negative ones given back. Priced as the
    /// server does: the road as it would be, against the road as it is.
    /// </summary>
    public (long wood, long ore, long fish, long wheat, long coins) EditCost()
    {
        var roads = Roads;
        if (roads == null || Selected == null) return default;
        var after = roads.Cost(Selected.Type, EditLength);
        var before = roads.Cost(savedType, SavedLength);
        return (after.wood - before.wood, after.ore - before.ore, after.fish - before.fish, after.wheat - before.wheat, after.coins - before.coins);
    }

    public bool EditAffordable()
    {
        var hall = gameManager.Village ? gameManager.Village.TownHall : null;
        if (!hall) return false;
        var c = EditCost();
        return hall.Wood >= c.wood && hall.Ore >= c.ore && hall.Fish >= c.fish && hall.Wheat >= c.wheat && hall.Coins >= c.coins;
    }

    private void Revert()
    {
        var road = Selected;
        var roads = Roads;
        // while saving, the server's answer decides what the road is
        if (road == null || roads == null || savedPoints == null || Busy) return;
        if (road.Type == savedType && SamePoints(road.Points, savedPoints)) return;
        road.Type = savedType;
        road.Points = new List<Vector2>(savedPoints);
        roads.Rebuild(road);
        RefreshFlags(true);
    }

    // ---- Shared ----------------------------------------------------------------------------------

    /// <summary>The road under the mouse, nearest first, or null.</summary>
    public VillageRoads.Road RoadUnderMouse()
    {
        var roads = Roads;
        if (roads == null || !mode.TryGetMousePoint(out var point)) return null;
        var flat = new Vector2(point.x, point.z);
        VillageRoads.Road best = null;
        var bestDistance = float.MaxValue;
        foreach (var road in roads.Roads)
        {
            var distance = RoadMesh.DistanceTo(road.Samples, flat, out _);
            if (distance < roads.WidthOf(road.Type) / 2f + 0.3f && distance < bestDistance)
            {
                best = road;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>Marks every road that runs through a building or the town hall, or clears the marks.</summary>
    public void RefreshFlags(bool show)
    {
        var roads = Roads;
        if (roads == null) return;
        foreach (var road in roads.Roads)
        {
            var flagged = false;
            if (show)
            {
                for (var i = 0; i < road.Samples.Count && !flagged; i += 3)
                {
                    flagged = mode.IsBuilt(road.Samples[i]);
                }
            }
            roads.SetFlagged(road, flagged);
        }
    }

    // ---- Input ---------------------------------------------------------------------------------

    /// <summary>Handles this frame's input when drawing or editing. True when it took the input.</summary>
    public bool HandleInput()
    {
        if (Busy) return Drawing || Selected != null;
        if (Drawing) return UpdateDrawing();
        if (Selected != null) return UpdateEditing();
        return false;
    }

    private bool UpdateDrawing()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            CancelDrawing();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Backspace)) UndoPoint();

        hasCursor = mode.TryGetMousePoint(out var point);
        if (hasCursor)
        {
            var next = Snap(new Vector2(point.x, point.z), drawPoints.Count > 0 ? drawPoints[drawPoints.Count - 1] : (Vector2?)null, null);
            if (next != cursor)
            {
                cursor = next;
                UpdatePreview();
            }
        }

        var overUi = Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement();
        var finish = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || (Input.GetMouseButtonDown(1) && !overUi);

        if (Input.GetMouseButtonDown(0) && !overUi && hasCursor)
        {
            // a click on the last point finishes, as in most drawing tools
            if (drawPoints.Count >= 2 && Vector2.Distance(cursor, drawPoints[drawPoints.Count - 1]) < 0.6f) finish = true;
            else if (drawPoints.Count == 0 || Vector2.Distance(cursor, drawPoints[drawPoints.Count - 1]) >= 0.5f)
            {
                drawPoints.Add(cursor);
                UpdatePreview();
            }
        }

        if (finish)
        {
            if (drawPoints.Count >= 2) _ = FinishDrawing();
            else CancelDrawing();
        }

        return true;
    }

    private async Task FinishDrawing()
    {
        var roads = Roads;
        if (roads == null || !DrawType.HasValue) return;

        // the cursor's piece is part of the preview only: price what is placed
        var length = VillageRoads.PaidLength(drawPoints);
        if (!CanPay(roads.Cost(DrawType.Value, length)))
        {
            Failed?.Invoke(ErrorText("resources"));
            return;
        }

        Busy = true;
        Changed?.Invoke();
        try
        {
            var (road, error) = await roads.BuildAsync(DrawType.Value, drawPoints);
            if (road == null)
            {
                Failed?.Invoke(ErrorText(error));
                return;
            }

            // carry on drawing the next road of the same type, from nothing
            drawPoints.Clear();
            UpdatePreview();
            RefreshFlags(true);
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    private bool UpdateEditing()
    {
        var road = Selected;
        var roads = Roads;
        if (roads == null) return false;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscapeKey.Claim();
            // the first Esc undoes the changes, the next lets go of the road
            if (Dirty) CancelEdit();
            else Deselect();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Apply();
            return true;
        }

        // dragging a point
        if (dragIndex >= 0)
        {
            if (mode.TryGetMousePoint(out var dragPoint))
            {
                var snapped = Snap(new Vector2(dragPoint.x, dragPoint.z), null, road);
                if (road.Points[dragIndex] != snapped)
                {
                    road.Points[dragIndex] = snapped;
                    roads.Rebuild(road);
                    DrawHandles();
                    Changed?.Invoke();
                }
            }

            if (!Input.GetMouseButton(0)) dragIndex = -1;
            return true;
        }

        var overUi = Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement();
        if (overUi) return false;

        var handle = HandleUnderMouse(road.Points);
        if (Input.GetMouseButtonDown(0) && handle >= 0)
        {
            dragIndex = handle;
            return true;
        }

        if (Input.GetMouseButtonDown(1) && handle >= 0)
        {
            if (road.Points.Count <= 2)
            {
                Failed?.Invoke(Localization.UI_ROAD_TWO_POINTS);
                return true;
            }

            road.Points.RemoveAt(handle);
            roads.Rebuild(road);
            DrawHandles();
            Changed?.Invoke();
            return true;
        }

        var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (Input.GetMouseButtonDown(0) && ctrl && mode.TryGetMousePoint(out var insertPoint))
        {
            var flat = Snap(new Vector2(insertPoint.x, insertPoint.z), null, road);
            road.Points.Insert(InsertIndex(road.Points, flat), flat);
            roads.Rebuild(road);
            DrawHandles();
            Changed?.Invoke();
            return true;
        }

        // any other click is the build mode's: a building, another road, or empty ground
        return false;
    }

    // ---- Helpers -------------------------------------------------------------------------------

    /// <summary>
    /// The point the mouse means: onto a nearby point of another road first, so roads meet end to
    /// end; else the 1 m grid, or free with Alt; Shift keeps the turn from <paramref name="from"/>
    /// to 15 degree steps.
    /// </summary>
    private Vector2 Snap(Vector2 point, Vector2? from, VillageRoads.Road except)
    {
        var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!alt && TryJoin(point, except, out var joined)) return joined;

        if (shift && from.HasValue)
        {
            var offset = point - from.Value;
            var angle = Mathf.Round(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg / AngleStep) * AngleStep * Mathf.Deg2Rad;
            var distance = alt ? offset.magnitude : Mathf.Round(offset.magnitude);
            return from.Value + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        return alt ? point : new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
    }

    private bool TryJoin(Vector2 point, VillageRoads.Road except, out Vector2 joined)
    {
        joined = point;
        var roads = Roads;
        if (roads == null) return false;
        var best = JoinDistance;
        var found = false;
        foreach (var road in roads.Roads)
        {
            if (road == except) continue;
            foreach (var p in road.Points)
            {
                var d = Vector2.Distance(p, point);
                if (d < best)
                {
                    best = d;
                    joined = p;
                    found = true;
                }
            }
        }
        return found;
    }

    private bool CanPay((long wood, long ore, long fish, long wheat, long coins) c)
    {
        var hall = gameManager.Village ? gameManager.Village.TownHall : null;
        return hall && hall.Wood >= c.wood && hall.Ore >= c.ore && hall.Fish >= c.fish && hall.Wheat >= c.wheat && hall.Coins >= c.coins;
    }

    private static bool SamePoints(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        if (a == null || b == null || a.Count != b.Count) return a == b;
        for (var i = 0; i < a.Count; ++i)
        {
            if ((a[i] - b[i]).sqrMagnitude > 0.0001f) return false;
        }
        return true;
    }

    /// <summary>Where a new point goes: into the segment it lies closest to.</summary>
    private static int InsertIndex(List<Vector2> points, Vector2 point)
    {
        var best = 1;
        var bestDistance = float.MaxValue;
        for (var i = 1; i < points.Count; ++i)
        {
            var d = RoadMesh.DistanceToSegment(point, points[i - 1], points[i]);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    private int HandleUnderMouse(IReadOnlyList<Vector2> points)
    {
        var camera = Camera.main;
        var roads = Roads;
        if (!camera || points == null || roads == null) return -1;
        var best = -1;
        var bestDistance = HandlePixels;
        for (var i = 0; i < points.Count; ++i)
        {
            var p = points[i];
            var world = new Vector3(p.x, roads.GroundHeight(new Vector3(p.x, 0f, p.y)), p.y);
            var screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f) continue;
            var d = Vector2.Distance(screen, Input.mousePosition);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    private void UpdatePreview()
    {
        var roads = Roads;
        if (roads == null || !DrawType.HasValue)
        {
            HidePreview();
            return;
        }

        var points = new List<Vector2>(drawPoints);
        if (hasCursor && (points.Count == 0 || points[points.Count - 1] != cursor)) points.Add(cursor);

        if (preview == null) preview = new VillageRoads.Road { Id = Guid.Empty };
        preview.Type = DrawType.Value;
        preview.Points = points;

        // what it would cost to finish here, the bit to the cursor included, so the number does not
        // jump when the point is placed
        DrawLength = VillageRoads.PaidLength(points);
        DrawAffordable = CanPay(roads.Cost(DrawType.Value, DrawLength));

        if (points.Count >= 2)
        {
            roads.Rebuild(preview);
            preview.Object.SetActive(true);
            roads.SetFlagged(preview, !DrawAffordable);
        }
        else if (preview.Object)
        {
            preview.Object.SetActive(false);
        }

        // the points placed so far get handles, so the shape can be read
        DrawPointHandles(drawPoints);
        Changed?.Invoke();
    }

    private void HidePreview()
    {
        if (preview != null && preview.Object) preview.Object.SetActive(false);
        DrawLength = 0f;
        DrawAffordable = true;
        HideHandles();
    }

    private void DrawHandles()
    {
        if (Selected == null) { HideHandles(); return; }
        DrawPointHandles(Selected.Points);
    }

    private void DrawPointHandles(IReadOnlyList<Vector2> points)
    {
        var roads = Roads;
        if (roads == null) return;
        if (!handleMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            handleMaterial = new Material(shader);
            var colour = new Color(1f, 0.82f, 0.38f);
            handleMaterial.SetColor("_BaseColor", colour);
            handleMaterial.color = colour;
            // over everything, buildings included: a road point is always reachable
            handleMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            handleMaterial.renderQueue = 4000;
        }

        while (handles.Count < points.Count)
        {
            var go = new GameObject("Road Handle " + handles.Count);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = handleMaterial;
            line.widthMultiplier = 0.18f;
            line.positionCount = HandleRingPoints;
            line.loop = true;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            handles.Add(line);
        }

        for (var i = 0; i < handles.Count; ++i)
        {
            var show = i < points.Count;
            handles[i].gameObject.SetActive(show);
            if (!show) continue;

            var p = points[i];
            var height = roads.GroundHeight(new Vector3(p.x, 0f, p.y)) + 0.25f;
            for (var k = 0; k < HandleRingPoints; ++k)
            {
                var a = k / (float)HandleRingPoints * Mathf.PI * 2f;
                handles[i].SetPosition(k, new Vector3(p.x + Mathf.Cos(a) * 0.55f, height, p.y + Mathf.Sin(a) * 0.55f));
            }
        }
    }

    private void HideHandles()
    {
        foreach (var handle in handles) if (handle) handle.gameObject.SetActive(false);
    }

    private static string ErrorText(string error)
    {
        switch (error)
        {
            case "resources": return Localization.UI_ROAD_ERROR_RESOURCES;
            case "locked": return Localization.UI_ROAD_ERROR_LOCKED;
            case "invalid": return Localization.UI_ROAD_ERROR_INVALID;
            default: return Localization.UI_ROAD_ERROR_UNKNOWN;
        }
    }

    private void OnDestroy()
    {
        if (handleMaterial) Destroy(handleMaterial);
    }
}
