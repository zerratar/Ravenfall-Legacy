using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ground guide shown while placing a building in build mode: its footprint outlined in green
/// when the spot is valid and red when it is not, and the buildings in the way outlined in red. The
/// grid itself is VillageGrid, over the whole buildable area.
/// </summary>
/// <remarks>
/// Built from line renderers with an unlit material made at runtime, so it needs no assets. Lines
/// are draped over the ground point by point, which keeps them visible on slopes instead of
/// cutting into the hillside.
/// </remarks>
public class PlacementOverlay : MonoBehaviour
{
    private const float Lift = 0.06f;

    private static readonly Color ValidColor = new Color(0.30f, 0.85f, 0.52f, 1f);
    private static readonly Color InvalidColor = new Color(0.95f, 0.32f, 0.30f, 1f);

    private LineRenderer footprint;
    // red rings under the buildings in the way, as many as there are; the extra ones are hidden
    private readonly List<LineRenderer> blockerRings = new List<LineRenderer>();
    private Material blockerMaterial;
    private const int RingPoints = 40;
    private Material footprintMaterial;
    private float cellSize = 1f;

    // What was last drawn. Draping costs a ground raycast per point, so the lines are only rebuilt
    // when the snapped position, rotation or validity actually changes, not every frame.
    private Vector3 drawnCenter = new Vector3(float.NaN, 0f, 0f);
    private float drawnRotation = float.NaN;
    private float drawnSize = float.NaN;
    private float drawnSizeZ = float.NaN;
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
        footprintMaterial = MakeMaterial(ValidColor);


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
        ShowBlockers(null);
    }

    /// <summary>
    /// Outlines the buildings in the way of a placement, in the invalid colour, so the streamer can
    /// see which ones to move instead of trying them one by one. Each is its own footprint: middle,
    /// turn and half size. Null or empty clears them.
    /// </summary>
    public void ShowBlockers(IReadOnlyList<(Vector3 centre, float rotation, Vector2 half)> blockers)
    {
        var count = blockers?.Count ?? 0;
        if (count > 0 && !blockerMaterial) blockerMaterial = MakeMaterial(InvalidColor);
        while (blockerRings.Count < count)
        {
            blockerRings.Add(MakeLine("Blocker " + blockerRings.Count, blockerMaterial, 0.32f, RingPoints, true));
        }

        for (var i = 0; i < blockerRings.Count; ++i)
        {
            var ring = blockerRings[i];
            var show = i < count;
            ring.gameObject.SetActive(show);
            if (!show) continue;

            var (centre, rotation, half) = blockers[i];
            var turn = Quaternion.Euler(0f, rotation, 0f);
            var corners = new[]
            {
                centre + turn * new Vector3(-half.x, 0f, -half.y),
                centre + turn * new Vector3(-half.x, 0f, half.y),
                centre + turn * new Vector3(half.x, 0f, half.y),
                centre + turn * new Vector3(half.x, 0f, -half.y),
            };
            var perSide = RingPoints / 4;
            for (var side = 0; side < 4; ++side)
            {
                for (var p = 0; p < perSide; ++p)
                {
                    ring.SetPosition(side * perSide + p, Ground(Vector3.Lerp(corners[side], corners[(side + 1) % 4], p / (float)perSide)));
                }
            }
        }
    }

    /// <param name="center">Where the building stands.</param>
    /// <param name="rotation">Its rotation around the vertical axis, in degrees.</param>
    /// <param name="halfSize">Half the width of its footprint, in metres.</param>
    public void Show(Vector3 center, float rotation, float halfSize, bool valid)
    {
        Show(center, rotation, halfSize, halfSize, valid);
    }

    /// <summary>A rectangular footprint, for the town hall area.</summary>
    /// <param name="center">The middle of the footprint.</param>
    /// <param name="rotation">Its rotation around the vertical axis, in degrees.</param>
    /// <param name="halfX">Half its width along its own x, in metres.</param>
    /// <param name="halfZ">Half its depth along its own z, in metres.</param>
    public void Show(Vector3 center, float rotation, float halfX, float halfZ, bool valid)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (center == drawnCenter && rotation == drawnRotation && halfX == drawnSize && halfZ == drawnSizeZ && valid == drawnValid)
        {
            return;
        }

        drawnCenter = center;
        drawnRotation = rotation;
        drawnSize = halfX;
        drawnSizeZ = halfZ;
        drawnValid = valid;

        var color = valid ? ValidColor : InvalidColor;
        footprintMaterial.SetColor("_BaseColor", color);
        footprintMaterial.color = color;

        // No patch of grid round the building any more: VillageGrid covers the whole buildable area
        // for as long as build mode is on, which says "grid" far better than a 13 m square did.

        // Footprint: a rectangle turned with the building, its sides draped over the ground too.
        var turn = Quaternion.Euler(0f, rotation, 0f);
        var corners = new[]
        {
            center + turn * new Vector3(-halfX, 0f, -halfZ),
            center + turn * new Vector3(-halfX, 0f, halfZ),
            center + turn * new Vector3(halfX, 0f, halfZ),
            center + turn * new Vector3(halfX, 0f, -halfZ),
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

    private static Vector3 Ground(Vector3 point)
    {
        return PlacementUtility.FindGroundPoint(point) + Vector3.up * Lift;
    }

    private void OnDestroy()
    {
        if (footprintMaterial) Destroy(footprintMaterial);
        if (blockerMaterial) Destroy(blockerMaterial);
    }
}
