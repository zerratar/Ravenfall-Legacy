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

**Tiers:** Chipped, Flawed, Normal, Flawless, Perfect. Each tier roughly doubles the bonus of the one before.
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

## Open questions

- Should socket count also vary by luck (an item drops with 0 to its maximum), with Crafting able to add one?
- Should the coin fee for combining scale with the tier?
- Should Perfect gems get a small extra effect each (a fire gem that sometimes explodes, say), or stay plain?
