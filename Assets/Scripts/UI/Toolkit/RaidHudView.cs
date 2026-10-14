using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The raid panel in the top centre status column: boss level, time left, health, raiders, and
    /// how to join.
    /// </summary>
    /// <remarks>
    /// Replaces three separate pieces of the old raid UI: a full width announcement card, a timer
    /// floating in the middle of the fight, and a health bar at the bottom. Viewers are there to
    /// watch the fight, so everything the raid has to say now sits in one place out of its way,
    /// under the XP boost pill rather than on top of it.
    ///
    /// <para>
    /// RaidManager pushes health, level and time every frame. Each value is cached and a label is
    /// only rewritten when what it would display actually changes, so the per frame cost is a few
    /// comparisons rather than a few string builds.
    /// </para>
    ///
    /// <para>
    /// Calls that arrive before the UIDocument has built its tree are kept and applied on bind.
    /// All Awake calls run before any OnEnable, and the document builds its tree in OnEnable, so a
    /// raid that starts during load would otherwise write into elements that do not exist yet.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class RaidHudView : MonoBehaviour
    {
        [Tooltip("How long the large 'boss appeared' line stays open before the panel folds it away.")]
        [SerializeField] private float announceSeconds = 4f;

        [Tooltip("Below this many seconds left, the timer turns to its warning colour.")]
        [SerializeField] private float lowTimeSeconds = 60f;

        /// <summary>Mirrors --rf-live, used inside rich text where a stylesheet value cannot reach.</summary>
        private const string LiveHex = "#5FA8C7";

        /// <summary>Mirrors --rf-accent.</summary>
        private const string AccentHex = "#E8A33D";

        private VisualElement hud;
        private Label announce;
        private Label title;
        private Label level;
        private Label raiders;
        private Label timer;
        private VisualElement fill;
        private VisualElement ghost;
        private Label hpText;
        private Label percent;
        private VisualElement joinPill;
        private Label join;
        private bool bound;

        // Desired state, kept so it can be applied whenever the tree becomes available.
        private bool visible;
        private bool joinable;
        private string joinText;
        private int bossLevel = -1;
        private int raiderCount = -1;
        private float secondsLeft = -1f;
        private float healthFraction = 1f;
        private float maxHealth;

        // What is currently on screen, so nothing is rewritten without a reason.
        private int shownLevel = int.MinValue;
        private int shownRaiders = int.MinValue;
        private int shownSeconds = int.MinValue;
        private bool shownLowTime;
        private long shownHp = long.MinValue;
        private int shownPercentTenths = int.MinValue;
        private float shownFill = -1f;

        private IVisualElementScheduledItem foldAnnouncement;

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            bound = false;
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
            }
        }

        /// <summary>
        /// Forgets what was last drawn, so a freshly built tree gets every value written. Called on
        /// each bind: after a change of language or a live reload the tree is new, and a cache that
        /// still matched the old one left labels empty.
        /// </summary>
        private void ResetShown()
        {
            shownLevel = int.MinValue;
            shownRaiders = int.MinValue;
            shownSeconds = int.MinValue;
            shownLowTime = false;
            shownHp = long.MinValue;
            shownPercentTenths = int.MinValue;
            shownFill = -1f;
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

            hud = root.Q<VisualElement>("raid-hud");
            announce = root.Q<Label>("raid-announce");
            title = root.Q<Label>("raid-title");
            level = root.Q<Label>("raid-level");
            raiders = root.Q<Label>("raid-raiders");
            timer = root.Q<Label>("raid-timer");
            fill = root.Q<VisualElement>("raid-hp-fill");
            ghost = root.Q<VisualElement>("raid-hp-ghost");
            hpText = root.Q<Label>("raid-hp-text");
            percent = root.Q<Label>("raid-percent");
            joinPill = root.Q<VisualElement>("raid-join-pill");
            join = root.Q<Label>("raid-join");

            if (hud == null)
            {
                return;
            }

            ResetShown();
            bound = true;

            if (title != null) title.text = Localization.UI_RAID_TITLE;
            if (announce != null) announce.text = Localization.UI_RAID_BOSS_APPEARED;

            // Force every cached value to be written once against the fresh tree.
            shownLevel = int.MinValue;
            shownRaiders = int.MinValue;
            shownSeconds = int.MinValue;
            shownHp = long.MinValue;
            shownPercentTenths = int.MinValue;
            shownFill = -1f;

            ApplyVisible(announceOnShow: false);
            ApplyJoin();
            ApplyLevel();
            ApplyRaiders();
            ApplyTimer();
            ApplyHealth();
        }

        // ---- Called by RaidNotifications -------------------------------------------------

        /// <summary>
        /// Opens the panel with the announcement line, which folds away after a few seconds.
        /// </summary>
        /// <param name="command">The chat command viewers type to join, including any raid code.</param>
        public void Show(string command)
        {
            visible = true;
            joinable = true;
            joinText = NotificationScreenView.WithCommand(Localization.UI_RAID_JOIN, command);

            // A new raid starts from full health; without this the ghost bar would drain down from
            // wherever the last raid ended.
            healthFraction = 1f;
            shownFill = -1f;

            if (!bound)
            {
                return;
            }

            ApplyVisible(announceOnShow: true);
            ApplyJoin();
            ApplyHealth();
        }

        public void Hide()
        {
            visible = false;
            foldAnnouncement?.Pause();
            if (bound)
            {
                ApplyVisible(announceOnShow: false);
            }
        }

        public void SetJoinable(bool value)
        {
            if (joinable == value)
            {
                return;
            }

            joinable = value;
            if (bound)
            {
                ApplyJoin();
            }
        }

        public void SetLevel(int combatLevel)
        {
            bossLevel = combatLevel;
            if (bound)
            {
                ApplyLevel();
            }
        }

        public void SetRaiders(int count)
        {
            raiderCount = count;
            if (bound)
            {
                ApplyRaiders();
            }
        }

        public void SetTimer(float seconds)
        {
            secondsLeft = seconds;
            if (bound)
            {
                ApplyTimer();
            }
        }

        /// <param name="fraction">Health remaining, 0 to 1.</param>
        /// <param name="max">Maximum health, used for the readout.</param>
        public void SetHealth(float fraction, float max)
        {
            healthFraction = Mathf.Clamp01(fraction);
            maxHealth = max;
            if (bound)
            {
                ApplyHealth();
            }
        }

        // ---- Applying state ------------------------------------------------------------------

        private void ApplyVisible(bool announceOnShow)
        {
            if (!visible)
            {
                hud.style.display = DisplayStyle.None;
                hud.RemoveFromClassList("rf-raid--announce");
                return;
            }

            var wasHidden = hud.style.display == DisplayStyle.None;
            hud.style.display = DisplayStyle.Flex;

            if (wasHidden)
            {
                // Drop in from above. The class is removed a frame later so the transition plays.
                hud.AddToClassList("rf-raid--entering");
                hud.schedule.Execute(() => hud.RemoveFromClassList("rf-raid--entering")).StartingIn(0);
            }

            if (!announceOnShow)
            {
                return;
            }

            hud.AddToClassList("rf-raid--announce");
            foldAnnouncement?.Pause();
            foldAnnouncement = hud.schedule
                .Execute(() => hud.RemoveFromClassList("rf-raid--announce"))
                .StartingIn((long)(announceSeconds * 1000f));
        }

        private void ApplyJoin()
        {
            var show = joinable && !string.IsNullOrEmpty(joinText);
            if (show && join != null)
            {
                join.text = joinText;
            }

            // Hidden by visibility rather than display, so the bottom line keeps its layout and the
            // percentage stays centred once the pill goes away.
            if (joinPill != null)
            {
                joinPill.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void ApplyLevel()
        {
            if (level == null || bossLevel == shownLevel)
            {
                return;
            }

            shownLevel = bossLevel;

            // "LV" in the label's colour, the number in gold, as in the mockup.
            level.text = bossLevel >= 0
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAID_LEVEL,
                    "<color=" + AccentHex + ">" + bossLevel.ToString(CultureInfo.InvariantCulture) + "</color>")
                : string.Empty;
        }

        private void ApplyRaiders()
        {
            if (raiders == null || raiderCount == shownRaiders)
            {
                return;
            }

            shownRaiders = raiderCount;
            if (raiderCount < 0)
            {
                raiders.text = string.Empty;
                return;
            }

            // The count in glacier blue, matching the icon beside it, the rest in the label's colour.
            var count = "<color=" + LiveHex + ">" + raiderCount.ToString(CultureInfo.InvariantCulture) + "</color>";
            raiders.text = raiderCount == 1
                ? Localization.UI_RAID_RAIDER_ONE.Replace("1", count)
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_RAID_RAIDERS, count);
        }

        private void ApplyTimer()
        {
            if (timer == null)
            {
                return;
            }

            var whole = secondsLeft < 0f ? -1 : Mathf.CeilToInt(secondsLeft);
            if (whole != shownSeconds)
            {
                shownSeconds = whole;

                // A clock rather than "897 seconds left": it reads at a glance, where a three digit
                // second count makes the viewer do arithmetic.
                timer.text = whole < 0 ? string.Empty : HudFormat.Clock(whole);
            }

            var low = secondsLeft >= 0f && secondsLeft <= lowTimeSeconds;
            if (low != shownLowTime)
            {
                shownLowTime = low;
                timer.EnableInClassList("rf-raid__timer--low", low);
            }
        }

        private void ApplyHealth()
        {
            if (fill != null && Mathf.Abs(healthFraction - shownFill) >= 0.001f)
            {
                shownFill = healthFraction;
                var width = new Length(healthFraction * 100f, LengthUnit.Percent);
                fill.style.width = width;

                // Same target as the fill; the stylesheet gives it a slower, delayed transition so it
                // trails behind a big hit.
                if (ghost != null)
                {
                    ghost.style.width = width;
                }
            }

            var current = maxHealth > 0f ? (long)Mathf.Round(healthFraction * maxHealth) : 0L;
            var tenths = (int)(healthFraction * 1000f);

            if (hpText != null && current != shownHp)
            {
                shownHp = current;
                hpText.text = maxHealth > 0f
                    ? HudFormat.Grouped(current) + " / " + HudFormat.Grouped((long)maxHealth) + " HP"
                    : string.Empty;
            }

            if (percent != null && tenths != shownPercentTenths)
            {
                shownPercentTenths = tenths;

                // Whole numbers while there is plenty left, a decimal near the end where every tenth
                // of a percent is the story.
                percent.text = tenths >= 100
                    ? (tenths / 10).ToString(CultureInfo.InvariantCulture) + "%"
                    : (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            }
        }
    }
}
