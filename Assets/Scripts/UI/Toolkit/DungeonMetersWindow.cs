using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Damage done, healing done and damage taken for the current or last dungeon run, ranked per
    /// player, in the style of MMO combat meters. The same window serves the tower: a boss fight
    /// there (mini boss or floor boss), and a part being trained on when the tower panel asks.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="CombatMeter.Dungeon"/>, which the combat code feeds. The window opens on its
    /// own when a run starts, stays up for <see cref="lingerSeconds"/> after it ends so the result can
    /// be read, then closes. Closing it by hand hides it until the next run.
    ///
    /// <para>
    /// One window, one run at a time (<see cref="Choose"/>): what the camera is watching. With the
    /// camera at a place of the tower it is the tower's: a boss fight (<see cref="CombatMeter.Tower"/>)
    /// while the camera is on its part, its result for the linger wherever in the tower the camera
    /// went, or the part's own meter while the tower panel's Meters button has it open. With the
    /// camera anywhere else it is the dungeon's, as before. Never over a tower cutscene, and never
    /// over the tower panel's floor map.
    /// </para>
    ///
    /// <para>
    /// Sorting happens on the refresh interval, not per hit, and the list is virtualised, so a run
    /// with a hundred players costs the same to draw as one with six.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class DungeonMetersWindow : MonoBehaviour
    {
        [Tooltip("How often the ranking is recalculated.")]
        [SerializeField] private float refreshInterval = 0.5f;

        [Tooltip("How long the result stays up after a run ends.")]
        [SerializeField] private float lingerSeconds = 60f;

        [Tooltip("Smallest size the window can be resized to.")]
        [SerializeField] private Vector2 minimumSize = new Vector2(420f, 240f);

        [SerializeField] private DungeonManager dungeon;

        private const float RowHeight = 48f;
        private const string TabKey = "rf_meters_tab";

        private enum Metric { Damage, Healing, Taken }

        // Font Awesome 5 Pro Solid: sword, bow-arrow, magic, plus.
        private static readonly string[] StyleGlyphs = { "", "", "", "" };
        private static readonly string[] StyleClasses = { "rf-meter-row--melee", "rf-meter-row--ranged", "rf-meter-row--magic", "rf-meter-row--healing" };

        private static readonly Color EscapedColor = new Color(0.78f, 0.71f, 1f);

        private sealed class Row
        {
            public VisualElement Root;
            public Label Rank;
            public Label Icon;
            public Label Name;
            /// <summary>The Damage Taken tab's count of blows escaped with Substitution: a wind and the number, after the name.</summary>
            public VisualElement Escaped;
            public Label EscapedCount;
            public VisualElement Fill;
            public Label Total;
            public Label Rate;
            public Label Pct;
            public int StyleIndex = -1;
        }

        private readonly List<CombatMeter.Entry> ranked = new List<CombatMeter.Entry>();

        private VisualElement window;
        private Label title;
        private Label room;
        private VisualElement summary;
        private Label timer;
        private readonly Button[] tabs = new Button[3];
        private Label rateHeader;
        private Label playerColumn;
        private ListView list;
        private Label empty;
        private Label totalCaption;
        private Label total;
        private Label totalRate;
        private VisualElement linger;
        private VisualElement lingerFill;
        private DraggableWindow drag;
        private bool bound;

        private static DungeonMetersWindow instance;

        private Metric metric;
        // the run on show, and the runs closed by hand (by meter: its run id)
        private CombatMeter source;
        private readonly Dictionary<CombatMeter, int> dismissed = new Dictionary<CombatMeter, int>();
        // the tower panel's Meters button: the part the camera is on, while no boss fight has the window
        private bool stretchOpen;
        private Button reset;
        private Button close;
        private bool shown;
        private float nextRefresh;
        private long top;
        private long sum;
        private float duration;

        private void Awake()
        {
            metric = (Metric)Mathf.Clamp(PlayerPrefs.GetInt(TabKey, 0), 0, 2);
        }

        /// <summary>The tower panel's Meters button has the part's own meter open.</summary>
        public static bool StretchOpen => instance && instance.stretchOpen;

        /// <summary>The run on show now is a tower one (a boss fight or a part's own), for the tests.</summary>
        public static CombatMeter Showing => instance && instance.shown ? instance.source : null;

        /// <summary>
        /// The tower panel's Meters button: the meter of the part the camera is on, opened or closed. A boss
        /// fight and its result come first; the part's own shows again after them.
        /// </summary>
        public static void ToggleStretch()
        {
            if (!instance) return;
            instance.stretchOpen = !instance.stretchOpen;
            instance.nextRefresh = 0f;
        }

        // Above the player, island and enemy details (12) and below the top status bar (15). The meters only
        // show during a run and a minute after; dragged under the player details they could not be reached
        // again until they closed.
        private const float SortingOrder = 13f;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc != null && doc.sortingOrder < SortingOrder) doc.sortingOrder = SortingOrder;
            instance = this;
            TryBind();
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
            source = null;
            bound = false;
            if (drag != null)
            {
                drag.Detach();
                drag = null;
            }
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            window = root?.Q<VisualElement>("meters-window");
            if (window == null)
            {
                return;
            }

            title = root.Q<Label>("meters-title");
            if (title != null) title.text = Localization.UI_METER_TITLE;
            playerColumn = root.Q<Label>("meters-col-player");
            if (playerColumn != null) playerColumn.text = Localization.UI_METER_PLAYER;
            var totalColumn = root.Q<Label>("meters-col-total");
            if (totalColumn != null) totalColumn.text = Localization.UI_METER_TOTAL;
            room = root.Q<Label>("meters-room");
            summary = room != null ? room.parent : null;
            timer = root.Q<Label>("meters-timer");
            tabs[0] = root.Q<Button>("meters-tab-damage");
            tabs[1] = root.Q<Button>("meters-tab-healing");
            tabs[2] = root.Q<Button>("meters-tab-taken");
            rateHeader = root.Q<Label>("meters-rate-header");
            list = root.Q<ListView>("meters-list");
            empty = root.Q<Label>("meters-empty");
            totalCaption = root.Q<Label>("meters-total-caption");
            total = root.Q<Label>("meters-total");
            totalRate = root.Q<Label>("meters-total-rate");
            linger = root.Q<VisualElement>("meters-linger");
            lingerFill = root.Q<VisualElement>("meters-linger-fill");

            if (title != null) title.text = Localization.UI_DUNGEON_TITLE;
            if (totalCaption != null) totalCaption.text = Localization.UI_METER_GROUP_TOTAL;
            if (empty != null) empty.text = Localization.UI_METER_EMPTY;

            var names = new[] { Localization.UI_METER_DAMAGE, Localization.UI_METER_HEALING, Localization.UI_METER_TAKEN };
            for (var i = 0; i < tabs.Length; ++i)
            {
                if (tabs[i] == null) continue;
                var index = i;
                tabs[i].text = names[i];
                tabs[i].clicked += () => SelectTab((Metric)index);
            }

            if (list != null)
            {
                list.itemsSource = ranked;
                list.makeItem = MakeRow;
                list.bindItem = BindRow;
                list.fixedItemHeight = RowHeight;
            }

            var header = root.Q<VisualElement>("meters-header");
            if (header != null)
            {
                drag = DraggableWindow.Attach(window, header, "dungeonmeters");
                drag?.EnableResize(root.Q<VisualElement>("meters-grip"), minimumSize);
            }

            var collapse = root.Q<Button>("meters-collapse");
            if (collapse != null) collapse.clicked += () => drag?.ToggleCollapsed();

            close = root.Q<Button>("meters-close");
            if (close != null) close.clicked += () =>
            {
                // a part's own meter is closed as it was opened; a run is hidden until the next one
                if (IsStretch(source)) stretchOpen = false;
                else if (source != null) dismissed[source] = source.RunId;
                source = null;
                SetShown(false);
            };

            // a part's own meter runs for as long as the part is trained on: this counts it from now
            if (collapse != null && collapse.parent != null)
            {
                reset = new Button(() =>
                {
                    var tower = TowerManager.Instance;
                    if (!tower || !IsStretch(source)) return;
                    foreach (var area in tower.Areas) if (area.Meter == source) { tower.ResetStretch(area); break; }
                    nextRefresh = 0f;
                }) { text = "\uf2ea", tooltip = Localization.UI_METER_RESET };
                reset.AddToClassList("rf-window__button");
                reset.AddToClassList("rf-icon");
                reset.AddToClassList("rf-meters__close");
                reset.style.display = DisplayStyle.None;
                collapse.parent.Insert(0, reset);
            }

            ApplyTab();
            bound = true;
            window.style.display = DisplayStyle.None;
            shown = false;
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound)
                {
                    return;
                }
            }

            // never over a tower cutscene, not for the half second until the next refresh either
            if (shown && TowerCutscene.Playing) nextRefresh = 0f;

            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + refreshInterval;

            var meter = Choose();
            if (meter != source)
            {
                source = meter;
                if (meter != null) ApplySource(meter);
            }

            var wasShown = shown;
            SetShown(meter != null);
            if (meter == null)
            {
                return;
            }

            if (!wasShown)
            {
                // filled now, looked at again in a moment when it has its place, shown then
                Refresh(meter);
                nextRefresh = Time.unscaledTime + 0.1f;
                return;
            }

            // no stacked windows: it keeps clear of the other windows, the tower panel and the corner pills
            KeepClear();

            // while the tower panel's floor map lies where this window is (no room beside it), it waits
            // (hidden, not closed: its place is still known when the map goes)
            var covered = TowerHudView.MapShown && window.panel == TowerHudView.PanelOwner && window.worldBound.Overlaps(TowerHudView.PanelBounds);
            window.style.visibility = covered ? Visibility.Hidden : Visibility.Visible;
            if (covered || (drag != null && drag.IsCollapsed))
            {
                return;
            }

            Refresh(meter);
        }

        private static bool IsStretch(CombatMeter meter) => meter != null && meter != CombatMeter.Dungeon && meter != CombatMeter.Tower;

        // ---- keeping clear of the other windows --------------------------------------------------------------

        // the window never lies over another HUD window (the player details, the players list, any other), the tower
        // panel or the pills in the bottom left corner (WindowKeepClear)
        private WindowKeepClear keepClear;

        private void KeepClear() => (keepClear ??= new WindowKeepClear(window, () => drag)).Update();

        /// <summary>A run that is on, or over for less than the linger, and not closed by hand.</summary>
        private bool Live(CombatMeter meter) =>
            meter.RunId > 0
            && (meter.Running || Time.unscaledTime - meter.EndedAt < lingerSeconds)
            && !(dismissed.TryGetValue(meter, out var run) && run == meter.RunId);

        /// <summary>
        /// The run to show now, or null: the one the camera is watching. A dungeon and a tower fight can go
        /// on at the same time (raids and dungeons run beside the tower), and the window is one.
        /// </summary>
        private CombatMeter Choose()
        {
            var tower = TowerManager.Instance;
            if (tower && tower.CameraInTower)
            {
                // never over a cutscene (a guardian's intro, its fall)
                if (TowerCutscene.Playing) return null;
                var area = tower.CameraArea;
                var fight = CombatMeter.Tower;
                // a fight where the camera is; its result wherever in the tower the camera went after it (the town)
                if (Live(fight) && (!fight.Running || (area != null && area == tower.MeterArea))) return fight;
                if (stretchOpen && area != null && area.Meter.RunId > 0) return area.Meter;
                return null;
            }

            return Live(CombatMeter.Dungeon) ? CombatMeter.Dungeon : null;
        }

        /// <summary>The window's title and buttons for the run it now shows.</summary>
        private void ApplySource(CombatMeter meter)
        {
            var stretch = IsStretch(meter);
            if (reset != null) reset.style.display = stretch ? DisplayStyle.Flex : DisplayStyle.None;
            if (close != null) close.tooltip = meter == CombatMeter.Tower ? Localization.UI_METER_HIDE_TOWER : null;
            SetTitle(meter);
        }

        private void SetTitle(CombatMeter meter)
        {
            if (title == null) return;
            var text = string.IsNullOrEmpty(meter.Title) ? Localization.UI_DUNGEON_TITLE : meter.Title;
            if (title.text == text) return;
            title.text = text;
            // a fight's name is longer than "DUNGEON": the window is at least as wide as its title and buttons
            if (string.IsNullOrEmpty(meter.Title))
            {
                window.style.minWidth = StyleKeyword.Null;
                return;
            }
            title.schedule.Execute(() =>
            {
                if (title.text != text) return;
                var wide = title.MeasureTextSize(text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                var actions = close != null && close.parent != null ? close.parent.resolvedStyle.width : 80f;
                if (!float.IsNaN(wide) && wide > 0f) window.style.minWidth = Mathf.Max(minimumSize.x, wide + actions + 56f);
            }).StartingIn(50);
        }

        private void SetShown(bool value)
        {
            if (value == shown)
            {
                return;
            }

            shown = value;
            // back to its own place before it goes: the next time it shows it starts from there
            if (!value) keepClear?.GoHome();
            window.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                // unseen until this refresh has looked at what lies under it (the floor map)
                window.style.visibility = Visibility.Hidden;
                nextRefresh = 0f;
            }
        }

        private void SelectTab(Metric value)
        {
            metric = value;
            PlayerPrefs.SetInt(TabKey, (int)value);
            PlayerPrefs.Save();
            ApplyTab();
            if (shown && source != null)
            {
                Refresh(source);
            }
        }

        private void ApplyTab()
        {
            for (var i = 0; i < tabs.Length; ++i)
            {
                tabs[i]?.EnableInClassList("rf-meters__tab--active", i == (int)metric);
            }

            if (rateHeader != null)
            {
                rateHeader.text = metric == Metric.Damage ? Localization.UI_METER_DPS : metric == Metric.Healing ? Localization.UI_METER_HPS : Localization.UI_METER_DTPS;
            }

            // the Damage Taken tab says what the wind by a name counts (Substitution's escapes)
            if (playerColumn != null) playerColumn.text = metric == Metric.Taken ? Localization.UI_METER_PLAYER_ESCAPED : Localization.UI_METER_PLAYER;
        }

        private void Refresh(CombatMeter meter)
        {
            // a part's own meter is given its title again when the part becomes another
            SetTitle(meter);

            // Summary line: where the run is, or that it is over.
            if (room != null)
            {
                string text;
                if (!meter.Running)
                {
                    var left = Mathf.Max(0, Mathf.CeilToInt(lingerSeconds - (Time.unscaledTime - meter.EndedAt)));
                    // whoever the run points out (the last blow on a floor boss) is named here, where there is room for
                    // it: by the names in the rows it was cut short
                    var marked = string.Empty;
                    if (!string.IsNullOrEmpty(meter.MarkText) && meter.Marked.Count > 0)
                    {
                        foreach (var id in meter.Marked)
                        {
                            var e = meter.Find(id);
                            if (e != null) marked += (marked.Length > 0 ? ", " : string.Empty) + e.Name;
                        }
                        if (marked.Length > 0) marked = "\n<size=17><color=#F2C14E>" + meter.MarkText + ": " + marked + "</color></size>";
                    }
                    // the names on a line of their own under the outcome: beside it two names ran into the timer
                    text = (string.IsNullOrEmpty(meter.Outcome) ? Localization.UI_METER_RESULT : meter.Outcome) + "  <size=15><color=#A79E8A>"
                        + string.Format(CultureInfo.InvariantCulture, Localization.UI_METER_CLOSING, left)
                        + "</color></size>" + marked;
                    if (summary != null) summary.EnableInClassList("rf-meters__summary--two", marked.Length > 0);
                }
                else if (meter != CombatMeter.Dungeon)
                {
                    if (summary != null) summary.EnableInClassList("rf-meters__summary--two", false);
                    // a tower run says what it is itself: "MINI BOSS", "FLOOR BOSS", "TRAINING"
                    text = meter.Caption ?? string.Empty;
                }
                else
                {
                    if (summary != null) summary.EnableInClassList("rf-meters__summary--two", false);
                    if (!dungeon)
                    {
                        var gm = GameManager.Instance;
                        dungeon = gm != null ? gm.Dungeons : null;
                    }

                    var d = dungeon ? dungeon.Dungeon : null;
                    if (d == null || d.Rooms == null || d.Rooms.Length == 0)
                    {
                        text = Localization.UI_DUNGEON_TITLE;
                    }
                    else if (d.Room != null && d.Room == d.BossRoom)
                    {
                        text = Localization.UI_DUNGEON_BOSS_ROOM;
                    }
                    else
                    {
                        text = string.Format(CultureInfo.InvariantCulture, Localization.UI_DUNGEON_ROOM, d.CurrentRoomIndex + 1, d.Rooms.Length);
                    }
                }

                if (room.text != text) room.text = text;
            }

            // The line under the header drains over the minute the result stays up.
            if (linger != null)
            {
                linger.style.display = meter.Running ? DisplayStyle.None : DisplayStyle.Flex;
                if (!meter.Running && lingerFill != null)
                {
                    var fraction = Mathf.Clamp01(1f - (Time.unscaledTime - meter.EndedAt) / lingerSeconds);
                    lingerFill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
                }
            }

            duration = meter.Duration;
            if (timer != null)
            {
                var clock = HudFormat.Clock((int)duration);
                if (timer.text != clock) timer.text = clock;
            }

            // Rank everyone with something to show for the selected metric.
            ranked.Clear();
            var entries = meter.Entries;
            for (var i = 0; i < entries.Count; ++i)
            {
                if (Value(entries[i]) > 0)
                {
                    ranked.Add(entries[i]);
                }
            }

            ranked.Sort((a, b) => Value(b).CompareTo(Value(a)));

            top = ranked.Count > 0 ? Value(ranked[0]) : 0;
            sum = metric == Metric.Damage ? meter.TotalDamage : metric == Metric.Healing ? meter.TotalHealing : meter.TotalTaken;

            if (empty != null)
            {
                empty.style.display = ranked.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                // the empty list gives its room to the note, so it sits in the middle, not on the footer
                if (list != null) list.style.display = ranked.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }

            list?.RefreshItems();

            if (total != null) total.text = HudFormat.Compact(sum);
            if (totalRate != null) totalRate.text = HudFormat.Compact(PerSecond(sum)) + " " + rateHeader?.text;
        }

        private long Value(CombatMeter.Entry e)
        {
            return metric == Metric.Damage ? e.Damage : metric == Metric.Healing ? e.Healing : e.Taken;
        }

        private double PerSecond(long value)
        {
            return duration > 1f ? value / (double)duration : value;
        }

        private VisualElement MakeRow()
        {
            var row = new Row();

            row.Root = new VisualElement();
            row.Root.AddToClassList("rf-meter-row");

            row.Rank = new Label();
            row.Rank.AddToClassList("rf-meter-row__rank");

            row.Icon = new Label();
            row.Icon.AddToClassList("rf-icon");
            row.Icon.AddToClassList("rf-meter-row__icon");

            var main = new VisualElement();
            main.AddToClassList("rf-meter-row__main");

            row.Name = new Label();
            row.Name.AddToClassList("rf-meter-row__name");
            row.Name.style.flexShrink = 1;
            row.Name.style.minWidth = 0;

            // the name and, on the Damage Taken tab, the blows escaped with Substitution (a wind and the number); the
            // number never gives way to a long name, the name shortens first
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;
            row.Escaped = new VisualElement();
            row.Escaped.style.flexDirection = FlexDirection.Row;
            row.Escaped.style.alignItems = Align.Center;
            row.Escaped.style.flexShrink = 0;
            row.Escaped.style.marginLeft = 8;
            row.Escaped.style.display = DisplayStyle.None;
            var wind = new Label("\uf72e");
            wind.AddToClassList("rf-icon");
            wind.style.fontSize = 15;
            wind.style.color = EscapedColor;
            wind.style.marginRight = 3;
            row.EscapedCount = new Label();
            row.EscapedCount.AddToClassList("rf-meter-row__name");
            row.EscapedCount.style.fontSize = 18;
            row.EscapedCount.style.color = EscapedColor;
            row.Escaped.Add(wind);
            row.Escaped.Add(row.EscapedCount);
            line.Add(row.Name);
            line.Add(row.Escaped);

            var bar = new VisualElement();
            bar.AddToClassList("rf-meter-row__bar");
            row.Fill = new VisualElement();
            row.Fill.AddToClassList("rf-meter-row__fill");
            bar.Add(row.Fill);

            main.Add(line);
            main.Add(bar);

            row.Total = new Label();
            row.Total.AddToClassList("rf-meter-row__total");

            row.Rate = new Label();
            row.Rate.AddToClassList("rf-meter-row__rate");

            row.Pct = new Label();
            row.Pct.AddToClassList("rf-meter-row__pct");

            row.Root.Add(row.Rank);
            row.Root.Add(row.Icon);
            row.Root.Add(main);
            row.Root.Add(row.Total);
            row.Root.Add(row.Rate);
            row.Root.Add(row.Pct);

            row.Root.userData = row;
            return row.Root;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (!(element.userData is Row row) || index < 0 || index >= ranked.Count)
            {
                return;
            }

            var e = ranked[index];
            var value = Value(e);

            row.Rank.text = (index + 1).ToString(CultureInfo.InvariantCulture);
            row.Root.EnableInClassList("rf-meter-row--first", index == 0);

            var style = (int)e.Style;
            if (style != row.StyleIndex)
            {
                if (row.StyleIndex >= 0) row.Root.RemoveFromClassList(StyleClasses[row.StyleIndex]);
                row.Root.AddToClassList(StyleClasses[style]);
                row.Icon.text = StyleGlyphs[style];
                row.StyleIndex = style;
            }

            if (row.Name.text != e.Name)
            {
                row.Name.text = e.Name;
            }

            // the Damage Taken tab also counts the boss area attacks a player escaped with Substitution
            var escaped = metric == Metric.Taken && e.Escaped > 0;
            row.Escaped.style.display = escaped ? DisplayStyle.Flex : DisplayStyle.None;
            if (escaped) row.EscapedCount.text = e.Escaped.ToString(CultureInfo.InvariantCulture);

            // whoever the run points out (the last blow on a floor boss) stands in gold; the summary line names them
            var pointedOut = source != null && !string.IsNullOrEmpty(source.MarkText) && source.Marked.Contains(e.PlayerId);
            row.Name.style.color = pointedOut ? new Color(0.95f, 0.76f, 0.31f) : HudFormat.ReadableNameColor(e.ColorHex);

            var fraction = top > 0 ? value / (float)top : 0f;
            row.Fill.style.width = new Length(fraction * 100f, LengthUnit.Percent);

            row.Total.text = HudFormat.Compact(value);
            row.Rate.text = HudFormat.Compact(PerSecond(value));
            row.Pct.text = sum > 0 ? HudFormat.Percent(value / (float)sum) : string.Empty;
        }
    }
}
