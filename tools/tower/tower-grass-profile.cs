// Makes Assets/World/Grass/Tower Grass Profile.asset: the grass of the tower's places (GrassField on each tower terrain,
// added by the builders). The kinds are the islands' (Assets/World/Grass/Grass Profile.asset: the same blades, tufts and
// flowers, so the tower's grass looks like the rest of the game's); the layers are every terrain layer the tower builders
// paint with, how grassy each is, and its colour read from its texture. Run it again after a builder takes a new layer.
// Editor only. Writes the result to %TEMP%/tower-grass-profile.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var path = "Assets/World/Grass/Tower Grass Profile.asset";
var islands = UnityEditor.AssetDatabase.LoadAssetAtPath<GrassProfile>("Assets/World/Grass/Grass Profile.asset");
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<GrassProfile>(path);
if (!profile) { profile = UnityEngine.ScriptableObject.CreateInstance<GrassProfile>(); UnityEditor.AssetDatabase.CreateAsset(profile, path); }

string Meadow(string n) => "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Terrain_Meadow_" + n + ".terrainlayer";
string Frost(string n) => "Assets/Scenes/Tower/Tower_Glacier/" + n + ".terrainlayer";
// how much grass grows where the ground is all this layer: meadows full, worn and leafy ground less, moss a little,
// snow a few frosty tufts; paths, tiles, mud, rock and ice none
var densities = new (string path, float density)[]
{
    (Meadow("Grass_01"), 1f), (Meadow("Grass_02"), 1f), (Meadow("Grass_Clover_01"), 1f), (Meadow("Grass_Flowers_01"), 1f),
    (Meadow("Grass_Leaves_01"), 0.85f), (Meadow("Grass_Pebbles_01"), 0.6f), (Meadow("Moss_01"), 0.5f),
    (Meadow("Dirt_Cracked_Leaves_Heavy_01"), 0.35f),
    (Meadow("Dirt_01"), 0f), (Meadow("Dirt_Cracked_Pebbles_01"), 0f), (Meadow("Mud_01"), 0f), (Meadow("Mud_Debris_01"), 0f),
    (Meadow("Cobblestone_01"), 0f), (Meadow("Footpath_Tile_01"), 0f), (Meadow("Ruin_Tiles_01"), 0f), (Meadow("Gravel_01"), 0f),
    (Meadow("Rock_Moss_01"), 0f), (Meadow("Moss_Rock_01"), 0f),
    (Frost("Snow"), 0.35f), (Frost("Snow_Shade"), 0.25f), (Frost("Snow_Packed"), 0f), (Frost("Ice"), 0f), (Frost("Rock"), 0f),
    (Frost("Flagstones_Frozen"), 0f),
};
var layers = new System.Collections.Generic.List<GrassProfile.Layer>();
var data = new UnityEngine.TerrainData();
var terrainLayers = new System.Collections.Generic.List<UnityEngine.TerrainLayer>();
foreach (var (p, d) in densities)
{
    var tl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(p);
    if (!tl) { log.AppendLine("missing layer " + p); continue; }
    layers.Add(new GrassProfile.Layer { terrainLayer = tl, density = d });
    terrainLayers.Add(tl);
}
profile.layers = layers.ToArray();
// the colours: the inspector's reader wants a terrain's data, so a throwaway one with every layer
data.terrainLayers = terrainLayers.ToArray();
GrassFieldEditor.ReadLayerColours(profile, data);
UnityEngine.Object.DestroyImmediate(data);
// the Marsh's leafy ground is brown: its grass is a tired olive green, not brown
foreach (var l in profile.layers) if (l.terrainLayer.name.Contains("Dirt_Cracked_Leaves_Heavy")) l.colour = new UnityEngine.Color(0.36f, 0.37f, 0.17f);

profile.kinds = islands.kinds.Select(k => new GrassProfile.Kind
{
    name = k.name, mesh = k.mesh, blades = k.blades, bladeWidth = k.bladeWidth, material = k.material,
    perSquareMetre = k.perSquareMetre, size = k.size, minDensity = k.minDensity, enabled = k.enabled,
}).ToArray();
// the Marsh's water stands at -0.95 and Mossgate's pond at -0.45 (the tower's places sit at height 0): nothing grows under them
profile.waterLevel = -0.5f;
profile.maxSlope = islands.maxSlope;
profile.tint = islands.tint;
profile.colourJitter = islands.colourJitter;
profile.patches = islands.patches;
UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.AssetDatabase.SaveAssets();
foreach (var l in profile.layers) log.AppendLine(l.terrainLayer.name + " " + l.density.ToString("0.00") + " " + l.colour);
log.AppendLine("kinds " + profile.kinds.Length);
log.AppendLine("saved " + path);
} catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-grass-profile.txt"), log.ToString());
return null;
