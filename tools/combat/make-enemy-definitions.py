# Groups MainWorld's enemies into kinds and writes tools/combat/enemy-definitions.json for
# tools/combat/apply-enemy-definitions.cs (combat v2, docs/combat-v2-design.md section 6).
#
# The Unity 6.7 upgrade (c016ead, 2026-08-14) turned SkillStat's levels into ObscuredIntSplit, and Unity dropped the
# old plain numbers from the scene: 136 island enemies have been Attack/Strength/Defense 1, Health 10 (combat level 3)
# ever since. Their levels come from MainWorld.unity at f944e82 (2025-05-09), the last save that still had them,
# matched by each EnemyController's file ID; enemies added since (Ravebridge, Mangrove) keep the current numbers.
#
# Inputs (made in the editor and from the scene files, see the commit that added this):
#   enemy-ids.txt          fileID|idType|path|island|chunk|prefab   for every enemy in the open scene
#   enemies.txt            the editor inventory, for each enemy's attack interval
#   scene-parse/en-f944e82.json, scene-parse/en-now.json   levels per file ID from the two scene files
#
# usage: python make-enemy-definitions.py <folder with the inputs> <output json>
import collections
import json
import re
import sys

S, OUT = sys.argv[1], sys.argv[2]
old = json.load(open(S + '/scene-parse/en-f944e82.json'))
now = json.load(open(S + '/scene-parse/en-now.json'))
inv = {}
for l in open(S + '/enemies.txt', encoding='utf-8'):
    if not l.startswith('scene|'):
        p = l.rstrip('\n').split('|')
        inv[p[1]] = p

ELEMENTS = ['Physical', 'Fire', 'Ice', 'Lightning', 'Poison']

# id -> (name, family, resistances). Resistances are flavour for when gear carries elements (nothing has
# element damage yet, so they change no fight today); tune them with the simulator.
KINDS = {
    'home-bunny': ('Bunny', 'Beast', {}),
    'home-cow': ('Cow', 'Beast', {}),
    'home-metalon-green': ('Green Metalon', 'Insect', {'Poison': 0.5, 'Fire': -0.25}),
    'home-metalon-purple': ('Purple Metalon', 'Insect', {'Lightning': 0.5, 'Fire': -0.25}),
    'home-metalon-red': ('Red Metalon', 'Insect', {'Fire': 0.5, 'Ice': -0.25}),
    'ravebridge-cow': ('Cow', 'Beast', {}),
    'ravebridge-goblin': ('Goblin', 'Humanoid', {}),
    'ravebridge-skeleton': ('Skeleton', 'Undead', {'Poison': 1.0, 'Fire': -0.25}),
    'ravebridge-tormented-soul': ('Tormented Soul', 'Spirit', {'Poison': 1.0, 'Ice': 0.25, 'Fire': -0.25}),
    'mangrove-pirate': ('Pirate', 'Humanoid', {}),
    'mangrove-pirate-captain': ('Pirate Captain', 'Humanoid', {}),
    'away-pirate': ('Pirate', 'Humanoid', {}),
    'away-pirate-captain': ('Pirate Captain', 'Humanoid', {}),
    'ironhill-dragon': ('Dragon', 'Dragon', {'Fire': 0.25}),
    'ironhill-dragon-red': ('Red Dragon', 'Dragon', {'Fire': 0.5, 'Ice': -0.25}),
    'ironhill-dragon-white': ('White Dragon', 'Dragon', {'Ice': 0.5, 'Fire': -0.25}),
    'ironhill-dragon-blue': ('Blue Dragon', 'Dragon', {'Lightning': 0.5, 'Poison': -0.25}),
    'ironhill-dragon-green': ('Green Dragon', 'Dragon', {'Poison': 0.5, 'Lightning': -0.25}),
    'ironhill-dragon-purple': ('Purple Dragon', 'Dragon', {'Fire': 0.25, 'Ice': 0.25, 'Lightning': 0.25, 'Poison': 0.25}),
    'ironhill-lord-quackers': ('Lord Quackers', 'Dragon', {'Ice': 0.5, 'Lightning': -0.25}),  # a duck at heart: water off his back
    'kyojin-ninja': ('Ninja', 'Humanoid', {'Poison': 0.25}),
    'kyojin-samurai': ('Samurai', 'Humanoid', {}),
    'iceheim-viking': ('Viking', 'Humanoid', {'Ice': 0.5, 'Fire': -0.25}),
    'iceheim-viking-chief': ('Viking Chief', 'Humanoid', {'Ice': 0.5, 'Fire': -0.25}),
    'atria-gladiator': ('Gladiator', 'Humanoid', {}),
    'eldara-elven-soldier': ('Elven Soldier', 'Humanoid', {'Lightning': 0.25}),
}


def kind(island, name, st):
    n = name.lower()
    if island == 'Main Island':
        if 'metalon' in n: return 'home-metalon-' + n.split()[0]
        if 'cow' in n: return 'home-cow'
        return 'home-bunny'
    if island == 'Ravebridge':
        for word, k in [('cow', 'ravebridge-cow'), ('goblin', 'ravebridge-goblin'), ('skeleton', 'ravebridge-skeleton'), ('soul', 'ravebridge-tormented-soul')]:
            if word in n: return k
    if island == 'Mangrove': return 'mangrove-pirate-captain' if 'captain' in n else 'mangrove-pirate'
    if island == 'Small Island': return 'away-pirate-captain' if 'captain' in n else 'away-pirate'
    if island == 'Ironhill Island':
        if 'quackers' in n: return 'ironhill-lord-quackers'
        for c in ['red', 'white', 'blue', 'green', 'purple']:
            if c + ' dragon' in n: return 'ironhill-dragon-' + c
        return 'ironhill-dragon'
    if island == 'Kyojin Island': return 'kyojin-ninja' if 'ninja' in n else 'kyojin-samurai'
    if island == 'Iceheim': return 'iceheim-viking-chief' if st[0] >= 300 else 'iceheim-viking'
    if island == 'Atria': return 'atria-gladiator'
    if island == 'Eldara': return 'eldara-elven-soldier'
    raise Exception('no kind for %s / %s' % (island, name))


groups = collections.defaultdict(list)
assign = {}
for line in open(S + '/enemy-ids.txt', encoding='utf-8'):
    fid, typ, path, isl, req, src = line.rstrip('\n').split('|')
    parts = path.split('/')
    island = parts[parts.index('Islands') + 1]
    name = re.sub(r'\s*\(\d+\)$', '', parts[-1])
    area = next(p for p in parts if p.startswith('Fighting'))
    rec = old.get(fid) or now[fid]
    s = rec['skills']
    # Healing may be 0 (most old enemies), which counts toward the combat level, so it is kept
    st = tuple(max(1, int(float(s[x] or 1))) for x in ['Attack', 'Strength', 'Defense', 'Health', 'Ranged', 'Magic']) + (int(float(s['Healing'] or 0)),)
    k = kind(island, name, st)
    interval = float(inv[path][20].replace(',', '.')) if path in inv else 1.0
    groups[k].append((st, interval, island + ', ' + area, name))
    assign[fid] = k


def definition(id, name, area, family, st, resist, interval, boss=False, scaled=False, count=0):
    r = {e: 0.0 for e in ELEMENTS}
    r.update(resist)
    return dict(Id=id, Name=name, Area=area, Family=family,
                Attack=st[0], Strength=st[1], Defense=st[2], Health=st[3], Ranged=st[4], Magic=st[5], Healing=st[6],
                Power=0, Accuracy=0, Armor=0, Style='Melee', Element='Physical', ElementShare=0.0, Resistance=r,
                Skills=[], AttackInterval=interval, IsBoss=boss, Scaled=scaled, Count=count)


defs, notes = [], []
for k, (nm, fam, res) in KINDS.items():
    g = groups[k]
    if not g:
        notes.append('no enemies: ' + k)
        continue
    st, c = collections.Counter(x[0] for x in g).most_common(1)[0]
    others = sorted(set(x[0] for x in g) - {st})
    if others:
        notes.append('%s: %d of %d had %s, now %s' % (k, len(g) - c, len(g), others, st))
    iv = collections.Counter(x[1] for x in g).most_common(1)[0][0]
    defs.append(definition(k, nm, g[0][2], fam, st, res, iv, count=len(g)))

for k, nm, area in [('dungeon-boss', 'Dungeon Boss', 'Dungeons (scaled to the party)'), ('raid-boss', 'Raid Boss', 'Raids (scaled to the raiders)')]:
    defs.append(definition(k, nm, area, 'Humanoid', (1, 1, 1, 10, 1, 1, 1), {}, 0, boss=True, scaled=True))

json.dump({'source': 'MainWorld enemies grouped by kind; levels from MainWorld.unity at f944e82 (2025-05-09), the last save '
                     'before the Unity 6.7 upgrade dropped them, or the current scene for enemies added since.',
           'definitions': defs, 'assign': assign}, open(OUT, 'w'), indent=1)
for d in defs:
    print('%-28s %3d  lv %d/%d/%d/%d  every %ss' % (d['Id'], d['Count'], d['Attack'], d['Strength'], d['Defense'], d['Health'], d['AttackInterval']))
print('\n'.join(notes))
