using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The dungeon panel in the top centre status column: joining, then the run, then the boss.
    /// </summary>
    /// <remarks>
    /// Unlike the raid panel, which is pushed values by RaidManager, this one reads DungeonManager on
    /// an interval. A dungeon has more state than a raid (phase, room, who joined, who is alive,
    /// enemies left, boss), and the old notifications only ever received part of it, so polling the
    /// manager keeps everything in one place and in step. DungeonNotifications still tells it when
    /// a dungeon is announced, because the join code is only known at that moment.
    ///
    /// <para>
    /// Labels are only rewritten when the value behind them changes, as in the other HUD views.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class DungeonHudView : MonoBehaviour
    {
        [SerializeField] private DungeonManager dungeon;

        [Tooltip("How often the panel reads the dungeon state.")]
        [SerializeField] private float refreshInterval = 0.1f;

        [Tooltip("How long the 'dungeon has appeared' line stays open before folding away.")]
        [SerializeField] private float announceSeconds = 4f;

        /// <summary>Mirrors --rf-accent, for numbers inside rich text.</summary>
        private const string AccentHex = "#E8A33D";

        private enum Phase { Hidden, Joining, Running }

        private VisualElement hud;
        private Label announce;
        private Label title;
        private Label level;
        private Label timer;
        private VisualElement fill;
        private VisualElement ghost;
        private Label hpText;
        private Label players;
        private Label percent;
        private VisualElement joinPill;
        private Label join;
        private VisualElement enemiesBox;
        private Label enemies;
        private bool bound;

        private string joinText;
        private bool announcePending;
        private float nextRefresh;
        private IVisualElementScheduledItem foldAnnouncement;

        private Phase shownPhase = Phase.Hidden;
        private string shownLevel;
        private int shownSeconds = int.MinValue;
        private string shownPlayers;
        private int shownEnemies = int.MinValue;
        private bool shownBar;
        private float shownFill = -1f;
        private long shownHp = long.MinValue;
        private int shownPercentTenths = int.MinValue;

        private void OnEnable()
        {
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
            shownPhase = Phase.Hidden;
            shownLevel = null;
            shownSeconds = int.MinValue;
            shownPlayers = null;
            shownEnemies = int.MinValue;
            shownBar = false;
            shownFill = -1f;
            shownHp = long.MinValue;
            shownPercentTenths = int.MinValue;
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            hud = root?.Q<VisualElement>("dungeon-hud");
            if (hud == null)
            {
                return;
            }

            announce = root.Q<Label>("dungeon-announce");
            title = root.Q<Label>("dungeon-title");
            level = root.Q<Label>("dungeon-level");
            timer = root.Q<Label>("dungeon-timer");
            fill = root.Q<VisualElement>("dungeon-hp-fill");
            ghost = root.Q<VisualElement>("dungeon-hp-ghost");
            hpText = root.Q<Label>("dungeon-hp-text");
            players = root.Q<Label>("dungeon-players");
            percent = root.Q<Label>("dungeon-percent");
            joinPill = root.Q<VisualElement>("dungeon-join-pill");
            join = root.Q<Label>("dungeon-join");
            enemiesBox = root.Q<VisualElement>("dungeon-enemies-box");
            enemies = root.Q<Label>("dungeon-enemies");

            if (title != null) title.text = Localization.UI_DUNGEON_TITLE;
            if (announce != null) announce.text = Localization.UI_DUNGEON_APPEARED;

            shownPhase = Phase.Hidden;
            hud.style.display = DisplayStyle.None;
            ResetShown();
            bound = true;
        }

        /// <summary>
        /// Called by DungeonNotifications when a dungeon is announced. Opens the panel with the
        /// announcement line, which folds away after a few seconds.
        /// </summary>
        public void Announce(string code)
        {
            var command = string.IsNullOrEmpty(code) ? "!dungeon" : "!dungeon " + code;
            joinText = NotificationScreenView.WithCommand(Localization.UI_DUNGEON_JOIN, command);
            announcePending = true;
            nextRefresh = 0f;
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

            if (!dungeon)
            {
                var gm = GameManager.Instance;
                dungeon = gm != null ? gm.Dungeons : null;
            }

            var phase = !dungeon || !dungeon.Active || !dungeon.Dungeon
                ? Phase.Hidden
                : dungeon.Started ? Phase.Running : Phase.Joining;

            ApplyPhase(phase);

            if (phase == Phase.Joining)
            {
                RefreshJoining();
            }
            else if (phase == Phase.Running)
            {
                RefreshRunning();
            }
        }

        private void ApplyPhase(Phase phase)
        {
            if (phase == shownPhase)
            {
                if (phase != Phase.Hidden && announcePending)
                {
                    StartAnnouncement();
                }

                return;
            }

            var wasHidden = shownPhase == Phase.Hidden;
            shownPhase = phase;

            // A new phase rewrites everything.
            shownLevel = null;
            shownSeconds = int.MinValue;
            shownPlayers = null;
            shownEnemies = int.MinValue;
            shownHp = long.MinValue;
            shownPercentTenths = int.MinValue;
            shownFill = -1f;
            shownBar = true;

            if (phase == Phase.Hidden)
            {
                hud.style.display = DisplayStyle.None;
                hud.RemoveFromClassList("rf-raid--announce");
                foldAnnouncement?.Pause();
                announcePending = false;
                return;
            }

            hud.style.display = DisplayStyle.Flex;
            if (wasHidden)
            {
                hud.AddToClassList("rf-raid--entering");
                hud.schedule.Execute(() => hud.RemoveFromClassList("rf-raid--entering")).StartingIn(0);
            }

            var joining = phase == Phase.Joining;
            SetShown(joinPill, joining && !string.IsNullOrEmpty(joinText));
            if (joining && join != null) join.text = joinText;
            SetShown(enemiesBox, !joining);
            SetBar(false);

            if (percent != null) percent.text = string.Empty;

            if (announcePending)
            {
                StartAnnouncement();
            }
        }

        private void StartAnnouncement()
        {
            announcePending = false;
            hud.AddToClassList("rf-raid--announce");
            foldAnnouncement?.Pause();
            foldAnnouncement = hud.schedule
                .Execute(() => hud.RemoveFromClassList("rf-raid--announce"))
                .StartingIn((long)(announceSeconds * 1000f));
        }

        private void RefreshJoining()
        {
            var boss = dungeon.Boss;
            var bossLevel = boss && boss.Enemy ? boss.Enemy.Stats.CombatLevel : 0;
            SetLevel(bossLevel > 0
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAID_LEVEL, Gold(bossLevel))
                : string.Empty);

            SetTimer(Mathf.CeilToInt(dungeon.SecondsUntilStart));

            var joined = dungeon.GetPlayers().Count;
            SetPlayers(string.Format(CultureInfo.InvariantCulture, Localization.UI_DUNGEON_JOINED, Live(joined)));
        }

        private void RefreshRunning()
        {
            var d = dungeon.Dungeon;
            var inBossRoom = d.Room != null && d.Room == d.BossRoom;
            var roomCount = d.Rooms != null ? d.Rooms.Length : 0;

            SetLevel(inBossRoom || roomCount <= 0
                ? Localization.UI_DUNGEON_BOSS_ROOM
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_DUNGEON_ROOM, Gold(d.CurrentRoomIndex + 1), Gold(roomCount)));

            SetTimer((int)dungeon.Elapsed.TotalSeconds);

            var joined = dungeon.GetPlayers().Count;
            var alive = dungeon.GetAlivePlayerCount();
            SetPlayers(string.Format(CultureInfo.InvariantCulture, Localization.UI_DUNGEON_ALIVE, Live(alive), joined));

            var enemyCount = dungeon.GetAliveEnemyCount();
            if (enemyCount != shownEnemies && enemies != null)
            {
                shownEnemies = enemyCount;
                enemies.text = enemyCount == 1
                    ? Localization.UI_DUNGEON_ENEMY_ONE
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_DUNGEON_ENEMIES, enemyCount);
            }

            var boss = dungeon.Boss;
            var stats = boss && boss.Enemy ? boss.Enemy.Stats : null;
            // Level, not MaxLevel: the boss's health is scaled to the group by setting Level and
            // CurrentValue directly, which leaves MaxLevel at the unscaled value. The old bar used
            // Level too.
            var max = stats != null ? (float)stats.Health.Level : 0f;
            var current = stats != null ? Mathf.Max(0f, stats.Health.CurrentValue) : 0f;

            // The bar appears in the boss room, or as soon as the boss is hurt, whichever is first.
            var showBar = stats != null && max > 0f && (inBossRoom || current < max);
            SetBar(showBar);
            if (showBar)
            {
                SetHealth(current / max, current, max);
            }
        }

        private void SetBar(bool value)
        {
            if (value == shownBar)
            {
                return;
            }

            shownBar = value;
            hud.EnableInClassList("rf-raid--no-bar", !value);
            if (!value && percent != null)
            {
                percent.text = string.Empty;
                shownPercentTenths = int.MinValue;
            }
        }

        private void SetHealth(float fraction, float current, float max)
        {
            fraction = Mathf.Clamp01(fraction);
            if (fill != null && Mathf.Abs(fraction - shownFill) >= 0.001f)
            {
                shownFill = fraction;
                var width = new Length(fraction * 100f, LengthUnit.Percent);
                fill.style.width = width;
                if (ghost != null) ghost.style.width = width;
            }

            var hp = (long)current;
            if (hpText != null && hp != shownHp)
            {
                shownHp = hp;
                hpText.text = HudFormat.Grouped(hp) + " / " + HudFormat.Grouped((long)max) + " HP";
            }

            var tenths = (int)(fraction * 1000f);
            if (percent != null && tenths != shownPercentTenths)
            {
                shownPercentTenths = tenths;
                percent.text = tenths >= 100
                    ? (tenths / 10).ToString(CultureInfo.InvariantCulture) + "%"
                    : (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            }
        }

        private void SetLevel(string text)
        {
            if (level != null && text != shownLevel)
            {
                shownLevel = text;
                level.text = text;
            }
        }

        private void SetTimer(int seconds)
        {
            if (timer != null && seconds != shownSeconds)
            {
                shownSeconds = seconds;
                timer.text = HudFormat.Clock(seconds);
            }
        }

        private void SetPlayers(string text)
        {
            if (players != null && text != shownPlayers)
            {
                shownPlayers = text;
                players.text = text;
            }
        }

        private static string Gold(int value)
        {
            return "<color=" + AccentHex + ">" + value.ToString(CultureInfo.InvariantCulture) + "</color>";
        }

        /// <summary>Glacier blue, matching the people icon beside the count.</summary>
        private static string Live(int value)
        {
            return "<color=#5FA8C7>" + value.ToString(CultureInfo.InvariantCulture) + "</color>";
        }

        private static void SetShown(VisualElement element, bool value)
        {
            if (element != null)
            {
                element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
