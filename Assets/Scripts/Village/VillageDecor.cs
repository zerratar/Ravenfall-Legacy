using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.SDK.Endpoints;
using UnityEngine;

/// <summary>
/// The village's decor: the island's own trees, bushes and flowers, with the village's changes applied over them,
/// and the pieces the streamer placed. Loaded with the village; build mode's decor tool changes it through here.
/// See docs/village-decor-design.md.
/// </summary>
/// <remarks>
/// The server keeps only changes (a moved or removed island piece, a placed piece), so applying them means putting
/// every island piece home first. Small pieces (flowers, grass) under a building or a road are hidden, and come back
/// when it moves away; they are never in the way. The other pieces stop buildings from being placed over them.
/// </remarks>
public class VillageDecor : MonoBehaviour
{
    [Serializable]
    public struct PiecePrefab
    {
        public string Key;
        public GameObject Prefab;
    }

    [Tooltip("The prefab for each piece of the price list, set by tools/village-rebuild/decor-defaults.cs.")]
    [SerializeField] private List<PiecePrefab> prefabs = new List<PiecePrefab>();

    private readonly List<VillageDecorItem> defaults = new List<VillageDecorItem>();
    private readonly Dictionary<string, VillageDecorItem> defaultsByKey = new Dictionary<string, VillageDecorItem>();
    private readonly List<VillageDecorItem> placed = new List<VillageDecorItem>();
    private readonly List<DecorPiece> pieces = new List<DecorPiece>();
    private Transform placedRoot;
    private GameManager gameManager;
    private bool collected;

    public bool Loaded { get; private set; }
    public IReadOnlyList<DecorPiece> Pieces => pieces;

    /// <summary>Something was placed, moved, removed or loaded.</summary>
    public event Action Changed;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    /// <summary>Every piece standing in the village now: the island's that are left and the placed ones.</summary>
    public IEnumerable<VillageDecorItem> Items
    {
        get
        {
            Collect();
            foreach (var item in defaults) if (item && !item.Removed) yield return item;
            foreach (var item in placed) if (item) yield return item;
        }
    }

    public DecorPiece PieceOf(string key) => pieces.FirstOrDefault(p => p.Key == key);

    public GameObject PrefabFor(string key)
    {
        foreach (var p in prefabs) if (p.Key == key) return p.Prefab;
        return null;
    }

    public string NameOf(string key) => Localization.DecorName(key, PieceOf(key)?.Name);

    // ---- Loading -----------------------------------------------------------------------------

    public async Task LoadAsync()
    {
        if (!gameManager || gameManager.RavenNest == null) return;
        try
        {
            var loadedPieces = await gameManager.RavenNest.Village.GetDecorPiecesAsync();
            var rows = await gameManager.RavenNest.Village.GetDecorAsync();
            pieces.Clear();
            if (loadedPieces != null) pieces.AddRange(loadedPieces);
            Apply(rows ?? Array.Empty<DecorInfo>());
            Loaded = true;
            Subscribe();
        }
        catch (Exception exc)
        {
            // an older server without decor: the island as shipped, and no Decor tab to use
            Shinobytes.Debug.LogWarning("Could not load the village's decor: " + exc.Message);
        }
    }

    private bool subscribed;

    // small pieces hide under what is built or drawn over them, and come back when it goes
    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;
        var roads = gameManager && gameManager.Village ? gameManager.Village.Roads : null;
        if (roads != null) roads.Changed += RefreshCovered;
        if (VillageBuildMode.Instance) VillageBuildMode.Instance.Changed += RefreshCovered;
    }

    private void OnDestroy()
    {
        if (!subscribed) return;
        var roads = gameManager && gameManager.Village ? gameManager.Village.Roads : null;
        if (roads != null) roads.Changed -= RefreshCovered;
        if (VillageBuildMode.Instance) VillageBuildMode.Instance.Changed -= RefreshCovered;
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;
        foreach (var item in FindObjectsByType<VillageDecorItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!item.IsDefault || defaultsByKey.ContainsKey(item.DefaultKey)) continue;
            item.RememberHome();
            defaults.Add(item);
            defaultsByKey[item.DefaultKey] = item;
        }
    }

    /// <summary>The island as shipped, with the village's changes over it, and the placed pieces.</summary>
    private void Apply(IReadOnlyList<DecorInfo> rows)
    {
        Collect();
        foreach (var item in defaults)
        {
            if (!item) continue;
            item.GoHome();
            item.Removed = false;
            item.Id = Guid.Empty;
            item.Small = PieceOf(item.Piece)?.Small ?? false;
        }

        foreach (var item in placed) if (item) Destroy(item.gameObject);
        placed.Clear();

        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.DefaultKey))
            {
                if (!defaultsByKey.TryGetValue(row.DefaultKey, out var item) || !item) continue;
                item.Id = row.Id;
                item.Removed = row.Removed;
                if (!row.Removed) item.transform.SetPositionAndRotation(new Vector3((float)row.X, (float)row.Y, (float)row.Z), Quaternion.Euler(0f, (float)row.Rotation, 0f));
            }
            else
            {
                Spawn(row);
            }
        }

        RefreshCovered();
        Changed?.Invoke();
    }

    private VillageDecorItem Spawn(DecorInfo row)
    {
        var prefab = PrefabFor(row.Piece);
        if (!prefab) return null;
        if (!placedRoot)
        {
            placedRoot = new GameObject("Placed Decor").transform;
            placedRoot.SetParent(transform, false);
        }

        var go = Instantiate(prefab, new Vector3((float)row.X, (float)row.Y, (float)row.Z), Quaternion.Euler(0f, (float)row.Rotation, 0f), placedRoot);
        var scale = row.Scale > 0 ? (float)row.Scale : 1f;
        go.transform.localScale = Vector3.one * scale;
        // the decor prefabs carry their own (it says how they are measured); the island's trees do not
        if (!go.TryGetComponent<VillageDecorItem>(out var item)) item = go.AddComponent<VillageDecorItem>();
        item.Setup(null, row.Piece);
        item.Id = row.Id;
        item.Small = PieceOf(row.Piece)?.Small ?? false;
        placed.Add(item);
        return item;
    }

    // ---- Changes, through the server ---------------------------------------------------------

    /// <summary>Places a piece. Null with the reason when the server refused.</summary>
    public async Task<(VillageDecorItem item, string error)> PlaceAsync(string piece, Vector3 position, float rotation)
    {
        var result = await gameManager.RavenNest.Village.PlaceDecorAsync(new DecorRequest
        {
            Piece = piece, X = position.x, Y = position.y, Z = position.z, Rotation = rotation, Scale = 1,
        });
        ApplyResources(result);
        if (result == null || !result.Ok || result.Decor == null) return (null, result?.Error ?? "unknown");
        var item = Spawn(result.Decor);
        RefreshCovered();
        Changed?.Invoke();
        return (item, null);
    }

    /// <summary>Saves where a piece stands now. On failure it goes back to where it was.</summary>
    public async Task<string> MoveAsync(VillageDecorItem item, Vector3 position, float rotation, Vector3 before, Quaternion beforeRotation)
    {
        var result = await gameManager.RavenNest.Village.MoveDecorAsync(new DecorRequest
        {
            Id = item.IsDefault ? (Guid?)null : item.Id,
            DefaultKey = item.IsDefault ? item.DefaultKey : null,
            X = position.x, Y = position.y, Z = position.z, Rotation = rotation,
        });
        if (result == null || !result.Ok)
        {
            if (item) item.transform.SetPositionAndRotation(before, beforeRotation);
            return result?.Error ?? "unknown";
        }

        if (item && result.Decor != null) item.Id = result.Decor.Id;
        RefreshCovered();
        Changed?.Invoke();
        return null;
    }

    /// <summary>Removes a piece; the result says what it gave back. Null result with the reason on failure.</summary>
    public async Task<(DecorResult result, string error)> RemoveAsync(VillageDecorItem item)
    {
        var result = await gameManager.RavenNest.Village.RemoveDecorAsync(new DecorRequest
        {
            Id = item.IsDefault ? (Guid?)null : item.Id,
            DefaultKey = item.IsDefault ? item.DefaultKey : null,
        });
        ApplyResources(result);
        if (result == null || !result.Ok) return (null, result?.Error ?? "unknown");

        if (item.IsDefault)
        {
            item.Removed = true;
            item.UpdateVisible();
        }
        else
        {
            placed.Remove(item);
            Destroy(item.gameObject);
        }

        Changed?.Invoke();
        return (result, null);
    }

    private void ApplyResources(DecorResult result)
    {
        if (result == null) return;
        var townHall = gameManager && gameManager.Village ? gameManager.Village.TownHall : null;
        if (townHall) townHall.SetResources((long)result.Coins, (long)result.Ore, (long)result.Wood, (long)result.Fish, (long)result.Wheat);
    }

    // ---- What stands where -------------------------------------------------------------------

    /// <summary>The piece nearest to a point on the ground, if the point is on it.</summary>
    public VillageDecorItem ItemAt(Vector3 point, VillageDecorItem except = null)
    {
        VillageDecorItem best = null;
        var bestScore = float.MaxValue;
        var flat = new Vector2(point.x, point.z);
        foreach (var item in Items)
        {
            if (!item || item == except || item.Covered || !item.gameObject.activeInHierarchy) continue;
            var p = item.transform.position;
            var distance = new Vector2(p.x - point.x, p.z - point.z).magnitude;
            float reach;
            if (item.Rectangle)
            {
                var shape = item.Shape;
                if (!shape.Grown(0.3f).Contains(p, item.ShapeRotation, flat)) continue;
                reach = Mathf.Max(shape.Half.x, shape.Half.y);
            }
            else
            {
                reach = Mathf.Max(item.Radius, 0.7f);
                if (distance > reach) continue;
            }

            // how far out on it, so a barrel by a long fence is still picked when clicked
            var score = distance / reach;
            if (score < bestScore)
            {
                best = item;
                bestScore = score;
            }
        }
        return best;
    }

    /// <summary>The pieces a building's footprint would stand on. Small pieces are never in the way.</summary>
    public List<VillageDecorItem> InTheWay(Footprint footprint, Vector3 position, float rotation, VillageDecorItem except = null)
    {
        var found = new List<VillageDecorItem>();
        foreach (var item in Items)
        {
            if (!item || item == except || item.Small) continue;
            if (Footprint.Overlaps(footprint, position, rotation, item.Shape, item.transform.position, item.ShapeRotation)) found.Add(item);
        }
        return found;
    }

    /// <summary>Hides the small pieces that a building or road now stands on, and shows the ones uncovered again.</summary>
    public void RefreshCovered()
    {
        var mode = VillageBuildMode.Instance;
        var roads = gameManager && gameManager.Village ? gameManager.Village.Roads : null;
        foreach (var item in defaults.Concat(placed))
        {
            if (!item) continue;
            var covered = false;
            if (item.Small)
            {
                var p = item.transform.position;
                covered = (mode && mode.CoversGround(p)) || (roads != null && OnRoad(roads, p));
            }
            item.Covered = covered;
            item.UpdateVisible();
        }
    }

    private static bool OnRoad(VillageRoads roads, Vector3 point)
    {
        var p = new Vector2(point.x, point.z);
        foreach (var road in roads.Roads)
        {
            // the curve as drawn (its samples), at the road's widest: a flower at the edge of a wide stretch hides
            var line = road.Samples != null && road.Samples.Count > 1 ? road.Samples : road.Points;
            var widest = 1f;
            foreach (var w in road.Widths) widest = Mathf.Max(widest, w);
            var half = roads.WidthOf(road.Type) / 2f * widest;
            for (var i = 1; i < line.Count; ++i)
            {
                var a = line[i - 1];
                var ab = line[i] - a;
                var t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                if ((a + ab * t - p).magnitude <= half) return true;
            }
        }
        return false;
    }
}
