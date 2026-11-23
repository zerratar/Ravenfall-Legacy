# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/dump_sets.py -- <repo> <out dir>
#
# The armor sets for the game: every cut and fitted piece of the Knight, the Barbarian and the Necromancer, male and
# female, written as plain mesh data (JSON) for tools/dark-fortress-armor/armor-sets.cs, which builds the meshes in
# Unity and puts them on the player prefab. FBX is not used on purpose: the Dark Fortress rig's bones come out of
# Blender with other rest orientations than the game's MFH bones, and the meshes must use the MFH parts' bind poses.
#
# Per piece: flat shaded triangles (three corners each, Blender world meters, Z up, facing -Y), their normals and UVs,
# a triangle list per material slot (0 the set's own atlas, 1 bare skin in the player's body material, UVs on an MFH
# skin texel), and up to four bone weights per corner by bone name (the Dark Fortress names: Finger_01_L ...).
# The male pieces carry the male bulk pass (male_bulk.py), the female ones the female fit (fit.py).
# Also the MFH male torso's positions in the same space, so the Unity side can find how Blender's world maps onto
# the MFH meshes' own space.
import bpy, bmesh, sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import dfa_lib as L
import cut as C
import fit as F
import skin as S
import male_bulk as M

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    repo, out = argv[0], argv[1]
    os.makedirs(out, exist_ok=True)

SETS = {"Knight": "SM_Chr_Knight_01", "Barbarian": "SM_Chr_Barbarian_01", "Necromancer": "SM_Chr_Necromancer_01"}
KIND = {"torso": "Torso", "upperarm": "ArmUpper", "lowerarm": "ArmLower", "hand": "Hand", "hips": "Hips", "leg": "Leg"}


def side_name(lib, kind, sex, side):
    l = lib.mfh["Chr_%sLeft_%s_00" % (kind, sex)]
    left_is_plus_x = sum(v.co.x for v in l.data.vertices) > 0
    return "Left" if (side == "L") == left_is_plus_x else "Right"


def part_name(lib, region, sex, short):
    base, _, side = region.partition("_")
    kind = KIND[base]
    if side:
        return "Chr_%s%s_%s_DF%s" % (kind, side_name(lib, kind, sex, side), sex, short)
    return "Chr_%s_%s_DF%s" % (kind, sex, short)


def dump_piece(o, name):
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.transform(o.matrix_world)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.normal_update()
    dl = bm.verts.layers.deform.active
    uvl = bm.loops.layers.uv.active
    gname = {g.index: g.name for g in o.vertex_groups}
    verts, normals, uvs, weights = [], [], [], []
    subs = [[], []]
    bones = []
    bidx = {}
    for f in bm.faces:
        slot = 1 if f.material_index == 1 else 0
        for l in f.loops:
            v = l.vert
            subs[slot].append(len(verts) // 3)
            verts += [round(c, 6) for c in v.co]
            normals += [round(c, 5) for c in f.normal]
            uvs += [round(c, 6) for c in l[uvl].uv]
            ws = sorted(((w, gname[g]) for g, w in v[dl].items() if w > 0), reverse=True)[:4]
            tot = sum(w for w, _ in ws) or 1.0
            row = []
            for w, n in ws:
                if n not in bidx:
                    bidx[n] = len(bones); bones.append(n)
                row += [bidx[n], round(w / tot, 5)]
            weights.append(row)
    bm.free()
    return {"name": name, "verts": verts, "normals": normals, "uvs": uvs, "sub": subs, "bones": bones,
            "weights": weights}


if __name__ == "__main__":
    for short, char in SETS.items():
        L.reset()
        lib = L.Library(repo)
        pieces = C.cut(lib, char, lib.src, cap_uv=C.dark_uv(bpy.data.images[os.path.basename(L.DF_TEX)]),
                       prepare=lambda o: (F.add_female_key(lib, o), M.add_male_key(lib, o)))
        fem, _ = F.morph_female(lib, pieces, lib.src)
        # the male pieces get the male bulk (male_bulk.py); the female ones keep the female fit
        M.apply_male(pieces)
        body_slot = L.flat_material("PlayerBody", (0.8, 0.6, 0.5))
        box, texel = S.skin_uv_box(repo), S.mfh_skin_texel(repo, lib)
        for sex, set_ in (("Male", pieces), ("Female", fem)):
            parts = []
            for region, p in sorted(set_.items()):
                S.split_skin(p, box, body_slot, texel)
                parts.append(dump_piece(p, part_name(lib, region, sex, short)))
            ref = lib.mfh["Chr_Torso_Male_00"]
            refpts = [round(c, 6) for v in ref.data.vertices for c in (ref.matrix_world @ v.co)]
            path = os.path.join(out, "DF%s_%s.json" % (short, sex))
            json.dump({"set": "DF" + short, "sex": sex, "parts": parts, "reference": refpts}, open(path, "w"))
            print("WROTE", path, [(p["name"], len(p["verts"]) // 3, len(p["sub"][1]) // 3) for p in parts])
    print("DONE")
