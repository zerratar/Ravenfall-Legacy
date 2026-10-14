using static MessageUtilities;

public static class Localization
{
    public static readonly string MSG_PATREON_ONLY = Meta("system", "fail") + "This command is for Patreon supporters only. Consider supporting the game on Patreon to gain access to this command.";
    public static readonly string PETRACE_NO_PET = Meta("minigame", "pet race", "fail") + "You need to equip a pet to play.";
    public static readonly string PETRACE_ALREADY_PLAYING = Meta("minigame", "pet race", "fail") + "You're already playing.";
    public static readonly string PETRACE_START_COMMAND = "race"; // Meta("minigame", "pet race") + 
    public static readonly string EQUIP_SHIELD_AND_TWOHANDED = Meta("equipment", "equip", "fail") + "You cannot equip a shield while having a 2-handed weapon equipped.";
    public static readonly string CANT_TRAIN_HERE = Meta("training", "fail") + "You cannot train {type} here.";
    public static readonly string NOT_HIGH_ENOUGH_SKILL = Meta("training", "fail") + "You need to have at least level {reqSkill} {type} to train this skill on this island.";
    public static readonly string NOT_HIGH_ENOUGH_COMBAT = Meta("training", "fail") + "You are not high enough combat level to train {skillName} here. Your combat level needs to be at least {reqCombat}.";
    public static readonly string NOT_HIGH_ENOUGH_SKILL_OR_COMBAT = Meta("training", "fail") + "You are not high enough level to train {skillName} here. Your skill or combat level needs to be at least {reqCombat}.";
    public static readonly string NOT_HIGH_ENOUGH_SKILL_AND_COMBAT = Meta("training", "fail") + "You need to be at least combat level {reqCombat} and skill level {reqSkill} to train this skill on this island.";

    public static readonly string GAME_NOT_LOADED = Meta("system", "fail") + "Game has not finished loading yet, try again soon!";
    public static readonly string GAME_NOT_READY = Meta("system", "fail") + "Game is not ready yet!";

    public static readonly string MSG_COMMAND_COOLDOWN = Meta("system", "fail") + "You must wait another {cooldown} secs to use that command.";
    public static readonly string MSG_NOT_PLAYING = Meta("system", "fail") + "You are not currently playing. Use !join to start playing!";
    public static readonly string MSG_EQUIP_STATS = Meta("info", "equipment") + "{armorPower} Armor, {weaponPower} Melee Weapon Power, {weaponAim} Melee Weapon Aim, {magicPower} Magic/Healing Power, {magicAim} Magic Aim, {rangedPower} Ranged Weapon Power, {rangedAim} Ranged Weapon Aim";
    public static readonly string MSG_EQUIP_STATS_FORMAT = "{armorPower} Armor, {weaponPower} Melee Weapon Power, {weaponAim} Melee Weapon Aim, {magicPower} Magic/Healing Power, {magicAim} Magic Aim, {rangedPower} Ranged Weapon Power, {rangedAim} Ranged Weapon Aim";

    public static readonly string MSG_STATS = Meta("info", "stats") + "Combat level {combatLevel}, {skills} -- TOTAL {total} --";
    public static readonly string MSG_SKILL = Meta("info", "skill") + "{skill}, {currenExp} / {requiredExp} EXP.";
    public static readonly string MSG_PLAYER_RESOURCES = Meta("info", "resources") + "Wood {wood}, Ore {ore}, Fish {fish}, Wheat {wheat}, Coin {coins}";
    public static readonly string MSG_PLAYER_COINS = Meta("info", "resources") + "You have {coins} ({coinsShort}) coins";
    public static readonly string MSG_TOWN_RESOURCES = Meta("info", "resources") + "Wood {wood}, Ore {ore}, Fish {fish}, Wheat {wheat}, Coin {coins}";
    public static readonly string MSG_STREAMER_TOKENS = Meta("info", "tokens") + "You have {tokenCount} {tokenName}(s) you can use.";
    public static readonly string MSG_PLAYER_INSPECT_URL = Meta("system", "inspect") + "https://www.ravenfall.stream/inspect/{characterId}";
    public static readonly string MSG_PLAYERS_ONLINE = Meta("system") + "There are currently {playerCount} players in the game.";
    public static readonly string MSG_MULTIPLIER_ENDS = Meta("system", "exp multiplier") + "The current exp multiplier (x{multiplier}) will end in {minutes}{seconds}.";
    public static readonly string MSG_MULTIPLIER_ENDED = Meta("system", "exp multiplier") + "The exp multiplier has expired.";
    public static readonly string MSG_MULTIPLIER_LIMIT = Meta("system", "exp multiplier") + "The exp multiplier limit has been set to {expMultiplier}!";
    public static readonly string MSG_MULTIPLIER_INCREASE = Meta("system", "exp multiplier") + "You have increased the exp multiplier with {added}. It is now at {multiplier}!";
    public static readonly string MSG_MULTIPLIER_RESET = Meta("system", "exp multiplier") + "You have reset the exp multiplier! NotLikeThis";
    public static readonly string MSG_MULTIPLIER_SET = Meta("system", "exp multiplier") + "The exp multiplier has been set to {amount}!";
    public static readonly string MSG_SUB_CREW = Meta("transaction", "subscription") + "Welcome to the {streamerDisplayName} crew! You will now gain {tierMultiplier}x more exp on this stream!";
    public static readonly string MSG_BIT_CHEER = Meta("transaction", "bits") + "Cheer {bitsLeft} more bits for a {tokenName}";
    public static readonly string MSG_BIT_CHEER_INCREASE = Meta("system", "exp multiplier") + "You have increased the multiplier timer with {multiAdded} minutes with your {bits} cheer!! We only need {bitsLeft} more bits for increasing the timer! <3";
    public static readonly string MSG_BIT_CHEER_LEFT = Meta("system", "exp multiplier") + "We only need {bitsLeft} more bits for increasing the multiplier timer!";
    public static readonly string MSG_SUB_TOKEN_REWARD = Meta("system", "reward") + "You were rewarded {amount} {tokenName}(s)!";
    public static readonly string MSG_RAID_START_CODE = Meta("event", "raid", "raid boss") + "A level {raidLevel} raid boss has appeared! Help fight him by typing '!raid code', find the code on stream.";
    public static readonly string MSG_RAID_START = Meta("event", "raid", "raid boss") + "A level {raidLevel} raid boss has appeared! Help fight him by typing !raid";
    public static readonly string MSG_RAID_START_ERROR = Meta("system", "raid", "raid start", "fail") + "Raid cannot be started right now.";
    public static readonly string MSG_DUEL_REQ = Meta("duel", "request") + "A duel request received from {opponent}, reply with !duel accept or !duel decline";
    public static readonly string MSG_DUEL_REQ_TIMEOUT = Meta("duel", "request", "timeout") + "The duel request from {requester} has timed out and automatically declined.";
    public static readonly string MSG_DUEL_REQ_DECLINE = Meta("duel", "request", "declined") + "Duel with {requester} was declined.";
    public static readonly string MSG_DUEL_REQ_ACCEPT = Meta("duel", "request", "accepted") + "You have accepted the duel against {requester}. May the best fighter win!";
    public static readonly string MSG_DUEL_WON = Meta("duel", "winner") + "You won the duel against {opponent}!";
    public static readonly string MSG_DUEL_ACCEPT_FERRY = Meta("duel", "accept", "fail") + "You cannot accept a duel while on the ferry.";
    public static readonly string MSG_DUEL_ACCEPT_IN_DUEL = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are already in a duel.";
    public static readonly string MSG_DUEL_ACCEPT_IN_ARENA = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are participating in the Arena.";
    public static readonly string MSG_DUEL_ACCEPT_IN_RAID = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are participating in a Raid.";
    public static readonly string MSG_DUEL_ACCEPT_NO_REQ = Meta("duel", "accept", "fail") + "You do not have any pending duel requests to accept.";
    public static readonly string MSG_FERRY_ALREADY_ON = Meta("travel", "sail", "fail") + "You're already on the ferry.";
    public static readonly string MSG_FERRY_ALREADY_WAITING = Meta("travel", "sail", "fail") + "You're already waiting for the ferry.";
    public static readonly string MSG_FERRY_TRAIN_SAIL = Meta("travel", "sail", "info") + "You've decided to sail the seas forever. Use !disembark or !travel <new destination> to leave the ferry. You will gain sailing exp for as long as you're on the ferry.";
    public static readonly string MSG_FERRY_ARRIVED = Meta("travel", "sail", "info") + "You have arrived at your destination, {islandName}!";
    public static readonly string MSG_DISEMBARK_ALREADY = Meta("travel", "sail", "fail") + "You're already disembarking the ferry.";
    public static readonly string MSG_DISEMBARK_FAIL = Meta("travel", "sail", "fail") + "You're not on the ferry.";
    public static readonly string MSG_ISLAND_ON_FERRY_DEST = Meta("info", "location") + "You're currently on the ferry, going to disembark at '{destination}'.";
    public static readonly string MSG_ISLAND_ON_FERRY = Meta("info", "location") + "You're currently on the ferry.";
    public static readonly string MSG_ISLAND = Meta("info", "location") + "You're on the island called {islandName}";
    public static readonly string MSG_RESTED = Meta("info", "rest") + "You're rested and gain {expBoost}x more exp for {restedTime}";
    public static readonly string MSG_RESTING = Meta("info", "rest") + "You're currently resting and will have {expBoost}x more exp for {restedTime}";


    public static readonly string MSG_ONSEN_WRONG_ISLAND = Meta("rest", "fail") + "Resting areas are available on Kyo, Heim, Atria and Eldara only. You're currently on {islandName}.";
    public static readonly string MSG_NOT_RESTED = Meta("info", "rest") + "You're not rested. Sail to Kyo, Heim, Atria or Eldara and use !rest or !onsen to rest.";

    public static readonly string[] MSG_ONSEN_FERRY = new string[]
    {
        Meta("rest", "fail") + "Trying to snooze on the ferry? Seasickness isn't a great lullaby! Head over to Kyo, Heim, Atria, or Eldara for a proper nap.",
        Meta("rest", "fail") + "Dreaming of a nap on the high seas? More like a splash in the face! Best wait until you're in Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "Resting on a ferry? Hope you brought a waterproof blanket! For dry dreams, try Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "You want to rest NOW? Maybe you enjoy the lull of waves... and the sudden splash! For a less aquatic nap, go to Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "Napping on the ferry? Bold move! But for a sleep without seaspray surprises, Kyo, Heim, Atria, or Eldara is the place to be."
    };

    public static readonly string MSG_ONSEN_FULL = Meta("rest", "fail") + "Onsen seem to be full. You will have to try again later.";
    public static readonly string MSG_ONSEN_ENTRY = Meta("rest", "fail") + "You are now resting peacefully at the onsen. You will gain 2x more exp for two seconds for every second you stay here. Use !onsen leave when you're done.";
    public static readonly string MSG_ONSEN_LEFT = Meta("rest", "fail") + "You have left the onsen.";
    public static readonly string MSG_ARENA_UNAVAILABLE_WAR = Meta("arena", "fail") + "Arena is unavailable during wars. Please wait for it to be over.";
    public static readonly string MSG_ARENA_WRONG_ISLAND = Meta("arena", "fail") + "There is no arena on the island you're on.";
    public static readonly string MSG_ARENA_FERRY = Meta("arena", "fail") + "You cannot join the arena while on the ferry.";
    public static readonly string MSG_ARENA_ALREADY_JOINED = Meta("arena", "fail") + "You have already joined the arena.";
    public static readonly string MSG_ARENA_ALREADY_STARTED = Meta("arena", "fail") + "You cannot join as the arena has already started.";
    public static readonly string MSG_ARENA_JOIN_ERROR = Meta("arena", "fail") + "You cannot join the arena at this time.";
    public static readonly string MSG_ARENA_JOIN = Meta("arena", "info") + "You have joined the arena. Good luck!";

    public static readonly string MSG_BUY_ITEM_NOT_FOUND = Meta("marketplace", "buy", "fail") + "Could not find an item matching the query '{query}'";
    public static readonly string MSG_BUY_ITEM_NOT_FOUND_SUGGEST = Meta("marketplace", "buy", "fail") + "Could not find an item matching the query '{query}', did you mean: {suggestion}?";
    public static readonly string MSG_BUY_ITEM_NOT_IN_MARKET = Meta("marketplace", "buy", "fail") + "Could not find any {itemName} in the marketplace.";
    public static readonly string MSG_BUY_ITEM_INSUFFICIENT_COIN = Meta("marketplace", "buy", "fail") + "You do not have enough coins to buy the {itemName}.";
    public static readonly string MSG_BUY_ITEM_MARKETPLACE_ERROR = Meta("marketplace", "buy", "fail") + "Error accessing marketplace right now.";
    public static readonly string MSG_BUY_ITEM_ERROR = Meta("marketplace", "buy", "fail") + "Error buying {itemName}. Server returned an error. :( Try !leave and then !join to see if it was successful or not.";
    public static readonly string MSG_SELL_ITEM_ERROR = Meta("marketplace", "sell", "fail") + "Error selling {itemName}. Server returned an error. :( Try !leave and then !join to see if it was successful or not.";
    public static readonly string MSG_VALUE_ITEM_ERROR = Meta("marketplace", "value", "fail") + "Error valuating {itemName}. Server returned an error.";


    public static readonly string MSG_BUY_ITEM_TOO_LOW = Meta("marketplace", "buy", "fail") + "Unable to buy any {itemName}, the cheapest asking price is {cheapestPrice}.";

    public static readonly string MSG_GATHERING_SUGGEST = Meta("info", "gathering") + "Could not find a resource type matching the query '{query}', did you mean: {suggestion}?";
    public static readonly string MSG_FISH_SUGGEST = Meta("info", "fishing") + "Could not find a fish type matching the query '{query}', did you mean: {suggestion}?";
    public static readonly string MSG_CHOP_SUGGEST = Meta("info", "woodcutting") + "Could not find a wood type matching the query '{query}', did you mean: {suggestion}?";
    public static readonly string MSG_MINE_SUGGEST = Meta("info", "mining") + "Could not find a ore type matching the query '{query}', did you mean: {suggestion}?";
    public static readonly string MSG_FARM_SUGGEST = Meta("info", "farming") + "Could not find a farmable type matching the query '{query}', did you mean: {suggestion}?";

    public static readonly string MSG_MINE_LEVEL_REQUIREMENT = Meta("info", "mining") + "You need to be level {level} to mine {oreName}.";
    public static readonly string MSG_FARM_LEVEL_REQUIREMENT = Meta("info", "farming") + "You need to be level {level} to farm {farmableName}.";
    public static readonly string MSG_CHOP_LEVEL_REQUIREMENT = Meta("info", "woodcutting") + "You need to be level {level} to chop {woodName}.";
    public static readonly string MSG_FISH_LEVEL_REQUIREMENT = Meta("info", "fishing") + "You need to be level {level} to fish {fishName}.";
    public static readonly string MSG_GATHER_LEVEL_REQUIREMENT = Meta("info", "gathering") + "You need to be level {level} to gather {resourceName}.";

    public static readonly string MSG_IN_DUEL = Meta("info", "location") + "You are currently fighting in a duel.";
    public static readonly string MSG_IN_RAID = Meta("info", "location") + "You are currently fighting in the raid.";
    public static readonly string MSG_RAID_STARTED = Meta("raid", "info") + "There is an active raid.";
    public static readonly string MSG_DUNGEON_STARTED = Meta("dungeon", "info") + "There is an active dungeon.";
    public static readonly string MSG_IN_DUNGEON = Meta("info", "location") + "You're currently in the dungeon.";
    public static readonly string MSG_STREAMRAID_NO_PLAYERS = Meta("system", "streamer raid") + "{raiderName} raided but without any players. Kappa";
    public static readonly string MSG_STREAMRAID_WAR_NO_PLAYERS = Meta("system", "streamer raid") + "{raiderName} raided with intent of war but we don't have any players. FeelsBadMan";
    public static readonly string MSG_JOIN_WELCOME = Meta("system", "join") + "Welcome to Ravenfall!";
    public static readonly string MSG_JOIN_WELCOME_FIRST_TIME = Meta("system", "join") + "Welcome to Ravenfall, {playerName}! You can get started by using the !train command. Why don't you try using !train all to train your Attack, Defense, and Strength all at once?";
    public static readonly string MSG_JOIN_FAILED = Meta("system", "join", "fail") + "Unable to join the game right now, server may be down and not responding.";
    public static readonly string MSG_JOIN_FAILED_ALREADY_PLAYING = Meta("system", "join", "fail") + "You're already playing!";
    public static readonly string MSG_REQ_RAID = Meta("system", "streamer raid", "fail") + "Requesting {raidType} on {streamer}s stream!";
    public static readonly string MSG_REQ_RAID_SOON = Meta("system", "streamer raid") + "The raid will start any moment now!";
    public static readonly string MSG_REQ_RAID_FAILED = Meta("system", "streamer raid", "fail") + "Request failed, streamer is no longer playing or has disabled raids. :(";
    public static readonly string MSG_CRAFT_FAILED = Meta("crafting", "fail") + "{category} {type} cannot be crafted right now.";
    public static readonly string MSG_CRAFT_FAILED_FERRY = Meta("crafting", "fail") + "You cannot craft while on the ferry";
    public static readonly string MSG_CRAFT_FAILED_STATION = Meta("crafting", "fail") + "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting";

    public static readonly string MSG_BREW_FAILED_STATION = Meta("alchemy", "fail") + "You have to be at the alchemy table to brew anything. Type !train alchemy before using the !brew command.";
    //public static readonly string MSG_BREW_FAILED_STATION = Meta("alchemy", "fail") + "You have to be at the alchemy table to brew anything. Type !train alchemy before using the !brew command.";

    public static readonly string MSG_CRAFT_CANCEL = Meta("crafting", "cancel") + "Your ongoing crafting has been cancelled.";
    public static readonly string MSG_BREW_CANCEL = Meta("alchemy", "cancel") + "Your ongoing brewing has been cancelled.";
    public static readonly string MSG_COOK_CANCEL = Meta("cooking", "cancel") + "Your ongoing cooking has been cancelled.";

    public static readonly string MSG_CRAFT_FAILED_LEVEL = Meta("crafting", "fail") + "You can't craft this item, it requires level {reqCraftingLevel} crafting.";
    public static readonly string MSG_CRAFT_FAILED_NOT_CRAFTABLE = Meta("crafting", "fail") + "{itemName} is not a craftable item.";

    public static readonly string MSG_CRAFT_FAILED_RES = Meta("crafting", "fail") + "Insufficient resources to craft {itemName}";
    public static readonly string MSG_CRAFT_EQUIPPED = Meta("crafting", "info") + "You crafted and equipped a {itemName}!";
    public static readonly string MSG_CRAFT_ITEM_NOT_FOUND = Meta("crafting", "fail") + "Could not find an item matching the query '{query}'";
    public static readonly string MSG_CRAFT_ITEM_NOT_FOUND_SUGGEST = Meta("crafting", "fail") + "Could not find an item matching the query '{query}', did you mean {suggestion}?";
    public static readonly string MSG_CRAFT_ITEM_NOT_FOUND_MEAN = Meta("crafting", "fail") + "Could not find an item matching the query '{query}', did you mean '{itemName}'?";
    public static readonly string MSG_CRAFT = Meta("crafting", "info") + "You crafted a {itemName}!";
    public static readonly string MSG_CRAFT_MANY = Meta("crafting", "info") + "You crafted x{amount} {itemName}!";


    public static readonly string MSG_GENERIC_NOT_SUITABLE_ITEM = "{itemName} is not a suitable item for this.";
    public static readonly string MSG_GENERIC_ITEM_NOT_FOUND = "Could not find an item matching the query '{query}'";

    public static readonly string MSG_GENERIC_FAILED_FERRY = Meta("ferry", "fail") + "You cannot do that while you are on the ferry";
    public static readonly string MSG_GENERIC_FAILED_DUNGEON = Meta("dungeon", "fail") + "You cannot do that while you are in a dungeon";
    public static readonly string MSG_GENERIC_FAILED_RAID = Meta("raid", "fail") + "You cannot do that while you are in a raid";
    public static readonly string MSG_GENERIC_FAILED_ARENA = Meta("arena", "fail") + "You cannot do that while you are in the arena";
    public static readonly string MSG_GENERIC_FAILED_DUEL = Meta("duel", "fail") + "You cannot do that while you are in a duel";
    public static readonly string MSG_GENERIC_FAILED_RESTING = Meta("resting", "fail") + "You cannot do that while you are resting";

    public static readonly string MSG_COOK_FAILED_FERRY = Meta("cooking", "fail") + "You cannot cook while on the ferry";
    public static readonly string MSG_BREW_FAILED_FERRY = Meta("alchemy", "fail") + "You cannot brew while on the ferry";


    public static readonly string MSG_GIFT = Meta("gift", "info") + "You gifted {giftCount}x {itemName} to {player}!";
    public static readonly string MSG_GIFT_PLAYER_NOT_FOUND = Meta("gift", "fail") + "Could not find an item or player matching the query '{query}'";
    public static readonly string MSG_GIFT_ITEM_NOT_FOUND = Meta("gift", "fail") + "Could not find a matching the query '{query}'";
    public static readonly string MSG_GIFT_ITEM_NOT_OWNED = Meta("gift", "fail") + "You do not have any {itemName} to gift.";
    public static readonly string MSG_GIFT_ERROR = Meta("gift", "fail") + "Error gifting {giftCount}x {itemName} to {player}. FeelsBadMan";

    public static readonly string MSG_SEND = Meta("send", "info") + "You sent {count}x {itemName} to {player}!";
    public static readonly string MSG_SEND_PLAYER_NOT_FOUND = Meta("send", "fail") + "Could not find an item or player matching the query '{query}'";
    public static readonly string MSG_SEND_ITEM_NOT_FOUND = Meta("send", "fail") + "Could not find a matching the query '{query}'";
    public static readonly string MSG_SEND_ITEM_NOT_OWNED = Meta("send", "fail") + "You do not have any {itemName} to gift.";
    public static readonly string MSG_SEND_ERROR = Meta("send", "fail") + "Error sending {count}x {itemName} to {player}. FeelsBadMan Either server is having hiccups or you don't have a character with that number or alias.";

    public static readonly string MSG_EQUIPPED = Meta("info", "equipment", "equip") + "You have equipped {itemName}.";
    public static readonly string MSG_EQUIPPED_ALL = Meta("info", "equipment", "equip") + "You have equipped all of your best items.";

    public static readonly string MSG_ITEM_COUNT_MISSING_ARGS = Meta("info", "item count", "fail") + "You must specify an item. Use !items (item name) or !count (item name)";
    public static readonly string MSG_ITEM_EXAMINE_MISSING_ARGS = Meta("info", "examine", "fail") + "You must specify an item. Use !examine (item name)";
    public static readonly string MSG_ITEM_USE_MISSING_ARGS = Meta("info", "use", "fail") + "You must specify an item. Use !use (item name)";

    public static readonly string MSG_ENCHANT_MISSING_ARGS = Meta("enchanting", "fail") + "You must specify an item to enchant. Use !enchant (item name)";
    public static readonly string MSG_ENCHANT_CLAN_SKILL = Meta("enchanting", "fail", "no clan") + "Enchanting is a clan skill. Join a clan to be able to use it!";
    public static readonly string MSG_ENCHANT_UNKNOWN_ERROR = Meta("enchanting", "fail") + "Enchanting failed. Unknown reason. Please try again later.";

    public static readonly string MSG_ENCHANT_COOLDOWN = Meta("enchanting", "fail", "cooldown") + "You have to wait {timeLeft} before you can try to enchant something again.";
    public static readonly string MSG_ENCHANT_FAILED = Meta("enchanting", "fail") + "You failed to enchant {itemName}. You may try again in {cooldown}";
    public static readonly string MSG_ENCHANT_NOT_AVAILABLE = Meta("enchanting", "fail", "no clan") + "You need to be part of a clan to use enchanting.";
    public static readonly string MSG_ENCHANT_NOT_ENCHANTABLE = Meta("enchanting", "fail", "not enchantable") + "{itemName} can't be enchanted. Only weapon, armor, amulet and rings can be enchanted.";
    public static readonly string MSG_ENCHANT_SUCCESS = Meta("enchanting", "info") + "You have successfully enchanted {oldItemName} into {newItemName}. You must now wait {cooldown} before you can enchant something again.";
    public static readonly string MSG_ENCHANT_REPLACE = Meta("enchanting", "info") + "You have successfully replaced the enchantment on {oldItemName}. It has now  become {newItemName}. You must now wait {cooldown} before you can enchant something again.";
    public static readonly string MSG_ENCHANT_WARN_REPLACE = Meta("enchanting", "info") + "{itemName} is already enchanted with {stats}. If you want to replace the enchantment, use !enchant replace {itemName}";
    public static readonly string MSG_ENCHANT_STATS = Meta("enchanting", "info") + "{enchantmentStats} was added to {enchantedItemName}";

    public static readonly string MSG_DISENCHANT_NO_ITEM = Meta("enchanting", "fail") + "It seem like you are trying to use '!disenchant' but you don't seem to have recently enchanted an item. Try using !disenchant (item name) instead.";
    public static readonly string MSG_DISENCHANT_NOT_ENCHANTED = Meta("enchanting", "fail") + "You cannot disenchant {itemName} as it is not enchanted.";
    public static readonly string MSG_DISENCHANT_UNKNOWN_ERROR = Meta("enchanting", "fail") + "Disenchanting {itemName} failed. Please try again later.";
    public static readonly string MSG_DISENCHANT_SUCCESS = Meta("enchanting", "info") + "You have successfully removed the enchantment from {oldItemName}.";
    public static readonly string MSG_ENCHANT_COST_NO_REQ = Meta("enchanting", "info") + "There are currently no requirements for enchanting {itemName}.";

    public static readonly string MSG_UNEQUIPPED = Meta("info", "equipment", "unequip") + "You have unequipped {itemName}.";
    public static readonly string MSG_UNEQUIPPED_ALL = Meta("info", "equipment", "unequip") + "You have unequipped all of your items.";
    public static readonly string MSG_SET_PET = Meta("info", "equipment", "pet") + "You have changed your active pet to {itemName}";
    public static readonly string MSG_ITEM_NOT_FOUND = Meta("info", "fail") + "Could not find an item matching the name: {query}";
    public static readonly string MSG_ITEM_NOT_FOUND_SUGGEST = Meta("info", "fail") + "Could not find an item matching the name: {query}, did you mean {suggestion}?";
    public static readonly string MSG_SET_PET_NOT_PET = Meta("info", "equipment", "pet", "fail") + "{itemName} is not a pet.";
    public static readonly string MSG_SET_PET_NOT_OWNED = Meta("info", "equipment", "pet", "fail") + "You do not have any {itemName}.";
    public static readonly string MSG_ITEM_NOT_EQUIPPED = Meta("info", "equipment", "unequip", "fail") + "You do not have {itemName} equipped.";



    // todo add more meta and review the ones added above.

    public static readonly string MSG_ITEM_NOT_OWNED = "You do not have any {itemName}.";
    public static readonly string MSG_GET_PET_NO_PET = "You do not have any pets equipped.";
    public static readonly string MSG_GET_PET = "You currently have {petName} equipped.";
    public static readonly string MSG_APPEARANCE_INVALID = "Invalid appearance data";
    public static readonly string MSG_TRAVEL_ALREADY_ON_ISLAND = "You cannot travel to the island you're already on.";
    public static readonly string MSG_TRAVEL_NO_SUCH_ISLAND = "No islands named '{islandName}'. You may travel to: '{islandList}'";
    public static readonly string MSG_TRAVEL_NO_SUCH_PLAYER = "No player named '{islandName}'.";
    public static readonly string MSG_TRAVEL_WAR = "You cannot travel when participating in a war. Please wait for it to be over.";
    public static readonly string MSG_TRAVEL_DUEL = "You cannot travel when duelling another player.";
    public static readonly string MSG_TRAVEL_ARENA = "You cannot travel when participating in the arena.";
    public static readonly string MSG_TRAVEL_DUNGEON = "You cannot travel when in the dungeon.";
    public static readonly string MSG_HIGHEST_SKILL_NO_PLAYERS = "Seems like no one is playing.";
    public static readonly string MSG_HIGHEST_SKILL = "{player} has the highest level {skillName} with level {level}.";
    public static readonly string MSG_HIGHEST_COMBAT = "{player} has the highest combat level with {combatLevel}.";
    public static readonly string MSG_HIGHEST_TOTAL = "{player} has the highest total level with {level}.";

    public static readonly string MSG_VILLAGE_BOOST = "Village is level {townHallLevel}, it needs {remainingExp} xp to level up. Active boosts {activeBoosts}";
    public static readonly string MSG_VILLAGE_BOOST_NO_BOOST = "Village is level {townHallLevel}, it needs {remainingExp} xp to level up. Without any active boost.";

    public static readonly string MSG_VILLAGE_UPDATED = "You've changed the village target to {targetType}, active boosts {activeBoosts}";
    public static readonly string MSG_HIGHSCORE_RANK = "You're at rank #{rank}";
    public static readonly string MSG_HIGHSCORE_MAIN_ONLY = "Only your main character is listed on the highscore right now.";
    public static readonly string MSG_HIGHSCORE_BAD_SKILL = "{skillName} is not a skill in the game. Did you misspell it?";
    public static readonly string MSG_MAX_MULTI = "Current Exp Multiplier is at {multiplier}.";
    public static readonly string MSG_MAX_MULTI_ALL = "Current Exp Multiplier is at {multiplier}. Max is {maxMultiplier}. Currently {cheerPot} bits.";
    public static readonly string MSG_MAX_MULTI_BITS = "Max exp multiplier is {maxMultiplier}. Currently {cheerPot} bits.";
    public static readonly string MSG_REDEEM = "{itemName} was redeemed for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public static readonly string MSG_REDEEM_EQUIP = "{itemName} was redeemed and equipped for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public static readonly string MSG_REDEEM_FAILED = "{itemName} could not be redeemed. Please try again.";
    public static readonly string MSG_REDEEM_INSUFFICIENT_TOKENS = "Not enough tokens to redeem this item. You have {tokenCount} but need {tokenCost}.";
    public static readonly string MSG_REDEEM_NOT_REDEEMABLE = "This item can not be redeemed yet.";
    public static readonly string MSG_REDEEM_ITEM_NOT_FOUND = "Could not find a redeemable item matching the query '{query}'";
    public static readonly string MSG_REDEEM_ITEM_NOT_FOUND_SUGGEST = "No such redeemable item, did you mean {query}?";

    public static readonly string MSG_SELL_TOO_MANY = Meta("marketplace", "sell", "fail") + "You cannot sell {itemAmount}x {itemName}. Max is {maxValue}.";
    public static readonly string MSG_SELL_ITEM_NOT_FOUND = Meta("marketplace", "sell", "fail") + "Could not find an item matching the name: {query}";
    public static readonly string MSG_SELL_ITEM_NOT_OWNED = Meta("marketplace", "sell", "fail") + "You do not have any {itemName} in your inventory.";
    public static readonly string MSG_SELL_MARKETPLACE_ERROR = Meta("marketplace", "fail") + "Error accessing marketplace right now.";
    public static readonly string MSG_ITEM_SOULBOUND = Meta("marketplace", "sell", "soulbound", "fail") + "{itemName} is soulbound and cannot be sold to the marketplace or gifted to another player.";
    public static readonly string MSG_ITEM_SELL_ENCHANTED = Meta("marketplace", "sell", "soulbound", "fail") + "{itemName} is enchanted and cannot be sold to the marketplace or gifted to another player.";

    public static readonly string MSG_SELL = Meta("marketplace", "sell", "info") + "{itemAmount}x {itemName} was put in the marketplace listing for {itemPrice} per item.";
    public static readonly string MSG_VALUE_ITEM = Meta("info", "item", "value") + "{itemName} can be sold for {vendorPrice} using !vendor";
    public static readonly string MSG_VALUE_ITEM_NOT_FOUND = Meta("info", "item", "value", "fail") + "Could not find an item matching the name: {query}";
    public static readonly string MSG_VENDOR_ITEM = Meta("vendor", "info", "item") + "You sold {vendorCount}x {itemName} to the vendor for {cost} coins!";
    public static readonly string MSG_VENDOR_ITEM_NOT_FOUND = Meta("vendor", "info", "item", "fail") + "Could not find an item matching the name: {query}";
    public static readonly string MSG_VENDOR_ITEM_NOT_OWNED = Meta("vendor", "info", "item", "fail") + "You do not own a {itemName}.";
    public static readonly string MSG_VENDOR_ITEM_FAILED = Meta("vendor", "info", "item", "fail") + "Error selling {vendorCount}x {itemName} to the vendor. FeelsBadMan";
    public static readonly string MSG_VENDOR_MISSING_ARGS = Meta("vendor", "info", "fail") + "To use the vendor, you must supply an action and item. You can use: !vendor sell itemName, !vendor buy itemName and !vendor value itemName";
    public static readonly string MSG_VENDOR_MISSING_ACTION = Meta("vendor", "info", "fail") + "Missing required vendor action, if you inteded to sell the item to the vendor, please use !vendor sell {query}";

    public static readonly string MSG_MARKET_MISSING_ARGS = Meta("marketplace", "info", "fail") + "To use the marketplace, you must supply an action and item. You can use: !market sell itemName, !market buy itemName and !market value itemName";
    public static readonly string MSG_MARKET_MISSING_ACTION = Meta("marketplace", "info", "fail") + "Missing required market action, if you inteded to put up an item for sale on the market, please use !market sell {query}";

    public static readonly string MSG_DUNGEON_START_FAILED_WAR = Meta("dungeon", "start", "fail", "war") + "Unable to start a dungeon during a war. Please wait for it to be over.";
    public static readonly string MSG_DUNGEON_START_FAILED_RAID = Meta("dungeon", "start", "fail", "raid") + "Unable to start a dungeon during a raid. Please wait for it to be over.";
    public static readonly string MSG_TICTACTOE_PLAY = Meta("minigame", "tic tac toe", "info") + "Use !ttt 1-9 to play!";
    public static readonly string MSG_TICTACTOE_STARTED = Meta("minigame", "tic tac toe", "info") + "A game of Tic Tac Toe has started! Use !tictactoe 1-9 to play!";
    public static readonly string MSG_TICTACTOE_JOINED = Meta("minigame", "tic tac toe", "info") + "You have joined team {teamName}!";
    public static readonly string MSG_TICTACTOE_REJECT_GAMEOVER = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because the game is over.";
    public static readonly string MSG_TICTACTOE_REJECT_TURN = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected as it is not your turn yet.";
    public static readonly string MSG_TICTACTOE_REJECT_NUMBER = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because the number was not part of the grid.";
    public static readonly string MSG_TICTACTOE_REJECT_PLAYED = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because someone else already played this number.";
    public static readonly string MSG_TICTACTOE_END_DRAW = Meta("minigame", "tic tac toe", "info", "result") + "The game ended in a draw. No one wins.";
    public static readonly string MSG_TICTACTOE_WIN = Meta("minigame", "tic tac toe", "info", "result") + "Team {winningTeam} wins!";
    public static readonly string MSG_TICTACTOE_WON_TOKEN = Meta("minigame", "tic tac toe", "info", "reward") + "You won a {tokenName}!";

    public static readonly string MSG_DIAPER_ON = "Diaper mode has been enabled, you have unequipped all armor.";
    public static readonly string MSG_DIAPER_OFF = "Diaper mode has been disabled, you have equipped your armor again.";
    public static readonly string MSG_TOGGLE_PET_NO_PET = "You have no more pets to cycle between.";
    public static readonly string MSG_TOGGLE_PET = "You have changed your active pet to {itemName}";

    public static readonly string MSG_TRAINING_RECOMMENDED_ISLAND = Meta("training", "info") + "You have not been gaining any experience for more than {mins} minutes. You're recommended to sail to {recommendedIslandName} to continue training. The broadcaster may now also use !sail {recommendedIslandName} {playerName} to make your character move.";

    public static readonly string MSG_TRAINING = "You're currently training {skill}.";
    public static readonly string MSG_TRAINING_NOTHING = "You're not training anything. Use !train <skill name> to start training!";
    public static readonly string MSG_TRAINING_SAILING = "You're currently sailing the seas. Use !disembark to leave the ferry or !travel <island name> to sail to a specific island.";
    public static readonly string MSG_JOIN_RAID = "You have joined the raid. Good luck!";
    public static readonly string MSG_JOIN_RAID_FERRY = "You cannot join the raid while on the ferry.";
    public static readonly string MSG_JOIN_RAID_WAR = "You cannot fight a raid boss during a war!";
    public static readonly string MSG_JOIN_RAID_DUNGEON = "You cannot fight a raid boss when participating in a dungeon!";
    public static readonly string MSG_JOIN_RAID_ALREADY = "You have already joined the raid.";
    public static readonly string MSG_JOIN_RAID_PAST_HEALTH = "You can no longer join the raid.";
    public static readonly string MSG_JOIN_RAID_ARENA = "You cannot join the raid while in the arena.";
    public static readonly string MSG_JOIN_RAID_DUEL = "You cannot join the raid while in a duel.";
    public static readonly string MSG_JOIN_RAID_NO_RAID = "There are no active raids at the moment.";

    public static readonly string MSG_ALREADY_IN_CLAN = "You cannot join a clan as you're already in one. Please leave your clan first using '!clan leave'";
    public static readonly string MSG_NOT_IN_CLAN = "You need to join a clan to use this command.";
    public static readonly string MSG_CLAN_NOT_FOUND = "Could not find a suitable clan using '{clanSearch}'. There must be atleast one player in the game with the target clan.";
    public static readonly string MSG_CLAN_INFO_UNKNOWN_ERROR = "Unable to get clan info at this time, please try again later.";
    public static readonly string MSG_CLAN_STATS_UNKNOWN_ERROR = "Unable to get clan stats at this time, please try again later.";

    public static readonly string MSG_MARKET_ITEM_UNAVAILABLE = "{itemName} is not available on the marketplace.";
    public static readonly string MSG_MARKET_VALUE_COUNT = "There are currently {available} {itemName} sold on the marketplace. Cheapest can be bought for {minPrice}, max price is {maxPrice}, average price is {avgPrice}, to buy {amount} it would cost roughly {cost} coins";
    public static readonly string MSG_MARKET_VALUE = "There are currently {available} {itemName} sold on the marketplace. Cheapest can be bought for {minPrice}, max price is {maxPrice}, average price is {avgPrice}";

    // ---- On screen notifications -------------------------------------------------------------
    // These were previously baked into PNG images (ArenaIsAboutToStart.png and friends), which
    // meant the wording could not be translated and every style change needed the art redrawn.
    // Kept here with the rest of the strings so the localization pass has one place to work.
    //
    // {command} is substituted with the chat command and highlighted by the UI, preserving the
    // red highlight the original images used to show viewers what to type.

    public static readonly string UI_ARENA_ABOUT_TO_START = "Arena is about to start";
    public static readonly string UI_ARENA_NOW_ACTIVE = "Arena is now active";
    public static readonly string UI_ARENA_JOIN = "Type {command} to join";
    public static readonly string UI_ARENA_STARTED = "May the best fighter win!";
    public static readonly string UI_ARENA_DRAW_TITLE = "Draw";
    public static readonly string UI_ARENA_DRAW_MESSAGE = "The arena ended in a Draw, everyone is rewarded a small gift";
    public static readonly string UI_ARENA_WINNER = "Congratulations for winning the arena!";

    public static readonly string UI_RAID_BOSS_APPEARED = "A boss appeared!!";
    public static readonly string UI_RAID_TITLE = "RAID BOSS";
    public static readonly string UI_RAID_LEVEL = "LV {0}";
    public static readonly string UI_RAID_RAIDERS = "{0} RAIDERS";
    public static readonly string UI_RAID_RAIDER_ONE = "1 RAIDER";
    public static readonly string UI_XP_BOOST = "XP BOOST \u00d7{0}";
    public static readonly string UI_XP_BOOST_THANKS = "Thanks to {0}";
    public static readonly string UI_PLAYERS_ONE = "1 player";
    public static readonly string UI_PLAYERS_COUNT = "{0} players";
    public static readonly string UI_PLAYERS_PARTIAL = "{0} of {1} players";
    public static readonly string UI_RESTED = "Rested {0}";
    public static readonly string UI_ISLAND_TRAINING = "TRAINING HERE";
    public static readonly string UI_ISLAND_STATISTICS = "ISLAND STATISTICS";
    public static readonly string UI_ISLAND_NOBODY = "Nobody is training here right now.";
    public static readonly string UI_ISLAND_ONSEN = "Onsen";
    public static readonly string UI_ISLAND_LEVEL = "Level {0}+";
    public static readonly string UI_ENEMY_LEVEL = "Combat level {0}";
    public static readonly string UI_ENEMY_KILLS = "{0} players defeated";
    public static readonly string UI_ENEMY_DEATHS = "{0} deaths";
    public static readonly string UI_TOWN_BUILD = "BUILD MODE";
    public static readonly string UI_TOWN_BUILD_EXIT = "EXIT BUILD";
    public static readonly string UI_BUILD_SUMMARY_TITLE = "Village bonuses";
    public static readonly string UI_BUILD_SUMMARY_PLOTS = "{0} of {1} plots built";
    public static readonly string UI_BUILD_SUMMARY_EMPTY = "No bonuses yet. Build houses and give them residents to boost their skills.";
    public static readonly string UI_BUILD_EMPTY_PLOT = "Empty plot";
    public static readonly string UI_BUILD_PLOT_NUMBER = "Plot {0}";
    public static readonly string UI_BUILD_EMPTY_PLOT_NOTE = "Pick a building in the tray below to build it here.";
    public static readonly string UI_BUILD_RESIDENT = "RESIDENT";
    public static readonly string UI_BUILD_NOBODY = "Nobody lives here yet";
    public static readonly string UI_BUILD_UNKNOWN_RESIDENT = "Away";
    public static readonly string UI_BUILD_BONUS = "+{0}% {1} XP";
    public static readonly string UI_BUILD_CARD_BONUS = "Up to +{0}% {1}";
    public static readonly string UI_BUILD_NO_RESIDENT_NOTE = "Assign a resident to switch on this building's bonus.";
    public static readonly string UI_BUILD_RESIDENT_AWAY_NOTE = "The resident is not in the game right now, so the bonus is paused.";
    public static readonly string UI_BUILD_ASSIGN = "Assign resident";
    public static readonly string UI_BUILD_CHANGE_RESIDENT = "Change resident";
    public static readonly string UI_BUILD_DEMOLISH = "Demolish";
    public static readonly string UI_BUILD_DEMOLISH_CONFIRM = "Click again";
    public static readonly string UI_BUILD_ASSIGN_TITLE = "Choose a resident for the {0}";
    public static readonly string UI_BUILD_ASSIGN_CURRENT = "Currently: {0}";
    public static readonly string UI_BUILD_LIVES_IN = "lives in plot {0}";
    public static readonly string UI_BUILD_NO_PLAYERS = "No players in the game to choose from.";
    public static readonly string UI_BUILD_HINT_IDLE = "Click a plot to select it  ·  Pick a building to place it  ·  Esc to exit";
    public static readonly string UI_BUILD_HINT_ARMED = "Click an empty plot or any free spot to build the {0}  ·  R to rotate  ·  Esc to cancel";
    public static readonly string UI_BUILD_HINT_EMPTY = "Pick a building below to build it here";
    public static readonly string UI_BUILD_HINT_BUILT = "Pick a building below to replace this one";
    public static readonly string UI_BUILD_HINT_BUSY = "Working on it...";
    public static readonly string UI_BUILD_HINT_MOVING = "Move the mouse to place  ·  R to rotate (Shift back, Alt 90°)  ·  Click to place  ·  Esc to cancel";
    public static readonly string UI_BUILD_HINT_MOVING_BLOCKED = "{0}  ·  R to rotate  ·  Esc to cancel";
    public static readonly string UI_BUILD_MOVE = "Move";
    public static readonly string UI_BUILD_ROTATE = "Rotate";
    public static readonly string UI_BUILD_SEARCH = "Search players...";
    public static readonly string UI_BUILD_TRAY_TITLE = "{0} BUILDINGS";
    public static readonly string UI_BUILD_BUILD = "BUILD";
    public static readonly string UI_BUILD_SUMMARY_CONTRIBUTING = "{0} buildings contributing";
    public static readonly string UI_BUILD_ONE_BUILDING = "1 building";
    public static readonly string UI_BUILD_BUILDINGS = "{0} buildings";
    public static readonly string UI_BUILD_CHANGE_RESIDENT_SHORT = "Change resident";
    public static readonly string UI_BUILD_SKILL_LEVEL = "{0} level {1}";
    public static readonly string UI_BUILD_PLOTS_FREE = "{0} of {1} plots free";
    public static readonly string UI_BUILD_ASSIGN_BUTTON = "Assign";
    public static readonly string UI_BUILD_TAG_CURRENT = "CURRENT";
    public static readonly string UI_BUILD_TAG_BEST = "BEST";
    public static readonly string UI_BUILD_RESET_POSITION = "Reset position";
    public static readonly string UI_BUILD_PLACE_OUTSIDE = "Too far from the town hall";
    public static readonly string UI_BUILD_PLACE_TOWN_HALL = "Overlaps the town hall";
    public static readonly string UI_BUILD_NO_FREE_PLOTS = "No free plots. Demolish a building, or level the village up for more.";
    public static readonly string UI_BUILD_PLACE_OVERLAP = "Too close to another building";
    public static readonly string UI_BUILD_PLACE_WATER = "You can't build on water";
    public static readonly string UI_BUILD_PLACE_STEEP = "The ground is too steep here";
    public static readonly string UI_BUILD_PLACE_REJECTED = "The server did not accept the move. Try again in a moment.";
    public static readonly string UI_BUILD_PLACE_UNSUPPORTED = "Moving buildings needs a server update.";
    public static readonly string UI_BUILD_HALL_TITLE = "Town hall";
    public static readonly string UI_BUILD_HALL_TIER = "Tier {0}";
    public static readonly string UI_BUILD_HALL_NOTE = "Move the town hall anywhere in the village. Its outline is the room the biggest town hall needs, so there is always space to upgrade.";
    public static readonly string UI_BUILD_HALL_PLOT = "The town hall needs this space; a building is in the way";
    public static readonly string UI_BUILD_HALL_OUTSIDE = "The town hall must stay inside the village";
    public static readonly string UI_BUILD_HINT_HALL = "Move, rotate or upgrade the town hall. Esc to deselect.";
    public static readonly string UI_BUILD_HALL_TIER_NAMED = "Tier {0}: {1}";
    public static readonly string UI_BUILD_HALL_NEXT = "NEXT: {0}";
    public static readonly string UI_BUILD_HALL_MAX = "This is the grandest town hall there is.";
    public static readonly string UI_BUILD_HALL_UPGRADE = "Upgrade";
    public static readonly string UI_SHORTCUT_HUD = "Hide or show the interface (F1)";
    public static readonly string UI_SHORTCUT_DETAILS = "Player details (F2)";
    public static readonly string UI_SHORTCUT_NEXT_PLAYER = "Watch the next player (Tab)";
    public static readonly string UI_SHORTCUT_FREE_CAMERA = "Free camera: hold Shift or Ctrl and Tab to fly, or click to switch it on and off";
    public static readonly string UI_SHORTCUT_SEARCH = "Find a player (Ctrl+F)";
    public static readonly string UI_SHORTCUT_EXP = "Player list: switch between total experience and experience per hour (I)";
    public static readonly string UI_SHORTCUT_LOGOS = "Hide logos (Q)";
    public static readonly string UI_SHORTCUT_UI_BIGGER = "Bigger interface (Ctrl and +)";
    public static readonly string UI_SHORTCUT_UI_SMALLER = "Smaller interface (Ctrl and -)";
    public static readonly string UI_SHORTCUT_GRAPHICS = "Turn graphics off or on, to save power (F9)";
    public static readonly string UI_SHORTCUT_MENU = "Menu (Esc)";
    public static readonly string UI_SHORTCUT_RAID = "Start a raid (admin, Ctrl+P)";
    public static readonly string UI_SHORTCUT_DUNGEON = "Start a dungeon (admin, Ctrl+O)";
    public static readonly string UI_TOWN_UPGRADE_READY = "A town hall upgrade is ready. Open build mode and select the town hall.";
    public static readonly string UI_BUILD_HALL_UPGRADE_FAILED = "The upgrade did not go through. Check the requirements and try again.";
    public static readonly string UI_BUILD_HALL_NEED_LEVEL = "Village level {0}";
    public static readonly string UI_BUILD_HALL_NEED = "{0} {1} / {2}";
    public static readonly string[] UI_BUILD_HALL_NAMES = { "Longhouse", "Town hall", "Guild hall", "Keep", "Castle" };
    public static readonly string UI_TOWN_VILLAGE = "VILLAGE";
    public static readonly string UI_TOWN_LEVEL = "LV {0}";
    public static readonly string UI_TOWN_HOUSES = "HOUSES";
    public static readonly string UI_TOWN_WOOD = "WOOD";
    public static readonly string UI_TOWN_ORE = "ORE";
    public static readonly string UI_TOWN_FISH = "FISH";
    public static readonly string UI_TOWN_WHEAT = "WHEAT";
    public static readonly string UI_TOWN_COINS = "COINS";
    public static readonly string UI_DUNGEON_TITLE = "DUNGEON";
    public static readonly string UI_DUNGEON_ROOM = "ROOM {0} / {1}";
    public static readonly string UI_DUNGEON_BOSS_ROOM = "BOSS ROOM";
    public static readonly string UI_DUNGEON_JOINED = "{0} JOINED";
    public static readonly string UI_DUNGEON_ALIVE = "{0} / {1} ALIVE";
    public static readonly string UI_DUNGEON_ENEMIES = "{0} ENEMIES";
    public static readonly string UI_DUNGEON_ENEMY_ONE = "1 ENEMY";
    public static readonly string UI_METER_DAMAGE = "Damage Done";
    public static readonly string UI_METER_HEALING = "Healing Done";
    public static readonly string UI_METER_TAKEN = "Damage Taken";
    public static readonly string UI_METER_GROUP_TOTAL = "GROUP TOTAL";
    public static readonly string UI_METER_EMPTY = "Nothing recorded yet.";
    public static readonly string UI_METER_RESULT = "RESULT";
    public static readonly string UI_DETAILS_TRAINING = "TRAINING";
    public static readonly string UI_DETAILS_IDLE = "IDLE";
    public static readonly string UI_DETAILS_IDLE_HINT = "Not training anything. Type {command} in chat to start.";
    public static readonly string UI_DETAILS_RESTING = "RESTING";
    public static readonly string UI_DETAILS_FULLY_RESTED_IN = "Fully rested in {0}";
    public static readonly string UI_DETAILS_FULLY_RESTED = "Fully rested";
    public static readonly string UI_DETAILS_RESTED_OF = "{0} of {1}";
    public static readonly string UI_METER_CLOSING = "Closes in {0}s";
    public static readonly string UI_DETAILS_NEXT_LEVEL = "Next level in {0}";
    public static readonly string UI_DETAILS_TARGET_REACHED = "Target level {0} reached";
    public static readonly string UI_DETAILS_MAX_LEVEL = "Max level";
    public static readonly string UI_DETAILS_LEVEL_TOO_HIGH = "Level too high for this island";
    public static readonly string UI_DETAILS_LEVEL_TOO_LOW = "Level too low for this island";
    public static readonly string UI_DETAILS_INELIGIBLE = "Not able to train here";
    public static readonly string UI_DETAILS_RECOMMENDED_TITLE = "Recommended island";
    public static readonly string UI_DETAILS_RECOMMENDED = "Recommended island: {0}";
    public static readonly string UI_DETAILS_CLAN_SKILL_LOCKED = "A clan skill. It shows here once the player's clan has unlocked it.";
    public static readonly string UI_DETAILS_EMPTY_SLOT = "Nothing equipped.";
    public static readonly string UI_DETAILS_TRANSMOGRIFIED = "Transmogrified";
    public static readonly string UI_RAID_JOIN = "Type {command} to join";
    public static readonly string UI_RAID_REWARD_HINT = "To get a chance of being rewarded items!";
    public static readonly string UI_MENU_CONTINUE = "Continue";
    public static readonly string UI_MENU_SETTINGS = "Settings";
    public static readonly string UI_MENU_LOGOUT = "Log out";
    public static readonly string UI_MENU_EXIT = "Exit game";
    public static readonly string UI_MENU_CONFIRM_LOGOUT = "Click again to log out";
    public static readonly string UI_MENU_CONFIRM_EXIT = "Click again to exit";
    public static readonly string UI_MENU_LOG_FOLDER = "Open log folder";
    public static readonly string[] UI_MENU_INFO = { "Version", "Graphics", "Graphics memory", "System memory", "System", "Uptime" };
    public static readonly string[] UI_MENU_TIPS =
    {
        "Tip: Sir Clucksworth sees everything.",
        "Tip: Potato mode is not a setting, it is a lifestyle.",
        "Tip: The raid horn volume goes all the way up to BWAAAH.",
        "Tip: Spin the observer camera as fast as it goes. Wheee!",
        "Tip: Ravens are smarter than they look.",
        "Tip: Press F1 when you want the world to yourself.",
        "Tip: The training dummies have been through a lot. Be gentle.",
        "Tip: Somewhere, a rubber duck is floating in a fishing tub.",
    };

    public static readonly string UI_SETTINGS_TITLE = "SETTINGS";
    public static readonly string UI_SETTINGS_FOOTER = "Changes apply right away and are saved when you close settings.";
    public static readonly string UI_SETTINGS_DONE = "Done";
    public static readonly string UI_SETTINGS_TAB_GAME = "Game";
    public static readonly string UI_SETTINGS_TAB_GRAPHICS = "Graphics";
    public static readonly string UI_SETTINGS_TAB_SOUND = "Sound";
    public static readonly string UI_SETTINGS_TAB_INTERFACE = "Interface";
    public static readonly string UI_SETTINGS_TAB_BOT = "Bot";
    public static readonly string UI_SETTINGS_TAB_ADMIN = "Admin";
    public static readonly string UI_KEY_HUD = "Hide or show the interface";
    public static readonly string UI_KEY_DETAILS = "Player details";
    public static readonly string UI_KEY_NEXT_PLAYER = "Watch the next player";
    public static readonly string UI_KEY_FREE_CAMERA = "Free camera (hold)";
    public static readonly string UI_KEY_NEXT_ISLAND = "Watch the next island";
    public static readonly string UI_KEY_CONTROL_PLAYER = "Take control of the watched player";
    public static readonly string UI_KEY_SEARCH = "Find a player";
    public static readonly string UI_KEY_EXP = "Player list: total exp or exp per hour";
    public static readonly string UI_KEY_LOGOS = "Hide logos";
    public static readonly string UI_KEY_UI_BIGGER = "Bigger interface";
    public static readonly string UI_KEY_UI_SMALLER = "Smaller interface";
    public static readonly string UI_KEY_GRAPHICS = "Turn graphics off or on";
    public static readonly string UI_KEY_PHYSICS = "Turn physics on or off";
    public static readonly string UI_KEY_SAVE = "Save the players now";
    public static readonly string UI_KEY_RELOAD = "Reload the game";
    public static readonly string UI_KEY_RAID = "Start a raid";
    public static readonly string UI_KEY_DUNGEON = "Start a dungeon";
    public static readonly string UI_SETTINGS_TAB_KEYS = "Keys";
    public static readonly string UI_SETTINGS_KEYS_HINT = "Click a key to change it, then press the new one. Esc cancels, Backspace leaves it unbound. A key already in use swaps over.";
    public static readonly string UI_SETTINGS_KEYS_PRESS = "Press a key";
    public static readonly string UI_SETTINGS_KEYS_SWAPPED = "Swapped with {0}";
    public static readonly string UI_SETTINGS_KEYS_FIXED = "Esc always opens the menu and backs out of windows, and the free camera flies with WASD, Q and E.";
    public static readonly string UI_SETTINGS_SECTION_KEYS_CAMERA = "CAMERA";
    public static readonly string UI_SETTINGS_SECTION_KEYS_INTERFACE = "INTERFACE";
    public static readonly string UI_SETTINGS_SECTION_KEYS_GAME = "GAME";
    public static readonly string UI_SETTINGS_SECTION_KEYS_ADMIN = "ADMIN";
    public static readonly string UI_SETTINGS_SECTION_DISPLAY = "DISPLAY";
    public static readonly string UI_SETTINGS_WINDOW_MODE = "Window mode";
    public static readonly string UI_SETTINGS_WINDOW_MODE_HINT = "Borderless fills the screen and is the quickest to switch away from while streaming.";
    public static readonly string[] UI_SETTINGS_WINDOW_MODE_OPTIONS = { "Borderless", "Fullscreen", "Windowed" };
    public static readonly string UI_SETTINGS_SCREEN_SIZE = "Resolution";
    public static readonly string UI_SETTINGS_SCREEN_NATIVE = "{0} x {1} (native)";
    public static readonly string UI_SETTINGS_SCREEN_SIZE_VALUE = "{0} x {1}";
    public static readonly string UI_SETTINGS_VSYNC = "Vertical sync";
    public static readonly string UI_SETTINGS_VSYNC_HINT = "Matches the monitor's refresh rate and stops tearing. The frame rate limit is not used while it is on.";
    public static readonly string UI_SETTINGS_FPS = "Frame rate limit";
    public static readonly string UI_SETTINGS_FPS_HINT = "A lower limit saves power and keeps the computer cool while streaming.";
    public static readonly string[] UI_SETTINGS_FPS_OPTIONS = { "No limit", "30", "60", "90", "120", "144", "165", "240" };
    public static readonly string UI_SETTINGS_MASTER = "Everything";
    public static readonly string UI_SETTINGS_MASTER_HINT = "All of the game's sound at once.";
    public static readonly string UI_SETTINGS_SECTION_FREE_CAMERA = "FREE CAMERA AND ZOOM";
    public static readonly string UI_SETTINGS_FREE_SPEED = "Flying speed";
    public static readonly string UI_SETTINGS_FREE_SPEED_HINT = "Hold Shift to fly five times faster and Alt to go slower. Keypad + and - still adjust it while flying.";
    public static readonly string UI_SETTINGS_LOOK = "Mouse look";
    public static readonly string UI_SETTINGS_LOOK_HINT = "Turning speed while holding the right mouse button or Ctrl in the free camera.";
    public static readonly string UI_SETTINGS_INVERT_LOOK = "Invert mouse look";
    public static readonly string UI_SETTINGS_ZOOM = "Scroll zoom";
    public static readonly string UI_SETTINGS_ZOOM_HINT = "How far one turn of the wheel zooms the player and island cameras.";
    public static readonly string UI_SETTINGS_INVERT_ZOOM = "Invert scroll zoom";
    public static readonly string UI_SETTINGS_TIMES = "x{0}";
    public static readonly string UI_SETTINGS_SECTION_CONNECTION = "CONNECTION";
    public static readonly string UI_SETTINGS_BOT_SERVER = "RavenBot server";
    public static readonly string UI_SETTINGS_BOT_SERVER_HINT = "As host:port. Leave it empty for the default.";
    public static readonly string UI_SETTINGS_LOCAL_BOT = "Accept a bot on this computer";
    public static readonly string UI_SETTINGS_LOCAL_BOT_HINT = "For streamers who run RavenBot themselves.";
    public static readonly string UI_SETTINGS_LOCAL_PORT = "Local bot port";
    public static readonly string UI_SETTINGS_SECTION_OVERLAYS = "STREAM OVERLAYS";
    public static readonly string UI_SETTINGS_QUERY = "Query engine";
    public static readonly string UI_SETTINGS_QUERY_HINT = "A local web API with the game's data, for overlays and tools.";
    public static readonly string UI_SETTINGS_QUERY_ADDRESS = "Query engine address";
    public static readonly string UI_SETTINGS_RELOAD_NOTE = "Applies when the game reloads.";
    public static readonly string UI_SETTINGS_RELOAD_BUTTON = "Reload now";
    public static readonly string UI_SETTINGS_EXP_TIMES = "Warn when this much is left";
    public static readonly string[] UI_SETTINGS_EXP_TIME_OPTIONS = { "10 min", "5 min", "3 min", "1 min", "30 s", "When it ends" };
    public static readonly string UI_SETTINGS_RESET_PAGE = "Reset page";
    public static readonly string UI_SETTINGS_RESET_PAGE_HINT = "Puts every setting on this page back to its default";
    public static readonly string UI_SETTINGS_RESET_ONE = "Back to the default";
    public static readonly string UI_SETTINGS_SECTION_OBSERVER = "OBSERVER CAMERA";
    public static readonly string UI_SETTINGS_OBSERVE = "Time on each player";
    public static readonly string UI_SETTINGS_OBSERVE_HINT = "How long the camera stays on a player before it moves on.";
    public static readonly string UI_SETTINGS_OBSERVE_SUB = "Time on subscribers";
    public static readonly string UI_SETTINGS_OBSERVE_VIP = "Time on VIPs";
    public static readonly string UI_SETTINGS_OBSERVE_MOD = "Time on moderators";
    public static readonly string UI_SETTINGS_OBSERVE_EVENT = "Time after a sub or a cheer";
    public static readonly string UI_SETTINGS_OBSERVE_EVENT_HINT = "The camera turns to whoever subscribed or cheered.";
    public static readonly string UI_SETTINGS_OBSERVE_ISLAND = "Time on each island";
    public static readonly string UI_SETTINGS_OBSERVE_EMPTY = "Visit empty islands";
    public static readonly string UI_SETTINGS_OBSERVE_EMPTY_HINT = "Islands with nobody on them still get a turn in the island camera.";
    public static readonly string UI_SETTINGS_AFK = "Kick players who are away";
    public static readonly string UI_SETTINGS_AFK_HINT = "Players who have not typed a command for this long are removed.";
    public static readonly string[] UI_SETTINGS_AFK_OPTIONS = { "Never", "After 1 hour", "After 2 hours", "After 4 hours", "After 8 hours", "After 12 hours", "After 24 hours" };
    public static readonly string UI_SETTINGS_NO_EXP = "Warn players who gain no exp";
    public static readonly string UI_SETTINGS_NO_EXP_HINT = "The bot points anyone training where they gain nothing to a better island.";
    public static readonly string UI_SETTINGS_NO_EXP_TIME = "First warning after";
    public static readonly string UI_SETTINGS_NO_EXP_TIME_HINT = "Later warnings wait five times as long.";
    public static readonly string[] UI_SETTINGS_NO_EXP_OPTIONS = { "2 minutes", "5 minutes", "10 minutes", "15 minutes", "30 minutes" };
    public static readonly string UI_SETTINGS_SECTION_LABELS = "STREAM LABELS";
    public static readonly string UI_SETTINGS_LABELS_TEXT = "Text files";
    public static readonly string UI_SETTINGS_LABELS_JSON = "Json files";
    public static readonly string UI_SETTINGS_SECTION_MESSAGES = "MESSAGES";
    public static readonly string UI_SETTINGS_SECTION_LOOT = "LOOT";
    public static readonly string UI_SETTINGS_LOOT_ORIGIN = "Say where loot came from";
    public static readonly string UI_SETTINGS_LOOT_ORIGIN_HINT = "!loot replies tell whether each item dropped in a raid or a dungeon.";
    public static readonly string UI_SETTINGS_SECONDS = "{0} s";
    public static readonly string UI_SETTINGS_ON = "On";
    public static readonly string UI_SETTINGS_OFF = "Off";

    public static readonly string UI_SETTINGS_SECTION_CAMERA = "CAMERA";
    public static readonly string UI_SETTINGS_SECTION_PLAYERS = "PLAYERS";
    public static readonly string UI_SETTINGS_SECTION_CHAT = "CHAT";
    public static readonly string UI_SETTINGS_SECTION_WORLD = "WORLD";
    public static readonly string UI_SETTINGS_CAMERA_ROTATION = "Observer camera rotation";
    public static readonly string UI_SETTINGS_CAMERA_ROTATION_HINT = "How fast the camera circles the player you are watching, and which way.";
    public static readonly string UI_SETTINGS_CAMERA_STILL = "No rotation";
    public static readonly string UI_SETTINGS_CAMERA_WHEE = "Wheee!";
    public static readonly string UI_SETTINGS_BOOST = "Subscriber exp bonus";
    public static readonly string UI_SETTINGS_BOOST_HINT = "Who gets the extra exp from your channel's subscription tier.";
    public static readonly string[] UI_SETTINGS_BOOST_OPTIONS = { "Subscribers", "Everyone" };
    public static readonly string UI_SETTINGS_EXPIRY = "Keep players after a restart";
    public static readonly string UI_SETTINGS_EXPIRY_HINT = "How long players stay in the saved game state when the game restarts.";
    public static readonly string[] UI_SETTINGS_EXPIRY_OPTIONS = { "Don't keep", "2 hours", "4 hours", "8 hours", "12 hours", "24 hours", "36 hours", "48 hours", "Forever" };
    public static readonly string UI_SETTINGS_EXPIRY_ALERT = "Tell chat when saved players expire";
    public static readonly string UI_SETTINGS_DROPS = "Item drop messages";
    public static readonly string UI_SETTINGS_DROPS_HINT = "How raid and dungeon loot is announced in chat. For example:";
    public static readonly string[] UI_SETTINGS_DROPS_OPTIONS = { "One item per message", "One player per message", "All in one message", "Items and counts", "Item names only", "No messages" };
    public static readonly string UI_SETTINGS_DROPS_NONE = "(nothing is posted)";
    public static readonly string UI_SETTINGS_PATHFINDING = "Navigation quality";
    public static readonly string UI_SETTINGS_PATHFINDING_HINT = "How much time players get to find their way each frame. Higher is smoother with many players, and costs more CPU.";
    public static readonly string[] UI_SETTINGS_PATHFINDING_OPTIONS = { "Low", "Normal", "High", "Ultra" };
    public static readonly string UI_SETTINGS_HOUSES = "Give empty houses to players";
    public static readonly string UI_SETTINGS_HOUSES_HINT = "Moves players into village houses that have nobody living in them.";
    public static readonly string UI_SETTINGS_LABELS = "Write stream label files";
    public static readonly string UI_SETTINGS_LABELS_HINT = "Text and json files your streaming software can show.";
    public static readonly string UI_SETTINGS_NO_RAIDS = "Turn off raids";
    public static readonly string UI_SETTINGS_NO_DUNGEONS = "Turn off dungeons";

    public static readonly string UI_SETTINGS_SECTION_QUALITY = "QUALITY";
    public static readonly string UI_SETTINGS_SECTION_DAYNIGHT = "DAY AND NIGHT";
    public static readonly string UI_SETTINGS_SECTION_PERFORMANCE = "PERFORMANCE";
    public static readonly string UI_SETTINGS_QUALITY = "Graphics quality";
    public static readonly string[] UI_SETTINGS_QUALITY_OPTIONS = { "Low", "Medium", "High" };
    public static readonly string UI_SETTINGS_POTATO = "Potato mode";
    public static readonly string UI_SETTINGS_POTATO_HINT = "The lowest graphics there are, for computers that are struggling.";
    public static readonly string UI_SETTINGS_AUTO_POTATO = "Potato mode while in the background";
    public static readonly string UI_SETTINGS_AUTO_POTATO_HINT = "Saves power when another window has focus.";
    public static readonly string UI_SETTINGS_POST = "Post processing";
    public static readonly string UI_SETTINGS_POST_HINT = "Bloom and colour grading.";
    public static readonly string UI_SETTINGS_DAYNIGHT = "Day and night cycle";
    public static readonly string UI_SETTINGS_REALTIME = "Follow the clock";
    public static readonly string UI_SETTINGS_REALTIME_HINT = "Day and night follow the time on this computer.";
    public static readonly string UI_SETTINGS_TIME = "Time of day";
    public static readonly string UI_SETTINGS_TIME_HINT = "Used while the day and night cycle is off.";
    public static readonly string UI_SETTINGS_DAY = "Day";
    public static readonly string UI_SETTINGS_NIGHT = "Night";
    public static readonly string UI_SETTINGS_VIEW_DISTANCE = "View distance";
    public static readonly string UI_SETTINGS_RESOLUTION = "Render resolution";
    public static readonly string UI_SETTINGS_RESOLUTION_HINT = "Draws the world at a lower resolution to save GPU time.";
    public static readonly string UI_SETTINGS_CLAN_CACHE = "Clan logos";
    public static readonly string UI_SETTINGS_CLAN_CACHE_HINT = "Throws away the downloaded logos so they are fetched again.";
    public static readonly string UI_SETTINGS_CLAN_CACHE_BUTTON = "Clear cache";

    public static readonly string UI_SETTINGS_MUSIC = "Music";
    public static readonly string UI_SETTINGS_EVENTS = "Raid horns and dungeon gongs";
    public static readonly string UI_SETTINGS_EVENTS_HINT = "Also the alert when another stream raids yours.";
    public static readonly string UI_SETTINGS_SILENCE = "Silence";
    public static readonly string UI_SETTINGS_BWAAAH = "BWAAAH";

    public static readonly string UI_SETTINGS_UI_SCALE = "Interface size";
    public static readonly string UI_SETTINGS_UI_SCALE_HINT = "Also Ctrl and + or - while playing.";
    public static readonly string UI_SETTINGS_NAMES = "Player names over heads";
    public static readonly string UI_SETTINGS_PLAYER_LIST = "Player list";
    public static readonly string UI_SETTINGS_PLAYER_DETAILS = "Player details";
    public static readonly string UI_SETTINGS_PLAYER_DETAILS_HINT = "The panel for the player the camera is watching (F2).";
    public static readonly string UI_SETTINGS_SHORTCUTS = "Shortcut bar";
    public static readonly string UI_SETTINGS_SHORTCUTS_HINT = "The row of keyboard shortcuts along the bottom.";
    public static readonly string UI_SETTINGS_RESET_WINDOWS = "Window positions";
    public static readonly string UI_SETTINGS_RESET_WINDOWS_HINT = "Puts every window back where it started.";
    public static readonly string UI_SETTINGS_RESET_WINDOWS_BUTTON = "Reset";

    public static readonly string UI_SETTINGS_BOT_HINT = "Which messages the bot posts in your chat.";
    public static readonly string UI_SETTINGS_BOT_EXP = "Exp multiplier running out";
    public static readonly string UI_SETTINGS_BOT_WELCOME = "Players joining and leaving";
    public static readonly string UI_SETTINGS_BOT_DISEMBARK = "Ferry arrivals";
    public static readonly string UI_SETTINGS_BOT_TRAIN = "Training warnings";
    public static readonly string UI_SETTINGS_BOT_TRAIN_HINT = "When a player trains something their island can't teach them.";
    public static readonly string UI_SETTINGS_BOT_DUNGEON = "Dungeon results";
    public static readonly string UI_SETTINGS_BOT_RAID = "Raid results";
    public static readonly string UI_SETTINGS_BOT_ITEMS = "Item drops";
    public static readonly string UI_SETTINGS_BOT_ITEMS_HINT = "Loot from raids and dungeons. How they read is under Game.";

    public static readonly string UI_SETTINGS_DEV_TOOLS = "Developer tools";
    public static readonly string UI_SETTINGS_DEV_TOOLS_HINT = "The column of debug buttons on the right of the screen.";
    public static readonly string UI_SETTINGS_DISCONNECT = "Server connection";
    public static readonly string UI_SETTINGS_DISCONNECT_HINT = "Drops the connection to test that the game reconnects.";
    public static readonly string UI_SETTINGS_DISCONNECT_BUTTON = "Disconnect";

    public static readonly string UI_RAID_COME_HELP = "Come help fight! Type {command}";

    public static readonly string UI_DUNGEON_APPEARED = "A dungeon has appeared!";
    public static readonly string UI_DUNGEON_JOIN = "Type {command} to join";
}
