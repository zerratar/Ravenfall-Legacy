using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Skill = RavenNest.Models.Skill;

namespace Shinobytes.UI
{
    /// <summary>
    /// The interface for village build mode: plot markers over the world, the side panel, the
    /// category toolbar and building tray, and the resident picker. State and server calls live in
    /// <see cref="VillageBuildMode"/>; this class only draws it and forwards clicks.
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

        [Tooltip("How often the side panel and markers re-read residents and bonuses.")]
        [SerializeField] private float refreshInterval = 0.5f;

        [Tooltip("Height of the plot markers above the plot, in metres.")]
        [SerializeField] private float markerHeight = 3f;

        private sealed class Category
        {
            public string Name;
            public string Glyph;
            public TownHouseSlotType[] Types;
        }

        /// <summary>
        /// Today's buildings grouped for the toolbar. Production, Decor and Paths join when those
        /// exist (docs/village-build-mode-design.md, phase 4).
        /// </summary>
        private static readonly Category[] Categories =
        {
            new Category { Name = "Combat", Glyph = "", Types = new[] { TownHouseSlotType.Melee, TownHouseSlotType.Ranged, TownHouseSlotType.Magic, TownHouseSlotType.Healing, TownHouseSlotType.Slayer } },
            new Category { Name = "Gathering", Glyph = "", Types = new[] { TownHouseSlotType.Woodcutting, TownHouseSlotType.Mining, TownHouseSlotType.Fishing, TownHouseSlotType.Farming, TownHouseSlotType.Gathering } },
            new Category { Name = "Crafting", Glyph = "", Types = new[] { TownHouseSlotType.Cooking, TownHouseSlotType.Crafting, TownHouseSlotType.Alchemy, TownHouseSlotType.Sailing } },
        };

        private sealed class Marker
        {
            public TownHouseSlot Slot;
            public VisualElement Root;
            public VisualElement Icon;
            public Label Plus;
            public Label Name;
            public string ShownState;
        }

        private sealed class Card
        {
            public TownHouseSlotType Type;
            public VisualElement Root;
            public Label Bonus;
        }

        private readonly Dictionary<TownHouseSlot, Marker> markers = new Dictionary<TownHouseSlot, Marker>();
        private readonly List<TownHouseSlot> markerScratch = new List<TownHouseSlot>();
        private readonly List<Card> cards = new List<Card>();
        private readonly List<Button> tabs = new List<Button>();
        private readonly List<PlayerController> candidates = new List<PlayerController>();
        private readonly Dictionary<Skill, Sprite> iconBySkill = new Dictionary<Skill, Sprite>();

        private VisualElement root;
        private VisualElement markerLayer;
        private VisualElement summary;
        private Label summaryTitle;
        private Label summarySubtitle;
        private VisualElement summaryList;
        private Label summaryEmpty;
        private VisualElement plot;
        private VisualElement plotIcon;
        private Label plotTitle;
        private Label plotSubtitle;
        private Label plotEmptyNote;
        private VisualElement plotBuilt;
        private Label residentCaption;
        private Label resident;
        private Label residentLevel;
        private Label bonus;
        private Label bonusNote;
        private Button assignButton;
        private Button demolishButton;
        private VisualElement tray;
        private VisualElement toolbar;
        private Label hint;
        private Label toast;
        private VisualElement modal;
        private Label assignTitle;
        private Label assignCurrent;
        private ListView assignList;
        private Label assignEmpty;
        private bool bound;

        private VillageBuildMode mode;
        private int category;
        private float nextRefresh;
        private float demolishConfirmUntil;
        private IVisualElementScheduledItem toastHide;
        private Camera cam;

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
            summary = root.Q<VisualElement>("side-summary");
            summaryTitle = root.Q<Label>("summary-title");
            summarySubtitle = root.Q<Label>("summary-subtitle");
            summaryList = root.Q<VisualElement>("summary-list");
            summaryEmpty = root.Q<Label>("summary-empty");
            plot = root.Q<VisualElement>("side-plot");
            plotIcon = root.Q<VisualElement>("plot-icon");
            plotTitle = root.Q<Label>("plot-title");
            plotSubtitle = root.Q<Label>("plot-subtitle");
            plotEmptyNote = root.Q<Label>("plot-empty-note");
            plotBuilt = root.Q<VisualElement>("plot-built");
            residentCaption = root.Q<Label>("plot-resident-caption");
            resident = root.Q<Label>("plot-resident");
            residentLevel = root.Q<Label>("plot-resident-level");
            bonus = root.Q<Label>("plot-bonus");
            bonusNote = root.Q<Label>("plot-bonus-note");
            assignButton = root.Q<Button>("plot-assign");
            demolishButton = root.Q<Button>("plot-demolish");
            tray = root.Q<VisualElement>("build-tray");
            toolbar = root.Q<VisualElement>("build-toolbar");
            hint = root.Q<Label>("build-hint");
            toast = root.Q<Label>("build-toast");
            modal = root.Q<VisualElement>("assign-modal");
            assignTitle = root.Q<Label>("assign-title");
            assignCurrent = root.Q<Label>("assign-current");
            assignList = root.Q<ListView>("assign-list");
            assignEmpty = root.Q<Label>("assign-empty");

            summaryTitle.text = Localization.UI_BUILD_SUMMARY_TITLE;
            summaryEmpty.text = Localization.UI_BUILD_SUMMARY_EMPTY;
            residentCaption.text = Localization.UI_BUILD_RESIDENT;
            plotEmptyNote.text = Localization.UI_BUILD_EMPTY_PLOT_NOTE;
            assignEmpty.text = Localization.UI_BUILD_NO_PLAYERS;

            root.Q<Button>("plot-close").clicked += () => mode?.ClearSelection();
            root.Q<Button>("assign-close").clicked += CloseAssign;
            assignButton.clicked += OpenAssign;
            demolishButton.clicked += OnDemolishClicked;

            // A click on the dimmed backdrop closes the picker, a click on the panel does not.
            modal.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == modal) CloseAssign();
            });

            assignList.itemsSource = candidates;
            assignList.makeItem = MakeCandidateRow;
            assignList.bindItem = BindCandidateRow;
            assignList.fixedItemHeight = 52f;

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
                RefreshMarkers();
                RefreshSide();
                RefreshCardBonuses();
            }

            if (demolishConfirmUntil > 0f && Time.unscaledTime > demolishConfirmUntil)
            {
                demolishConfirmUntil = 0f;
                RefreshDemolishButton();
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
                if (!slot)
                {
                    continue;
                }

                var world = slot.transform.position + Vector3.up * (markerHeight + slot.IconOffset);
                var viewport = cam.WorldToViewportPoint(world);
                var visible = viewport.z > 0f;
                marker.Root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
                if (!visible) continue;

                var point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                marker.Root.style.translate = new Translate(point.x, point.y);
            }
        }

        private void OnActiveChanged(bool active)
        {
            if (!bound) return;

            root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (!active)
            {
                CloseAssign();
                markerLayer.Clear();
                markers.Clear();
                return;
            }

            SelectCategory(category);
            nextRefresh = 0f;
        }

        private void OnChanged()
        {
            if (!bound || mode == null || !mode.IsActive) return;

            // A selected building switches the tray to its category, so its replacements are there.
            if (mode.Selected && !VillageBuildMode.IsEmpty(mode.Selected))
            {
                var index = CategoryOf(mode.Selected.SlotType);
                if (index >= 0 && index != category) SelectCategory(index);
            }

            demolishConfirmUntil = 0f;
            RefreshMarkers();
            RefreshSide();
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
                var tab = new Button(() => SelectCategory(index)) { text = Categories[i].Name };
                tab.AddToClassList("rf-build-tab");

                var glyph = new Label(Categories[i].Glyph) { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("rf-icon");
                glyph.AddToClassList("rf-build-tab__icon");
                tab.Add(glyph);

                toolbar.Add(tab);
                tabs.Add(tab);
            }
        }

        private void SelectCategory(int index)
        {
            category = Mathf.Clamp(index, 0, Categories.Length - 1);
            for (var i = 0; i < tabs.Count; ++i)
            {
                tabs[i].EnableInClassList("rf-build-tab--active", i == category);
            }

            BuildTray();
        }

        private void BuildTray()
        {
            tray.Clear();
            cards.Clear();
            if (mode == null) return;

            var catalogue = mode.Catalogue;
            foreach (var type in Categories[category].Types)
            {
                TownHouse house = null;
                for (var i = 0; i < catalogue.Count; ++i)
                {
                    if (catalogue[i] && catalogue[i].Type == type) { house = catalogue[i]; break; }
                }

                if (!house) continue;

                var card = new Card { Type = type, Root = new VisualElement() };
                card.Root.AddToClassList("rf-build-card");

                var preview = new VisualElement { pickingMode = PickingMode.Ignore };
                preview.AddToClassList("rf-build-card__preview");
                var texture = BuildingPreview.Get(house.Prefab);
                if (texture) preview.style.backgroundImage = new StyleBackground(texture);

                var name = new Label(house.Name) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-build-card__name");

                var skillRow = new VisualElement { pickingMode = PickingMode.Ignore };
                skillRow.AddToClassList("rf-build-card__skill");
                var skillIcon = new VisualElement { pickingMode = PickingMode.Ignore };
                skillIcon.AddToClassList("rf-build-card__skill-icon");
                var sprite = IconFor(type);
                if (sprite) skillIcon.style.backgroundImage = new StyleBackground(sprite);
                card.Bonus = new Label { pickingMode = PickingMode.Ignore };
                card.Bonus.AddToClassList("rf-build-card__bonus");
                skillRow.Add(skillIcon);
                skillRow.Add(card.Bonus);

                card.Root.Add(preview);
                card.Root.Add(name);
                card.Root.Add(skillRow);
                card.Root.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => mode?.Choose(type));

                tray.Add(card.Root);
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
                card.Root.EnableInClassList("rf-build-card--disabled", mode.Busy);
            }
        }

        /// <summary>Each card shows the bonus the best available player would give, as a reason to pick it.</summary>
        private void RefreshCardBonuses()
        {
            if (mode == null) return;
            foreach (var card in cards)
            {
                var ranked = mode.RankResidents(card.Type);
                var best = ranked.Count > 0 ? VillageBuildMode.BonusFor(ranked[0], card.Type) : 0f;
                var text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_CARD_BONUS, Mathf.RoundToInt(best), SkillName(card.Type));
                if (card.Bonus.text != text) card.Bonus.text = text;
            }
        }

        // ---- Markers -------------------------------------------------------------------------

        private void RefreshMarkers()
        {
            if (mode == null) return;

            var plots = mode.Plots;
            markerScratch.Clear();
            foreach (var pair in markers)
            {
                markerScratch.Add(pair.Key);
            }

            foreach (var slot in plots)
            {
                if (!markers.TryGetValue(slot, out var marker))
                {
                    marker = MakeMarker(slot);
                    markers[slot] = marker;
                    markerLayer.Add(marker.Root);
                }

                markerScratch.Remove(slot);
                UpdateMarker(marker);
            }

            // Plots that went away, as when the slot count drops.
            foreach (var stale in markerScratch)
            {
                markers[stale].Root.RemoveFromHierarchy();
                markers.Remove(stale);
            }
        }

        private Marker MakeMarker(TownHouseSlot slot)
        {
            var marker = new Marker { Slot = slot, Root = new VisualElement() };
            marker.Root.AddToClassList("rf-plot");
            marker.Root.pickingMode = PickingMode.Ignore;

            var badge = new VisualElement();
            badge.AddToClassList("rf-plot__badge");
            badge.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                mode?.Select(slot);
                evt.StopPropagation();
            });

            marker.Icon = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.Icon.AddToClassList("rf-plot__icon");

            marker.Plus = new Label("") { pickingMode = PickingMode.Ignore };
            marker.Plus.AddToClassList("rf-icon");
            marker.Plus.AddToClassList("rf-plot__plus");

            badge.Add(marker.Icon);
            badge.Add(marker.Plus);

            marker.Name = new Label { pickingMode = PickingMode.Ignore };
            marker.Name.AddToClassList("rf-plot__name");

            marker.Root.Add(badge);
            marker.Root.Add(marker.Name);
            return marker;
        }

        private void UpdateMarker(Marker marker)
        {
            var slot = marker.Slot;
            var empty = VillageBuildMode.IsEmpty(slot);
            var selected = mode.Selected == slot;
            var target = mode.Armed.HasValue && empty;
            var muted = mode.Armed.HasValue && !empty;
            var name = empty ? string.Empty : ResidentName(slot);

            var state = (int)slot.SlotType + "|" + selected + "|" + target + "|" + muted + "|" + name;
            if (state == marker.ShownState) return;
            marker.ShownState = state;

            marker.Root.EnableInClassList("rf-plot--empty", empty);
            marker.Root.EnableInClassList("rf-plot--selected", selected);
            marker.Root.EnableInClassList("rf-plot--target", target);
            marker.Root.EnableInClassList("rf-plot--muted", muted);

            marker.Plus.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            marker.Icon.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            if (!empty)
            {
                var sprite = IconFor(slot.SlotType);
                marker.Icon.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            }

            marker.Name.text = name;
            marker.Name.style.display = string.IsNullOrEmpty(name) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ---- Side panel ----------------------------------------------------------------------

        private void RefreshSide()
        {
            if (mode == null) return;

            var slot = mode.Selected;
            summary.style.display = slot ? DisplayStyle.None : DisplayStyle.Flex;
            plot.style.display = slot ? DisplayStyle.Flex : DisplayStyle.None;

            if (!slot)
            {
                RefreshSummary();
                return;
            }

            var empty = VillageBuildMode.IsEmpty(slot);
            var house = HouseFor(slot.SlotType);

            plotTitle.text = empty ? Localization.UI_BUILD_EMPTY_PLOT : (house ? house.Name : slot.SlotType.ToString());
            plotSubtitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_PLOT_NUMBER, slot.Slot + 1);
            var sprite = empty ? null : IconFor(slot.SlotType);
            plotIcon.style.backgroundImage = sprite ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
            plotIcon.style.display = sprite ? DisplayStyle.Flex : DisplayStyle.None;

            plotEmptyNote.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            plotBuilt.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            if (empty) return;

            var hasResident = slot.OwnerUserId.HasValue;
            var player = slot.Player;
            resident.text = hasResident ? ResidentName(slot) : Localization.UI_BUILD_NOBODY;
            if (player) resident.style.color = HudFormat.ReadableNameColor(player.PlayerNameHexColor);
            else resident.style.color = new StyleColor(StyleKeyword.Null);

            var stats = player ? player.Stats : slot.PlayerSkills;
            var skill = stats != null ? GameMath.GetSkillByHouseType(stats, slot.SlotType) : null;
            residentLevel.text = skill != null ? SkillName(slot.SlotType) + " " + ((int)skill.Level).ToString(CultureInfo.InvariantCulture) : string.Empty;

            var value = skill != null && hasResident ? GameMath.CalculateHouseExpBonus(skill) : 0f;
            bonus.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_BONUS, Mathf.RoundToInt(value), SkillName(slot.SlotType));

            bonusNote.text = !hasResident ? Localization.UI_BUILD_NO_RESIDENT_NOTE
                : !player ? Localization.UI_BUILD_RESIDENT_AWAY_NOTE
                : string.Empty;
            bonusNote.style.display = string.IsNullOrEmpty(bonusNote.text) ? DisplayStyle.None : DisplayStyle.Flex;

            assignButton.text = hasResident ? Localization.UI_BUILD_CHANGE_RESIDENT : Localization.UI_BUILD_ASSIGN;
            assignButton.SetEnabled(!mode.Busy);
            RefreshDemolishButton();
        }

        private void RefreshSummary()
        {
            var plots = mode.Plots;
            var built = 0;
            foreach (var p in plots) if (!VillageBuildMode.IsEmpty(p)) built++;
            summarySubtitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_SUMMARY_PLOTS, built, plots.Count);

            summaryList.Clear();
            var bonuses = mode.BonusByType();
            var top = 0f;
            foreach (var b in bonuses.Values) top = Mathf.Max(top, b);

            var ordered = new List<KeyValuePair<TownHouseSlotType, float>>(bonuses);
            ordered.Sort((a, b) => b.Value.CompareTo(a.Value));

            foreach (var pair in ordered)
            {
                var row = new VisualElement();
                row.AddToClassList("rf-build-summary__row");

                var icon = new VisualElement();
                icon.AddToClassList("rf-build-summary__icon");
                var sprite = IconFor(pair.Key);
                if (sprite) icon.style.backgroundImage = new StyleBackground(sprite);

                var name = new Label(SkillName(pair.Key));
                name.AddToClassList("rf-build-summary__name");

                var bar = new VisualElement();
                bar.AddToClassList("rf-build-summary__bar");
                var fill = new VisualElement();
                fill.AddToClassList("rf-build-summary__fill");
                fill.style.width = new Length(top > 0f ? pair.Value / top * 100f : 0f, LengthUnit.Percent);
                bar.Add(fill);

                var value = new Label("+" + Mathf.RoundToInt(pair.Value).ToString(CultureInfo.InvariantCulture) + "%");
                value.AddToClassList("rf-build-summary__value");

                row.Add(icon);
                row.Add(name);
                row.Add(bar);
                row.Add(value);
                summaryList.Add(row);
            }

            summaryEmpty.style.display = ordered.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDemolishClicked()
        {
            if (mode == null || !mode.Selected) return;

            // Two clicks, so a demolition is never one slip of the mouse.
            if (demolishConfirmUntil <= 0f)
            {
                demolishConfirmUntil = Time.unscaledTime + 3f;
                RefreshDemolishButton();
                return;
            }

            demolishConfirmUntil = 0f;
            _ = mode.DemolishAsync(mode.Selected);
        }

        private void RefreshDemolishButton()
        {
            var confirming = demolishConfirmUntil > 0f;
            demolishButton.text = confirming ? Localization.UI_BUILD_DEMOLISH_CONFIRM : Localization.UI_BUILD_DEMOLISH;
            demolishButton.EnableInClassList("rf-build-button--confirm", confirming);
            demolishButton.SetEnabled(mode != null && !mode.Busy);
        }

        // ---- Resident picker -----------------------------------------------------------------

        private void OpenAssign()
        {
            if (mode == null || !mode.Selected || VillageBuildMode.IsEmpty(mode.Selected)) return;

            var slot = mode.Selected;
            var house = HouseFor(slot.SlotType);
            assignTitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_ASSIGN_TITLE, house ? house.Name : slot.SlotType.ToString());
            assignCurrent.text = slot.OwnerUserId.HasValue
                ? string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_ASSIGN_CURRENT, ResidentName(slot))
                : Localization.UI_BUILD_NOBODY;

            candidates.Clear();
            foreach (var player in mode.RankResidents(slot.SlotType))
            {
                if (player.UserId != slot.OwnerUserId) candidates.Add(player);
            }

            assignList.RefreshItems();
            assignList.ScrollToItem(0);
            assignEmpty.style.display = candidates.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            modal.style.display = DisplayStyle.Flex;
        }

        private void CloseAssign()
        {
            if (modal != null) modal.style.display = DisplayStyle.None;
        }

        private VisualElement MakeCandidateRow()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-build-assign__row");

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-build-assign__text");
            var name = new Label { name = "name", pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-build-assign__name");
            var level = new Label { name = "level", pickingMode = PickingMode.Ignore };
            level.AddToClassList("rf-build-assign__level");
            text.Add(name);
            text.Add(level);

            var value = new Label { name = "bonus", pickingMode = PickingMode.Ignore };
            value.AddToClassList("rf-build-assign__bonus");

            row.Add(text);
            row.Add(value);
            row.RegisterCallback<UnityEngine.UIElements.ClickEvent>(evt =>
            {
                if (row.userData is PlayerController player && mode != null && mode.Selected)
                {
                    CloseAssign();
                    _ = mode.AssignAsync(mode.Selected, player);
                }
            });
            return row;
        }

        private void BindCandidateRow(VisualElement row, int index)
        {
            if (index < 0 || index >= candidates.Count || mode == null || !mode.Selected) return;
            var player = candidates[index];
            var type = mode.Selected.SlotType;
            row.userData = player;

            var name = row.Q<Label>("name");
            name.text = player.PlayerName;
            name.style.color = HudFormat.ReadableNameColor(player.PlayerNameHexColor);

            var skill = player.Stats != null ? GameMath.GetSkillByHouseType(player.Stats, type) : null;
            var home = mode.HomeOf(player);
            var level = skill != null ? SkillName(type) + " " + ((int)skill.Level).ToString(CultureInfo.InvariantCulture) : string.Empty;
            if (home) level += "  ·  " + string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_LIVES_IN, home.Slot + 1);
            row.Q<Label>("level").text = level;

            row.Q<Label>("bonus").text = "+" + Mathf.RoundToInt(VillageBuildMode.BonusFor(player, type)).ToString(CultureInfo.InvariantCulture) + "%";
            row.EnableInClassList("rf-build-assign__row--best", index == 0);
        }

        // ---- Hint and toast ------------------------------------------------------------------

        private void RefreshHint()
        {
            if (mode == null) return;

            string text;
            if (mode.Busy) text = Localization.UI_BUILD_HINT_BUSY;
            else if (mode.Armed.HasValue)
            {
                var house = HouseFor(mode.Armed.Value);
                text = string.Format(CultureInfo.InvariantCulture, Localization.UI_BUILD_HINT_ARMED, house ? house.Name : mode.Armed.Value.ToString());
            }
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

        private static int CategoryOf(TownHouseSlotType type)
        {
            for (var i = 0; i < Categories.Length; ++i)
            {
                if (Array.IndexOf(Categories[i].Types, type) >= 0) return i;
            }

            return -1;
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
            return type.ToString();
        }

        private static string ResidentName(TownHouseSlot slot)
        {
            if (slot.Player) return slot.Player.PlayerName;
            if (!string.IsNullOrEmpty(slot.PlayerName)) return slot.PlayerName;
            return slot.OwnerUserId.HasValue ? Localization.UI_BUILD_UNKNOWN_RESIDENT : string.Empty;
        }
    }
}
