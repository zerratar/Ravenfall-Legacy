"""
Pet egg icons (docs/battle-pets-design.md, "Eggs and evolution materials"): each hatched species' first stage climbing
out of a cracked egg, or a seed husk for the plants, with the dark outline the gem and resource icons have.

The pet pictures come from the game: tools/items/egg-renders.cs (eval_file in play mode) renders every species' first
stage alone against black and against white, and the difference gives a clean alpha. Run:

    python tools/items/egg-icons.py <folder with the renders>

and the icons land in Assets/Resources/Items as "<egg-name>.png" (the game finds item icons by their name), and in the
website's imgs/items; the pets' pictures at each stage go to the website's imgs/pets ("wolf-pup.webp", "wolf-pup-1.webp").
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageChops

S = 512  # drawn at twice the size, saved at 256

# egg name, shell colour, spot colour, pattern
EGGS = {
    "wolf-pup": ("Wolf Pup Egg", (232, 226, 214), (96, 104, 120), "spots"),
    "bud": ("Bud Seed", (128, 92, 60), (150, 200, 110), "stripes"),
    "sun-blossom": ("Sun Blossom Seed", (70, 58, 50), (238, 214, 120), "stripes"),
    "practice-dummy": ("Practice Dummy Egg", (226, 204, 168), (204, 52, 48), "targets"),
    "shell": ("Shell Egg", (190, 222, 240), (60, 72, 160), "spots"),
    "snakelet": ("Snakelet Egg", (240, 196, 150), (150, 40, 40), "bands"),
    "bomb": ("Bomb Egg", (70, 78, 92), (250, 196, 70), "spots"),
    "dragon-spark": ("Dragon Spark Egg", (236, 120, 60), (255, 220, 90), "spots"),
}

OUTLINE = (24, 20, 22, 255)


def matte(black, white):
    """The pet with alpha, from its renders on black and on white."""
    b = black.convert("RGB")
    w = white.convert("RGB")
    out = Image.new("RGBA", b.size)
    bp, wp, op = b.load(), w.load(), out.load()
    for y in range(b.size[1]):
        for x in range(b.size[0]):
            br, bg, bb = bp[x, y]
            wr, wg, wb = wp[x, y]
            a = 1 - ((wr - br) + (wg - bg) + (wb - bb)) / (3 * 255)
            a = max(0.0, min(1.0, a))
            if a < 0.02:
                op[x, y] = (0, 0, 0, 0)
                continue
            op[x, y] = (min(255, int(br / a)), min(255, int(bg / a)), min(255, int(bb / a)), int(a * 255))
    return out.crop(out.getbbox()) if out.getbbox() else out


def shade(color, f):
    return tuple(max(0, min(255, int(c * f))) for c in color)


def egg_mask(cx, cy, rx, ry):
    """An egg: narrower at the top."""
    m = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(m)
    pts = []
    for i in range(360):
        t = math.radians(i)
        x = math.cos(t)
        y = math.sin(t)
        k = 1 - 0.18 * (-y if y < 0 else 0)  # top narrower
        pts.append((cx + x * rx * k, cy + y * ry))
    d.polygon(pts, fill=255)
    return m


def crack_line(cx, rx, y, rng, teeth=9, depth=26):
    pts = []
    for i in range(teeth * 2 + 1):
        x = cx - rx - 10 + (2 * rx + 20) * i / (teeth * 2)
        pts.append((x, y + (0 if i % 2 == 0 else -depth) + rng.randint(-6, 6)))
    return pts


def pattern(layer, kind, color, cx, cy, rx, ry, rng):
    d = ImageDraw.Draw(layer)
    c = color + (255,)
    if kind == "spots":
        for _ in range(16):
            x = rng.uniform(cx - rx, cx + rx)
            y = rng.uniform(cy - ry, cy + ry)
            r = rng.uniform(9, 22)
            d.ellipse((x - r, y - r * 0.8, x + r, y + r * 0.8), fill=c)
    elif kind == "stripes":
        for i in range(-3, 4):
            x = cx + i * rx * 0.3
            d.line([(x, cy - ry), (x + i * 6, cy + ry)], fill=c, width=14)
    elif kind == "bands":
        for i in range(-2, 3):
            y = cy + i * ry * 0.38
            d.arc((cx - rx, y - 40, cx + rx, y + 40), 10, 170, fill=c, width=16)
    elif kind == "targets":
        for (x, y) in ((cx - rx * 0.45, cy + ry * 0.35), (cx + rx * 0.5, cy + ry * 0.15), (cx, cy + ry * 0.7)):
            for r, col in ((30, c), (20, (255, 255, 255, 255)), (10, c)):
                d.ellipse((x - r, y - r, x + r, y + r), fill=col)


def icon(species, pet, rng):
    name, shell, spot, kind = EGGS[species]
    cx, cy, rx, ry = S // 2, int(S * 0.64), int(S * 0.30), int(S * 0.30)
    crack_y = cy - int(ry * 0.15)
    canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # the inside of the shell behind the pet
    back = egg_mask(cx, cy, rx, ry)
    inner = Image.new("RGBA", (S, S), shade(shell, 0.55) + (255,))
    cut = Image.new("L", (S, S), 0)
    ImageDraw.Draw(cut).polygon(crack_line(cx, rx, crack_y - 40, rng, depth=22) + [(cx + rx + 20, S), (cx - rx - 20, S)], fill=255)
    back_mask = ImageChops.multiply(back, cut)
    canvas.paste(inner, (0, 0), back_mask)

    # the pet, climbing out
    h = int(S * 0.62)
    scale = h / pet.size[1]
    w = int(pet.size[0] * scale)
    if w > S * 0.80:
        w = int(S * 0.80)
        h = int(pet.size[1] * w / pet.size[0])
    p = pet.resize((w, h), Image.LANCZOS)
    px = cx - w // 2
    py = crack_y + int(h * 0.22) - h  # its lowest fifth is still in the shell
    py = max(int(S * 0.04), py)
    canvas.alpha_composite(p, (px, py))

    # the front of the shell, broken along the crack
    front_cut = Image.new("L", (S, S), 0)
    crack = crack_line(cx, rx, crack_y, rng)
    ImageDraw.Draw(front_cut).polygon(crack + [(cx + rx + 20, S), (cx - rx - 20, S)], fill=255)
    front_mask = ImageChops.multiply(egg_mask(cx, cy, rx, ry), front_cut)

    body = Image.new("RGBA", (S, S), shell + (255,))
    # light from the top left
    light = Image.new("L", (S, S), 0)
    ImageDraw.Draw(light).ellipse((cx - rx * 1.4, cy - ry * 1.6, cx + rx * 0.6, cy + ry * 0.4), fill=255)
    light = light.filter(ImageFilter.GaussianBlur(40))
    dark = Image.new("RGBA", (S, S), shade(shell, 0.72) + (255,))
    body = Image.composite(body, dark, light)
    pattern(body, kind, spot, cx, cy, rx, ry, rng)
    # a shine
    shine = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(shine).ellipse((cx - rx * 0.62, cy - ry * 0.05, cx - rx * 0.30, cy + ry * 0.45), fill=(255, 255, 255, 90))
    body.alpha_composite(shine.filter(ImageFilter.GaussianBlur(6)))

    # the outline: the front shell's edge, thick like the gem icons
    edge = front_mask.filter(ImageFilter.MaxFilter(13))
    outline = Image.new("RGBA", (S, S), OUTLINE)
    canvas.paste(outline, (0, 0), ImageChops.subtract(edge, front_mask))
    canvas.paste(body, (0, 0), front_mask)

    # an outline around everything, so it reads on any background
    alpha = canvas.split()[3]
    ring = ImageChops.subtract(alpha.point(lambda a: 255 if a > 40 else 0).filter(ImageFilter.MaxFilter(7)), alpha.point(lambda a: 255 if a > 40 else 0))
    under = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    under.paste(Image.new("RGBA", (S, S), OUTLINE), (0, 0), ring)
    under.alpha_composite(canvas)
    return name, under.resize((256, 256), Image.LANCZOS)


def slug(name):
    return name.replace(" ", "-").replace("'", "").lower()


def website_picture(pet, path):
    """The pet cropped, squared with a little margin, 256 px webp, as tools/website/pet-renders.py makes them."""
    w, h = pet.size
    side = int(max(w, h) * 1.12)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(pet, ((side - w) // 2, (side - h) // 2))
    square.resize((256, 256), Image.LANCZOS).save(path, quality=90)


def main():
    src = sys.argv[1]
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, "..", "..", "Assets", "Resources", "Items")
    web = os.path.join(here, "..", "..", "..", "RavenNest", "src", "RavenNest.Blazor", "wwwroot", "imgs")
    for species in EGGS:
        for stage in (1, 2):
            f = species + "-" + str(stage)
            if os.path.exists(os.path.join(src, f + "-black.png")):
                pet = matte(Image.open(os.path.join(src, f + "-black.png")), Image.open(os.path.join(src, f + "-white.png")))
                website_picture(pet, os.path.join(web, "pets", f + ".webp"))
    for species in EGGS:
        black = Image.open(os.path.join(src, species + "-black.png"))
        white = Image.open(os.path.join(src, species + "-white.png"))
        pet = matte(black, white)
        name, img = icon(species, pet, random.Random(species))
        website_picture(pet, os.path.join(web, "pets", species + ".webp"))
        path = os.path.normpath(os.path.join(out, slug(name) + ".png"))
        img.save(path)
        img.save(os.path.join(web, "items", slug(name) + ".png"))
        print(path)


if __name__ == "__main__":
    main()
