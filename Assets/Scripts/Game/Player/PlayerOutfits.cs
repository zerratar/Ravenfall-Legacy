using System;
using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;
using Skill = RavenNest.Models.Skill;

/// <summary>
///     A player's outfits per skill, as the game knows them (docs/outfits-design.md): gear that follows what is trained.
///     The server keeps the outfits and decides what is worn; this watches what the player trains and whether they are
///     in a raid, a dungeon or on a tower floor, asks the server when the outfit should change, and applies the answer
///     (the gear itself comes with it as inventory changes, the pet through the stable). The rules are
///     RavenNest.Models.OutfitRules, the server's too.
/// </summary>
/// <remarks>
///     Looked at once a second per player, and the server is only asked when something would change
///     (OutfitRules.NeedsAsking): a player who never wore different gear for different skills costs nothing, and a raid
///     that begins asks only for the gatherers who have a combat outfit of their own. Because it is the state that is
///     watched (training, in a fight or not) and not each way in and out, a player who is knocked out, leaves a raid
///     early or is sent home when it ends gets their own outfit back all the same; one who leaves the game in the
///     middle is put right by the server when they next join (OutfitManager.ComeBack).
/// </remarks>
public sealed class PlayerOutfits
{
    private readonly PlayerController owner;
    private CharacterOutfits state = new CharacterOutfits();

    // the skill whose outfit is on, as far as the game knows: the server's word, or before it kept anything the skill
    // that was trained until now
    private Skill worn = Skill.None;

    // what was last looked at and settled (asked for, or needing nothing)
    private bool seen;
    private Skill seenTrained = Skill.None;
    private bool seenFighting;

    private bool asking;
    private float nextLook;

    public PlayerOutfits(PlayerController owner)
    {
        this.owner = owner;
        // not everyone in the same frame
        nextLook = Time.time + UnityEngine.Random.Range(0.5f, 1.5f);
    }

    /// <summary>The outfits as the server last said. Never null.</summary>
    public CharacterOutfits State => state;

    /// <summary>The server's last answer that named pieces gone from an outfit (what the player was told once); null before any.</summary>
    public OutfitResult LastGone { get; private set; }

    /// <summary>The skill whose outfit is on.</summary>
    public Skill Worn => worn != Skill.None ? worn : OutfitRules.Desired(Trained, Fighting, state.Fight);

    /// <summary>The skill the player trains, as outfits know it; None without a task.</summary>
    public Skill Trained => owner ? OutfitRules.Normalize(owner.ActiveSkill) : Skill.None;

    /// <summary>In a raid, a dungeon or on a tower floor (a tower town is a calm place: the training outfit).</summary>
    public bool Fighting => owner
        && ((owner.raidHandler != null && owner.raidHandler.InRaid)
        || (owner.dungeonHandler != null && owner.dungeonHandler.InDungeon)
        || (owner.towerHandler != null && owner.towerHandler.InTower && !owner.towerHandler.InTown));

    /// <summary>A real player whose inventory the server feeds: bots and games without the feed have no outfits.</summary>
    public bool Active => owner && !owner.IsBot && owner.Inventory && owner.Inventory.FedByServer;

    /// <summary>The character as it joined: its outfits from the server.</summary>
    public void Load(string text)
    {
        state = CharacterOutfits.Parse(text);
        worn = state.Worn;
        seen = false;
        ApplyWord();
    }

    /// <summary>The server's answer to anything about outfits: they are as it says.</summary>
    public void Apply(OutfitResult result, bool announce)
    {
        if (result == null || result.Outfits == null) return;
        var followed = state.Follow;
        state = CharacterOutfits.Parse(result.Outfits);
        if (state.Worn != Skill.None) worn = state.Worn;
        // "change outfit with training" was turned on again: what is trained now is looked at afresh
        if (state.Follow && !followed) seen = false;
        ApplyWord();
        if (result.Pets != null && owner) owner.BattlePets.Apply(new PetResult { Status = PetResultStatus.Ok, Pets = result.Pets });
        if (announce) AnnounceGone(result);
    }

    // the role or line asked for with the outfit that is on ("!equip tank") ranks the gear that arrives (Inventory.AskedOutfit)
    private void ApplyWord()
    {
        if (!owner || !owner.Inventory) return;
        var outfit = state.Find(worn);
        owner.Inventory.AskedOutfit = outfit != null ? GearOutfit.Parse(outfit.Word) : null;
    }

    /// <summary>
    ///     "!equip tank" or "!equip all" was used: the role or line (null for none) belongs to the outfit that is on. The
    ///     server keeps it the same way when it does its "equip best"; this is the game's note of it until the server
    ///     next sends the outfits.
    /// </summary>
    public void Asked(GearOutfit asked)
    {
        if (!Active) return;
        var skill = Worn;
        if (skill == Skill.None) return;
        var outfit = asked != null ? state.GetOrAdd(skill) : state.Find(skill);
        if (outfit != null) outfit.Word = asked != null ? asked.Word : null;
    }

    // ---------------------------------------------------------------- once a second

    public void Tick()
    {
        if (asking) return;
        var now = Time.time;
        if (now < nextLook) return;
        nextLook = now + 1f;
        if (!Active || owner.Removed) return;

        var trained = Trained;
        if (trained == Skill.None) return;
        var fighting = Fighting;
        if (seen && trained == seenTrained && fighting == seenFighting) return;

        // never in the middle of a fight: someone who picks another skill while the raid, dungeon or floor goes on
        // keeps what they wear until it is over (!outfit (skill) changes it at once, for those who mean it)
        if (seen && seenFighting && fighting) return;

        // an outfit is on from the first moment: before the server kept anything it is the one of what was trained
        // until now, and the server is told so when it is first asked
        var nothingKept = worn == Skill.None;
        if (nothingKept) worn = seen ? OutfitRules.Desired(seenTrained, seenFighting, state.Fight) : OutfitRules.Desired(trained, fighting, state.Fight);

        if ((!seen && nothingKept) || !OutfitRules.NeedsAsking(state, trained, fighting, worn))
        {
            Settle(trained, fighting);
            return;
        }

        Follow(trained, fighting);
    }

    private void Settle(Skill trained, bool fighting)
    {
        seen = true;
        seenTrained = trained;
        seenFighting = fighting;
    }

    private async void Follow(Skill trained, bool fighting)
    {
        asking = true;
        try
        {
            var result = await owner.GameManager.RavenNest.Players.OutfitFollowAsync(owner.Id, (int)trained, fighting, (int)worn);
            if (!owner || owner.Removed) return;
            if (result == null || result.Status != OutfitStatus.Ok)
            {
                // not now (the server is away, or the player is not in its session yet): asked again in a while
                nextLook = Time.time + 5f;
                return;
            }
            Apply(result, true);
            Settle(trained, fighting);
        }
        catch (Exception exc)
        {
            nextLook = Time.time + 5f;
            Shinobytes.Debug.LogError("Outfit for " + (owner ? owner.PlayerName : "?") + ": " + exc.Message);
        }
        finally
        {
            asking = false;
        }
    }

    // pieces of the outfit were gone (sold, traded, stashed) and the best the player owns filled in: said once, since
    // the outfit has the new pieces from then on
    private void AnnounceGone(OutfitResult result)
    {
        var format = GoneText(result, out var args);
        if (format != null) owner.GameManager.RavenBot.SendReply(owner, format, args);
    }

    /// <summary>
    ///     What to tell the player when pieces of an outfit were gone: the text and its arguments, or null when none
    ///     were. Said once: the outfit has the new pieces from then on.
    /// </summary>
    public string GoneText(OutfitResult result, out object[] args)
    {
        args = null;
        if (result == null || result.Gone == null || result.Gone.Length == 0 || !owner || !owner.GameManager) return null;
        LastGone = result;
        var gone = new List<string>();
        var filled = new List<string>();
        for (var i = 0; i < result.Gone.Length; ++i)
        {
            var item = owner.GameManager.Items.Get(result.Gone[i]);
            if (item != null && !gone.Contains(item.Name)) gone.Add(item.Name);
            var with = result.Filled != null && i < result.Filled.Length && result.Filled[i] != Guid.Empty ? owner.GameManager.Items.Get(result.Filled[i]) : null;
            if (with != null && !filled.Contains(with.Name)) filled.Add(with.Name);
        }
        if (gone.Count == 0) return null;
        var skill = Localization.CombatStyleName((Skill)result.Skill);
        if (filled.Count > 0)
        {
            args = new object[] { skill, Join(gone), Join(filled) };
            return Localization.MSG_OUTFIT_FILLED;
        }
        args = new object[] { skill, Join(gone) };
        return Localization.MSG_OUTFIT_GONE;
    }

    // ---------------------------------------------------------------- gear that arrives

    /// <summary>
    ///     May a piece that arrives replace what is worn in its slot by itself: not with "upgrade automatically" off, and
    ///     never in a slot that is pinned in the outfit that is on.
    /// </summary>
    public bool MayReplace(ItemType type)
    {
        return !Active || OutfitRules.MayReplace(state, Worn, OutfitRules.SlotOf(type));
    }

    /// <summary>
    ///     The game put a piece on by itself (one that arrived and beat what was worn): the server is told, so what it
    ///     takes to be worn, and keeps as the outfit, is what the player wears. A piece that is still on its way to the
    ///     server is told when it is there (PlayerController.AddQueuedItemAsync).
    /// </summary>
    public void PutOn(GameInventoryItem item)
    {
        if (!Active || item == null || item.InventoryItem == null || !item.InventoryItem.Equipped) return;
        if (owner.IsBeingAdded(item)) return;
        _ = owner.GameManager.RavenNest.Players.EquipInventoryItemAsync(owner.Id, item.InstanceId);
    }

    /// <summary>
    ///     A piece arrived (found, bought, a reward): it also goes into every outfit that is not on, where its slot there
    ///     is "best" and it beats the outfit's piece (OutfitRules.Upgrades). The game only looks whether any outfit would
    ///     take it; the server decides and keeps it.
    /// </summary>
    public void Arrived(GameInventoryItem item)
    {
        if (!Active || !state.Upgrade || state.Outfits.Count == 0 || item == null || item.Item == null) return;
        var slot = OutfitRules.SlotOf(item.Item.Type);
        if (!OutfitRules.IsOutfitSlot(slot)) return;
        var on = Worn;
        var any = false;
        foreach (var outfit in state.Outfits)
        {
            if (outfit.Skill == on) continue;
            var entry = outfit.Get(slot);
            if (entry == null || entry.Pinned) continue;
            any = true;
            break;
        }
        if (!any) return;

        var pieces = Pieces();
        var candidate = PieceOf(item, false);
        foreach (var outfit in state.Outfits)
        {
            if (outfit.Skill == on || !OutfitRules.Upgrades(outfit, candidate, pieces)) continue;
            Offer(item);
            return;
        }
    }

    private async void Offer(GameInventoryItem item)
    {
        try
        {
            var result = await owner.GameManager.RavenNest.Players.OutfitOfferAsync(owner.Id, item.InstanceId, item.Item.Id);
            if (!owner || owner.Removed || result == null || result.Status != OutfitStatus.Ok) return;
            Apply(result, false);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Outfit offer for " + (owner ? owner.PlayerName : "?") + ": " + exc.Message);
        }
    }

    /// <summary>What the player has, as the outfit rules see it.</summary>
    public List<OutfitPiece> Pieces() => Pieces(null);

    private List<OutfitPiece> Pieces(List<GameInventoryItem> items)
    {
        var pieces = new List<OutfitPiece>();
        if (!owner || !owner.Inventory) return pieces;
        foreach (var item in owner.Inventory.GetEquippedItems().ToArray())
        {
            if (item == null || item.Item == null || !OutfitRules.IsOutfitSlot(OutfitRules.SlotOf(item.Item.Type))) continue;
            pieces.Add(PieceOf(item, true));
            if (items != null) items.Add(item);
        }
        foreach (var item in owner.Inventory.GetBackpackItems().ToArray())
        {
            if (item == null || item.Item == null || !OutfitRules.IsOutfitSlot(OutfitRules.SlotOf(item.Item.Type))) continue;
            pieces.Add(PieceOf(item, false));
            if (items != null) items.Add(item);
        }
        return pieces;
    }

    /// <summary>
    ///     After the game's own "equip best" ("!equip all", "!equip tank"): the pinned pieces of the outfit that is on go
    ///     back on, as the server puts them back after its "equip best" (OutfitManager.EquippedBest, by the same rule),
    ///     so the two wear the same afterwards.
    /// </summary>
    public void WearPinned()
    {
        if (!Active) return;
        var on = state.Find(Worn);
        if (on == null || on.PinnedCount == 0) return;

        // a copy: the outfits are the server's to change
        var pinned = new Outfit { Skill = on.Skill, Word = on.Word };
        foreach (var slot in on.Slots)
        {
            if (slot.Pinned) pinned.Set(slot.Slot, slot.Item, slot.ItemId, true);
        }
        var items = new List<GameInventoryItem>();
        var pieces = Pieces(items);
        var plan = OutfitRules.Wear(pinned, pieces, true);
        foreach (var index in plan.Equip) owner.Inventory.Equip(items[index]);
    }

    private OutfitPiece PieceOf(GameInventoryItem item, bool isWorn)
    {
        var stack = item.InventoryItem;
        return new OutfitPiece
        {
            Id = item.InstanceId,
            ItemId = item.Item.Id,
            Type = item.Item.Type,
            Category = item.Item.Category,
            Material = item.Item.Material,
            Classic = Inventory.GetItemValue(item),
            Worn = isWorn,
            Wearable = owner.Inventory.CanEquipItem(item) && item.CanBeEquipped(),
            Plain = (item.Enchantments == null || item.Enchantments.Count == 0) && item.TransmogrificationId == null
                && (stack == null || (string.IsNullOrEmpty(stack.Sockets) && string.IsNullOrEmpty(stack.Enchantment))),
            Name = item.Item.Name,
        };
    }

    // ---------------------------------------------------------------- in words, for chat

    /// <summary>What is worn, in a few words: "Elder Atlarus plate", "Nightfall (rogue)", "Windrunner and Warden (ranger)".</summary>
    public string DescribeWorn()
    {
        var items = new List<Item>();
        if (owner && owner.Inventory)
        {
            foreach (var item in owner.Inventory.GetEquippedItems().ToArray())
            {
                if (item != null && item.Item != null) items.Add(item.Item);
            }
        }
        return Describe(items);
    }

    /// <summary>A kept outfit's gear, in a few words; null when the skill has none.</summary>
    public string Describe(Skill skill)
    {
        var outfit = state.Find(skill);
        if (outfit == null || !owner || !owner.GameManager) return null;
        var items = new List<Item>();
        foreach (var slot in outfit.Slots)
        {
            var item = owner.GameManager.Items.Get(slot.ItemId);
            if (item != null) items.Add(item);
        }
        return Describe(items);
    }

    /// <summary>
    ///     Gear in a few words: the armor's materials, the one with the most pieces first (two names at most), and the
    ///     class gear line or "plate" after them. With no armor at all, the first other piece by name.
    /// </summary>
    public static string Describe(List<Item> items)
    {
        var materials = new List<ItemMaterial>();
        var counts = new List<int>();
        Item other = null;
        foreach (var item in items)
        {
            var slot = OutfitRules.SlotOf(item.Type);
            var look = !ItemLines.IsClassGear(item.Material) && (item.Type == ItemType.Hat || item.Type == ItemType.Mask);
            if (!OutfitRules.IsArmorSlot(slot) || item.Material == ItemMaterial.None || look)
            {
                if (other == null || (slot == RavenNest.Models.EquipmentSlot.MeleeWeapon && OutfitRules.SlotOf(other.Type) != RavenNest.Models.EquipmentSlot.MeleeWeapon)) other = item;
                continue;
            }
            var index = materials.IndexOf(item.Material);
            if (index < 0)
            {
                materials.Add(item.Material);
                counts.Add(1);
            }
            else counts[index]++;
        }

        if (materials.Count == 0) return other != null ? other.Name : Localization.OUTFIT_NO_GEAR;

        // the material with the most pieces, then the higher one
        var first = 0;
        for (var i = 1; i < materials.Count; ++i)
        {
            if (counts[i] > counts[first] || (counts[i] == counts[first] && (int)materials[i] > (int)materials[first])) first = i;
        }
        var second = -1;
        var line = ItemLines.Of(materials[first]);
        for (var i = 0; i < materials.Count; ++i)
        {
            if (i == first || ItemLines.Of(materials[i]) != line) continue;
            if (second < 0 || counts[i] > counts[second] || (counts[i] == counts[second] && (int)materials[i] > (int)materials[second])) second = i;
        }

        var names = Utility.AddSpacesToSentence(materials[first].ToString());
        if (second >= 0) names += " " + Localization.OUTFIT_AND + " " + Utility.AddSpacesToSentence(materials[second].ToString());
        return line == ItemLine.Plate ? names + " " + GearOutfit.NameOf(line) : names + " (" + GearOutfit.NameOf(line) + ")";
    }

    private static string Join(List<string> names)
    {
        if (names.Count == 1) return names[0];
        if (names.Count > 3) names = names.GetRange(0, 3);
        return string.Join(", ", names.GetRange(0, names.Count - 1)) + " " + Localization.OUTFIT_AND + " " + names[names.Count - 1];
    }
}
