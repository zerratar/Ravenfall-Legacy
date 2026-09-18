# Feature opportunities

A survey of what the server already stores against what the site and game actually show. The
method was the same one that has been paying off all week: read the domain models, then check
whether anything writes to them.

**Every row below carries a verdict, because the difference matters.** A model with a live writer
is a feature waiting for a screen. A model with no writer is a stub, and `ClanBankItem` already
proved this codebase has those. Where a verdict says "plumbing only", the entity is loaded but I
could not confirm from source whether rows exist; that needs one look at the database.

| Thing | Verdict | Why it matters |
| --- | --- | --- |
| `CharacterSkillRecord` | **Live.** Written in three places, read by the highscore provider. | A dated level-up history per character. |
| `NPC` + `NPCItemDrop` + `NPCSpawn` | **Stub. Tables are empty and nothing seeds them.** | Bestiary stays blocked. See the correction below. |
| `ExpMultiplierEvent` | Registered and loaded. | Nothing on the site says a 2x weekend is running. |
| `ItemRecipe` + `ItemRecipeIngredient` | Loaded; `ItemService` already exposes lookups. | Crafting is invisible on the site. |
| `DailyAggregatedEconomyReport`, `DailyAggregatedMarketplaceData` | **Live, and already surfaced under `Pages/Admin/Economy/`.** I missed those pages. | A *public* economy page is the opportunity, not a first one. |
| `Pet` / `PetTier` / `PetType` / `BattlePet` | On `Player` already. | Pets are owned and never shown. |
| `CharacterSessionActivity` | Loaded. | What a character did in a session. |
| `Achievement` / `CharacterAchievement` | **Stub.** `CharacterAchievement` appears in exactly one file: its entity set declaration. Nothing awards one. | Would have to be built, not surfaced. |
| `Title` (the entity) | **Probably a stub.** The many matches for "Title" are notification titles and Patreon pledge titles, not this. | Same. |

---

## 1. The bestiary: I was wrong, it is still blocked

I previously wrote that the bestiary looked unblocked, because `NPC`, `NPCItemDrop` and
`NPCSpawn` are registered entity sets with stats, drop chances and boss flags, and the drop
lookup is already indexed by NPC.

**The tables are empty and nothing seeds them.** Confirmed by the person who runs the server. The
plumbing is real and the schema is right, but there is no data and no writer, which puts it in the
same class as `ClanBankItem`: a table built ahead of a feature that never landed.

The original assessment in [dashboard-design-system.md](dashboard-design-system.md) therefore
stands. Filling a bestiary needs a source, and the game client is the only thing that knows its
own enemy and drop tables.

**The lesson, which is why this section stays.** I checked that the entity was *referenced* and
concluded the feature existed. Being referenced by `GameData` and the DbContext only proves
somebody declared it. The test that separates a live feature from a stub is **what writes rows**,
and for NPCs the answer is nothing. I applied that test correctly to `CharacterAchievement` two
sections later and failed to apply it here, because an indexed drop lookup looked like intent.
Intent is not data.

Anything below marked "plumbing only" carries the same caveat and deserves the same check before
anyone plans work around it.

## 2. Level-up history, which unlocks the thing I guessed at

`CharacterSkillRecord` stores `characterId`, `skillIndex`, `skillName`, `skillLevel`,
`skillExperience` and **`dateReached`**, and it is genuinely written.

Earlier I suggested storing exp per hour over time to turn "about 6 hours" into a real projection,
and filed it as future work. It turns out a level-up history has been accumulating the whole time.
From it, with no new writes:

- **A progression timeline.** When each level landed, per skill. This is the single most nostalgic
  screen a long running RPG can offer.
- **"3 levels this week"** on the character page, and a personal best.
- **Honest pace.** The gap between the last two level dates is a measured rate, not a snapshot of
  `ExpPerHour`, so it survives the character being idle.
- **Clan and stream milestone feeds.** "Zerratar reached Woodcutting 99" is a real event with a
  real timestamp, and it is the natural content for the clan activity feed already proposed in
  [clan-features-design.md](clan-features-design.md).

This is the highest value item on the list. The data exists, it is dated, and nothing shows it.

## 3. Exp multiplier events

`ExpMultiplierEvent` is loaded and the game runs multiplier weekends. The site never mentions one.

A banner on the overview saying **"2x experience until Sunday"** is a few lines and it is the
single most effective thing a site can do to bring someone back into the game. If the event has an
end time, a countdown makes it better. This is probably the best value to effort ratio in the whole
document.

## 4. Where do I get this item

`ItemRecipe`, `ItemRecipeIngredient` and `ResourceItemDrop` are loaded, and `ItemService` already
has `GetItemRecipesByIngredient` and `GetResourceItemDrop` sitting unused by any page.

Combined with the NPC drop table, every item could answer:

- what it is made from, and what it is an ingredient for
- which skill and level crafts it
- which monsters drop it, and at what chance
- what it sells for

The item detail dialog built this week is already the right place to put it, and the rarity and
item cell components already exist. This turns the stash from storage into a reference.

## 5. Economy

**Correction.** I wrote that nothing reads these aggregates. There is a whole `Pages/Admin/Economy/`
folder that does: `EconomyOverview`, `Marketplace`, `MarketplaceAvgPrice`,
`MarketplaceItemAmountsSellers`. I surveyed the models and the dashboard and never opened the admin
pages, so I reported "unused" when I meant "not used anywhere I looked".

The opportunity is still there but it is a different one: the economy data is **admin only**.

A public economy page - what things sell for over time, what is scarce, what the marketplace is
doing - is the kind of thing a trading community builds spreadsheets for when the game does not
provide it. The aggregation is the expensive half and it is done.

## 6. Pets

`Player` carries `BattlePets` and `ActiveBattlePet`, with `Pet`, `PetTier` and `PetType` behind
them. The site shows none of it. Pets are collectible, which means a collection screen with the
ones you have not got greyed out, which is exactly the kind of thing that makes people go and play.
The equipment slot grid built this week is the pattern to copy.

## 7. Smaller, still worth it

- **Session activity.** `CharacterSessionActivity` could give a stream a "what happened tonight"
  summary, which is content a streamer would put on screen.
- **Duels.** `DuelRequest` exists. A win/loss record per character is a leaderboard nobody has.
- **Village.** `VillageInfo` and `VillageHouseInfo` are modelled and the Patreon page talks about
  house counts, but there is no village screen.
- **Scrolls.** `ScrollInfo` and `ScrollType` are modelled; the stash shows scrolls as ordinary
  items with no explanation of what using one does.
- **Gifting.** `GiftTransaction` is recorded. A gift history is both a nice screen and an audit
  trail.

## 8. Achievements are a stub, and that is worth knowing

`Achievement`, `AchievementReward`, `CharacterAchievement` and `UserAchievement` all exist as
models. `CharacterAchievement` is referenced in exactly one place in the entire solution: the line
in `GameData` that declares its entity set. **Nothing awards an achievement and nothing reads one.**

The same appears true of the `Title` entity, which has a `bonusType` and a `bonusValue`, implying
titles were meant to grant stat bonuses.

This is not a reason to avoid achievements. It is a reason to cost them honestly: they are a
feature to build, not a feature to surface, and they should be quoted alongside the bestiary rather
than confused with it.

---

## What I would do first

1. **Exp multiplier banner.** Smallest thing here with the largest effect on people opening the
   game.
2. **Level-up history on the character page.** The data is live and dated, and it makes the
   character page something you revisit rather than check.
3. **Item sources in the item dialog.** The lookups already exist and are unused, and the dialog is
   already built. Check first that recipes are not already surfaced in the admin economy pages.

Everything above is website work against existing data, with no game client change and no schema
change, which is what makes it worth doing before the larger clan bank work.
