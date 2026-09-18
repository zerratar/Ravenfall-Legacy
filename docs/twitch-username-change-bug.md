# Twitch username change: why the bot stops connecting

Status: fixed in code, not yet play tested. Spans RavenNest and RavenBot. The game client
turned out not to be involved: it sends no username at all.

## What was changed

| Where | Change |
| --- | --- |
| `RavenBot` `UserProvider.Get(Guid)` | Re-reads the name from settings on every call instead of only when the user is first created. This is the fix for the reported symptom. |
| `RavenBot` `UserProvider.Get(string, string, string)` | Assigns `Username` as well as `DisplayName` when matching on a platform id. |
| `RavenNest` `UserNameSync` (new) | One place that writes `User.UserName`, `User.DisplayName`, `UserAccess.PlatformUsername` and every `Character.Name` together. |
| `RavenNest` `SessionManager.BeginSessionAsync` | Resolves the current Twitch login from the stable account id on session start and applies it. This is what removes the manual step. |
| `RavenNest` `AuthService`, `SessionInfoProvider`, `PlayerManager` | All three now go through `UserNameSync` instead of each writing a different subset. |
| `RavenNest` `GameData.GetUserByUsername` | Falls back to `FindUser`, so a half renamed account is still found by the Discord endpoints. |
| `RavenNest` `TwitchRequests.GetUserByIdAsync` (new) | Looks an account up by numeric id using an app access token, so no user sign in is needed. |

### Live renames

A rename made part way through a stream is now handled too, in two halves:

- `PlatformNameWatcher` (new hosted service) sweeps live sessions every 5 minutes, asks Twitch
  what those accounts are currently called, and applies anything that changed. Lookups are
  batched 100 ids per request, so every live streamer costs one or two requests per sweep.
- `StreamBotApp.CheckSessionOwnerNames` runs every 15 seconds on the bot, re-reads the session
  owner's name, and hands a changed name to `sessionManager.Update`, which raises the event
  that leaves the old channel and joins the new one.

### The push to the bot never arrived

`RavenBotApiClient.UpdateUserSettingsAsync` delivers settings by writing
`{FolderPaths.UserSettingsPath}/{userId}.json` and trusting the bot to notice. That only works
if both processes resolve to the same folder. They do not:

- Ravenfall writes `C:\Ravenfall\Data\user-settings`
- the bot reads `G:\Ravenfall\Data\user-settings` on Windows, `../user-settings/` otherwise

and the production bot host is `ravenbot.ravenfall.stream`, a different machine, where no
shared path can work at all. So logging in on the website updated Ravenfall and never told the
bot, which is a large part of why that never fixed anything.

There was already a network route for this. Handlers are keyed by class name, so the bot's
`UserSettingsPacketHandler` has always been reachable as `usersettings` on the same HTTP
transport as `pubsub` and `userrole`, with retry and host failover. It had simply never been
called. `PushUserNamesAsync` now uses it.

### Detect and push in the same place

The push lives in `SessionInfoProvider.StoreAsync`, not in the login services. Every Twitch
login route reaches `StoreAsync` first, through `SetTwitchTokenAsync`, and that is where the
rename is applied. A login service calling `UserNameSync.Apply` afterwards gets false back,
because the work was already done, and would conclude there was nothing to tell the bot. The
database would be correct and the bot would never hear about it, which is the exact failure
being fixed.

The rule that avoids this: whatever detects the change sends it. Anything checking after the
fact cannot tell "nothing changed" apart from "someone else already handled it".

The Kick branch of `StoreAsync` also only assigned `UserName` when it was null, so a Kick
rename never applied at all. It now goes through the same path as Twitch.

`PushUserNamesAsync` is deliberately called only where a rename was actually detected.
`UpdateUserSettingsAsync` runs on every successful player join, so pushing from there would
have meant a couple of thousand extra requests per thousand player stream for names that had
not changed.

Also fixed on the way: `RavenBotApiClient.UpdateUserSettings` had its directory check inverted
(`if (Directory.Exists(dir)) Directory.CreateDirectory(dir)`), so on a fresh install the write
threw and the settings were silently lost.

No restart of either service is required for a rename to take effect. The entities are change
tracked and flushed by the batched upserts, so an in-memory update reaches every connected
client. A direct database edit is invisible until restart, which is why the manual repair
needed one and this does not.

Recovery path for an already broken account: the streamer starts the game. Session start
resolves the new name from Twitch, `UserNameSync` writes it everywhere, the settings sent to
the bot carry it, `UserProvider` now picks it up, and `sessionManager.Update` leaves the old
channel and joins the new one.

A rename made while already live is picked up within about 5 minutes by the watcher, or within
about 15 seconds of the streamer logging in on the website.

Worth verifying on dev before this goes out:

- `TwitchClientId` and `TwitchClientSecret` must be valid for a client credentials grant. That
  is a different grant to the user login flow they are otherwise used for.
- The `usersettings` route on the bot has never been exercised in production. `pubsub` and
  `userrole` use the same transport, so the mechanism is proven, but this specific handler is
  not.

## Original diagnosis

## Symptom

A streamer renames themselves on Twitch. Afterwards the bot no longer talks in their
channel. Logging in on the website does not reliably fix it. Editing the username directly
in the database does.

## The chain that decides which Twitch channel the bot joins

1. Game client, `Assets/Scripts/Shinobytes/Network/GameClient.cs:299`

   ```csharp
   Write(GameMessageResponse.CreateArgs("session", sessionId, userId, created, userSettings));
   ```

   Note what is not in there: a username. The client sends only ids plus the settings
   dictionary it received from RavenNest.

2. RavenNest builds that dictionary in `GameData.GetUserSettings`
   (`src/RavenNest.BusinessLogic/Data/GameData.cs:1447-1470`):

   ```csharp
   settings[a.Platform.ToLower() + "_name"] = a.PlatformUsername;  // UserAccess
   settings["ravenfall_name"] = user.UserName;                     // User
   ```

   So two independent copies of the name travel to the bot.

3. Bot, `src/ROBot.Core/GameServer/RavenfallConnection.cs:197-225`:

   ```csharp
   settingsManager.Set(userId, userSettings);      // fresh values land here
   var player = playerProvider.Get(userId);        // and are then ignored, see below
   this.sessionInfo = new RemoteGameSessionInfo { ..., Owner = player, ... };
   ```

4. `BotServer.Connection_OnSessionInfoReceived` -> `sessionManager.Update(...)` ->
   `session.Name = newOwner.Username` -> `StreamBotApp.OnSessionUpdated` ->
   `LeaveChannelAsync(oldName)` / `JoinChannelAsync(session.Name)`.

Every channel join in the bot resolves to `session.Name`, which resolves to
`UserProvider.Get(userId).Username`. That single value is the whole bug surface.

## Root cause: the bot caches the name forever

`src/RavenBot.Core.Ravenfall/UserProvider.cs:36-62`

```csharp
public User Get(Guid userId)
{
    lock (mutex)
    {
        var player = users.FirstOrDefault(x => x.Id == userId);
        if (player == null)
        {
            player = new User();
            player.Id = userId;
            var settings = settingsManager.Get(userId);
            if (settings.HasValues)
            {
                player.DisplayName = player.Username = settings.RavenfallUserName;
                ...
            }
            users.Add(player);
        }
        return player;   // cache hit: Username is never refreshed
    }
}
```

The name is assigned only on the miss path. On a hit the object is handed back untouched,
even though `settingsManager` was updated with the correct current name one line earlier at
the call site.

`UserProvider` is registered with `ioc.RegisterShared<IUserProvider, UserProvider>()`
(`src/ROBot/Program.cs:75`), so `users` is a process wide list in a bot that stays up for
weeks and serves every streamer. Once a streamer's entry exists with the old name, every
later session registration reuses it and the bot keeps joining the old channel. Restarting
the process is the only thing that clears it, which is why a manual database edit appeared
to be the fix: the edit was correct, but it was the restart that made it take effect.

This also explains why the server side fixes did not help. RavenNest was already sending the
correct name in `ravenfall_name`; the bot threw it away.

## Secondary defects found on the way

### RavenBot

- `UserProvider.cs:105-118`, `Get(string userId, string username, string platform)` matches
  an existing user and then updates `Platform`, `PlatformId` and `DisplayName`, but not
  `Username`. This is the "their Twitch user id is still the same, so match on that" path,
  and it cannot heal the name even when it matches correctly.

- `Get(Guid)` stamps the broadcaster with `Platform = "ravenfall"` and
  `PlatformId = userId.ToString()`. So `GetByUserId(twitchId, "twitch")` cannot find the
  broadcaster, and in `Get(ICommandSender sender, ...)` the predicate

  ```csharp
  x.Username == sender.Username || x.PlatformId == sender.UserId && x.Identifier == identifier
  ```

  (which parses as `A || (B && C)`) fails on both sides for a renamed streamer: the username
  no longer matches, and the platform id is a Ravenfall guid rather than a Twitch id. A
  second duplicate `User` is created silently, while `GetBroadcaster()` keeps returning the
  stale one.

### RavenNest

Four code paths write the username, and no two of them write the same set of fields:

| Path | `User.UserName` | `User.DisplayName` | `UserAccess.PlatformUsername` | `Character.Name` |
| --- | --- | --- | --- | --- |
| `AuthService.TwitchLoginAsync` (:194, :202) | yes, keeps platform postfix | no | yes | no |
| `SessionInfoProvider.StoreAsync` (:210, :216) | yes, no postfix handling | yes | no | no |
| `PlayerManager` join path (:280, :335) | yes | yes | no | yes |
| `SessionManager.BeginSessionAsync` | no | no | no | no |

Consequences:

- `UserAccess.PlatformUsername` only ever refreshes through `AuthService.TwitchLoginAsync`.
  Any other route leaves it stale, and it is what gets published as `settings["twitch_name"]`.
  Which login route the streamer happened to take decides which fields healed, which is the
  "not fully set everywhere on their accounts" behaviour.
- Starting a game session does not refresh the streamer's name at all. `BeginSessionAsync`
  only does `gameData.GetUser(token.UserId)`.
- The two web paths disagree about the `platformPostfix` convention, so they can write
  different values into `User.UserName` for the same account.

Also:

- `GameData.GetUserByUsername` (`GameData.cs:3827`) matches only `User.UserName`, unlike
  `FindUser` (:3363) which also checks `UserAccess.PlatformId` and `PlatformUsername`.
  `ROBotController` uses the narrow one on all three Discord endpoints (:117, :155, :192),
  so those break for a renamed user until `User.UserName` catches up.
- `PlayerManager.cs:3290-3323` is the earlier attempt at an automatic fix. It is commented
  out and now only logs `"User name mismatch..."`, so a rename shows up in the logs and is
  then ignored.

## Note on the data layer

The RavenNest data layer is in-memory with key lookups and batched upserts, so the fix is
just entity mutation and needs no explicit save and no restart.

Two user lookups are the exception and still scan rather than using a key:
`GetUser(platformId, platform)` walks `userAccess.Entities`, and `GetUserByUsername` and
`FindUser` walk `users.Entities`. `GetUser(platformId, platform)` is on the player join path,
so it is the one worth turning into a keyed lookup on `platform + platformId`. The
`GetUserByUsername` fallback added here makes a miss scan twice, which is acceptable only
because its three callers are Discord endpoints rather than anything hot. Neither is part of
this bug; noting them so they are not forgotten.

## Fix as implemented, smallest first

1. **RavenBot, `UserProvider.Get(Guid)`**: refresh `Username` and `DisplayName` from
   `settingsManager` on the cache hit path, not just on create. This alone should stop the
   bot joining the wrong channel, because `sessionManager.Update` already handles the
   leave/join correctly once it is handed the right name. Lowest risk and it does not touch
   player data.

2. **RavenBot, `Get(string userId, string username, string platform)`**: also assign
   `Username` when a match is found by platform id.

3. **RavenNest**: make one place responsible for applying a username change, writing
   `User.UserName`, `User.DisplayName`, `UserAccess.PlatformUsername` and `Character.Name`
   together, and call it from all three refresh paths. Settle the `platformPostfix`
   convention there rather than in two separate call sites.

4. **RavenNest**: point `ROBotController` at `FindUser` instead of `GetUserByUsername`, or
   widen `GetUserByUsername` to fall back to `UserAccess.PlatformUsername`.

5. Re-enable the `PlayerManager` block, or delete it, once step 3 gives it a single function
   to call.

Steps 1 and 2 are the ones that fix the reported symptom. Steps 3 to 5 are what stop the
copies drifting apart again.
