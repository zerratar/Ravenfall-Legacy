# Headless: blender.exe -b --factory-startup --python tools/dark-fortress-armor/inspect_rigs.py -- <repo> <out.json>
#
# Imports the POLYGON Dark Fortress characters and the Modular Fantasy Hero parts into an empty scene and writes what
# a fit needs to know into a JSON file: the meshes of each file (vertex count, vertex groups, bounds), the armatures
# and their bones (head/tail in the rest pose, in world space, so proportions can be compared).
import bpy, sys, json, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
repo, out = argv[0], argv[1]

DF = os.path.join(repo, "Assets/Synty/PolygonDarkFortress/Models/Characters.fbx")
MFH = os.path.join(repo, "Assets/PolygonFantasyHeroCharacters/Models/ModularCharacters.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path, tag):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False, ignore_leaf_bones=False, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        o["src"] = tag
    return new


def bounds(o):
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = [min(p[i] for p in pts) for i in range(3)]
    mx = [max(p[i] for p in pts) for i in range(3)]
    return [round(v, 4) for v in mn + mx]


result = {}
for path, tag in ((DF, "df"), (MFH, "mfh")):
    objs = import_fbx(path, tag)
    info = {"meshes": {}, "armatures": {}}
    for o in objs:
        if o.type == "MESH":
            arm = next((m.object.name for m in o.modifiers if m.type == "ARMATURE" and m.object), None)
            info["meshes"][o.name] = {
                "verts": len(o.data.vertices), "faces": len(o.data.polygons),
                "groups": [g.name for g in o.vertex_groups], "armature": arm,
                "parent": o.parent.name if o.parent else None,
                "bounds": bounds(o), "materials": [m.name for m in o.data.materials if m],
                "uv": [u.name for u in o.data.uv_layers],
            }
        elif o.type == "ARMATURE":
            bones = {}
            for b in o.data.bones:
                bones[b.name] = {
                    "parent": b.parent.name if b.parent else None,
                    "head": [round(v, 4) for v in (o.matrix_world @ b.head_local)],
                    "tail": [round(v, 4) for v in (o.matrix_world @ b.tail_local)],
                }
            info["armatures"][o.name] = {"bones": bones, "matrix": [list(r) for r in o.matrix_world],
                                         "parent": o.parent.name if o.parent else None}
    result[tag] = info
    # keep the two sets apart: move this file's objects out of the way of the next import's names
    for o in objs:
        o.name = tag + "__" + o.name

with open(out, "w") as f:
    json.dump(result, f, indent=1)
print("WROTE", out)
