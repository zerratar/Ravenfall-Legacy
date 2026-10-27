using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
// Aliased rather than importing RavenNest.Models wholesale; the namespace also carries names that
// collide with the global Utility and GameMath helpers.
using Skill = RavenNest.Models.Skill;
using ItemType = RavenNest.Models.ItemType;
using ItemCategory = RavenNest.Models.ItemCategory;

namespace Shinobytes.UI
{
    /// <summary>
    /// The observed player's details, rebuilt on UI Toolkit: identity, what they are training, every
    /// skill, and their gear with a live preview.
    /// </summary>
    /// <remarks>
    /// <c>PlayerDetails</c> stays the controller. It decides who is observed and for how long, and
    /// forwards to this window when it is assigned. This class only draws.
    ///
    /// <para>
    /// The old panel spread its work over a dozen components, each with its own Update: seven skill
    /// observers rebuilding labels whenever experience moved, resource observers formatting every
    /// frame, and a training block rebuilt ten times a second. Here one refresh walks everything on
    /// an interval, and every element keeps the last value it showed, so nothing is rewritten unless
    /// what it displays changed.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerDetailsWindow : MonoBehaviour
    {
        [Serializable]
        public struct SkillIcon
        {
            public Skill Skill;
            public Sprite Icon;
        }

        [Serializable]
        public struct SlotIcon
        {
            public ItemSlot Slot;
            public Sprite Icon;
        }

        [Tooltip("How often the header, training card and skill tiles refresh.")]
        [SerializeField] private float refreshInterval = 0.25f;

        [Tooltip("How often gear stats, scroll counts and clan skills refresh. They change rarely.")]
        [SerializeField] private float slowRefreshInterval = 1f;

        [Tooltip("Degrees the preview turns per button press.")]
        [SerializeField] private float turnDegrees = 45f;
        [Tooltip("Degrees the preview turns per pixel dragged.")]
        [SerializeField] private float previewTurnSpeed = 0.5f;

        [Header("Icons")]
        [SerializeField] private SkillIcon[] skillIcons = Array.Empty<SkillIcon>();
        [SerializeField] private Sprite enchantingIcon;
        [SerializeField] private SlotIcon[] slotIcons = Array.Empty<SlotIcon>();
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite armorIcon;
        [SerializeField] private Sprite meleeIcon;
        [SerializeField] private Sprite rangedIcon;
        [SerializeField] private Sprite magicIcon;
        [SerializeField] private Sprite expScrollIcon;
        [SerializeField] private Sprite raidScrollIcon;
        [SerializeField] private Sprite dungeonScrollIcon;

        [Header("Preview")]
        [Tooltip("What the face camera renders into.")]
        [SerializeField] private Texture previewTexture;
        [SerializeField] private PlayerObserveCamera observeCamera;

        /// <summary>Raised when the close button is pressed. PlayerDetails hides itself in response.</summary>
        public event Action CloseRequested;

        private const string GearOpenKey = "rf_details_gear_open";

        /// <summary>
        /// Skill tiles, column by column. Filled top to bottom so related skills sit together: combat
        /// first, then support, then gathering and crafting. A <see cref="Skill.None"/> entry is the
        /// clan skill, which is not part of the player's own stats.
        /// </summary>
        private static readonly Skill[][] SkillColumns =
        {
            new[] { Skill.Attack, Skill.Defense, Skill.Strength, Skill.Health, Skill.Magic },
            new[] { Skill.Ranged, Skill.Healing, Skill.Alchemy, Skill.None, Skill.Woodcutting },
            new[] { Skill.Fishing, Skill.Mining, Skill.Crafting, Skill.Cooking, Skill.Farming },
            new[] { Skill.Slayer, Skill.Sailing, Skill.Gathering },
        };

        /// <summary>Equipment grid, three to a row.</summary>
        private static readonly ItemSlot[] SlotOrder =
        {
            ItemSlot.Head, ItemSlot.Amulet, ItemSlot.Ring,
            ItemSlot.MeleeWeapon, ItemSlot.Chest, ItemSlot.Shield,
            ItemSlot.Gloves, ItemSlot.Leggings, ItemSlot.Pet,
            ItemSlot.Boots, ItemSlot.RangedWeapon, ItemSlot.MagicWeapon,
        };

        private sealed class SkillTile
        {
            public Skill Skill;
            public bool IsClan;
            public VisualElement Root;
            public VisualElement Icon;
            public Label Lock;
            public Label Name;
            public Label Level;
            public VisualElement Fill;

            public SkillStat Stat;
            public int LastCurrent = int.MinValue;
            public int LastLevel = int.MinValue;
            public float LastProgress = -1f;
            public bool LastActive;
            public int LastLocked = -1;
        }

        private sealed class SlotView
        {
            public ItemSlot Slot;
            public VisualElement Root;
            public VisualElement Image;
            public Sprite Placeholder;
            public GameInventoryItem Item;
        }

        private enum StatKind { Armor, Melee, Ranged, Magic }

        private sealed class StatTile
        {
            public StatKind Kind;
            public VisualElement Root;
            public Label Power;
            public Label Aim;
            public int LastPower = int.MinValue;
            public int LastAim = int.MinValue;
            public int LastBonus = int.MinValue;
            public int LastState = -1;
        }

        private sealed class ScrollChip
        {
            public string Match;
            public VisualElement Root;
            public Label Count;
            public long Last = long.MinValue;
        }

        private readonly List<SkillTile> skillTiles = new List<SkillTile>();
        private readonly List<SlotView> slots = new List<SlotView>();
        private readonly List<StatTile> statTiles = new List<StatTile>();
        private readonly List<ScrollChip> scrolls = new List<ScrollChip>();
        private readonly Dictionary<Skill, Sprite> iconBySkill = new Dictionary<Skill, Sprite>();

        private VisualElement window;
        private VisualElement header;
        private Label nameLabel;
        private Label clanLabel;
        private VisualElement coinIconElement;
        private Label coinLabel;
        private Label levelLabel;
        private Button collapseButton;
        private Button closeButton;
        private VisualElement observeFill;
        private readonly Dictionary<string, Label> badges = new Dictionary<string, Label>();
        private Label restedLabel;

        private VisualElement training;
        private VisualElement trainingIcon;
        private Label trainingGlyph;
        private VisualElement trainingBar;
        private Label trainingTitle;
        private Label trainingPercent;
        private VisualElement trainingFill;
        private Label trainingStatus;
        private Label trainingNext;
        private Label trainingRate;
        private Label trainingHint;

        private Button gearToggle;
        private VisualElement previewImage;
        private Button turnLeft;
        private Button turnRight;

        private DraggableWindow drag;
        private HudTooltip tooltip;
        private bool bound;

        private PlayerController player;
        private bool visible = true;
        private bool gearOpen;
        private float nextRefresh;
        private float nextSlowRefresh;
        private SkillStat clanEnchanting;

        // Last shown values for the header and training card.
        private string shownName;
        private string shownColor;
        private int shownLevel = int.MinValue;
        private long shownCoins = long.MinValue;
        private int shownRestedMinutes = int.MinValue;
        private bool shownRestedGaining;
        private Sprite shownTrainingIcon;
        private string shownTrainingClass;
        private float shownTrainingFill = -1f;
        private float shownObserve = -1f;

        // ---- Lifecycle -------------------------------------------------------------------

        private void Awake()
        {
            for (var i = 0; i < skillIcons.Length; ++i)
            {
                iconBySkill[skillIcons[i].Skill] = skillIcons[i].Icon;
            }

            gearOpen = PlayerPrefs.GetInt(GearOpenKey, 0) == 1;
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

            if (!IsShowing || (drag != null && drag.IsCollapsed))
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now >= nextSlowRefresh)
            {
                nextSlowRefresh = now + slowRefreshInterval;
                RefreshSlow();
            }

            if (now >= nextRefresh)
            {
                nextRefresh = now + refreshInterval;
                RefreshFast();
            }
        }

        private bool IsShowing => visible && player;

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

            window = root.Q<VisualElement>("details-window");
            if (window == null)
            {
                return;
            }

            var levelCaption = root.Q<Label>("details-level-caption");
            if (levelCaption != null) levelCaption.text = Localization.UI_DETAILS_LEVEL_CAPTION;
            var gearTitle = root.Q<Label>("gear-title");
            if (gearTitle != null) gearTitle.text = Localization.UI_DETAILS_EQUIPMENT;

            header = root.Q<VisualElement>("details-header");
            nameLabel = root.Q<Label>("details-name");
            clanLabel = root.Q<Label>("details-clan");
            coinIconElement = root.Q<VisualElement>("details-coin-icon");
            coinLabel = root.Q<Label>("details-coin-amount");
            levelLabel = root.Q<Label>("details-level");
            collapseButton = root.Q<Button>("details-collapse");
            closeButton = root.Q<Button>("details-close");
            observeFill = root.Q<VisualElement>("details-observe-fill");
            restedLabel = root.Q<Label>("details-rested");

            badges.Clear();
            foreach (var id in new[] { "broadcaster", "moderator", "subscriber", "vip", "dev" })
            {
                var badge = root.Q<Label>("badge-" + id);
                if (badge != null) badge.text = BadgeText(id);
                if (badge != null)
                {
                    badges[id] = badge;
                    badge.style.display = DisplayStyle.None;
                }
            }

            training = root.Q<VisualElement>("details-training");
            trainingIcon = root.Q<VisualElement>("training-icon");
            trainingGlyph = root.Q<Label>("training-glyph");
            trainingBar = root.Q<VisualElement>("training-bar");
            trainingTitle = root.Q<Label>("training-title");
            trainingPercent = root.Q<Label>("training-percent");
            trainingFill = root.Q<VisualElement>("training-fill");
            trainingStatus = root.Q<Label>("training-status");
            trainingNext = root.Q<Label>("training-next");
            trainingRate = root.Q<Label>("training-rate");
            trainingHint = root.Q<Label>("training-hint");

            gearToggle = root.Q<Button>("details-gear-toggle");
            previewImage = root.Q<VisualElement>("details-preview-image");
            turnLeft = root.Q<Button>("details-turn-left");
            turnRight = root.Q<Button>("details-turn-right");

            tooltip = HudTooltip.Create(root.Q<VisualElement>("details-root") ?? root);

            if (coinIconElement != null && coinIcon != null)
            {
                coinIconElement.style.backgroundImage = new StyleBackground(coinIcon);
            }

            BuildSkills(root.Q<VisualElement>("details-skills"));
            BuildSlots(root.Q<VisualElement>("details-slots"));
            BuildStats(root.Q<VisualElement>("details-stats"));
            BuildScrolls(root.Q<VisualElement>("details-scrolls"));
            ApplyPreviewTexture();

            if (header != null)
            {
                drag = DraggableWindow.Attach(window, header, "playerdetails");
                // wider only: the height follows what is shown
                drag?.EnableResize(root.Q<VisualElement>("details-grip"), new Vector2(520f, 0f), widthOnly: true);
                drag?.EnableLeftResize(root.Q<VisualElement>("details-grip-left"));
            }

            if (collapseButton != null) collapseButton.clicked += OnCollapseClicked;
            if (closeButton != null) closeButton.clicked += () => CloseRequested?.Invoke();
            if (gearToggle != null) gearToggle.clicked += () => ToggleGear();
            if (turnLeft != null) turnLeft.clicked += () => Turn(-turnDegrees);
            if (turnRight != null) turnRight.clicked += () => Turn(turnDegrees);
            BindPreviewControls(root.Q<VisualElement>("details-preview"), root.Q<Button>("details-preview-reset"));

            bound = true;
            ApplyGearOpen();
            ResetShown();
            ApplyVisibility();
        }

        // ---- Called by PlayerDetails ---------------------------------------------------------

        /// <summary>Shows <paramref name="target"/>, or hides the window when null.</summary>
        public void Observe(PlayerController target)
        {
            player = target;
            clanEnchanting = null;
            ResetShown();
            ApplyVisibility();

            if (bound && IsShowing)
            {
                RefreshNow();
            }
        }

        /// <summary>The F2 and settings toggle. Independent of whether anyone is observed.</summary>
        public void SetVisible(bool value)
        {
            visible = value;
            tooltip?.Hide();
            ApplyVisibility();
            if (bound && IsShowing)
            {
                RefreshNow();
            }
        }

        /// <summary>Drives the thin line under the header that drains until the camera moves on.</summary>
        public void SetObserveTime(float secondsLeft, float secondsTotal)
        {
            if (observeFill == null)
            {
                return;
            }

            var fraction = secondsTotal > 0f ? Mathf.Clamp01(secondsLeft / secondsTotal) : 0f;
            if (Mathf.Abs(fraction - shownObserve) < 0.002f)
            {
                return;
            }

            shownObserve = fraction;
            observeFill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
        }

        /// <summary>Called when the observed player's gear changes.</summary>
        public void EquipmentChanged()
        {
            if (bound && IsShowing)
            {
                RefreshSlots();
                RefreshStats();
            }
        }

        /// <summary>The old inventory toggle, kept so existing callers keep working.</summary>
        public bool ToggleGear()
        {
            gearOpen = !gearOpen;
            PlayerPrefs.SetInt(GearOpenKey, gearOpen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyGearOpen();

            if (gearOpen && bound && IsShowing)
            {
                RefreshSlots();
                RefreshStats();
            }

            return gearOpen;
        }

        public bool IsGearOpen => gearOpen;

        // ---- Building ------------------------------------------------------------------------

        private void BuildSkills(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            skillTiles.Clear();

            foreach (var column in SkillColumns)
            {
                var col = new VisualElement();
                col.AddToClassList("rf-skills__column");

                foreach (var skill in column)
                {
                    var tile = MakeSkillTile(skill);
                    col.Add(tile.Root);
                    skillTiles.Add(tile);
                }

                container.Add(col);
            }
        }

        private SkillTile MakeSkillTile(Skill skill)
        {
            var isClan = skill == Skill.None;
            var tile = new SkillTile { Skill = skill, IsClan = isClan };

            tile.Root = new VisualElement();
            tile.Root.AddToClassList("rf-skill");

            tile.Icon = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.Icon.AddToClassList("rf-skill__icon");
            var sprite = isClan ? enchantingIcon : IconFor(skill);
            if (sprite != null)
            {
                tile.Icon.style.backgroundImage = new StyleBackground(sprite);
            }

            // Shown instead of the icon while the clan skill is unavailable.
            tile.Lock = new Label("") { pickingMode = PickingMode.Ignore };
            tile.Lock.AddToClassList("rf-icon");
            tile.Lock.AddToClassList("rf-skill__lock");
            tile.Lock.style.display = DisplayStyle.None;

            var info = new VisualElement { pickingMode = PickingMode.Ignore };
            info.AddToClassList("rf-skill__info");

            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("rf-skill__line");

            tile.Name = new Label(isClan ? Localization.UI_SKILL_ENCHANTING : Localization.SkillName(skill)) { pickingMode = PickingMode.Ignore };
            tile.Name.AddToClassList("rf-skill__name");

            tile.Level = new Label { pickingMode = PickingMode.Ignore };
            tile.Level.AddToClassList("rf-skill__level");

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rf-skill__bar");
            tile.Fill = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.Fill.AddToClassList("rf-skill__fill");
            bar.Add(tile.Fill);

            // Name on its own line, with the bar and level under it. Side by side, a 17px level left
            // about five letters for the name, so most skills read "Atta..." and "Fishi...".
            line.Add(bar);
            line.Add(tile.Level);

            info.Add(tile.Name);
            info.Add(line);

            tile.Root.Add(tile.Icon);
            tile.Root.Add(tile.Lock);
            tile.Root.Add(info);

            tooltip?.Attach(tile.Root, t => FillSkillTooltip(t, tile));
            return tile;
        }

        private void BuildSlots(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            slots.Clear();

            for (var i = 0; i < SlotOrder.Length; ++i)
            {
                var view = new SlotView { Slot = SlotOrder[i], Placeholder = SlotPlaceholder(SlotOrder[i]) };

                view.Root = new VisualElement();
                view.Root.AddToClassList("rf-slot");
                if (i % 3 == 2)
                {
                    view.Root.AddToClassList("rf-slot--row-end");
                }

                view.Image = new VisualElement { pickingMode = PickingMode.Ignore };
                view.Image.AddToClassList("rf-slot__image");
                view.Root.Add(view.Image);

                tooltip?.Attach(view.Root, t => FillItemTooltip(t, view));
                container.Add(view.Root);
                slots.Add(view);
                ShowSlot(view, null);
            }
        }

        private void BuildStats(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            statTiles.Clear();

            AddStat(container, StatKind.Armor, Localization.UI_DETAILS_STAT_ARMOR, armorIcon);
            AddStat(container, StatKind.Melee, Localization.UI_DETAILS_STAT_MELEE, meleeIcon);
            AddStat(container, StatKind.Ranged, Localization.UI_DETAILS_STAT_RANGED, rangedIcon);
            AddStat(container, StatKind.Magic, Localization.UI_DETAILS_STAT_MAGIC, magicIcon).Root.AddToClassList("rf-stat--last");
        }

        private StatTile AddStat(VisualElement container, StatKind kind, string caption, Sprite icon)
        {
            var tile = new StatTile { Kind = kind };

            tile.Root = new VisualElement();
            tile.Root.AddToClassList("rf-stat");

            var iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.AddToClassList("rf-stat__icon");
            if (icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(icon);
            }

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-stat__text");

            var captionLabel = new Label(caption) { pickingMode = PickingMode.Ignore };
            captionLabel.AddToClassList("rf-stat__caption");

            tile.Power = new Label { pickingMode = PickingMode.Ignore };
            tile.Power.AddToClassList("rf-stat__power");

            tile.Aim = new Label { pickingMode = PickingMode.Ignore };
            tile.Aim.AddToClassList("rf-stat__aim");
            if (kind == StatKind.Armor)
            {
                tile.Aim.style.display = DisplayStyle.None;
            }

            text.Add(captionLabel);
            text.Add(tile.Power);
            text.Add(tile.Aim);
            tile.Root.Add(iconElement);
            tile.Root.Add(text);

            tooltip?.Attach(tile.Root, t => FillStatTooltip(t, tile));
            container.Add(tile.Root);
            statTiles.Add(tile);
            return tile;
        }

        private void BuildScrolls(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            container.Clear();
            scrolls.Clear();

            AddScroll(container, "exp", Localization.UI_DETAILS_SCROLL_EXP, expScrollIcon);
            AddScroll(container, "raid", Localization.UI_DETAILS_SCROLL_RAID, raidScrollIcon);
            AddScroll(container, "dungeon", Localization.UI_DETAILS_SCROLL_DUNGEON, dungeonScrollIcon);
        }

        private void AddScroll(VisualElement container, string match, string title, Sprite icon)
        {
            var chip = new ScrollChip { Match = match };

            chip.Root = new VisualElement();
            chip.Root.AddToClassList("rf-scroll-chip");

            var iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
            iconElement.AddToClassList("rf-scroll-chip__icon");
            if (icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(icon);
            }

            chip.Count = new Label("0") { pickingMode = PickingMode.Ignore };
            chip.Count.AddToClassList("rf-scroll-chip__count");

            chip.Root.Add(iconElement);
            chip.Root.Add(chip.Count);

            tooltip?.Attach(chip.Root, t =>
            {
                t.SetTitle(title);
                t.AddRow(Localization.UI_DETAILS_OWNED, HudFormat.Grouped(Math.Max(0, chip.Last)));
            });

            container.Add(chip.Root);
            scrolls.Add(chip);
        }

        // ---- Refreshing ------------------------------------------------------------------------

        private void RefreshNow()
        {
            nextRefresh = 0f;
            nextSlowRefresh = 0f;
            RefreshSlow();
            RefreshFast();
            RefreshSlots();
        }

        /// <summary>Header, training card and skill tiles.</summary>
        private void RefreshFast()
        {
            if (!player || player.Removed)
            {
                return;
            }

            RefreshHeader();
            RefreshTraining();
            RefreshSkills();
        }

        /// <summary>Gear stats, scroll counts and the clan skill.</summary>
        private void RefreshSlow()
        {
            if (!player || player.Removed)
            {
                return;
            }

            RefreshClanSkill();
            RefreshScrolls();
            RefreshStats();
        }

        private void RefreshHeader()
        {
            var name = player.CharacterIndex > 0
                ? player.PlayerName + " #" + player.CharacterIndex.ToString(CultureInfo.InvariantCulture)
                : player.PlayerName;
            if (name != shownName)
            {
                shownName = name;
                nameLabel.text = name;
            }

            if (!string.Equals(shownColor, player.PlayerNameHexColor, StringComparison.Ordinal))
            {
                shownColor = player.PlayerNameHexColor;
                nameLabel.style.color = HudFormat.ReadableNameColor(shownColor);
            }

            var clan = player.clanHandler != null && player.clanHandler.InClan && player.clanHandler.ClanInfo != null
                ? "<" + player.clanHandler.ClanInfo.Name + ">"
                : string.Empty;
            SetText(clanLabel, clan);
            SetShown(clanLabel, clan.Length > 0);

            var level = (int)player.Stats.CombatLevel;
            if (level != shownLevel)
            {
                shownLevel = level;
                levelLabel.text = level.ToString(CultureInfo.InvariantCulture);
            }

            var coins = player.Resources != null ? (long)player.Resources.Coins : 0L;
            if (coins != shownCoins)
            {
                shownCoins = coins;
                coinLabel.text = HudFormat.Compact(coins);
            }

            SetBadge("broadcaster", player.IsBroadcaster);
            SetBadge("moderator", player.IsModerator);
            SetBadge("subscriber", player.IsSubscriber);
            SetBadge("vip", player.IsVip);
            SetBadge("dev", player.IsGameAdmin);

            RefreshRested();
        }

        private void RefreshRested()
        {
            if (restedLabel == null)
            {
                return;
            }

            var seconds = player.Rested != null ? player.Rested.RestedTime : 0d;
            var minutes = seconds > 0 ? (int)(seconds / 60d) : 0;
            var gaining = player.onsenHandler != null && player.onsenHandler.InOnsen;

            if (minutes == shownRestedMinutes && gaining == shownRestedGaining)
            {
                return;
            }

            shownRestedMinutes = minutes;
            shownRestedGaining = gaining;

            if (seconds <= 0 && !gaining)
            {
                restedLabel.style.display = DisplayStyle.None;
                return;
            }

            // under a minute reads "<1m": "Rested 0m" looked like no rest at all
            var time = minutes >= 60
                ? HudFormat.Duration(minutes * 60)
                : minutes >= 1 ? minutes.ToString(CultureInfo.InvariantCulture) + "m" : "<1m";
            restedLabel.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTED, time);
            restedLabel.EnableInClassList("rf-details__rested--gaining", gaining);
            restedLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// The training card. The same cases as the old panel, in the same order, so the two never
        /// disagree about what a player is doing: target reached, max level, wrong island for their
        /// level, ineligible, and otherwise the time to the next level.
        /// </summary>
        private void RefreshTraining()
        {
            if (player.onsenHandler && player.onsenHandler.InOnsen)
            {
                ShowResting();
                return;
            }

            var isSailing = player.ferryHandler && player.ferryHandler.OnFerry;
            var task = player.GetTask();
            var isTraining = task != TaskType.None || !string.IsNullOrEmpty(player.GetTaskArgument()) || isSailing;

            SkillStat skill = null;
            string skillName = null;
            var trainingAll = false;

            if (isSailing)
            {
                skill = player.Stats.Sailing;
                skillName = Localization.SkillName(Skill.Sailing);
            }
            else if (isTraining)
            {
                try
                {
                    skill = player.GetActiveSkillStat();
                }
                catch
                {
                    skill = null;
                }

                if (skill != null)
                {
                    trainingAll = skill == player.GetSkill(Skill.Health);
                    skillName = trainingAll ? Localization.UI_SKILL_ALL : Localization.SkillName(skill.Name);
                }
            }

            if (skill == null)
            {
                // Nothing to measure, so no bar: just what to do about it.
                var idleHint = NotificationScreenView.WithCommand(Localization.UI_DETAILS_IDLE_HINT, "!train [skill]");
                ShowTraining("rf-training--idle", null, Localization.UI_DETAILS_IDLE,
                    string.Empty, -1f, idleHint, null, string.Empty, string.Empty, string.Empty, "\uf075");
                return;
            }

            int skillLevel = skill.Level;
            var nextLevelExp = GameMath.ExperienceForLevel(skillLevel + 1);
            var progress = skill.Experience > 0 && nextLevelExp > 0 ? Mathf.Clamp01((float)(skill.Experience / nextLevelExp)) : 0f;
            var percent = (progress * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            var title = Localization.UI_DETAILS_TRAINING + " · " + skillName.ToUpperInvariant()
                + (trainingAll ? string.Empty : "  <color=#E8A33D>" + skillLevel.ToString(CultureInfo.InvariantCulture) + "</color>");
            var icon = trainingAll ? IconFor(Skill.Attack) : IconFor(skill.Type);

            var perHour = skill.GetExperiencePerHour();
            var rate = perHour > 0 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RATE_PER_HOUR, HudFormat.Compact(perHour)) : string.Empty;

            var recommended = IslandManager.GetSuitableIsland(skillLevel);
            var currentIsland = player.Island ? player.Island.Island : RavenNest.Models.Island.Ferry;
            var state = player.CurrentExpGainState;

            if (state == ExpGainState.TargetLevelReached)
            {
                ShowTraining("rf-training--done", icon, title, percent, progress,
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_TARGET_REACHED, player.AutoTrainTargetLevel),
                    "rf-training__status--done", string.Empty, string.Empty, string.Empty);
                return;
            }

            if (skillLevel >= GameMath.MaxLevel)
            {
                ShowTraining(null, icon, title, string.Empty, 1f, Localization.UI_DETAILS_MAX_LEVEL,
                    "rf-training__status--max", string.Empty, string.Empty, string.Empty);
                return;
            }

            if ((state == ExpGainState.LevelTooHigh || state == ExpGainState.LevelTooLow) && recommended != currentIsland)
            {
                var status = state == ExpGainState.LevelTooHigh ? Localization.UI_DETAILS_LEVEL_TOO_HIGH : Localization.UI_DETAILS_LEVEL_TOO_LOW;
                ShowTraining("rf-training--warning", icon, title, percent, progress, status, "rf-training__status--warning",
                    Localization.UI_DETAILS_RECOMMENDED_TITLE,
                    "<color=#E8A33D>!sail " + recommended + "</color>", string.Empty);
                return;
            }

            if (player.Chunk == null && !isSailing)
            {
                var status = !player.Island ? Localization.UI_DETAILS_INELIGIBLE : Localization.UI_DETAILS_LEVEL_TOO_LOW;
                ShowTraining("rf-training--warning", icon, title, percent, progress, status, "rf-training__status--warning",
                    string.Empty, string.Empty, string.Empty);
                return;
            }

            var left = TimeToLevel(trainingAll, skill);
            var time = HudFormat.TimeToLevel(left, out var tone);
            var timeColor = tone == 0 ? "#5FA8C7" : tone == 1 ? "#E8A33D" : "#FF6E64";
            var next = string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_NEXT_LEVEL, "<color=" + timeColor + ">" + time + "</color>");

            var onRecommended = (currentIsland == RavenNest.Models.Island.Ferry && skill.Type == Skill.Sailing)
                || (currentIsland != RavenNest.Models.Island.Ferry && recommended == currentIsland);
            var hint = onRecommended ? string.Empty : string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_RECOMMENDED, recommended);

            ShowTraining(null, icon, title, percent, progress, string.Empty, null, next, rate, hint);
        }

        /// <summary>
        /// Resting in the onsen: how full the rested pool is, and how long until it is topped up.
        /// Rested time builds at <see cref="OnsenHandler.RestedGainFactor"/> times real time up to
        /// <see cref="CharacterRestedState.RestedTimeMax"/>.
        /// </summary>
        private void ShowResting()
        {
            var max = CharacterRestedState.RestedTimeMax;
            var rested = player.Rested != null ? Math.Min(player.Rested.RestedTime, max) : 0d;
            var fraction = max > 0 ? Mathf.Clamp01((float)(rested / max)) : 0f;
            var full = rested >= max - 1d;

            var percent = (fraction * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
            var amount = string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_RESTED_OF,
                HudFormat.Duration((int)rested), HudFormat.Duration((int)max));
            var next = full
                ? Localization.UI_DETAILS_FULLY_RESTED
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_FULLY_RESTED_IN,
                    "<color=#5FA8C7>" + HudFormat.Duration((int)((max - rested) / OnsenHandler.RestedGainFactor)) + "</color>");

            ShowTraining(full ? "rf-training--done" : "rf-training--resting", null, Localization.UI_DETAILS_RESTING,
                percent, fraction, amount, null, next, string.Empty, string.Empty, "\uf593");
        }

        /// <summary>
        /// Time to the next level. When training all, the combat skill that will level first.
        /// </summary>
        private TimeSpan TimeToLevel(bool trainingAll, SkillStat skill)
        {
            var now = DateTime.UtcNow;
            if (!trainingAll)
            {
                return skill.GetEstimatedTimeToLevelUp() - now;
            }

            // The old panel compared defence twice here and never looked at attack.
            var str = player.Stats.Strength.GetEstimatedTimeToLevelUp() - now;
            var def = player.Stats.Defense.GetEstimatedTimeToLevelUp() - now;
            var atk = player.Stats.Attack.GetEstimatedTimeToLevelUp() - now;
            var best = str;
            if (def > TimeSpan.Zero && (best <= TimeSpan.Zero || def < best)) best = def;
            if (atk > TimeSpan.Zero && (best <= TimeSpan.Zero || atk < best)) best = atk;
            return best;
        }

        /// <param name="fill">0 to 1, or below 0 to hide the bar.</param>
        /// <param name="glyph">A Font Awesome glyph shown instead of <paramref name="icon"/>.</param>
        private void ShowTraining(string stateClass, Sprite icon, string title, string percent, float fill,
            string status, string statusClass, string next, string rate, string hint, string glyph = null)
        {
            if (!string.Equals(stateClass, shownTrainingClass, StringComparison.Ordinal))
            {
                if (shownTrainingClass != null) training.RemoveFromClassList(shownTrainingClass);
                if (stateClass != null) training.AddToClassList(stateClass);
                shownTrainingClass = stateClass;
            }

            if (icon != shownTrainingIcon)
            {
                shownTrainingIcon = icon;
                trainingIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            }

            var useGlyph = icon == null && !string.IsNullOrEmpty(glyph);
            SetShown(trainingIcon, !useGlyph);
            SetShown(trainingGlyph, useGlyph);
            if (useGlyph) SetText(trainingGlyph, glyph);
            SetShown(trainingBar, fill >= 0f);

            SetText(trainingTitle, title);
            SetText(trainingPercent, percent);

            if (fill >= 0f && Mathf.Abs(fill - shownTrainingFill) >= 0.001f)
            {
                shownTrainingFill = fill;
                trainingFill.style.width = new Length(fill * 100f, LengthUnit.Percent);
            }

            SetText(trainingStatus, status);
            SetShown(trainingStatus, !string.IsNullOrEmpty(status));
            trainingStatus.EnableInClassList("rf-training__status--warning", statusClass == "rf-training__status--warning");
            trainingStatus.EnableInClassList("rf-training__status--done", statusClass == "rf-training__status--done");
            trainingStatus.EnableInClassList("rf-training__status--max", statusClass == "rf-training__status--max");

            SetText(trainingNext, next);
            SetShown(trainingNext, !string.IsNullOrEmpty(next));
            SetText(trainingRate, rate);
            SetShown(trainingRate, !string.IsNullOrEmpty(rate));
            SetText(trainingHint, hint);
            SetShown(trainingHint, !string.IsNullOrEmpty(hint));
        }

        private void RefreshSkills()
        {
            SkillStat active = null;
            try
            {
                if (player.GetTask() != TaskType.None || (player.ferryHandler && player.ferryHandler.OnFerry))
                {
                    active = player.ferryHandler && player.ferryHandler.OnFerry ? player.Stats.Sailing : player.GetActiveSkillStat();
                }
            }
            catch
            {
                active = null;
            }

            var health = player.Stats.Health;
            var trainingAll = active != null && active == health;

            for (var i = 0; i < skillTiles.Count; ++i)
            {
                var tile = skillTiles[i];
                var stat = tile.IsClan ? clanEnchanting : player.Stats.GetSkill(tile.Skill);
                tile.Stat = stat;

                var locked = stat == null ? 1 : 0;
                if (locked != tile.LastLocked)
                {
                    tile.LastLocked = locked;
                    tile.Root.EnableInClassList("rf-skill--locked", locked == 1);
                    tile.Icon.style.display = locked == 1 ? DisplayStyle.None : DisplayStyle.Flex;
                    tile.Lock.style.display = locked == 1 ? DisplayStyle.Flex : DisplayStyle.None;
                    if (locked == 1)
                    {
                        tile.Level.text = "-";
                        tile.Fill.style.width = 0;
                        tile.LastCurrent = int.MinValue;
                        tile.LastLevel = int.MinValue;
                        tile.LastProgress = -1f;
                    }
                }

                if (stat == null)
                {
                    continue;
                }

                var isActive = active != null && (stat == active
                    || (trainingAll && (tile.Skill == Skill.Attack || tile.Skill == Skill.Defense || tile.Skill == Skill.Strength)));
                if (isActive != tile.LastActive)
                {
                    tile.LastActive = isActive;
                    tile.Root.EnableInClassList("rf-skill--active", isActive);
                }

                int level = stat.Level;
                int current = stat.CurrentValue;
                if (level != tile.LastLevel || current != tile.LastCurrent)
                {
                    tile.LastLevel = level;
                    tile.LastCurrent = current;

                    // The value shown is what the skill is worth right now, so a potion or a drain
                    // shows up. Colour says which; the tooltip has both numbers.
                    tile.Level.text = current.ToString(CultureInfo.InvariantCulture);
                    tile.Level.EnableInClassList("rf-skill__level--max", level >= GameMath.MaxLevel && current == level);
                    tile.Level.EnableInClassList("rf-skill__level--boosted", current > level);
                    tile.Level.EnableInClassList("rf-skill__level--drained", current < level);
                }

                var nextExp = GameMath.ExperienceForLevel(level + 1);
                var progress = level >= GameMath.MaxLevel ? 1f
                    : stat.Experience > 0 && nextExp > 0 ? Mathf.Clamp01((float)(stat.Experience / nextExp)) : 0f;
                if (Mathf.Abs(progress - tile.LastProgress) >= 0.005f)
                {
                    tile.LastProgress = progress;
                    tile.Fill.style.width = new Length(progress * 100f, LengthUnit.Percent);
                }
            }
        }

        private void RefreshClanSkill()
        {
            clanEnchanting = null;
            if (player.clanHandler == null || !player.clanHandler.InClan || player.clanHandler.ClanInfo == null)
            {
                return;
            }

            var gm = GameManager.Instance;
            var skills = gm != null && gm.Clans != null ? gm.Clans.GetClanSkills(player.clanHandler.ClanInfo.Id) : null;
            if (skills == null)
            {
                return;
            }

            for (var i = 0; i < skills.Count; ++i)
            {
                if (skills[i] != null && skills[i].Name != null
                    && skills[i].Name.IndexOf("enchant", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    clanEnchanting = skills[i];
                    return;
                }
            }
        }

        private void RefreshScrolls()
        {
            if (scrolls.Count == 0 || player.Inventory == null)
            {
                return;
            }

            var owned = player.Inventory.GetInventoryItemsOfCategory(ItemCategory.Scroll);
            for (var s = 0; s < scrolls.Count; ++s)
            {
                var chip = scrolls[s];
                long count = 0;
                if (owned != null)
                {
                    for (var i = 0; i < owned.Count; ++i)
                    {
                        var item = owned[i];
                        if (item?.Item?.Name != null && item.Item.Name.IndexOf(chip.Match, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count = (long)item.InventoryItem.Amount;
                            break;
                        }
                    }
                }

                if (count == chip.Last)
                {
                    continue;
                }

                chip.Last = count;
                chip.Count.text = HudFormat.Compact(count);
                chip.Root.EnableInClassList("rf-scroll-chip--empty", count <= 0);
            }
        }

        private void RefreshStats()
        {
            if (!gearOpen)
            {
                return;
            }

            var eq = player.EquipmentStats ?? EquipmentStats.Zero;
            var style = player.TrainingMelee ? StatKind.Melee
                : player.TrainingRanged ? StatKind.Ranged
                : player.TrainingMagic || player.TrainingHealing ? StatKind.Magic
                : StatKind.Armor;

            for (var i = 0; i < statTiles.Count; ++i)
            {
                var tile = statTiles[i];
                int power, aim, bonus;
                switch (tile.Kind)
                {
                    case StatKind.Melee: power = eq.WeaponPower; aim = eq.WeaponAim; bonus = eq.WeaponBonus; break;
                    case StatKind.Ranged: power = eq.RangedPower; aim = eq.RangedAim; bonus = eq.RangedBonus; break;
                    case StatKind.Magic: power = eq.MagicPower; aim = eq.MagicAim; bonus = eq.MagicBonus; break;
                    default: power = eq.ArmorPower; aim = 0; bonus = eq.ArmorPowerBonus; break;
                }

                if (power != tile.LastPower || bonus != tile.LastBonus)
                {
                    tile.LastPower = power;
                    tile.LastBonus = bonus;
                    tile.Power.text = HudFormat.Grouped(power);
                    tile.Power.EnableInClassList("rf-stat__power--bonus", bonus > 0);
                }

                if (aim != tile.LastAim)
                {
                    tile.LastAim = aim;
                    tile.Aim.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_DETAILS_AIM, HudFormat.Grouped(aim));
                }

                // 0 neutral, 1 the style in use, 2 a style not in use. Armor always counts, and while
                // the player is not fighting nothing is singled out.
                var state = tile.Kind == StatKind.Armor || style == StatKind.Armor ? 0 : tile.Kind == style ? 1 : 2;
                if (state != tile.LastState)
                {
                    tile.LastState = state;
                    tile.Root.EnableInClassList("rf-stat--active", state == 1);
                    tile.Root.EnableInClassList("rf-stat--dim", state == 2);
                }
            }
        }

        private void RefreshSlots()
        {
            if (!gearOpen || player.Inventory == null)
            {
                return;
            }

            for (var i = 0; i < slots.Count; ++i)
            {
                ShowSlot(slots[i], GetEquipped(slots[i].Slot));
            }
        }

        private void ShowSlot(SlotView view, GameInventoryItem item)
        {
            view.Item = item;
            var sprite = item != null ? EquipmentSlot.GetItemSprite(item) : null;
            var shown = sprite != null ? sprite : view.Placeholder;

            view.Image.style.backgroundImage = shown != null ? new StyleBackground(shown) : new StyleBackground(StyleKeyword.None);
            view.Image.EnableInClassList("rf-slot__image--placeholder", sprite == null);

            var enchanted = item != null && item.Enchantments != null && item.Enchantments.Count > 0;
            view.Root.EnableInClassList("rf-slot--equipped", item != null && !enchanted);
            view.Root.EnableInClassList("rf-slot--enchanted", enchanted);
        }

        /// <summary>The same lookups EquipmentSlotManager uses, so both panels show the same items.</summary>
        private GameInventoryItem GetEquipped(ItemSlot slot)
        {
            var inv = player.Inventory;
            switch (slot)
            {
                case ItemSlot.Head:
                case ItemSlot.Amulet:
                case ItemSlot.Ring:
                case ItemSlot.Pet:
                    return inv.GetEquipmentBySlot(slot);
                case ItemSlot.Chest: return inv.GetEquipmentOfType(ItemType.Chest);
                case ItemSlot.Gloves: return inv.GetEquipmentOfType(ItemType.Gloves);
                case ItemSlot.Leggings: return inv.GetEquipmentOfType(ItemType.Leggings);
                case ItemSlot.Boots: return inv.GetEquipmentOfType(ItemType.Boots);
                case ItemSlot.MeleeWeapon:
                    return inv.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword)
                        ?? inv.GetEquipmentOfType(ItemCategory.Weapon, ItemType.OneHandedSword);
                case ItemSlot.Shield: return inv.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
                case ItemSlot.MagicWeapon: return inv.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedStaff);
                case ItemSlot.RangedWeapon: return inv.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedBow);
                default: return null;
            }
        }

        // ---- Tooltips ------------------------------------------------------------------------

        private void FillSkillTooltip(HudTooltip t, SkillTile tile)
        {
            if (tile.Stat == null)
            {
                t.SetTitle(tile.IsClan ? Localization.UI_SKILL_ENCHANTING : Localization.SkillName(tile.Skill));
                t.SetNote(tile.IsClan ? Localization.UI_DETAILS_CLAN_SKILL_LOCKED : string.Empty);
                return;
            }

            var stat = tile.Stat;
            int level = stat.Level;
            int current = stat.CurrentValue;

            t.SetTitle(tile.IsClan ? Localization.UI_SKILL_ENCHANTING : Localization.SkillName(tile.Skill));
            t.AddRow(Localization.UI_TIP_LEVEL, level.ToString(CultureInfo.InvariantCulture), level >= GameMath.MaxLevel ? "rf-tooltip__value--gold" : null);
            if (current > level) t.AddRow(Localization.UI_TIP_BOOSTED, current.ToString(CultureInfo.InvariantCulture), "rf-tooltip__value--good");
            if (current < level) t.AddRow(Localization.UI_TIP_DRAINED, current.ToString(CultureInfo.InvariantCulture), "rf-tooltip__value--bad");

            if (level >= GameMath.MaxLevel)
            {
                t.SetNote(Localization.UI_DETAILS_MAX_LEVEL);
                return;
            }

            var nextExp = GameMath.ExperienceForLevel(level + 1);
            double exp = stat.Experience;
            if (nextExp > 0)
            {
                t.AddRow(Localization.UI_TIP_PROGRESS, HudFormat.Percent((float)(exp / nextExp)));
                t.AddRow(Localization.UI_TIP_EXPERIENCE, HudFormat.Compact(exp) + " / " + HudFormat.Compact(nextExp));
            }

            var perHour = stat.GetExperiencePerHour();
            if (perHour > 0)
            {
                t.AddRow(Localization.UI_TIP_RATE, string.Format(CultureInfo.InvariantCulture, Localization.UI_RATE_PER_HOUR, HudFormat.Compact(perHour)), "rf-tooltip__value--good");
                var time = HudFormat.TimeToLevel(stat.GetEstimatedTimeToLevelUp() - DateTime.UtcNow, out _);
                t.AddRow(Localization.UI_TIP_NEXT_LEVEL, time);
            }
        }

        private void FillItemTooltip(HudTooltip t, SlotView view)
        {
            var item = view.Item;
            if (item == null || item.Item == null)
            {
                t.SetTitle(SlotName(view.Slot));
                t.SetNote(Localization.UI_DETAILS_EMPTY_SLOT);
                return;
            }

            FillItem(t, item);
        }

        /// <summary>An item's tooltip: its name, stats and enchantments (also hero mode's inventory).</summary>
        internal static void FillItem(HudTooltip t, GameInventoryItem item)
        {
            t.SetTitle(item.InventoryItem?.Name ?? item.Item.Name);

            if (item.Item.Type == ItemType.Pet)
            {
                t.SetNote(Localization.UI_TIP_PET);
                return;
            }

            var stats = item.GetItemStats();
            if (stats != null)
            {
                foreach (var s in stats)
                {
                    if (s == null || (s.Value == 0 && s.Bonus == 0))
                    {
                        continue;
                    }

                    var value = HudFormat.Grouped(s.Value);
                    if (s.Bonus > 0) value += "  +" + HudFormat.Grouped(s.Bonus);
                    else if (s.Bonus < 0) value += "  " + HudFormat.Grouped(s.Bonus);
                    t.AddRow(Localization.StatName(s.Name), value, s.Bonus > 0 ? "rf-tooltip__value--good" : s.Bonus < 0 ? "rf-tooltip__value--bad" : null);
                }
            }

            if (item.Enchantments != null)
            {
                for (var i = 0; i < item.Enchantments.Count; ++i)
                {
                    var e = item.Enchantments[i];
                    var value = e.ValueType == RavenNest.Models.AttributeValueType.Percent
                        ? "+" + ((int)(e.Value * 100)).ToString(CultureInfo.InvariantCulture) + "%"
                        : "+" + e.Value.ToString("0.##", CultureInfo.InvariantCulture);
                    t.AddRow(Localization.StatName(e.Name), value, "rf-tooltip__value--magic");
                }
            }

            if (item.TransmogrificationId != null)
            {
                t.SetNote(Localization.UI_DETAILS_TRANSMOGRIFIED);
            }
        }

        private void FillStatTooltip(HudTooltip t, StatTile tile)
        {
            if (!player)
            {
                return;
            }

            var eq = player.EquipmentStats ?? EquipmentStats.Zero;
            switch (tile.Kind)
            {
                case StatKind.Armor:
                    t.SetTitle(Localization.UI_TIP_ARMOR);
                    AddSplit(t, Localization.UI_TIP_ARMOR_POWER, eq.BaseArmorPower, eq.ArmorPowerBonus);
                    t.SetNote(Localization.UI_TIP_ARMOR_NOTE);
                    break;
                case StatKind.Melee:
                    t.SetTitle(Localization.UI_TIP_MELEE);
                    AddSplit(t, Localization.UI_TIP_WEAPON_POWER, eq.BaseWeaponPower, eq.WeaponPowerBonus);
                    AddSplit(t, Localization.UI_TIP_WEAPON_AIM, eq.BaseWeaponAim, eq.WeaponAimBonus);
                    break;
                case StatKind.Ranged:
                    t.SetTitle(Localization.UI_TIP_RANGED);
                    AddSplit(t, Localization.UI_TIP_RANGED_POWER, eq.BaseRangedPower, eq.RangedPowerBonus);
                    AddSplit(t, Localization.UI_TIP_RANGED_AIM, eq.BaseRangedAim, eq.RangedAimBonus);
                    break;
                default:
                    t.SetTitle(Localization.UI_TIP_MAGIC);
                    AddSplit(t, Localization.UI_TIP_MAGIC_POWER, eq.BaseMagicPower, eq.MagicPowerBonus);
                    AddSplit(t, Localization.UI_TIP_MAGIC_AIM, eq.BaseMagicAim, eq.MagicAimBonus);
                    AddSplit(t, Localization.UI_TIP_HEALING_POWER, eq.BaseHealingPower, eq.HealingPowerBonus);
                    break;
            }
        }

        private static void AddSplit(HudTooltip t, string label, int baseValue, int bonus)
        {
            var value = HudFormat.Grouped(baseValue + bonus);
            if (bonus != 0)
            {
                value += "  (" + HudFormat.Grouped(baseValue) + (bonus > 0 ? " +" : " ") + HudFormat.Grouped(bonus) + ")";
            }

            t.AddRow(label, value, bonus > 0 ? "rf-tooltip__value--good" : null);
        }

        // ---- Helpers ---------------------------------------------------------------------------

        private void OnCollapseClicked()
        {
            drag?.ToggleCollapsed();
            tooltip?.Hide();
            if (drag != null && !drag.IsCollapsed && IsShowing)
            {
                RefreshNow();
            }
        }

        private void Turn(float degrees)
        {
            if (observeCamera != null)
            {
                observeCamera.Rotate(degrees);
            }
        }

        /// <summary>
        /// Mouse control of the preview, as in a model viewer: drag to turn and tilt, scroll to zoom,
        /// drag with the middle button to move the view, and the corner button to put it back.
        /// </summary>
        private void BindPreviewControls(VisualElement preview, Button reset)
        {
            if (preview == null)
            {
                return;
            }

            if (reset != null)
            {
                reset.clicked += () => observeCamera?.ResetView();
                // the corner button is also where the mouse controls are explained
                tooltip?.Attach(reset, t =>
                {
                    t.SetTitle(Localization.UI_DETAILS_PREVIEW_RESET);
                    t.SetNote(Localization.UI_DETAILS_PREVIEW_HINT);
                });
            }

            var button = -1;
            var last = Vector2.zero;

            preview.RegisterCallback<PointerDownEvent>(evt =>
            {
                // the turn and reset buttons handle their own clicks
                if (evt.target is Button || (evt.button != 0 && evt.button != 2))
                {
                    return;
                }

                button = evt.button;
                last = evt.position;
                preview.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            preview.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (button < 0 || !preview.HasPointerCapture(evt.pointerId) || observeCamera == null)
                {
                    return;
                }

                var delta = (Vector2)evt.position - last;
                last = evt.position;
                if (button == 0)
                {
                    observeCamera.Rotate(delta.x * previewTurnSpeed);
                    observeCamera.Tilt(delta.y * previewTurnSpeed);
                }
                else
                {
                    // the view follows the pointer: dragging right moves the character right
                    var height = Mathf.Max(1f, preview.resolvedStyle.height);
                    observeCamera.Pan(new Vector2(delta.x, delta.y) / height);
                }
                evt.StopPropagation();
            });

            void Release(IPointerEvent evt)
            {
                if (button < 0)
                {
                    return;
                }

                button = -1;
                if (preview.HasPointerCapture(evt.pointerId))
                {
                    preview.ReleasePointer(evt.pointerId);
                }
            }

            preview.RegisterCallback<PointerUpEvent>(evt => Release(evt));
            preview.RegisterCallback<PointerCaptureOutEvent>(_ => button = -1);

            preview.RegisterCallback<WheelEvent>(evt =>
            {
                // scrolling up comes closer
                observeCamera?.Zoom(Mathf.Pow(1.12f, evt.delta.y > 0 ? 1f : -1f));
                evt.StopPropagation();
            });
        }

        private void ApplyVisibility()
        {
            if (!bound)
            {
                return;
            }

            var show = IsShowing;
            window.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
            {
                tooltip?.Hide();
            }
        }

        private void ApplyGearOpen()
        {
            if (window != null)
            {
                window.EnableInClassList("rf-details--gear-closed", !gearOpen);
            }
        }

        private void ApplyPreviewTexture()
        {
            if (previewImage == null || previewTexture == null)
            {
                return;
            }

            var rt = previewTexture as RenderTexture;
            previewImage.style.backgroundImage = rt != null
                ? new StyleBackground(Background.FromRenderTexture(rt))
                : new StyleBackground(Background.FromTexture2D(previewTexture as Texture2D));
        }

        /// <summary>Forgets every shown value so the next refresh writes everything for the new player.</summary>
        private void ResetShown()
        {
            shownName = null;
            shownColor = null;
            shownLevel = int.MinValue;
            shownCoins = long.MinValue;
            shownRestedMinutes = int.MinValue;
            shownTrainingFill = -1f;
            shownObserve = -1f;

            // The training row caches its icon and state class as well. A rebuilt tree starts without
            // them, and a cache left pointing at the old sprite meant the icon was never set again.
            if (training != null && shownTrainingClass != null) training.RemoveFromClassList(shownTrainingClass);
            shownTrainingClass = null;
            if (trainingIcon != null) trainingIcon.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            shownTrainingIcon = null;

            for (var i = 0; i < skillTiles.Count; ++i)
            {
                var tile = skillTiles[i];
                tile.LastCurrent = int.MinValue;
                tile.LastLevel = int.MinValue;
                tile.LastProgress = -1f;
                tile.LastLocked = -1;
            }

            for (var i = 0; i < statTiles.Count; ++i)
            {
                statTiles[i].LastPower = int.MinValue;
                statTiles[i].LastAim = int.MinValue;
                statTiles[i].LastBonus = int.MinValue;
                statTiles[i].LastState = -1;
            }

            for (var i = 0; i < scrolls.Count; ++i)
            {
                scrolls[i].Last = long.MinValue;
            }
        }

        private Sprite IconFor(Skill skill)
        {
            return iconBySkill.TryGetValue(skill, out var sprite) ? sprite : null;
        }

        private Sprite SlotPlaceholder(ItemSlot slot)
        {
            for (var i = 0; i < slotIcons.Length; ++i)
            {
                if (slotIcons[i].Slot == slot)
                {
                    return slotIcons[i].Icon;
                }
            }

            return null;
        }

        private static string BadgeText(string id)
        {
            switch (id)
            {
                case "broadcaster": return Localization.UI_BADGE_BROADCASTER;
                case "moderator": return Localization.UI_BADGE_MODERATOR;
                case "subscriber": return Localization.UI_BADGE_SUBSCRIBER;
                case "vip": return Localization.UI_BADGE_VIP;
                case "dev": return Localization.UI_BADGE_DEV;
                default: return id;
            }
        }

        private static string SlotName(ItemSlot slot)
        {
            return Localization.SlotName(slot);
        }

        private void SetBadge(string id, bool value)
        {
            if (badges.TryGetValue(id, out var badge))
            {
                SetShown(badge, value);
            }
        }

        private static void SetText(Label label, string value)
        {
            if (label != null && label.text != value)
            {
                label.text = value;
            }
        }

        private static void SetShown(VisualElement element, bool value)
        {
            if (element == null)
            {
                return;
            }

            var display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display)
            {
                element.style.display = display;
            }
        }
    }
}
