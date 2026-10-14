using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Where grass is painted in or out, over a terrain: one byte per square metre, 128 for the terrain's own grass, 0 for
/// none and 255 for full grass. Only the 32 m tiles that have been painted are kept, so the asset stays small and
/// diffs well; the brush in the <see cref="GrassField"/> inspector paints it, undoably.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Grass Mask", fileName = "Grass Mask")]
public class GrassMask : ScriptableObject
{
    public const int TileSize = 32;
    public const byte Neutral = 128;

    [Serializable]
    public class Tile
    {
        public int x;
        public int y;
        public byte[] values = new byte[TileSize * TileSize];
    }

    [Tooltip("Metres per mask texel.")]
    [SerializeField, Min(0.25f)] private float metresPerTexel = 1f;
    [SerializeField] private List<Tile> tiles = new List<Tile>();

    [NonSerialized] private Dictionary<Vector2Int, Tile> lookup;

    public float MetresPerTexel => metresPerTexel;
    public IReadOnlyList<Tile> Tiles => tiles;

    /// <summary>The value at a texel: <see cref="Neutral"/> where nothing is painted.</summary>
    public byte Get(int x, int y)
    {
        var tile = TileAt(x, y, false);
        return tile != null ? tile.values[Local(y) * TileSize + Local(x)] : Neutral;
    }

    /// <summary>Paints a texel; a tile is added the first time something other than neutral is painted in it.</summary>
    public void Set(int x, int y, byte value)
    {
        var tile = TileAt(x, y, value != Neutral);
        if (tile != null) tile.values[Local(y) * TileSize + Local(x)] = value;
    }

    /// <summary>Drops tiles painted back to neutral everywhere, after a stroke.</summary>
    public void Tidy()
    {
        var removed = tiles.RemoveAll(t => Array.TrueForAll(t.values, v => v == Neutral));
        if (removed > 0) lookup = null;
    }

    private Tile TileAt(int x, int y, bool create)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<Vector2Int, Tile>();
            foreach (var t in tiles) lookup[new Vector2Int(t.x, t.y)] = t;
        }
        var key = new Vector2Int(Mathf.FloorToInt(x / (float)TileSize), Mathf.FloorToInt(y / (float)TileSize));
        if (lookup.TryGetValue(key, out var tile) || !create) return tile;

        tile = new Tile { x = key.x, y = key.y };
        for (var i = 0; i < tile.values.Length; ++i) tile.values[i] = Neutral;
        tiles.Add(tile);
        lookup[key] = tile;
        return tile;
    }

    private static int Local(int v) => ((v % TileSize) + TileSize) % TileSize;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // an undo or a paste replaced the tiles
        lookup = null;
    }
#endif
}
