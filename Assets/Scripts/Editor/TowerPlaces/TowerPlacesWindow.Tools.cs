using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Ravenfall > Tower Places: Check (the tower audit), Rebake walking and Play this part.</summary>
public sealed partial class TowerPlacesWindow
{
    private const string PrefQuiet = "Ravenfall.TowerPlaces.QuietChat";

    private Button checkButton, rebakeButton, playButton;
    private Label checkSummary, playStatus;
    private VisualElement checkList;
    private Toggle quietToggle, onlyProblemsToggle;
    private TowerAudit.Result lastCheck;
    private string lastCheckScene;

    partial void BuildEdits(VisualElement panel);
    partial void RefreshEdits();
    partial void EnableEdits();
    partial void DisableEdits();

    partial void OnEnableMore() => EnableEdits();
    partial void OnDisableMore() => DisableEdits();

    partial void BuildMore(VisualElement panel)
    {
        panel.Add(Header("Check, walking and play"));
        var row = Row();
        checkButton = Btn("Check", RunCheck, "The tower audit: can everything be walked to from where players arrive?");
        checkButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(checkButton);
        rebakeButton = Btn("Rebake walking", Rebake, "Bake the place's navmesh again (as its builder does), then Check");
        row.Add(rebakeButton);
        playButton = Btn("Play this part", Play, "Enter play mode and send a test bot straight here");
        row.Add(playButton);
        panel.Add(row);
        quietToggle = new Toggle("Quiet chat while playing") { value = EditorPrefs.GetBool(PrefQuiet, true) };
        quietToggle.labelElement.style.minWidth = 180;
        quietToggle.tooltip = "Mutes chat for that play session and stops the chat bot once the game has signed in, so nothing of the test reaches a real chat";
        quietToggle.RegisterValueChangedCallback(e => EditorPrefs.SetBool(PrefQuiet, e.newValue));
        panel.Add(quietToggle);
        playStatus = Text("", Dim);
        panel.Add(playStatus);

        checkSummary = Text("", Dim);
        checkSummary.style.marginTop = 6;
        checkSummary.style.unityFontStyleAndWeight = FontStyle.Bold;
        panel.Add(checkSummary);
        onlyProblemsToggle = new Toggle("Only problems") { value = true };
        onlyProblemsToggle.labelElement.style.minWidth = 110;
        onlyProblemsToggle.RegisterValueChangedCallback(_ => ShowCheck());
        panel.Add(onlyProblemsToggle);
        checkList = new VisualElement();
        panel.Add(checkList);

        BuildEdits(panel);

        // the play status changes while the game runs
        rootVisualElement.schedule.Execute(() => { if (playStatus != null) playStatus.text = TowerPlayPart.Status; }).Every(500);
    }

    partial void RefreshMore()
    {
        if (checkButton == null) return;
        var playing = EditorApplication.isPlayingOrWillChangePlaymode;
        var open = OpenSceneOfSelected().IsValid();
        checkButton.SetEnabled(!playing);
        rebakeButton.SetEnabled(!playing && open);
        playButton.SetEnabled(!playing && Selected != null);
        playButton.text = Selected != null && Selected.IsTown ? "Play this town" : Selected != null && !Selected.IsPart ? "Play this place" : "Play this part";
        playStatus.text = TowerPlayPart.Status;
        if (Selected == null || lastCheckScene != Selected.ScenePath) { lastCheck = null; ShowCheck(); }
        RefreshEdits();
    }

    private void RunCheck()
    {
        var e = Selected;
        if (e == null) return;
        var scene = OpenSceneOfSelected();
        if (!scene.IsValid())
        {
            OpenSelected(false);
            scene = OpenSceneOfSelected();
            if (!scene.IsValid()) return;
        }
        lastCheck = TowerAudit.Run(scene);
        lastCheckScene = e.ScenePath;
        ShowCheck();
        Message("Checked " + scene.name + ".", Dim);
    }

    private void ShowCheck()
    {
        if (checkList == null) return;
        checkList.Clear();
        onlyProblemsToggle.style.display = lastCheck == null ? DisplayStyle.None : DisplayStyle.Flex;
        if (lastCheck == null)
        {
            checkSummary.text = "";
            return;
        }
        checkSummary.text = lastCheck.Problems == 0
            ? "Check: " + lastCheck.Checked + " spots, no problems."
            : "Check: " + lastCheck.Checked + " spots, " + lastCheck.Problems + " problem" + (lastCheck.Problems == 1 ? "" : "s") + ". Click one to look at it.";
        checkSummary.style.color = lastCheck.Problems == 0 ? Good : Bad;
        var shown = 0;
        foreach (var line in lastCheck.Lines)
        {
            if (onlyProblemsToggle.value && !line.Problem) continue;
            if (++shown > 400) { checkList.Add(Text("... and more (the full report: tools/tower/tower-audit.cs)", Dim, 12)); break; }
            var l = Text(line.Text.Trim(), line.Problem ? Bad : (Color?)null, 12);
            l.style.paddingLeft = 4; l.style.paddingTop = 1; l.style.paddingBottom = 1;
            if (line.At.HasValue)
            {
                var at = line.At.Value;
                l.tooltip = "Click to look at it";
                l.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => TowerPlaceScenes.FrameAt(at));
                l.RegisterCallback<MouseEnterEvent>(_ => l.style.backgroundColor = RowHover);
                l.RegisterCallback<MouseLeaveEvent>(_ => l.style.backgroundColor = StyleKeyword.Null);
            }
            checkList.Add(l);
        }
        if (shown == 0) checkList.Add(Text("Nothing to show: untick Only problems for every line.", Dim, 12));
    }

    private void Rebake()
    {
        var scene = OpenSceneOfSelected();
        if (!scene.IsValid()) return;
        var log = new StringBuilder();
        bool ok;
        try
        {
            EditorUtility.DisplayProgressBar("Tower Places", "Baking the walking ground of " + scene.name + "...", 0.5f);
            ok = TowerPlaceNav.Rebake(scene, log);
        }
        finally { EditorUtility.ClearProgressBar(); }
        RunCheck();
        Message((ok ? "Rebaked. " : "The bake did not finish. ") + log.ToString().Trim().Replace("\n", " ") + " The navmesh asset is in a gitignored folder: commit it with git add -f.", ok ? Good : Bad);
    }

    private void Play()
    {
        var e = Selected;
        if (e == null) return;
        if (!TowerPlayPart.Request(e.Key, quietToggle.value, out var error)) Message(error, Bad);
        else Message("", Dim);
        RefreshAll();
    }
}
