# Clan pages: a public clan list, clan pages, join requests and the clan's own little website

Asked for on Discord (2026-11-06): a player wanted to compare the enchanting levels of clans before
switching, and the website has no place that lists clans. Zerratar's answer grew it into a clan
list, a page per clan, requests to join, privacy options for the owner, and a page the owner can
edit with news for the clan, "a little bit MySpace".

Decided with Zerratar (2026-11-06):
- **Existing clans start open**, the way they work today, and owners opt in to approval.
- **The public page shows everything by default.** The owner hides what they do not want shown
  once step 3 exists.
- Built in four steps; step 1 first.

---

## What exists today

- 240 clans on the dev copy; 214 with two or more members, 131 with ten or more, the biggest 655.
- **Enchanting is the only clan skill** (227 clans have it, the highest at level 412). `ClanSkill`
  holds level and experience per clan; the website's `ClanSkill` model also carries `MaxLevel`.
- Clan level and experience (`Clan.Level`, `Clan.Experience`), the creation date, the owner
  (`Clan.UserId`), ranks (`ClanRole`, with a level and per rank permissions) and memberships
  (`CharacterClanMembership`, with the join date).
- Owners and officers manage the clan under `/clan` (overview, ranks, stash, look). Nothing about a
  clan is visible to anyone outside it.
- Logos: an uploaded image (`/api/clan/image/{LogoImageId}`), an old URL (`Clan.Logo`) or the
  owner's Twitch picture (`/api/twitch/logo/{ownerUserId}`), in that order, as `/clan` shows them.
- Characters have a public page, `/inspect/{characterId}`.

### Joining is open to everyone, by accident

`!clan join <clan name>` joins any clan at once, without the owner. `Clan.IsPublic` exists, is
never set anywhere, and `ClanManager.JoinClan` refuses clans where it is **true**, the opposite of
its name; since every clan has it false, every clan takes anyone. The rank permission
`CanMakePublic` exists and guards nothing. Step 2 replaces both with a join policy.

---

## Step 1: the clan list and the clan pages (read only)

**`/clans`**, linked from the Game menu and the footer next to Towns:
- One row per clan with members: logo, name, owner, clan level, **Enchanting level**, members, how
  many are playing right now, founded.
- Search by clan or owner name. Sort by Enchanting (the default, since that is what the request
  was), clan level, members, playing now, newest. 50 rows at a time with Show more, as the other
  lists (one render of all 240 rows is the kind that dropped connections on the vendor page).
- Clans without members (a founder who left their own clan) are left out.

**`/clans/{name}`** (the name, matched without case; a clan id works too, so links survive a rename
through the id form):
- Header: logo, name, owner (their stream link when the owner is live), clan level with its
  progress bar, founded, members, playing now.
- Clan skills with level, progress and max, as on `/clan`.
- Ranks, highest first, each with its members.
- The member list: character (to `/inspect`), rank, joined, playing now and where. Searchable,
  paged at 50.
- A "Manage your clan" link when the visitor is in the clan and may manage it.

Data comes from a new `PublicClanService`, readable without signing in, building small view models
(no `Player` objects, which carry inventories). The list is cached for 60 seconds; the live counts
read the active sessions once per build, as `ClanService.GetMembers` does.

## Step 2: join policy and join requests

- `Clan.JoinPolicy`: **Open** (default, today's behaviour), **On request**, **Invite only**. Set by
  the owner and ranks with `CanMakePublic` (renamed in the editor to "Can change who may join").
  `IsPublic` and the inverted check go.
- `ClanJoinRequest` (clan, character, message up to 200 characters, created, state, decided by,
  decided at). One open request per character; a character in a clan cannot request.
- The clan page gets **Request to join** (or **Join** for an open clan) for a signed-in visitor:
  pick which of your characters, add a message.
- `/clan` gets a Requests tab for anyone with `CanCreateInvite`: accept puts the character in the
  lowest rank, decline closes it; both notify the player (`UserNotification`, as invites do).
- `!clan join <name>` follows the policy: an open clan joins as now, an on request clan files a
  request ("Your request to join X was sent; its officers decide on the website"), an invite only
  clan says so. This needs a reply text in the bot (and the game if it formats the result).

## Step 3: privacy

`ClanPageSettings` per clan, flags the owner sets under `/clan` (Page tab): show the member list,
show who is playing now, show join dates, show rank holders, show the clan skills' progress. All on
by default (decided above). Always shown, because the list is for comparing clans: name, logo,
owner, level, Enchanting level, member count.

## Step 4: the clan's own little website

- **About**: a short text (plain text with line breaks and links, no HTML), shown at the top.
- **News posts**: title and text, public or members only, a pinned post; written by the owner and
  ranks with a new `CanPostNews` permission. Newest first, 10 per page.
- **Looks**: an accent colour, and a banner image through the existing clan image upload and
  admin review (`ClanImage` with a new kind).
- **Top 8**: the owner picks eight members to feature, as MySpace's Top 8 friends.
- **A visitor counter** at the bottom of the page, the old fashioned kind ("You are visitor
  #000042"), counted once per visitor per day.
- Moderation: a Report button on posts and the about text, a reports list for admins, and admins
  can hide a post or the about text (the clan sees that it was hidden). Same shape as clan image
  review.

Tables: `ClanPageSettings` (the step 3 flags, about text, accent colour, banner image, Top 8 as a
list of character ids, visitor count), `ClanPost`, `ClanPostReport`. `DatabaseSchema` creates
them at startup.

---

## Later, from clan-features-design.md

A clan highscore (by level, by Enchanting, by members playing this week) is the same data as the
list sorted; the activity feed (joins, leaves, promotions, level ups) would fill the clan page
between news posts.
