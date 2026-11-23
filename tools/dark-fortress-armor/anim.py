# The game's own animations on the Blender rig (headless Blender 5.x).
#
# The player animator (Assets/Animations/PlayerAnimationController.controller) plays Mecanim humanoid clips from the
# RPG Character and Crafting Mecanim packs, retargeted by Unity onto the Player prefab's avatar (ModularCharacters.fbx).
# Those FBX files hold no armature, only animated empties (B_Pelvis, B_L_UpperArm, ...), and no rest pose; the rest
# pose Unity built the clip's avatar from is in the clip's .meta (humanDescription.skeleton), and the bone mapping
# (humanName -> boneName) is in the .meta of the clip and of ModularCharacters.fbx.
#
# Retargeting here works like Mecanim's in its simplest form: for each humanoid bone, the world rotation it has in a
# frame, relative to the rest pose of the clip's avatar, is put on the target bone relative to the target's rest
# (T-pose): R_target(f) = R_src(f) * R_src_rest^-1 * R_target_rest. The hips move by the source's pelvis offset,
# scaled by hip height. Mecanim also spreads hand roll over the forearm (foreArmTwist 0.5 on the player's avatar);
# that is copied here as half the hand's roll about the forearm axis moved onto the forearm.
import bpy, os, re, math
from mathutils import Vector, Quaternion, Matrix

REPO_ANIMS = {
    "idle": "Assets/RPG Character Animation Pack/Animations/Relax/RPG-Character@Idle.FBX",
    "combat": "Assets/RPG Character Animation Pack/Animations/Armed/RPG-Character@Armed-Idle.FBX",
    "run": "Assets/RPG Character Animation Pack/Animations/Unarmed/RPG-Character@Unarmed-Run-Forward.FBX",
    "attack": "Assets/RPG Character Animation Pack/Animations/Sword/RPG-Character@Sword-Attack-R1.FBX",
    "attack2h": "Assets/RPG Character Animation Pack/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Attack1.FBX",
    "chop": "Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Chop-Finish.FBX",
    "fish": "Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Fishing-Cast.FBX",
    "cheer": "Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Cheer2.FBX",
}
TARGET_META = "Assets/PolygonFantasyHeroCharacters/Models/ModularCharacters.fbx.meta"
SKIP = ("Index", "Middle", "Ring", "Little", "Thumb", "Eye", "Jaw")


def human_map(meta_path):
    t = open(meta_path, encoding="utf-8").read()
    return {h.strip(): b.strip() for b, h in re.findall(r"- boneName: (.*)\n\s*humanName: (.*)", t)
            if not any(s in h for s in SKIP)}


def rest_skeleton(meta_path):
    t = open(meta_path, encoding="utf-8").read()
    sk = t[t.index("    skeleton:"):]
    out = {}
    for m in re.finditer(r"- name: (.*)\n\s*parentName: .*\n\s*position: \{x: (\S+), y: (\S+), z: (\S+)\}\n"
                         r"\s*rotation: \{x: (\S+), y: (\S+), z: (\S+), w: (\S+)\}", sk):
        n = m.group(1).strip()
        px, py, pz, qx, qy, qz, qw = (float(m.group(i).rstrip(",")) for i in range(2, 9))
        out[n] = ((px, py, pz), (qx, qy, qz, qw))
    return out


def _unity_to_fbx_rot(q):
    x, y, z, w = q
    return Quaternion((w, x, -y, -z))


def _wrot(o):
    return o.matrix_world.to_quaternion().normalized()


class Clip:
    def _enforce_tpose(self):
        _enforce(self)

    """One clip sampled at some frames: per humanoid bone the world rotation relative to the clip's rest, and the
    pelvis offset."""

    def __init__(self, repo, key, frames=None, fractions=(0.0, 0.25, 0.5, 0.75)):
        path = os.path.join(repo, REPO_ANIMS[key]) if key in REPO_ANIMS else key
        meta = path + ".meta"
        self.key = key
        hm = human_map(meta)
        rest = rest_skeleton(meta)
        before = set(bpy.data.objects)
        acts_before = set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=path, use_anim=True)
        new = [o for o in bpy.data.objects if o not in before]
        by = {o.name.split(".")[0]: o for o in new}
        acts = [a for a in bpy.data.actions if a not in acts_before]
        lo = min(a.frame_range[0] for a in acts)
        hi = max(a.frame_range[1] for a in acts)
        self.range = (lo, hi)
        if frames is None:
            frames = [round(lo + f * (hi - lo)) for f in fractions]
        self.frames = frames
        sc = bpy.context.scene
        self.samples = {}
        pelvis = by[hm["Hips"]]
        raw = {}
        for f in frames:
            sc.frame_set(int(f))
            bpy.context.view_layer.update()
            s = {h: _wrot(by[b]) for h, b in hm.items() if b in by}
            s["_hips_pos"] = pelvis.matrix_world.translation.copy()
            s["_pos"] = {h: by[b].matrix_world.translation.copy() for h, b in hm.items() if b in by}
            raw[f] = s
        # The clip's world is Z up but may face any way: turn it about Z so the character's left (left thigh minus
        # right thigh, first frame) is +X, as on the target rig.
        p0 = raw[frames[0]]["_pos"]
        left = p0["LeftUpperLeg"] - p0["RightUpperLeg"]
        left.z = 0
        ang = math.atan2(left.y, left.x)
        Qa = Quaternion((0, 0, 1), -ang)
        for f, s in raw.items():
            out = {h: Qa @ q for h, q in s.items() if not h.startswith("_")}
            out["_hips_pos"] = Qa @ s["_hips_pos"]
            self.samples[f] = out
        # The rest pose of the clip's avatar, from the .meta: the bones' local rotations below the pelvis (the
        # root and pelvis conventions of the avatar's source model differ from this file's), then the whole
        # skeleton turned so it stands up (+Z) with its left at +X.
        for o in new:
            o.animation_data_clear()
        for o in new:
            nm = o.name.split(".")[0]
            if o.parent is None:
                o.matrix_basis = Matrix.Scale(o.matrix_world.to_scale()[0], 4)  # keep the file's unit scale
                continue
            if nm in rest:
                (px, py, pz), q = rest[nm]
                o.rotation_mode = "QUATERNION"
                o.rotation_quaternion = _unity_to_fbx_rot(q)
                o.location = Vector((-px, py, pz)) * 100.0
        bpy.context.view_layer.update()
        pos = {h: by[b].matrix_world.translation.copy() for h, b in hm.items() if b in by}
        up = (pos["Head"] - pos["Hips"]).normalized()
        lft = (pos["LeftUpperLeg"] - pos["RightUpperLeg"])
        lft = (lft - up * lft.dot(up)).normalized()
        fwd = up.cross(lft)  # completes a right-handed frame (x=left, y=up x left, z=up)
        Mrest = Matrix((lft, fwd, up)).transposed()  # columns: where X, Y, Z of the aligned frame are now
        Qr = Mrest.to_quaternion().inverted()
        self.rest = {h: Qr @ _wrot(by[b]) for h, b in hm.items() if b in by}
        self.rest_positions = {h: Qr @ (p - pos["Hips"]) for h, p in pos.items()}
        self.rest_hips = Vector((0, 0, (Qr @ (pos["Hips"] - (pos["LeftFoot"] + pos["RightFoot"]) / 2)).z))
        self._enforce_tpose()
        # the pelvis height over the feet in the sampled frames, for the bob
        for f, s in self.samples.items():
            pp = raw[f]["_pos"]
            feet = min(pp["LeftFoot"].z, pp["RightFoot"].z)
            s["_hip_height"] = pp["Hips"].z - feet
        for o in new:
            bpy.data.objects.remove(o, do_unlink=True)
        for a in acts:
            bpy.data.actions.remove(a)


CHAINS = [(("LeftUpperArm", "LeftLowerArm", "LeftHand"), Vector((1, 0, 0))),
          (("RightUpperArm", "RightLowerArm", "RightHand"), Vector((-1, 0, 0))),
          (("LeftUpperLeg", "LeftLowerLeg", "LeftFoot"), Vector((0, 0, -1))),
          (("RightUpperLeg", "RightLowerLeg", "RightFoot"), Vector((0, 0, -1)))]


def _enforce(clip):
    """Like Unity's Enforce T-Pose: arms straight out sideways, legs straight down, in the clip's rest. The Crafting
    pack's avatar was made from a model standing with its arms down."""
    for chain, want in CHAINS:
        if not all(h in clip.rest_positions for h in chain):
            continue
        for i in range(len(chain) - 1):
            a, b = chain[i], chain[i + 1]
            d = clip.rest_positions[b] - clip.rest_positions[a]
            if d.length < 1e-6:
                continue
            q = d.normalized().rotation_difference(want)
            pivot = clip.rest_positions[a].copy()
            for h in chain[i:]:
                clip.rest[h] = q @ clip.rest[h]
                clip.rest_positions[h] = pivot + q @ (clip.rest_positions[h] - pivot)


ORDER = ["Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
         "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
         "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
         "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
         "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"]


def apply(rig, clip, frame, target_map, forearm_twist=0.5, hips_scale=None):
    """Pose rig (the Dark Fortress / MFH rig in Blender) as the clip is at frame."""
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    A = rig.matrix_world
    s = clip.samples[frame]
    rest_t = {h: (A @ rig.data.bones[b].matrix_local).to_quaternion().normalized()
              for h, b in target_map.items() if b in rig.data.bones}
    world = {}
    for h in ORDER:
        if h not in s or h not in clip.rest or h not in rest_t:
            continue
        world[h] = s[h] @ clip.rest[h].inverted() @ rest_t[h]
    # Mecanim's forearm twist: half the hand's roll about the forearm goes onto the forearm
    if forearm_twist:
        for side in ("Left", "Right"):
            la, hd = side + "LowerArm", side + "Hand"
            if la in world and hd in world:
                rel = world[la].inverted() @ world[hd]
                # the forearm's own axis is its bone Y axis in the target rest
                swing, twist = _swing_twist(rel, Vector((0, 1, 0)))
                part = Quaternion((1, 0, 0, 0)).slerp(twist, forearm_twist)
                world[la] = world[la] @ part
    for h in ORDER:
        if h not in world:
            continue
        b = target_map[h]
        pb = rig.pose.bones[b]
        bpy.context.view_layer.update()
        head_w = A @ pb.head
        M = Matrix.Translation(head_w) @ world[h].to_matrix().to_4x4()
        # keep the armature's scale out of the bone matrix
        sc = A.to_scale()[0]
        pb.matrix = A.inverted() @ M @ Matrix.Scale(sc, 4)
    # hips height: the clip's pelvis height over its lower foot, scaled to the target's leg length
    bpy.context.view_layer.update()
    if "Hips" in target_map and clip.rest_hips.z:
        pb = rig.pose.bones[target_map["Hips"]]
        rest_head = A @ rig.data.bones[target_map["Hips"]].head_local
        ankle = A @ rig.data.bones[target_map["LeftFoot"]].head_local
        k = (rest_head.z - ankle.z) / clip.rest_hips.z
        dz = (s["_hip_height"] - clip.rest_hips.z) * k
        cur = pb.matrix.copy()
        cur.translation = A.inverted() @ (rest_head + Vector((0, 0, dz)))
        pb.matrix = cur
    bpy.context.view_layer.update()


def _swing_twist(q, axis):
    v = Vector((q.x, q.y, q.z))
    p = axis * v.dot(axis)
    t = Quaternion((q.w, p.x, p.y, p.z))
    if t.magnitude < 1e-8:
        return q, Quaternion((1, 0, 0, 0))
    t.normalize()
    return q @ t.inverted(), t


def target_map(repo):
    return human_map(os.path.join(repo, TARGET_META))
