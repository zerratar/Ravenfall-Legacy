using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puts a boat's look on it (docs/own-boats.md, "Customizing boats"). The Sloop, the Brigantine and the Galleon are
/// built from Synty's Pirate Realm small, medium and large ships under their "Model": the hull, front, middle, back, figurehead and ram of the look's variants, the mast,
/// bowsprit, rigging, helm and grate every one has, then its sails and planks by material and its flag at the
/// masthead. The Rowboat and the Fishing Boat keep their models: the fishing boat's sail (the small ship's, see
/// tools/sailing/make-boat-prefabs.cs) takes the look's sails and its flag flies from its masthead as the Sloop's does, the rowboat's from
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

    /// <summary>
    /// The look's value for the clan's logo (the server's BoatLooks.ClanLogo): on the flag, the plain dark flag with the
    /// logo; on the sails, the plain cream sails with the logo where the emblem sails have theirs.
    /// </summary>
    public const int ClanLogo = 99;
    private const int PlainDarkFlag = 6, PlainSails = 1;
    private static readonly Dictionary<(Material, Texture, Color), Material> clanLooks = new Dictionary<(Material, Texture, Color), Material>();

    // where the logo goes, as fractions of the sheet from its top left: the flag cloth's shape (u 0.40 to 0.73, v 0.71
    // to 0.97); the sails' four emblem places (Pirate Realm's Sail_Symbols textures, 4096 px) and the gaff sails' middle
    private static readonly Rect[] FlagLogoAreas = { Rect.MinMaxRect(0.4014f, 1f - 0.9694f, 0.7325f, 1f - 0.7078f) };
    private static readonly Rect[] SailLogoAreas =
    {
        Rect.MinMaxRect(604 / 4096f, 68 / 4096f, 1432 / 4096f, 960 / 4096f),
        Rect.MinMaxRect(660 / 4096f, 1156 / 4096f, 1376 / 4096f, 1920 / 4096f),
        Rect.MinMaxRect(1020 / 4096f, 2516 / 4096f, 2060 / 4096f, 3636 / 4096f),
        Rect.MinMaxRect(3288 / 4096f, 3184 / 4096f, 3904 / 4096f, 3848 / 4096f),
        // and the fore-and-aft (gaff) sails, which have no emblem of their own (u 0.8 to 1, v 0.5 to 1 on the sheet)
        Rect.MinMaxRect(0.81f, 0.196f, 0.99f, 0.396f),
    };
    /// <summary>
    /// How wide the logo is drawn against its height in each area, so it shows round on the sail: the gaff sails' strip
    /// is stretched across them more than up them.
    /// </summary>
    private static readonly float[] SailLogoAspects = { 1f, 1f, 1f, 1f, 0.6f };
    private static readonly float[] FlagLogoAspects = { 1f };

    /// <summary>Puts the look on the boat; the owner's clan gives the clan flag its logo.</summary>
    public static void Apply(GameObject boat, BoatItems.Tier tier, string tag, PlayerController owner = null)
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
            Paint(model, parts, look, owner);
            Flag(boat.transform, model, parts, look["flag"], true, owner);
        }
        else
        {
            // the fishing boat's sail is the small ship's: it wears the look's sails and furls in port
            BoatSails.Collect(boat, boat.transform);
            Paint(boat.transform, parts, look, owner);
            Flag(boat.transform, boat.transform, parts, look["flag"], false, owner);
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
    private static void Paint(Transform model, BoatParts parts, Dictionary<string, int> look, PlayerController owner)
    {
        var clanSails = look["sails"] == ClanLogo;
        var sails = Pick(parts.Sails, clanSails ? PlainSails : look["sails"]);
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
        if (!clanSails || !sails) return;

        // the clan's sails: in the clan's colour at once, then with the logo once it is there, the furled ones still
        var plain = sails;
        var tint = ClanColor(owner, false);
        var shown = plain;
        void Swap(Material to)
        {
            if (!model || !to || to == shown) return;
            var fromStill = BoatSails.Still(shown);
            var toStill = BoatSails.Still(to);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; ++i)
                {
                    if (mats[i] == shown) { mats[i] = to; changed = true; }
                    else if (mats[i] == fromStill) { mats[i] = toStill; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
            shown = to;
        }
        if (tint.HasValue) Swap(ClanLogoMaterial(plain, null, SailLogoAreas, SailLogoAspects, 1024, tint));
        WithClanLogo(owner, logo => Swap(ClanLogoMaterial(plain, logo, SailLogoAreas, SailLogoAspects, 1024, tint)));
    }

    private static Material Pick(Material[] variants, int variant) =>
        variants != null && variant >= 1 && variant <= variants.Length ? variants[variant - 1] : null;

    /// <summary>
    /// The look's flag: at the masthead of the tallest mast (the ships and the fishing boat's sail), or on a pole at the
    /// stern (the rowboat). It hangs from its top edge with its leading edge at the mast or pole, streaming aft, as a
    /// real flag flies. None for flag 0.
    /// </summary>
    private static void Flag(Transform boat, Transform model, BoatParts parts, int flag, bool ship, PlayerController owner)
    {
        var old = boat.Find(FlagName);
        if (old) Kill(old.gameObject);
        var clan = flag == ClanLogo;
        var material = Pick(parts.Flags, clan ? PlainDarkFlag : flag);
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
        if (top.HasValue)
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
        if (clan)
        {
            var plain = material;
            var tint = ClanColor(owner, true);
            void Wear(Material to)
            {
                // a late download: not for a flag taken down since (another look, the boat put away)
                if (cloth && to) foreach (var r in cloth.GetComponentsInChildren<Renderer>()) r.sharedMaterial = to;
            }
            if (tint.HasValue) Wear(ClanLogoMaterial(plain, null, FlagLogoAreas, FlagLogoAspects, 512, tint));
            WithClanLogo(owner, logo => Wear(ClanLogoMaterial(plain, logo, FlagLogoAreas, FlagLogoAspects, 512, tint)));
        }
    }

    /// <summary>The cloth colour the owner's clan picked for its flag (or its sails), none for the plain one.</summary>
    private static Color? ClanColor(PlayerController owner, bool flag)
    {
        if (!owner || !owner.clanHandler || !owner.clanHandler.InClan || owner.clanHandler.ClanInfo == null || !owner.GameManager) return null;
        var look = owner.GameManager.ClanLooks?.Get(owner.clanHandler.ClanInfo.Id);
        var hex = flag ? look?.FlagColor : look?.SailColor;
        return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color) ? color : (Color?)null;
    }

    /// <summary>
    /// The owner's clan logo, as the capes get it (the owner's upload or their Twitch picture), once it has downloaded;
    /// nothing while the owner is in no clan, or the logos are hidden.
    /// </summary>
    private static void WithClanLogo(PlayerController owner, System.Action<Texture> then)
    {
        if (!owner || !owner.clanHandler || !owner.clanHandler.InClan || owner.clanHandler.ClanInfo == null) return;
        var game = owner.GameManager;
        if (!game || game.LogoCensor || game.PlayerLogo == null) return;
        var clan = owner.clanHandler.ClanInfo;
        var url = owner.clanHandler.Logo;
        // the logo address stays the same when the owner uploads a new one: its id makes it a new address
        var look = game.ClanLooks?.Get(clan.Id);
        if (look?.LogoImageId != null && !string.IsNullOrEmpty(url))
            url += (url.Contains("?") ? "&v=" : "?v=") + look.LogoImageId.Value.ToString("N");
        game.PlayerLogo.GetLogo(clan.OwnerUserId, url, logo =>
        {
            if (logo && logo.texture) then(logo.texture);
        });
    }

    /// <summary>
    /// A plain flag or plain sails in the clan's colour, with the logo as their symbol: Synty's fabric shader tints the
    /// cloth and lays a symbol texture over it (the emblem flags' and sails' pictures), so the logo goes on a clear
    /// texture in the areas the emblems use, in its own colours, over the cloth's shading. No logo: the colour alone.
    /// Kept per material, logo and colour.
    /// </summary>
    private static Material ClanLogoMaterial(Material plain, Texture logo, Rect[] areas, float[] aspects, int size, Color? tint = null)
    {
        var key = (plain, logo, tint ?? Color.clear);
        if (clanLooks.TryGetValue(key, out var made) && made) return made;
        if (!plain || !plain.HasProperty("_Symbol_Texture")) return null;
        if (clanLooks.Count > 32) clanLooks.Clear();
        if (!logo)
        {
            var colored = new Material(plain) { name = plain.name.StartsWith("ShipSails_") ? "ShipSails_Clan" : "Clan flag" };
            Tint(colored, tint);
            clanLooks[key] = colored;
            return colored;
        }

        var symbol = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = plain.name + " clan symbol",
            useMipMap = true,
            autoGenerateMips = true,
            wrapMode = TextureWrapMode.Clamp,
        };
        symbol.Create();
        var previous = RenderTexture.active;
        RenderTexture.active = symbol;
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, size, size, 0);
        for (var i = 0; i < areas.Length; ++i)
        {
            // as big as most of the area, in its middle, narrowed where the sheet is stretched across the sail
            var area = areas[i];
            var aspect = i < aspects.Length ? aspects[i] : 1f;
            var height = Mathf.Min(area.width / aspect, area.height) * size * 0.85f;
            var width = height * aspect;
            var centre = area.center * size;
            Graphics.DrawTexture(new Rect(centre.x - width * 0.5f, centre.y - height * 0.5f, width, height), logo);
        }
        GL.PopMatrix();
        RenderTexture.active = previous;

        // the clan sails keep a "ShipSails_" name, so a later look finds them as it finds any sails
        var material = new Material(plain) { name = plain.name.StartsWith("ShipSails_") ? "ShipSails_Clan" : "Clan flag" };
        // the plain ones have the overlay off; on, it is the emblem ones' variant, so a build has it
        material.EnableKeyword("_USE_SYMBOL_OVERLAY");
        material.SetFloat("_USE_SYMBOL_OVERLAY", 1f);
        material.SetTexture("_Symbol_Texture", symbol);
        material.SetColor("_Symbol_Color_Tint", Color.white);
        Tint(material, tint);
        clanLooks[key] = material;
        return material;
    }

    private static void Tint(Material material, Color? tint)
    {
        if (!tint.HasValue) return;
        if (material.HasProperty("_Color_Tint")) material.SetColor("_Color_Tint", tint.Value);
        if (material.HasProperty("_SSSTint")) material.SetColor("_SSSTint", tint.Value);
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
