using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Ravebridge's loot: what hero mode's enemies drop (StreamingAssets/Ravebridge/loot.json). A kill by the hero tosses
/// coins and items onto the ground (LootDrop); walking over them picks them up into the character's Ravebridge bag.
/// The bag is bound to the island: kept on this computer with the character's island quests (RavebridgeQuests.State,
/// in the Ravebridge character's file or states/ravebridge/quests-(character id).json), never sent to the server and
/// never in the character's own inventory. Ravebridge coins are the island's own. Healing draughts top up the hero's
/// potions, and Q drinks one from the bag when those have run out. What the rest is for (the quests, a merchant) comes
/// later; quest rewards that may leave the island go through the server as the hero chest does.
/// </summary>
public static class RavebridgeLoot
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public class Item
    {
        public RavebridgeQuests.Text name;
        public RavebridgeQuests.Text description;
        public string icon;
        public string rarity;
        public string kind;
        public long value;

        [JsonIgnore] public string Key;
        [JsonIgnore] public Rarity Grade;
        [JsonIgnore] public string Glyph;
        [JsonIgnore] public bool IsPotion => kind == "potion";
        [JsonIgnore] public string Name => name != null ? name.Get() : Key;
        [JsonIgnore] public string Description => description != null ? description.Get() : string.Empty;
    }

    public class Drop { public string item; public float chance; public int[] amount; }
    public class Table { public string match; public int[] coins; public List<Drop> drops; }
    public class Book { public Dictionary<string, Item> items; public List<Table> tables; public List<Drop> any; }

    private const float SaveAfter = 5f;
    private static Book book;
    private static readonly HashSet<PlayerController> unsaved = new HashSet<PlayerController>();
    private static float saveAt;

    /// <summary>Something went into a bag: its owner, what (null for coins), how many. For the HUD's popups.</summary>
    public static event Action<PlayerController, Item, long> Picked;

    /// <summary>A bag changed (the inventory window redraws).</summary>
    public static event Action<PlayerController> Changed;

    public static Book Data
    {
        get
        {
            if (book != null) return book;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "Ravebridge", "loot.json");
                book = JsonConvert.DeserializeObject<Book>(File.ReadAllText(path));
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("The Ravebridge loot could not be read: " + exc.Message);
            }
            book ??= new Book();
            book.items ??= new Dictionary<string, Item>();
            book.tables ??= new List<Table>();
            book.any ??= new List<Drop>();
            foreach (var pair in book.items)
            {
                var item = pair.Value;
                item.Key = pair.Key;
                item.Grade = Enum.TryParse<Rarity>(item.rarity, true, out var grade) ? grade : Rarity.Common;
                item.Glyph = int.TryParse(item.icon, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code) ? char.ConvertFromUtf32(code) : "?";
            }
            return book;
        }
    }

    public static Item Find(string key) => key != null && Data.items.TryGetValue(key, out var item) ? item : null;

    /// <summary>The colour of a rarity: grey, green, blue, purple, orange (as the action RPGs do).</summary>
    public static Color ColorOf(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: return new Color(0.35f, 0.9f, 0.35f);
            case Rarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case Rarity.Epic: return new Color(0.75f, 0.4f, 1f);
            case Rarity.Legendary: return new Color(1f, 0.6f, 0.15f);
            default: return new Color(0.88f, 0.86f, 0.8f);
        }
    }

    public static readonly Color CoinColor = new Color(1f, 0.82f, 0.3f);

    // ---- Dropping ------------------------------------------------------------------------------------------

    /// <summary>
    /// A hero's kill: its table's coins and items fall around where it stood. The key is the wave kind's model (with
    /// "boss " in front) or the enemy's own name.
    /// </summary>
    public static void Killed(PlayerController hero, EnemyController enemy)
    {
        if (!hero || !enemy) return;
        var key = (WaveRun.Current ? WaveRun.Current.LootKey(enemy) : null) ?? enemy.name.ToLowerInvariant();
        Table table = null;
        foreach (var t in Data.tables)
        {
            if (t != null && (string.IsNullOrEmpty(t.match) || key.Contains(t.match))) { table = t; break; }
        }
        if (table == null) return;

        var at = enemy.Position;
        var coins = Mathf.RoundToInt(Between(table.coins) * RavebridgeShop.CoinFactor(hero));
        if (coins > 0) LootDrop.Create(hero, null, coins, at);
        var best = Rarity.Common;
        foreach (var list in new[] { table.drops, Data.any })
        {
            if (list == null) continue;
            foreach (var drop in list)
            {
                var item = Find(drop?.item);
                if (item == null || UnityEngine.Random.value > drop.chance) continue;
                LootDrop.Create(hero, item, Math.Max(1, Between(drop.amount)), at);
                if (item.Grade > best) best = item.Grade;
            }
        }
        if (best >= Rarity.Rare)
        {
            GameSounds.Play(SoundKind.RareDrop, at, best >= Rarity.Epic ? 0.9f : 0.6f, 0.15f, hero);
            HeroEffects.Burst(HeroEffects.Get?.Cheer, at + Vector3.up * 0.6f, 1f, 2f);
        }
    }

    private static int Between(int[] range)
    {
        if (range == null || range.Length == 0) return 1;
        if (range.Length == 1) return range[0];
        return UnityEngine.Random.Range(range[0], range[1] + 1);
    }

    // ---- The bag -------------------------------------------------------------------------------------------

    /// <summary>A drop picked up: into the bag, with its sound and its popup.</summary>
    public static void Pick(PlayerController player, Item item, long amount)
    {
        if (!player || amount <= 0) return;
        if (item == null)
        {
            RavebridgeQuests.StateOf(player).Coins += amount;
            GameSounds.Play(SoundKind.CoinPickup, player.Position, 0.7f, 0f, player);
            MarkChanged(player);
        }
        else
        {
            Add(player, item, amount);
            GameSounds.Play(item.IsPotion ? SoundKind.PotionPickup : SoundKind.ItemPickup, player.Position, 0.7f, 0f, player);
        }
        Picked?.Invoke(player, item, amount);
    }

    /// <summary>Items into the bag (a healing draught into the hero's potions while there is room).</summary>
    public static void Add(PlayerController player, Item item, long amount)
    {
        if (!player || item == null || amount <= 0) return;
        var state = RavebridgeQuests.StateOf(player);
        state.Loot ??= new Dictionary<string, long>();
        var hero = HeroController.Active;
        var left = amount;
        if (item.IsPotion && hero && hero.Player == player)
        {
            while (left > 0 && hero.Potions < hero.MaxPotions) { hero.AddPotion(); left--; }
        }
        if (left > 0) state.Loot[item.Key] = (state.Loot.TryGetValue(item.Key, out var had) ? had : 0) + left;
        MarkChanged(player);
    }

    /// <summary>Items out of the bag (sold); never below none.</summary>
    public static void Remove(PlayerController player, Item item, long amount)
    {
        if (!player || item == null || amount <= 0) return;
        var loot = RavebridgeQuests.StateOf(player).Loot;
        if (loot == null || !loot.TryGetValue(item.Key, out var had)) return;
        if (had - amount <= 0) loot.Remove(item.Key);
        else loot[item.Key] = had - amount;
        MarkChanged(player);
    }

    /// <summary>The bag changed some other way (coins spent): saved a few seconds later.</summary>
    public static void Saved(PlayerController player) => MarkChanged(player);

    public static long Coins(PlayerController player) => player ? RavebridgeQuests.StateOf(player).Coins : 0;

    public static long Count(PlayerController player, string key)
    {
        if (!player) return 0;
        var loot = RavebridgeQuests.StateOf(player).Loot;
        return loot != null && loot.TryGetValue(key, out var n) ? n : 0;
    }

    /// <summary>What the bag holds, best first.</summary>
    public static List<(Item Item, long Amount)> Contents(PlayerController player)
    {
        var list = new List<(Item, long)>();
        var loot = player ? RavebridgeQuests.StateOf(player).Loot : null;
        if (loot == null) return list;
        foreach (var pair in loot)
        {
            var item = Find(pair.Key);
            if (item != null && pair.Value > 0) list.Add((item, pair.Value));
        }
        list.Sort((a, b) => a.Item1.Grade != b.Item1.Grade ? b.Item1.Grade.CompareTo(a.Item1.Grade) : string.CompareOrdinal(a.Item1.Name, b.Item1.Name));
        return list;
    }

    /// <summary>A healing draught out of the bag (the hero's own potions are gone): true if there was one.</summary>
    public static bool TakePotion(PlayerController player)
    {
        if (!player) return false;
        var loot = RavebridgeQuests.StateOf(player).Loot;
        if (loot == null) return false;
        string key = null;
        foreach (var pair in loot)
        {
            var item = Find(pair.Key);
            if (item != null && item.IsPotion && pair.Value > 0) { key = pair.Key; break; }
        }
        if (key == null) return false;
        loot[key] -= 1;
        MarkChanged(player);
        return true;
    }

    public static long PotionsInBag(PlayerController player)
    {
        if (!player) return 0;
        var loot = RavebridgeQuests.StateOf(player).Loot;
        if (loot == null) return 0;
        long n = 0;
        foreach (var pair in loot) { var item = Find(pair.Key); if (item != null && item.IsPotion) n += pair.Value; }
        return n;
    }

    private static void MarkChanged(PlayerController player)
    {
        if (unsaved.Count == 0) saveAt = Time.unscaledTime + SaveAfter;
        unsaved.Add(player);
        // its tick saves it (a purchase may come before anything has dropped)
        LootManager.Ensure();
        Changed?.Invoke(player);
    }

    /// <summary>Every frame (LootDrop's manager): the bags that changed go to disk a few seconds later.</summary>
    public static void Tick()
    {
        if (unsaved.Count > 0 && Time.unscaledTime >= saveAt) SaveNow();
    }

    /// <summary>Every changed bag to disk now (hero mode ending, the game closing).</summary>
    public static void SaveNow()
    {
        foreach (var player in unsaved) if (player) RavebridgeQuests.Save(player);
        unsaved.Clear();
    }
}
