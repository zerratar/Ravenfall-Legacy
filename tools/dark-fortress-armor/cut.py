# Cutting a Dark Fortress character into the Modular Fantasy Hero part regions (headless Blender 5.x).
#
# The Dark Fortress characters are one skinned mesh on the same skeleton as the MFH parts, in a T-pose, so the
# regions can be cut with planes in world space where the game's items meet:
#   chest item   = torso + upper arms (+ shoulder pads, elbow pads)
#   gloves item  = lower arms + hands
#   leggings     = hips (pelvis and thighs down to the knee) (+ knee pads)
#   boots        = legs (knee down, feet included)
# The planes between items sit inside the overlap of the MFH parts, so a cut piece covers the edge of whatever MFH
# piece it is worn with: elbow |x| 0.52 (MFH upper arm ends 0.526, lower arm starts 0.51), waist z ~0.90 (MFH torso
# ends 0.83-0.86, hips start 0.94-0.95), knee z ~0.40 (hips end 0.37-0.38, legs start 0.40). Within an item (torso to
# upper arm, lower arm to hand) the split follows the bone weights or a plane at the wrist (|x| 0.795).
# Faces weighted to the head (Head, Eyes, Eyebrows, Jaw) are dropped: the player's own head is worn. Every opening a
# cut leaves gets a cap in a dark texel of the atlas, so nothing can be seen through from the elevated camera.
# Loose parts are sorted by their bones: shoulder pads (Clavicle + Shoulder), knee pads (LowerLeg near the knee),
# the rest goes with the region it sits in.
import bpy, bmesh
from mathutils import Vector, Matrix
import numpy as np

HEAD_BONES = ("Head", "Eyes", "Eyebrows", "Jaw")

DEFAULT_CUTS = {"elbow": 0.52, "wrist": 0.795, "waist": 0.90, "knee": 0.40, "arm_z": 1.12, "robe": 0}
FOOT_BONES = ("Ankle_L", "Ankle_R", "Ball_L", "Ball_R", "Toes_L", "Toes_R")


def dark_uv(image, max_lum=0.08):
    """A UV in the darkest flat area of the atlas (for the caps)."""
    w, h = image.size
    px = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)[:, :, :3]
    lum = px @ np.array([0.3, 0.59, 0.11], dtype=np.float32)
    # a texel whose 5x5 neighborhood is all dark, so filtering cannot bleed a color in
    k = 2
    best = None
    ys, xs = np.where(lum < max_lum)
    for y, x in zip(ys[::37], xs[::37]):
        if k <= y < h - k and k <= x < w - k and lum[y - k:y + k + 1, x - k:x + k + 1].max() < max_lum:
            best = (x, y)
            break
    if best is None:
        y, x = np.unravel_index(np.argmin(lum), lum.shape)
        best = (x, y)
    return ((best[0] + 0.5) / w, (best[1] + 0.5) / h)


def to_world_mesh(lib, name, coll):
    """A copy of a Dark Fortress character mesh baked into world meters (Z up, facing -Y), bound like the MFH meshes."""
    src = lib.df[name]
    o = src.copy()
    o.data = src.data.copy()
    o.name = name + "_W"
    coll.objects.link(o)
    o.data.transform(src.matrix_world)
    o.parent = None
    o.matrix_world = Matrix.Identity(4)
    o.parent = lib.rig
    o.matrix_parent_inverse = lib.rig.matrix_world.inverted()
    for m in o.modifiers:
        if m.type == "ARMATURE":
            m.object = lib.rig
    return o


def _weights(bm, o):
    dl = bm.verts.layers.deform.active
    names = {g.index: g.name for g in o.vertex_groups}
    return dl, names


def _face_bone_share(f, dl, names, bones):
    tot = 0.0
    for v in f.verts:
        for gi, w in v[dl].items():
            if names.get(gi) in bones:
                tot += w
    return tot / len(f.verts)


def _islands(bm):
    seen = set(); out = []
    for f in bm.faces:
        if f.index in seen:
            continue
        stack = [f]; seen.add(f.index); comp = []
        while stack:
            x = stack.pop(); comp.append(x)
            for e in x.edges:
                for y in e.link_faces:
                    if y.index not in seen:
                        seen.add(y.index); stack.append(y)
        out.append(comp)
    return out


def _bisect(bm, faces, co, no):
    geom = list({v for f in faces for v in f.verts}) + list({e for f in faces for e in f.edges}) + list(faces)
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_inner=False, clear_outer=False)
    bm.faces.ensure_lookup_table()


def head_tree(lib, name="Chr_Head_Male_00"):
    from mathutils.bvhtree import BVHTree
    hb = bmesh.new(); hb.from_mesh(lib.mfh[name].data); hb.transform(lib.mfh[name].matrix_world); hb.normal_update()
    t = BVHTree.FromBMesh(hb)
    hb.free()
    return t


def cut(lib, name, coll, cuts=None, cap_uv=None, prepare=None, pads="baked"):
    """Returns {region: object} for one Dark Fortress character. Regions: torso, upperarm_L/R, lowerarm_L/R, hand_L/R,
    hips, leg_L/R, and with pads="separate" also shoulder_L/R, knee_L/R (missing ones are left out).
    pads="baked" (the default since the second pass) keeps the pauldrons in the upper arm pieces and the knee caps in
    the hips piece: they are skinned to the clavicle and shoulder (knee: lower leg), which the game's rigid pad slots
    (one Shoulder_Attachment bone under the clavicle, shared by both sexes) cannot do, and baked they get the female
    fit with the rest. The items then carry NoShoulderPads / NoKneePads."""
    c = dict(DEFAULT_CUTS)
    c.update(cuts or {})
    o = to_world_mesh(lib, name, coll)
    if prepare:
        prepare(o)  # e.g. fit.add_female_key: shape keys ride through the cuts (bisect interpolates them)
    bm = bmesh.new()
    bm.from_mesh(o.data)
    dl, names = _weights(bm, o)
    # edges that were open in the character itself: never capped (only what a cut opens is)
    ob = bm.edges.layers.int.new("orig_open")
    for e in bm.edges:
        e[ob] = 1 if e.is_boundary else 0

    # 1. loose parts that are attachments of their own (pads), decided before any cutting
    region_of = {}
    for isl in _islands(bm):
        share = lambda bones: sum(_face_bone_share(f, dl, names, bones) for f in isl) / len(isl)
        cen = sum((f.calc_center_median() for f in isl), Vector()) / len(isl)
        side = "L" if cen.x > 0 else "R"
        if len(isl) > 0.5 * len(bm.faces):
            continue  # the body shell
        if share(("Clavicle_" + side, "Shoulder_" + side)) > 0.9 and cen.z > 1.3 and share(("Shoulder_" + side,)) < 0.8:
            r = ("upperarm_" if pads == "baked" else "shoulder_") + side
        elif share(("LowerLeg_" + side, "UpperLeg_" + side)) > 0.9 and 0.3 < cen.z < 0.55:
            r = "hips" if pads == "baked" else "knee_" + side
        elif share(HEAD_BONES) > 0.5:
            r = "drop"
        else:
            r = None  # placed by position below
        if r:
            for f in isl:
                region_of[f] = r

    # 2. the cuts between items, as planes through the faces they concern
    free = lambda: [f for f in bm.faces if f not in region_of]
    for s in (1, -1):
        arm = [f for f in free() if f.calc_center_median().z > c["arm_z"] and s * f.calc_center_median().x > 0.35]
        _bisect(bm, arm, (s * c["elbow"], 0, 0), (1, 0, 0))
        arm = [f for f in free() if f.calc_center_median().z > c["arm_z"] and s * f.calc_center_median().x > 0.6]
        _bisect(bm, arm, (s * c["wrist"], 0, 0), (1, 0, 0))
    body = [f for f in free() if 0.6 < f.calc_center_median().z < 1.1 and abs(f.calc_center_median().x) < 0.4]
    _bisect(bm, body, (0, 0, c["waist"]), (0, 0, 1))
    if not c["robe"]:
        legs = [f for f in free() if f.calc_center_median().z < 0.62 and abs(f.calc_center_median().x) < 0.4]
        _bisect(bm, legs, (0, 0, c["knee"]), (0, 0, 1))

    # 3. every face to a region. The character's own neck under the collar, where the player's head mesh hides
    # it on a male, goes too: on the thinner female neck it would poke through the skin
    htree = head_tree(lib)
    for f in bm.faces:
        if f in region_of:
            continue
        p = f.calc_center_median()
        if p.z > 1.35 and abs(p.x) < 0.15:
            loc, nor, _, dist = htree.find_nearest(p)
            if loc is not None and dist < 0.04 and (p - loc).dot(nor) < -0.001:
                region_of[f] = "drop"
    for f in bm.faces:
        if f in region_of:
            continue
        p = f.calc_center_median()
        side = "L" if p.x > 0 else "R"
        ax = abs(p.x)
        if _face_bone_share(f, dl, names, HEAD_BONES) > 0.5:
            r = "drop"
        elif p.z > c["arm_z"] and ax > c["wrist"]:
            r = "hand_" + side
        elif p.z > c["arm_z"] and ax > c["elbow"]:
            r = "lowerarm_" + side
        elif p.z > c["arm_z"] and _face_bone_share(f, dl, names, ("Shoulder_" + side, "Elbow_" + side)) > 0.5:
            r = "upperarm_" + side
        elif p.z > c["waist"]:
            r = "torso"
        elif c["robe"]:
            # a robe: the whole skirt is the leggings, the boots are only the shoes (the faces on the feet bones)
            r = "leg_" + side if _face_bone_share(f, dl, names, FOOT_BONES) > 0.5 else "hips"
        elif p.z > c["knee"]:
            r = "hips"
        else:
            r = "leg_" + side
        region_of[f] = r

    # 4. one object per region, openings capped
    out = {}
    uv_cap = cap_uv
    for r in sorted(set(region_of.values())):
        if r == "drop":
            continue
        b2 = bm.copy()
        b2.faces.ensure_lookup_table()
        keep = {f.index for f in bm.faces if region_of.get(f) == r}
        bmesh.ops.delete(b2, geom=[f for f in b2.faces if f.index not in keep], context="FACES")
        bmesh.ops.delete(b2, geom=[v for v in b2.verts if not v.link_faces], context="VERTS")
        if not r.startswith(("shoulder", "knee")):
            before = set(b2.faces)
            ob2 = b2.edges.layers.int.get("orig_open")
            bmesh.ops.holes_fill(b2, edges=[e for e in b2.edges if e.is_boundary and not e[ob2]], sides=0)
            caps = [f for f in b2.faces if f not in before]
            # the neck opening: a funnel instead of a flat lid, its tip sunk 10 cm into the chest, so the lid
            # can never cut through the player's own neck (a thin female neck showed dark specks of a flat one)
            neck = [f for f in caps if f.calc_center_median().z > 1.35 and abs(f.calc_center_median().x) < 0.1]
            if neck:
                res = bmesh.ops.poke(b2, faces=neck, center_mode="MEAN_WEIGHTED")
                shape_l = b2.verts.layers.shape.get("female")
                for v in res["verts"]:
                    ring = [e.other_vert(v) for e in v.link_edges]
                    v.co = sum((u.co for u in ring), Vector()) / len(ring) - Vector((0, 0, 0.10))
                    if shape_l:
                        v[shape_l] = sum((u[shape_l] for u in ring), Vector()) / len(ring) - Vector((0, 0, 0.10))
                    male_l = b2.verts.layers.shape.get("male")
                    if male_l:
                        v[male_l] = sum((u[male_l] for u in ring), Vector()) / len(ring) - Vector((0, 0, 0.10))
                caps = [f for f in b2.faces if f not in before]
            if caps:
                bmesh.ops.triangulate(b2, faces=caps, quad_method="BEAUTY", ngon_method="BEAUTY")
                caps = [f for f in b2.faces if f not in before]
                uvl = b2.loops.layers.uv.active
                for f in caps:
                    for l in f.loops:
                        l[uvl].uv = uv_cap
        for f in b2.faces:
            f.smooth = False
        shape = b2.verts.layers.shape.get("female")
        fem = [c for v in b2.verts for c in v[shape]] if shape else None
        mshape = b2.verts.layers.shape.get("male")
        male = [c for v in b2.verts for c in v[mshape]] if mshape else None
        me = bpy.data.meshes.new(name + "_" + r)
        b2.to_mesh(me)
        b2.free()
        me.materials.clear()
        for m in o.data.materials:
            me.materials.append(m)
        po = bpy.data.objects.new(me.name, me)
        if me.shape_keys:
            po.shape_key_clear()  # the piece itself is the male one
        if fem is not None:
            po["female"] = fem  # the female positions travel as a flat list on the object
        if male is not None:
            po["male"] = male  # the male bulk (male_bulk.py), the same way
        for g in o.vertex_groups:
            po.vertex_groups.new(name=g.name)
        coll.objects.link(po)
        po.parent = lib.rig
        po.matrix_parent_inverse = lib.rig.matrix_world.inverted()
        mod = po.modifiers.new("Armature", "ARMATURE")
        mod.object = lib.rig
        out[r] = po
    bm.free()
    bpy.data.objects.remove(o, do_unlink=True)
    return out


def add_bare_shins(lib, pieces, sex, skin_mat=None, overlap=0.02):
    """A robe's boots are only shoes (its shins are under the skirt), but the game's boots slot replaces the whole
    lower leg: worn with other leggings the shins would be missing. So the shoes get the game's own bare shin above
    them (the MFH base leg of that side and sex, faces above the shoe top), in material slot 1, which in the game is
    the player's body material: the shin is the player's own skin."""
    for side in ("L", "R"):
        p = pieces.get("leg_" + side)
        if not p:
            continue
        # the MFH base leg on this side (the male file names its legs the other way round)
        cands = ["Chr_LegLeft_%s_00" % sex, "Chr_LegRight_%s_00" % sex]
        want_plus = side == "L"
        src = next(lib.mfh[c] for c in cands
                   if (sum(v.co.x for v in lib.mfh[c].data.vertices) > 0) == want_plus)
        top = max((p.matrix_world @ v.co).z for v in p.data.vertices)
        bm = bmesh.new(); bm.from_mesh(p.data)
        dl = bm.verts.layers.deform.verify()
        uvl = bm.loops.layers.uv.active
        tgt_groups = {g.name: g.index for g in p.vertex_groups}
        sb = bmesh.new(); sb.from_mesh(src.data); sb.transform(src.matrix_world)
        sdl = sb.verts.layers.deform.active
        suv = sb.loops.layers.uv.active
        sgn = {g.index: g.name for g in src.vertex_groups}
        to_local = p.matrix_world.inverted()
        vmap = {}
        if len(p.data.materials) < 2:
            p.data.materials.append(skin_mat or bpy.data.materials.get("SKIN") or p.data.materials[0])
        # the shin down to the ankle (its lower faces run inside the shoe shaft, hidden), the foot itself left out
        floor = min(top - overlap, 0.12)
        for f in sb.faces:
            if f.calc_center_median().z < floor:
                continue
            vs = []
            for v in f.verts:
                if v not in vmap:
                    nv = bm.verts.new(to_local @ v.co)
                    for gi, w in v[sdl].items():
                        n = sgn[gi]
                        if n not in tgt_groups:
                            tgt_groups[n] = p.vertex_groups.new(name=n).index
                        nv[dl][tgt_groups[n]] = w
                    vmap[v] = nv
                vs.append(vmap[v])
            try:
                nf = bm.faces.new(vs)
            except ValueError:
                continue
            nf.material_index = 1
            nf.smooth = False
            for l, sl in zip(nf.loops, f.loops):
                l[uvl].uv = sl[suv].uv
        bm.to_mesh(p.data)
        bm.free(); sb.free()
