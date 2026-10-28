// Run with: unity command eval_file --file tools/sailing/make-boat-prefabs.cs
//
// Makes the own boats' prefabs in Assets/World/Sailing/Boats (docs/own-boats.md, phase 2) from the Synty kits, as
// the decor prefabs are made: Rowboat (PolygonPirates' rowing boat), FishingBoat (its small boat with one pair of oars, a fish pile, a net over the side,
// two rods and the medium boat's rig scaled down as its sail) and Sloop (its medium boat). Each has its pivot at the water line, the bow along +Z, a "Seat" where
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
// the fishing boat's sail (see below): its mast's place along the boat and the rig's size against the medium boat's
const float MastZ = 0.9f, RigScale = 0.42f, FishPileZ = -0.2f;
// the net over the starboard side ahead of the oars: its top edge (the prefab's pivot) on the gunwale, turned with the
// hull's curve towards the bow, clear of the front oars, tipped in at the foot as the planks curve in but clear of them
const float NetX = 1.5f, NetY = 0.84f, NetZ = 2.3f, NetYaw = -5f, NetRoll = -20f, NetScale = 0.65f;
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
        // one pair of oars for its one rower: the kit's front pair (Oar_01, beside the mast) goes, the pair nearer the
        // owner's place at the stern stays
        foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true))
            if (t.name == "SM_Prop_Oar_01") t.gameObject.SetActive(false);
        Prop("Assets/PolygonVikings/Prefabs/Props/SM_Prop_Fish_Pile_01.prefab", new UnityEngine.Vector3(0, deck, FishPileZ), UnityEngine.Vector3.zero, 1.2f);
        // the rods lean out over each side, as when trolling, not crossed at the stern
        Prop("Assets/PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01.prefab", new UnityEngine.Vector3(0.55f, deck, -1.2f), new UnityEngine.Vector3(-15, 0, -40), 1.6f);
        Prop("Assets/PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01.prefab", new UnityEngine.Vector3(-0.55f, deck, -1.2f), new UnityEngine.Vector3(-15, 0, 40), 1.6f);
        // a net hung over the side to dry, its top along the gunwale
        Prop("Assets/PolygonVikings/Prefabs/Props/SM_Prop_Net_01.prefab", new UnityEngine.Vector3(NetX, NetY, NetZ), new UnityEngine.Vector3(0, NetYaw, NetRoll), NetScale);

        // its sail: the medium boat's mast and sails, scaled down around the mast's foot (the kit has no small rig)
        var medium = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonPirates/Prefabs/Vehicles/SM_Veh_Boat_Medium_01_Hull_Attachments.prefab");
        UnityEngine.Transform FindPart(UnityEngine.Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (UnityEngine.Transform c in t) { var f = FindPart(c, n); if (f) return f; }
            return null;
        }
        var srcMast = medium ? FindPart(medium.transform, "SM_Veh_Boat_Medium_01_Mast") : null;
        if (srcMast)
        {
            // the deck under the mast, as under the seat
            var mastDeck = deck;
            var mastFrom = root.transform.position + new UnityEngine.Vector3(0, 30f, MastZ);
            var mastBest = float.MinValue;
            foreach (var c in model.GetComponentsInChildren<UnityEngine.Collider>())
            {
                if (c.Raycast(new UnityEngine.Ray(mastFrom, UnityEngine.Vector3.down), out var hit, 60f) && hit.point.y > mastBest && hit.point.y < root.transform.position.y + 4f) mastBest = hit.point.y;
            }
            if (mastBest > float.MinValue) mastDeck = mastBest - root.transform.position.y;
            var foot = new UnityEngine.Vector3(0, mastDeck, MastZ);
            // the sails are the mast's children and come with it
            var rig = UnityEngine.Object.Instantiate(srcMast.gameObject, root.transform);
            rig.name = "Rig";
            rig.transform.localPosition = foot;
            rig.transform.localRotation = srcMast.localRotation;
            rig.transform.localScale = srcMast.localScale * RigScale;
            foreach (var r in rig.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (r.name.Contains("Sails_Up")) r.gameObject.SetActive(false);
        }
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
