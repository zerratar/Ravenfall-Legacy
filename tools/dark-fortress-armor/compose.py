# python tools/dark-fortress-armor/compose.py <render dir> <out dir> [tag]
# Puts the Blender renders of render_test.py together into labeled sheets (system Python with Pillow).
import sys, os
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
tag = sys.argv[3] if len(sys.argv) > 3 else "knight"
os.makedirs(out, exist_ok=True)
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 24)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
BG = (34, 37, 43)
FG = (235, 235, 235)
MUTED = (170, 176, 186)


def sheet(rows, cols, path, title, row_w=170, head_h=96, scale=1.0):
    """rows: [(label, image path)], each image holds len(cols) equal cells side by side."""
    ims = [Image.open(p).convert("RGB") for _, p in rows]
    if scale != 1.0:
        ims = [im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS) for im in ims]
    w = max(im.width for im in ims)
    h = sum(im.height for im in ims)
    o = Image.new("RGB", (row_w + w, head_h + h), BG)
    d = ImageDraw.Draw(o)
    d.text((16, 12), title, font=FONT, fill=FG)
    cw = w / len(cols)
    for i, c in enumerate(cols):
        lines = c.split("\n")
        for j, ln in enumerate(lines):
            tw = d.textlength(ln, font=SMALL)
            d.text((row_w + cw * i + (cw - tw) / 2, 44 + j * 22), ln, font=SMALL, fill=MUTED)
    y = head_h
    for (label, _), im in zip(rows, ims):
        o.paste(im, (row_w, y))
        for j, ln in enumerate(label.split("\n")):
            d.text((16, y + im.height // 2 - 12 + j * 24), ln, font=SMALL, fill=FG)
        y += im.height
    o.save(path)
    print("WROTE", path, o.size)


p = lambda n: os.path.join(src, "%s-%s.png" % (tag, n))
name = tag.capitalize()
pre = os.path.join(out, "dark-fortress-armor-test-")
if tag != "knight":
    pre += tag + "-"

sheet([("Front", p("apose-front")), ("Back", p("apose-back")), ("Side", p("apose-side")),
       ("Three-\nquarter", p("apose-three-quarter"))],
      ["Original %s\n(Dark Fortress)" % name, "Cut and fitted,\nmale (MFH head)", "Cut and fitted,\nfemale (MFH head)"],
      pre + "apose.png", "Dark Fortress %s as modular armor - A-pose" % name, scale=0.8)

if os.path.exists(p("poses-front")):
    cols = []
    for pose in ("Arms raised", "Walking stride", "Combat stance"):
        cols += [pose + "\noriginal", pose + "\nmale", pose + "\nfemale"]
    sheet([("Front", p("poses-front")), ("Three-\nquarter", p("poses-three-quarter")),
           ("Game\ncamera", p("poses-game"))], cols, pre + "poses.png",
          "Deformation: the cut pieces keep the character's own skin weights (same skeleton)", scale=0.85)

looks = ["Knight chest\nbare rest", "Knight chest+gloves\nplate legs, boots", "Plate chest\nKnight rest",
         "Plate gloves\nKnight leggings", "Chest, no pads\nKnight boots"]
for pose in ("apose", "combat"):
    if os.path.exists(p("mix-%s-front" % pose)):
        cols = ["Male: " + l for l in looks] + ["Female: " + l for l in looks]
        cols = [c.replace(": ", "\n", 1).split("\n")[0] + " - " + c.split("\n", 1)[1].replace("\n", ", ") if False else c for c in cols]
        sheet([("Front", p("mix-%s-front" % pose)), ("Three-\nquarter", p("mix-%s-three-quarter" % pose)),
               ("Back", p("mix-%s-back" % pose))],
              ["M: " + l.split("\n")[0] + "\n" + l.split("\n")[1] for l in looks] +
              ["F: " + l.split("\n")[0] + "\n" + l.split("\n")[1] for l in looks],
              pre + "mix-%s.png" % pose, "%s pieces worn with existing game parts (%s)" % (name, "A-pose" if pose == "apose" else "combat stance"))

cells = os.path.join(src, "cells")
rows = [("neck", "Neck\ngame camera"), ("neck-back", "Neck\nfrom behind"), ("shoulders", "Shoulders\narms raised"),
        ("hips-front", "Hips\nfront"), ("elbow", "Elbow\ncombat"), ("waist", "Waist\nstride"),
        ("knee", "Knee\nstride"), ("knee-combat", "Knee\ncombat")]
if os.path.exists(os.path.join(cells, "%s-seam-neck-0.png" % tag)):
    strips = []
    for key, label in rows:
        ims = [Image.open(os.path.join(cells, "%s-seam-%s-%d.png" % (tag, key, i))).convert("RGB") for i in range(5)]
        strip = Image.new("RGB", (sum(i.width for i in ims), ims[0].height))
        x = 0
        for im in ims:
            strip.paste(im, (x, 0)); x += im.width
        sp = os.path.join(cells, "%s-strip-%s.png" % (tag, key))
        strip.save(sp)
        strips.append((label, sp))
    sheet(strips, ["Male, full set", "Female, full set", "M: Knight chest\nplate legs, boots",
                   "F: plate chest, Knight\ngloves, leggings", "M: bare chest, Knight\ngloves, leggings"],
          pre + "seams.png", "Close-ups where the items meet", scale=0.62)
