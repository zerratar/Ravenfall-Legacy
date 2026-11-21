# Cuts the skill tree icon sheets (made with the Image Generator, sheets of sigils on an even grid) into one icon
# per skill for the website's star map: RavenNest/src/RavenNest.Blazor/wwwroot/imgs/skills/<id>.webp, the trees'
# emblems as tree-<name>.webp and the skill point as point.webp. Each icon is trimmed to what is drawn and centred on a
# transparent square.
#
#   python tools/website/skill-icon-sheets.py [folder with skill-sheet-a.webp, -b, -c and -d; default tools/website/skill-icon-sheets]
# (the sheets as made are kept there, with skill-medallion.webp, the centre of the map, saved by hand as imgs/skills/medallion.webp)
import os
import sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
SHEETS = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "skill-icon-sheets")
OUT = os.path.join(ROOT, "..", "RavenNest", "src", "RavenNest.Blazor", "wwwroot", "imgs", "skills")
SIZE = 160

LAYOUT = {
    "a": (4, ["taunt", "shield-wall", "shield-bash", "rally", "mend", "renew", "barrier", "cleanse", "sanctuary",
              "cleave", "crushing-blow", "whirlwind", "leap", "expose-weakness", "flurry", "disarm"]),
    "b": (4, ["multishot", "piercing-shot", "arrow-rain", "snare", "rapid-fire", "fireball", "frost-nova",
              "chain-lightning", "toxic-cloud", "meteor", "second-wind", "iron-skin", "mark-prey", "execute",
              "last-stand", "battle-rhythm"]),
    "c": (3, ["tree-bulwark", "tree-restoration", "tree-might", "tree-precision", "tree-marksman", "tree-elements",
              "tree-vitality", "tree-hunter", "point"]),
    # the Beastmaster tree (2026-11-21); the last row is spare sigils (a pack, a guard, an element), not cut
    "d": (3, ["tree-beastmaster", "sic-em", "mend-pet", "call-of-the-wild", "trick-training", "shared-mending",
              None, None, None]),
}


# A sheet whose sigils do not sit on even thirds: where each one is (left, top, right, bottom on a 1024 sheet), with
# room to spare; the trim below finds what is drawn inside it.
BOXES = {
    "d": [(20, 15, 340, 325), (345, 25, 660, 310), (690, 15, 1010, 315),
          (20, 340, 340, 640), (345, 330, 665, 645), (675, 350, 1015, 620),
          None, None, None],
}


def cut(sheet, grid, names, boxes=None):
    w, h = sheet.size
    cw, ch = w / grid, h / grid
    for i, name in enumerate(names):
        if name is None:
            continue
        x, y = i % grid, i // grid
        box = boxes[i] if boxes else None
        if box:
            k = w / 1024.0
            cell = sheet.crop(tuple(int(v * k) for v in box))
        else:
            cell = sheet.crop((int(x * cw), int(y * ch), int((x + 1) * cw), int((y + 1) * ch)))
        # what is drawn: alpha above a faint glow
        mask = cell.getchannel("A").point(lambda a: 255 if a > 24 else 0)
        box = mask.getbbox() or (0, 0, cell.width, cell.height)
        icon = cell.crop(box)
        side = max(icon.width, icon.height)
        square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        square.alpha_composite(icon, ((side - icon.width) // 2, (side - icon.height) // 2))
        square.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, name + ".webp"), quality=88, method=6)


def main():
    os.makedirs(OUT, exist_ok=True)
    count = 0
    for key, (grid, names) in LAYOUT.items():
        path = os.path.join(SHEETS, "skill-sheet-" + key + ".webp")
        if not os.path.exists(path):
            print("missing", path)
            continue
        cut(Image.open(path).convert("RGBA"), grid, names, BOXES.get(key))
        count += len([n for n in names if n])
    print("cut", count, "icons into", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
