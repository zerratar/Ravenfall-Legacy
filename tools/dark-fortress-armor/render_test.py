# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/render_test.py -- <repo> <out dir> [character] [sheets]
#
# Cuts a Dark Fortress character into MFH regions, fits the pieces to the female body and renders the comparison
# sheets (one PNG per view row; compose.py puts them together with labels):
#   apose   original | fitted male | fitted female, A-pose, front / back / side / three-quarter
#   poses   the same three in raised arms, walking stride and combat stance, front / three-quarter / game camera
#   mix     the pieces worn with existing MFH parts (plain body, plate), male and female, front / three-quarter / back
#   seams   close-ups of the neck (game camera), shoulders and elbows (raised), waist and knees (stride)
import bpy, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
name = argv[2] if len(argv) > 2 else "SM_Chr_Knight_01"
sheets = argv[3].split(",") if len(argv) > 3 else ["apose", "poses", "mix", "seams"]
cuts = json.loads(argv[4]) if len(argv) > 4 else None
os.makedirs(out, exist_ok=True)
tag = name.replace("SM_Chr_", "").replace("_01", "").lower()

L.reset()
lib = L.Library(repo)
pieces = C.cut(lib, name, lib.src, cuts=cuts, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]),
               prepare=lambda o: F.add_female_key(lib, o))
fem, pushed = F.morph_female(lib, pieces, lib.src)
print("PIECES", {k: len(v.data.vertices) for k, v in pieces.items()}, "PUSHED", pushed)

HEAD = {"m": ["Chr_Head_Male_00", "Chr_Eyebrow_Male_01", "Chr_Hair_01"],
        "f": ["Chr_Head_Female_00", "Chr_Eyebrow_Female_01", "Chr_Hair_02"]}
SEX = {"m": "Male", "f": "Female"}
# what each item slot shows, per look: MFH part names ({s} = Male/Female) or "dfa:<region>"
SLOTS = {
    "chest": {"body": ["Chr_Torso_{s}_00", "Chr_ArmUpperLeft_{s}_00", "Chr_ArmUpperRight_{s}_00"],
              "plate": ["Chr_Torso_{s}_15", "Chr_ArmUpperLeft_{s}_{ua}", "Chr_ArmUpperRight_{s}_{ua}",
                        "Chr_ShoulderAttachLeft_{sh}", "Chr_ShoulderAttachRight_{sh}"],
              "dfa": ["torso", "upperarm_L", "upperarm_R", "shoulder_L", "shoulder_R"],
              "dfa-nopads": ["torso", "upperarm_L", "upperarm_R"]},
    "gloves": {"body": ["Chr_ArmLowerLeft_{s}_00", "Chr_ArmLowerRight_{s}_00", "Chr_HandLeft_{s}_00", "Chr_HandRight_{s}_00"],
               "plate": ["Chr_ArmLowerLeft_{s}_{la}", "Chr_ArmLowerRight_{s}_{la}", "Chr_HandLeft_{s}_14", "Chr_HandRight_{s}_14"],
               "dfa": ["lowerarm_L", "lowerarm_R", "hand_L", "hand_R"]},
    "legs": {"body": ["Chr_Hips_{s}_00"], "plate": ["Chr_Hips_{s}_25", "Chr_KneeAttachLeft_06", "Chr_KneeAttachRight_06"],
             "dfa": ["hips", "knee_L", "knee_R"]},
    "boots": {"body": ["Chr_LegLeft_{s}_00", "Chr_LegRight_{s}_00"],
              "plate": ["Chr_LegLeft_{s}_{bt}", "Chr_LegRight_{s}_{bt}"],
              "dfa": ["leg_L", "leg_R"]},
}
PLATE_IDX = {"m": {"ua": "13", "sh": "20", "la": "05", "bt": "16"}, "f": {"ua": "12", "sh": "14", "la": "15", "bt": "08"}}
coll = L.shot_collection()


def figure(sex, chest="dfa", gloves="dfa", legs="dfa", boots="dfa", original=False):
    r = L.new_rig(lib, 0, coll)
    if original:
        L.attach(lib.df[name], r, coll)
        return r
    for n in HEAD[sex]:
        L.attach(lib.mfh[n], r, coll)
    look = {"chest": chest, "gloves": gloves, "legs": legs, "boots": boots}
    for slot, which in look.items():
        for part in SLOTS[slot][which]:
            if which.startswith("dfa"):
                src = (pieces if sex == "m" else fem).get(part)
                if src:
                    L.attach(src, r, coll)
            else:
                n = part.format(s=SEX[sex], **PLATE_IDX[sex])
                if n in lib.mfh:
                    L.attach(lib.mfh[n], r, coll)
                else:
                    print("MISSING", n)
    return r


def clear():
    for o in list(coll.objects):
        bpy.data.objects.remove(o, do_unlink=True)


def shoot(rigs, label, views, cell=(360, 660), spacing=1.25, height=2.05, cz=0.95):
    paths = L.shoot(rigs, os.path.join(out, "%s-%s.png" % (tag, label)), views=views, cell=cell, spacing=spacing,
                    height=height, cz=cz)
    print("SHOT", paths)


if "apose" in sheets:
    rigs = [figure("m", original=True), figure("m"), figure("f")]
    for r in rigs:
        L.pose(r, "apose")
    shoot(rigs, "apose", ("front", "back", "side", "three-quarter"), cell=(520, 860), spacing=1.3)
    clear()

if "poses" in sheets:
    rigs = []
    for p in ("raised", "walk", "combat"):
        for kind in ("orig", "m", "f"):
            r = figure("m", original=True) if kind == "orig" else figure(kind)
            L.pose(r, p)
            rigs.append(r)
    shoot(rigs, "poses", ("front", "three-quarter", "game"), cell=(330, 720), spacing=1.45, height=2.4, cz=1.05)
    clear()

if "mix" in sheets:
    looks = [dict(chest="dfa", gloves="body", legs="body", boots="body"),
             dict(chest="dfa", gloves="dfa", legs="plate", boots="plate"),
             dict(chest="plate", gloves="dfa", legs="dfa", boots="dfa"),
             dict(chest="body", gloves="plate", legs="dfa", boots="body"),
             dict(chest="dfa-nopads", gloves="body", legs="body", boots="dfa")]
    for pose in ("apose", "combat"):
        rigs = []
        for sex in ("m", "f"):
            for lk in looks:
                r = figure(sex, **lk)
                L.pose(r, pose)
                rigs.append(r)
        shoot(rigs, "mix-" + pose, ("front", "three-quarter", "back"), cell=(300, 640), spacing=1.25)
        clear()

if "seams" in sheets:
    # close-ups of the joints where items meet, one cell per figure and joint (compose.py makes the grid)
    looks = [("m", {}), ("f", {}),
             ("m", dict(chest="dfa", gloves="body", legs="plate", boots="plate")),
             ("f", dict(chest="plate", gloves="dfa", legs="dfa", boots="plate")),
             ("m", dict(chest="body", gloves="dfa", legs="dfa", boots="body"))]
    rows = [("neck", "apose", "game", "Neck", 0.62, (0, 0, -0.02)),
            ("neck-back", "apose", "back-high", "Neck", 0.62, (0, 0, -0.02)),
            ("shoulders", "raised", "front", "Spine_03", 0.95, (0, 0, 0.12)),
            ("hips-front", "apose", "front", "Hips", 0.75, (0, 0, -0.12)),
            ("elbow", "combat", "side-r", "Elbow_R", 0.6, (0, -0.05, 0)),
            ("waist", "walk", "side", "Spine_01", 0.75, (0, 0, -0.08)),
            ("knee", "walk", "three-quarter", "LowerLeg_R", 0.6, (0, 0, 0.04)),
            ("knee-combat", "combat", "front", "LowerLeg_L", 0.6, (0, 0, 0.04))]
    figs = []
    for i, (sex, lk) in enumerate(looks):
        r = figure(sex, **lk)
        figs.append(r)
    from mathutils import Vector
    for label, pose, view, bone, ortho, off in rows:
        for r in figs:
            L.pose(r, pose)
        bpy.context.view_layer.update()
        for i, r in enumerate(figs):
            r.location = (i * 4.0, i * 4.0, 0)  # diagonal, so no other figure is in line with any view
        bpy.context.view_layer.update()
        for i, r in enumerate(figs):
            L.setup_render(420, 420)
            c = r.matrix_world @ r.pose.bones[bone].head + Vector(off)
            L.camera(view, c, ortho)
            p = os.path.join(out, "cells", "%s-seam-%s-%d.png" % (tag, label, i))
            L.render(p)
    clear()
print("DONE")
