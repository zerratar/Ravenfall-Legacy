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
}