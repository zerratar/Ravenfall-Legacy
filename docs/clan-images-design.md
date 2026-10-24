# Clan logo and cape images

Status: phase 1 (server) built 2026-10-24, RavenNest 008cf384: the `ClanImage` table (created at
startup), `Game/ClanImages/ClanImageManager.cs` (SkiaSharp re-encode), the endpoints in
`ClanController`, the `ClanImages:RequireReview` setting, event 48 `ClanLookChanged` and
`/admin/clan-images`. The re-encode and its checks are tested; the upload and admin page still need a
run with a signed-in browser. Decided: shape per clan with an optional per-role override, SkiaSharp,
three.js copied into the site, images kept but locked while the owner is not a patron.
Phase 2 started: re-UV'd cape meshes in `Assets/PolygonFantasyHeroCharacters/Models (Keep)/ClanCapes`
(gitignored with the rest of the Synty folder), not yet on `Player.prefab`.

Clan owners (clans are a patron feature) upload their own clan logo and, separately, their own cape
image on the website. A proper editor lets them crop, zoom, rotate and place the image, and the cape
editor shows a live 3D preview of the real cape mesh from the game. They also pick which of the cape
shapes their clan wears. Uploads go live straight away by default; an admin page lists every upload,
can block one, and has a global switch that makes every upload wait for review.

## How it works today

- **The logo** is the owner's Twitch profile picture. `LogoService` downloads it into the
  `user_images` file cache (`GeneratedData/.../user_images/clan_logo_<twitchId>.png`) and serves it at
  `api/twitch/clan-logo/{twitchId}` and `api/clan/logo/{twitchId}`. `Clan.Logo` can hold a URL of a
  custom image, which `LogoService.GetClanLogoAsync` already prefers, but nothing on the site sets it.
- **The cape** shows the same logo. The game's `PlayerLogoManager` downloads the clan logo and
  `SyntyPlayerAppearance.SetCapeLogo` puts it on the cape material.
- **The cape shader** (`Assets/Shaders/Cape.shadergraph`) is as simple as it gets: the base colour is
  the texture sampled at the mesh's UVs. Whatever the image is, it is spread over the cape by the UVs.
- **The cape meshes:** `Player.prefab` has five, chosen by `SyntyAppearance.Cape`. On the server
  `ClanRole.Cape` holds a cape index per role, copied to the member's appearance, but every role is
  created with 0 and nothing changes it.

| Index | Shape | UVs |
|---|---|---|
| 0 | Long, straight hem (`ClanCape0.dae`) | Re-made for logos: the cape covers u 0.24-0.75, v 0.02-0.99 |
| 1 | Pointed tip | Synty atlas: u 0.30-0.43, v 0.32-0.37 |
| 2 | Short | Synty atlas, same patch |
| 3 | Long, the same shape as 0 | Synty atlas, same patch |
| 4 | Tattered, torn hem | Synty atlas, same patch |

![The five cape meshes from behind and from the side](mockups/cape-shapes.png)

So there are four different shapes (0 and 3 are the same). **Capes 1-4 cannot show an image yet:**
their UVs cover a patch about 13 % by 5 % of the texture, so a logo on them would be a stretched sliver
of its middle. Cape 0 was given its own UVs for exactly this reason.

## The cape shapes (the clan picks one)

The game client gets new versions of capes 1, 2 and 4 with UVs like cape 0's: the back of the cape is
projected flat onto the texture, so the image reads correctly from behind. All four shapes use the same
image layout, so one cape image fits every shape; a shape only decides where the image is cut (the
pointed cape loses the bottom corners, the short one the bottom third, the tattered one gets holes).
The skinning stays the same, so the capes move as they do now. Cape 3 is dropped from the choice, as it
is cape 0.

The cape image is **512 x 1024 (1:2)**, the whole back of the long cape. Cape 0 is re-made to that
layout too. The front of the cape (the side against the body) shows the same image, mirrored, as now.

The shape is stored as a new `Clan.CapeStyle` (0-3, meaning long, pointed, short, tattered) and an
optional per-role override (`ClanRole.CapeStyle`, empty means the clan's). The old `ClanRole.Cape` and
`SyntyAppearance.Cape` stay as they are (always 0), so **older clients keep showing cape 0** instead of
an atlas cape with a sliver of the image. Only new clients read the style.

## The website

On the clan page, owners get a new **Look** section with two cards: Clan logo and Cape. Each shows the
current image (and its review state when one is waiting) and opens the editor.

**The editor** (a dialog, our own canvas code, no cropping library needed):

- Pick or drop a file: PNG, JPEG, WebP or GIF (first frame), up to 10 MB and 4096 px.
- The frame is fixed: a square for the logo, 1:2 for the cape. The image moves under it: drag to
  move, wheel or a slider to zoom, rotate by a slider (any angle) or 90 degree buttons, flip.
- Fit and Fill buttons, and a background colour for what the image does not cover (transparent for
  the logo, a cloth colour for the cape, as the cape has no transparency).
- For the logo, a round preview next to the square, as Twitch and the site show it round in places.
- **The cape preview:** a three.js view of the chosen cape shape on a simple figure, turning slowly,
  that can be rotated with the mouse. It uses the same image as the editor canvas, updated as you edit,
  so what you see is what the game will show. A shape picker under it switches between the four capes.
  The meshes come from the game as one small `.glb` (about 30 KB) made by an editor script, with the
  new UVs.
- Apply uploads the result. The browser sends the edited image at its final size (512 x 512 or
  512 x 1024 PNG), so the server never has to crop or rotate.
- "Use Twitch picture" puts the logo back to the profile picture; for the cape, "Use the logo" goes
  back to the logo on a cloth colour (the default).

**When review is off** the reply is "Your clan logo is updated." **When review is on** (or for this
owner): "Your image has been sent for review. It can take up to 24 hours. Your current one stays until
then." The card shows "Waiting for review" with the new image until it is decided.

## The server

**Storage.** A new table `ClanImage` with a row per upload:

| Column | Meaning |
|---|---|
| Id, ClanId, UserId | What clan, who uploaded |
| Kind | Logo or Cape |
| State | Live, Waiting, Replaced, Rejected, Blocked, Removed (owner went back to the default) |
| Created, ReviewedAt, ReviewedBy | When, and which admin decided |
| Hash, Width, Height, Size | For the admin page and to skip identical re-uploads |

The image itself is a file, `GeneratedData/clan-images/<id>.png`, like the logo cache. Blocking deletes
the file but keeps the row, so the history still shows that something was blocked. A new
`User.ClanImagesNeedReview` flag (set by Block) and a server setting `ClanImages:RequireReview` (a
toggle in Admin, Server settings, off by default) decide whether a new upload is Live or Waiting.

**Checks on upload:** the clan owner only, the clan exists, the size and dimensions are exactly what
the editor sends, and the file really is a PNG that decodes. The server re-encodes the image, which
strips any metadata and anything hidden after the image data. This needs an image library on the
server: SixLabors.ImageSharp (free for open source and companies under 1M USD revenue) or SkiaSharp
(MIT, needs native libraries where the server runs). One upload a minute per clan.

**Serving:**

- `api/clan/logo/{twitchId}` and `api/twitch/clan-logo/{twitchId}` serve the live custom logo when
  there is one, otherwise the Twitch picture as now. **Old clients get custom logos with no update.**
- A new `api/clan/cape/{clanId}` serves the live cape image, or 404 when the clan has none.
- `Clan` (the model the game and site get) gains `LogoVersion`, `CapeVersion` and `CapeStyle`. The
  versions go on the image addresses (`?v=...`) so the game's one-hour logo cache picks up a change.
- A game event `ClanLookChanged` (clan id, versions, style) tells running games to reload the images
  and capes of that clan's members right away.

## The admin page

`/admin/clan-images`, one row per uploader (clan owner), newest activity first, with waiting ones on
top:

| User | Clan | Logo | Cape | State | Last upload | Needs review | |
|---|---|---|---|---|---|---|---|
| name | clan name | thumbnail | thumbnail | Live / Waiting | time | yes / no | Accept, Reject, Block |

- Expanding a row shows that user's history: every upload with thumbnail, kind, state, time and who
  reviewed it, 10 per page.
- **Accept** (waiting uploads): it goes live.
- **Reject** (waiting uploads): it is not used; the owner can try again. No flag.
- **Block** (any live or waiting upload): deletes the image, puts that kind back to the default
  (Twitch picture, or logo cape), and sets the user's Needs review flag, so all their future uploads
  wait for review whatever the global switch says. The owner sees "Your image was removed." with no
  details.
- **Needs review** can be switched off again from the row (a mistake, or forgiven).
- Admin home shows the number of uploads waiting, and the global switch is shown at the top of the
  page as well as in Server settings.

## The game client

- New UVs for the cape meshes (an editor script makes `ClanCape1/2/4` mesh assets from the Synty ones,
  keeping bone weights and bind poses) and cape 0 moved to the 1:2 layout.
- `SyntyPlayerAppearance`: a clan member wears the mesh for the clan's (or role's) `CapeStyle`.
- `PlayerLogoManager`: the cape gets the clan's cape image when there is one. Otherwise the game makes
  the default cape itself: the logo centred on a cloth colour at 512 x 1024, as the logo alone would be
  stretched by the new UVs.
- A handler for `ClanLookChanged`.
- Streamers who want none of this keep the existing logo censor setting, which already replaces all
  logos.

## Phases

1. **Server:** the table, storage, upload and serving, the setting and flag, the version fields and
   event, the admin page.
2. **Game meshes:** the new UVs, the `.glb` export for the website, cape style in the client, the
   default cape and the event. A client release.
3. **Website editor:** the Look section, the editor, the 3D preview, the shape picker.

The logo part works for everyone after phases 1 and 3 (old clients included). Cape images and shapes
need the phase 2 client.

## Open questions

- **Image library on the server:** ImageSharp or SkiaSharp (see above). Both are a new package.
- **three.js on the site:** it would be copied into `wwwroot/js` (one file, about 700 KB, MIT), like
  the other scripts there, rather than loaded from a CDN.
- **Patreon lapsing:** when the owner stops being a patron, does the clan keep its images? Proposed:
  yes, but they cannot change them until they are a patron again.
- **Per-role cape shapes:** worth having (officers with the tattered cape, recruits with the short
  one), or one shape for the whole clan to start with?
