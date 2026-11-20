# Mangroves for the drowned Marsh (concept docs/mockups/tower/concept-marsh.jpg: "twisted mangrove-like roots arching
# out of the water"), made in Blender from the command line:
#   blender -b --python tools/tower/blender/tower_mangroves.py -- <repo root>
# None of the packs has one (their swamp trees are bare dead cypresses). Each is a short twisted trunk standing on prop
# roots that arch out and down into the water, branches spreading to a broad, flat canopy of chunky leaf clumps. Four of
# them, exported as FBX into Assets/World/Tower/Halls/Models (Mangrove_01 to 04), previews in Temp/Blender. Its foot is at
# the origin (the water line). Materials "Bark", "Leaves", "LeavesLight"; the Unity builder assigns its own by name.
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Assets", "World", "Tower", "Halls", "Models")
PREVIEW = os.path.join(ROOT, "Temp", "Blender")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global BARK, LEAVES, LIGHT
    BARK = material("Bark", (0.30, 0.22, 0.15))
    LEAVES = material("Leaves", (0.20, 0.38, 0.16))
    LIGHT = material("LeavesLight", (0.36, 0.52, 0.20))


def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    return m


def tube(points, radius, taper, mat, res=4):
    """A bevelled curve through points, thick at the first and thin at the last, made a mesh."""
    curve = bpy.data.curves.new("tube", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 0
    curve.resolution_u = res
    curve.use_fill_caps = True
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for k, p in enumerate(points):
        bp = spline.bezier_points[k]
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = "AUTO"
        bp.radius = 1.0 - (1.0 - taper) * k / (len(points) - 1)
    obj = bpy.data.objects.new("tube", curve)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj.data.materials.append(mat)
    return obj


def clump(at, size, rnd, mat):
    """A chunky clump of leaves: a low-poly sphere, squashed, its points nudged so no two are alike."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0, location=at)
    obj = bpy.context.active_object
    for v in obj.data.vertices:
        v.co *= 1.0 + rnd.uniform(-0.18, 0.18)
    obj.scale = (size * rnd.uniform(0.9, 1.2), size * rnd.uniform(0.9, 1.2), size * rnd.uniform(0.5, 0.7))
    obj.rotation_euler = (0, 0, rnd.uniform(0, math.tau))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def mangrove(seed):
    rnd = random.Random(seed)
    parts = []
    lean = Vector((rnd.uniform(-0.8, 0.8), rnd.uniform(-0.8, 0.8), 0))
    base = Vector((0, 0, rnd.uniform(1.8, 2.6)))       # where the roots meet the trunk, above the water
    top = base + Vector((lean.x, lean.y, rnd.uniform(3.2, 4.4)))
    mid = (base + top) / 2 + Vector((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), 0))
    parts.append(tube([base - Vector((0, 0, 0.3)), mid, top], rnd.uniform(0.42, 0.55), 0.6, BARK))
    # prop roots: out from the foot of the trunk, arching high, down into the water
    roots = rnd.randint(9, 13)
    for i in range(roots):
        a = i / roots * math.tau + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        start = base + d * 0.25 + Vector((0, 0, rnd.uniform(-0.4, 0.6)))
        reach = rnd.uniform(2.2, 4.2)
        arch = start + d * reach * 0.45 + Vector((0, 0, rnd.uniform(0.3, 0.9)))
        foot = d * reach + Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), -0.6))
        parts.append(tube([start, arch, foot], rnd.uniform(0.11, 0.17), 0.7, BARK, res=5))
    # branches up and out to the canopy, a clump of leaves at each end and some between
    branches = rnd.randint(4, 6)
    ends = []
    for i in range(branches):
        a = i / branches * math.tau + rnd.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        start = top - Vector((0, 0, rnd.uniform(0.0, 1.4)))
        out = rnd.uniform(2.4, 3.8)
        end = top + d * out + Vector((0, 0, rnd.uniform(0.6, 1.6)))
        bend = (start + end) / 2 + Vector((0, 0, 0.6))
        parts.append(tube([start, bend, end], rnd.uniform(0.16, 0.22), 0.45, BARK))
        ends.append(end)
    for e in ends:
        parts.append(clump(e + Vector((0, 0, 0.5)), rnd.uniform(1.5, 2.0), rnd, LEAVES if rnd.random() < 0.6 else LIGHT))
        for k in range(2):
            off = Vector((rnd.uniform(-1.4, 1.4), rnd.uniform(-1.4, 1.4), rnd.uniform(0.1, 0.9)))
            parts.append(clump(e + off, rnd.uniform(1.0, 1.5), rnd, LEAVES if rnd.random() < 0.5 else LIGHT))
    parts.append(clump(top + Vector((0, 0, 1.4)), rnd.uniform(1.8, 2.3), rnd, LEAVES))
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    tree = bpy.context.active_object
    for p in tree.data.polygons:
        p.use_smooth = False
    return tree


def export(obj, name):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             use_mesh_modifiers=True, add_leaf_bones=False)


def preview(name, cam_loc, look):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = cam_loc
    cam.rotation_euler = (Vector(look) - Vector(cam_loc)).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


report = {}
for i, seed in enumerate((11, 23, 37, 53)):
    clear()
    tree = mangrove(seed)
    name = "Mangrove_0%d" % (i + 1)
    tree.name = name
    report[name] = len(tree.data.polygons)
    if i == 0:
        preview("mangrove", (17, -17, 9), (0, 0, 4.5))
    export(tree, name)
print("MANGROVES", report)
