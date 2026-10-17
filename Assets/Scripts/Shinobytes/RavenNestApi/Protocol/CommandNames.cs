// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
namespace RavenNest.Protocol
{
    /// <summary>
    /// The commands the server answers on the v2 connection, with their arguments in order and what they return.
    /// They mirror the game's REST calls of the same purpose, with the same argument and result models, so moving a
    /// call from REST to v2 changes how it travels and nothing else. A name is never reused for something else.
    /// </summary>
    public static class Commands
    {
        // Players joining and leaving

        /// <summary>(PlayerJoinData) -> PlayerJoinResult</summary>
        public const string PlayerJoin = "players.join";

        /// <summary>(PlayerRestoreData) -> PlayerRestoreResult</summary>
        public const string PlayerRestore = "players.restore";

        /// <summary>(Guid characterId) -> bool</summary>
        public const string PlayerRemove = "players.remove";

        /// <summary>(Guid characterId, string reason) -> bool</summary>
        public const string PlayerRemoveFailed = "players.remove-failed";

        // Items

        /// <summary>(Guid characterId, Guid itemId) -> AddItemResult</summary>
        public const string AddItem = "items.add";

        /// <summary>(Guid characterId, InventoryItem item) -> AddItemInstanceResult</summary>
        public const string AddItemInstance = "items.add-instance";

        /// <summary>(Guid characterId, Guid recipeId, long amount) -> ItemProductionResult</summary>
        public const string Produce = "items.produce";

        /// <summary>(Guid characterId, Guid itemId, int amount) -> CraftItemResult</summary>
        public const string Craft = "items.craft";

        /// <summary>(Guid characterId, Guid itemId) -> RedeemItemResult</summary>
        public const string Redeem = "items.redeem";

        /// <summary>(Guid characterId, Guid inventoryItemId, string argument or null) -> ItemUseResult</summary>
        public const string UseItem = "items.use";

        /// <summary>(Guid characterId, Guid inventoryItemId, long amount) -> long (coins received)</summary>
        public const string Vendor = "items.vendor";

        /// <summary>(Guid characterId, Guid receiverUserId, Guid inventoryItemId, long amount) -> GiftItemResult</summary>
        public const string Gift = "items.gift";

        /// <summary>(Guid characterId, string receiverAlias, Guid inventoryItemId, long amount) -> GiftItemResult</summary>
        public const string SendItem = "items.send";

        /// <summary>(Guid senderCharacterId, Guid receiverCharacterId, long amount) -> long</summary>
        public const string SendCoins = "coins.send";

        /// <summary>(Guid characterId, int amount) -> bool</summary>
        public const string AddTokens = "tokens.add";

        // Equipment

        /// <summary>(Guid characterId, Guid inventoryItemId) -> bool</summary>
        public const string Equip = "equipment.equip";

        /// <summary>(Guid characterId, Guid itemId) -> bool</summary>
        public const string EquipItem = "equipment.equip-item";

        /// <summary>(Guid characterId) -> bool</summary>
        public const string EquipBest = "equipment.equip-best";

        /// <summary>(Guid characterId, Guid inventoryItemId) -> bool</summary>
        public const string Unequip = "equipment.unequip";

        /// <summary>(Guid characterId) -> bool</summary>
        public const string UnequipAll = "equipment.unequip-all";

        /// <summary>(Guid characterId) -> bool</summary>
        public const string ToggleHelmet = "equipment.toggle-helmet";

        // Enchanting

        /// <summary>(Guid characterId, Guid inventoryItemId) -> ItemEnchantmentResult</summary>
        public const string Enchant = "enchanting.enchant";

        /// <summary>(Guid characterId, Guid inventoryItemId) -> ItemEnchantmentResult</summary>
        public const string Disenchant = "enchanting.disenchant";

        /// <summary>(Guid characterId) -> EnchantmentCooldownResult</summary>
        public const string EnchantmentCooldown = "enchanting.cooldown";

        /// <summary>(Guid characterId) -> ClearEnchantmentCooldownResult</summary>
        public const string ClearEnchantmentCooldown = "enchanting.clear-cooldown";

        // Raids and dungeons

        /// <summary>(Guid[] characterIds) -> bool[]</summary>
        public const string AutoJoinRaid = "autojoin.raid";

        /// <summary>(Guid[] characterIds) -> bool[]</summary>
        public const string AutoJoinDungeon = "autojoin.dungeon";

        /// <summary>() -> int</summary>
        public const string AutoJoinRaidCost = "autojoin.raid-cost";

        // Other

        /// <summary>(Guid characterId, string skill) -> int (rank)</summary>
        public const string Highscore = "highscore.rank";

        /// <summary>(LoyaltyUpdate) -> bool</summary>
        public const string LoyaltyUpdate = "loyalty.update";
    }
}
