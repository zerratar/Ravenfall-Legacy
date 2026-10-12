using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ground guide shown while moving a building in build mode: a grid patch that follows the
/// terrain around the building, and its footprint outlined in green when the spot is valid and red
/// when it is not.
/// </summary>
/// <remarks>
/// Built from line renderers with an unlit material made at runtime, so it needs no assets. Lines
/// are draped over the ground point by point, which keeps them visible on slopes instead of
/// cutting into the hillside.
/// </remarks>
public class PlacementOverlay : MonoBehaviour
{
    private const int GridHalfCells = 6;
    private const int PointsPerLine = 13;
    private const float Lift = 0.06f;

    private static readonly Color GridColor = new Color(1f, 0.96f, 0.84f, 1f);
    private static readonly Color ValidColor = new Color(0.30f, 0.85f, 0.52f, 1f);
    private static readonly Color InvalidColor = new Color(0.95f, 0.32f, 0.30f, 1f);

    private readonly List<LineRenderer> grid = new List<LineRenderer>();
    private LineRenderer footprint;
    private Material gridMaterial;
    private Material footprintMaterial;
    private float cellSize = 1f;

    // What was last drawn. Draping costs a ground raycast per point, so the lines are only rebuilt
    // when the snapped position, rotation or validity actually changes, not every frame.
    private Vector3 drawnCenter = new Vector3(float.NaN, 0f, 0f);
    private float drawnRotation = float.NaN;
    private float drawnSize = float.NaN;
    private bool drawnValid;

    private static readonly Color GhostValidTint = new Color(0.55f, 1f, 0.62f, 1f);
    private static readonly Color GhostInvalidTint = new Color(1f, 0.42f, 0.40f, 1f);
    private static MaterialPropertyBlock ghostBlock;

    /// <summary>
    /// A copy of a building to place: no behaviour, no colliders (so it never catches the mouse ray
    /// or blocks other checks), tinted with <see cref="TintGhost"/>.
    /// </summary>
    public static GameObject MakeGhost(GameObject prefab)
    {
        var ghost = Instantiate(prefab);
        ghost.name = prefab.name + " (Placing)";
        foreach (var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (var collider in ghost.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        TintGhost(ghost, true);
        return ghost;
    }

    /// <summary>
    /// Tints the ghost green when it can be placed and red when it cannot. A property block keeps
    /// the building's own materials, so its shape and lighting still read, rather than turning it
    /// into a flat silhouette.
    /// </summary>
    public static void TintGhost(GameObject ghost, bool valid)
    {
        if (!ghost) return;
        if (ghostBlock == null) ghostBlock = new MaterialPropertyBlock();

        var tint = valid ? GhostValidTint : GhostInvalidTint;
        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            renderer.GetPropertyBlock(ghostBlock);
            ghostBlock.SetColor("_BaseColor", tint);
            ghostBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(ghostBlock);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public static PlacementOverlay Create(float cell)
    {
        var go = new GameObject("Build Mode Placement Overlay");
        var overlay = go.AddComponent<PlacementOverlay>();
        overlay.cellSize = cell;
        overlay.Build();
        overlay.Hide();
        return overlay;
    }

    private void Build()
    {
        gridMaterial = MakeMaterial(GridColor);
        footprintMaterial = MakeMaterial(ValidColor);

        var lines = (GridHalfCells * 2 + 1) * 2;
        for (var i = 0; i < lines; ++i)
        {
            grid.Add(MakeLine("Grid " + i, gridMaterial, 0.03f, PointsPerLine, false));
        }

        footprint = MakeLine("Footprint", footprintMaterial, 0.14f, 4 * 8, true);
    }

    private LineRenderer MakeLine(string name, Material material, float width, int points, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.positionCount = points;
        line.loop = loop;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 2;
        return line;
    }

    private static Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.color = color;
        return material;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        drawnCenter = new Vector3(float.NaN, 0f, 0f);
    }

    /// <param name="center">Where the building stands.</param>
    /// <param name="rotation">Its rotation around the vertical axis, in degrees.</param>
    /// <param name="halfSize">Half the width of its footprint, in metres.</param>
    public void Show(Vector3 center, float rotation, float halfSize, bool valid)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (center == drawnCenter && rotation == drawnRotation && halfSize == drawnSize && valid == drawnValid)
        {
            return;
        }

        drawnCenter = center;
        drawnRotation = rotation;
        drawnSize = halfSize;
        drawnValid = valid;

        var color = valid ? ValidColor : InvalidColor;
        footprintMaterial.SetColor("_BaseColor", color);
        footprintMaterial.color = color;

        // The grid is aligned to the world, like the snapping, not to the building.
        var origin = new Vector3(Mathf.Round(center.x / cellSize) * cellSize, center.y, Mathf.Round(center.z / cellSize) * cellSize);
        var extent = GridHalfCells * cellSize;
        var index = 0;
        for (var i = -GridHalfCells; i <= GridHalfCells; ++i)
        {
            var offset = i * cellSize;
            Drape(grid[index++], origin + new Vector3(offset, 0f, -extent), origin + new Vector3(offset, 0f, extent));
            Drape(grid[index++], origin + new Vector3(-extent, 0f, offset), origin + new Vector3(extent, 0f, offset));
        }

        // Footprint: a square turned with the building, its sides draped over the ground too.
        var turn = Quaternion.Euler(0f, rotation, 0f);
        var corners = new[]
        {
            center + turn * new Vector3(-halfSize, 0f, -halfSize),
            center + turn * new Vector3(-halfSize, 0f, halfSize),
            center + turn * new Vector3(halfSize, 0f, halfSize),
            center + turn * new Vector3(halfSize, 0f, -halfSize),
        };

        var perSide = footprint.positionCount / 4;
        for (var side = 0; side < 4; ++side)
        {
            var a = corners[side];
            var b = corners[(side + 1) % 4];
            for (var p = 0; p < perSide; ++p)
            {
                var point = Vector3.Lerp(a, b, p / (float)perSide);
                footprint.SetPosition(side * perSide + p, Ground(point));
            }
        }
    }

    private void Drape(LineRenderer line, Vector3 from, Vector3 to)
    {
        for (var p = 0; p < PointsPerLine; ++p)
        {
            line.SetPosition(p, Ground(Vector3.Lerp(from, to, p / (float)(PointsPerLine - 1))));
        }
    }

    private static Vector3 Ground(Vector3 point)
    {
        return PlacementUtility.FindGroundPoint(point) + Vector3.up * Lift;
    }

    private void OnDestroy()
    {
        if (gridMaterial) Destroy(gridMaterial);
        if (footprintMaterial) Destroy(footprintMaterial);
    }
}
