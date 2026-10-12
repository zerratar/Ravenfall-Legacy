// Tier 3: the guild hall. Stone and timber hall with a bell tower on a raised terrace, cobbled forecourt, torches.
{
    var root = tierRoot(3);
    var hall = freshHall(3);
    Patch(hall, "Forecourt", new UnityEngine.Vector3(0, 0, 0f), 17f, 14f, 0.08f, cobbleMat, 3f);
    // terrace: 1 m stone blocks in a 4x2 grid behind the forecourt, the hall stands on it
    const float T = 0.6f; // terrace height
    for (var x = 0; x < 5; ++x) for (var z = 0; z < 3; ++z)
    {
        var blk = Put(hall, FKC + "Wall_Block_05.prefab", new UnityEngine.Vector3(-11f + x * 5.5f, 0, 6f + z * 5.5f), 0, 1f);
        blk.transform.localScale = new UnityEngine.Vector3(1f, T, 1f);
    }
    Patch(hall, "Terrace Top", new UnityEngine.Vector3(0, T - 0.04f, 11.5f), 13.6f, 8.2f, 0.0f, cobbleMat, 3f);
    // two broad steps up to the terrace
    for (var st = 0; st < 2; ++st)
    {
        var step = Put(hall, FKC + "Wall_Block_05.prefab", new UnityEngine.Vector3(0, 0, 2.6f - st * 0.9f), 0, 1f);
        step.transform.localScale = new UnityEngine.Vector3(1.5f, T * (2 - st) / 3f, 0.35f);
    }
    var house = Put(hall, FKB + "SM_Bld_Preset_House_10_Optimized.prefab", new UnityEngine.Vector3(1f, T, 18f), 0, 1.15f);
    // the house's own little tower is trimmed off (everything above the ridge) and the clock and bell
    // tower from blender/clock_tower.py stands in its place; it is modelled in the house's space
    var houseMf = house.GetComponent<UnityEngine.MeshFilter>();
    houseMf.sharedMesh = TrimAbove(houseMf.sharedMesh, 10.2f, "SM_Bld_Preset_House_10_NoTower");
    Put(house.transform, VM + "Village_ClockTower.fbx", UnityEngine.Vector3.zero, 0, 1f).name = "Clock Tower";
    // torches on pillars either side of the steps, planters along the terrace edge
    foreach (var x in new[] { -4.5f, 4.5f })
    {
        Put(hall, FKC + "Battlements_Pillar_01.prefab", new UnityEngine.Vector3(x, 0, 2.6f), 0, 1.3f);
        Fire(hall, new UnityEngine.Vector3(x, 0, 2.6f), false);
    }
    foreach (var x in new[] { -9f, 9f }) Put(hall, FKP + "SM_Prop_PlanterBox_Preset_01.prefab", new UnityEngine.Vector3(x, T, 3.9f), 0, 1f);
    // low walls round the forecourt sides
    // low parapets along the forecourt sides (pieces span -5..0 on their local x)
    for (var i = 0; i < 2; ++i)
    {
        Put(hall, FKC + "Battlements_01.prefab", new UnityEngine.Vector3(-15.5f, 0, -9f + i * 5f), -90, 0.8f);
        Put(hall, FKC + "Battlements_01.prefab", new UnityEngine.Vector3(15.5f, 0, -4f + i * 5f), 90, 0.8f);
    }
    Put(hall, FKP + "SM_Prop_Street_Lamp_02.prefab", new UnityEngine.Vector3(-7f, 0, -9f), 0, 1.3f);
    Put(hall, FKP + "SM_Prop_Street_Lamp_02.prefab", new UnityEngine.Vector3(7f, 0, -9f), 0, 1.3f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01.prefab", new UnityEngine.Vector3(-11f, 0, -3f), 225, 1f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01_Posters_01.prefab", new UnityEngine.Vector3(-11f, 0, -3f), 225, 1f);
    foreach (var x in new[] { -12.5f, 12.5f })
    {
        var pole = Put(hall, FKP + "Banners/SM_Prop_Flag_Pole_02.prefab", new UnityEngine.Vector3(x, T, 4.5f), 0, 1f);
        pole.transform.localScale = new UnityEngine.Vector3(1f, 2f, 1f);
        Put(hall, FKP + "Banners/SM_Prop_Flag_11.prefab", new UnityEngine.Vector3(x, T + 8.1f, 4.55f), 0, 1f);
    }
    Keep(root, "Resources", new UnityEngine.Vector3(19f, 0, 10f), -90);
    Keep(root, "InfoPos (2)", new UnityEngine.Vector3(0, 7f, -14f), 0);
    Keep(root, "StreamerSpawn (3)", new UnityEngine.Vector3(13f, T, 7f), 0);
    Keep(root, "Chr_FantasyHero_Preset_113", new UnityEngine.Vector3(0, T, 9f), 180);
    Keep(root, "SM_Prop_Wall_Banner_01", new UnityEngine.Vector3(-3.5f, 4.5f, 8.2f), 0);
    FitCollider(root, house);
    sb.Append("tier3 built, children=").Append(hall.childCount).Append(" || ");
}
