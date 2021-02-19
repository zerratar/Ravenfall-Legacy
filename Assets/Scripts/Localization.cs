public static class Localization
{
    public const string GAME_NOT_LOADED = "Game has not finished loading yet, try again soon!";
    public const string GAME_NOT_READY = "Game is not ready yet!";

    public const string MSG_COMMAND_COOLDOWN = "You must wait another {cooldown} secs to use that command.";

    public const string MSG_NOT_PLAYING = "You are not currently playing. Use !join to start playing!";
    public const string MSG_STATS = "Combat level {combatLevel}, {skills} -- TOTAL {total} --, Eq - power {weaponPower}, aim {weaponAim}, armor {armorPower}";
    public const string MSG_SKILL = "{skill}, {currenExp} / {requiredExp} EXP.";
    public const string MSG_RESOURCES = "Wood {wood}, Ore {ore}, Fish {fish}, Wheat {wheat}, Coin {coins}";
    public const string MSG_TOKENS = "You have {tokenCount} {tokenName}(s) you can use.";

    public const string MSG_PLAYER_INSPECT_URL = "https://www.ravenfall.stream/inspect/{characterId}";
    public const string MSG_PLAYERS_ONLINE = "There are currently {playerCount} players in the game.";

    public const string MSG_MULTIPLIER_ENDS = "The current exp multiplier (x{multiplier}) will end in {minutes}{seconds}.";
    public const string MSG_MULTIPLIER_LIMIT = "The exp multiplier limit has been set to {expMultiplier}!";
    public const string MSG_MULTIPLIER_INCREASE = "You have increased the exp multiplier with {added}. It is now at {multiplier}!";
    public const string MSG_MULTIPLIER_RESET = "You have reset the exp multiplier! NotLikeThis";
    public const string MSG_MULTIPLIER_SET = "The exp multiplier has been set to {amount}!";

    public const string MSG_SUB_CREW = "Welcome to the {streamerDisplayName} crew! You will now gain {tierMultiplier}x more exp on this stream!";
    public const string MSG_BIT_CHEER = "Cheer {bitsLeft} more bits for a {tokenName}";
    public const string MSG_BIT_CHEER_INCREASE = "You have increased the multiplier timer with {multiAdded} minutes with your {bits} cheer!! We only need {bitsLeft} more bits for increasing the timer! <3";
    public const string MSG_BIT_CHEER_LEFT = "We only need {bitsLeft} more bits for increasing the multiplier timer!";
    public const string MSG_SUB_TOKEN_REWARD = "You were rewarded {amount} {tokenName}(s)!";

    public const string MSG_RAID_START = "A level {raidLevel} raid boss has appeared! Help fight him by typing !raid";
    public const string MSG_RAID_START_ERROR = "Raid cannot be started right now.";

    public const string MSG_DUEL_REQ = "A duel request received from {opponent}, reply with !duel accept or !duel decline";
    public const string MSG_DUEL_REQ_TIMEOUT = "The duel request from {requester} has timed out and automatically declined.";
    public const string MSG_DUEL_REQ_DECLINE = "Duel with {requester} was declined.";
    public const string MSG_DUEL_REQ_ACCEPT = "You have accepted the duel against {requester}. May the best fighter win!";
    public const string MSG_DUEL_WON = "You won the duel against {opponent}!";

    public const string MSG_DUEL_ACCEPT_FERRY = "You cannot accept a duel while on the ferry.";
    public const string MSG_DUEL_ACCEPT_IN_DUEL = "You cannot accept the duel of another player as you are already in a duel.";
    public const string MSG_DUEL_ACCEPT_IN_ARENA = "You cannot accept the duel of another player as you are participating in the Arena.";
    public const string MSG_DUEL_ACCEPT_IN_RAID = "You cannot accept the duel of another player as you are participating in a Raid.";
    public const string MSG_DUEL_ACCEPT_NO_REQ = "You do not have any pending duel requests to accept.";

    public const string MSG_FERRY_ALREADY_ON = "You're already on the ferry.";
    public const string MSG_FERRY_ALREADY_WAITING = "You're already waiting for the ferry.";

    public const string MSG_DISEMBARK_ALREADY = "You're already disembarking the ferry.";
    public const string MSG_DISEMBARK_FAIL = "You're not on the ferry.";

    public const string MSG_ISLAND_ON_FERRY_DEST = "You're currently on the ferry, going to disembark at '{destination}'.";
    public const string MSG_ISLAND_ON_FERRY = "You're currently on the ferry.";
    public const string MSG_ISLAND = "You're on the island called {islandName}";

    public const string MSG_ARENA_UNAVAILABLE_WAR = "Arena is unavailable during wars. Please wait for it to be over.";
    public const string MSG_ARENA_WRONG_ISLAND = "There is no arena on the island you're on.";
    public const string MSG_ARENA_FERRY = "You cannot join the arena while on the ferry.";
    public const string MSG_ARENA_ALREADY_JOINED = "You have already joined the arena.";
    public const string MSG_ARENA_ALREADY_STARTED = "You cannot join as the arena has already started.";
    public const string MSG_ARENA_JOIN_ERROR = "You cannot join the arena at this time.";
    public const string MSG_ARENA_JOIN = "You have joined the arena. Good luck!";

    public const string MSG_BUY_ITEM_NOT_FOUND = "Could not find an item matching the query '{query}'";
    public const string MSG_BUY_ITEM_NOT_IN_MARKET = "Could not find any {itemName} in the marketplace.";
    public const string MSG_BUY_ITEM_INSUFFICIENT_COIN = "You do not have enough coins to buy the {itemName}.";
    public const string MSG_BUY_ITEM_MARKETPLACE_ERROR = "Error accessing marketplace right now.";
    public const string MSG_BUY_ITEM_ERROR = "Error buying {itemName}. Server returned an error. :( Try !leave and then !join to see if buying it was successeful or not.";
    public const string MSG_BUY_ITEM_TOO_LOW = "Unable to buy any {itemName}, the cheapest asking price is {cheapestPrice}.";

    public const string MSG_IN_DUEL = "You are currently fighting in a duel.";
    public const string MSG_IN_RAID = "You are currently fighting in the raid.";

    public const string MSG_RAID_STARTED = "There is an active raid.";
    public const string MSG_DUNGEON_STARTED = "There is an active dungeon.";
    public const string MSG_IN_DUNGEON = "You're currently in the dungeon.";

    public const string MSG_STREAMRAID_NO_PLAYERS = "{raiderName} raided but without any players. Kappa";
    public const string MSG_STREAMRAID_WAR_NO_PLAYERS = "{raiderName} raided with intent of war but we don't have any players. FeelsBadMan";

    public const string MSG_JOIN_WELCOME = "Welcome to the game!";
    public const string MSG_JOIN_FAILED = "Failed create or find a player with the username {player}";
    public const string MSG_JOIN_FAILED_ALREADY_PLAYING = "You're already playing!";

    public const string MSG_REQ_RAID = "Requesting {raidType} on {streamer}s stream!";
    public const string MSG_REQ_RAID_SOON = "The raid will start any moment now!";
    public const string MSG_REQ_RAID_FAILED = "Request failed, streamer is no longer playing or has disabled raids. :(";

    public const string MSG_CRAFT_FAILED = "{category} {type} cannot be crafted right now.";
    public const string MSG_CRAFT_FAILED_FERRY = "You cannot craft while on the ferry";
    public const string MSG_CRAFT_FAILED_STATION = "You can't currently craft weapons or armor. You have to be at the crafting table by typing !train crafting";
    public const string MSG_CRAFT_FAILED_LEVEL = "You can't craft this item, it requires level {reqCraftingLevel} crafting.";
    public const string MSG_CRAFT_FAILED_RES = "Insufficient resources to craft {itemName}";
    public const string MSG_CRAFT_EQUIPPED = "You crafted and equipped a {itemName}!";
    public const string MSG_CRAFT_ITEM_NOT_FOUND = "Could not find an item matching the query '{query}'";
    public const string MSG_CRAFT = "You crafted a {itemName}!";
    public const string MSG_CRAFT_MANY = "You crafted x{amount} {itemName}s!";

    public const string MSG_GIFT = "You gifted {giftCount}x {itemName} to {player}!";
    public const string MSG_GIFT_PLAYER_NOT_FOUND = "Could not find an item or player matching the query '{query}'";
    public const string MSG_GIFT_ITEM_NOT_FOUND = "Could not find a matching the query '{query}'";
    public const string MSG_GIFT_ERROR = "Error gifting {giftCount}x {itemName} to {player}. FeelsBadMan";

    public const string MSG_EQUIPPED = "You have equipped {itemName}.";
    public const string MSG_EQUIPPED_ALL = "You have equipped all of your best items.";

    public const string MSG_UNEQUIPPED = "You have unequipped {itemName}.";
    public const string MSG_UNEQUIPPED_ALL = "You have unequipped all of your items.";

    public const string MSG_SET_PET = "You have changed your active pet to {itemName}";
    public const string MSG_ITEM_NOT_FOUND = "Could not find an item matching the name: {query}";
    public const string MSG_SET_PET_NOT_PET = "{itemName} is not a pet.";
    public const string MSG_SET_PET_NOT_OWNED = "You do not have any {itemName}.";

    public const string MSG_ITEM_NOT_EQUIPPED = "You do not have {itemName} equipped.";

    public const string MSG_ITEM_NOT_OWNED = "You do not have any {itemName}.";

    public const string MSG_GET_PET_NO_PET = "You do not have any pets equipped.";
    public const string MSG_GET_PET = "You currently have {petName} equipped.";

    public const string MSG_APPEARANCE_INVALID = "Invalid appearance data";

    public const string MSG_TRAVEL_NO_SUCH_ISLAND = "No islands named '{islandName}'. You may travel to: '{islandList}'";
    public const string MSG_TRAVEL_WAR = "You cannot travel when participating in a war. Please wait for it to be over.";

    public const string MSG_HIGHEST_SKILL = "{player} has the highest level {skillName} with level {level}.";
    public const string MSG_HIGHEST_COMBAT = "{player} has the highest combat level with {combatLevel}.";
    public const string MSG_HIGHEST_TOTAL = "{player} has the highest total level with {level}.";

    public const string MSG_VILLAGE_BOOST = "Village is level {townHallLevel}, active boosts {activeBoosts}";

    public const string MSG_HIGHSCORE_RANK = "You're at rank #{rank}";
    public const string MSG_HIGHSCORE_MAIN_ONLY = "Only your main character is listed on the highscore right now.";
    public const string MSG_HIGHSCORE_BAD_SKILL = "{skillName} is not a skill in the game. Did you misspell it?";

    public const string MSG_MAX_MULTI = "Current Exp Multiplier is at {multiplier}.";
    public const string MSG_MAX_MULTI_ALL = "Current Exp Multiplier is at {multiplier}. Max is {maxMultiplier}. Currently {cheerPot} bits.";
    public const string MSG_MAX_MULTI_BITS = "Max exp multiplier is {maxMultiplier}. Currently {cheerPot} bits.";

    public const string MSG_REDEEM = "{itemName} was redeemed for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public const string MSG_REDEEM_EQUIP = "{itemName} was redeemed and equipped for {tokenCost} tokens. You have {tokensLeft} tokens left.";
    public const string MSG_REDEEM_FAILED = "{itemName} could not be redeemed. Please try again.";
    public const string MSG_REDEEM_INSUFFICIENT_TOKENS = "Not enough tokens to redeem this item. You have {tokenCount} but need {tokenCost}.";
    public const string MSG_REDEEM_NOT_REDEEMABLE = "This item can not be redeemed yet.";
    public const string MSG_REDEEM_ITEM_NOT_FOUND = "Could not find a redeemable item matching the query '{query}'";

    public const string MSG_SELL_TOO_MANY = "You cannot sell {itemAmount}x {itemName}. Max is {maxValue}.";
    public const string MSG_SELL_ITEM_NOT_FOUND = "Could not find an item matching the name: {query}";
    public const string MSG_SELL_ITEM_NOT_OWNED = "You do not have any {itemName} in your inventory.";
    public const string MSG_SELL_MARKETPLACE_ERROR = "Error accessing marketplace right now.";
    public const string MSG_SELL = "{itemAmount}x {itemName} was put in the marketplace listing for {itemPrice} per item.";

    public const string MSG_VALUE_ITEM = "{itemName} can be sold for {vendorPrice} in the !vendor";
    public const string MSG_VALUE_ITEM_NOT_FOUND = "Could not find an item matching the name: {query}";

    public const string MSG_VENDOR_ITEM = "You sold {vendorCount}x {itemName} to the vendor for {cost} coins!";
    public const string MSG_VENDOR_ITEM_NOT_FOUND = "Could not find an item matching the name: {query}";
    public const string MSG_VENDOR_ITEM_FAILED = "Error selling {vendorCount}x {itemName} to the vendor. FeelsBadMan";

    public const string MSG_DUNGEON_START_FAILED_WAR = "Unable to start a dungeon during a war. Please wait for it to be over.";
    public const string MSG_DUNGEON_START_FAILED_RAID = "Unable to start a dungeon during a raid. Please wait for it to be over.";

    public const string MSG_TICTACTOE_PLAY = "Use !ttt 1-9 to play!";
    public const string MSG_TICTACTOE_STARTED = "A game of Tic Tac Toe has started! Use !tictactoe 1-9 to play!";
    public const string MSG_TICTACTOE_JOINED = "You have joined team {teamName}!";
    public const string MSG_TICTACTOE_REJECT_GAMEOVER = "Your action was rejected because the game is over.";
    public const string MSG_TICTACTOE_REJECT_TURN = "Your action was rejected as it is not their turn yet.";
    public const string MSG_TICTACTOE_REJECT_NUMBER = "Your action was rejected because the number was not part of the grid.";
    public const string MSG_TICTACTOE_REJECT_PLAYED = "Your action was rejected because someone else already played this number.";
    public const string MSG_TICTACTOE_END_DRAW = "The game ended in a draw. No one wins.";
    public const string MSG_TICTACTOE_WIN = "Team {winningTeam} wins!";
    public const string MSG_TICTACTOE_WON_TOKEN = "You won a {tokenName}!";

    public const string MSG_DIAPER_ON = "Diaper mode has been enabled, you have unequipped all armor.";
    public const string MSG_DIAPER_OFF = "Diaper mode has been disabled, you have equipped your armor again.";

    public const string MSG_TOGGLE_PET_NO_PET = "You have no more pets to cycle between.";
    public const string MSG_TOGGLE_PET = "You have changed your active pet to {itemName}";

    public const string MSG_TRAINING = "You're currently training {skill}.";
    public const string MSG_TRAINING_NOTHING = "You're not training anything. Use !train <skill name> to start training!";

    public const string MSG_JOIN_RAID = "You have joined the raid. Good luck!";
    public const string MSG_JOIN_RAID_FERRY = "You cannot join the raid while on the ferry.";
    public const string MSG_JOIN_RAID_WAR = "You cannot fight a raid boss during a war!";
    public const string MSG_JOIN_RAID_ALREADY = "You have already joined the raid.";
    public const string MSG_JOIN_RAID_PAST_HEALTH = "You can no longer join the raid.";
    public const string MSG_JOIN_RAID_ARENA = "You cannot join the raid while in the arena.";
    public const string MSG_JOIN_RAID_DUEL = "You cannot join the raid while in a duel.";
    public const string MSG_JOIN_RAID_NO_RAID = "There are no active raids at the moment.";

}