// Run with: unity command eval_file --file tools/items/class-gear-preview.cs (edit mode; never in play mode)
//
// Dresses copies of the Player prefab in class gear through the game's REAL equip code and renders them, without
// the server having the items: for looking at the gear, and for testing what the item flags do.
//
// The items are made in memory from tools/items/class-gear-parts.txt (a RavenNest.Models.Item per line, tier and
// piece, plus the plate pieces of the metal tiers), given to an ItemManager of their own, and run through the
// methods the game calls: ItemManager.LoadItemData (the model id parsing), ItemController.Create (the look of a
// transmogrified item), SyntyPlayerAppearance.SetAppearance / Equip / Unequip / UpdateAppearance /
// ToggleHelmVisibility and ItemManager.GetMaterial. Nothing is switched on or off by hand, so the picture and the
// part list in the log are what the game would show. Only the mesh combiner is left out (it needs play mode).
//
// Everything lives in an isolated preview scene (PreviewRenderUtility) and is destroyed afterwards; the open scene
// is not touched. The work is spread over editor frames (one render per frame), so the eval returns at once: wait
// for <job file>.done, which holds the log.
//
// The job file is Temp/class-gear-preview.job (key=value lines; several renders are separated by a line "---"):
//   out=C:\somewhere\picture.png      where the picture goes (the folder is made)
//   w=380 / h=700 / ss=2              the size of one character's cell, and the supersampling (the cell is rendered
//                                     ss times as large and averaged down)
//   viewH=2.3 / cy=1.0                what a cell shows: this many meters high, centered at this height
//   yaw=28 / pitch=6                  the character turned from the camera, the camera looking down
//   bg=#22252B                        the background, or "clear" for a transparent one
//   pose=idle                         a frame of the idle animation, or "tpose" for the prefab's own pose
//   clip=<asset path> / clipAt=0.1      the clip the pose is a frame of (the relaxed idle by default; any of the player's clips,
//                                     e.g. Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Chop-Finish.FBX)
//   gender=male hair=2 head=0 brows=0 beard=0 skinColor=#C99A7A hairColor=#4A3524 eyeColor=#2B2622
//   cape=-1 / capeHidden=0            the player's cape (0 to 4, as from the server) and whether the server says it
//                                     is hidden (SyntyAppearance.CapeHidden)
//   v0=label | key=value | do=action, action, ...     one character per v line, side by side (at most 10);
//                                     its keys override the ones above
// Actions (an item is "<Line> <tier> [piece name]" or "plate <Material> [Helmet|Chest|Gloves|Leggings|Boots]";
// without a piece it is the whole set, with the line's main head piece; "X as Y" is X transmogrified into Y;
// "custom <ItemType> <Material> <model ids, separated by +> [HeadMask flags, separated by +]" is a piece that is in no
// table, for trying parts out):
//   wear <item>       as the game equips: the piece replaces what is worn in its slot, then everything is put on
//                     again from nothing (Inventory.Equip -> PlayerEquipment.EquipAll)
//   puton <item>      only SyntyPlayerAppearance.Equip on top of what is shown (no rebuild): the part switching
//   takeoff <slot>    Head, Chest, Gloves, Leggings or Boots (PlayerEquipment.Unequip with a rebuild)
//   toggle            the helmet toggle (!toggle helmet)
//   capetoggle        the cape toggle (!toggle cape: SyntyPlayerAppearance.ToggleCapeVisibility)
//   capeimage         a picture on the cape, as a clan's cape image is put on (SyntyPlayerAppearance.SetCapeTexture);
//                     the log then says whether it is still on the cape's material ("capeImage=kept")
//   rebuild           everything off and on again (PlayerEquipment.UpdateAppearance)
//   skincolor <hex>   another skin color arrives from the server (an appearance update, as after a change on the
//                     website: SetAppearance, then everything is put on again)
// The log's "state:" says what the game would ask the appearance: CapeVisible, HasCape, BackPieceShows; and for armor
// that shows skin: skinMats (how many materials of the player's own are on, one a tier material at most) and
// skinMatch (whether each has the player's skin color; "none" when no part on shows skin).
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var jobPath = System.IO.Path.Combine(project, "Temp", "class-gear-preview.job");
var donePath = jobPath + ".done";
var partsPath = System.IO.Path.Combine(project, "tools", "items", "class-gear-parts.txt");
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "not in play mode";
if (!System.IO.File.Exists(jobPath)) return "no job file: " + jobPath;
if (System.IO.File.Exists(donePath)) System.IO.File.Delete(donePath);
var inv = System.Globalization.CultureInfo.InvariantCulture;

// ---- the jobs -------------------------------------------------------------------------------------------------------
var jobs = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
{
    var cur = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var raw in System.IO.File.ReadAllLines(jobPath))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;
        if (line == "---") { if (cur.Count > 0) jobs.Add(cur); cur = new System.Collections.Generic.Dictionary<string, string>(); continue; }
        var eq = line.IndexOf('=');
        if (eq > 0) cur[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
    }
    if (cur.Count > 0) jobs.Add(cur);
}
if (jobs.Count == 0) return "no jobs in " + jobPath;

// ---- the pieces and the items made from them ------------------------------------------------------------------------
// in the order of their materials in ItemMaterial (eight a line, from Apprentice on)
var lines = new[] { "Mage", "Healer", "Ranger", "Melee", "Tank", "Rogue", "Barbarian", "Samurai", "Paladin", "DarkKnight", "WitchHunter", "Monk", "Beastmaster" };
var pieces = new System.Collections.Generic.List<string[]>(); // line, type, name, male ids, female ids, mask
foreach (var raw in System.IO.File.ReadAllLines(partsPath))
{
    var line = raw.Trim();
    if (line.Length == 0 || line[0] == '#') continue;
    var cells = line.Split('|');
    if (cells.Length != 6 && cells.Length != 7) return "bad row in class-gear-parts.txt: " + line;
    for (var i = 0; i < cells.Length; i++) cells[i] = cells[i].Trim();
    pieces.Add(cells);
}
// a row is for the tiers its last column names ("3-5", "8"), or for all of them without one
bool RowIsFor(string[] row, int tier)
{
    if (row.Length < 7 || row[6].Length == 0) return true;
    var range = row[6].Split('-');
    var from = int.Parse(range[0]);
    var to = range.Length > 1 ? int.Parse(range[1]) : from;
    return tier >= from && tier <= to;
}
var allItems = new System.Collections.Generic.List<RavenNest.Models.Item>();
var lookup = new System.Collections.Generic.Dictionary<System.Guid, RavenNest.Models.Item>();
ItemManager itemManager = null;
var itemByKey = new System.Collections.Generic.Dictionary<string, RavenNest.Models.Item>(System.StringComparer.OrdinalIgnoreCase);
RavenNest.Models.Item Make(string key, string name, RavenNest.Models.ItemType type, RavenNest.Models.ItemMaterial material, string male, string female, RavenNest.Models.HeadMask mask)
{
    var item = new RavenNest.Models.Item
    {
        Id = System.Guid.NewGuid(), Name = name, Category = RavenNest.Models.ItemCategory.Armor, Type = type,
        Material = material, MaleModelId = male, FemaleModelId = female, HeadMask = mask,
    };
    allItems.Add(item);
    itemByKey[key] = item;
    return item;
}
for (var li = 0; li < lines.Length; li++)
{
    for (var tier = 1; tier <= 8; tier++)
    {
        var material = (RavenNest.Models.ItemMaterial)((int)RavenNest.Models.ItemMaterial.Apprentice + li * 8 + tier - 1);
        foreach (var p in pieces)
        {
            if (!string.Equals(p[0], lines[li], System.StringComparison.OrdinalIgnoreCase) || !RowIsFor(p, tier)) continue;
            if (itemByKey.ContainsKey(lines[li] + " " + tier + " " + p[2])) return "two rows in class-gear-parts.txt for " + lines[li] + " tier " + tier + " " + p[2];
            var type = (RavenNest.Models.ItemType)System.Enum.Parse(typeof(RavenNest.Models.ItemType), p[1]);
            var mask = RavenNest.Models.HeadMask.None;
            foreach (var flag in p[5].Split(',')) mask |= (RavenNest.Models.HeadMask)System.Enum.Parse(typeof(RavenNest.Models.HeadMask), flag.Trim());
            Make(lines[li] + " " + tier + " " + p[2], material + " " + p[2], type, material, p[3], p[4], mask);
        }
    }
}
// the plate of the metal tiers, as the item rows have it (Rune Helmet "5", Chest "15", ...)
var plate = new[] { "Helmet|5|5", "Chest|15|15", "Gloves|14|14", "Leggings|25|25", "Boots|16|8" };
for (var v = 1; v < (int)RavenNest.Models.ItemMaterial.Apprentice; v++)
{
    var material = (RavenNest.Models.ItemMaterial)v;
    foreach (var row in plate)
    {
        var c = row.Split('|');
        Make("plate " + material + " " + c[0], material + " " + c[0], (RavenNest.Models.ItemType)System.Enum.Parse(typeof(RavenNest.Models.ItemType), c[0]), material, c[1], c[2], RavenNest.Models.HeadMask.None);
    }
}
// "<Line> <tier> [piece]" or "plate <Material> [piece]": the items it names (a whole set without a piece)
System.Collections.Generic.List<RavenNest.Models.Item> Items(string spec)
{
    var found = new System.Collections.Generic.List<RavenNest.Models.Item>();
    var t = spec.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    if (t.Length < 2) throw new System.Exception("not an item: " + spec);
    if (string.Equals(t[0], "custom", System.StringComparison.OrdinalIgnoreCase))
    {
        if (t.Length < 4) throw new System.Exception("custom <ItemType> <Material> <ids> [mask]: " + spec);
        var key = string.Join(" ", t);
        if (!itemByKey.TryGetValue(key, out var custom))
        {
            var mask = RavenNest.Models.HeadMask.None;
            if (t.Length > 4) foreach (var flag in t[4].Split('+')) mask |= (RavenNest.Models.HeadMask)System.Enum.Parse(typeof(RavenNest.Models.HeadMask), flag);
            var ids = t[3].Replace('+', ',');
            custom = Make(key, t[2] + " " + t[1] + " " + ids, (RavenNest.Models.ItemType)System.Enum.Parse(typeof(RavenNest.Models.ItemType), t[1]), (RavenNest.Models.ItemMaterial)System.Enum.Parse(typeof(RavenNest.Models.ItemMaterial), t[2]), ids, ids, mask);
            lookup[custom.Id] = custom;
            itemManager.LoadItemData(); // the game's own parsing of the new row
        }
        found.Add(custom);
        return found;
    }
    var rest = string.Join(" ", t, 2, t.Length - 2);
    if (rest.Length > 0)
    {
        if (!itemByKey.TryGetValue(t[0] + " " + t[1] + " " + rest, out var one)) throw new System.Exception("no such piece: " + spec);
        found.Add(one);
        return found;
    }
    if (string.Equals(t[0], "plate", System.StringComparison.OrdinalIgnoreCase))
    {
        foreach (var row in plate) found.Add(itemByKey["plate " + t[1] + " " + row.Split('|')[0]]);
        return found;
    }
    var head = false;
    foreach (var p in pieces)
    {
        if (!string.Equals(p[0], t[0], System.StringComparison.OrdinalIgnoreCase) || !RowIsFor(p, int.Parse(t[1]))) continue;
        var isHead = p[1] == "Helmet" || p[1] == "Hat" || p[1] == "Mask" || p[1] == "HeadCovering";
        if (isHead && head) continue; // the main head piece only
        head |= isHead;
        found.Add(itemByKey[t[0] + " " + t[1] + " " + p[2]]);
    }
    if (found.Count == 0) throw new System.Exception("no such set: " + spec);
    return found;
}
string SlotOf(RavenNest.Models.ItemType type)
{
    switch (type)
    {
        case RavenNest.Models.ItemType.Helmet: case RavenNest.Models.ItemType.Hat: case RavenNest.Models.ItemType.Mask: case RavenNest.Models.ItemType.HeadCovering: return "Head";
        default: return type.ToString();
    }
}

// ---- the run: one render per editor frame ---------------------------------------------------------------------------
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var log = new System.Text.StringBuilder();
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab");
if (!prefab) return "no Assets/Prefabs/Player.prefab";
var temp = new System.Collections.Generic.List<UnityEngine.Object>();
// the characters are used again for the next render (the game dresses a player again and again too): Unity logs an
// error for every material copy Renderer.material makes in edit mode, about 14 for a character's first dressing
var pool = new System.Collections.Generic.List<UnityEngine.GameObject>();
var pru = new UnityEditor.PreviewRenderUtility();
var next = 0;
var sw = System.Diagnostics.Stopwatch.StartNew();
UnityEngine.Color Hex(string h) { UnityEngine.ColorUtility.TryParseHtmlString(h, out var c); return c; }

void Setup()
{
    // an item manager of its own: the metal tiers' materials as on the scene's (so the class gear's come from
    // Resources through the real GetMaterial), and the items above
    var go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("Class Gear Preview Items", UnityEngine.HideFlags.HideAndDontSave);
    temp.Add(go);
    pru.AddSingleGO(go);
    itemManager = go.AddComponent<ItemManager>();
    var names = System.Enum.GetNames(typeof(RavenNest.Models.ItemMaterial));
    var metal = new UnityEngine.Material[(int)RavenNest.Models.ItemMaterial.Apprentice - 1];
    for (var i = 0; i < metal.Length; i++)
    {
        var name = names[i + 1];
        if (name == "Abraxas") name = "Ultima";
        if (name == "ElderAdamantite") name = "ElderAdamantitite";
        metal[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/" + name + ".mat");
    }
    typeof(ItemManager).GetField("itemMaterials", bf).SetValue(itemManager, metal);
    typeof(ItemManager).GetField("items", bf).SetValue(itemManager, allItems);
    foreach (var item in allItems) lookup[item.Id] = item;
    typeof(ItemManager).GetField("itemLookup", bf).SetValue(itemManager, lookup);
    itemManager.LoadItemData();
}

void Render(System.Collections.Generic.Dictionary<string, string> baseJob)
{
    var variants = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
    var vkeys = new System.Collections.Generic.List<string>();
    foreach (var kv in baseJob) if (kv.Key.Length > 1 && kv.Key[0] == 'v' && char.IsDigit(kv.Key[1])) vkeys.Add(kv.Key);
    vkeys.Sort((x, y) => int.Parse(x.Substring(1)).CompareTo(int.Parse(y.Substring(1))));
    foreach (var vk in vkeys)
    {
        var d = new System.Collections.Generic.Dictionary<string, string>(baseJob);
        var parts = baseJob[vk].Split('|');
        d["label"] = parts[0].Trim();
        for (var i = 1; i < parts.Length; i++) { var eq = parts[i].IndexOf('='); if (eq > 0) d[parts[i].Substring(0, eq).Trim()] = parts[i].Substring(eq + 1).Trim(); }
        variants.Add(d);
    }
    if (variants.Count == 0) { var d = new System.Collections.Generic.Dictionary<string, string>(baseJob); d["label"] = "-"; variants.Add(d); }
    if (variants.Count > 10) throw new System.Exception("too many characters in one render (10 at most)");
    string B(string k, string d = "") => baseJob.TryGetValue(k, out var v) ? v : d;
    float BF(string k, float d) => baseJob.TryGetValue(k, out var v) ? float.Parse(v, inv) : d;
    var ss = UnityEngine.Mathf.Clamp((int)BF("ss", 2), 1, 4);
    var cellW = (int)BF("w", 380); var cellH = (int)BF("h", 700);
    var viewH = BF("viewH", 2.3f);
    var spacing = BF("spacing", viewH * cellW / cellH);
    log.Append("== " + B("out", "?") + "\n");
    for (var i = 0; i < pool.Count; i++) pool[i].SetActive(i < variants.Count);

    for (var vi = 0; vi < variants.Count; vi++)
    {
        var job = variants[vi];
        string J(string k, string d = "") => job.TryGetValue(k, out var v) ? v : d;
        float JF(string k, float d) => job.TryGetValue(k, out var v) ? float.Parse(v, inv) : d;
        int JI(string k, int d) => job.TryGetValue(k, out var v) ? int.Parse(v, inv) : d;

        var isNew = vi >= pool.Count;
        var character = isNew ? pru.InstantiatePrefabInScene(prefab) : pool[vi];
        if (isNew) { temp.Add(character); pool.Add(character); }
        character.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
        character.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
        character.transform.localScale = UnityEngine.Vector3.one;
        var app = character.GetComponentInChildren<SyntyPlayerAppearance>(true);
        app.useMeshCombiner = false; // the combiner runs in coroutines: play mode only
        typeof(SyntyPlayerAppearance).GetField("itemManager", bf).SetValue(app, itemManager);

        // only the character's parts are to be seen: name tags, effects, lights and cameras of the prefab are not
        var partObjects = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
        var partFields = new System.Collections.Generic.List<System.Reflection.FieldInfo>();
        foreach (var f in typeof(SyntyPlayerAppearance).GetFields(bf))
        {
            if (f.FieldType != typeof(UnityEngine.GameObject[]) || !(f.GetValue(app) is UnityEngine.GameObject[] arr)) continue;
            partFields.Add(f);
            foreach (var p in arr) if (p) partObjects.Add(p);
        }
        if (isNew)
        {
            // the belt and back parts are in no list until an item wears one (the game finds them then)
            foreach (var t in character.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (t.name.StartsWith("Chr_HipsAttachment_") || t.name.StartsWith("Chr_BackAttachment_")) partObjects.Add(t.gameObject);
            foreach (var r in character.GetComponentsInChildren<UnityEngine.Renderer>(true)) if (!partObjects.Contains(r.gameObject)) r.enabled = false;
            foreach (var c in character.GetComponentsInChildren<UnityEngine.Light>(true)) c.enabled = false;
            foreach (var c in character.GetComponentsInChildren<UnityEngine.Canvas>(true)) c.enabled = false;
            foreach (var c in character.GetComponentsInChildren<UnityEngine.Camera>(true)) c.enabled = false;
        }

        // the player's own look, set as the game sets it
        var male = J("gender", "male") != "female";
        var look = new RavenNest.Models.SyntyAppearance
        {
            Gender = male ? RavenNest.Models.Gender.Male : RavenNest.Models.Gender.Female,
            Hair = JI("hair", 2), Head = JI("head", 0), Eyebrows = JI("brows", 0), FacialHair = JI("beard", male ? 0 : -1), Cape = JI("cape", -1),
            SkinColor = J("skinColor", "#C99A7A"), HairColor = J("hairColor", "#4A3524"), BeardColor = J("hairColor", "#4A3524"),
            EyeColor = J("eyeColor", "#2B2622"), StubbleColor = J("stubbleColor", "#B98C70"), ScarColor = J("scarColor", "#BD8F72"),
            WarPaintColor = J("warPaintColor", "#C99A7A"), HelmetVisible = true,
            CapeHidden = J("capeHidden", "0") == "1",
        };
        var worn = new System.Collections.Generic.List<ItemController>(); // PlayerEquipment.equippedObjects
        // the character's PlayerEquipment was never woken (edit mode): given what Awake would have found, and the
        // list above as its worn items, its own UpdateAppearance is what rebuilds here, as in the game
        var equipment = app.GetComponent<PlayerEquipment>();
        if (!equipment) throw new System.Exception("the Player prefab has no PlayerEquipment next to its appearance");
        typeof(PlayerEquipment).GetField("equippedObjects", bf).SetValue(equipment, worn);
        typeof(PlayerEquipment).GetField("appearance", bf).SetValue(equipment, app);
        UnityEngine.Texture2D capeImage = null;
        ItemController Controller(string spec)
        {
            // "X as Y": X with Y's look (transmogrified)
            var asAt = spec.IndexOf(" as ", System.StringComparison.OrdinalIgnoreCase);
            var item = Items(asAt < 0 ? spec : spec.Substring(0, asAt))[0];
            var skin = asAt < 0 ? null : Items(spec.Substring(asAt + 4))[0];
            var go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(item.Name, UnityEngine.HideFlags.HideAndDontSave, typeof(ItemController));
            go.transform.SetParent(character.transform, false);
            var instance = new RavenNest.Models.InventoryItem { Id = System.Guid.NewGuid(), ItemId = item.Id, TransmogrificationId = skin?.Id };
            return go.GetComponent<ItemController>().Create(itemManager, new GameInventoryItem(null, instance, item, skin), male);
        }
        void Rebuild()
        {
            // everything off, the player's own look on, every worn item on again (the mesh combiner is off)
            equipment.UpdateAppearance();
        }
        void TakeOff(string slot, bool rebuild)
        {
            var e = worn.Find(x => SlotOf(x.Type) == slot);
            if (!e) return;
            app.Unequip(e);
            worn.Remove(e);
            UnityEngine.Object.DestroyImmediate(e.gameObject);
            if (rebuild) Rebuild();
        }
        System.Collections.Generic.List<string> Specs(string what)
        {
            // a whole set becomes one spec per piece
            var specs = new System.Collections.Generic.List<string>();
            if (what.IndexOf(" as ", System.StringComparison.OrdinalIgnoreCase) >= 0 || what.StartsWith("custom ", System.StringComparison.OrdinalIgnoreCase)) { specs.Add(what); return specs; }
            foreach (var item in Items(what))
                foreach (var kv in itemByKey) if (kv.Value == item) { specs.Add(kv.Key); break; }
            return specs;
        }

        // errors logged by the game's code while it dresses the character go in the log (not the one Unity logs
        // for every Renderer.material copy made in edit mode: those copies are destroyed below)
        var materialCopies = 0;
        UnityEngine.Application.LogCallback onLog = (message, stack, type) =>
        {
            if (type == UnityEngine.LogType.Log || type == UnityEngine.LogType.Warning) return;
            if (message.Contains("renderer.material")) { materialCopies++; return; }
            log.Append("  LOGGED " + type + ": " + (message.Length > 400 ? message.Substring(0, 400) : message) + "\n");
        };
        UnityEngine.Application.logMessageReceived += onLog;
        try
        {
            app.SetAppearance(look, null, false);
            foreach (var rawAction in J("do").Split(','))
            {
                var action = rawAction.Trim();
                if (action.Length == 0) continue;
                var space = action.IndexOf(' ');
                var verb = (space < 0 ? action : action.Substring(0, space)).ToLowerInvariant();
                var what = space < 0 ? "" : action.Substring(space + 1).Trim();
                switch (verb)
                {
                    case "wear":
                        foreach (var spec in Specs(what))
                        {
                            var c = Controller(spec);
                            TakeOff(SlotOf(c.Type), false);
                            worn.Add(c);
                        }
                        Rebuild();
                        break;
                    case "puton":
                        foreach (var spec in Specs(what))
                        {
                            var c = Controller(spec);
                            var old = worn.Find(x => SlotOf(x.Type) == SlotOf(c.Type));
                            if (old) { worn.Remove(old); UnityEngine.Object.DestroyImmediate(old.gameObject); }
                            worn.Add(c);
                            app.Equip(c);
                        }
                        break;
                    case "takeoff": TakeOff(what, true); break;
                    case "toggle": app.ToggleHelmVisibility(); break;
                    case "capetoggle": app.ToggleCapeVisibility(); break;
                    case "capeimage":
                        capeImage = new UnityEngine.Texture2D(4, 4) { name = "Preview Cape Image", hideFlags = UnityEngine.HideFlags.HideAndDontSave };
                        temp.Add(capeImage);
                        app.SetCapeTexture(capeImage);
                        break;
                    case "rebuild": Rebuild(); break;
                    case "skincolor":
                        look.SkinColor = what;
                        app.SetAppearance(look, null, false);
                        Rebuild();
                        break;
                    default: throw new System.Exception("unknown action: " + action);
                }
            }
        }
        finally { UnityEngine.Application.logMessageReceived -= onLog; }

        // what is on now, for the log: every active part with its material
        log.Append("  [" + vi + "] " + job["label"] + " (" + (male ? "male" : "female") + ", hair " + look.Hair + "): ");
        foreach (var f in partFields)
        {
            var arr = (UnityEngine.GameObject[])f.GetValue(app);
            var isMale = f.Name.StartsWith("male"); var isFemale = f.Name.StartsWith("female");
            for (var i = 0; i < arr.Length; i++)
            {
                if (!arr[i] || !arr[i].activeInHierarchy) continue;
                var r = arr[i].GetComponent<UnityEngine.Renderer>();
                var m = r && r.sharedMaterial ? r.sharedMaterial.name.Replace(" (Instance)", "").Replace(" (Skin)", "+skin") : "-";
                log.Append(f.Name + "[" + i + "]" + (m == "FantasyHero" ? "" : ":" + m) + ((isMale && !male) || (isFemale && male) ? "(WRONG GENDER)" : "") + " ");
            }
        }
        // the cape's picture: still on the material the cape is drawn with (or would be, while it is hidden)
        var imageState = "none";
        if (capeImage)
        {
            var capeMaterials = (System.Collections.Generic.List<UnityEngine.Material>)typeof(SyntyPlayerAppearance).GetField("capeLogoMaterials", bf).GetValue(app);
            var capeParts = (UnityEngine.GameObject[])typeof(SyntyPlayerAppearance).GetField("capes", bf).GetValue(app);
            var capePart = app.Cape >= 0 && app.Cape < capeParts.Length ? capeParts[app.Cape] : null;
            var capeRenderer = capePart ? capePart.GetComponent<UnityEngine.Renderer>() : null;
            imageState = capeRenderer && capeRenderer.sharedMaterial && capeRenderer.sharedMaterial.GetTexture("_Texture") == capeImage
                && capeMaterials.Contains(capeRenderer.sharedMaterial) ? "kept" : "lost";
        }
        // armor that shows skin: the player's own copies of the tier materials that are on, and their skin color
        var skinMaterials = new System.Collections.Generic.HashSet<UnityEngine.Material>();
        foreach (var r in character.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
            if (r.enabled && r.sharedMaterial && r.sharedMaterial.name.EndsWith(" (Skin)")) skinMaterials.Add(r.sharedMaterial);
        var skinMatch = "none";
        foreach (var m in skinMaterials)
        {
            var c = (UnityEngine.Color32)m.GetColor("_Color_Skin"); var want = (UnityEngine.Color32)app.SkinColor;
            if (skinMatch == "none") skinMatch = "True";
            if (c.r != want.r || c.g != want.g || c.b != want.b) skinMatch = "False";
        }
        log.Append("| state: skinMats=" + skinMaterials.Count + " skinMatch=" + skinMatch + " capeVisible=" + app.CapeVisible + " hasCape=" + app.HasCape + " backPiece=" + app.BackPieceShows + " capeImage=" + imageState + " ");
        log.Append("| material copies logged: " + materialCopies + " | worn: ");
        foreach (var e in worn) log.Append(e.Name + (e.Skin != null ? " as " + e.Skin.Name : "") + " <" + e.LookType + "," + e.Material + "," + e.HeadMask + ">; ");
        log.Append("\n");
        foreach (var e in worn) if (e) UnityEngine.Object.DestroyImmediate(e.gameObject);

        if (J("pose", "idle") != "tpose")
        {
            var animator = character.GetComponentInChildren<UnityEngine.Animator>(true);
            UnityEngine.AnimationClip clip = null;
            foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(J("clip", "Assets/RPG Character Animation Pack/Animations/Relax/RPG-Character@Idle.FBX"))) if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview")) { clip = c; break; }
            if (clip && animator) clip.SampleAnimation(animator.gameObject, clip.length * JF("clipAt", 0.1f));
        }
        foreach (var r in character.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false)) r.updateWhenOffscreen = true;
        character.transform.SetPositionAndRotation(new UnityEngine.Vector3((vi - (variants.Count - 1) * 0.5f) * spacing, 0f, 0f), UnityEngine.Quaternion.Euler(0f, 180f - JF("yaw", 28f), 0f));
    }

    var w = cellW * variants.Count * ss; var h = cellH * ss;
    var cam = pru.camera;
    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    cam.nearClipPlane = 0.1f; cam.farClipPlane = 80f;
    var target = new UnityEngine.Vector3(BF("cx", 0f), BF("cy", 1.0f), 0f);
    cam.orthographic = true;
    cam.orthographicSize = viewH * 0.5f;
    // in front of the characters (they face -z, less their yaw), a little above
    cam.transform.position = target + UnityEngine.Quaternion.Euler(BF("pitch", 6f), 0f, 0f) * UnityEngine.Vector3.back * 20f;
    cam.transform.rotation = UnityEngine.Quaternion.LookRotation(target - cam.transform.position, UnityEngine.Vector3.up);
    var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    if (data) { data.renderPostProcessing = false; data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; }
    // key from the camera's upper left, fill from its right
    pru.lights[0].intensity = BF("key", 1.35f); pru.lights[0].color = Hex("#FFF4E6");
    pru.lights[0].transform.rotation = cam.transform.rotation * UnityEngine.Quaternion.Euler(32f, 30f, 0f);
    pru.lights[0].shadows = UnityEngine.LightShadows.None;
    pru.lights[1].intensity = BF("fill", 0.55f); pru.lights[1].color = Hex("#CFE0FF");
    pru.lights[1].transform.rotation = cam.transform.rotation * UnityEngine.Quaternion.Euler(8f, -55f, 0f);
    pru.ambientColor = Hex(B("ambient", "#5A5F6B"));

    UnityEngine.Color32[] Shot(UnityEngine.Color background)
    {
        cam.backgroundColor = background;
        pru.BeginStaticPreview(new UnityEngine.Rect(0, 0, w, h));
        pru.Render(true, false);
        var tex = pru.EndStaticPreview();
        var px = tex.GetPixels32();
        UnityEngine.Object.DestroyImmediate(tex);
        return px;
    }
    UnityEngine.Color32[] pixels;
    if (B("bg", "#22252B") == "clear")
    {
        // no alpha comes out of a preview: the same picture on black and on white gives it
        var onBlack = Shot(UnityEngine.Color.black);
        var onWhite = Shot(UnityEngine.Color.white);
        pixels = new UnityEngine.Color32[onBlack.Length];
        for (var i = 0; i < pixels.Length; i++)
        {
            var b = onBlack[i]; var wh = onWhite[i];
            var a = 255 - ((wh.r - b.r) + (wh.g - b.g) + (wh.b - b.b)) / 3;
            a = a < 0 ? 0 : a > 255 ? 255 : a;
            if (a == 0) { pixels[i] = new UnityEngine.Color32(0, 0, 0, 0); continue; }
            pixels[i] = new UnityEngine.Color32((byte)System.Math.Min(255, b.r * 255 / a), (byte)System.Math.Min(255, b.g * 255 / a), (byte)System.Math.Min(255, b.b * 255 / a), (byte)a);
        }
    }
    else
    {
        pixels = Shot(Hex(B("bg", "#22252B")));
    }
    // averaged down by the supersampling factor (colors weighted by their alpha)
    var ow = w / ss; var oh = h / ss;
    var small = new UnityEngine.Color32[ow * oh];
    for (var y = 0; y < oh; y++)
    {
        for (var x = 0; x < ow; x++)
        {
            int r = 0, g = 0, bl = 0, a = 0;
            for (var dy = 0; dy < ss; dy++)
            {
                for (var dx = 0; dx < ss; dx++)
                {
                    var p = pixels[(y * ss + dy) * w + x * ss + dx];
                    r += p.r * p.a; g += p.g * p.a; bl += p.b * p.a; a += p.a;
                }
            }
            small[y * ow + x] = a == 0 ? new UnityEngine.Color32(0, 0, 0, 0) : new UnityEngine.Color32((byte)(r / a), (byte)(g / a), (byte)(bl / a), (byte)(a / (ss * ss)));
        }
    }
    var result = new UnityEngine.Texture2D(ow, oh, UnityEngine.TextureFormat.RGBA32, false);
    result.SetPixels32(small);
    var outPath = B("out", System.IO.Path.Combine(project, "Temp", "class-gear-preview.png"));
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
    System.IO.File.WriteAllBytes(outPath, UnityEngine.ImageConversion.EncodeToPNG(result));
    UnityEngine.Object.DestroyImmediate(result);
    log.Append("  -> " + ow + "x" + oh + " @" + sw.ElapsedMilliseconds + "ms\n");

}

void Finish(string outcome)
{
    // the material copies Renderer.material made go with the characters
    foreach (var c in pool)
    {
        if (!c) continue;
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>(true))
            foreach (var m in r.sharedMaterials) if (m && !UnityEditor.EditorUtility.IsPersistent(m)) UnityEngine.Object.DestroyImmediate(m);
        // and the copies of tier materials made for armor that shows skin (the game frees them in OnDestroy, which
        // edit mode does not call)
        var appearance = c.GetComponentInChildren<SyntyPlayerAppearance>(true);
        var own = appearance ? typeof(SyntyPlayerAppearance).GetField("skinMaterials", bf).GetValue(appearance) as System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material> : null;
        if (own != null) foreach (var m in own.Values) if (m) UnityEngine.Object.DestroyImmediate(m);
    }
    foreach (var o in temp) if (o) UnityEngine.Object.DestroyImmediate(o);
    temp.Clear();
    pru.Cleanup();
    System.IO.File.WriteAllText(donePath, outcome + "\n" + log);
}

UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("play mode started");
        if (itemManager == null) { Setup(); return; }
        if (next < jobs.Count) { Render(jobs[next++]); return; }
        UnityEditor.EditorApplication.update -= step;
        Finish("ok: " + jobs.Count + " renders in " + sw.ElapsedMilliseconds + " ms");
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        Finish("FAILED at render " + next + ": " + e);
    }
};
UnityEditor.EditorApplication.update += step;
return "started " + jobs.Count + " renders; wait for " + donePath;
