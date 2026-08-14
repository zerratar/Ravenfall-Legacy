"""
Preflight check before migrating a screen to UI Toolkit.

Reports which of our own MonoBehaviours live on, or underneath, each Canvas in a scene.

Why this exists: the project's convention is to put UI scripts on the Canvas object and on its
children, because that makes them easy to find in the hierarchy. That means disabling a Canvas
GameObject to hide the old UI also stops those scripts running, and they fail silently because
nothing executes at all. It already happened once on Update.unity and CodeOfConduct.unity, where
GameUpdater and CodeOfConductController both sit on the Canvas itself.

The rule that follows: hide the old UI by disabling the Canvas COMPONENT, never the GameObject.
Run this first so you know what is at stake for the screen you are about to touch.

Usage:
    python tools/canvas-script-preflight.py Assets/Scenes/MainWorld.unity
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GUID_RE = re.compile(r'^guid:\s*([0-9a-f]{32})', re.M)


def script_guids():
    """guid -> script file name, for our own scripts only."""
    out = {}
    for dp, _, fns in os.walk(os.path.join(ROOT, "Assets", "Scripts")):
        for fn in fns:
            if not fn.endswith(".cs.meta"):
                continue
            p = os.path.join(dp, fn)
            try:
                m = GUID_RE.search(open(p, encoding="utf-8", errors="replace").read())
            except OSError:
                continue
            if m:
                out[m.group(1)] = fn[:-8]
    return out


def analyse(scene_path):
    guids = script_guids()
    text = open(scene_path, encoding="utf-8", errors="replace").read()
    docs = text.split("--- !u!")

    go_names, go_active = {}, {}
    transform_parent, transform_go, go_transform = {}, {}, {}
    canvas_gos, mono_by_go = set(), {}

    for d in docs:
        head, _, body = d.partition("\n")
        cls = head.split()[0] if head.split() else ""
        fid_m = re.search(r'&(\d+)', head)
        fid = fid_m.group(1) if fid_m else None

        if cls == "1" and fid:
            n = re.search(r'm_Name:\s*(.*)', body)
            a = re.search(r'm_IsActive:\s*(\d)', body)
            go_names[fid] = (n.group(1).strip() if n else "?")
            go_active[fid] = (a.group(1) if a else "?")
        elif cls in ("4", "224") and fid:
            g = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', body)
            f = re.search(r'm_Father:\s*\{fileID:\s*(\d+)\}', body)
            if g:
                transform_go[fid] = g.group(1)
                go_transform[g.group(1)] = fid
            transform_parent[fid] = f.group(1) if f else "0"
        elif cls == "223" and fid:
            g = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', body)
            if g:
                canvas_gos.add(g.group(1))
        elif cls == "114" and fid:
            g = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', body)
            s = re.search(r'm_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})', body)
            if g and s and s.group(1) in guids:
                mono_by_go.setdefault(g.group(1), []).append(guids[s.group(1)])

    def ancestors(go):
        seen, t = [], go_transform.get(go)
        while t and t != "0":
            parent_t = transform_parent.get(t, "0")
            if parent_t == "0":
                break
            pgo = transform_go.get(parent_t)
            if not pgo:
                break
            seen.append(pgo)
            t = parent_t
        return seen

    print(f"scene: {os.path.relpath(scene_path, ROOT)}")
    print(f"canvases: {len(canvas_gos)}")
    print()

    total_on, total_under = 0, 0
    for c in sorted(canvas_gos, key=lambda g: go_names.get(g, "")):
        name, active = go_names.get(c, "?"), go_active.get(c, "?")
        on_canvas = mono_by_go.get(c, [])
        under = []
        for go, scripts in mono_by_go.items():
            if go != c and c in ancestors(go):
                under.extend((go_names.get(go, "?"), s) for s in scripts)

        total_on += len(on_canvas)
        total_under += len(under)

        print(f"  Canvas '{name}' (GameObject active={active})")
        if on_canvas:
            print(f"    ON THE CANVAS OBJECT ITSELF  <-- disabling the GameObject stops these")
            for s in on_canvas:
                print(f"      {s}")
        if under:
            print(f"    on children ({len(under)})")
            for go_name, s in sorted(under)[:20]:
                print(f"      {s}  on '{go_name}'")
            if len(under) > 20:
                print(f"      ... and {len(under) - 20} more")
        if not on_canvas and not under:
            print("    no scripts of ours")
        print()

    print(f"total: {total_on} on canvas objects, {total_under} on children")
    if total_on:
        print()
        print("Disable the Canvas COMPONENT, not the GameObject, or the scripts above never run.")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    p = sys.argv[1]
    analyse(p if os.path.isabs(p) else os.path.join(ROOT, p))
