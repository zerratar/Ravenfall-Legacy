import os, re, sys

ROOT = r"G:/Ravenfall/Projects/Ravenfall Legacy"
PREFAB = ROOT + "/Assets/Prefabs/Player.prefab"

# guid -> script path
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

text = open(PREFAB, encoding="utf-8", errors="replace").read().replace("\r\n", "\n")
docs = text.split("--- !u!")

SKIP = {
    "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
    "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name",
    "m_EditorClassIdentifier",
}

results = []
for d in docs:
    if not d.startswith("114 "):
        continue
    ms = re.search(r'm_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32})', d)
    if not ms:
        continue
    path = guid2path.get(ms.group(1))
    if not path:
        continue
    src = open(path, encoding="utf-8", errors="replace").read().replace("\r\n", "\n")

    # code defaults for serialized-capable fields
    defaults = {}
    for m in re.finditer(
        r'^\s*(?:\[SerializeField\]\s*)?(?:\[[^\]]*\]\s*)*(?:public|private|protected|internal)\s+'
        r'(?:readonly\s+)?(float|int|bool|double|string)\s+(\w+)\s*(?:=\s*([^;]+))?;',
        src, re.M):
        typ, name, val = m.group(1), m.group(2), (m.group(3) or "").strip()
        defaults[name] = (typ, val)

    for line in d.split("\n"):
        fm = re.match(r'^  (\w+):\s*(.+?)\s*$', line)
        if not fm:
            continue
        name, pval = fm.group(1), fm.group(2)
        if name in SKIP or name not in defaults:
            continue
        typ, cval = defaults[name]
        if typ == "bool":
            pv = "true" if pval == "1" else "false"
            cv = cval if cval else "false"
        elif typ in ("float", "double"):
            try:
                pv = float(pval)
            except ValueError:
                continue
            try:
                cv = float(re.sub(r'[fd]$', '', cval)) if cval else 0.0
            except ValueError:
                continue
            if abs(pv - cv) < 1e-6:
                continue
            results.append((os.path.basename(path), name, typ, cval or "(none)", pval, "MISMATCH"))
            continue
        elif typ == "int":
            try:
                pv = int(pval)
            except ValueError:
                continue
            try:
                cv = int(cval) if cval else 0
            except ValueError:
                continue
            if pv == cv:
                continue
            results.append((os.path.basename(path), name, typ, cval or "(none)", pval, "MISMATCH"))
            continue
        else:
            continue
        if pv != cv:
            results.append((os.path.basename(path), name, typ, cval or "(none)", pval, "MISMATCH"))

print(f"{'script':32s} {'field':30s} {'type':7s} {'code default':16s} {'prefab'}")
print("-" * 110)
for r in sorted(results):
    print(f"{r[0]:32s} {r[1]:30s} {r[2]:7s} {r[3]:16s} {r[4]}")
print()
print("total mismatches:", len(results))
