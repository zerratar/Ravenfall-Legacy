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
            /// <summary>The roads tab: road types to draw instead of buildings.</summary>
            public bool Roads;
            /// <summary>The decor tab: trees, bushes, flowers and rocks to place (docs/village-decor-design.md).</summary>
            public bool Decor;
        }

        /// <summary>
        /// Today's buildings grouped for the toolbar. Production, Decor and Paths join when those
        /// exist (docs/village-build-mode-design.md, phases 3 and 4).
        /// </summary>
        private static readonly Category[] Categories =
        {
            new Category { Name = "Combat", Glyph = "\uf71c", Types = new[] { TownHouseSlotType.Melee, TownHouseSlotType.Ranged, TownHouseSlotType.Magic, TownHouseSlotType.Healing, TownHouseSlotType.Slayer } },
            new Category { Name = "Gathering", Glyph = "\uf6e3", Types = new[] { TownHouseSlotType.Woodcutting, TownHouseSlotType.Mining, TownHouseSlotType.Fishing, TownHouseSlotType.Farming, TownHouseSlotType.Gathering } },
            new Category { Name = "Crafting", Glyph = "\uf6e3", Types = new[] { TownHouseSlotType.Cooking, TownHouseSlotType.Crafting, TownHouseSlotType.Alchemy, TownHouseSlotType.Sailing } },
            new Category { Name = "Roads", Glyph = "\uf018", Types = new TownHouseSlotType[0], Roads = true },
            new Category { Name = "Decor", Glyph = "\uf1bb", Types = new TownHouseSlotType[0], Decor = true },
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
        private VisualElement roadPanel;
        private Label plotOutside;
        private Label areaLabel;
        private Label roadTitle;
        private Label roadSubtitle;
        private VisualElement roadTypes;
        private VisualElement roadRing;
        private Label roadTypeCaption;
        private Label roadCostCaption;
        private Label roadShapeCaption;
        private VisualElement roadShape;
        private VisualElement roadCosts;
        private VisualElement roadKeys;
        private VisualElement roadActions;
        private Button roadPrimary;
        private Button roadSecondary;
        private Button roadRemove;
        private float roadRemoveConfirmUntil;

        // decor: the tray's group and the panel for the piece in hand or selected, made here rather than in the UXML
        private string decorGroup = "Trees";
        private VisualElement decorPanel;
        private VisualElement decorRing;
        private Label decorTitle;
        private Label decorSubtitle;
        private Label decorCaption;
        private Label decorCost;
        private Button decorMove;
        private Button decorRotate;
        private Button decorRemove;
        private float decorRemoveConfirmUntil;
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
        private bool rotateHovered;
        private Button assignButton;
        private Label assignLabel;
        private Button resetButton;
        private VisualElement hallUpgrade;
        private Label hallNext;
        private VisualElement hallNeeds;
        private VisualElement hallGets;
        private Label hallGetsCaption;
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
        private ScrollView trayScroll;
        private VisualElement trayTools;
        private TextField trayFilter;
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
            roadPanel = root.Q<VisualElement>("road-panel");
            plotOutside = root.Q<Label>("plot-outside");
            roadTitle = root.Q<Label>("road-title");
            roadSubtitle = root.Q<Label>("road-subtitle");
            roadTypes = root.Q<VisualElement>("road-types");
            roadRemove = root.Q<Button>("road-remove");
            roadRing = root.Q<VisualElement>("road-ring");
            roadTypeCaption = root.Q<Label>("road-type-caption");
            roadCostCaption = root.Q<Label>("road-cost-caption");
            roadShapeCaption = root.Q<Label>("road-shape-caption");
            roadShape = root.Q<VisualElement>("road-shape");
            roadCosts = root.Q<VisualElement>("road-costs");
            roadKeys = root.Q<VisualElement>("road-keys");
            roadActions = root.Q<VisualElement>("road-actions");
            roadPrimary = root.Q<Button>("road-primary");
            roadSecondary = root.Q<Button>("road-secondary");
            BuildDecorPanel();
            var roadClose = root.Q<Button>("road-close");
            if (roadClose != null) roadClose.clicked += OnRoadCloseClicked;
            if (roadRemove != null) roadRemove.clicked += OnRoadRemoveClicked;
            if (roadPrimary != null) roadPrimary.clicked += OnRoadPrimaryClicked;
            if (roadSecondary != null) roadSecondary.clicked += OnRoadSecondaryClicked;
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
            hallGets = root.Q<VisualElement>("hall-gets");
            hallGetsCaption = root.Q<Label>("hall-gets-caption");
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
            trayScroll = root.Q<ScrollView>("tray-scroll");
            trayTools = root.Q<VisualElement>("tray-tools");
            trayFilter = root.Q<TextField>("tray-filter");
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
            // Pointing at Rotate shows the turn first: the turned footprint, green or red, and the
            // buildings in the way ringed, as moving does. Before, a turn that did not fit only said
            // so, without showing where or what was in the way.
            rotateButton.RegisterCallback<PointerEnterEvent>(_ => { rotateHovered = true; mode?.PreviewRotation(rotateStep); });
            rotateButton.RegisterCallback<PointerLeaveEvent>(_ => { rotateHovered = false; mode?.EndPreview(); });
            rotateButton.clicked += async () =>
            {
                if (mode == null) return;
                // Moving: turn what follows the mouse. Before, the tile was greyed out during a move, so lining a
                // building up with a turned neighbour meant placing it, turning it and picking it up again.
                if (mode.IsMoving)
                {
                    mode.RotateMove(rotateStep);
                    return;
                }

                if (mode.HallSelected) await mode.RotateHallInPlaceAsync(rotateStep);
                else if (mode.Selected) await mode.RotateInPlaceAsync(mode.Selected, rotateStep);
                // still pointing at it: show the next turn
                if (rotateHovered && mode != null) mode.PreviewRotation(rotateStep);
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

            // the tray keeps its width and scrolls sideways (it grew and shrank with each tab, moving the buttons
            // under the mouse); the wheel scrolls it too
            trayScroll.RegisterCallback<WheelEvent>(evt =>
            {
                trayScroll.scrollOffset += new Vector2(evt.delta.y * 60f, 0f);
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            // the filter finds what to build in every tab; Esc clears it, then lets go of the keyboard
            trayFilter.textEdition.placeholder = Localization.UI_BUILD_FILTER;
            trayFilter.RegisterValueChangedCallback(evt =>
            {
                trayScroll.scrollOffset = Vector2.zero;
                BuildTray();
            });
            trayFilter.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape) return;
                if (string.IsNullOrEmpty(trayFilter.value)) trayFilter.Blur();
                else trayFilter.value = string.Empty;
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

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
            mode.Roads.Changed += OnChanged;
            mode.Roads.Failed += ShowToast;
            mode.Decor.Changed += OnChanged;
            mode.Decor.Failed += ShowToast;
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
            mode.Roads.Changed -= OnChanged;
            mode.Roads.Failed -= ShowToast;
            mode.Decor.Changed -= OnChanged;
            mode.Decor.Failed -= ShowToast;
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

            // a click in the world takes the keyboard back from the tray's filter
            if (Input.GetMouseButtonDown(0) && trayFilter.panel?.focusController?.focusedElement is VisualElement focused
                && (focused == trayFilter || trayFilter.Contains(focused)) && !Shinobytes.UI.HudPointer.IsOverHud())
            {
                trayFilter.Blur();
            }

            if (demolishConfirmUntil > 0f && Time.unscaledTime > demolishConfirmUntil)
            {
                demolishConfirmUntil = 0f;
                RefreshDemolish();
            }
        }

        private void LateUpdate()
        {
            if (mode == null || !mode.IsActive)
            {
                if (areaLabel != null) areaLabel.style.display = DisplayStyle.None;
                return;
            }

            if (!cam || !cam.isActiveAndEnabled) cam = Camera.main;
            var panel = markerLayer.panel;
            if (!cam || panel == null) return;

            PlaceAreaLabel(panel);
            if (markers.Count == 0) return;

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

        /// <summary>
        /// Names the ring the next town hall tier opens, on the ring itself, on the side in view.
        /// </summary>
        private void PlaceAreaLabel(IPanel panel)
        {
            if (areaLabel == null)
            {
                areaLabel = new Label { pickingMode = PickingMode.Ignore };
                areaLabel.AddToClassList("rf-build-area-label");
                markerLayer.Add(areaLabel);
            }

            var world = Vector3.zero;
            var show = !mode.IsMoving && mode.TryGetNextAreaLabelPoint(cam, out world)
                && cam.WorldToViewportPoint(world).z > 0f;
            areaLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            areaLabel.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_AREA_NEXT, mode.NextAreaTier, Mathf.RoundToInt(mode.NextAreaRadius));
            var point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world + Vector3.up, cam);
            areaLabel.style.translate = new Translate(point.x, point.y);
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
            RefreshRoad();
            RefreshDecor();
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
            // a tab picked shows that tab, not what was being looked for
            trayFilter.SetValueWithoutNotify(string.Empty);
            trayScroll.scrollOffset = Vector2.zero;
            if (openCategory >= 0)
            {
                BuildTray();
            }
            else trayFilter.Blur();
        }

        private void BuildTray()
        {
            trayCards.Clear();
            trayTools.Clear();
            trayTools.style.display = DisplayStyle.None;
            cards.Clear();
            if (mode == null || openCategory < 0) return;

            // something typed in the filter: what matches it, from every tab
            var query = trayFilter.value?.Trim() ?? string.Empty;
            if (query.Length > 0)
            {
                trayGlyph.text = "";
                trayTitle.text = Localization.UI_BUILD_FILTER_TITLE;
                BuildFilteredTray(query);
                return;
            }

            var category = Categories[openCategory];
            trayGlyph.text = category.Glyph;
            // "COMBAT BUILDINGS", but not "DECOR BUILDINGS" or "ROADS BUILDINGS"
            trayTitle.text = category.Decor || category.Roads
                ? CategoryName(category).ToUpperInvariant()
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_TRAY_TITLE, CategoryName(category).ToUpperInvariant());

            if (category.Roads)
            {
                BuildRoadTray();
                return;
            }

            if (category.Decor)
            {
                BuildDecorTray();
                return;
            }

            foreach (var type in category.Types)
            {
                AddHouseCard(type);
            }

            RefreshCards();
            RefreshCardBonuses();
        }

        /// <summary>
        /// Every building, road and decor piece whose name (or its tab's or group's name) holds the text typed in the
        /// tray's filter, as the cards of their own tabs.
        /// </summary>
        private void BuildFilteredTray(string query)
        {
            bool Matches(params string[] texts)
            {
                foreach (var text in texts)
                {
                    if (!string.IsNullOrEmpty(text) && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                return false;
            }

            foreach (var category in Categories)
            {
                if (category.Roads || category.Decor) continue;
                foreach (var type in category.Types)
                {
                    var house = HouseFor(type);
                    if (house && Matches(Localization.HouseName(house.Type), SkillName(type), CategoryName(category))) AddHouseCard(type);
                }
            }

            var roads = GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.Roads : null;
            if (roads != null)
            {
                foreach (var type in roads.Types)
                {
                    if (Matches(RoadName(type.Id), Localization.UI_BUILD_CATEGORY_ROADS)) AddRoadCard(roads, type);
                }
            }

            var decor = DecorLayer;
            if (decor != null && decor.Loaded)
            {
                foreach (var piece in decor.Pieces)
                {
                    if (Matches(decor.NameOf(piece.Key), Localization.DecorGroup(piece.Group), Localization.UI_DECOR_TAB)) AddDecorCard(decor, piece);
                }
            }

            if (trayCards.childCount == 0)
            {
                var none = new Label(string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_FILTER_NONE, query)) { pickingMode = PickingMode.Ignore };
                none.AddToClassList("rf-build-note");
                trayCards.Add(none);
            }

            RefreshCards();
            RefreshCardBonuses();
        }

        private void AddHouseCard(TownHouseSlotType type)
        {
            var house = HouseFor(type);
            if (!house) return;

            var card = new Card { Type = type, Root = new VisualElement() };
            card.Root.AddToClassList("rf-build-card");

            var preview = new VisualElement { pickingMode = PickingMode.Ignore };
            preview.AddToClassList("rf-build-card__preview");
            // the picture comes a frame or two later the first time; cached after
            BuildingPreview.Get(house.Prefab, texture =>
            {
                if (texture) preview.style.backgroundImage = new StyleBackground(texture);
            });

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
            marker.Plus = new Label("\uf067") { pickingMode = PickingMode.Ignore };
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

            // standing outside the area the town hall allows: kept, but marked
            var outside = !moving && mode.IsOutsideArea(slot);

            var state = (int)slot.SlotType + "|" + selected + "|" + target + "|" + muted + "|" + moving + "|" + blocked + "|" + outside + "|" + name;
            if (state == marker.ShownState) return;
            marker.ShownState = state;

            marker.Root.EnableInClassList("rf-plot--empty", empty && !blocked);
            marker.Root.EnableInClassList("rf-plot--selected", selected && !moving);
            marker.Root.EnableInClassList("rf-plot--target", target);
            marker.Root.EnableInClassList("rf-plot--muted", muted);
            marker.Root.EnableInClassList("rf-plot--moving", moving && !blocked);
            marker.Root.EnableInClassList("rf-plot--moving-blocked", blocked);
            marker.Root.EnableInClassList("rf-plot--outside", outside);

            var sprite = empty ? null : IconFor(slot.SlotType);
            marker.Plus.style.display = sprite ? DisplayStyle.None : DisplayStyle.Flex;
            marker.Plus.text = blocked ? "\uf00d" : "\uf067";
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
            if (plotOutside != null)
            {
                var outside = mode.IsOutsideArea(slot) && !mode.IsMoving;
                plotOutside.style.display = outside ? DisplayStyle.Flex : DisplayStyle.None;
                plotOutside.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_OUTSIDE_NOTE, Mathf.RoundToInt(mode.AreaRadius));
            }

            var idle = !mode.Busy && !mode.IsMoving;
            moveButton.SetEnabled(idle);
            // also while moving: it turns the building that follows the mouse, as R does
            rotateButton.SetEnabled(!mode.Busy);
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
            // also while moving: it turns the building that follows the mouse, as R does
            rotateButton.SetEnabled(!mode.Busy);
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
            hallGets.Clear();
            if (!townHall.HasNextTier)
            {
                hallNext.text = Localization.UI_BUILD_HALL_MAX;
                hallGetsCaption.style.display = DisplayStyle.None;
                hallUpgradeButton.style.display = DisplayStyle.None;
                return;
            }

            // What it takes: one row each, with what the village has, how far along, and a tick
            // once it is there. The red text alone did not read as requirements at all.
            var next = Mathf.Clamp(townHall.BoughtTier.Value + 1, 0, Localization.UI_BUILD_HALL_NAMES.Length - 1);
            hallNext.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_NEXT, Localization.UI_BUILD_HALL_NAMES[next].ToUpperInvariant());
            AddNeed("\uf005", Localization.UI_BUILD_HALL_NEED_LEVEL_NAME, townHall.Level, townHall.NextLevel);
            AddNeed("\uf1bb", Localization.UI_ROAD_RESOURCE_WOOD, townHall.Wood, townHall.NextWood);
            AddNeed("\uf3a5", Localization.UI_ROAD_RESOURCE_ORE, townHall.Ore, townHall.NextOre);
            AddNeed("\uf578", Localization.UI_ROAD_RESOURCE_FISH, townHall.Fish, townHall.NextFish);
            AddNeed("\ue2cd", Localization.UI_ROAD_RESOURCE_WHEAT, townHall.Wheat, townHall.NextWheat);

            // What it gives: the new hall, more room to build in, and the road types it unlocks.
            hallGetsCaption.text = Localization.UI_BUILD_HALL_GETS;
            hallGetsCaption.style.display = DisplayStyle.Flex;
            AddGet("\ue0de", string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_GET_HALL, Localization.UI_BUILD_HALL_NAMES[next]));
            var areaNow = mode.AreaRadiusFor(townHall.BoughtTier.Value);
            var areaNext = mode.AreaRadiusFor(townHall.BoughtTier.Value + 1);
            if (areaNext > areaNow)
            {
                AddGet("\uf31d", string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_GET_ROOM, Mathf.RoundToInt(areaNow), Mathf.RoundToInt(areaNext)));
            }
            var roads = GameManager.Instance.Village.Roads;
            if (roads != null)
            {
                foreach (var type in roads.Types)
                {
                    if (type.Tier == townHall.BoughtTier.Value + 1)
                    {
                        AddGet("\uf018", string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_GET_ROAD, RoadName(type.Id)));
                    }
                }
            }

            hallUpgradeButton.style.display = DisplayStyle.Flex;
            hallUpgradeButton.SetEnabled(idle && townHall.CanUpgrade);
        }

        private void AddNeed(string glyph, string name, long have, long need)
        {
            if (need <= 0) return;
            var met = have >= need;

            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-build-need");
            row.EnableInClassList("rf-build-need--met", met);

            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("rf-build-need__line");
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-build-need__icon");
            var label = new Label(name) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-build-need__name");
            var amount = new Label(string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HALL_NEED_AMOUNT,
                Math.Min(have, need).ToString("N0", CultureInfo.InvariantCulture), need.ToString("N0", CultureInfo.InvariantCulture))) { pickingMode = PickingMode.Ignore };
            amount.AddToClassList("rf-build-need__amount");
            var tick = new Label(met ? "\uf00c" : string.Empty) { pickingMode = PickingMode.Ignore };
            tick.AddToClassList("rf-icon");
            tick.AddToClassList("rf-build-need__tick");
            line.Add(icon);
            line.Add(label);
            line.Add(amount);
            line.Add(tick);

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rf-build-need__bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("rf-build-need__fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(have / (float)need) * 100f);
            bar.Add(fill);

            row.Add(line);
            row.Add(bar);
            hallNeeds.Add(row);
        }

        private void AddGet(string glyph, string text)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-build-get");
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-build-get__icon");
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-build-get__text");
            row.Add(icon);
            row.Add(label);
            hallGets.Add(row);
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

        // ---- Roads ---------------------------------------------------------------------------

        private void BuildRoadTray()
        {
            var roads = GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.Roads : null;
            if (roads == null || roads.Types.Count == 0)
            {
                var none = new Label(Localization.UI_ROAD_NONE) { pickingMode = PickingMode.Ignore };
                none.AddToClassList("rf-build-note");
                trayCards.Add(none);
                return;
            }

            foreach (var type in roads.Types)
            {
                AddRoadCard(roads, type);
            }
        }

        private void AddRoadCard(VillageRoads roads, RavenNest.SDK.Endpoints.RoadType type)
        {
            var id = type.Id;
            var card = new VisualElement();
            card.AddToClassList("rf-build-card");
            card.AddToClassList("rf-build-card--road");
            card.EnableInClassList("rf-build-card--locked", !type.Unlocked);
            card.EnableInClassList("rf-build-card--armed", mode.Roads.DrawType == id);

            // a patch of the road itself, as the house cards show the house
            var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            swatch.AddToClassList("rf-build-card__preview");
            swatch.AddToClassList("rf-build-road-swatch");
            var material = roads.MaterialFor(id);
            if (material && material.mainTexture is Texture2D surface) swatch.style.backgroundImage = new StyleBackground(surface);

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-build-card__info");
            var column = new VisualElement { pickingMode = PickingMode.Ignore };
            var name = new Label(RoadName(id)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-build-card__name");
            var cost = new Label(type.Unlocked
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_PER_METRE, CostText(id, 1f))
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_LOCKED, type.Tier + 1)) { pickingMode = PickingMode.Ignore };
            cost.AddToClassList("rf-build-card__bonus");
            column.Add(name);
            column.Add(cost);
            text.Add(column);

            var draw = new Button(() =>
            {
                mode?.ChooseRoad(id);
                // the road panel takes over while drawing; the tray would only be in the way
                OpenCategory(-1);
            }) { text = Localization.UI_ROAD_DRAW.ToUpperInvariant() }; // as the buildings' BUILD
            draw.AddToClassList("rf-build-card__build");
            draw.SetEnabled(type.Unlocked);

            card.Add(swatch);
            card.Add(text);
            card.Add(draw);
            trayCards.Add(card);
        }

        private void RefreshRoad()
        {
            if (roadPanel == null || mode == null) return;
            var tool = mode.Roads;
            var road = tool.Selected;
            var roads = GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.Roads : null;
            var hall = GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.TownHall : null;
            var show = (road != null || tool.Drawing) && roads != null && hall && !modalOpen;
            roadPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            var drawing = tool.Drawing;
            var dirty = !drawing && tool.Dirty;
            var type = drawing ? tool.DrawType.Value : road.Type;

            // the ring shows the surface, as the tray's cards do
            var material = roads.MaterialFor(type);
            roadRing.style.backgroundImage = material && material.mainTexture is Texture2D surface ? new StyleBackground(surface) : new StyleBackground();

            roadTitle.text = RoadName(type);
            roadSubtitle.text = drawing
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_DRAWING_SUBTITLE, Mathf.RoundToInt(tool.DrawLength))
                : string.Format(CultureInfo.InvariantCulture, dirty ? Localization.UI_ROAD_EDITED_SUBTITLE : Localization.UI_ROAD_LENGTH, Mathf.RoundToInt(tool.EditLength));
            roadPanel.EnableInClassList("rf-build-road--dirty", dirty);


            // Type: a chip each. Drawing, it changes what is drawn; editing, it is a change like any other.
            roadTypeCaption.text = Localization.UI_ROAD_TYPE;
            roadTypes.Clear();
            foreach (var info in roads.Types)
            {
                var id = info.Id;
                var chip = new Button(() =>
                {
                    if (mode == null) return;
                    if (mode.Roads.Drawing) mode.Roads.SetDrawType(id);
                    else mode.Roads.SetType(id);
                }) { text = RoadName(id) };
                chip.AddToClassList("rf-build-road-chip");
                chip.EnableInClassList("rf-build-road-chip--current", type == id);
                chip.EnableInClassList("rf-build-road-chip--saved", dirty && tool.SavedType == id && type != id);
                chip.SetEnabled(info.Unlocked && !tool.Busy);
                if (!info.Unlocked) chip.text = RoadName(id) + "  " + string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_TIER_SHORT, info.Tier + 1);
                roadTypes.Add(chip);
            }

            // Shape, while editing: the picked point's width, and how round each end is.
            roadShapeCaption.style.display = drawing ? DisplayStyle.None : DisplayStyle.Flex;
            roadShape.style.display = drawing ? DisplayStyle.None : DisplayStyle.Flex;
            roadShape.Clear();
            if (!drawing)
            {
                roadShapeCaption.text = Localization.UI_ROAD_SHAPE_CAPTION;
                if (tool.SelectedPoint >= 0)
                {
                    var row = new VisualElement();
                    row.AddToClassList("rf-build-road-shape-row");
                    var label = new Label(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_POINT_WIDTH, tool.SelectedPoint + 1)) { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("rf-build-road-shape-row__name");
                    var narrower = new Button(() => mode?.Roads.ChangePointWidth(-0.1f)) { text = "\uf068" };
                    narrower.AddToClassList("rf-icon");
                    narrower.AddToClassList("rf-build-road-step");
                    narrower.SetEnabled(!tool.Busy && tool.PointWidth > VillageRoads.MinWidth + 0.001f);
                    var value = new Label(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_WIDTH_VALUE, tool.PointWidth.ToString("0.0", CultureInfo.InvariantCulture))) { pickingMode = PickingMode.Ignore };
                    value.AddToClassList("rf-build-road-shape-row__value");
                    var wider = new Button(() => mode?.Roads.ChangePointWidth(0.1f)) { text = "\uf067" };
                    wider.AddToClassList("rf-icon");
                    wider.AddToClassList("rf-build-road-step");
                    wider.SetEnabled(!tool.Busy && tool.PointWidth < VillageRoads.MaxWidth - 0.001f);
                    row.Add(label); row.Add(narrower); row.Add(value); row.Add(wider);
                    roadShape.Add(row);
                }
                else
                {
                    var hint = new Label(Localization.UI_ROAD_PICK_POINT) { pickingMode = PickingMode.Ignore };
                    hint.AddToClassList("rf-build-road-shape-hint");
                    roadShape.Add(hint);
                }
                AddEndRow(Localization.UI_ROAD_START, true, road.StartRound);
                AddEndRow(Localization.UI_ROAD_END, false, road.EndRound);
            }

            // Cost: what the road drawn so far costs, or what applying the change costs or gives back.
            roadCosts.Clear();
            if (drawing)
            {
                roadCostCaption.text = Localization.UI_ROAD_COST_CAPTION;
                AddRoadRow("\uf545", Localization.UI_ROAD_ROW_LENGTH, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_METRES, Mathf.RoundToInt(tool.DrawLength)), null, null);
                AddPayRows(roads.Cost(type, tool.DrawLength), roads.Cost(type, 1f), hall, false);
            }
            else if (dirty)
            {
                roadCostCaption.text = Localization.UI_ROAD_CHANGE_CAPTION;
                AddRoadRow("\uf545", Localization.UI_ROAD_ROW_LENGTH, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_CHANGE_TO,
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_METRES, Mathf.RoundToInt(tool.SavedLength)),
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_METRES, Mathf.RoundToInt(tool.EditLength))), null, null);
                if (tool.WidthsChanged)
                {
                    // the average width along the road, as the price counts it
                    float Average(IReadOnlyList<Vector2> p, IReadOnlyList<float> w) { var l = VillageRoads.PaidLength(p); return l > 0f ? VillageRoads.PaidLength(p, w) / l : 1f; }
                    var before = string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_WIDTH_VALUE, tool.SavedAverageWidth.ToString("0.00", CultureInfo.InvariantCulture));
                    var after = string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_WIDTH_VALUE, Average(road.Points, road.Widths).ToString("0.00", CultureInfo.InvariantCulture));
                    AddRoadRow("\uf337", Localization.UI_ROAD_ROW_WIDTH, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_CHANGE_TO, before, after), null, null);
                }
                if (tool.SavedType != road.Type)
                {
                    AddRoadRow("\uf018", Localization.UI_ROAD_ROW_TYPE, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_CHANGE_TO, RoadName(tool.SavedType), RoadName(road.Type)), null, null);
                }
                var c = tool.EditCost();
                if (c.wood == 0 && c.ore == 0 && c.fish == 0 && c.wheat == 0 && c.coins == 0)
                {
                    AddRoadRow("\uf00c", Localization.UI_ROAD_NO_COST, string.Empty, null, "rf-build-road-row--refund");
                }
                else AddPayRows(c, default, hall, true);
            }
            else
            {
                roadCostCaption.text = Localization.UI_ROAD_ABOUT_CAPTION;
                AddRoadRow("\uf545", Localization.UI_ROAD_ROW_LENGTH, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_METRES, Mathf.RoundToInt(tool.EditLength)), null, null);
                AddRoadRow("\uf3c5", Localization.UI_ROAD_ROW_POINTS, road.Points.Count.ToString(CultureInfo.InvariantCulture), null, null);
                AddRoadRow("\uf51e", Localization.UI_ROAD_ROW_PRICE, string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_PER_METRE, CostText(type, 1f)), null, null);
            }

            // Keys: what the mouse and keyboard do right now.
            roadKeys.Clear();
            if (drawing)
            {
                AddRoadKey(Localization.UI_ROAD_KEYNAME_CLICK, Localization.UI_ROAD_KEY_ADD);
                AddRoadKey("Enter", Localization.UI_ROAD_KEY_BUILD);
                AddRoadKey(Localization.UI_ROAD_KEYNAME_BACKSPACE, Localization.UI_ROAD_UNDO);
                AddRoadKey("Esc", Localization.UI_ROAD_KEY_STOP);
                AddRoadKey("Alt", Localization.UI_ROAD_KEY_FREE);
                AddRoadKey("Shift", Localization.UI_ROAD_KEY_ANGLE);
            }
            else
            {
                AddRoadKey(Localization.UI_ROAD_KEYNAME_DRAG, Localization.UI_ROAD_KEY_MOVE);
                AddRoadKey(Localization.UI_ROAD_KEYNAME_CTRL_CLICK, Localization.UI_ROAD_KEY_ADD);
                AddRoadKey(Localization.UI_ROAD_KEYNAME_RIGHT_CLICK, Localization.UI_ROAD_KEY_REMOVE);
                AddRoadKey("[ ]", Localization.UI_ROAD_KEY_WIDTH);
                AddRoadKey("Esc", dirty ? Localization.UI_ROAD_KEY_UNDO_CHANGES : Localization.UI_ROAD_KEY_DESELECT);
                if (dirty) AddRoadKey("Enter", Localization.UI_ROAD_APPLY);
            }

            // Buttons: build and undo while drawing, apply and cancel with changes, else remove.
            var idle = !tool.Busy;
            if (drawing)
            {
                roadActions.style.display = DisplayStyle.Flex;
                roadPrimary.text = tool.Busy ? Localization.UI_BUILD_HINT_BUSY : Localization.UI_ROAD_BUILD;
                roadPrimary.SetEnabled(idle && tool.DrawPointCount >= 2 && tool.DrawAffordable);
                roadSecondary.text = Localization.UI_ROAD_UNDO;
                roadSecondary.SetEnabled(idle && tool.DrawPointCount > 0);
                roadRemove.style.display = DisplayStyle.None;
            }
            else if (dirty)
            {
                roadActions.style.display = DisplayStyle.Flex;
                roadPrimary.text = tool.Busy ? Localization.UI_BUILD_HINT_BUSY : Localization.UI_ROAD_APPLY;
                roadPrimary.SetEnabled(idle && tool.EditAffordable());
                roadSecondary.text = Localization.UI_ROAD_CANCEL;
                roadSecondary.SetEnabled(idle);
                roadRemove.style.display = DisplayStyle.None;
            }
            else
            {
                roadActions.style.display = DisplayStyle.None;
                roadRemove.style.display = DisplayStyle.Flex;
                var confirming = Time.unscaledTime < roadRemoveConfirmUntil;
                roadRemove.text = confirming ? Localization.UI_ROAD_REMOVE_CONFIRM : Localization.UI_ROAD_REMOVE;
                roadRemove.EnableInClassList("rf-build-wide--confirm", confirming);
                roadRemove.SetEnabled(idle);
            }
        }

        /// <summary>
        /// A row per resource the road costs. With <paramref name="change"/> the amounts are a
        /// difference, and negative ones are given back. Short of something, the row goes red.
        /// </summary>
        private void AddPayRows((long wood, long ore, long fish, long wheat, long coins) c, (long wood, long ore, long fish, long wheat, long coins) perMetre, TownHallManager hall, bool change)
        {
            void Row(string glyph, string name, long amount, long rate, long have)
            {
                // drawing, every resource of the type shows from the first point on
                if (amount == 0 && (change || rate == 0)) return;
                if (amount < 0)
                {
                    AddRoadRow(glyph, name, "+" + (-amount).ToString("N0", CultureInfo.InvariantCulture), Localization.UI_ROAD_BACK, "rf-build-road-row--refund");
                    return;
                }
                AddRoadRow(glyph, name, amount.ToString("N0", CultureInfo.InvariantCulture),
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_HAVE, have.ToString("N0", CultureInfo.InvariantCulture)),
                    have < amount ? "rf-build-road-row--short" : null);
            }

            Row("\uf1bb", Localization.UI_ROAD_RESOURCE_WOOD, c.wood, perMetre.wood, hall.Wood);
            Row("\uf3a5", Localization.UI_ROAD_RESOURCE_ORE, c.ore, perMetre.ore, hall.Ore);
            Row("\uf578", Localization.UI_ROAD_RESOURCE_FISH, c.fish, perMetre.fish, hall.Fish);
            Row("\ue2cd", Localization.UI_ROAD_RESOURCE_WHEAT, c.wheat, perMetre.wheat, hall.Wheat);
            Row("\uf51e", Localization.UI_ROAD_RESOURCE_COINS, c.coins, perMetre.coins, hall.Coins);
        }

        private void AddRoadRow(string glyph, string name, string value, string aside, string modifier)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-build-road-row");
            if (modifier != null) row.AddToClassList(modifier);
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-build-road-row__icon");
            var label = new Label(name) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-build-road-row__name");
            var amount = new Label(value) { pickingMode = PickingMode.Ignore };
            amount.AddToClassList("rf-build-road-row__value");
            row.Add(icon);
            row.Add(label);
            row.Add(amount);
            if (aside != null)
            {
                var note = new Label(aside) { pickingMode = PickingMode.Ignore };
                note.AddToClassList("rf-build-road-row__aside");
                row.Add(note);
            }
            roadCosts.Add(row);
        }

        /// <summary>One end of the road: round, soft or square, the one it is lit.</summary>
        private void AddEndRow(string name, bool start, float round)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-build-road-shape-row");
            var label = new Label(name) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-build-road-shape-row__name");
            row.Add(label);
            foreach (var (text, value) in new[] { (Localization.UI_ROAD_END_ROUND, 1f), (Localization.UI_ROAD_END_SOFT, 0.5f), (Localization.UI_ROAD_END_SQUARE, 0f) })
            {
                var chip = new Button(() => mode?.Roads.SetEnd(start, value)) { text = text };
                chip.AddToClassList("rf-build-road-chip");
                chip.AddToClassList("rf-build-road-chip--small");
                chip.EnableInClassList("rf-build-road-chip--current", Mathf.Abs(round - value) < 0.26f);
                chip.SetEnabled(!mode.Roads.Busy);
                row.Add(chip);
            }
            roadShape.Add(row);
        }

        private void AddRoadKey(string key, string text)
        {
            var pair = new VisualElement { pickingMode = PickingMode.Ignore };
            pair.AddToClassList("rf-build-road-key");
            var cap = new Label(key) { pickingMode = PickingMode.Ignore };
            cap.AddToClassList("rf-build-road-key__cap");
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-build-road-key__text");
            pair.Add(cap);
            pair.Add(label);
            roadKeys.Add(pair);
        }

        private void OnRoadPrimaryClicked()
        {
            var tool = mode?.Roads;
            if (tool == null) return;
            if (tool.Drawing) tool.Finish();
            else tool.Apply();
        }

        private void OnRoadSecondaryClicked()
        {
            var tool = mode?.Roads;
            if (tool == null) return;
            if (tool.Drawing) tool.UndoPoint();
            else tool.CancelEdit();
        }

        private void OnRoadCloseClicked()
        {
            var tool = mode?.Roads;
            if (tool == null) return;
            if (tool.Drawing) tool.CancelDrawing();
            else mode.ClearSelection();
        }

        private void OnRoadRemoveClicked()
        {
            // a second click within a few seconds, as demolishing a building asks
            if (Time.unscaledTime < roadRemoveConfirmUntil)
            {
                roadRemoveConfirmUntil = 0f;
                mode?.Roads.RemoveSelected();
                return;
            }

            roadRemoveConfirmUntil = Time.unscaledTime + 3f;
            RefreshRoad();
            roadRemove.schedule.Execute(RefreshRoad).StartingIn(3100);
        }

        private static string RoadName(int type)
        {
            switch (type)
            {
                case 0: return Localization.UI_ROAD_DIRT;
                case 1: return Localization.UI_ROAD_GRAVEL;
                case 2: return Localization.UI_ROAD_COBBLES;
                case 3: return Localization.UI_ROAD_PAVING;
                default: return Localization.UI_BUILD_CATEGORY_ROADS;
            }
        }

        /// <summary>"24 wood, 4 coins": what that much road of the type costs.</summary>
        private static string CostText(int type, float length)
        {
            var roads = GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.Roads : null;
            if (roads == null) return string.Empty;
            var c = roads.Cost(type, length);
            var parts = new List<string>();
            if (c.wood > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_COST_WOOD, c.wood));
            if (c.ore > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_COST_ORE, c.ore));
            if (c.fish > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_COST_FISH, c.fish));
            if (c.wheat > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_COST_WHEAT, c.wheat));
            if (c.coins > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_ROAD_COST_COINS, c.coins));
            return parts.Count == 0 ? Localization.UI_ROAD_FREE : string.Join(", ", parts);
        }

        // ---- Decor ---------------------------------------------------------------------------

        private VillageDecor DecorLayer => GameManager.Instance && GameManager.Instance.Village ? GameManager.Instance.Village.Decor : null;

        /// <summary>The decor tray: a chip per group above, and that group's pieces as cards, like the buildings.</summary>
        private void BuildDecorTray()
        {
            var decor = DecorLayer;
            if (decor == null || !decor.Loaded || decor.Pieces.Count == 0)
            {
                var none = new Label(Localization.UI_DECOR_NONE) { pickingMode = PickingMode.Ignore };
                none.AddToClassList("rf-build-note");
                trayCards.Add(none);
                return;
            }

            var groups = new List<string>();
            foreach (var piece in decor.Pieces) if (!groups.Contains(piece.Group)) groups.Add(piece.Group);
            if (!groups.Contains(decorGroup)) decorGroup = groups.Count > 0 ? groups[0] : null;
            foreach (var group in groups)
            {
                var g = group;
                var chip = new Button(() =>
                {
                    decorGroup = g;
                    trayScroll.scrollOffset = Vector2.zero;
                    BuildTray();
                });
                chip.AddToClassList("rf-build-decor__group");
                chip.EnableInClassList("rf-build-decor__group--active", g == decorGroup);
                var glyph = new Label(DecorGroupGlyph(g)) { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("rf-icon");
                glyph.AddToClassList("rf-build-decor__group-icon");
                var label = new Label(Localization.DecorGroup(g)) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("rf-build-decor__group-label");
                chip.Add(glyph);
                chip.Add(label);
                trayTools.Add(chip);
            }
            trayTools.style.display = DisplayStyle.Flex;

            foreach (var piece in decor.Pieces)
            {
                if (piece.Group == decorGroup) AddDecorCard(decor, piece);
            }
        }

        private void AddDecorCard(VillageDecor decor, RavenNest.SDK.Endpoints.DecorPiece piece)
        {
            var key = piece.Key;
            var card = new VisualElement();
            card.AddToClassList("rf-build-card");
            card.AddToClassList("rf-build-card--decor");
            card.EnableInClassList("rf-build-card--locked", !piece.Unlocked);
            card.EnableInClassList("rf-build-card--armed", mode.Decor.Armed == key);

            var preview = new VisualElement { pickingMode = PickingMode.Ignore };
            preview.AddToClassList("rf-build-card__preview");
            var prefab = decor.PrefabFor(key);
            if (prefab)
            {
                BuildingPreview.Get(prefab, texture =>
                {
                    if (texture) preview.style.backgroundImage = new StyleBackground(texture);
                });
            }

            var info = new VisualElement { pickingMode = PickingMode.Ignore };
            info.AddToClassList("rf-build-card__info");
            var ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("rf-build-card__ring");
            var glyph = new Label(DecorGroupGlyph(piece.Group)) { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("rf-icon");
            glyph.AddToClassList("rf-build-card__glyph");
            ring.Add(glyph);

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.style.flexShrink = 1;
            var name = new Label(decor.NameOf(key)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-build-card__name");
            var cost = new Label(piece.Unlocked ? DecorCost(piece) : DecorLocked(piece)) { pickingMode = PickingMode.Ignore };
            cost.AddToClassList("rf-build-card__bonus");
            text.Add(name);
            text.Add(cost);
            info.Add(ring);
            info.Add(text);

            var place = new Button(() =>
            {
                mode?.ChooseDecor(key);
                // the decor panel takes over while placing; the tray would only be in the way
                OpenCategory(-1);
            }) { text = Localization.UI_DECOR_PLACE.ToUpperInvariant() }; // as the buildings' BUILD
            place.AddToClassList("rf-build-card__build");
            place.SetEnabled(piece.Unlocked);

            card.Add(preview);
            card.Add(info);
            card.Add(place);
            trayCards.Add(card);
        }

        /// <summary>The icon of a decor group, on its chip and on its pieces' cards.</summary>
        private static string DecorGroupGlyph(string group)
        {
            switch (group)
            {
                case "Trees": return "";
                case "Bushes": return "";
                case "Flowers": return "";
                case "Rocks": return "";
                case "Fences": return "";
                case "Lights": return "";
                case "Market": return "";
                case "Farm": return "";
                case "Village": return "";
                case "Banners": return "";
                case "Statues": return "";
                default: return "";
            }
        }

        private static string DecorCost(RavenNest.SDK.Endpoints.DecorPiece piece)
        {
            var text = VillageDecorTool.CostText(piece);
            return string.IsNullOrEmpty(text) ? Localization.UI_DECOR_FREE : text;
        }

        /// <summary>What a locked piece waits for: the town hall's tier first, then the village's level.</summary>
        private static string DecorLocked(RavenNest.SDK.Endpoints.DecorPiece piece)
        {
            var townHall = GameManager.Instance && GameManager.Instance.Village != null ? GameManager.Instance.Village.TownHall : null;
            var tier = townHall ? townHall.Tier : 0;
            return piece.Level > 0 && tier >= piece.Tier
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_LEVEL, piece.Level)
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED, piece.Tier + 1);
        }

        /// <summary>The panel for the decor in hand or selected, built once, in the road panel's look.</summary>
        private void BuildDecorPanel()
        {
            if (decorPanel != null || roadPanel == null || roadPanel.parent == null) return;

            decorPanel = new VisualElement();
            decorPanel.AddToClassList("rf-build-panel");
            decorPanel.AddToClassList("rf-build-plot");
            decorPanel.AddToClassList("rf-build-road");
            decorPanel.AddToClassList("rf-build-decor-panel");
            decorPanel.style.display = DisplayStyle.None;

            var head = new VisualElement();
            head.AddToClassList("rf-build-panel__head");
            decorRing = new VisualElement { pickingMode = PickingMode.Ignore };
            decorRing.AddToClassList("rf-build-ring");
            decorRing.AddToClassList("rf-build-road__ring");
            var titles = new VisualElement { pickingMode = PickingMode.Ignore };
            titles.AddToClassList("rf-build-plot__titles");
            decorTitle = new Label { pickingMode = PickingMode.Ignore };
            decorTitle.AddToClassList("rf-build-plot__title");
            decorSubtitle = new Label { pickingMode = PickingMode.Ignore };
            decorSubtitle.AddToClassList("rf-build-plot__subtitle");
            titles.Add(decorTitle);
            titles.Add(decorSubtitle);
            var close = new Button(() => mode?.ClearSelection()) { text = "\uf00d" };
            close.AddToClassList("rf-window__button");
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-build__close");
            head.Add(decorRing);
            head.Add(titles);
            head.Add(close);

            var body = new VisualElement();
            body.AddToClassList("rf-build-road__body");
            decorCaption = new Label();
            decorCaption.AddToClassList("rf-build-caption");
            decorCaption.AddToClassList("rf-build-road__caption");
            decorCost = new Label();
            decorCost.AddToClassList("rf-build-decor-panel__cost");
            body.Add(decorCaption);
            body.Add(decorCost);

            var actions = new VisualElement();
            actions.AddToClassList("rf-build-road__actions");
            decorMove = new Button(() =>
            {
                if (mode == null) return;
                if (mode.Decor.Armed != null) mode.Decor.Stop();
                else mode.Decor.StartMove();
            });
            decorMove.AddToClassList("rf-build-road__button");
            decorMove.AddToClassList("rf-build-road__button--primary");
            decorRotate = new Button(() =>
            {
                if (mode == null) return;
                if (mode.Decor.Armed != null || mode.Decor.Moving) mode.Decor.Turn(45f);
                else _ = mode.Decor.RotateSelectedAsync(45f);
            }) { text = Localization.UI_DECOR_ROTATE };
            decorRotate.AddToClassList("rf-build-road__button");
            actions.Add(decorMove);
            actions.Add(decorRotate);

            decorRemove = new Button(OnDecorRemove);
            decorRemove.AddToClassList("rf-build-wide");
            decorRemove.AddToClassList("rf-build-wide--danger");

            decorPanel.Add(head);
            decorPanel.Add(body);
            decorPanel.Add(actions);
            decorPanel.Add(decorRemove);

            var parent = roadPanel.parent;
            parent.Insert(parent.IndexOf(roadPanel) + 1, decorPanel);
        }

        private void RefreshDecor()
        {
            if (decorPanel == null || mode == null) return;
            var tool = mode.Decor;
            var decor = DecorLayer;
            var key = tool.Armed ?? (tool.Selected ? tool.Selected.Piece : null);
            var show = key != null && decor != null && !modalOpen;
            decorPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            var piece = decor.PieceOf(key);
            decorTitle.text = decor.NameOf(key);
            var prefab = decor.PrefabFor(key);
            if (prefab) BuildingPreview.Get(prefab, texture => { if (texture) decorRing.style.backgroundImage = new StyleBackground(texture); });

            if (tool.Armed != null)
            {
                decorSubtitle.text = Localization.UI_DECOR_PLACING_SUBTITLE;
                decorCaption.text = Localization.UI_DECOR_COST;
                decorCost.text = piece != null ? DecorCost(piece) : string.Empty;
                decorMove.text = Localization.UI_DECOR_STOP;
                decorRemove.style.display = DisplayStyle.None;
            }
            else
            {
                var item = tool.Selected;
                decorSubtitle.text = tool.Moving ? Localization.UI_DECOR_MOVING_SUBTITLE : item.IsDefault ? Localization.UI_DECOR_ISLAND : Localization.UI_DECOR_PLACED;
                decorCaption.text = Localization.UI_DECOR_REFUND;
                decorCost.text = piece != null
                    ? NonEmpty(VillageDecorTool.RefundText(Math.Floor(piece.Wood / 2), Math.Floor(piece.Ore / 2), Math.Floor(piece.Fish / 2), Math.Floor(piece.Wheat / 2), Math.Floor(piece.Coins / 2)))
                    : string.Empty;
                decorMove.text = Localization.UI_DECOR_MOVE;
                decorRemove.style.display = DisplayStyle.Flex;
                var confirming = Time.unscaledTime < decorRemoveConfirmUntil;
                decorRemove.text = confirming ? Localization.UI_BUILD_DEMOLISH_CONFIRM : Localization.UI_DECOR_REMOVE;
            }

            decorMove.SetEnabled(!tool.Busy && !tool.Moving);
            decorRotate.SetEnabled(!tool.Busy);
            decorRemove.SetEnabled(!tool.Busy && !tool.Moving);
        }

        private static string NonEmpty(string text) => string.IsNullOrEmpty(text) ? Localization.UI_DECOR_FREE : text;

        private void OnDecorRemove()
        {
            if (mode == null || !mode.Decor.Selected) return;
            if (Time.unscaledTime < decorRemoveConfirmUntil)
            {
                decorRemoveConfirmUntil = 0f;
                _ = mode.Decor.RemoveSelectedAsync();
                return;
            }

            decorRemoveConfirmUntil = Time.unscaledTime + 3f;
            RefreshDecor();
            decorRemove.schedule.Execute(RefreshDecor).StartingIn(3100);
        }

        // ---- Hint and toast ------------------------------------------------------------------

        private void RefreshHint()
        {
            string text;
            var tool = mode.Roads;
            var decorTool = mode.Decor;
            if (mode.Busy || tool.Busy || decorTool.Busy) text = Localization.UI_BUILD_HINT_BUSY;
            else if (decorTool.Armed != null || decorTool.Moving)
            {
                text = decorTool.Problem != null
                    ? string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HINT_MOVING_BLOCKED, decorTool.Problem)
                    : decorTool.Moving ? Localization.UI_DECOR_MOVING_SUBTITLE : Localization.UI_DECOR_PLACING_SUBTITLE;
            }
            else if (decorTool.Selected) text = Localization.UI_DECOR_HINT_SELECTED;
            else if (tool.Drawing)
            {
                var length = Mathf.RoundToInt(tool.DrawLength);
                var cost = CostText(tool.DrawType.Value, tool.DrawLength);
                text = length <= 0 ? Localization.UI_ROAD_HINT_START
                    : string.Format(CultureInfo.InvariantCulture, tool.DrawAffordable ? Localization.UI_ROAD_HINT_DRAWING : Localization.UI_ROAD_HINT_TOO_DEAR, length, cost);
            }
            else if (tool.Selected != null) text = Localization.UI_ROAD_HINT_SELECTED;
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
                case "Roads": return Localization.UI_BUILD_CATEGORY_ROADS;
                case "Decor": return Localization.UI_DECOR_TAB;
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
