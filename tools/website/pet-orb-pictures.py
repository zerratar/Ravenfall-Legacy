# The website's pictures of the three orb pets (companion-blue-orb, -green-orb, -red-orb).
#
# tools/website/pet-renders.cs renders a pet's model, and takes its particle systems off first. The orbs have no model:
# each is two additive particle systems (a core of Circle37 sprites, a glow of Circle67 sprites) and a point light, so
# nothing was left to render and they had no picture. This draws them from what the game draws them with: the two
# sprites in the systems' own start colors, added together as the additive shader does, several particles deep (the
# core holds about nine sprites at a time: 30 a second living 0.3 s; the glow about four).
#
#   python tools/website/pet-orb-pictures.py <folder with Circle37.png, Circle67.png and orbs.txt> [RavenNest repo]
#
# The folder is made by an eval that copies the two sprites out of the particle materials of Resources/Pets/*_ORB and
# writes orbs.txt, a line a system: color|system|start color RGBA|size|...|sprite (the eval is in this session's
# notes; it is three lines of Resources.Load and Graphics.Blit).
import os, sys
import numpy as np
from PIL import Image
src = sys.argv[1]
repo = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "RavenNest")
out = os.path.join(repo, "src", "RavenNest.Blazor", "wwwroot", "imgs", "pets")
SIZE = 256
# how many sprites of a system overlap at a time, and how much of the picture its start size covers
DEPTH = {"core": 9 * 0.35, "glow": 4 * 0.5}   # sprites alive at once, times their average fade over a lifetime
orbs = {}
for line in open(os.path.join(src, "orbs.txt")):
    c = line.strip().split("|")
    if len(c) < 6: continue
    orbs.setdefault(c[0], []).append((c[1], c[2], float(c[3]), c[5]))
largest = max(size for systems in orbs.values() for _, _, size, _ in systems)
for name, systems in orbs.items():
    acc = np.zeros((SIZE, SIZE, 3), dtype=np.float64)
    for system, color, size, sprite in systems:
        kind = "glow" if system == "Glow" else "core"
        rgb = np.array([int(color[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])
        side = max(8, int(SIZE * 0.92 * size / largest))
        im = Image.open(os.path.join(src, sprite + ".png")).convert("RGBA").resize((side, side), Image.LANCZOS)
        a = np.asarray(im).astype(np.float64) / 255.0
        # the additive particle shader: the sprite's color times its alpha times the particle's color, added
        layer = a[..., :3] * a[..., 3:4] * rgb * DEPTH[kind]
        o = (SIZE - side) // 2
        acc[o:o + side, o:o + side] += layer
    # on a web page there is nothing behind it to add to: the light becomes the picture's alpha, its hue the color
    peak = acc.max(axis=2)
    alpha = np.clip(peak, 0.0, 1.0)
    color = np.where(peak[..., None] > 1e-6, acc / np.maximum(peak[..., None], 1e-6), 0.0)
    # where more light was added than the screen can show the game's orb goes white-hot in the middle
    over = np.clip(peak - 1.0, 0.0, 1.5)[..., None] / 1.5
    color = color + (1.0 - color) * over
    px = np.dstack([np.clip(color, 0, 1), alpha[..., None]])
    Image.fromarray((px * 255).astype(np.uint8), "RGBA").save(os.path.join(out, "companion-%s-orb.webp" % name.lower()), quality=90)
    print("companion-%s-orb.webp" % name.lower())
