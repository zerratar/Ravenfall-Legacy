# Galewharf's rock (tower band 3's town, concept A: docs/mockups/galewharf-concept-a-island.jpg), made in Blender from the
# command line:
#   blender -b --python tools/tower/blender/galewharf_rocks.py -- <repo root>
# Makes three meshes and exports them as FBX into Assets/World/Tower/Galewharf/Models, with preview renders in
# Temp/Blender:
#   GalewharfIsland  the floating island's underside: a jagged, layered rock cone under the rim, an earth band at the
#                    top, roots hanging from under the edge. Its top ring follows the rim in tools/tower/galewharf-rim.json,
#                    which the Unity builder also reads (the terrain is cut along the same line).
#   GalewharfIslet   the lighthouse's islet: a small rock with a flat grassy top (walkable), the same look.
#   GalewharfSpire   the colossal stone spire behind the town, a carved stair winding up it, its foot lost in the clouds.
# Units are metres, Y up in Unity (the export converts). Materials: "Rock", "Earth", "Grass", "Stone", "Root"; the Unity
# builder assigns its own materials by those names.
import bpy, bmesh, math, json, os, sys, random
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Assets", "World", "Tower", "Galewharf", "Models")
PREVIEW = os.path.join(ROOT, "Temp", "Blender")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)
random.seed(19)

# ---- the rim: radius by compass angle (0 = east, 90 = north in Unity's x/z), shared with the Unity builder ----
N = 72
def rim_radius(a):
    r = 44.0 + 4.0 * math.sin(3 * a + 0.7) + 2.5 * math.sin(5 * a + 2.1) + 1.4 * math.sin(11 * a + 0.3)
    # the pier's point in the south-east and the rope bridge's point in the east reach out a little
    r += 5.0 * max(0.0, math.cos(a - math.radians(-30))) ** 6
    r += 3.0 * max(0.0, math.cos(a - math.radians(15))) ** 10
    # the north is straight-ish for the bridge to the spire
    r += 3.0 * max(0.0, math.cos(a - math.radians(90))) ** 4
    return r
rim = [(i * 360.0 / N, rim_radius(math.radians(i * 360.0 / N))) for i in range(N)]
with open(os.path.join(ROOT, "tools", "tower", "galewharf-rim.json"), "w") as f:
    json.dump({"note": "Galewharf's rim: [degrees from east toward north, radius in metres]; made by tools/tower/blender/galewharf_rocks.py", "rim": [[round(a, 2), round(r, 3)] for a, r in rim]}, f, indent=0)

# Blender: x east, y north, z up (the FBX export makes Unity's z = Blender's y)
def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    make_materials()

def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    return m

def slot(obj, m):
    names = [x.name if x else None for x in obj.data.materials]
    if m.name not in names:
        obj.data.materials.append(m)
        names.append(m.name)
    return names.index(m.name)

def assign_by_height(obj, slots, split):
    """slots: [(material, from_z)] top first; faces whose centre is above from_z take that material. Call before
    joining the roots (they keep theirs)."""
    index = [slot(obj, m) for m, _ in slots]
    for p in obj.data.polygons:
        z = sum((obj.data.vertices[v].co.z for v in p.vertices)) / len(p.vertices)
        p.material_index = index[-1]
        for i, (m, from_z) in enumerate(slots):
            if z >= from_z:
                p.material_index = index[i]
                break

def faceted(obj, ratio):
    """Chunky low-poly facets: collapse decimation, then flat shading."""
    mod = obj.modifiers.new("decimate", "DECIMATE")
    mod.ratio = ratio
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = False

def snap_rim(obj, outline, lip, top_z=-0.25):
    """Decimation moves vertices; the top ring is put back on the rim (radius by angle, interpolated) at its height."""
    seg = len(outline)
    def radius(a):
        deg = math.degrees(a) % 360.0
        step = 360.0 / seg
        i = int(deg // step)
        f = (deg - i * step) / step
        return outline[i % seg][1] * (1 - f) + outline[(i + 1) % seg][1] * f
    moved = 0
    for v in obj.data.vertices:
        if v.co.z > top_z - 0.6:
            a = math.atan2(v.co.y, v.co.x)
            r = math.hypot(v.co.x, v.co.y)
            if r < 1.0:
                continue
            target = radius(a) + lip
            v.co.x, v.co.y, v.co.z = math.cos(a) * target, math.sin(a) * target, top_z
            moved += 1
    return moved

def uv_box(obj, scale):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=scale)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)

def export(obj, name):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    for child in obj.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False)
    return path

def make_materials():
    """Made again after every reset (clear() empties the file)."""
    global ROCK, EARTH, GRASS, STONE, ROOT_M
    ROCK = material("Rock", (0.42, 0.43, 0.46))
    EARTH = material("Earth", (0.36, 0.27, 0.18))
    GRASS = material("Grass", (0.30, 0.45, 0.20))
    STONE = material("Stone", (0.55, 0.54, 0.52))
    ROOT_M = material("Root", (0.30, 0.20, 0.12))

def underside(name, outline, depth, tip_offset, lip=0.6, rings=26, seed=0.0, top_cap=False, spikes=0):
    """A hanging rock cone whose top ring follows outline [(deg, r)], strata ledges and a jagged tip."""
    bm = bmesh.new()
    seg = len(outline)
    verts = []
    for k in range(rings + 1):
        t = k / rings                         # 0 at the top ring, 1 at the tip
        z = -0.25 - depth * (t ** 1.15)
        # the cone narrows, faster near the tip; strata: now and then the rock steps out a little, unevenly
        shrink = (1 - t) ** 0.85
        strata = 0.035 * math.sin(t * 31.0 + seed + 2.0 * noise.noise(Vector((t * 3.0, seed, 0.0)))) * min(1.0, t * 8.0)
        ring = []
        for i, (deg, r) in enumerate(outline):
            a = math.radians(deg)
            rr = (r + lip) * max(0.02, shrink * (1 + strata))
            # columns: noise that changes fast round the rim and slowly downward, so the faces run in vertical ribs;
            # finer chips on top of that, all stronger lower down; the tip drifts off centre
            col = noise.noise(Vector((math.cos(a) * 6.0 + seed, math.sin(a) * 6.0, t * 1.2)))
            chip = noise.noise(Vector((math.cos(a) * 14.0, math.sin(a) * 14.0 + seed, t * 5.0)))
            # none at the very top: the top ring is the rim the terrain is cut along
            rr *= 1 + (0.16 * col + 0.06 * chip) * min(1.0, t * 1.6)
            x = math.cos(a) * rr + tip_offset[0] * t ** 2
            y = math.sin(a) * rr + tip_offset[1] * t ** 2
            ring.append(bm.verts.new((x, y, z + (1.2 * noise.noise(Vector((x * 0.2, y * 0.2, seed))) * t if k > 0 else 0.0))))
        verts.append(ring)
    tip = bm.verts.new((tip_offset[0], tip_offset[1], -depth - 6.0))
    for k in range(rings):
        for i in range(seg):
            a, b = verts[k][i], verts[k][(i + 1) % seg]
            c, d = verts[k + 1][(i + 1) % seg], verts[k + 1][i]
            bm.faces.new((a, d, c, b))
    for i in range(seg):
        bm.faces.new((verts[rings][i], tip, verts[rings][(i + 1) % seg]))
    if top_cap:
        top = bm.verts.new((0, 0, -0.25))
        for i in range(seg):
            bm.faces.new((verts[0][(i + 1) % seg], top, verts[0][i]))
    # hanging spikes under the body, as in the concept: a few cones pointing down from the lower half
    rnd = random.Random(int(seed * 100))
    for j in range(spikes):
        deg, r = outline[rnd.randrange(seg)]
        a = math.radians(deg)
        t0 = 0.35 + rnd.random() * 0.35
        r0 = (r + lip) * (1 - t0) ** 0.85 * (0.55 + rnd.random() * 0.3)
        cx, cy, cz = math.cos(a) * r0, math.sin(a) * r0, -0.25 - depth * t0 ** 1.15
        base_r = 2.0 + rnd.random() * 4.0
        length = 6.0 + rnd.random() * 14.0
        sides = 6
        ring = [bm.verts.new((cx + math.cos(k / sides * 6.283 + j) * base_r, cy + math.sin(k / sides * 6.283 + j) * base_r, cz + 1.0)) for k in range(sides)]
        point = bm.verts.new((cx + rnd.uniform(-1.5, 1.5), cy + rnd.uniform(-1.5, 1.5), cz - length))
        for k in range(sides):
            bm.faces.new((ring[k], point, ring[(k + 1) % sides]))
    mesh = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj

def roots(parent, outline, count, seed):
    """Roots hanging from just under the rim: tapered curves, bevelled, made meshes, parented to the rock."""
    rnd = random.Random(seed)
    made = []
    for j in range(count):
        deg, r = outline[rnd.randrange(len(outline))]
        a = math.radians(deg)
        start = Vector((math.cos(a) * (r - 0.3), math.sin(a) * (r - 0.3), -1.6 - rnd.random() * 1.5))
        curve = bpy.data.curves.new("root", "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = 0.18 + rnd.random() * 0.15
        curve.bevel_resolution = 1
        spline = curve.splines.new("BEZIER")
        pts = 4
        spline.bezier_points.add(pts - 1)
        length = 4.0 + rnd.random() * 7.0
        for i, bp in enumerate(spline.bezier_points):
            t = i / (pts - 1)
            out = Vector((math.cos(a), math.sin(a), 0)) * (0.6 * math.sin(t * math.pi) + 0.4 * t)
            p = start + out + Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), -length * t))
            bp.co = p
            bp.handle_left_type = bp.handle_right_type = "AUTO"
            bp.radius = 1.0 - 0.85 * t
        o = bpy.data.objects.new("Root", curve)
        bpy.context.collection.objects.link(o)
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        bpy.ops.object.convert(target="MESH")
        o.select_set(False)
        o.data.materials.append(ROOT_M)
        made.append(o)
    # one mesh with the rock
    bpy.ops.object.select_all(action="DESELECT")
    for o in made:
        o.select_set(True)
    parent.select_set(True)
    bpy.context.view_layer.objects.active = parent
    bpy.ops.object.join()
    return parent

def preview(obj, name, cam_loc, look):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    scene.world = scene.world or bpy.data.worlds.new("World")
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = cam_loc
    direction = Vector(look) - Vector(cam_loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.clip_end = 2000
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)

report = {}

# ---- the island's underside ----
clear()
island = underside("GalewharfIsland", rim, depth=58.0, tip_offset=(-6.0, 4.0), seed=1.3, spikes=9)
faceted(island, 0.3)
report["island_rim_snapped"] = snap_rim(island, rim, 0.6)
assign_by_height(island, [(EARTH, -2.6), (ROCK, -9999)], None)
island = roots(island, rim, 26, 5)
uv_box(island, 6.0)
report["island_faces"] = len(island.data.polygons)
preview(island, "galewharf-island", (110, -120, 20), (0, 0, -18))
report["island"] = export(island, "GalewharfIsland")

# ---- the lighthouse's islet: a small rock with a flat grassy top ----
clear()
islet_outline = [(i * 360.0 / 36, 9.0 + 1.6 * math.sin(math.radians(i * 10) * 3 + 0.4) + 0.8 * math.sin(math.radians(i * 10) * 7)) for i in range(36)]
islet = underside("GalewharfIslet", islet_outline, depth=20.0, tip_offset=(1.5, -1.0), lip=0.3, rings=12, seed=4.2, top_cap=True, spikes=3)
faceted(islet, 0.5)
assign_by_height(islet, [(GRASS, -0.3), (EARTH, -1.6), (ROCK, -9999)], None)
islet = roots(islet, islet_outline, 6, 9)
uv_box(islet, 4.0)
report["islet_faces"] = len(islet.data.polygons)
preview(islet, "galewharf-islet", (30, -34, 10), (0, 0, -6))
report["islet"] = export(islet, "GalewharfIslet")

# ---- the spire: a colossal tapering column of rock, a stair winding up it ----
clear()
bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=1.0, depth=1.0, location=(0, 0, 0))
spire = bpy.context.active_object
spire.name = "GalewharfSpire"
bm = bmesh.new()
bm.from_mesh(spire.data)
bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.5], cuts=40, use_grid_fill=False)
H, BOTTOM = 260.0, -120.0
for v in bm.verts:
    t = v.co.z + 0.5                                  # 0 at the foot, 1 at the top
    r = 26.0 * (1 - t) ** 0.9 + 6.0 * t               # wide foot, slender top
    a = math.atan2(v.co.y, v.co.x)
    n = noise.noise(Vector((math.cos(a) * 1.7, math.sin(a) * 1.7, t * 7.0)))
    col = noise.noise(Vector((math.cos(a) * 5.0, math.sin(a) * 5.0, t * 3.0)))
    r *= 1 + 0.16 * n + 0.12 * col + 0.04 * math.sin(t * 47 + 3 * n)   # chunky, ribbed, with uneven strata
    v.co = Vector((math.cos(a) * r, math.sin(a) * r, BOTTOM + t * H))
bm.to_mesh(spire.data)
bm.free()
faceted(spire, 0.4)
spire.data.materials.append(ROCK)
# the stair: a ledge winding up two and a half turns from the bridge's landing (south side, where the town is)
bm = bmesh.new()
steps = 260
prev = None
for s in range(steps + 1):
    t = s / steps
    z = 2.0 + t * 120.0
    a = math.radians(-90 + t * 900)                    # starts on the south side, toward the town
    rr = (26.0 * (1 - (z - BOTTOM) / H) ** 0.9 + 6.0 * (z - BOTTOM) / H) * 1.02
    inner = Vector((math.cos(a) * (rr - 1.0), math.sin(a) * (rr - 1.0), z))
    outer = Vector((math.cos(a) * (rr + 4.2), math.sin(a) * (rr + 4.2), z))
    top_i, top_o = bm.verts.new(inner), bm.verts.new(outer)
    bot_o = bm.verts.new(outer - Vector((0, 0, 1.6)))
    par = bm.verts.new(outer + Vector((0, 0, 0.9)))   # a low parapet on the outside
    if prev:
        pi, po, pb, pp = prev
        bm.faces.new((pi, top_i, top_o, po))          # the tread
        bm.faces.new((po, top_o, bot_o, pb))          # the outer face
        bm.faces.new((po, pp, par, top_o))            # the parapet
    prev = (top_i, top_o, bot_o, par)
stair_mesh = bpy.data.meshes.new("Stair")
bm.normal_update()
bm.to_mesh(stair_mesh)
bm.free()
stair = bpy.data.objects.new("Stair", stair_mesh)
bpy.context.collection.objects.link(stair)
stair.data.materials.append(STONE)
bpy.ops.object.select_all(action="DESELECT")
stair.select_set(True)
spire.select_set(True)
bpy.context.view_layer.objects.active = spire
bpy.ops.object.join()
uv_box(spire, 8.0)
report["spire_faces"] = len(spire.data.polygons)
preview(spire, "galewharf-spire", (0, -260, 40), (0, 0, 20))
report["spire"] = export(spire, "GalewharfSpire")

with open(os.path.join(PREVIEW, "galewharf-rocks.json"), "w") as f:
    json.dump(report, f, indent=1)
print("GALEWHARF DONE", json.dumps(report))
