# Migrating the UI to UI Toolkit

A long running plan, done one screen at a time, with a shippable game at every point. uGUI and UI
Toolkit coexist in the same project, so there is never a big bang cutover.

## Why, in order of how much it actually matters

**1. Consistency stops being a discipline problem.** The UI drifted across years of solo work
because nothing enforced a shared style. uGUI can be made consistent with prefab discipline, but
nothing makes you keep it. USS does: one stylesheet, one set of tokens, and consistency becomes the
default rather than something to remember. Three duplicate SDF assets of the same font sitting in
the project (`FOT-NewRodin Pro M SDF 1/2/3`) is that drift made visible.

**2. The UI leaves the scene file.** This one is easy to miss and worth a lot here. There are no UI
prefabs; the entire interface is built into the scene hierarchy, which is why `MainWorld.unity`
holds 971 RectTransforms and is 125 MB. Every UI tweak today means editing a 125 MB binary-ish YAML
file that cannot be reviewed, cannot be merged, and had to be moved to Git LFS to be pushable at
all. UI Toolkit layouts live in `.uxml` and `.uss` text files, outside the scene. Migrating a
screen physically removes it from `MainWorld.unity`, and the result diffs cleanly.

**3. Room to grow.** Pets, NPCs, quests and achievements all need UI. Adding those to the current
setup means more hand placed hierarchy in the same scene.

**4. Performance.** Real, but the smallest reason. Do not lead with it.

## What is actually in scope

Corrected from an earlier estimate of "143 files with a Canvas": almost all of those are
third party demo scenes, mostly SuperScrollView. The real UI is:

| Scene | Canvases | RectTransforms |
|---|---|---|
| `MainWorld.unity` | 2 | 971 |
| `Overlay.unity` | 1 | 35 |
| `CodeOfConduct.unity` | 1 | 18 |
| `Update.unity` | 1 | 12 |

Around 1,036 UI objects in four scenes, driven by 34 scripts. That is a real project, but it is
roughly ten screens, not 143.

### Screens

- Player details (the observed player panel, the most seen UI on stream)
- Player list and its rows
- Settings, with graphics, sounds and UI tabs
- Main menu
- Island details
- Player inventory
- Notifications: arena, raid, stream raid
- **Game restore overlay** - the full screen "Game is being restored / Downloading characters
  data" cover shown while `GameCache.IsAwaitingGameRestore` is set. It sits over everything, so it
  is both high visibility and a good early candidate: it has almost no interaction, only text.
- Stat and skill observers
- Code of Conduct screen (own scene)
- Update screen (own scene)
- Overlay scene UI

### Not migrating

World space UI stays exactly as it is. UI Toolkit's runtime is screen space; world space is not a
real answer there.

- `NameTag` and `NameTagManager`
- `HealthBar` and `HealthBarManager`
- `DamageCounter` and `DamageCounterManager`
- `MessageBubbleManager`
- DamageNumbersPro, an entire TMP based asset

## Notifications carry text baked into images, which blocks localization

The arena, raid, dungeon and duel notifications do not draw their headline text. They toggle
GameObjects holding pre-rendered images with the English wording painted in, and only the dynamic
parts (names, timers, levels) are real text.

| Image | Size |
|---|---|
| `CongratulationsForWinning.png` | 997x76 |
| `ArenaStart.png` | 866x99 |
| `ComeFight.png` | 653x59 |
| `ArenaIsAboutToStart.png` | 789x196 |
| `ArenaIsNowActive.png` | 684x196 |
| `ArenaDraw.png` | 841x314 |
| `RaidBoss.png` | 980x307 |

Those aspect ratios are the tell: they are lines of text, not artwork.

This matters beyond consistency. **Text inside a PNG cannot be translated.** A Spanish speaking
streamer currently gets English banners no matter what, because the words are pixels. The game
already has `Localization.cs` with 261 string constants for bot messages, so the infrastructure
exists; the UI is what was left behind.

Each image also had to be redrawn by hand whenever the style changed, which is a large part of why
these screens drifted apart from each other over the years.

Converting them to real text in UXML therefore does three things at once: it makes the
notifications consistent, it removes seven hand maintained assets, and it is the prerequisite for
the localization pass later. That is why notifications rank above Player Details despite being less
visible, even though Player Details is the more watched panel.

## Rule: never disable a Canvas GameObject, disable the Canvas component

The project's convention is to put UI scripts **on the Canvas object and on its children**, because
that makes them easy to find in the hierarchy. So switching off a Canvas GameObject to hide the old
UI also stops those scripts from running, and it fails completely silently: nothing executes, so
there is no error, just a screen that never populates.

This already bit both migrated screens. `GameUpdater` and `CodeOfConductController` are components
on the Canvas object itself, so disabling it meant `Awake` never ran and the new UI sat empty while
looking correctly styled.

**Hide old UI by disabling the Canvas component, and the GraphicRaycaster with it** so the hidden
hierarchy cannot swallow clicks. Leave the GameObject active.

Run `tools/canvas-script-preflight.py <scene>` before touching a screen. It reads the scene file
directly, needs no editor, and lists every one of our scripts on or under each Canvas. Current
state:

| Scene | Canvas | Our scripts |
|---|---|---|
| `Update` | Canvas | 1 on the canvas itself (`GameUpdater`), 1 on a child |
| `CodeOfConduct` | Canvas | 1 on the canvas itself (`CodeOfConductController`) |
| `MainWorld` | Canvas2D | 52 on children |
| `MainWorld` | Canvas3D | 25 on children, mostly `LookAt`, this is the world space canvas |

MainWorld carries 77 of our scripts under canvases. That is the real reason its screens have to be
migrated one panel at a time rather than by switching the canvas off.

## A second silent failure worth knowing about

Unity runs **every `Awake` before any `OnEnable`**, and `UIDocument` builds its visual tree in
`OnEnable`. A controller that writes text from `Awake` is therefore writing into a tree that does
not exist yet. Nothing throws; the UXML placeholder text simply stays on screen.

Both views handle this by holding values until the tree exists and flushing them once it does. Any
new screen view should do the same rather than assuming the tree is ready.

## The design constraint that should drive every decision

**This UI is watched through a video encoder by an audience, not just operated by a player.**

Twitch at 1080p is around 6000 kbps, and a large share of viewers watch at 720p or lower, often on
a phone, while also reading chat. H.264 at that bitrate destroys thin strokes, low contrast pairs,
small text, subtle gradients and fine outlines.

That has a direct consequence for the existing direction. The project carries 18 weights of
FOT-Rodin and FOT-NewRodin Pro, the NieR:Automata typeface family. NieR's look is thin type, low
contrast, beige on beige. It is beautiful at native resolution on a monitor and turns to mush
through a stream encoder.

The useful part: the bold weights of the same family are already in the project
(`FOT-NewRodin Pro B`, `DB`, `EB`, `UB`). Keeping the family and dropping the Light weights
preserves the identity that was wanted while surviving compression. That is continuity, not a
reset.

Rules of thumb that follow:

- High contrast, solid fills rather than gradients
- Heavier weights, larger minimum sizes than a desktop app would need
- Fewer things on screen at once; a viewer glances, they do not study
- Anything that must be readable on a phone at 720p gets tested at that size, not at 4K

## Order of work

**Step 0, before any migration: the design system.** Migrating an inconsistent UI to UI Toolkit
produces an inconsistent UI in USS. Define the tokens first: colour, type scale, spacing, panel
chrome, and the small set of shared controls. Everything after this hangs off that file.

Then, smallest and most isolated first, so the cost per screen is learned somewhere cheap:

1. **`Update.unity`** - 12 RectTransforms, its own scene, seen rarely. The ideal spike.
2. **`CodeOfConduct.unity`** - 18, also isolated.
3. **`Overlay.unity`** - 35, self contained, and it is what viewers see.
4. **Notifications** - small, self contained, high visibility on stream.
5. **Game restore overlay** - text only, no interaction, but covers the whole screen so it is worth
   getting right early.
6. **Player details** - the most seen panel, and the one that benefits most.
7. **Player list and rows** - depends on virtualised lists; UI Toolkit's `ListView` replaces
   SuperScrollView here.
8. **Settings and its tabs** - the largest script at 522 lines, but low traffic and low risk.
9. **Island details, inventory** - last.

Each screen: build it in `.uxml` plus shared `.uss`, port the driving script to query the visual
tree, delete the old hierarchy from the scene, play test, commit. One screen per commit so any of
it can be reverted alone.

## Notes on the work itself

- Do not invest further in optimising uGUI panels that are on this list. `PlayerDetails` was
  already given a refresh throttle, which was worth it because the migration is months out, but
  going deeper there would be wasted.
- Every screen removed shrinks `MainWorld.unity`. Track the size; it is a good progress metric and
  it directly reduces the LFS problem.
- `SuperScrollView` can eventually be dropped once lists move to `ListView`, which removes a large
  third party dependency.
- Keep a screenshot of each screen before migrating. Consistency is the goal, but changes should be
  deliberate rather than accidental.
