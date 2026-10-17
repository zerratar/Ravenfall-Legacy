# Network v2: a reliable path between game, server and bot

Status: agreed 2026-10-17 (see "Decisions"). Nothing here is built yet, except the server fixes listed under
"Done so far".

## Why

Streamers with 500 to 1000+ players lose saves, see events go missing and get "item doesn't exist" after crafting.
An investigation of the client, the server and the bots (October 2026) found that this is not one bug. Every link
in the chain is fire-and-forget:

- Saves go over a one-way TCP socket (3921) with no acknowledgement. The client clears its "changed" flags before
  sending, so any failed send is lost for good. A batching bug can stop most players being saved until a restart.
- Server events (loot, item adds, gifts) have no sequence numbers, acknowledgements or replay. They were taken off
  the queue before sending, wiped on errors, and parked unread while the client was disconnected.
- When a client reconnected, the server could end the only game loop of that stream for the rest of the session.
- The save flush cleared changes after writing them, dropping whatever changed during the write; one bad row
  blocked its whole table.
- `!craft` could take longer than the client's 25 second timeout: the server crafted, the client never heard,
  crashed on the empty result after removing the ingredients locally, and never added the items.
- The bot-to-game link has no acknowledgements and its heartbeat check is turned off on both sides; replies after a
  reconnect go to the dead connection.

## Done so far (server only, helps today's clients after a restart)

On the RavenNest branch `sync-fixes`, not deployed:

1. A reconnecting client no longer ends its stream's game loop (connections are counted per streamer).
2. Game events wait in one queue per streamer and leave it only when a send is accepted; delivered to the newest
   connection only, kept while the client is away (up to 10,000 events or 10 minutes).
3. The save flush keeps changes made while it writes (versioned change tracking), and a failing row is retried on
   its own with a back-off instead of blocking its table.
4. One bad character or frame no longer drops a whole delta save batch and its connection.
5. One inventory per character (the provider race), and crafting never takes ingredients for nothing.

Load tested 2026-10-17 against a local server on the dev database (`RavenNest.LoadTest`, Release builds):

| Run | Before (`f5afc9b4`) | `sync-fixes` |
|---|---|---|
| 1000 players, 5 min, event socket reconnect every minute | 391 events; none at all for half of each minute after a reconnect | 1448 events, steady through every reconnect |
| 4 streams x 500, new HTTP connection per request, database check | no saves lost | no saves lost (also with 20 reconnects) |

Pure load on a clean local network loses no saves on either; the losses need dropped connections and the old
client's own send path, which only v2 fixes. Adding an item is the slowest command (p50 330 ms at 40 commands a
second), worth a look.

These cannot make the old client reliable (it still forgets what it sent), but they remove the worst server-side
losses. The rest of this document is the real fix.

## Goals

- Nothing a player earns or receives is lost because a packet, request or connection was.
- 1000 players per stream comfortably; tested to 2000.
- One connection per game session, not HTTP plus two TCP ports plus a request per command.
- Old clients keep working on the old paths until everyone has updated.
- Room to move authority to the server later (see "Later: server authority") without changing the protocol.

Not goals for now: moving any authority to the server, changing game rules, or rewriting the database layer.

## Overview

```
Game client  <==== one WebSocket (wss, 443), MessagePack frames ====>  RavenNest
   |  outbox per player (saves)                                           |  one actor per game session
   |  command table (request id -> pending)                               |  versioned saves, durable acks
   |  event cursor (last applied sequence)                                |  event log per session (replayable)
   |
   +---- bot link (TCP, framed, heartbeats, command ids) ---- RavenBot / ROBot
```

Three kinds of traffic share the connection:

| Kind | Direction | Rule |
|---|---|---|
| Saves | client to server | versioned per player; kept by the client until the server says they are stored |
| Commands | client to server, with a reply | have a request id; safe to repeat; the server's answer is what the client applies |
| Events | server to client | numbered per session; the client confirms what it applied; replayed after a reconnect |

## Transport

- **WebSocket** over TLS on 443 at a new endpoint, `/api/v2/game`. It gets through firewalls, ASP.NET serves it
  natively and asynchronously, and `ClientWebSocket` works on both runtimes the game uses: Mono in the Unity 6.7
  editor and CoreCLR in player builds.
- **MessagePack** with explicit keys (`[Key(n)]`) for every message, so fields can be added without breaking either
  side. Both sides already ship MessagePack. Large snapshots use its LZ4 option.
- **Framing**: one WebSocket binary message per envelope. The envelope carries a message type, a sequence number
  where the kind needs one, and the payload.
- **Heartbeat**: a ping every 5 seconds each way; no traffic for 20 seconds means the connection is dead and is
  closed and reopened. Today's sockets never notice a half-open connection.
- **Shared contracts**: a `RavenNest.Protocol` library holding the message types, built for .NET Standard 2.1 so the
  editor (Mono) can use it; the server and player builds use newer .NET on top.

## Session and handshake

1. The client signs in over REST as today and begins a session (`BeginSession`), which is unchanged for old clients.
   For v2 the response also carries a **signed** session token: a small binary token (session id, user id, expiry)
   with an HMAC-SHA256 signature by the server. It is shorter than today's unsigned Base64 JSON, which anyone could
   forge, and the server checks it without a lookup. Old clients keep the old token; the signed one is v2 only.
2. The client opens the WebSocket and sends `Hello { token, clientVersion, protocolVersion, lastEventSequence,
   resumeId }`.
3. The server checks the version (the existing `IsExpectedVersion` and `GameUpdates` gate), attaches the connection
   to the session's actor and answers `Welcome { serverTime, resumeAccepted, eventSequence }`.
4. **Resume**: if the client reconnects within a few minutes with the same `resumeId`, the server replays every
   event after `lastEventSequence` and keeps the session as it was. Otherwise it answers `ResyncRequired`, and the
   client sends full snapshots of every player (see "Full resync").
5. A newer connection for the same session replaces the older one, which is closed with a reason. There is only ever
   one live connection per session, so nothing is copied to two connections.

## Saves

The client simulates the game and owns progress for now, as today. What changes is that saves become reliable.

- **Per-player version**: each player has a version number that goes up whenever their saved state changes (exp,
  levels, state fields such as island, task, health, rested, auto-join settings).
- **What is sent**: `PlayerSave { characterId, version, changedFields..., expGains[] }`. Each skill sends both the
  new total and the exp gained since the last version the server stored. The server uses the total today; the gain
  is there so a later version can check it (see "Later: server authority").
- **Batching**: every 2 seconds the client sends one `SaveBatch` with every player whose version moved since their
  last stored version. 1000 players with one or two changed skills each is a few tens of kilobytes.
- **Applying**: the server applies a save only if its version is newer than the stored one, so a repeated or
  reordered save is harmless.
- **Two acknowledgements**:
  - `SaveAccepted { characterId, version }` when the session actor has applied it in memory;
  - `SaveStored { characterId, version }` once the database flush has written it.
  The client keeps a player's changes until `SaveStored` for that version or newer. If the server restarts before
  flushing, the client still has the data and sends it again.
- **Leaving players**: a player who leaves is saved with a final version, and the client waits (briefly, with a time
  limit) for `SaveStored` before forgetting them.
- **Quitting**: Menu > Exit and closing the window (including Alt+F4, caught with `Application.wantsToQuit`) send the
  outstanding saves at once and show "Saving players to the server before exiting..." until every `SaveStored` has
  arrived, for at most about 3 seconds, then quit. Most of the time this is well under a second.
- **Outbox journal**: the client also writes unconfirmed saves to a small file on disk (appended as they are sent,
  trimmed as they are stored). If the game is killed, crashes or the 3 seconds run out, the next start sends what is
  in the file. Versions make this safe: anything the server already has is ignored.
- **Full resync**: every 5 minutes, and after a `ResyncRequired`, the client sends full snapshots. The server keeps
  whichever is newer, so this is a safety net that costs little.

## Commands

Commands are what players ask for in chat: craft, equip, gift, send, vendor, buy, use, enchant, and so on.

- **Request id**: every command has a unique id (the bot's command id, see "Bot link", or one the client makes).
- **Safe to repeat**: the session actor keeps each command's result for 15 minutes. A repeated request id returns the
  stored result instead of crafting or gifting twice. A timeout on the client means "ask again with the same id",
  never "it failed".
- **Long commands**: a big craft answers `Accepted` at once, then sends progress and the final result as events, so
  no request has to stay open for 25 seconds.
- **The server's answer is what counts**: a result carries the inventory changes as the server made them
  (`InventoryChanges { characterId, revision, stacks added/changed/removed with their ids and amounts }`). The client
  applies those, not its own arithmetic. Today the client removes ingredients itself, with a bug when they span more
  than one stack.
- **Rejections say why**: every failure has a reason the client can tell the player, instead of a null.

## Events

- **Numbered per session**: each event has a sequence number. The client applies events in order and confirms the
  last one applied (`EventAck { sequence }`, sent with the next save batch or after 1 second).
- **Kept until confirmed**: the session actor keeps events until they are confirmed, bounded (for example 20,000 or
  30 minutes). After a reconnect it replays from the client's last confirmed sequence.
- **Survives a server restart**: each live session also appends its unconfirmed events to its own log file on disk
  (one file per session in its own folder, not the database). Confirmed events are trimmed from it, so a file holds
  only what the client has not applied yet. A session's file is deleted 30 minutes after the session ends, and a
  sweep removes any file older than a day. After a restart, a reconnecting session's events are replayed from its
  file. This avoids the event table that grew without end the last time events were stored in the database.
- **Safe to apply twice**: each event has an id; the client remembers recent ids and skips repeats.
- **Player not in the game yet**: an event for a player who is joining is held until they have joined, not dropped.
- **Inventory revisions**: every inventory change carries the character's inventory revision. A gap (revision 41
  after 39) makes the client ask for that character's inventory, so a lost change heals itself.

## Server

- **One actor per game session**: a single worker owns a session's players, events and command results, fed by a
  queue (`System.Threading.Channels`). Messages for one session are handled in order, without locks, and without the
  per-session 100 ms polling thread used today.
- **Persistence**: the versioned change tracking from `sync-fixes` stays. The flush becomes asynchronous and stops
  holding the kernel lock that every other operation waits on today. After each flush it tells the affected session
  actors which player versions are now stored, so they can send `SaveStored`.
- **Shared state**: v2 sessions and old sessions write into the same `GameData`, so old and new clients run side by
  side, and a player can move between streams of either kind.
- **Limits**: per-connection message size limits, a per-session command rate limit, and a bounded event log, so one
  misbehaving client cannot hurt others.

## Game client

- **Network on its own thread**: an async loop reads and writes the WebSocket; nothing waits on the main thread.
- **Main-thread dispatcher with a budget**: incoming results and events are applied in Unity's update, up to a few
  milliseconds per frame. Today the client either applies one bot command per frame or drains a whole event queue at
  once.
- **Outbox per player**: holds each player's unconfirmed changes, which replaces `DeltaClient`'s dirty flags.
- **Command table**: pending commands by request id, each with its time-out and retry. No `async void` handlers, and
  no swallowed null results.
- **Replaced on v2**: `DeltaClient`, `TcpApi` (the old event socket), and the per-command REST calls. Signing in and
  beginning a session stay on REST.
- **Switchable**: a setting and a server flag can fall back to the old path, in case v2 misbehaves for someone.

## Bot link (RavenBot and ROBot)

Built 2026-10-17 as fixes that keep the wire format (newline-delimited JSON on 4040/4041), so old games and old
bots keep working with new ones and nothing has to be negotiated. Mapping the link showed the losses came from how
lines and connections were handled, not from the framing itself.

Game (`GameClient`, `RavenBotConnection`):

- **Throughput**: bot commands are handled for up to 4 ms (at most 50) a frame instead of one a frame.
- **Repeats ignored**: a command with the same identifier and chat message id (`CorrelationId`, the Twitch or
  Discord message id) within ten minutes runs once, so a bot can resend after a reconnect.
- **Replies to the live connection**: a reply written to a connection that was closed or replaced goes to the live
  one, or waits up to two minutes for the next; unsent replies move over when a connection closes.
- **A bad line is not fatal**: invalid JSON is logged and skipped; end of stream ends the read loop.
- **No busy writer**: the writer waits for messages instead of spinning a core; a broken socket closes the link.
- **Heartbeat**: 30 s without anything from the remote bot (which pings every 3 s) reconnects.
- The session start time is sent in ISO 8601 so the bot parses it in any culture.

Bots (RavenBot repo, branch `bot-link-reliability`):

- ROBot's connection list is locked; each link has one writer (concurrent writes used to drop the link).
- An empty line is skipped, not taken as a disconnect.
- Commands queued while a streamer's link is down belong to the session and go to the next link (kept 2 minutes).
- A reconnect for the same session takes over the session and channel without leaving and rejoining it; a dropped
  link's session is held for 90 s.
- Long replies are split (Twitch 500, Discord 2000 characters) instead of being dropped.
- The heartbeat closes a link after 45 s of silence, once the game has answered a ping on it.
- The per-IP connect limit went from 4 to 20 a minute.

Still open: a command id that travels on to the server as the v2 request id (the chat message id would do), so the
whole chain from chat to database is idempotent, not only the game step.

## Backward compatibility and rollout

- The old REST endpoints and the TCP ports 3920 and 3921 stay, unchanged in behaviour, with the `sync-fixes` repairs.
- v2 lives on its own endpoint. The client chooses it by version; the server can switch v2 off per streamer or
  globally.
- Bot changes are additive: a bot that knows the new link uses it with a game that supports it, and otherwise keeps
  the old link.

Rollout:

1. Deploy `sync-fixes` (after load testing), so every streamer benefits now.
2. Ship the `RavenNest.Protocol` library and the server's v2 endpoint, switched off.
3. Ship a game version that can use v2, switched off by default; turn it on for a few volunteer streamers.
4. Turn it on for everyone; keep the old path for old clients.
5. Bot link v2 in both bots, then turn it on per game version.
6. Retire the old paths once no supported client uses them.

## Testing

- **Load tester** (`RavenNest.LoadTest`, being built): simulates streams of 1000 to 2000 players on the old protocol
  now and on v2 later, reports latencies and failures, and checks afterwards that the server stored exactly the exp
  that was sent. It runs only against a local server with the dev database copy.
- **Chaos tests**: drop the connection mid-save, delay and reorder messages, repeat commands, restart the server
  during a session. After each, every player's stored state must match what the client had.
- **Metrics**: unconfirmed saves per session, event log length, command retries, flush times, and reconnects, on a
  page the streamer and admins can see.

## Later: server authority (not part of this work)

The protocol leaves room for the server to take over decisions later, in small steps, without a new protocol:

- **Changes, not only totals**: saves carry the exp gained per skill alongside the total, so the server can check a
  gain against what the activity could produce, and later compute it.
- **Requests, not results**: anything that creates items or currency (loot, drops) can become a command whose
  outcome the server decides; the server already has drop tables.
- **One actor per session**: the natural place to add checks and server-side rules.

Suggested order when the time comes: validate and log first, then let the server decide rewards (drops, loot,
coins), then progress (exp). The game itself (movement, combat) stays on the client.

## Decisions (2026-10-17)

1. **Signed tokens**: v2 only, as a small binary token with an HMAC signature. Old clients keep the old token until
   every active client has updated.
2. **Events across a restart**: in memory, plus a per-session append-only log on disk outside the database, trimmed
   by acknowledgements and deleted after the session (see "Events").
3. **Closing the game**: waits at most about 3 seconds with a "Saving players to the server before exiting..."
   notice; anything unconfirmed stays in the on-disk outbox and is sent on the next start (see "Saves").
4. **Hosting**: production runs on a machine on the developer's own network with nothing in front that limits
   WebSockets; the only limit is the connection's bandwidth.
