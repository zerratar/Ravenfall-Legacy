# Gems and sockets

Part of [combat-v2-design.md](combat-v2-design.md). Gear gets **sockets**, and players put **enchanted gems**
in them to choose the bonus they want: fire on a sword, ice resistance on a chest, crit on a ring. Gems are
items, **tradeable**, and the main way elements and resistances get onto gear.

Decided with Zerratar (2026-11-06):
- **Removing a gem destroys it,** to begin with.
- A **chance to keep it** comes from the clan's Enchanting level, and from a node in the player's skill tree
  for those who spend points on it.

---

## 1. The gems

Raw **Ruby, Sapphire, Emerald and Topaz** already exist as items. Enchanting a raw gem turns it into an
enchanted gem that fits in a socket. A gem does something different depending on where it goes:

| Gem | Element / stat | In a weapon | In armour | In a ring or amulet |
|---|---|---|---|---|
| **Ruby** | Fire | adds fire damage | fire resistance | longer burns |
| **Sapphire** | Ice | adds ice damage | ice resistance | more chill stacks |
| **Topaz** | Lightning | adds lightning damage | lightning resistance | more shock jumps |
| **Emerald** | Poison | adds poison damage | poison resistance | more poison stacks |
| **Diamond** (new) | Precision | crit chance | crit resistance (fewer crits taken) | crit damage |
| **Onyx** (new) | Vitality | life steal | max HP | healing received |
| **Amethyst** (new) | Arcane | healing power | all resistances (small) | skill cooldowns |

**Tiers:** Chipped, Flawed, Polished, Flawless, Perfect. Each tier roughly doubles the bonus of the one before.
A gem's bonus depends only on its kind and tier, so gems of the same kind and tier are the same: easy to
price, easy to trade.

## 2. Making and upgrading gems

- **Enchanting a gem** uses the clan's Enchanting skill, as enchanting gear does: you need to be in a clan with
  Enchanting, and the clan's level decides the odds of a better tier (a low level clan makes Chipped gems,
  a high level clan sometimes makes Flawless ones). This gives clan Enchanting a reason to matter to every
  member, and makes gem making something clans are known for.
- **Combining:** three gems of the same kind and tier become one of the next tier, for a coin fee. It is the
  main way to reach Perfect, and a sink for gems and coins.
- **Sources of raw gems:** mining and gathering (as today), more of them from tower floors, raid and dungeon
  chests. Tower floor chests can also give enchanted gems directly.

## 3. Sockets

- How many sockets a piece of gear has depends on its level: none below level 25, 1 from 25, 2 from 150,
  3 from 400. Rings and amulets have at most 1. Sockets are there from the start, empty.
- A socketed item becomes unique, the way an enchanted item is unique today (it no longer stacks).
- Gems stay in the item when it is traded, sold on the marketplace or put in a bank; the listing shows them.
- Enchantments and gems sit side by side: **enchantments give base stats and skill bonuses, gems give elements,
  resistances, crit and vitality,** so the two do not overlap.

## 4. Removing a gem

- Taking a gem out (or putting a new one over it) **destroys the old gem**, unless the keep roll succeeds.
- **The chance to keep it:**
  - from the clan's Enchanting level: nothing below level 100, then rising to 40% at level 400;
  - plus up to 15% from the *Gem Salvager* node (three ranks) in the Artisan tree (see
    [combat-skills-design.md](combat-skills-design.md));
  - at most 55% together, so removing is always a decision.
- The game and the website say the chance before you confirm.

## 5. Commands and pages

- `!gems` lists your gems; `!gem combine ruby flawed` combines three into the next tier.
- `!socket <item> <gem>` puts a gem in the item's first free socket (or the slot you name).
- `!unsocket <item> <slot>` removes one, after saying the chance to keep it.
- The game's item tooltips and the website's inventory show sockets as empty rings or gem icons, with the
  bonus each gives in that item.
- The marketplace lists gems as normal items, and socketed gear with its gems.

## 6. Technical notes

- New item kinds: enchanted gems (kind and tier). Raw gems stay as they are.
- The inventory item stores its gems (one gem item per socket), like it stores its enchantment and tag today,
  and the stack key includes them. (`InventoryItem.Tag` is `nvarchar(max)` since 2026-11-05; gems get their
  own field rather than sharing the tag.)
- Gem bonuses go into the character sheet like other stats (see the overview, section 1), through the shared
  combat library.

## 7. Built (2026-11-08)

The first version, with the defaults below for the open questions (easy to change in `RavenNest.Combat.Gems`):

- **The rules** live in the shared library (`RavenNest.Combat/Gems.cs`, tests in `GemsTests.cs`), so the server, the game
  and the website agree. An enchanted gem is an item with a **fixed id** made from its name (`Gems.ItemId`), so any reader
  knows a gem from its id alone. An item keeps its gems in `InventoryItem.Sockets`: one gem item id per socket, comma
  separated, empty for an empty socket, null without gems. Socketed gear is one of a kind (part of the stack key), keeps its
  gems in the bank, the clan bank and on the marketplace, and cannot go on a streamer's shelf yet.
- **Bonuses** (Chipped; times 2, 3.5, 6 and 10 for the tiers above):

  | Gem | Weapon | Armour | Ring or amulet |
  |---|---|---|---|
  | Ruby | 3% fire damage | 1.5% fire resistance | 4% stronger burns |
  | Sapphire | 3% ice damage | 1.5% ice resistance | 4% longer freezes and stuns |
  | Topaz | 3% lightning damage | 1.5% lightning resistance | 4% harder lightning jumps |
  | Emerald | 3% poison damage | 1.5% poison resistance | 4% stronger poison |
  | Diamond | 0.5% crit chance | 1% crit resistance | 2% crit damage |
  | Onyx | 0.4% life steal | 0.5% more health | 2% more healing received |
  | Amethyst | 2% healing power | 0.5% every resistance | 1.5% shorter cooldowns (30% at most) |

  Four new gear stats came with them: crit resistance, poison bonus, lightning jump bonus and cooldown reduction. A
  weapon's gems go with its style, like its own element. They count in full in the tower (level sync caps only levels and
  classic gear numbers).
- **Raw gems:** Topaz now drops while mining from level 40, and the new Amethyst (60), Onyx (80) and Diamond (100) too.
- **Enchanting a gem** takes no cooldown and gives no clan experience; the tier odds come from the clan's Enchanting level
  (Flawed from 30, Polished from 100, Flawless from 250, at most 50%, 30% and 10%). Perfect only by combining.
- **Combining** costs 1,000, 5,000, 25,000 and 125,000 coins for the four steps. Gems sell to the vendor for the raw gem's
  price times three per tier, so combining never pays at the vendor.
- **Keeping a gem** taken out: from clan Enchanting 100, up to 40% at 400. The *Gem Salvager* node is not in the skill
  trees yet (the library takes its ranks when it is).
- **Commands:** `!gems` (what you own), `!gems ruby` (what a ruby does where), `!gems enchant ruby 5`,
  `!gems combine ruby flawed 2`, `!socket rune sword ruby` (the best ruby you own into the first free socket; a tier
  picks the gem, a number at the end picks the socket and replaces the gem in it) and `!unsocket rune sword 2`. Sent by the
  bot as `gems_command`; the game calls the server's `gems.enchant`, `gems.combine`, `gems.socket` and `gems.unsocket`.
- **Shown** in the game's item tooltips (both), `!stats` of an item, and the website's item details on the character page.
- **Icons:** the three new raw gems and one enchanted look per kind from the Image Generator; the five tiers are made from
  it by `tools/items/gem-tiers.py` (gold pips, one to five; Chipped duller, Perfect glowing).

Checked on the local server with the streamer's own character: ten raw rubies enchanted (7 Chipped, 3 Flawed at the
clan's level), six Chipped combined into two Flawed for 2,000 coins, two socketed into the worn Elder Atlarus 2H Sword
(fire damage 5% to 17% in the game at once, through the inventory feed), one replaced and one taken out (a 3.9% keep
chance, lost both times). Bots: two Perfect Rubies in a staff give its magic hits 60% fire on the combat sheet, a Perfect
Onyx 5% more health. Not seen yet: the tooltips on screen, the website dialog, the commands through a real chat.

## Open questions

- Should socket count also vary by luck (an item drops with 0 to its maximum), with Crafting able to add one?
- Should the coin fee for combining scale with the tier?
- Should Perfect gems get a small extra effect each (a fire gem that sometimes explodes, say), or stay plain?
