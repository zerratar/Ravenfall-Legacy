using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
///     Ravenfall > Tower Places: a review pass over every place of the tower, one by one (docs/tower-places-editor.md).
///     Every floor's town and parts in climbing order (from RavenNest.Combat.Tower, so new floors show up by themselves),
///     and the tower scenes not in the climb yet. Picking one opens its scene beside MainWorld (which stays open and is
///     never saved here) and frames it in the Scene view, with markers for where players arrive, the monster spots, the
///     boss, the gates, the town's people, work stands, rest spots and strolls. A reviewed tick and a note per place are
///     kept in git (tools/tower/layouts/review.json).
/// </summary>
public sealed partial class TowerPlacesWindow : EditorWindow
{
    [SerializeField] private string selectedKey;

    private List<TowerPlaceCatalog.Entry> entries = new List<TowerPlaceCatalog.Entry>();

    [MenuItem("Ravenfall/Tower Places")]
    public static void OpenWindow()
    {
        var w = GetWindow<TowerPlacesWindow>("Tower Places");
        w.minSize = new Vector2(820, 560);
        w.Show();
    }

    private void OnEnable()
    {
        entries = TowerPlaceCatalog.Build();
        TowerPlaceReview.Load();
        TowerPlaceMarkers.Attach();
        EditorSceneManager.sceneOpened += OnScenesChanged;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        EditorSceneManager.sceneDirtied += OnSceneClosed;
        EditorSceneManager.sceneSaved += OnSceneClosed;
        EditorApplication.playModeStateChanged += OnPlayMode;
        OnEnableMore();
    }

    private void OnDisable()
    {
        TowerPlaceMarkers.Detach();
        EditorSceneManager.sceneOpened -= OnScenesChanged;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
        EditorSceneManager.sceneDirtied -= OnSceneClosed;
        EditorSceneManager.sceneSaved -= OnSceneClosed;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        OnDisableMore();
    }

    partial void OnEnableMore();
    partial void OnDisableMore();
    partial void BuildMore(VisualElement panel);
    partial void RefreshMore();

    private void OnScenesChanged(Scene s, OpenSceneMode m) => RefreshAll();
    private void OnSceneClosed(Scene s) => RefreshAll();
    private void OnPlayMode(PlayModeStateChange c) => RefreshAll();

    private TowerPlaceCatalog.Entry Selected => entries.FirstOrDefault(e => e.Key == selectedKey);

    // ---- the window (UI Toolkit) ----------------------------------------------------------------------------------

    private static Color Dim => EditorGUIUtility.isProSkin ? new Color(0.62f, 0.62f, 0.62f) : new Color(0.33f, 0.33f, 0.33f);
    private static Color RowOn => EditorGUIUtility.isProSkin ? new Color(0.17f, 0.27f, 0.38f) : new Color(0.72f, 0.82f, 0.95f);
    private static Color RowHover => EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.80f, 0.80f, 0.80f);
    private static readonly Color Good = new Color(0.35f, 0.8f, 0.4f);
    private static readonly Color Warn = new Color(0.95f, 0.7f, 0.15f);
    private static readonly Color Bad = new Color(0.95f, 0.38f, 0.32f);

    private Label progressLabel;
    private ScrollView listScroll;
    private readonly Dictionary<string, VisualElement> rows = new Dictionary<string, VisualElement>();
    private readonly Dictionary<string, VisualElement> rowTicks = new Dictionary<string, VisualElement>();
    private Label titleLabel, detailLabel, stateLabel, messageLabel;
    private Toggle reviewedToggle;
    private TextField noteField;
    private VisualElement panel, emptyPanel;
    private Button saveButton, closeButton, openButton;
    private Toggle markersToggle;
    private readonly List<Toggle> kindToggles = new List<Toggle>();

    private static Label Header(string text)
    {
        var l = new Label(text);
        l.style.unityFontStyleAndWeight = FontStyle.Bold;
        l.style.fontSize = 14;
        l.style.marginTop = 12; l.style.marginBottom = 4;
        return l;
    }

    private static VisualElement Row()
    {
        var r = new VisualElement();
        r.style.flexDirection = FlexDirection.Row;
        r.style.alignItems = Align.Center;
        return r;
    }

    private static Button Btn(string text, Action click, string tooltip = null)
    {
        var b = new Button(click) { text = text, tooltip = tooltip };
        b.style.height = 26;
        b.style.fontSize = 13;
        b.style.paddingLeft = 10; b.style.paddingRight = 10;
        return b;
    }

    private static Label Text(string text, Color? color = null, int size = 13)
    {
        var l = new Label(text);
        l.style.whiteSpace = WhiteSpace.Normal;
        l.style.fontSize = size;
        if (color.HasValue) l.style.color = color.Value;
        return l;
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft = 6; root.style.paddingRight = 6; root.style.paddingTop = 6; root.style.paddingBottom = 6;

        var top = Row();
        top.style.marginBottom = 6;
        progressLabel = new Label();
        progressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        progressLabel.style.fontSize = 14;
        progressLabel.style.flexGrow = 1;
        top.Add(progressLabel);
        var nextOpen = Btn("Next not reviewed", () => Step(+1, true), "The next place that has no reviewed tick");
        top.Add(nextOpen);
        root.Add(top);

        var split = new TwoPaneSplitView(0, 330, TwoPaneSplitViewOrientation.Horizontal);
        split.style.flexGrow = 1;
        root.Add(split);

        listScroll = new ScrollView();
        listScroll.style.flexGrow = 1;
        split.Add(listScroll);
        BuildList();

        var right = new ScrollView();
        right.style.flexGrow = 1;
        right.style.paddingLeft = 10;
        split.Add(right);

        emptyPanel = Text("Pick a place on the left. It opens beside MainWorld (which stays open and is never saved here) and the Scene view looks at it. Shortcuts in this window: [ and ] for the previous and next place.", Dim);
        emptyPanel.style.marginTop = 8;
        right.Add(emptyPanel);

        panel = new VisualElement();
        right.Add(panel);

        titleLabel = new Label();
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 17;
        titleLabel.style.whiteSpace = WhiteSpace.Normal;
        panel.Add(titleLabel);
        detailLabel = Text("", Dim);
        panel.Add(detailLabel);
        stateLabel = Text("");
        stateLabel.style.marginTop = 4;
        panel.Add(stateLabel);

        var nav = Row();
        nav.style.marginTop = 8;
        nav.Add(Btn("Previous", () => Step(-1, false), "The place before this one ( [ )"));
        openButton = Btn("Open and frame", () => OpenSelected(true), "Open the place's scene beside MainWorld and look at it");
        openButton.style.flexGrow = 1;
        openButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        nav.Add(openButton);
        nav.Add(Btn("Next", () => Step(+1, false), "The place after this one ( ] )"));
        panel.Add(nav);

        panel.Add(Header("Review"));
        reviewedToggle = new Toggle("Reviewed");
        reviewedToggle.labelElement.style.minWidth = 80;
        reviewedToggle.labelElement.style.fontSize = 13;
        reviewedToggle.RegisterValueChangedCallback(e => { if (Selected != null) { TowerPlaceReview.Set(Selected.Key, e.newValue, noteField.value); RefreshList(); } });
        panel.Add(reviewedToggle);
        noteField = new TextField("Note") { multiline = true };
        noteField.labelElement.style.minWidth = 80;
        noteField.labelElement.style.fontSize = 13;
        noteField.style.whiteSpace = WhiteSpace.Normal;
        noteField.style.minHeight = 54;
        noteField.RegisterCallback<FocusOutEvent>(_ => SaveNote());
        noteField.RegisterValueChangedCallback(_ => noteDirty = true);
        panel.Add(noteField);
        panel.Add(Text("The tick and the note are kept in " + TowerPlaceReview.FilePath + " (in git).", Dim, 12));

        panel.Add(Header("Markers in the Scene view"));
        markersToggle = new Toggle("Show markers") { value = TowerPlaceMarkers.Enabled };
        markersToggle.labelElement.style.minWidth = 110;
        markersToggle.RegisterValueChangedCallback(e => { TowerPlaceMarkers.Enabled = e.newValue; RefreshAll(); });
        panel.Add(markersToggle);
        var kinds = new VisualElement();
        kinds.style.flexDirection = FlexDirection.Row;
        kinds.style.flexWrap = Wrap.Wrap;
        kindToggles.Clear();
        foreach (TowerPlaceMarkers.Kind k in Enum.GetValues(typeof(TowerPlaceMarkers.Kind)))
        {
            var kind = k;
            var cell = Row();
            cell.style.marginRight = 12;
            cell.style.marginBottom = 2;
            var swatch = new VisualElement();
            swatch.style.width = 12; swatch.style.height = 12; swatch.style.marginRight = 4;
            swatch.style.backgroundColor = TowerPlaceMarkers.KindColors[(int)k];
            var t = new Toggle { value = TowerPlaceMarkers.Shown(k) };
            t.RegisterValueChangedCallback(e => TowerPlaceMarkers.Show(kind, e.newValue));
            var l = new Label(TowerPlaceMarkers.KindLabels[(int)k]);
            l.style.fontSize = 13;
            l.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => t.value = !t.value);
            cell.Add(t); cell.Add(swatch); cell.Add(l);
            kindToggles.Add(t);
            kinds.Add(cell);
        }
        panel.Add(kinds);
        panel.Add(Text("Big things always have a label; the small ones show theirs when the Scene view camera is near. Click a label to select its object.", Dim, 12));

        panel.Add(Header("Save"));
        var saveRow = Row();
        saveButton = Btn("Save place", SaveOpen, "Save the open tower scene (MainWorld is never saved here)");
        saveButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        saveRow.Add(saveButton);
        closeButton = Btn("Close place", CloseOpen, "Close the tower scene (asks to save it when it has changes)");
        saveRow.Add(closeButton);
        panel.Add(saveRow);

        BuildMore(panel);

        messageLabel = Text("");
        messageLabel.style.marginTop = 8;
        panel.Add(messageLabel);

        root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        RefreshAll();
    }

    private bool noteDirty;

    private void SaveNote()
    {
        if (!noteDirty || Selected == null) return;
        noteDirty = false;
        TowerPlaceReview.Set(Selected.Key, reviewedToggle.value, noteField.value);
        RefreshList();
    }

    private void OnKey(KeyDownEvent e)
    {
        if (e.target is TextField || (e.target as VisualElement)?.GetFirstAncestorOfType<TextField>() != null) return;
        if (e.keyCode == KeyCode.LeftBracket) { Step(-1, false); e.StopPropagation(); }
        if (e.keyCode == KeyCode.RightBracket) { Step(+1, false); e.StopPropagation(); }
    }

    private void BuildList()
    {
        listScroll.Clear();
        rows.Clear();
        rowTicks.Clear();
        string group = null;
        foreach (var entry in entries)
        {
            if (entry.Group != group)
            {
                group = entry.Group;
                var h = Header(group);
                h.style.marginLeft = 2;
                listScroll.Add(h);
            }
            var e = entry;
            var row = Row();
            row.style.paddingLeft = 4; row.style.paddingRight = 4; row.style.paddingTop = 3; row.style.paddingBottom = 3;
            row.style.borderTopLeftRadius = 3; row.style.borderTopRightRadius = 3; row.style.borderBottomLeftRadius = 3; row.style.borderBottomRightRadius = 3;
            // a dot: filled green when reviewed, an orange ring when it has only a note, a gray ring otherwise
            var tick = new VisualElement();
            tick.style.width = 12; tick.style.height = 12; tick.style.marginRight = 8; tick.style.marginLeft = 2;
            tick.style.flexShrink = 0;
            tick.style.borderTopLeftRadius = 6; tick.style.borderTopRightRadius = 6; tick.style.borderBottomLeftRadius = 6; tick.style.borderBottomRightRadius = 6;
            tick.style.borderTopWidth = 2; tick.style.borderBottomWidth = 2; tick.style.borderLeftWidth = 2; tick.style.borderRightWidth = 2;
            row.Add(tick);
            var name = new Label(e.Title);
            name.style.fontSize = 13;
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.flexGrow = 1;
            name.style.flexShrink = 1;
            if (e.IsTown) name.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(name);
            row.tooltip = e.Detail;
            row.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => Select(e.Key, true));
            row.RegisterCallback<MouseEnterEvent>(_ => { if (e.Key != selectedKey) row.style.backgroundColor = RowHover; });
            row.RegisterCallback<MouseLeaveEvent>(_ => { if (e.Key != selectedKey) row.style.backgroundColor = StyleKeyword.Null; });
            rows[e.Key] = row;
            rowTicks[e.Key] = tick;
            listScroll.Add(row);
        }
    }

    private void Select(string key, bool open)
    {
        SaveNote();
        selectedKey = key;
        RefreshAll();
        if (rows.TryGetValue(key, out var row)) listScroll.ScrollTo(row);
        if (open) OpenSelected(true);
    }

    private void Step(int by, bool notReviewed)
    {
        if (entries.Count == 0) return;
        var i = entries.FindIndex(e => e.Key == selectedKey);
        for (var n = 0; n < entries.Count; n++)
        {
            i = (i + by + entries.Count) % entries.Count;
            if (i < 0) i = 0;
            if (!notReviewed || !TowerPlaceReview.Reviewed(entries[i].Key)) { Select(entries[i].Key, true); return; }
        }
        Message("Every place is reviewed.", Good);
    }

    private void OpenSelected(bool frame)
    {
        var e = Selected;
        if (e == null) return;
        if (!TowerPlaceScenes.Open(e.ScenePath, out var scene, out var error))
        {
            Message(error, Bad);
            RefreshAll();
            return;
        }
        var place = TowerPlaceScenes.PlaceIn(scene);
        if (frame && place) TowerPlaceScenes.Frame(place);
        Message(place ? "" : scene.name + " has no TowerFloor or TowerTown at its root.", place ? Dim : Warn);
        RefreshAll();
    }

    private Scene OpenSceneOfSelected() => Selected == null ? default : TowerPlaceScenes.Loaded(Selected.ScenePath);

    private void SaveOpen()
    {
        var scenes = TowerPlaceScenes.OpenTowerScenes();
        if (scenes.Count == 0) { Message("No tower scene is open.", Warn); return; }
        var saved = new List<string>();
        foreach (var s in scenes)
        {
            if (!TowerPlaceScenes.Save(s, out var error)) { Message(error, Bad); return; }
            saved.Add(s.name);
        }
        Message("Saved " + string.Join(", ", saved) + AfterSaveNote(), Good);
        RefreshAll();
    }

    private string AfterSaveNote() => afterSave != null ? afterSave() : "";
    private Func<string> afterSave;

    private void CloseOpen()
    {
        foreach (var s in TowerPlaceScenes.OpenTowerScenes())
        {
            if (s.isDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex("Tower Places", s.name + " has changes that are not saved. Save them?", "Save", "Cancel", "Don't save");
                if (choice == 1) return;
                if (choice == 0 && !TowerPlaceScenes.Save(s, out var error)) { Message(error, Bad); return; }
            }
            TowerPlaceScenes.Close(s);
        }
        Message("Closed.", Dim);
        RefreshAll();
    }

    private void Message(string text, Color color)
    {
        if (messageLabel == null) return;
        messageLabel.text = text ?? "";
        messageLabel.style.color = color;
    }

    private void RefreshAll()
    {
        if (progressLabel == null) return;
        RefreshList();
        var e = Selected;
        panel.style.display = e == null ? DisplayStyle.None : DisplayStyle.Flex;
        emptyPanel.style.display = e == null ? DisplayStyle.Flex : DisplayStyle.None;
        if (e != null)
        {
            titleLabel.text = e.Title;
            detailLabel.text = (e.Group + ". " + e.Detail).Trim();
            var scene = OpenSceneOfSelected();
            var playing = EditorApplication.isPlayingOrWillChangePlaymode;
            if (playing) { stateLabel.text = "Play mode: places open and save in edit mode."; stateLabel.style.color = Warn; }
            else if (!scene.IsValid()) { stateLabel.text = "Not open."; stateLabel.style.color = Dim; }
            else if (scene.isDirty) { stateLabel.text = "Open, with changes that are not saved."; stateLabel.style.color = Warn; }
            else { stateLabel.text = "Open beside the other scenes, saved."; stateLabel.style.color = Good; }
            openButton.text = scene.IsValid() ? "Frame" : "Open and frame";
            openButton.SetEnabled(!playing);
            saveButton.SetEnabled(!playing && TowerPlaceScenes.OpenTowerScenes().Any(s => s.isDirty));
            closeButton.SetEnabled(!playing && TowerPlaceScenes.OpenTowerScenes().Count > 0);
            reviewedToggle.SetValueWithoutNotify(TowerPlaceReview.Reviewed(e.Key));
            if (!noteDirty) noteField.SetValueWithoutNotify(TowerPlaceReview.Note(e.Key));
            markersToggle.SetValueWithoutNotify(TowerPlaceMarkers.Enabled);
        }
        RefreshMore();
    }

    private void RefreshList()
    {
        if (progressLabel == null) return;
        var done = entries.Count(e => TowerPlaceReview.Reviewed(e.Key));
        progressLabel.text = done + " / " + entries.Count + " reviewed";
        foreach (var e in entries)
        {
            if (!rows.TryGetValue(e.Key, out var row)) continue;
            var reviewed = TowerPlaceReview.Reviewed(e.Key);
            var hasNote = TowerPlaceReview.Note(e.Key).Length > 0;
            var tick = rowTicks[e.Key];
            var ring = reviewed ? Good : hasNote ? Warn : Dim;
            tick.style.borderTopColor = ring; tick.style.borderBottomColor = ring; tick.style.borderLeftColor = ring; tick.style.borderRightColor = ring;
            tick.style.backgroundColor = reviewed ? Good : (StyleColor)StyleKeyword.Null;
            row.style.backgroundColor = e.Key == selectedKey ? RowOn : (StyleColor)StyleKeyword.Null;
            row.tooltip = e.Detail + (hasNote ? "\nNote: " + TowerPlaceReview.Note(e.Key) : "");
        }
    }
}
