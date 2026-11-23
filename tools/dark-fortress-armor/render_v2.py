# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/render_v2.py -- <repo> <out dir> <character> [sheets] [cuts json]
#
# Second pass renders: the cut and fitted pieces (cut.py with the pads baked in, fit.py) posed by the game's own
# animation clips (anim.py), with bare skin in the player's skin color (skin.py). Sheets (one PNG per view row;
# compose_v2.py labels them):
#   anim   original | male | female, in the relaxed idle, the run, a sword attack and the woodcutting chop
#   mix    the pieces worn with existing game parts (bare body, the plate set), male and female, idle and attack
#   skin   male and female in a light, a medium and a dark skin tone
#   pads   the baked pauldrons against the game's own shoulder pads, idle and attack
#   seams  close-ups of neck, shoulders, elbows, waist and knees in game frames (cells; compose_v2.py makes the grid)
import bpy, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F
import anim as A
import skin as S

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out, name = argv[0], argv[1], argv[2]
sheets = argv[3].split(",") if len(argv) > 3 else ["anim", "mix", "skin", "pads", "seams"]
cuts = json.loads(argv[4]) if len(argv) > 4 and argv[4] else None
os.makedirs(out, exist_ok=True)
tag = name.replace("SM_Chr_", "").lower()
if tag.endswith("_01") and not tag.startswith("undead"):
    tag = tag[:-3]

L.reset()
lib = L.Library(repo)
tm = A.target_map(repo)
pieces = C.cut(lib, name, lib.src, cuts=cuts, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]),
               prepare=lambda o: F.add_female_key(lib, o))
fem, _ = F.morph_female(lib, pieces, lib.src)
if cuts and cuts.get("robe"):
    C.add_bare_shins(lib, pieces, "Male")
    C.add_bare_shins(lib, fem, "Female")
box = S.skin_uv_box(repo)
texel = S.mfh_skin_texel(repo, lib)
skin_placeholder = L.flat_material("SKIN", (0.8, 0.6, 0.5))
nskin = {}
for set_ in (pieces, fem):
    for r, p in set_.items():
        nskin[r] = S.split_skin(p, box, skin_placeholder, texel)
print("PIECES", {k: len(v.data.vertices) for k, v in pieces.items()}, "SKIN FACES", nskin)
tones = {k: S.tone_material(repo, lib, "TONE_" + k, rgb) for k, rgb in S.TONES.items()}

HEAD = {"m": ["Chr_Head_Male_00", "Chr_Eyebrow_Male_01", "Chr_Hair_01"],
        "f": ["Chr_Head_Female_00", "Chr_Eyebrow_Female_01", "Chr_Hair_02"]}
SEX = {"m": "Male", "f": "Female"}
SLOTS = {
    "chest": {"body": ["Chr_Torso_{s}_00", "Chr_ArmUpperLeft_{s}_00", "Chr_ArmUpperRight_{s}_00"],
              "plate": ["Chr_Torso_{s}_15", "Chr_ArmUpperLeft_{s}_{ua}", "Chr_ArmUpperRight_{s}_{ua}",
                        "Chr_ShoulderAttachLeft_{sh}", "Chr_ShoulderAttachRight_{sh}"],
              "dfa": ["torso", "upperarm_L", "upperarm_R"],
              "dfa+gamepads": ["torso", "upperarm_L", "upperarm_R", "Chr_ShoulderAttachLeft_{sh}",
                               "Chr_ShoulderAttachRight_{sh}"]},
    "gloves": {"body": ["Chr_ArmLowerLeft_{s}_00", "Chr_ArmLowerRight_{s}_00", "Chr_HandLeft_{s}_00", "Chr_HandRight_{s}_00"],
               "plate": ["Chr_ArmLowerLeft_{s}_{la}", "Chr_ArmLowerRight_{s}_{la}", "Chr_HandLeft_{s}_14", "Chr_HandRight_{s}_14"],
               "dfa": ["lowerarm_L", "lowerarm_R", "hand_L", "hand_R"]},
    "legs": {"body": ["Chr_Hips_{s}_00"], "plate": ["Chr_Hips_{s}_25", "Chr_KneeAttachLeft_06", "Chr_KneeAttachRight_06"],
             "dfa": ["hips"]},
    "boots": {"body": ["Chr_LegLeft_{s}_00", "Chr_LegRight_{s}_00"],
              "plate": ["Chr_LegLeft_{s}_{bt}", "Chr_LegRight_{s}_{bt}"],
              "dfa": ["leg_L", "leg_R"]},
}
PLATE_IDX = {"m": {"ua": "13", "sh": "20", "la": "05", "bt": "16"}, "f": {"ua": "12", "sh": "14", "la": "15", "bt": "08"}}
coll = L.shot_collection()
clips = {}


def clip(key):
    if key not in clips:
        clips[key] = A.Clip(repo, key)
    return clips[key]


def figure(sex, chest="dfa", gloves="dfa", legs="dfa", boots="dfa", original=False, tone="medium"):
    r = L.new_rig(lib, 0, coll)
    if original:
        L.attach(lib.df[name], r, coll)
        return r
    mat = tones[tone]
    for n in HEAD[sex]:
        o = L.attach(lib.mfh[n], r, coll)
        o.data.materials[0] = mat
    look = {"chest": chest, "gloves": gloves, "legs": legs, "boots": boots}
    for slot, which in look.items():
        for part in SLOTS[slot][which]:
            if part.startswith("Chr_"):
                n = part.format(s=SEX[sex], **PLATE_IDX[sex])
                if n in lib.mfh:
                    o = L.attach(lib.mfh[n], r, coll)
                    o.data.materials[0] = mat
                else:
                    print("MISSING", n)
            else:
                src = (pieces if sex == "m" else fem).get(part)
                if src:
                    o = L.attach(src, r, coll)
                    if len(o.data.materials) > 1:
                        o.data.materials[1] = mat
    return r


def clear():
    for o in list(coll.objects):
        bpy.data.objects.remove(o, do_unlink=True)


def frame(key, i=1):
    c = clip(key)
    return c, c.frames[i]


def shoot(rigs, label, views, cell=(300, 640), spacing=1.25, height=2.1, cz=0.95):
    paths = L.shoot(rigs, os.path.join(out, "%s-%s.png" % (tag, label)), views=views, cell=cell, spacing=spacing,
                    height=height, cz=cz)
    print("SHOT", paths)


ANIMS = [("idle", 1), ("run", 1), ("attack", 1), ("chop", 2)]

if "anim" in sheets:
    rigs = []
    for key, i in ANIMS:
        c, f = frame(key, i)
        for kind in ("orig", "m", "f"):
            r = figure("m", original=True) if kind == "orig" else figure(kind)
            A.apply(r, c, f, tm)
            rigs.append(r)
    shoot(rigs, "anim", ("front", "back", "side", "game"), cell=(270, 640), spacing=1.3)
    clear()

if "mix" in sheets:
    looks = [dict(chest="dfa", gloves="body", legs="body", boots="body"),
             dict(chest="dfa", gloves="dfa", legs="plate", boots="plate"),
             dict(chest="plate", gloves="dfa", legs="dfa", boots="dfa"),
             dict(chest="body", gloves="plate", legs="dfa", boots="body"),
             dict(chest="body", gloves="body", legs="body", boots="dfa")]
    for key, i in (("idle", 1), ("attack", 1)):
        c, f = frame(key, i)
        rigs = []
        for sex in ("m", "f"):
            for lk in looks:
                r = figure(sex, **lk)
                A.apply(r, c, f, tm)
                rigs.append(r)
        shoot(rigs, "mix-" + key, ("front", "game", "back"), cell=(290, 640), spacing=1.3)
        clear()

if "skin" in sheets:
    c, f = frame("idle", 1)
    rigs = []
    for tone in ("light", "medium", "dark"):
        for sex in ("m", "f"):
            r = figure(sex, tone=tone)
            A.apply(r, c, f, tm)
            rigs.append(r)
    shoot(rigs, "skin", ("front", "game"), cell=(300, 640), spacing=1.25)
    clear()

if "pads" in sheets:
    rigs = []
    for key, i in (("idle", 1), ("attack", 1)):
        c, f = frame(key, i)
        for sex in ("m", "f"):
            for chest in ("dfa", "dfa+gamepads", "plate"):
                r = figure(sex, chest=chest)
                A.apply(r, c, f, tm)
                rigs.append(r)
    shoot(rigs, "pads", ("front", "back", "game"), cell=(260, 520), spacing=1.25, height=1.4, cz=1.25)
    clear()

if "seams" in sheets:
    looks = [("m", {}), ("f", {}),
             ("m", dict(chest="dfa", gloves="body", legs="plate", boots="plate")),
             ("f", dict(chest="plate", gloves="dfa", legs="dfa", boots="plate")),
             ("m", dict(chest="body", gloves="dfa", legs="dfa", boots="body"))]
    rows = [("neck", "idle", 1, "game", "Neck", 0.62, (0, 0, -0.02)),
            ("neck-back", "idle", 1, "back-high", "Neck", 0.62, (0, 0, -0.02)),
            ("shoulders", "attack", 1, "front", "Spine_03", 0.95, (0, 0, 0.1)),
            ("elbow", "chop", 2, "three-quarter", "Elbow_R", 0.6, (0, 0, 0)),
            ("waist", "run", 1, "side", "Spine_01", 0.75, (0, 0, -0.08)),
            ("knee", "run", 1, "three-quarter", "LowerLeg_R", 0.6, (0, 0, 0.04))]
    figs = [figure(sex, **lk) for sex, lk in looks]
    for label, key, i, view, bone, ortho, off in rows:
        c, f = frame(key, i)
        for r in figs:
            A.apply(r, c, f, tm)
        for j, r in enumerate(figs):
            r.location = (j * 4.0, j * 4.0, 0)
        bpy.context.view_layer.update()
        from mathutils import Vector
        for j, r in enumerate(figs):
            L.setup_render(420, 420)
            ctr = r.matrix_world @ r.pose.bones[bone].head + Vector(off)
            L.camera(view, ctr, ortho)
            L.render(os.path.join(out, "cells", "%s-seam-%s-%d.png" % (tag, label, j)))
    clear()
print("DONE")
