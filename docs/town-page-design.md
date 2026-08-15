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

## Not started

Analysis only. The build wants a fresh session: it is a new service method, a new page and a new
layout, and the first two thirds of that are not verifiable in a static harness.
