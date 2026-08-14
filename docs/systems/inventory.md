# Inventory & Equipment System

**Location:** `Assets/Scripts/Game/Inventory/`

## Overview

Players have an inventory (backpack) for storing items and equipment slots for wearing gear. The system handles item management, equipping, trading, and stat bonuses.

---

## Inventory Class

**Location:** `Assets/Scripts/Game/Inventory/Inventory.cs`

```csharp
public class Inventory : MonoBehaviour
{
    private List<GameInventoryItem> backpack;    // Stored items
    private List<GameInventoryItem> equipped;    // Worn items
    
    public IReadOnlyList<GameInventoryItem> Equipped { get; }
}
```

### Core Methods

```csharp
// Add items
public GameInventoryItem AddToBackpack(Guid inventoryId, Item item, long amount);

// Remove items
public void Remove(GameInventoryItem item, double amount, bool removeEquipped = false);
public void RemoveByItemId(Guid itemId, long amount);
public void RemoveByInventoryId(Guid inventoryItemId, long amount);

// Check contents
public bool Contains(Guid itemId, int amount);
public GameInventoryItem FromBackpack(Guid itemId);
```

### Equipment Methods

```csharp
public void EquipBestItems();              // Auto-equip best gear
public void UnequipArmor();                // Remove all armor
public void UnequipAll();                  // Remove everything
public bool IsEquipped(GameInventoryItem item);
public void EquipAll();                    // Re-equip all equipped items
```

---

## GameInventoryItem

Represents an item instance in a player's inventory:

```csharp
public class GameInventoryItem
{
    public Item Item { get; }              // Item definition
    public Item SkinItem { get; set; }     // Cosmetic override
    public InventoryItem InventoryItem { get; } // Server data
    public PlayerController Player { get; }
    
    public Guid InstanceId { get; }        // Unique instance ID
    public Guid ItemId { get; }            // Item type ID
    public string Name { get; }
    public long Amount { get; set; }       // Stack size
    
    public ItemCategory Category { get; }
    public ItemType Type { get; }
    
    // Enchantments
    public IReadOnlyList<ItemEnchantment> Enchantments { get; }
    public Guid? TransmogrificationId { get; }
    
    // Requirements
    public int RequiredDefenseLevel { get; }
    public int RequiredAttackLevel { get; }
    public int RequiredSlayerLevel { get; }
    public int RequiredMagicLevel { get; }
    public int RequiredRangedLevel { get; }
    
    public bool Soulbound { get; }
    
    public ItemController Controller { get; } // Visual representation
}
```

### Equipment Check

```csharp
public bool CanBeEquipped()
{
    if (!IsEquippableType) return false;
    
    return Player.Stats.Defense.Level >= RequiredDefenseLevel
        && Player.Stats.Attack.Level >= RequiredAttackLevel
        && Player.Stats.Slayer.Level >= RequiredSlayerLevel
        && Player.Stats.Magic.Level >= RequiredMagicLevel
        && Player.Stats.Ranged.Level >= RequiredRangedLevel;
}

public bool IsEquippableType => 
    Category == ItemCategory.Weapon ||
    Category == ItemCategory.Armor ||
    Category == ItemCategory.Cosmetic ||
    Category == ItemCategory.Pet ||
    Category == ItemCategory.Skin ||
    Category == ItemCategory.Ring ||
    Category == ItemCategory.Amulet;
```

---

## Item Categories

```csharp
public enum ItemCategory
{
    Weapon,
    Armor,
    Cosmetic,
    Pet,
    Skin,
    Ring,
    Amulet,
    Resource,
    Food,
    Scroll,
    LootBox,
    QuestItem
}
```

---

## Item Types

```csharp
public enum ItemType
{
    // Weapons
    OneHandedSword,
    TwoHandedSword,
    OneHandedAxe,
    TwoHandedAxe,
    Bow,
    Staff,
    Wand,
    
    // Armor
    Helmet,
    Chest,
    Leggings,
    Gloves,
    Boots,
    Shield,
    
    // Tools (non-combat)
    Woodcutting,
    Mining,
    Fishing,
    Farming,
    Cooking,
    Crafting,
    Alchemy,
    
    // Accessories
    Ring,
    Amulet,
    Pet
}
```

---

## PlayerEquipment

**Location:** `Assets/Scripts/Game/Player/PlayerEquipment.cs`

Manages equipped item visuals and stats:

```csharp
public class PlayerEquipment : MonoBehaviour
{
    public IReadOnlyList<GameInventoryItem> EquippedItems { get; }
    
    public void UpdateAppearance();
    public void HideEquipments(bool hide);
    public void RefreshWeapon();
}
```

---

## Equipment Stats

**Location:** `Assets/Scripts/Game/Combat/EquipmentStats.cs`

Total bonuses from all equipped items:

```csharp
public class EquipmentStats
{
    public int Armor;              // Damage reduction
    public int WeaponPower;        // Melee damage
    public int WeaponAim;          // Melee accuracy
    public int MagicPower;         // Magic damage
    public int MagicAim;           // Magic accuracy
    public int RangedPower;        // Ranged damage
    public int RangedAim;          // Ranged accuracy
    public int HealingPower;       // Healing bonus
}
```

---

## ItemManager

**Location:** `Assets/Scripts/Game/ItemManager.cs` (referenced in GameManager)

Central item database:

```csharp
public class ItemManager : MonoBehaviour
{
    public Item Get(Guid itemId);
    public Item GetByName(string name);
    public bool Loaded { get; }
}
```

---

## ItemController

Visual representation of items in the world:

```csharp
public class ItemController : MonoBehaviour
{
    public Item Item { get; set; }
    public Item Skin { get; set; }
    
    public void UpdateAppearance();
    public void CleanupModel();
}
```

---

## Item Enchantments

Items can have magical enchantments:

```csharp
public class ItemEnchantment
{
    public EnchantmentType Type;
    public float Value;
    public float Duration;
}

// Get enchantments from item
public static IReadOnlyList<ItemEnchantment> GetItemEnchantments(string enchantmentData);
```

---

## Trading

### Market (Player-to-Player)

```csharp
// Buy from market
Connection.Register<BuyItemFromMarket>("buy_item");

// Sell on market
Connection.Register<PutItemOnMarket>("sell_item");
```

### Vendor (NPC)

```csharp
// Sell to NPC vendor
Connection.Register<SellItemToVendorVendor>("vendor_item");

// Use vendor
Connection.Register<UseVendor>("vendor");
```

### Gifting

```csharp
// Gift to another player
Connection.Register<GiftItem>("gift_item");
Connection.Register<SendItem>("send_item");
```

---

## Crafting Integration

Items are created through crafting:

```csharp
// CraftingManager handles recipes
public class CraftingManager : MonoBehaviour
{
    public CraftingRecipe GetRecipe(Guid itemId);
    public bool CanCraft(PlayerController player, CraftingRecipe recipe);
    public void Craft(PlayerController player, CraftingRecipe recipe);
}
```

---

## Loot System

**Location:** `Assets/Scripts/Game/Player/PlayerLootManager.cs`

Tracks item drops for players:

```csharp
public class PlayerLootManager
{
    public void RecordLoot(Item item, long amount, int dungeonIndex, int raidIndex);
}
```
