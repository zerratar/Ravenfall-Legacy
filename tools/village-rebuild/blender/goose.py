# The fountain goose for tier 5: a proud white goose with its wings half raised, about 0.9 m tall, standing
# on the origin and facing -y (Unity +z). The crown is added in Unity from the hat kit, like the other
# crowned easter eggs.
WHITE, GREY, ORANGE, BLACK = "f7f2f0", "bbb7b1", "e8862a", "191a1b"
b = Builder("Village_Goose")

def loft(stations, n, col, cap0=True, cap1=True):
    """Tube through stations [(centre, rx, rz)]; rings are perpendicular to the path, rx across (x), rz in the path plane."""
    pts = [Vector(s[0]) for s in stations]
    rings = []
    for i, (cen, rx, rz) in enumerate(stations):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        side = Vector((1, 0, 0))
        # (side, up, t) stays right handed and turns smoothly with the path, so the winding never flips.
        # Forcing up to point upward, as this once did, flipped rings where the S neck leans back.
        up = t.cross(side).normalized()
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n
            ring.append(Vector(cen) + side * math.cos(a) * rx + up * math.sin(a) * rz)
        rings.append(ring)
    vs = [v for r in rings for v in r]
    idx = []
    for i in range(len(rings) - 1):
        a, c = i * n, (i + 1) * n
        for k in range(n): idx.append((a + k, a + (k + 1) % n, c + (k + 1) % n, c + k))
    if cap0: idx.append(tuple(reversed(range(n))))
    if cap1: idx.append(tuple(range((len(rings) - 1) * n, len(rings) * n)))
    fs = b.add(vs, idx)
    for f in fs: f.normal_update()
    b.paint(fs, "PAL", col)

def slab(outline, z_of, thick, col, mirror_x=False):
    """Extruded flat shape: outline is a list of 3D points, thickness along the face normal."""
    pts = [Vector(p) for p in outline]
    if mirror_x: pts = [Vector((-p.x, p.y, p.z)) for p in pts][::-1]
    n = (pts[1] - pts[0]).cross(pts[2] - pts[0]).normalized()
    m = len(pts)
    vs = [p - n * thick / 2 for p in pts] + [p + n * thick / 2 for p in pts]
    idx = [tuple(reversed(range(m))), tuple(range(m, 2 * m))] + [(k, (k + 1) % m, m + (k + 1) % m, m + k) for k in range(m)]
    fs = b.add(vs, idx)
    for f in fs: f.normal_update()
    b.paint(fs, "PAL", col)

# body, tail (back, +y) to breast (front, -y)
loft([((0, 0.38, 0.44), 0.015, 0.012), ((0, 0.31, 0.40), 0.08, 0.05), ((0, 0.21, 0.35), 0.15, 0.11), ((0, 0.08, 0.31), 0.185, 0.145),
      ((0, -0.05, 0.31), 0.185, 0.155), ((0, -0.16, 0.34), 0.155, 0.14), ((0, -0.23, 0.39), 0.11, 0.11), ((0, -0.255, 0.45), 0.07, 0.075)], 10, WHITE)
# neck: an S curve up to the head
loft([((0, -0.2, 0.38), 0.085, 0.085), ((0, -0.25, 0.46), 0.068, 0.068), ((0, -0.28, 0.53), 0.055, 0.055), ((0, -0.295, 0.60), 0.048, 0.048), ((0, -0.285, 0.68), 0.045, 0.045),
      ((0, -0.26, 0.75), 0.046, 0.046), ((0, -0.255, 0.81), 0.05, 0.05)], 8, WHITE, cap0=False, cap1=False)
# head, slightly long, a touch forward of the neck top
b.sphere((0, -0.28, 0.845), 0.072, "PAL", WHITE, seg=8, rings=5)
loft([((0, -0.31, 0.85), 0.055, 0.06), ((0, -0.24, 0.85), 0.05, 0.055)], 8, WHITE)
# beak with the black nail at its tip
loft([((0, -0.335, 0.835), 0.032, 0.028), ((0, -0.38, 0.828), 0.024, 0.018), ((0, -0.425, 0.82), 0.012, 0.009)], 6, ORANGE, cap0=False)
b.sphere((0, -0.428, 0.82), 0.012, "PAL", BLACK, seg=5, rings=3)
# eyes
for sx in (-1, 1): b.sphere((sx * 0.055, -0.3, 0.87), 0.013, "PAL", BLACK, seg=5, rings=3)

# wings, half raised and swept back: white coverts and grey primaries with a feathered trailing edge
for sx in (1, -1):
    root = Vector((sx * 0.13, -0.08, 0.42))
    def wp(u, v, lift):
        # u along the wing (root to tip, back and up), v across it
        return (root.x + sx * (0.02 + u * 0.16 + lift * 0.05), root.y + u * 0.42 + v * 0.05, root.z + u * 0.26 + v * 0.16 - lift * 0.02)
    coverts = [wp(0, -0.6, 0), wp(0.15, 0.55, 0.3), wp(0.55, 0.45, 0.8), wp(0.62, -0.1, 1.0), wp(0.45, -0.75, 0.7)]
    slab(coverts, None, 0.035, WHITE)
    prim = [wp(0.45, -0.75, 0.7), wp(0.62, -0.1, 1.0), wp(0.55, 0.45, 0.8), wp(0.85, 0.35, 1.1), wp(0.92, 0.05, 1.2),
            wp(1.0, -0.3, 1.25), wp(0.86, -0.45, 1.1), wp(0.82, -0.7, 1.0)]
    slab(prim, None, 0.025, GREY)
# a fluffy tuft of tail feathers
slab([(-0.05, 0.33, 0.41), (0.05, 0.33, 0.41), (0.03, 0.42, 0.47), (0, 0.40, 0.46), (-0.03, 0.42, 0.47)], None, 0.03, WHITE)

# legs and webbed feet
for sx in (-1, 1):
    b.prism((sx * 0.065, 0.02, 0.02), 0.022, 0.2, 6, "PAL", ORANGE, r_top=0.018)
    foot = [(sx * 0.065 - 0.012, 0.04, 0.012), (sx * 0.065 + 0.012, 0.04, 0.012), (sx * 0.065 + 0.07, -0.09, 0.012), (sx * 0.065, -0.07, 0.012), (sx * 0.065 - 0.07, -0.09, 0.012)]
    slab(foot, None, 0.02, ORANGE)

goose = b.finish()
export_fbx([goose], "Village_Goose")
preview([goose], "goose", views=((210, 12), (120, 25), (0, 15)), res=(900, 900), dist_mul=1.3)
