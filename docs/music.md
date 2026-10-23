# Music

The background music is a playlist per situation (`MusicManager`, `MusicLibrary`). Tracks are shuffled without the
same song twice in a row, with a short pause between them; a change of situation crossfades.

## Adding tracks

Drop the files in `Assets/Sounds/Music`. Each track's situation comes from, in this order:

1. **A folder** named after the situation: `Assets/Sounds/Music/Night/Owls.mp3`.
2. **The prompt number** its name starts with: `3.Dreamy Lullaby.mp3`, `3.Dreamy Lullaby (1).mp3` (the table below).
3. **A situation name** the file is called or starts with: `War.wav`, `Night - Owls.mp3`.

Anything else plays by day. Both versions Suno makes of a song can go in: they are two tracks of the same song, and
the playlist avoids playing them back to back. The library rebuilds by itself when files change and before every
build (or Ravenfall > Rebuild Music Library); music is imported to stream from disk, so many tracks cost no memory.

| # | Situation | Plays |
|---|---|---|
| 1, 2 | Day | the general music, anywhere by day |
| 3 | Night | at night |
| 4 | Rain | in rain and storms |
| 5 | Sailing | on the ferry, or the camera over the sea |
| 6 | Kyo | on Kyojin |
| 7 | Heim | on Iceheim |
| 8 | Eldara | on Eldara |
| 9 | Atria | on Atria |
| 10 | Ironhill | on Ironhill |
| 11 | Village | in village build mode |
| 12 | Tavern | in the tavern |
| 13 | Onsen | when the watched player is in the onsen |
| 14 | Menu | on the login screen, before the session starts |
| 15 | Dungeon | with the camera down in a dungeon |
| 16 | DungeonBoss | in a dungeon's boss room |
| 17 | RaidBoss | during a raid |
| 18 | War | during a war or stream raid |
| 19 | Arena | during an arena fight the camera watches |

Folders can also be named Home and Away for those islands.

## How the playlist picks

From the most specific situation down, the first that has tracks: menu, war, raid boss, dungeon boss, dungeon,
arena, tavern, village build mode, onsen, sailing, rain, night, the island, day. An island with its own tracks
mixes them with the day's (its own twice as often), so two island tracks do not repeat for hours.

Sudden changes (war, raid boss, dungeon, arena, the menu) switch within a second. Gentle ones (night falling, rain,
another island, off the ferry) once they have lasted 2.5 seconds, but at most once every 30 seconds: the camera moves
to another player every 10 seconds by itself, and the music should not change with every one.
With no library, the three clips on the MusicManager play as before (background, raid boss, war).

The tavern lute and Bewitched from the music packs are added to Tavern and Dungeon.

## Loudness

Every track plays at the same loudness: the library builder measures each file once with ffmpeg (EBU R128, cached
in `Library/RavenfallLoudness.txt`) and the playlist turns louder tracks down to -17 LUFS, the level of the quietest
Suno tracks (they measured -17 to -14.6; the old raid and war music -13.3). Only ever down, so nothing clips.
ffmpeg is looked for in `RAVENFALL_FFMPEG`, on the PATH and in `C:\Tools\ffmpeg\bin`; without it tracks play as
they are.

## Versioning

The music files are not in git (WAV is ignored, the Suno MP3s are not added), and neither is the library, which is
built from the files. Builds come from the machine that has them.
