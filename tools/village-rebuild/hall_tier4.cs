// Tier 4: the keep. Crenellated stone keep with round towers and blue cones, knight statues, a fountain plaza.
// The keep has no roof: from above you look down into its great hall.
{
    var root = tierRoot(4);
    var hall = freshHall(4);
    Patch(hall, "Plaza", new UnityEngine.Vector3(0, 0, 3.5f), 18f, 15f, 0.05f, cobbleMat, 3f);
    // keep: 15 x 10, two storeys, battlements on top
    WallRun(hall, new[] { "Wall_01", "Wall_Door_Big_01", "Wall_01" }, -7.5f, 8f, 0, 0f);
    WallRun(hall, new[] { "Wall_Window_01", "Wall_Window_Fancy_01", "Wall_Window_01" }, -7.5f, 8f, 0, 5f);
    WallRun(hall, new[] { "Wall_01", "Wall_Fireplace_01", "Wall_01" }, -7.5f, 18f, 0, 0f);
    WallRun(hall, new[] { "Wall_Window_02", "Wall_Window_02", "Wall_Window_02" }, -7.5f, 18f, 0, 5f);
    foreach (var x in new[] { -7.5f, 7.5f })
    {
        WallRun(hall, new[] { "Wall_Window_01", "Wall_Window_01" }, x, 8f, 1, 0f);
        WallRun(hall, new[] { "Wall_Window_02", "Wall_Window_02" }, x, 8f, 1, 5f);
        WallRun(hall, new[] { "Battlements_01", "Battlements_01" }, x, 8f, 1, 10f);
    }
    WallRun(hall, new[] { "Battlements_01", "Battlements_01", "Battlements_01" }, -7.5f, 8f, 0, 10f);
    WallRun(hall, new[] { "Battlements_01", "Battlements_01", "Battlements_01" }, -7.5f, 18f, 0, 10f);
    // towers: big ones at the front corners, small at the back
    // our own round towers (blender/tower.py); the kit's tower segments have a doorway on every level
    foreach (var x in new[] { -7.5f, 7.5f })
    {
        Put(hall, VM + "Village_Tower_T4_Front.fbx", new UnityEngine.Vector3(x, 0, 8f), 0, 1f);
        Put(hall, VM + "Village_Tower_T4_Back.fbx", new UnityEngine.Vector3(x, 0, 18f), 0, 1f);
    }
    // banners down the front wall
    Banner(hall, new UnityEngine.Vector3(-3.6f, 9.6f, 7.55f), 0, 1f);
    Banner(hall, new UnityEngine.Vector3(3.6f, 9.6f, 7.55f), 0, 1f);
    // great hall inside
    Put(hall, FKP + "SM_Prop_Rug_01.prefab", new UnityEngine.Vector3(0, 0.06f, 13f), 90, 1.6f);
    Put(hall, FKP + "Furniture/SM_Prop_Throne_01.prefab", new UnityEngine.Vector3(0, 0, 16.5f), 180, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Table_Fancy_01.prefab", new UnityEngine.Vector3(-3.5f, 0, 12.5f), 90, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Chair_Fancy_01.prefab", new UnityEngine.Vector3(-4.6f, 0, 12.5f), 90, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Chair_Fancy_01.prefab", new UnityEngine.Vector3(-2.4f, 0, 12.5f), -90, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Cabinet_Stackable_01.prefab", new UnityEngine.Vector3(6.6f, 0, 11f), -90, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Cabinet_Stackable_02.prefab", new UnityEngine.Vector3(6.6f, 0, 13f), -90, 1f);
    Put(hall, FKP + "SM_Prop_Chandelier_01.prefab", new UnityEngine.Vector3(0, 7.5f, 13f), 0, 1.2f);
    // plaza: fountain, knights at the gate, torches, lamps, planters, side parapets
    Put(hall, FK + "Environments/SM_Env_Fountain_01.prefab", new UnityEngine.Vector3(0, 0, -3f), 0, 1.25f);
    foreach (var x in new[] { -5f, 5f })
    {
        Put(hall, FKP + "SM_Prop_Statue_Plinth_01.prefab", new UnityEngine.Vector3(x, 0, 4.5f), 0, 1f);
        Put(hall, FKP + "SM_Prop_Statue_Knight_01.prefab", new UnityEngine.Vector3(x, 1.9f, 4.5f), 180, 1f);
        Fire(hall, new UnityEngine.Vector3(x * 0.5f, 0, 6.6f), false);
        Put(hall, FKP + "SM_Prop_Street_Lamp_02.prefab", new UnityEngine.Vector3(x * 2.2f, 0, -9f), 0, 1.3f);
        Put(hall, FKP + "SM_Prop_PlanterBox_Preset_02.prefab", new UnityEngine.Vector3(x * 2.4f, 0, 2.5f), 90, 1f);
    }
    foreach (var x in new[] { -4.2f, 4.2f }) Put(hall, FKP + "Furniture/SM_Prop_Bench_Seat_02.prefab", new UnityEngine.Vector3(x, 0, -3f), x < 0 ? 90 : -90, 1f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01.prefab", new UnityEngine.Vector3(-13f, 0, 4f), 210, 1f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01_Posters_01.prefab", new UnityEngine.Vector3(-13f, 0, 4f), 210, 1f);
    Keep(root, "Resources", new UnityEngine.Vector3(22f, 0, 12f), -90);
    Keep(root, "InfoPos (2)", new UnityEngine.Vector3(0, 9f, -16f), 0);
    Keep(root, "StreamerSpawn (2)", new UnityEngine.Vector3(-13f, 0, 9f), 0);
    Keep(root, "Chr_FantasyHero_Preset_113", new UnityEngine.Vector3(4f, 0, 15.5f), 200);
    Keep(root, "SM_Prop_Wall_Banner_01", new UnityEngine.Vector3(-3.5f, 4.2f, 17.6f), 180);
    var keepBox = new UnityEngine.GameObject("Keep Bounds"); keepBox.transform.SetParent(hall, false);
    var box = root.GetComponent<UnityEngine.BoxCollider>(); box.center = new UnityEngine.Vector3(0, 7.5f, 13f); box.size = new UnityEngine.Vector3(19f, 15f, 14f);
    UnityEngine.Object.DestroyImmediate(keepBox);
    GrowCollider(root);
    sb.Append("tier4 built, children=").Append(hall.childCount).Append(" || ");
}
