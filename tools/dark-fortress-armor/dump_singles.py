# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/dump_singles.py -- <repo> <out dir>
#
# The single Dark Fortress pieces (docs/dark-fortress-armor-test.md, "Single pieces"), written like dump_sets.py
# (DFSingles_Male.json, DFSingles_Female.json) for armor-sets.cs:
#   shoulder pads (one pair for both sexes, in the male file), skinned to the clavicle and shoulder like the pack has them:
#     DFGuard           SM_Chr_Attach_Armor_01, a pair of plate shoulder guards
#     DFFurMantle       SM_Chr_Attach_Fur_01, a fur mantle split at the middle into its two sides
#     DFUndeadPauldron  Undead 02's spiked pauldrons (loose parts of their own)
#     DFWraithCollar    the Wraith's mossy fur collar with skulls, cut out of its robe (the faces near the neck that
#                       stand more than 2 cm off the body), split at the middle
#   lower arms DFPlagueLord: the Plague Lord's spiked bracers (the cut of cut.py, the male bulk, the female fit)
#   legs DFUndead: Undead 02's spiked boots and greaves (loose parts, with the female fit and the male bulk)
# The Plague Lord's "pouch belt" is not taken: it is a ring of pustules on its belly over an iron corset, not pouches.
import bpy, bmesh, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F
import male_bulk as M
import skin as S
from mathutils import Vector
from mathutils.bvhtree import BVHTree
import dump_sets as D

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]
os.makedirs(out, exist_ok=True)
PAD_SCALE = 1.2      # the pack's bodies are slim: the pads a little bigger and further out, for both sexes
PAD_OUT = 0.015

L.reset()
lib = L.Library(repo)
male, female = [], []
shown = []  # (object, sex) for the optional check render


def islands(bm):
    seen = set(); res = []
    for f in bm.faces:
        if f.index in seen: continue
        st = [f]; seen.add(f.index); comp = []
        while st:
            x = st.pop(); comp.append(x)
            for e in x.edges:
                for y in e.link_faces:
                    if y.index not in seen: seen.add(y.index); st.append(y)
        res.append(comp)
    return res


def world(name, keys=False):
    o = C.to_world_mesh(lib, name, lib.src)
    if keys:
        F.add_female_key(lib, o)
        M.add_male_key(lib, o)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bm.faces.ensure_lookup_table()
    return o, bm


def pads(o, bm, faces, base):
    """faces -> a left and a right pad (split at x = 0 when they cross it), scaled about each side's center."""
    picked = {f.index for f in faces}
    lay = bm.faces.layers.int.get("pick") or bm.faces.layers.int.new("pick")  # a new layer invalidates face references
    bm.faces.ensure_lookup_table()
    faces = [bm.faces[i] for i in picked]
    for f in faces: f[lay] = 1
    if any(v.co.x > 0.01 for f in faces for v in f.verts) and any(v.co.x < -0.01 for f in faces for v in f.verts):
        C._bisect(bm, faces, (0, 0, 0), (1, 0, 0))  # the halves keep the face's "pick"
    region = {f: ("pad_L" if f.calc_center_median().x > 0 else "pad_R") for f in bm.faces if f[lay]}
    ps = C.build_pieces(lib, o, bm, region, base, lib.src, (0.01, 0.99), cap=False)
    for side, p in ps.items():
        mw = p.matrix_world; inv = mw.inverted()
        pts = [mw @ v.co for v in p.data.vertices]
        cen = sum(pts, Vector()) / len(pts)
        sgn = 1 if side == "pad_L" else -1
        for v, w in zip(p.data.vertices, pts):
            q = cen + (w - cen) * PAD_SCALE
            q.x += PAD_OUT * sgn
            v.co = inv @ q
        name = "Chr_ShoulderAttach%s_DF%s" % ("Left" if side == "pad_L" else "Right", base)
        male.append(D.dump_piece(p, name)); shown.append((p, "both"))


# shoulder guards and fur mantle: the whole attachment
for src, base in (("SM_Chr_Attach_Armor_01", "Guard"), ("SM_Chr_Attach_Fur_01", "FurMantle")):
    o, bm = world(src)
    pads(o, bm, list(bm.faces), base)

# Undead 02's pauldrons: its loose parts on the clavicle and shoulder
o, bm = world("SM_Chr_Undead_02")
dl = bm.verts.layers.deform.active
names = {g.index: g.name for g in o.vertex_groups}
sel = []
for isl in islands(bm):
    cen = sum((f.calc_center_median() for f in isl), Vector()) / len(isl)
    side = "L" if cen.x > 0 else "R"
    share = sum(C._face_bone_share(f, dl, names, ("Clavicle_" + side, "Shoulder_" + side)) for f in isl) / len(isl)
    if share > 0.9 and cen.z > 1.3:
        sel += isl
pads(o, bm, sel, "UndeadPauldron")

# the Wraith's collar: the light faces (moss, fur, bones) around the neck and shoulders of its dark robe; not its head
o, bm = world("SM_Chr_Wraith_01")
import numpy as np
img = bpy.data.images[os.path.basename(L.DF_TEX)]
iw, ih = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(ih, iw, 4)
uvl = bm.loops.layers.uv.active
dl = bm.verts.layers.deform.active
names = {g.index: g.name for g in o.vertex_groups}
sel = []
for f in bm.faces:
    c = f.calc_center_median()
    if not (1.25 < c.z < 1.6 and abs(c.x) < 0.36): continue
    if C._face_bone_share(f, dl, names, ("Head", "Jaw", "Eyes", "Eyebrows")) > 0.3: continue
    u = sum(l[uvl].uv[0] for l in f.loops) / len(f.loops); v = sum(l[uvl].uv[1] for l in f.loops) / len(f.loops)
    col = px[min(ih - 1, int(v * ih)), min(iw - 1, int(u * iw)), :3]
    if float(col @ np.array([0.299, 0.587, 0.114], dtype=np.float32)) > 0.27:
        sel.append(f)
# keep the biggest connected patch (the collar), not stray robe folds
selset = set(sel); comps = []; seen = set()
for f in sel:
    if f in seen: continue
    st = [f]; seen.add(f); comp = []
    while st:
        x = st.pop(); comp.append(x)
        for e in x.edges:
            for y in e.link_faces:
                if y in selset and y not in seen: seen.add(y); st.append(y)
    comps.append(comp)
comps.sort(key=len, reverse=True)
sel = [f for comp in comps if len(comp) > 0.1 * len(comps[0]) for f in comp]
pads(o, bm, sel, "WraithCollar")

# the Plague Lord's bracers: the full cut with both fits, only the lower arms kept
pieces = C.cut(lib, "SM_Chr_Plague_Lord_01", lib.src, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]),
               prepare=lambda o: (F.add_female_key(lib, o), M.add_male_key(lib, o)), pads="separate")
fem, _ = F.morph_female(lib, {k: v for k, v in pieces.items() if k.startswith("lowerarm")}, lib.src)
M.apply_male(pieces)
box, texel = S.skin_uv_box(repo), S.mfh_skin_texel(repo, lib)
body_slot = L.flat_material("PlayerBody", (0.8, 0.6, 0.5))
for side in ("L", "R"):
    for sex, src, lst in (("Male", pieces, male), ("Female", fem, female)):
        p = src["lowerarm_" + side]
        S.split_skin(p, box, body_slot, texel)
        lst.append(D.dump_piece(p, D.part_name(lib, "lowerarm_" + side, sex, "PlagueLord"))); shown.append((p, sex))

# Undead 02's boots: its loose parts on the lower legs and feet, with both fits
o, bm = world("SM_Chr_Undead_02", keys=True)
uvl = bm.loops.layers.uv.active
dl = bm.verts.layers.deform.active
names = {g.index: g.name for g in o.vertex_groups}
region = {}
for isl in islands(bm):
    cen = sum((f.calc_center_median() for f in isl), Vector()) / len(isl)
    top = max(v.co.z for f in isl for v in f.verts)
    if top < 0.5 and cen.z < 0.38 and abs(cen.x) > 0.03 and len(isl) < 0.3 * len(bm.faces):  # not the knee shards
        for f in isl:
            # not the glowing green bone of the shin showing at the boot's top: the skeleton, not the boot
            u = sum(l[uvl].uv[0] for l in f.loops) / len(f.loops); v = sum(l[uvl].uv[1] for l in f.loops) / len(f.loops)
            col = px[min(ih - 1, int(v * ih)), min(iw - 1, int(u * iw)), :3]
            if col[1] > col[0] + 0.15 and col[1] > 0.45: continue
            region[f] = "leg_" + ("L" if cen.x > 0 else "R")
ps = C.build_pieces(lib, o, bm, region, "SM_Chr_Undead_02", lib.src, (0.01, 0.99), cap=False)
fem, _ = F.morph_female(lib, ps, lib.src)
M.apply_male(ps)
# the boots end below the knee: the game's own bare shin above them, in the player's skin (as the robes' shoes)
C.add_bare_shins(lib, ps, "Male", body_slot)
C.add_bare_shins(lib, fem, "Female", body_slot)
for side in ("L", "R"):
    male.append(D.dump_piece(ps["leg_" + side], D.part_name(lib, "leg_" + side, "Male", "Undead"))); shown.append((ps["leg_" + side], "Male"))
    female.append(D.dump_piece(fem["leg_" + side], D.part_name(lib, "leg_" + side, "Female", "Undead"))); shown.append((fem["leg_" + side], "Female"))

ref = lib.mfh["Chr_Torso_Male_00"]
refpts = [round(c, 6) for v in ref.data.vertices for c in (ref.matrix_world @ v.co)]
for sex, lst in (("Male", male), ("Female", female)):
    path = os.path.join(out, "DFSingles_%s.json" % sex)
    json.dump({"set": "DFSingles", "sex": sex, "parts": lst, "reference": refpts}, open(path, "w"))
    print("WROTE", path, [(p["name"], len(p["verts"]) // 3) for p in lst])
if len(argv) > 2 and argv[2] == "render":
    # a check: every piece on the bare male and female body, in the game's idle
    import anim as A
    tm = A.target_map(repo); clip = A.Clip(repo, "idle")
    coll = L.shot_collection()
    BODY = {"Male": ["Chr_Head_Male_00", "Chr_Hair_01", "Chr_Torso_Male_00", "Chr_ArmUpperLeft_Male_00", "Chr_ArmUpperRight_Male_00",
                     "Chr_HandLeft_Male_00", "Chr_HandRight_Male_00", "Chr_Hips_Male_00", "Chr_ArmLowerLeft_Male_00",
                     "Chr_ArmLowerRight_Male_00", "Chr_LegLeft_Male_00", "Chr_LegRight_Male_00"],
            "Female": ["Chr_Head_Female_00", "Chr_Hair_02", "Chr_Torso_Female_00", "Chr_ArmUpperLeft_Female_00",
                       "Chr_ArmUpperRight_Female_00", "Chr_HandLeft_Female_00", "Chr_HandRight_Female_00", "Chr_Hips_Female_00",
                       "Chr_ArmLowerLeft_Female_00", "Chr_ArmLowerRight_Female_00", "Chr_LegLeft_Female_00", "Chr_LegRight_Female_00"]}
    rigs = []
    for sex in ("Male", "Female"):
        r = L.new_rig(lib, 0, coll)
        kinds = {"ArmLower", "Leg"}
        for n in BODY[sex]:
            if any(k in n for k in kinds): continue
            L.attach(lib.mfh[n], r, coll)
        for o, sx in shown:
            if sx == sex or (sx == "both" and "Guard" in o.name): L.attach(o, r, coll)
        rigs.append(r)
        for base in ("FurMantle", "UndeadPauldron", "WraithCollar"):
            r = L.new_rig(lib, 0, coll)
            for n in BODY[sex]: L.attach(lib.mfh[n], r, coll)
            for o, sx in shown:
                if sx == "both" and base in o.name: L.attach(o, r, coll)
            rigs.append(r)
    for r in rigs: A.apply(r, clip, clip.frames[1], tm)
    L.shoot(rigs, os.path.join(out, "singles-check.png"), views=("front", "three-quarter", "back"), cell=(300, 600), spacing=1.1)
print("DONE")
