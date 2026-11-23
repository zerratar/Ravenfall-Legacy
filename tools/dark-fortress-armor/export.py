# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/export.py -- <repo> <out dir> [character] [cuts json]
#
# Cuts and fits a Dark Fortress character (cut.py, fit.py) and writes two FBX files for Unity, one per sex, each the
# rig plus the pieces named the way the MFH parts are (so they can sit beside them in the player's part arrays):
#   Chr_Torso_Male_DF<Name>, Chr_ArmUpperLeft_Male_DF<Name>, ..., Chr_Hips_Male_DF<Name>, Chr_LegLeft_Male_DF<Name>,
#   (since the second pass the pauldrons are in the upper arm pieces and the knee caps in the hips piece; with
#   cut.cut(pads="separate") they come out as Chr_ShoulderAttachLeft_DF<Name> / Chr_KneeAttachLeft_DF<Name>, one
#   set for both sexes, in the male file).
# Left and Right follow the MFH file, which is not consistent: male "LegLeft" parts sit on the character's right
# side (x < 0), female ones on the left; arms are "Left" at x > 0 for both. The names here copy whatever the MFH
# base part on that side is called.
# The rig is the Dark Fortress one (same joints as the MFH rig); its finger bones are called Finger_01_L etc. where
# the MFH rig has Finger_01 under Hand_L, and it has a Jaw bone the pieces do not use. Bind by bone path in Unity.
import bpy, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F
import skin as S

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
name = argv[2] if len(argv) > 2 else "SM_Chr_Knight_01"
cuts = json.loads(argv[3]) if len(argv) > 3 else None
os.makedirs(out, exist_ok=True)
short = name.replace("SM_Chr_", "").replace("_01", "").replace("_", "")

L.reset()
lib = L.Library(repo)
pieces = C.cut(lib, name, lib.src, cuts=cuts, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]),
               prepare=lambda o: F.add_female_key(lib, o))
fem, _ = F.morph_female(lib, pieces, lib.src)
# second pass: robes keep their whole skirt in the leggings and get the game's bare shin above the shoes; bare skin
# goes to material slot 1, "PlayerBody", which in the game is the player's own body material (FantasyHero.mat copy
# with their _Color_Skin), its faces' UVs on a skin texel of the MFH atlas
if cuts and cuts.get("robe"):
    C.add_bare_shins(lib, pieces, "Male")
    C.add_bare_shins(lib, fem, "Female")
body_slot = L.flat_material("PlayerBody", (0.8, 0.6, 0.5))
box, texel = S.skin_uv_box(repo), S.mfh_skin_texel(repo, lib)
for set_ in (pieces, fem):
    for p in set_.values():
        S.split_skin(p, box, body_slot, texel)

KIND = {"torso": "Torso", "upperarm": "ArmUpper", "lowerarm": "ArmLower", "hand": "Hand", "hips": "Hips",
        "leg": "Leg"}


def side_name(kind, sex, side):
    """'Left' or 'Right': what the MFH base part of this kind and sex on this side (L = +x) is called."""
    l = lib.mfh["Chr_%sLeft_%s_00" % (kind, sex)]
    xs = sum(v.co.x for v in l.data.vertices)
    left_is_plus_x = xs > 0
    return "Left" if (side == "L") == left_is_plus_x else "Right"


def part_name(region, sex):
    base, _, side = region.partition("_")
    if base == "shoulder":
        return "Chr_ShoulderAttach%s_DF%s" % ("Left" if side == "L" else "Right", short)
    if base == "knee":
        return "Chr_KneeAttach%s_DF%s" % ("Left" if side == "L" else "Right", short)
    kind = KIND[base]
    if side:
        return "Chr_%s%s_%s_DF%s" % (kind, side_name(kind, sex, side), sex, short)
    return "Chr_%s_%s_DF%s" % (kind, sex, short)


for sex, src in (("Male", pieces), ("Female", fem)):
    coll = bpy.data.collections.new("EXPORT_" + sex)
    bpy.context.scene.collection.children.link(coll)
    rig = lib.rig.copy()
    rig.data = lib.rig.data.copy()
    rig.name = "Armature"
    coll.objects.link(rig)
    for pb in rig.pose.bones:
        pb.matrix_basis.identity()
    names = []
    for region, p in sorted(src.items()):
        if sex == "Female" and region.startswith(("shoulder", "knee")):
            continue  # the pads are shared: the male file has them
        o = p.copy()
        o.data = p.data.copy()
        o.name = o.data.name = part_name(region, sex)
        for k in list(o.keys()):
            del o[k]
        coll.objects.link(o)
        o.parent = rig
        o.matrix_parent_inverse = p.matrix_parent_inverse.copy()
        for m in o.modifiers:
            if m.type == "ARMATURE":
                m.object = rig
        names.append(o.name)
    bpy.ops.object.select_all(action="DESELECT")
    for o in coll.objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path = os.path.join(out, "DF%s_%s.fbx" % (short, sex))
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
                             add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", use_mesh_modifiers=False, primary_bone_axis="Y",
                             secondary_bone_axis="X")
    print("EXPORTED", path, names)
    for o in list(coll.objects):
        bpy.data.objects.remove(o, do_unlink=True)
print("DONE")
