# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/survey.py -- <repo> <out dir>
# The five test characters next to the MFH male and female base bodies, front and side, A-pose; plus a per-mesh
# report of loose parts and which bones weight them.
import bpy, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
os.makedirs(out, exist_ok=True)
L.reset()
lib = L.Library(repo)
coll = L.shot_collection()

BODY = {
    "male": ["Chr_Head_Male_00", "Chr_Eyebrow_Male_01", "Chr_Hair_01", "Chr_Torso_Male_00", "Chr_ArmUpperLeft_Male_00",
             "Chr_ArmUpperRight_Male_00", "Chr_ArmLowerLeft_Male_00", "Chr_ArmLowerRight_Male_00",
             "Chr_HandLeft_Male_00", "Chr_HandRight_Male_00", "Chr_Hips_Male_00", "Chr_LegLeft_Male_00",
             "Chr_LegRight_Male_00"],
    "female": ["Chr_Head_Female_00", "Chr_Eyebrow_Female_01", "Chr_Hair_02", "Chr_Torso_Female_00",
               "Chr_ArmUpperLeft_Female_00", "Chr_ArmUpperRight_Female_00", "Chr_ArmLowerLeft_Female_00",
               "Chr_ArmLowerRight_Female_00", "Chr_HandLeft_Female_00", "Chr_HandRight_Female_00",
               "Chr_Hips_Female_00", "Chr_LegLeft_Female_00", "Chr_LegRight_Female_00"],
}
subjects = ["SM_Chr_Knight_01", "SM_Chr_Inquisitor_01", "SM_Chr_Barbarian_01", "SM_Chr_Plague_Lord_01",
            "SM_Chr_Apothecary_01", "male", "female"]
rigs = []
for i, s in enumerate(subjects):
    r = L.new_rig(lib, 0, coll)
    rigs.append(r)
    if s in BODY:
        for n in BODY[s]:
            L.attach(lib.mfh[n], r, coll)
    else:
        L.attach(lib.df[s], r, coll)
    L.pose(r, "apose")

L.shoot(rigs, os.path.join(out, "survey.png"), views=("front", "side", "back"))

# loose parts and weights of the knight
import bmesh
rep = {}
for name in subjects[:5]:
    o = lib.df[name]
    bm = bmesh.new(); bm.from_mesh(o.data)
    bm.verts.ensure_lookup_table()
    seen = set(); parts = []
    for v in bm.verts:
        if v.index in seen: continue
        stack = [v]; comp = []
        seen.add(v.index)
        while stack:
            x = stack.pop(); comp.append(x.index)
            for e in x.link_edges:
                y = e.other_vert(x)
                if y.index not in seen:
                    seen.add(y.index); stack.append(y)
        parts.append(comp)
    gname = {g.index: g.name for g in o.vertex_groups}
    info = []
    for comp in sorted(parts, key=len, reverse=True):
        w = {}
        zs = []
        for i in comp:
            vv = o.data.vertices[i]
            zs.append(vv.co.y)  # data is Y up in centimeters
            for g in vv.groups:
                w[gname[g.group]] = w.get(gname[g.group], 0) + g.weight
        top = sorted(w.items(), key=lambda kv: -kv[1])[:4]
        info.append({"verts": len(comp), "y_cm": [round(min(zs), 1), round(max(zs), 1)],
                     "bones": [(k, round(val / len(comp), 2)) for k, val in top]})
    rep[name] = info
    bm.free()
json.dump(rep, open(os.path.join(out, "loose-parts.json"), "w"), indent=1)
print("DONE")
