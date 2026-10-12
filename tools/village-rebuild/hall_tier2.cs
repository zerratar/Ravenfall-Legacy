// Tier 2: the town hall. Two storeys under a blue slate roof, a bell tower beside it, the well and market stalls out front.
{
    var root = tierRoot(2);
    var hall = freshHall(2);
    Patch(hall, "Yard", new UnityEngine.Vector3(0, 0, 2f), 16f, 13f, 0.14f, dirtMat, 4f);
    var house = Put(hall, FKB + "SM_Bld_Preset_House_07_Optimized.prefab", new UnityEngine.Vector3(0f, 0, 13.5f), 0, 1.4f);
    Put(hall, FKB + "SM_Bld_Preset_Church_01_B_Optimized.prefab", new UnityEngine.Vector3(13.5f, 0, 13f), 0, 1.1f);
    Put(hall, FKP + "SM_Prop_Well_01.prefab", new UnityEngine.Vector3(0f, 0, -4f), 0, 1.6f);
    Put(hall, FKP + "SM_Prop_Well_Plinth_01.prefab", new UnityEngine.Vector3(0f, 0, -4f), 0, 1.6f);
    Put(hall, FKP + "SM_Prop_Market_Stall_01.prefab", new UnityEngine.Vector3(-9f, 0, -2f), 90, 1.15f);
    Put(hall, FKP + "SM_Prop_Market_Stall_02.prefab", new UnityEngine.Vector3(9f, 0, -2f), -90, 1.15f);
    Put(hall, FKP + "SM_Prop_Crate_Stack_01.prefab", new UnityEngine.Vector3(-11.5f, 0, 3f), 20, 1f);
    Put(hall, FKP + "SM_Prop_Barrel_Stack_01.prefab", new UnityEngine.Vector3(11.5f, 0, 3.5f), -10, 1f);
    Put(hall, FKP + "SM_Prop_Street_Lamp_01.prefab", new UnityEngine.Vector3(-4.5f, 0, -8.5f), 90, 1.2f);
    Put(hall, FKP + "SM_Prop_Street_Lamp_01.prefab", new UnityEngine.Vector3(4.5f, 0, -8.5f), -90, 1.2f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01.prefab", new UnityEngine.Vector3(-6.5f, 0, -9.5f), 195, 1f);
    Put(hall, FKP + "SM_Prop_Quest_Board_01_Posters_01.prefab", new UnityEngine.Vector3(-6.5f, 0, -9.5f), 195, 1f);
    for (var i = 0; i < 2; ++i)
    {
        var x = i == 0 ? -8.5f : 8.5f;
        var pole = Put(hall, FKP + "Banners/SM_Prop_Flag_Pole_02.prefab", new UnityEngine.Vector3(x, 0, 6.5f), 0, 1f);
        pole.transform.localScale = new UnityEngine.Vector3(1f, 2f, 1f);
        Put(hall, FKP + "Banners/SM_Prop_Flag_11.prefab", new UnityEngine.Vector3(x, 8.1f, 6.55f), 0, 1f);
    }
    Fire(hall, new UnityEngine.Vector3(-3.2f, 0, 3.5f), false);
    Fire(hall, new UnityEngine.Vector3(3.2f, 0, 3.5f), false);
    Keep(root, "Resources", new UnityEngine.Vector3(-16f, 0, 8f), 90);
    Keep(root, "InfoPos (1)", new UnityEngine.Vector3(0, 6f, -14f), 0);
    FitCollider(root, house);
    var res = root.Find("Resources"); if (res) { var rs = res.GetComponentsInChildren<UnityEngine.Renderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); sb.Append("res bounds size=").Append(b.size.ToString("F1")).Append(" center rel=").Append((b.center - root.position).ToString("F1")).Append(" || "); }
    sb.Append("tier2 built, children=").Append(hall.childCount).Append(" || ");
}
