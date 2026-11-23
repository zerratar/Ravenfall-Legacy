# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/bulk_sheet.py -- <repo> <out dir>
# The male pieces before and after the male bulk pass (male_bulk.py), beside the game's male plate and the Tank
# class gear, in the game's idle and combat stance (anim.py): front, side and the game camera; one PNG per row,
# compose_bulk.py makes the sheet.
import bpy, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import anim as A
import male_bulk as M

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
os.makedirs(out, exist_ok=True)
L.reset()
lib = L.Library(repo)
tm = A.target_map(repo)
coll = L.shot_collection()
HEAD = ["Chr_Head_Male_00", "Chr_Hair_01", "Chr_Eyebrow_Male_01"]
REG = ("torso", "upperarm_L", "upperarm_R", "lowerarm_L", "lowerarm_R", "hand_L", "hand_R", "hips", "leg_L", "leg_R")

sets = {}
for ch in ("Knight", "Barbarian", "Necromancer"):
    before = C.cut(lib, "SM_Chr_%s_01" % ch, lib.src, cap_uv=(0.01, 0.99))
    after = C.cut(lib, "SM_Chr_%s_01" % ch, lib.src, cap_uv=(0.01, 0.99), prepare=lambda o: M.add_male_key(lib, o))
    M.apply_male(after)
    sets[ch] = (before, after)


def figure(pieces=None, parts=()):
    r = L.new_rig(lib, 0, coll)
    for n in HEAD: L.attach(lib.mfh[n], r, coll)
    if pieces:
        for k in REG:
            if k in pieces: L.attach(pieces[k], r, coll)
    for n in parts: L.attach(lib.mfh[n], r, coll)
    return r


def dims(objs, z, band=0.03):
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    sl = [p for p in pts if abs(p.z - z) < band]
    if not sl: return "-"
    return "w%.1f d%.1f" % ((max(p.x for p in sl) - min(p.x for p in sl)) * 100, (max(p.y for p in sl) - min(p.y for p in sl)) * 100)


for name, objs in [("plate", [lib.mfh["Chr_Torso_Male_15"]]), ("tank", [lib.mfh["Chr_Torso_Male_20"]]),
                   ("bare", [lib.mfh["Chr_Torso_Male_00"]])] +         [(ch + " " + w, [sets[ch][i]["torso"]]) for ch in sets for i, w in ((0, "before"), (1, "after"))]:
    print("TORSO %-20s chest %s  under %s  waist %s" % (name, dims(objs, 1.32), dims(objs, 1.20), dims(objs, 1.02)))
for name, objs in [("plate", [lib.mfh["Chr_ArmUpperLeft_Male_13"]]), ("bare", [lib.mfh["Chr_ArmUpperLeft_Male_00"]])] +         [(ch + " " + w, [sets[ch][i]["upperarm_L"]]) for ch in sets for i, w in ((0, "before"), (1, "after"))]:
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    sl = [p for p in pts if abs(abs(p.x) - 0.36) < 0.03]
    if not sl: continue
    print("UPPERARM %-17s y%.1f z%.1f" % (name, (max(p.y for p in sl) - min(p.y for p in sl)) * 100, (max(p.z for p in sl) - min(p.z for p in sl)) * 100))

figs = []
for ch in ("Knight", "Barbarian", "Necromancer"):
    figs.append(figure(sets[ch][0])); figs.append(figure(sets[ch][1]))
figs.append(figure(parts=["Chr_Torso_Male_15", "Chr_ArmUpperLeft_Male_13", "Chr_ArmUpperRight_Male_13",
                          "Chr_ArmLowerLeft_Male_05", "Chr_ArmLowerRight_Male_05", "Chr_HandLeft_Male_14",
                          "Chr_HandRight_Male_14", "Chr_Hips_Male_25", "Chr_LegLeft_Male_16", "Chr_LegRight_Male_16",
                          "Chr_ShoulderAttachLeft_20", "Chr_ShoulderAttachRight_20", "Chr_KneeAttachLeft_06",
                          "Chr_KneeAttachRight_06"]))
figs.append(figure(parts=["Chr_Torso_Male_20", "Chr_ArmUpperLeft_Male_14", "Chr_ArmUpperRight_Male_14",
                          "Chr_ArmLowerLeft_Male_04", "Chr_ArmLowerRight_Male_04", "Chr_HandLeft_Male_12",
                          "Chr_HandRight_Male_12", "Chr_Hips_Male_20", "Chr_LegLeft_Male_13", "Chr_LegRight_Male_13",
                          "Chr_ShoulderAttachLeft_10", "Chr_ShoulderAttachRight_10", "Chr_ElbowAttachLeft_03",
                          "Chr_ElbowAttachRight_03", "Chr_KneeAttachLeft_06", "Chr_KneeAttachRight_06"]))
for key in ("idle", "combat"):
    c = A.Clip(repo, key)
    for r in figs:
        A.apply(r, c, c.frames[1], tm)
    L.shoot(figs, os.path.join(out, "bulk-%s.png" % key), views=("front", "side", "game"), cell=(250, 600),
            spacing=1.15, height=2.2, cz=1.0)
print("DONE")
