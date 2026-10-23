using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The town resource bar at the top of the screen in free camera mode: village level and
    /// progress, wood, ore, fish, wheat and coins with their hourly rates, and houses built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bar folds into a small "VILLAGE" tab. Down in a dungeon it is folded on its own, since the
    /// village is not on screen there; the tab opens it for that visit, and leaving the dungeon brings
    /// back whatever the streamer had above ground. Folding it above ground is remembered.
    /// </para>
    /// <para>
    /// The server only ever sends totals, so the hourly rates are measured here: the totals are
    /// sampled every <see cref="sampleInterval"/> seconds, and a rate is the change over the
    /// samples kept for the last <see cref="rateWindow"/> seconds. Sampling runs whether or not the
    /// bar is on screen, so the rates are ready the moment it opens. A rate only shows once there
    /// is at least a minute of history behind it; a guess from two samples seconds apart would
    /// swing wildly with every server update.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class TownBarView : MonoBehaviour
    {
        [SerializeField] private float refreshInterval = 0.5f;

        [Tooltip("Seconds between samples of the resource totals.")]
        [SerializeField] private float sampleInterval = 10f;

        [Tooltip("How far back, in seconds, the hourly rates look.")]
        [SerializeField] private float rateWindow = 600f;

        [Tooltip("Rates are hidden until this many seconds of history exist.")]
        [SerializeField] private float minimumHistory = 60f;

        [Header("Icons")]
        [SerializeField] private Sprite woodIcon;
        [SerializeField] private Sprite oreIcon;
        [SerializeField] private Sprite fishIcon;
        [SerializeField] private Sprite wheatIcon;
        [SerializeField] private Sprite coinIcon;

        private const int ResourceCount = 5;

        private sealed class Resource
        {
            public Label Value;
            public Label Rate;
            public long Shown = long.MinValue;
            public long ShownRate = long.MinValue;
            public bool RateVisible;
        }

        private struct Sample
        {
            public float Time;
            public long[] Values;
        }

        private readonly Resource[] resources = new Resource[ResourceCount];
        private readonly Queue<Sample> samples = new Queue<Sample>();
        private readonly long[] current = new long[ResourceCount];
        private long[] lastSample;

        /// <summary>
        /// A change between two samples larger than this fraction of the previous total, and more
        /// than <see cref="JumpFloor"/>, is treated as new data rather than as gathering.
        /// </summary>
        private const double JumpFraction = 0.5;
        private const long JumpFloor = 1000;

        private VisualElement bar;
        private Label level;
        private VisualElement expFill;
        private Label houses;
        private Button build;
        private Label buildLabel;
        private Label buildBadge;
        private bool shownUpgrade;
        private bool shownBuildActive;
        private bool bound;
        private bool shown;

        // Folding: the tab that stands in for the bar, the streamer's choice above ground (saved), and
        // whether the bar was opened during the current dungeon visit (forgotten on leaving).
        private const string FoldedKey = "rf_townbar_folded";
        // the camera is down in a dungeon below this height (as MusicManager and DungeonMood)
        private const float DungeonDepth = -30f;
        private Button tab;
        private Button fold;
        private bool tabShown;
        private bool foldedAboveGround;
        private bool openedInDungeon;
        private bool inDungeon;

        private float nextRefresh;
        private float nextSample;
        private int shownLevel = int.MinValue;
        private float shownExp = -1f;
        private string shownHouses;

        private void OnEnable()
        {
            // The shortcut bar shares this document; it needs no scene object of its own.
            if (!GetComponent<ShortcutBarView>()) gameObject.AddComponent<ShortcutBarView>();

            TryBind();
        }

        private void OnDisable()
        {
            bound = false;
        }

        /// <summary>
        /// Forgets what was last drawn, so a freshly built tree gets every value written. Called on
        /// each bind: after a change of language or a live reload the tree is new, and a cache that
        /// still matched the old one left labels empty.
        /// </summary>
        private void ResetShown()
        {
            shownUpgrade = false;
            shownBuildActive = false;
            shown = false;
            tabShown = false;
            shownLevel = int.MinValue;
            shownExp = -1f;
            shownHouses = null;
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            bar = root?.Q<VisualElement>("town-bar");
            if (bar == null)
            {
                return;
            }

            level = root.Q<Label>("town-level");
            expFill = root.Q<VisualElement>("town-exp-fill");
            houses = root.Q<Label>("town-houses");
            build = root.Q<Button>("town-build");
            buildLabel = root.Q<Label>("town-build-label");
            buildBadge = root.Q<Label>("town-build-badge");
            if (build != null)
            {
                build.clicked += () => VillageBuildMode.Instance?.Toggle();
                if (buildLabel != null) buildLabel.text = Localization.UI_TOWN_BUILD;
            }

            var caption = root.Q<Label>("town-caption");
            if (caption != null) caption.text = Localization.UI_TOWN_VILLAGE;
            var housesCaption = root.Q<Label>("town-houses-caption");
            if (housesCaption != null) housesCaption.text = Localization.UI_TOWN_HOUSES;

            var container = root.Q<VisualElement>("town-resources");
            if (container != null)
            {
                container.Clear();
                AddResource(container, 0, Localization.UI_TOWN_WOOD, woodIcon);
                AddResource(container, 1, Localization.UI_TOWN_ORE, oreIcon);
                AddResource(container, 2, Localization.UI_TOWN_FISH, fishIcon);
                AddResource(container, 3, Localization.UI_TOWN_WHEAT, wheatIcon);
                AddResource(container, 4, Localization.UI_TOWN_COINS, coinIcon);
            }

            tab = root.Q<Button>("town-tab");
            if (tab != null)
            {
                tab.clicked += () => SetFolded(false);
                tab.tooltip = Localization.UI_TOWN_SHOW;
                var tabLabel = root.Q<Label>("town-tab-label");
                if (tabLabel != null) tabLabel.text = Localization.UI_TOWN_VILLAGE;
                tab.style.display = DisplayStyle.None;
            }

            fold = root.Q<Button>("town-fold");
            if (fold != null)
            {
                fold.clicked += () => SetFolded(true);
                fold.tooltip = Localization.UI_TOWN_HIDE;
            }

            foldedAboveGround = PlayerPrefs.GetInt(FoldedKey, 0) == 1;

            bar.style.display = DisplayStyle.None;
            shown = false;
            ResetShown();
            bound = true;
        }

        private void AddResource(VisualElement container, int index, string caption, Sprite icon)
        {
            var section = new VisualElement { pickingMode = PickingMode.Ignore };
            section.AddToClassList("rf-town__section");

            var iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.AddToClassList("rf-town__res-icon");
            if (icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(icon);
            }

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-town__text");

            var captionLabel = new Label(caption) { pickingMode = PickingMode.Ignore };
            captionLabel.AddToClassList("rf-town__caption");

            var value = new Label { pickingMode = PickingMode.Ignore };
            value.AddToClassList("rf-town__value");

            var rate = new Label { pickingMode = PickingMode.Ignore };
            rate.AddToClassList("rf-town__rate");
            rate.style.display = DisplayStyle.None;

            text.Add(captionLabel);
            text.Add(value);
            text.Add(rate);
            section.Add(iconElement);
            section.Add(text);
            container.Add(section);

            resources[index] = new Resource { Value = value, Rate = rate };
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

            var gm = GameManager.Instance;
            var townHall = gm != null && gm.Village != null ? gm.Village.TownHall : null;

            // Nothing to show, and nothing worth sampling, until the game has loaded and the village
            // totals have come from the server. Before that every total reads 0.
            var loaded = townHall && gm.IsLoaded && gm.RavenNest != null && gm.RavenNest.Authenticated;
            if (!loaded)
            {
                SetShown(false);
                return;
            }

            var now = Time.unscaledTime;
            if (now >= nextSample)
            {
                nextSample = now + sampleInterval;
                TakeSample(townHall, now);
            }

            // Checked every frame, which is two flag reads, so the bar goes the moment the camera
            // leaves the village. Only the contents wait for the refresh interval.
            // The village is what is on screen in free camera mode. The tavern also uses the free
            // camera, but it has its own interface and the bar would only be in the way there.
            // IsFreeCameraActive rather than State == Free: State starts as Free and the automatic
            // observe path never changes it, which left the bar up while a player was observed.
            var camera = gm.Camera;
            var visible = camera != null
                && camera.IsFreeCameraActive
                && !(gm.Tavern && gm.Tavern.IsActivated);

            // Entering or leaving a dungeon starts over with that place's default: folded below,
            // the streamer's own choice above.
            var dungeonNow = visible && gm.Dungeons && gm.Dungeons.Started && camera.transform.position.y < DungeonDepth;
            if (dungeonNow != inDungeon)
            {
                inDungeon = dungeonNow;
                openedInDungeon = false;
            }

            var folded = inDungeon ? !openedInDungeon : foldedAboveGround;
            SetTabShown(visible && folded);

            var open = visible && !folded;
            var appearing = open && !shown;
            SetShown(open);
            if (!open || (now < nextRefresh && !appearing))
            {
                return;
            }

            nextRefresh = now + refreshInterval;
            Refresh(townHall, now);
        }

        private void SetShown(bool value)
        {
            if (!bound || value == shown)
            {
                return;
            }

            shown = value;
            bar.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetTabShown(bool value)
        {
            if (tab == null || value == tabShown)
            {
                return;
            }

            tabShown = value;
            tab.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Folds the bar into the tab, or opens it. In a dungeon this lasts the visit, above ground it is saved.</summary>
        private void SetFolded(bool value)
        {
            if (inDungeon)
            {
                openedInDungeon = !value;
                return;
            }

            foldedAboveGround = value;
            PlayerPrefs.SetInt(FoldedKey, value ? 1 : 0);
        }

        private void Read(TownHallManager townHall)
        {
            current[0] = townHall.Wood;
            current[1] = townHall.Ore;
            current[2] = townHall.Fish;
            current[3] = townHall.Wheat;
            current[4] = townHall.Coins;
        }

        private void TakeSample(TownHallManager townHall, float now)
        {
            Read(townHall);

            // Rates are only honest across samples of the same data. Start over when the totals
            // first arrive (everything was 0 until then, which turned the whole stockpile into an
            // hourly "gain") and when a total jumps by more than it could plausibly change in one
            // interval, as on a reconnect or a state reload.
            if (samples.Count > 0 && IsDiscontinuity(lastSample, current))
            {
                samples.Clear();
            }

            if (IsEmpty(current))
            {
                samples.Clear();
                return;
            }

            var copy = (long[])current.Clone();
            lastSample = copy;
            samples.Enqueue(new Sample { Time = now, Values = copy });

            while (samples.Count > 0 && now - samples.Peek().Time > rateWindow)
            {
                samples.Dequeue();
            }
        }

        private void RefreshBuildButton(TownHallManager townHall)
        {
            if (build == null)
            {
                return;
            }

            var mode = VillageBuildMode.Instance;
            build.style.display = mode ? DisplayStyle.Flex : DisplayStyle.None;
            var active = mode && mode.IsActive;

            // The next town hall tier can be bought: say so on the button that leads to it.
            var upgrade = !active && townHall && townHall.CanUpgrade;
            if (upgrade != shownUpgrade && buildBadge != null)
            {
                shownUpgrade = upgrade;
                buildBadge.style.display = upgrade ? DisplayStyle.Flex : DisplayStyle.None;
                build.tooltip = upgrade ? Localization.UI_TOWN_UPGRADE_READY : string.Empty;
            }

            if (active == shownBuildActive)
            {
                return;
            }

            shownBuildActive = active;
            build.EnableInClassList("rf-town__build--active", active);
            if (buildLabel != null) buildLabel.text = active ? Localization.UI_TOWN_BUILD_EXIT : Localization.UI_TOWN_BUILD;
        }

        private static bool IsEmpty(long[] values)
        {
            for (var i = 0; i < values.Length; ++i)
            {
                if (values[i] != 0) return false;
            }

            return true;
        }

        private static bool IsDiscontinuity(long[] previous, long[] next)
        {
            if (previous == null)
            {
                return false;
            }

            if (IsEmpty(previous) != IsEmpty(next))
            {
                return true;
            }

            for (var i = 0; i < next.Length; ++i)
            {
                var delta = Math.Abs(next[i] - previous[i]);
                if (delta > JumpFloor && delta > Math.Abs(previous[i]) * JumpFraction)
                {
                    return true;
                }
            }

            return false;
        }

        private void Refresh(TownHallManager townHall, float now)
        {
            RefreshBuildButton(townHall);

            int villageLevel = townHall.Level;
            if (villageLevel != shownLevel)
            {
                shownLevel = villageLevel;
                level.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_TOWN_LEVEL,
                    "<color=#E8A33D>" + villageLevel.ToString(CultureInfo.InvariantCulture) + "</color>");
            }

            // Village experience counts within the level, like skills: the amount needed for the
            // next level is ExperienceForLevel(level + 1).
            var needed = GameMath.ExperienceForLevel(villageLevel + 1);
            var exp = needed > 0 ? Mathf.Clamp01((float)(townHall.Experience / needed)) : 0f;
            if (expFill != null && Mathf.Abs(exp - shownExp) >= 0.002f)
            {
                shownExp = exp;
                expFill.style.width = new Length(exp * 100f, LengthUnit.Percent);
            }

            var housesText = townHall.UsedSlots.ToString(CultureInfo.InvariantCulture) + " / " + townHall.SlotCount.ToString(CultureInfo.InvariantCulture);
            if (houses != null && housesText != shownHouses)
            {
                shownHouses = housesText;
                houses.text = housesText;
            }

            Read(townHall);
            var oldest = samples.Count > 0 ? samples.Peek() : default;
            var history = samples.Count > 0 ? now - oldest.Time : 0f;

            for (var i = 0; i < ResourceCount; ++i)
            {
                var r = resources[i];
                if (r == null)
                {
                    continue;
                }

                if (current[i] != r.Shown)
                {
                    r.Shown = current[i];
                    r.Value.text = current[i] < 10_000_000 ? HudFormat.Grouped(current[i]) : HudFormat.Compact(current[i]);
                }

                var hasRate = history >= minimumHistory && oldest.Values != null;
                var perHour = hasRate ? (long)((current[i] - oldest.Values[i]) / (double)history * 3600d) : 0L;
                var showRate = hasRate && perHour != 0;

                if (showRate != r.RateVisible)
                {
                    r.RateVisible = showRate;
                    r.Rate.style.display = showRate ? DisplayStyle.Flex : DisplayStyle.None;
                }

                if (showRate && perHour != r.ShownRate)
                {
                    r.ShownRate = perHour;
                    var magnitude = Math.Abs(perHour);
                    var text = magnitude < 100_000 ? HudFormat.Grouped(magnitude) : HudFormat.Compact(magnitude);
                    r.Rate.text = (perHour > 0 ? "+" : "-") + string.Format(CultureInfo.InvariantCulture, Localization.UI_TOWN_RATE, text);
                    r.Rate.EnableInClassList("rf-town__rate--down", perHour < 0);
                }
            }
        }
    }
}
