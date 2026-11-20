# The caverns round the tower's dungeon floors (concepts docs/mockups/tower/concept-crypt.jpg and concept-frostvault.jpg),
# made in Blender from the command line:
#   blender -b --python tools/tower/blender/tower_caves.py -- <repo root>
# The Crypt and the Frozen Vault were rooms floating in a void with the sky showing past their walls. Each now sits in a
# cavern: a rubble floor between the rooms, walls rising round them, and a ceiling over it all. Exported as FBX into
# Assets/World/Tower/Caves/Models, previews in Temp/Blender:
#   <Name>Cave      the floor and walls, one heightfield round the rooms (faces up: seen from inside and from above)
#   <Name>Ceiling   the vault over it, faces down: it shows from inside the cave only, so the game's camera, which
#                   looks down from above, still sees the floor through it
# The rooms (local metres, x east, z north in Unity) are the builders' (tools/tower/tower-crypt-build.cs and
# tower-frostvault-build.cs). Materials "Rock", "Earth", "Root", "Ice"; the Unity builders assign their own by name.
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Assets", "World", "Tower", "Caves", "Models")
PREVIEW = os.path.join(ROOT, "Temp", "Blender")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREVIEW, exist_ok=True)

ROOMS = [(-10, -75, 10, -50), (-15, -50, 15, 10), (-45, -40, -15, -10), (15, -40, 45, -10), (-30, 10, 30, 60)]


def sdf(x, y):
    """Distance to the nearest room, negative inside one (Blender y is Unity's z)."""
    best = 1e9
    for x0, y0, x1, y1 in ROOMS:
        dx = max(x0 - x, 0.0, x - x1)
        dy = max(y0 - y, 0.0, y - y1)
        if dx == 0.0 and dy == 0.0:
            d = -min(x - x0, x1 - x, y - y0, y1 - y)
        else:
            d = math.hypot(dx, dy)
        best = min(best, d)
    return best


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global ROCK, EARTH, ROOT_M, ICE
    ROCK = material("Rock", (0.33, 0.32, 0.31))
    EARTH = material("Earth", (0.30, 0.24, 0.17))
    ROOT_M = material("Root", (0.28, 0.19, 0.11))
    ICE = material("Ice", (0.55, 0.78, 0.95))


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


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


class Cave:
    """One cavern's shape: how far the floor runs out from the rooms (margin), how tall the walls are, the noise seed,
    and how spiky the walls and the ceiling are (ice is sharper than rock)."""

    def __init__(self, margin, wall, seed, spikes, shape=None):
        self.margin, self.wall, self.seed, self.spikes = margin, wall, seed, spikes
        # the outline the cave grows from: distance to it, negative inside (the rooms by default)
        self.shape = shape or sdf
        self.width = 8.0   # the wall's run from the cave floor to its top

    def m(self, x, y):
        # the floor's edge wanders in and out, so the cave is not the rooms' outline grown
        return self.margin + 5.0 * noise.noise(Vector((x * 0.035, y * 0.035, self.seed)))

    def ground(self, x, y):
        d = self.shape(x, y)
        m = self.m(x, y)
        n = noise.noise(Vector((x * 0.12, y * 0.12, self.seed + 3.0)))
        floor = -0.7 + 0.3 * n
        if d < m:
            # rubble and boulders between the rooms, fewer right by them; the floor rises a little toward the walls
            rubble = max(0.0, noise.noise(Vector((x * 0.32, y * 0.32, self.seed + 7.0))) - 0.25) * 2.2
            return floor + rubble * smooth((d - 1.5) / 4.0) + 1.6 * (max(d, 0.0) / m) ** 3
        t = min(1.0, (d - m) / self.width)
        # ribs that run up the wall: noise that changes fast along it; ledges now and then
        rib = noise.noise(Vector((x * 0.28, y * 0.28, self.seed + 11.0)))
        ledge = 0.06 * math.sin(t * 17.0 + rib * 3.0)
        # the walls rise higher in places (never lower than the ceiling's edge, or a slit would show between them)
        tall = self.wall * (1.0 + 0.45 * max(0.0, noise.noise(Vector((x * 0.022, y * 0.022, self.seed + 5.0)))))
        h = floor + 1.6 + tall * (t ** 0.55) * (1.0 + ledge) + (2.2 + 2.0 * self.spikes) * rib * t
        if d > m + self.width:
            h = floor + 1.6 + tall + 1.5 * rib
        return h

    def ceiling(self, x, y):
        d = self.shape(x, y)
        m = self.m(x, y)
        # a vault: highest over the middle of the cave, meeting the walls (and running on inside the rock past them)
        c = smooth((m + self.width - d) / 22.0)
        h = self.wall + 0.5 + 6.0 * math.sqrt(c)
        # stalactites, sharper and longer in ice
        drip = max(0.0, noise.noise(Vector((x * 0.22, y * 0.22, self.seed + 21.0))) - 0.15)
        h -= drip * (3.0 + 5.0 * self.spikes) * c
        return h


def heightfield(name, f, keep, step=1.5, x0=-80.0, x1=80.0, y0=-110.0, y1=95.0, down=False):
    """A grid mesh with z = f(x, y), cells kept where keep(x, y) holds for a corner; faces down if asked."""
    bm = bmesh.new()
    nx = int((x1 - x0) / step) + 1
    ny = int((y1 - y0) / step) + 1
    grid = {}
    for j in range(ny):
        for i in range(nx):
            x = x0 + i * step
            y = y0 + j * step
            grid[(i, j)] = (x, y)
    verts = {}
    def v(i, j):
        if (i, j) not in verts:
            x, y = grid[(i, j)]
            verts[(i, j)] = bm.verts.new((x, y, f(x, y)))
        return verts[(i, j)]
    for j in range(ny - 1):
        for i in range(nx - 1):
            corners = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            if not any(keep(*grid[c]) for c in corners):
                continue
            vs = [v(*c) for c in corners]
            bm.faces.new(list(reversed(vs)) if down else vs)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def faceted(obj, ratio):
    mod = obj.modifiers.new("decimate", "DECIMATE")
    mod.ratio = ratio
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = False


def by_slope(obj, flat, steep, cut=0.72):
    a, b = slot(obj, flat), slot(obj, steep)
    obj.data.update()
    for p in obj.data.polygons:
        p.material_index = a if abs(p.normal.z) > cut else b


def uv_box(obj, scale):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=scale)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def hanging(parent, cave, count, seed, kind):
    """Roots (crypt) or icicles (vault) hanging from the ceiling over the open floor, joined into the ceiling."""
    rnd = random.Random(seed)
    parts = []
    tries = 0
    while len(parts) < count and tries < count * 40:
        tries += 1
        x = rnd.uniform(-70, 70)
        y = rnd.uniform(-100, 85)
        d = cave.shape(x, y)
        if d > cave.m(x, y) + 2.0 or d < -6.0:
            continue
        top = cave.ceiling(x, y) + 0.5
        if kind == "root":
            length = rnd.uniform(4.0, 11.0)
            curve = bpy.data.curves.new("root", "CURVE")
            curve.dimensions = "3D"
            curve.bevel_depth = rnd.uniform(0.3, 0.65)
            curve.bevel_resolution = 0
            curve.resolution_u = 4
            spline = curve.splines.new("BEZIER")
            n = 5
            spline.bezier_points.add(n - 1)
            wx, wy = rnd.uniform(-1, 1), rnd.uniform(-1, 1)
            for k, pt in enumerate(spline.bezier_points):
                t = k / (n - 1)
                px = x + wx * 2.5 * t + 0.6 * math.sin(t * 7 + seed)
                py = y + wy * 2.5 * t + 0.6 * math.cos(t * 6 + seed)
                pt.co = (px, py, top - length * t)
                pt.handle_left_type = pt.handle_right_type = "AUTO"
                pt.radius = 1.0 - 0.85 * t
            obj = bpy.data.objects.new("root", curve)
            bpy.context.collection.objects.link(obj)
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.convert(target="MESH")
            obj.select_set(False)
            obj.data.materials.append(ROOT_M)
        else:
            length = rnd.uniform(2.5, 8.0)
            bpy.ops.mesh.primitive_cone_add(vertices=rnd.choice((5, 6, 7)), radius1=rnd.uniform(0.6, 1.4), radius2=0.0, depth=length,
                                            location=(x, y, top - length / 2), rotation=(math.pi, 0, rnd.uniform(0, math.pi)))
            obj = bpy.context.active_object
            obj.data.materials.append(ICE)
        parts.append(obj)
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    parent.select_set(True)
    bpy.context.view_layer.objects.active = parent
    bpy.ops.object.join()
    return parent


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
    cam = bpy.data.objects.new("cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = cam_loc
    cam.rotation_euler = (Vector(look) - Vector(cam_loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.clip_end = 2000
    scene.camera = cam
    scene.render.filepath = os.path.join(PREVIEW, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


report = {}
for name, cave, wall_mat, hang, hang_count in (
    ("Crypt", Cave(margin=11.0, wall=24.0, seed=2.7, spikes=0.0), "Rock", "root", 70),
    ("FrostVault", Cave(margin=10.0, wall=26.0, seed=8.1, spikes=1.0), "Ice", "icicle", 110),
):
    clear()
    walls = ROCK if wall_mat == "Rock" else ICE
    keep_floor = lambda x, y, c=cave: c.shape(x, y) < c.m(x, y) + c.width + 6.0
    ground = heightfield(name + "Cave", cave.ground, keep_floor)
    faceted(ground, 0.32)
    by_slope(ground, EARTH, walls)
    uv_box(ground, 6.0)
    report[name + "Cave"] = len(ground.data.polygons)

    keep_ceiling = lambda x, y, c=cave: c.shape(x, y) < c.m(x, y) + c.width + 4.0
    ceiling = heightfield(name + "Ceiling", cave.ceiling, keep_ceiling, step=2.5, down=True)
    faceted(ceiling, 0.4)
    ceiling.data.materials.append(walls)
    ceiling = hanging(ceiling, cave, hang_count, 31 if hang == "root" else 47, hang)
    uv_box(ceiling, 6.0)
    report[name + "Ceiling"] = len(ceiling.data.polygons)

    preview(name.lower() + "-cave-above", (-60, -150, 110), (0, -10, 0))
    preview(name.lower() + "-cave-inside", (0, -95, 9), (0, 20, 6))
    export(ground, name + "Cave")
    export(ceiling, name + "Ceiling")

# the Glacier (concept-glacier.jpg): no cavern, but a ring of blue ice cliffs round the valley, snow on their tops, in front
# of the white hills; open to the sky, so no ceiling. Only the ring is kept (inside it is the terrain).
clear()
glacier = Cave(margin=9.0, wall=27.0, seed=5.3, spikes=0.7, shape=lambda x, y: math.hypot(x, y) - 64.0)
keep_ring = lambda x, y, c=glacier: c.m(x, y) - 3.0 < c.shape(x, y) < c.m(x, y) + c.width + 7.0
cliffs = heightfield("GlacierCliffs", glacier.ground, keep_ring, x0=-105.0, x1=105.0, y0=-105.0, y1=105.0)
faceted(cliffs, 0.32)
by_slope(cliffs, EARTH, ICE)
uv_box(cliffs, 6.0)
report["GlacierCliffs"] = len(cliffs.data.polygons)
preview("glacier-cliffs-inside", (0, -40, 10), (0, 60, 10))
export(cliffs, "GlacierCliffs")

print("CAVES", report)
