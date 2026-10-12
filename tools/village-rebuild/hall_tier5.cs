// Tier 5: the grand castle. Three storey keep with a tall spired tower, corner turrets, stained glass,
// a statue fountain on a big plaza, guards, the jester, and the residents of the great hall.
{
    var root = tierRoot(5);
    var hall = freshHall(5);
    Patch(hall, "Plaza", new UnityEngine.Vector3(0, 0, 3f), 24f, 19f, 0.04f, cobbleMat, 3f);
    const float Z0 = 10f, Z1 = 25f, X0 = -12.5f;
    // front: door in the middle, fancy windows, a tall stained glass window over the door
    WallRun(hall, new[] { "Wall_01", "Wall_Window_Fancy_01", "Wall_Door_Big_01", "Wall_Window_Fancy_01", "Wall_01" }, X0, Z0, 0, 0f);
    WallRun(hall, new[] { "Wall_Window_01", "Wall_Window_Fancy_01", "Wall_Window_Fancy_Tall_01", "Wall_Window_Fancy_01", "Wall_Window_01" }, X0, Z0, 0, 5f);
    WallRun(hall, new[] { "Wall_01", "Wall_Window_02", null, "Wall_Window_02", "Wall_01" }, X0, Z0, 0, 10f);
    // back
    WallRun(hall, new[] { "Wall_01", "Wall_Fireplace_01", "Wall_01", "Wall_Fireplace_01", "Wall_01" }, X0, Z1, 0, 0f);
    WallRun(hall, new[] { "Wall_Window_02", "Wall_Window_02", "Wall_01", "Wall_Window_02", "Wall_Window_02" }, X0, Z1, 0, 5f);
    WallRun(hall, new[] { "Wall_01", "Wall_Window_01", "Wall_01", "Wall_Window_01", "Wall_01" }, X0, Z1, 0, 10f);
    foreach (var x in new[] { X0, -X0 })
    {
        WallRun(hall, new[] { "Wall_Window_01", "Wall_Window_Fancy_01", "Wall_Window_01" }, x, Z0, 1, 0f);
        WallRun(hall, new[] { "Wall_Window_02", "Wall_Window_02", "Wall_Window_02" }, x, Z0, 1, 5f);
        WallRun(hall, new[] { "Wall_01", "Wall_Window_01", "Wall_01" }, x, Z0, 1, 10f);
        WallRun(hall, new[] { "Battlements_01", "Battlements_01", "Battlements_01" }, x, Z0, 1, 15f);
    }
    var crenel = new[] { "Battlements_01", "Battlements_01", "Battlements_01", "Battlements_01", "Battlements_01" };
    WallRun(hall, crenel, X0, Z0, 0, 15f);
    WallRun(hall, crenel, X0, Z1, 0, 15f);
    // towers: the great spire at the back, big front corners, smaller back corners, little turrets on the front wall
    Tower(hall, "Wall_Tower_L_01", "Roof_Cap_Round_04", new UnityEngine.Vector3(0, 0, Z1 + 1.5f), 6);
    Tower(hall, "Wall_Tower_L_01", "Roof_Cap_Round_06", new UnityEngine.Vector3(X0, 0, Z0), 4);
    Tower(hall, "Wall_Tower_L_01", "Roof_Cap_Round_06", new UnityEngine.Vector3(-X0, 0, Z0), 4);
    Tower(hall, "Wall_Tower_M_01", "Roof_Cap_Round_03", new UnityEngine.Vector3(X0, 0, Z1), 4);
    Tower(hall, "Wall_Tower_M_01", "Roof_Cap_Round_03", new UnityEngine.Vector3(-X0, 0, Z1), 4);
    Tower(hall, "Wall_Tower_S_01", "Roof_Cap_Round_05", new UnityEngine.Vector3(-5f, 15f, Z0), 1);
    Tower(hall, "Wall_Tower_S_01", "Roof_Cap_Round_05", new UnityEngine.Vector3(5f, 15f, Z0), 1);
    foreach (var x in new[] { -8.5f, -2.6f, 2.6f, 8.5f }) Banner(hall, new UnityEngine.Vector3(x, 14.6f, Z0 - 0.45f), 0, 1.3f);
    // great hall, open to the sky so you can look in from above
    for (var fx = 0; fx < 5; ++fx) for (var fz = 0; fz < 3; ++fz)
        Put(hall, FKC + "Floor_Stone_01.prefab", new UnityEngine.Vector3(X0 + 5f * (fx + 1), 0.12f, Z0 + 5f * (fz + 1)), 0, 1f);
    Put(hall, FKP + "SM_Prop_Rug_01.prefab", new UnityEngine.Vector3(0, 0.18f, 17.5f), 90, 2.2f);
    Put(hall, FKP + "Furniture/SM_Prop_Throne_02.prefab", new UnityEngine.Vector3(0, 0, 23.2f), 180, 1.2f);
    for (var i = 0; i < 3; ++i)
    {
        Put(hall, FKP + "Furniture/SM_Prop_Table_Wood_01.prefab", new UnityEngine.Vector3(-6.5f, 0, 13.5f + i * 3.2f), 90, 1f);
        Put(hall, FKP + "Furniture/SM_Prop_Table_Wood_01.prefab", new UnityEngine.Vector3(6.5f, 0, 13.5f + i * 3.2f), 90, 1f);
    }
    foreach (var x in new[] { -9.8f, 9.8f }) for (var i = 0; i < 3; ++i)
        Put(hall, FKP + "Furniture/SM_Prop_Cabinet_Stackable_01.prefab", new UnityEngine.Vector3(x + (x < 0 ? -1.6f : 1.6f), 0, 13f + i * 2.2f), x < 0 ? 90 : -90, 1f);
    Put(hall, FKP + "SM_Prop_Chandelier_01.prefab", new UnityEngine.Vector3(0, 11f, 14.5f), 0, 1.5f);
    Put(hall, FKP + "SM_Prop_Chandelier_01.prefab", new UnityEngine.Vector3(0, 11f, 20.5f), 0, 1.5f);
    // plaza: statue fountain, knights, guards at the door, torches, lamps, benches, planters, stalls
    Put(hall, "Assets/PolygonElvenRealm/Prefabs/Props/SM_Prop_Fountain_01.prefab", new UnityEngine.Vector3(0, 0, -4f), 0, 1.1f);
    var knights = new System.Collections.Generic.List<UnityEngine.GameObject>();
    foreach (var x in new[] { -8.5f, 8.5f })
    {
        Put(hall, FKP + "SM_Prop_Statue_Plinth_01.prefab", new UnityEngine.Vector3(x, 0, 6f), 0, 1.1f);
        knights.Add(Put(hall, FKP + "SM_Prop_Statue_Knight_01.prefab", new UnityEngine.Vector3(x, 2.1f, 6f), 180, 1.1f));
        Fire(hall, new UnityEngine.Vector3(x * 0.66f, 0, 9f), false);
        Put(hall, FKP + "SM_Prop_Street_Lamp_02.prefab", new UnityEngine.Vector3(x * 1.35f, 0, -10f), 0, 1.35f);
        Put(hall, FKP + "SM_Prop_PlanterBox_Preset_03.prefab", new UnityEngine.Vector3(x * 0.75f, 0, 3f), 90, 1.1f);
        Put(hall, FKP + "Furniture/SM_Prop_Bench_Seat_02.prefab", new UnityEngine.Vector3(x * 0.6f, 0, -4f), x < 0 ? 90 : -90, 1f);
    }
    Put(hall, FKP + "SM_Prop_Market_Stall_03.prefab", new UnityEngine.Vector3(-17f, 0, -4f), 90, 1.15f);
    Put(hall, FKP + "SM_Prop_Market_Stall_02.prefab", new UnityEngine.Vector3(17f, 0, -4f), -90, 1.15f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01_Posters_01.prefab", new UnityEngine.Vector3(-14f, 0, 4f), 205, 1f);
    // easter eggs
    HatOn(knights[1], "Assets/EpiXR_PolygonHats/Prefabs/Lp_TrafficCone01.prefab", 3.2f, 0.25f).name = "Very Official Knight Cone";
    var chicken = Put(hall, "Assets/Animals_FREE/Prefabs/Chicken_001.prefab", new UnityEngine.Vector3(2.6f, 1.05f, -4f), 200, 1f);
    if (chicken)
    {
        chicken.name = "Sir Clucksworth the Crowned";
        foreach (var mb in chicken.GetComponentsInChildren<UnityEngine.MonoBehaviour>()) mb.enabled = false;
        HatOn(chicken, "Assets/EpiXR_PolygonHats/Prefabs/Lp_CrownHat01.prefab", 1.2f, 0.06f);
    }
    Put(hall, FKP + "SM_Prop_Statue_Plinth_01.prefab", new UnityEngine.Vector3(12f, 0, -12f), 0, 0.9f);
    var raven = Put(hall, "Assets/Assets/Asset Series/Quirky Series/Forest/Crow/Prefab/Crow.prefab", new UnityEngine.Vector3(12f, 1.71f, -12f), 200, 0.35f);
    if (raven)
    {
        raven.name = "Party Raven";
        foreach (var mb in raven.GetComponentsInChildren<UnityEngine.MonoBehaviour>()) mb.enabled = false;
        HatOn(raven, "Assets/EpiXR_PolygonHats/Prefabs/Lp_PartyHat01.prefab", 0.9f, 0.04f);
    }
    // kept pieces
    Keep(root, "SM_Prop_Quest_Board_01", new UnityEngine.Vector3(-14f, 0, 4f), 205);
    Keep(root, "SM_Chr_Jester_01", new UnityEngine.Vector3(-5.5f, 0, -8f), 160);
    Keep(root, "SM_Chr_Soldier_Male_01", new UnityEngine.Vector3(-3.2f, 0, 8.6f), 180);
    Keep(root, "SM_Chr_Soldier_Female_01", new UnityEngine.Vector3(3.2f, 0, 8.6f), 180);
    Keep(root, "Resources", new UnityEngine.Vector3(26f, 0, 14f), -90);
    Keep(root, "InfoPos (2)", new UnityEngine.Vector3(0, 12f, -20f), 0);
    Keep(root, "StreamerSpawn (1)", new UnityEngine.Vector3(-18f, 0, 10f), 0);
    Keep(root, "Chr_FantasyHero_Preset_113", new UnityEngine.Vector3(5f, 0, 22f), 200);
    Keep(root, "SM_Prop_Wall_Banner_01", new UnityEngine.Vector3(-5f, 5.5f, Z1 - 0.45f), 180);
    var box = root.GetComponent<UnityEngine.BoxCollider>(); box.center = new UnityEngine.Vector3(0, 10f, 17.5f); box.size = new UnityEngine.Vector3(29f, 20f, 18f);
    sb.Append("tier5 built, children=").Append(hall.childCount).Append(" chicken=").Append(chicken != null).Append(" raven=").Append(raven != null).Append(" || ");
}
