using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Hero mode's inventory (I, docs/hero-mode-design.md): what the hero wears, slot by slot, and the backpack as a grid
    /// of icons. A right click equips an item from the backpack (or takes a worn one off); hovering shows the item, as
    /// the details window's gear does. The streamer's characters change through the server, as !equip does; the
    /// Ravebridge character only on this computer. HeroHudView owns it; the window can be dragged by its title.
    /// </summary>
    public sealed class HeroInventoryWindow
    {
        private const int Columns = 7;

        // the worn slots, in the order shown
        private static readonly ItemSlot[] Slots =
        {
            ItemSlot.Head, ItemSlot.Amulet, ItemSlot.Cape, ItemSlot.Chest, ItemSlot.Gloves, ItemSlot.Belt, ItemSlot.Leggings,
            ItemSlot.Boots, ItemSlot.Ring, ItemSlot.MeleeWeapon, ItemSlot.Shield, ItemSlot.RangedWeapon, ItemSlot.MagicWeapon, ItemSlot.Pet,
        };

        private readonly VisualElement window;
        private readonly Label title;
        private readonly VisualElement gear;
        private readonly Label bagTitle;
        private readonly VisualElement bag;
        private readonly Label hint;
        private readonly HudTooltip tooltip;
        private PlayerController shown;
        private ulong shownState;
        private float nextCheck;
        private bool busy;
        private string notice;
        private float noticeUntil;

        public bool IsOpen => window.style.display == DisplayStyle.Flex;

        public HeroInventoryWindow(VisualElement layer)
        {
            window = new VisualElement { name = "hero-inventory" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-hero-inv");
            var header = new VisualElement();
            header.AddToClassList("rf-squest__header");
            title = new Label();
            title.AddToClassList("rf-squest__title");
            var close = new Button(Close) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            header.Add(title);
            header.Add(close);

            var gearTitle = new Label(Localization.UI_HERO_INVENTORY_WORN);
            gearTitle.AddToClassList("rf-hero-inv__section");
            gear = new VisualElement();
            gear.AddToClassList("rf-hero-inv__grid");
            bagTitle = new Label();
            bagTitle.AddToClassList("rf-hero-inv__section");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rf-hero-inv__scroll");
            bag = new VisualElement();
            bag.AddToClassList("rf-hero-inv__grid");
            scroll.Add(bag);
            hint = new Label();
            hint.AddToClassList("rf-squest__hint");

            window.Add(header);
            window.Add(gearTitle);
            window.Add(gear);
            window.Add(bagTitle);
            window.Add(scroll);
            window.Add(hint);
            window.style.display = DisplayStyle.None;
            layer.Add(window);
            DraggableWindow.Attach(window, header, "hero-inventory");
            tooltip = HudTooltip.Create(layer);
        }

        public void Toggle(PlayerController player)
        {
            if (IsOpen) { Close(); return; }
            shown = player;
            shownState = 0;
            window.style.display = DisplayStyle.Flex;
            Redraw();
        }

        public void Close() => window.style.display = DisplayStyle.None;

        public void Remove() => window.RemoveFromHierarchy();

        /// <summary>Redraws when what the hero carries changed (checked a few times a second).</summary>
        public void Update(PlayerController player)
        {
            if (!IsOpen) return;
            if (!player) { Close(); return; }
            if (player != shown) { shown = player; shownState = 0; }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.25f;
            if (!string.IsNullOrEmpty(notice) && Time.unscaledTime > noticeUntil) { notice = null; ShowHint(); }
            if (State(player) != shownState) Redraw();
        }

        private static ulong State(PlayerController player)
        {
            unchecked
            {
                ulong h = 1469598103934665603UL;
                foreach (var i in player.Inventory.GetEquippedItems().ToList()) h = (h ^ (ulong)i.InstanceId.GetHashCode()) * 1099511628211UL;
                h = (h ^ 0xff) * 1099511628211UL;
                foreach (var i in player.Inventory.GetBackpackItems().ToList()) h = (h ^ (ulong)i.InstanceId.GetHashCode() ^ (ulong)i.Amount) * 1099511628211UL;
                return h;
            }
        }

        private void Redraw()
        {
            var player = shown;
            if (!player) return;
            shownState = State(player);
            title.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_INVENTORY, player.Name);

            gear.Clear();
            var worn = player.Inventory.GetEquippedItems().ToList();
            foreach (var slot in Slots)
            {
                var item = worn.FirstOrDefault(i => i.Item != null && Inventory.GetEquipmentSlot(i.Item.Type) == slot);
                if (item == null && (slot == ItemSlot.Belt || slot == ItemSlot.Cape)) continue; // rarely worn: shown only when worn
                gear.Add(Tile(item, slot, true));
            }

            bag.Clear();
            var items = player.Inventory.GetBackpackItems().ToList()
                .Where(i => i != null && i.Item != null)
                .OrderByDescending(i => i.IsEquippableType)
                .ThenBy(i => i.IsEquippableType ? (int)Inventory.GetEquipmentSlot(i.Item.Type) : 0)
                .ThenByDescending(i => i.IsEquippableType ? Inventory.GetItemValue(i) : 0)
                .ThenBy(i => i.Name)
                .ToList();
            foreach (var item in items) bag.Add(Tile(item, ItemSlot.None, false));
            bagTitle.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_INVENTORY_BAG, items.Count);
            if (items.Count == 0)
            {
                var empty = new Label(Localization.UI_HERO_INVENTORY_EMPTY);
                empty.AddToClassList("rf-hero-inv__empty");
                bag.Add(empty);
            }
            ShowHint();
        }

        private void ShowHint() => hint.text = !string.IsNullOrEmpty(notice) ? notice : Localization.UI_HERO_INVENTORY_HINT;

        private VisualElement Tile(GameInventoryItem item, ItemSlot slot, bool worn)
        {
            var tile = new VisualElement();
            tile.AddToClassList("rf-hero-inv__tile");
            var player = shown;
            if (item == null)
            {
                tile.AddToClassList("rf-hero-inv__tile--empty");
                tooltip.Attach(tile, t => { t.SetTitle(SlotName(slot)); t.SetNote(Localization.UI_DETAILS_EMPTY_SLOT); });
                return tile;
            }

            var sprite = EquipmentSlot.GetItemSprite(item);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-hero-inv__icon");
            if (sprite) icon.style.backgroundImage = new StyleBackground(sprite);
            tile.Add(icon);
            if (!sprite)
            {
                var name = new Label(Initials(item.Name)) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-hero-inv__initials");
                tile.Add(name);
            }
            if (item.Amount > 1)
            {
                var amount = new Label(HudFormat.Compact(item.Amount)) { pickingMode = PickingMode.Ignore };
                amount.AddToClassList("rf-hero-inv__amount");
                tile.Add(amount);
            }
            var canEquip = item.IsEquippableType && player.Inventory.CanEquipItem(item);
            tile.EnableInClassList("rf-hero-inv__tile--worn", worn);
            tile.EnableInClassList("rf-hero-inv__tile--blocked", !worn && item.IsEquippableType && !canEquip);
            tile.EnableInClassList("rf-hero-inv__tile--other", !item.IsEquippableType);

            tooltip.Attach(tile, t =>
            {
                PlayerDetailsWindow.FillItem(t, item);
                if (worn) t.SetNote(Localization.UI_HERO_INVENTORY_UNEQUIP);
                else if (item.IsEquippableType) t.SetNote(canEquip ? Localization.UI_HERO_INVENTORY_EQUIP : string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_INVENTORY_NEEDS, Needs(player, item)));
            });
            tile.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button != 1) return;
                e.StopPropagation();
                if (worn) Unequip(player, item);
                else Equip(player, item);
            });
            return tile;
        }

        private async void Equip(PlayerController player, GameInventoryItem item)
        {
            if (busy || !player) return;
            if (!item.IsEquippableType) return;
            if (!player.Inventory.CanEquipItem(item))
            {
                Notice(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_INVENTORY_NEEDS, Needs(player, item)));
                return;
            }
            busy = true;
            try
            {
                if (player.IsLocal || player.IsBot)
                {
                    player.Equip(item, false);
                    if (player.IsLocal) RavebridgeCharacter.Save();
                }
                else if (!await player.EquipmentActions.EquipAsync(item)) Notice(Localization.UI_HERO_INVENTORY_FAILED);
                HeroController.Active?.RefreshWeapon();
            }
            finally
            {
                busy = false;
                shownState = 0;
            }
        }

        private async void Unequip(PlayerController player, GameInventoryItem item)
        {
            if (busy || !player) return;
            busy = true;
            try
            {
                if (player.IsLocal || player.IsBot)
                {
                    player.Inventory.Unequip(item, true);
                    if (player.IsLocal) RavebridgeCharacter.Save();
                }
                else if (await player.GameManager.RavenNest.Players.UnequipInventoryItemAsync(player.Id, item.InstanceId)) player.Inventory.Unequip(item, true);
                else Notice(Localization.UI_HERO_INVENTORY_FAILED);
                HeroController.Active?.RefreshWeapon();
            }
            finally
            {
                busy = false;
                shownState = 0;
            }
        }

        private void Notice(string text)
        {
            notice = text;
            noticeUntil = Time.unscaledTime + 4f;
            ShowHint();
        }

        /// <summary>The levels an item needs that the character does not have, as "Attack 50, Defense 30".</summary>
        private static string Needs(PlayerController p, GameInventoryItem item)
        {
            var s = p.Stats;
            var parts = new List<string>();
            if (s.Attack.Level < item.RequiredAttackLevel) parts.Add(Localization.StatName("Attack") + " " + item.RequiredAttackLevel);
            if (s.Defense.Level < item.RequiredDefenseLevel) parts.Add(Localization.StatName("Defense") + " " + item.RequiredDefenseLevel);
            if (s.Ranged.Level < item.RequiredRangedLevel) parts.Add(Localization.StatName("Ranged") + " " + item.RequiredRangedLevel);
            if (s.Magic.Level < item.RequiredMagicLevel && s.Healing.Level < item.RequiredMagicLevel) parts.Add(Localization.StatName("Magic") + " " + item.RequiredMagicLevel);
            if (s.Slayer.Level < item.RequiredSlayerLevel) parts.Add(Localization.StatName("Slayer") + " " + item.RequiredSlayerLevel);
            return string.Join(", ", parts);
        }

        private static string SlotName(ItemSlot slot) => Localization.SlotName(slot);

        private static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var words = name.Split(' ');
            return words.Length > 1 ? (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpperInvariant() : name.Substring(0, Mathf.Min(2, name.Length));
        }
    }
}
