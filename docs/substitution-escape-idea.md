# Substitution: escaping a boss's area attack (idea)

The owner's idea of 2026-11-27, written down so it is not forgotten. Not built; not planned for a release yet.

## Where it came from

Tower bosses telegraph their area attacks: a slam fills a red ring, a spit a ring in its element's color, and
everyone inside is hit when it lands. Players do not step out of the rings (the owner, 2026-11-27: "no, at least
for now"), so dodging is not something every player does. Instead, escaping a ring becomes a **rare power** a
player can have, like a substitution jutsu (kawarimi): the player is somewhere else when the blow lands.

## The effect

When a player stands inside a boss's area attack as it lands, the power may trigger: the player escapes to just
outside the ring and takes no damage from that blow.

Looks, one per source (so the look tells you what you found):

| Look | What happens |
|---|---|
| **Smoke step** | The player bursts into a puff of smoke (a log or a training dummy could thud down where they stood, as a joke) and reappears in a second puff just outside the ring. |
| **Shadow phase** | The player turns into a dark shadow, slides out of the ring along the ground and takes shape again at its edge. |

More looks can come later per theme (a flurry of leaves, frost mist, a crackle of lightning, a swarm of bats for
the Batwing Staff).

## Rules (first guesses, to tune)

- **When:** only against telegraphed area attacks (slams, spits and later ones like them), never against a plain
  swing, so a tank holding the boss is not made immune.
- **How often:** a chance and a cooldown, both: for example a 35 to 60% chance when inside a ring, then 20 to 40
  seconds before it can trigger again. The source decides the numbers.
- **Where it lands:** a walkable spot just outside the ring, on the side facing the player's group, never into
  another ring or a wall (sample the navmesh).
- **Fights on:** the player keeps their target and walks back in, so damage dealers lose a few seconds of
  fighting for the dodge.
- **Chat and meters:** "Substitution!" as an effect word over the player (Effect words setting), and the meters'
  Damage Taken tab can count blows escaped.
- **Settings:** the looks follow Boss attack effects; potato mode shows a short flash instead of particles.

## Where players get it (rare finds)

All three can exist, each with its own look:

1. **A special gem**: a new gem kind that cannot be made, combined or enchanted from raw gems (for example a
   "Shadowstone" or "Smoke Quartz"), found only on rare drops: high tower floors' chests, floor bosses, Legendary
   dungeons. Socketed in armor it gives the shadow phase; its tier sets chance and cooldown. Takes a socket, so it
   costs a stat gem.
2. **A magical attribute on an item**: a rare roll on gear from the same sources, "Substitution 40%", giving the
   smoke step. Fits the existing magical attributes (thorns, life steal and the rest) and their caps.
3. **A skill**: a node in a tree (the Precision tree's rogue side, or a capstone path), learned with points,
   with a fixed chance and cooldown and the look of the player's choice.

If a player has more than one source, the best chance and the shortest cooldown count, not both stacked.

## Open questions for the owner

- Which of the three sources first (my suggestion: the magical attribute, it fits what exists; the gem next, as the
  chase item).
- Which release: it fits best with the gates and breaks of v0.9.8 (more bosses with area attacks).
- Whether healers' and tanks' gear may roll it, or only damage dealers'.
