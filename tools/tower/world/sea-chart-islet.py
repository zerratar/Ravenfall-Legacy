# Puts the world tower's islet on the sea chart own boats sail by
# (Assets/StreamingAssets/Sailing/sea-chart.json, baked by tools/sailing/bake-sea-chart.cs): its ground as land, so
# boats sail round it and not through it, and its dock ("Tower"), so they can sail to it:
#   python tools/tower/world/sea-chart-islet.py <repo root> [chart=<another chart file>] [before=<a chart from before>]
# The bake looks at MainWorld only, and the tower lives in its own scene, so run this again after every bake of the
# chart and after the tower is moved or scaled (stack.json "position", "faces", "scale"). It keeps a note in the
# chart of the cells it turned to land ("towerIslet") and turns those back to water first, so a moved tower leaves
# nothing behind; the rest of the file stays as it is. A chart patched before that note existed (the tower's first
# place, 405,-60) is put right once with before=<the chart as it was before the patch>, whose water it starts from.
#
# Reads tools/tower/world/stack.json. Land: a disc of the tower's radius plus the rocks round it, the plaza's
# headland on the side the gate faces, and the pier as a thin strip. The dock: off the pier's end, where the ferry
# stops too (world-tower-build.cs puts the ferry's stop at the same distance: pier end + 8 m), heading across the
# pier's line the way the ferry passes (from Heim's side to Atria's).
import base64, json, math, os, re, sys

args = [a for a in sys.argv[1:] if "=" not in a]
opts = dict(a.split("=", 1) for a in sys.argv[1:] if "=" in a)
root = args[0] if args else os.getcwd()
stack = json.load(open(os.path.join(root, "tools", "tower", "world", "stack.json")))
built = json.load(open(os.path.join(root, "tools", "tower", "world", "stack-built.json")))
path = opts.get("chart", os.path.join(root, "Assets", "StreamingAssets", "Sailing", "sea-chart.json"))
text = open(path, encoding="utf-8", newline="").read()      # line ends kept as they are
nl = "\r\n" if "\r\n" in text else "\n"
chart = json.loads(text)
cell, ox, oz, width, height = chart["cell"], chart["originX"], chart["originZ"], chart["width"], chart["height"]
bits = bytearray(base64.b64decode(chart["water"]))

def is_water(i): return bits[i >> 3] & (1 << (i & 7))
def set_water(i, on):
    if on: bits[i >> 3] |= 1 << (i & 7)
    else: bits[i >> 3] &= ~(1 << (i & 7)) & 0xFF

# what an earlier run turned to land goes back to water
undone = 0
if "towerIslet" in chart:
    for i in chart["towerIslet"]["cells"]:
        if not is_water(i): set_water(i, True); undone += 1
elif "before" in opts:
    old = json.load(open(opts["before"], encoding="utf-8"))
    assert (old["width"], old["height"], old["originX"], old["originZ"]) == (width, height, ox, oz), "the chart from before is another chart"
    was = bytearray(base64.b64decode(old["water"]))
    for i in range(width * height):
        if (was[i >> 3] & (1 << (i & 7))) and not is_water(i): set_water(i, True); undone += 1

s = float(stack.get("scale", 1.0))
cx, cz = stack["position"]
fx, fz = stack["faces"][0] - cx, stack["faces"][1] - cz
length = math.hypot(fx, fz)
fx, fz = fx / length, fz / length                 # the way the gate faces
rx, rz = fz, -fx                                  # across it (to the right, seen from above with the gate up)
marks = built["marks"]
radius = (stack["baseRadius"] + 52.0) * s         # the wall, the rocks at its foot and the stacks standing off them
quay = -marks["quayHead"][2] * s                  # the outer edge of the quay's stone landing, out from the tower's middle
headland = quay + 4.0                             # the plaza's headland and the quay's steps
half = 16.0 * s
pier_end = quay + 15.0 * s                        # three 5 m pieces of pier (world-tower-build.cs, PierPieces)
stop = pier_end + 8.0                             # where the ferry stops and own boats come alongside

cells = []
for z in range(height):
    for x in range(width):
        wx, wz = ox + (x + 0.5) * cell, oz + (z + 0.5) * cell
        dx, dz = wx - cx, wz - cz
        along = dx * fx + dz * fz
        across = abs(dx * rx + dz * rz)
        land = math.hypot(dx, dz) <= radius or (0.0 <= along <= headland and across <= half) or (0.0 <= along <= pier_end - 3.0 and across <= 5.0)
        if not land: continue
        i = z * width + x
        if is_water(i):
            set_water(i, False)
            cells.append(i)

# the dock: the boat lies across the pier's line, bow toward the side away from Heim (the ferry's way on to Atria)
heim = next((d for d in chart["docks"] if d["island"] == "Heim"), None)
side = 1.0
if heim is not None and (heim["x"] - cx) * rx + (heim["z"] - cz) * rz > 0: side = -1.0
dock = {"island": "Tower", "x": round(cx + fx * stop, 2), "z": round(cz + fz * stop, 2), "heading": round(math.degrees(math.atan2(rx * side, rz * side)), 1)}
di = int((dock["z"] - oz) // cell) * width + int((dock["x"] - ox) // cell)
dock_wet = bool(is_water(di))

new_water = base64.b64encode(bytes(bits)).decode("ascii")
out, n = re.subn(r'("water"\s*:\s*")[^"]*(")', lambda m: m.group(1) + new_water + m.group(2), text, count=1)
assert n == 1
# the note of what was turned, on one line after the water
out = re.sub(r'\s*"towerIslet"\s*:\s*\{[^}]*\},?', '', out)
note = '"towerIslet": {"comment": "tools/tower/world/sea-chart-islet.py: the cells it turned to land for the world tower\'s islet, turned back when the tower moves.", "at": [%s, %s], "cells": [%s]},' % (cx, cz, ", ".join(str(i) for i in cells))
out, n = re.subn(r'("water"\s*:\s*"[^"]*",)', lambda m: m.group(1) + nl + "  " + note, out, count=1)
assert n == 1
# the dock entry: replaced if there is one, else added after the last
entry = '{' + nl + '      "island": "Tower",' + nl + '      "x": %s,' % dock["x"] + nl + '      "z": %s,' % dock["z"] + nl + '      "heading": %s' % dock["heading"] + nl + '    }'
if re.search(r'\{\s*"island"\s*:\s*"Tower"[^}]*\}', out):
    out = re.sub(r'\{\s*"island"\s*:\s*"Tower"[^}]*\}', lambda m: entry, out, count=1)
else:
    out, n = re.subn(r'(\})(\s*\]\s*\}\s*)$', lambda m: m.group(1) + "," + nl + "    " + entry + m.group(2), out, count=1)
    assert n == 1
check = json.loads(out)
assert check["water"] == new_water and any(d["island"] == "Tower" for d in check["docks"]) and len(check["towerIslet"]["cells"]) == len(cells)
open(path, "w", encoding="utf-8", newline="").write(out)
print("sea chart: %d cells back to water, %d cells of water turned to land round the tower at %s, %s (scale %s)" % (undone, len(cells), cx, cz, s))
print("dock Tower at %s, %s heading %s, %s; pier's end %.0f m and stop %.0f m out from the tower's middle" % (dock["x"], dock["z"], dock["heading"], "in water" if dock_wet else "ON A LAND CELL", pier_end, stop))
