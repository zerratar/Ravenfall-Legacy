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
/// through them. Points snap to the 1 m grid, Alt places them freely, Shift keeps the direction
/// to 15 degree steps. Backspace takes the last point back, Enter, a right click or a click on the
/// last point finishes, Esc stops. The road is drawn as it will look, red when the village cannot
/// pay for it.
/// </para>
/// <para>
/// <b>Editing.</b> Click a road to select it. Drag a point to move it, Ctrl click the road to add
/// a point, right click a point to take it away, Delete removes the road. Every change is saved
/// when the mouse is let go. A road under a building is reached by clicking the building again:
/// the second click goes to the road beneath, so no road is ever stuck where it cannot be picked.
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
    private const int HandleRingPoints = 24;

    private VillageBuildMode mode;
    private GameManager gameManager;

    // drawing
    private readonly List<Vector2> drawPoints = new List<Vector2>();
    private VillageRoads.Road preview;
    private Vector2 cursor;
    private bool hasCursor;

    // editing
    private int dragIndex = -1;
    private List<Vector2> dragBefore;
    private bool dragMoved;

    private readonly List<LineRenderer> handles = new List<LineRenderer>();
    private Material handleMaterial;

    /// <summary>The road type being drawn, or null.</summary>
    public int? DrawType { get; private set; }
    public VillageRoads.Road Selected { get; private set; }
    public bool Busy { get; private set; }
    public bool Drawing => DrawType.HasValue;

    /// <summary>The road being drawn: its length and what it costs.</summary>
    public float DrawLength { get; private set; }
    public bool DrawAffordable { get; private set; } = true;

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

    // ---- Called by build mode and its view --------------------------------------------------------

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

    public void Select(VillageRoads.Road road)
    {
        if (road == null || Busy) return;
        CancelDrawing();
        Selected = road;
        DrawHandles();
        Changed?.Invoke();
    }

    public void Deselect()
    {
        if (Selected == null) return;
        Selected = null;
        dragIndex = -1;
        HideHandles();
        Changed?.Invoke();
    }

    /// <summary>Gives the selected road another type, paying or getting back the difference.</summary>
    public async void SetType(int type)
    {
        var road = Selected;
        var roads = Roads;
        if (road == null || roads == null || Busy || road.Type == type) return;
        var info = roads.TypeOf(type);
        if (info == null || !info.Unlocked) return;

        await Save(road, type, road.Points, new List<Vector2>(road.Points), road.Type);
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
            else Deselect();
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

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

        if (Input.GetKeyDown(KeyCode.Backspace) && drawPoints.Count > 0)
        {
            drawPoints.RemoveAt(drawPoints.Count - 1);
            UpdatePreview();
        }

        hasCursor = mode.TryGetMousePoint(out var point);
        if (hasCursor)
        {
            var next = Snap(new Vector2(point.x, point.z), drawPoints.Count > 0 ? drawPoints[drawPoints.Count - 1] : (Vector2?)null);
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
                Changed?.Invoke();
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
        if (!DrawAffordable)
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
            Deselect();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemoveSelected();
            return true;
        }

        var overUi = Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement();

        // dragging a point
        if (dragIndex >= 0)
        {
            if (mode.TryGetMousePoint(out var dragPoint))
            {
                var snapped = Snap(new Vector2(dragPoint.x, dragPoint.z), null);
                if (road.Points[dragIndex] != snapped)
                {
                    road.Points[dragIndex] = snapped;
                    dragMoved = true;
                    roads.Rebuild(road);
                    DrawHandles();
                }
            }

            if (!Input.GetMouseButton(0))
            {
                dragIndex = -1;
                if (dragMoved) _ = Save(road, road.Type, road.Points, dragBefore, road.Type);
            }
            return true;
        }

        if (overUi) return false;

        var handle = HandleUnderMouse(road);
        if (Input.GetMouseButtonDown(0) && handle >= 0)
        {
            dragIndex = handle;
            dragBefore = new List<Vector2>(road.Points);
            dragMoved = false;
            return true;
        }

        if (Input.GetMouseButtonDown(1) && handle >= 0)
        {
            if (road.Points.Count <= 2)
            {
                Failed?.Invoke(Localization.UI_ROAD_TWO_POINTS);
                return true;
            }

            var before = new List<Vector2>(road.Points);
            road.Points.RemoveAt(handle);
            roads.Rebuild(road);
            DrawHandles();
            _ = Save(road, road.Type, road.Points, before, road.Type);
            return true;
        }

        var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (Input.GetMouseButtonDown(0) && ctrl && mode.TryGetMousePoint(out var insertPoint))
        {
            var flat = Snap(new Vector2(insertPoint.x, insertPoint.z), null);
            var at = InsertIndex(road.Points, flat);
            var before = new List<Vector2>(road.Points);
            road.Points.Insert(at, flat);
            roads.Rebuild(road);
            DrawHandles();
            _ = Save(road, road.Type, road.Points, before, road.Type);
            return true;
        }

        // any other click is the build mode's: a building, another road, or empty ground
        return false;
    }

    private async Task Save(VillageRoads.Road road, int type, IReadOnlyList<Vector2> points, IReadOnlyList<Vector2> before, int typeBefore)
    {
        var roads = Roads;
        if (roads == null) return;
        Busy = true;
        Changed?.Invoke();
        try
        {
            var error = await roads.ChangeAsync(road, type, new List<Vector2>(points), before, typeBefore);
            if (error != null) Failed?.Invoke(ErrorText(error));
            if (Selected == road) DrawHandles();
            RefreshFlags(true);
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    // ---- Helpers -------------------------------------------------------------------------------

    private static Vector2 Snap(Vector2 point, Vector2? from)
    {
        var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shift && from.HasValue)
        {
            var offset = point - from.Value;
            var angle = Mathf.Round(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg / AngleStep) * AngleStep * Mathf.Deg2Rad;
            var distance = alt ? offset.magnitude : Mathf.Round(offset.magnitude);
            return from.Value + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        return alt ? point : new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
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

    private int HandleUnderMouse(VillageRoads.Road road)
    {
        var camera = Camera.main;
        if (!camera || road == null) return -1;
        var roads = Roads;
        var best = -1;
        var bestDistance = HandlePixels;
        for (var i = 0; i < road.Points.Count; ++i)
        {
            var p = road.Points[i];
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

        // what the placed points cost; the bit to the cursor is shown but not counted until placed
        DrawLength = VillageRoads.PaidLength(points);
        var cost = roads.Cost(DrawType.Value, DrawLength);
        var hall = gameManager.Village.TownHall;
        DrawAffordable = hall && hall.Wood >= cost.wood && hall.Ore >= cost.ore && hall.Fish >= cost.fish
            && hall.Wheat >= cost.wheat && hall.Coins >= cost.coins;

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
