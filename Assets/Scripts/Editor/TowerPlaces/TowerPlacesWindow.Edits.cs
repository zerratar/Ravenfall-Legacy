using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Ravenfall > Tower Places: the owner's hand edits (TowerPlaceEdits): Show my edits, Revert, Reapply my edits.</summary>
public sealed partial class TowerPlacesWindow
{
    private Label editsSummary;
    private Button showEditsButton, reapplyButton, stampButton;
    private VisualElement editsList;
    [SerializeField] private bool showEdits;
    private bool editsStale = true;

    partial void EnableEdits()
    {
        EditorApplication.hierarchyChanged += MarkEditsStale;
        Undo.postprocessModifications += OnModifications;
    }

    partial void DisableEdits()
    {
        EditorApplication.hierarchyChanged -= MarkEditsStale;
        Undo.postprocessModifications -= OnModifications;
    }

    private UndoPropertyModification[] OnModifications(UndoPropertyModification[] mods)
    {
        MarkEditsStale();
        return mods;
    }

    private void MarkEditsStale() => editsStale = true;

    partial void BuildEdits(VisualElement panel)
    {
        panel.Add(Header("My edits (kept through a rebuild)"));
        editsSummary = Text("", Dim);
        panel.Add(editsSummary);
        var row = Row();
        row.style.marginTop = 4;
        showEditsButton = Btn("Show my edits", () => { showEdits = !showEdits; editsStale = true; RefreshEdits(); });
        row.Add(showEditsButton);
        reapplyButton = Btn("Reapply my edits", Reapply, "Put the saved change list back on the open place (after a rebuild; the builders do it themselves)");
        row.Add(reapplyButton);
        stampButton = Btn("Give objects ids", StampOpen, "This place has no fixed ids yet: give every object one, with how it stands now as what was built");
        row.Add(stampButton);
        panel.Add(row);
        editsList = new VisualElement();
        editsList.style.marginTop = 4;
        panel.Add(editsList);
        panel.Add(Text("Kept: objects moved, turned, scaled, put under another parent, renamed, hidden or shown; deleted; added (a prefab, a duplicate of an object of the place, or an empty object). Not kept: terrain sculpting and painting, materials, a component's own values, the order of children, the inside of an added copy.", Dim, 12));
        rootVisualElement.schedule.Execute(() => { if (editsStale && showEdits) RefreshEdits(); }).Every(1000);
    }

    partial void RefreshEdits()
    {
        if (editsSummary == null) return;
        var scene = OpenSceneOfSelected();
        var playing = EditorApplication.isPlayingOrWillChangePlaymode;
        editsList.Clear();
        showEditsButton.text = showEdits ? "Hide my edits" : "Show my edits";
        if (Selected == null || !scene.IsValid() || playing)
        {
            var file = Selected != null ? TowerPlaceEdits.Load(Selected.Layout) : null;
            editsSummary.text = file != null && file.Count > 0 ? file.Count + " edit(s) saved in " + TowerPlaceEdits.FileFor(Selected.Layout) + ". Open the place to see them." : "Open the place to see its edits.";
            showEditsButton.SetEnabled(false); reapplyButton.SetEnabled(false); stampButton.style.display = DisplayStyle.None;
            return;
        }
        var ids = TowerPlaceEdits.Registry(scene, false);
        stampButton.style.display = ids ? DisplayStyle.None : DisplayStyle.Flex;
        showEditsButton.SetEnabled(ids);
        var saved = TowerPlaceEdits.Load(scene.name);
        reapplyButton.SetEnabled(ids && saved.Count > 0);
        if (!ids)
        {
            editsSummary.text = "This place has no fixed ids yet, so edits to it cannot be kept through a rebuild. Give objects ids first (before editing).";
            editsSummary.style.color = Warn;
            return;
        }
        editsSummary.style.color = Dim;
        if (!showEdits)
        {
            editsSummary.text = saved.Count > 0 ? saved.Count + " edit(s) saved in " + TowerPlaceEdits.FileFor(scene.name) + "." : "No edits saved for this place yet (" + TowerPlaceEdits.FileFor(scene.name) + " is written when the place is saved).";
            return;
        }
        editsStale = false;
        var now = TowerPlaceEdits.Compute(scene, false);
        var count = now.changed.Count + now.deleted.Count + now.added.Count;
        editsSummary.text = count == 0 && saved.unapplied.Count == 0
            ? "No edits: the place is as it was built."
            : count + " edit(s) now" + (scene.isDirty ? " (save the place to write them to " + TowerPlaceEdits.FileFor(scene.name) + ")" : ", in " + TowerPlaceEdits.FileFor(scene.name)) + (saved.unapplied.Count > 0 ? "; " + saved.unapplied.Count + " could not be put back after the last rebuild" : "") + ".";
        var shown = 0;
        void Add(TowerPlaceEdits.Edit e, string kind)
        {
            if (++shown > 300) { if (shown == 301) editsList.Add(Text("... and more", Dim, 12)); return; }
            var r = Row();
            r.style.paddingTop = 2; r.style.paddingBottom = 2;
            var label = Text(Describe(e, kind, scene), kind == "unapplied" ? Bad : (Color?)null, 12);
            label.style.flexGrow = 1; label.style.flexShrink = 1;
            label.tooltip = e.id;
            r.Add(label);
            if (kind != "deleted" && kind != "unapplied")
            {
                var look = Btn("Look", () => Look(scene, e));
                look.style.height = 22; look.style.fontSize = 12;
                r.Add(look);
            }
            var undo = Btn(kind == "unapplied" ? "Forget" : "Revert", () => RevertEdit(scene, e, kind), kind == "unapplied" ? "Drop it from the change list" : "Put it back as it was built");
            undo.style.height = 22; undo.style.fontSize = 12;
            r.Add(undo);
            editsList.Add(r);
        }
        foreach (var e in now.changed) Add(e, "changed");
        foreach (var e in now.deleted) Add(e, "deleted");
        foreach (var e in now.added) Add(e, "added");
        foreach (var e in saved.unapplied) Add(e, "unapplied");
    }

    private static string Describe(TowerPlaceEdits.Edit e, string kind, Scene scene)
    {
        var name = string.IsNullOrEmpty(e.name) ? e.id : e.name;
        switch (kind)
        {
            case "deleted": return "Deleted: " + name;
            case "added":
                return "Added: " + name + (string.IsNullOrEmpty(e.prefab) ? string.IsNullOrEmpty(e.copyOf) ? " (from scratch: comes back empty)" : " (a copy)" : " (" + System.IO.Path.GetFileNameWithoutExtension(e.prefab) + ")");
            case "unapplied": return "Not put back (" + e.what + "): " + name + (string.IsNullOrEmpty(e.note) ? "" : ", " + e.note);
        }
        var parts = new List<string>();
        if (e.HasTransform && e.built != null && e.built.Length >= 3)
        {
            var d = (TowerPlaceJson.ToVec(e.position, Vector3.zero) - TowerPlaceJson.ToVec(e.built, Vector3.zero)).magnitude;
            parts.Add(d >= 0.01f ? "moved " + d.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + " m" : "turned or scaled");
        }
        if (!string.IsNullOrEmpty(e.parent)) parts.Add("under another parent");
        if (!string.IsNullOrEmpty(e.rename)) parts.Add("renamed " + e.rename);
        if (e.active == "off") parts.Add("hidden");
        if (e.active == "on") parts.Add("shown");
        return "Changed: " + name + (e.id.Contains(">") ? " (part of a prefab copy)" : "") + ", " + string.Join(", ", parts);
    }

    private void Look(Scene scene, TowerPlaceEdits.Edit e)
    {
        var target = FindById(scene, e.id);
        if (!target) return;
        Selection.activeGameObject = target;
        TowerPlaceScenes.FrameAt(target.transform.position, 10f);
    }

    private static GameObject FindById(Scene scene, string id)
    {
        return TowerPlaceEdits.FindObject(scene, id);
    }

    private void RevertEdit(Scene scene, TowerPlaceEdits.Edit e, string kind)
    {
        var ok = TowerPlaceEdits.Revert(scene, e, kind, out var message);
        Message(message, ok ? Good : Bad);
        editsStale = true;
        RefreshAll();
    }

    private void Reapply()
    {
        var scene = OpenSceneOfSelected();
        if (!scene.IsValid()) return;
        var result = TowerPlaceEdits.Reapply(scene, scene.name);
        Message("Put back " + result.Applied + " edit(s)" + (result.Unapplied > 0 ? ", " + result.Unapplied + " could not be (listed in red; they stay in the file)" : "") + ". " + result.Log.ToString().Trim().Replace("\n", " ") + (result.Applied > 0 ? " Rebake walking if anything that is walked on moved, then save." : ""), result.Unapplied > 0 ? Warn : Good);
        editsStale = true;
        RefreshAll();
    }

    private void StampOpen()
    {
        var scene = OpenSceneOfSelected();
        if (!scene.IsValid()) return;
        var n = TowerPlaceEdits.Stamp(scene, "Tower Places");
        Message("Gave " + n + " objects their ids. Save the place to keep them.", Good);
        RefreshAll();
    }
}
