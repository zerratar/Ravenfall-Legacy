# python tools/dark-fortress-armor/colorways.py <repo>
#
# The tinted copies of the Dark Fortress atlas the armor sets' tiers use where none of the pack's own colorways reads
# as a different tier at stream distance (docs/dark-fortress-armor-test.md, "Color pass"). Written to
# Assets/World/DarkFortressArmor/Textures (local like the pack: rerun on a new machine), turned into materials by
# armor-sets.cs. Pillow and numpy.
#   DFArmor_Voidknight_Albedo   the pack's 01_A with every grey (the plate) turned a deep violet, luminance kept
#   DFArmor_Voidweaver_Albedo   01_B with the glowing parts (where the emissive map is lit) violet instead of white
#   DFArmor_Voidweaver_Emissive 01_B's emissive map in that violet
#   DFArmor_Stormborn_Albedo    02_A with the leathers and furs a cold storm grey-blue (the pack's blue trim stays)
import sys, os, colorsys
import numpy as np
from PIL import Image

repo = sys.argv[1]
SRC = os.path.join(repo, "Assets/Synty/PolygonDarkFortress/Textures")
OUT = os.path.join(repo, "Assets/World/DarkFortressArmor/Textures")
os.makedirs(OUT, exist_ok=True)


def load(rel):
    return np.asarray(Image.open(os.path.join(SRC, rel)).convert("RGB"), dtype=np.float32) / 255.0


def save(arr, name):
    Image.fromarray((np.clip(arr, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, name + ".png"))
    print("WROTE", name)


def hsv(arr):
    mx = arr.max(axis=2); mn = arr.min(axis=2)
    s = np.where(mx > 1e-5, (mx - mn) / np.maximum(mx, 1e-5), 0)
    return mx, s


def recolor(arr, mask, rgb, strength=1.0, keep=0.0):
    """Pixels in mask take the hue of rgb at their own brightness (keep: how much of the old color stays)."""
    lum = arr @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    target = np.array(rgb, dtype=np.float32)
    tl = float(target @ np.array([0.299, 0.587, 0.114]))
    col = target[None, None, :] * (lum / max(tl, 1e-4))[:, :, None]
    col = col * (1 - keep) + arr * keep
    m = (mask.astype(np.float32) * strength)[:, :, None]
    return arr * (1 - m) + col * m


# Voidknight: violet plate
a = load("Alts/PolygonDarkFortress_Texture_01_A.png")
v, s = hsv(a)
grey = (s < 0.18) & (v > 0.06)
save(recolor(a, grey, (0.34, 0.24, 0.56), strength=0.75, keep=0.2), "DFArmor_Voidknight_Albedo")

# Voidweaver: violet glow (the tabard and the runes)
b = load("Alts/PolygonDarkFortress_Texture_01_B.png")
e = load("Emissive/PolygonDarkFortress_Emissive_01_B.png")
lit = e.max(axis=2) > 0.25
save(recolor(b, lit, (0.44, 0.36, 1.0), strength=1.0), "DFArmor_Voidweaver_Albedo")
save(recolor(e, lit, (0.44, 0.36, 1.0), strength=1.0), "DFArmor_Voidweaver_Emissive")

# Stormborn: storm-grey leather and fur under the blue trim
c = load("Alts/PolygonDarkFortress_Texture_02_A.png")
v, s = hsv(c)
r, g, bb = c[:, :, 0], c[:, :, 1], c[:, :, 2]
warm = (r > bb + 0.04) & (s > 0.12) & (v > 0.08)
save(recolor(c, warm, (0.42, 0.50, 0.62), strength=0.8, keep=0.15), "DFArmor_Stormborn_Albedo")
