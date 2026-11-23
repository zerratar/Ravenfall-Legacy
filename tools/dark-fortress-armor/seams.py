# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/seams.py -- <repo> <out.json>
# Where the MFH parts end: the open boundary loops of each base body part (and a few armored ones), with their
# center, size and the bones that weight them. These are the seams a cut Dark Fortress piece has to cover.
import bpy, bmesh, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
L.reset()
lib = L.Library(repo)
names = [n for n in lib.mfh if n.endswith("_00") and not n.startswith("Chr_Head_No")]
names += ["Chr_Torso_Male_15", "Chr_Hips_Male_25", "Chr_LegLeft_Male_16", "Chr_ArmLowerLeft_Male_05",
          "Chr_ArmUpperLeft_Male_13", "Chr_HandLeft_Male_14", "Chr_Torso_Female_15", "Chr_Hips_Female_25"]
res = {}
for n in names:
    o = lib.mfh.get(n)
    if not o:
        continue
    bm = bmesh.new(); bm.from_mesh(o.data)
    edges = [e for e in bm.edges if e.is_boundary]
    # boundary loops
    left = set(edges); loops = []
    adj = {}
    for e in edges:
        for v in e.verts:
            adj.setdefault(v.index, []).append(e)
    while left:
        e = left.pop(); comp = {e}; stack = [e]
        while stack:
            x = stack.pop()
            for v in x.verts:
                for y in adj[v.index]:
                    if y in left:
                        left.discard(y); comp.add(y); stack.append(y)
        vs = {v.index for x in comp for v in x.verts}
        pts = [o.data.vertices[i].co for i in vs]
        c = sum(pts, Vector()) / len(pts)
        gname = {g.index: g.name for g in o.vertex_groups}
        w = {}
        for i in vs:
            for g in o.data.vertices[i].groups:
                w[gname[g.group]] = w.get(gname[g.group], 0) + g.weight / len(vs)
        loops.append({"n": len(vs), "center": [round(x, 3) for x in c],
                      "min": [round(min(p[k] for p in pts), 3) for k in range(3)],
                      "max": [round(max(p[k] for p in pts), 3) for k in range(3)],
                      "weights": {k: round(v, 2) for k, v in sorted(w.items(), key=lambda kv: -kv[1])[:4]}})
    res[n] = {"verts": len(o.data.vertices), "loops": loops,
              "zrange": [round(min(v.co.z for v in o.data.vertices), 3), round(max(v.co.z for v in o.data.vertices), 3)]}
    bm.free()
json.dump(res, open(out, "w"), indent=1)
print("DONE")
