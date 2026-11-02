using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puts a boat's look on it (docs/own-boats.md, "Customizing boats"). The Sloop, the Brigantine and the Galleon are
/// built from Synty's Pirate Realm small, medium and large ships under their "Model": the hull, front, middle, back, figurehead and ram of the look's variants, the mast,
/// bowsprit, rigging, helm and grate every one has, then its sails and planks by material and its flag at the
/// masthead. The Rowboat and the Fishing Boat keep their models: the fishing boat's sail (the small ship's, see
/// tools/sailing/make-boat-prefabs.cs) takes the look's sails and its flag flies from its masthead, the rowboat's from
/// a pole at the stern. Every flag flies beside its mast or pole, as a real one does. Run at launch and again when
/// the look changes while the boat is out; its seats are never touched.
/// </summary>
public static class BoatAssembler
{
    private const string ModelName = "Model";
    private const string FlagName = "Look Flag";
    private const float ShipFlagScale = 0.75f;
    private const float SmallFlagScale = 0.32f;
    /// <summary>The fishing boat's flag at its masthead, between the two.</summary>
    private const float SailFlagScale = 0.42f;
    /// <summary>Half the flag cloth's width (SM_Prop_Flag_01, 3.78 m, hanging from the middle of its top edge).</summary>
    private const float FlagHalfWidth = 1.89f;

    public static void Apply(GameObject boat, BoatItems.Tier tier, string tag)
    {
        var parts = OwnBoatPrefabs.Parts;
        if (!boat || !parts) return;
        var look = BoatLook.Parse(tag, tier);

        var ship = parts.For(tier);
        if (ship != null && ship.Hulls != null && ship.Hulls.Length > 0)
        {
            var model = boat.transform.Find(ModelName);
            if (!model)
            {
                model = new GameObject(ModelName).transform;
                model.SetParent(boat.transform, false);
            }
            BuildShip(model, ship, look);
            // the sails taken in and set (in port, at anchor), the furled ones painted with the rest
            BoatSails.Collect(boat, model);
            Paint(model, parts, look);
            Flag(boat.transform, model, parts, look["flag"], true);
        }
        else
        {
            // the fishing boat's sail is the small ship's: it wears the look's sails and furls in port
            BoatSails.Collect(boat, boat.transform);
            Paint(boat.transform, parts, look);
            Flag(boat.transform, boat.transform, parts, look["flag"], false);
        }
    }

    private static GameObject Pick(GameObject[] variants, int variant) =>
        variants != null && variant >= 1 && variant <= variants.Length ? variants[variant - 1] : null;

    private static Transform Put(GameObject prefab, Transform parent, Vector3 at = default)
    {
        if (!prefab) return null;
        var o = Object.Instantiate(prefab, parent, false);
        o.transform.localPosition = at;
        o.transform.localRotation = Quaternion.identity;
        foreach (var c in o.GetComponentsInChildren<Collider>()) Kill(c);
        return o.transform;
    }

    private static void BuildShip(Transform model, BoatParts.Ship parts, Dictionary<string, int> look)
    {
        for (var i = model.childCount - 1; i >= 0; --i) Kill(model.GetChild(i).gameObject);

        var hull = Put(Pick(parts.Hulls, look["hull"]) ?? Pick(parts.Hulls, 1), model);
        if (!hull) return;
        // the hull's own mast and bowsprit locators stay empty: the rig brings both, where the preset had them
        foreach (var t in hull.GetComponentsInChildren<Transform>())
            if (t != hull && (t.name.Contains("Mast_Locator") || t.name.Contains("Bowsprit_Locator"))) Kill(t.gameObject);
        Put(parts.Rig, hull);
        Put(Pick(parts.Fronts, look["front"]) ?? Pick(parts.Fronts, 1), hull);
        Put(Pick(parts.Middles, look["middle"]) ?? Pick(parts.Middles, 1), hull);
        Put(Pick(parts.Backs, look["back"]) ?? Pick(parts.Backs, 1), hull);
        Put(Pick(parts.Figureheads, look["figurehead"]), hull);
        Put(Pick(parts.Rams, look["ram"]), hull);
    }

    /// <summary>The look's sails and planks, in place of the kit's own on every renderer; the furled sails' cloth still.</summary>
    private static void Paint(Transform model, BoatParts parts, Dictionary<string, int> look)
    {
        var sails = Pick(parts.Sails, look["sails"]);
        var wood = Pick(parts.Woods, look["wood"]);
        var furled = BoatSails.Still(sails);
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            var changed = false;
            var tied = r.name.Contains("_Tied");
            for (var i = 0; i < mats.Length; ++i)
            {
                var name = mats[i] ? mats[i].name : "";
                if (sails && name.StartsWith("ShipSails_", System.StringComparison.Ordinal)) { mats[i] = tied ? furled : sails; changed = true; }
                else if (wood && name.StartsWith("Ship_Planks_", System.StringComparison.Ordinal)) { mats[i] = wood; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    private static Material Pick(Material[] variants, int variant) =>
        variants != null && variant >= 1 && variant <= variants.Length ? variants[variant - 1] : null;

    /// <summary>
    /// The look's flag: at the masthead of the tallest mast (the ships), from the peak of the fishing boat's gaff, or on a
    /// pole at the stern (the rowboat). It hangs from its top edge with its leading edge at the mast or pole, streaming aft, as a
    /// real flag flies. None for flag 0.
    /// </summary>
    private static void Flag(Transform boat, Transform model, BoatParts parts, int flag, bool ship)
    {
        var old = boat.Find(FlagName);
        if (old) Kill(old.gameObject);
        var material = Pick(parts.Flags, flag);
        if (!material || !parts.Flag) return;

        var holder = new GameObject(FlagName).transform;
        holder.SetParent(boat, false);
        Bounds? top = null, all = null, hullBounds = null;
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(holder)) continue;
            all = all.HasValue ? Grow(all.Value, r.bounds) : r.bounds;
            if (r.name.Contains("Hull") || r.name == ModelName) hullBounds = hullBounds.HasValue ? Grow(hullBounds.Value, r.bounds) : r.bounds;
            // the tallest mast (the Brigantine has a front and a back one, the Galleon three, the fishing boat one)
            if (r.name.Contains("_Mast_") && !r.name.Contains("Boom") && !r.name.Contains("Sail") && !r.name.Contains("Locator") && (!top.HasValue || r.bounds.max.y > top.Value.max.y)) top = r.bounds;
        }
        if (!all.HasValue) { Kill(holder.gameObject); return; }

        Vector3 at;
        float scale;
        // the fishing boat's gaff peak, marked on its prefab (tools/sailing/make-boat-prefabs.cs): a gaff-rigged boat flies
        // its flag there, clear of the sail
        var point = ship ? null : boat.Find("FlagPoint");
        if (point)
        {
            scale = SailFlagScale;
            at = boat.InverseTransformPoint(point.position) + Vector3.back * (FlagHalfWidth * scale);
        }
        else if (top.HasValue)
        {
            scale = ship ? ShipFlagScale : SailFlagScale;
            var head = boat.InverseTransformPoint(new Vector3(top.Value.center.x, top.Value.max.y, top.Value.center.z));
            var mastRadius = Mathf.Min(top.Value.extents.x, top.Value.extents.z) / Mathf.Max(0.01f, boat.lossyScale.z);
            at = head + Vector3.back * (FlagHalfWidth * scale + mastRadius);
        }
        else
        {
            // the stern: the back of the hull, at its gunwale; the kit's banded post stands upright from its foot
            var b = hullBounds ?? all.Value;
            var stern = boat.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.min.z));
            const float PoleScale = 0.7f, PoleHeight = 2.36f * PoleScale, PoleRadius = 0.06f;
            var foot = stern.y - 0.3f;
            var poleZ = stern.z + 0.35f;
            var pole = parts.FlagPole ? Object.Instantiate(parts.FlagPole, holder, false) : null;
            if (pole)
            {
                pole.transform.localRotation = Quaternion.identity;
                pole.transform.localScale = Vector3.one * PoleScale;
                pole.transform.localPosition = new Vector3(0f, foot, poleZ);
                foreach (var c in pole.GetComponentsInChildren<Collider>()) Kill(c);
            }
            scale = SmallFlagScale;
            at = new Vector3(0f, foot + PoleHeight - 0.05f, poleZ - FlagHalfWidth * scale - PoleRadius);
        }

        var cloth = Object.Instantiate(parts.Flag, holder, false);
        cloth.transform.localPosition = at;
        // the banner hangs from its top edge, its face to either side of the boat, streaming aft
        cloth.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        cloth.transform.localScale = Vector3.one * scale;
        foreach (var c in cloth.GetComponentsInChildren<Collider>()) Kill(c);
        foreach (var r in cloth.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
    }

    private static Bounds Grow(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    /// <summary>Gone now, out of the hierarchy at once (a Destroy waits for the frame's end); in the editor too (the tool's preview).</summary>
    private static void Kill(Object o)
    {
        if (!o) return;
        if (o is GameObject go) go.transform.SetParent(null, false);
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }
}
