# Tier 3 clock and bell tower. It replaces the small tower of SM_Bld_Preset_House_10 and is modelled in
# that house's own space (Blender axes, the house front faces +y), so in Unity it is parented to the
# house at the origin. The Unity side trims the old tower off a copy of the house mesh; the stub left
# between the ridge and z 10.2 ends up inside this shaft.
#
# From the bottom: brick shaft with stone corner quoins, a clock on all four sides, a stone cornice,
# an open timber belfry with a gold bell, a blue tiled spire with dormers, a gold finial and a pennant.
CX, CY = -2.5, 12.45      # old tower centre
Z0 = 8.0                  # shaft starts inside the roof
ZS = 12.6                 # top of the shaft
W = 3.0                   # shaft width

STONE, STONE_D, WOOD, WOOD_D = "b39d80", "8b8b8b", "8a572e", "633d1a"
GOLD, BELL, WHITE, IRON, BLUE = "d1a44c", "bda248", "f7f2f0", "191a1b", "396b7c"

b = Builder("Village_ClockTower")
c = lambda x, y, z: (CX + x, CY + y, z)

# shaft, open at the top (the cornice covers it); the bottom shows where it overhangs the front bay
b.box(c(0, 0, (Z0 + ZS) / 2), (W, W, ZS - Z0), "WALL", skip=("top",))
b.box(c(0, 0, Z0 + 0.12), (W + 0.16, W + 0.16, 0.24), "PAL", STONE)
# corner quoins: alternating long and short stones up each corner
q = 0.42
for i, z in enumerate([Z0 + 0.5 + k * 0.55 for k in range(int((ZS - Z0 - 0.6) / 0.55))]):
    long_side = 0.62 if i % 2 == 0 else 0.4
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(c(sx * (W / 2 - long_side / 2 + 0.05), sy * (W / 2 + 0.02), z), (long_side, q * 0.25, 0.42), "PAL", STONE)
            b.box(c(sx * (W / 2 + 0.02), sy * (W / 2 - (1.02 - long_side) / 2 + 0.05), z), (q * 0.25, 1.02 - long_side, 0.42), "PAL", STONE)
# string course under the clocks
b.box(c(0, 0, 10.25), (W + 0.16, W + 0.16, 0.18), "PAL", STONE)

# clocks: a round dial in a square stone frame, gold ring, hour marks, hands at ten past ten
def clock(face):
    # face: rotation about z (0 = front, +y)
    R = Matrix.Rotation(face, 3, 'Z')
    zc = 11.45
    def put(local, size, col, rz=0.0):
        p = R @ Vector(local)
        sz = Vector(size)
        # size is given in face space (x along the face, y out of it, z up); swap for side faces
        if abs(math.sin(face)) > 0.5: sz = Vector((sz.y, sz.x, sz.z))
        b.box(c(p.x, p.y, local[2]), tuple(sz), "PAL", col, rot_z=rz)
    out = W / 2
    put((0, out + 0.04, zc), (1.95, 0.08, 1.95), STONE)                 # square frame
    # dial as a 12-sided disc facing out, built from a prism laid on its side
    n = 12; rr = 0.78
    dial_c = R @ Vector((0, out + 0.1, 0))
    ring = []
    for i in range(n):
        a = 2 * math.pi * i / n + math.pi / n
        ring.append(Vector((math.cos(a), 0, math.sin(a))))
    def disc(radius, depth, col):
        vs = []
        for d in (0, depth):
            for r in ring:
                p = R @ Vector((r.x * radius, d, 0)) + dial_c
                vs.append((CX + p.x, CY + p.y, zc + r.z * radius))
        idx = [tuple(range(n, 2 * n)), tuple(reversed(range(n)))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
        fs = b.add(vs, idx)
        for f in fs: f.normal_update()
        b.paint(fs, "PAL", col)
    disc(0.9, 0.06, GOLD)
    dial_c = R @ Vector((0, out + 0.16, 0))
    disc(0.76, 0.03, WHITE)
    for h in range(12):
        a = 2 * math.pi * h / 12
        big = h % 3 == 0
        p = R @ Vector((math.sin(a) * 0.62, out + 0.2, 0))
        put((p.x, p.y, zc + math.cos(a) * 0.62), (0.07 if not big else 0.1, 0.03, 0.16 if big else 0.1), IRON)
    # hands: hour at ten, minute at two (ten past ten), pivot cap in gold
    for ang, length, wd in ((-60, 0.4, 0.09), (60, 0.62, 0.06)):
        a = math.radians(ang)
        mid = R @ Vector((math.sin(a) * length / 2, out + 0.23, 0))
        hv = Vector((math.sin(a), 0, math.cos(a)))
        # a thin plank from the centre outwards, tilted in the face plane
        sx = R @ Vector((wd / 2, 0, 0)); sy = R @ Vector((0, 0.02, 0))
        dirv = R @ Vector((hv.x * length / 2, 0, 0))
        cen = Vector((CX + mid.x, CY + mid.y, zc + hv.z * length / 2))
        up = Vector((dirv.x, dirv.y, hv.z * length / 2))
        side = R @ Vector((math.cos(a) * wd / 2, 0, 0)) + Vector((0, 0, -math.sin(a) * wd / 2))
        vs = [cen - up - side - sy, cen - up + side - sy, cen + up + side - sy, cen + up - side - sy,
              cen - up - side + sy, cen - up + side + sy, cen + up + side + sy, cen + up - side + sy]
        fs = b.add(vs, [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)])
        for f in fs: f.normal_update()
        b.paint(fs, "PAL", IRON)
    put((R @ Vector((0, out + 0.27, 0))).to_tuple()[:2] + (zc,), (0.12, 0.04, 0.12), GOLD)
for face in (0, math.pi / 2, math.pi, -math.pi / 2):
    clock(face)

# cornice: two stepped stone slabs
b.box(c(0, 0, ZS + 0.1), (W + 0.3, W + 0.3, 0.2), "PAL", STONE)
b.box(c(0, 0, ZS + 0.3), (W + 0.55, W + 0.55, 0.2), "PAL", STONE)
ZB = ZS + 0.4             # belfry floor
HB = 2.5                  # belfry height
WB = W + 0.3
# belfry floor seen through the openings
b.box(c(0, 0, ZB + 0.05), (WB - 0.1, WB - 0.1, 0.1), "PAL", WOOD_D)
# corner posts and mid posts
for sx in (-1, 1):
    for sy in (-1, 1):
        b.box(c(sx * (WB / 2 - 0.2), sy * (WB / 2 - 0.2), ZB + HB / 2), (0.36, 0.36, HB), "PAL", WOOD_D)
# arched look: diagonal braces in each opening corner, rails with balusters at the bottom
for face in range(4):
    R = Matrix.Rotation(face * math.pi / 2, 3, 'Z')
    def fb(local, size, col):
        p = R @ Vector(local)
        sz = size if face % 2 == 0 else (size[1], size[0], size[2])
        b.box(c(p.x, p.y, local[2]), sz, "PAL", col)
    o = WB / 2 - 0.2
    fb((0, o, ZB + 0.75), (WB - 0.5, 0.14, 0.12), WOOD)        # rail
    fb((0, o, ZB + 0.12), (WB - 0.5, 0.18, 0.16), WOOD_D)      # sill
    for i in range(7):
        fb((-1.2 + i * 0.4, o, ZB + 0.43), (0.08, 0.08, 0.55), WOOD)
    fb((0, o, ZB + HB - 0.15), (WB, 0.3, 0.3), WOOD_D)         # top beam
    # knee braces under the top beam
    for sx in (-1, 1):
        R2 = R @ Matrix.Rotation(sx * math.radians(45), 3, 'Y')
        p = R @ Vector((sx * (o - 0.45), o, 0))
        brace = Vector((0.12, 0.12, 0.8))
        vs = []
        for dz in (-0.4, 0.4):
            for dx, dy in ((-0.06, -0.06), (0.06, -0.06), (0.06, 0.06), (-0.06, 0.06)):
                v = R2 @ Vector((dx, dy, dz))
                vs.append((CX + p.x + v.x, CY + p.y + v.y, ZB + HB - 0.6 + v.z))
        fs = b.add(vs, [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)])
        for f in fs: f.normal_update()
        b.paint(fs, "PAL", WOOD)
# the bell on a yoke beam
b.box(c(0, 0, ZB + HB - 0.45), (WB - 0.4, 0.22, 0.22), "PAL", WOOD_D)
b.prism(c(0, 0, ZB + 0.75), 0.72, 0.18, 12, "PAL", BELL, r_top=0.66)                 # lip
b.prism(c(0, 0, ZB + 0.93), 0.66, 0.85, 12, "PAL", BELL, r_top=0.42)                 # waist
b.prism(c(0, 0, ZB + 1.78), 0.42, 0.18, 12, "PAL", BELL, r_top=0.25)                 # shoulder
b.prism(c(0, 0, ZB + 1.96), 0.1, 0.2, 6, "PAL", IRON)                                # hanger
b.sphere(c(0, 0, ZB + 0.62), 0.12, "PAL", IRON, seg=6, rings=4)                      # clapper

# spire: eave slab, a four sided roof turned to the shaft, little dormers, finial, pennant
ZR = ZB + HB
b.box(c(0, 0, ZR + 0.08), (WB + 0.5, WB + 0.5, 0.16), "PAL", WOOD_D)
RH = 4.4
half = (WB + 0.9) / 2
b.prism(c(0, 0, ZR + 0.16), half * math.sqrt(2), RH, 4, "ROOF", r_top=0.0, rot=math.pi / 4)
# a ridge trim: thin gold flashing at the tip
b.prism(c(0, 0, ZR + 0.16 + RH * 0.86), half * math.sqrt(2) * 0.15, RH * 0.14, 4, "PAL", GOLD, r_top=0.0, rot=math.pi / 4)
for face in range(4):
    R = Matrix.Rotation(face * math.pi / 2, 3, 'Z')
    p = R @ Vector((0, half * 0.62, 0))
    zd = ZR + 0.16 + RH * 0.3
    sz = (0.55, 0.5, 0.6) if face % 2 == 0 else (0.5, 0.55, 0.6)
    b.box(c(p.x, p.y, zd + 0.3), sz, "PAL", WHITE)
    p2 = R @ Vector((0, half * 0.62 + 0.26, 0))
    b.box(c(p2.x, p2.y, zd + 0.28), (0.3, 0.04, 0.36) if face % 2 == 0 else (0.04, 0.3, 0.36), "PAL", IRON)
    b.prism(c(p.x, p.y, zd + 0.6), 0.48, 0.45, 4, "ROOF", r_top=0.0, rot=math.pi / 4)
ZT = ZR + 0.16 + RH
b.sphere(c(0, 0, ZT + 0.12), 0.22, "PAL", GOLD, seg=8, rings=5)
b.prism(c(0, 0, ZT + 0.3), 0.05, 1.4, 6, "PAL", GOLD)
b.sphere(c(0, 0, ZT + 1.0), 0.12, "PAL", GOLD, seg=6, rings=4)
b.prism(c(0, 0, ZT + 1.7), 0.08, 0.3, 6, "PAL", GOLD, r_top=0.0)
# pennant: a double sided swallowtail flag in Synty blue, flying east
fl = [c(0.05, 0, ZT + 1.62), c(1.3, 0, ZT + 1.5), c(1.0, 0, ZT + 1.33), c(1.3, 0, ZT + 1.16), c(0.05, 0, ZT + 1.1)]
fs = b.add(fl, [(0, 1, 2, 4), (2, 3, 4), (4, 2, 1, 0), (4, 3, 2)])
b.paint(fs, "PAL", BLUE)

tower = b.finish()

# reference house, its old tower trimmed the same way the Unity side does it
house = [o for o in import_fbx(KIT + "Models/SM_Bld_Preset_House_10_Optimized.fbx") if o.type == 'MESH']
main = [o for o in house if o.name.endswith("House_10_Optimized")][0]
bm = bmesh.new(); bm.from_mesh(main.data); mw = main.matrix_world
cut = [f for f in bm.faces if all((mw @ v.co).z > 10.2 for v in f.verts)]
bmesh.ops.delete(bm, geom=cut, context='FACES'); bm.to_mesh(main.data); bm.free()
pal_house = bpy.data.materials.new("house_preview"); pal_house.use_nodes = True
img = bpy.data.images.load(KIT + "Textures/PolygonFantasyKingdom_Texture_04_C.png", check_existing=True) if os.path.exists(KIT + "Textures/PolygonFantasyKingdom_Texture_04_C.png") else _pal_img
t = pal_house.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img; t.interpolation = 'Closest'
pal_house.node_tree.links.new(t.outputs[0], pal_house.node_tree.nodes["Principled BSDF"].inputs[0])
for o in house:
    o.data.materials.clear(); o.data.materials.append(pal_house)
print("NOTE cut faces", len(cut))

export_fbx([tower], "Village_ClockTower")
preview([tower] + house, "clocktower", views=((200, 18), (150, 30)), res=(900, 1100), dist_mul=1.0)
preview([tower], "clocktower_close", views=((180, 8),), res=(900, 1100), dist_mul=1.1)
