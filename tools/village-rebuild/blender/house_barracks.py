# Barracks (melee): a long hall with a training yard of sand, armoured dummies (one knocked flat on its back), a
# weapon rack, shields with crossed swords on the wall and a banner. The dummies, rack, shields, swords, banner,
# barrel and bench are Synty props (anchors, see house_kit.py); the hall, yard and emblem are ours.
# About 9 x 9 m, facing +y (Unity -z).
exec(open('house_kit.py').read())
b = Builder("Village_House_Barracks")
f = Frame()
STEEL, SAND = "b9b9b9", "c6a177"

def swords_emblem(b, ef, z):
    emb(b, ef, z, [(-0.17, 0.17), (0.17, 0.17), (0.17, -0.02), (0.0, -0.2), (-0.17, -0.02)], RED, d=0.015)
    for s in (-1, 1):
        emb_bar(b, ef, z, (-0.2 * s, -0.17), (0.2 * s, 0.2), 0.035, STEEL, d=0.025)
        emb_bar(b, ef, z, (-0.13 * s, -0.04), (-0.05 * s, -0.12), 0.03, GOLD, d=0.03)

yard(b, f, 4.6, 4.4, DIRT, seed=10.0)
# the training ground: trodden gravel in the yard (HouseYard, like the yard itself)
yard(b, f, 2.9, 1.9, jag=0.1, seed=3.0, x=-1.2, y=1.9, kind="gravel")

# the long hall
hf = f.sub(0.6, -2.5, 0.0)
cottage(b, hf, 6.6, 3.6, 3.2, roof="ROOF_RED", pitch=40, overhang=0.45, plinth=0.5, shutters="red",
        front_windows=(-2.3, 2.3), side_windows=(0.0,), back_windows=(-1.6, 1.6), door_x=0.0, chimney=(2.4, -0.5))
fw = hf.sub(0, 1.8, 0)
sign(b, fw, 3.35, 2.6, 0.0, swords_emblem)
for x in (-1.15, 1.15):
    # two swords crossed behind each shield: pivots at the grips, turned 40 degrees each way about the shield's middle
    for s in (-1, 1):
        anchor(fw, "sword", x + s * 0.33, 0.05, 1.9, roll=s * math.radians(40), s=1.05)
    anchor(fw, "shield", x, 0.14, 2.35, s=0.85)

# the training yard: three dummies standing out at the far edge of the sand, beside and between the openings
# rather than in front of them, one knocked flat nearer the hall where it hides nothing
dummy(b, f, -3.5, 1.1, yaw=0.2)
dummy(b, f, -2.6, 3.0, yaw=-0.1)
dummy(b, f, -0.6, 3.1, yaw=0.3)
# the fallen one, flat on its back, base and all (the infirmary has it bandaged)
anchor(f, "dummy_down", -1.6, 1.3, yaw=math.radians(-70), tilt=math.radians(-90))
# the weapon rack: swords and spears, past the end of the hall so it stays clear of the window
rf = f.sub(4.0, 1.2, -math.pi / 2)
anchor(rf, "camp_rack", 0, 0)
# a banner pole (out at the yard's corner, past the end of the hall), a water barrel and a bench
anchor(f, "banner", 4.2, 3.1, yaw=math.radians(-20), s=0.95)
barrel(b, f, 2.2, 0.0)
anchor(f, "bench", 2.0, 3.3)

house = b.finish(recalc=False)
export_fbx([house], "Village_House_Barracks")
preview([house], "house_barracks", views=((200, 28), (150, 32)), res=(1100, 950), dist_mul=1.05)
