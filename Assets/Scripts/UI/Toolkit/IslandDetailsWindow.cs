using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Skill = RavenNest.Models.Skill;

namespace Shinobytes.UI
{
    /// <summary>
    /// The island the camera is touring: name, level requirement, onsen, players, what they are
    /// training there, and the island's statistics.
    /// </summary>
    /// <remarks>
    /// <c>IslandDetails</c> stays the entry point and forwards here when assigned. The old panel
    /// rebuilt every label every frame, including a walk over every player on the island; this one
    /// refreshes twice a second and only writes what changed.
    ///
    /// <para>
    /// Training is a ranked bar chart rather than fourteen fixed counters, most of them zero: the
    /// question a viewer asks is "what are people doing here", and a ranking answers it at a glance.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class IslandDetailsWindow : MonoBehaviour
    {
        [Serializable]
        public struct SkillIcon
        {
            public Skill Skill;
            public Sprite Icon;
        }

        [SerializeField] private float refreshInterval = 0.5f;
        [SerializeField] private SkillIcon[] skillIcons = Array.Empty<SkillIcon>();

        /// <summary>The skills shown in the chart, in the order ties are broken.</summary>
        private static readonly Skill[] ChartSkills =
        {
            Skill.Attack, Skill.Defense, Skill.Strength, Skill.Magic, Skill.Ranged, Skill.Healing,
            Skill.Woodcutting, Skill.Fishing, Skill.Mining, Skill.Crafting, Skill.Cooking,
            Skill.Farming, Skill.Gathering, Skill.Alchemy,
        };

        private sealed class SkillRow
        {
            public Skill Skill;
            public VisualElement Root;
            public VisualElement Fill;
            public Label Count;
            public int Value;
            public int Shown = -1;
            public float ShownFill = -1f;
        }

        private sealed class StatRow
        {
            public Func<IslandStatistics, long> Read;
            public Label Value;
            public long Shown = long.MinValue;
        }

        private readonly List<SkillRow> skillRows = new List<SkillRow>();
        private readonly List<SkillRow> sorted = new List<SkillRow>();
        private readonly List<StatRow> statRows = new List<StatRow>();
        private readonly Dictionary<Skill, SkillRow> rowBySkill = new Dictionary<Skill, SkillRow>();

        private VisualElement window;
        private Label nameLabel;
        private Label levelChip;
        private Label onsenChip;
        private Label players;
        private VisualElement timerFill;
        private VisualElement training;
        private Label trainingEmpty;
        private DraggableWindow drag;
        private bool bound;

        private IslandController island;
        private float timeLeft;
        private float timeTotal;
        private float nextRefresh;
        private string shownOrder;

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            bound = false;
            drag?.Detach();
            drag = null;
        }

        /// <summary>
        /// Forgets what was last drawn, so a freshly built tree gets every value written. Called on
        /// each bind: after a change of language or a live reload the tree is new, and a cache that
        /// still matched the old one left labels empty.
        /// </summary>
        private void ResetShown()
        {
            shownOrder = null;
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            window = root?.Q<VisualElement>("island-window");
            if (window == null)
            {
                return;
            }

            nameLabel = root.Q<Label>("island-name");
            levelChip = root.Q<Label>("island-level");
            onsenChip = root.Q<Label>("island-onsen");
            players = root.Q<Label>("island-players");
            timerFill = root.Q<VisualElement>("island-timer-fill");
            training = root.Q<VisualElement>("island-training");
            trainingEmpty = root.Q<Label>("island-training-empty");

            SetText(root.Q<Label>("island-training-title"), Localization.UI_ISLAND_TRAINING);
            SetText(root.Q<Label>("island-stats-title"), Localization.UI_ISLAND_STATISTICS);
            SetText(trainingEmpty, Localization.UI_ISLAND_NOBODY);
            SetText(onsenChip, Localization.UI_ISLAND_ONSEN);

            BuildTraining();
            BuildStats(root.Q<VisualElement>("island-stats"));

            var header = root.Q<VisualElement>("island-header");
            if (header != null)
            {
                drag = DraggableWindow.Attach(window, header, "islanddetails");
                // wider only: the height follows what is shown
                drag?.EnableResize(root.Q<VisualElement>("island-grip"), new Vector2(380f, 0f), widthOnly: true);
                drag?.EnableLeftResize(root.Q<VisualElement>("island-grip-left"));
            }

            var collapse = root.Q<Button>("island-collapse");
            if (collapse != null) collapse.clicked += () => drag?.ToggleCollapsed();

            window.style.display = DisplayStyle.None;
            ResetShown();
            bound = true;
            ApplyVisibility();
        }

        // ---- Called by IslandDetails ---------------------------------------------------------

        public void Observe(IslandController target, float seconds)
        {
            island = target;
            timeLeft = seconds;
            timeTotal = seconds;
            shownOrder = null;
            for (var i = 0; i < skillRows.Count; ++i) { skillRows[i].Shown = -1; skillRows[i].ShownFill = -1f; }
            for (var i = 0; i < statRows.Count; ++i) statRows[i].Shown = long.MinValue;

            if (island)
            {
                island.RebuildPlayerList();
            }

            ApplyVisibility();
            nextRefresh = 0f;
        }

        private void ApplyVisibility()
        {
            if (bound)
            {
                window.style.display = island ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ---- Building ------------------------------------------------------------------------

        private void BuildTraining()
        {
            training.Clear();
            skillRows.Clear();
            rowBySkill.Clear();

            foreach (var skill in ChartSkills)
            {
                var row = new SkillRow { Skill = skill };
                row.Root = new VisualElement { pickingMode = PickingMode.Ignore };
                row.Root.AddToClassList("rf-island__skill");

                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-island__skill-icon");
                var sprite = IconFor(skill);
                if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);

                var name = new Label(skill.ToString()) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-island__skill-name");

                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("rf-island__skill-bar");
                row.Fill = new VisualElement { pickingMode = PickingMode.Ignore };
                row.Fill.AddToClassList("rf-island__skill-fill");
                bar.Add(row.Fill);

                row.Count = new Label { pickingMode = PickingMode.Ignore };
                row.Count.AddToClassList("rf-island__skill-count");

                row.Root.Add(icon);
                row.Root.Add(name);
                row.Root.Add(bar);
                row.Root.Add(row.Count);
                row.Root.style.display = DisplayStyle.None;

                training.Add(row.Root);
                skillRows.Add(row);
                rowBySkill[skill] = row;
            }
        }

        private void BuildStats(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            statRows.Clear();

            // Font Awesome 5 Pro Solid glyphs.
            AddStat(container, "", Localization.UI_ISLAND_STAT_MONSTERS, s => s.MonstersDefeated);
            AddStat(container, "", Localization.UI_ISLAND_STAT_PLAYERS_KILLED, s => s.PlayersKilled);
            AddStat(container, "", Localization.UI_ISLAND_STAT_RAID_BOSSES, s => s.RaidBossesSpawned);
            AddStat(container, "", Localization.UI_ISLAND_STAT_ITEMS_GATHERED, s => s.ItemsGathered);
            AddStat(container, "", Localization.UI_ISLAND_STAT_TREES, s => s.TreesCutDown);
            AddStat(container, "", Localization.UI_ISLAND_STAT_ROCKS, s => s.RocksMined);
            AddStat(container, "", Localization.UI_ISLAND_STAT_FISH, s => s.FishCaught);
            AddStat(container, "", Localization.UI_ISLAND_STAT_CROPS, s => s.CropsHarvested);
            AddStat(container, "", Localization.UI_ISLAND_STAT_FOOD, s => s.FoodCooked);
            AddStat(container, "", Localization.UI_ISLAND_STAT_CRAFTED, s => s.ItemsCrafted);
            AddStat(container, "", Localization.UI_ISLAND_STAT_POTIONS, s => s.PotionsBrewed);
        }

        private void AddStat(VisualElement container, string glyph, string label, Func<IslandStatistics, long> read)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-island__stat");

            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-island__stat-icon");

            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-island__stat-label");

            var value = new Label { pickingMode = PickingMode.Ignore };
            value.AddToClassList("rf-island__stat-value");

            root.Add(icon);
            root.Add(name);
            root.Add(value);
            container.Add(root);
            statRows.Add(new StatRow { Read = read, Value = value });
        }

        // ---- Refreshing ----------------------------------------------------------------------

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (!island)
            {
                if (window.style.display == DisplayStyle.Flex) ApplyVisibility();
                return;
            }

            timeLeft -= Time.deltaTime;
            if (timerFill != null)
            {
                var fraction = timeTotal > 0f ? Mathf.Clamp01(timeLeft / timeTotal) : 0f;
                timerFill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
            }

            if (Time.unscaledTime < nextRefresh || (drag != null && drag.IsCollapsed))
            {
                return;
            }

            nextRefresh = Time.unscaledTime + refreshInterval;

            try
            {
                Refresh();
            }
            catch (Exception exc)
            {
                // Islands rebuild their player lists as people come and go; a bad frame is not
                // worth more than one log line per island.
                Shinobytes.Debug.LogError("Island details refresh failed for " + island.Identifier + ": " + exc.Message);
                nextRefresh = Time.unscaledTime + 5f;
            }
        }

        private void Refresh()
        {
            SetText(nameLabel, island.Identifier);
            SetText(levelChip, string.Format(CultureInfo.InvariantCulture, Localization.UI_ISLAND_LEVEL, island.LevelRequirement));

            var gm = GameManager.Instance;
            var onsen = gm != null && gm.Onsen != null && gm.Onsen.RestingAreaAvailable(island);
            onsenChip.style.display = onsen ? DisplayStyle.Flex : DisplayStyle.None;

            var list = island.GetPlayers();
            var count = list != null ? list.Count : 0;
            SetText(players, count.ToString(CultureInfo.InvariantCulture));

            RefreshTraining(list);

            var stats = island.Statistics;
            if (stats != null)
            {
                for (var i = 0; i < statRows.Count; ++i)
                {
                    var row = statRows[i];
                    var value = row.Read(stats);
                    if (value != row.Shown)
                    {
                        row.Shown = value;
                        row.Value.text = HudFormat.Compact(value);
                    }
                }
            }
        }

        private void RefreshTraining(IReadOnlyList<PlayerController> list)
        {
            for (var i = 0; i < skillRows.Count; ++i) skillRows[i].Value = 0;

            if (list != null)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var player = list[i];
                    if (!player || player.isDestroyed)
                    {
                        continue;
                    }

                    SkillStat active;
                    try { active = player.GetActiveSkillStat(); }
                    catch { active = null; }

                    if (active == null)
                    {
                        continue;
                    }

                    // Training "all" counts toward each melee skill, as the old panel did.
                    if (player.GetTask() == TaskType.Fighting && (active.Type == Skill.Health || active.Type == Skill.Melee))
                    {
                        Count(Skill.Attack);
                        Count(Skill.Defense);
                        Count(Skill.Strength);
                        continue;
                    }

                    // The old panel showed the Health counter in the Healing row, so healers never
                    // appeared there. Healing is counted as itself here.
                    Count(active.Type);
                }
            }

            sorted.Clear();
            var top = 0;
            for (var i = 0; i < skillRows.Count; ++i)
            {
                if (skillRows[i].Value > 0)
                {
                    sorted.Add(skillRows[i]);
                    top = Mathf.Max(top, skillRows[i].Value);
                }
            }

            sorted.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : Array.IndexOf(ChartSkills, a.Skill).CompareTo(Array.IndexOf(ChartSkills, b.Skill)));

            // Reorder only when the ranking changed, so rows are not moved twice a second for nothing.
            var order = string.Empty;
            for (var i = 0; i < sorted.Count; ++i) order += (int)sorted[i].Skill + ",";
            if (order != shownOrder)
            {
                shownOrder = order;
                for (var i = 0; i < skillRows.Count; ++i)
                {
                    skillRows[i].Root.style.display = skillRows[i].Value > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                }

                for (var i = 0; i < sorted.Count; ++i)
                {
                    sorted[i].Root.BringToFront();
                }
            }

            for (var i = 0; i < sorted.Count; ++i)
            {
                var row = sorted[i];
                if (row.Value != row.Shown)
                {
                    row.Shown = row.Value;
                    row.Count.text = row.Value.ToString(CultureInfo.InvariantCulture);
                }

                var fill = top > 0 ? row.Value / (float)top : 0f;
                if (Mathf.Abs(fill - row.ShownFill) >= 0.005f)
                {
                    row.ShownFill = fill;
                    row.Fill.style.width = new Length(fill * 100f, LengthUnit.Percent);
                }
            }

            if (trainingEmpty != null)
            {
                trainingEmpty.style.display = sorted.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void Count(Skill skill)
        {
            if (rowBySkill.TryGetValue(skill, out var row))
            {
                row.Value++;
            }
        }

        private Sprite IconFor(Skill skill)
        {
            for (var i = 0; i < skillIcons.Length; ++i)
            {
                if (skillIcons[i].Skill == skill) return skillIcons[i].Icon;
            }

            return null;
        }

        private static void SetText(Label label, string value)
        {
            if (label != null && label.text != value)
            {
                label.text = value;
            }
        }
    }
}
