# Taming gear: the Beastmaster line

**Status: approved 2026-11-22, not built yet** (it starts when the outfits work lands, since both change the
equipment code). The owner: "do you think we should add in Taming gear too? that helps out with taming/boosts
your pet?", then "do it. also general improvement of pets/boosts to the pets and stuff. but should still be combat
related for both somewhat, because player would be using the gear and be in combat at the same time. pet isnt the
only thing in combat."

The other twelve lines are in `class-gear-design.md`; this is the thirteenth and follows their rules unless it says
otherwise here.

## The idea

Gear for the player who fights with a pet. It makes the **pet** better at whatever it does and keeps the
**wearer** a fighter: the set has combat numbers of its own that work with any weapon, since a tamer may swing a
sword, shoot or cast. What it gives up is the big power bonus of a damage line. So with a pet out it is a real
choice beside a damage set, and without a pet it is plainly the weaker one.

## The line

- **Line:** Beastmaster, eight tiers at levels 1, 30, 70, 120, 200, 400, 650, 900, **of Taming** (the first gear
  that Taming levels open). Six pieces a tier, two head pieces with the same stats, as every line.
- **Armor:** leather weight (the Ranger's factor, 0.6).
- **Look:** furs, hides, feathers, bone and antler from the unused Synty parts; earthy browns and greens with the
  teal of the Beastmaster tree. Higher tiers wear more (pelts on the shoulders, a feathered or antlered hood), as
  the other lines grow. Eight materials.
- **Tier names** (the first word of the items; to be changed where one clashes with an existing material):
  Fledgling, Tracker, Houndmaster, Falconer, Wildheart, Packlord, Primal, Wyrmkin.
- **Made from** Leather and Hide with the tier's bar, as the leather lines are. Tower chests: a fighter with a pet
  out can get a Beastmaster piece in place of their skill's line now and then.

## Stats

A full set, tier 1 to tier 8, in the measure the other lines use (percent):

| For | Stat | Tier 1 | Tier 8 | Note |
|---|---|---|---|---|
| You | Max health | 1 | 8 | works with every weapon |
| You | Crit chance | 0.5 | 3 | works with every weapon |
| Pet | Pet power (blows and heals) | 3 | 20 | every pet role |
| Pet | Pet health | 3 | 20 | |
| Pet | Pet recovery (up sooner after a knockout) | 2 | 15 | |
| Pet | Gatherer pet's skills | 1 | 10 | so a gatherer's outfit can be this set too |
| Taming | Tame chance | 2 | 15 | new stat; a share more of the chance, not points added |

No melee, ranged or magic power bonus: a damage line gives 12 to 16% of that at tier 8, and that is the price.

**To tune in the simulator before it is final:** with a damage pet out, the wearer and pet together should land
within about five percent of the same character in their damage line; with no pet out, clearly behind. If a pet's
share of a fight is too small for that, raise pet power before giving the wearer a power bonus.

## How it fits the rest

- **Auto-equip:** the line fits Taming only (while taming), so "equip best" for Attack or Ranged never picks it
  by itself. A player chooses it: `!equip beastmaster` (also `!equip tamer`, `!equip taming`), or as the pinned
  outfit of a skill once outfits are in.
- **Trees and paths:** it stacks with the Beastmaster tree (Fang and Claw, Thick Hide, Back on Its Feet, Keen
  Senses use the same stats) and suits each of its three endings.
- **Supply:** the gatherer stat is up to 10% of Forage's 5%, on top of the Gathering tree; small, but it belongs
  on the list in `trade-skills-design.md`.
- **Website:** icons for the 48 pieces, the line on the item pages, and the parts in the 3D figure's export (the
  export is built from the item list, so a re-run picks the line up).
- **Older games (0.9.6)** show the pieces in the default look, as with the other class gear.
