// Run with: unity command eval_file --file tools/hero/hero-effects.cs
//
// Makes Assets/World/Hero/Resources/HeroEffects.asset (HeroEffects): the looks of hero mode's skills, from the AAA
// Projectiles pack (the transform moved projectiles, which carry their own hit and flash) and the game's arrow. The
// pack names its prefabs by number only; the comments say what each looks like. Also the HeroOrb and HeroMarker
// materials (the HUD's orbs, the click marker). Run it again after changing a pick.
var pack = "Assets/ErbGameArt/AAA Projectiles Vol 1/Prefabs/";
System.Func<string, UnityEngine.GameObject> load = p => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
System.Func<int, UnityEngine.GameObject> projectile = n => load(pack + "Projectiles(transform)/Projectile " + n + ".prefab");
System.Func<int, UnityEngine.GameObject> hit = n => load(pack + "Hit " + n + ".prefab");

var path = "Assets/World/Hero/Resources/HeroEffects.asset";
var fx = UnityEditor.AssetDatabase.LoadAssetAtPath<HeroEffects>(path);
if (!fx)
{
    fx = UnityEngine.ScriptableObject.CreateInstance<HeroEffects>();
    UnityEditor.AssetDatabase.CreateAsset(fx, path);
}
fx.Arrow = load("Assets/Prefabs/Arrow.prefab"); // the arrow the game's archers shoot
fx.HolyBolt = projectile(1);   // pale green
fx.Fireball = projectile(25);  // orange fire
fx.Lightning = projectile(22); // light blue
fx.Meteor = projectile(16);    // deep orange
fx.Smite = projectile(4);      // gold
fx.ArrowRain = hit(23);        // small orange sparks
fx.Snare = hit(12);            // dark green
fx.FrostNova = hit(8);         // ice blue
fx.MeteorBlast = hit(3);       // orange red
fx.Mend = hit(24);             // bright green
fx.Sanctuary = hit(6);         // green
fx.Angel = hit(14);            // white
fx.Cheer = hit(4);             // yellow sparkle
fx.Arrival = hit(7);           // purple puff where a wave enemy comes in
// loot on the ground (LootDrop): a coin, a pouch for items, a potion bottle
fx.LootCoin = load("Assets/EpiXR_PolygonHats/Prefabs/Lp_Coin01.prefab");
fx.LootPouch = load("Assets/PolygonAdventure/Prefabs/Items/SM_Item_Pouch_01.prefab");
fx.LootPotion = load("Assets/PolygonAdventure/Prefabs/Items/SM_Item_Potion_02.prefab");
UnityEditor.EditorUtility.SetDirty(fx);
UnityEditor.AssetDatabase.SaveAssets();

// the materials the HUD orbs and the click marker are drawn with: kept in Resources so builds include their shaders
foreach (var (file, shaderName) in new[] { ("HeroOrb", "Ravenfall/Hero Orb"), ("HeroMarker", "Ravenfall/Hero Marker"), ("HeroOrbLight", "Shader Graphs/HeroOrbLight") })
{
    var shader = UnityEngine.Shader.Find(shaderName);
    if (!shader) return "no shader " + shaderName;
    var materialPath = "Assets/World/Hero/Resources/" + file + ".mat";
    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
    if (!material) UnityEditor.AssetDatabase.CreateAsset(new UnityEngine.Material(shader), materialPath);
    else if (material.shader != shader) material.shader = shader;
}
UnityEditor.AssetDatabase.SaveAssets();

// the sword trail's material: URP's unlit particles, transparent and additive
var trailPath = "Assets/World/Hero/Resources/HeroTrail.mat";
var trailMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(trailPath);
if (!trailMaterial)
{
    trailMaterial = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit"));
    UnityEditor.AssetDatabase.CreateAsset(trailMaterial, trailPath);
}
trailMaterial.SetFloat("_Surface", 1f);
trailMaterial.SetFloat("_Blend", 2f);
trailMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
trailMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
trailMaterial.SetFloat("_ZWrite", 0f);
trailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
trailMaterial.renderQueue = 3000;
trailMaterial.SetColor("_BaseColor", UnityEngine.Color.white);
UnityEditor.EditorUtility.SetDirty(trailMaterial);
UnityEditor.AssetDatabase.SaveAssets();

// the orbs' artwork (docs/Unity-Glass-Orb-Kit, at 512 px): the glass as colour, the liquid body and the flow noise as
// data (linear), the flow mirrored at its edges so its seams never show; on the HeroOrb material
var textures = "Assets/World/Hero/Textures/";
foreach (var (file, srgb, wrap) in new[] { ("HeroOrbGlass", true, UnityEngine.TextureWrapMode.Clamp), ("HeroOrbBody", false, UnityEngine.TextureWrapMode.Clamp), ("HeroOrbFlow", false, UnityEngine.TextureWrapMode.Mirror) })
{
    var importer = UnityEditor.AssetImporter.GetAtPath(textures + file + ".png") as UnityEditor.TextureImporter;
    if (!importer) return "no " + file;
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.sRGBTexture = srgb;
    importer.wrapMode = wrap;
    importer.mipmapEnabled = true;
    importer.alphaIsTransparency = srgb;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.SaveAndReimport();
}
var orbMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Hero/Resources/HeroOrb.mat");
orbMaterial.SetTexture("_Glass", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(textures + "HeroOrbGlass.png"));
orbMaterial.SetTexture("_Body", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(textures + "HeroOrbBody.png"));
orbMaterial.SetTexture("_Flow", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(textures + "HeroOrbFlow.png"));
UnityEditor.EditorUtility.SetDirty(orbMaterial);
UnityEditor.AssetDatabase.SaveAssets();

var missing = new System.Collections.Generic.List<string>();
foreach (var f in typeof(HeroEffects).GetFields())
{
    if (f.FieldType == typeof(UnityEngine.GameObject) && !(UnityEngine.GameObject)f.GetValue(fx)) missing.Add(f.Name);
}
return path + (missing.Count > 0 ? ", missing: " + string.Join(", ", missing) : ", all set");
