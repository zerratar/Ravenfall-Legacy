# Shared parts for the village houses, built on common.py: timber framed cottages, windows, doors, signboards,
# yards and small props. A house script runs common.py, then this, then builds with these functions.
# Houses face +y (Unity -z). Every function takes a Builder first and places parts in world coordinates;
# Frame turns local coordinates (x along the front, y out of it, z up) into the world.
TIMBER, TIMBER_L, STONE, STONE_D, WARM, DARK, GOLD = "693b13", "8a572e", "8b8b8b", "696762", "e4aa60", "191a1b", "d1a44c"
DIRT_OLD, GRASS, ROCK, ROCK_D, IRON, WHITE = "9c907c", "666f4a", "787877", "5d5c5c", "3a2e2c", "f5f5f5"
DIRT = "7e7564"   # packed earth
SHUTTER = {"blue": "396b7c", "green": "4d7451", "red": "714242", "brown": "8e5a30"}

class Frame:
    def __init__(self, x=0.0, y=0.0, rot=0.0):
        self.o = Vector((x, y, 0)); self.rot = rot; self.R = Matrix.Rotation(rot, 3, 'Z')
    def p(self, x, y, z): return tuple(self.o + self.R @ Vector((x, y, z)))
    def sub(self, x, y, rot=0.0):
        o = self.o + self.R @ Vector((x, y, 0)); f = Frame(o.x, o.y, self.rot + rot); return f

# ---------------------------------------------------------------- Synty props
# Props the Synty packs already have (barrels, crates, dummies, racks, benches, trees and so on) are not modelled
# here: a house places an anchor, an empty, and houses_synty.cs (in the editor) puts the Synty prefab for its key
# there and bakes the house and its props into one mesh per material. Only what Synty lacks (the easter eggs,
# trade emblems, the cottages and yards) is built here. See the Houses section of the README.
#
# The anchor's name carries everything but its position, because transforms change axes on the way into Unity:
#   SYNTY_<key>__<n>__y<yaw degrees>__t<tilt degrees>__r<roll degrees>__s<scale>[__l<length>][__h<height>]
# yaw is the world turn about z (0 faces Blender +y, Unity -z, like the houses), tilt tips the prop over about its
# own x (forward, onto its face, for positive tilt), roll turns it about the way it faces (a sword hung at an
# angle on a wall), scale multiplies the size houses_synty.cs gives the key, and length and height (metres)
# stretch props that are fitted to a span, such as fence sections.
ANCHORS = []

def anchor(f, key, x, y, z=0.0, yaw=0.0, s=1.0, tilt=0.0, roll=0.0, length=None, height=None):
    """A spot for the Synty prop `key`: local (x, y, z) in frame f, turned by yaw (radians, 0 = facing the frame's
    +y like the house), tipped over by tilt (radians), scaled by s."""
    p = f.p(x, y, z)
    name = "SYNTY_%s__%d__y%.1f__t%.1f__r%.1f__s%.3f" % (key, len(ANCHORS), math.degrees(f.rot + yaw) % 360.0, math.degrees(tilt), math.degrees(roll), s)
    if length is not None: name += "__l%.2f" % length
    if height is not None: name += "__h%.2f" % height
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_type = 'ARROWS'
    ob.location = p
    ob.rotation_euler = (tilt, 0.0, f.rot + yaw)
    scene.collection.objects.link(ob)
    ANCHORS.append(ob)
    return ob

# every house's export carries its anchors along
_export_fbx_mesh_only = export_fbx
def export_fbx(objs, name):
    _export_fbx_mesh_only(list(objs) + ANCHORS + YARDS, name)
    print("NOTE anchors", len(ANCHORS))

def bar(b, a, c, w, d, key, col=None, normal=None):
    """A square bar from point a to point c, w wide and d deep; normal sets which way its depth faces."""
    a, c = Vector(a), Vector(c)
    X = (c - a).normalized()
    n = Vector(normal) if normal is not None else (Vector((0, 0, 1)) if abs(X.z) < 0.9 else Vector((1, 0, 0)))
    Y = (n - X * n.dot(X)).normalized(); Z = X.cross(Y)
    R = Matrix((X, Y, Z)).transposed()
    return b.box(tuple((a + c) / 2), ((c - a).length, d, w), key, col, rot=R)

YARDS = []

def yard(b, f, rx, ry, col=DIRT, jag=0.12, seed=1.0, x=0.0, y=0.0, kind="dirt"):
    """The trodden ground of the lot. Not modelled: a flat pad sat on the grass like a plate. This leaves a
    marker, an empty named YARD__<n>__x<half x>__z<half y>__j<jag>__e<seed>__k<kind>__y<yaw>, and the game lays a
    patch of the island's own terrain texture over the ground there (HouseYard, added by houses_prefabs.cs),
    fading raggedly into the grass. kind is dirt or gravel. The patch fades out at its rim, so it is drawn a
    little larger than the pad was, to look as big."""
    p = f.p(x, y, 0.0)
    ob = bpy.data.objects.new("YARD__%d__x%.2f__z%.2f__j%.2f__e%.2f__k%s__y%.1f" % (len(YARDS), rx * 1.18, ry * 1.18, jag, seed, kind, math.degrees(f.rot) % 360.0), None)
    ob.empty_display_type = 'CIRCLE'
    ob.location = p
    scene.collection.objects.link(ob)
    YARDS.append(ob)
    return ob

def window(b, f, x, z, side_y, w=0.8, h=1.0, shutters="blue", lit=True):
    """A window on a wall whose outer face is at local y = side_y (facing +y in the frame)."""
    y = side_y
    b.box(f.p(x, y + 0.04, z), (w + 0.2, 0.08, h + 0.2), "PAL", TIMBER, rot_z=f.rot)
    b.face([f.p(x - w / 2, y + 0.09, z - h / 2), f.p(x + w / 2, y + 0.09, z - h / 2), f.p(x + w / 2, y + 0.09, z + h / 2), f.p(x - w / 2, y + 0.09, z + h / 2)],
           f.R @ Vector((0, 1, 0)), "GLASS" if lit else "PAL", WARM if lit else DARK)
    b.box(f.p(x, y + 0.11, z), (0.06, 0.04, h), "PAL", TIMBER, rot_z=f.rot)
    b.box(f.p(x, y + 0.11, z), (w, 0.04, 0.06), "PAL", TIMBER, rot_z=f.rot)
    b.box(f.p(x, y + 0.12, z - h / 2 - 0.13), (w + 0.36, 0.24, 0.1), "PAL", TIMBER_L, rot_z=f.rot)
    if shutters:
        for s in (-1, 1):
            b.box(f.p(x + s * (w / 2 + 0.24), y + 0.07, z), (w * 0.4, 0.06, h + 0.05), "PAL", SHUTTER[shutters], rot_z=f.rot)

def window_half_width(w=0.8, shutters=True):
    """Half the width a window takes on a wall, shutters included, for the framing to keep clear of."""
    return w / 2 + 0.24 + w * 0.2 + 0.05 if shutters else w / 2 + 0.15

def frame_wall(b, wf, length, h, plinth, openings, n, beams=True, braces=True):
    """Half-timbering on one wall, laid out round its openings (x centre, half width, bottom, top in the
    wall's own space): sill and head beams, posts either side of every opening, a rail at window-sill
    height through the panels without an opening, and braces in the corner panels that are wide enough,
    rising from the panel's inner edge into the corner post. Nothing crosses a window or a door."""
    z0, z1 = plinth + 0.1, h - 0.1
    if beams:
        for z in (z0, z1):
            b.box(wf.p(0, 0.04, z), (length + 0.06, 0.12, 0.2), "PAL", TIMBER, rot_z=wf.rot)
    studs = []
    for x, hw, zb, zt in openings:
        for side in (-1, 1):
            sx = x + side * (hw + 0.08)
            if abs(sx) < length / 2 - 0.25:
                studs.append(sx)
                b.box(wf.p(sx, 0.03, (z0 + z1) / 2), (0.16, 0.1, z1 - z0), "PAL", TIMBER, rot_z=wf.rot)
    edges = sorted([-length / 2 + 0.13] + studs + [length / 2 - 0.13])
    sill = min([zb for x, hw, zb, zt in openings if zb > 0.5], default=plinth + (h - plinth) * 0.4)
    for i in range(len(edges) - 1):
        a, c = edges[i], edges[i + 1]
        if c - a < 0.3 or any(a - 0.01 < x < c + 0.01 for x, hw, zb, zt in openings):
            continue
        bar(b, wf.p(a, 0.03, sill), wf.p(c, 0.03, sill), 0.14, 0.09, "PAL", TIMBER, normal=n)
        corner = i == 0 or i == len(edges) - 2
        if braces and corner and c - a >= 0.6:
            outer, inner = (a, c) if i == 0 else (c, a)
            bar(b, wf.p(inner, 0.05, z0 + 0.1), wf.p(outer, 0.05, z1 - 0.1), 0.14, 0.09, "PAL", TIMBER, normal=n)

def door(b, f, x, side_y, w=1.1, h=2.1, col=TIMBER_L, arch=True):
    y = side_y
    b.box(f.p(x, y + 0.05, h / 2 + 0.05), (w + 0.26, 0.1, h + 0.2), "PAL", TIMBER, rot_z=f.rot)
    pts = [(x - w / 2, 0.0), (x + w / 2, 0.0), (x + w / 2, h - (w / 2 if arch else 0))]
    if arch:
        for i in range(1, 6): a = math.pi * i / 6; pts.append((x + math.cos(a) * w / 2, h - w / 2 + math.sin(a) * w / 2))
    pts.append((x - w / 2, h - (w / 2 if arch else 0)))
    b.solid_xz(pts, y + 0.1, y + 0.16, "PAL", col, rot_z=f.rot, origin=f.o)
    for zz in (0.5, h - 0.7):
        b.box(f.p(x, y + 0.18, zz), (w - 0.1, 0.03, 0.09), "PAL", DARK, rot_z=f.rot)
    b.sphere(f.p(x + w * 0.3, y + 0.2, 1.0), 0.05, "PAL", GOLD, seg=6, rings=3)
    b.box(f.p(x, y + 0.45, -0.05), (w + 0.5, 0.7, 0.2), "PAL", STONE, rot_z=f.rot)

def cottage(b, f, w, d, h, roof="ROOF_RED", pitch=40, overhang=0.45, plinth=0.45, shutters="blue",
            front_windows=(-1.6, 1.6), side_windows=(0.0,), back_windows=(0.0,), door_x=0.0, chimney=None, braces=True,
            wall="STUCCO", wall_col=None, post_col=TIMBER, beams=True):
    """A timber framed cottage centred on the frame origin: stone plinth, stucco walls with timber framing,
    windows and a door, and a gabled roof with its ridge along x. Returns the ridge height.
    wall and wall_col set the wall material (a palette colour for painted boards), post_col the corner posts,
    and beams turns the sill and top beams on or off."""
    # plinth and walls
    b.box(f.p(0, 0, (plinth - 0.9) / 2), (w + 0.3, d + 0.3, plinth + 0.9), "WALL", rot_z=f.rot)
    b.box(f.p(0, 0, (plinth + h) / 2), (w, d, h - plinth), wall, wall_col, rot_z=f.rot)
    # timber frame: corner posts, sill and top beams, braces at each wall's ends
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(f.p(sx * w / 2, sy * d / 2, (plinth + h) / 2), (0.26, 0.26, h - plinth + 0.02), "PAL", post_col, rot_z=f.rot)
    walls = [(Frame(*f.p(0, d / 2, 0)[:2], f.rot), w), (Frame(*f.p(0, -d / 2, 0)[:2], f.rot + math.pi), w),
             (Frame(*f.p(w / 2, 0, 0)[:2], f.rot - math.pi / 2), d), (Frame(*f.p(-w / 2, 0, 0)[:2], f.rot + math.pi / 2), d)]
    fw = walls[0][0]; bw = walls[1][0]; rw = walls[2][0]; lw = walls[3][0]
    wz = plinth + (h - plinth) * 0.55
    hw = window_half_width(shutters=bool(shutters))
    win = lambda xs: [(x, hw, wz - 0.5 - 0.2, wz + 0.5 + 0.12) for x in xs]
    openings = [win(front_windows), win(back_windows), win(side_windows), win(side_windows)]
    if door_x is not None: openings[0].append((door_x, 0.55 + 0.14, 0.0, 2.25))
    for (wf, length), ops in zip(walls, openings):
        frame_wall(b, wf, length, h, plinth, ops, wf.R @ Vector((0, 1, 0)), beams=beams, braces=braces)
    for x in front_windows: window(b, fw, x, wz, 0.0, shutters=shutters)
    for x in back_windows: window(b, bw, x, wz, 0.0, shutters=shutters)
    for x in side_windows:
        window(b, rw, x, wz, 0.0, shutters=shutters); window(b, lw, x, wz, 0.0, shutters=shutters)
    if door_x is not None: door(b, fw, door_x, 0.0)
    # roof: two sloped slabs over the walls, gable triangles, barge boards and a ridge beam
    t = math.tan(math.radians(pitch))
    ridge = h + (d / 2) * t
    hx = w / 2 + overhang * 0.8
    ey = d / 2 + overhang; ez = h - overhang * t
    nrm = lambda sy: Vector((0, sy * math.sin(math.radians(pitch)), math.cos(math.radians(pitch)))) * 0.2
    for sy in (-1, 1):
        lo = [Vector(f.p(-hx, sy * ey, ez)), Vector(f.p(hx, sy * ey, ez)), Vector(f.p(hx, 0, ridge + 0.02)), Vector(f.p(-hx, 0, ridge + 0.02))]
        if sy < 0: lo = [lo[1], lo[0], lo[3], lo[2]]
        nv = f.R @ nrm(sy)
        if (lo[1] - lo[0]).cross(lo[2] - lo[1]).z < 0: lo = lo[::-1]
        fs = b.quad_box(lo, [v + nv for v in lo], roof)
        b.paint([fs[0]], "PAL", TIMBER)
        b.paint(fs[1:], roof, tile=(3.0, 3.0))
    for sx in (-1, 1):
        gf = f.sub(sx * w / 2, 0, -sx * math.pi / 2)
        b.solid_xz([(-d / 2, h), (d / 2, h), (0, ridge)], -0.14, 0.02, wall, wall_col, rot_z=gf.rot, origin=gf.o)
        n = gf.R @ Vector((0, 1, 0))
        bar(b, gf.p(0, 0.04, h), gf.p(0, 0.04, ridge - 0.1), 0.18, 0.1, "PAL", TIMBER, normal=n)
        bar(b, gf.p(-d / 2 + 0.6, 0.04, h + 0.5 * t), gf.p(d / 2 - 0.6, 0.04, h + 0.5 * t), 0.16, 0.1, "PAL", TIMBER, normal=n)
        for sy in (-1, 1):
            bar(b, f.p(sx * (hx + 0.05), sy * (ey + 0.05), ez - 0.05), f.p(sx * (hx + 0.05), 0, ridge + 0.12), 0.22, 0.12, "PAL", TIMBER, normal=f.R @ Vector((sx, 0, 0)))
    bar(b, f.p(-hx - 0.1, 0, ridge + 0.2), f.p(hx + 0.1, 0, ridge + 0.2), 0.2, 0.24, "PAL", TIMBER)
    if chimney is not None:
        cx, cy = chimney
        b.box(f.p(cx, cy, (ridge + 1.0) / 2), (0.75, 0.75, ridge + 1.0), "WALL", rot_z=f.rot)
        b.box(f.p(cx, cy, ridge + 1.05), (0.95, 0.95, 0.16), "PAL", STONE, rot_z=f.rot)
    return ridge

def sign(b, f, x, z, side_y, emblem, along=1):
    """A signboard hanging from a bracket on a wall, standing out from it, with an emblem on both faces.
    along = 1 puts the bracket towards +x."""
    y = side_y
    bar(b, f.p(x, y, z + 0.5), f.p(x, y + 1.0, z + 0.5), 0.1, 0.1, "PAL", TIMBER)
    bar(b, f.p(x, y, z - 0.1), f.p(x, y + 0.7, z + 0.45), 0.08, 0.08, "PAL", TIMBER)
    for yy in (y + 0.45, y + 0.9):
        b.box(f.p(x, yy, z + 0.33), (0.03, 0.03, 0.3), "PAL", DARK, rot_z=f.rot)
    sf = f.sub(x, y + 0.68, math.pi / 2)    # board faces along the wall, readable from both sides
    b.box(sf.p(0, 0, z), (0.72, 0.06, 0.52), "PAL", TIMBER_L, rot_z=sf.rot)
    b.box(sf.p(0, 0, z), (0.8, 0.04, 0.6), "PAL", TIMBER, rot_z=sf.rot)
    for s in (1, -1):
        emblem(b, sf.sub(0, s * 0.045, 0 if s > 0 else math.pi), z)

def lantern_post(b, f, x, y, h=2.2):
    b.box(f.p(x, y, h / 2), (0.14, 0.14, h), "PAL", TIMBER, rot_z=f.rot)
    bar(b, f.p(x, y, h - 0.15), f.p(x + 0.45, y, h - 0.15), 0.08, 0.08, "PAL", TIMBER)
    b.box(f.p(x + 0.42, y, h - 0.48), (0.22, 0.22, 0.3), "GLASS", WARM, rot_z=f.rot)
    b.prism(f.p(x + 0.42, y, h - 0.33), 0.18, 0.14, 4, "PAL", DARK, r_top=0.0, rot=f.rot + math.pi / 4)

def crate(b, f, x, y, z=0.0, s=0.6, yaw=0.0):
    b.box(f.p(x, y, z + s / 2), (s, s, s), "PAL", TIMBER_L, rot_z=f.rot + yaw)
    for dz in (0.06, s - 0.06):
        b.box(f.p(x, y, z + dz), (s + 0.02, s + 0.02, 0.08), "PAL", TIMBER, rot_z=f.rot + yaw)

def barrel(b, f, x, y, z=0.0, r=0.32, h=0.8):
    b.lathe(f.p(x, y, z), [(r * 0.85, 0), (r, h * 0.5), (r * 0.85, h)], 10, "PAL", TIMBER_L)
    b.lathe(f.p(x, y, z), [(r * 0.85, h), (0.0, h)], 10, "PAL", TIMBER)
    for zz in (0.12, h - 0.12):
        b.lathe(f.p(x, y, z), [(r * 0.92, zz - 0.04), (r * 0.95, zz + 0.04)], 10, "PAL", DARK)

def rock(b, f, x, y, r, sz=0.7, col=ROCK, seg=7, seed=0.0):
    """A low poly boulder: a squashed sphere with its top flattened."""
    c = Vector(f.p(x, y, r * sz * 0.35))
    vs = []
    rings = 4
    for j in range(rings + 1):
        phi = -math.pi / 2 + math.pi * j / rings
        for i in range(seg):
            th = 2 * math.pi * i / seg + seed
            k = 1 + 0.18 * math.sin(i * 2.3 + j * 1.7 + seed * 3)
            vs.append(c + Vector((math.cos(th) * math.cos(phi) * r * k, math.sin(th) * math.cos(phi) * r * k, max(math.sin(phi), -0.4) * r * sz)))
    idx = []
    for j in range(rings):
        for i in range(seg):
            a = j * seg + i; bb = j * seg + (i + 1) % seg
            idx.append((a, bb, bb + seg, a + seg))
    idx.append(tuple(reversed(range(seg)))); idx.append(tuple(range(rings * seg, rings * seg + seg)))
    fs = b.add(vs, idx)
    for fc in fs: fc.normal_update()
    b.paint(fs, "PAL", col)

# yard props
exec(open("house_props.py").read())
