# Marks the world tower's islet as land on the sea chart own boats sail by
# (Assets/StreamingAssets/Sailing/sea-chart.json, baked by tools/sailing/bake-sea-chart.cs), so boats sail round it
# and not through it:
#   python tools/tower/world/sea-chart-islet.py <repo root>
# The bake looks at MainWorld only, and the tower lives in its own scene, so run this again after every bake of the
# chart and after the tower is moved (stack.json "position"); it only ever turns water into land, inside the islet's
# reach, and leaves the rest of the file as it is. Reads tools/tower/world/stack.json: a disc of the tower's radius
# plus the rocks round it, and the plaza's headland with the quay and the pier on the side the gate faces.
import base64, json, math, os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
stack = json.load(open(os.path.join(root, "tools", "tower", "world", "stack.json")))
path = os.path.join(root, "Assets", "StreamingAssets", "Sailing", "sea-chart.json")
text = open(path, encoding="utf-8", newline="").read()      # line ends kept as they are
chart = json.loads(text)
cell, ox, oz, width, height = chart["cell"], chart["originX"], chart["originZ"], chart["width"], chart["height"]
bits = bytearray(base64.b64decode(chart["water"]))

cx, cz = stack["position"]
radius = stack["baseRadius"] + 52.0            # the wall, the rocks at its foot and the stacks standing off them
fx, fz = stack["faces"][0] - cx, stack["faces"][1] - cz
length = math.hypot(fx, fz)
fx, fz = fx / length, fz / length
reach = stack["baseRadius"] + 96.0             # the plaza's headland, the quay's steps and the pier
half = 16.0

changed = 0
for z in range(height):
    for x in range(width):
        wx, wz = ox + (x + 0.5) * cell, oz + (z + 0.5) * cell
        dx, dz = wx - cx, wz - cz
        along = dx * fx + dz * fz
        across = abs(-dx * fz + dz * fx)
        land = math.hypot(dx, dz) <= radius or (0.0 <= along <= reach and across <= half)
        if not land:
            continue
        index = z * width + x
        if bits[index >> 3] & (1 << (index & 7)):
            bits[index >> 3] &= ~(1 << (index & 7)) & 0xFF
            changed += 1

if changed:
    new = base64.b64encode(bytes(bits)).decode("ascii")
    out, n = re.subn(r'("water"\s*:\s*")[^"]*(")', lambda m: m.group(1) + new + m.group(2), text, count=1)
    assert n == 1
    open(path, "w", encoding="utf-8", newline="").write(out)
print("sea chart: %d cells of water turned to land round the tower at %s, %s" % (changed, cx, cz))
