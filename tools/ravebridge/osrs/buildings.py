# Run with: python tools/ravebridge/osrs/buildings.py <lumbridge.json> <buildings.json> [overview.png]
#
# The map's buildings, for swapping the Lumbridge test's generated ones for Ravenfall's (tools/ravebridge/lumbridge-swap.cs):
# a building is a connected patch of covered tiles (a roof surface or an upper floor over them); for each its tiles,
# bounds, storeys, the stuff of its walls (stone, plaster, wood), where its doors are and the telling things inside
# (pews, an anvil, a range, bank booths), so a stand-in of the right kind and size can be picked and turned to face
# its door.
import json
import sys
from collections import Counter

import numpy as np
from scipy import ndimage

src, out = sys.argv[1], sys.argv[2]
d = json.load(open(src))
N = d['sizeX']

covered = np.zeros((N, N), bool)
storeys = np.zeros((N, N), int)
for l in d['locs']:
    sf = l.get('surface')
    if not sf:
        continue
    # the samples of the surface, back to tiles
    x0, z0, nx, nz, hs = sf['x0'], sf['z0'], sf['nx'], sf['nz'], sf['h']
    for j in range(nz):
        for i in range(nx):
            if hs[j * nx + i] < 0:
                continue
            tx = (l['x'] * 128 + 64 + x0 + i * 32) // 128
            ty = (l['y'] * 128 + 64 + z0 + j * 32) // 128
            if 0 <= tx < N and 0 <= ty < N:
                covered[tx, ty] = True
                storeys[tx, ty] = max(storeys[tx, ty], l['level'])
for level in (1, 2, 3):
    for f in d['floors'][level]:
        if len(f) < 3 or f[2] >= 0:
            covered[f[0], f[1]] = True
            storeys[f[0], f[1]] = max(storeys[f[0], f[1]], level)

labels, n = ndimage.label(covered)

KIND_OF = {}
def wall_kind(l):
    lk = l['look']
    if not lk['rgb']:
        return None
    rgb, h = lk['rgb'], lk['height']
    if h < 200:
        return None
    if sum(rgb) > 480:
        return 'plaster'
    if rgb[0] > rgb[2] * 1.5 and rgb[0] > 80:
        return 'wood'
    return 'stone'

TELLING = ['Church pew', 'Altar', 'Organ', 'Church organ', 'Anvil', 'Furnace', 'Range', 'Cooking range', 'Bank booth',
           'Closed bank booth', 'Bed', 'Throne', 'Spinning wheel', 'Loom', 'Counter', 'Shelves', 'Hopper', 'Millstones',
           'Ladder', 'Staircase', 'Table', 'Bookcase', 'Coffin', 'Font', 'Grandfather Clock', 'Hay bales', 'Food trough',
           'Coop', 'Cooking pot', 'Tanning line', 'Water wheel', 'Sails']

buildings = []
for b in range(1, n + 1):
    tiles = np.argwhere(labels == b)
    if len(tiles) < 6:
        continue
    xs, ys = tiles[:, 0], tiles[:, 1]
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    tileset = set(map(tuple, tiles.tolist()))
    kinds = Counter()
    doors = []
    things = Counter()
    for l in d['locs']:
        x, y = l['x'], l['y']
        if not (x0 - 1 <= x <= x1 + 1 and y0 - 1 <= y <= y1 + 1):
            continue
        if l['type'] in (0, 2, 9) and l['level'] == 0:
            k = wall_kind(l)
            if k:
                kinds[k] += 1
            n_ = l['name'] or ''
            if 'oor' in n_ and (x, y) in tileset or ('oor' in n_ and any((x + dx, y + dy) in tileset for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))):
                doors.append([x, y, l['rot']])
        if l['type'] in (10, 11) and (x, y) in tileset and l['name'] in TELLING:
            things[l['name']] += 1
    buildings.append({
        'id': len(buildings) + 1,
        'tiles': [[int(a), int(c)] for a, c in tiles.tolist()],
        'bounds': [x0, y0, x1, y1],
        'size': [x1 - x0 + 1, y1 - y0 + 1],
        'area': len(tiles),
        'storeys': int(storeys[labels == b].max()) + 1,
        'walls': kinds.most_common(1)[0][0] if kinds else 'none',
        'doors': doors,
        'things': dict(things),
        'centre': [round(float(xs.mean()) + 0.5, 2), round(float(ys.mean()) + 0.5, 2)],
    })

json.dump(buildings, open(out, 'w'))
for b in buildings:
    print(b['id'], 'at', b['bounds'][:2], 'size', b['size'], 'area', b['area'], 'storeys', b['storeys'], b['walls'], 'doors', len(b['doors']), b['things'])

if len(sys.argv) > 3:
    from PIL import Image, ImageDraw
    prev = Image.open(sys.argv[3].replace('.png', '-base.png')).convert('RGB') if False else None
    T = 5
    img = Image.new('RGB', (N * T, N * T), (40, 60, 40))
    dr = ImageDraw.Draw(img)
    for x in range(N):
        for y in range(N):
            if d['overlay'][x][y] in (442, 6, 7):
                dr.rectangle([x * T, (N - 1 - y) * T, x * T + T - 1, (N - 1 - y) * T + T - 1], fill=(40, 70, 140))
    for b in buildings:
        col = {'stone': (150, 150, 150), 'plaster': (230, 225, 210), 'wood': (150, 100, 50)}.get(b['walls'], (200, 60, 200))
        for x, y in b['tiles']:
            dr.rectangle([x * T, (N - 1 - y) * T, x * T + T - 1, (N - 1 - y) * T + T - 1], fill=col)
        cx, cy = b['centre']
        dr.text((cx * T - 4, (N - cy) * T - 6), str(b['id']), fill=(255, 0, 0))
    img.save(sys.argv[3])
