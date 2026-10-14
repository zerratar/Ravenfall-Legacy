using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Skill = RavenNest.Models.Skill;

namespace Shinobytes.UI
{
    /// <summary>
    /// The interface for village build mode, after docs/mockups/build-*.webp: markers over every plot,
    /// village bonuses on the left, the selected building on the right, the category toolbar and
    /// building tray at the bottom, a card for what is being placed, and the resident picker.
    /// State and server calls live in <see cref="VillageBuildMode"/>; this class draws it and forwards
    /// clicks.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BuildModeView : MonoBehaviour
    {
        [Serializable]
        public struct SkillIcon
        {
            public Skill Skill;
            public Sprite Icon;
        }

        [SerializeField] private SkillIcon[] skillIcons = Array.Empty<SkillIcon>();

        [Tooltip("How often the panels and markers re-read residents and bonuses.")]
        [SerializeField] private float refreshInterval = 0.5f;

        [Tooltip("Height of the plot markers above the plot, in metres.")]
        [SerializeField] private float markerHeight = 3f;

        [Tooltip("Degrees the Rotate tile turns a building.")]
        [SerializeField] private float rotateStep = 45f;

        private const string BonusCollapsedKey = "rf_build_bonuses_collapsed";

        private sealed class Category
        {
            public string Name;
            public string Glyph;
            public TownHouseSlotType[] Types;
        }

        /// <summary>
        /// Today's buildings grouped for the toolbar. Production, Decor and Paths join when those
        /// exist (docs/village-build-mode-design.md, phases 3 and 4).
        /// </summary>
        private static readonly Category[] Categories =
        {
            new Category { Name = "Combat", Glyph = "", Types = new[] { TownHouseSlotType.Melee, TownHouseSlotType.Ranged, TownHouseSlotType.Magic, TownHouseSlotType.Healing, TownHouseSlotType.Slayer } },
            new Category { Name = "Gathering", Glyph = "", Types = new[] { TownHouseSlotType.Woodcutting, TownHouseSlotType.Mining, TownHouseSlotType.Fishing, TownHouseSlotType.Farming, TownHouseSlotType.Gathering } },
            new Category { Name = "Crafting", Glyph = "", Types = new[] { TownHouseSlotType.Cooking, TownHouseSlotType.Crafting, TownHouseSlotType.Alchemy, TownHouseSlotType.Sailing } },
        };

        private sealed class Marker
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Plus;
            public Label Name;
            public string ShownState;
            public bool Far;
        }

        private sealed class Card
        {
            public TownHouseSlotType Type;
            public VisualElement Root;
            public Label Bonus;
        }

        private readonly Dictionary<TownHouseSlot, Marker> markers = new Dictionary<TownHouseSlot, Marker>();
        private readonly List<TownHouseSlot> stale = new List<TownHouseSlot>();
        private readonly List<Card> cards = new List<Card>();
        private readonly List<Button> tabs = new List<Button>();
        private readonly List<PlayerController> ranked = new List<PlayerController>();
        private readonly List<PlayerController> candidates = new List<PlayerController>();
        private readonly Dictionary<Skill, Sprite> iconBySkill = new Dictionary<Skill, Sprite>();

        private VisualElement root;
        private VisualElement markerLayer;

        private VisualElement bonusPanel;
        private VisualElement bonusList;
        private Label bonusEmpty;
        private Label bonusFooter;

        private VisualElement plotPanel;
        private VisualElement plotIcon;
        private Label plotGlyph;
        private Label plotTitle;
        private Label plotSubtitle;
        private Label plotEmptyNote;
        private VisualElement plotBuilt;
        private Label resident;
        private Label residentLevel;
        private Label bonus;
        private Label bonusNote;
        private Button moveButton;
        private Button rotateButton;
        private Button assignButton;
        private Label assignLabel;
        private Button resetButton;
        private VisualElement hallUpgrade;
        private Label hallNext;
        private VisualElement hallNeeds;
        private Button hallUpgradeButton;
        private Button demolishButton;
        private Label demolishLabel;

        private VisualElement placingCard;
        private VisualElement placingIcon;
        private Label placingTitle;
        private Label placingBonus;
        private Label placingPlots;

        private VisualElement tray;
        private Label trayGlyph;
        private Label trayTitle;
        private VisualElement trayCards;
        private VisualElement toolbar;
        private Label hint;
        private Label toast;

        private VisualElement modal;
        private Label assignTitle;
        private TextField assignSearch;
        private Label assignCurrent;
        private ListView assignList;
        private Label assignEmpty;

        private bool bound;
        private bool modalOpen;
        private VillageBuildMode mode;
        private int openCategory = -1;
        private float nextRefresh;
        private float demolishConfirmUntil;
        private IVisualElementScheduledItem toastHide;
        private Camera cam;
        private DraggableWindow bonusDrag;
        private DraggableWindow plotDrag;

        /// <summary>Beyond this many metres from the camera, built plots show only their icon.</summary>
        private const float NameDistance = 50f;

        private void Awake()
        {
            foreach (var icon in skillIcons)
            {
                iconBySkill[icon.Skill] = icon.Icon;
            }
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            bound = false;
            Unhook();
            bonusDrag?.Detach();
            plotDrag?.Detach();
            bonusDrag = null;
            plotDrag = null;
        }

        // ---- Binding -------------------------------------------------------------------------

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            root = doc != null ? doc.rootVisualElement?.Q<VisualElement>("build-root") : null;
            if (root == null)
            {
                return;
            }

            markerLayer = root.Q<VisualElement>("build-markers");

            bonusPanel = root.Q<VisualElement>("bonus-panel");
            bonusList = root.Q<VisualElement>("bonus-list");
            bonusEmpty = root.Q<Label>("bonus-empty");
            bonusFooter = root.Q<Label>("bonus-footer");

            plotPanel = root.Q<VisualElement>("plot-panel");
            plotIcon = root.Q<VisualElement>("plot-icon");
            plotGlyph = root.Q<Label>("plot-glyph");
            plotTitle = root.Q<Label>("plot-title");
            plotSubtitle = root.Q<Label>("plot-subtitle");
            plotEmptyNote = root.Q<Label>("plot-empty-note");
            plotBuilt = root.Q<VisualElement>("plot-built");
            resident = root.Q<Label>("plot-resident");
            residentLevel = root.Q<Label>("plot-resident-level");
            bonus = root.Q<Label>("plot-bonus");
            bonusNote = root.Q<Label>("plot-bonus-note");
            moveButton = root.Q<Button>("plot-move");
            rotateButton = root.Q<Button>("plot-rotate");
            assignButton = root.Q<Button>("plot-assign");
            assignLabel = root.Q<Label>("plot-assign-label");
            resetButton = root.Q<Button>("plot-reset");
            hallUpgrade = root.Q<VisualElement>("hall-upgrade");
            hallNext = root.Q<Label>("hall-next");
            hallNeeds = root.Q<VisualElement>("hall-needs");
            hallUpgradeButton = root.Q<Button>("hall-upgrade-button");
            demolishButton = root.Q<Button>("plot-demolish");
            demolishLabel = root.Q<Label>("plot-demolish-label");

            placingCard = root.Q<VisualElement>("placing-card");
            placingIcon = root.Q<VisualElement>("placing-icon");
            placingTitle = root.Q<Label>("placing-title");
            placingBonus = root.Q<Label>("placing-bonus");
            placingPlots = root.Q<Label>("placing-plots");

            tray = root.Q<VisualElement>("build-tray");
            trayGlyph = root.Q<Label>("tray-glyph");
            trayTitle = root.Q<Label>("tray-title");
            trayCards = root.Q<VisualElement>("tray-cards");
            toolbar = root.Q<VisualElement>("build-toolbar");
            hint = root.Q<Label>("build-hint");
            toast = root.Q<Label>("build-toast");

            modal = root.Q<VisualElement>("assign-modal");
            assignTitle = root.Q<Label>("assign-title");
            assignSearch = root.Q<TextField>("assign-search");
            assignCurrent = root.Q<Label>("assign-current");
            assignList = root.Q<ListView>("assign-list");
            assignEmpty = root.Q<Label>("assign-empty");

            // Static text.
            root.Q<Label>("bonus-title").text = Localization.UI_BUILD_SUMMARY_TITLE;
            bonusEmpty.text = Localization.UI_BUILD_SUMMARY_EMPTY;
            root.Q<Label>("plot-resident-caption").text = Localization.UI_BUILD_RESIDENT;
            plotEmptyNote.text = Localization.UI_BUILD_EMPTY_PLOT_NOTE;
            root.Q<Label>("plot-move-label").text = Localization.UI_BUILD_MOVE;
            root.Q<Label>("plot-rotate-label").text = Localization.UI_BUILD_ROTATE;
            resetButton.text = Localization.UI_BUILD_RESET_POSITION;
            assignEmpty.text = Localization.UI_BUILD_NO_PLAYERS;
            assignSearch.textEdition.placeholder = Localization.UI_BUILD_SEARCH;

            // Actions.
            root.Q<Button>("bonus-toggle").clicked += ToggleBonuses;

            // Both side panels move like every other HUD window and remember where they were left.
            bonusDrag = DraggableWindow.Attach(bonusPanel, root.Q<VisualElement>("bonus-head"), "buildbonuses");
            plotDrag = DraggableWindow.Attach(plotPanel, root.Q<VisualElement>("plot-head"), "buildplot");
            // wider only: the height follows what is shown
            bonusDrag?.EnableResize(root.Q<VisualElement>("bonus-grip"), new Vector2(340f, 0f), widthOnly: true);
            plotDrag?.EnableResize(root.Q<VisualElement>("plot-grip"), new Vector2(380f, 0f), widthOnly: true);
            root.Q<Button>("plot-close").clicked += () => mode?.ClearSelection();
            root.Q<Button>("tray-close").clicked += () => OpenCategory(-1);
            root.Q<Button>("assign-close").clicked += CloseAssign;
            moveButton.clicked += () =>
            {
                if (mode == null) return;
                if (mode.HallSelected) mode.StartHallMove();
                else if (mode.Selected) mode.StartMove(mode.Selected);
            };
            rotateButton.clicked += () =>
            {
                if (mode == null) return;
                if (mode.HallSelected) _ = mode.RotateHallInPlaceAsync(rotateStep);
                else if (mode.Selected) _ = mode.RotateInPlaceAsync(mode.Selected, rotateStep);
            };
            assignButton.clicked += OpenAssign;
            resetButton.clicked += () =>
            {
                if (mode == null) return;
                if (mode.HallSelected) _ = mode.ResetHallPlacementAsync();
                else if (mode.Selected) _ = mode.ResetPlacementAsync(mode.Selected);
            };
            demolishButton.clicked += OnDemolishClicked;
            hallUpgradeButton.text = Localization.UI_BUILD_HALL_UPGRADE;
            hallUpgradeButton.clicked += () => { if (mode != null) _ = mode.UpgradeHallAsync(); };
            assignSearch.RegisterValueChangedCallback(evt => FilterCandidates());

            // A click on the dimmed backdrop closes the picker; a click on the panel does not.
            modal.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == modal) CloseAssign();
            });

            assignList.itemsSource = candidates;
            assignList.makeItem = MakeCandidateRow;
            assignList.bindItem = BindCandidateRow;
            assignList.fixedItemHeight = 70f;

            if (PlayerPrefs.GetInt(BonusCollapsedKey, 0) == 1)
            {
                bonusPanel.AddToClassList("rf-build-bonuses--collapsed");
            }

            BuildToolbar();

            bound = true;
            root.style.display = DisplayStyle.None;
        }

        private void Hook()
        {
            if (mode != null || !VillageBuildMode.Instance)
            {
                return;
            }

            mode = VillageBuildMode.Instance;
            mode.ActiveChanged += OnActiveChanged;
            mode.Changed += OnChanged;
            mode.Failed += ShowToast;
            OnActiveChanged(mode.IsActive);
        }

        private void Unhook()
        {
            if (mode == null)
            {
                return;
            }

            mode.ActiveChanged -= OnActiveChanged;
            mode.Changed -= OnChanged;
            mode.Failed -= ShowToast;
            mode = null;
        }

        // ---- Lifecycle -----------------------------------------------------------------------

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            Hook();
            if (mode == null || !mode.IsActive)
            {
                return;
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + refreshInterval;
                RefreshAll();
                RefreshCardBonuses();
            }

            if (demolishConfirmUntil > 0f && Time.unscaledTime > demolishConfirmUntil)
            {
                demolishConfirmUntil = 0f;
                RefreshDemolish();
            }
        }

        private void LateUpdate()
        {
            if (mode == null || !mode.IsActive || markers.Count == 0)
            {
                return;
            }

            if (!cam || !cam.isActiveAndEnabled) cam = Camera.main;
            var panel = markerLayer.panel;
            if (!cam || panel == null) return;

            foreach (var pair in markers)
            {
                var slot = pair.Key;
                var marker = pair.Value;
                if (!slot) continue;

                var world = slot.transform.position + Vector3.up * (markerHeight + slot.IconOffset);
                var visible = cam.WorldToViewportPoint(world).z > 0f;
                marker.Root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
                if (!visible) continue;

                var point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                marker.Root.style.translate = new Translate(point.x, point.y);

                var far = slot != mode.Selected && slot != mode.Moving
                    && Vector3.Distance(cam.transform.position, world) > NameDistance;
                if (far != marker.Far)
                {
                    marker.Far = far;
                    marker.Root.EnableInClassList("rf-plot--far", far);
                }
            }
        }

        private void OnActiveChanged(bool active)
        {
            if (!bound) return;

            root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active)
            {
                CloseAssign();
                OpenCategory(-1);
                markerLayer.Clear();
                markers.Clear();
                return;
            }

            nextRefresh = 0f;
        }

        private void OnChanged()
        {
            if (!bound || mode == null || !mode.IsActive) return;
            demolishConfirmUntil = 0f;
            root.EnableInClassList("rf-build--moving", mode.IsMoving);
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshMarkers();
            RefreshBonuses();
            RefreshPlot();
            RefreshPlacing();
            RefreshCards();
            RefreshHint();
        }

        // ---- Toolbar and tray ----------------------------------------------------------------

        private void BuildToolbar()
        {
            toolbar.Clear();
            tabs.Clear();
            for (var i = 0; i < Categories.Length; ++i)
            {
                var index = i;
                var tab = new Button(() => OpenCategory(openCategory == index ? -1 : index));
                tab.AddToClassList("rf-build-tab");

                var glyph = new Label(Categories[i].Glyph) { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("rf-icon");
                glyph.AddToClassList("rf-build-tab__icon");

                var label = new Label(CategoryName(Categories[i])) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("rf-build-tab__label");

                tab.Add(glyph);
                tab.Add(label);
                toolbar.Add(tab);
                tabs.Add(tab);
            }
        }

        private void OpenCategory(int index)
        {
            openCategory = index >= 0 && index < Categories.Length ? index : -1;
            for (var i = 0; i < tabs.Count; ++i)
            {
                tabs[i].EnableInClassList("rf-build-tab--active", i == openCategory);
            }

            tray.style.display = openCategory >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (openCategory >= 0)
            {
                BuildTray();
            }
        }

        private void BuildTray()
        {
            trayCards.Clear();
            cards.Clear();
            if (mode == null || openCategory < 0) return;

            var category = Categories[openCategory];
            trayGlyph.text = category.Glyph;
            trayTitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_TRAY_TITLE, CategoryName(category).ToUpperInvariant());

            foreach (var type in category.Types)
            {
                var house = HouseFor(type);
                if (!house) continue;

                var card = new Card { Type = type, Root = new VisualElement() };
                card.Root.AddToClassList("rf-build-card");

                var preview = new VisualElement { pickingMode = PickingMode.Ignore };
                preview.AddToClassList("rf-build-card__preview");
                var texture = BuildingPreview.Get(house.Prefab);
                if (texture) preview.style.backgroundImage = new StyleBackground(texture);

                var info = new VisualElement { pickingMode = PickingMode.Ignore };
                info.AddToClassList("rf-build-card__info");
                var ring = new VisualElement { pickingMode = PickingMode.Ignore };
                ring.AddToClassList("rf-build-card__ring");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-build-card__icon");
                var sprite = IconFor(type);
                if (sprite) icon.style.backgroundImage = new StyleBackground(sprite);
                ring.Add(icon);

                var text = new VisualElement { pickingMode = PickingMode.Ignore };
                var name = new Label(Localization.HouseName(house.Type)) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-build-card__name");
                card.Bonus = new Label { pickingMode = PickingMode.Ignore };
                card.Bonus.AddToClassList("rf-build-card__bonus");
                text.Add(name);
                text.Add(card.Bonus);
                info.Add(ring);
                info.Add(text);

                var build = new Button(() => mode?.Choose(type)) { text = Localization.UI_BUILD_BUILD };
                build.AddToClassList("rf-build-card__build");

                card.Root.Add(preview);
                card.Root.Add(info);
                card.Root.Add(build);
                trayCards.Add(card.Root);
                cards.Add(card);
            }

            RefreshCards();
            RefreshCardBonuses();
        }

        private void RefreshCards()
        {
            if (mode == null) return;
            foreach (var card in cards)
            {
                card.Root.EnableInClassList("rf-build-card--armed", mode.Armed == card.Type);
                card.Root.EnableInClassList("rf-build-card--current", mode.Selected && mode.Selected.SlotType == card.Type);
                card.Root.EnableInClassList("rf-build-card--disabled", mode.Busy || mode.IsMoving);
            }
        }

        /// <summary>Each card shows the bonus the best player available would give, as a reason to pick it.</summary>
        private void RefreshCardBonuses()
        {
            if (mode == null) return;
            foreach (var card in cards)
            {
                var text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_BONUS, Mathf.RoundToInt(BestBonus(card.Type)), SkillName(card.Type));
                if (card.Bonus.text != text) card.Bonus.text = text;
            }
        }

        private float BestBonus(TownHouseSlotType type)
        {
            var players = mode.RankResidents(type);
            return players.Count > 0 ? VillageBuildMode.BonusFor(players[0], type) : 0f;
        }

        // ---- Markers -------------------------------------------------------------------------

        private void RefreshMarkers()
        {
            if (mode == null) return;

            stale.Clear();
            stale.AddRange(markers.Keys);

            foreach (var slot in mode.Plots)
            {
                if (!markers.TryGetValue(slot, out var marker))
                {
                    marker = MakeMarker(slot);
                    markers[slot] = marker;
                    markerLayer.Add(marker.Root);
                }

                stale.Remove(slot);
                UpdateMarker(slot, marker);
            }

            foreach (var slot in stale)
            {
                markers[slot].Root.RemoveFromHierarchy();
                markers.Remove(slot);
            }
        }

        private Marker MakeMarker(TownHouseSlot slot)
        {
            var marker = new Marker { Root = new VisualElement { pickingMode = PickingMode.Ignore } };
            marker.Root.AddToClassList("rf-plot");

            var pill = new VisualElement();
            pill.AddToClassList("rf-plot__pill");
            pill.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                mode?.Select(slot);
                evt.StopPropagation();
            });

            var ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("rf-plot__ring");
            marker.Icon = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.Icon.AddToClassList("rf-plot__icon");
            marker.Plus = new Label("") { pickingMode = PickingMode.Ignore };
            marker.Plus.AddToClassList("rf-icon");
            marker.Plus.AddToClassList("rf-plot__plus");
            ring.Add(marker.Icon);
            ring.Add(marker.Plus);

            marker.Name = new Label { pickingMode = PickingMode.Ignore };
            marker.Name.AddToClassList("rf-plot__name");

            pill.Add(ring);
            pill.Add(marker.Name);
            marker.Root.Add(pill);
            return marker;
        }

        private void UpdateMarker(TownHouseSlot slot, Marker marker)
        {
            var empty = VillageBuildMode.IsEmpty(slot);
            var selected = mode.Selected == slot;
            var target = mode.Armed.HasValue && empty;
            var muted = mode.Armed.HasValue && !empty;
            var moving = mode.Moving == slot;
            var blocked = moving && !mode.MoveValid;

            // While moving to a spot that is not allowed, the pill says why, next to the building.
            var house = HouseFor(slot.SlotType);
            var name = blocked ? mode.MoveProblem : empty ? string.Empty : (Localization.HouseName(house, slot.SlotType));

            var state = (int)slot.SlotType + "|" + selected + "|" + target + "|" + muted + "|" + moving + "|" + blocked + "|" + name;
            if (state == marker.ShownState) return;
            marker.ShownState = state;

            marker.Root.EnableInClassList("rf-plot--empty", empty && !blocked);
            marker.Root.EnableInClassList("rf-plot--selected", selected && !moving);
            marker.Root.EnableInClassList("rf-plot--target", target);
            marker.Root.EnableInClassList("rf-plot--muted", muted);
            marker.Root.EnableInClassList("rf-plot--moving", moving && !blocked);
            marker.Root.EnableInClassList("rf-plot--moving-blocked", blocked);

            var sprite = empty ? null : IconFor(slot.SlotType);
            marker.Plus.style.display = sprite ? DisplayStyle.None : DisplayStyle.Flex;
            marker.Plus.text = blocked ? "" : "";
            marker.Icon.style.display = sprite ? DisplayStyle.Flex : DisplayStyle.None;
            if (sprite) marker.Icon.style.backgroundImage = new StyleBackground(sprite);

            marker.Name.text = name;
            marker.Root.EnableInClassList("rf-plot--no-name", string.IsNullOrEmpty(name));
        }

        // ---- Village bonuses -----------------------------------------------------------------

        private void ToggleBonuses()
        {
            var collapsed = !bonusPanel.ClassListContains("rf-build-bonuses--collapsed");
            bonusPanel.EnableInClassList("rf-build-bonuses--collapsed", collapsed);
            PlayerPrefs.SetInt(BonusCollapsedKey, collapsed ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RefreshBonuses()
        {
            // Buildings per type, and how many of them are actually giving a bonus.
            var counts = new Dictionary<TownHouseSlotType, int>();
            foreach (var slot in mode.Plots)
            {
                if (VillageBuildMode.IsEmpty(slot)) continue;
                counts.TryGetValue(slot.SlotType, out var n);
                counts[slot.SlotType] = n + 1;
            }

            var bonuses = mode.BonusByType();
            var types = new List<TownHouseSlotType>(counts.Keys);
            types.Sort((a, b) =>
            {
                bonuses.TryGetValue(a, out var ba);
                bonuses.TryGetValue(b, out var bb);
                return bb != ba ? bb.CompareTo(ba) : counts[b].CompareTo(counts[a]);
            });

            var top = 0f;
            foreach (var value in bonuses.Values) top = Mathf.Max(top, value);

            bonusList.Clear();
            var contributing = 0;
            foreach (var type in types)
            {
                bonuses.TryGetValue(type, out var value);
                if (value > 0f) contributing += counts[type];
                bonusList.Add(MakeBonusRow(type, value, top, counts[type]));
            }

            bonusEmpty.style.display = types.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            bonusFooter.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_SUMMARY_CONTRIBUTING, contributing);
            bonusFooter.style.display = types.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement MakeBonusRow(TownHouseSlotType type, float value, float top, int count)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-build-bonuses__row");

            var icon = new VisualElement();
            icon.AddToClassList("rf-build-bonuses__icon");
            var sprite = IconFor(type);
            if (sprite) icon.style.backgroundImage = new StyleBackground(sprite);

            var text = new VisualElement();
            text.AddToClassList("rf-build-bonuses__text");

            var line = new VisualElement();
            line.AddToClassList("rf-build-bonuses__line");
            var name = new Label(SkillName(type));
            name.AddToClassList("rf-build-bonuses__name");
            var amount = new Label("+" + Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture) + "%");
            amount.AddToClassList("rf-build-bonuses__value");
            line.Add(name);
            line.Add(amount);

            var meter = new VisualElement();
            meter.AddToClassList("rf-build-bonuses__meter");
            var bar = new VisualElement();
            bar.AddToClassList("rf-build-bonuses__bar");
            var fill = new VisualElement();
            fill.AddToClassList("rf-build-bonuses__fill");
            fill.style.width = new Length(top > 0f ? value / top * 100f : 0f, LengthUnit.Percent);
            bar.Add(fill);
            var buildings = new Label(string.Format(CultureInfo.InvariantCulture, count == 1 ? Localization.UI_BUILD_ONE_BUILDING : Localization.UI_BUILD_BUILDINGS, count));
            buildings.AddToClassList("rf-build-bonuses__count");
            meter.Add(bar);
            meter.Add(buildings);

            text.Add(line);
            text.Add(meter);
            row.Add(icon);
            row.Add(text);
            return row;
        }

        // ---- Selected building ---------------------------------------------------------------

        private void RefreshPlot()
        {
            if (mode.HallSelected)
            {
                RefreshHall();
                return;
            }

            var slot = mode.Selected;
            plotPanel.style.display = slot && !modalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!slot) return;

            plotEmptyNote.text = Localization.UI_BUILD_EMPTY_PLOT_NOTE;
            assignButton.style.display = DisplayStyle.Flex;
            hallUpgrade.style.display = DisplayStyle.None;
            plotGlyph.text = "\uf067";

            var empty = VillageBuildMode.IsEmpty(slot);
            var house = HouseFor(slot.SlotType);

            var sprite = empty ? null : IconFor(slot.SlotType);
            plotIcon.style.display = sprite ? DisplayStyle.Flex : DisplayStyle.None;
            if (sprite) plotIcon.style.backgroundImage = new StyleBackground(sprite);
            plotGlyph.style.display = sprite ? DisplayStyle.None : DisplayStyle.Flex;

            plotTitle.text = empty ? Localization.UI_BUILD_EMPTY_PLOT : (Localization.HouseName(house, slot.SlotType));
            plotSubtitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_PLOT_NUMBER, slot.Slot + 1);

            plotEmptyNote.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            plotBuilt.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;

            var idle = !mode.Busy && !mode.IsMoving;
            moveButton.SetEnabled(idle);
            rotateButton.SetEnabled(idle);
            assignButton.SetEnabled(idle && !empty);
            resetButton.SetEnabled(idle);
            resetButton.style.display = slot.IsMoved ? DisplayStyle.Flex : DisplayStyle.None;
            demolishButton.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshDemolish();

            assignLabel.text = slot.OwnerUserId.HasValue ? Localization.UI_BUILD_CHANGE_RESIDENT_SHORT : Localization.UI_BUILD_ASSIGN;
            if (empty) return;

            var hasResident = slot.OwnerUserId.HasValue;
            var player = slot.Player;
            resident.text = hasResident ? ResidentName(slot) : Localization.UI_BUILD_NOBODY;
            if (player) resident.style.color = HudFormat.ReadableNameColor(player.PlayerNameHexColor);
            else resident.style.color = new StyleColor(StyleKeyword.Null);

            var stats = player ? player.Stats : slot.PlayerSkills;
            var skill = stats != null ? GameMath.GetSkillByHouseType(stats, slot.SlotType) : null;
            residentLevel.text = skill != null && hasResident
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_SKILL_LEVEL, SkillName(slot.SlotType), (int)skill.Level)
                : string.Empty;
            residentLevel.style.display = string.IsNullOrEmpty(residentLevel.text) ? DisplayStyle.None : DisplayStyle.Flex;

            var value = skill != null && hasResident ? GameMath.CalculateHouseExpBonus(skill) : 0f;
            bonus.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_BONUS, Mathf.RoundToInt(value), SkillName(slot.SlotType));

            bonusNote.text = !hasResident ? Localization.UI_BUILD_NO_RESIDENT_NOTE
                : !player ? Localization.UI_BUILD_RESIDENT_AWAY_NOTE
                : string.Empty;
            bonusNote.style.display = string.IsNullOrEmpty(bonusNote.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// The town hall in the building panel: its tier, a note on the outline it moves with, and Move,
        /// Rotate and Reset position. No resident and no demolishing.
        /// </summary>
        private void RefreshHall()
        {
            var townHall = GameManager.Instance && GameManager.Instance.Village != null ? GameManager.Instance.Village.TownHall : null;
            plotPanel.style.display = townHall && !modalOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!townHall) return;

            plotIcon.style.display = DisplayStyle.None;
            plotGlyph.style.display = DisplayStyle.Flex;
            plotGlyph.text = "\uf66f";
            var tier = Mathf.Clamp(townHall.Tier, 0, Localization.UI_BUILD_HALL_NAMES.Length - 1);
            plotTitle.text = Localization.UI_BUILD_HALL_TITLE;
            plotSubtitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_TIER_NAMED, tier + 1, Localization.UI_BUILD_HALL_NAMES[tier]);
            plotEmptyNote.text = Localization.UI_BUILD_HALL_NOTE;
            plotEmptyNote.style.display = DisplayStyle.Flex;
            plotBuilt.style.display = DisplayStyle.None;

            var idle = !mode.Busy && !mode.IsMoving;
            moveButton.SetEnabled(idle);
            rotateButton.SetEnabled(idle);
            assignButton.style.display = DisplayStyle.None;
            resetButton.SetEnabled(idle);
            resetButton.style.display = townHall.IsMoved ? DisplayStyle.Flex : DisplayStyle.None;
            demolishButton.style.display = DisplayStyle.None;
            RefreshHallUpgrade(townHall, idle);
        }

        /// <summary>
        /// The upgrade section: the next tier, each requirement with what the village has (met in green,
        /// missing in red), and the Upgrade button, enabled when everything is met. Hidden when the server
        /// does not sell tiers.
        /// </summary>
        private void RefreshHallUpgrade(TownHallManager townHall, bool idle)
        {
            if (!townHall.BoughtTier.HasValue)
            {
                hallUpgrade.style.display = DisplayStyle.None;
                return;
            }

            hallUpgrade.style.display = DisplayStyle.Flex;
            hallNeeds.Clear();
            if (!townHall.HasNextTier)
            {
                hallNext.text = Localization.UI_BUILD_HALL_MAX;
                hallUpgradeButton.style.display = DisplayStyle.None;
                return;
            }

            var next = Mathf.Clamp(townHall.BoughtTier.Value + 1, 0, Localization.UI_BUILD_HALL_NAMES.Length - 1);
            hallNext.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_NEXT, Localization.UI_BUILD_HALL_NAMES[next].ToUpperInvariant());
            AddNeed(string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_NEED_LEVEL, townHall.NextLevel), townHall.Level >= townHall.NextLevel);
            AddResourceNeed(Localization.UI_TOWN_WOOD, townHall.Wood, townHall.NextWood);
            AddResourceNeed(Localization.UI_TOWN_ORE, townHall.Ore, townHall.NextOre);
            AddResourceNeed(Localization.UI_TOWN_FISH, townHall.Fish, townHall.NextFish);
            AddResourceNeed(Localization.UI_TOWN_WHEAT, townHall.Wheat, townHall.NextWheat);

            hallUpgradeButton.style.display = DisplayStyle.Flex;
            hallUpgradeButton.SetEnabled(idle && townHall.CanUpgrade);
        }

        private void AddResourceNeed(string name, long have, int need)
        {
            if (need <= 0) return;
            AddNeed(string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_NEED, name,
                Math.Min(have, need).ToString("N0", CultureInfo.InvariantCulture), need.ToString("N0", CultureInfo.InvariantCulture)), have >= need);
        }

        private void AddNeed(string text, bool met)
        {
            var label = new Label(text);
            label.AddToClassList("rf-build-hall__need");
            label.EnableInClassList("rf-build-hall__need--met", met);
            hallNeeds.Add(label);
        }

        private void OnDemolishClicked()
        {
            if (mode == null || !mode.Selected) return;

            // Two clicks, so a demolition is never one slip of the mouse.
            if (demolishConfirmUntil <= 0f)
            {
                demolishConfirmUntil = Time.unscaledTime + 3f;
                RefreshDemolish();
                return;
            }

            demolishConfirmUntil = 0f;
            _ = mode.DemolishAsync(mode.Selected);
        }

        private void RefreshDemolish()
        {
            var confirming = demolishConfirmUntil > 0f;
            demolishLabel.text = confirming ? Localization.UI_BUILD_DEMOLISH_CONFIRM : Localization.UI_BUILD_DEMOLISH;
            demolishButton.EnableInClassList("rf-build-wide--confirm", confirming);
            demolishButton.SetEnabled(mode != null && !mode.Busy && !mode.IsMoving);
        }

        // ---- What is being placed ------------------------------------------------------------

        private void RefreshPlacing()
        {
            TownHouseSlotType? type = mode.Armed;
            if (!type.HasValue && mode.Moving && !VillageBuildMode.IsEmpty(mode.Moving))
            {
                type = mode.Moving.SlotType;
            }

            if (!type.HasValue)
            {
                placingCard.style.display = DisplayStyle.None;
                return;
            }

            var house = HouseFor(type.Value);
            var sprite = IconFor(type.Value);
            placingIcon.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            placingTitle.text = Localization.HouseName(house, type.Value);
            placingBonus.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_BONUS, Mathf.RoundToInt(BestBonus(type.Value)), SkillName(type.Value));
            var (free, total) = mode.PlotCounts();
            placingPlots.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_PLOTS_FREE, free, total);
            placingCard.style.display = DisplayStyle.Flex;
        }

        // ---- Resident picker -----------------------------------------------------------------

        private void OpenAssign()
        {
            if (mode == null || !mode.Selected || VillageBuildMode.IsEmpty(mode.Selected)) return;

            var slot = mode.Selected;
            var house = HouseFor(slot.SlotType);
            assignTitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_ASSIGN_TITLE, Localization.HouseName(house, slot.SlotType));

            if (slot.OwnerUserId.HasValue)
            {
                var color = slot.Player ? ColorUtility.ToHtmlStringRGB(HudFormat.ReadableNameColor(slot.Player.PlayerNameHexColor)) : "FFF4D6";
                assignCurrent.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_ASSIGN_CURRENT, "<color=#" + color + ">" + ResidentName(slot) + "</color>");
                assignCurrent.style.display = DisplayStyle.Flex;
            }
            else
            {
                assignCurrent.style.display = DisplayStyle.None;
            }

            ranked.Clear();
            ranked.AddRange(mode.RankResidents(slot.SlotType));
            assignSearch.SetValueWithoutNotify(string.Empty);
            FilterCandidates();

            modalOpen = true;
            modal.style.display = DisplayStyle.Flex;
            plotPanel.style.display = DisplayStyle.None;
            assignSearch.Focus();
        }

        private void FilterCandidates()
        {
            var query = assignSearch.value?.Trim() ?? string.Empty;
            candidates.Clear();
            foreach (var player in ranked)
            {
                if (query.Length == 0 || (player.PlayerName != null && player.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    candidates.Add(player);
                }
            }

            assignList.RefreshItems();
            if (candidates.Count > 0) assignList.ScrollToItem(0);
            assignEmpty.style.display = candidates.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CloseAssign()
        {
            if (modal == null) return;
            modalOpen = false;
            modal.style.display = DisplayStyle.None;
            if (mode != null && mode.IsActive) RefreshPlot();
        }

        private VisualElement MakeCandidateRow()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-build-assign__row");

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-build-assign__text");

            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("rf-build-assign__line");
            var name = new Label { name = "name", pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-build-assign__name");
            var tag = new Label { name = "tag", pickingMode = PickingMode.Ignore };
            tag.AddToClassList("rf-build-assign__tag");
            line.Add(name);
            line.Add(tag);

            var level = new Label { name = "level", pickingMode = PickingMode.Ignore };
            level.AddToClassList("rf-build-assign__level");
            text.Add(line);
            text.Add(level);

            var value = new Label { name = "bonus", pickingMode = PickingMode.Ignore };
            value.AddToClassList("rf-build-assign__bonus");

            var assign = new Button { name = "assign", text = Localization.UI_BUILD_ASSIGN_BUTTON };
            assign.AddToClassList("rf-build-assign__button");
            assign.clicked += () => Assign(row.userData as PlayerController);

            row.Add(text);
            row.Add(value);
            row.Add(assign);
            return row;
        }

        private void Assign(PlayerController player)
        {
            if (!player || mode == null || !mode.Selected || player.UserId == mode.Selected.OwnerUserId) return;
            var slot = mode.Selected;
            CloseAssign();
            _ = mode.AssignAsync(slot, player);
        }

        private void BindCandidateRow(VisualElement row, int index)
        {
            if (index < 0 || index >= candidates.Count || mode == null || !mode.Selected) return;
            var player = candidates[index];
            var slot = mode.Selected;
            var type = slot.SlotType;
            row.userData = player;

            var name = row.Q<Label>("name");
            name.text = player.PlayerName;
            name.style.color = HudFormat.ReadableNameColor(player.PlayerNameHexColor);

            var isCurrent = player.UserId == slot.OwnerUserId;
            var isBest = ranked.Count > 0 && ranked[0] == player && !isCurrent;
            var tag = row.Q<Label>("tag");
            tag.text = isCurrent ? Localization.UI_BUILD_TAG_CURRENT : isBest ? Localization.UI_BUILD_TAG_BEST : string.Empty;
            tag.style.display = string.IsNullOrEmpty(tag.text) ? DisplayStyle.None : DisplayStyle.Flex;
            tag.EnableInClassList("rf-build-assign__tag--best", isBest);

            var skill = player.Stats != null ? GameMath.GetSkillByHouseType(player.Stats, type) : null;
            var level = skill != null ? string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_SKILL_LEVEL, SkillName(type), (int)skill.Level) : string.Empty;
            var home = mode.HomeOf(player);
            if (home && home != slot) level += "  ·  " + string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_LIVES_IN, home.Slot + 1);
            row.Q<Label>("level").text = level;

            row.Q<Label>("bonus").text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_BONUS, Mathf.RoundToInt(VillageBuildMode.BonusFor(player, type)), SkillName(type));
            row.Q<Button>("assign").SetEnabled(!isCurrent && !mode.Busy);
            row.EnableInClassList("rf-build-assign__row--best", isBest);
        }

        // ---- Hint and toast ------------------------------------------------------------------

        private void RefreshHint()
        {
            string text;
            if (mode.Busy) text = Localization.UI_BUILD_HINT_BUSY;
            else if (mode.IsMoving)
            {
                text = mode.MoveValid
                    ? Localization.UI_BUILD_HINT_MOVING
                    : string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HINT_MOVING_BLOCKED, mode.MoveProblem);
            }
            else if (mode.Armed.HasValue)
            {
                var house = HouseFor(mode.Armed.Value);
                text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HINT_ARMED, Localization.HouseName(house, mode.Armed.Value));
            }
            else if (mode.HallSelected) text = Localization.UI_BUILD_HINT_HALL;
            else if (mode.Selected && VillageBuildMode.IsEmpty(mode.Selected)) text = Localization.UI_BUILD_HINT_EMPTY;
            else if (mode.Selected) text = Localization.UI_BUILD_HINT_BUILT;
            else text = Localization.UI_BUILD_HINT_IDLE;

            hint.text = text;
        }

        private void ShowToast(string message)
        {
            if (!bound) return;
            toast.text = message;
            toast.AddToClassList("rf-build-toast--visible");
            toastHide?.Pause();
            toastHide = toast.schedule.Execute(() => toast.RemoveFromClassList("rf-build-toast--visible")).StartingIn(4000);
        }

        // ---- Helpers -------------------------------------------------------------------------

        private TownHouse HouseFor(TownHouseSlotType type)
        {
            if (mode == null) return null;
            var catalogue = mode.Catalogue;
            for (var i = 0; i < catalogue.Count; ++i)
            {
                if (catalogue[i] && catalogue[i].Type == type) return catalogue[i];
            }

            return null;
        }

        private Sprite IconFor(TownHouseSlotType type)
        {
            // House types are named after the skill they boost, except Melee, which trains the
            // three melee skills and borrows the attack icon.
            var skill = type == TownHouseSlotType.Melee ? Skill.Attack
                : Enum.TryParse<Skill>(type.ToString(), out var parsed) ? parsed
                : Skill.None;
            return iconBySkill.TryGetValue(skill, out var sprite) ? sprite : null;
        }

        private static string SkillName(TownHouseSlotType type)
        {
            return Localization.SkillName(type.ToString());
        }

        private static string CategoryName(Category category)
        {
            switch (category.Name)
            {
                case "Combat": return Localization.UI_BUILD_CATEGORY_COMBAT;
                case "Gathering": return Localization.UI_BUILD_CATEGORY_GATHERING;
                case "Crafting": return Localization.UI_BUILD_CATEGORY_CRAFTING;
                default: return category.Name;
            }
        }

        private static string ResidentName(TownHouseSlot slot)
        {
            if (slot.Player) return slot.Player.PlayerName;
            if (!string.IsNullOrEmpty(slot.PlayerName)) return slot.PlayerName;
            return slot.OwnerUserId.HasValue ? Localization.UI_BUILD_UNKNOWN_RESIDENT : string.Empty;
        }
    }
}
