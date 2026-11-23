# Shared helpers for the Dark Fortress armor test (headless Blender 5.x).
#
# The Dark Fortress characters and the Modular Fantasy Hero (MFH) parts sit on the same POLYGON skeleton (every joint
# within 0.1 mm), but Blender's FBX importer brings the MFH file in with its bones in centimeters and its meshes in
# meters, so posing the MFH armature tears the meshes apart. The working rig is therefore the Dark Fortress armature:
# MFH meshes are baked to world space and bound to it, with their finger groups renamed to the Dark Fortress names
# (MFH "Finger_01" / "Finger_01.001" are the left / right "Finger_01_L" / "Finger_01_R").
import bpy, os, math
from mathutils import Vector, Matrix

DF_FBX = "Assets/Synty/PolygonDarkFortress/Models/Characters.fbx"
MFH_FBX = "Assets/PolygonFantasyHeroCharacters/Models/ModularCharacters.fbx"
DF_TEX = "Assets/Synty/PolygonDarkFortress/Textures/Alts/PolygonDarkFortress_Texture_01_A.png"
MFH_TEX = "Assets/PolygonFantasyHeroCharacters/Materials/StandardMaterials/PolygonFantasyHero_Texture_01_A.png"

FINGERS = ["Thumb_01", "Thumb_02", "Thumb_03", "IndexFinger_01", "IndexFinger_02", "IndexFinger_03", "IndexFinger_04",
           "Finger_01", "Finger_02", "Finger_03", "Finger_04"]


# MFH attachment bones (rigid pads, capes) the Dark Fortress rig lacks: they never move on their own, so their parent
# stands in for them
ATTACH_PARENT = {"Shoulder_Attachment_L": "Clavicle_L", "Shoulder_Attachment_R": "Clavicle_R",
                 "Elbow_Attachment_L": "Elbow_L", "Elbow_Attachment_R": "Elbow_R",
                 "Knee_Attachment_L": "LowerLeg_L", "Knee_Attachment_R": "LowerLeg_R",
                 "Hips_Attachment": "Hips", "Chest_Attachment": "Spine_03", "Back_Attachment": "Spine_03",
                 "Head_Attachment": "Head"}


def mfh_to_df_group(name):
    """An MFH vertex group name as the Dark Fortress rig calls the bone."""
    if name in ATTACH_PARENT:
        return ATTACH_PARENT[name]
    base = name[:-4] if name.endswith(".001") else name
    if base in FINGERS:
        return base + ("_R" if name.endswith(".001") else "_L")
    return name


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _import(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    return [o for o in bpy.data.objects if o not in before]


def image_material(name, image_path):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(image_path, check_existing=True)
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.85
    nt.nodes.active = tex
    return mat


def flat_material(name, rgb):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb, 1)
    return mat


class Library:
    """Everything imported once: the Dark Fortress characters on their rig, and the MFH parts bound to that rig.
    All source objects are kept in a hidden collection; use instance() to put copies into the shot."""

    def __init__(self, repo):
        self.repo = repo
        self.src = bpy.data.collections.new("SRC")
        bpy.context.scene.collection.children.link(self.src)
        df_mat = image_material("DF", os.path.join(repo, DF_TEX))
        mfh_mat = image_material("MFH", os.path.join(repo, MFH_TEX))

        df = _import(os.path.join(repo, DF_FBX))
        self.rig = next(o for o in df if o.type == "ARMATURE")
        self.rig.name = "RIG"
        self.df = {}
        for o in df:
            self._move_to_src(o)
            if o.type == "MESH":
                o.data.materials.clear()
                o.data.materials.append(df_mat)
                self.df[o.name] = o

        mfh = _import(os.path.join(repo, MFH_FBX))
        self.mfh = {}
        for o in mfh:
            if o.type != "MESH":
                continue
            mw = o.matrix_world.copy()
            o.parent = None
            o.data.transform(mw)
            o.matrix_world = Matrix.Identity(4)
            for m in list(o.modifiers):
                o.modifiers.remove(m)
            for g in o.vertex_groups:
                g.name = mfh_to_df_group(g.name)
            o.data.materials.clear()
            o.data.materials.append(mfh_mat)
            self._move_to_src(o)
            self.mfh[o.name] = o
        for o in mfh:
            if o.type != "MESH":
                bpy.data.objects.remove(o, do_unlink=True)
        # DF meshes live in the rig's (centimetre) space; MFH ones are now in world meters: give both a rig binding
        for o in self.mfh.values():
            o.parent = self.rig
            o.matrix_parent_inverse = self.rig.matrix_world.inverted()
            mod = o.modifiers.new("Armature", "ARMATURE")
            mod.object = self.rig
        self.src.hide_render = True
        self.src.hide_viewport = True

    def _move_to_src(self, o):
        for c in list(o.users_collection):
            c.objects.unlink(o)
        self.src.objects.link(o)


def shot_collection(name="SHOT"):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def new_rig(lib, offset_x, coll):
    """A copy of the rig (shared bone data, its own pose) standing offset_x meters along X."""
    r = lib.rig.copy()
    r.animation_data_clear()
    coll.objects.link(r)
    r.location.x += offset_x
    for pb in r.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    return r


def attach(mesh_src, rig, coll, mesh_copy=True):
    """A copy of a source mesh, skinned to rig."""
    o = mesh_src.copy()
    if mesh_copy:
        o.data = mesh_src.data.copy()
    coll.objects.link(o)
    o.parent = rig
    # the source's parent inverse: DF meshes are in rig space, MFH ones in world meters
    o.matrix_parent_inverse = mesh_src.matrix_parent_inverse.copy()
    o.matrix_basis = mesh_src.matrix_basis.copy()
    for m in o.modifiers:
        if m.type == "ARMATURE":
            m.object = rig
    if not any(m.type == "ARMATURE" for m in o.modifiers):
        m = o.modifiers.new("Armature", "ARMATURE")
        m.object = rig
    return o


# ---- posing in world terms ---------------------------------------------------------------------------------------

def rotate_world(rig, bone, axis, deg):
    """Rotate a pose bone about a world axis through its head (after whatever its parents did)."""
    bpy.context.view_layer.update()
    pb = rig.pose.bones[bone]
    A = rig.matrix_world
    head_w = A @ pb.head
    R = Matrix.Translation(head_w) @ Matrix.Rotation(math.radians(deg), 4, Vector(axis)) @ Matrix.Translation(-head_w)
    pb.matrix = A.inverted() @ R @ A @ pb.matrix
    bpy.context.view_layer.update()


POSES = {
    # A-pose: arms down 50 degrees, as the game's idle roughly holds them
    "apose": [("Shoulder_L", (0, 1, 0), 52), ("Shoulder_R", (0, 1, 0), -52),
              ("Elbow_L", (0, 0, 1), 8), ("Elbow_R", (0, 0, 1), -8)],
    # both arms raised above the head (shoulder pads and armpits)
    "raised": [("Shoulder_L", (0, 1, 0), -60), ("Shoulder_R", (0, 1, 0), 60),
               ("Elbow_L", (0, 1, 0), -35), ("Elbow_R", (0, 1, 0), 35)],
    # walking stride: left leg forward, right back with a bent knee, arms swinging the other way
    "walk": [("UpperLeg_L", (1, 0, 0), -32), ("LowerLeg_L", (1, 0, 0), 12),
             ("UpperLeg_R", (1, 0, 0), 22), ("LowerLeg_R", (1, 0, 0), 38),
             ("Shoulder_L", (0, 1, 0), 72), ("Shoulder_R", (0, 1, 0), -72),
             ("Shoulder_L", (1, 0, 0), 22), ("Shoulder_R", (1, 0, 0), -26),
             ("Elbow_L", (1, 0, 0), 10), ("Elbow_R", (1, 0, 0), -30),
             ("Spine_02", (0, 0, 1), 6)],
    # combat stance: wide legs, bent knees, twisted torso, sword arm forward and bent, shield arm up
    "combat": [("UpperLeg_L", (0, 1, 0), -12), ("UpperLeg_L", (1, 0, 0), -28), ("LowerLeg_L", (1, 0, 0), 38),
               ("UpperLeg_R", (0, 1, 0), 12), ("UpperLeg_R", (1, 0, 0), 14), ("LowerLeg_R", (1, 0, 0), 30),
               ("Spine_01", (1, 0, 0), 8), ("Spine_02", (0, 0, 1), 14),
               ("Shoulder_R", (0, 1, 0), -62), ("Shoulder_R", (0, 0, 1), 40), ("Elbow_R", (1, 0, 0), -70),
               ("Shoulder_L", (0, 1, 0), 45), ("Shoulder_L", (0, 0, 1), 55), ("Elbow_L", (0, 0, 1), 75)],
    "tpose": [],
}


def pose(rig, name):
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    for bone, axis, deg in POSES[name]:
        if deg:
            rotate_world(rig, bone, axis, deg)


# ---- rendering -------------------------------------------------------------------------------------------------------

VIEWS = {  # camera direction (from the subject towards the camera), in world terms; the characters face -Y
    "front": (0, -1, 0), "back": (0, 1, 0), "side": (1, 0, 0), "three-quarter": (0.75, -1, 0),
    "side-r": (-1, 0, 0),
    "game": (0.35, -1, 1.0),  # the elevated stream camera
    "back-high": (-0.5, 1, 0.9),
}


def setup_render(width, height, bg=(0.13, 0.14, 0.16)):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "TEXTURE"
    sh.show_backface_culling = True
    sh.show_shadows = False
    sh.show_cavity = False
    sh.show_object_outline = False
    sc.render.resolution_x = width
    sc.render.resolution_y = height
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.display.render_aa = "16"
    if not sc.world:
        sc.world = bpy.data.worlds.new("W")
    sh.background_type = "VIEWPORT"
    sh.background_color = bg
    sc.render.image_settings.file_format = "PNG"
    sc.view_settings.view_transform = "Standard"


def camera(view, center, ortho_scale, dist=8.0):
    sc = bpy.context.scene
    cam = sc.camera
    if not cam:
        cam = bpy.data.objects.new("CAM", bpy.data.cameras.new("CAM"))
        sc.collection.objects.link(cam)
        sc.camera = cam
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho_scale
    d = Vector(VIEWS[view]).normalized() if isinstance(view, str) else Vector(view).normalized()
    cam.location = Vector(center) + d * dist
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 100


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def layout(rigs, view, spacing=1.25):
    """Line the rigs up across the picture for a view: along the camera's right vector on the ground, so every view
    (back, side, three-quarter, the elevated game camera) shows them in the same order and evenly spaced."""
    n = len(rigs)
    d = Vector(VIEWS[view])
    right = Vector((-d.y, d.x, 0)).normalized()
    for i, r in enumerate(rigs):
        off = (i - (n - 1) / 2) * spacing
        r.location = right * off
    bpy.context.view_layer.update()
    return (n - 1) * spacing


def shoot(rigs, out_path, views=("front", "back", "side", "three-quarter"), cell=(340, 620), spacing=1.25,
          height=2.05, cz=0.95):
    """One picture per view of all rigs side by side; returns the paths."""
    paths = []
    for v in views:
        span = layout(rigs, v, spacing)
        width_m = span + spacing
        setup_render(int(cell[0] * len(rigs)), cell[1])
        ortho = max(width_m, height * cell[0] * len(rigs) / cell[1])
        camera(v, (0, 0, cz), ortho)
        p = out_path.replace(".png", "-%s.png" % v)
        render(p)
        paths.append(p)
    layout(rigs, "front", spacing)
    return paths
