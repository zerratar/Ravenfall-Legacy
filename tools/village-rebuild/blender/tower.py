# Round castle towers for the town hall tiers, replacing the kit's stacked tower segments (which have a
# doorway on every level). One model per use, standing on the origin, with the kit's castle brick wrapped
# round them and the kit roof tiles on the roofs.
#
# Plinth, string courses every 5 m, staggered windows (arrow slits low down, warm lit arched windows
# higher up), corbels and a crenellated parapet, then a cone, onion or needle roof with a gold finial and a
# pennant. The turret is a corbelled bartizan that sits on a wall top.
STONE, STONE_D, GOLD, DARK, BLUE, WARM = "a4a29e", "787877", "d1a44c", "191a1b", "396b7c", "e4aa60"

def facet_frame(cx, cy, r, n, k):
    """Origin, tangent and outward normal of facet k of an n sided lathe (facets sit between vertices)."""
    ac = (k + 0.5) * 2 * math.pi / n
    ap = r * math.cos(math.pi / n)
    O = Vector((cx + math.cos(ac) * ap, cy + math.sin(ac) * ap, 0))
    return O, Vector((-math.sin(ac), math.cos(ac), 0)), Vector((math.cos(ac), math.sin(ac), 0)), ac

def plate(b, O, T, N, outline, w0, w1, key, col):
    """Extrudes a (u, v) outline on a facet from depth w0 to w1 off the wall. Outline is counter-clockwise
    seen from outside."""
    m = len(outline)
    P = lambda u, v, w: O + T * u + Vector((0, 0, v)) + N * w
    vs = [P(u, v, w0) for u, v in outline] + [P(u, v, w1) for u, v in outline]
    idx = [tuple(range(m, 2 * m)), tuple(reversed(range(m)))] + [(k, (k + 1) % m, m + (k + 1) % m, m + k) for k in range(m)]
    fs = b.add(vs, idx)
    for f in fs: f.normal_update()
    b.paint(fs, key, col)

def arch_outline(w, h, z0, pts=4):
    """Round headed window: rectangle with a semicircular top, counter-clockwise seen from outside."""
    hw = w / 2; zs = z0 + h - hw
    out = [(-hw, z0), (hw, z0)]
    for i in range(pts + 1):
        a = math.pi * i / pts
        out.append((math.cos(a) * hw, zs + math.sin(a) * hw))
    # seen from outside +u points right (along T), so this runs counter-clockwise
    return out

def window(b, cx, cy, r, n, k, z0, kind):
    O, T, N, ac = facet_frame(cx, cy, r, n, k)
    if kind == "slit":
        plate(b, O, T, N, arch_outline(0.34, 1.1, z0 - 0.05, 3), -0.02, 0.05, "PAL", STONE)
        plate(b, O, T, N, [(-0.07, z0), (0.07, z0), (0.07, z0 + 0.95), (-0.07, z0 + 0.95)], 0.05, 0.07, "PAL", DARK)
    else:
        wd, ht = (0.5, 1.3) if kind == "arch" else (0.62, 1.6)
        plate(b, O, T, N, arch_outline(wd + 0.22, ht + 0.12, z0 - 0.06), -0.02, 0.06, "PAL", STONE)
        plate(b, O, T, N, arch_outline(wd, ht, z0), 0.06, 0.08, "GLASS", WARM)
        plate(b, O, T, N, [(-wd / 2 - 0.16, z0 - 0.2), (wd / 2 + 0.16, z0 - 0.2), (wd / 2 + 0.16, z0 - 0.06), (-wd / 2 - 0.16, z0 - 0.06)], -0.02, 0.16, "PAL", STONE)
        # a mullion down the middle
        plate(b, O, T, N, [(-0.03, z0), (0.03, z0), (0.03, z0 + ht - 0.05), (-0.03, z0 + ht - 0.05)], 0.08, 0.1, "PAL", STONE_D)

def finial(b, cx, cy, z, scale=1.0):
    s = scale
    b.sphere((cx, cy, z + 0.15 * s), 0.22 * s, "PAL", GOLD, seg=8, rings=5)
    b.prism((cx, cy, z + 0.3 * s), 0.05 * s, 1.5 * s, 6, "PAL", GOLD)
    b.sphere((cx, cy, z + 1.05 * s), 0.11 * s, "PAL", GOLD, seg=6, rings=4)
    b.prism((cx, cy, z + 1.8 * s), 0.08 * s, 0.3 * s, 6, "PAL", GOLD, r_top=0.0)
    zt = z + 1.72 * s
    fl = [(cx + 0.05, cy, zt), (cx + 1.35 * s, cy, zt - 0.12 * s), (cx + 1.05 * s, cy, zt - 0.29 * s), (cx + 1.35 * s, cy, zt - 0.46 * s), (cx + 0.05, cy, zt - 0.52 * s)]
    fs = b.add(fl, [(0, 1, 2, 4), (2, 3, 4), (4, 2, 1, 0), (4, 3, 2)])
    b.paint(fs, "PAL", BLUE)

def make_tower(name, r, body_h, roof, n=16, windows=True):
    b = Builder(name)
    cx = cy = 0.0
    # plinth sinks 1 m into the ground so a slope never shows a gap; body above it
    b.lathe((cx, cy, 0), [(r + 0.3, -1.0), (r + 0.3, 0.5), (r, 1.0), (r, body_h)], n, "WALL", cyl_radius=r)
    levels = int(round(body_h / 5))
    for L in range(1, levels):
        z = 5.0 * L
        b.lathe((cx, cy, 0), [(r, z - 0.01), (r + 0.12, z), (r + 0.12, z + 0.24), (r, z + 0.25)], n, "PAL", STONE)
    if windows:
        for L in range(levels):
            kind = "slit" if L == 0 else ("big" if L == levels - 1 and levels > 2 else "arch")
            if r < 1.4 and kind != "slit": kind = "slit"
            for j in range(4):
                k = (j * (n // 4) + (L % 2) * (n // 8)) % n
                window(b, cx, cy, r, n, k, 5.0 * L + (1.6 if L == 0 else 1.7), kind)
    # corbels: a stepped ring and a row of machicolation blocks under the parapet
    bh = body_h
    rp = r + 0.45
    b.lathe((cx, cy, 0), [(r, bh - 0.6), (r + 0.2, bh - 0.6), (r + 0.2, bh - 0.3), (rp, bh)], n, "PAL", STONE)
    for k in range(n):
        a = (k + 0.5) * 2 * math.pi / n
        b.box((cx + math.cos(a) * (r + 0.2), cy + math.sin(a) * (r + 0.2), bh - 0.85), (0.28, 0.42, 0.5), "PAL", STONE_D, rot_z=a + math.pi / 2)
    # parapet: outer and inner faces, a walkway, and merlons on top
    b.lathe((cx, cy, 0), [(rp, bh), (rp, bh + 0.7)], n, "WALL", cyl_radius=rp)
    b.lathe((cx, cy, 0), [(rp - 0.3, bh + 0.7), (rp - 0.3, bh + 0.25)], n, "WALL", cyl_radius=rp)      # inner, faces in
    b.lathe((cx, cy, 0), [(rp, bh + 0.7), (rp - 0.3, bh + 0.7)], n, "PAL", STONE)                     # top, faces up
    b.lathe((cx, cy, 0), [(rp - 0.3, bh + 0.25), (0.0, bh + 0.25)], n, "PAL", STONE_D)                 # walkway
    nm = n // 2
    for k in range(nm):
        a = 2 * math.pi * k / nm
        b.box((cx + math.cos(a) * (rp - 0.15), cy + math.sin(a) * (rp - 0.15), bh + 1.05), (0.3, 2 * rp * math.sin(math.pi / nm) * 0.55, 0.7), "WALL", rot_z=a)
    top = bh + 0.25
    rr = rp - 0.45
    if roof == "cone":
        b.lathe((cx, cy, 0), [(rr * 0.98, top), (rr * 1.25, top + 1.0), (0, top + rr * 3.0)], n, "ROOF", cyl_radius=rr)
        finial(b, cx, cy, top + rr * 3.0 - 0.1)
    elif roof == "needle":
        b.lathe((cx, cy, 0), [(rr * 0.98, top), (rr * 1.2, top + 1.0), (rr * 0.5, top + rr * 3.2), (0, top + rr * 5.2)], n, "ROOF", cyl_radius=rr)
        finial(b, cx, cy, top + rr * 5.2 - 0.1, 1.3)
        # four little pinnacles on the parapet
        for k in range(4):
            a = math.pi / 4 + k * math.pi / 2
            px, py = cx + math.cos(a) * (rp - 0.15), cy + math.sin(a) * (rp - 0.15)
            b.box((px, py, bh + 1.2), (0.4, 0.4, 1.0), "PAL", STONE, rot_z=a)
            b.prism((px, py, bh + 1.7), 0.32, 1.3, 4, "ROOF", r_top=0.0, rot=a + math.pi / 4)
            b.sphere((px, py, bh + 3.05), 0.08, "PAL", GOLD, seg=6, rings=4)
    elif roof == "onion":
        rd = rr * 0.75
        b.lathe((cx, cy, 0), [(rd, top), (rd, top + 1.0)], n, "WALL", cyl_radius=rd)
        for k in range(4):
            O, T, N, ac = facet_frame(cx, cy, rd, n, k * n // 4)
            plate(b, O, T, N, arch_outline(0.3, 0.6, top + 0.2, 3), 0.0, 0.03, "PAL", DARK)
        prof = [(rd, top + 1.0), (rd * 1.35, top + 1.6), (rd * 1.45, top + 2.2), (rd * 1.25, top + 2.9), (rd * 0.75, top + 3.5),
                (rd * 0.32, top + 4.0), (rd * 0.16, top + 4.5), (0, top + 4.8)]
        b.lathe((cx, cy, 0), prof, n, "ROOF", cyl_radius=rd * 1.4)
        b.prism((cx, cy, top + 0.95), rd + 0.08, 0.12, n, "PAL", GOLD)                                # gold band at the drum top
        finial(b, cx, cy, top + 4.7)
    return b.finish(recalc=False)

def make_turret(name, r=1.0, body_h=2.8, n=12):
    b = Builder(name)
    # corbelled cone hanging under the turret, then the body, then a pepper-pot roof with an eave
    b.lathe((0, 0, 0), [(0, -1.8), (r * 0.55, -1.0), (r * 0.9, -0.35), (r, 0.0), (r, body_h)], n, "WALL", cyl_radius=r)
    b.lathe((0, 0, 0), [(r, -0.3), (r + 0.1, -0.3), (r + 0.1, -0.06), (r, -0.05)], n, "PAL", STONE)
    for j in range(3):
        window(b, 0, 0, r, n, j * 4 + 1, 0.7, "slit")
    b.lathe((0, 0, 0), [(r, body_h), (r + 0.28, body_h)], n, "PAL", STONE_D)          # eave underside, faces down
    b.lathe((0, 0, 0), [(r + 0.28, body_h), (r + 0.1, body_h + 0.5), (0, body_h + r * 3.0)], n, "ROOF", cyl_radius=r)
    finial(b, 0, 0, body_h + r * 3.0 - 0.1, 0.7)
    return b.finish(recalc=False)

made = [
    make_tower("Village_Tower_T4_Front", 1.9, 15, "onion"),
    make_tower("Village_Tower_T4_Back", 1.5, 10, "cone"),
    make_tower("Village_Tower_T5_Front", 2.0, 20, "onion"),
    make_tower("Village_Tower_T5_Back", 1.6, 20, "cone"),
    make_tower("Village_Tower_T5_Spire", 2.2, 30, "needle"),
    make_turret("Village_Turret"),
]
for o in made: export_fbx([o], o.name)
# line them up for one preview
x = 0.0
for o in made:
    o.location.x = x; x += 7.0
made[-1].location.z = 4.0
preview(made, "towers", views=((180, 8), (215, 22)), res=(1800, 1000), dist_mul=0.85)
