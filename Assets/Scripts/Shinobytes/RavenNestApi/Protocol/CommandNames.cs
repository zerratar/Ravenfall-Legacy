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

        /// <summary>(Guid characterId, Guid listingId, long amount) -> StreamerShopBuyResult: buys from a tavern shelf.</summary>
        public const string ShopBuy = "shop.buy";

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

        // Gems (docs/gems-design.md in the game repo); kinds and tiers are RavenNest.Combat's GemKind and GemTier

        /// <summary>(Guid characterId, int kind, int amount) -> GemResult: raw gems enchanted by the clan</summary>
        public const string GemEnchant = "gems.enchant";

        /// <summary>(Guid characterId, int kind, int tier, int times) -> GemResult: three of a tier into one of the next</summary>
        public const string GemCombine = "gems.combine";

        /// <summary>(Guid characterId, Guid inventoryItemId, int kind, int tier (-1 the best owned), int socket (0 the first free)) -> GemResult</summary>
        public const string GemSocket = "gems.socket";

        /// <summary>(Guid characterId, Guid inventoryItemId, int socket (0 the last filled)) -> GemResult</summary>
        public const string GemUnsocket = "gems.unsocket";

        // Battle pets (docs/battle-pets-design.md in the game repo); species are RavenNest.Combat.PetSpecies ids

        /// <summary>(Guid characterId) -> PetResult with Pets: the stable</summary>
        public const string PetList = "pets.list";

        /// <summary>(Guid characterId, string species) -> PetResult: a creature tamed (the game rolled the chance)</summary>
        public const string PetTame = "pets.tame";

        /// <summary>(Guid characterId, Guid itemId) -> PetResult: a pet item the character has, woken to fight</summary>
        public const string PetWake = "pets.wake";

        /// <summary>(Guid characterId, Guid petId (empty: put the pet away)) -> PetResult</summary>
        public const string PetUse = "pets.use";

        /// <summary>(Guid characterId, Guid petId, string name) -> PetResult</summary>
        public const string PetRename = "pets.rename";

        /// <summary>(Guid characterId, Guid petId, int slot (0 based), string skill (empty clears)) -> PetResult</summary>
        public const string PetSlot = "pets.slot";

        /// <summary>(Guid characterId, Guid petId) -> PetResult</summary>
        public const string PetEvolve = "pets.evolve";

        /// <summary>(Guid characterId, Guid petId) -> PetResult: let go for good</summary>
        public const string PetRelease = "pets.release";

        /// <summary>(Guid characterId, Guid petId, double experience) -> PetResult: what the pet earned while out</summary>
        public const string PetExperience = "pets.exp";

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

        // Inventory

        /// <summary>
        /// (Guid characterId) -> nothing; the result's Inventory holds the character's whole inventory (Full), at the
        /// revision the server has reached. For a game whose copy no longer adds up to the server's checksum.
        /// </summary>
        public const string InventoryGet = "inventory.get";
    }
}
