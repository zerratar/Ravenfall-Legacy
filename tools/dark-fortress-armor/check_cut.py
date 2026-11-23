# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/check_cut.py -- <repo> <out dir> [character]
# The character, its cut pieces put back together, and the pieces pulled apart (T-pose), front and back.
import bpy, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
name = argv[2] if len(argv) > 2 else "SM_Chr_Knight_01"
L.reset()
lib = L.Library(repo)
pieces = C.cut(lib, name, lib.src, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]))
print("PIECES", {k: len(v.data.vertices) for k, v in pieces.items()})
coll = L.shot_collection()
r0 = L.new_rig(lib, 0, coll); L.attach(lib.df[name], r0, coll)
r1 = L.new_rig(lib, 0, coll)
for p in pieces.values():
    L.attach(p, r1, coll)
r2 = L.new_rig(lib, 0, coll)
push = {"torso": (0, 0, 0.08), "hips": (0, 0, -0.04), "shoulder": (0.12, 0, 0.1), "upperarm": (0.1, 0, 0.05),
        "lowerarm": (0.22, 0, 0.05), "hand": (0.34, 0, 0.05), "leg": (0.06, 0, -0.14), "knee": (0.1, -0.12, -0.14)}
for k, p in pieces.items():
    o = L.attach(p, r2, coll)
    base = k.split("_")[0]
    d = Vector(push.get(base, (0, 0, 0)))
    if k.endswith("_R"):
        d.x = -d.x
    o["push"] = d
for r in (r0, r1):
    L.pose(r, "tpose")
rigs = [r0, r1, r2]
L.layout(rigs, "front", 2.6)
for o in coll.objects:
    if "push" in o.keys():
        o.location = Vector(o["push"])
L.shoot(rigs, os.path.join(out, "cut-%s.png" % name), views=("front", "back"), cell=(800, 560), spacing=2.6)
print("DONE")
