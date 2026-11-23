# python tools/dark-fortress-armor/compose_sets.py <shots dir> <out dir>
# Labeled sheets from the play-mode captures of tools/dark-fortress-armor/armor-play.cs (Pillow).
import sys, os
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 26)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 21)
BG, FG, MUTED = (34, 37, 43), (235, 235, 235), (175, 181, 191)
CELL = (300, 400)  # each capture is 900 x 1200, shown at a third


def sheet(name, title, rows):
    """rows: [(row label, [(file, cell label), ...])]"""
    cols = max(len(r[1]) for r in rows)
    lw, th, lh = 150, 56, 50
    W = lw + cols * CELL[0]
    H = th + len(rows) * (CELL[1] + lh)
    o = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(o)
    d.text((16, 14), title, font=FONT, fill=FG)
    y = th
    for label, cells in rows:
        for j, ln in enumerate(label.split("\n")):
            d.text((16, y + lh + CELL[1] // 2 - 14 + j * 24), ln, font=SMALL, fill=FG)
        for i, (f, cl) in enumerate(cells):
            x = lw + i * CELL[0]
            lines = cl.split("\n")
            for j, ln in enumerate(lines):
                tw = d.textlength(ln, font=SMALL)
                d.text((x + (CELL[0] - tw) / 2, y + (14 if len(lines) == 1 else 2) + j * 23), ln, font=SMALL, fill=MUTED)
            p = os.path.join(src, f + ".png")
            if os.path.exists(p):
                im = Image.open(p).convert("RGB").resize(CELL, Image.LANCZOS)
                o.paste(im, (x, y + lh))
        y += CELL[1] + lh
    path = os.path.join(out, "dark-fortress-armor-sets-" + name + ".png")
    o.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(path, optimize=True)
    print("WROTE", path, o.size, os.path.getsize(path) // 1024, "KB")


NAMES = {"dk": "Dark Knight", "barb": "Barbarian", "mage": "Mage"}
TIER = {"dk": {"7": "Voidknight", "8": "Abyssal"}, "barb": {"7": "Stormborn", "8": "Worldbreaker"},
        "mage": {"7": "Voidweaver", "8": "Starcaller"}}
sheet("worn", "The Dark Fortress sets in play (the real equip code, medium skin, idle)",
      [(NAMES[k], [("set-%s-%s-%s" % (k, t, s), "%s %s" % (TIER[k][t], s)) for t in ("8", "7") for s in ("male", "female")])
       for k in ("dk", "barb", "mage")])
sheet("animations", "The game's own animations: run, sword attack, woodcutting, fishing (game camera height)",
      [("%s\n%s" % (NAMES[k], s), [("anim-%s-%s" % (k, a), a) for a in ("run", "attack", "chop", "fish")])
       for k, s in (("dk", "male"), ("barb", "female"), ("mage", "male"))])
sheet("skin", "Bare skin in the player's own skin color: light, medium, dark",
      [(t.capitalize(), [("skin-%s-%s-%s" % (t, k, s), "%s %s" % (NAMES[k], s)) for k in ("barb", "mage") for s in ("male", "female")])
       for t in ("light", "medium", "dark")])
MIX = [("dkchest-runelegs", "Abyssal chest,\nRune rest"), ("runechest-dkrest", "Rune chest,\nAbyssal rest"),
       ("barbgloves-mageboots", "Rune, Worldbreaker\ngloves, Starcaller boots"),
       ("magerobe-barbkilt", "Starcaller robe,\nWorldbreaker kilt"), ("magelegs-bare", "Starcaller skirt\nand boots only"),
       ("dkchest-bare", "Abyssal chest\nand gloves only")]
sheet("mixed", "Worn with the game's own gear",
      [(s.capitalize(), [("mix-%s-%s" % (n, s), l) for n, l in MIX]) for s in ("male", "female")])
sheet("back-side", "From behind (male) and the side (female), tier 8",
      [("Back", [("back-%s" % k, NAMES[k]) for k in ("dk", "barb", "mage")]),
       ("Side", [("side-%s" % k, NAMES[k]) for k in ("dk", "barb", "mage")])])

# the game's clips in edit mode through the real equip code (tools/items/class-gear-preview.cs with clip=): every
# clip is a frame of the animation itself, which play mode cannot hold still on demand
if len(sys.argv) > 3:
    pre = sys.argv[3]
    cols = ["Dark Knight\nmale", "Barbarian\nmale", "Mage\nmale", "Dark Knight\nfemale", "Barbarian\nfemale", "Mage\nfemale"]
    names = [("idle", "Idle"), ("run", "Run"), ("attack", "Sword\nattack"), ("combat", "Combat\nstance"), ("chop", "Woodcutting"), ("fish", "Fishing")]
    ims = [Image.open(pre + "-%s.png" % n).convert("RGB") for n, _ in names]
    ims = [i.resize((int(i.width * 0.55), int(i.height * 0.55)), Image.LANCZOS) for i in ims]
    w = max(i.width for i in ims); lw = 170; head = 100
    o = Image.new("RGB", (lw + w, head + sum(i.height for i in ims)), BG)
    d = ImageDraw.Draw(o)
    d.text((16, 14), "Tier 8 sets in the game's own animations (frames of the player's clips, real equip code)", font=FONT, fill=FG)
    cw = w / len(cols)
    for i, c in enumerate(cols):
        for j, ln in enumerate(c.split("\n")):
            tw = d.textlength(ln, font=SMALL)
            d.text((lw + cw * i + (cw - tw) / 2, 48 + j * 23), ln, font=SMALL, fill=MUTED)
    y = head
    for (n, label), im in zip(names, ims):
        o.paste(im, (lw, y))
        for j, ln in enumerate(label.split("\n")):
            d.text((16, y + im.height // 2 - 12 + j * 24), ln, font=SMALL, fill=FG)
        y += im.height
    path = os.path.join(out, "dark-fortress-armor-sets-animations.png")
    o.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(path, optimize=True)
    print("WROTE", path, o.size, os.path.getsize(path) // 1024, "KB")
