# The ruined hall walls round the first band's open floors, the Overgrown Halls and the drowned Marsh (concepts
# docs/mockups/tower/concept-overgrown.jpg and concept-marsh.jpg), made in Blender from the command line:
#   blender -b --python tools/tower/blender/tower_halls.py -- <repo root>
# The band is "the Overgrown Halls", inside the tower, but both floors were open fields in a bowl of wooded hills under
# the sky. Each now stands inside a vast ruined hall: a ring of tall stone walls with piers and pointed windows, its
# top broken, whole stretches fallen so the sun pours in, giant roots climbing over it. Like the caverns it shows from
# inside only (the faces that look outward are gone), so the camera is never walled off from the players. Exported as FBX
# into Assets/World/Tower/Halls/Models (OvergrownHall, MarshHall), previews in Temp/Blender. Materials "Stone", "Moss",
# "Root"; the Unity builders assign their own by name.
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
    global STONE, MOSS, ROOT_M
    STONE = material("Stone", (0.55, 0.53, 0.48))
    MOSS = material("Moss", (0.30, 0.42, 0.20))
    ROOT_M = material("Root", (0.30, 0.20, 0.12))


def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    return m


def link(obj):
    bpy.context.collection.objects.link(obj)
    return obj


def ring_wall(radius, thickness, base, height, segments):
    """A closed ring of wall, its outline a polygon of segments sides (Blender x east, y north = Unity z)."""
    bm = bmesh.new()
    inner, outer = [], []
    for i in range(segments):
        a = i / segments * math.tau
        for lst, r in ((inner, radius), (outer, radius + thickness)):
            lst.append((math.cos(a) * r, math.sin(a) * r))
    rings = []
    for z in (base, base + height):
        rings.append(([bm.verts.new((x, y, z)) for x, y in inner], [bm.verts.new((x, y, z)) for x, y in outer]))
    (ib, ob), (it, ot) = rings
    for i in range(segments):
        j = (i + 1) % segments
        bm.faces.new((ib[i], ib[j], it[j], it[i]))      # inner face (faces in)
        bm.faces.new((ob[j], ob[i], ot[i], ot[j]))      # outer face
        bm.faces.new((it[i], it[j], ot[j], ot[i]))      # top
        bm.faces.new((ob[i], ob[j], ib[j], ib[i]))      # bottom
    bm.normal_update()
    mesh = bpy.data.meshes.new("wall")
    bm.to_mesh(mesh)
    bm.free()
    return link(bpy.data.objects.new("wall", mesh))


def window_cutter(a, radius, thickness, sill, width, height, name):
    """A pointed-arch prism through the wall at angle a: a box with a gothic point on top."""
    bm = bmesh.new()
    half = width / 2
    # the profile in the wall's plane: u along the wall, v up
    prof = [(-half, 0.0), (half, 0.0), (half, height - width * 0.75)]
    steps = 6
    for k in range(1, steps + 1):          # right arc up to the point
        t = k / steps
        prof.append((half * (1 - t) ** 1.2, height - width * 0.75 + width * 0.75 * math.sin(t * math.pi / 2)))
    for k in range(steps - 1, 0, -1):      # left arc back down
        t = k / steps
        prof.append((-half * (1 - t) ** 1.2, height - width * 0.75 + width * 0.75 * math.sin(t * math.pi / 2)))
    prof.append((-half, height - width * 0.75))
    tangent = Vector((-math.sin(a), math.cos(a), 0))
    normal = Vector((math.cos(a), math.sin(a), 0))
    front, back = [], []
    for u, v in prof:
        p = normal * (radius - 2.0) + tangent * u + Vector((0, 0, sill + v))
        front.append(bm.verts.new(p))
        back.append(bm.verts.new(p + normal * (thickness + 4.0)))
    n = len(prof)
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((front[i], back[i], back[j], front[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    return link(bpy.data.objects.new(name, mesh))


def pilaster(a, radius, base, height):
    """A pier standing proud of the wall's inside at angle a, between windows: the hall's ribs."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    p = bpy.context.active_object
    p.scale = (2.2, 2.0, height)
    p.location = (math.cos(a) * (radius - 0.7), math.sin(a) * (radius - 0.7), base + height / 2)
    p.rotation_euler = (0, 0, a)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return p


def join(objs, into):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    into.select_set(True)
    bpy.context.view_layer.objects.active = into
    bpy.ops.object.join()
    return into


def boolean(target, cutters, op="DIFFERENCE"):
    cut = join(cutters[1:], cutters[0]) if len(cutters) > 1 else cutters[0]
    mod = target.modifiers.new("cut", "BOOLEAN")
    mod.object = cut
    mod.operation = op
    mod.solver = "EXACT"
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut)


def ruin(obj, base, height, seed, fallen):
    """Breaks the wall's top: every vertex near the top drops by a ragged amount, a lot where a stretch has fallen."""
    for v in obj.data.vertices:
        if v.co.z < base + height * 0.35:
            continue
        a = math.atan2(v.co.y, v.co.x)
        ragged = 0.5 + 0.5 * noise.noise(Vector((math.cos(a) * 9.0, math.sin(a) * 9.0, seed)))
        drop = 2.0 + 7.0 * ragged
        f = fallen(a)
        drop += f * height * 0.62
        top = base + height - drop
        if v.co.z > top:
            v.co.z = max(top, base + height * 0.3)


def keep_inside(obj, radius, thickness):
    """Deletes faces that look outward from the hall (and the hidden bottoms), so the walls show from inside only."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.normal_update()
    gone = []
    for f in bm.faces:
        c = f.calc_center_median()
        radial = Vector((c.x, c.y, 0)).normalized()
        if f.normal.dot(radial) > 0.35 or f.normal.z < -0.7:
            gone.append(f)
    bmesh.ops.delete(bm, geom=gone, context="FACES")
    bm.to_mesh(obj.data)
    bm.free()


def roots(count, radius, base, height, seed):
    """Giant roots: down over the wall's inside from its top and out across the hall floor, thick and tapering."""
    rnd = random.Random(seed)
    parts = []
    for i in range(count):
        a = rnd.uniform(0, math.tau)
        top = base + height * rnd.uniform(0.45, 0.85)
        reach = rnd.uniform(6.0, 16.0)
        curve = bpy.data.curves.new("root", "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = rnd.uniform(1.1, 2.2)
        curve.bevel_resolution = 0
        curve.resolution_u = 5
        spline = curve.splines.new("BEZIER")
        pts = [
            (radius + 1.0, top),
            (radius - 0.6, top * 0.6 + base * 0.4),
            (radius - 1.2, base + 3.0),
            (radius - 3.0 - reach * 0.4, base + 3.4),
            (radius - 3.0 - reach, base + 2.6),
        ]
        spline.bezier_points.add(len(pts) - 1)
        da = rnd.uniform(-0.05, 0.05)
        for k, (r, z) in enumerate(pts):
            t = k / (len(pts) - 1)
            aa = a + da * t * 3 + 0.02 * math.sin(t * 9 + i)
            bp = spline.bezier_points[k]
            bp.co = (math.cos(aa) * r, math.sin(aa) * r, z)
            bp.handle_left_type = bp.handle_right_type = "AUTO"
            bp.radius = 1.0 - 0.7 * t
        obj = link(bpy.data.objects.new("root", curve))
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target="MESH")
        obj.data.materials.append(ROOT_M)
        parts.append(obj)
    return parts


def faceted_flat(obj):
    for p in obj.data.polygons:
        p.use_smooth = False


def by_height(obj, moss_below, seed):
    """Moss low on the walls and on the ledges (faces looking up), stone above."""
    s, m = slot(obj, STONE), slot(obj, MOSS)
    for p in obj.data.polygons:
        c = p.center
        # moss on the ledges and sills (faces looking up) and right at the foot, where the ground meets the wall
        wet = c.z < moss_below and abs(p.normal.z) < 0.3 and (p.area < 40.0)
        p.material_index = m if (wet or p.normal.z > 0.6) else s


def slot(obj, mat):
    names = [x.name if x else None for x in obj.data.materials]
    if mat.name not in names:
        obj.data.materials.append(mat)
        names.append(mat.name)
    return names.index(mat.name)


def uv_box(obj, scale):
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=scale)
    bpy.ops.object.mode_set(mode="OBJECT")


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
    scene.display.shading.show_backface_culling = True
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    cam_data = bpy.data.cameras.new("cam")
    cam = link(bpy.data.objects.new("cam", cam_data))
    cam.location = cam_loc
    cam.rotation_euler = (Vector(look) - Vector(cam_loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.clip_end = 2000
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


report = {}
RADIUS, THICK, BASE, HEIGHT, BAYS = 74.0, 3.0, -6.0, 40.0, 28
for name, seed, gaps, root_count, moss in (
    # the arrival is in the south (-90 degrees): a fallen stretch there opens the hall to the camera's way in
    ("OvergrownHall", 3.3, (-90.0, 30.0, 150.0, 215.0), 22, 2.0),
    ("MarshHall", 6.6, (-90.0, 60.0, 120.0, 250.0, 330.0), 28, 4.5),
):
    clear()
    rnd = random.Random(int(seed * 100))
    wall = ring_wall(RADIUS, THICK, BASE, HEIGHT, BAYS * 4)
    def fallen(a, gaps=gaps):
        f = 0.0
        for g in gaps:
            d = abs((math.degrees(a) - g + 180.0) % 360.0 - 180.0)
            f = max(f, max(0.0, 1.0 - d / 11.0))
        return f
    cutters, extras = [], []
    for i in range(BAYS):
        a = (i + 0.5) / BAYS * math.tau
        if fallen(a) > 0.5:
            continue
        cutters.append(window_cutter(a, RADIUS, THICK, sill=10.0 + rnd.uniform(-1, 1), width=5.2, height=14.0, name="win"))
    for i in range(BAYS):
        a = i / BAYS * math.tau
        if fallen(a) > 0.6:
            continue
        extras.append(pilaster(a, RADIUS, BASE, HEIGHT * rnd.uniform(0.75, 1.0)))
        # (no buttresses: they stand outside, where nothing shows, and their sides floated in wide shots)
    # the windows go through the plain ring (a clean solid), the piers and buttresses join after
    boolean(wall, cutters)
    wall = join(extras, wall)
    # more cuts up the wall so the ruin's broken top has vertices to break
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    for z in (BASE + HEIGHT * 0.45, BASE + HEIGHT * 0.62, BASE + HEIGHT * 0.78, BASE + HEIGHT * 0.9):
        bpy.ops.mesh.bisect(plane_co=(0, 0, z), plane_no=(0, 0, 1), use_fill=False)
        bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    ruin(wall, BASE, HEIGHT, seed, fallen)
    keep_inside(wall, RADIUS, THICK)
    by_height(wall, 1.0 + moss * 0.3, seed)
    faceted_flat(wall)
    wall = join(roots(root_count, RADIUS, BASE, HEIGHT, int(seed * 10)), wall)
    uv_box(wall, 7.0)
    wall.name = name
    report[name] = len(wall.data.polygons)
    preview(name.lower() + "-inside", (0, -40, 14), (0, 60, 12))
    preview(name.lower() + "-above", (-90, -140, 120), (0, 0, 0))
    export(wall, name)
print("HALLS", report)
