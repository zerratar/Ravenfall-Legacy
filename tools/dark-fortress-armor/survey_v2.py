# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/survey_v2.py -- <repo> <out dir>
#
# Every Dark Fortress character against the game's body: a frame of the game's own idle and run (anim.py) beside the
# plain MFH male, front and side, and numbers per body region:
#   out_cm   how far the character's surface lies outside the MFH male body on average (bulk; negative = thinner)
#   in_pct   share of its vertices more than 1 cm inside the MFH body (thin, skeletal or rotten limbs)
#   max_cm   the 95th percentile distance outside (big silhouettes, robes, spikes)
#   skin_pct share of faces that use the atlas' skin swatches (bare skin)
# The Behemoth and Colossus are on a bigger rig (BR_Characters.fbx, arm span 2.88 m) and are only measured by size.
import bpy, bmesh, sys, os, json, statistics
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import anim as A
import skin as S
from mathutils import Vector
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
os.makedirs(out, exist_ok=True)
L.reset()
lib = L.Library(repo)
tm = A.target_map(repo)

NAMES = ["Knight", "Inquisitor", "Barbarian", "Apothecary", "Necromancer", "Plague_Lord", "Plague", "Undead_01",
         "Undead_02", "Wraith", "Corpse"]


def mesh_name(nm):
    return "SM_Chr_%s" % nm if nm[-3:-2] == "_" and nm[-2:].isdigit() else "SM_Chr_%s_01" % nm
BODY = ["Chr_Head_Male_00", "Chr_Torso_Male_00", "Chr_ArmUpperLeft_Male_00", "Chr_ArmUpperRight_Male_00",
        "Chr_ArmLowerLeft_Male_00", "Chr_ArmLowerRight_Male_00", "Chr_HandLeft_Male_00", "Chr_HandRight_Male_00",
        "Chr_Hips_Male_00", "Chr_LegLeft_Male_00", "Chr_LegRight_Male_00"]
REGION = {"Chr_Head_Male_00": "head", "Chr_Torso_Male_00": "torso", "Chr_ArmUpperLeft_Male_00": "arms",
          "Chr_ArmUpperRight_Male_00": "arms", "Chr_ArmLowerLeft_Male_00": "arms", "Chr_ArmLowerRight_Male_00": "arms",
          "Chr_HandLeft_Male_00": "hands", "Chr_HandRight_Male_00": "hands", "Chr_Hips_Male_00": "hips",
          "Chr_LegLeft_Male_00": "legs", "Chr_LegRight_Male_00": "legs"}

# one BVH per body part, to tell which region a vertex belongs to and how far out it is
trees = {}
for n in BODY:
    bm = bmesh.new(); bm.from_mesh(lib.mfh[n].data); bm.transform(lib.mfh[n].matrix_world); bm.normal_update()
    trees[n] = BVHTree.FromBMesh(bm)
    bm.free()

skin_box = S.skin_uv_box(repo)
stats = {}
for nm in NAMES:
    o = lib.df[mesh_name(nm)]
    mw = o.matrix_world
    per = {}
    for v in o.data.vertices:
        w = mw @ v.co
        best = None
        for n, t in trees.items():
            loc, nor, _, d = t.find_nearest(w)
            if loc is not None and (best is None or d < best[0]):
                best = (d, n, (w - loc).dot(nor))
        if best:
            per.setdefault(REGION[best[1]], []).append(best[2])
    uv = o.data.uv_layers.active.data
    head_groups = {g.index for g in o.vertex_groups if g.name in ("Head", "Eyes", "Eyebrows", "Jaw")}
    def is_head(p):
        tot = sum(g.weight for vi in p.vertices for g in o.data.vertices[vi].groups if g.group in head_groups)
        return tot / len(p.vertices) > 0.5
    body = [p for p in o.data.polygons if not is_head(p)]
    skin = sum(1 for p in body if S.in_box(S.face_uv(uv, p), skin_box))
    st = {"skin_pct": round(100 * skin / len(body), 1), "verts": len(o.data.vertices)}
    for r, ds in per.items():
        ds.sort()
        st[r] = {"out_cm": round(100 * statistics.mean(ds), 1),
                 "in_pct": round(100 * sum(1 for d in ds if d < -0.01) / len(ds)),
                 "max_cm": round(100 * ds[int(0.95 * (len(ds) - 1))], 1)}
    stats[nm] = st
    print("STAT", nm, json.dumps(st))
json.dump(stats, open(os.path.join(out, "survey-v2.json"), "w"), indent=1)

# pictures: idle and run frames, every character beside the plain body
clips = {"idle": A.Clip(repo, "idle"), "run": A.Clip(repo, "run")}
coll = L.shot_collection()
for clip_key, fi in (("idle", 1), ("run", 1)):
    pass
ATTACH = ["SM_Chr_Attach_Armor_01", "SM_Chr_Attach_Fur_01"]
for clip_key, fi in (("idle", 1), ("run", 1)):
    c = clips[clip_key]
    rigs = []
    r = L.new_rig(lib, 0, coll)
    for n in BODY + ["Chr_Hair_01", "Chr_Eyebrow_Male_01"]:
        L.attach(lib.mfh[n], r, coll)
    A.apply(r, c, c.frames[fi], tm)
    rigs.append(r)
    for nm in NAMES:
        r = L.new_rig(lib, 0, coll)
        L.attach(lib.df[mesh_name(nm)], r, coll)
        A.apply(r, c, c.frames[fi], tm)
        rigs.append(r)
    for att in ATTACH:  # the pack's loose attachments, on the plain game body
        r = L.new_rig(lib, 0, coll)
        for n in BODY + ["Chr_Hair_01", "Chr_Eyebrow_Male_01"]:
            L.attach(lib.mfh[n], r, coll)
        L.attach(lib.df[att], r, coll)
        A.apply(r, c, c.frames[fi], tm)
        rigs.append(r)
    L.shoot(rigs, os.path.join(out, "survey-%s.png" % clip_key), views=("front", "side", "back"), cell=(260, 600),
            spacing=1.15, height=2.25, cz=1.0)
    for o in list(coll.objects):
        bpy.data.objects.remove(o, do_unlink=True)
print("DONE")
