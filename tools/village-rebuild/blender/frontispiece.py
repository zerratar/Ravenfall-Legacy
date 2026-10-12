# Tier 5 frontispiece: the centre bay of the castle front, from the ground to a gable over the battlements.
# It takes the two middle wall slots of the front (the big door and the window above it), keeping the kit
# pivot: in Blender it spans x 0..5 and z 0..15 and faces +y (Unity: x -5..0, facing -z).
#
# Buttresses run from the ground to pinnacles. Between them: a pointed portal with stepped archivolts and
# wooden doors under a gablet, three lancets, the rose, and the gable with its round window and finial.
# The glass and the doors show on the inside too, because the great hall is open to the sky.
#
# Every face is oriented explicitly (face() and solid_xz() check the direction), so finish() runs without
# Blender's normal guessing, which got flat glass wrong.
X0, X1, H, T = 0.0, 5.0, 15.0, 0.56
CX = 2.5
BY = -T / 2       # back face (the kit wall's back)
F = T / 2 + 0.3   # front face: the bay stands 0.3 m proud of the kit walls
STONE, STONE_D, GOLD, IRON, WOOD, WOOD_D = "a4a29e", "787877", "d1a44c", "191a1b", "8a572e", "693b13"
RED, BLUE, GOLDG, GREEN, PURPLE = "c04531", "396b7c", "e4aa60", "5c8f51", "564888"
FRONT, BACK, UP = Vector((0, 1, 0)), Vector((0, -1, 0)), Vector((0, 0, 1))

b = Builder("Village_Frontispiece")

def face(pts, want, key, col=None):
    fs = b.add([Vector(p) for p in pts], [tuple(range(len(pts)))])
    for f in fs:
        f.normal_update()
        if f.normal.dot(want) < 0: f.normal_flip()
    b.paint(fs, key, col)
    return fs

def solid_xz(outline, y0, y1, key, col=None):
    """A slab from y0 (back) to y1 (front) with an (x, z) outline."""
    area = sum(outline[i][0] * outline[(i + 1) % len(outline)][1] - outline[(i + 1) % len(outline)][0] * outline[i][1] for i in range(len(outline)))
    pts = outline if area > 0 else outline[::-1]          # counter-clockwise in x/z
    m = len(pts)
    vs = [Vector((x, y0, z)) for x, z in pts] + [Vector((x, y1, z)) for x, z in pts]
    idx = [tuple(range(m)), tuple(reversed(range(m, 2 * m)))] + [(k, m + k, m + (k + 1) % m, (k + 1) % m) for k in range(m)]
    fs = b.add(vs, idx)
    for f in fs: f.normal_update()
    b.paint(fs, key, col)
    return fs

def bar(a, c, w, y0, y1, col):
    """A straight bar of width w from point a to c in the wall plane."""
    a, c = Vector((a[0], 0, a[1])), Vector((c[0], 0, c[1]))
    d = (c - a).normalized(); s = Vector((-d.z, 0, d.x)) * (w / 2); e = d * (w / 2)
    solid_xz([((a - s - e).x, (a - s - e).z), ((c - s + e).x, (c - s + e).z), ((c + s + e).x, (c + s + e).z), ((a + s - e).x, (a + s - e).z)], y0, y1, "PAL", col)

def pointed_arch(xl, xr, zs, k=0.8, n=8):
    """Points of a pointed arch from the left springing over the apex to the right springing."""
    w = xr - xl; R = w * k; mid = (xl + xr) / 2
    cl = xl + R          # the left arc starts at the left springing; its centre lies a radius to the right
    ta = math.acos((mid - cl) / R)
    left = [(cl + R * math.cos(math.pi - (math.pi - ta) * i / n), zs + R * math.sin(math.pi - (math.pi - ta) * i / n)) for i in range(n + 1)]
    right = [(2 * mid - x, z) for x, z in reversed(left[:-1])]
    return left + right

# ---------------------------------------------------------------- wall, gable
GH = 3.4
solid_xz([(X0, H), (X1, H), (CX, H + GH)], BY, F - 0.05, "WALL")
slope_len = math.hypot(GH, CX - X0)
for sgn in (-1, 1):
    a = (CX + sgn * (CX + 0.15), H - 0.1); c = (CX, H + GH + 0.15)
    bar(a, c, 0.34, BY - 0.08, F + 0.12, STONE)
solid_xz([(X0 + 0.5, H - 0.32), (X1 - 0.5, H - 0.32), (X1 - 0.5, H), (X0 + 0.5, H)], F, F + 0.25, "PAL", STONE)   # cornice
# round window in the gable
def disc(y, cz, r0, r1, n, cols, key, want, cx=CX, phase=0.0):
    for i in range(n):
        a0 = phase + 2 * math.pi * i / n; a1 = phase + 2 * math.pi * (i + 1) / n
        p = lambda r, a: (cx + math.cos(a) * r, y, cz + math.sin(a) * r)
        pts = [p(0, 0), p(r1, a0), p(r1, a1)] if r0 <= 1e-4 else [p(r0, a0), p(r1, a0), p(r1, a1), p(r0, a1)]
        face(pts, want, key, cols[i % len(cols)])
def tube(cz, r, y0, y1, n, col, inward=False, cx=CX):
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n; am = (a0 + a1) / 2
        p = lambda a, y: (cx + math.cos(a) * r, y, cz + math.sin(a) * r)
        want = Vector((math.cos(am), 0, math.sin(am))) * (-1 if inward else 1)
        face([p(a0, y0), p(a1, y0), p(a1, y1), p(a0, y1)], want, "PAL", col)
OC = H + 1.25
disc(F - 0.02, OC, 0.0, 0.45, 8, [BLUE, GOLDG], "GLASS", FRONT, phase=math.pi / 8)
disc(BY - 0.02, OC, 0.0, 0.45, 8, [BLUE, GOLDG], "GLASS", BACK, phase=math.pi / 8)
disc(F + 0.12, OC, 0.45, 0.64, 12, [STONE], "PAL", FRONT)
tube(OC, 0.64, F - 0.05, F + 0.12, 12, STONE)
tube(OC, 0.45, F - 0.05, F + 0.12, 12, STONE_D, inward=True)
# gold finial: an orb with a fleur of three points
b.sphere((CX, 0.15, H + GH + 0.4), 0.22, "PAL", GOLD, seg=8, rings=5)
b.prism((CX, 0.15, H + GH + 0.55), 0.08, 1.0, 6, "PAL", GOLD, r_top=0.0)
for sgn in (-1, 1):
    solid_xz([(CX, H + GH + 0.6), (CX + sgn * 0.45, H + GH + 1.05), (CX + sgn * 0.14, H + GH + 0.82)], 0.1, 0.2, "PAL", GOLD)

# ---------------------------------------------------------------- buttresses and pinnacles
for xl, xr in ((X0, X0 + 0.62), (X1 - 0.62, X1)):
    xc = (xl + xr) / 2
    steps = [(-1.0, 5.0, F + 0.55), (5.0, 10.0, F + 0.42), (10.0, H + 0.6, F + 0.3)]
    for z0, z1, yf in steps:
        b.box((xc, (F + yf) / 2, (z0 + z1) / 2), (xr - xl, yf - F + 0.02, z1 - z0), "WALLD", skip=("back",))
    for (z0, z1, yf), nxt in zip(steps[:-1], steps[1:]):
        # sloped stone set-off where the buttress steps back
        y_low, y_high = yf, nxt[2]
        b.quad_box([(xl - 0.02, F, z1), (xr + 0.02, F, z1), (xr + 0.02, y_low + 0.04, z1), (xl - 0.02, y_low + 0.04, z1)],
                   [(xl - 0.02, F, z1 + 0.42), (xr + 0.02, F, z1 + 0.42), (xr + 0.02, y_high + 0.02, z1 + 0.42), (xl - 0.02, y_high + 0.02, z1 + 0.42)], "PAL", STONE)
    b.box((xc, F + 0.3, 0.35), (xr - xl + 0.16, 0.7, 0.7), "PAL", STONE, skip=("back",))          # plinth
    py = F + 0.15
    b.box((xc, py, H + 0.72), (0.74, 0.5, 0.24), "PAL", STONE)
    b.box((xc, py, H + 1.3), (0.5, 0.36, 1.0), "PAL", STONE)
    b.prism((xc, py, H + 1.8), 0.4, 1.9, 4, "ROOF", r_top=0.0, rot=math.pi / 4)
    b.sphere((xc, py, H + 3.8), 0.11, "PAL", GOLD, seg=6, rings=4)

# ---------------------------------------------------------------- portal
PXL, PXR, PZS = 1.25, 3.75, 3.1
inner = pointed_arch(PXL, PXR, PZS)
apex = max(z for x, z in inner)
# the wall in three pieces round the opening, so the doors sit in a real recess
solid_xz([(X0, -1.0), (PXL, -1.0), (PXL, H), (X0, H)], BY, F, "WALL")
solid_xz([(PXR, -1.0), (X1, -1.0), (X1, H), (PXR, H)], BY, F, "WALL")
solid_xz(inner + [(PXR, H), (PXL, H)], BY, F, "WALL")
# doors: two leaves filling the arch, planks, iron straps, gold rings; on the inside face as well
mid_i = len(inner) // 2
left_leaf = [(PXL, 0.0), (CX, 0.0)] + [(CX, apex)] + [p for p in inner[:mid_i]][::-1]
right_leaf = [(CX, 0.0), (PXR, 0.0)] + inner[mid_i + 1:][::-1] + [(CX, apex)]
solid_xz(left_leaf, BY, F - 0.12, "PAL", WOOD)
solid_xz(right_leaf, BY, F - 0.12, "PAL", WOOD)
for x in (CX - 0.62, CX + 0.62):
    solid_xz([(x - 0.02, 0.0), (x + 0.02, 0.0), (x + 0.02, apex - 0.6), (x - 0.02, apex - 0.6)], F - 0.12, F - 0.09, "PAL", WOOD_D)
solid_xz([(CX - 0.03, 0.0), (CX + 0.03, 0.0), (CX + 0.03, apex), (CX - 0.03, apex)], F - 0.12, F - 0.08, "PAL", WOOD_D)
for z in (0.7, 2.0, 3.3):
    for x0, x1 in ((PXL + 0.05, CX - 0.12), (CX + 0.12, PXR - 0.05)):
        solid_xz([(x0, z), (x1, z), (x1, z + 0.12), (x0, z + 0.12)], F - 0.12, F - 0.07, "PAL", IRON)
for x in (CX - 0.3, CX + 0.3):
    b.sphere((x, F - 0.05, 1.5), 0.09, "PAL", GOLD, seg=6, rings=4)
# reveal: the soffit and jambs between the doors and the wall face
def strip(pts_a, pts_b, col, key="PAL"):
    for i in range(len(pts_a) - 1):
        a0, a1, b0, b1 = pts_a[i], pts_a[i + 1], pts_b[i], pts_b[i + 1]
        n = (Vector(a1) - Vector(a0)).cross(Vector(b0) - Vector(a0))
        face([a0, a1, b1, b0], n, key, col)
rev = [(PXL, 0.0)] + inner + [(PXR, 0.0)]
for i in range(len(rev) - 1):
    (ax, az), (cx_, cz) = rev[i], rev[i + 1]
    mx, mz = (ax + cx_) / 2, (az + cz) / 2
    if mz <= PZS + 1e-3: want = Vector((1 if mx < CX else -1, 0, 0))          # jambs face the opening
    else: want = Vector((CX - mx, 0, (PZS - 1.0) - mz)).normalized()          # soffit faces down and in
    face([(ax, F - 0.12, az), (cx_, F - 0.12, cz), (cx_, F, cz), (ax, F, az)], want, "PAL", STONE_D)
# archivolts: three stepped bands, each further out and further forward, on jamb shafts to the ground
for j, (d0, d1, yb) in enumerate(((0.0, 0.2, 0.12), (0.2, 0.42, 0.22), (0.42, 0.62, 0.32))):
    # keep the same arc centres: the radius grows by the offset
    R0 = (PXR - PXL) * 0.8
    a_in = pointed_arch(PXL - d0, PXR + d0, PZS, k=(R0 + d0) / (PXR - PXL + 2 * d0))
    a_out = pointed_arch(PXL - d1, PXR + d1, PZS, k=(R0 + d1) / (PXR - PXL + 2 * d1))
    yf = F + yb
    col = STONE if j != 1 else STONE_D
    for i in range(len(a_in) - 1):
        q = [(a_in[i][0], yf, a_in[i][1]), (a_in[i + 1][0], yf, a_in[i + 1][1]), (a_out[i + 1][0], yf, a_out[i + 1][1]), (a_out[i][0], yf, a_out[i][1])]
        face(q, FRONT, "PAL", col)
        mx = (a_out[i][0] + a_out[i + 1][0]) / 2; mz = (a_out[i][1] + a_out[i + 1][1]) / 2
        out_n = Vector((mx - CX, 0, mz - (PZS - 1.0))).normalized()
        face([(a_out[i][0], F, a_out[i][1]), (a_out[i + 1][0], F, a_out[i + 1][1]), (a_out[i + 1][0], yf, a_out[i + 1][1]), (a_out[i][0], yf, a_out[i][1])], out_n, "PAL", col)
        face([(a_in[i][0], F, a_in[i][1]), (a_in[i + 1][0], F, a_in[i + 1][1]), (a_in[i + 1][0], yf, a_in[i + 1][1]), (a_in[i][0], yf, a_in[i][1])], -out_n, "PAL", STONE_D)
    for xa, xb in ((PXL - d1, PXL - d0), (PXR + d0, PXR + d1)):
        solid_xz([(xa, 0.0), (xb, 0.0), (xb, PZS), (xa, PZS)], F, yf, "PAL", col)
    # little capitals at the springing
    for xa, xb in ((PXL - d1 - 0.04, PXL - d0 + 0.04), (PXR + d0 - 0.04, PXR + d1 + 0.04)):
        solid_xz([(xa, PZS - 0.18), (xb, PZS - 0.18), (xb, PZS), (xa, PZS)], F, yf + 0.06, "PAL", GOLD if j == 0 else STONE)
# steps up to the doors
for s in range(3):
    depth = 1.35 - s * 0.45
    w = 3.6 - s * 0.3
    b.box((CX, F + depth / 2, (s + 1) * 0.15 / 2), (w, depth, (s + 1) * 0.15), "PAL", STONE if s % 2 == 0 else STONE_D, skip=("back", "bottom"))
# gablet over the portal, with crockets of gold and a small finial
GZ0, GZ1 = apex + 0.55, apex + 1.75
yg0, yg1 = F + 0.02, F + 0.42
solid_xz([(PXL - 0.75, GZ0 - 0.25), (PXR + 0.75, GZ0 - 0.25), (CX, GZ1)], F, yg0 + 0.05, "WALL")
for sgn in (-1, 1):
    bar((CX + sgn * (CX - PXL + 0.85), GZ0 - 0.35), (CX, GZ1 + 0.12), 0.22, F, yg1, STONE)
b.sphere((CX, yg1 - 0.1, GZ1 + 0.3), 0.14, "PAL", GOLD, seg=6, rings=4)
b.prism((CX, yg1 - 0.1, GZ1 + 0.38), 0.05, 0.5, 6, "PAL", GOLD, r_top=0.0)
# a shield on the gablet: blue with a gold bar
solid_xz([(CX - 0.32, GZ0 + 0.35), (CX + 0.32, GZ0 + 0.35), (CX + 0.32, GZ0 + 0.85), (CX, GZ0 + 1.05), (CX - 0.32, GZ0 + 0.85)], yg0 + 0.05, yg0 + 0.11, "PAL", BLUE)
solid_xz([(CX - 0.06, GZ0 + 0.4), (CX + 0.06, GZ0 + 0.4), (CX + 0.06, GZ0 + 0.98), (CX - 0.06, GZ0 + 0.98)], yg0 + 0.11, yg0 + 0.14, "PAL", GOLD)

# ---------------------------------------------------------------- lancets
def lancet(xc, z0, w, h, cols):
    hw = w / 2
    arch = pointed_arch(xc - hw, xc + hw, z0 + h - hw * 1.5, k=0.85, n=4)
    outline = [(xc - hw, z0), (xc + hw, z0)] + arch[::-1]
    split = z0 + h - hw * 1.5
    lo = [(xc - hw, z0), (xc + hw, z0), (xc + hw, split), (xc - hw, split)]
    for y, want in ((F + 0.02, FRONT), (BY - 0.02, BACK)):
        face([(x, y, z) for x, z in lo], want, "GLASS", cols[0])
        face([(x, y, z) for x, z in arch], want, "GLASS", cols[1])
    for i in range(len(outline)):
        bar(outline[i], outline[(i + 1) % len(outline)], 0.11, F, F + 0.12, GOLD)
    bar((xc - hw, split), (xc + hw, split), 0.08, F, F + 0.1, GOLD)
    solid_xz([(xc - hw - 0.16, z0 - 0.2), (xc + hw + 0.16, z0 - 0.2), (xc + hw + 0.16, z0 - 0.05), (xc - hw - 0.16, z0 - 0.05)], F, F + 0.28, "PAL", STONE)
LZ = GZ1 + 0.4
lancet(CX - 1.1, LZ, 0.6, 1.9, [BLUE, RED])
lancet(CX, LZ, 0.7, 2.3, [RED, GOLDG])
lancet(CX + 1.1, LZ, 0.6, 1.9, [BLUE, RED])

# ---------------------------------------------------------------- rose
RR = 1.6
RC = LZ + 2.3 + 0.35 + RR + 0.4
N = 16
for y, want in ((F + 0.02, FRONT), (BY - 0.02, BACK)):
    disc(y, RC, 0.0, 0.46, 8, [GOLDG], "GLASS", want)
    disc(y, RC, 0.46, 1.06, 8, [RED, BLUE], "GLASS", want, phase=math.pi / 8)
    disc(y, RC, 1.06, RR, N, [BLUE, GOLDG, RED, BLUE, GREEN, GOLDG, PURPLE, RED], "GLASS", want)
for y0 in (F, BY - 0.1):
    y1 = y0 + 0.1
    for i in range(8):
        a = math.pi / 8 + 2 * math.pi * i / 8
        bar((CX + math.cos(a) * 0.44, RC + math.sin(a) * 0.44), (CX + math.cos(a) * 1.08, RC + math.sin(a) * 1.08), 0.09, y0, y1, STONE)
    for i in range(N):
        a = 2 * math.pi * i / N
        bar((CX + math.cos(a) * 1.04, RC + math.sin(a) * 1.04), (CX + math.cos(a) * RR, RC + math.sin(a) * RR), 0.07, y0, y1, STONE)
    want = FRONT if y0 == F else BACK
    for r in (0.46, 1.06):
        disc(y1 if want == FRONT else y0, RC, r - 0.05, r + 0.05, N, [STONE], "PAL", want)
# moulded ring: face, outer and inner edges, a gold bead, keystones
disc(F + 0.3, RC, RR - 0.02, RR + 0.4, 24, [STONE], "PAL", FRONT)
tube(RC, RR + 0.4, F, F + 0.3, 24, STONE)
tube(RC, RR - 0.02, F, F + 0.3, 24, STONE_D, inward=True)
disc(F + 0.31, RC, RR + 0.16, RR + 0.24, 24, [GOLD], "PAL", FRONT)
for i in range(4):
    a = math.pi / 2 * i
    bar((CX + math.cos(a) * (RR - 0.05), RC + math.sin(a) * (RR - 0.05)), (CX + math.cos(a) * (RR + 0.5), RC + math.sin(a) * (RR + 0.5)), 0.32, F + 0.3, F + 0.38, STONE)
print("NOTE apex %.2f gablet %.2f-%.2f lancets %.2f rose centre %.2f top %.2f" % (apex, GZ0, GZ1, LZ, RC, RC + RR + 0.5))

front = b.finish(recalc=False)
export_fbx([front], "Village_Frontispiece")

# context: the kit walls either side and the battlements (Blender x is mirrored from Unity)
ctx = []
def kit(name, loc):
    for o in import_fbx(KIT + "Models/SM_Bld_Castle_" + name + ".fbx"):
        if o.type != 'MESH': continue
        o.location = Vector(loc)
        o.data.materials.clear(); o.data.materials.append(MAT["PAL"] if "Glass" in o.name else MAT["WALL"])
        ctx.append(o)
for dx in (5, -5):
    kit("Wall_Window_Fancy_01", (dx, 0, 0)); kit("Wall_Window_Fancy_01", (dx, 0, 5)); kit("Wall_Window_02", (dx, 0, 10)); kit("Battlements_01", (dx, 0, 15))
preview([front] + ctx, "frontispiece", views=((180, 4), (215, 18)), res=(1000, 1200), dist_mul=0.9)
