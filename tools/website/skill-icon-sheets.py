# Cuts the skill tree icon sheets (made with the Image Generator, three sheets of sigils on an even grid) into one icon
# per skill for the website's star map: RavenNest/src/RavenNest.Blazor/wwwroot/imgs/skills/<id>.webp, the trees'
# emblems as tree-<name>.webp and the skill point as point.webp. Each icon is trimmed to what is drawn and centred on a
# transparent square.
#
#   python tools/website/skill-icon-sheets.py [folder with skill-sheet-a.webp, -b and -c; default tools/website/skill-icon-sheets]
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
}


def cut(sheet, grid, names):
    w, h = sheet.size
    cw, ch = w / grid, h / grid
    for i, name in enumerate(names):
        x, y = i % grid, i // grid
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
        cut(Image.open(path).convert("RGBA"), grid, names)
        count += len(names)
    print("cut", count, "icons into", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
