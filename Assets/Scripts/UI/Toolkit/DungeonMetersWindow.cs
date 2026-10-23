using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Damage done, healing done and damage taken for the current or last dungeon run, ranked per
    /// player, in the style of MMO combat meters.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="CombatMeter.Dungeon"/>, which the combat code feeds. The window opens on its
    /// own when a run starts, stays up for <see cref="lingerSeconds"/> after it ends so the result can
    /// be read, then closes. Closing it by hand hides it until the next run.
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

        private sealed class Row
        {
            public VisualElement Root;
            public Label Rank;
            public Label Icon;
            public Label Name;
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
        private Label timer;
        private readonly Button[] tabs = new Button[3];
        private Label rateHeader;
        private ListView list;
        private Label empty;
        private Label totalCaption;
        private Label total;
        private Label totalRate;
        private VisualElement linger;
        private VisualElement lingerFill;
        private DraggableWindow drag;
        private bool bound;

        private Metric metric;
        private int dismissedRun = -1;
        private bool shown;
        private float nextRefresh;
        private long top;
        private long sum;
        private float duration;

        private void Awake()
        {
            metric = (Metric)Mathf.Clamp(PlayerPrefs.GetInt(TabKey, 0), 0, 2);
        }

        // Above the player, island and enemy details (12) and below the top status bar (15). The meters only
        // show during a run and a minute after; dragged under the player details they could not be reached
        // again until they closed.
        private const float SortingOrder = 13f;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc != null && doc.sortingOrder < SortingOrder) doc.sortingOrder = SortingOrder;
            TryBind();
        }

        private void OnDisable()
        {
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
            var playerColumn = root.Q<Label>("meters-col-player");
            if (playerColumn != null) playerColumn.text = Localization.UI_METER_PLAYER;
            var totalColumn = root.Q<Label>("meters-col-total");
            if (totalColumn != null) totalColumn.text = Localization.UI_METER_TOTAL;
            room = root.Q<Label>("meters-room");
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

            var close = root.Q<Button>("meters-close");
            if (close != null) close.clicked += () =>
            {
                dismissedRun = CombatMeter.Dungeon.RunId;
                SetShown(false);
            };

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

            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + refreshInterval;

            var meter = CombatMeter.Dungeon;
            var want = meter.RunId > 0
                && meter.RunId != dismissedRun
                && (meter.Running || Time.unscaledTime - meter.EndedAt < lingerSeconds);

            SetShown(want);
            if (!want || (drag != null && drag.IsCollapsed))
            {
                return;
            }

            Refresh(meter);
        }

        private void SetShown(bool value)
        {
            if (value == shown)
            {
                return;
            }

            shown = value;
            window.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                nextRefresh = 0f;
            }
        }

        private void SelectTab(Metric value)
        {
            metric = value;
            PlayerPrefs.SetInt(TabKey, (int)value);
            PlayerPrefs.Save();
            ApplyTab();
            if (shown)
            {
                Refresh(CombatMeter.Dungeon);
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
        }

        private void Refresh(CombatMeter meter)
        {
            // Summary line: where the run is, or that it is over.
            if (room != null)
            {
                string text;
                if (!meter.Running)
                {
                    var left = Mathf.Max(0, Mathf.CeilToInt(lingerSeconds - (Time.unscaledTime - meter.EndedAt)));
                    text = Localization.UI_METER_RESULT + "  <size=15><color=#A79E8A>"
                        + string.Format(CultureInfo.InvariantCulture, Localization.UI_METER_CLOSING, left)
                        + "</color></size>";
                }
                else
                {
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

            var bar = new VisualElement();
            bar.AddToClassList("rf-meter-row__bar");
            row.Fill = new VisualElement();
            row.Fill.AddToClassList("rf-meter-row__fill");
            bar.Add(row.Fill);

            main.Add(row.Name);
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

            row.Name.style.color = HudFormat.ReadableNameColor(e.ColorHex);

            var fraction = top > 0 ? value / (float)top : 0f;
            row.Fill.style.width = new Length(fraction * 100f, LengthUnit.Percent);

            row.Total.text = HudFormat.Compact(value);
            row.Rate.text = HudFormat.Compact(PerSecond(value));
            row.Pct.text = sum > 0 ? HudFormat.Percent(value / (float)sum) : string.Empty;
        }
    }
}
