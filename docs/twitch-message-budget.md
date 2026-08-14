# Twitch message budget — length, rate limits, and who we name

Ravenfall talks to viewers through chat, and chat is a **scarce, shared resource**. Two separate
limits apply, and only one of them is currently handled in code.

## The two limits

**Per-message length.** A chat message over roughly 500 characters is rejected outright. Nobody
sees it. `PlayerItemDropText` caps messages at 475 characters to stay clear of this.

**Rate limit.** A bot may only send a limited number of messages per window. Excess messages are
**silently skipped** — they do not error, they simply never arrive. This is the limit that bites
today, and it gets much worse with large sessions: a raid or dungeon with ~300 auto-joined
players generates announcements naming everyone.

There is currently **no rate limiting or send queue anywhere in the codebase**. Messages go
straight out through `RavenBot.Announce`.

## Why fixing length made rate pressure worse

`PlayerItemDropText` used to build one `"<item> was found by ..."` line naming every receiver and
emit it as a single message, however long it got. With hundreds of receivers that message
exceeded the length limit and was rejected — nobody was told about their drop.

That is now split into as many messages as needed. Strictly better (some announcements arrive
where previously none did), but it **converts one rejected message into several accepted ones**,
spending more of the rate-limit budget precisely when sessions are largest.

Length is solved. Volume is not.

## The intended fix: spend the budget on players who are actually there

A viewer who has issued no commands for an hour is quite possibly not watching. Naming them costs
exactly as much budget as naming an active player, and delivers far less.

So when an announcement would exceed the message budget, **drop the least recently active
receivers first** rather than splitting indefinitely. Activity resets the moment a player issues
any command, putting them straight back into the active pool.

The same principle generalises beyond drop messages: how often a player is observed by the camera,
and how often messages are directed at them, can all decay with inactivity and reset on activity.

### The signal already exists

- `PlayerController.LastChatCommandUtc` — written on every chat command
  (`PlayerController`, `RavenBotConnection`) and restored across sessions in `PlayerManager`.
- `PlayerController.TimeSinceLastChatCommandUtc` — the convenience wrapper.
- Already exposed to streamers by `QueryEngineAPI` as the `CommandIdleTime` column.

The codebase already filters on it in places — `GameManager` uses `LastChatCommandUtc` when
deciding who to tell about auto-join failures, and `PlayerController` has an hours-based idle
check. The proposal generalises an existing pattern rather than introducing a new concept.

### Where it belongs

The selection policy is pure logic — a list of receivers, their idle times, and a budget, in;
the receivers to name, out. That belongs in `Ravenfall.Core` alongside `PlayerItemDropText`, where
it can be unit tested. Note the message formatter should stay a formatter: deciding *who* is worth
naming is a separate concern from rendering the sentence.

Worth deciding deliberately when implementing:

- whether pruned players are silently omitted or summarised ("...and 217 others")
- the inactivity threshold, and whether it is fixed or scales with session size
- whether a player who just received a rare drop should be exempt regardless of idle time
- whether a send queue with real rate limiting should exist underneath all of this, so the policy
  has a real budget to spend rather than an assumed one
