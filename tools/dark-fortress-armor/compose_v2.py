# python tools/dark-fortress-armor/compose_v2.py <render dir> <survey dir> <out dir>
# Labeled sheets from the second pass renders (render_v2.py, survey_v2.py), quantized for the repository.
import sys, os
from PIL import Image, ImageDraw, ImageFont

src, sdir, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 24)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
BG, FG, MUTED = (34, 37, 43), (235, 235, 235), (170, 176, 186)
PRE = "dark-fortress-armor-test-v2-"


def sheet(rows, cols, name, title, scale=0.62, row_w=150, head_h=96):
    ims = []
    for _, p in rows:
        im = Image.open(p).convert("RGB")
        ims.append(im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS))
    w = max(i.width for i in ims)
    o = Image.new("RGB", (row_w + w, head_h + sum(i.height for i in ims)), BG)
    d = ImageDraw.Draw(o)
    d.text((16, 12), title, font=FONT, fill=FG)
    cw = w / len(cols)
    for i, c in enumerate(cols):
        for j, ln in enumerate(c.split("\n")):
            tw = d.textlength(ln, font=SMALL)
            d.text((row_w + cw * i + (cw - tw) / 2, 44 + j * 22), ln, font=SMALL, fill=MUTED)
    y = head_h
    for (label, _), im in zip(rows, ims):
        o.paste(im, (row_w, y))
        for j, ln in enumerate(label.split("\n")):
            d.text((16, y + im.height // 2 - 12 + j * 24), ln, font=SMALL, fill=FG)
        y += im.height
    path = os.path.join(out, PRE + name + ".png")
    o.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(path, optimize=True)
    print("WROTE", path, o.size, os.path.getsize(path) // 1024, "KB")


p = lambda f: os.path.join(src, f)

# survey of every character
sheet([("Front", os.path.join(sdir, "survey-idle-front.png")), ("Side", os.path.join(sdir, "survey-idle-side.png")),
       ("Back", os.path.join(sdir, "survey-idle-back.png")), ("Run", os.path.join(sdir, "survey-run-side.png"))],
      ["Game body", "Knight", "Inquisitor", "Barbarian", "Apothecary", "Necro-\nmancer", "Plague\nLord", "Plague",
       "Undead 01", "Undead 02", "Wraith", "Corpse", "Attach\nArmor 01", "Attach\nFur 01"],
      "survey", "Every Dark Fortress character in the game's own idle and run (retargeted clips)", scale=0.55)

anim_cols = []
for clip in ("Idle", "Run", "Sword hit", "Woodcut"):
    anim_cols += [clip + "\noriginal", clip + "\nmale", clip + "\nfemale"]
mix_cols = ["M: Knight chest\nbare rest", "M: chest+gloves\nplate legs, boots", "M: plate chest\npack rest",
            "M: plate gloves\npack leggings", "M: pack boots\nbare rest"]
mix_cols = mix_cols + [c.replace("M:", "F:") for c in mix_cols]
for ch in ("knight", "barbarian", "necromancer", "inquisitor", "apothecary"):
    nm = ch.capitalize()
    if os.path.exists(p(ch + "-anim-front.png")):
        sheet([("Front", p(ch + "-anim-front.png")), ("Back", p(ch + "-anim-back.png")),
               ("Side", p(ch + "-anim-side.png")), ("Game\ncamera", p(ch + "-anim-game.png"))],
              anim_cols, ch + "-anim", "%s: the game's own animations, original vs cut and fitted (medium skin tone)" % nm,
              scale=0.58)
    if os.path.exists(p(ch + "-mix-idle-front.png")):
        cols = [c.replace("Knight", nm).replace("pack", nm) for c in mix_cols]
        sheet([("Idle\nfront", p(ch + "-mix-idle-front.png")), ("Idle\ngame cam", p(ch + "-mix-idle-game.png")),
               ("Idle\nback", p(ch + "-mix-idle-back.png")), ("Attack\nfront", p(ch + "-mix-attack-front.png")),
               ("Attack\ngame cam", p(ch + "-mix-attack-game.png"))],
              cols, ch + "-mix", "%s pieces worn with the game's own parts" % nm, scale=0.58)

# skin tones: the characters with bare skin, one row each
rows = []
for ch in ("barbarian", "necromancer", "apothecary"):
    if os.path.exists(p(ch + "-skin-front.png")):
        rows.append((ch.capitalize() + "\nfront", p(ch + "-skin-front.png")))
        rows.append((ch.capitalize() + "\ngame cam", p(ch + "-skin-game.png")))
sheet(rows, ["Light\nmale", "Light\nfemale", "Medium\nmale", "Medium\nfemale", "Dark\nmale", "Dark\nfemale"],
      "skin", "Bare skin on the pieces drawn with the player's own body material, three skin tones", scale=0.62)

sheet([("Front", p("knight-pads-front.png")), ("Back", p("knight-pads-back.png")), ("Game\ncamera", p("knight-pads-game.png"))],
      ["Idle M\npauldrons baked", "Idle M\n+ game pads", "Idle M\nplate set", "Idle F\npauldrons baked",
       "Idle F\n+ game pads", "Idle F\nplate set", "Attack M\npauldrons baked", "Attack M\n+ game pads",
       "Attack M\nplate set", "Attack F\npauldrons baked", "Attack F\n+ game pads", "Attack F\nplate set"],
      "knight-pads", "Knight pauldrons baked into the upper arms, next to the game's own shoulder pads", scale=0.62)

cells = os.path.join(src, "cells")
srows = [("neck", "Neck\ngame cam"), ("neck-back", "Neck\nbehind"), ("shoulders", "Shoulders\nattack"),
         ("elbow", "Elbow\nchop"), ("waist", "Waist\nrun"), ("knee", "Knee\nrun")]
strips = []
for key, label in srows:
    ims = [Image.open(os.path.join(cells, "knight-seam-%s-%d.png" % (key, i))).convert("RGB") for i in range(5)]
    s = Image.new("RGB", (sum(i.width for i in ims), ims[0].height))
    x = 0
    for im in ims:
        s.paste(im, (x, 0)); x += im.width
    sp = os.path.join(cells, "knight-strip-%s.png" % key)
    s.save(sp)
    strips.append((label, sp))
sheet(strips, ["Male, full set", "Female, full set", "M: Knight chest\nplate legs, boots",
               "F: plate chest, Knight\ngloves, leggings", "M: bare chest, Knight\ngloves, leggings"],
      "knight-seams", "Knight: close-ups where the items meet, in game animation frames", scale=0.62)
