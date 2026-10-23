# Sounds

Besides the music ([music.md](music.md)), the game plays recorded sounds from the Environment Sounds pack
(Asset Store) and from `Assets/Sounds/Effects`. Without them it stays silent, except the weather, which then falls
back to its generated rain and thunder (`WeatherSounds`).

## What plays

**What players do** (`GameSounds.Play`, 3D, at the target, timed to land on the swing):

| Action | Sound |
|---|---|
| Woodcutting | an axe in the tree; the tree falling when it goes down |
| Mining | a pick on rock |
| Fishing | a splash |
| Farming | soil |
| Gathering | leaves and branches |
| Cooking | sizzling |
| Alchemy | bubbling |
| Crafting | hammering or sawing |

With hundreds of players at work that would be a wall of noise, so: only within 45 m of the camera, at most three of
a kind at once and not closer than a tenth of a second apart, and other players at about half the volume of the one
the camera follows (who is always heard).

**The place round the camera** (loops that grow and fade):

- wind, stronger in rain and storms and on Iceheim, with gusts when it is up;
- birds by day and crickets at night, insects on warm days, all quieter in rain (not on Iceheim);
- waves, by how much open water there is round the focus;
- the village by day, on the home island;
- crackling at the four fires and torches nearest the focus;
- now and then a call from an animal near the focus (cows, chickens, roosters, sheep, pigs, horses, dogs; by whole
  words of their names); a rooster at dawn in the village.

Down in a dungeon all of these go quiet.

**The weather** (`WeatherEffects`): light, moderate or strong rain by how hard it rains, crossfading as it changes;
recorded thunder, far strikes muffled and lower.

## Settings

Sound: **Sound effects** (everything above but the weather), next to Weather sounds, Music and Master.

## Adding sounds

`SoundLibraryBuilder` builds the library (`Assets/Sounds/Resources/SoundLibrary.asset`) when sounds change and before
every build (or Ravenfall > Rebuild Sound Library). A pack sound's kind comes from its name ("Wood Chop 1-3"); loop
kinds only from the pack's Ambient Loops folder. Our own sounds go in `Assets/Sounds/Effects/<Kind>/`, for example
`Assets/Sounds/Effects/WoodChop/`, and come before the pack's. The kinds are `SoundKind`. At most eight of a kind
are used, so a build carries only what plays.

Loops stream from disk in stereo; one-shots are kept in memory, compressed, in mono (they are placed in the world).
Neither the pack nor the library is in git; builds come from the machine that has them.
