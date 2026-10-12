# Shared helpers for the village models. Run headless, one model per run:
#   blender --background --factory-startup --python-expr "exec(open('common.py').read()); exec(open('clock_tower.py').read())"
# (run.sh does exactly that). Every model is built from code, so the scripts are the source and the
# FBX files in Assets/Village/Models can be regenerated at any time.
#
# Materials carry the exact names of the kit materials they should use in Unity
# (PolygonFantasyKingdom_Mat_01_A and so on). The Unity side remaps them by name, so the new pieces
# share the kit's textures and sit in the scene like any Synty piece:
#   PAL   the palette atlas. Faces get their UVs collapsed onto one swatch, the way Synty colours props.
#   WALL  the castle brick texture, box projected at the kit's density (5 m per U, 2.5 m per V).
#   WALLD the same brick in the kit's darker castle wall material, for buttresses and trim.
#   ROOF  the castle roof tiles, box projected at about 6 m per unit.
#   GLASS the stained glass: the palette with emission (made by the Unity script).
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

PROJECT = r"G:/Ravenfall/Projects/Ravenfall Legacy"
KIT = PROJECT + "/Assets/PolygonFantasyKingdom/"
OUT = PROJECT + "/Assets/Village/Models/"
SHOTS = PROJECT + "/Temp/village-shots/"
os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- palette
_pal_img = bpy.data.images.load(KIT + "Textures/PolygonFantasyKingdom_Texture_01_A.png", check_existing=True)
_pw, _ph = _pal_img.size
_px = _pal_img.pixels[:]
def _pix(x, y):
    i = (y * _pw + x) * 4
    return _px[i], _px[i + 1], _px[i + 2]
# swatch candidates: grid samples whose neighbourhood is one flat colour
_swatches = []
_G = 128
for gy in range(_G):
    for gx in range(_G):
        x = int((gx + 0.5) * _pw / _G); y = int((gy + 0.5) * _ph / _G)
        c = _pix(x, y); flat = True
        for dx, dy in ((-9, 0), (9, 0), (0, -9), (0, 9), (-9, -9), (9, 9)):
            n = _pix(min(max(x + dx, 0), _pw - 1), min(max(y + dy, 0), _ph - 1))
            if abs(n[0] - c[0]) + abs(n[1] - c[1]) + abs(n[2] - c[2]) > 0.02: flat = False; break
        if flat: _swatches.append(((x + 0.5) / _pw, (y + 0.5) / _ph, c))
_swatch_cache = {}
def swatch(hexcol):
    """UV of the palette swatch closest to the colour (sRGB hex)."""
    if hexcol in _swatch_cache: return _swatch_cache[hexcol]
    t = [int(hexcol[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    best = min(_swatches, key=lambda s: (s[2][0] - t[0]) ** 2 * 0.3 + (s[2][1] - t[1]) ** 2 * 0.59 + (s[2][2] - t[2]) ** 2 * 0.11)
    _swatch_cache[hexcol] = (best[0], best[1])
    print("SWATCH %s -> %02x%02x%02x" % ((hexcol,) + tuple(int(v * 255) for v in best[2])))
    return _swatch_cache[hexcol]

# ---------------------------------------------------------------- materials
def _tex_mat(name, image_path, emission=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    m.use_backface_culling = True   # like Unity: an inside-out face shows as a hole in the previews
    nt = m.node_tree; bsdf = nt.nodes.get("Principled BSDF")
    img = bpy.data.images.load(image_path, check_existing=True)
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = 'Closest' if 'Texture_01' in image_path else 'Linear'
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.9
    if emission > 0:
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission
    return m
MAT = {
    "PAL": _tex_mat("PolygonFantasyKingdom_Mat_01_A", KIT + "Textures/PolygonFantasyKingdom_Texture_01_A.png"),
    "WALL": _tex_mat("PolygonFantasyKingdom_Mat_Castle_Wall", KIT + "Textures/Castle/PolygonFantasyKingdon_Texture_Wall_Brick_01.png"),
    "WALLD": _tex_mat("PolygonFantasyKingdom_Mat_Castle_Wall Darker", KIT + "Textures/Castle/PolygonFantasyKingdon_Texture_Wall_Brick_01.png"),
    "ROOF": _tex_mat("PolygonFantasyKingdom_Mat_Castle_Roof", KIT + "Textures/Castle/PolygonFantasyKingdon_Texture_Roof_Tile_01.png"),
    "GLASS": _tex_mat("Village_StainedGlass", KIT + "Textures/PolygonFantasyKingdom_Texture_01_A.png", emission=0.8),
}
# Materials of our own made from a kit material with another kit texture (models_import.cs makes the Unity
# side the same way): key -> (material name, kit material it copies, texture under Textures/Castle).
DERIVED = {
    "STUCCO": ("Village_Wall_Stucco", "PolygonFantasyKingdom_Mat_Castle_Wall", "PolygonFantasyKingdon_Texture_Wall_Stucco_01.png"),
    "ROOF_BROWN": ("Village_Roof_Tile_Brown", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_02.png"),
    "ROOF_RED": ("Village_Roof_Tile_Red", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_03.png"),
    "ROOF_TEAL": ("Village_Roof_Tile_Teal", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_04.png"),
    "SLAT_BLUE": ("Village_Roof_Slat_Blue", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_01.png"),
    "SLAT_BROWN": ("Village_Roof_Slat_Brown", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_02.png"),
    "SLAT_RED": ("Village_Roof_Slat_Red", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_03.png"),
    "SLAT_TEAL": ("Village_Roof_Slat_Teal", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_04.png"),
}
for _k, (_name, _base, _tex) in DERIVED.items():
    MAT[_k] = _tex_mat(_name, KIT + "Textures/Castle/" + _tex)
ROOFLIKE = ("ROOF", "ROOF_BROWN", "ROOF_RED", "ROOF_TEAL", "SLAT_BLUE", "SLAT_BROWN", "SLAT_RED", "SLAT_TEAL")

_wd = MAT["WALLD"].node_tree; _mix = _wd.nodes.new("ShaderNodeMix"); _mix.data_type = 'RGBA'; _mix.blend_type = 'MULTIPLY'
_mix.inputs[0].default_value = 1.0; _mix.inputs[7].default_value = (0.62, 0.6, 0.58, 1)
_wd.links.new([n for n in _wd.nodes if n.type == 'TEX_IMAGE'][0].outputs[0], _mix.inputs[6]); _wd.links.new(_mix.outputs[2], _wd.nodes["Principled BSDF"].inputs[0])

# ---------------------------------------------------------------- building
class Builder:
    """Collects faces into one bmesh per model, each face tagged with a material and UV rule."""
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mats = []  # material keys in slot order

    def _slot(self, key):
        if key not in self.mats: self.mats.append(key)
        return self.mats.index(key)

    def paint(self, faces, key, col=None, tile=None):
        """Material key plus a palette colour (PAL/GLASS) or a box projection tile (u_m, v_m) for WALL/ROOF."""
        slot = self._slot(key)
        for f in faces:
            f.material_index = slot
            if col is not None:
                u, v = swatch(col)
                for l in f.loops: l[self.uv].uv = (u, v)
            else:
                tu, tv = tile or ((5.0, 2.5) if key in ("WALL", "WALLD") else (4.0, 4.0) if key == "STUCCO" else (6.0, 6.0))
                n = f.normal
                ax = max(range(3), key=lambda i: abs(n[i]))
                for l in f.loops:
                    p = l.vert.co
                    if ax == 2:   l[self.uv].uv = (p.x / tu, p.y / tu)       # floors and roofs seen from above
                    elif ax == 0: l[self.uv].uv = (p.y / tu, p.z / tv)
                    else:         l[self.uv].uv = (p.x / tu, p.z / tv)
        return faces

    def add(self, verts, faces_idx):
        vs = [self.bm.verts.new(v) for v in verts]
        out = []
        for idx in faces_idx:
            try: out.append(self.bm.faces.new([vs[i] for i in idx]))
            except ValueError: pass
        return out

    # primitives. Blender axes: x right, y back (the front of a model faces -y), z up.
    def box(self, c, s, key, col=None, tile=None, rot_z=0.0, skip=(), rot=None):
        """rot, a 3x3 Matrix, turns the box freely; otherwise rot_z turns it about the vertical."""
        hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        R = rot if rot is not None else Matrix.Rotation(rot_z, 3, 'Z')
        vs = [Vector(c) + R @ Vector((x, y, z)) for z in (-hz, hz) for y in (-hy, hy) for x in (-hx, hx)]
        quads = {"bottom": (0, 2, 3, 1), "top": (4, 5, 7, 6), "front": (0, 1, 5, 4), "back": (2, 6, 7, 3), "left": (0, 4, 6, 2), "right": (1, 3, 7, 5)}
        fs = self.add(vs, [q for k, q in quads.items() if k not in skip])
        for f in fs: f.normal_update()
        return self.paint(fs, key, col, tile)

    def prism(self, c, r, h, n, key, col=None, tile=None, r_top=None, rot=0.0, cap_top=True, cap_bottom=True):
        """n-sided prism or frustum standing on c (r_top=0 gives a cone or pyramid)."""
        r_top = r if r_top is None else r_top
        c = Vector(c)
        ring = lambda rr, z: [c + Vector((math.cos(rot + 2 * math.pi * i / n) * rr, math.sin(rot + 2 * math.pi * i / n) * rr, z)) for i in range(n)]
        if r_top <= 1e-4:
            vs = ring(r, 0) + [c + Vector((0, 0, h))]
            idx = [(i, (i + 1) % n, n) for i in range(n)]
            if cap_bottom: idx.append(tuple(reversed(range(n))))
        else:
            vs = ring(r, 0) + ring(r_top, h)
            idx = [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
            if cap_bottom: idx.append(tuple(reversed(range(n))))
            if cap_top: idx.append(tuple(range(n, 2 * n)))
        fs = self.add(vs, idx)
        for f in fs: f.normal_update()
        return self.paint(fs, key, col, tile)

    def sphere(self, c, r, key, col, seg=8, rings=5, sz=1.0):
        c = Vector(c); vs = [c + Vector((0, 0, -r * sz))]
        for j in range(1, rings):
            phi = math.pi * j / rings - math.pi / 2
            for i in range(seg):
                th = 2 * math.pi * i / seg
                vs.append(c + Vector((math.cos(th) * math.cos(phi) * r, math.sin(th) * math.cos(phi) * r, math.sin(phi) * r * sz)))
        vs.append(c + Vector((0, 0, r * sz)))
        top = len(vs) - 1; idx = []
        for i in range(seg): idx.append((0, 1 + (i + 1) % seg, 1 + i))
        for j in range(rings - 2):
            a = 1 + j * seg; b = a + seg
            for i in range(seg): idx.append((a + i, a + (i + 1) % seg, b + (i + 1) % seg, b + i))
        a = 1 + (rings - 2) * seg
        for i in range(seg): idx.append((a + i, a + (i + 1) % seg, top))
        fs = self.add(vs, idx)
        for f in fs: f.normal_update()
        return self.paint(fs, key, col)

    def paint_cyl(self, faces, key, centre, radius, tile=None):
        """Wraps a tiling texture round a vertical axis: u follows the circumference at the given radius, v the height."""
        slot = self._slot(key)
        tu, tv = tile or ((5.0, 2.5) if key == "WALL" else (6.0, 6.0))
        cx, cy = centre[0], centre[1]
        for f in faces:
            f.material_index = slot
            fc = f.calc_center_median()
            ac = math.atan2(fc.y - cy, fc.x - cx)
            for l in f.loops:
                p = l.vert.co
                a = math.atan2(p.y - cy, p.x - cx)
                while a - ac > math.pi: a -= 2 * math.pi
                while a - ac < -math.pi: a += 2 * math.pi
                if abs(p.x - cx) < 1e-5 and abs(p.y - cy) < 1e-5: a = ac
                l[self.uv].uv = (a * radius / tu, p.z / tv)
        return faces

    def lathe(self, centre, profile, n, key, col=None, tile=None, rot=0.0, cyl_radius=None):
        """Surface of revolution from a (radius, height) profile, bottom to top. A radius of 0 closes the shape
        with a point. Walls use cylindrical UVs when cyl_radius is given."""
        c = Vector(centre)
        rings = []
        for r, z in profile:
            if r <= 1e-5: rings.append([c + Vector((0, 0, z))])
            else: rings.append([c + Vector((math.cos(rot + 2 * math.pi * i / n) * r, math.sin(rot + 2 * math.pi * i / n) * r, z)) for i in range(n)])
        vs = []; starts = []
        for ring in rings: starts.append(len(vs)); vs += ring
        idx = []
        for j in range(len(rings) - 1):
            a, b2 = rings[j], rings[j + 1]; sa, sb = starts[j], starts[j + 1]
            if len(a) == 1 and len(b2) == 1: continue
            if len(a) == 1:   idx += [(sa, sb + (i + 1) % n, sb + i) for i in range(n)]
            elif len(b2) == 1: idx += [(sa + i, sa + (i + 1) % n, sb) for i in range(n)]
            else:             idx += [(sa + i, sa + (i + 1) % n, sb + (i + 1) % n, sb + i) for i in range(n)]
        fs = self.add(vs, idx)
        for f in fs: f.normal_update()
        if cyl_radius: return self.paint_cyl(fs, key, centre, cyl_radius, tile)
        return self.paint(fs, key, col, tile)

    def quad_box(self, corners_lo, corners_hi, key, col=None):
        """A hexahedron from four bottom and four top corners (counter-clockwise seen from above)."""
        vs = list(corners_lo) + list(corners_hi)
        fs = self.add(vs, [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)])
        for f in fs: f.normal_update()
        return self.paint(fs, key, col)

    def face(self, pts, want, key, col=None):
        """One flat face, turned to face the direction want."""
        fs = self.add([Vector(p) for p in pts], [tuple(range(len(pts)))])
        for f in fs:
            f.normal_update()
            if f.normal.dot(Vector(want)) < 0: f.normal_flip()
        return self.paint(fs, key, col)

    def solid_xz(self, outline, y0, y1, key, col=None, rot_z=0.0, origin=(0, 0, 0), tile=None):
        """A slab with an (x, z) outline from y0 to y1, then turned by rot_z about the vertical and moved to
        origin. Turning keeps the winding, so the faces stay outward."""
        area = sum(outline[i][0] * outline[(i + 1) % len(outline)][1] - outline[(i + 1) % len(outline)][0] * outline[i][1] for i in range(len(outline)))
        pts = outline if area > 0 else outline[::-1]
        m = len(pts)
        R = Matrix.Rotation(rot_z, 3, 'Z'); O = Vector(origin)
        vs = [O + R @ Vector((x, y0, z)) for x, z in pts] + [O + R @ Vector((x, y1, z)) for x, z in pts]
        idx = [tuple(range(m)), tuple(reversed(range(m, 2 * m)))] + [(k, m + k, m + (k + 1) % m, (k + 1) % m) for k in range(m)]
        fs = self.add(vs, idx)
        for f in fs: f.normal_update()
        return self.paint(fs, key, col, tile)

    def finish(self, smooth=False, recalc=True):
        """recalc lets Blender guess the face directions, which suits closed shapes. Models built from open
        surfaces (towers, walls) pass False and rely on their winding: the primitives here all wind outward,
        and a lathe profile that runs upward faces out, inward along a level faces up."""
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-5)
        if recalc: bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me); self.bm.free()
        for f in me.polygons: f.use_smooth = smooth
        for k in self.mats: me.materials.append(MAT[k])
        ob = bpy.data.objects.new(self.name, me)
        scene.collection.objects.link(ob)
        return ob

# ---------------------------------------------------------------- import, export, preview
def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    return new

def export_fbx(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = OUT + name + ".fbx"
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, path_mode='STRIP', add_leaf_bones=False)
    print("EXPORTED", path, os.path.getsize(path))

def world_bounds(objs):
    lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    return lo, hi

def preview(objs, name, views=((35, 25),), res=(1000, 1000), focus=None, dist_mul=1.6):
    """Renders the objects from (azimuth, elevation) pairs in degrees, 0 azimuth = from the front (-y)."""
    bpy.context.view_layer.update()   # world matrices are stale after moving objects in the same run
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new("w"); scene.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.65, 0.8, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    if not bpy.data.objects.get("Sun"):
        sd = bpy.data.lights.new("Sun", 'SUN'); sd.energy = 3.5; sd.angle = 0.2
        so = bpy.data.objects.new("Sun", sd); scene.collection.objects.link(so)
        so.rotation_euler = (math.radians(50), 0, math.radians(-35))
    lo, hi = focus or world_bounds(objs)
    ctr = (lo + hi) / 2; size = (hi - lo).length
    cd = bpy.data.cameras.new("Cam"); cd.lens = 50
    cam = bpy.data.objects.new("Cam", cd); scene.collection.objects.link(cam); scene.camera = cam
    paths = []
    for i, (az, el) in enumerate(views):
        a, e = math.radians(az), math.radians(el)
        d = size * dist_mul
        cam.location = ctr + Vector((math.sin(a) * math.cos(e) * d, -math.cos(a) * math.cos(e) * d, math.sin(e) * d))
        cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = SHOTS + "%s_%d.png" % (name, i)
        bpy.ops.render.render(write_still=True)
        paths.append(scene.render.filepath)
    bpy.data.objects.remove(cam)
    print("PREVIEW", " ".join(paths))
