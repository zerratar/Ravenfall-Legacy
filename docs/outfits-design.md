# Outfits: gear that follows what you train, and the website's gear pages

**Status: proposal with mockups, nothing built** (2026-11-22). From the owner: "will player auto equip gear
loadouts when they switch training? for instance when joining raid/dungeon/towers, so they dont have to equip
everytime. It can remember what you wore when you trained something and fill in missing gear ... maybe let them
choose what gear they want for each skill and fall back to equip best ... may need to be an option", and "ive
wanted for a long time a rework on both inventory tab and equipment tab because they feel very outdated".

## Today

Nothing changes when a player switches what they train. Gear only changes when:
- a new item arrives and beats what is worn for the trained skill (it is put on by itself),
- the player types `!equip <item>` or `!equip all` (the best for the trained skill, one class gear line),
- `!equip <role>` (being built 2026-11-22): the best for a named skill or gear line.

So a ranger who joins a raid as a healer, or a miner who goes fighting, re-equips by hand every time.

## The proposal

**An outfit per skill.** Each skill a character trains has an outfit: what goes in every slot (head, chest,
gloves, leggings, boots, weapon, shield, amulet, ring) and which **pet** is out. A tank pet for the Defense
outfit, a gatherer pet for Mining.

**Every slot is either "best" or pinned.**
- *Best* (the default): the game keeps the best piece for that skill there, by the rule `!equip all` uses.
- *Pinned*: the player's own pick, which nothing replaces. A pin is set on the website (or `!outfit pin`).

**It remembers by itself.** With no outfit set for a skill, the first time you leave that skill what you are
wearing is kept as its outfit. So a player who never opens the website still gets "what I wore last time I
trained this" and never something they did not choose.

**When training changes, the outfit changes.** On `!train`, on joining a raid, dungeon or tower floor, and back
again afterwards:
1. the new skill's outfit is put on;
2. a slot whose item is gone (sold, traded, stashed) is filled with the best piece owned, and the player is told
   once in chat;
3. a skill that has no outfit yet keeps what is worn (and that becomes its outfit).

The change happens when the activity starts, never in the middle of a fight.

**Raids, dungeons and the tower.** A character fights there with the combat skill it trains. One that is
gathering fights with the combat skill it trained last, and that skill's outfit is what goes on; when the fight
is over the gathering outfit comes back.

**Better gear found later.** A new piece that beats a *best* slot of any outfit goes into that outfit, also
while another outfit is worn (today it is only compared with what is on the body, for the skill trained right
now). Pinned slots are left alone.

**Two switches, per character** (website, and `!outfit auto on|off`):
- "Change outfit when I switch training": on by default. Because of rule 3 it does nothing to a player until
  they have worn different gear for different skills.
- "Upgrade to better gear automatically": on by default, which is how the game behaves today.

**Chat, kept small:** `!outfit` (what the outfit for the trained skill is), `!outfit save` (keep what I wear
now for this skill), `!outfit <skill>` (wear that skill's outfit now), `!outfit auto on|off`.

**Where it lives.** On the server, per character: skill, slot, the item, pinned or not, and the pet. The server
changes the equipment when the task changes and the game shows it, as with every equipment change. Transmog
(looks) is separate and unaffected.

**Open questions**
- Should the pet be part of the outfit (proposed), or stay as it is?
- One outfit for all gathering and making skills together, or one each (tools differ)?
- A separate "raid outfit" a player can pick, or simply the combat skill's own (proposed)?

## The website: Equipment and Inventory

Three mockups (generated pictures to go by, not designs to copy; the numbers in them are invented):

- `docs/mockups/gear-equipment-mockup.png`: **Equipment with outfits.** Outfits per skill down the left with
  the two switches under them, a 3D doll in the middle with the slots around it (gem dots on each, a pin on a
  pinned slot, a plus on an empty one), stats on the right with what the outfit changes. The "Set bonus" box in
  the picture is not a feature the game has; it is there as an idea only.
- `docs/mockups/gear-inventory-mockup.png`: **Inventory.** Search, filter chips, sorting, a grid with rarity
  borders, stack counts, an "E" on worn items and a green arrow on upgrades, and a side panel for the picked
  item: compare with what is worn, the gem sockets, Equip, Add to outfit, Send to stash, Sell. A bag meter and
  "Select several" for doing one thing to many items.
- `docs/mockups/gear-unified-mockup.png`: **One "Gear" tab.** The doll and the bag side by side: a row of skill
  buttons picks the outfit, pieces that fit it are outlined, and a piece is dragged from the bag onto a slot.

**Recommendation:** the third for wearing things and the second for everything else. That is, an Equipment tab
built like the "Gear" mockup (doll, outfit row, and the wearable part of the bag beside it for dragging), and an
Inventory tab like the second mockup for the whole bag: selling, the stash, gems, food, resources. The outfits
list of the first mockup becomes the row of skill buttons.

What it can reuse: the 3D character of the customizer (three.js, with the pre-rendered fallback), the gem picker
and socket rows, the item tooltip, the pets tab's pictures.

## Steps, if approved

1. Outfits on the server, in the game and in chat (no website needed to benefit: it remembers by itself).
2. The Equipment tab (doll, outfits, pins, drag and drop, phone layout).
3. The Inventory tab (grid, filters, compare, several at once).
