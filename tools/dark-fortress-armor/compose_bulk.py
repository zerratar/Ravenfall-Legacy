# python tools/dark-fortress-armor/compose_bulk.py <bulk render dir> <out png>
# The male bulk sheet: bulk_sheet.py's rows (idle and combat) and chest_closeups.py's before and after close-ups.
import sys, os
from PIL import Image, ImageDraw, ImageFont

src, outp = sys.argv[1], sys.argv[2]
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 26)
SMALL = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
BG, FG, MUTED = (34, 37, 43), (235, 235, 235), (175, 181, 191)
cols = ["Knight\nbefore", "Knight\nafter", "Barbarian\nbefore", "Barbarian\nafter", "Necromancer\nbefore",
        "Necromancer\nafter", "Game plate\n(Rune)", "Tank class\ngear"]
rows = [("Idle\nfront", "bulk-idle-front.png"), ("Idle\nside", "bulk-idle-side.png"), ("Idle\ngame cam", "bulk-idle-game.png"),
        ("Combat\nfront", "bulk-combat-front.png"), ("Combat\ngame cam", "bulk-combat-game.png")]
scale = 0.7
ims = [Image.open(os.path.join(src, f)).convert("RGB") for _, f in rows]
ims = [i.resize((int(i.width * scale), int(i.height * scale)), Image.LANCZOS) for i in ims]
# chest close-ups: before and after side by side per character, front, side and three-quarter
cb = [Image.open(os.path.join(src, "chest-before-%s.png" % v)).convert("RGB") for v in ("front", "side", "three-quarter")]
ca = [Image.open(os.path.join(src, "chest-after-%s.png" % v)).convert("RGB") for v in ("front", "side", "three-quarter")]
cw = cb[0].width // 5
chest = Image.new("RGB", (cw * 6, cb[0].height * 3), BG)
for r in range(3):
    for i in range(3):
        chest.paste(cb[r].crop((i * cw, 0, (i + 1) * cw, cb[r].height)), (i * 2 * cw, r * cb[0].height))
        chest.paste(ca[r].crop((i * cw, 0, (i + 1) * cw, ca[r].height)), ((i * 2 + 1) * cw, r * cb[0].height))
w = max(i.width for i in ims)
cs = w / chest.width * 0.75
chest = chest.resize((int(chest.width * cs), int(chest.height * cs)), Image.LANCZOS)
lw, head = 150, 100
H = head + sum(i.height for i in ims) + 90 + chest.height + 20
o = Image.new("RGB", (lw + w, H), BG)
d = ImageDraw.Draw(o)
d.text((16, 12), "Male bulk pass: the male pieces before and after, beside the game's plate and Tank gear", font=FONT, fill=FG)
cwid = w / len(cols)
for i, c in enumerate(cols):
    for j, ln in enumerate(c.split("\n")):
        tw = d.textlength(ln, font=SMALL)
        d.text((lw + cwid * i + (cwid - tw) / 2, 48 + j * 22), ln, font=SMALL, fill=MUTED)
y = head
for (label, _), im in zip(rows, ims):
    o.paste(im, (lw, y))
    for j, ln in enumerate(label.split("\n")):
        d.text((16, y + im.height // 2 - 12 + j * 24), ln, font=SMALL, fill=FG)
    y += im.height
d.text((16, y + 20), "Chest close-ups (front, side, three-quarter): before | after, Knight, Barbarian, Necromancer", font=SMALL, fill=FG)
o.paste(chest, (lw, y + 60))
o.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(outp, optimize=True)
print("WROTE", outp, o.size, os.path.getsize(outp) // 1024, "KB")
