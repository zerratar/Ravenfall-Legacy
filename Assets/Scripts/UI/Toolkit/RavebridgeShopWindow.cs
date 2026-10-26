using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The shopkeeper's cart (RavebridgeShop), opened from talking to him: Buy lists what the cart sells for Ravebridge
    /// coins (a draught, buffs, hero mode upgrades with their level), Sell the loot in the Ravebridge bag at its worth,
    /// one at a time or all of a kind. In the quest window's look, with the coins at the top. Walking away from the
    /// shopkeeper, or no longer playing, closes it. RavebridgeDialogueView owns it, on the same layer.
    /// </summary>
    public sealed class RavebridgeShopWindow
    {
        private readonly VisualElement window;
        private readonly Label coins;
        private readonly Button buyTab;
        private readonly Button sellTab;
        private readonly VisualElement list;
        private readonly Label notice;
        private PlayerController shopper;
        private bool selling;
        private float noticeUntil;
        private bool dirty;

        public bool IsOpen => window.style.display == DisplayStyle.Flex;

        public RavebridgeShopWindow(VisualElement layer)
        {
            window = new VisualElement { name = "ravebridge-shop" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-shop");
            var header = new VisualElement();
            header.AddToClassList("rf-squest__header");
            var title = new Label(Localization.UI_RBSHOP_TITLE);
            title.AddToClassList("rf-squest__title");
            var close = new Button(Close) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            header.Add(title);
            header.Add(close);

            var bar = new VisualElement();
            bar.AddToClassList("rf-shop__bar");
            var tabs = new VisualElement();
            tabs.AddToClassList("rf-squest-chips");
            buyTab = new Button(() => { selling = false; Redraw(); }) { text = Localization.UI_RBSHOP_BUY };
            sellTab = new Button(() => { selling = true; Redraw(); }) { text = Localization.UI_RBSHOP_SELL };
            buyTab.AddToClassList("rf-squest-chip");
            sellTab.AddToClassList("rf-squest-chip");
            tabs.Add(buyTab);
            tabs.Add(sellTab);
            coins = new Label();
            coins.AddToClassList("rf-shop__coins");
            bar.Add(tabs);
            bar.Add(coins);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rf-shop__scroll");
            list = new VisualElement();
            scroll.Add(list);
            notice = new Label();
            notice.AddToClassList("rf-squest__hint");

            window.Add(header);
            window.Add(bar);
            window.Add(scroll);
            window.Add(notice);
            window.style.display = DisplayStyle.None;
            layer.Add(window);

            RavebridgeShop.Opened += Open;
            RavebridgeShop.Changed += p => { if (p == shopper) dirty = true; };
            RavebridgeLoot.Changed += p => { if (p == shopper) dirty = true; };
        }

        public void Remove()
        {
            RavebridgeShop.Opened -= Open;
            window.RemoveFromHierarchy();
        }

        private void Open(PlayerController player)
        {
            shopper = player;
            selling = false;
            window.style.display = DisplayStyle.Flex;
            Say(null);
            Redraw();
        }

        public void Close()
        {
            window.style.display = DisplayStyle.None;
            shopper = null;
        }

        public void Update()
        {
            if (!IsOpen) return;
            var keeper = IslandNpc.Find("shopkeeper");
            if (!shopper || RavebridgePlay.Playing != shopper || (keeper && Vector3.Distance(shopper.Position, keeper.transform.position) > keeper.TalkDistance + 4f))
            {
                Close();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping) { EscapeKey.Claim(); Close(); return; }
            if (noticeUntil > 0f && Time.unscaledTime > noticeUntil) Say(null);
            if (dirty) Redraw();
        }

        private void Redraw()
        {
            dirty = false;
            var player = shopper;
            if (!player) return;
            buyTab.EnableInClassList("rf-squest-chip--active", !selling);
            sellTab.EnableInClassList("rf-squest-chip--active", selling);
            // the exact amount: it is what is spent here
            coins.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_LOOT_BAG_COINS, RavebridgeLoot.Coins(player).ToString("N0", CultureInfo.InvariantCulture));
            list.Clear();
            if (selling) DrawSell(player);
            else DrawBuy(player);
        }

        private void DrawBuy(PlayerController player)
        {
            var have = RavebridgeLoot.Coins(player);
            foreach (var entry in RavebridgeShop.Entries)
            {
                var e = entry;
                var price = RavebridgeShop.PriceOf(player, e);
                var color = e.Item != null ? RavebridgeLoot.ColorOf(e.Item.Grade) : e.IsUpgrade ? RavebridgeLoot.ColorOf(RavebridgeLoot.Rarity.Epic) : RavebridgeLoot.ColorOf(RavebridgeLoot.Rarity.Rare);
                var detail = e.Description;
                if (e.IsUpgrade) detail += "\n" + string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_LEVEL, RavebridgeShop.Level(player, e.key), e.MaxLevel);
                if (e.IsBuff)
                {
                    var left = RavebridgeShop.BuffLeft(player, e.stat);
                    if (left > 0f) detail += "\n" + string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_BUFF_LEFT, Mathf.CeilToInt(left / 60f));
                }
                var label = price < 0 ? Localization.UI_RBSHOP_MAXED : string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_PRICE, price);
                var row = Row(e.Glyph, color, e.Name, detail);
                var button = new Button(() => Buy(player, e)) { text = label };
                button.AddToClassList("rf-squest-chip");
                button.AddToClassList("rf-shop__price");
                button.EnableInClassList("rf-squest-chip--active", price >= 0 && have >= price);
                button.SetEnabled(price >= 0);
                row.Add(button);
                list.Add(row);
            }
        }

        private void DrawSell(PlayerController player)
        {
            var contents = RavebridgeLoot.Contents(player);
            var any = false;
            foreach (var (item, amount) in contents)
            {
                if (item.value <= 0) continue;
                any = true;
                var it = item;
                var row = Row(item.Glyph, RavebridgeLoot.ColorOf(item.Grade), amount > 1 ? item.Name + " x" + amount : item.Name,
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_WORTH, item.value));
                var one = new Button(() => Sell(player, it, 1)) { text = Localization.UI_RBSHOP_SELL_ONE };
                one.AddToClassList("rf-squest-chip");
                one.AddToClassList("rf-shop__price");
                row.Add(one);
                if (amount > 1)
                {
                    var all = new Button(() => Sell(player, it, -1)) { text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_SELL_ALL, item.value * amount) };
                    all.AddToClassList("rf-squest-chip");
                    all.AddToClassList("rf-squest-chip--active");
                    all.AddToClassList("rf-shop__price");
                    row.Add(all);
                }
                list.Add(row);
            }
            if (!any)
            {
                var empty = new Label(Localization.UI_RBSHOP_NOTHING_TO_SELL);
                empty.AddToClassList("rf-hero-inv__empty");
                list.Add(empty);
            }
        }

        private static VisualElement Row(string glyph, Color color, string name, string detail)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-shop__row");
            var tile = new VisualElement();
            tile.AddToClassList("rf-hero-inv__tile");
            tile.AddToClassList("rf-hero-inv__tile--island");
            tile.style.borderTopColor = tile.style.borderBottomColor = tile.style.borderLeftColor = tile.style.borderRightColor = new Color(color.r, color.g, color.b, 0.75f);
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-hero-inv__glyph");
            icon.style.color = color;
            tile.Add(icon);
            var text = new VisualElement();
            text.AddToClassList("rf-shop__text");
            var title = new Label(name);
            title.AddToClassList("rf-shop__name");
            var info = new Label(detail);
            info.AddToClassList("rf-shop__detail");
            text.Add(title);
            text.Add(info);
            row.Add(tile);
            row.Add(text);
            return row;
        }

        private void Buy(PlayerController player, RavebridgeShop.Entry entry)
        {
            switch (RavebridgeShop.Buy(player, entry))
            {
                case RavebridgeShop.BuyResult.Bought: Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_BOUGHT, entry.Name)); break;
                case RavebridgeShop.BuyResult.TooPoor: Say(Localization.UI_RBSHOP_TOO_POOR); break;
                case RavebridgeShop.BuyResult.Maxed: Say(Localization.UI_RBSHOP_MAXED); break;
            }
            Redraw();
        }

        private void Sell(PlayerController player, RavebridgeLoot.Item item, long amount)
        {
            var got = RavebridgeShop.Sell(player, item, amount);
            if (got > 0) Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_RBSHOP_SOLD, item.Name, got));
            Redraw();
        }

        private void Say(string text)
        {
            notice.text = string.IsNullOrEmpty(text) ? Localization.UI_RBSHOP_HINT : text;
            noticeUntil = string.IsNullOrEmpty(text) ? 0f : Time.unscaledTime + 4f;
        }
    }
}
