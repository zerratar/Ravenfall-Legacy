# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/chest_closeups.py -- <repo> <out png prefix> [bulk]
# Close-ups of the chest (front, side, three-quarter) of the pack's Knight, Barbarian and Necromancer, their male
# pieces (with "bulk": after the male bulk field, male_bulk.py), and the game's male plate and Tank cuirass for scale.
import bpy, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F
import anim as A

argv = sys.argv[sys.argv.index("--") + 1:]
repo, prefix = argv[0], argv[1]
bulk = len(argv) > 2 and argv[2] == "bulk"
L.reset()
lib = L.Library(repo)
tm = A.target_map(repo)
clip = A.Clip(repo, "idle")
coll = L.shot_collection()
HEAD = ["Chr_Head_Male_00", "Chr_Hair_01"]
figs = []
labels = []


def body(parts):
    r = L.new_rig(lib, 0, coll)
    for n in parts: L.attach(lib.mfh[n], r, coll)
    return r


for ch in ("Knight", "Barbarian", "Necromancer"):
    prep = None
    if bulk:
        import male_bulk as M
        prep = lambda o: M.add_male_key(lib, o)
    pieces = C.cut(lib, "SM_Chr_%s_01" % ch, lib.src, cap_uv=(0.01, 0.99), prepare=prep)
    if bulk:
        M.apply_male(pieces)
    r = L.new_rig(lib, 0, coll)
    for n in HEAD: L.attach(lib.mfh[n], r, coll)
    for k in ("torso", "upperarm_L", "upperarm_R", "lowerarm_L", "lowerarm_R", "hand_L", "hand_R", "hips"):
        if k in pieces: L.attach(pieces[k], r, coll)
    figs.append(r); labels.append(ch)
figs.append(body(HEAD + ["Chr_Torso_Male_15", "Chr_ArmUpperLeft_Male_13", "Chr_ArmUpperRight_Male_13",
                         "Chr_ArmLowerLeft_Male_05", "Chr_ArmLowerRight_Male_05", "Chr_HandLeft_Male_14",
                         "Chr_HandRight_Male_14", "Chr_Hips_Male_25", "Chr_ShoulderAttachLeft_20", "Chr_ShoulderAttachRight_20"]))
figs.append(body(HEAD + ["Chr_Torso_Male_20", "Chr_ArmUpperLeft_Male_14", "Chr_ArmUpperRight_Male_14",
                         "Chr_ArmLowerLeft_Male_04", "Chr_ArmLowerRight_Male_04", "Chr_HandLeft_Male_12",
                         "Chr_HandRight_Male_12", "Chr_Hips_Male_20"]))
for r in figs:
    A.apply(r, clip, clip.frames[1], tm)
for view in ("front", "side", "three-quarter"):
    L.layout(figs, view, 0.9)
    L.setup_render(300 * len(figs), 420)
    L.camera(view, (0, 0, 1.28), 0.9 * len(figs))
    L.render(prefix + "-" + view + ".png")
print("DONE")
