import os, re, glob, collections

ROOT = r"G:/Ravenfall/Projects/Ravenfall Legacy"

guid_re = re.compile(r'^guid:\s*([0-9a-f]{32})', re.M)
guid2path = {}
for dp, _, fns in os.walk(ROOT + "/Assets/Scripts"):
    for fn in fns:
        if not fn.endswith(".cs.meta"):
            continue
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

rows = []
files = glob.glob(ROOT + "/Assets/Prefabs/**/*.prefab", recursive=True)
for pf in files:
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
        if not defs:
            continue
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
                    pv = pval == "1"
                    cv = (cval == "true")
                elif typ in ("float", "double"):
                    pv = float(pval)
                    cv = float(re.sub(r'[fd]$', '', cval)) if cval else 0.0
                    if abs(pv - cv) < 1e-6:
                        continue
                else:
                    pv = int(pval)
                    cv = int(cval) if cval else 0
            except ValueError:
                continue
            if pv != cv:
                rows.append((os.path.basename(path), name, str(cv), str(pv),
                             os.path.relpath(pf, ROOT).replace("\\", "/")))

print("prefabs scanned:", len(files), " mismatches:", len(rows))
print()
by = collections.Counter((r[0], r[1], r[2], r[3]) for r in rows)
print(f"{'script':30s} {'field':26s} {'code':>10s} {'prefab':>10s}  count")
print("-" * 92)
for (s, f, c, p), n in by.most_common(40):
    print(f"{s:30s} {f:26s} {c:>10s} {p:>10s}  {n}")
