# Network v2: a reliable path between game, server and bot

Status: proposal for review. Nothing here is built yet, except the server fixes listed under "Done so far".

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
   For v2 the response also carries a **signed** session token (HMAC by the server), replacing today's unsigned
   Base64 JSON, which anyone could forge.
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
- **Leaving players and quitting**: a player who leaves is saved with a final version, and the client waits (briefly,
  with a time limit) for `SaveStored` before forgetting them. Closing the game waits for outstanding saves for a few
  seconds and shows that it is saving.
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

The link between the bot and the game client gets the same treatment, in both bots.

- **Framing**: length-prefixed frames instead of newline-delimited JSON, so a malformed line no longer kills the link.
- **Heartbeats that act**: ping every 5 seconds; no reply for 15 seconds closes the link and reconnects. Today the
  check exists but is commented out on both sides.
- **One writer per connection**: a single send queue per link, so the ping and chat replies no longer write to the
  same stream at once.
- **Command ids**: every chat command gets an id and the game confirms receipt. Unconfirmed commands are resent after
  a reconnect, and repeats are ignored by id. The id travels on to the server as the command's request id, so the
  whole chain from chat to database is idempotent.
- **Replies to the live connection**: replies are sent by session and request id, not through the connection object
  that received the command, so they survive a reconnect.
- **Throughput**: the game handles commands with a frame budget instead of one per frame.
- **Smaller fixes found on the way**: lock ROBot's connection list, split long replies, and do not drop a queued
  command when its connection is replaced.

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

## Open questions

1. Signed tokens: rotate the existing token format for everyone, or only for v2? v2-only is safer for old clients.
2. Should events be stored in the database as well, so a server restart can replay them? Today they live in memory
   only. Bounded storage in memory is simpler; a restart would then require a full resync from the client.
3. How long may closing the game wait for outstanding saves? A few seconds with a visible "saving" notice is the
   proposal.
4. Is there a hosting limit on WebSockets (proxy, load balancer) on the production server to check before relying
   on them?
