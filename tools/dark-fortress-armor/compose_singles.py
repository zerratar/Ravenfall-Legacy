# python tools/dark-fortress-armor/compose_singles.py <preview dir> <out dir>
# The color pass and single pieces sheets from class-gear-preview.cs renders (Pillow), quantized for the repository.
import sys, os
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 26)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 21)
BG, FG, MUTED = (34, 37, 43), (235, 235, 235), (175, 181, 191)


def sheet(name, title, cols, rows, scale=0.6):
    ims = [Image.open(os.path.join(src, f + ".png")).convert("RGB") for _, f in rows]
    ims = [i.resize((int(i.width * scale), int(i.height * scale)), Image.LANCZOS) for i in ims]
    w = max(i.width for i in ims); lw, head = 160, 100
    o = Image.new("RGB", (lw + w, head + sum(i.height for i in ims)), BG)
    d = ImageDraw.Draw(o)
    d.text((16, 14), title, font=FONT, fill=FG)
    cw = w / len(cols)
    for i, c in enumerate(cols):
        for j, ln in enumerate(c.split("\n")):
            tw = d.textlength(ln, font=SMALL)
            d.text((lw + cw * i + (cw - tw) / 2, 48 + j * 23), ln, font=SMALL, fill=MUTED)
    y = head
    for (label, _), im in zip(rows, ims):
        o.paste(im, (lw, y))
        for j, ln in enumerate(label.split("\n")):
            d.text((16, y + im.height // 2 - 12 + j * 24), ln, font=SMALL, fill=FG)
        y += im.height
    path = os.path.join(out, name + ".png")
    o.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(path, optimize=True)
    print("WROTE", path, o.size, os.path.getsize(path) // 1024, "KB")


sheet("dark-fortress-armor-colors", "The six tier looks after the color pass (game camera, real equip code)",
      ["Voidknight\nDark Knight 7", "Abyssal\nDark Knight 8", "Stormborn\nBarbarian 7", "Worldbreaker\nBarbarian 8",
       "Voidweaver\nMage 7", "Starcaller\nMage 8"],
      [("Male\ngame cam", "colors-male"), ("Female\ngame cam", "colors-female"), ("Male\nfront", "colors-front-male")])
cols = ["Melee 6\n(before)", "Melee 7\nplate guards", "Beastmaster 7\nfur mantle", "Rogue 7: Undead\npauldrons, boots",
        "Witch Hunter 7\ncollar, bracers", "Witch Hunter 6\n(before)"]
sheet("dark-fortress-single-pieces-worn", "Single pieces on their class gear tiers (real equip code)", cols,
      [("Male\nidle", "singles-idle-male"), ("Female\nidle", "singles-idle-female"), ("Male\nbehind", "singles-back-male"),
       ("Female\nbehind", "singles-back-female")])
sheet("dark-fortress-single-pieces-animations", "Single pieces in the game's sword attack and run", cols,
      [("Male\nattack", "singles-attack-male"), ("Female\nattack", "singles-attack-female"), ("Male\nrun", "singles-run-male"),
       ("Female\nrun", "singles-run-female")])
sheet("dark-fortress-single-pieces-mixed", "Single pieces mixed with other gear",
      ["Guards on\nRune plate", "Collar on\nAbyssal", "Mantle on\nWorldbreaker", "Bracers on\nRune plate",
       "Spiked boots\nunder Starcaller", "Pauldrons on\nRune plate"],
      [("Male", "singles-mix-male"), ("Female", "singles-mix-female")])
