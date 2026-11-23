# The band 4 great hall's own shapes (docs/tower-design.md section 22, concept docs/mockups/tower/band4/floor-b-great-hall.jpg),
# made in Blender from the command line:
#   blender -b --python tools/tower/blender/tower_great_hall.py -- <repo root>
# - Wall stubs: what is left of a Dark Fortress wall piece while the camera looks over it (TowerCutaway). The pack's own
#   5 m wall and arch pieces cut down to about a metre with a broken top, so the stub keeps the wall's bricks and UVs.
#   The cut faces get the material "Cap" (the builder gives it a dark stone).
# - The rune circle in the middle of the banquet hall: rings, a seven pointed star and runes as thin flat strips, and
#   cracks running out from it, all material "Rune" (the builder makes it glow violet).
# Exported as FBX into Assets/World/Tower/GreatHall/Models; previews in Temp/Blender.
import bpy, bmesh, math, os, sys, random
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Assets", "World", "Tower", "GreatHall", "Models")
PREVIEW = os.path.join(ROOT, "Temp", "Blender")
SYNTY = os.path.join(ROOT, "Assets", "Synty", "PolygonDarkFortress", "Models")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    return m


def export(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True)
    print("exported", name)


def stub(source, name, height, seed, gap=None):
    """The Synty piece cut down to `height` metres with a broken top; `gap` (from, to along its length) cut out for a doorway."""
    clear()
    bpy.ops.import_scene.fbx(filepath=os.path.join(SYNTY, source + ".fbx"))
    parts = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    # out from under the import's empties first, keeping where they are: exporting a child alone drops its parent's turn
    # (the arch came out lying on its back beside every door)
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    for o in [o for o in bpy.context.scene.objects if o.type == "EMPTY"]:
        bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    wall = bpy.context.view_layer.objects.active
    # into plain world metres: the import's scale and turn applied
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.context.view_layer.update()
    pts = [wall.matrix_world @ v.co for v in wall.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    along_x = (hi.x - lo.x) >= (hi.y - lo.y)
    a0, a1 = (lo.x, hi.x) if along_x else (lo.y, hi.y)
    b0, b1 = (lo.y, hi.y) if along_x else (lo.x, hi.x)
    print(source, "bounds", tuple(round(c, 2) for c in lo), tuple(round(c, 2) for c in hi), "along", "x" if along_x else "y")

    # the cutter: everything above a jagged line, across the whole thickness
    rnd = random.Random(seed)
    steps = 14
    top = hi.z + 5.0
    line = []
    for i in range(steps + 1):
        t = i / steps
        a = a0 - 0.3 + (a1 - a0 + 0.6) * t
        z = lo.z + height + (rnd.random() - 0.5) * 0.55 + 0.25 * math.sin(t * 9.0 + seed)
        line.append((a, z))
    bm = bmesh.new()
    pad = 1.0
    front, back = [], []
    for (a, z) in line + [(line[-1][0], top), (line[0][0], top)]:
        for b, lst in ((b0 - pad, front), (b1 + pad, back)):
            co = Vector((a, b, z)) if along_x else Vector((b, a, z))
            lst.append(bm.verts.new(co))
    n = len(front)
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((front[i], front[j], back[j], back[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("Cutter")
    bm.to_mesh(me)
    bm.free()
    cutter = bpy.data.objects.new("Cutter", me)
    bpy.context.collection.objects.link(cutter)
    cutter.data.materials.append(material("Cap", (0.16, 0.15, 0.17)))

    mod = wall.modifiers.new("cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    mod.solver = "EXACT"
    try:
        mod.material_mode = "TRANSFER"
    except Exception:
        pass
    bpy.context.view_layer.objects.active = wall
    bpy.ops.object.modifier_apply(modifier="cut")
    bpy.data.objects.remove(cutter)
    if gap:
        bm = bmesh.new()
        g0, g1 = a0 + gap[0], a0 + gap[1]
        lo3 = Vector((g0, b0 - 1, lo.z - 1)) if along_x else Vector((b0 - 1, g0, lo.z - 1))
        hi3 = Vector((g1, b1 + 1, hi.z + 1)) if along_x else Vector((b1 + 1, g1, hi.z + 1))
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector((lo3.x if v.co.x < 0 else hi3.x, lo3.y if v.co.y < 0 else hi3.y, lo3.z if v.co.z < 0 else hi3.z))
        me = bpy.data.meshes.new("Gap")
        bm.to_mesh(me)
        bm.free()
        gapper = bpy.data.objects.new("Gap", me)
        bpy.context.collection.objects.link(gapper)
        gapper.data.materials.append(material("Cap", (0.16, 0.15, 0.17)))
        mod = wall.modifiers.new("gap", "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.object = gapper
        mod.solver = "EXACT"
        try:
            mod.material_mode = "TRANSFER"
        except Exception:
            pass
        bpy.context.view_layer.objects.active = wall
        bpy.ops.object.modifier_apply(modifier="gap")
        bpy.data.objects.remove(gapper)
    # the exact boolean gives up on a mesh that is not closed (the arch: its parts overlap); then a straight cut at the
    # height instead, the cut faces filled
    if wall.dimensions.z > height + 1.0:
        bpy.ops.object.modifier_remove(modifier="cut") if wall.modifiers.get("cut") else None
        me = wall.data
        bm = bmesh.new()
        bm.from_mesh(me)
        res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, lo.z + height),
                                     plane_no=(0, 0, 1), clear_outer=True)
        cut_edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if cut_edges:
            filled = bmesh.ops.holes_fill(bm, edges=cut_edges, sides=0)
            cap = len(wall.data.materials)
            wall.data.materials.append(material("Cap", (0.16, 0.15, 0.17)))
            for f in filled["faces"]:
                f.material_index = cap
        bm.to_mesh(me)
        bm.free()
        print(name, "cut straight instead")
    wall.name = name
    wall.data.name = name
    print(name, "faces", len(wall.data.polygons), "materials", [m.name for m in wall.data.materials if m],
          "parent", wall.parent, "dims", tuple(round(d, 2) for d in wall.dimensions))
    export([wall], name)


def strip(bm, pts, width, y=0.0):
    """A flat strip of `width` along a polyline of (x, z) points at height y (Blender z up: x, y ground, z height)."""
    left, right = [], []
    for i, p in enumerate(pts):
        q0 = pts[max(0, i - 1)]
        q1 = pts[min(len(pts) - 1, i + 1)]
        d = Vector((q1[0] - q0[0], q1[1] - q0[1], 0.0))
        if d.length < 1e-6:
            d = Vector((1, 0, 0))
        d.normalize()
        nrm = Vector((-d.y, d.x, 0.0)) * (width / 2)
        left.append(bm.verts.new((p[0] + nrm.x, p[1] + nrm.y, y)))
        right.append(bm.verts.new((p[0] - nrm.x, p[1] - nrm.y, y)))
    for i in range(len(pts) - 1):
        bm.faces.new((left[i], right[i], right[i + 1], left[i + 1]))


def rune_circle():
    clear()
    rnd = random.Random(7)
    bm = bmesh.new()
    R = 6.5

    def ring(r, w, seg=96):
        pts = [(r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg)) for i in range(seg + 1)]
        strip(bm, pts, w, 0.0)

    ring(R, 0.32)
    ring(R - 0.75, 0.18)
    ring(2.0, 0.2)
    # a seven pointed star between the rings (every third point)
    star = 7
    pts = []
    for k in range(star + 1):
        i = (k * 3) % star
        a = 2 * math.pi * i / star + math.pi / 2
        pts.append(((R - 0.8) * math.cos(a), (R - 0.8) * math.sin(a)))
    for k in range(star):
        strip(bm, [pts[k], pts[k + 1]], 0.18, 0.004)
    # runes in the band between the two outer rings: short marks of two or three strokes
    for k in range(28):
        a = 2 * math.pi * (k + 0.5) / 28
        c = Vector(((R - 0.38) * math.cos(a), (R - 0.38) * math.sin(a), 0))
        t = Vector((-math.sin(a), math.cos(a), 0))
        r = Vector((math.cos(a), math.sin(a), 0))
        s = 0.2
        shapes = [
            [(-s, -s), (0, s), (s, -s)],
            [(-s, s), (-s, -s), (s, -s)],
            [(0, -s), (0, s)],
            [(-s, 0), (s, 0), (s, s)],
            [(-s, -s), (s, s)],
        ]
        shape = shapes[rnd.randrange(len(shapes))]
        line = [(c + t * u + r * v) for (u, v) in shape]
        strip(bm, [(p.x, p.y) for p in line], 0.09, 0.006)
    # cracks running out from the circle across the floor, thinning as they go
    for k in range(9):
        a = 2 * math.pi * (k + rnd.random() * 0.6) / 9
        p = Vector((R * math.cos(a), R * math.sin(a), 0))
        pts = [(p.x, p.y)]
        for j in range(3 + rnd.randrange(3)):
            a += (rnd.random() - 0.5) * 0.7
            step = 0.8 + rnd.random() * 1.1
            p = p + Vector((math.cos(a), math.sin(a), 0)) * step
            pts.append((p.x, p.y))
        strip(bm, pts, 0.12, 0.002)
    me = bpy.data.meshes.new("GreatHallRuneCircle")
    bm.to_mesh(me)
    bm.free()
    for poly in me.polygons:
        poly.use_smooth = False
    obj = bpy.data.objects.new("GreatHallRuneCircle", me)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material("Rune", (0.6, 0.25, 1.0)))
    # faces up (the strips were wound either way)
    bm = bmesh.new()
    bm.from_mesh(me)
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    bm.to_mesh(me)
    bm.free()
    export([obj], "GreatHallRuneCircle")


stub("SM_Bld_Wall_L_01", "GreatHallWallStub", 1.0, 3)
stub("SM_Bld_Wall_L_01", "GreatHallWallStubB", 0.8, 11)
# the doorway's stub: a plain wall's, with the arch's opening (about 2.8 m at the floor) cut out of its middle
stub("SM_Bld_Wall_L_01", "GreatHallArchStub", 1.0, 5, gap=(1.1, 3.9))
rune_circle()
print("done")
