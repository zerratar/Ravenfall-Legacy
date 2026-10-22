# Release checklist: the network v2 game version

For the release that ships the Unity 6.7 upgrade, the visual overhaul, village build mode and roads, and the v2
server connection (`docs/network-v2.md`). Steps are in order; the server goes first, the game last.

## 1. Server (RavenNest `master`)

1. **Back up the production database.**
2. **Deploy RavenNest `master` and start it once.** At startup it adds the missing tables and columns itself
   (`DatabaseSchema`, logged line by line), including the village house positions, the town hall tier and roads.
   Expect one warning: "AppSettings:TokenSigningKey is not set" (step 5).
3. **Stop the server and run `sql/village-house-freeze-default-layout.sql`.** It stores the old default position
   of every built house that has none, so existing villages keep their look when the new client's default layout
   arrives. It needs the columns from step 2, and the server must be stopped (it keeps everything in memory and
   would write the old values back). Then start the server again.
4. **Check that today's game still works**: a streamer on the released game (0.9.4.3a) can start a session,
   players join and gain experience (saved), and events arrive. The old path is unchanged for them.
5. **Token signing key** (closes forged auth and session tokens): add `AppSettings:TokenSigningKey` and restart.
   Every player and streamer then logs in once more, so this is best done together with the release.
   Generate the value with:
   `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)) | Set-Clipboard`

## 2. Game

6. **Unity version**: the project is on 6000.7.0b4 (beta). 6000.7.0b6 is the newest 6.7 beta; 6.7 is what makes
   CoreCLR player builds possible. Decide whether to move to b6 first.
7. **Version**: bump `bundleVersion` (ProjectSettings, now 0.9.4.4a) and set the server's expected client version
   when the build is published, so streamers are told to update.
8. **Test a development build against production** (or staging), before publishing:
   - it says "Using the v2 server connection: wss://www.ravenfall.stream/api/v2/game" and the server logs
     "v2 session started" (this is the first time v2 runs over wss with the real certificate, and on CoreCLR);
   - players join, gain experience; restart the game and the progress is there;
   - pull the network for a minute: it reconnects and nothing is lost;
   - Menu > Exit and Alt+F4 show "Saving players to the server before exiting..." and close within ~3 s;
   - commands from chat: join, craft, equip, gift, vendor.
9. **Publish the build.**

## 3. Bots

10. **Redeploy ROBot** from RavenBot `master` (heartbeat, one writer per link, commands survive reconnects,
    split long replies). It works with old and new games, so it can go before or after the game.
11. **Release the local bot** (same fixes for streamers who run it themselves).

## 4. After the release

Watch the RavenNest log for:

- `v2 session started` per streamer, and `v2 Hello ... refused` (token or version problems);
- `Missed N inventory change(s)` in game logs (the game repairs those itself; many would point at a server issue);
- the save flush (`Saving ... failed`, back-off messages).

If v2 misbehaves:

- **For everyone**: set `AppSettings:DisableGameConnectionV2` to `true` and restart the server. Games are told to
  use the old path and switch over by themselves; no new client is needed.
- **For one streamer**: start the game with `-network-v1`.
