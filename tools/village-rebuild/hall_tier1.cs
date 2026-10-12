// Tier 1: the village longhouse. Thatched cottage, open shelter, campfire, notice board, a banner.
{
    var root = tierRoot(1);
    var hall = freshHall(1);
    Patch(hall, "Yard", new UnityEngine.Vector3(-0.5f, 0, 2f), 13f, 11f, 0.16f, dirtMat, 4f);
    var house = Put(hall, FKB + "SM_Bld_Preset_House_09_B_Optimized.prefab", new UnityEngine.Vector3(3f, 0, 11f), 0, 1.3f);
    Put(hall, FKB + "SM_Bld_Preset_Shelter_02_Optimized.prefab", new UnityEngine.Vector3(-8.5f, 0, 9f), 0, 1.05f);
    // fire ring with log seats
    Put(hall, FKP + "SM_Prop_FirePit_01.prefab", new UnityEngine.Vector3(-1f, 0, -4f), 0, 1.8f);
    Fire(hall, new UnityEngine.Vector3(-1f, 0, -4f), true);
    Put(hall, FKP + "Furniture/SM_Prop_Bench_Seat_01.prefab", new UnityEngine.Vector3(-4.2f, 0, -4f), 90, 1f);
    Put(hall, FKP + "Furniture/SM_Prop_Bench_Seat_01.prefab", new UnityEngine.Vector3(-0.6f, 0, -7.2f), 0, 1f);
    Put(hall, FK + "Generic/SM_Generic_TreeStump_01.prefab", new UnityEngine.Vector3(2.4f, 0, -3.2f), 0, 0.7f);
    // quest board and banner
    Put(hall, FKP + "SM_Prop_Quest_Board_01.prefab", new UnityEngine.Vector3(7.5f, 0, -3.5f), 155, 1f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01_Posters_01.prefab", new UnityEngine.Vector3(7.5f, 0, -3.5f), 155, 1f);
    var pole = Put(hall, FKP + "Banners/SM_Prop_Flag_Pole_02.prefab", new UnityEngine.Vector3(9.5f, 0, 2f), 0, 1f);
    pole.transform.localScale = new UnityEngine.Vector3(1f, 1.75f, 1f);
    Put(hall, FKP + "Banners/SM_Prop_Flag_11.prefab", new UnityEngine.Vector3(9.5f, 7.1f, 2.05f), 0, 1f);
    // yard clutter
    Put(hall, FKP + "SM_Prop_Wood_Pile_01.prefab", new UnityEngine.Vector3(-9.5f, 0, 0f), 90, 1f);
    Put(hall, FKP + "SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(7f, 0, 3.6f), 0, 1f);
    Put(hall, FKP + "SM_Prop_Barrel_01.prefab", new UnityEngine.Vector3(7.8f, 0, 4.4f), 40, 1f);
    Put(hall, FKP + "SM_Prop_Crate_Wood_01.prefab", new UnityEngine.Vector3(-3.5f, 0, 1f), 15, 1f);
    Put(hall, FKP + "SM_Prop_Hay_Bale_01.prefab", new UnityEngine.Vector3(-8f, 0, 3.5f), 10, 1f);
    Put(hall, FKP + "SM_Prop_Sack_Stack_01.prefab", new UnityEngine.Vector3(-5f, 0, 6f), 0, 1f);
    // fence along the left and front-left of the yard
    for (var i = 0; i < 5; ++i) Put(hall, FKP + "SM_Prop_Fence_01.prefab", new UnityEngine.Vector3(-12.5f, 0, -5f + i * 2.4f), 90, 1f);
    for (var i = 0; i < 3; ++i) Put(hall, FKP + "SM_Prop_Fence_01.prefab", new UnityEngine.Vector3(-11.3f + i * 2.4f, 0, -7.2f), 0, 1f);
    Keep(root, "InfoPos", new UnityEngine.Vector3(0, 5f, -12f), 0);
    FitCollider(root, house);
    sb.Append("tier1 built, children=").Append(hall.childCount).Append(" || ");
}
