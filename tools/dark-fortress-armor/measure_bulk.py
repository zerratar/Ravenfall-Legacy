# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/measure_bulk.py -- <repo> <out dir>
#
# The silhouette of the male pieces against the game's own male armor (T-pose, world meters): per height slice the
# width (x) and depth (y) of the torso with its upper arms near the body, and the front profile of the chest (to see
# whether a plate is shaped as a bust: two forward mounds with a valley between). Writes bulk.json and prints a table.
import bpy, bmesh, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
os.makedirs(out, exist_ok=True)
L.reset()
lib = L.Library(repo)

SLICES = {"shoulder": 1.44, "chest": 1.32, "underchest": 1.20, "waist": 1.06, "hips": 0.94}


def world_verts(objs):
    pts = []
    for o in objs:
        pts += [o.matrix_world @ v.co for v in o.data.vertices]
    return pts


def slice_stats(pts, z, band=0.03, xmax=0.30):
    s = [p for p in pts if abs(p.z - z) < band and abs(p.x) < xmax]
    if not s:
        return None
    w = max(p.x for p in s) - min(p.x for p in s)
    front = min(p.y for p in s)   # the characters face -Y
    back = max(p.y for p in s)
    # front profile: the most forward point in x bins, for a bust check
    prof = []
    for i in range(-6, 7):
        x0 = i * 0.025
        b = [p.y for p in s if abs(p.x - x0) < 0.0125 and p.y < 0]
        prof.append(round(min(b) * 100, 1) if b else None)
    return {"width": round(w * 100, 1), "depth": round((back - front) * 100, 1), "front": round(front * 100, 1),
            "profile": prof}


sets = {
    "MFH body": ["Chr_Torso_Male_00", "Chr_ArmUpperLeft_Male_00", "Chr_ArmUpperRight_Male_00", "Chr_Hips_Male_00"],
    "MFH plate": ["Chr_Torso_Male_15", "Chr_ArmUpperLeft_Male_13", "Chr_ArmUpperRight_Male_13", "Chr_Hips_Male_25",
                  "Chr_ShoulderAttachLeft_20", "Chr_ShoulderAttachRight_20"],
    "Tank cuirass": ["Chr_Torso_Male_20", "Chr_ArmUpperLeft_Male_14", "Chr_ArmUpperRight_Male_14", "Chr_Hips_Male_20"],
    "Melee hauberk": ["Chr_Torso_Male_17", "Chr_ArmUpperLeft_Male_19", "Chr_ArmUpperRight_Male_19", "Chr_Hips_Male_10"],
}
res = {}
for name, parts in sets.items():
    pts = world_verts([lib.mfh[p] for p in parts if p in lib.mfh])
    res[name] = {k: slice_stats(pts, z) for k, z in SLICES.items()}
for ch in ("Knight", "Barbarian", "Necromancer"):
    o = lib.df["SM_Chr_%s_01" % ch]
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    res[ch] = {k: slice_stats(pts, z) for k, z in SLICES.items()}
json.dump(res, open(os.path.join(out, "bulk.json"), "w"), indent=1)
for name, r in res.items():
    row = "  ".join("%s w%s d%s" % (k[:5], v["width"], v["depth"]) if v else k + " -" for k, v in r.items())
    print("BULK %-14s %s" % (name, row))
for name, r in res.items():
    print("PROF %-14s chest %s" % (name, r["chest"]["profile"] if r["chest"] else None))
    print("PROF %-14s under %s" % (name, r["underchest"]["profile"] if r["underchest"] else None))
