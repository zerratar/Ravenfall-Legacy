# Fisher's log (fishing): a fisher's hut, fish drying on a rack, a net hung on a frame with floats, an upturned
# rowboat with its oars, barrels of the catch, rods against the wall, and a tub with a rubber duck in it.
# The rack and its fish, the net, the rowboat, the oars, the rods, the barrels, the fish in them and the bucket are
# Synty props; the hut, the net frame, the tub and the duck are ours. About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Fishing")
f = Frame()
FISH, FISH_B, NET, FLOAT, DUCK, BEAK = "b9b9b9", "5f8191", "3a2e2c", "e8862a", "e4aa60", "e8862a"

def fish_emblem(b, ef, z):
    emb(b, ef, z, [(-0.2, 0.0), (-0.08, 0.1), (0.1, 0.08), (0.16, 0.0), (0.1, -0.08), (-0.08, -0.1)], FISH_B, d=0.02)
    emb(b, ef, z, [(0.14, 0.0), (0.24, 0.09), (0.24, -0.09)], FISH_B, d=0.02)
    emb_circle(b, ef, z, -0.13, 0.02, 0.018, DARK, d=0.025, n=6)

yard(b, f, 4.6, 4.4, DIRT, seed=14.0)

# the hut
hf = f.sub(2.1, -2.3, 0.0)
cottage(b, hf, 4.2, 3.4, 2.9, roof="SLAT_TEAL", pitch=42, overhang=0.42, plinth=0.4, shutters="blue",
        front_windows=(-1.0,), side_windows=(0.0,), back_windows=(0.0,), door_x=0.85, chimney=(-1.2, -0.6))
fw = hf.sub(0, 1.7, 0)
sign(b, fw, 2.15, 2.4, 0.0, fish_emblem)
for k, x in enumerate((1.55, 1.75)):                      # rods between the door and the corner, leaning on the wall
    anchor(fw, "fishing_rod", x, 0.4, yaw=k * 0.4, tilt=math.radians(-8), s=1.0 - 0.1 * k)

# the drying rack with its fish
rf = f.sub(-2.3, -2.4, 0.15)
anchor(rf, "fish_rack", 0, 0)
# the net on its frame, with orange floats
nf = f.sub(-3.95, 0.5, math.pi / 2)            # side-on along the left edge, clear of the hut window
for x in (-1.2, 1.2):
    b.box(nf.p(x, 0, 1.1), (0.12, 0.12, 2.2), "PAL", TIMBER, rot_z=nf.rot)
bar(b, nf.p(-1.35, 0, 2.15), nf.p(1.35, 0, 2.15), 0.1, 0.1, "PAL", TIMBER_L)
# the Synty net hangs from its top edge, along the frame
anchor(nf, "net", 0, 0.06, 2.1, yaw=math.pi / 2, s=1.05)

# the rowboat, keel up, and its oars
hull(b, f, -1.8, 2.3, length=3.4, beam=1.25, depth=0.5, z=0.0, yaw=0.35, col="5f8191", upturned=True)
for s in (-1, 1):
    anchor(f, "oar", 0.2 + s * 0.15, 2.6, 0.03, yaw=1.3 + s * 0.08, s=0.8)
# barrels of the catch, a bucket, the duck tub
barrel(b, f, 3.4, 0.4)
anchor(f, "fish_crate", 3.9, 1.0, yaw=0.5)
tc = f.sub(2.6, 2.7, 0.0)
b.lathe(tc.p(0, 0, 0), [(0.55, 0.0), (0.62, 0.42), (0.55, 0.42), (0.5, 0.08)], 10, "PAL", TIMBER_L)
b.lathe(tc.p(0, 0, 0), [(0.58, 0.36), (0.0, 0.36)], 10, "GLASS", "396b7c")
for z in (0.1, 0.32):
    b.lathe(tc.p(0, 0, 0), [(0.57 + 0.05 * z, z - 0.03), (0.6 + 0.05 * z, z + 0.03)], 10, "PAL", DARK)
dk = tc.sub(0.1, 0.05, 0.6)
b.sphere(dk.p(0, 0, 0.42), 0.11, "PAL", DUCK, seg=6, rings=4, sz=0.75)
b.sphere(dk.p(0, 0.08, 0.53), 0.07, "PAL", DUCK, seg=6, rings=3)
b.prism(dk.p(0, 0.15, 0.52), 0.03, 0.03, 4, "PAL", BEAK, r_top=0.01)
for s in (-1, 1): b.sphere(dk.p(s * 0.035, 0.13, 0.56), 0.012, "PAL", DARK, seg=4, rings=3)
anchor(f, "bucket", 1.2, 3.4)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Fishing")
preview([house], "house_fishing", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
