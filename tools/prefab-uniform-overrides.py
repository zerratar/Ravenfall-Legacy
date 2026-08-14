import os, re, glob, collections

ROOT = r"G:/Ravenfall/Projects/Ravenfall Legacy"
guid_re = re.compile(r'^guid:\s*([0-9a-f]{32})', re.M)
guid2path = {}
for dp, _, fns in os.walk(ROOT + "/Assets/Scripts"):
    for fn in fns:
        if fn.endswith(".cs.meta"):
            p = os.path.join(dp, fn)
            try:
                m = guid_re.search(open(p, encoding="utf-8", errors="replace").read())
            except Exception:
                continue
            if m:
                guid2path[m.group(1)] = p[:-5]

srccache = {}


def defaults_for(path):
    if path in srccache:
        return srccache[path]
    try:
        src = open(path, encoding="utf-8", errors="replace").read().replace("\r\n", "\n")
    except Exception:
        srccache[path] = {}
        return {}
    d = {}
    for m in re.finditer(
        r'^\s*(?:\[SerializeField\]\s*)?(?:\[[^\]]*\]\s*)*(?:public|private|protected|internal)\s+'
        r'(?:readonly\s+)?(float|int|bool|double)\s+(\w+)\s*(?:=\s*([^;]+))?;', src, re.M):
        d[m.group(2)] = (m.group(1), (m.group(3) or "").strip())
    srccache[path] = d
    return d


SKIP = {"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
        "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name",
        "m_EditorClassIdentifier"}

# (script, field) -> Counter of observed prefab values
obs = collections.defaultdict(collections.Counter)
codedef = {}

for pf in glob.glob(ROOT + "/Assets/Prefabs/**/*.prefab", recursive=True):
    try:
        text = open(pf, encoding="utf-8", errors="replace").read().replace("\r\n", "\n")
    except Exception:
        continue
    for d in text.split("--- !u!"):
        if not d.startswith("114 "):
            continue
        ms = re.search(r'm_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32})', d)
        if not ms:
            continue
        path = guid2path.get(ms.group(1))
        if not path:
            continue
        defs = defaults_for(path)
        for line in d.split("\n"):
            fm = re.match(r'^  (\w+):\s*(.+?)\s*$', line)
            if not fm:
                continue
            name, pval = fm.group(1), fm.group(2)
            if name in SKIP or name not in defs:
                continue
            typ, cval = defs[name]
            try:
                if typ == "bool":
                    pv, cv = (pval == "1"), (cval == "true")
                elif typ in ("float", "double"):
                    pv = float(pval)
                    cv = float(re.sub(r'[fd]$', '', cval)) if cval else 0.0
                else:
                    pv = int(pval)
                    cv = int(cval) if cval else 0
            except ValueError:
                continue
            key = (os.path.basename(path), name)
            obs[key][pv] += 1
            codedef[key] = cv

print("Fields where EVERY prefab overrides to the SAME value (code default is simply wrong):")
print(f"{'script':30s} {'field':28s} {'code':>9s} {'prefab':>9s} {'instances':>10s}")
print("-" * 92)
n = 0
for key, c in sorted(obs.items(), key=lambda kv: -sum(kv[1].values())):
    if len(c) != 1:
        continue
    val = next(iter(c))
    if val == codedef[key]:
        continue
    if sum(c.values()) < 3:
        continue
    print(f"{key[0]:30s} {key[1]:28s} {str(codedef[key]):>9s} {str(val):>9s} {sum(c.values()):>10d}")
    n += 1
print("\ncandidates:", n)
