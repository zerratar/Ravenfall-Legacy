# Bare skin on Dark Fortress pieces in the player's own skin color (headless Blender 5.x).
#
# How the game colors skin: every MFH body part uses Assets/PolygonFantasyHeroCharacters/Materials/FantasyHero.mat
# (shader Shaders/Synty.shadergraph). The shader starts from the atlas and lerps in flat colors by mask channels;
# skin is Mask_03's red channel (black = skin), filled with the flat _Color_Skin that SyntyPlayerAppearance sets per
# player (SkinColor; WithSkin does the same on a copy of an armor tier's material for SkinParts).
#
# The Dark Fortress atlas has its skin as a block of swatches in its lower left corner; the pack marks them with
# Assets/Synty/PolygonGeneric/Textures/SkinMask.png (white = skin), which its Generic_Standard shader tints with
# _Skin_Color. That tint does not reproduce the MFH shader's flat skin, so the pieces take the game's route instead:
# every face whose UV lies in the skin swatches goes to a second material slot "Skin", with its UVs moved onto a skin
# texel of the MFH atlas. In the game that slot is the player's own body material (the one their bare arms use), so
# the skin on the armor is exactly the skin next to it, for every skin color, with no new shader.
import bpy, os
import numpy as np

DF_SKINMASK = "Assets/Synty/PolygonGeneric/Textures/SkinMask.png"
MFH_MASK3 = "Assets/PolygonFantasyHeroCharacters/Textures/Masks/PolygonFantasyHero_Texture_Mask_03.png"


def _image(path):
    img = bpy.data.images.load(path, check_existing=True)
    w, h = img.size
    return np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4), w, h


def skin_uv_box(repo):
    """(u0, v0, u1, v1) of the white area of the Dark Fortress skin mask (Blender UVs: v up)."""
    px, w, h = _image(os.path.join(repo, DF_SKINMASK))
    ys, xs = np.where(px[:, :, 0] > 0.5)
    # Blender's pixel rows start at the bottom, as UV v does
    return (xs.min() / w, ys.min() / h, (xs.max() + 1) / w, (ys.max() + 1) / h)


def face_uv(uv_data, poly):
    u = sum(uv_data[l].uv[0] for l in poly.loop_indices) / poly.loop_total
    v = sum(uv_data[l].uv[1] for l in poly.loop_indices) / poly.loop_total
    return u, v


def in_box(uv, box, margin=0.0):
    return box[0] - margin <= uv[0] <= box[2] + margin and box[1] - margin <= uv[1] <= box[3] + margin


def mfh_skin_texel(repo, lib):
    """A UV on the MFH atlas inside a skin area (Mask_03 red black over a 9x9 texel block): the center of a face of
    the bare torso that is skin."""
    px, w, h = _image(os.path.join(repo, MFH_MASK3))
    me = lib.mfh["Chr_Torso_Male_00"].data
    uvd = me.uv_layers.active.data
    for p in me.polygons:
        u, v = face_uv(uvd, p)
        x, y = int(u * w), int(v * h)
        if 4 <= x < w - 4 and 4 <= y < h - 4 and px[y - 4:y + 5, x - 4:x + 5, 0].max() < 0.5:
            return (u, v)
    raise RuntimeError("no skin texel found on the MFH atlas")


def split_skin(obj, box, skin_mat, texel):
    """Faces of obj whose UV center lies in the skin swatches: material slot 1 (skin_mat), UVs onto texel."""
    me = obj.data
    if len(me.materials) < 2:
        me.materials.append(skin_mat)
    else:
        me.materials[1] = skin_mat
    uvd = me.uv_layers.active.data
    n = 0
    for p in me.polygons:
        if in_box(face_uv(uvd, p), box):
            p.material_index = 1
            for l in p.loop_indices:
                uvd[l].uv = texel
            n += 1
    return n


def tone_material(repo, lib, name, rgb):
    """The MFH atlas with its skin area filled with a flat color, as the game's shader draws it: one material per
    skin tone, for renders (Workbench shows the image only)."""
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    from dfa_lib import MFH_TEX
    base, w, h = _image(os.path.join(repo, MFH_TEX))
    m3, w3, h3 = _image(os.path.join(repo, MFH_MASK3))
    if (w3, h3) != (w, h):
        # nearest-neighbour resample of the mask to the atlas size
        ys = (np.arange(h) * h3 // h); xs = (np.arange(w) * w3 // w)
        m3 = m3[ys][:, xs]
    out = base.copy()
    skin = m3[:, :, 0] < 0.5
    out[skin, 0], out[skin, 1], out[skin, 2] = _lin(rgb[0]), _lin(rgb[1]), _lin(rgb[2])
    # stubble (Mask_03 blue): the game paints it with _Color_Stubble; here a darker shade of the skin
    stub = (m3[:, :, 2] < 0.5) & ~skin
    out[stub, 0], out[stub, 1], out[stub, 2] = rgb[0] * 0.72, rgb[1] * 0.68, rgb[2] * 0.66
    img = bpy.data.images.new(name, w, h)
    img.pixels[:] = out.ravel()
    img.pack()
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    nt.nodes.active = tex
    return mat


def _lin(c):
    # the atlas pixels Blender hands over are already as stored (sRGB values); colors given as sRGB 0..1
    return c


TONES = {"light": (0.93, 0.78, 0.66), "medium": (0.72, 0.52, 0.38), "dark": (0.36, 0.24, 0.17)}
