import os
S = os.path.dirname(os.path.abspath(__file__)) + '/'
s = open(S + 'tower-crypt-build.cs', encoding='utf-8').read()


def rep(a, b, n=1):
    global s
    assert s.count(a) == n, (a[:80], s.count(a))
    s = s.replace(a, b)


rep('''// Builds Assets/Scenes/Tower/Tower_Crypt.unity: the crypt under the Overgrown Halls''',
    '''// Builds Assets/Scenes/Tower/Tower_FrostVault.unity: the ice vault of the Frozen Vault band (floors 13, 14, 17, 18,
// 20), made from the crypt's builder: the same rooms and walls, frozen over, cold blue light against warm braziers,
// icicles hanging off walls and pillars, snow drifted in the corners, ice crystals glowing blue. Was: the crypt under the Overgrown Halls''')
rep('"Tower_Crypt"', '"Tower_FrostVault"')
rep('"/Tower_Crypt"', '"/Tower_FrostVault"')
rep('"/Tower_Crypt.unity"', '"/Tower_FrostVault.unity"')
rep('var root = new UnityEngine.GameObject("Tower Floor - The Crypt");', 'var root = new UnityEngine.GameObject("Tower Floor - The Frost Vault");')
rep('''floor.SkyAmbient = new UnityEngine.Color(0.20f, 0.27f, 0.26f);
floor.EquatorAmbient = new UnityEngine.Color(0.13f, 0.17f, 0.15f);
floor.GroundAmbient = new UnityEngine.Color(0.07f, 0.08f, 0.07f);
floor.FogColor = new UnityEngine.Color(0.05f, 0.08f, 0.07f);
floor.FogDensity = 0.016f;
floor.SunColor = new UnityEngine.Color(0.62f, 0.80f, 0.72f);
floor.SunIntensity = 0.4f;''', '''floor.SkyAmbient = new UnityEngine.Color(0.42f, 0.52f, 0.68f);
floor.EquatorAmbient = new UnityEngine.Color(0.28f, 0.35f, 0.48f);
floor.GroundAmbient = new UnityEngine.Color(0.12f, 0.15f, 0.20f);
floor.FogColor = new UnityEngine.Color(0.30f, 0.40f, 0.54f);
floor.FogDensity = 0.011f;
floor.SunColor = new UnityEngine.Color(0.75f, 0.86f, 1f);
floor.SunIntensity = 0.75f;''')
rep('voidMat.color = new UnityEngine.Color(0.015f, 0.025f, 0.022f);', 'voidMat.color = new UnityEngine.Color(0.05f, 0.08f, 0.13f);')

# viking realm paths
rep('var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";', '''var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";
var ve = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";''')

# icicles off the pillar tops instead of vines
rep('''    if (!broken && rng.Next(3) != 0) Put(fv + "SM_Env_Vine_Large_0" + (1 + rng.Next(3)) + ".prefab", V(x + 0.9f, z), R() * 360f, One(1.1f), growth, false).transform.localPosition = new UnityEngine.Vector3(x, 3.5f + R() * 2f, z);''',
    '''    if (!broken && rng.Next(3) != 0) Put(ve + "SM_Env_Icicle_Small_Cluster_0" + (1 + rng.Next(2)) + ".prefab", V(x + 0.9f, z), R() * 360f, One(1.4f), growth, false).transform.localPosition = new UnityEngine.Vector3(x + 0.8f, 3.6f + R() * 1.5f, z);''')

# snow drifts instead of grass patches
rep('''    for (var k = 0; k < 5; k++) Put(pnb + "SM_Env_Grass_Med_Clump_0" + (1 + rng.Next(3)) + ".prefab", V(x + (R() - 0.5f) * 3f, z + (R() - 0.5f) * 3f), R() * 360f, One(0.8f + R() * 0.5f), growth);''',
    '''    for (var k = 0; k < 2; k++) Put(ve + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3)) + ".prefab", V(x + (R() - 0.5f) * 3f, z + (R() - 0.5f) * 3f), R() * 360f, One(0.35f + R() * 0.25f), growth);''')
# icicles hanging off the low walls instead of creeping vines
rep('''    var v = Put(fv + "SM_Env_Vine_Creeping_0" + (1 + rng.Next(7)) + ".prefab", root.transform.InverseTransformPoint(w.position), w.localEulerAngles.y, One(1.6f), growth, false);
    if (v) v.transform.position = w.position + UnityEngine.Vector3.up * 2.4f;''',
    '''    var v = Put(ve + (rng.Next(2) == 0 ? "SM_Env_Icicle_Small_Sloped_0" + (1 + rng.Next(2)) : "SM_Env_Icicle_Small_0" + (1 + rng.Next(3))) + ".prefab", root.transform.InverseTransformPoint(w.position), w.localEulerAngles.y, One(1.5f), growth, false);
    if (v) v.transform.position = w.position + UnityEngine.Vector3.up * 2.2f;''')
# snow at the wall feet instead of grass
rep('''    Put(pnb + (rng.Next(3) == 0 ? "SM_Env_Grass_Tall_Clump_0" + (1 + 2 * rng.Next(3)) : "SM_Env_Grass_Med_Clump_0" + (1 + rng.Next(3))) + ".prefab", V(x, z), R() * 360f, One(0.9f + R() * 0.5f), growth);''',
    '''    if (rng.Next(2) == 0) Put(ve + "SM_Env_Snow_Edge_0" + (1 + rng.Next(6)) + ".prefab", V(x, z), R() * 360f, One(0.6f + R() * 0.4f), growth);''')
rep('''for (var i = 0; i < 120; i++)''', '''for (var i = 0; i < 90; i++)''')
# snow hills in the corners instead of brambles
rep('''    Put("Assets/PolygonDungeonRealms/Prefabs/Environments/SM_Env_Bramble_0" + (1 + rng.Next(3)) + ".prefab", c, R() * 360f, One(1.8f + R() * 0.6f), growth);''',
    '''    Put(ve + "SM_Env_Snow_Hill_0" + (1 + rng.Next(3)) + ".prefab", c, R() * 360f, One(0.5f + R() * 0.3f), growth);''')
# ice crystals (icicles turned upright) instead of giant mushrooms
rep('''    Put(e + "Misc/SM_Env_Mushroom_Giant_0" + (1 + rng.Next(5)) + ".prefab", m, R() * 360f, One(1.2f + R() * 0.8f), growth);
    for (var k = 0; k < 3; k++) Put(e + "Misc/SM_Env_Mushroom_Small_0" + (1 + rng.Next(4)) + ".prefab", m + V((R() - 0.5f) * 3f, (R() - 0.5f) * 3f), R() * 360f, One(1f + R()), growth);''',
    '''    for (var k = 0; k < 4; k++)
    {
        var crystal = Put(ve + "SM_Env_Icicle_0" + (1 + rng.Next(4)) + ".prefab", m + V((R() - 0.5f) * 2.5f, (R() - 0.5f) * 2.5f), R() * 360f, One(1.6f + R() * 1.4f), growth, false);
        if (crystal) { crystal.transform.localRotation = UnityEngine.Quaternion.Euler(180f + (R() - 0.5f) * 30f, R() * 360f, (R() - 0.5f) * 30f); crystal.transform.localPosition += UnityEngine.Vector3.up * 0.2f; }
    }''')
# icicles over the throne hall's walls instead of roots
rep('''    Put(e + "Misc/SM_Env_Roots_0" + (1 + rng.Next(2)) + ".prefab", V(x, z), R() * 360f, One(4f + R() * 2f), growth);''',
    '''    var ic = Put(ve + "SM_Env_Icicle_0" + (1 + rng.Next(4)) + ".prefab", V(x, z), R() * 360f, One(2.2f + R()), growth, false);
    if (ic) ic.transform.localPosition = new UnityEngine.Vector3(x, 3.4f, z);''')
# no cobwebs in the cold
rep('''        if (rng.Next(2) == 0) Put(dr + "SM_Env_SpiderWeb_0"''', '''        if (rng.Next(9) == 0) Put(dr + "SM_Env_SpiderWeb_0"''')
# the floor: frosted pale blue, not darkened green
rep('''            dm = new UnityEngine.Material(m0) { name = m0.name + " Crypt" };
            var col = dm.HasProperty("_BaseColor") ? dm.GetColor("_BaseColor") : dm.color;
            col = new UnityEngine.Color(col.r * 0.62f, col.g * 0.66f, col.b * 0.62f, col.a);''',
    '''            dm = new UnityEngine.Material(m0) { name = m0.name + " Vault" };
            var col = dm.HasProperty("_BaseColor") ? dm.GetColor("_BaseColor") : dm.color;
            col = new UnityEngine.Color(col.r * 0.78f, col.g * 0.88f, col.b * 1.08f, col.a);
            if (dm.HasProperty("_Smoothness")) dm.SetFloat("_Smoothness", 0.45f);''')
# the crystals glow ice blue
rep('''light.color = new UnityEngine.Color(0.45f, 1f, 0.75f); light.range = 9f; light.intensity = 1.2f;''', '''light.color = new UnityEngine.Color(0.55f, 0.8f, 1f); light.range = 10f; light.intensity = 1.6f;''')
rep('var l = new UnityEngine.GameObject("Mushroom Glow");', 'var l = new UnityEngine.GameObject("Crystal Glow");')
# the band's looks
rep('''var models = new[] { "home-bunny", "ravebridge-goblin", "home-metalon-green", "home-metalon-purple", "home-metalon-red", "ravebridge-skeleton", "home-cow", "ironhill-dragon-green" };''',
    '''var models = new[] { "iceheim-viking", "home-bunny", "ravebridge-skeleton", "ravebridge-tormented-soul", "ironhill-lord-quackers", "iceheim-viking-chief", "ironhill-dragon-white", "kyojin-samurai", "home-cow" };''')
rep('''    Put(dr + (rng.Next(2) == 0 ? "SM_Env_Rubble_Slab_0" + (1 + rng.Next(5)) : "SM_Env_Rubble_Rocks_0" + (1 + rng.Next(3))) + ".prefab", V(x, z), R() * 360f, One(0.9f + R() * 0.5f), props);''',
    '''    Put(ve + "SM_Env_Stone_0" + (1 + rng.Next(4)) + ".prefab", V(x, z), R() * 360f, One(0.4f + R() * 0.35f), props);''')

# snow drifts with a few bones instead of the crypt's dirt-and-bone piles
rep('''    for (var i = 0; i < 6; i++) Put(e + "Bones/SM_Env_BonePile_0" + (1 + rng.Next(3)) + ".prefab", V(side * (18f + R() * 24f), -36f + R() * 24f), R() * 360f, One(1f + R() * 0.5f), props);''',
    '''    for (var i = 0; i < 6; i++) { var bp = V(side * (18f + R() * 24f), -36f + R() * 24f); Put(ve + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3)) + ".prefab", bp, R() * 360f, One(0.45f + R() * 0.25f), props); Put(e + "Bones/SM_Env_Bone_Skull_Jaw_0" + (1 + rng.Next(2)) + ".prefab", bp + V(1.2f, 0.4f), R() * 360f, One(1f), props); }''')
rep('"tower-crypt-build.txt"', '"tower-frostvault-build.txt"')

# the Viking Realm's snow mounds and hills are dirt with a snow cap: in the vault's light they read brown. White trims instead.
s = s.replace('ve + "SM_Env_Snow_Mound_0" + (1 + rng.Next(3))', 've + "SM_Env_Trim_Snow_0" + (1 + rng.Next(7))')
s = s.replace('ve + "SM_Env_Snow_Hill_0" + (1 + rng.Next(3))', 've + "SM_Env_Snow_Edge_0" + (1 + rng.Next(6))')
s = s.replace('Put(e + "Bones/" + (rng.Next(2) == 0 ? "SM_Env_Bone_Skull_Jaw_0" + (1 + rng.Next(2)) : "SM_Env_BonePile_Small_0" + (1 + rng.Next(2))) + ".prefab", V(x, z), R() * 360f, One(1f), props);', 'Put(ve + "SM_Env_Trim_Snow_0" + (1 + rng.Next(7)) + ".prefab", V(x, z), R() * 360f, One(0.7f), props);')
s = s.replace(' Put(e + "Bones/SM_Env_Bone_Skull_Jaw_0" + (1 + rng.Next(2)) + ".prefab", bp + V(1.2f, 0.4f), R() * 360f, One(1f), props); }', ' }')
open(S + 'tower-frostvault-build.cs', 'w', encoding='utf-8').write(s)
print('ok', s.count('Crypt'))
