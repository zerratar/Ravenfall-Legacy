# The floating rocks under the tower's sky floors, the Sky Docks and the Storm Spire (concepts
# docs/mockups/tower/concept-skydocks.jpg and concept-stormspire.jpg), made in Blender from the command line:
#   blender -b --python tools/tower/blender/tower_islands.py -- <repo root>
# Both floors were terrain mesas whose sides were stretched terrain dressed with cliff props. Each now gets Galewharf's
# kind of rock (the functions are Galewharf's, tools/tower/blender/galewharf_rocks.py): a jagged, layered cone under the
# rim with an earth band and roots, its top ring on the rim the floor's builder writes to tools/tower/<floor>-rim.json
# (run the builder once first; it cuts its terrain along the same line). Exported as FBX into
# Assets/World/Tower/Sky/Models (SkyDocksRock, StormSpireRock), previews in Temp/Blender. Materials "Rock", "Earth",
# "Root" (the builders assign their own by name).
import bpy, bmesh, math, json, os, sys, random
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Assets", "World", "Tower", "Sky", "Models")
PREVIEW = os.path.join(ROOT, "Temp", "Blender")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)
random.seed(29)

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
for name, rim_file, depth, tip, seed, spikes, root_count in (
    ("SkyDocksRock", "skydocks-rim.json", 54.0, (5.0, -4.0), 4.2, 11, 30),
    ("StormSpireRock", "stormspire-rim.json", 48.0, (-4.0, 6.0), 7.7, 14, 18),
):
    path = os.path.join(ROOT, "tools", "tower", rim_file)
    if not os.path.exists(path):
        report[name] = "no " + rim_file + " (run the floor's builder once)"
        continue
    rim = [tuple(p) for p in json.load(open(path))["rim"]]
    clear()
    rock = underside(name, rim, depth=depth, tip_offset=tip, seed=seed, spikes=spikes)
    faceted(rock, 0.3)
    report[name + "_rim_snapped"] = snap_rim(rock, rim, 0.6)
    assign_by_height(rock, [(EARTH, -2.6), (ROCK, -9999)], None)
    rock = roots(rock, rim, root_count, int(seed * 10))
    uv_box(rock, 6.0)
    report[name] = len(rock.data.polygons)
    preview(rock, name.lower(), (-110, -120, 10), (0, 0, -18))
    export(rock, name)
print("ISLANDS", report)
