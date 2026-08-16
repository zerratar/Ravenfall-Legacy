# Town page for the dashboard

Requested repeatedly and missing for years: a streamer's own town, showing the houses built, who is
assigned to each, the slots, the bonuses, the levels and the town's resources.

This is the analysis, not the change. Written so the build starts from a known position rather than
rediscovering it.

## The data exists. The service does not serve it.

| Thing | Where | State |
| --- | --- | --- |
| Town level, experience, name | `Village` | Real. Also has `resourcesId`. |
| Houses | `VillageHouse` | Real: `villageId`, `userId`, `characterId`, `slot`, `type`, `created`. |
| Town resources | `Resources` via `Village.ResourcesId` | Real, and reachable with `GameData.GetResources(resourcesId)`. |
| Per house bonus | `TownService.CalculateHouseExpBonus` | Real, and already correct. |
| Skill behind a house type | `TownService.GetSkillByHouseType` | Real, covers all eleven types. |
| Aggregate bonus per type | `TownData.GetActiveBonus` | Real. |

So almost nothing needs inventing. What blocks the page is the shape of the one method that reads
all this.

### `TownService.GetTownsAsync` is built for the public page, not for this one

Three things about it make it unusable here, and each is a small fix:

1. **It only walks `GetActiveSessions()`.** A streamer who is not live has no town at all, which is
   precisely the person most likely to be looking at this page. It needs a lookup by user id:
   `GameData.GetVillageByUserId(userId)` already exists and is what the loop calls anyway.
2. **It skips empty houses.** `if (house.UserId == null) continue;` means an unbuilt or unassigned
   slot never reaches the model, so the page cannot draw the town as a grid of slots. Empty slots
   are the interesting ones: they are what the streamer is being asked to fill.
3. **`TownHouseData` carries no name and no slot number.** It has `AssignedCharacterId` but not the
   character's name, and `VillageHouse.slot` is dropped entirely, so the houses cannot be laid out
   in their real positions.

It also never reads `Village.ResourcesId`, so town resources are absent from the model despite being
one field away.

### What to add

A second method rather than a change to the existing one, because the public `/towns` page depends
on the current behaviour:

```
TownService.GetMyTownAsync(Guid userId) -> MyTownData
```

- village by user id, so it works off stream;
- **every** house, empty ones included, carrying `Slot`, `Type`, `AssignedCharacterName`,
  `AssignedCharacterId`, `Bonus`, `IsActive`;
- the town's `Resources`;
- level, experience, and experience to the next level, which `GameMath` can already answer the same
  way the clan progress bar does.

## The page

`/town`, dashboard layout, in the nav under "Your stream" next to Bot.

Graphical, and it does not need artwork to be so. The town is a **grid of slots**, one tile per
slot, which is what the game itself is: a fixed number of plots. Per tile:

- the house type as an icon, using the same FontAwesome set the item filters already use, since the
  eleven house types map onto skills the site already has icons for;
- the assigned character's name, or an empty state that says the slot is free;
- the bonus that slot contributes, as a percentage;
- a dim treatment for a slot whose assigned character is not currently locked to this stream, which
  is what `IsActive` already distinguishes and which is the single most useful thing on the page: a
  house assigned to somebody who has stopped playing is a bonus the streamer is not getting.

Above the grid: town level with progress to the next, the slot counts that `TownData` already
computes (total, used, active), and the resources as `rf-stat` tiles.

Below it: the per type bonus summary from `GetActiveBonus`, so "what is my town actually giving me"
is answered in one place rather than by adding up tiles.

Everything above is `rf-tile`, `rf-stat`, `rf-statlist` and the rarity-style tinting that already
exists. No new components, and no images.

## Order

1. `MyTownData` and `GetMyTownAsync`. The page cannot be started before this and it is the only
   part touching the server.
2. The page, read only.
3. Only then consider whether assigning a house from the website is wanted. It is a write into live
   game state and belongs with the plan and commit work, not with a read only page.

## Built

All three of the above, except step 3, which stays deliberately not started.

`TownService.GetMyTownAsync` and `MyTownData` are in, alongside `TownHouseTypes`, which is the
house type vocabulary as one list in the shape `ItemFilterOptions` already uses: label, icon and
the skill behind it. The page is `/town`, in the nav under "Your stream" next to Bot. `/town` had
been a second route onto the public towns page; nothing linked to it, since the front page nav and
footer both point at `/towns`.

### What the analysis missed, and the page shows

The analysis stopped at "the data exists". Four things fell out of asking the next question.

- **When the next plot arrives.** Slots are ten town levels apiece with the Patreon tiers acting as
  a floor, which means a patron's next slot from levelling is the one past the floor they already
  have rather than the next multiple of ten. Working from the granted count rather than from the
  level is what makes that right.
- **How long the next town level takes.** `VillageProcessor` awards
  `GameMath.GetVillageExperience` once per session tick and that function is linear in elapsed
  time, so asking it for one second gives the rate. A level lands somewhere between three hours of
  streaming at town level 10 and 283 at 399. Two surprises come with it: the player count it passes
  is a fixed 750, so **a town levels at the same speed however many viewers are playing**, and
  Mithril and above doubles it.
- **Which skill a house type actually reads.** A Melee house is driven by the assignee's **Health**
  level. It is the one type not named after its own skill and nothing in the game says so.
- **The assignee's skill level**, next to the bonus, since the bonus is only that number over 999
  times 200.

### Two things the shared helpers get wrong here

Both were suppressed at the source rather than worked around in the page.

- `GetSkillByHouseType` falls through to **Mining** for `Undefined` and `NoSkill`, so an unbuilt
  plot would have been credited with a mining bonus. `GetTownsAsync` never noticed because the
  public page only ever asks `GetActiveBonus` for real skill types.
- `GameData.CreateVillage` sets an administrator's `Level` from `GameMath.ExperienceForLevel(30)`
  rather than from `30`, so those rows hold a village level in the tens of thousands. The slot
  count is capped anyway, so it is cosmetic, and the level is clamped the same way
  `VillageManager.GetVillageInfo` already clamps it for the game client. **The underlying bug is
  still there.**

`village.Name` is `"Village"` on every row and nothing ever changes it, so it is not shown. Same
question as the resources: the data exists, and it does not mean anything.

### The plot grid

`rf-plot` rather than `rf-slot`, which is already an equipment square. Four states, each named in
words as well as in colour: **counting**, **away** (assigned to somebody not playing here),
**free** (built, nobody in it) and **nothing built**. Away is what the page exists for.

`rf-badge--quiet` is new and general: a plain badge is drawn in full ink and outranks the good and
warn badges beside it, which is the wrong order when the plain one says "nothing built".

### Two faults the browser check caught

Both in the bonus summary, both invisible without measuring.

- **One grid for every row scrambled at the narrow breakpoint.** A bar told to span the full width
  cannot fit the row its name is on, so it takes the next row, the value follows it, and the next
  row's name lands in the value's column. It is a grid per row with `grid-template-areas` now,
  which cannot reflow that way.
- **Content sized side tracks gave every bar a different width.** Measured 727 to 836 pixels across
  seven rows. A set of bars that exists only to be compared by eye is worthless if the tracks
  differ, so both side tracks are fixed: 7rem holds the longest skill name and 5.5rem holds the
  widest value a town can reach, which is forty plots of one type at 200% each.

Verified at 1280, 760, 560 and 375. No horizontal document scroll at any of them.

### Choosing who lives on a plot

Built after all, once the client side was understood well enough to know the write was safe.

**Why it is safe.** `Assets/Scripts/VillageManager.cs` loads village info **once** when the session
starts and then waits for events. `LoadVillageAsync` only runs while `state == LoadingState.None`
and sets `Loaded` on success, so it is a retry rather than a poll. A website write would therefore
be invisible to a running game until the next session start. `SetHouseOccupantAsync` pushes a
`GameEventType.VillageInfo` event to the owner's session after every change, which is the same
event `SessionManager` already sends at session start and which `VillageInfoEventHandler` turns
into `Village.SetHouses`. That handler is guarded by `HousesAreUpdating`, so it will not fight an
in game update in progress. With nothing running there is no event to send and none needed.

**Only people playing on the stream can be chosen.** A plot given to anybody else contributes
nothing until they arrive, which is the state the page exists to warn about, so offering it as a
choice would be offering the problem. Emptying a plot works with the game off and is the useful
half when it is: it frees a plot held by somebody who has stopped playing.

**Candidates are ordered by what they would be worth in that plot.** That is the question being
asked and nothing else answers it: `!village` assigns whoever typed it, with no notion of who
would be best. A candidate already holding another plot says so, since choosing them moves them
rather than adding them.

**Two bugs in the existing path, worked around rather than inherited.**
`VillageManager.AssignPlayerToHouse` sets `UserId` without touching `CharacterId`, and
`RemoveHouse` clears `UserId` and leaves `CharacterId` behind. A slot can therefore carry a
character id belonging to whoever held it before. `DescribeHouse` only trusts the named character
when it still belongs to the assigned user, and the write here sets both fields together.
`GetTownsAsync` has the same flaw and still does. Believing a stale id names the wrong person and
lets their skill decide the bonus.

### Building a plot, and setting them all

Added after the above, from use: the page could change who lived on a plot but not what was built
on it, which is the half that decides which skill is boosted.

**Per plot**, the type picker sits above the occupant list in the same dialog. The two are decided
together: the type decides which skill the plot reads, and the occupant list is ordered by exactly
that skill. Changing the type keeps the occupant, the way the in game `BuildHouse` does, since a
plot is normally retyped to suit whoever is already in it. Demolishing is the fifteenth chip,
"Nothing", and does clear the occupant, because nobody lives on a bare plot; without it a plot
could never go back to empty from the website.

**Setting every plot** mirrors `SetVillageBoostTarget` exactly: one type on all of them, then
everyone playing ranked by the skill that type reads, best into the lowest slot, as many as there
are plots, remainder emptied. Two deliberate differences from the game.

- **It shows the result first.** The command picks and commits in one keystroke, which is fine when
  you are looking at the town and not fine for a control that rewrites forty plots and every
  assignment on them. The preview names who moves in, which plot each lands on, what the town would
  be worth, and says outright that anyone not in the list loses their plot. This is the plan and
  commit idea applied to a control that already existed.
- **Off stream it retypes the plots and leaves the occupants alone.** The game can only run this
  while live, so it always has players to assign. With the game off there is nobody to move in, and
  clearing every plot to replace them with nothing is destruction rather than a rearrangement.

`rf-panel__head` is new, and general: a panel title with a control beside it, the third place to
want that shape after `rf-inv__head` and `rf-adminuser__head`.

**A harness trap worth recording.** The harness used `d-none` as its own mode class to hide a
dialog. Bootstrap is loaded on these pages and defines `.d-none { display: none !important }`, so
it hid the entire `body` and the page measured as zero by zero. Harness class names have to avoid
the utility namespaces the real page loads, the same way page class names do.

**A third layout fault**, from the same browser check: the dialog was a sibling of
`rf-modal-backdrop` rather than a child of it. The backdrop is the flex box that centres the modal,
so as siblings nothing centred it and it rendered below the fold. Every other converted page nests
them; this is the one place the pattern is not enforced by anything.

### Known and not fixed

The write is last one wins against an in game `AssignVillage`, which rewrites every plot at once. A
website assignment made in the same moment as a `!village` type change is overwritten. The
consequence is one assignment lost, and closing it properly needs locking that nothing else in the
village path has.
