using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
// Aliased rather than importing RavenNest.Models wholesale, matching PlayerListItem. The
// namespace also carries names that collide with the global Utility and GameMath helpers.
using Skill = RavenNest.Models.Skill;

namespace Shinobytes.UI
{
    /// <summary>
    /// The player list, rebuilt on UI Toolkit as a draggable, resizable, collapsible window around a
    /// virtualised list.
    /// </summary>
    /// <remarks>
    /// The old list is why streamers turned this off. It instantiated rows up to the visible count,
    /// then ran a full refresh on every one of them every frame, rebuilding strings each time. The
    /// guard inside its SetText compared the finished string, so it avoided the text mesh rebuild
    /// but had already paid for the concatenation and a Trim allocation to get there. On top of
    /// that, the tracked player collection used Contains on add, FirstOrDefault on remove and
    /// RemoveAt(0) on every scroll step, all linear, so a thousand player stream paid a thousand
    /// element move per row of scrolling.
    ///
    /// <para>
    /// Three things change here. Rows exist only while on screen, because the list is virtualised.
    /// Rows refresh on an interval rather than per frame, and only the rows that exist. And each row
    /// keeps the last value it displayed, so a string is only built when the number behind it
    /// actually changed rather than every time it is looked at.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerListWindow : MonoBehaviour
    {
        /// <summary>
        /// How often visible rows are refreshed. Experience ticks arrive far slower than a frame,
        /// and the bars carry a CSS transition, so movement still looks continuous between updates.
        ///
        /// <para>
        /// This is worth more than it first appears. Level and Experience are ObscuredIntSplit and
        /// ObscuredDouble, so every read decrypts rather than just loading a field. The old list did
        /// that for every value on every pooled row every frame.
        /// </para>
        /// </summary>
        [SerializeField] private float refreshInterval = 0.25f;

        [Tooltip("Scrolls the list slowly so every viewer eventually sees their own row.")]
        [SerializeField] private bool autoScroll = true;

        [Tooltip("Rows per second when auto scrolling.")]
        [SerializeField] private float autoScrollSpeed = 0.35f;

        [Tooltip("Smallest size the window can be resized to. Below this the rows stop being legible on stream.")]
        [SerializeField] private Vector2 minimumSize = new Vector2(260f, 150f);

        /// <summary>Must match the row height in ravenfall-hud.uss and the UXML.</summary>
        private const float RowHeight = 96f;

        /// <summary>Health below this fraction switches the bar to its brighter warning colour.</summary>
        private const float LowHealth = 0.34f;

        private readonly List<PlayerController> players = new List<PlayerController>();

        /// <summary>
        /// Every row element the list has created. Virtualisation keeps this to roughly the number
        /// on screen, so walking it to refresh is cheap and, importantly, independent of how many
        /// players are in the session.
        /// </summary>
        private readonly List<RowState> rows = new List<RowState>();

        private VisualElement window;
        private VisualElement header;
        private Label countLabel;
        private ListView listView;
        private ScrollView scrollView;
        private Button collapseButton;
        private Label visibleLabel;
        private DraggableWindow drag;

        private int shownVisible = -1;
        private int shownTotal = -1;

        private float nextRefresh;
        private bool requestedVisible = true;
        private bool hiddenForBuildMode;
        // Until the game has loaded and logged in, so the list does not sit over the login screen.
        private bool hiddenBeforeLogin;

        /// <summary>The player the camera is following, whose row is highlighted.</summary>
        private PlayerController observed;
        private bool bound;
        private bool pointerInside;

        /// <summary>
        /// Cached per row so a refresh can tell whether anything actually changed before building a
        /// string. This is what keeps the list off the allocation graph.
        /// </summary>
        private sealed class RowState
        {
            public VisualElement Root;
            public Label Name;
            public Label Rested;
            public Label Level;
            public Label SkillLabel;
            public Label Percent;
            public Label Rate;
            public VisualElement HealthBar;
            public VisualElement HealthFill;
            public VisualElement ExpFill;

            public PlayerController Player;
            public string LastName;
            public string LastColorHex;
            /// <summary>-1 unknown, so a recycled row always writes its highlight on first refresh.</summary>
            public int LastHighlight = -1;
            public int LastCombatLevel = -1;
            public int LastSkillLevel = -1;
            public Skill LastSkill = Skill.None;
            public long LastRate = -1;
            public float LastProgress = -1f;
            public int LastPercentTenths = -1;
            public float LastHealth = -1f;
            public bool LastHealthLow;
            public int LastRestedMinutes = -1;

            public void Reset()
            {
                LastName = null;
                LastColorHex = null;
                LastHighlight = -1;
                LastCombatLevel = -1;
                LastSkillLevel = -1;
                LastSkill = Skill.None;
                LastRate = -1;
                LastProgress = -1f;
                LastPercentTenths = -1;
                LastHealth = -1f;
                LastHealthLow = false;
                LastRestedMinutes = -1;
            }
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            bound = false;

            if (collapseButton != null)
            {
                collapseButton.clicked -= OnCollapseClicked;
                collapseButton = null;
            }

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
            if (root == null)
            {
                return;
            }

            window = root.Q<VisualElement>("playerlist-window");
            header = root.Q<VisualElement>("playerlist-header");
            countLabel = root.Q<Label>("playerlist-count");
            listView = root.Q<ListView>("playerlist-view");
            collapseButton = root.Q<Button>("playerlist-collapse");
            visibleLabel = root.Q<Label>("playerlist-visible");
            var grip = root.Q<VisualElement>("playerlist-grip");

            if (window == null || listView == null)
            {
                return;
            }

            listView.itemsSource = players;
            listView.makeItem = MakeRow;
            listView.bindItem = BindRow;
            listView.unbindItem = UnbindRow;
            listView.fixedItemHeight = RowHeight;

            // Q rather than a property: the ScrollView inside a ListView is not part of its public
            // surface, and this is the only way to drive the ticker scroll.
            scrollView = listView.Q<ScrollView>();

            if (scrollView != null)
            {
                // Auto scrolling while somebody is reading or dragging the scrollbar is hostile, so
                // it pauses whenever the pointer is over the list.
                scrollView.RegisterCallback<PointerEnterEvent>(_ => pointerInside = true);
                scrollView.RegisterCallback<PointerLeaveEvent>(_ => pointerInside = false);
            }

            if (header != null)
            {
                drag = DraggableWindow.Attach(window, header, "playerlist");

                // Enabled before the first layout pass, which is when the saved size is restored.
                drag?.EnableResize(grip, minimumSize);
            }

            if (collapseButton != null)
            {
                collapseButton.clicked += OnCollapseClicked;
            }

            var closeButton = root.Q<Button>("playerlist-close");
            if (closeButton != null)
            {
                closeButton.clicked += () =>
                {
                    if (HUDController.Instance) HUDController.Instance.SetPlayerListVisible(false);
                    else SetVisible(false);
                };
            }

            bound = true;
        }

        private void OnCollapseClicked()
        {
            drag?.ToggleCollapsed();

            // Rows were not refreshed while folded away, so bring them current straight away rather
            // than showing stale values for up to one refresh interval.
            if (drag != null && !drag.IsCollapsed)
            {
                nextRefresh = 0f;
            }
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-player-row");

            // ---- left: text and bars ----
            var main = new VisualElement();
            main.AddToClassList("rf-player-row__main");

            var nameLine = new VisualElement();
            nameLine.AddToClassList("rf-player-row__line");

            var name = new Label();
            name.AddToClassList("rf-player-row__name");

            var rested = new Label();
            rested.AddToClassList("rf-player-row__rested");
            rested.style.display = DisplayStyle.None;

            nameLine.Add(name);
            nameLine.Add(rested);

            var meta = new VisualElement();
            meta.AddToClassList("rf-player-row__meta");

            var skill = new Label();
            skill.AddToClassList("rf-player-row__skill");

            var percent = new Label();
            percent.AddToClassList("rf-player-row__percent");

            var spacer = new VisualElement();
            spacer.AddToClassList("rf-player-row__spacer");

            var rate = new Label();
            rate.AddToClassList("rf-player-row__rate");

            meta.Add(skill);
            meta.Add(percent);
            meta.Add(spacer);
            meta.Add(rate);

            var healthBar = new VisualElement();
            healthBar.AddToClassList("rf-bar");
            healthBar.AddToClassList("rf-bar--health");
            var healthFill = new VisualElement();
            healthFill.AddToClassList("rf-bar__fill");
            healthBar.Add(healthFill);

            var expBar = new VisualElement();
            expBar.AddToClassList("rf-bar");
            expBar.AddToClassList("rf-bar--exp");
            var expFill = new VisualElement();
            expFill.AddToClassList("rf-bar__fill");
            expBar.Add(expFill);

            main.Add(nameLine);
            main.Add(meta);
            main.Add(healthBar);
            main.Add(expBar);

            // ---- right: combat level ----
            var levelCol = new VisualElement();
            levelCol.AddToClassList("rf-player-row__level-col");

            var levelCaption = new Label("LV");
            levelCaption.AddToClassList("rf-player-row__level-caption");

            var level = new Label();
            level.AddToClassList("rf-player-row__level");

            levelCol.Add(levelCaption);
            levelCol.Add(level);

            row.Add(main);
            row.Add(levelCol);

            var state = new RowState
            {
                Root = row,
                Name = name,
                Rested = rested,
                Level = level,
                SkillLabel = skill,
                Percent = percent,
                Rate = rate,
                HealthBar = healthBar,
                HealthFill = healthFill,
                ExpFill = expFill
            };

            row.userData = state;

            // Clicking a row points the camera at that player, as clicking a name did in the old list.
            row.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => ObserveRow(state));
            rows.Add(state);
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            if (element.userData is not RowState state)
            {
                return;
            }

            state.Player = index >= 0 && index < players.Count ? players[index] : null;

            // Reset the caches so a recycled row does not keep the previous player's values and skip
            // the update that would correct them.
            state.Reset();

            element.EnableInClassList("rf-player-row--alt", (index & 1) == 1);

            Refresh(state);

            // Play the entry transition. The class is the offset, transparent state; removing it on a
            // later frame lets the transition on the base class animate back. Adding and removing it
            // in the same layout pass gives the transition no start value, so it would snap.
            element.AddToClassList("rf-player-row--entering");
            element.schedule.Execute(() => element.RemoveFromClassList("rf-player-row--entering")).StartingIn(0);
        }

        private void UnbindRow(VisualElement element, int index)
        {
            if (element.userData is RowState state)
            {
                state.Player = null;
            }
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                return;
            }

            var game = GameManager.Instance;
            var waiting = !(game && game.IsLoaded && game.RavenNest != null && game.RavenNest.Authenticated);
            if (waiting != hiddenBeforeLogin)
            {
                hiddenBeforeLogin = waiting;
                ApplyVisible();
            }

            if (waiting)
            {
                return;
            }

            var building = VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive;
            if (building != hiddenForBuildMode)
            {
                hiddenForBuildMode = building;
                ApplyVisible();
            }

            if (building)
            {
                return;
            }

            // Folded away, nothing is visible, so there is nothing worth refreshing or scrolling.
            if (drag != null && drag.IsCollapsed)
            {
                return;
            }

            AutoScroll();

            nextRefresh -= Time.unscaledDeltaTime;
            if (nextRefresh > 0f)
            {
                return;
            }

            nextRefresh = refreshInterval;

            var gm = GameManager.Instance;
            observed = gm != null && gm.Camera != null && gm.Camera.Observer != null ? gm.Camera.Observer.ObservedPlayer : null;

            for (var i = 0; i < rows.Count; ++i)
            {
                Refresh(rows[i]);
            }

            RefreshFooter();
        }

        private void ObserveRow(RowState state)
        {
            var player = state.Player;
            var gm = GameManager.Instance;
            if (!player || gm == null || gm.Camera == null)
            {
                return;
            }

            gm.Camera.ObservePlayer(player);
            observed = player;

            // Move the highlight straight away rather than on the next refresh.
            for (var i = 0; i < rows.Count; ++i)
            {
                if (rows[i].Player)
                {
                    RefreshIdentity(rows[i], rows[i].Player, rows[i].Player == player);
                }
            }
        }

        /// <summary>
        /// "8 of 11 players" when the list is taller than the window, "11 players" when it all fits.
        /// Only fully visible rows count, so a row cut in half by the bottom edge is not claimed as seen.
        /// </summary>
        private void RefreshFooter()
        {
            if (visibleLabel == null || scrollView == null)
            {
                return;
            }

            var viewport = scrollView.contentViewport.contentRect.height;
            var fits = float.IsNaN(viewport) ? players.Count : Mathf.FloorToInt(viewport / RowHeight);
            var visible = Mathf.Clamp(fits, 0, players.Count);

            if (visible == shownVisible && players.Count == shownTotal)
            {
                return;
            }

            shownVisible = visible;
            shownTotal = players.Count;

            if (visible >= players.Count)
            {
                visibleLabel.text = players.Count == 1
                    ? Localization.UI_PLAYERS_ONE
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_PLAYERS_COUNT, players.Count);
            }
            else
            {
                visibleLabel.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_PLAYERS_PARTIAL, visible, players.Count);
            }
        }

        private void AutoScroll()
        {
            if (!autoScroll || pointerInside || scrollView == null || players.Count == 0)
            {
                return;
            }

            var viewHeight = scrollView.contentViewport.contentRect.height;
            var contentHeight = players.Count * RowHeight;
            if (contentHeight <= viewHeight)
            {
                return;
            }

            var offset = scrollView.scrollOffset;
            offset.y += autoScrollSpeed * RowHeight * Time.unscaledDeltaTime;

            // Wrap at the bottom so the list cycles rather than parking on the last row. The old
            // list achieved this by rotating the data, which is what made removal linear.
            if (offset.y >= contentHeight - viewHeight)
            {
                offset.y = 0f;
            }

            scrollView.scrollOffset = offset;
        }

        /// <summary>
        /// Brings one row up to date, writing only the parts whose underlying value changed.
        /// </summary>
        private void Refresh(RowState state)
        {
            var player = state.Player;
            if (player == null || !player || player.isDestroyed)
            {
                return;
            }

            RefreshIdentity(state, player, observed != null && observed == player);
            RefreshHealth(state, player);
            RefreshRested(state, player);
            RefreshSkill(state, player);
        }

        private static void RefreshIdentity(RowState state, PlayerController player, bool isObserved)
        {
            if (state.LastName != player.PlayerName)
            {
                state.LastName = player.PlayerName;
                state.Name.text = player.PlayerName;
            }

            if (!string.Equals(state.LastColorHex, player.PlayerNameHexColor, System.StringComparison.Ordinal))
            {
                state.LastColorHex = player.PlayerNameHexColor;
                state.Name.style.color = HudFormat.ReadableNameColor(player.PlayerNameHexColor);
            }

            // The highlight follows the observed player, so the list and the camera agree on who is
            // on screen. It used to mark the broadcaster, and a recycled row could keep that class
            // for whoever it showed next, because the cache reset to "off" while the class stayed on.
            var highlight = isObserved ? 1 : 0;
            if (highlight != state.LastHighlight)
            {
                state.LastHighlight = highlight;
                state.Root.EnableInClassList("rf-player-row--highlight", isObserved);
            }

            var combatLevel = player.Stats != null ? player.Stats.CombatLevel : 0;
            if (combatLevel != state.LastCombatLevel)
            {
                state.LastCombatLevel = combatLevel;
                state.Level.text = combatLevel.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static void RefreshHealth(RowState state, PlayerController player)
        {
            var health = player.Stats != null ? player.Stats.Health : null;
            var fraction = 0f;
            if (health != null)
            {
                int current = health.CurrentValue;
                int max = health.Level;
                fraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            }

            if (SetFill(state.HealthFill, fraction, ref state.LastHealth))
            {
                var low = fraction < LowHealth;
                if (low != state.LastHealthLow)
                {
                    state.LastHealthLow = low;
                    state.HealthBar.EnableInClassList("rf-bar--low", low);
                }
            }
        }

        private static void RefreshRested(RowState state, PlayerController player)
        {
            var seconds = player.Rested != null ? player.Rested.RestedTime : 0d;
            var minutes = seconds > 0 ? (int)(seconds / 60d) : 0;
            if (minutes == state.LastRestedMinutes)
            {
                return;
            }

            state.LastRestedMinutes = minutes;
            if (seconds <= 0)
            {
                state.Rested.style.display = DisplayStyle.None;
                return;
            }

            var restedText = minutes >= 60
                ? (minutes / 60).ToString(CultureInfo.InvariantCulture) + "h"
                : minutes.ToString(CultureInfo.InvariantCulture) + "m";
            state.Rested.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTED, restedText);
            state.Rested.style.display = DisplayStyle.Flex;
        }

        private static void RefreshSkill(RowState state, PlayerController player)
        {
            // Wrapped because GetActiveSkillStat has thrown in the past when a player is mid state
            // change; the old row logged and swallowed it the same way, just noisier.
            SkillStat skill = null;
            var activeSkill = Skill.None;
            try
            {
                activeSkill = player.ActiveSkill;
                skill = player.GetActiveSkillStat();
            }
            catch
            {
                skill = null;
            }

            if (skill == null)
            {
                if (state.LastSkill != Skill.None || state.LastProgress < 0f)
                {
                    state.LastSkill = Skill.None;
                    state.SkillLabel.text = "Idle";
                    state.Percent.text = string.Empty;
                    state.Rate.text = string.Empty;
                    state.LastPercentTenths = -1;
                    state.LastRate = -1;
                    SetFill(state.ExpFill, 0f, ref state.LastProgress);
                }
                return;
            }

            // Health and melee have no meaningful level or progress readout here, matching the old
            // row. They train the combat stats underneath rather than a single skill of their own.
            var levelled = activeSkill != Skill.Health && activeSkill != Skill.Melee;

            if (activeSkill != state.LastSkill || skill.Level != state.LastSkillLevel)
            {
                state.LastSkill = activeSkill;
                state.LastSkillLevel = skill.Level;

                // Full names rather than the three letter codes. The level now has its own column,
                // so there is room, and "Woodcutting" reads at a glance where "Woo" did not.
                state.SkillLabel.text = levelled
                    ? activeSkill.ToString() + "  <b>" + skill.Level + "</b>"
                    : activeSkill.ToString();
            }

            if (!levelled)
            {
                if (state.LastRate != 0)
                {
                    state.LastRate = 0;
                    state.Rate.text = string.Empty;
                    state.Percent.text = string.Empty;
                    state.LastPercentTenths = -1;
                }

                SetFill(state.ExpFill, 0f, ref state.LastProgress);
                return;
            }

            var perHour = (long)skill.GetExperiencePerHour();
            if (perHour != state.LastRate)
            {
                state.LastRate = perHour;
                state.Rate.text = perHour > 0 ? HudFormat.Compact(perHour) + " xp/h" : string.Empty;
            }

            // SkillStat.Experience counts within the current level and resets on level up, carrying
            // any overflow forward. ExperienceForLevel(n) returns the amount needed to get from n-1
            // to n rather than a running total, so for a level L skill ExperienceForLevel(L + 1) is
            // exactly the size of the level being worked through and this is a true 0..1 ratio. The
            // cumulative form is the obsolete OldExperienceForLevel, which overflowed and capped the
            // maximum level.
            var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
            var progress = skill.Experience > 0 && nextLevelExp > 0
                ? Mathf.Clamp01((float)(skill.Experience / nextLevelExp))
                : 0f;

            SetFill(state.ExpFill, progress, ref state.LastProgress);

            // Tenths of a percent, so the label only rebuilds when the shown digit changes.
            var tenths = (int)(progress * 1000f);
            if (tenths != state.LastPercentTenths)
            {
                state.LastPercentTenths = tenths;

                // Invariant culture on purpose: the game runs on machines set to Swedish and other
                // locales that would print 42,1 and mix decimal marks with the English text around it.
                state.Percent.text = (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            }
        }

        /// <summary>
        /// Sets a bar fill, skipping changes too small to see. Returns true when it wrote.
        /// </summary>
        private static bool SetFill(VisualElement fill, float fraction, ref float last)
        {
            fraction = Mathf.Clamp01(fraction);

            // Sub pixel changes are invisible and still cost a layout pass.
            if (last >= 0f && Mathf.Abs(fraction - last) < 0.005f)
            {
                return false;
            }

            last = fraction;
            fill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
            return true;
        }

        // ---- Data ----------------------------------------------------------------

        public void AddPlayer(PlayerController player)
        {
            if (!player || players.Contains(player))
            {
                return;
            }

            players.Add(player);
            OnPlayersChanged();
        }

        public void RemovePlayer(PlayerController player)
        {
            if (!player || !players.Remove(player))
            {
                return;
            }

            OnPlayersChanged();
        }

        public void SetPlayers(IReadOnlyList<PlayerController> source)
        {
            players.Clear();
            for (var i = 0; i < source.Count; ++i)
            {
                if (source[i])
                {
                    players.Add(source[i]);
                }
            }

            OnPlayersChanged();
        }

        private void OnPlayersChanged()
        {
            // The footer depends on the count as much as on the window size.
            shownTotal = -1;

            if (countLabel != null)
            {
                countLabel.text = players.Count.ToString(CultureInfo.InvariantCulture);
            }

            // RefreshItems rebinds what is on screen and re-measures the scroll range without
            // throwing away the pooled row elements, which Rebuild would.
            listView?.RefreshItems();
        }

        public void SetVisible(bool visible)
        {
            requestedVisible = visible;
            ApplyVisible();
        }

        /// <summary>
        /// Visible when the settings say so, except before login and during village build mode, which
        /// uses the left side of the screen for its bonuses panel.
        /// </summary>
        private void ApplyVisible()
        {
            if (window == null)
            {
                return;
            }

            var visible = requestedVisible && !hiddenForBuildMode && !hiddenBeforeLogin;
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (window.style.display != display)
            {
                window.style.display = display;
            }
        }
    }
}
