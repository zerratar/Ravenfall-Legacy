# Legendary weapons

The Legendary tier (v0.9.7, 2026-11-23): true endgame weapons, so maxed players have something left to chase. The
owner approved the plan on 2026-11-23: "go ahead with the legendary plan, 950 is fine. I want it to be the best
available weapons in the game as it grows. great idea". This page is the design with its numbers; "Built" and
"Tested" at the end say what exists and what was played.

## 1. The five

The Dark Fortress uniques (docs/dark-fortress-design.md) that were rare chest finds become the first Legendaries. They
keep their models, names, ids and their fortress touch (life steal, fire, thorns, a crit chance); they leave the chest
table for good. Skullcrusher, the Gravebloom Warhammer and the Sunstaff of the Inquisitor stay rare chest uniques, and
Dreadforged stays crafted.

| Legendary | Type | Worn with | Found by (950) and the ending of | Power |
|---|---|---|---|---|
| Soulreaver | 2H sword | Attack 950 | Attack, Precision | **Soul Harvest** |
| Hellhorn Mace | 1H (axe slot) | Attack 950 | **Strength, Might** | **Dread** |
| Batwing Staff | staff | Magic 950 | Magic, Elements | **Bat Swarm** |
| Spiked Bulwark | shield | Defense 950 | Defense, Bulwark | **Iron Aegis** |
| Fortress Hand Cannon | bow | Ranged 950 | Ranged, Marksman | **Thunder Shot** |

The Hellhorn Mace is found by Strength 950 and the Might ending (approved 2026-11-23), so it is not a second
Precision find beside the Soulreaver. Items have no Strength requirement (the classic columns are Attack, Defense,
Magic, Ranged and Slayer), so it is still worn with Attack 950, like every one handed weapon
(`LegendaryInfo.Finder`).

## 2. Who can find one

The server checks all of it per character at the moment a boss falls. A character qualifies for a Legendary when:

1. its level in that Legendary's finding skill is **950** or more (the item asks for 950 too, so a new player cannot use
   one even if it had one);
2. it has **fought in a clear on the tower's top floor**: any part of floor 3, the Storm Spire, whose guardian (a mini
   boss or the floor boss) fell with the character there. This is the "climber" achievement's progress, which the
   server already keeps per character as the part's step (`QuestTracker.TowerCleared`). Floor 3 is built and playable;
   the line is a constant (`Legendary.TowerFloor = 3`, as a step `Legendary.TowerStep` = 21) so a floor 4 does not
   suddenly lock everyone out. **To do when floor 4 ships: raise it** (it is on the release checklist).
   (Until 2026-11-24 floors were called bands and parts floors, so this read "floor 21 and up": the same part, floor 3,
   part 1. See docs/tower-worlds-proposal.md section 12.);
3. it has **chosen its ending** (one of the three capstones) in that skill's combat tree
   (docs/capstone-paths-design.md);
4. it **does not own that Legendary already** (any stack in its inventory, worn or not).

A character that qualifies for at least one Legendary (owned or not) rolls; a find is one of those it does not own
yet, picked at random.

**Legend's Echo** (approved 2026-11-23): when the character already owns every Legendary it qualifies for, a find
becomes a Legend's Echo instead: **+10 boss kills** of awakening on its least awakened one of those (the first on a
tie), and when all of them are at rank 5, a **Flawless gem** of a random kind. It is quiet: a word over the character
("Legend's Echo"), no chat line; the gem arrives with the boss's rewards. The bad-luck count works as for a find. The
simulator (`legendary`, its "Finds" part) and a unit test: a dedicated player who qualifies for all five finds them on
days 19, 40, 61, 82 and 102 (medians), then gets about 1.4 echoes a month.

## 3. Where they come from

Only these bosses, only when they fall with the character in the fight:

| Source | Base chance a qualifying character |
|---|---|
| a guardian on tower floor 3 or above (a mini boss or the floor boss), on the stream's clear | 4% |
| the boss of a Legendary (heroic) dungeon, tier 4 | 0.25% |
| a raid boss | 0.10% |

Never crafted, never sold, never in a chest: the five lose their chest drop rows at server start. A tower floor clears
once per stream, so the tower is a rare big chance; dungeons and raids are the steady road. The server cannot tell how
hard a raid was (the game scales the boss to the players there), so a raid counts for less than a heroic dungeon.

A Legendary is **soulbound** and **stays with the character that found it**:

- no trading, gifting, marketplace, clan bank or streamer shelves (the soulbound rules, and the gift and marketplace
  checks name Legendaries too);
- **never sold to the vendor** (approved 2026-11-23): the server's vendor refuses one on every path (one item, the bulk
  sells by filter or list, the website), the game's `!vendor` answers "Legendaries can't be sold. The Soulreaver stays
  with you.", and the website shows "Never sold" with no Sell button. Selling destroyed it and its awakening for a coin,
  which was nearly always an accident;
- **not sent to the stash or to another of the owner's characters**: other soulbound items may go there (the stash is
  account bound by design, `UserBankItem.Soulbound`), a Legendary not (one of each per character, its awakening). The
  server refuses both, the bulk "send to stash" skips it, and the website shows both buttons off with "A Legendary
  stays with the character that found it.";
- **disenchanting** made a new stack and dropped the tag (the awakening), the gems, the look and the name of any item;
  it keeps them now. Enchanting keeps the stack. There is no drop, destroy or salvage of items in the game.

The item row still says 1 coin (an older game reads it), and the vendor never restocks soulbound items anyway.

### Bad-luck protection

Every qualifying boss kill raises the character's own chance a little until it finds one, then it starts again:

    chance = base x min(10, 1 + kills / 75)

`kills` is the number of qualifying boss kills since the last find (every source counts one). After 75 kills the
chance is twice the base, after 300 five times, and it stops growing at ten times (675 kills). Nothing is shown in the
game; it is quiet by design.

### What that means (simulated)

`legend_sim.py` style check, now a unit test (`LegendaryTests.DedicatedPlayerFindsOneInWeeks`), 20,000 characters:
a dedicated player streams about 4 hours a day, which is about 8 dungeons (3 of them heroic) and 8 raids. A casual one
plays an hour a day (1 heroic dungeon, 2 raids). Tower bosses are left out (they only help).

| Player | Mean | Median | 1 in 10 is luckier than | 1 in 10 waits longer than | 1 in 100 waits longer than |
|---|---|---|---|---|---|
| dedicated, 4 h a day | 21 days | 19 days | 6 days | 39 days | 58 days |
| casual, 1 h a day | 74 days | 67 days | 18 days | 138 days | 201 days |

About 210 qualifying boss kills on average. Weeks for a dedicated player, never years: even the unluckiest one in a
hundred is under two months. Each next Legendary takes about as long again.

## 4. The best weapons, and they stay the best

No Legendary has hand-set numbers. At server start (and whenever an item is added or changed) the server looks at
every item of the same type (2H swords for the Soulreaver, shields for the Spiked Bulwark, and so on) that is not a
Legendary and takes the best accuracy and the best power (the best armor for the shield) among them, each on its own.
The Legendary is that **plus a margin**:

| Rank | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Margin over the best | 10% | 12.5% | 15% | 17.5% | 20% |

The item row holds rank 1 (that is what an item page and an older game show); the game and the website scale a
higher rank's numbers by (1 + margin) / 1.10. On 2026-11-23 the best 2H sword is the Elder Atlarus 2H Sword (776 /
776, level 999) and the best 2H sword power is the Elder Atlarus Katana's 834, so the Soulreaver is 854 / 918 at rank 1 and
932 / 1,002 at rank 5 (each stat on its own: the best accuracy and the best power need not be one item). When a better 2H sword ever comes,
the Soulreaver follows it at the next start. Code: `Legendary.Derive` (RavenNest.Combat), `GameData.EnsureLegendaries`.

## 5. The powers

Each power is a new gear stat (RavenNest.Combat `GearStat.SoulHarvest`...), worked out by the shared library so the game
and the simulator agree, and carried out where the fight runs (the game's `CombatLegendary`, like `CombatPaths` for the
capstone paths). Its strength grows with the rank, evenly from rank 1 to rank 5:

| Power | What it does | Rank 1 | Rank 5 |
|---|---|---|---|
| **Soul Harvest** (Soulreaver) | Every enemy you struck that falls heals you for a share of your maximum health | 5% | 10% |
| **Dread** (Hellhorn Mace) | A melee hit has a chance to freeze the enemy in fear (a stun): no attacks for a while. Bosses shrug it off | 8%, 2 s | 14%, 3 s |
| **Bat Swarm** (Batwing Staff) | Every few seconds a spell sends a swarm of bats: 3 bites on the enemies nearest the target (within 8 m, the target too) for a share of the spell's damage each | every 10 s, 35% | every 8 s, 55% |
| **Iron Aegis** (Spiked Bulwark) | Thorns answer ranged and magic blows too, not only melee, with more thorns; and every 10 s of fighting you and the allies within 10 m get a shield of a share of your health for 6 s | +5% thorns, 5% shield | +12% thorns, 10% shield |
| **Thunder Shot** (Fortress Hand Cannon) | Every third shot that hits is a blast: more damage, a splash on up to two enemies within 3 m of the target, and the target is knocked back 2.5 m and staggered (bosses only take the damage) | +25%, 10% splash, 0.8 s | +40%, 20% splash, 1.2 s |

Every power shows a word over the fighter (Soul Harvest, Feared, Bat Swarm, Iron Aegis, Thunder Shot, with Effect
words) and a small ring or bolt (with Weapon effects, off in potato mode). Numbers were set with the simulator
(section 8). The Hellhorn Mace's fortress touch went from 35% fire to 15%: fire passes half of armor, and with 35% the
Legendary hit half again as hard as the best axe at rank 1 before its power did anything.

## 6. Awakening

A Legendary grows through 5 ranks from boss kills **with it worn**: every raid, every dungeon (any tier) and every
tower clear whose boss falls with the character there counts one. The server counts them on the item itself (the
inventory item's tag, `legend:<kills>`), so the game gets them with the inventory feed; nothing is decided by the game.

| Rank | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Boss kills worn | found | 30 | 90 | 200 | 400 |
| Stat margin | 10% | 12.5% | 15% | 17.5% | 20% |
| Gem sockets | 3 | 3 | 4 | 4 | 5 |
| Glow | none | none | faint | bright | full, with motes |

A dedicated player (about 16 bosses a day) fully awakens one in about 25 days. Sockets beyond a normal item's three
are a Legendary's alone; a gem in the 4th or 5th socket works like any other. When a Legendary reaches rank 5 the
server notes it for the Hall of Legends.

The glow is a light and a soft halo on the weapon in the Legendary's color (Soulreaver pale blue, Hellhorn ember red,
Batwing violet, Bulwark steel and green, Hand Cannon gold). It has its own setting (Graphics, "Legendary glow", on by
default) and is off in potato mode.

## 7. What the stream sees

- **A find**: one short chat line, rate limited in the game (at most one Legendary line a minute; finds that pile up
  are joined into one line): "zerratar found a Legendary: the Soulreaver!" A full awakening gets a line too, through
  the same limit. Nothing else is said in chat (the bot's ChatPacer handles the rest).
- **Hall of Legends** on the website (`/legends`): for each Legendary its picture, power and who found it first in the
  world, and every character that has fully awakened one, with dates. Nothing is spoiled that the item pages do not
  show already.
- **Hidden titles**: one achievement, "legend", hidden, counting the different Legendaries a character has found:
  tier 1 at one ("Legendbearer"), tier 2 at all five ("Keeper of Legends").
- **Tooltips** in the game and on the website: "Legendary", the rank, the power with its numbers at this rank, and the
  awakening progress ("Awakening: 37 / 90 boss kills to rank 3").

## 8. The simulator

`RavenNest.CombatSimulator legendary [runs]` (`LegendaryScenarios.cs`; `legendary 200 hellhorn-mace=FIRE_DAMAGE:20%`
tries another fortress touch): a level 999 player in the standard kit for the Legendary's style, with the best normal
item of the type, then with the Legendary at rank 1 and rank 5 (worked out from that item as the server does, with its
fortress touch and its power). No skill tree on either side, so only the weapon differs. 300 s a fight, 200 runs.

- **one**: enemies of the player's level one after another, the player held at 90% health; **pack**: four at once;
  **boss**: one boss whose pool lasts about 90 s for the normal item. Damage dealt, as a multiple of the normal item.
- **room**: sixteen to clear, four at a time, hitting at 30% of the kit's power, health not held (a fall costs 10 s):
  time to clear and damage taken (after shields) as multiples, and falls (the normal item's in brackets).

| Legendary | Fight | Rank 1 | Rank 5 |
|---|---|---|---|
| Soulreaver | one / pack / boss | 1.10 / 1.09 / 1.10 | 1.20 / 1.21 / 1.21 |
| | room | time 0.90, taken 0.99, falls 7.1 (8.3) | time 0.83, taken 0.99, falls 6.4 (8.3) |
| Hellhorn Mace | one / pack / boss | 1.28 / 1.30 / 1.35 | 1.39 / 1.42 / 1.47 |
| | room | time 0.80, taken 0.83, falls 7.3 (8.9) | time 0.76, taken 0.81, falls 7.1 (8.9) |
| Batwing Staff | one / pack / boss | 1.15 / 1.37 / 1.19 | 1.31 / 1.71 / 1.37 |
| | room | time 0.71, taken 0.77, falls 6.5 (9.0) | time 0.57, taken 0.65, falls 5.4 (9.0) |
| Spiked Bulwark | one / pack / boss | 1.00 / 1.04 / 1.06 | 1.00 / 1.06 / 1.09 |
| | room | time 0.93, taken 0.88, falls 7.4 (8.4) | time 0.86, taken 0.77, falls 6.3 (8.4) |
| Fortress Hand Cannon | one / pack / boss | 1.21 / 1.34 / 1.23 | 1.38 / 1.67 / 1.40 |
| | room | time 0.80, taken 0.85, falls 7.5 (9.0) | time 0.67, taken 0.73, falls 6.4 (9.0) |

The Spiked Bulwark's shields also go to every ally within 10 m: about 3,000 health a minute per ally at rank 1 and
6,000 at rank 5 (a level 999 player has about 10,000).

What was tuned: the first draft's Bat Swarm (every 8 s, 5 bites at 70% at rank 5) was 2.3 times the best staff in a
pack, now 1.7; Thunder Shot (+90% and a 60% splash on everyone) was 3.0 times in a pack, now 1.7, its splash on two
enemies at most; Iron Aegis's shield went from 3% to 5% (6% to 10%), it took only 2% off; the Hellhorn Mace's fire went
from 35% to 15% (section 5). Soul Harvest and Dread add no damage; they show in the room (fewer falls, less taken).

Not modelled: positions (every enemy is in reach of the swarm, the blast and the aegis, so the area powers are at
their best here), skill trees and skills, more than one ally, the tower's level sync (gear counts only up to a floor's
level there, the powers in full).

## 9. How it is built

Server (RavenNest):
- `RavenNest.Combat/Legendary.cs`: the five (keys, fixed ids, skill, tree, colors), the chances and the bad-luck
  count, ranks, margins, sockets, the powers' numbers and their text; `GearStat` values for the powers; Thunder Shot,
  Bat Swarm and Iron Aegis rules in `LegendaryRules` with a per-fighter state.
- `GameData.Legendary.cs`: the stats derived from the catalog at start and when items change, soulbound, sell price,
  the requirement at 950, the chest drop rows removed.
- `LegendaryManager`: eligibility, the roll, the bad-luck count (`CharacterLegendary`), finds (`LegendaryFind`),
  awakening (the tag), the titles, the Hall of Legends; called from raid, dungeon and tower rewards.
- Sockets: `GemManager` asks `Legendary.Sockets(rank)` for a Legendary.

Game: `CombatLegendary` (the powers in the fights), `LegendaryGlow` (the glow), the rank scaling in
`PlayerController.UpdateEquipmentEffect`, tooltips, the chat line, the setting.

Website: `/legends`, item tooltips.

## 10. Built, tested, calls

**Built 2026-11-23** (game and RavenNest on master, the local server only; nothing deployed):

- RavenNest: `RavenNest.Combat/Legendary.cs` (rules, numbers, powers, `LegendaryRules`), `GearStat.ThornsAllStyles`,
  `GameData.Legendary.cs`, `LegendaryManager` (the roll, finds, awakening, the Hall), the `CharacterLegendary` and
  `LegendaryFind` tables (made at start), raids, dungeons and tower clears calling it, `GemManager` sockets by rank,
  the hidden `legend` achievement, the simulator's `legendary` command; tests `LegendaryTests` (Combat) and
  `LegendaryServerTests`.
- Game: `CombatLegendary` (the five powers, hooked into `CombatRules` and `PlayerCombat.Defeated`), `LegendaryGlow`
  (and the Legendary glow setting), `LegendaryNews` (the chat line, the rank words, the wearer's numbers following the
  feed's tag), rank scaling in `ItemExtension.GetItemStats`, sockets and tooltip lines (`GetLegendaryLines`) in the
  details window and the inventory, the titles' words.
- Website: `/legends` (Hall of Legends, in the Game menu), the Equipment tab's tip card and item panel (rank, power,
  awakening bar, the numbers at the rank), the item list's line, "Legendary" as the rarity name.
- Test hooks, development only: `generated-data/legendary-force.txt` (character ids: the next qualifying boss kill
  finds one) and `generated-data/legendary-awaken.txt` ("id kills": the worn Legendaries' count at the next boss kill).

**Tested in play** on the dev character (Attack 999; for the test its climber floor was set to 21 and Perfect Form
learned, then put back): a raid reward with the force file found the Soulreaver (soulbound, tag `legend:0`, put on
by itself as the better weapon), the chat line was made ("zerratar found a Legendary, the Soulreaver"; chat was muted
and the bot stopped, so it went nowhere); fighting dragons, Soul Harvest healed 43 times with its word over the
character; a raid with the awaken file took it to rank 3 (4 sockets, power 918 to 960, the faint glow) and a dungeon to
rank 5 (5 sockets, 1,001 power, the full glow, "zerratar's Soulreaver is fully awakened" a few minutes after the find
line); the details window's tooltip; the Hall of Legends page with the find and the awakening; the Equipment tab's
tip card in the gear preview. Everything given was taken back afterwards.

**Follow-ups tested (2026-11-23, second round)**: with Strength 999 and the Might ending (Colossus) the force file
found the Hellhorn Mace; the server's vendor sold 0 of it, and `!vendor hellhorn mace` answered "Legendaries can't be
sold. The Hellhorn Mace stays with you." (captured in a client that never sends; chat was muted); the Equipment tab
(gear preview) shows Send to stash and Send to another character off on a Legendary. The dev character was put back
exactly (a snapshot diff, the village's coins). Legend's Echo is unit tested and simulated, not played.

**Not tested in play**: a real find (only the forced one; the odds are the unit test's), a tower clear of floor 21+,
Dread, Bat Swarm, Iron Aegis and Thunder Shot in a fight (the rules are unit tested and simulated, the game code
compiles and follows Soul Harvest's path), the knockback on the navmesh, the shield on allies, a gem in a 4th or 5th
socket through `!socket`, the website's socket picker with five sockets, an older 0.9.6 game seeing a Legendary.

Pictures: docs/mockups/legendary-fight.png (the Soulreaver in a fight), -rank3.png and -rank5.png (the glow),
-tooltip.png (the details window, with Soul Harvest's word in the world), -hall.png and -hall-phone.png (the Hall of
Legends), -web-tooltip.png (the Equipment tab).

**Calls for Zerratar**

- Odds: 4% / 0.25% / 0.1% with bad luck up to ten times; a dedicated player waits about three weeks, a casual one
  two to three months. Raids count less because the server cannot tell a hard raid from an easy one.
- The tower line is floor 3 as a constant, not "the top floor": raise it when floor 4 is out, if wanted.
- Ranks need 30 / 90 / 200 / 400 boss kills worn (about 25 days to rank 5 for a dedicated player).

Decided 2026-11-23 (the owner approved all calls): the Hellhorn Mace is found by Strength and Might; owning every one
gives a Legend's Echo; the tower line stays at floor 3 (then called floor 21) until floor 4; raids keep their lower chance; Legendaries are
never sold and never leave their finder (section 3).
- Titles: Legendbearer (one found) and Keeper of Legends (all five), hidden.
