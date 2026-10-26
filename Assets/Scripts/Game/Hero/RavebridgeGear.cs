using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Hero gear: worn only in hero mode on Ravebridge, on top of the character's own equipment and never instead of it.
/// Four slots (amulet, ring, charm, relic); each piece is a base (StreamingAssets/Ravebridge/loot.json, "gear")
/// rolled with affixes when it drops, more and stronger the rarer it is: damage, guard (less damage taken), attack
/// speed, double hits, life leech, health back each second, mana back, coins from drops, potion healing. Named after
/// its strongest affix ("Bent wire ring of Swiftness"). Kept with the Ravebridge bag in the character's Ravebridge
/// state on this computer (RavebridgeQuests.State.Gear); hero mode reads the worn pieces' totals (<see cref="Stat"/>).
/// </summary>
public static class RavebridgeGear
{
    [Serializable]
    public class Piece
    {
        public string Id;
        public string Base;
        public string Rarity;
        public Dictionary<string, float> Affixes = new Dictionary<string, float>();
        public bool Worn;

        [JsonIgnore] public RavebridgeLoot.Rarity Grade => Enum.TryParse<RavebridgeLoot.Rarity>(Rarity, true, out var g) ? g : RavebridgeLoot.Rarity.Common;
        [JsonIgnore] public BaseData Data => Book.bases != null && Base != null && Book.bases.TryGetValue(Base, out var b) ? b : null;
        [JsonIgnore] public string Slot => Data?.slot ?? "charm";
        [JsonIgnore] public string Glyph => Data != null && int.TryParse(Data.icon, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var c) ? char.ConvertFromUtf32(c) : "?";
        [JsonIgnore] public Color Color => RavebridgeLoot.ColorOf(Grade);
        [JsonIgnore] public long Value => Book.rarity != null && Book.rarity.TryGetValue(Rarity ?? "common", out var r) ? r.value : 10;

        /// <summary>The base's name, with the strongest affix's suffix: "Bent wire ring of Swiftness".</summary>
        [JsonIgnore]
        public string Name
        {
            get
            {
                var name = Data?.name?.Get() ?? Base;
                var top = Affixes == null || Affixes.Count == 0 ? null : Affixes.OrderByDescending(a => a.Value / Math.Max(0.001f, Book.affixes.TryGetValue(a.Key, out var d) ? d.max : 1f)).First().Key;
                var suffix = top != null && Book.affixes.TryGetValue(top, out var affix) ? affix.suffix?.Get() : null;
                var format = Book.names?.Get() ?? "{0} of {1}";
                return string.IsNullOrEmpty(suffix) ? name : string.Format(format, name, suffix);
            }
        }
    }

    public class BaseData { public RavebridgeQuests.Text name; public string slot; public string icon; }
    public class AffixData { public RavebridgeQuests.Text text; public RavebridgeQuests.Text suffix; public float min; public float max; }
    public class RarityData { public int affixes; public float power; public long value; public float weight; }
    public class GearBook
    {
        public Dictionary<string, BaseData> bases;
        public Dictionary<string, RavebridgeQuests.Text> slots;
        public Dictionary<string, AffixData> affixes;
        public Dictionary<string, RarityData> rarity;
        public Dictionary<string, float> boss;
        public RavebridgeQuests.Text names;
    }

    private class GearFile { public GearBook gear; }

    public static readonly string[] Slots = { "amulet", "ring", "charm", "relic" };

    private static GearBook book;
    public static GearBook Book
    {
        get
        {
            if (book != null) return book;
            try
            {
                var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Ravebridge", "loot.json");
                book = JsonConvert.DeserializeObject<GearFile>(System.IO.File.ReadAllText(path))?.gear;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("The Ravebridge gear could not be read: " + exc.Message);
            }
            book ??= new GearBook();
            book.bases ??= new Dictionary<string, BaseData>();
            book.slots ??= new Dictionary<string, RavebridgeQuests.Text>();
            book.affixes ??= new Dictionary<string, AffixData>();
            book.rarity ??= new Dictionary<string, RarityData>();
            return book;
        }
    }

    /// <summary>Gear changed (the inventory window, the shop).</summary>
    public static event Action<PlayerController> Changed;

    public static string SlotName(string slot) => Book.slots.TryGetValue(slot, out var t) ? t.Get() : slot;

    // ---- Rolling ---------------------------------------------------------------------------------------------

    /// <summary>A new piece: a random base, a rarity by the weights (a boss's better), its affixes rolled.</summary>
    public static Piece Roll(bool boss)
    {
        if (Book.bases.Count == 0 || Book.affixes.Count == 0 || Book.rarity.Count == 0) return null;
        var rarity = Pick(boss && Book.boss != null ? Book.boss : Book.rarity.ToDictionary(r => r.Key, r => r.Value.weight));
        var data = Book.rarity.TryGetValue(rarity, out var r) ? r : new RarityData { affixes = 1, power = 1f };
        var bases = Book.bases.Keys.ToList();
        var piece = new Piece { Id = Guid.NewGuid().ToString("N"), Base = bases[UnityEngine.Random.Range(0, bases.Count)], Rarity = rarity };
        var pool = Book.affixes.Keys.ToList();
        for (var i = 0; i < data.affixes && pool.Count > 0; i++)
        {
            var key = pool[UnityEngine.Random.Range(0, pool.Count)];
            pool.Remove(key);
            var affix = Book.affixes[key];
            var value = UnityEngine.Random.Range(affix.min, affix.max) * data.power;
            // whole percents, or tenths for the small ones (leech, regen)
            piece.Affixes[key] = affix.max < 2f ? Mathf.Round(value * 10f) / 10f : Mathf.Round(value);
        }
        return piece;
    }

    private static string Pick(Dictionary<string, float> weights)
    {
        var total = weights.Values.Sum();
        var roll = UnityEngine.Random.value * total;
        foreach (var w in weights)
        {
            roll -= w.Value;
            if (roll <= 0f) return w.Key;
        }
        return weights.Keys.First();
    }

    // ---- The character's gear ----------------------------------------------------------------------------------

    public static List<Piece> Of(PlayerController player)
    {
        if (!player) return new List<Piece>();
        var state = RavebridgeQuests.StateOf(player);
        return state.Gear ??= new List<Piece>();
    }

    public static Piece Worn(PlayerController player, string slot) => Of(player).FirstOrDefault(p => p.Worn && p.Slot == slot);

    /// <summary>A piece into the character's gear (from a drop or the satchel).</summary>
    public static void Add(PlayerController player, Piece piece)
    {
        if (!player || piece == null) return;
        Of(player).Add(piece);
        // nothing in its slot yet: worn at once
        if (Worn(player, piece.Slot) == null) piece.Worn = true;
        RavebridgeLoot.Saved(player);
        Changed?.Invoke(player);
    }

    /// <summary>Wears a piece, taking off whatever was in its slot; or takes it off when it is worn.</summary>
    public static void Toggle(PlayerController player, Piece piece)
    {
        if (!player || piece == null) return;
        if (piece.Worn) piece.Worn = false;
        else
        {
            foreach (var other in Of(player)) if (other.Worn && other.Slot == piece.Slot) other.Worn = false;
            piece.Worn = true;
        }
        RavebridgeLoot.Saved(player);
        Changed?.Invoke(player);
    }

    /// <summary>Sold at the shopkeeper's: gone, its worth in Ravebridge coins.</summary>
    public static long Sell(PlayerController player, Piece piece)
    {
        if (!player || piece == null || !Of(player).Remove(piece)) return 0;
        RavebridgeQuests.StateOf(player).Coins += piece.Value;
        GameSounds.Play(SoundKind.Sell, player.Position, 0.7f, 0f, player);
        RavebridgeLoot.Saved(player);
        Changed?.Invoke(player);
        return piece.Value;
    }

    /// <summary>An affix's total over the worn pieces, in percent (0 when none).</summary>
    public static float Stat(PlayerController player, string affix)
    {
        if (!player) return 0f;
        var total = 0f;
        foreach (var p in Of(player)) if (p.Worn && p.Affixes != null && p.Affixes.TryGetValue(affix, out var v)) total += v;
        return total;
    }

    /// <summary>One affix as it reads: "+5% damage".</summary>
    public static string Describe(string affix, float value)
    {
        var text = Book.affixes.TryGetValue(affix, out var a) ? a.text?.Get() : null;
        var number = value.ToString(value % 1f == 0f ? "0" : "0.0", CultureInfo.InvariantCulture);
        return string.Format(text ?? "{0} " + affix, number);
    }

    /// <summary>The worn pieces' totals, one line each ("+12% damage"), strongest first.</summary>
    public static List<string> Totals(PlayerController player)
    {
        var list = new List<string>();
        foreach (var affix in Book.affixes.Keys)
        {
            var total = Stat(player, affix);
            if (total > 0f) list.Add(Describe(affix, total));
        }
        return list;
    }
}
