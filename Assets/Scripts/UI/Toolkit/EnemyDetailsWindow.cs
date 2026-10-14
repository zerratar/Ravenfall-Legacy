using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Target frame for an enemy clicked in free camera mode: name, level, health, combat skills,
    /// gear stats and its record against players.
    /// </summary>
    /// <remarks>
    /// <c>EnemyObserveDialog</c> stays the entry point for FreeCamera and forwards here when
    /// assigned. Health refreshes every frame, as a target frame should; everything else only when
    /// it changes.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class EnemyDetailsWindow : MonoBehaviour
    {
        [Header("Icons")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite defenseIcon;
        [SerializeField] private Sprite strengthIcon;
        [SerializeField] private Sprite armorIcon;
        [SerializeField] private Sprite meleeIcon;

        /// <summary>Raised when the close button is pressed.</summary>
        public event Action CloseRequested;

        private sealed class Stat
        {
            public Label Value;
            public string Shown;
        }

        private VisualElement window;
        private Label nameLabel;
        private Label level;
        private VisualElement healthFill;
        private Label health;
        private Label kills;
        private Label deaths;
        private Stat attack, defense, strength, armor, power, aim;
        private DraggableWindow drag;
        private bool bound;

        private EnemyController target;
        private float shownFill = -1f;
        private int shownHp = int.MinValue;
        private int shownKills = int.MinValue;
        private int shownDeaths = int.MinValue;

        public bool IsVisible => target;

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
            shownFill = -1f;
            shownHp = int.MinValue;
            shownKills = int.MinValue;
            shownDeaths = int.MinValue;
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            window = root?.Q<VisualElement>("enemy-window");
            if (window == null)
            {
                return;
            }

            nameLabel = root.Q<Label>("enemy-name");
            level = root.Q<Label>("enemy-level");
            healthFill = root.Q<VisualElement>("enemy-health-fill");
            health = root.Q<Label>("enemy-health");
            kills = root.Q<Label>("enemy-kills");
            deaths = root.Q<Label>("enemy-deaths");

            var stats = root.Q<VisualElement>("enemy-stats");
            if (stats != null)
            {
                stats.Clear();
                attack = AddStat(stats, Localization.UI_ENEMY_ATTACK, attackIcon);
                defense = AddStat(stats, Localization.UI_ENEMY_DEFENSE, defenseIcon);
                strength = AddStat(stats, Localization.UI_ENEMY_STRENGTH, strengthIcon);
                armor = AddStat(stats, Localization.UI_ENEMY_ARMOR, armorIcon);
                power = AddStat(stats, Localization.UI_ENEMY_POWER, meleeIcon);
                aim = AddStat(stats, Localization.UI_ENEMY_AIM, meleeIcon);
            }

            var close = root.Q<Button>("enemy-close");
            if (close != null) close.clicked += () => CloseRequested?.Invoke();

            var header = root.Q<VisualElement>("enemy-header");
            if (header != null)
            {
                drag = DraggableWindow.Attach(window, header, "enemydetails");
                // wider only: the height follows what is shown
                drag?.EnableResize(root.Q<VisualElement>("enemy-grip"), new Vector2(340f, 0f), widthOnly: true);
                drag?.EnableLeftResize(root.Q<VisualElement>("enemy-grip-left"));
            }

            ResetShown();
            bound = true;
            window.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Stat AddStat(VisualElement container, string caption, Sprite icon)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-enemy__stat");

            var iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.AddToClassList("rf-enemy__stat-icon");
            if (icon != null) iconElement.style.backgroundImage = new StyleBackground(icon);

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            var captionLabel = new Label(caption) { pickingMode = PickingMode.Ignore };
            captionLabel.AddToClassList("rf-enemy__stat-caption");
            var value = new Label { pickingMode = PickingMode.Ignore };
            value.AddToClassList("rf-enemy__stat-value");
            text.Add(captionLabel);
            text.Add(value);

            root.Add(iconElement);
            root.Add(text);
            container.Add(root);
            return new Stat { Value = value };
        }

        // ---- Called by EnemyObserveDialog ----------------------------------------------------

        public void Show(EnemyController enemy)
        {
            target = enemy;
            shownFill = -1f;
            shownHp = int.MinValue;
            shownKills = int.MinValue;
            shownDeaths = int.MinValue;
            if (attack != null) { attack.Shown = defense.Shown = strength.Shown = armor.Shown = power.Shown = aim.Shown = null; }

            if (bound)
            {
                window.style.display = enemy ? DisplayStyle.Flex : DisplayStyle.None;
                if (enemy) Refresh();
            }
        }

        public void Hide()
        {
            target = null;
            if (bound)
            {
                window.style.display = DisplayStyle.None;
            }
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (!target)
            {
                if (window.style.display == DisplayStyle.Flex) window.style.display = DisplayStyle.None;
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            var s = target.Stats;
            if (s == null)
            {
                return;
            }

            SetText(nameLabel, target.Name.Replace("_", " "));
            SetText(level, string.Format(CultureInfo.InvariantCulture, Localization.UI_ENEMY_LEVEL, (int)s.CombatLevel));

            // Level rather than MaxLevel: scaled enemies set Level and CurrentValue directly.
            int max = s.Health.Level;
            int hp = Mathf.Max(0, s.Health.CurrentValue);
            var fill = max > 0 ? Mathf.Clamp01(hp / (float)max) : 0f;
            if (Mathf.Abs(fill - shownFill) >= 0.002f)
            {
                shownFill = fill;
                healthFill.style.width = new Length(fill * 100f, LengthUnit.Percent);
            }

            if (hp != shownHp)
            {
                shownHp = hp;
                health.text = HudFormat.Grouped(hp) + " / " + HudFormat.Grouped(max) + " HP";
            }

            Set(attack, s.Attack.Level);
            Set(defense, s.Defense.Level);
            Set(strength, s.Strength.Level);

            var eq = target.EquipmentStats ?? EquipmentStats.Zero;
            Set(armor, eq.ArmorPower);
            Set(power, eq.WeaponPower);
            Set(aim, eq.WeaponAim);

            if (target.PlayerKillCount != shownKills)
            {
                shownKills = target.PlayerKillCount;
                kills.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_ENEMY_KILLS, HudFormat.Grouped(shownKills));
            }

            if (target.DeathCount != shownDeaths)
            {
                shownDeaths = target.DeathCount;
                deaths.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_ENEMY_DEATHS, HudFormat.Grouped(shownDeaths));
            }
        }

        private static void Set(Stat stat, int value)
        {
            if (stat == null) return;
            var text = HudFormat.Grouped(value);
            if (text != stat.Shown)
            {
                stat.Shown = text;
                stat.Value.text = text;
            }
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
