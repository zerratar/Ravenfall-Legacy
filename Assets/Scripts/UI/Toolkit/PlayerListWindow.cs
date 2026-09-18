using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
// Aliased rather than importing RavenNest.Models wholesale, matching PlayerListItem. The
// namespace also carries names that collide with the global Utility and GameMath helpers.
using Skill = RavenNest.Models.Skill;

namespace Shinobytes.UI
{
    /// <summary>
    /// The player list, rebuilt on UI Toolkit as a draggable window around a virtualised list.
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

        private const float RowHeight = 64f;

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
        private DraggableWindow drag;

        private float nextRefresh;
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
            public Label Level;
            public Label SkillLabel;
            public Label Rate;
            public VisualElement BarFill;

            public PlayerController Player;
            public string LastName;
            public int LastCombatLevel = -1;
            public int LastSkillLevel = -1;
            public Skill LastSkill = Skill.None;
            public long LastRate = -1;
            public float LastProgress = -1f;
        }

        private void OnEnable()
        {
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
            if (root == null)
            {
                return;
            }

            window = root.Q<VisualElement>("playerlist-window");
            header = root.Q<VisualElement>("playerlist-header");
            countLabel = root.Q<Label>("playerlist-count");
            listView = root.Q<ListView>("playerlist-view");

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
            }

            bound = true;
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-player-row");

            var main = new VisualElement();
            main.AddToClassList("rf-player-row__main");

            var name = new Label();
            name.AddToClassList("rf-player-row__name");

            var sub = new VisualElement();
            sub.AddToClassList("rf-player-row__sub");

            var skill = new Label();
            skill.AddToClassList("rf-player-row__skill");

            var rate = new Label();
            rate.AddToClassList("rf-player-row__rate");

            sub.Add(skill);
            sub.Add(rate);

            var bar = new VisualElement();
            bar.AddToClassList("rf-bar");
            var fill = new VisualElement();
            fill.AddToClassList("rf-bar__fill");
            bar.Add(fill);

            main.Add(name);
            main.Add(sub);
            main.Add(bar);

            var level = new Label();
            level.AddToClassList("rf-player-row__level");

            row.Add(main);
            row.Add(level);

            var state = new RowState
            {
                Root = row,
                Name = name,
                Level = level,
                SkillLabel = skill,
                Rate = rate,
                BarFill = fill
            };

            row.userData = state;
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
            state.LastName = null;
            state.LastCombatLevel = -1;
            state.LastSkillLevel = -1;
            state.LastSkill = Skill.None;
            state.LastRate = -1;
            state.LastProgress = -1f;

            element.EnableInClassList("rf-player-row--alt", (index & 1) == 1);

            Refresh(state);

            // Play the entry transition.
            //
            // The stylesheet defines rf-player-row--entering as the offset, transparent state, and
            // the transition on the base class animates back out of it. Nothing applied the class
            // until now, so the animation was declared and never ran. It has to be removed on a
            // later frame than it is added: setting and clearing a property within one layout pass
            // gives the transition no start value to move from, so it snaps.
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

            AutoScroll();

            nextRefresh -= Time.unscaledDeltaTime;
            if (nextRefresh > 0f)
            {
                return;
            }

            nextRefresh = refreshInterval;

            for (var i = 0; i < rows.Count; ++i)
            {
                Refresh(rows[i]);
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

            if (state.LastName != player.PlayerName)
            {
                state.LastName = player.PlayerName;
                state.Name.text = player.PlayerName;
            }

            var combatLevel = player.Stats != null ? player.Stats.CombatLevel : 0;
            if (combatLevel != state.LastCombatLevel)
            {
                state.LastCombatLevel = combatLevel;
                state.Level.text = combatLevel.ToString();
            }

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
                if (state.LastSkill != Skill.None)
                {
                    state.LastSkill = Skill.None;
                    state.SkillLabel.text = string.Empty;
                    state.Rate.text = string.Empty;
                    SetFill(state, 0f);
                }
                return;
            }

            if (activeSkill != state.LastSkill || skill.Level != state.LastSkillLevel)
            {
                state.LastSkill = activeSkill;
                state.LastSkillLevel = skill.Level;

                // Health and melee have no meaningful level readout here, matching the old row.
                state.SkillLabel.text = activeSkill == Skill.Health || activeSkill == Skill.Melee
                    ? activeSkill.GetShortName()
                    : activeSkill.GetShortName() + " " + skill.Level;
            }

            if (activeSkill == Skill.Health || activeSkill == Skill.Melee)
            {
                if (state.LastRate != 0)
                {
                    state.LastRate = 0;
                    state.Rate.text = string.Empty;
                }
            }
            else
            {
                var perHour = (long)skill.GetExperiencePerHour();
                if (perHour != state.LastRate)
                {
                    state.LastRate = perHour;
                    state.Rate.text = perHour > 0 ? Utility.FormatValue(perHour) + " xp/h" : string.Empty;
                }
            }

            // Confirmed correct rather than merely copied.
            //
            // SkillStat.Experience counts within the current level and resets on level up, carrying
            // any overflow forward. ExperienceForLevel(n) returns ExperienceArray[n - 2], which is
            // the amount needed to get from n-1 to n rather than a running total, so for a level L
            // skill ExperienceForLevel(L + 1) is exactly the size of the level being worked through.
            // Experience over that is a true 0..1 ratio.
            //
            // The cumulative form is the obsolete OldExperienceForLevel: totalling experience across
            // levels eventually overflowed and put a ceiling on max level, which is why it resets
            // per level now.
            var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
            var progress = skill.Experience > 0 && nextLevelExp > 0
                ? (float)(skill.Experience / nextLevelExp)
                : 0f;

            SetFill(state, progress);
        }

        private static void SetFill(RowState state, float progress)
        {
            progress = Mathf.Clamp01(progress);

            // Sub pixel changes are invisible and still cost a layout pass.
            if (Mathf.Abs(progress - state.LastProgress) < 0.005f)
            {
                return;
            }

            state.LastProgress = progress;
            state.BarFill.style.width = new Length(progress * 100f, LengthUnit.Percent);
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
            if (countLabel != null)
            {
                countLabel.text = players.Count.ToString();
            }

            // RefreshItems rebinds what is on screen and re-measures the scroll range without
            // throwing away the pooled row elements, which Rebuild would.
            listView?.RefreshItems();
        }

        public void SetVisible(bool visible)
        {
            if (window != null)
            {
                window.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
