# The Dark Fortress weapon and head prefabs (docs/dark-fortress-design.md): writes their table for
# dark-fortress-prefabs.cs to %TEMP%/df-make.txt. Run: python tools/dark-fortress/dark-fortress-prefabs.py [names...],
# then unity command eval_file --file tools/dark-fortress/dark-fortress-prefabs.cs. The prefabs are in Assets/Resources
# (not versioned), so this table is their record.
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from rot import compose
D = "Assets/Synty/PolygonDarkFortress/Prefabs/Weapons"
OUT = "Assets/Resources/Character/Weapons/DarkFortress"
# kind: base lp, base euler, model-space extra rotation
KINDS = {
    "1h":    ((0.078, 0.043, -0.003), (340.918, 95.319, 254.105), (0, 90, 0)),
    "2hsw":  ((0.088, 0.05, -0.002), (282.565, 180, 180), (0, 0, 0)),
    "2hax":  ((0.099, 0.025, -0.092), (0.567, 267.506, 102.795), (0, 90, 0)),
    "staff": ((0.069, -0.03, 0.0), (350.735, 178.501, 97.785), (90, 0, 0)),
    "shield":((0.033, -0.081, 0.0), (275.839, 270, 0), (0, 0, 0)),
    "bow":   ((-0.077, -0.048, 0.013), (29.602, 195.58, 350.886), (0, 0, 0)),
}
# file name, source, kind, scale, grip offset (model space y, before scale) or None, extra rotation override
W = [
    ("DF Sword 01", "Sword_01", "2hsw", 1.15, None),
    ("DF Sword 02", "Sword_02", "2hsw", 1.12, None),
    ("DF Sword 03", "Sword_03", "1h", 0.72, None),
    ("DF Mace 01", "Mace_01", "1h", 0.72, 0.33),
    ("DF Mace 02", "Mace_02", "1h", 0.72, 0.2),
    ("DF Club 01", "Club_01", "1h", 0.95, 0.06),
    ("DF Hammer 01", "Hammer_01", "1h", 0.85, 0.2),
    ("DF Axe 01", "Axe_01", "2hax", 1.15, 0.3),
    ("DF Saw 01", "Saw_01", "2hax", 1.0, 0.3),
    ("DF Scythe 01", "Scythe_01", "staff", 0.85, -0.1),
    ("DF Staff 01", "Staff_01", "staff", 0.85, -0.35),
    ("DF Staff 02", "Staff_02", "staff", 0.8, -0.4),
    ("DF Staff 03", "Staff_03", "staff", 0.95, -0.3),
    ("DF Shield 01", "Shield_01", "shield", 0.72, None),
    ("DF Shield 02", "Shield_02", "shield", 0.75, None),
    ("DF Cannon 01", "Cannon_01", "bow", 0.8, None),
]
# hand set: name|lp|euler|scale|offset (empty keeps the kind's). The cannon points along the arm when the bow is drawn.
over = {"DF Cannon 01": ["DF Cannon 01", "", "7.916,290.111,29.916", ""]}
# the head pieces: the pack's attachments at the Head bone's place (from the game's Head_Attachment), scale 1; the
# Colossus's are made for a tilted head and are not used
HEADS = ["Helmet_02", "Helmet_03", "Helmet_06", "Helmet_07", "Helmet_08", "Helmet_09", "Crown_01", "Crown_02", "Crown_03",
         "Mask_01", "Mask_02", "Mask_03", "Mask_04"]
HEAD_OUT = "Assets/Resources/Character/Appearance/Head/DarkFortress"
HEAD_SRC = "Assets/Synty/PolygonDarkFortress/Prefabs/Characters/Chr_Attach"
only = sys.argv[1:]
lines = []
for name, src, kind, scale, off in W:
    if only and name not in only: continue
    lp, base, extra = KINDS[kind]
    e = compose(base, extra)
    lps = ','.join(str(v) for v in lp)
    es = ','.join(str(v) for v in e)
    sc = str(scale)
    offs = '' if off is None else '0,%s,0' % off
    if name in over:
        p = over[name]
        lps = p[1] or lps; es = p[2] or es; sc = p[3] or sc
        if len(p) > 4: offs = p[4]
    lines.append('|'.join([f"{OUT}/{name}.prefab", f"{D}/SM_Wep_{src}.prefab", lps, es, sc, offs]))
for h in HEADS:
    name = "DF " + h.replace("_", " ")
    if only and name not in only: continue
    scale = "1.08" if h == "Mask_04" else "1"
    lines.append('|'.join([f"{HEAD_OUT}/{name}.prefab", f"{HEAD_SRC}/SM_Chr_Attach_{h}.prefab", "0,-0.216,-0.011", "0,0,0", scale, ""]))
open(os.path.join(os.environ['TEMP'], 'df-make.txt'), 'w').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
