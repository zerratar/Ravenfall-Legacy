using System.Collections.Generic;
using System.Globalization;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The tower panel in the top centre status column, under the raid and dungeon panels and one of their family: the
    /// floor the stream is on, its quests with their progress, the boss countdown and fight (time left, the boss's
    /// health), the town between floors, how many are inside and what to type next.
    /// </summary>
    /// <remarks>
    /// Before this, nothing on screen showed the climb: viewers only learned the floor, the quests or a countdown by
    /// typing !tower info (docs/tower-design.md, section 2: "The quest tracker shows them").
    ///
    /// <para>
    /// The panel asks <see cref="TowerManager"/> four times a second rather than being pushed to, and rewrites a label
    /// only when its text changes. Its elements are made in code and added to the status column (TopStatus.uxml's
    /// top-column), so the column stacks it with whatever else is showing; when the document builds a new tree (a
    /// change of language) the panel is made again.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class TowerHudView : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;
        private const int MaxQuests = 4;

        private VisualElement hud;
        private Label title, floorLabel, timer, clock, barText, people, phase, join;
        private VisualElement liveDot, bar, fill, quests, joinPill;
        private readonly List<(VisualElement row, Label text, Label count)> questRows = new List<(VisualElement, Label, Label)>();
        private float nextRefresh;
        private bool shown;
        private float shownFill = -1f;

        /// <summary>Adds the panel to the status column's document (the one the raid panel is on), once.</summary>
        public static bool Ensure()
        {
            var raid = FindAnyObjectByType<RaidHudView>();
            if (!raid) return false;
            if (!raid.GetComponent<TowerHudView>()) raid.gameObject.AddComponent<TowerHudView>();
            return true;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            if (hud == null || hud.panel == null)
            {
                if (!Build()) return;
            }
            Refresh();
        }

        private bool Build()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            var column = root != null ? root.Q<VisualElement>("top-column") : null;
            if (column == null) return false;

            hud = Box("rf-raid", "rf-raid--tower");
            hud.name = "tower-hud";
            hud.style.display = DisplayStyle.None;
            shown = false;
            shownFill = -1f;

            var top = Box("rf-raid__top");
            title = Text("rf-raid__title");
            floorLabel = Text("rf-raid__level");
            liveDot = Box("rf-live-dot");
            clock = Text("rf-icon", "rf-raid__clock");
            clock.text = "";
            timer = Text("rf-raid__timer");
            top.Add(title);
            top.Add(floorLabel);
            top.Add(Box("rf-raid__spacer"));
            top.Add(liveDot);
            top.Add(clock);
            top.Add(timer);
            hud.Add(top);

            bar = Box("rf-raid__bar");
            fill = Box("rf-raid__fill");
            barText = Text("rf-raid__hp");
            bar.Add(fill);
            bar.Add(barText);
            hud.Add(bar);

            quests = Box("rf-tower__quests");
            questRows.Clear();
            for (var i = 0; i < MaxQuests; i++)
            {
                var row = Box("rf-tower__quest");
                var check = Text("rf-icon", "rf-tower__check");
                check.text = "";
                var text = Text("rf-raid__raiders", "rf-tower__quest-text");
                var count = Text("rf-raid__level", "rf-tower__quest-count");
                row.Add(check);
                row.Add(text);
                row.Add(count);
                quests.Add(row);
                questRows.Add((row, text, count));
            }
            hud.Add(quests);

            var bottom = Box("rf-raid__bottom");
            var left = Box("rf-raid__side");
            var peopleIcon = Text("rf-icon", "rf-raid__people");
            peopleIcon.text = "";
            people = Text("rf-raid__raiders");
            left.Add(peopleIcon);
            left.Add(people);
            phase = Text("rf-raid__percent");
            var right = Box("rf-raid__side", "rf-raid__side--end");
            joinPill = Box("rf-raid__join-pill");
            var chat = Text("rf-icon", "rf-raid__chat");
            chat.text = "";
            join = Text("rf-raid__join");
            joinPill.Add(chat);
            joinPill.Add(join);
            right.Add(joinPill);
            bottom.Add(left);
            bottom.Add(phase);
            bottom.Add(right);
            hud.Add(bottom);

            column.Add(hud);
            return true;
        }

        private static VisualElement Box(params string[] classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) e.AddToClassList(c);
            return e;
        }

        private static Label Text(params string[] classes)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        private static void Set(Label label, string text)
        {
            if (label != null && label.text != text) label.text = text;
        }

        private static void Show(VisualElement e, bool show)
        {
            if (e != null) e.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetFill(float share)
        {
            share = Mathf.Clamp01(share);
            if (Mathf.Abs(share - shownFill) < 0.002f) return;
            shownFill = share;
            fill.style.width = new Length(share * 100f, LengthUnit.Percent);
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60).ToString(CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private void Refresh()
        {
            var tower = TowerManager.Instance;
            var onFloor = tower ? tower.Players().Count : 0;
            var inTown = tower ? tower.TownPlayers().Count : 0;
            var active = tower && tower.State != null && (onFloor + inTown > 0 || tower.Phase == TowerManager.TowerPhase.Countdown || tower.Phase == TowerManager.TowerPhase.Fight);
            if (active != shown)
            {
                shown = active;
                Show(hud, active);
            }
            if (!active) return;

            var phaseNow = tower.Phase;
            var fighting = phaseNow == TowerManager.TowerPhase.Fight;
            var countdown = phaseNow == TowerManager.TowerPhase.Countdown;
            var stream = Tower.Get(tower.StreamFloor);
            var here = tower.Definition ?? stream;
            // everyone between floors: the town, and the floor that waits
            var resting = onFloor == 0 && inTown > 0 && !fighting && !countdown;
            var training = !resting && tower.Training;

            // a vote that is open: what it is for and when it passes, and the command that adds a voice
            string voteText = null, voteCommand = null;
            if (!fighting && !countdown && tower.TryGetOpenVote(out var voteKey, out var voteSeconds, out var voteMissing, out _))
            {
                if (voteKey == "boss") { voteText = string.Format(Localization.UI_TOWER_VOTE_BOSS, voteSeconds); voteCommand = "!tower boss"; }
                else if (voteKey == "climb") { voteText = string.Format(Localization.UI_TOWER_VOTE_CLIMB, voteSeconds); voteCommand = "!tower climb"; }
                else if (voteKey == "close") { voteText = string.Format(Localization.UI_TOWER_VOTE_CLOSE, voteSeconds, voteMissing); voteCommand = "!tower close"; }
                else if (voteKey.StartsWith("floor ")) { voteText = string.Format(Localization.UI_TOWER_VOTE_FLOOR, voteSeconds, voteKey.Substring(6)); voteCommand = "!tower " + voteKey; }
            }

            Set(title, Localization.UI_TOWER_TITLE);
            Set(people, (onFloor + inTown).ToString(CultureInfo.InvariantCulture));
            hud.EnableInClassList("rf-raid--tower-fight", fighting);

            if (resting)
            {
                var town = tower.CurrentTown;
                Set(floorLabel, town != null ? town.Name : "");
                Show(bar, false);
                Show(quests, false);
                Show(liveDot, false);
                Show(clock, false);
                Set(timer, "");
                if (voteText != null)
                {
                    Set(phase, voteText);
                    Show(joinPill, true);
                    Set(join, voteCommand);
                }
                else if (tower.AllCleared || stream == null)
                {
                    Set(phase, Localization.UI_TOWER_ALL_CLEARED);
                    Show(joinPill, false);
                }
                else
                {
                    Set(phase, string.Format(Localization.UI_TOWER_NEXT_FLOOR, stream.Number, stream.Name));
                    Show(joinPill, true);
                    Set(join, "!tower climb");
                }
                return;
            }

            Set(floorLabel, here != null ? string.Format(Localization.UI_TOWER_FLOOR, here.Number, here.Name) : "");
            Show(liveDot, fighting || countdown);
            Show(clock, fighting || countdown);
            Set(timer, fighting || countdown ? Clock(tower.PhaseSecondsLeft) : "");

            if (fighting)
            {
                // the boss's health in the bar, as in a raid
                var boss = TowerFloor.Loaded ? TowerFloor.Loaded.Boss : null;
                var max = boss && boss.Stats != null ? boss.Stats.Health.MaxValue : 0;
                var hp = boss && boss.Stats != null ? Mathf.Max(0, boss.Stats.Health.CurrentValue) : 0;
                Show(bar, max > 0);
                Show(quests, false);
                SetFill(max > 0 ? hp / (float)max : 0f);
                Set(barText, HudFormat.Grouped(hp) + " / " + HudFormat.Grouped(max));
                Set(phase, here != null && here.Boss != null ? here.Boss.Name : "");
                Show(joinPill, false);
                return;
            }

            if (training)
            {
                Show(bar, false);
                Show(quests, false);
                Set(phase, voteText ?? string.Format(Localization.UI_TOWER_TRAINING, tower.StreamFloor));
                Show(joinPill, true);
                Set(join, voteCommand ?? "!tower floor");
                return;
            }

            // the floor's quests: one row each, and all of them together in the bar
            var done = 0L;
            var total = 0L;
            var rows = 0;
            if (stream != null && !tower.AllCleared)
            {
                foreach (var q in stream.Quests)
                {
                    var count = 0;
                    if (tower.State.Quests != null)
                        foreach (var p in tower.State.Quests)
                            if (p.Id == q.Id) { count = p.Count; break; }
                    count = Mathf.Clamp(count, 0, q.Amount);
                    done += count;
                    total += q.Amount;
                    if (rows >= questRows.Count) continue;
                    var row = questRows[rows++];
                    Show(row.row, true);
                    Set(row.text, q.Text);
                    Set(row.count, count.ToString(CultureInfo.InvariantCulture) + " / " + q.Amount.ToString(CultureInfo.InvariantCulture));
                    row.row.EnableInClassList("rf-tower__quest--done", count >= q.Amount);
                }
            }
            for (var i = rows; i < questRows.Count; i++) Show(questRows[i].row, false);
            // the rows while there is something left to do; all done, the bar and the boss line say it
            Show(quests, rows > 0 && done < total);
            Show(bar, total > 0);
            SetFill(total > 0 ? done / (float)total : 0f);
            Set(barText, string.Format(Localization.UI_TOWER_QUESTS, done, total));

            var bossName = here != null && here.Boss != null ? here.Boss.Name : "";
            if (countdown)
            {
                Set(phase, string.Format(Localization.UI_TOWER_BOSS_STARTS, bossName));
                Show(joinPill, true);
                Set(join, "!tower");
            }
            else if (voteText != null)
            {
                Set(phase, voteText);
                Show(joinPill, true);
                Set(join, voteCommand);
            }
            else if (tower.AllCleared)
            {
                Set(phase, Localization.UI_TOWER_ALL_CLEARED);
                Show(joinPill, false);
            }
            else if (tower.State.BossOpen)
            {
                var wait = tower.RetrySecondsLeft;
                Set(phase, wait > 0f ? string.Format(Localization.UI_TOWER_REGROUP, Mathf.CeilToInt(wait)) : string.Format(Localization.UI_TOWER_BOSS_READY, bossName));
                Show(joinPill, true);
                Set(join, "!tower boss");
            }
            else
            {
                Set(phase, "");
                Show(joinPill, true);
                Set(join, "!tower");
            }
        }
    }
}
