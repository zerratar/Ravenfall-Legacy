# Substitution: escaping a boss's area attack

The owner's idea of 2026-11-27, built the same day for v0.9.7 as a rare find (sections 1 to 3 are the idea as it was
written down; section 4 onward is what was built). The rules and numbers live in the shared combat library,
`RavenNest.Combat.Substitution` (RavenNest repo), so the server, the game and the simulator agree.

## 1. Where it came from

Tower bosses telegraph their area attacks: a slam fills a red ring, a spit a ring in its element's color, and
everyone inside is hit when it lands. Players do not step out of the rings (the owner, 2026-11-27: "no, at least
for now"), so dodging is not something every player does. Instead, escaping a ring becomes a **rare power** a
player can have, like a substitution jutsu (kawarimi): the player is somewhere else when the blow lands.

## 2. The effect

When a player stands inside a boss's area attack as it lands, the power may trigger: the player escapes to just
outside the ring and takes no damage from that blow.

Looks, one per source (so the look tells you what you found):

| Look | What happens |
|---|---|
| **Smoke step** | The player bursts into a puff of smoke (a log or a training dummy could thud down where they stood, as a joke) and reappears in a second puff just outside the ring. |
| **Shadow phase** | The player turns into a dark shadow, slides out of the ring along the ground and takes shape again at its edge. |

More looks can come later per theme (a flurry of leaves, frost mist, a crackle of lightning, a swarm of bats for
the Batwing Staff).

## 3. The idea's first guesses

- Only against telegraphed area attacks, never a plain swing; a chance and a cooldown, both; a walkable spot just
  outside the ring on the group's side; the player keeps their target and walks back in; "Substitution!" as an effect
  word and a count on the meters' Damage Taken tab; the looks follow Boss attack effects, potato mode a short flash.
- Three possible sources: a special gem, a magical attribute, a skill node. With more than one, the best chance and
  the shortest cooldown count, not both stacked.

Decided with the owner (2026-11-27): the attribute and the gem now, the skill node later; any gear and any role.

## 4. Built (2026-11-27)

### The power

- **When:** a tower guardian's slam or spit lands with the player inside its ring (`TowerManager`), and hero mode's
  slam on the hero (`WaveRun.Moves`). Nothing else calls it: a plain swing is never escaped, so a tank holding the boss
  is not made immune (checked: a tank with a Perfect Shadowstone and a sure roll took every swing and escaped the slams).
- **The roll:** `Substitution.Escapes(power, lastEscape, now, roll)`: ready (its cooldown since the last escape is over)
  and the roll under the chance. A failed roll costs nothing; the cooldown starts only on an escape.
- **Where to:** `Substitutions.FindSpot` in the game: halfway between the group's side (the others standing outside
  the ring) and straight out from where the player stands, turned up to 165 degrees either way and 1.3, 2.1 or 3 m
  past the ring's edge, until a spot is on the navmesh, at about the player's height (not a ledge or a roof), outside
  any other ring on the ground, and reachable on foot within a short path (never through a wall into another room).
  No such spot: no escape, and the cooldown is not spent. Escapes in play landed 3.9 to 6.7 m from where the player stood.
- **After:** the player keeps their target, turns to the fight and walks back in. No damage and no knockback from
  that blow.
- **Several sources never stack:** `Substitution.Best` takes the best chance and the shortest cooldown of the worn
  attribute and Shadowstones (60% and 18 s at most); the look is the one of the source with the better chance (on a
  tie the shorter cooldown's, then the shadow phase). The game keeps it on `EquipmentStats.Substitution`.

### The two sources

| Source | Look | Chance | Cooldown | How it is found |
|---|---|---|---|---|
| Magical attribute `SUBSTITUTION` on any piece of gear | Smoke step | 35% on an item of level 100 or less, rising to 50% at level 600 (whole percents) | 30 s | Pre-rolled on rare drops only; enchanting never rolls it |
| **Shadowstone**, a gem in armor | Shadow phase | Chipped 40%, Flawed 45%, Polished 50%, Flawless 55%, Perfect 60% | 30, 27, 24, 21, 18 s | Rare drops only, socket-ready; never enchanted from a raw gem or combined |

- **The attribute** is stored in the item's enchantment text (`SUBSTITUTION:42%`) and named like a gear stat
  (`GearStat.Substitution`, for labels and parsing; a combat sheet ignores it, since it never adds up). It is not an
  `ItemAttribute` row, so enchanting cannot roll it. **Enchanting keeps it** beside the new rolls, and **disenchanting
  leaves it** (an item with nothing but it has nothing to disenchant). The found piece is named "... of Substitution"
  and is not soulbound (a rare drop like any other, until it is enchanted).
- **The Shadowstone** is `GemKind.Shadowstone`, the eighth gem kind, with five tier items whose ids come from its name
  like every gem's (`Gems.ItemId`). `Gems.KindCount` stays the seven made gems (a random gem, Legend's Echo's gem, the
  bottles' gems are never a Shadowstone); `Gems.AllKindCount` counts it. It works in **armor only** (the server
  answers NotSocketable for a weapon, ring or amulet; the game and the website say so first) and gives no gear stat.
  **It never crumbles:** taking it out or socketing over it always gives it back, with nothing to confirm. Why: it is a
  found-only chase item that cannot be made again, and a levelling player must be able to move it into their next
  armor; a keep roll would leave it stuck in old gear or make players never touch it. Vendor price 20,000 coins for
  Chipped, doubling each tier; tradeable like other gems.

### Finding one (the server's `SubstitutionManager`)

Everyone who fought rolls when a source's boss falls; the gem about a third as often as the attribute.

| Source | Attribute | Shadowstone |
|---|---|---|
| A tower part's chest from floor 2 up (its guardian on the stream's clear) | 0.3% | 0.1% |
| A tower floor boss (floor 1's too) | 2% | 0.75% |
| A Legendary dungeon's boss (tier 4) | 0.3% | 0.1% |
| A raid boss | 0.04% | 0.015% |

- A regular stream (four raids of fifteen a night, a Legendary dungeon every other night with ten, a part and a half of
  the climb with six players and a floor boss every ten parts) finds the attribute about every **12 nights** and a
  Shadowstone about every **35** (`Substitution.NightFinds`, tested). A big stream (raids of forty, three parts with
  fifteen) sees one about every other night.
- **The attribute's piece:** gear the character can wear (weapons, armor that takes gems, rings and amulets with a
  combat stat; no Legendary, nothing priced 0 or 1), a random kind, one of the five best of that kind at or below the
  part's synced level in the tower (no cap in raids and dungeons). The first version took it from the chest's drop
  table, which on floor 2 is mostly level 20 pieces; that gave a Black Helmet to a high level character in play.
- **The gem's tier:** Chipped 40%, Flawed 30%, Polished 18%, Flawless 9%, Perfect 3%; a floor boss's is the better of
  two rolls.
- The reward carries the attribute (`EventItemReward.Enchantment`, RavenNest.Models) so the game can say it.
- **Development hook:** lines `<character id> attribute` and `<character id> gem` in
  `src/RavenNest.Blazor/generated-data/substitution-force.txt` (Development only) give those finds at that character's
  next source boss.

### Shown everywhere

- **The fight:** "Substitution!" over the player (Effect words), the look, and the meters' Damage Taken tab counts each
  player's escapes (a wind and the number by the name; the column reads "PLAYER (ESCAPED)").
- **The looks** (`SubstitutionFx`): smoke step: a thick grey puff where they stood with low smoke rolling out, a stubby
  log (the Synty log prop, `World/Tower/Resources/SubstitutionLog`) thuds down there, rocks and sinks away, and a
  second puff where they appear. Shadow phase: the body vanishes, dark smoke is drawn into the floor, a dark streak
  with violet wisps slides out along the ground and a column of dark wisps rises where they take shape. Both follow the
  settings' "Boss attack effects"; with it off or in potato mode one short flash at each end (white for smoke, violet
  for shadow). Mid grey smoke, since a pale one vanished on the snow at a stream camera's distance.
- **Items:** the game's tooltips (a "Substitution 45%, every 27 s" row, the sentence as the details window's note),
  `!stats` of an item, the socket lines ("Flawed Shadowstone: Substitution"); the SUBSTITUTION enchantment is not
  listed among the enchantments. `!gems shadowstone` explains it; `!gems enchant` and `!gems combine` refuse it;
  `!socket` refuses it outside armor.
- **The website:** the Equipment and Inventory tabs' tooltip card and item panel (a violet Substitution block, a badge,
  "Enchanted" only for rolled enchantments), the gem picker (a Shadowstone's line in armor, "only works in armor"
  elsewhere with the button off, a note that it is found only and always comes back), the Shadowstone's item pages
  (its description). `/dev/gear-preview` wears Rune Gloves with Substitution 44% and has a Shadowstone in a Dragon Chest.
- **Chat:** "Rare find! zerratar found a Mithril Sword with Substitution 35%: a chance to escape a boss's slam or spit in
  a puff of smoke", or "... found a Polished Shadowstone, a gem that can't be made: in armor it slips its wearer out of
  a boss's slam or spit as a shadow" (one line at most a minute, finds of the same minute joined), plus the normal
  drop line and "Rare find!" over the player.
- **Icons:** one enchanted Shadowstone from the Image Generator (smoky black crystal with a violet glow and a wisp of
  smoke), the five tiers made by `tools/items/gem-tiers.py`.

### Checked (2026-11-27)

- Unit tests (`SubstitutionTests`): chances, cooldowns, no stacking, the attribute through enchanting, sockets in armor
  only, tower sources, drop odds, gem tiers.
- Simulator `RavenNest.CombatSimulator substitution 300`: five fresh players against floors 1 to 3's guardians, the
  second player takes 22 to 29% less from slams and spits with the attribute (43%) or a Chipped Shadowstone, 32 to 40%
  with a Perfect one, 1 to 14% less in all; the group's wins barely move.
- Play mode with bots on the local server, chat cut off: two tower fights (floor 2 parts 2 and 3) with bots wearing
  the attribute and a Flawless Shadowstone: 19 natural escapes from slams and spits, the cooldown held (30 s and 21 s),
  a bot outside the ring untouched, failed rolls taking the full blow, a tank never escaping swings. Both looks,
  potato mode's flash and the meters captured in docs/mockups/substitution-2026-11-27/. The forced finds through two
  real clears (a Polished Shadowstone, a Black Helmet and then a Mithril Sword with Substitution 35%) with the chat line;
  the Shadowstone through the server's gem calls (a weapon refused it, boots took it and the power showed through the
  inventory feed, it came out kept without a confirm, enchant and combine refused it).

### Not checked yet

- Hero mode's slam with a hero who has it (wired in, not played).
- A real (unforced) find in a raid or a Legendary dungeon; the website pages signed in with a real found item.
- Two players escaping the same blow side by side: their "Substitution!" words overlap a little.

## 5. Open questions for the owner

- The numbers are first guesses: chances, cooldowns, drop odds (about one attribute every 12 nights, a Shadowstone
  every 35 for a regular stream).
- Should found pieces be soulbound like Legendaries, or stay tradeable (now: tradeable until enchanted)?
- More looks later per theme, and the skill node (the Precision tree's rogue side or a capstone path).
