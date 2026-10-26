// Run with: unity command eval_file --file tools/sailing/make-boat-prefabs.cs
//
// Makes the own boats' prefabs in Assets/World/Sailing/Boats (docs/own-boats.md, phase 2) from the Synty kits, as
// the decor prefabs are made: Rowboat (PolygonPirates' rowing boat), FishingBoat (its small boat with a fish pile and
// two rods) and Sloop (its medium boat). Each has its pivot at the water line, the bow along +Z, a "Seat" where
// the owner sits or stands, high enough that the sea never shows inside the hull, (found by casting down onto the hull) and no colliders. Rerun to rebuild them.
var folder = "Assets/World/Sailing/Boats";
System.IO.Directory.CreateDirectory(folder);
var boats = new[]
{
    // SeatHeight: where the owner's feet go, above the water line; NaN finds the deck by casting down onto the hull
    new { Name = "Rowboat", Kit = "Assets/PolygonPirates/Prefabs/Vehicles/SM_Veh_Boat_Rowing_01_Hull_Attachments.prefab", Draft = 0.4f, Seat = -0.55f, SeatHeight = 0.28f },
    new { Name = "FishingBoat", Kit = "Assets/PolygonPirates/Prefabs/Vehicles/SM_Veh_Boat_Small_01_Hull_Attachments.prefab", Draft = 0.42f, Seat = -2.2f, SeatHeight = float.NaN },
    new { Name = "Sloop", Kit = "Assets/PolygonPirates/Prefabs/Vehicles/SM_Veh_Boat_Medium_01_Hull_Attachments.prefab", Draft = 1.1f, Seat = -5.5f, SeatHeight = float.NaN },
};
var log = new System.Text.StringBuilder();
var at = new UnityEngine.Vector3(0, -3000, 0);
foreach (var b in boats)
{
    var kit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(b.Kit);
    if (!kit) { log.Append(b.Name + ": kit missing; "); continue; }
    var root = new UnityEngine.GameObject(b.Name);
    root.transform.position = at;
    var model = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(kit, root.transform);
    model.name = "Model";
    model.transform.localPosition = new UnityEngine.Vector3(0, -b.Draft, 0);
    model.transform.localRotation = UnityEngine.Quaternion.identity;

    // the deck under the seat, from the hull's own colliders, before they go
    UnityEngine.Physics.SyncTransforms();
    var deck = 0.3f;
    var from = root.transform.position + new UnityEngine.Vector3(0, 30f, b.Seat);
    var best = float.MinValue;
    foreach (var c in model.GetComponentsInChildren<UnityEngine.Collider>())
    {
        if (c.Raycast(new UnityEngine.Ray(from, UnityEngine.Vector3.down), out var hit, 60f) && hit.point.y > best && hit.point.y < root.transform.position.y + 4f) best = hit.point.y;
    }
    if (best > float.MinValue) deck = best - root.transform.position.y;
    if (!float.IsNaN(b.SeatHeight)) deck = b.SeatHeight;

    if (b.Name == "FishingBoat")
    {
        void Prop(string path, UnityEngine.Vector3 local, UnityEngine.Vector3 euler, float scale)
        {
            var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            if (!pf) return;
            var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, root.transform);
            o.transform.localPosition = local;
            o.transform.localRotation = UnityEngine.Quaternion.Euler(euler);
            o.transform.localScale *= scale;
        }
        Prop("Assets/PolygonVikings/Prefabs/Props/SM_Prop_Fish_Pile_01.prefab", new UnityEngine.Vector3(0, deck, 1.6f), UnityEngine.Vector3.zero, 1.2f);
        Prop("Assets/PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01.prefab", new UnityEngine.Vector3(0.6f, deck, -1.2f), new UnityEngine.Vector3(-35, 20, 0), 1.6f);
        Prop("Assets/PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01.prefab", new UnityEngine.Vector3(-0.6f, deck, -1.2f), new UnityEngine.Vector3(-35, -20, 0), 1.6f);
    }

    foreach (var c in root.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    var seat = new UnityEngine.GameObject("Seat").transform;
    seat.SetParent(root.transform, false);
    seat.localPosition = new UnityEngine.Vector3(0, deck, b.Seat);
    root.transform.position = UnityEngine.Vector3.zero;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + b.Name + ".prefab");
    UnityEngine.Object.DestroyImmediate(root);
    log.Append(b.Name + " deck " + deck.ToString("0.00") + "; ");
}
UnityEditor.AssetDatabase.SaveAssets();
return log.ToString();
