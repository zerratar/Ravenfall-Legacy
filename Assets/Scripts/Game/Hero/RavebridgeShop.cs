using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// The shopkeeper's cart on Ravebridge (it finally came): Ravebridge coins buy healing draughts, buffs for a few
/// minutes (a whetstone, a hearty stew) and hero mode upgrades that stay (a bigger potion belt, light boots, a lucky
/// charm); the loot in the Ravebridge bag sells for its worth. What it sells is in StreamingAssets/Ravebridge/loot.json
/// ("shop"). Upgrades are kept with the bag in the character's Ravebridge state (on this computer); buffs last while
/// the game runs. Talking to the shopkeeper opens it (RavebridgeQuests), RavebridgeShopWindow shows it.
/// </summary>
public static class RavebridgeShop
{
    public class Entry
    {
        public string key;
        public string kind;    // item, buff, upgrade
        public string item;    // an item's key in the loot data
        public string stat;    // damage, guard; potions, roll, luck
        public float amount;
        public float minutes;
        public long price;
        public long[] prices;  // an upgrade's, level by level
        public string icon;
        public RavebridgeQuests.Text name;
        public RavebridgeQuests.Text description;

        [JsonIgnore] public RavebridgeLoot.Item Item => kind == "item" ? RavebridgeLoot.Find(item) : null;
        [JsonIgnore] public string Name => Item != null ? Item.Name : name != null ? name.Get() : key;
        [JsonIgnore] public string Description => Item != null ? Item.Description : description != null ? description.Get() : string.Empty;
        [JsonIgnore] public string Glyph => Item != null ? Item.Glyph : int.TryParse(icon, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code) ? char.ConvertFromUtf32(code) : "?";
        [JsonIgnore] public bool IsUpgrade => kind == "upgrade";
        [JsonIgnore] public bool IsBuff => kind == "buff";
        [JsonIgnore] public int MaxLevel => prices?.Length ?? 0;
    }

    public enum BuyResult { Bought, TooPoor, Maxed, Unknown }

    /// <summary>The piece the last satchel held (the window names it).</summary>
    public static RavebridgeGear.Piece LastGear { get; private set; }

    // buffs bought: per character, per stat, until when (unscaled time) and how strong
    private static readonly Dictionary<Guid, Dictionary<string, (float Until, float Amount)>> buffs = new Dictionary<Guid, Dictionary<string, (float, float)>>();

    /// <summary>The shopkeeper opened his cart for a character (the window listens).</summary>
    public static event Action<PlayerController> Opened;

    /// <summary>Something was bought or sold (the window redraws, the HUD updates).</summary>
    public static event Action<PlayerController> Changed;

    public static IReadOnlyList<Entry> Entries => Shop ?? (IReadOnlyList<Entry>)Array.Empty<Entry>();

    private static List<Entry> shop;
    private static List<Entry> Shop
    {
        get
        {
            if (shop != null) return shop;
            try
            {
                var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Ravebridge", "loot.json");
                shop = JsonConvert.DeserializeObject<ShopFile>(System.IO.File.ReadAllText(path))?.shop;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("The Ravebridge shop could not be read: " + exc.Message);
            }
            shop ??= new List<Entry>();
            shop.RemoveAll(e => e == null || string.IsNullOrEmpty(e.key));
            return shop;
        }
    }

    private class ShopFile { public List<Entry> shop; }

    public static void Open(PlayerController player)
    {
        if (player) Opened?.Invoke(player);
    }

    // ---- Prices and levels -----------------------------------------------------------------------------------

    public static int Level(PlayerController player, string key)
    {
        if (!player) return 0;
        var upgrades = RavebridgeQuests.StateOf(player).Upgrades;
        return upgrades != null && upgrades.TryGetValue(key, out var level) ? level : 0;
    }

    /// <summary>What the entry costs now (an upgrade's next level), or -1 when there is nothing more to buy.</summary>
    public static long PriceOf(PlayerController player, Entry entry)
    {
        if (entry.IsUpgrade)
        {
            var level = Level(player, entry.key);
            return level < entry.MaxLevel ? entry.prices[level] : -1;
        }
        return entry.price;
    }

    public static BuyResult Buy(PlayerController player, Entry entry)
    {
        if (!player || entry == null) return BuyResult.Unknown;
        var price = PriceOf(player, entry);
        if (price < 0) return BuyResult.Maxed;
        var state = RavebridgeQuests.StateOf(player);
        if (state.Coins < price) return BuyResult.TooPoor;

        switch (entry.kind)
        {
            case "item":
                var item = entry.Item;
                if (item == null) return BuyResult.Unknown;
                state.Coins -= price;
                // straight into the bag (a draught into the hero's potions while there is room)
                RavebridgeLoot.Add(player, item, 1);
                break;
            case "buff":
                state.Coins -= price;
                if (!buffs.TryGetValue(player.Id, out var mine)) buffs[player.Id] = mine = new Dictionary<string, (float, float)>();
                var from = mine.TryGetValue(entry.stat, out var had) && had.Until > Time.unscaledTime ? had.Until : Time.unscaledTime;
                mine[entry.stat] = (from + entry.minutes * 60f, entry.amount);
                break;
            case "gear":
                // the satchel rolls as a boss's drop does: worth its price more often than not
                var piece = RavebridgeGear.Roll(true);
                if (piece == null) return BuyResult.Unknown;
                state.Coins -= price;
                RavebridgeLoot.PickGear(player, piece);
                LastGear = piece;
                break;
            case "upgrade":
                state.Coins -= price;
                state.Upgrades ??= new Dictionary<string, int>();
                state.Upgrades[entry.key] = Level(player, entry.key) + 1;
                if (entry.stat == "potions" && HeroController.Active && HeroController.Active.Player == player) HeroController.Active.AddPotion();
                break;
            default:
                return BuyResult.Unknown;
        }
        GameSounds.Play(SoundKind.Purchase, player.Position, 0.7f, 0f, player);
        RavebridgeLoot.Saved(player);
        Changed?.Invoke(player);
        return BuyResult.Bought;
    }

    /// <summary>Sells loot from the bag at its worth: some of it, or all with <paramref name="amount"/> below 0.</summary>
    public static long Sell(PlayerController player, RavebridgeLoot.Item item, long amount)
    {
        if (!player || item == null) return 0;
        var have = RavebridgeLoot.Count(player, item.Key);
        if (amount < 0 || amount > have) amount = have;
        if (amount <= 0) return 0;
        var coins = item.value * amount;
        RavebridgeLoot.Remove(player, item, amount);
        RavebridgeQuests.StateOf(player).Coins += coins;
        GameSounds.Play(SoundKind.Sell, player.Position, 0.7f, 0f, player);
        RavebridgeLoot.Saved(player);
        Changed?.Invoke(player);
        return coins;
    }

    // ---- What they do in hero mode ------------------------------------------------------------------------------

    private static float Upgrade(PlayerController player, string stat)
    {
        var total = 0;
        foreach (var e in Entries) if (e.IsUpgrade && e.stat == stat) total += Level(player, e.key);
        return total;
    }

    private static float Buff(PlayerController player, string stat)
    {
        if (!player || !buffs.TryGetValue(player.Id, out var mine) || !mine.TryGetValue(stat, out var b)) return 0f;
        return b.Until > Time.unscaledTime ? b.Amount : 0f;
    }

    /// <summary>Seconds left of a buff, 0 when it is not on.</summary>
    public static float BuffLeft(PlayerController player, string stat)
    {
        if (!player || !buffs.TryGetValue(player.Id, out var mine) || !mine.TryGetValue(stat, out var b)) return 0f;
        return Mathf.Max(0f, b.Until - Time.unscaledTime);
    }

    /// <summary>The buffs on now, for the HUD: the shop entry and the seconds left.</summary>
    public static IEnumerable<(Entry Entry, float Left)> ActiveBuffs(PlayerController player)
    {
        foreach (var e in Entries)
        {
            if (!e.IsBuff) continue;
            var left = BuffLeft(player, e.stat);
            if (left > 0f) yield return (e, left);
        }
    }

    /// <summary>The hero's damage, times this (the whetstone).</summary>
    public static float DamageFactor(PlayerController player) => 1f + Buff(player, "damage") + RavebridgeGear.Stat(player, "damage") / 100f;

    /// <summary>The damage the hero takes, times this (the stew and the gear's guard; never under 40%).</summary>
    public static float TakenFactor(PlayerController player) => Mathf.Max(0.4f, 1f - Buff(player, "guard") - RavebridgeGear.Stat(player, "guard") / 100f);

    /// <summary>Potions the hero carries over the three (the belt).</summary>
    public static int ExtraPotions(PlayerController player) => Mathf.RoundToInt(Upgrade(player, "potions"));

    /// <summary>Seconds off the roll's wait (the boots).</summary>
    public static float RollSooner(PlayerController player) => Upgrade(player, "roll") * 0.5f;

    /// <summary>Coins from drops, times this (the charm).</summary>
    public static float CoinFactor(PlayerController player) => 1f + Upgrade(player, "luck") * 0.1f + RavebridgeGear.Stat(player, "luck") / 100f;
}
