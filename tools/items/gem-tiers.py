# Makes the five tier icons of each enchanted gem (docs/gems-design.md) from one picture per kind:
# Assets/Resources/Items/enchanted-<kind>.png (made by the Image Generator) becomes chipped-, flawed-, polished-,
# flawless- and perfect-<kind>.png. A tier shows as gold pips along the bottom (one to five), the gem grows a little
# with each tier, Chipped is duller and Perfect gets a soft glow. Copied to the website's item pictures as well.
#
#   python tools/items/gem-tiers.py
import os
import shutil
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
ITEMS = os.path.join(ROOT, "Assets", "Resources", "Items")
WEBSITE = [os.path.join(ROOT, "..", "RavenNest", "src", "RavenNest.Blazor", "wwwroot", "imgs", "items")]
KINDS = ["ruby", "sapphire", "topaz", "emerald", "diamond", "onyx", "amethyst"]
TIERS = ["chipped", "flawed", "polished", "flawless", "perfect"]
SCALE = [0.74, 0.80, 0.86, 0.92, 0.96]
SIZE = 256


def pip(draw, cx, cy, r):
    points = [(cx, cy - r), (cx + r * 0.75, cy), (cx, cy + r), (cx - r * 0.75, cy)]
    draw.polygon(points, fill=(30, 20, 10, 255))
    inner = [(cx, cy - r + 4), (cx + r * 0.75 - 3.5, cy), (cx, cy + r - 4), (cx - r * 0.75 + 3.5, cy)]
    draw.polygon(inner, fill=(250, 205, 90, 255))
    draw.polygon([(cx, cy - r + 4), (cx + r * 0.75 - 3.5, cy), (cx, cy)], fill=(255, 240, 170, 255))


def tier_icon(source, tier):
    gem = source.copy()
    if tier == 0:
        gem = ImageEnhance.Color(gem).enhance(0.55)
        gem = ImageEnhance.Brightness(gem).enhance(0.85)
    side = int(SIZE * SCALE[tier])
    gem = gem.resize((side, side), Image.LANCZOS)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    top = int((SIZE - side) / 2) - 20
    canvas.alpha_composite(gem, ((SIZE - side) // 2, max(0, top)))

    if tier == 4:
        # a soft glow of the gem's own colour behind it
        glow = canvas.copy().filter(ImageFilter.GaussianBlur(10))
        glow = ImageEnhance.Brightness(glow).enhance(1.6)
        under = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        under.alpha_composite(glow)
        under.alpha_composite(canvas)
        canvas = under

    draw = ImageDraw.Draw(canvas)
    count = tier + 1
    r, gap = 17, 34
    width = (count - 1) * gap
    for i in range(count):
        pip(draw, SIZE / 2 - width / 2 + i * gap, SIZE - 22, r)
    return canvas


def main():
    made = 0
    for kind in KINDS:
        path = os.path.join(ITEMS, "enchanted-" + kind + ".png")
        if not os.path.exists(path):
            print("missing", path)
            continue
        source = Image.open(path).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS)
        for t, tier in enumerate(TIERS):
            name = tier + "-" + kind + ".png"
            out = os.path.join(ITEMS, name)
            tier_icon(source, t).save(out)
            for folder in WEBSITE:
                if os.path.isdir(folder):
                    shutil.copyfile(out, os.path.join(folder, name))
            made += 1
    print("made", made, "icons")


if __name__ == "__main__":
    main()
