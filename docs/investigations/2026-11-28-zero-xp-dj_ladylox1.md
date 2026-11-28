# Zero experience report, dj_ladylox1 (2026-11-28)

Discord report 2026-11-27: both characters stopped gaining experience in every skill, on every stream, through !leave and !join. A new account in the same streams gained normally. The player suspected a stuck session lock on the server.

Read only investigation of production (game 0.9.6.1a, RavenNest at 3d7f131d): the twice-daily data backups, the server logs, the bot's command log, the public highscore API. Nothing on the server was changed.

## Verdict

No account or server fault. The server took every save it got. The zero came from how the game works, and it looks like a bug to the player:

1. (Corrected under "What would fix it": the real cause was a healer with no fighting area, combat level under the island's.) A healer only gains experience by healing someone below 90% health on the same island: a non-healer, or itself. With nobody hurt, the healer targets itself, finds itself at full health, drops the target at once and does nothing. No animation, no walking, no experience, no message. The idle check skips healers (Healing counts as a skill that stands still), so nothing nudges it either. Same rule in 0.9.5, 0.9.6 and on master (Tasks/FightingTask.cs, GetTarget and IsCompleted).
2. The tests with other skills were made on Kyo and Heim, where those skills are below the level gate, so they give 0 by design. The game even replied "You are not high enough level to train Fighting here ... at least 300" to the player's !train defense on Heim.
3. Earlier the same morning the player rested in the onsen (0 by design), and a x100 experience boost ended at 09:30 UTC. Both make normal play feel like zero after a boosted week.

How sure: high that the server and account are fine. Fairly high that character 1's 18 hours of zero on Kyo were the idle healer case (the evidence is below). Low certainty about 09:22 to 10:15 on Heim, which the backups cannot resolve.

## Timeline (UTC)

The account and character 1 were made on 2026-11-21. Healing went from 100 to 370 in six days, with heavy boosts. Character 1 lived in a 24/7 stream (stream A). Stream B is another 24/7 stream.

| Time | What happened (bot command log, backups, logs) |
|---|---|
| 11-27 05:19 | Healing 370 reached (skill record). Health 290 at 05:50. |
| 11-27 05:54 | `!rest` in stream A on Heim. Experience stops about here: the experience inside the levels fits a stop at about 05:57. |
| 05:56 to 09:21 | Resting. |
| 09:22 to 09:23 | `!train healing`, `!train defense` (bot: "not high enough level ... at least 300"), `!train healing`. |
| 09:30 | A x100 experience multiplier ends in both streams. |
| 10:15 to 10:21 | `!travel kyo` (own rowboat), `!unstuck`, `!train healing 999`. |
| 10:22 to 10:26 | `!onsen leave`, `!leave`, `!join`, `!train healing`, `!train mining` (Mining 168 is below Kyo's 200), `!leave`. |
| 10:28 to 10:31 | Stream B: `!join`, `!train healing`, `!stats`, `!leave` (3 minutes in all). |
| 10:31 | Stream B: `!join 2` makes character 2, then `!train all` at Home. |
| 10:34 | Stream A: `!join` with character 1 again. This is its LastUsed. |
| 10:34 to 11-28 04:04 | Character 1 stays in stream A on Kyo. Its skills and its whole saved state (position, health 290/290, ExpPerHour 0) do not change across three backups. |
| 11-27 10:31 to 11-28 04:04 | Character 2 trains in stream B and gains normally: combat 1 to 12 by 16:04, 30 by 03:04, 32 by 04:01. |
| 11-28 04:14 | `!rest` in stream A. |
| 11-28 07:32 to 07:33 | `!train healing`, `!travel heim`. |
| 11-28 08:48 | Public highscore: Healing 374 (since 08:48), Health 293, Sailing 28 (07:35). Character 1 gains again. |

## What the data rules out

- Account flags: User.Status is null (OK), no ban, no patreon row, no status effects, no clan, no odd properties.
- Session lock: the locks always followed the player's joins (each LastUsed and lock in the backups matches a !join in the bot log). The server never once moved character 1 between streams on a save ("Changing session" is logged about 4,050 times for others, never for this character). The refusal itself is only logged in DEBUG builds on the v2 and delta save path, so its absence proves nothing; what does is that character 1 was locked to the very stream it sat in, and a refused save also tells the game to remove the player, which the player would have seen.
- Server refusing saves: in 3d7f131d a save is dropped only when the session owner is banned (Status >= 1), the character is locked to another live stream that saved it within 5 minutes (TryClaimForSession / TryMoveToSession), the level is over 999, or the skill index is unknown. Character 1 sat in the stream it was locked to, and character 2 saved fine. The obsolete TCP save also skips "Compromised" characters, but nothing sets that for this account.
- The player's own stale game session (started 2026-11-21, never stopped) is normal: 2,329 such rows exist, and nothing on the play path reads it.
- Inventory and appearance look ordinary, so a failed spawn (Update returns early until SetAppearance's callback runs) is unlikely.

## Why it points at the idle healer case

- For 18 hours in stream A, character 1's saved state stayed exactly the same: position (-359, -1, 75) by Kyo's arrival point, health full, ExpPerHour 0. A healer that was healing would have walked to its targets.
- Stream A's other Kyo fighters (9 to 11 of them, mostly melee around level 290 to 384, gaining) all had full saved health. Nobody on Kyo needed healing. Stream A's healers on Heim, where some fighters were hurt, kept gaining all night.
- Gains came back after the player sailed back to Heim on 11-28.

## What 0.9.6.1 does with a player that neither moves nor trains

- PlayerValidator marks a player stuck only when it has a target and has idled longer than its skill's limit. Healing, Mining, Fishing, Farming, Cooking, Crafting and Alchemy count as standing still (limit = never), so an idle healer is never marked stuck.
- !unstuck (PlayerUnstuck to PlayerController.Unstuck) teleports the player: to the raid or dungeon spawn, out of a duel, to Ravebridge's spawn, onto the ferry when it has no island, back into the onsen, or else to the island's spawn, and it sets the current task again. It does not change targets, so a healer with nobody to heal stands idle again at the spawn.
- Saved state is applied on every join (PlayerController.SetPlayer): island and position (under -3.5 m goes to the island spawn; dungeon goes to the island spawn; no island but a position means the ferry), onsen (rejoins the resting area if the island has one), rested time, auto join counts, auto train target, raid and dungeon styles, task. Character 1's saved state had nothing unusual: InOnsen, InRaid, InDungeon and InArena false, no destination, auto train target 0. The saved island and position (Kyo arrival point) do follow the character to every stream. On Kyo that put a healer where nobody was hurt.

## What would fix it

For this player (nothing to correct in the data; it already works again):

- Train healing where others get hurt (Heim in a busy stream, raids and dungeons). On a quiet island a healer gains nothing.
- Train other skills on an island that suits that skill's level (Woodcutting 1 at Home; Attack, Defense and Strength to Home or Away, combat level 168 is too low for Heim's 300).
- Character 2 was never affected.

For the game (fixed 2026-11-28, in the commit that adds this paragraph, "Healers no longer stand idle"):

- **Correction to point 1 of the verdict.** A play test with bots showed that on master (and in 0.9.6.1, the same code) a
  healer alone on an island already healed itself at full health and earned experience: DoTask drops the completed
  self target and, in the same frame, takes it again and heals. What really stood still was a healer with no chunk.
  `!train healing` sets the chunk with SetChunk, not GotoClosest, and with strict level requirements (the default)
  ChunkManager.GetChunkOfType only lets a fighting area in by combat level. Character 1 had Healing 370 but combat
  level 168, under Kyo's 200 (and Heim's 300): no chunk, so DoTask never ran: no animation, no experience, no message.
  GotoClosest has a fallback for this (any area the combat or the skill level reaches), which is why the healer worked
  again after `!travel heim` (the arrival goes through GotoClosest) and why it came and went with what else happened
  on the stream. Reproduced: a bot with Healing 750 and combat 391 told to heal on Eldara (700) got no chunk and
  stood still.
- **Fix.** SetChunk gives a healer the same fallback: the highest fighting area its combat or Healing level reaches, or
  the lowest, where the island's rules say "level too low" as for anyone (PlayerController.HealerChunk). The healer's
  own self target now stays until someone it may heal (the same rules as before) needs healing more, checked when the
  next heal is due instead of every frame (FightingTask.IsCompleted, FindHealTarget); the tower's healers check the
  same way. A heal on oneself at full health earns its experience but makes no threat, does not strike with Holy Fire,
  and shows no floating number (no number for any heal that restores nothing).
- Experience per hour measured with bots (Healing 976, Eldara, no rested boost): idle self healing 1.84M, healing a
  fighter kept at 50% health 1.90M, in a dungeon 2.02M, in the tower 2.23M. Idle healing is about as good as real
  healing, because the experience per heal does not depend on the amount healed. Not tuned; the owner's call.

Ideas not done:

- `!train healing` on an island whose areas are all above the healer (both levels) now heals with no experience, and
  only the no-experience warning (AnnounceNoExpGain) says why. A reply like GotoClosest's "not high enough" would be
  clearer.
- A healer told to heal on the world tower's islet takes SetChunk, not GotoClosest, so it never gets WorldTower.Settle.
- GotoClosest turns ChunkManager.StrictLevelRequirements off for the whole game when a player finds no area, until
  the session settings set it again: level rules that change with what someone else did.
- A !why or !exp reply that names the reason (resting, level too low, nobody to heal, target level reached) would have
  answered this report on its own.
