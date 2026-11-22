# How UI mockups are generated in this project (a handover for other projects)

Written 2026-11-22 for an agent on another project who wants to do the same. It covers what a mockup is for here,
the request that makes one, how the prompt is built, the working routine and the mistakes already paid for.
Nothing in it needs this repository; the Unity tooling at the end is only how this project wraps the same call.

## What a mockup is for

A generated picture of a screen that does not exist yet, made **before** building, so the owner can pick a
direction from two or three looks in a minute. It is a picture to go by, not a design to copy: the numbers in it
are invented, some details will be things the product does not have, and the real page is then built in the
project's own design system and judged against the picture ("is the real one at least this good?").

It has paid off for: the skill tree map (two styles, the owner picked one), the pets pages, the character
details window, the website's gear pages (three layouts). Cost is a few pictures per decision.

## The request

One HTTPS call to OpenAI's image API per picture. This project uses the GPT-Image 2.6 models (moved from 2.5 on
2026-11-08; **check the provider's current model list, the names change**):

- `gpt-image-2.6-sunburst`: finer detail, text in the picture comes out readable, stays close to input pictures
  in edits. About 40 to 60 seconds. **Use this one for UI mockups.**
- `gpt-image-2.6-flare`: quicker (about 20 seconds), simpler and crisper. Good for single icons.

New picture from text, `POST https://api.openai.com/v1/images/generations`, JSON body:

```json
{ "model": "gpt-image-2.6-sunburst", "prompt": "...", "size": "1536x1024", "quality": "high",
  "output_format": "png", "n": 1 }
```

The answer holds the PNG as base64 in `data[0].b64_json`.

Picture from pictures (a restyle of a screenshot, a variant of an existing mockup),
`POST https://api.openai.com/v1/images/edits`, multipart form with the same text fields plus one or more files
named `image[]` (and optionally `mask`).

Settings that matter:
- **Size** in multiples of 16. `1536x1024` for a desktop screen, `1024x1536` for a phone screen, `1024x1024`
  for icons and icon sheets.
- **Quality** `high` for mockups: small text is the first thing to break at lower quality.
- **Background:** leave it out for mockups. `"background": "transparent"` is for icons only.
- The key comes from the `OPENAI_API_KEY` environment variable of the person who pays. An agent never types,
  prints, logs or commits it.

A minimal script with nothing but the Python standard library. It follows the request this project's own tool
sends; the tool is C#, so this Python version itself has not been run:

```python
# mockup.py "<prompt>" out.png [size]     needs OPENAI_API_KEY in the environment
import base64, json, os, sys, urllib.request

prompt, out = sys.argv[1], sys.argv[2]
size = sys.argv[3] if len(sys.argv) > 3 else "1536x1024"
if os.path.exists(out):
    sys.exit("refusing to overwrite " + out + " (and to pay twice): pick a new name")
body = json.dumps({"model": "gpt-image-2.6-sunburst", "prompt": prompt, "size": size,
                   "quality": "high", "output_format": "png", "n": 1}).encode()
request = urllib.request.Request("https://api.openai.com/v1/images/generations", data=body, headers={
    "Authorization": "Bearer " + os.environ["OPENAI_API_KEY"], "Content-Type": "application/json"})
with urllib.request.urlopen(request, timeout=360) as response:   # a picture can take a minute
    data = json.load(response)
with open(out, "wb") as f:
    f.write(base64.b64decode(data["data"][0]["b64_json"]))
print("saved", out)
```

## The prompt

A mockup prompt is one long paragraph in four parts. The first part is shared by every picture of a set, so the
set looks like one product.

1. **What it is and where:** "A polished web page UI mockup, desktop browser, 3:2, for the player dashboard of
   (the product, in five words)."
2. **The design system in words,** taken from the real product (look at a screenshot of it first): page and
   panel colors, the accent color and what it is used for, corner radius, the type ("bold white sans-serif text
   that is large and easy to read"), what state colors mean, what the pictures in it look like ("small painted
   pictures of stylized low-poly fantasy items on dark square tiles"). Also the fixed chrome: the sidebar's
   entries and the tab row, by name.
3. **This screen's layout, region by region,** in reading order with positions ("LEFT column, a panel titled
   'Outfits': ...", "CENTER: ...", "RIGHT third: ..."). Name every panel, and for every label write the exact
   words in quotes. State what is selected, what is empty, what is being hovered or dragged: states make a
   mockup useful.
4. **Guard rails at the end of the shared part:** "generous spacing, nothing cramped, no lorem ipsum, no real
   brand logos, no watermark".

What makes the difference:
- **Give it the words.** Every label you do not write, it invents. Keep labels short and few: about 40 short
  texts in one picture stay readable, a wall of small text does not.
- **Give it real content:** real item names, real skill names, plausible numbers. It then reads as the product.
- **Ask for two or three different layouts or styles, not three tries of one.** Change one thing between them
  (the layout, or the visual style) and keep the shared part word for word.
- **Describe, do not instruct.** "The Inventory tab is active" works; "make it look modern" does not.
- **Do not ask for what you would not build.** Whatever is in the prompt, the owner will see and may want. (In
  the gear pages set a "Set bonus" box was asked for although the game has no set bonuses; it then had to be
  explained away.)

A real example, the shared part and one screen of the gear pages set of 2026-11-22:

> A polished web page UI mockup, desktop browser, 3:2, for the player dashboard of the fantasy idle MMO
> Ravenfall. Dark theme: near-black blue-gray page background, charcoal panels with thin lighter borders and
> softly rounded corners, a warm amber-gold accent color for the primary buttons, the active tab and
> highlights, bold white sans-serif text that is large and easy to read, green numbers for stat gains and red
> for losses, item rarity shown by colored borders (gray, green, blue, purple, orange). A narrow dark left
> sidebar with the word RAVENFALL at the top and menu entries Overview, Characters, Item Stash, Boats, Clan,
> Vendor. Above the content a row of tabs: Overview, Skills, Skill Trees, Equipment, Inventory, Pets,
> Customize. Item icons are small painted pictures of stylized low-poly fantasy items (...) on dark square
> tiles. (...) Clean modern game launcher feel, generous spacing, nothing cramped, no lorem ipsum, no real
> brand logos, no watermark.
>
> The Inventory tab is active. TOP: a wide search field with a magnifier icon, then filter chips in a row: All
> (selected, gold), Weapons, Armor, Jewelry, Gems, Food, Potions, Resources, Pets, and on the right a 'Sort:
> Best first' dropdown and grid or list view buttons. MAIN AREA, left two thirds: a tidy grid of square item
> tiles, eight per row, five rows, each with a painted item icon, a colored rarity border, a stack count in the
> bottom right corner for stackable items, a small green up arrow badge on a few items meaning better than
> what is worn (...); one tile is selected with a gold glow. RIGHT third: a detail panel for the selected item,
> 'Nightfall Tunic', with a big item picture, a purple 'EPIC' tag, 'Rogue gear, tier 8', a compare block with
> two columns 'This item' and 'Worn now' (...) and buttons at the bottom: 'Equip' (large, gold), 'Add to
> outfit', 'Send to stash', 'Sell'. BOTTOM: a slim bar 'Bag 212 of 400' with a thin progress bar and 'Select
> several' on the right.

The results are in `docs/mockups/gear-inventory-mockup.png`, `gear-unified-mockup.png` and
`gear-equipment-mockup.png`.

## The routine

1. **Look at the real screen first** (a screenshot), and write the design system paragraph from it.
2. **Decide what the two or three pictures should let the owner choose between.**
3. **Start all of them once.** They run in parallel; each takes about a minute.
4. **Open every result and look at it closely** before showing it: is the text readable, does the layout match
   what was asked, did it add things nobody asked for? Note those.
5. **Show them with a recommendation** and say plainly what in them is invented or not a real feature.
6. **Keep the chosen ones in the repository** (`docs/mockups/`), named for what they show, and name them in the
   design document. The builder of the real page is told to look at them and compare the result.

## Mistakes already paid for

- **Never send a request again because you are unsure it ran.** A request that timed out may still have been
  made and billed. Check whether the output file or the job exists first. Give the start script a guard (the
  example refuses to overwrite; this project writes a "started" marker file). A blind re-run here once doubled
  three pictures.
- **Retry only on 429 and 5xx,** with a growing wait, and not when the error code says the quota or spend limit
  is reached (`insufficient_quota` and similar): that will not pass on a retry.
- **Allow several minutes per request** before calling it failed, and then do not resend (see above).
- **Every picture costs the owner money.** Plan the set, run it once, and fix the prompt only when a result is
  unusable. Many small pictures come cheaper as one sheet (a 4x4 grid on one picture, cut apart by a script).
- **Edits follow the input closely with Sunburst,** best from the full size original rather than a shrunk copy.
  In the multipart form the file field is named `image[]`, quoted.
- **A "transparent" picture can still come back with a painted backdrop.** Check the border pixels and cut it
  out in a second pass if needed. Not a concern for mockups.
- **The picture is not the design.** Sizes, spacing and colors of the real page come from the product's design
  system; the mockup decides layout and mood.

## How this project wraps it (Unity only, for reference)

- `Assets/Scripts/Editor/ImageGen/ImageGen.cs`: `ImageGen.Generate(prompt, outPath, options, then, tag)` and
  `ImageGen.Edit(prompt, inputPngs, outPath, ...)` return at once; `ImageGen.Status()` lists the last jobs.
  Options: `Model` (Flare or Sunburst), `Size`, `Quality`, `Transparent`, `AutoCutout`. The menu Ravenfall >
  Image Generator is the same thing with a window, where the owner enters the key.
- `tools/ImageGenService`: a small local web service (http://127.0.0.1:57421) that actually sends the requests,
  so jobs survive editor reloads. `GET /health`, `POST /jobs`, `GET /jobs?last=N`, `GET /jobs/{id}`. It holds
  the key in memory only and stops after 30 idle minutes.
- A mockup run is one short editor script that builds the shared paragraph, adds each screen's paragraph, and
  calls `ImageGen.Generate` once per picture with `Model = Sunburst`, `Size = "1536x1024"`, `Quality = "high"`,
  `Transparent = false`, `AutoCutout = false`, writing into `docs/mockups/`.
