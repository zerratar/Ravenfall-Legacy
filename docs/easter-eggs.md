# Easter eggs

Every silly thing hidden in Ravenfall, old and new: where it is, what makes it happen, how often, and what turns it
off. It is a developer document and spoils everything; players should find these themselves. When an area is
redesigned, keep its eggs (move them, do not delete them) and add new ones; the owner loves them.

The new batch of 2026-11-23 (v0.9.7) lives in `Assets/Scripts/Game/Fun/` (game) and `OddFinds` /
`AchievementManager` (RavenNest). Most of it follows one setting:

**Settings > Game > World > Silly surprises** (`SettingsApplier.SillySurprises`, saved as `sillySurprises`, on by
default). Off hides the world scenes, stops the moments, Rob Boss's paintings and the special days. It is always off
in potato mode and with graphics off. Odd finds, item descriptions, `!use` lines, achievements, bottle notes and menu
tips are not visual and do not follow it.

Testing helpers:

- Debug menu (right side, editor): **April Fools: by date / ON / OFF** cycles `SillySurprises.ForceAprilFools`.
- `SillySurprises.TestDay = new DateTime(2026, 11, 13)` (from an eval) shows a day's world scenes (Friday the 13th).
- `SillySurprises.Trigger("ow" | "rock" | "slap" | "nap" | "brrr" | "arr")` plays a moment now.
- `RobBossPainter.PaintNow(null | "view" | "player" | "woodcutter" | "clucksworth" | "chickens" | "lady" | "monkeys" | "hair")`.
- **Ravenfall > Mute Chat In Play Mode** (editor menu, per machine) sets `AdminControlData.NoChatBotMessages` when the
  scripts load for play mode, before the scene and the sign in, so a test cannot leak to the real Twitch chat.
- Captures used while placing the scenes: `docs/mockups/easter-eggs-*.png`.

## Placed in the world (since 2026-11-23)

Made by `tools/easter-eggs/world-gags.cs` into the local prefab `Assets/Resources/EasterEggs/World Gags.prefab`
(Resources is not in git, like every prefab: rerun the tool on a new machine). `SillySurprises` puts it in the world
when the game starts, so **MainWorld is never changed**. Each scene is a `WorldGag` (key, optional day, optional
island to hide with); its signs are `TextMeshPro` on a standing sign board and take their words from Localization
(`UI_GAG_SIGN_*`) when shown. Animals are the Low Poly Animated Animals pack (navmesh agents and missing scripts
taken off, `MonkeyVoice` answers their sound events, `SillyAnimalPose` holds a pose, culled when off screen). Static
otherwise; `SillyBob` gently bobs a floating thing while it is on screen.

| Key | Where | What |
|---|---|---|
| `ironhill-sheep-on-the-anvil` | Ironhill, the Dwarf Hold forge (the forge-master's anvil, 25, 5, 457) | A sheep standing on the anvil, and PLEASE DO NOT LEAVE SHEEP ON THE ANVIL. Pays off bottle note 18. |
| `home-seagull-sandwich` | Home, the shore by the fishing spot (121, -3, 79) | DO NOT FEED THE SEAGULLS, with a seagull sitting on top next to its stolen sandwich. Bottle notes 9, 10, 13. |
| `away-wanted-left-boot` | Away, by the pirates' market (229, -2, -288) | WANTED: ONE LEFT BOOT. REWARD 3 COINS. ASK FOR THE CAPTAIN. Bottle note 16 and the Old Boot odd find. |
| `heim-skeleton-fisher` | Heim, the end of the north pier (-299, -2, -382) | A skeleton lying by his rod, BITING ANY MINUTE NOW. |
| `heim-fish-in-a-scarf` | Heim, the half barrel on the shore (-307, -2, -392) | A fish bundled up in three scarves. Bottle note 32 ("the fish there wear three"). |
| `kyo-quackers-in-the-onsen` | Kyo, the water by the hot spring (-551, -3.4, 67) | A little yellow duckling bobbing in the water: Quackers, "last seen floating past Kyo" (bottle note 19). |
| `ravebridge-no-cow-level` | Ravebridge, the cow field gate (-1590, 3, 1699) | THERE IS NO COW LEVEL. Under the Ravebridge island object, so it hides with the island. |
| `home-friday-13th-black-cat` | Home, the town hall square (-15, 8, -120), **Friday the 13th only** | A black cat staring at you, and BAD LUCK TODAY. PET THE CAT ANYWAY. |
| `home-april-king-clucksworth` | Home, in front of the town hall fountain (-20, 8, -118), **April 1st only** | King Clucksworth, a hen in a big crown, and ALL HAIL KING CLUCKSWORTH, KING OF EVERYTHING. |
| `Templates` (hidden) | none | The fish for the fish slap, the party hat and the traffic cone for April 1st. |

## Rob Boss paints (since 2026-11-23)

`RobBoss` is the painter on the hill on Home (-91, 16, 128), standing at an easel whose canvas
(`RobBossPainting/Frame`) was blank. `RobBossPainter` (added at runtime, found by name) now paints on it:

- **When:** the first painting 2 to 4 minutes after the start, then every 9 to 16 minutes, only while the main camera
  is within 160 m (nobody would see it otherwise). The canvas keeps the last painting.
- **How:** he says one of ten lines in his own spirit (`UI_ROB_QUOTE_n`: "There are no mistakes here. Only happy
  little goblins."), the canvas goes blank and the picture appears dab by dab over 28 seconds, then the gallery card
  floats over it: `"Tree, Moments Before Bot123" by Rob Boss. 420 coins. Not for sale.` (`UI_ROB_GALLERY`, titles
  `UI_ROB_TITLE_<SUBJECT>_n`, a random price).
- **What:** a real snapshot: one render of a camera of its own at 576 x 768, shrunk to 288 x 384 and run through a
  painterly filter on the CPU (a Kuwahara oil paint smoothing, more color, fewer colors, warmth, canvas grain, a ragged
  bare rim), a few rows a frame. Subjects: the view from his hill, the player standing nearest to him (within 45 m),
  a woodcutter and the tree about to have a bad day, Sir Clucksworth (when the top town hall tier shows him), the
  screaming lady, the Mangrove monkey chief, and a self-portrait that is only his hair.
- **April 1st:** only chickens (King Clucksworth), when there is one to paint.
- **Off:** the Silly surprises setting (so potato mode and graphics off).

## Rare silly moments (since 2026-11-23)

`SillySurprises` plays one every 12 to 25 minutes (the first after 6 to 15), on the island the camera watches when
there is someone to pick there. Words over heads only (the effect words view, bold and outlined, stream readable); no
chat, no change to anyone's state. Off with the setting.

| Kind | What |
|---|---|
| `ow` | A woodcutter's tree says "Ow!", and a moment later the woodcutter says "Sorry!". |
| `rock` | A miner shouts "Rock and stone!" and up to three others on the island shout it back. |
| `slap` | A fish leaps out of the water, slaps a fisher ("Slap!") and drops back in. |
| `nap` | A pet out with its owner dozes: "Zzz", three times. |
| `brrr` | Someone on Heim shivers: "Brrr!". |
| `arr` | Talk Like a Pirate Day (September 19) only: someone says "Arr!". |

## Special days (since 2026-11-23)

By the streamer's own clock. All off with the setting.

- **April 1st** (the owner's favorite; force it with the debug menu button):
  - Big head mode: every character's head bone at 1.7 times its size.
  - Party hats and traffic cones on the enemies near the camera and on every pet out (checked every 5 seconds).
    Without help the hat is a guess (head bone, or the top front of the body, sized by height), which is off on
    cows (no head bone, so it floats) and chickens (too big). Fix one by hand: add a **Hat Fit** component to the
    enemy or pet prefab, set its Anchor to the head or neck bone, press "Show hat preview", move/turn/scale the
    preview hat in the scene view, then "Take values from preview" and save the prefab. "No Hat" skips it.
    Test with the debug menu's "April Fools" button.
  - King Clucksworth in front of the town hall (above), and Rob Boss paints only chickens.
  - Bottle notes are pranksters' (`UI_BOTTLE_APRIL_n`, 8 of them) instead of the server's note; the server's roll,
    the map chance and everything else stay as they are.
  - Item tooltips (the player details window) put a silly word in front of an item's name, the same one every time
    for that item: "Slightly Haunted Bronze Sword", "Gently Sneezed On Old Boot" (`UI_APRIL_ITEM_WORDS`).
  - One line of fake patch notes in chat, once a session, 10 minutes after April 1st starts in it
    (`MSG_APRIL_PATCH_NOTES`): "Patch notes 0.9.7.1: chickens now outrank players. Trees fight back on Thursdays...".
  - Older: an empty "1st of April" season object in MainWorld's `SeasonEventManager`, free for more.
- **Friday the 13th:** the black cat on the town hall square (above).
- **September 19 (Talk Like a Pirate Day):** the `arr` moment joins the others.
- Older, scene objects (`SeasonEventManager`): Halloween (October, Home's graveyard, witches, scarecrows and pumpkins),
  Christmas and New Year groups.

## Odd finds while gathering (since 2026-11-23)

Server rolled (`RavenNest.BusinessLogic.Game.OddFinds`, `ResourceTaskProcessor.GatherRound`): its own roll, apart
from the drop table, so it never replaces a real find, the Gathering tree and pets do not change it, and the website's
drop lists do not give it away. **1 in 3,000 gathering rounds** (a round is 10 seconds: about one in 8 hours), and
never two within **6 hours** for one character (kept in memory; a server restart forgets it). Worth 1 coin at the
vendor, tradable, never sold or crafted (`GameData.EnsureOddFinds`). The server tells the game (game event 61,
`OddFind`); the game shows "Found: Old Boot!" over the character (always, it is a real item) and says one line in chat
at most once every 10 minutes for the whole stream (`OddFindEventHandler.ChatEverySeconds`). Each counts for the hidden
`oddities` achievement.

| Item | Skill | Found line (chat) |
|---|---|---|
| Old Boot | Fishing (anywhere, at sea too) | "{player} reeled in an Old Boot. A left one. The Pirates of Away will want to hear about this." |
| Quackers | Fishing on Kyo (half the odd finds there) | "{player} fished up Quackers the rubber duck! Whoever sent that bottle can stop worrying now." |
| Pet Rock | Mining | "{player} mined out a suspiciously round rock and has decided to keep it as a pet." |
| Birds Nest | Woodcutting | "A Birds Nest fell out of the tree {player} was chopping. It is empty, apart from a note." |
| Lookalike Potato | Farming | "{player} dug up a potato that looks a bit like someone famous. Nobody can say who." |
| Very Good Stick | Gathering | "{player} found a Very Good Stick. Not just a good stick. A very good one." |

`!use <item>` says one of 3 or 4 lines (`UI_ODD_<KEY>_n` in `Localization.cs`, picked in `OddThings`); the item stays,
no server call. `!examine` shows the server's funny description. Icons: Old Boot, Pet Rock, Birds Nest and Very Good
Stick drawn with the Image Generator (IconForge); Quackers reuses the Rubber Duck icon and the Lookalike Potato the
Potato icon (it looks like a potato, which is the joke).

## !use lines for things that did nothing (since 2026-11-23)

`OddThings` in `UseItem`:

- The tower towns' keepsakes: the **Rubber Duck** ("Squeak. Ylva would be proud of you."), the **Holy Hand Grenade**
  ("...one, two, five! Three, sir! Three! ...nothing happens. Pip took the boom out, for safety.") and **Woollys
  Bell** ("Ding. Somewhere above the clouds, a sheep goes baa."), two lines each.
- Words that are no item: `!use xyzzy` ("Nothing happens."), `!use plugh` ("A hollow voice says: Fool."),
  `!use the force` / `force`, `!use head` / `your head` / `brain` ("{player} uses their head. Ow."),
  `!use cheats` / `hacks` ("Cheats on! Just kidding. The ravens saw that.").

## Funny item descriptions (2026-11-23)

Server item rows (`GameData.Entities.cs`, applied at start by `GetOrCreateItem`), seen with `!examine` and on the
website: Water, Mushroom, Salt, Sand, Cabbage (Nibbles), Eggs and Raw Chicken (Sir Clucksworth), Wheat, Tomato, Potato
(the Lookalike Potato), Paper and Vial (bottles), Logs, Raw Sprat (seagulls), Raw Puffer Fish, Ham Sandwich (seagulls),
Copper Ore, Tin Ore, Coal, Wood Plank, Abraxas Token. Older funny ones: Message in a Bottle, Treasure Map, Banana,
Mango, Golden Banana, Raw Anglerfish, Raw Sea Serpent, Holy Hand Grenade, Rubber Duck, Woollys Bell, Frostbeard Amulet.

## Hidden achievements and titles

Counted on the server (`AchievementManager`), hidden on the website until earned, announced in chat as a secret
(`AchievementReachedEventHandler`):

| Key | How | Title |
|---|---|---|
| `wrong` | 5 wrong quest answers | the Confidently Wrong |
| `quitter` | 10 quests given up | the Quitter |
| `nibbled` | knocked out by Nibbles the Vicious, the tower's floor 1 bunny | the Bunny Snack |
| `treasure` | the rare find in a Treasure Map's treasure | the Treasure Hunter |
| `oddities` (new) | 5 odd finds while gathering | the Hoarder |
| `penpal` (new) | 25 Messages in a Bottle opened | zerratar's Pen Pal |

## Messages in bottles

`!use message in a bottle` (fished at sea, 2% a round, an hour apart): one of 53 notes (48 plus 5 rare) the server
picks and the game words (`UI_BOTTLE_NOTE_n`), or one bottle in eight a Treasure Map. Hidden from the `!use` help. Many
notes set up jokes placed in the world since: the sandwich-stealing seagull (Home), the left boot (Away), the sheep on
the anvil (Ironhill), the scarves on Heim's fish, Quackers off Kyo. Details: `docs/own-boats.md`, "Messages in bottles".

## Older eggs (keep them)

- **The screaming lady** (`ScreamingLady.cs`): turns to face the camera and screams when it comes close.
- **The QR code tapestry** on a wall.
- **The town hall, top tier** (2026-10-12 rebuild, `TownHall_Tier_5`): Sir Clucksworth (now "Sir Clucksworth the
  Dethroned", in a paper bag beside the fountain), Sir Honksworth the Crowned (a goose who took his crown), a raven in
  a party hat, a knight statue wearing a traffic cone.
- **The Mangrove island** (`docs/banana-island.md`): the "insufficient bananas" sign, NO PIRATES and NO MONKEYS signs
  (the monkeys cannot read), the monkeys' banana pile, the regatta's gags (swim tow, human propeller, hanging on for
  dear life, the Kraken).
- **Ravebridge** (`docs/ravebridge-design.md`): a Lumbridge tribute; Commaster and RavenMMO the cows, the Count by his
  coffin after dark, the duke who never gives quests.
- **The Ironhill runestone:** "Power is emerging from this area... But nothing seem to be happening."
- **The tower:** Nibbles the Vicious and his big sister Snowball; the Holy Hand Grenade, the Rubber Duck and Woolly's
  Bell keepsakes.
- **Menu tips** (`Localization.UI_MENU_TIPS`, the Esc menu): "Sir Clucksworth sees everything." and friends; eight more
  added 2026-11-23 ("Tip: There is no cow level.", "Tip: Nothing happens when you !use xyzzy. Or does it?").
- **Diaper mode** (`!diaper`), `!big` and `!small`.
