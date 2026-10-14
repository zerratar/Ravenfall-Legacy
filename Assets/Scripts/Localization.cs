using static MessageUtilities;

/// <summary>
/// Every text the game shows or posts in chat, in English. The fields are not readonly: Languages
/// replaces them with a translation when another language is chosen. See Languages.cs.
/// </summary>
public static class Localization
{
    public static string MSG_PATREON_ONLY = Meta("system", "fail") + "This command is for Patreon supporters only. Consider supporting the game on Patreon to gain access to this command.";
    public static string PETRACE_NO_PET = Meta("minigame", "pet race", "fail") + "You need to equip a pet to play.";
    public static string PETRACE_ALREADY_PLAYING = Meta("minigame", "pet race", "fail") + "You're already playing.";
    public static string PETRACE_START_COMMAND = "race"; // Meta("minigame", "pet race") + 
    public static string EQUIP_SHIELD_AND_TWOHANDED = Meta("equipment", "equip", "fail") + "You cannot equip a shield while having a 2-handed weapon equipped.";
    public static string CANT_TRAIN_HERE = Meta("training", "fail") + "You cannot train {type} here.";
    public static string NOT_HIGH_ENOUGH_SKILL = Meta("training", "fail") + "You need to have at least level {reqSkill} {type} to train this skill on this island.";
    public static string NOT_HIGH_ENOUGH_COMBAT = Meta("training", "fail") + "You are not high enough combat level to train {skillName} here. Your combat level needs to be at least {reqCombat}.";
    public static string NOT_HIGH_ENOUGH_SKILL_OR_COMBAT = Meta("training", "fail") + "You are not high enough level to train {skillName} here. Your skill or combat level needs to be at least {reqCombat}.";
    public static string NOT_HIGH_ENOUGH_SKILL_AND_COMBAT = Meta("training", "fail") + "You need to be at least combat level {reqCombat} and skill level {reqSkill} to train this skill on this island.";

    public static string GAME_NOT_LOADED = Meta("system", "fail") + "Game has not finished loading yet, try again soon!";
    public static string GAME_NOT_READY = Meta("system", "fail") + "Game is not ready yet!";

    public static string MSG_COMMAND_COOLDOWN = Meta("system", "fail") + "You must wait another {cooldown} secs to use that command.";
    public static string MSG_NOT_PLAYING = Meta("system", "fail") + "You are not currently playing. Use !join to start playing!";
    public static string MSG_EQUIP_STATS = Meta("info", "equipment") + "{armorPower} Armor, {weaponPower} Melee Weapon Power, {weaponAim} Melee Weapon Aim, {magicPower} Magic/Healing Power, {magicAim} Magic Aim, {rangedPower} Ranged Weapon Power, {rangedAim} Ranged Weapon Aim";
    public static string MSG_EQUIP_STATS_FORMAT = "{armorPower} Armor, {weaponPower} Melee Weapon Power, {weaponAim} Melee Weapon Aim, {magicPower} Magic/Healing Power, {magicAim} Magic Aim, {rangedPower} Ranged Weapon Power, {rangedAim} Ranged Weapon Aim";

    public static string MSG_STATS = Meta("info", "stats") + "Combat level {combatLevel}, {skills} -- TOTAL {total} --";
    public static string MSG_SKILL = Meta("info", "skill") + "{skill}, {currenExp} / {requiredExp} EXP.";
    public static string MSG_PLAYER_RESOURCES = Meta("info", "resources") + "Wood {wood}, Ore {ore}, Fish {fish}, Wheat {wheat}, Coin {coins}";
    public static string MSG_PLAYER_COINS = Meta("info", "resources") + "You have {coins} ({coinsShort}) coins";
    public static string MSG_TOWN_RESOURCES = Meta("info", "resources") + "Wood {wood}, Ore {ore}, Fish {fish}, Wheat {wheat}, Coin {coins}";
    public static string MSG_STREAMER_TOKENS = Meta("info", "tokens") + "You have {tokenCount} {tokenName}(s) you can use.";
    public static string MSG_PLAYER_INSPECT_URL = Meta("system", "inspect") + "https://www.ravenfall.stream/inspect/{characterId}";
    public static string MSG_PLAYERS_ONLINE = Meta("system") + "There are currently {playerCount} players in the game.";
    public static string MSG_MULTIPLIER_ENDS = Meta("system", "exp multiplier") + "The current exp multiplier (x{multiplier}) will end in {minutes}{seconds}.";
    public static string MSG_MULTIPLIER_ENDED = Meta("system", "exp multiplier") + "The exp multiplier has expired.";
    public static string MSG_MULTIPLIER_LIMIT = Meta("system", "exp multiplier") + "The exp multiplier limit has been set to {expMultiplier}!";
    public static string MSG_MULTIPLIER_INCREASE = Meta("system", "exp multiplier") + "You have increased the exp multiplier with {added}. It is now at {multiplier}!";
    public static string MSG_MULTIPLIER_RESET = Meta("system", "exp multiplier") + "You have reset the exp multiplier! NotLikeThis";
    public static string MSG_MULTIPLIER_SET = Meta("system", "exp multiplier") + "The exp multiplier has been set to {amount}!";
    public static string MSG_SUB_CREW = Meta("transaction", "subscription") + "Welcome to the {streamerDisplayName} crew! You will now gain {tierMultiplier}x more exp on this stream!";
    public static string MSG_BIT_CHEER = Meta("transaction", "bits") + "Cheer {bitsLeft} more bits for a {tokenName}";
    public static string MSG_BIT_CHEER_INCREASE = Meta("system", "exp multiplier") + "You have increased the multiplier timer with {multiAdded} minutes with your {bits} cheer!! We only need {bitsLeft} more bits for increasing the timer! <3";
    public static string MSG_BIT_CHEER_LEFT = Meta("system", "exp multiplier") + "We only need {bitsLeft} more bits for increasing the multiplier timer!";
    public static string MSG_SUB_TOKEN_REWARD = Meta("system", "reward") + "You were rewarded {amount} {tokenName}(s)!";
    public static string MSG_RAID_START_CODE = Meta("event", "raid", "raid boss") + "A level {raidLevel} raid boss has appeared! Help fight him by typing '!raid code', find the code on stream.";
    public static string MSG_RAID_START = Meta("event", "raid", "raid boss") + "A level {raidLevel} raid boss has appeared! Help fight him by typing !raid";
    public static string MSG_RAID_START_ERROR = Meta("system", "raid", "raid start", "fail") + "Raid cannot be started right now.";
    public static string MSG_DUEL_REQ = Meta("duel", "request") + "A duel request received from {opponent}, reply with !duel accept or !duel decline";
    public static string MSG_DUEL_REQ_TIMEOUT = Meta("duel", "request", "timeout") + "The duel request from {requester} has timed out and automatically declined.";
    public static string MSG_DUEL_REQ_DECLINE = Meta("duel", "request", "declined") + "Duel with {requester} was declined.";
    public static string MSG_DUEL_REQ_ACCEPT = Meta("duel", "request", "accepted") + "You have accepted the duel against {requester}. May the best fighter win!";
    public static string MSG_DUEL_WON = Meta("duel", "winner") + "You won the duel against {opponent}!";
    public static string MSG_DUEL_ACCEPT_FERRY = Meta("duel", "accept", "fail") + "You cannot accept a duel while on the ferry.";
    public static string MSG_DUEL_ACCEPT_IN_DUEL = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are already in a duel.";
    public static string MSG_DUEL_ACCEPT_IN_ARENA = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are participating in the Arena.";
    public static string MSG_DUEL_ACCEPT_IN_RAID = Meta("duel", "accept", "fail") + "You cannot accept the duel of another player as you are participating in a Raid.";
    public static string MSG_DUEL_ACCEPT_NO_REQ = Meta("duel", "accept", "fail") + "You do not have any pending duel requests to accept.";
    public static string MSG_FERRY_ALREADY_ON = Meta("travel", "sail", "fail") + "You're already on the ferry.";
    public static string MSG_FERRY_ALREADY_WAITING = Meta("travel", "sail", "fail") + "You're already waiting for the ferry.";
    public static string MSG_FERRY_TRAIN_SAIL = Meta("travel", "sail", "info") + "You've decided to sail the seas forever. Use !disembark or !travel <new destination> to leave the ferry. You will gain sailing exp for as long as you're on the ferry.";
    public static string MSG_FERRY_ARRIVED = Meta("travel", "sail", "info") + "You have arrived at your destination, {islandName}!";
    public static string MSG_DISEMBARK_ALREADY = Meta("travel", "sail", "fail") + "You're already disembarking the ferry.";
    public static string MSG_DISEMBARK_FAIL = Meta("travel", "sail", "fail") + "You're not on the ferry.";
    public static string MSG_ISLAND_ON_FERRY_DEST = Meta("info", "location") + "You're currently on the ferry, going to disembark at '{destination}'.";
    public static string MSG_ISLAND_ON_FERRY = Meta("info", "location") + "You're currently on the ferry.";
    public static string MSG_ISLAND = Meta("info", "location") + "You're on the island called {islandName}";
    public static string MSG_RESTED = Meta("info", "rest") + "You're rested and gain {expBoost}x more exp for {restedTime}";
    public static string MSG_RESTING = Meta("info", "rest") + "You're currently resting and will have {expBoost}x more exp for {restedTime}";


    public static string MSG_ONSEN_WRONG_ISLAND = Meta("rest", "fail") + "Resting areas are available on Kyo, Heim, Atria and Eldara only. You're currently on {islandName}.";
    public static string MSG_NOT_RESTED = Meta("info", "rest") + "You're not rested. Sail to Kyo, Heim, Atria or Eldara and use !rest or !onsen to rest.";

    public static string[] MSG_ONSEN_FERRY = new string[]
    {
        Meta("rest", "fail") + "Trying to snooze on the ferry? Seasickness isn't a great lullaby! Head over to Kyo, Heim, Atria, or Eldara for a proper nap.",
        Meta("rest", "fail") + "Dreaming of a nap on the high seas? More like a splash in the face! Best wait until you're in Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "Resting on a ferry? Hope you brought a waterproof blanket! For dry dreams, try Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "You want to rest NOW? Maybe you enjoy the lull of waves... and the sudden splash! For a less aquatic nap, go to Kyo, Heim, Atria, or Eldara.",
        Meta("rest", "fail") + "Napping on the ferry? Bold move! But for a sleep without seaspray surprises, Kyo, Heim, Atria, or Eldara is the place to be."
    };

    public static string MSG_ONSEN_FULL = Meta("rest", "fail") + "Onsen seem to be full. You will have to try again later.";
    public static string MSG_ONSEN_ENTRY = Meta("rest", "fail") + "You are now resting peacefully at the onsen. You will gain 2x more exp for two seconds for every second you stay here. Use !onsen leave when you're done.";
    public static string MSG_ONSEN_LEFT = Meta("rest", "fail") + "You have left the onsen.";
    public static string MSG_ARENA_UNAVAILABLE_WAR = Meta("arena", "fail") + "Arena is unavailable during wars. Please wait for it to be over.";
    public static string MSG_ARENA_WRONG_ISLAND = Meta("arena", "fail") + "There is no arena on the island you're on.";
    public static string MSG_ARENA_FERRY = Meta("arena", "fail") + "You cannot join the arena while on the ferry.";
    public static string MSG_ARENA_ALREADY_JOINED = Meta("arena", "fail") + "You have already joined the arena.";
    public static string MSG_ARENA_ALREADY_STARTED = Meta("arena", "fail") + "You cannot join as the arena has already started.";
    public static string MSG_ARENA_JOIN_ERROR = Meta("arena", "fail") + "You cannot join the arena at this time.";
    public static string MSG_ARENA_JOIN = Meta("arena", "info") + "You have joined the arena. Good luck!";

    public static string MSG_BUY_ITEM_NOT_FOUND = Meta("marketplace", "buy", "fail") + "Could not find an item matching the query '{query}'";
    public static string MSG_BUY_ITEM_NOT_FOUND_SUGGEST = Meta("marketplace", "buy", "fail") + "Could not find an item matching the query '{query}', did you mean: {suggestion}?";
    public static string MSG_BUY_ITEM_NOT_IN_MARKET = Meta("marketplace", "buy", "fail") + "Could not find any {itemName} in the marketplace.";
    public static string MSG_BUY_ITEM_INSUFFICIENT_COIN = Meta("marketplace", "buy", "fail") + "You do not have enough coins to buy the {itemName}.";
    public static string MSG_BUY_ITEM_MARKETPLACE_ERROR = Meta("marketplace", "buy", "fail") + "Error accessing marketplace right now.";
    public static string MSG_BUY_ITEM_ERROR = Meta("marketplace", "buy", "fail") + "Error buying {itemName}. Server returned an error. :( Try !leave and then !join to see if it was successful or not.";
    public static string MSG_SELL_ITEM_ERROR = Meta("marketplace", "sell", "fail") + "Error selling {itemName}. Server returned an error. :( Try !leave and then !join to see if it was successful or not.";
    public static string MSG_VALUE_ITEM_ERROR = Meta("marketplace", "value", "fail") + "Error valuating {itemName}. Server returned an error.";


    public static string MSG_BUY_ITEM_TOO_LOW = Meta("marketplace", "buy", "fail") + "Unable to buy any {itemName}, the cheapest asking price is {cheapestPrice}.";

    public static string MSG_GATHERING_SUGGEST = Meta("info", "gathering") + "Could not find a resource type matching the query '{query}', did you mean: {suggestion}?";
    public static string MSG_FISH_SUGGEST = Meta("info", "fishing") + "Could not find a fish type matching the query '{query}', did you mean: {suggestion}?";
    public static string MSG_CHOP_SUGGEST = Meta("info", "woodcutting") + "Could not find a wood type matching the query '{query}', did you mean: {suggestion}?";
    public static string MSG_MINE_SUGGEST = Meta("info", "mining") + "Could not find a ore type matching the query '{query}', did you mean: {suggestion}?";
    public static string MSG_FARM_SUGGEST = Meta("info", "farming") + "Could not find a farmable type matching the query '{query}', did you mean: {suggestion}?";

    public static string MSG_MINE_LEVEL_REQUIREMENT = Meta("info", "mining") + "You need to be level {level} to mine {oreName}.";
    public static string MSG_FARM_LEVEL_REQUIREMENT = Meta("info", "farming") + "You need to be level {level} to farm {farmableName}.";
    public static string MSG_CHOP_LEVEL_REQUIREMENT = Meta("info", "woodcutting") + "You need to be level {level} to chop {woodName}.";
    public static string MSG_FISH_LEVEL_REQUIREMENT = Meta("info", "fishing") + "You need to be level {level} to fish {fishName}.";
    public static string MSG_GATHER_LEVEL_REQUIREMENT = Meta("info", "gathering") + "You need to be level {level} to gather {resourceName}.";

    public static string MSG_IN_DUEL = Meta("info", "location") + "You are currently fighting in a duel.";
    public static string MSG_IN_RAID = Meta("info", "location") + "You are currently fighting in the raid.";
    public static string MSG_RAID_STARTED = Meta("raid", "info") + "There is an active raid.";
    public static string MSG_DUNGEON_STARTED = Meta("dungeon", "info") + "There is an active dungeon.";
    public static string MSG_IN_DUNGEON = Meta("info", "location") + "You're currently in the dungeon.";
    public static string MSG_STREAMRAID_NO_PLAYERS = Meta("system", "streamer raid") + "{raiderName} raided but without any players. Kappa";
    public static string MSG_STREAMRAID_WAR_NO_PLAYERS = Meta("system", "streamer raid") + "{raiderName} raided with intent of war but we don't have any players. FeelsBadMan";
    public static string MSG_JOIN_WELCOME = Meta("system", "join") + "Welcome to Ravenfall!";
    public static string MSG_JOIN_WELCOME_FIRST_TIME = Meta("system", "join") + "Welcome to Ravenfall, {playerName}! You can get started by using the !train command. Why don't you try using !train all to train your Attack, Defense, and Strength all at once?";
    public static string MSG_JOIN_FAILED = Meta("system", "join", "fail") + "Unable to join the game right now, server may be down and not responding.";
    public static string MSG_JOIN_FAILED_ALREADY_PLAYING = Meta("system", "join", "fail") + "You're already playing!";
    public static string MSG_REQ_RAID = Meta("system", "streamer raid", "fail") + "Requesting {raidType} on {streamer}s stream!";
    public static string MSG_REQ_RAID_SOON = Meta("system", "streamer raid") + "The raid will start any moment now!";
    public static string MSG_REQ_RAID_FAILED = Meta("system", "streamer raid", "fail") + "Request failed, streamer is no longer playing or has disabled raids. :(";
    public static string MSG_CRAFT_FAILED = Meta("crafting", "fail") + "{category} {type} cannot be crafted right now.";
    public static string MSG_CRAFT_FAILED_FERRY = Meta("crafting", "fail") + "You cannot craft while on the ferry";
    public static string MSG_CRAFT_FAILED_STATION = Meta("crafting", "fail") + "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting";

    public static string MSG_BREW_FAILED_STATION = Meta("alchemy", "fail") + "You have to be at the alchemy table to brew anything. Type !train alchemy before using the !brew command.";
    //public static string MSG_BREW_FAILED_STATION = Meta("alchemy", "fail") + "You have to be at the alchemy table to brew anything. Type !train alchemy before using the !brew command.";

    public static string MSG_CRAFT_CANCEL = Meta("crafting", "cancel") + "Your ongoing crafting has been cancelled.";
    public static string MSG_BREW_CANCEL = Meta("alchemy", "cancel") + "Your ongoing brewing has been cancelled.";
    public static string MSG_COOK_CANCEL = Meta("cooking", "cancel") + "Your ongoing cooking has been cancelled.";

    public static string MSG_CRAFT_FAILED_LEVEL = Meta("crafting", "fail") + "You can't craft this item, it requires level {reqCraftingLevel} crafting.";
    public static string MSG_CRAFT_FAILED_NOT_CRAFTABLE = Meta("crafting", "fail") + "{itemName} is not a craftable item.";

    public static string MSG_CRAFT_FAILED_RES = Meta("crafting", "fail") + "Insufficient resources to craft {itemName}";
    public static string MSG_CRAFT_EQUIPPED = Meta("crafting", "info") + "You crafted and equipped a {itemName}!";
    public static string MSG_CRAFT_ITEM_NOT_FOUND = Meta("crafting", "fail") + "Could not find an item matching the query '{query}'";
    public static string MSG_CRAFT_ITEM_NOT_FOUND_SUGGEST = Meta("crafting", "fail") + "Could not find an item matching the query '{query}', did you mean {suggestion}?";
    public static string MSG_CRAFT_ITEM_NOT_FOUND_MEAN = Meta("crafting", "fail") + "Could not find an item matching the query '{query}', did you mean '{itemName}'?";
    public static string MSG_CRAFT = Meta("crafting", "info") + "You crafted a {itemName}!";
    public static string MSG_CRAFT_MANY = Meta("crafting", "info") + "You crafted x{amount} {itemName}!";


    public static string MSG_GENERIC_NOT_SUITABLE_ITEM = "{itemName} is not a suitable item for this.";
    public static string MSG_GENERIC_ITEM_NOT_FOUND = "Could not find an item matching the query '{query}'";

    public static string MSG_GENERIC_FAILED_FERRY = Meta("ferry", "fail") + "You cannot do that while you are on the ferry";
    public static string MSG_GENERIC_FAILED_DUNGEON = Meta("dungeon", "fail") + "You cannot do that while you are in a dungeon";
    public static string MSG_GENERIC_FAILED_RAID = Meta("raid", "fail") + "You cannot do that while you are in a raid";
    public static string MSG_GENERIC_FAILED_ARENA = Meta("arena", "fail") + "You cannot do that while you are in the arena";
    public static string MSG_GENERIC_FAILED_DUEL = Meta("duel", "fail") + "You cannot do that while you are in a duel";
    public static string MSG_GENERIC_FAILED_RESTING = Meta("resting", "fail") + "You cannot do that while you are resting";

    public static string MSG_COOK_FAILED_FERRY = Meta("cooking", "fail") + "You cannot cook while on the ferry";
    public static string MSG_BREW_FAILED_FERRY = Meta("alchemy", "fail") + "You cannot brew while on the ferry";


    public static string MSG_GIFT = Meta("gift", "info") + "You gifted {giftCount}x {itemName} to {player}!";
    public static string MSG_GIFT_PLAYER_NOT_FOUND = Meta("gift", "fail") + "Could not find an item or player matching the query '{query}'";
    public static string MSG_GIFT_ITEM_NOT_FOUND = Meta("gift", "fail") + "Could not find a matching the query '{query}'";
    public static string MSG_GIFT_ITEM_NOT_OWNED = Meta("gift", "fail") + "You do not have any {itemName} to gift.";
    public static string MSG_GIFT_ERROR = Meta("gift", "fail") + "Error gifting {giftCount}x {itemName} to {player}. FeelsBadMan";

    public static string MSG_SEND = Meta("send", "info") + "You sent {count}x {itemName} to {player}!";
    public static string MSG_SEND_PLAYER_NOT_FOUND = Meta("send", "fail") + "Could not find an item or player matching the query '{query}'";
    public static string MSG_SEND_ITEM_NOT_FOUND = Meta("send", "fail") + "Could not find a matching the query '{query}'";
    public static string MSG_SEND_ITEM_NOT_OWNED = Meta("send", "fail") + "You do not have any {itemName} to gift.";
    public static string MSG_SEND_ERROR = Meta("send", "fail") + "Error sending {count}x {itemName} to {player}. FeelsBadMan Either server is having hiccups or you don't have a character with that number or alias.";

    public static string MSG_EQUIPPED = Meta("info", "equipment", "equip") + "You have equipped {itemName}.";
    public static string MSG_EQUIPPED_ALL = Meta("info", "equipment", "equip") + "You have equipped all of your best items.";

    public static string MSG_ITEM_COUNT_MISSING_ARGS = Meta("info", "item count", "fail") + "You must specify an item. Use !items (item name) or !count (item name)";
    public static string MSG_ITEM_EXAMINE_MISSING_ARGS = Meta("info", "examine", "fail") + "You must specify an item. Use !examine (item name)";
    public static string MSG_ITEM_USE_MISSING_ARGS = Meta("info", "use", "fail") + "You must specify an item. Use !use (item name)";

    public static string MSG_ENCHANT_MISSING_ARGS = Meta("enchanting", "fail") + "You must specify an item to enchant. Use !enchant (item name)";
    public static string MSG_ENCHANT_CLAN_SKILL = Meta("enchanting", "fail", "no clan") + "Enchanting is a clan skill. Join a clan to be able to use it!";
    public static string MSG_ENCHANT_UNKNOWN_ERROR = Meta("enchanting", "fail") + "Enchanting failed. Unknown reason. Please try again later.";

    public static string MSG_ENCHANT_COOLDOWN = Meta("enchanting", "fail", "cooldown") + "You have to wait {timeLeft} before you can try to enchant something again.";
    public static string MSG_ENCHANT_FAILED = Meta("enchanting", "fail") + "You failed to enchant {itemName}. You may try again in {cooldown}";
    public static string MSG_ENCHANT_NOT_AVAILABLE = Meta("enchanting", "fail", "no clan") + "You need to be part of a clan to use enchanting.";
    public static string MSG_ENCHANT_NOT_ENCHANTABLE = Meta("enchanting", "fail", "not enchantable") + "{itemName} can't be enchanted. Only weapon, armor, amulet and rings can be enchanted.";
    public static string MSG_ENCHANT_SUCCESS = Meta("enchanting", "info") + "You have successfully enchanted {oldItemName} into {newItemName}. You must now wait {cooldown} before you can enchant something again.";
    public static string MSG_ENCHANT_REPLACE = Meta("enchanting", "info") + "You have successfully replaced the enchantment on {oldItemName}. It has now  become {newItemName}. You must now wait {cooldown} before you can enchant something again.";
    public static string MSG_ENCHANT_WARN_REPLACE = Meta("enchanting", "info") + "{itemName} is already enchanted with {stats}. If you want to replace the enchantment, use !enchant replace {itemName}";
    public static string MSG_ENCHANT_STATS = Meta("enchanting", "info") + "{enchantmentStats} was added to {enchantedItemName}";

    public static string MSG_DISENCHANT_NO_ITEM = Meta("enchanting", "fail") + "It seem like you are trying to use '!disenchant' but you don't seem to have recently enchanted an item. Try using !disenchant (item name) instead.";
    public static string MSG_DISENCHANT_NOT_ENCHANTED = Meta("enchanting", "fail") + "You cannot disenchant {itemName} as it is not enchanted.";
    public static string MSG_DISENCHANT_UNKNOWN_ERROR = Meta("enchanting", "fail") + "Disenchanting {itemName} failed. Please try again later.";
    public static string MSG_DISENCHANT_SUCCESS = Meta("enchanting", "info") + "You have successfully removed the enchantment from {oldItemName}.";
    public static string MSG_ENCHANT_COST_NO_REQ = Meta("enchanting", "info") + "There are currently no requirements for enchanting {itemName}.";

    public static string MSG_UNEQUIPPED = Meta("info", "equipment", "unequip") + "You have unequipped {itemName}.";
    public static string MSG_UNEQUIPPED_ALL = Meta("info", "equipment", "unequip") + "You have unequipped all of your items.";
    public static string MSG_SET_PET = Meta("info", "equipment", "pet") + "You have changed your active pet to {itemName}";
    public static string MSG_ITEM_NOT_FOUND = Meta("info", "fail") + "Could not find an item matching the name: {query}";
    public static string MSG_ITEM_NOT_FOUND_SUGGEST = Meta("info", "fail") + "Could not find an item matching the name: {query}, did you mean {suggestion}?";
    public static string MSG_SET_PET_NOT_PET = Meta("info", "equipment", "pet", "fail") + "{itemName} is not a pet.";
    public static string MSG_SET_PET_NOT_OWNED = Meta("info", "equipment", "pet", "fail") + "You do not have any {itemName}.";
    public static string MSG_ITEM_NOT_EQUIPPED = Meta("info", "equipment", "unequip", "fail") + "You do not have {itemName} equipped.";



    // todo add more meta and review the ones added above.

    public static string MSG_ITEM_NOT_OWNED = "You do not have any {itemName}.";
    public static string MSG_GET_PET_NO_PET = "You do not have any pets equipped.";
    public static string MSG_GET_PET = "You currently have {petName} equipped.";
    public static string MSG_APPEARANCE_INVALID = "Invalid appearance data";
    public static string MSG_TRAVEL_ALREADY_ON_ISLAND = "You cannot travel to the island you're already on.";
    public static string MSG_TRAVEL_NO_SUCH_ISLAND = "No islands named '{islandName}'. You may travel to: '{islandList}'";
    public static string MSG_TRAVEL_NO_SUCH_PLAYER = "No player named '{islandName}'.";
    public static string MSG_TRAVEL_WAR = "You cannot travel when participating in a war. Please wait for it to be over.";
    public static string MSG_TRAVEL_DUEL = "You cannot travel when duelling another player.";
    public static string MSG_TRAVEL_ARENA = "You cannot travel when participating in the arena.";
    public static string MSG_TRAVEL_DUNGEON = "You cannot travel when in the dungeon.";
    public static string MSG_HIGHEST_SKILL_NO_PLAYERS = "Seems like no one is playing.";
    public static string MSG_HIGHEST_SKILL = "{player} has the highest level {skillName} with level {level}.";
    public static string MSG_HIGHEST_COMBAT = "{player} has the highest combat level with {combatLevel}.";
    public static string MSG_HIGHEST_TOTAL = "{player} has the highest total level with {level}.";

    public static string MSG_VILLAGE_BOOST = "Village is level {townHallLevel}, it needs {remainingExp} xp to level up. Active boosts {activeBoosts}";
    public static string MSG_VILLAGE_BOOST_NO_BOOST = "Village is level {townHallLevel}, it needs {remainingExp} xp to level up. Without any active boost.";

    public static string MSG_VILLAGE_UPDATED = "You've changed the village target to {targetType}, active boosts {activeBoosts}";
    public static string MSG_HIGHSCORE_RANK = "You're at rank #{rank}";
    public static string MSG_HIGHSCORE_MAIN_ONLY = "Only your main character is listed on the highscore right now.";
    public static string MSG_HIGHSCORE_BAD_SKILL = "{skillName} is not a skill in the game. Did you misspell it?";
    public static string MSG_MAX_MULTI = "Current Exp Multiplier is at {multiplier}.";
    public static string MSG_MAX_MULTI_ALL = "Current Exp Multiplier is at {multiplier}. Max is {maxMultiplier}. Currently {cheerPot} bits.";
    public static string MSG_MAX_MULTI_BITS = "Max exp multiplier is {maxMultiplier}. Currently {cheerPot} bits.";
    public static string MSG_REDEEM = "{itemName} was redeemed for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public static string MSG_REDEEM_EQUIP = "{itemName} was redeemed and equipped for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public static string MSG_REDEEM_FAILED = "{itemName} could not be redeemed. Please try again.";
    public static string MSG_REDEEM_INSUFFICIENT_TOKENS = "Not enough tokens to redeem this item. You have {tokenCount} but need {tokenCost}.";
    public static string MSG_REDEEM_NOT_REDEEMABLE = "This item can not be redeemed yet.";
    public static string MSG_REDEEM_ITEM_NOT_FOUND = "Could not find a redeemable item matching the query '{query}'";
    public static string MSG_REDEEM_ITEM_NOT_FOUND_SUGGEST = "No such redeemable item, did you mean {query}?";

    public static string MSG_SELL_TOO_MANY = Meta("marketplace", "sell", "fail") + "You cannot sell {itemAmount}x {itemName}. Max is {maxValue}.";
    public static string MSG_SELL_ITEM_NOT_FOUND = Meta("marketplace", "sell", "fail") + "Could not find an item matching the name: {query}";
    public static string MSG_SELL_ITEM_NOT_OWNED = Meta("marketplace", "sell", "fail") + "You do not have any {itemName} in your inventory.";
    public static string MSG_SELL_MARKETPLACE_ERROR = Meta("marketplace", "fail") + "Error accessing marketplace right now.";
    public static string MSG_ITEM_SOULBOUND = Meta("marketplace", "sell", "soulbound", "fail") + "{itemName} is soulbound and cannot be sold to the marketplace or gifted to another player.";
    public static string MSG_ITEM_SELL_ENCHANTED = Meta("marketplace", "sell", "soulbound", "fail") + "{itemName} is enchanted and cannot be sold to the marketplace or gifted to another player.";

    public static string MSG_SELL = Meta("marketplace", "sell", "info") + "{itemAmount}x {itemName} was put in the marketplace listing for {itemPrice} per item.";
    public static string MSG_VALUE_ITEM = Meta("info", "item", "value") + "{itemName} can be sold for {vendorPrice} using !vendor";
    public static string MSG_VALUE_ITEM_NOT_FOUND = Meta("info", "item", "value", "fail") + "Could not find an item matching the name: {query}";
    public static string MSG_VENDOR_ITEM = Meta("vendor", "info", "item") + "You sold {vendorCount}x {itemName} to the vendor for {cost} coins!";
    public static string MSG_VENDOR_ITEM_NOT_FOUND = Meta("vendor", "info", "item", "fail") + "Could not find an item matching the name: {query}";
    public static string MSG_VENDOR_ITEM_NOT_OWNED = Meta("vendor", "info", "item", "fail") + "You do not own a {itemName}.";
    public static string MSG_VENDOR_ITEM_FAILED = Meta("vendor", "info", "item", "fail") + "Error selling {vendorCount}x {itemName} to the vendor. FeelsBadMan";
    public static string MSG_VENDOR_MISSING_ARGS = Meta("vendor", "info", "fail") + "To use the vendor, you must supply an action and item. You can use: !vendor sell itemName, !vendor buy itemName and !vendor value itemName";
    public static string MSG_VENDOR_MISSING_ACTION = Meta("vendor", "info", "fail") + "Missing required vendor action, if you inteded to sell the item to the vendor, please use !vendor sell {query}";

    public static string MSG_MARKET_MISSING_ARGS = Meta("marketplace", "info", "fail") + "To use the marketplace, you must supply an action and item. You can use: !market sell itemName, !market buy itemName and !market value itemName";
    public static string MSG_MARKET_MISSING_ACTION = Meta("marketplace", "info", "fail") + "Missing required market action, if you inteded to put up an item for sale on the market, please use !market sell {query}";

    public static string MSG_DUNGEON_START_FAILED_WAR = Meta("dungeon", "start", "fail", "war") + "Unable to start a dungeon during a war. Please wait for it to be over.";
    public static string MSG_DUNGEON_START_FAILED_RAID = Meta("dungeon", "start", "fail", "raid") + "Unable to start a dungeon during a raid. Please wait for it to be over.";
    public static string MSG_TICTACTOE_PLAY = Meta("minigame", "tic tac toe", "info") + "Use !ttt 1-9 to play!";
    public static string MSG_TICTACTOE_STARTED = Meta("minigame", "tic tac toe", "info") + "A game of Tic Tac Toe has started! Use !tictactoe 1-9 to play!";
    public static string MSG_TICTACTOE_JOINED = Meta("minigame", "tic tac toe", "info") + "You have joined team {teamName}!";
    public static string MSG_TICTACTOE_REJECT_GAMEOVER = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because the game is over.";
    public static string MSG_TICTACTOE_REJECT_TURN = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected as it is not your turn yet.";
    public static string MSG_TICTACTOE_REJECT_NUMBER = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because the number was not part of the grid.";
    public static string MSG_TICTACTOE_REJECT_PLAYED = Meta("minigame", "tic tac toe", "fail") + "Your action was rejected because someone else already played this number.";
    public static string MSG_TICTACTOE_END_DRAW = Meta("minigame", "tic tac toe", "info", "result") + "The game ended in a draw. No one wins.";
    public static string MSG_TICTACTOE_WIN = Meta("minigame", "tic tac toe", "info", "result") + "Team {winningTeam} wins!";
    public static string MSG_TICTACTOE_WON_TOKEN = Meta("minigame", "tic tac toe", "info", "reward") + "You won a {tokenName}!";

    public static string MSG_DIAPER_ON = "Diaper mode has been enabled, you have unequipped all armor.";
    public static string MSG_DIAPER_OFF = "Diaper mode has been disabled, you have equipped your armor again.";
    public static string MSG_TOGGLE_PET_NO_PET = "You have no more pets to cycle between.";
    public static string MSG_TOGGLE_PET = "You have changed your active pet to {itemName}";

    public static string MSG_TRAINING_RECOMMENDED_ISLAND = Meta("training", "info") + "You have not been gaining any experience for more than {mins} minutes. You're recommended to sail to {recommendedIslandName} to continue training. The broadcaster may now also use !sail {recommendedIslandName} {playerName} to make your character move.";

    public static string MSG_TRAINING = "You're currently training {skill}.";
    public static string MSG_TRAINING_NOTHING = "You're not training anything. Use !train <skill name> to start training!";
    public static string MSG_TRAINING_SAILING = "You're currently sailing the seas. Use !disembark to leave the ferry or !travel <island name> to sail to a specific island.";
    public static string MSG_JOIN_RAID = "You have joined the raid. Good luck!";
    public static string MSG_JOIN_RAID_FERRY = "You cannot join the raid while on the ferry.";
    public static string MSG_JOIN_RAID_WAR = "You cannot fight a raid boss during a war!";
    public static string MSG_JOIN_RAID_DUNGEON = "You cannot fight a raid boss when participating in a dungeon!";
    public static string MSG_JOIN_RAID_ALREADY = "You have already joined the raid.";
    public static string MSG_JOIN_RAID_PAST_HEALTH = "You can no longer join the raid.";
    public static string MSG_JOIN_RAID_ARENA = "You cannot join the raid while in the arena.";
    public static string MSG_JOIN_RAID_DUEL = "You cannot join the raid while in a duel.";
    public static string MSG_JOIN_RAID_NO_RAID = "There are no active raids at the moment.";

    public static string MSG_ALREADY_IN_CLAN = "You cannot join a clan as you're already in one. Please leave your clan first using '!clan leave'";
    public static string MSG_NOT_IN_CLAN = "You need to join a clan to use this command.";
    public static string MSG_CLAN_NOT_FOUND = "Could not find a suitable clan using '{clanSearch}'. There must be atleast one player in the game with the target clan.";
    public static string MSG_CLAN_INFO_UNKNOWN_ERROR = "Unable to get clan info at this time, please try again later.";
    public static string MSG_CLAN_STATS_UNKNOWN_ERROR = "Unable to get clan stats at this time, please try again later.";

    public static string MSG_MARKET_ITEM_UNAVAILABLE = "{itemName} is not available on the marketplace.";
    public static string MSG_MARKET_VALUE_COUNT = "There are currently {available} {itemName} sold on the marketplace. Cheapest can be bought for {minPrice}, max price is {maxPrice}, average price is {avgPrice}, to buy {amount} it would cost roughly {cost} coins";
    public static string MSG_MARKET_VALUE = "There are currently {available} {itemName} sold on the marketplace. Cheapest can be bought for {minPrice}, max price is {maxPrice}, average price is {avgPrice}";

    // ---- On screen notifications -------------------------------------------------------------
    // These were previously baked into PNG images (ArenaIsAboutToStart.png and friends), which
    // meant the wording could not be translated and every style change needed the art redrawn.
    // Kept here with the rest of the strings so the localization pass has one place to work.
    //
    // {command} is substituted with the chat command and highlighted by the UI, preserving the
    // red highlight the original images used to show viewers what to type.

    public static string UI_ARENA_ABOUT_TO_START = "Arena is about to start";
    public static string UI_ARENA_NOW_ACTIVE = "Arena is now active";
    public static string UI_ARENA_JOIN = "Type {command} to join";
    public static string UI_ARENA_STARTED = "May the best fighter win!";
    public static string UI_ARENA_DRAW_TITLE = "Draw";
    public static string UI_ARENA_DRAW_MESSAGE = "The arena ended in a Draw, everyone is rewarded a small gift";
    public static string UI_ARENA_WINNER = "Congratulations for winning the arena!";

    public static string UI_RAID_BOSS_APPEARED = "A boss appeared!!";
    public static string UI_RAID_TITLE = "RAID BOSS";
    public static string UI_RAID_LEVEL = "LV {0}";
    public static string UI_RAID_RAIDERS = "{0} RAIDERS";
    public static string UI_RAID_RAIDER_ONE = "1 RAIDER";
    public static string UI_XP_BOOST = "XP BOOST \u00d7{0}";
    public static string UI_XP_BOOST_THANKS = "Thanks to {0}";
    public static string UI_PLAYERS_ONE = "1 player";
    public static string UI_PLAYERS_COUNT = "{0} players";
    public static string UI_PLAYERS_PARTIAL = "{0} of {1} players";
    public static string UI_RESTED = "Rested {0}";
    public static string UI_ISLAND_TRAINING = "TRAINING HERE";
    public static string UI_ISLAND_STATISTICS = "ISLAND STATISTICS";
    public static string UI_ISLAND_NOBODY = "Nobody is training here right now.";
    public static string UI_ISLAND_ONSEN = "Onsen";
    public static string UI_ISLAND_LEVEL = "Level {0}+";
    public static string UI_ENEMY_LEVEL = "Combat level {0}";
    public static string UI_ENEMY_KILLS = "{0} players defeated";
    public static string UI_ENEMY_DEATHS = "{0} deaths";
    public static string UI_TOWN_BUILD = "BUILD MODE";
    public static string UI_TOWN_BUILD_EXIT = "EXIT BUILD";
    public static string UI_BUILD_SUMMARY_TITLE = "Village bonuses";
    public static string UI_BUILD_SUMMARY_PLOTS = "{0} of {1} plots built";
    public static string UI_BUILD_SUMMARY_EMPTY = "No bonuses yet. Build houses and give them residents to boost their skills.";
    public static string UI_BUILD_EMPTY_PLOT = "Empty plot";
    public static string UI_BUILD_PLOT_NUMBER = "Plot {0}";
    public static string UI_BUILD_EMPTY_PLOT_NOTE = "Pick a building in the tray below to build it here.";
    public static string UI_BUILD_RESIDENT = "RESIDENT";
    public static string UI_BUILD_NOBODY = "Nobody lives here yet";
    public static string UI_BUILD_UNKNOWN_RESIDENT = "Away";
    public static string UI_BUILD_BONUS = "+{0}% {1} XP";
    public static string UI_BUILD_CARD_BONUS = "Up to +{0}% {1}";
    public static string UI_BUILD_NO_RESIDENT_NOTE = "Assign a resident to switch on this building's bonus.";
    public static string UI_BUILD_RESIDENT_AWAY_NOTE = "The resident is not in the game right now, so the bonus is paused.";
    public static string UI_BUILD_ASSIGN = "Assign resident";
    public static string UI_BUILD_CHANGE_RESIDENT = "Change resident";
    public static string UI_BUILD_DEMOLISH = "Demolish";
    public static string UI_BUILD_DEMOLISH_CONFIRM = "Click again";
    public static string UI_BUILD_ASSIGN_TITLE = "Choose a resident for the {0}";
    public static string UI_BUILD_ASSIGN_CURRENT = "Currently: {0}";
    public static string UI_BUILD_LIVES_IN = "lives in plot {0}";
    public static string UI_BUILD_NO_PLAYERS = "No players in the game to choose from.";
    public static string UI_BUILD_HINT_IDLE = "Click a plot to select it  ·  Pick a building to place it  ·  Esc to exit";
    public static string UI_BUILD_HINT_ARMED = "Click an empty plot or any free spot to build the {0}  ·  R to rotate  ·  Esc to cancel";
    public static string UI_BUILD_HINT_EMPTY = "Pick a building below to build it here";
    public static string UI_BUILD_HINT_BUILT = "Pick a building below to replace this one";
    public static string UI_BUILD_HINT_BUSY = "Working on it...";
    public static string UI_BUILD_HINT_MOVING = "Move the mouse to place  ·  R to rotate (Shift back, Alt 90°)  ·  Click to place  ·  Esc to cancel";
    public static string UI_BUILD_HINT_MOVING_BLOCKED = "{0}  ·  R to rotate  ·  Esc to cancel";
    public static string UI_BUILD_MOVE = "Move";
    public static string UI_BUILD_ROTATE = "Rotate";
    public static string UI_BUILD_SEARCH = "Search players...";
    public static string UI_BUILD_TRAY_TITLE = "{0} BUILDINGS";
    public static string UI_BUILD_BUILD = "BUILD";
    public static string UI_BUILD_SUMMARY_CONTRIBUTING = "{0} buildings contributing";
    public static string UI_BUILD_ONE_BUILDING = "1 building";
    public static string UI_BUILD_BUILDINGS = "{0} buildings";
    public static string UI_BUILD_CHANGE_RESIDENT_SHORT = "Change resident";
    public static string UI_BUILD_SKILL_LEVEL = "{0} level {1}";
    public static string UI_BUILD_PLOTS_FREE = "{0} of {1} plots free";
    public static string UI_BUILD_ASSIGN_BUTTON = "Assign";
    public static string UI_BUILD_TAG_CURRENT = "CURRENT";
    public static string UI_BUILD_TAG_BEST = "BEST";
    public static string UI_BUILD_RESET_POSITION = "Reset position";
    public static string UI_BUILD_PLACE_OUTSIDE = "Too far from the town hall";
    public static string UI_BUILD_PLACE_TOWN_HALL = "Overlaps the town hall";
    public static string UI_BUILD_NO_FREE_PLOTS = "No free plots. Demolish a building, or level the village up for more.";
    public static string UI_BUILD_PLACE_OVERLAP = "Too close to another building";
    public static string UI_BUILD_BLOCKED_BY = "In the way: {0}";
    public static string UI_BUILD_BLOCKED_MORE = "and {0} more";

    // Roads, drawn in build mode
    public static string UI_BUILD_CATEGORY_ROADS = "Roads";
    public static string UI_ROAD_DIRT = "Dirt road";
    public static string UI_ROAD_GRAVEL = "Gravel road";
    public static string UI_ROAD_COBBLES = "Cobbles";
    public static string UI_ROAD_DRAW = "Draw";
    public static string UI_ROAD_PER_METRE = "{0} a metre";
    public static string UI_ROAD_LOCKED = "Needs town hall tier {0}";
    public static string UI_ROAD_LENGTH = "{0} m long";
    public static string UI_ROAD_TYPE = "TYPE";
    public static string UI_ROAD_FLAGGED = "Runs through a building. Move a point or the building; nothing is moved for you.";
    public static string UI_ROAD_EDIT_NOTE = "Drag a point to move it. Ctrl click the road to add a point, right click a point to take it away. Delete removes the road.";
    public static string UI_ROAD_REMOVE = "Remove road (half the cost back)";
    public static string UI_ROAD_REMOVE_CONFIRM = "Click again to remove";
    public static string UI_ROAD_NONE = "Roads are not available on this server yet.";
    public static string UI_ROAD_HINT_START = "Click to place the first point of the road. Esc to stop.";
    public static string UI_ROAD_HINT_DRAWING = "{0} m, costs {1}. Click to add points, Enter or right click to finish, Backspace takes one back. Alt places freely, Shift keeps to 15 degree turns.";
    public static string UI_ROAD_HINT_TOO_DEAR = "{0} m would cost {1}, more than the village has. Backspace takes a point back.";
    public static string UI_ROAD_HINT_SELECTED = "Drag the points to reshape the road. Esc to deselect.";
    public static string UI_ROAD_COST_WOOD = "{0} wood";
    public static string UI_ROAD_COST_ORE = "{0} ore";
    public static string UI_ROAD_COST_FISH = "{0} fish";
    public static string UI_ROAD_COST_WHEAT = "{0} wheat";
    public static string UI_ROAD_COST_COINS = "{0} coins";
    public static string UI_ROAD_FREE = "nothing";
    public static string UI_ROAD_TWO_POINTS = "A road needs at least two points.";
    public static string UI_ROAD_ERROR_RESOURCES = "The village does not have the resources for that.";
    public static string UI_ROAD_ERROR_LOCKED = "That road type is not unlocked yet.";
    public static string UI_ROAD_ERROR_INVALID = "That road cannot be built: too short, too long or with too many points.";
    public static string UI_ROAD_ERROR_UNKNOWN = "The road could not be saved. Try again.";
    public static string UI_BUILD_PLACE_WATER = "You can't build on water";
    public static string UI_BUILD_PLACE_STEEP = "The ground is too steep here";
    public static string UI_BUILD_PLACE_REJECTED = "The server did not accept the move. Try again in a moment.";
    public static string UI_BUILD_PLACE_UNSUPPORTED = "Moving buildings needs a server update.";
    public static string UI_BUILD_HALL_TITLE = "Town hall";
    public static string UI_BUILD_HALL_TIER = "Tier {0}";
    public static string UI_BUILD_HALL_NOTE = "Move the town hall anywhere in the village. Its outline is the room the biggest town hall needs, so there is always space to upgrade.";
    public static string UI_BUILD_HALL_PLOT = "The town hall needs this space";
    public static string UI_BUILD_HALL_OUTSIDE = "The town hall must stay inside the village";
    public static string UI_BUILD_HINT_HALL = "Move, rotate or upgrade the town hall. Esc to deselect.";
    public static string UI_BUILD_HALL_TIER_NAMED = "Tier {0}: {1}";
    public static string UI_BUILD_HALL_NEXT = "NEXT: {0}";
    public static string UI_BUILD_HALL_MAX = "This is the grandest town hall there is.";
    public static string UI_BUILD_HALL_UPGRADE = "Upgrade";
    public static string UI_SHORTCUT_MENU = "Menu (Esc)";
    public static string UI_TOWN_UPGRADE_READY = "A town hall upgrade is ready. Open build mode and select the town hall.";
    public static string UI_BUILD_HALL_UPGRADE_FAILED = "The upgrade did not go through. Check the requirements and try again.";
    public static string UI_BUILD_HALL_NEED_LEVEL = "Village level {0}";
    public static string UI_BUILD_HALL_NEED = "{0} {1} / {2}";
    public static string[] UI_BUILD_HALL_NAMES = { "Longhouse", "Town hall", "Guild hall", "Keep", "Castle" };
    public static string UI_TOWN_VILLAGE = "VILLAGE";
    public static string UI_TOWN_LEVEL = "LV {0}";
    public static string UI_TOWN_HOUSES = "HOUSES";
    public static string UI_TOWN_WOOD = "WOOD";
    public static string UI_TOWN_ORE = "ORE";
    public static string UI_TOWN_FISH = "FISH";
    public static string UI_TOWN_WHEAT = "WHEAT";
    public static string UI_TOWN_COINS = "COINS";
    public static string UI_DUNGEON_TITLE = "DUNGEON";
    public static string UI_DUNGEON_ROOM = "ROOM {0} / {1}";
    public static string UI_DUNGEON_BOSS_ROOM = "BOSS ROOM";
    public static string UI_DUNGEON_JOINED = "{0} JOINED";
    public static string UI_DUNGEON_ALIVE = "{0} / {1} ALIVE";
    public static string UI_DUNGEON_ENEMIES = "{0} ENEMIES";
    public static string UI_DUNGEON_ENEMY_ONE = "1 ENEMY";
    public static string UI_METER_DAMAGE = "Damage Done";
    public static string UI_METER_HEALING = "Healing Done";
    public static string UI_METER_TAKEN = "Damage Taken";
    public static string UI_METER_GROUP_TOTAL = "GROUP TOTAL";
    public static string UI_METER_EMPTY = "Nothing recorded yet.";
    public static string UI_METER_RESULT = "RESULT";
    public static string UI_DETAILS_TRAINING = "TRAINING";
    public static string UI_DETAILS_IDLE = "IDLE";
    public static string UI_DETAILS_IDLE_HINT = "Not training anything. Type {command} in chat to start.";
    public static string UI_DETAILS_RESTING = "RESTING";
    public static string UI_DETAILS_FULLY_RESTED_IN = "Fully rested in {0}";
    public static string UI_DETAILS_FULLY_RESTED = "Fully rested";
    public static string UI_DETAILS_RESTED_OF = "{0} of {1}";
    public static string UI_METER_CLOSING = "Closes in {0}s";
    public static string UI_DETAILS_NEXT_LEVEL = "Next level in {0}";
    public static string UI_DETAILS_TARGET_REACHED = "Target level {0} reached";
    public static string UI_DETAILS_MAX_LEVEL = "Max level";
    public static string UI_DETAILS_LEVEL_TOO_HIGH = "Level too high for this island";
    public static string UI_DETAILS_LEVEL_TOO_LOW = "Level too low for this island";
    public static string UI_DETAILS_INELIGIBLE = "Not able to train here";
    public static string UI_DETAILS_RECOMMENDED_TITLE = "Recommended island";
    public static string UI_DETAILS_RECOMMENDED = "Recommended island: {0}";
    public static string UI_DETAILS_CLAN_SKILL_LOCKED = "A clan skill. It shows here once the player's clan has unlocked it.";
    public static string UI_DETAILS_EMPTY_SLOT = "Nothing equipped.";
    public static string UI_DETAILS_TRANSMOGRIFIED = "Transmogrified";
    public static string UI_RAID_JOIN = "Type {command} to join";
    public static string UI_RAID_REWARD_HINT = "To get a chance of being rewarded items!";
    public static string UI_MENU_CONTINUE = "Continue";
    public static string UI_MENU_SETTINGS = "Settings";
    public static string UI_MENU_LOGOUT = "Log out";
    public static string UI_MENU_EXIT = "Exit game";
    public static string UI_MENU_CONFIRM_LOGOUT = "Click again to log out";
    public static string UI_MENU_CONFIRM_EXIT = "Click again to exit";
    public static string UI_MENU_LOG_FOLDER = "Open log folder";
    public static string[] UI_MENU_INFO = { "Version", "Graphics", "Graphics memory", "System memory", "System", "Uptime" };
    public static string[] UI_MENU_TIPS =
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

    public static string UI_SETTINGS_TITLE = "SETTINGS";
    public static string UI_SETTINGS_FOOTER = "Changes apply right away and are saved when you close settings.";
    public static string UI_SETTINGS_DONE = "Done";
    public static string UI_SETTINGS_TAB_GAME = "Game";
    public static string UI_SETTINGS_TAB_GRAPHICS = "Graphics";
    public static string UI_SETTINGS_TAB_SOUND = "Sound";
    public static string UI_SETTINGS_TAB_INTERFACE = "Interface";
    public static string UI_SETTINGS_TAB_BOT = "Bot";
    public static string UI_SETTINGS_TAB_ADMIN = "Admin";
    public static string UI_KEY_HUD = "Hide or show the interface";
    public static string UI_KEY_DETAILS = "Player details";
    public static string UI_KEY_NEXT_PLAYER = "Watch the next player";
    public static string UI_KEY_FREE_CAMERA = "Free camera (hold)";
    public static string UI_KEY_NEXT_ISLAND = "Watch the next island";
    public static string UI_KEY_CONTROL_PLAYER = "Take control of the watched player";
    public static string UI_KEY_SEARCH = "Find a player";
    public static string UI_KEY_EXP = "Player list: switch what each row shows";
    public static string UI_KEY_LOGOS = "Hide logos";
    public static string UI_KEY_UI_BIGGER = "Bigger interface";
    public static string UI_KEY_UI_SMALLER = "Smaller interface";
    public static string UI_KEY_GRAPHICS = "Turn graphics off or on";
    public static string UI_KEY_PHYSICS = "Turn physics on or off";
    public static string UI_KEY_SAVE = "Save the players now";
    public static string UI_KEY_RELOAD = "Reload the game";
    public static string UI_KEY_RAID = "Start a raid";
    public static string UI_KEY_DUNGEON = "Start a dungeon";
    public static string UI_SETTINGS_TAB_KEYS = "Keys";
    public static string UI_SETTINGS_KEYS_HINT = "Click a key to change it, then press the new one. Esc cancels, Backspace leaves it unbound. A key already in use swaps over.";
    public static string UI_SETTINGS_KEYS_PRESS = "Press a key";
    public static string UI_SETTINGS_KEYS_SWAPPED = "Swapped with {0}";
    public static string UI_SETTINGS_KEYS_FIXED = "Esc always opens the menu and backs out of windows, and the free camera flies with WASD, Q and E.";
    public static string UI_SETTINGS_SECTION_KEYS_CAMERA = "CAMERA";
    public static string UI_SETTINGS_SECTION_KEYS_INTERFACE = "INTERFACE";
    public static string UI_SETTINGS_SECTION_KEYS_GAME = "GAME";
    public static string UI_SETTINGS_SECTION_KEYS_ADMIN = "ADMIN";
    public static string UI_SETTINGS_SECTION_DISPLAY = "DISPLAY";
    public static string UI_SETTINGS_WINDOW_MODE = "Window mode";
    public static string UI_SETTINGS_WINDOW_MODE_HINT = "Borderless fills the screen and is the quickest to switch away from while streaming.";
    public static string[] UI_SETTINGS_WINDOW_MODE_OPTIONS = { "Borderless", "Fullscreen", "Windowed" };
    public static string UI_SETTINGS_SCREEN_SIZE = "Resolution";
    public static string UI_SETTINGS_SCREEN_NATIVE = "{0} x {1} (native)";
    public static string UI_SETTINGS_SCREEN_SIZE_VALUE = "{0} x {1}";
    public static string UI_SETTINGS_VSYNC = "Vertical sync";
    public static string UI_SETTINGS_VSYNC_HINT = "Matches the monitor's refresh rate and stops tearing. The frame rate limit is not used while it is on.";
    public static string UI_SETTINGS_FPS = "Frame rate limit";
    public static string UI_SETTINGS_FPS_HINT = "A lower limit saves power and keeps the computer cool while streaming.";
    public static string[] UI_SETTINGS_FPS_OPTIONS = { "No limit", "30", "60", "90", "120", "144", "165", "240" };
    public static string UI_SETTINGS_MASTER = "Everything";
    public static string UI_SETTINGS_MASTER_HINT = "All of the game's sound at once.";
    public static string UI_SETTINGS_SECTION_FREE_CAMERA = "FREE CAMERA AND ZOOM";
    public static string UI_SETTINGS_FREE_SPEED = "Flying speed";
    public static string UI_SETTINGS_FREE_SPEED_HINT = "Hold Shift to fly five times faster and Alt to go slower. Keypad + and - still adjust it while flying.";
    public static string UI_SETTINGS_LOOK = "Mouse look";
    public static string UI_SETTINGS_LOOK_HINT = "Turning speed while holding the right mouse button or Ctrl in the free camera.";
    public static string UI_SETTINGS_INVERT_LOOK = "Invert mouse look";
    public static string UI_SETTINGS_ZOOM = "Scroll zoom";
    public static string UI_SETTINGS_ZOOM_HINT = "How far one turn of the wheel zooms the player and island cameras.";
    public static string UI_SETTINGS_INVERT_ZOOM = "Invert scroll zoom";
    public static string UI_SETTINGS_TIMES = "x{0}";
    public static string UI_SETTINGS_SECTION_CONNECTION = "CONNECTION";
    public static string UI_SETTINGS_BOT_SERVER = "RavenBot server";
    public static string UI_SETTINGS_BOT_SERVER_HINT = "As host:port. Leave it empty for the default.";
    public static string UI_SETTINGS_LOCAL_BOT = "Accept a bot on this computer";
    public static string UI_SETTINGS_LOCAL_BOT_HINT = "For streamers who run RavenBot themselves.";
    public static string UI_SETTINGS_LOCAL_PORT = "Local bot port";
    public static string UI_SETTINGS_SECTION_OVERLAYS = "STREAM OVERLAYS";
    public static string UI_SETTINGS_QUERY = "Query engine";
    public static string UI_SETTINGS_QUERY_HINT = "A local web API with the game's data, for overlays and tools.";
    public static string UI_SETTINGS_QUERY_ADDRESS = "Query engine address";
    public static string UI_SETTINGS_RELOAD_NOTE = "Applies when the game reloads.";
    public static string UI_SETTINGS_RELOAD_BUTTON = "Reload now";
    public static string UI_SETTINGS_EXP_TIMES = "Warn when this much is left";
    public static string[] UI_SETTINGS_EXP_TIME_OPTIONS = { "10 min", "5 min", "3 min", "1 min", "30 s", "When it ends" };
    public static string UI_SKILL_ATTACK = "Attack";
    public static string UI_SKILL_DEFENSE = "Defense";
    public static string UI_SKILL_STRENGTH = "Strength";
    public static string UI_SKILL_HEALTH = "Health";
    public static string UI_SKILL_WOODCUTTING = "Woodcutting";
    public static string UI_SKILL_FISHING = "Fishing";
    public static string UI_SKILL_MINING = "Mining";
    public static string UI_SKILL_CRAFTING = "Crafting";
    public static string UI_SKILL_COOKING = "Cooking";
    public static string UI_SKILL_FARMING = "Farming";
    public static string UI_SKILL_SLAYER = "Slayer";
    public static string UI_SKILL_MAGIC = "Magic";
    public static string UI_SKILL_RANGED = "Ranged";
    public static string UI_SKILL_SAILING = "Sailing";
    public static string UI_SKILL_HEALING = "Healing";
    public static string UI_SKILL_GATHERING = "Gathering";
    public static string UI_SKILL_ALCHEMY = "Alchemy";
    public static string UI_SKILL_MELEE = "Melee";
    public static string UI_SKILL_ALL = "All";
    public static string UI_SKILL_ENCHANTING = "Enchanting";
    public static string UI_SLOT_HEAD = "Helmet";
    public static string UI_SLOT_AMULET = "Amulet";
    public static string UI_SLOT_RING = "Ring";
    public static string UI_SLOT_CHEST = "Chest";
    public static string UI_SLOT_GLOVES = "Gloves";
    public static string UI_SLOT_BELT = "Belt";
    public static string UI_SLOT_LEGGINGS = "Leggings";
    public static string UI_SLOT_BOOTS = "Boots";
    public static string UI_SLOT_MELEEWEAPON = "Melee weapon";
    public static string UI_SLOT_MAGICWEAPON = "Magic weapon";
    public static string UI_SLOT_RANGEDWEAPON = "Ranged weapon";
    public static string UI_SLOT_SHIELD = "Shield";
    public static string UI_SLOT_PET = "Pet";
    public static string UI_SLOT_CAPE = "Cape";
    public static string UI_HOUSE_WOODCUTTING = "Lumbermill";
    public static string UI_HOUSE_MAGIC = "Magic house";
    public static string UI_HOUSE_RANGED = "Archer range";
    public static string UI_HOUSE_MELEE = "Barracks";
    public static string UI_HOUSE_COOKING = "Cooking house";
    public static string UI_HOUSE_CRAFTING = "Crafting house";
    public static string UI_HOUSE_FARMING = "Farmer's house";
    public static string UI_HOUSE_MINING = "Mining shelter";
    public static string UI_HOUSE_SAILING = "Sailor's log";
    public static string UI_HOUSE_SLAYER = "Slayers' guild";
    public static string UI_HOUSE_FISHING = "Fisher's log";
    public static string UI_HOUSE_HEALING = "Healing house";
    public static string UI_HOUSE_GATHERING = "Gathering hut";
    public static string UI_HOUSE_ALCHEMY = "Alchemist's house";
    public static string UI_TIME_UNKNOWN = "Unknown";
    public static string UI_TIME_NEVER = "When hell freezes over";
    public static string UI_TIME_UNREASONABLE = "Unreasonably long";
    public static string UI_TIME_TOO_LONG = "Way too long";
    public static string UI_TIME_WEEKS = "{0} weeks";
    public static string UI_TIME_DAYS = "{0}d {1}h";
    public static string UI_TIME_HOURS = "{0}h {1}m";
    public static string UI_TIME_MINUTES = "{0}m {1}s";
    public static string UI_TIME_SECONDS = "{0}s";
    public static string UI_RATE_PER_HOUR = "{0} xp/h";
    public static string UI_RATE_LEFT = "{0} xp left";
    public static string UI_TOWN_RATE = "{0}/h";
    public static string UI_PLAYERLIST_TITLE = "PLAYERS";
    public static string UI_PLAYERLIST_LEVEL = "LV";
    public static string UI_PLAYERLIST_IDLE = "Idle";
    public static string UI_DETAILS_LEVEL_CAPTION = "LV";
    public static string UI_DETAILS_EQUIPMENT = "EQUIPMENT";
    public static string UI_BADGE_BROADCASTER = "Streamer";
    public static string UI_BADGE_MODERATOR = "Moderator";
    public static string UI_BADGE_SUBSCRIBER = "Subscriber";
    public static string UI_BADGE_VIP = "VIP";
    public static string UI_BADGE_DEV = "Developer";
    public static string UI_DETAILS_STAT_ARMOR = "ARMOR";
    public static string UI_DETAILS_STAT_MELEE = "MELEE";
    public static string UI_DETAILS_STAT_RANGED = "RANGED";
    public static string UI_DETAILS_STAT_MAGIC = "MAGIC";
    public static string UI_DETAILS_SCROLL_EXP = "Exp scrolls";
    public static string UI_DETAILS_SCROLL_RAID = "Raid scrolls";
    public static string UI_DETAILS_SCROLL_DUNGEON = "Dungeon scrolls";
    public static string UI_DETAILS_OWNED = "Owned";
    public static string UI_DETAILS_AIM = "Aim {0}";
    public static string UI_TIP_LEVEL = "Level";
    public static string UI_TIP_BOOSTED = "Boosted to";
    public static string UI_TIP_DRAINED = "Drained to";
    public static string UI_TIP_PROGRESS = "Progress";
    public static string UI_TIP_EXPERIENCE = "Experience";
    public static string UI_TIP_RATE = "Rate";
    public static string UI_TIP_NEXT_LEVEL = "Next level";
    public static string UI_TIP_PET = "Aww! What a cute little pet!";
    public static string UI_TIP_ARMOR = "Armor";
    public static string UI_TIP_ARMOR_POWER = "Armor power";
    public static string UI_TIP_ARMOR_NOTE = "Reduces damage taken from every attack style.";
    public static string UI_TIP_MELEE = "Melee";
    public static string UI_TIP_WEAPON_POWER = "Weapon power";
    public static string UI_TIP_WEAPON_AIM = "Weapon aim";
    public static string UI_TIP_RANGED = "Ranged";
    public static string UI_TIP_RANGED_POWER = "Ranged power";
    public static string UI_TIP_RANGED_AIM = "Ranged aim";
    public static string UI_TIP_MAGIC = "Magic";
    public static string UI_TIP_MAGIC_POWER = "Magic power";
    public static string UI_TIP_MAGIC_AIM = "Magic aim";
    public static string UI_TIP_HEALING_POWER = "Healing power";
    public static string UI_ISLAND_STAT_MONSTERS = "Monsters defeated";
    public static string UI_ISLAND_STAT_PLAYERS_KILLED = "Players killed";
    public static string UI_ISLAND_STAT_RAID_BOSSES = "Raid bosses";
    public static string UI_ISLAND_STAT_ITEMS_GATHERED = "Items gathered";
    public static string UI_ISLAND_STAT_TREES = "Trees cut down";
    public static string UI_ISLAND_STAT_ROCKS = "Rocks mined";
    public static string UI_ISLAND_STAT_FISH = "Fish caught";
    public static string UI_ISLAND_STAT_CROPS = "Crops harvested";
    public static string UI_ISLAND_STAT_FOOD = "Food cooked";
    public static string UI_ISLAND_STAT_CRAFTED = "Items crafted";
    public static string UI_ISLAND_STAT_POTIONS = "Potions brewed";
    public static string UI_ENEMY_ATTACK = "ATTACK";
    public static string UI_ENEMY_DEFENSE = "DEFENSE";
    public static string UI_ENEMY_STRENGTH = "STRENGTH";
    public static string UI_ENEMY_ARMOR = "ARMOR";
    public static string UI_ENEMY_POWER = "POWER";
    public static string UI_ENEMY_AIM = "AIM";
    public static string UI_FLOAT_MISS = "Miss";
    public static string UI_BUILD_CATEGORY_COMBAT = "Combat";
    public static string UI_BUILD_CATEGORY_GATHERING = "Gathering";
    public static string UI_BUILD_CATEGORY_CRAFTING = "Crafting";
    public static string UI_METER_TITLE = "DUNGEON";
    public static string UI_METER_PLAYER = "PLAYER";
    public static string UI_METER_TOTAL = "TOTAL";
    public static string UI_METER_DPS = "DPS";
    public static string UI_METER_HPS = "HPS";
    public static string UI_METER_DTPS = "DTPS";
    public static string UI_COC_TITLE = "Code of Conduct";
    public static string UI_COC_VERSION = "Version {0}";
    public static string UI_COC_MODIFIED = "Last Modified {0}";
    public static string UI_COC_LOADING = "Loading the current code of conduct from the server...";
    public static string UI_COC_DECLINE = "Decline and Exit";
    public static string UI_COC_ACCEPT = "Accept";
    public static string UI_UPDATE_VERSION = "VERSION";
    public static string UI_UPDATE_CHECKING = "Checking for updates.";
    public static string UI_UPDATE_STARTING = "Starting game...";
    public static string UI_UPDATE_UP_TO_DATE = "Game is up to date. Starting game...";
    public static string UI_UPDATE_ERROR = "Error occurred when trying to update the game. \r\nPress space to open the log folder.";
    public static string UI_UPDATE_NO_CONNECTION = "Unable to connect to the server. Please check your internet connection.";
    public static string UI_UPDATE_DOWNLOADING = "Downloading update {0}";
    public static string UI_UPDATE_INITIALIZING = "Initializing update.";
    public static string UI_SETTINGS_RESET_PAGE = "Reset page";
    public static string UI_SETTINGS_LANGUAGE = "Language";
    public static string UI_SETTINGS_LANGUAGE_HINT = "For the interface and the bot's messages in your chat. A few windows switch over when the game reloads.";
    public static string UI_SETTINGS_OBSERVE_SUB_HINT = "When the camera picks a subscriber, it stays on them at least this long.";
    public static string UI_SETTINGS_OBSERVE_VIP_HINT = "When the camera picks a VIP, it stays on them at least this long.";
    public static string UI_SETTINGS_OBSERVE_MOD_HINT = "When the camera picks a moderator, it stays on them at least this long.";
    public static string UI_SETTINGS_OBSERVE_ISLAND_HINT = "How long the island camera shows each island before moving to the next.";
    public static string UI_SETTINGS_INVERT_LOOK_HINT = "Moving the mouse up looks down.";
    public static string UI_SETTINGS_INVERT_ZOOM_HINT = "Scrolling up zooms out.";
    public static string UI_SETTINGS_EXPIRY_ALERT_HINT = "Posts in chat when players are dropped because their saved state has run out.";
    public static string UI_SETTINGS_NO_RAIDS_HINT = "No raids start at all, neither on their own nor by chat command.";
    public static string UI_SETTINGS_NO_DUNGEONS_HINT = "No dungeons start at all, neither on their own nor by chat command.";
    public static string UI_SETTINGS_PHYSICS = "Physics";
    public static string UI_SETTINGS_PHYSICS_HINT = "Simulated physics in the world. Off saves CPU with many players. Shortcut: {0}.";
    public static string UI_SETTINGS_LABELS_TEXT_HINT = "One plain text file per value, the easiest to add as a text source in OBS.";
    public static string UI_SETTINGS_LABELS_JSON_HINT = "The same values as json, for overlays that read data.";
    public static string UI_SETTINGS_QUERY_ADDRESS_HINT = "Where the API listens, as host:port/path.";
    public static string UI_SETTINGS_SCREEN_SIZE_HINT = "The size the game draws at.";
    public static string UI_SETTINGS_QUALITY_HINT = "Lower levels draw fewer shadows and effects, for slower computers.";
    public static string UI_SETTINGS_DRAW_WORLD = "Draw the world";
    public static string UI_SETTINGS_DRAW_WORLD_HINT = "Off shows a black screen while the game keeps running, to save power when only chat matters. Shortcut: {0}.";
    public static string UI_SETTINGS_DAYNIGHT_HINT = "The sun moves and night falls. Off holds the time of day set below.";
    public static string UI_SETTINGS_VIEW_DISTANCE_HINT = "How far away the world is still drawn. Lower is faster.";
    public static string UI_SETTINGS_MUSIC_HINT = "The background music and the raid music.";
    public static string UI_SETTINGS_NAMES_HINT = "The name tags above characters in the world.";
    public static string UI_SETTINGS_PLAYER_LIST_HINT = "The list of everyone playing. {0} also hides it, with the rest of the interface.";
    public static string UI_SETTINGS_EXP_DISPLAY = "Player list shows";
    public static string UI_SETTINGS_EXP_DISPLAY_HINT = "What each row shows on its right. {0} switches between them.";
    public static string[] UI_SETTINGS_EXP_DISPLAY_OPTIONS = { "Exp per hour", "Exp left", "Time to level", "Nothing" };
    public static string UI_SETTINGS_HIDE_LOGOS = "Hide logos";
    public static string UI_SETTINGS_HIDE_LOGOS_HINT = "Player and clan logos show a plain image instead, everywhere. Shortcut: {0}.";
    public static string UI_SETTINGS_BOT_EXP_HINT = "A chat warning before an exp multiplier runs out.";
    public static string UI_SETTINGS_EXP_TIMES_HINT = "Pick as many as you like.";
    public static string UI_SETTINGS_BOT_WELCOME_HINT = "A welcome when someone joins the game, and a goodbye when they leave.";
    public static string UI_SETTINGS_BOT_DISEMBARK_HINT = "A message when a player gets off the ferry.";
    public static string UI_SETTINGS_BOT_DUNGEON_HINT = "The summary in chat when a dungeon ends.";
    public static string UI_SETTINGS_BOT_RAID_HINT = "The summary in chat when a raid ends.";
    public static string UI_SETTINGS_LOCAL_PORT_HINT = "The port a bot on this computer connects to.";
    public static string UI_SETTINGS_RESET_PAGE_HINT = "Puts every setting on this page back to its default";
    public static string UI_SETTINGS_RESET_ONE = "Back to the default";
    public static string UI_SETTINGS_SECTION_OBSERVER = "OBSERVER CAMERA";
    public static string UI_SETTINGS_OBSERVE = "Time on each player";
    public static string UI_SETTINGS_OBSERVE_HINT = "How long the camera stays on a player before it moves on.";
    public static string UI_SETTINGS_OBSERVE_SUB = "Time on subscribers";
    public static string UI_SETTINGS_OBSERVE_VIP = "Time on VIPs";
    public static string UI_SETTINGS_OBSERVE_MOD = "Time on moderators";
    public static string UI_SETTINGS_OBSERVE_EVENT = "Time after a sub or a cheer";
    public static string UI_SETTINGS_OBSERVE_EVENT_HINT = "The camera turns to whoever subscribed or cheered.";
    public static string UI_SETTINGS_OBSERVE_ISLAND = "Time on each island";
    public static string UI_SETTINGS_OBSERVE_EMPTY = "Visit empty islands";
    public static string UI_SETTINGS_OBSERVE_EMPTY_HINT = "Islands with nobody on them still get a turn in the island camera.";
    public static string UI_SETTINGS_AFK = "Kick players who are away";
    public static string UI_SETTINGS_AFK_HINT = "Players who have not typed a command for this long are removed.";
    public static string[] UI_SETTINGS_AFK_OPTIONS = { "Never", "After 1 hour", "After 2 hours", "After 4 hours", "After 8 hours", "After 12 hours", "After 24 hours" };
    public static string UI_SETTINGS_NO_EXP = "Warn players who gain no exp";
    public static string UI_SETTINGS_NO_EXP_HINT = "The bot points anyone training where they gain nothing to a better island.";
    public static string UI_SETTINGS_NO_EXP_TIME = "First warning after";
    public static string UI_SETTINGS_NO_EXP_TIME_HINT = "Later warnings wait five times as long.";
    public static string[] UI_SETTINGS_NO_EXP_OPTIONS = { "2 minutes", "5 minutes", "10 minutes", "15 minutes", "30 minutes" };
    public static string UI_SETTINGS_LABELS_TEXT = "Text files";
    public static string UI_SETTINGS_LABELS_JSON = "Json files";
    public static string UI_SETTINGS_SECTION_MESSAGES = "MESSAGES";
    public static string UI_SETTINGS_SECTION_LOOT = "LOOT";
    public static string UI_SETTINGS_LOOT_ORIGIN = "Say where loot came from";
    public static string UI_SETTINGS_LOOT_ORIGIN_HINT = "!loot replies tell whether each item dropped in a raid or a dungeon.";
    public static string UI_SETTINGS_SECONDS = "{0} s";
    public static string UI_SETTINGS_ON = "On";
    public static string UI_SETTINGS_OFF = "Off";

    public static string UI_SETTINGS_SECTION_PLAYERS = "PLAYERS";
    public static string UI_SETTINGS_SECTION_WORLD = "WORLD";
    public static string UI_SETTINGS_CAMERA_ROTATION = "Observer camera rotation";
    public static string UI_SETTINGS_CAMERA_ROTATION_HINT = "How fast the camera circles the player you are watching, and which way.";
    public static string UI_SETTINGS_CAMERA_STILL = "No rotation";
    public static string UI_SETTINGS_CAMERA_WHEE = "Wheee!";
    public static string UI_SETTINGS_BOOST = "Subscriber exp bonus";
    public static string UI_SETTINGS_BOOST_HINT = "Who gets the extra exp from your channel's subscription tier.";
    public static string[] UI_SETTINGS_BOOST_OPTIONS = { "Subscribers", "Everyone" };
    public static string UI_SETTINGS_EXPIRY = "Keep players after a restart";
    public static string UI_SETTINGS_EXPIRY_HINT = "How long players stay in the saved game state when the game restarts.";
    public static string[] UI_SETTINGS_EXPIRY_OPTIONS = { "Don't keep", "2 hours", "4 hours", "8 hours", "12 hours", "24 hours", "36 hours", "48 hours", "Forever" };
    public static string UI_SETTINGS_EXPIRY_ALERT = "Tell chat when saved players expire";
    public static string UI_SETTINGS_DROPS = "Announcement style";
    public static string UI_SETTINGS_DROPS_HINT = "How raid and dungeon loot is announced in chat. For example:";
    public static string[] UI_SETTINGS_DROPS_OPTIONS = { "One item per message", "One player per message", "All in one message", "Items and counts", "Item names only", "No messages" };
    public static string UI_SETTINGS_DROPS_NONE = "(nothing is posted)";
    public static string UI_SETTINGS_PATHFINDING = "Navigation quality";
    public static string UI_SETTINGS_PATHFINDING_HINT = "How much time players get to find their way each frame. Higher is smoother with many players, and costs more CPU.";
    public static string[] UI_SETTINGS_PATHFINDING_OPTIONS = { "Low", "Normal", "High", "Ultra" };
    public static string UI_SETTINGS_HOUSES = "Give empty houses to players";
    public static string UI_SETTINGS_HOUSES_HINT = "Moves players into village houses that have nobody living in them.";
    public static string UI_SETTINGS_LABELS = "Write stream label files";
    public static string UI_SETTINGS_LABELS_HINT = "Text and json files your streaming software can show.";
    public static string UI_SETTINGS_NO_RAIDS = "Turn off raids";
    public static string UI_SETTINGS_NO_DUNGEONS = "Turn off dungeons";
    public static string UI_SETTINGS_JOIN_CODE = "Join with a code";
    public static string UI_SETTINGS_JOIN_CODE_HINT = "Raids and dungeons show a word on stream, and viewers join by typing it, such as !raid apple. Keeps out those who are not watching. From the next raid or dungeon.";

    // Login screen
    public static string UI_LOGIN_TITLE = "Sign in";
    public static string UI_LOGIN_LEAD = "Sign in with Twitch, or with your username and the password you set for the game.";
    public static string UI_LOGIN_USERNAME = "USERNAME";
    public static string UI_LOGIN_PASSWORD = "PASSWORD";
    public static string UI_LOGIN_SHOW_PASSWORD = "Show password";
    public static string UI_LOGIN_HIDE_PASSWORD = "Hide password";
    public static string UI_LOGIN_REMEMBER = "Remember me";
    public static string UI_LOGIN_FORGOT = "Forgot your password?";
    public static string UI_LOGIN_SUBMIT = "SIGN IN";
    public static string UI_LOGIN_BUSY = "SIGNING IN...";
    public static string UI_LOGIN_FAILED_TITLE = "That did not match";
    public static string UI_LOGIN_FAILED = "This is the password you set for the game client, not your Twitch password. If you have never set one, use Sign in with Twitch above.";
    public static string UI_LOGIN_UNREACHABLE_TITLE = "Could not reach the server";
    public static string UI_LOGIN_UNREACHABLE = "Check your internet connection and try again. If it keeps happening, the server may be down for a moment.";
    public static string UI_LOGIN_MISSING = "Fill in both your username and your password.";
    public static string UI_LOGIN_NO_PASSWORD = "New to Ravenfall? Signing in with Twitch makes your account, there is nothing else to fill in.";
    public static string UI_LOGIN_VERSION = "Ravenfall v{0}";
    public static string UI_LOGIN_QUIT = "Quit";
    public static string UI_LOGIN_TWITCH = "Sign in with Twitch";
    public static string UI_LOGIN_OR = "or use a game password";
    public static string UI_LOGIN_WAIT_TITLE = "Approve the sign in in your browser";
    public static string UI_LOGIN_WAIT_TEXT = "The Ravenfall website opened in your browser. Check that it shows this same code, and approve it there. The game signs in by itself, and from then on every time it starts.";
    public static string UI_LOGIN_WAITING = "Waiting for you to approve...";
    public static string UI_LOGIN_WAIT_REOPEN = "Open the page again";
    public static string UI_LOGIN_WAIT_CANCEL = "Cancel";
    public static string UI_LOGIN_TWITCH_DENIED_TITLE = "Sign in cancelled";
    public static string UI_LOGIN_TWITCH_DENIED = "The sign in was turned down on the website.";
    public static string UI_LOGIN_TWITCH_EXPIRED_TITLE = "The sign in ran out";
    public static string UI_LOGIN_TWITCH_EXPIRED = "It was not approved within ten minutes. Try again.";
    public static string UI_LOGIN_DEVICE_EXPIRED_TITLE = "Sign in again";
    public static string UI_LOGIN_DEVICE_EXPIRED = "The saved sign in for this computer no longer works. It was signed out somewhere else, or not used for a long time.";

    // Chat connection warning
    public static string UI_CONN_BOT_OFFLINE = "Bot offline";
    public static string UI_CONN_BOT_CONNECTING = "Connecting to the bot";
    public static string UI_CONN_NOT_IN_CHAT = "Not in your chat";
    public static string UI_CONN_BOT_OFFLINE_NOTE = "The game cannot reach the bot that reads your Twitch chat, so chat commands do nothing for now. It keeps trying on its own. Click to check the bot connection in the settings.";
    public static string UI_CONN_BOT_CONNECTING_NOTE = "Reaching the bot that reads your Twitch chat. This usually takes a few seconds.";
    public static string UI_CONN_NOT_IN_CHAT_SIGN_IN_NOTE = "The bot is connected, and joins your Twitch channel once you are signed in.";
    public static string UI_CONN_NOT_IN_CHAT_NOTE = "The bot is connected but has not joined your Twitch channel, so it cannot see chat. It usually joins within a few seconds. Click to check the bot settings if it does not.";
    public static string UI_CONN_SERVER_LOST = "Connection to the server lost";
    public static string UI_CONN_SERVER_LOST_NOTE = "Keep the game running. It reconnects on its own and nothing is lost, but restarting now could lose progress.";
    public static string UI_CONN_OUTDATED = "This version of Ravenfall is out of date";
    public static string UI_CONN_OUTDATED_NOTE = "Restart the game to update it.";

    // Getting the game ready after signing in
    public static string UI_LOADING_TITLE = "Getting your game ready";
    public static string UI_LOADING_READY = "Ready";
    public static string UI_LOADING_SESSION = "Starting your session";
    public static string UI_LOADING_SERVER = "Connecting to the game server";
    public static string UI_LOADING_ITEMS = "Loading items";
    public static string UI_LOADING_VILLAGE = "Loading your village";
    public static string UI_LOADING_SLOW = "This is taking longer than usual. The game keeps trying; if it stays here, check your connection.";

    public static string UI_SETTINGS_SECTION_QUALITY = "QUALITY";
    public static string UI_SETTINGS_SECTION_DAYNIGHT = "DAY AND NIGHT";
    public static string UI_SETTINGS_SECTION_PERFORMANCE = "PERFORMANCE";
    public static string UI_SETTINGS_QUALITY = "Graphics quality";
    public static string[] UI_SETTINGS_QUALITY_OPTIONS = { "Low", "Medium", "High" };
    public static string UI_SETTINGS_POTATO = "Potato mode";
    public static string UI_SETTINGS_POTATO_HINT = "The lowest graphics there are, for computers that are struggling.";
    public static string UI_SETTINGS_AUTO_POTATO = "Potato mode while in the background";
    public static string UI_SETTINGS_AUTO_POTATO_HINT = "Saves power when another window has focus.";
    public static string UI_SETTINGS_POST = "Post processing";
    public static string UI_SETTINGS_POST_HINT = "Bloom and colour grading.";
    public static string UI_SETTINGS_DAYNIGHT = "Day and night cycle";
    public static string UI_SETTINGS_REALTIME = "Follow the clock";
    public static string UI_SETTINGS_REALTIME_HINT = "Day and night follow the time on this computer.";
    public static string UI_SETTINGS_TIME = "Time of day";
    public static string UI_SETTINGS_TIME_HINT = "Used while the day and night cycle is off.";
    public static string UI_SETTINGS_DAY = "Day";
    public static string UI_SETTINGS_NIGHT = "Night";
    public static string UI_SETTINGS_VIEW_DISTANCE = "View distance";
    public static string UI_SETTINGS_RESOLUTION = "Render resolution";
    public static string UI_SETTINGS_RESOLUTION_HINT = "Draws the world at a lower resolution to save GPU time.";
    public static string UI_SETTINGS_CLAN_CACHE = "Clan logos";
    public static string UI_SETTINGS_CLAN_CACHE_HINT = "Throws away the downloaded logos so they are fetched again.";
    public static string UI_SETTINGS_CLAN_CACHE_BUTTON = "Clear cache";

    public static string UI_SETTINGS_MUSIC = "Music";
    public static string UI_SETTINGS_EVENTS = "Raid horns and dungeon gongs";
    public static string UI_SETTINGS_EVENTS_HINT = "Also the alert when another stream raids yours.";
    public static string UI_SETTINGS_SILENCE = "Silence";
    public static string UI_SETTINGS_BWAAAH = "BWAAAH";

    public static string UI_SETTINGS_UI_SCALE = "Interface size";
    public static string UI_SETTINGS_UI_SCALE_HINT = "Shortcuts: {0} and {1}.";
    public static string UI_SETTINGS_NAMES = "Player names over heads";
    public static string UI_SETTINGS_PLAYER_LIST = "Player list";
    public static string UI_SETTINGS_PLAYER_DETAILS = "Player details";
    public static string UI_SETTINGS_PLAYER_DETAILS_HINT = "The panel for the player the camera is watching. Shortcut: {0}.";
    public static string UI_SETTINGS_SHORTCUTS = "Shortcut bar";
    public static string UI_SETTINGS_SHORTCUTS_HINT = "The row of keyboard shortcuts along the bottom.";
    public static string UI_SETTINGS_RESET_WINDOWS = "Window positions";
    public static string UI_SETTINGS_RESET_WINDOWS_HINT = "Puts every window back where it started.";
    public static string UI_SETTINGS_RESET_WINDOWS_BUTTON = "Reset";

    public static string UI_SETTINGS_BOT_HINT = "Which messages the bot posts in your chat.";
    public static string UI_SETTINGS_BOT_EXP = "Exp multiplier running out";
    public static string UI_SETTINGS_BOT_WELCOME = "Players joining and leaving";
    public static string UI_SETTINGS_BOT_DISEMBARK = "Ferry arrivals";
    public static string UI_SETTINGS_BOT_TRAIN = "Training warnings";
    public static string UI_SETTINGS_BOT_TRAIN_HINT = "When a player trains something their island can't teach them.";
    public static string UI_SETTINGS_BOT_DUNGEON = "Dungeon results";
    public static string UI_SETTINGS_BOT_RAID = "Raid results";
    public static string UI_SETTINGS_BOT_ITEMS = "Loot announcements";
    public static string UI_SETTINGS_BOT_ITEMS_HINT = "Only the chat messages about raid and dungeon loot. Players get their loot either way.";

    public static string UI_SETTINGS_DEV_TOOLS = "Developer tools";
    public static string UI_SETTINGS_DEV_TOOLS_HINT = "The column of debug buttons on the right of the screen.";
    public static string UI_SETTINGS_DISCONNECT = "Server connection";
    public static string UI_SETTINGS_DISCONNECT_HINT = "Drops the connection to test that the game reconnects.";
    public static string UI_SETTINGS_DISCONNECT_BUTTON = "Disconnect";

    public static string UI_RAID_COME_HELP = "Come help fight! Type {command}";

    public static string UI_DUNGEON_APPEARED = "A dungeon has appeared!";
    public static string UI_DUNGEON_JOIN = "Type {command} to join";

    // ---- Names from enums, looked up so they follow the language --------------------------------

    /// <summary>A skill's name in the current language; the enum name when there is none.</summary>
    public static string SkillName(RavenNest.Models.Skill skill) => SkillName(skill.ToString());

    public static string SkillName(string skill)
    {
        switch (skill)
        {
            case "Attack": return UI_SKILL_ATTACK;
            case "Defense": return UI_SKILL_DEFENSE;
            case "Strength": return UI_SKILL_STRENGTH;
            case "Health": return UI_SKILL_HEALTH;
            case "Woodcutting": return UI_SKILL_WOODCUTTING;
            case "Fishing": return UI_SKILL_FISHING;
            case "Mining": return UI_SKILL_MINING;
            case "Crafting": return UI_SKILL_CRAFTING;
            case "Cooking": return UI_SKILL_COOKING;
            case "Farming": return UI_SKILL_FARMING;
            case "Slayer": return UI_SKILL_SLAYER;
            case "Magic": return UI_SKILL_MAGIC;
            case "Ranged": return UI_SKILL_RANGED;
            case "Sailing": return UI_SKILL_SAILING;
            case "Healing": return UI_SKILL_HEALING;
            case "Gathering": return UI_SKILL_GATHERING;
            case "Alchemy": return UI_SKILL_ALCHEMY;
            case "Melee": return UI_SKILL_MELEE;
            default: return skill;
        }
    }

    /// <summary>The words of an item drop message, for the Core assembly that builds it.</summary>
    public static PlayerItemDropWording ItemDropWording()
    {
        return new PlayerItemDropWording
        {
            YouFoundA = MSG_DROP_YOU_FOUND_A,
            YouFoundAn = MSG_DROP_YOU_FOUND_AN,
            FoundBy = MSG_DROP_FOUND_BY,
            And = MSG_LIST_AND,
        };
    }

    /// <summary>
    /// An item stat or enchantment as shown in a tooltip. The game names them in English and
    /// looks some of them up by that name, so they are only translated here, for display.
    /// </summary>
    public static string StatName(string stat)
    {
        switch (stat?.ToUpperInvariant())
        {
            case "WEAPON AIM": return UI_TIP_WEAPON_AIM;
            case "WEAPON POWER": return UI_TIP_WEAPON_POWER;
            case "RANGED AIM": return UI_TIP_RANGED_AIM;
            case "RANGED POWER": return UI_TIP_RANGED_POWER;
            case "MAGIC AIM": return UI_TIP_MAGIC_AIM;
            case "MAGIC POWER": return UI_TIP_MAGIC_POWER;
            case "ARMOR":
            case "ARMOUR": return UI_TIP_ARMOR;
            case "POWER": return UI_ENCHANT_POWER;
            case "AIM": return UI_ENCHANT_AIM;
            default: return SkillName(stat);
        }
    }

    public static string SlotName(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Head: return UI_SLOT_HEAD;
            case ItemSlot.Amulet: return UI_SLOT_AMULET;
            case ItemSlot.Ring: return UI_SLOT_RING;
            case ItemSlot.Chest: return UI_SLOT_CHEST;
            case ItemSlot.Gloves: return UI_SLOT_GLOVES;
            case ItemSlot.Belt: return UI_SLOT_BELT;
            case ItemSlot.Leggings: return UI_SLOT_LEGGINGS;
            case ItemSlot.Boots: return UI_SLOT_BOOTS;
            case ItemSlot.MeleeWeapon: return UI_SLOT_MELEEWEAPON;
            case ItemSlot.MagicWeapon: return UI_SLOT_MAGICWEAPON;
            case ItemSlot.RangedWeapon: return UI_SLOT_RANGEDWEAPON;
            case ItemSlot.Shield: return UI_SLOT_SHIELD;
            case ItemSlot.Pet: return UI_SLOT_PET;
            case ItemSlot.Cape: return UI_SLOT_CAPE;
            default: return slot.ToString();
        }
    }

    /// <summary>A village house's name in the current language, for its type.</summary>
    public static string HouseName(TownHouseSlotType type)
    {
        switch (type)
        {
            case TownHouseSlotType.Woodcutting: return UI_HOUSE_WOODCUTTING;
            case TownHouseSlotType.Magic: return UI_HOUSE_MAGIC;
            case TownHouseSlotType.Ranged: return UI_HOUSE_RANGED;
            case TownHouseSlotType.Melee: return UI_HOUSE_MELEE;
            case TownHouseSlotType.Cooking: return UI_HOUSE_COOKING;
            case TownHouseSlotType.Crafting: return UI_HOUSE_CRAFTING;
            case TownHouseSlotType.Farming: return UI_HOUSE_FARMING;
            case TownHouseSlotType.Mining: return UI_HOUSE_MINING;
            case TownHouseSlotType.Sailing: return UI_HOUSE_SAILING;
            case TownHouseSlotType.Slayer: return UI_HOUSE_SLAYER;
            case TownHouseSlotType.Fishing: return UI_HOUSE_FISHING;
            case TownHouseSlotType.Healing: return UI_HOUSE_HEALING;
            case TownHouseSlotType.Gathering: return UI_HOUSE_GATHERING;
            case TownHouseSlotType.Alchemy: return UI_HOUSE_ALCHEMY;
            default: return type.ToString();
        }
    }

    public static string HouseName(TownHouse house, TownHouseSlotType fallback) => HouseName(house ? house.Type : fallback);
    // GameManager
    public static string MSG_RESTORE_STATE_EXPIRED = Meta("system", "restore") + "Player restore state file has expired. No players have been added back.";
    public static string MSG_AUTOJOIN_DUNGEON_JOINED_COUNTER = Meta("dungeon", "join", "auto") + "You have automatically joined the dungeon! You will join {counter} more.";
    public static string MSG_AUTOJOIN_DUNGEON_JOINED = Meta("dungeon", "join", "auto") + "You have automatically joined the dungeon!";
    public static string MSG_AUTOJOIN_DUNGEON_JOINED_NAMES = Meta("dungeon", "join", "auto") + "{playerNames} have automatically joined the dungeon!";
    public static string MSG_AUTOJOIN_DUNGEON_JOINED_COUNT = Meta("dungeon", "join", "auto") + "{playerCount} players have automatically joined the dungeon!";
    public static string MSG_AUTOJOIN_DUNGEON_FAILED = Meta("dungeon", "join", "auto", "fail") + "You have failed to join the dungeon. Make sure you have enough coins to automatically join.";
    public static string MSG_AUTOJOIN_DUNGEON_FAILED_NAMES = Meta("dungeon", "join", "auto", "fail") + "{playerNames} failed to join the dungeon. You may not have enough coins.";
    public static string MSG_AUTOJOIN_DUNGEON_FAILED_COUNT = Meta("dungeon", "join", "auto", "fail") + "{playerCount} players failed to join the dungeon.";
    public static string MSG_AUTOJOIN_RAID_FAILED = Meta("raid", "join", "auto", "fail") + "You have failed to join the raid. Make sure you have enough coins to automatically join.";
    public static string MSG_AUTOJOIN_RAID_FAILED_NAMES = Meta("raid", "join", "auto", "fail") + "{playerNames} failed to join the raid. You may not have enough coins.";
    public static string MSG_AUTOJOIN_RAID_FAILED_COUNT = Meta("raid", "join", "auto", "fail") + "{playerCount} players failed to join the raid.";
    public static string MSG_AUTOJOIN_RAID_JOINED_COUNTER = Meta("raid", "join", "auto") + "You have automatically joined the raid! You will join {counter} more.";
    public static string MSG_AUTOJOIN_RAID_JOINED = Meta("raid", "join", "auto") + "You have automatically joined the raid!";
    public static string MSG_AUTOJOIN_RAID_JOINED_NAMES = Meta("raid", "join", "auto") + "{playerNames} have automatically joined the raid!";
    public static string MSG_AUTOJOIN_RAID_JOINED_COUNT = Meta("raid", "join", "auto") + "{playerCount} players have automatically joined the raid!";

    // DungeonManager
    public static string MSG_DUNGEON_ENDED_NO_SURVIVORS = Meta("dungeon", "result") + "The dungeon has ended without any surviving players.";
    public static string MSG_DUNGEON_VICTORY_NO_CONNECTION = Meta("dungeon", "result") + "Victorious!! Dungeon boss was slain but unfortunately the connection to the server has been broken, rewards will be distributed later.";
    public static string MSG_DUNGEON_VICTORY_TREASURE = Meta("dungeon", "result", "reward") + "Victorious!! The dungeon boss was slain and yielded {itemCount} item treasures!";
    public static string MSG_DUNGEON_VICTORY_NO_TREASURE = Meta("dungeon", "result") + "Victorious!! The dungeon boss was slain but did not yield any treasure.";
    public static string MSG_DUNGEON_STARTS_IN_MINUTES = Meta("dungeon", "info", "timer") + "{minutes}m{seconds}s until dungeon starts.";
    public static string MSG_DUNGEON_STARTS_IN_SECONDS = Meta("dungeon", "info", "timer") + "{seconds}s until dungeon starts.";
    public static string MSG_DUNGEON_AVAILABLE_CODE = Meta("event", "dungeon") + "{dungeonName} is available. Type '!dungeon code' to join. Find the code on the stream.";
    public static string MSG_DUNGEON_AVAILABLE = Meta("event", "dungeon") + "{dungeonName} is available. Type !dungeon to join.";

    // RaidManager
    public static string MSG_RAID_TIMEOUT = Meta("raid", "result") + "Oh no! The raid boss was not killed in time. No rewards will be given.";
    public static string MSG_RAID_VICTORY_NO_CONNECTION = Meta("raid", "result") + "Victorious!! Raid boss was slain but unfortunately the connection to the server has been broken, rewards will be distributed later.";
    public static string MSG_RAID_VICTORY_TREASURE = Meta("raid", "result", "reward") + "Victorious!! The raid boss was slain and yielded {itemCount} item treasures!";
    public static string MSG_RAID_VICTORY_NO_TREASURE = Meta("raid", "result") + "Victorious!! The raid boss was slain but did not yield any treasure.";

    // RaidEventHandler
    public static string MSG_STREAMRAID_WAR_DECLARED = Meta("system", "streamer raid") + "{raiderName} is declaring war with an army of {raiderPlayerCount}!";
    public static string MSG_STREAMRAID_RAIDING = Meta("system", "streamer raid") + "{raiderName} is raiding with {raiderPlayerCount} players!";
    public static string MSG_STREAMRAID_NO_RAIDERS_ADDED = Meta("system", "streamer raid", "fail") + "Raid could not be started, as 0 players could be added to the raid.";
    public static string MSG_STREAMRAID_WAR_STARTING = Meta("system", "streamer raid") + "The raid war from {raiderName} is starting. {myPlayerCount} vs {raiderPlayerCount}!";

    // GameUpdatedEventHandler
    public static string MSG_UPDATE_REQUIRED = Meta("system", "update") + "Attention! An essential update to Ravenfall, v{version}, is available. This update contains critical changes. To avoid any game disruption, game will attempt to automatically restart to apply the update.";
    public static string MSG_UPDATE_AVAILABLE = Meta("system", "update") + "Good news! Ravenfall v{version} is out now! For optimal gameplay and new features, restart your game to update. Enjoy!";

    // StreamRaidManager (on screen)
    public static string UI_STREAMRAID_WAR_DECLARED = "<b>{0}</b> has declared war with an army of <b>{1}</b>!";
    public static string UI_STREAMRAID_FRIENDLY_RAID = "Friendly raid from <b>{0}</b> with <b>{1}</b> players!";

    // PlayerManager
    public static string MSG_RESTORE_FAILED = Meta("system", "restore", "fail") + "Failed to restore players. See game log files for more details.";
    public static string MSG_RESTORE_PARTIAL = Meta("system", "restore") + "{restoredCount} out of {playerCount} were added back to the game.";
    public static string MSG_RESTORE_FAILED_NAMES = Meta("system", "restore", "fail") + "{failedCount} players failed to be added back: {playerNames}";
    public static string MSG_RESTORE_SUCCESS = Meta("system", "restore") + "{playerCount} players restored.";

    // PlayerController
    public static string MSG_PLAYER_ITEM_FOUND_EQUIPPED = Meta("info", "item", "found") + "You found and equipped a {itemName}!";
    public static string MSG_PLAYER_ITEM_FOUND = Meta("info", "item", "found") + "You found a {itemName}!";
    public static string MSG_TRAINING_NOT_TRAINABLE_SUGGEST = Meta("training", "fail") + "{skillName} is not a trainable skill. Did you mean {suggestion}?";
    public static string MSG_TRAINING_NO_EXP_RECOMMENDED_ISLAND = Meta("training", "info") + "You have not been gaining any experience for more than {mins} minutes. You're recommended to sail to {recommendedIslandName1} to continue training. The broadcaster may now also use !sail {recommendedIslandName2} {playerName} to make your character move.";

    // PlayerEquipmentActions (the MSG_EQUIP_REQ_ fields are pieces of the {requirement} in MSG_EQUIP_REQUIREMENTS_NOT_MET, filled with string.Format)
    public static string MSG_EQUIP_CANT_BE_EQUIPPED = Meta("equipment", "equip", "fail") + "{itemName} can't be equipped.";
    public static string MSG_EQUIP_REQUIREMENTS_NOT_MET = Meta("equipment", "equip", "fail") + "You do not meet the requirements to equip {itemName}. {requirement}";
    public static string MSG_EQUIP_REQ_LEVEL = "You require level {0}";
    public static string MSG_EQUIP_REQ_ATTACK = "{0} Attack.";
    public static string MSG_EQUIP_REQ_DEFENSE = "{0} Defense.";
    public static string MSG_EQUIP_REQ_MAGIC_OR_HEALING = "{0} Magic or Healing.";
    public static string MSG_EQUIP_REQ_RANGED = "{0} Ranged.";
    public static string MSG_EQUIP_REQ_SLAYER = "{0} Slayer.";

    // RaidForce
    public static string MSG_RAID_FORCE_DISABLED = Meta("raid", "scroll", "fail") + "Raids have been disabled in this stream by the broadcaster.";
    public static string MSG_RAID_FORCE_BUSY = Meta("raid", "scroll", "fail") + "Someone just used a raid scroll.";
    public static string MSG_RAID_FORCE_ALREADY_ACTIVE = Meta("raid", "scroll", "fail") + "There is already an active raid.";
    public static string MSG_RAID_FORCE_DUNGEON_BUSY = Meta("raid", "scroll", "fail") + "Someone tried to use a dungeon scroll. Please wait before using a raid scroll.";
    public static string MSG_RAID_START_FAILED_DUNGEON = Meta("raid", "start", "fail", "dungeon") + "Unable to start a raid during a dungeon. Please wait for it to be over.";
    public static string MSG_RAID_START_FAILED_WAR = Meta("raid", "start", "fail", "war") + "Unable to start a raid during a war. Please wait for it to be over.";
    public static string MSG_RAID_FORCE_TRY_LATER = Meta("raid", "scroll", "fail") + "Raid cannot be started right now. Please try again later.";
    public static string MSG_RAID_FORCE_USED_JOINED = Meta("raid", "scroll") + "You have used a Raid Scroll and joined the raid. Good luck!";
    public static string MSG_RAID_FORCE_USED = Meta("raid", "scroll") + "You have used a Raid Scroll.";
    public static string MSG_RAID_FORCE_FAILED_REFUND = Meta("raid", "scroll", "fail") + "Raid could not be started. Scroll will be refunded.";
    public static string MSG_RAID_FORCE_NO_SCROLLS = Meta("raid", "scroll", "fail") + "You do not have any Raid Scrolls! Redeem them under streamer loyalty on the website.";
    public static string MSG_EVENT_SCROLL_SERVER_ERROR = Meta("system", "scroll", "fail") + "Server was not able to give back a valid response. Uh oh.. BUG!";

    // DungeonForce
    public static string MSG_DUNGEON_FORCE_DISABLED = Meta("dungeon", "scroll", "fail") + "Dungeons have been disabled in this stream by the broadcaster.";
    public static string MSG_DUNGEON_FORCE_BUSY = Meta("dungeon", "scroll", "fail") + "Someone just used a dungeon scroll.";
    public static string MSG_DUNGEON_FORCE_ALREADY_ACTIVE = Meta("dungeon", "scroll", "fail") + "There is already an active dungeon.";
    public static string MSG_DUNGEON_FORCE_RAID_BUSY = Meta("dungeon", "scroll", "fail") + "Someone tried to use a raid scroll. Please wait before using a dungeon scroll.";
    public static string MSG_DUNGEON_FORCE_TRY_LATER = Meta("dungeon", "scroll", "fail") + "Dungeon cannot be started right now. Please try again later.";
    public static string MSG_DUNGEON_FORCE_USED_JOINED = Meta("dungeon", "scroll") + "You have used a Dungeon Scroll and joined the dungeon. Good luck!";
    public static string MSG_DUNGEON_FORCE_USED = Meta("dungeon", "scroll") + "You have used a Dungeon Scroll.";
    public static string MSG_DUNGEON_FORCE_FAILED = Meta("dungeon", "scroll", "fail") + "Dungeon could not be started. Try again later.";
    public static string MSG_DUNGEON_FORCE_NO_SCROLLS = Meta("dungeon", "scroll", "fail") + "You do not have any Dungeon Scrolls! Redeem them under streamer loyalty on the website.";

    // ProduceItemCommand
    public static string MSG_PRODUCE_NOT_CONNECTED = Meta("system", "fail") + "This action can only be done while connected to the server.";
    public static string MSG_PRODUCE_STATUS_ALMOST_DONE = Meta("crafting", "info") + "You're busy making something and should be done any second!";
    public static string MSG_PRODUCE_STATUS = Meta("crafting", "info") + "You're busy making something and will be done in {timeLeft}.";
    public static string MSG_PRODUCE_NO_ACTION = Meta("crafting", "fail") + "You don't seem to have an ongoing action.";
    public static string MSG_PRODUCE_NO_ACTION_TO_CANCEL = Meta("crafting", "fail") + "You don't seem to have an action to cancel.";
    public static string MSG_PRODUCE_NOT_CREATABLE = Meta("crafting", "fail") + "{itemName} is not an item that can be created.";
    public static string MSG_PRODUCE_WRONG_SKILL = Meta("crafting", "fail") + "{itemName} is not an item that can be created using {skillName}. It requires level {requiredSkillLevel} {requiredSkill}";
    public static string MSG_PRODUCE_LEVEL_TOO_LOW = Meta("crafting", "fail") + "{name} requires level {requiredSkillLevel} {requiredSkill}";
    public static string MSG_PRODUCE_NOT_ENOUGH_RESOURCES = Meta("crafting", "fail") + "You do not have enough resources for creating {name}. You need {requirements}";
    public static string MSG_PRODUCE_SERVER_ERROR = Meta("crafting", "fail") + "Server returned an error, please try again later.";

    // {verb} and {verbPast} are filled from the MSG_PRODUCE_TERMS_ fields below
    public static string MSG_PRODUCE_FAILED_SERVER = Meta("crafting", "fail") + "Unable to {verb} {recipeName}. Server returned an error. Please try again later.";
    public static string MSG_PRODUCE_FAILED_INGREDIENTS = Meta("crafting", "fail") + "Unable to {verb} {recipeName}. Could you be missing some ingredients?";
    public static string MSG_PRODUCE_SUCCESS = Meta("crafting", "info") + "You have successfully {verbPast} {quantity} {itemName}";
    public static string MSG_PRODUCE_SUCCESS_RECEIVED = Meta("crafting", "info") + "You have successfully {verbPast} {recipeName}! You've received {quantity} {itemName}";
    public static string MSG_PRODUCE_FAILED = Meta("crafting", "fail") + "Oh no! You have failed to {verb} {recipeName}! You've received {quantity} {itemName}";
    public static string MSG_PRODUCE_MIXED_RESULT = Meta("crafting", "info") + "You have {verbPast} {quantity} {recipeName} and received {items}";

    // the {quantity} when one item was made: "a" or "an" before the item name
    public static string MSG_PRODUCE_QUANTITY_A = "a";
    public static string MSG_PRODUCE_QUANTITY_AN = "an";

    // production verbs, { verb, past participle }: "Unable to {verb}", "You have {verbPast}"
    public static string[] MSG_PRODUCE_TERMS_CHURN = { "churn", "churned" };
    public static string[] MSG_PRODUCE_TERMS_FERMENT = { "ferment", "fermented" };
    public static string[] MSG_PRODUCE_TERMS_MAKE = { "make", "made" };
    public static string[] MSG_PRODUCE_TERMS_PICK = { "pick", "picked" };
    public static string[] MSG_PRODUCE_TERMS_HARVEST = { "harvest", "harvested" };
    public static string[] MSG_PRODUCE_TERMS_CULTIVATE = { "cultivate", "cultivated" };
    public static string[] MSG_PRODUCE_TERMS_GRIND = { "grind", "ground" };
    public static string[] MSG_PRODUCE_TERMS_PLUCK = { "pluck", "plucked" };
    public static string[] MSG_PRODUCE_TERMS_GATHER = { "gather", "gathered" };
    public static string[] MSG_PRODUCE_TERMS_COLLECT = { "collect", "collected" };
    public static string[] MSG_PRODUCE_TERMS_REFINE = { "refine", "refined" };
    public static string[] MSG_PRODUCE_TERMS_PROCESS = { "process", "processed" };
    public static string[] MSG_PRODUCE_TERMS_FETCH = { "fetch", "fetched" };
    public static string[] MSG_PRODUCE_TERMS_MOLD = { "mold", "molded" };
    public static string[] MSG_PRODUCE_TERMS_CHOP = { "chop", "chopped" };
    public static string[] MSG_PRODUCE_TERMS_MILL = { "mill", "milled" };
    public static string[] MSG_PRODUCE_TERMS_BAKE = { "bake", "baked" };
    public static string[] MSG_PRODUCE_TERMS_EXTRACT = { "extract", "extracted" };
    public static string[] MSG_PRODUCE_TERMS_MINE = { "mine", "mined" };
    public static string[] MSG_PRODUCE_TERMS_CUT = { "cut", "cut" };
    public static string[] MSG_PRODUCE_TERMS_INSCRIBE = { "inscribe", "inscribed" };
    public static string[] MSG_PRODUCE_TERMS_COOK = { "cook", "cooked" };
    public static string[] MSG_PRODUCE_TERMS_BREW = { "brew", "brewed" };
    public static string[] MSG_PRODUCE_TERMS_CRAFT = { "craft", "crafted" };

    // DuelPlayer
    public static string MSG_DUEL_DURING_WAR = Meta("duel", "fail") + "Unable to duel someone during a war. Please wait for it to be over.";
    public static string MSG_DUEL_PLAYER_NOT_FOUND = Meta("duel", "fail") + "No player with the name {targetPlayerName} is currently playing.";
    public static string MSG_DUEL_FAILED_FERRY = Meta("duel", "fail") + "You cannot duel another player while on the ferry.";
    public static string MSG_DUEL_TARGET_FERRY = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as they are on the ferry.";
    public static string MSG_DUEL_TARGET_OTHER_ISLAND = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as they are not on the same island.";
    public static string MSG_DUEL_SELF = Meta("duel", "fail") + "You cannot duel yourself.";
    public static string MSG_DUEL_FAILED_IN_DUEL = Meta("duel", "fail") + "You cannot duel another player as you are already in a duel.";
    public static string MSG_DUEL_FAILED_IN_ARENA = Meta("duel", "fail") + "You cannot duel another player as you are participating in the Arena.";
    public static string MSG_DUEL_FAILED_IN_RAID = Meta("duel", "fail") + "You cannot duel another player as you are participating in a Raid.";
    public static string MSG_DUEL_FAILED_IN_DUNGEON = Meta("duel", "fail") + "You cannot duel another player as you are participating in a Dungeon.";
    public static string MSG_DUEL_TARGET_IN_DUEL = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as he or she is participating in a duel.";
    public static string MSG_DUEL_TARGET_IN_ARENA = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as he or she is participating in the Arena.";
    public static string MSG_DUEL_TARGET_IN_RAID = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as he or she is participating in a Raid.";
    public static string MSG_DUEL_TARGET_IN_DUNGEON = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as he or she is participating in a Dungeon.";
    public static string MSG_DUEL_TARGET_HAS_REQUEST = Meta("duel", "fail") + "You cannot duel {targetPlayerName} as he or she already has an active duel request.";

    // GiftItem
    public static string MSG_GIFT_COINS_PLAYER_NOT_FOUND = Meta("gift", "fail") + "No player with the name {playerName} found in this stream. Maybe you misspelled it?";
    public static string MSG_GIFT_COINS_SELF = Meta("gift", "fail") + "You cannot gift coins to yourself.";
    public static string MSG_GIFT_COINS_NEGATIVE = Meta("gift", "fail") + "You cannot gift a negative amount of coins.";
    public static string MSG_GIFT_COINS = Meta("gift", "info") + "You have gifted {playerName} with {amount} coins!";
    public static string MSG_GIFT_COINS_SINGLE = Meta("gift", "info") + "Did you feel extra nice today? You have gifted {playerName} with a single coin!";
    public static string MSG_GIFT_COINS_FAILED = Meta("gift", "fail") + "You could not gift any coins to {playerName}, make sure the name is spelled correctly and that you have enough coins to send.";
    public static string MSG_GIFT_COINS_ERROR = Meta("gift", "fail") + "Unable to gift any coins right now. Please try again later.";
    public static string MSG_GIFT_SET_HELP = Meta("gift", "fail") + "When gifting a whole item set, please include target player name, item type, word set. You can only gift one set at a time and only unequipped items will be gifted. example !gift playerName rune set";
    public static string MSG_GIFT_SET_PARTIAL = Meta("gift", "info") + "You have gifted {itemCount} out of {itemSetCount} from the {setType} set to {targetPlayerName}! The items gifted were {itemList}";
    public static string MSG_GIFT_SET_A = Meta("gift", "info") + "You have gifted a {setType} set to {targetPlayerName}! The items gifted were {itemList}";
    public static string MSG_GIFT_SET_AN = Meta("gift", "info") + "You have gifted an {setType} set to {targetPlayerName}! The items gifted were {itemList}";

    // GiftItem and SendItem
    public static string MSG_ITEM_SET_NOT_FOUND = Meta("info", "item set", "fail") + "No item sets could be found matching the name {query}";
    public static string MSG_ITEM_SET_NONE_OWNED = Meta("info", "item set", "fail") + "You do not have any of the items in the set '{query}'";

    // SendItem
    public static string MSG_SEND_SET_HELP = Meta("send", "fail") + "When sending a whole item set, please include target player name, item type, word set. You can only send one set at a time and only unequipped items will be sent. example !send characterNumber rune set";
    public static string MSG_SEND_SELF = Meta("send", "fail") + "You cannot send items to yourself.";
    public static string MSG_SEND_SET_PARTIAL = Meta("send", "info") + "You have sent {itemCount} out of {itemSetCount} from the {setType} set to {targetPlayerName}! The items sent were {itemList}";
    public static string MSG_SEND_SET_A = Meta("send", "info") + "You have sent a {setType} set to {targetPlayerName}! The items sent were {itemList}";
    public static string MSG_SEND_SET_AN = Meta("send", "info") + "You have sent an {setType} set to {targetPlayerName}! The items sent were {itemList}";

    // TeleportToIsland
    public static string MSG_TELEPORT_FAILED_DUNGEON = Meta("travel", "teleport", "fail") + "You cannot teleport while in a dungeon.";
    public static string MSG_TELEPORT_FAILED_WAR = Meta("travel", "teleport", "fail") + "You cannot teleport while in a raid war.";
    public static string MSG_TELEPORT_FAILED_ARENA = Meta("travel", "teleport", "fail") + "You cannot teleport while in the arena.";
    public static string MSG_TELEPORT_FAILED_DUEL = Meta("travel", "teleport", "fail") + "You cannot teleport while in a duel.";
    public static string MSG_TELEPORT_FAILED_RAID = Meta("travel", "teleport", "fail") + "You cannot teleport while in a raid.";
    public static string MSG_TELEPORT_INVALID_ISLAND = Meta("travel", "teleport", "fail") + "{island} is not a valid destination. Available islands are {islandList}";
    public static string MSG_TELEPORT_NO_TOME = Meta("travel", "teleport", "fail") + "You do not have any suitable Teleportation Tomes to teleport to {island}.";
    public static string MSG_TELEPORT_SUCCESS = Meta("travel", "teleport", "info") + "You have used a {itemName} and teleported to {island}!";

    // UseItem (TeleportToIsland also uses MSG_USE_ITEM_NOT_NOW)
    public static string MSG_USE_ITEM_CANNOT_USE = Meta("info", "use", "fail") + "{itemName} cannot be used.";
    public static string MSG_USE_ITEM_TOME = Meta("info", "use", "fail") + "{itemName} cannot be used directly, use !teleport <island> instead.";
    public static string MSG_USE_ITEM_FERRY_BOOST_ACTIVE = Meta("info", "use", "fail") + "There is already a ferry boost active. It will last for {boostTime}";
    public static string MSG_USE_ITEM_NOT_NOW = Meta("info", "use", "fail") + "{itemName} can not be used right now.";
    public static string MSG_USE_ITEM_NO_EFFECT = Meta("info", "use", "fail") + "Nothing good will happen if you play with {itemName}.";

    // the first sentence of the use reply; the sentences after it are added below
    public static string MSG_USE_ITEM_USED = Meta("info", "use") + "You have used {itemName}.";
    public static string MSG_USE_ITEM_DRANK_A = Meta("info", "use") + "You drank a {itemName}.";
    public static string MSG_USE_ITEM_DRANK_AN = Meta("info", "use") + "You drank an {itemName}.";
    public static string MSG_USE_ITEM_ATE_A = Meta("info", "use") + "You ate a {itemName}.";
    public static string MSG_USE_ITEM_ATE_AN = Meta("info", "use") + "You ate an {itemName}.";

    // added after the first sentence, so no routing prefix. UseItem adds the effect index to each placeholder name
    public static string MSG_USE_ITEM_TELEPORTED = "You were teleported to {island}!";
    public static string MSG_USE_ITEM_FERRY_FASTER = "The ferry will now move faster for {boostTime}.";
    public static string MSG_USE_ITEM_HEALED = "You have recovered {healthAmount} HP!";
    public static string MSG_USE_ITEM_DAMAGED = "You took {damageAmount} damage!";
    public static string MSG_USE_ITEM_HEAL_OVER_TIME = "You will be healed {healPercent}% of your total health over {healTotalSeconds} seconds.";
    public static string MSG_USE_ITEM_GAINED_MINUTES_SECONDS = "You gained {effectPercent}% {effectType} for {effectMinutes} minutes and {effectSeconds} seconds.";
    public static string MSG_USE_ITEM_GAINED_MINUTES = "You gained {effectPercent}% {effectType} for {effectMinutes} minutes.";
    public static string MSG_USE_ITEM_GAINED_SECONDS = "You gained {effectPercent}% {effectType} for {effectSeconds} seconds.";

    // ItemUsage
    public static string MSG_ITEM_USAGE_NONE = Meta("info", "item usage", "fail") + "{itemName} is not used in any recipes.";
    public static string MSG_ITEM_USAGE = Meta("info", "item usage") + "{name} is used for: {list}";

    // ExamineItem
    public static string MSG_ITEM_EXAMINE_NO_DESCRIPTION = Meta("info", "examine") + "There is not much to say about {itemName}";

    // EquipItem
    public static string MSG_EQUIP_NOT_EQUIPPABLE = Meta("info", "equipment", "equip", "fail") + "{itemName} is not an equippable item.";

    // ItemCount
    public static string MSG_ITEM_COUNT = Meta("info", "item count") + "You have {itemCount}x {itemName}(s).";

    // GetLoot
    public static string MSG_LOOT_CLEARED = Meta("info", "loot") + "Your loot records have been cleared.";
    public static string MSG_LOOT_NONE = Meta("info", "loot") + "You have not looted anything yet.";
    public static string MSG_LOOT_FILTER_NO_RESULT = Meta("info", "loot") + "The filter '{query}' did not yield any result.";

    // built in the game, so {0} style. {0} is how long ago, like 5m 10s
    public static string MSG_LOOT_AGO = "{0} ago";
    public static string MSG_LOOT_FROM_DUNGEON_AGO = "from dungeon {0} ago";
    public static string MSG_LOOT_FROM_RAID_AGO = "from raid {0} ago";

    // DisenchantItem
    public static string MSG_DISENCHANT_NONE_RECENT = Meta("enchanting", "fail") + "You have not enchanted any items recently.";
    public static string MSG_DISENCHANT_LAST_EXPIRED = Meta("enchanting", "fail") + "It has been more than 5 minutes since you enchanted '{itemName}', to avoid disenchanting by mistake you have to use '!disenchant last' to continue.";

    // EnchantmentCooldown
    public static string MSG_ENCHANT_COOLDOWN_UNAVAILABLE = Meta("enchanting", "fail", "cooldown") + "Unable to get cooldown from the server right now. Please try again later.";
    public static string MSG_ENCHANT_NO_COOLDOWN = Meta("enchanting", "info", "cooldown") + "You do not have any cooldown.";
    public static string MSG_ENCHANT_COOLDOWN_CLEAR_INFO = Meta("enchanting", "info", "cooldown") + "You have to wait {timeLeft} before you can try to enchant something again. If you want to clear this cooldown, use !enchant clear cooldown, this will cost {pricePerSecond} coins per second. ~Total {totalCost} coins";

    // the {timeLeft} above, built in the game, so {0} style
    public static string MSG_ENCHANT_TIME_ONE_HOUR = "1 hour";
    public static string MSG_ENCHANT_TIME_HOURS = "{0} hours";
    public static string MSG_ENCHANT_TIME_ONE_MINUTE = "1 minute";
    public static string MSG_ENCHANT_TIME_MINUTES = "{0} minutes";
    public static string MSG_ENCHANT_TIME_ONE_SECOND = "1 second";
    public static string MSG_ENCHANT_TIME_SECONDS = "{0} seconds";
    public static string MSG_ENCHANT_TIME_MOMENT = "a moment";

    // ClearEnchantmentCooldown
    public static string MSG_ENCHANT_COOLDOWN_CLEARED = Meta("enchanting", "info", "cooldown") + "You have cleared your enchantment cooldown for a total of {totalCost} coins";
    public static string MSG_ENCHANT_COOLDOWN_CLEAR_FAILED = Meta("enchanting", "fail", "cooldown") + "Unable to clear the cooldown, either you don't have a cooldown active or you don't have enough coins.";
    public static string MSG_ENCHANT_COOLDOWN_CLEAR_ERROR = Meta("enchanting", "fail", "cooldown") + "An error occurred while trying to clear your enchantment cooldown. Please try again later.";

    // MaxMultiplier
    // The base text gets MSG_EXPBOOST_GLOBAL_PART and then MSG_EXPBOOST_RESTED_PART appended in code, so the parts carry no Meta prefix.
    public static string MSG_EXPBOOST = Meta("info", "exp multiplier") + "Your current exp boost: {expMulti}x";
    public static string MSG_EXPBOOST_TIER = Meta("info", "exp multiplier") + "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon";
    public static string MSG_EXPBOOST_HUTS = Meta("info", "exp multiplier") + "Your current exp boost: {expMulti}x. You gain {hutMulti}x from huts";
    public static string MSG_EXPBOOST_TIER_AND_HUTS = Meta("info", "exp multiplier") + "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon and {hutMulti}x from huts";
    public static string MSG_EXPBOOST_TIER_HUTS_LIST = Meta("info", "exp multiplier") + "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon, {hutMulti}x from huts";
    public static string MSG_EXPBOOST_RESTED_ONLY = Meta("info", "exp multiplier") + "You're gaining {expMulti}x more exp for being rested";
    public static string MSG_EXPBOOST_GLOBAL_PART = " and {globalMulti}x from global exp multiplier for {globalTime}";
    public static string MSG_EXPBOOST_RESTED_PART = " and the total is multiplied by {restedMulti}x from being rested.";

    // AutoRaid
    public static string MSG_RAID_AUTOJOIN_COUNT = Meta("raid", "auto join") + "You will automatically join the next {autoJoinCounter} raids, until you use !raid join off. It will cost {cost} each time you automatically join a raid.";
    public static string MSG_RAID_AUTOJOIN_STATUS_ON = Meta("raid", "auto join") + "You will automatically join raids until you use !raid join off.";
    public static string MSG_RAID_AUTOJOIN_STATUS_OFF = Meta("raid", "auto join") + "You are not set to automatically join any raids.";
    public static string MSG_RAID_AUTOJOIN_ON = Meta("raid", "auto join") + "You will automatically join raids until you use !raid join off. It will cost {cost} each time you automatically join a raid.";
    public static string MSG_RAID_AUTOJOIN_OFF = Meta("raid", "auto join") + "You will no longer automatically join raids";
    public static string MSG_RAID_AUTOJOIN_INVALID = Meta("raid", "auto join", "fail") + "{query} is not a valid value for raid auto join settings.";

    // AutoDungeon
    public static string MSG_DUNGEON_AUTOJOIN_COUNT = Meta("dungeon", "auto join") + "You will automatically join the next {autoJoinCounter} dungeons, until you use !dungeon join off. It will cost {cost} each time you automatically join a dungeon.";
    public static string MSG_DUNGEON_AUTOJOIN_STATUS_ON = Meta("dungeon", "auto join") + "You will automatically join dungeons until you use !dungeon join off.";
    public static string MSG_DUNGEON_AUTOJOIN_STATUS_OFF = Meta("dungeon", "auto join") + "You are not set to automatically join any dungeons.";
    public static string MSG_DUNGEON_AUTOJOIN_ON = Meta("dungeon", "auto join") + "You will automatically join dungeons until you use !dungeon join off. It will cost {cost} each time you automatically join a dungeon.";
    public static string MSG_DUNGEON_AUTOJOIN_OFF = Meta("dungeon", "auto join") + "You will no longer automatically join dungeons";
    public static string MSG_DUNGEON_AUTOJOIN_INVALID = Meta("dungeon", "auto join", "fail") + "{query} is not a valid value for dungeon auto join settings";

    // DungeonJoin
    public static string MSG_DUNGEON_JOIN_WAR = Meta("dungeon", "join", "fail") + "Dungeon is unavailable during wars. Please wait for it to be over.";
    public static string MSG_DUNGEON_JOIN_NO_DUNGEON = Meta("dungeon", "join", "fail") + "No active dungeons available, sorry.";
    public static string MSG_DUNGEON_JOIN_STARTED = Meta("dungeon", "join", "fail") + "The dungeon has already started. You will have to wait for the next one!";
    public static string MSG_DUNGEON_JOIN_SUCCESS = Meta("dungeon", "join") + "You have joined the dungeon. Good luck!";
    public static string MSG_DUNGEON_JOIN_ALREADY = Meta("dungeon", "join", "fail") + "You have already joined the dungeon.";
    public static string MSG_DUNGEON_JOIN_WRONG_CODE = Meta("dungeon", "join", "fail") + "You have used an incorrect command for joining the dungeon. Use !dungeon [word seen on stream] to join.";

    // JoinRaid
    public static string MSG_RAID_JOIN_WRONG_CODE = Meta("raid", "join", "fail") + "You have used an incorrect command for joining the raid. Use !raid [word seen on stream] to join.";

    // UseExpMultiplierScroll
    public static string MSG_EXP_SCROLL_MAX_REACHED = Meta("system", "exp multiplier", "fail") + "The maximum multiplier has already been reached.";
    public static string MSG_EXP_SCROLL_USED_MANY = Meta("system", "exp multiplier") + "You have used {scrollCount} Exp Multiplier Scrolls. The multiplier is now at {multiplier}.";
    public static string MSG_EXP_SCROLL_USED_ONE = Meta("system", "exp multiplier") + "You have used an Exp Multiplier Scroll. The multiplier is now at {multiplier}.";
    public static string MSG_EXP_SCROLL_NONE = Meta("system", "exp multiplier", "fail") + "You do not have any Exp Multiplier Scrolls! Redeem them under streamer loyalty on the website.";
    public static string MSG_EXP_SCROLL_OUT_OF_SYNC = Meta("system", "exp multiplier", "fail") + "Unable to use any scrolls at this time, game may be out of sync. Try again later.";
    public static string MSG_EXP_SCROLL_ERROR = Meta("system", "exp multiplier", "fail") + "Unable to use any scrolls at this time, game may be out of sync. Try again later. Error has been logged.";

    // UseFerryBoostScroll
    public static string MSG_FERRY_BOOST_NO_SCROLL = Meta("ferry", "boost", "fail") + "You don't have any ferry boost scrolls.";
    public static string MSG_FERRY_BOOST_CANNOT_USE = Meta("ferry", "boost", "fail") + "{itemName} can not be used right now.";
    public static string MSG_FERRY_BOOST_ACTIVE = Meta("ferry", "boost") + "The ferry will now move faster for {time}.";
    public static string MSG_FERRY_BOOST_ERROR = Meta("ferry", "boost", "fail") + "Error using ferry boost scroll";

    // AutoRestStatus
    public static string MSG_AUTOREST_INACTIVE = Meta("info", "rest", "auto rest") + "Auto rest is currently inactive. Use !auto rest <start minute> <stop minute> to activate.";
    public static string MSG_AUTOREST_ACTIVE_WHEN_OUT = Meta("info", "rest", "auto rest") + "Auto rest is currently active. You will auto rest until you have {autoRestStopMinutes} minutes of rested as soon as you run out of rested time.";
    public static string MSG_AUTOREST_ACTIVE = Meta("info", "rest", "auto rest") + "Auto rest is currently active. You will auto rest until you have {autoRestStopMinutes} minutes of rested. You will start resting when you have {autoRestStartMinutesLeft} minutes of rested left.";

    // AutoRest
    public static string MSG_AUTOREST_SET_FAILED = Meta("rest", "auto rest", "fail") + "Failed to set auto rest values. Make sure the start value is lower than the stop value.";
    public static string MSG_AUTOREST_ACTIVATED = Meta("rest", "auto rest") + "Auto Rest has been activated, every time you start resting will cost you {coins} coins per second you rest.";

    // AutoRestStop
    public static string MSG_AUTOREST_STOPPED = Meta("rest", "auto rest") + "Auto rest has been stopped.";

    // RaidCombatStyleSet, DungeonCombatStyleSet
    public static string MSG_COMBAT_SKILL_NOT_FOUND = Meta("info", "skill", "fail") + "No such combat skill: {skill}";
    public static string MSG_RAID_SKILL_MISSING = Meta("raid", "skill", "fail") + "You need to specify a combat skill to set. Example: !raid skill magic";
    public static string MSG_RAID_SKILL_SET = Meta("raid", "skill") + "Your combat skill during raids has been set to {skill}.";
    public static string MSG_DUNGEON_SKILL_MISSING = Meta("dungeon", "skill", "fail") + "You need to specify a combat skill to set. Example: !dungeon skill magic";
    public static string MSG_DUNGEON_SKILL_SET = Meta("dungeon", "skill") + "Your combat skill during dungeons has been set to {skill}.";

    // RaidGetCombatStyle, DungeonGetCombatStyle
    public static string MSG_RAID_SKILL_NOT_SET = Meta("raid", "skill") + "Your raid skill is not set. Use !raid skill <skill> to set it.";
    public static string MSG_RAID_SKILL_CURRENT = Meta("raid", "skill") + "Your raid skill is set to {skill}";
    public static string MSG_DUNGEON_SKILL_NOT_SET = Meta("dungeon", "skill") + "Your dungeon skill is not set. Use !dungeon skill <skill> to set it.";
    public static string MSG_DUNGEON_SKILL_CURRENT = Meta("dungeon", "skill") + "Your dungeon skill is set to {skill}";

    // RaidCombatStyleClear, DungeonCombatStyleClear
    public static string MSG_RAID_SKILL_RESET = Meta("raid", "skill") + "The skill used for raids has been reset.";
    public static string MSG_DUNGEON_SKILL_RESET = Meta("dungeon", "skill") + "The skill used for dungeons has been reset.";

    // RaidStop, DungeonStop
    public static string MSG_RAID_FORCE_STOPPED = Meta("raid", "stop") + "Raid has been forcibly stopped.";
    public static string MSG_DUNGEON_FORCE_STOPPED = Meta("dungeon", "stop") + "Dungeon has been forcibly stopped.";

    // RedeemStreamerToken
    public static string MSG_REDEEM_TOKEN_SUCCESS = Meta("redeem", "info") + "You have successfully redeemed a {itemName} for {amount} {currencyName} and now have {amountLeft} left.";
    public static string MSG_REDEEM_TOKEN_FAILED_REASON = Meta("redeem", "fail") + "Unable to redeem {itemName}. {reason}";
    public static string MSG_REDEEM_TOKEN_FAILED = Meta("redeem", "fail") + "Unable to redeem {itemName} right now.";

    // GetTokenCount, GetScrollsCount
    // MSG_SCROLL_COUNT_ITEM is filled in code, one per scroll; the joined list goes into {items}. MSG_LIST_AND sits between the last two.
    public static string MSG_TOKENS_NONE = Meta("info", "tokens") + "You do not have any seasonal tokens. Join Raids or Dungeons during seasonal events to get some!";
    public static string MSG_SCROLLS_NONE = Meta("info", "scrolls") + "You do not have any scrolls. You can redeem them under streamer loyalty on the website";
    public static string MSG_SCROLL_COUNT = Meta("info", "scrolls") + "You have {items}.";
    public static string MSG_SCROLL_COUNT_ITEM = "{amount} {itemName}(s)";
    public static string MSG_LIST_AND = " and ";

    // PlayerEq (the MSG_EQ_STAT_ fields are filled in code with Replace and joined into {stats}; the list uses MSG_LIST_AND from another part)
    public static string MSG_EQ_INVALID_TYPE = Meta("info", "equipment", "fail") + "{target} is not a valid equipment type. These are the available ones: {typeList}";
    public static string MSG_EQ_NONE_OF_TYPE = Meta("info", "equipment", "fail") + "You don't seem to have any {type} equipped.";
    public static string MSG_EQ_ITEM_STATS_A = Meta("info", "equipment") + "You have a {itemName} equipped with the following stats: {stats}";
    public static string MSG_EQ_ITEM_STATS_AN = Meta("info", "equipment") + "You have an {itemName} equipped with the following stats: {stats}";
    public static string MSG_EQ_ITEMS_STATS = Meta("info", "equipment") + "You have the following items equipped: {itemList}. These items give the total of {stats}";
    public static string MSG_EQ_STAT_ARMOR = "{armorPower} Armor";
    public static string MSG_EQ_STAT_WEAPON_POWER = "{weaponPower} Melee Weapon Power";
    public static string MSG_EQ_STAT_WEAPON_AIM = "{weaponAim} Melee Weapon Aim";
    public static string MSG_EQ_STAT_MAGIC_POWER = "{magicPower} Magic/Healing Power";
    public static string MSG_EQ_STAT_MAGIC_AIM = "{magicAim} Magic Aim";
    public static string MSG_EQ_STAT_RANGED_POWER = "{rangedPower} Ranged Weapon Power";
    public static string MSG_EQ_STAT_RANGED_AIM = "{rangedAim} Ranged Weapon Aim";

    // Mine, Chop, Fish, Farm, Gather: the word before the last suggestion in a list, "A, B or C" (companion of MSG_LIST_AND)
    public static string MSG_LIST_OR = " or ";

    // PlayerStats
    public static string MSG_STATS_SKILL_NOT_FOUND = Meta("info", "stats", "fail") + "No skill found matching: {skillName}";
    public static string MSG_STATS_ITEM_MISSING = Meta("info", "stats", "fail") + "Oh dear! {itemName} seems to be missing stats!";
    public static string MSG_STATS_ITEM = Meta("info", "stats") + "{itemName}: {stats}";

    // CraftRequirement (the MSG_CRAFTREQ_LEVEL_ fields are filled in code with Replace and joined into {equipRequirements})
    public static string MSG_CRAFTREQ_DROP_LEVEL = Meta("crafting", "info") + "{itemName} requires level {level} {skillName}.";
    public static string MSG_CRAFTREQ_EQUIP_ONLY = Meta("crafting", "info") + "{itemName} requires {equipRequirements} to equip. This item cannot be created.";
    public static string MSG_CRAFTREQ_NOT_CREATABLE = Meta("crafting", "fail") + "{itemName} is not an item that can be created.";
    public static string MSG_CRAFTREQ_RECIPE_EQUIP = Meta("crafting", "info") + "{name} requires level {level} {skillName} and {requirements}. To equip this item you require {equipRequirements}";
    public static string MSG_CRAFTREQ_RECIPE = Meta("crafting", "info") + "{name} requires level {level} {skillName} and {requirements}";
    public static string MSG_CRAFTREQ_LEVEL_ATTACK = "Level {level} Attack";
    public static string MSG_CRAFTREQ_LEVEL_DEFENSE = "Level {level} Defense";
    public static string MSG_CRAFTREQ_LEVEL_RANGED = "Level {level} Ranged";
    public static string MSG_CRAFTREQ_LEVEL_MAGIC = "Level {level} Magic or Healing";
    public static string MSG_CRAFTREQ_LEVEL_SLAYER = "Level {level} Slayer";

    // PlayerUnstuck
    public static string MSG_UNSTUCK_ALL = Meta("system", "unstuck") + "Unstucking all players.";
    public static string MSG_UNSTUCK_TRAINING_SET = Meta("system", "unstuck") + "{playerCount} players set to train All.";
    public static string MSG_UNSTUCK_TRAINING_NONE = Meta("system", "unstuck", "fail") + "No players eligible for setting training to All.";
    public static string MSG_UNSTUCK_COOLDOWN = Meta("system", "unstuck", "fail") + "Unstuck command can only be used once every 30s per character.";

    // KickPlayer
    public static string MSG_KICK_AFK_COUNT = Meta("system", "kick") + "{kickedPlayerCount} players were kicked from the game.";
    public static string MSG_KICK_PLAYER = Meta("system", "kick") + "{player} was kicked from the game.";
    public static string MSG_KICK_NOT_FOUND_SUGGEST = Meta("system", "kick", "fail") + "No player with the name '{player}' is playing. Did you mean {suggestion}?";
    public static string MSG_KICK_NOT_FOUND = Meta("system", "kick", "fail") + "No player with the name '{player}' is playing.";

    // IslandInfo (also reuses MSG_IN_DUNGEON)
    public static string MSG_ISLAND_IN_WAR = Meta("info", "location") + "You're currently in a streamer raid war.";
    public static string MSG_ISLAND_INSIDE_DUNGEON = Meta("info", "location") + "You are currently inside the dungeon.";
    public static string MSG_ISLAND_STUCK = Meta("info", "location", "stuck") + "Uh oh. Your character may be stuck.";
    public static string MSG_ISLAND_STUCK_ASK_MODERATOR = Meta("info", "location", "stuck") + "Uh oh. Your character may be stuck. {moderatorName}, could you please use !show @{player} to see if player is stuck?";

    // ClanRemove, ClanPromote, ClanDemote, ClanInvite
    public static string MSG_CLAN_PLAYER_NOT_PLAYING = Meta("clan", "fail") + "No player with name {playerName} is currently playing.";
    public static string MSG_CLAN_REMOVED = Meta("clan", "remove") + "{playerName} has been removed from the clan.";
    public static string MSG_CLAN_REMOVE_NO_PERMISSION = Meta("clan", "remove", "fail") + "You don't have permission to remove {playerName} or the player is not a member of your clan.";
    public static string MSG_CLAN_PROMOTED = Meta("clan", "promote") + "Congratulations {playerName}! You have been promoted to {roleName}!";
    public static string MSG_CLAN_PROMOTE_NO_PERMISSION = Meta("clan", "promote", "fail") + "You don't have permission to promote {playerName} or the player is not a member of your clan.";
    public static string MSG_CLAN_DEMOTED = Meta("clan", "demote") + "{playerName} has been demoted to {roleName}";
    public static string MSG_CLAN_DEMOTE_NO_PERMISSION = Meta("clan", "demote", "fail") + "You don't have permission to demote {playerName} or the player is not a member of your clan.";
    public static string MSG_CLAN_INVITE_SENT = Meta("clan", "invite") + "Invite was successfully sent. {playerName}, you may use the command '!clan accept' if you wish to join the clan {clanName}. Or '!clan decline' to decline.";
    public static string MSG_CLAN_INVITE_FAILED = Meta("clan", "invite", "fail") + "{playerName} could not be invited right now.";

    // ClanJoin, ClanAccept, ClanDecline, ClanLeave
    public static string MSG_CLAN_JOINED = Meta("clan", "join") + "You have joined {clanName} as a {roleName}!";
    public static string MSG_CLAN_JOIN_FAILED = Meta("clan", "join", "fail") + "You are not able to join that clan at this moment.";
    public static string MSG_CLAN_ACCEPT_FAILED = Meta("clan", "invite", "fail") + "Unable to accept clan invite. Either you don't have any invites or server is bonkers.";
    public static string MSG_CLAN_DECLINED = Meta("clan", "invite") + "You have declined the most recent clan invite.";
    public static string MSG_CLAN_DECLINE_FAILED = Meta("clan", "invite", "fail") + "Unable to decline clan invite. Either you don't have any invites or server is bonkers.";
    public static string MSG_CLAN_LEFT = Meta("clan", "leave") + "You have departed your clan and are no longer a member of {clanName}.";
    public static string MSG_CLAN_LEAVE_FAILED = Meta("clan", "leave", "fail") + "Unable to leave your clan right now, unknown error. Please try again later.";

    // ClanInfoHandler (MSG_CLAN_INFO_ROLE and _ROLE_MANY are filled in code with Replace and joined into {roleList})
    public static string MSG_CLAN_INFO = Meta("clan", "info") + "Your clan is {clanName} led by {ownerName}. There are currently {roleList}. A total of {memberCount} members.";
    public static string MSG_CLAN_INFO_ROLE = "{count} {roleName}";
    public static string MSG_CLAN_INFO_ROLE_MANY = "{count} {roleName}s";

    // ClanRank
    public static string MSG_CLAN_RANK = Meta("clan", "info") + "Your clan role is Level {roleLevel} - {roleName}. You joined the clan {date}.";

    // ClanStatsHandler (sent through ReplyMessageBuilder: MSG_CLAN_STATS first, then one of the two skill sentences; MSG_CLAN_STATS_SKILL is filled in code with Replace)
    public static string MSG_CLAN_STATS = Meta("clan", "info") + "Your clan is {clanName} led by {ownerName} and is currently level {clanLevel}.";
    public static string MSG_CLAN_STATS_ENCHANTING = "Your clan has an enchanting level of {level}.";
    public static string MSG_CLAN_STATS_SKILLS = "Your clan has the following skills {skills}.";
    public static string MSG_CLAN_STATS_SKILL = "{skillName} level {level}";

    // UpdateGame
    public static string MSG_UPDATE_NO_NEW_VERSION = Meta("system", "update") + "There is no new version available. If you want to forcibly update then use the command one more time.";

    // TownStats, TownHouses
    public static string MSG_VILLAGE_LEVEL = Meta("village", "info") + "Village is level {townHallLevel}, it needs {remainingExp} xp to level up.";
    public static string MSG_VILLAGE_HOUSES_NONE = Meta("village", "info") + "No players assigned to houses.";

    // SetVillageHuts
    public static string MSG_VILLAGE_HUTS_INVALID_SKILL = Meta("village", "fail") + "{targetName} is not a valid skill name for the huts. Make sure you use the full and proper names. Ex. Healing and not heal.";
    public static string MSG_VILLAGE_HUTS_UPDATING = Meta("village", "info") + "Updating village to target {targetName}. Please wait...";

    // PlayerLogUpload, GameStateUpload
    public static string MSG_UPLOAD_BROADCASTER_MODERATOR_ONLY = Meta("system", "fail") + "Only the Broadcaster and Moderators can use this command.";

    // PlayerLeave
    public static string MSG_LEAVE_LEFT_GAME = Meta("system", "leave") + "You have left the game.";

    // MonsterPlayer
    public static string MSG_MONSTER_FAILED = Meta("system", "monster", "fail") + "Player could not be turned into a monster right now :(";

    // GetStatusEffects
    public static string MSG_STATUS_EFFECTS_NONE = Meta("info", "status effects") + "You do not have any active status effects.";

    // FerryTravel
    public static string MSG_FERRY_SAIL_NO_PERMISSION = Meta("travel", "sail", "fail") + "You do not have permission to set this player to sail.";

    // FerryInfo (sent through ReplyMessageBuilder: one of the first two, then the captain and boost sentences when they apply)
    public static string MSG_FERRY_INFO_DESTINATION = Meta("info", "ferry") + "The ferry is currently sailing towards {destination} with {playerCount} players on board.";
    public static string MSG_FERRY_INFO = Meta("info", "ferry") + "The ferry is currently sailing with {playerCount} players on board.";
    public static string MSG_FERRY_INFO_CAPTAIN = "{captainName} is the captain with {sailingLevel} sailing.";
    public static string MSG_FERRY_INFO_BOOST = "There is an active ferry boost giving 5x the speed, boost ends in {timeLeft}.";

    // ClientVersion
    public static string MSG_CLIENT_VERSION = Meta("system", "version") + "The streamer is currently running Ravenfall v{version}";

    // Dps (the stat pieces are joined with spaces by ReplyMessageBuilder)
    public static string MSG_DPS_NONE = Meta("info", "dps") + "You have not dealt any damage or healed anyone yet.";
    public static string MSG_DPS_DPS = "⚔️ {DPS} DPS";
    public static string MSG_DPS_MAX_DPS = "⚔️ {highestDPS} Max DPS";
    public static string MSG_DPS_MAX_DAMAGE = "💥 {highestDamage} Max DMG";
    public static string MSG_DPS_TOTAL_DAMAGE = "💥 {totalDamage} Total";
    public static string MSG_DPS_HPS = "🧙 {HPS} HPS";
    public static string MSG_DPS_MAX_HPS = "🧙 {highestHPS} Max HPS";
    public static string MSG_DPS_MAX_HEAL = "❤️ {highestHeal} Max HEAL";
    public static string MSG_DPS_TOTAL_HEALING = "❤️ {totalHealing} Total";

    // RaidStreamer: the {raidType} in MSG_REQ_RAID
    public static string MSG_REQ_RAID_TYPE_WAR = "war raid";
    public static string MSG_REQ_RAID_TYPE_RAID = "raid";

    // BuyItemFromMarket, UseMarketplace
    public static string MSG_MARKET_BOUGHT = Meta("marketplace", "buy", "info") + "{itemName} was bought for {price} coins.";
    public static string MSG_MARKET_BOUGHT_EQUIPPED = Meta("marketplace", "buy", "info") + "{itemName} was bought and equipped for {price} coins.";
    public static string MSG_MARKET_BOUGHT_MANY = Meta("marketplace", "buy", "info") + "{totalAmount}x {itemName} was bought for {price} coins.";
    public static string MSG_MARKET_BOUGHT_MANY_EQUIPPED = Meta("marketplace", "buy", "info") + "{totalAmount}x {itemName} was bought and equipped for {price} coins.";

    // PlayerItemDropText: built in the game, so {0} style. {0} is the player, {1} the item.
    public static string MSG_DROP_YOU_FOUND_A = "{0} you found a {1}";
    public static string MSG_DROP_YOU_FOUND_AN = "{0} you found an {1}";

    // {0} is the item, {1} the list of players
    public static string MSG_DROP_FOUND_BY = "{0} was found by {1}.";

    // the word before the last name in a list: "A, B and C"
    // DropEventManager
    public static string MSG_DROP_EVENT_STARTED = Meta("system", "item drop event") + "Great news everyone! {amount}x {itemName} was dropped in the world!";

    // Window resize grips
    public static string UI_WINDOW_RESIZE = "Drag to resize";
    public static string UI_WINDOW_RESIZE_WIDTH = "Drag to make wider or narrower";

    // Item tooltips: enchantments that raise power or aim of any kind
    public static string UI_ENCHANT_POWER = "Power";
    public static string UI_ENCHANT_AIM = "Aim";

    // Player details preview
    public static string UI_DETAILS_PREVIEW_RESET = "Reset view";
    public static string UI_DETAILS_PREVIEW_HINT = "Drag to turn, scroll to zoom, drag with the middle button to move.";
}
