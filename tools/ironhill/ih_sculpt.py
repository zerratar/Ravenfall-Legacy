"""Designs Ironhill's new ground on a copy of the world terrain's heights and draws a shaded relief to judge it.

Terrain: origin (-1245.06, -30, -852.52), 2560 m, 513 samples (5 m each), heights 0..1 over 100 m.
Writes ih-heights.bin (the island window only) and ih-relief.png.
"""
import numpy as np
from PIL import Image

S = "C:/Users/kaaru/AppData/Local/Temp/claude/G--Ravenfall-Projects-Ravenfall-Legacy/86b0e615-81c6-408c-acb9-282e465846ce/scratchpad/"
N, OX, OY, OZ, STEP, TALL = 513, -1245.06, -30.0, -852.52, 5.0, 100.0
h0 = np.frombuffer(open(S + "terrain-heights-before-ironhill.bin", "rb").read(), dtype=np.float32).reshape(N, N)
world = OY + h0 * TALL  # [iz, ix]

# the window we touch
X0, X1, Z0, Z1 = -110, 120, 250, 575
ix0, ix1 = int((X0 - OX) / STEP), int((X1 - OX) / STEP) + 1
iz0, iz1 = int((Z0 - OZ) / STEP), int((Z1 - OZ) / STEP) + 1
xs = OX + np.arange(ix0, ix1) * STEP
zs = OZ + np.arange(iz0, iz1) * STEP
X, Z = np.meshgrid(xs, zs)
cur = world[iz0:iz1, ix0:ix1].copy()

SEA = -3.2

def seg_dist(px, pz, a, b):
    ax, az = a; bx, bz = b
    vx, vz = bx - ax, bz - az
    t = np.clip(((px - ax) * vx + (pz - az) * vz) / (vx * vx + vz * vz), 0, 1)
    return np.hypot(px - (ax + t * vx), pz - (az + t * vz)), t

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)

# the ridge: west (over the cave) highest, stepping down east; (x, z, height above sea)
ridge = [(-86, 500, 18), (-58, 524, 30), (-22, 538, 50), (14, 530, 56), (50, 504, 36), (78, 472, 10)]
best = np.full(X.shape, -1e9)
for a, b in zip(ridge, ridge[1:]):
    d, t = seg_dist(X, Z, a[:2], b[:2])
    top = a[2] + (b[2] - a[2]) * t
    # south of the ridge the flank is wide (the hold sits on it), north it drops to the sea
    north = (Z > (a[1] + (b[1] - a[1]) * t))
    width = np.where(north, 30.0, 64.0)
    k = 1 - smooth(0, 1, d / width)
    m = SEA + top * k ** 0.85
    best = np.maximum(best, m)

# rough it up: two octaves of value noise, stronger high up
rng = np.random.default_rng(7)
def noise(scale, amp):
    g = rng.standard_normal((int(330 / scale) + 3, int(240 / scale) + 3))
    gi = (Z - Z0) / scale; gj = (X - X0) / scale
    i0 = gi.astype(int); j0 = gj.astype(int); fi = gi - i0; fj = gj - j0
    fi = fi * fi * (3 - 2 * fi); fj = fj * fj * (3 - 2 * fj)
    v = (g[i0, j0] * (1 - fi) * (1 - fj) + g[i0 + 1, j0] * fi * (1 - fj) + g[i0, j0 + 1] * (1 - fi) * fj + g[i0 + 1, j0 + 1] * fi * fj)
    return v * amp
lift = np.clip((best - SEA) / 40, 0, 1)
best = best + (noise(22, 3.0) + noise(9, 1.2)) * lift

# the dwarf hold's yard: a flat shelf cut into the south flank, the face rising steeply behind it (the gate's wall)
cx, cz, hx, hz = 16.0, 463.5, 30.0, 19.5
qx = np.maximum(np.abs(X - cx) - hx, 0); qz = np.maximum(np.abs(Z - cz) - hz, 0)
dy = np.hypot(qx, qz)  # 0 inside the yard
shelf = SEA + 8.0
back = Z > cz + hz  # behind the yard the face rises at once
k = np.where(back, smooth(0, 3.5, dy), smooth(0, 9, dy))
best = np.where(dy < 9, shelf * (1 - k) + np.maximum(best, shelf) * k, best)

# round the cave the ground only climbs to its rocky roof, so the outcrop over it stays in sight
shoulder = SEA + 15.0 + noise(7, 1.5)
ring = smooth(1.0, 1.9, cave_d := np.hypot((X + 35) / 22.0, (Z - 478) / 28.0))
best = np.where(cave_d < 1.9, np.minimum(best, shoulder + (best - shoulder) * ring), best)

# the cave stays as it is: no ground inside its walls, the mountain wraps round it
cave = np.hypot((X + 35) / 22.0, (Z - 478) / 28.0)
inside = 1 - smooth(0.82, 0.97, cave)
best = best * (1 - inside) + np.minimum(cur, -6.0) * inside

# whatever rises fades out well inside the window, so its border never shows as a cliff
edge = np.minimum.reduce([X - X0, X1 - X, Z - Z0, Z1 - Z])
best = SEA - 20 + (best - (SEA - 20)) * smooth(4, 30, edge)
new = np.maximum(cur, best)
# keep the south of the island (z < 400) exactly as it was for now
new = np.where(Z < 395, cur, new * smooth(395, 415, Z) + cur * (1 - smooth(395, 415, Z)))

np.save(S + "ih-new.npy", new)
out = ((new - OY) / TALL).astype(np.float32)
open(S + "ih-heights.bin", "wb").write(out.tobytes())
open(S + "ih-window.txt", "w").write(f"{ix0} {iz0} {ix1 - ix0} {iz1 - iz0}")

# shaded relief, north up, with sea tint
gz, gx = np.gradient(new, STEP)
light = np.clip(0.6 + (-gx * 0.6 + gz * 0.6) * 0.5, 0, 1)
col = np.zeros(new.shape + (3,))
land = new > SEA
hgt = np.clip((new - SEA) / 50, 0, 1)
col[..., 0] = np.where(land, 0.45 + 0.4 * hgt, 0.1)
col[..., 1] = np.where(land, 0.55 + 0.2 * hgt, 0.35)
col[..., 2] = np.where(land, 0.3 + 0.4 * hgt, 0.55)
col *= light[..., None]
img = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8)).transpose(Image.FLIP_TOP_BOTTOM)
img = img.resize((img.width * 8, img.height * 8), Image.NEAREST)
from PIL import ImageDraw
d = ImageDraw.Draw(img)
def px(x, z): return ((x - X0) / STEP * 8, (Z1 - z) / STEP * 8)
for (x, z, label) in [(-36, 494, "rune"), (14, 470, "gate"), (30, 293, "dock"), (10, 409, "fight"), (-56, 411, "mine")]:
    p = px(x, z); d.ellipse([p[0] - 5, p[1] - 5, p[0] + 5, p[1] + 5], outline=(255, 0, 0)); d.text((p[0] + 7, p[1] - 6), label, fill=(255, 255, 255))
img.save(S + "ih-relief.png")
print("max", float(new.max()), "window", ix0, iz0, ix1 - ix0, iz1 - iz0)
