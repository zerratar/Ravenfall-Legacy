# Fisher's log (fishing): a fisher's hut, fish drying on a rack, a net hung on a frame with floats, an upturned
# rowboat with its oars, barrels of the catch, rods against the wall, and a tub with a rubber duck in it.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Fishing")
f = Frame()
FISH, FISH_B, NET, FLOAT, DUCK, BEAK = "b9b9b9", "5f8191", "3a2e2c", "e8862a", "e4aa60", "e8862a"

def fish_emblem(b, ef, z):
    emb(b, ef, z, [(-0.2, 0.0), (-0.08, 0.1), (0.1, 0.08), (0.16, 0.0), (0.1, -0.08), (-0.08, -0.1)], FISH_B, d=0.02)
    emb(b, ef, z, [(0.14, 0.0), (0.24, 0.09), (0.24, -0.09)], FISH_B, d=0.02)
    emb_circle(b, ef, z, -0.13, 0.02, 0.018, DARK, d=0.025, n=6)

def fish(b, c, along, up, length=0.42, col=FISH):
    """A flat fish hanging by its tail: body and tail as faces on both sides."""
    c = Vector(c); along = Vector(along).normalized(); up = Vector(up).normalized(); n = along.cross(up)
    P = lambda u, v: tuple(c + along * (u * length) + up * (v * length))
    body = [P(-0.5, 0.0), P(-0.25, 0.2), P(0.15, 0.18), P(0.32, 0.0), P(0.15, -0.18), P(-0.25, -0.2)]
    tail = [P(0.3, 0.0), P(0.5, 0.16), P(0.5, -0.16)]
    for s in (1, -1):
        b.face(body, n * s, "PAL", col); b.face(tail, n * s, "PAL", col)
    for s in (1, -1):
        b.sphere(tuple(Vector(P(-0.36, 0.05)) + n * 0.01 * s), 0.02, "PAL", DARK, seg=4, rings=3)

yard(b, f, 4.6, 4.4, DIRT, seed=14.0)

# the hut
hf = f.sub(2.1, -2.3, 0.0)
cottage(b, hf, 4.2, 3.4, 2.9, roof="SLAT_TEAL", pitch=42, overhang=0.42, plinth=0.4, shutters="blue",
        front_windows=(-1.0,), side_windows=(0.0,), back_windows=(0.0,), door_x=0.85, chimney=(-1.2, -0.6))
fw = hf.sub(0, 1.7, 0)
sign(b, fw, 2.15, 2.4, 0.0, fish_emblem)
for k, x in enumerate((-1.75, -1.6)):
    bar(b, fw.p(x, 0.35, 0.0), fw.p(x + 0.1, 0.18, 2.6), 0.035, 0.035, "PAL", TIMBER_L)            # rods
    b.box(fw.p(x + 0.1, 0.18, 2.45), (0.01, 0.01, 0.3), "PAL", WHITE, rot_z=fw.rot)

# the drying rack: two A-frames, two lines, fish hanging from them
rf = f.sub(-2.3, -2.4, 0.15)
for x in (-1.3, 1.3):
    for s in (-1, 1):
        bar(b, rf.p(x, s * 0.5, 0.0), rf.p(x, 0.0, 2.0), 0.1, 0.1, "PAL", TIMBER)
for y, z in ((-0.12, 1.8), (0.12, 1.55)):
    bar(b, rf.p(-1.4, y, z), rf.p(1.4, y, z), 0.04, 0.04, "PAL", "c6a177")
    for k in range(6):
        x = -1.0 + k * 0.4
        fish(b, rf.p(x, y, z - 0.26), (0, 0, 1), rf.R @ Vector((1, 0, 0)), length=0.42 - 0.04 * (k % 2), col=FISH if (k + int(z * 10)) % 2 else FISH_B)
# the net on its frame, with orange floats
nf = f.sub(-0.4, 1.0, -0.2)                     # out in the yard, clear of the hut window
for x in (-1.2, 1.2):
    b.box(nf.p(x, 0, 1.1), (0.12, 0.12, 2.2), "PAL", TIMBER, rot_z=nf.rot)
bar(b, nf.p(-1.35, 0, 2.15), nf.p(1.35, 0, 2.15), 0.1, 0.1, "PAL", TIMBER_L)
cols, rows = 8, 6
for i in range(cols + 1):
    x = -1.1 + 2.2 * i / cols
    pts = [nf.p(x, 0.06, 2.1 - 1.6 * j / rows - 0.12 * math.sin(math.pi * i / cols) * (j / rows)) for j in range(rows + 1)]
    for j in range(rows): bar(b, pts[j], pts[j + 1], 0.02, 0.02, "PAL", NET)
for j in range(rows + 1):
    pts = [nf.p(-1.1 + 2.2 * i / cols, 0.06, 2.1 - 1.6 * j / rows - 0.12 * math.sin(math.pi * i / cols) * (j / rows)) for i in range(cols + 1)]
    for i in range(cols): bar(b, pts[i], pts[i + 1], 0.02, 0.02, "PAL", NET)
for i in range(1, cols, 2):
    x = -1.1 + 2.2 * i / cols
    b.sphere(nf.p(x, 0.08, 0.5 - 0.12 * math.sin(math.pi * i / cols)), 0.07, "PAL", FLOAT, seg=5, rings=3)

# the rowboat, keel up, and its oars
hull(b, f, -1.8, 2.3, length=3.4, beam=1.25, depth=0.5, z=0.0, yaw=0.35, col="5f8191", upturned=True)
for s in (-1, 1):
    of = f.sub(0.2 + s * 0.15, 2.6, 1.3)
    bar(b, of.p(-1.0, 0, 0.06), of.p(0.6, 0, 0.06), 0.05, 0.05, "PAL", TIMBER_L)
    b.box(of.p(0.75, 0, 0.05), (0.42, 0.16, 0.03), "PAL", TIMBER_L, rot_z=of.rot)
# barrels of the catch, a bucket, the duck tub
for k, (x, y) in enumerate(((3.4, 0.4), (3.9, 1.0))):
    barrel(b, f, x, y)
    b.lathe(f.p(x, y, 0), [(0.27, 0.78), (0.0, 0.78)], 10, "PAL", TIMBER)
    for j in range(3):
        a = j * 2.1 + k
        fish(b, f.p(x + math.cos(a) * 0.1, y + math.sin(a) * 0.1, 0.92), (0, 0, -1), (math.cos(a + 1), math.sin(a + 1), 0), length=0.3, col=FISH if j % 2 else FISH_B)
tc = f.sub(2.6, 2.7, 0.0)
b.lathe(tc.p(0, 0, 0), [(0.55, 0.0), (0.62, 0.42), (0.55, 0.42), (0.5, 0.08)], 10, "PAL", TIMBER_L)
b.lathe(tc.p(0, 0, 0), [(0.58, 0.36), (0.0, 0.36)], 10, "GLASS", "396b7c")
for z in (0.1, 0.32):
    b.lathe(tc.p(0, 0, 0), [(0.57 + 0.05 * z, z - 0.03), (0.6 + 0.05 * z, z + 0.03)], 10, "PAL", DARK)
dk = tc.sub(0.1, 0.05, 0.6)
b.sphere(dk.p(0, 0, 0.42), 0.11, "PAL", DUCK, seg=6, rings=4, sz=0.75)
b.sphere(dk.p(0, 0.08, 0.53), 0.07, "PAL", DUCK, seg=6, rings=3)
b.prism(dk.p(0, 0.15, 0.52), 0.03, 0.03, 4, "PAL", BEAK, r_top=0.01)
for s in (-1, 1): b.sphere(dk.p(s * 0.035, 0.13, 0.56), 0.012, "PAL", DARK, seg=4, rings=3)
b.lathe(f.p(1.2, 3.4, 0), [(0.18, 0.0), (0.22, 0.32), (0.0, 0.32)], 8, "PAL", "8b8b8b")

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Fishing")
preview([house], "house_fishing", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
