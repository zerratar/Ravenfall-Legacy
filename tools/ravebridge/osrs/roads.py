# Run with: python tools/ravebridge/osrs/roads.py <lumbridge.json> <roads.json>
#
# The map's roads as centre lines for the road tool (WorldRoad), for tools/ravebridge/lumbridge-test.cs: each kind of
# road tile (stone roads, dirt paths, Al Kharid's sandy roads) is masked at four pixels a tile, cleaned, thinned to its
# skeleton, and the skeleton cut into lines between its ends and crossings, simplified, each point with the road's
# width there (from how far it is to the road's edge). Paved areas wider than a road (the castle courtyard) are left to
# the terrain's paint. It also lists the tiles the roads cover, so their paint can go back to the ground under them.
import json
import sys

import numpy as np
from scipy import ndimage
from skimage.morphology import skeletonize

src, out = sys.argv[1], sys.argv[2]
d = json.load(open(src))
N = d['sizeX']
U = 4                      # pixels a tile
TILE = 2.5                 # metres a tile
overlay = np.array(d['overlay'])

KINDS = {
    'Cobbles': {10, 2, 3, 4},                 # Lumbridge's grey stone roads
    'Dirt': {14, 22, 101, 16, 89, 108, 102},  # earth paths
    'Gravel': {26},                           # Al Kharid's sandy roads
}
MAX_HALF = 2.6             # tiles: wider than this is a square, not a road
MIN_LEN = 3.0              # tiles: shorter bits are left out

# indoors: under an upper floor or a roof (the keep's paved halls, the palace's)
indoor = np.zeros((N, N), bool)
for level in (1, 2, 3):
    for f in d['floors'][level]:
        if len(f) < 3 or f[2] >= 0:
            indoor[f[0], f[1]] = True
for l in d['locs']:
    if l.get('surface') and l['level'] > 0:
        indoor[l['x'], l['y']] = True
# water, and the mud along it (earth tiles up to three from water are banks, not paths)
water = np.isin(overlay, [442, 6, 7])
near_water = ndimage.binary_dilation(water, iterations=3)

roads = []
covered = set()


def neighbours(p, sk):
    y, x = p
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if (dy or dx) and 0 <= y + dy < sk.shape[0] and 0 <= x + dx < sk.shape[1] and sk[y + dy, x + dx]:
                yield (y + dy, x + dx)


def rdp(points, eps):
    if len(points) < 3:
        return points
    a, b = np.array(points[0], float), np.array(points[-1], float)
    ab = b - a
    n = np.linalg.norm(ab)
    best, bi = 0.0, 0
    for i in range(1, len(points) - 1):
        p = np.array(points[i], float)
        dist = np.linalg.norm(p - a) if n == 0 else abs(ab[0] * (a[1] - p[1]) - ab[1] * (a[0] - p[0])) / n
        if dist > best:
            best, bi = dist, i
    if best > eps:
        return rdp(points[:bi + 1], eps)[:-1] + rdp(points[bi:], eps)
    return [points[0], points[-1]]


for kind, ids in KINDS.items():
    tiles = np.isin(overlay, list(ids)) & ~indoor   # [x][y]
    if kind == 'Dirt':
        tiles &= ~near_water
    mask = np.kron(tiles.T, np.ones((U, U), bool))  # [y pixel][x pixel]
    mask = ndimage.binary_closing(mask, iterations=2)
    mask = ndimage.binary_opening(mask, iterations=1)
    labels, n = ndimage.label(mask)
    sizes = ndimage.sum(mask, labels, range(1, n + 1))
    for i, size in enumerate(sizes):
        if size < 6 * U * U:
            mask[labels == i + 1] = False
    dist = ndimage.distance_transform_edt(mask) / U  # tiles to the edge
    sk = skeletonize(mask)
    # the narrow parts only: a skeleton through a square is not a road
    sk &= dist <= MAX_HALF
    pts = set(zip(*np.nonzero(sk)))
    deg = {p: sum(1 for _ in neighbours(p, sk)) for p in pts}
    nodes = {p for p in pts if deg[p] != 2}
    seen_edges = set()
    lines = []

    def walk(start, nxt):
        path = [start, nxt]
        prev, cur = start, nxt
        while cur not in nodes:
            nb = [q for q in neighbours(cur, sk) if q != prev and q not in path[-3:]]
            if not nb:
                break
            prev, cur = cur, nb[0]
            path.append(cur)
        return path

    for s in nodes:
        for nb in neighbours(s, sk):
            key = (s, nb)
            if key in seen_edges:
                continue
            path = walk(s, nb)
            seen_edges.add((s, nb))
            if len(path) >= 2:
                seen_edges.add((path[-1], path[-2]))
            lines.append(path)
    # loops with no node (a ring road)
    visited = {p for line in lines for p in line}
    for p in pts:
        if p in visited:
            continue
        ring = [p]
        visited.add(p)
        cur = p
        while True:
            nb = [q for q in neighbours(cur, sk) if q not in visited]
            if not nb:
                break
            cur = nb[0]
            visited.add(cur)
            ring.append(cur)
        ring.append(p)
        lines.append(ring)

    for path in lines:
        if len(path) * 1.0 / U < MIN_LEN:
            continue
        simple = rdp(path, 0.35 * U)
        # points every two tiles at most along the simplified line, for the widths
        dense = []
        for a, b in zip(simple, simple[1:]):
            a, b = np.array(a, float), np.array(b, float)
            steps = max(1, int(np.linalg.norm(b - a) / (2 * U)))
            for k in range(steps):
                dense.append(a + (b - a) * k / steps)
        dense.append(np.array(simple[-1], float))
        points, widths = [], []
        for p in dense:
            y, x = int(round(p[0])), int(round(p[1]))
            y = min(max(y, 0), dist.shape[0] - 1)
            x = min(max(x, 0), dist.shape[1] - 1)
            half = max(0.6, float(dist[y, x]))
            # pixel centres to world metres: x east, y north
            points.append([round((p[1] + 0.5) / U * TILE, 2), round((p[0] + 0.5) / U * TILE, 2)])
            widths.append(round(min(8.0, max(2.0, half * 2 * TILE * 0.95)), 2))
            for ty in range(int((p[0] / U) - half - 0.5), int((p[0] / U) + half + 1.5)):
                for tx in range(int((p[1] / U) - half - 0.5), int((p[1] / U) + half + 1.5)):
                    if 0 <= tx < N and 0 <= ty < N and tiles[tx, ty]:
                        covered.add((tx, ty))
        roads.append({'surface': kind, 'points': points, 'widths': widths})

json.dump({'roads': roads, 'covered': sorted(covered)}, open(out, 'w'))
print(len(roads), 'roads,', sum(len(r['points']) for r in roads), 'points,', len(covered), 'tiles covered')
for kind in KINDS:
    print(' ', kind, sum(1 for r in roads if r['surface'] == kind))
