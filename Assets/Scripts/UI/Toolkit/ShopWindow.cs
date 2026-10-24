using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RavenNest.SDK.Endpoints;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The streamer's tavern shelves: putting their own items up for coins, changing or taking down what
    /// is up, and the sales. Opened by clicking a shelf in the tavern (<see cref="TavernShop"/>).
    /// </summary>
    /// <remarks>
    /// Three tabs. "Put on a shelf": where the item comes from (the stash or one of the streamer's
    /// characters in the stream), a search that filters the grid of icons, and the listing on the right
    /// (shelf, price, how many, how long, who may buy, a limit per viewer). Clicking a filled shelf opens
    /// the same panel on that listing to change it or take it down. "Your listings" and "Sales" list what
    /// is up and what has sold. Every coin from a sale goes to the village treasury, never to the
    /// streamer, and the window says so on every tab.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ShopWindow : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Vector2 minimumSize = new Vector2(900f, 560f);

        public static ShopWindow Instance { get; private set; }

        /// <summary>True while the window is up, and for the rest of the frame it closed in (for Esc).</summary>
        public static bool IsOpen => Instance && (Instance.open || Instance.closedFrame == Time.frameCount);

        private static readonly int[] DurationHours = { 1, 6, 24, 72, 168, 336, 720 };

        private enum Page { Sell, Listings, Sales }

        private VisualElement modal;
        private Label titleLabel;
        private Label treasuryLabel;
        private Label statusLabel;
        private Button closeButton;
        private readonly Button[] tabButtons = new Button[3];
        private readonly VisualElement[] pages = new VisualElement[3];
        private VisualElement sourceRow;
        private TextField searchField;
        private ScrollView grid;
        private ScrollView form;
        private ScrollView listingsList;
        private Label salesTotals;
        private ScrollView salesList;
        private DraggableWindow drag;

        private bool bound;
        private bool open;
        private int closedFrame = -1;
        private Page page;

        // what the window works on
        private StreamerShopSources sources;
        private int sourceIndex;
        private StreamerShopSellableItem selected;
        private StreamerShopListingInfo editing;
        private int shelf = -1;
        private bool busy;

        // the listing being made or changed
        private long price;
        private long amount;
        private int hours = 24;
        private StreamerShopAccess access;
        private readonly HashSet<Guid> roles = new HashSet<Guid>();
        private int maxPerViewer;

        private void Awake()
        {
            Instance = this;
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            TavernShop.Changed += OnShopChanged;
            TryBind();
        }

        private void OnDisable()
        {
            TavernShop.Changed -= OnShopChanged;
            bound = false;
            drag?.Detach();
            drag = null;
        }

        private void Update()
        {
            if (bound && modal.panel == null)
            {
                bound = false;
                drag?.Detach();
                drag = null;
            }

            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (open && Input.GetKeyDown(KeyCode.Escape))
            {
                EscapeKey.Claim();
                Close();
            }

            // the tavern closed under the window: there are no shelves to manage any more
            if (open && gameManager && gameManager.Tavern && !gameManager.Tavern.IsActivated) Close();
        }

        // ---- Opening and closing ---------------------------------------------------------------------

        /// <summary>Opens on a shelf: the listing on it to change, or an empty shelf to fill.</summary>
        public void OpenShelf(int index)
        {
            shelf = index;
            editing = TavernShop.Latest?.Listings?.FirstOrDefault(x => x.Shelf == index);
            selected = null;
            if (editing != null) LoadEdit(editing);
            page = Page.Sell;
            Open();
        }

        public void Open()
        {
            open = true;
            if (bound) ShowNow();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            closedFrame = Time.frameCount;
            if (modal != null) modal.style.display = DisplayStyle.None;
        }

        private void TryBind()
        {
            if (bound) return;
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) return;

            modal = root.Q<VisualElement>("shop-modal");
            if (modal == null) return;

            titleLabel = root.Q<Label>("shop-title");
            treasuryLabel = root.Q<Label>("shop-treasury");
            statusLabel = root.Q<Label>("shop-status");
            closeButton = root.Q<Button>("shop-close");
            tabButtons[0] = root.Q<Button>("shop-tab-sell");
            tabButtons[1] = root.Q<Button>("shop-tab-listings");
            tabButtons[2] = root.Q<Button>("shop-tab-sales");
            pages[0] = root.Q<VisualElement>("shop-page-sell");
            pages[1] = root.Q<VisualElement>("shop-page-listings");
            pages[2] = root.Q<VisualElement>("shop-page-sales");
            sourceRow = root.Q<VisualElement>("shop-sources");
            searchField = root.Q<TextField>("shop-search");
            grid = root.Q<ScrollView>("shop-grid");
            form = root.Q<ScrollView>("shop-form");
            listingsList = root.Q<ScrollView>("shop-listings");
            salesTotals = root.Q<Label>("shop-sales-totals");
            salesList = root.Q<ScrollView>("shop-sales");

            closeButton.clicked += Close;
            tabButtons[0].clicked += () => SelectPage(Page.Sell);
            tabButtons[1].clicked += () => SelectPage(Page.Listings);
            tabButtons[2].clicked += () => SelectPage(Page.Sales);
            searchField.RegisterValueChangedCallback(_ => BuildGrid());
            grid.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            form.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var window = root.Q<VisualElement>("shop-window");
            drag = DraggableWindow.Attach(window, root.Q<VisualElement>(className: "rf-shop__header"), "shop");
            drag?.EnableResize(root.Q<VisualElement>("shop-grip"), minimumSize);

            ApplyTexts();
            bound = true;
            if (open) ShowNow();
            else modal.style.display = DisplayStyle.None;
        }

        private void ApplyTexts()
        {
            titleLabel.text = Localization.UI_SHOP_TITLE;
            tabButtons[0].text = Localization.UI_SHOP_TAB_SELL;
            tabButtons[1].text = Localization.UI_SHOP_TAB_LISTINGS;
            tabButtons[2].text = Localization.UI_SHOP_TAB_SALES;
            SetPlaceholder(searchField, Localization.UI_SHOP_SEARCH);
        }

        private void ShowNow()
        {
            modal.style.display = DisplayStyle.Flex;
            Status(null);
            ShowTreasury();
            SelectPage(page);
            LoadSources();
        }

        private void SelectPage(Page next)
        {
            page = next;
            for (var i = 0; i < pages.Length; ++i)
            {
                pages[i].style.display = i == (int)next ? DisplayStyle.Flex : DisplayStyle.None;
                tabButtons[i].EnableInClassList("rf-shop__tab--active", i == (int)next);
            }

            if (next == Page.Sell) { BuildSources(); BuildGrid(); BuildForm(); }
            else if (next == Page.Listings) BuildListings();
            else LoadSales();
        }

        private void OnShopChanged()
        {
            if (!open || !bound) return;
            ShowTreasury();
            // a listing being changed may have sold out or been taken down meanwhile
            if (editing != null)
            {
                var now = TavernShop.Latest?.Listings?.FirstOrDefault(x => x.Id == editing.Id);
                if (now == null) { editing = null; BuildForm(); }
                else editing = now;
            }

            if (page == Page.Listings) BuildListings();
            else if (page == Page.Sell) BuildForm();
        }

        private void ShowTreasury()
        {
            var coins = TavernShop.Latest?.VillageCoins ?? 0;
            treasuryLabel.text = string.Format(Localization.UI_SHOP_TREASURY, coins.ToString("N0", CultureInfo.InvariantCulture));
        }

        // ---- Sources and the grid -------------------------------------------------------------------

        private async void LoadSources()
        {
            if (gameManager == null || gameManager.RavenNest == null) return;
            try
            {
                var loaded = await gameManager.RavenNest.Shop.GetSourcesAsync();
                if (loaded == null) return;
                sources = loaded;
                sourceIndex = Mathf.Clamp(sourceIndex, 0, Math.Max(0, sources.Sources.Count - 1));
                if (page == Page.Sell) { BuildSources(); BuildGrid(); BuildForm(); }
            }
            catch (Exception exc)
            {
                Status(string.Format(Localization.UI_SHOP_ERROR, exc.Message), true);
            }
        }

        private void BuildSources()
        {
            sourceRow.Clear();
            if (sources == null) return;
            for (var i = 0; i < sources.Sources.Count; ++i)
            {
                var index = i;
                var source = sources.Sources[i];
                var name = source.Kind == StreamerShopSourceKind.Stash ? Localization.UI_SHOP_SOURCE_STASH : source.Name;
                var chip = Chip(name, i == sourceIndex, () => { sourceIndex = index; selected = null; BuildSources(); BuildGrid(); BuildForm(); });
                sourceRow.Add(chip);
            }
        }

        private void BuildGrid()
        {
            grid.Clear();
            var source = sources != null && sourceIndex < sources.Sources.Count ? sources.Sources[sourceIndex] : null;
            if (source == null)
            {
                grid.Add(Note(Localization.UI_SHOP_LOADING));
                return;
            }

            var query = searchField.value?.Trim() ?? "";
            var items = source.Items
                .Where(x => query.Length == 0 || DisplayName(x).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Sellable)
                .ThenBy(x => DisplayName(x))
                .ToList();

            if (items.Count == 0)
            {
                grid.Add(Note(Localization.UI_SHOP_NO_ITEMS));
                return;
            }

            var wrap = new VisualElement();
            wrap.AddToClassList("rf-shop__tiles");
            foreach (var item in items) wrap.Add(Tile(item));
            grid.Add(wrap);
        }

        private VisualElement Tile(StreamerShopSellableItem item)
        {
            var tile = new Button(() =>
            {
                if (!item.Sellable) { Status(item.Reason, true); return; }
                selected = item;
                editing = null;
                price = Math.Max(1, item.VendorPrice);
                amount = 1;
                hours = 24;
                access = StreamerShopAccess.Everyone;
                roles.Clear();
                maxPerViewer = 0;
                if (shelf < 0 || TavernShop.Latest?.Listings?.Any(x => x.Shelf == shelf) == true) shelf = FirstFreeShelf();
                BuildGrid();
                BuildForm();
            });
            tile.AddToClassList("rf-shop__tile");
            tile.EnableInClassList("rf-shop__tile--disabled", !item.Sellable);
            tile.EnableInClassList("rf-shop__tile--selected", selected != null && Same(selected, item) && editing == null);
            tile.tooltip = DisplayName(item) + (item.Sellable ? "" : "\n" + item.Reason);

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-shop__tile-icon");
            tile.Add(icon);
            LoadIcon(icon, item.ItemName);

            var count = new Label(Short(item.Amount)) { pickingMode = PickingMode.Ignore };
            count.AddToClassList("rf-shop__tile-count");
            tile.Add(count);

            var name = new Label(DisplayName(item)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-shop__tile-name");
            tile.Add(name);
            return tile;
        }

        // ---- The listing panel ----------------------------------------------------------------------

        private void BuildForm()
        {
            form.Clear();
            if (editing != null) { BuildEditForm(); return; }
            if (selected == null)
            {
                form.Add(Note(Localization.UI_SHOP_PICK_ITEM));
                return;
            }

            form.Add(ItemHeader(selected.ItemName, DisplayName(selected), string.Format(Localization.UI_SHOP_YOU_HAVE, selected.Amount.ToString("N0"))));

            // the shelf: the free ones
            var shop = TavernShop.Latest;
            var free = FreeShelves();
            form.Add(Heading(Localization.UI_SHOP_SHELF));
            if (free.Count == 0)
            {
                form.Add(Note(Localization.UI_SHOP_NO_FREE_SHELF));
                return;
            }
            if (!free.Contains(shelf)) shelf = free[0];
            var shelfRow = Row();
            foreach (var s in free)
            {
                var index = s;
                shelfRow.Add(Chip((s + 1).ToString(), s == shelf, () => { shelf = index; BuildForm(); }));
            }
            form.Add(shelfRow);

            AddTermsFields(true);

            var put = new Button(() => Submit());
            put.text = Localization.UI_SHOP_PUT;
            put.AddToClassList("rf-button");
            put.AddToClassList("rf-button--primary");
            put.AddToClassList("rf-shop__submit");
            put.SetEnabled(!busy);
            form.Add(put);
            form.Add(Note(Localization.UI_SHOP_TO_VILLAGE, "rf-shop__village-note"));
        }

        private void BuildEditForm()
        {
            var item = gameManager.Items.Get(editing.ItemId);
            var name = !string.IsNullOrEmpty(editing.Name) ? editing.Name : item?.Name;
            form.Add(ItemHeader(item?.Name, string.Format(Localization.UI_SHOP_EDITING, editing.Shelf + 1, name),
                string.Format(Localization.UI_SHOP_LISTING_STATS, editing.Amount.ToString("N0"), editing.Sold.ToString("N0"), editing.CoinsEarned.ToString("N0")),
                string.Format(Localization.UI_SHOP_TIME_LEFT, TimeLeft(editing.Expires))));

            AddTermsFields(false);

            // side by side and the same width: a chip row wrapped them onto two lines
            var buttons = new VisualElement();
            buttons.AddToClassList("rf-shop__actions");
            var save = new Button(() => SaveEdit()) { text = Localization.UI_SHOP_SAVE };
            save.AddToClassList("rf-button");
            save.AddToClassList("rf-button--primary");
            save.AddToClassList("rf-shop__action");
            save.SetEnabled(!busy);
            var down = new Button(() => TakeDown(editing)) { text = Localization.UI_SHOP_TAKE_DOWN };
            down.AddToClassList("rf-button");
            down.AddToClassList("rf-shop__action");
            down.SetEnabled(!busy);
            buttons.Add(save);
            buttons.Add(down);
            form.Add(buttons);
            // no village note under the buttons here: the banner at the top already says it, and the
            // wrapped note sat cramped under the buttons
        }

        /// <summary>Price, how many (new listings only), how long, who may buy and the limit per viewer.</summary>
        private void AddTermsFields(bool isNew)
        {
            form.Add(Heading(Localization.UI_SHOP_PRICE_LABEL));
            var priceField = NumberField(price, v => price = v);
            form.Add(priceField);
            var vendor = isNew ? selected?.VendorPrice ?? 0 : 0;
            if (vendor > 0) form.Add(Note(string.Format(Localization.UI_SHOP_VENDOR_HINT, vendor.ToString("N0")), "rf-shop__hint"));

            if (isNew)
            {
                form.Add(Heading(Localization.UI_SHOP_AMOUNT_LABEL));
                var amountRow = Row();
                var amountField = NumberField(amount, v => amount = v);
                amountRow.Add(amountField);
                var all = new Button(() => { amount = selected.Amount; amountField.value = amount.ToString(CultureInfo.InvariantCulture); }) { text = Localization.UI_SHOP_ALL };
                all.AddToClassList("rf-button");
                amountRow.Add(all);
                form.Add(amountRow);
            }

            form.Add(Heading(Localization.UI_SHOP_DURATION_LABEL));
            var durationRow = Row();
            if (!isNew) durationRow.Add(Chip(Localization.UI_SHOP_DURATION_KEEP, hours == 0, () => { hours = 0; BuildForm(); }));
            foreach (var h in DurationHours)
            {
                var value = h;
                durationRow.Add(Chip(Hours(h), hours == h, () => { hours = value; BuildForm(); }));
            }
            form.Add(durationRow);

            form.Add(Heading(Localization.UI_SHOP_ACCESS_LABEL));
            var hasClan = sources?.HasClan ?? false;
            var accessRow = Row();
            accessRow.Add(Chip(Localization.UI_SHOP_ACCESS_EVERYONE, access == StreamerShopAccess.Everyone, () => { access = StreamerShopAccess.Everyone; BuildForm(); }));
            var clan = Chip(Localization.UI_SHOP_ACCESS_CLAN, access == StreamerShopAccess.Clan, () => { access = StreamerShopAccess.Clan; BuildForm(); });
            var clanRoles = Chip(Localization.UI_SHOP_ACCESS_ROLES, access == StreamerShopAccess.ClanRoles, () => { access = StreamerShopAccess.ClanRoles; BuildForm(); });
            clan.SetEnabled(hasClan);
            clanRoles.SetEnabled(hasClan);
            accessRow.Add(clan);
            accessRow.Add(clanRoles);
            form.Add(accessRow);
            if (!hasClan) form.Add(Note(Localization.UI_SHOP_NO_CLAN, "rf-shop__hint"));

            if (access == StreamerShopAccess.ClanRoles && sources != null)
            {
                var roleRow = Row();
                foreach (var role in sources.ClanRoles)
                {
                    var id = role.Id;
                    roleRow.Add(Chip(role.Name, roles.Contains(id), () => { if (!roles.Remove(id)) roles.Add(id); BuildForm(); }));
                }
                form.Add(roleRow);
            }

            form.Add(Heading(Localization.UI_SHOP_LIMIT_LABEL));
            form.Add(NumberField(maxPerViewer, v => maxPerViewer = (int)Math.Min(int.MaxValue, v)));
        }

        private void LoadEdit(StreamerShopListingInfo listing)
        {
            price = listing.PricePerItem;
            hours = 0;
            access = listing.Access;
            roles.Clear();
            foreach (var id in listing.AllowedClanRoleIds ?? new List<Guid>()) roles.Add(id);
            maxPerViewer = listing.MaxPerViewer;
        }

        // ---- Actions --------------------------------------------------------------------------------

        private async void Submit()
        {
            if (busy || selected == null) return;
            if (amount < 1 || amount > selected.Amount) { Status(string.Format(Localization.UI_SHOP_YOU_HAVE, selected.Amount.ToString("N0")), true); return; }

            var source = sources.Sources[sourceIndex];
            var request = new StreamerShopListingRequest
            {
                Shelf = shelf,
                SourceKind = source.Kind,
                SourceCharacterId = source.CharacterId,
                ItemId = selected.ItemId,
                Tag = selected.Tag,
                Enchantment = selected.Enchantment,
                TransmogrificationId = selected.TransmogrificationId,
                Name = selected.Name,
                Flags = selected.Flags,
                Amount = amount,
                PricePerItem = price,
                DurationHours = hours,
                Access = access,
                AllowedClanRoleIds = roles.ToList(),
                MaxPerViewer = maxPerViewer,
            };

            // kept now: the shop that comes back moves the shelf choice on to the next free one
            var listedOn = shelf + 1;
            var listedName = DisplayName(selected);
            // closed once it is up: left open, the panel made people wonder whether a second item would
            // go on another shelf or replace the one just put up. The item on the shelf says it worked.
            await Run(async () => await gameManager.RavenNest.Shop.ListAsync(request), result =>
            {
                Status(string.Format(Localization.UI_SHOP_DONE_LISTED, listedName, listedOn));
                selected = null;
                Close();
            });
        }

        private async void SaveEdit()
        {
            if (busy || editing == null) return;
            var request = new StreamerShopListingRequest
            {
                PricePerItem = price,
                DurationHours = hours,
                Access = access,
                AllowedClanRoleIds = roles.ToList(),
                MaxPerViewer = maxPerViewer,
            };
            var id = editing.Id;
            await Run(async () => await gameManager.RavenNest.Shop.EditAsync(id, request), result =>
            {
                editing = result.Listing;
                if (editing != null) LoadEdit(editing);
                Status(Localization.UI_SHOP_DONE_SAVED);
            });
        }

        private async void TakeDown(StreamerShopListingInfo listing)
        {
            if (busy || listing == null) return;
            var id = listing.Id;
            await Run(async () => await gameManager.RavenNest.Shop.RemoveAsync(id), result =>
            {
                if (editing != null && editing.Id == id) editing = null;
                Status(Localization.UI_SHOP_DONE_REMOVED);
                LoadSources();
            });
        }

        /// <summary>Runs a shop call: shows it busy, shows the shop it sends back, or the reason it was refused.</summary>
        private async System.Threading.Tasks.Task Run(Func<System.Threading.Tasks.Task<StreamerShopResult>> call, Action<StreamerShopResult> done)
        {
            busy = true;
            BuildForm();
            try
            {
                var result = await call();
                if (result == null) { Status(string.Format(Localization.UI_SHOP_ERROR, "no answer"), true); return; }
                if (!result.Ok) { Status(result.Error, true); return; }
                if (result.Shop != null) TavernShop.Apply(result.Shop);
                done(result);
            }
            catch (Exception exc)
            {
                Status(string.Format(Localization.UI_SHOP_ERROR, exc.Message), true);
            }
            finally
            {
                busy = false;
                if (page == Page.Sell) { BuildGrid(); BuildForm(); }
                else if (page == Page.Listings) BuildListings();
            }
        }

        // ---- Your listings --------------------------------------------------------------------------

        private void BuildListings()
        {
            listingsList.Clear();
            var listings = TavernShop.Latest?.Listings;
            if (listings == null || listings.Count == 0)
            {
                listingsList.Add(Note(Localization.UI_SHOP_NO_LISTINGS));
                return;
            }

            foreach (var listing in listings.OrderBy(x => x.Shelf))
            {
                var item = gameManager.Items.Get(listing.ItemId);
                var name = !string.IsNullOrEmpty(listing.Name) ? listing.Name : item?.Name;
                var row = new VisualElement();
                row.AddToClassList("rf-shop__row");

                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-shop__row-icon");
                LoadIcon(icon, item?.Name);
                row.Add(icon);

                var text = new VisualElement { pickingMode = PickingMode.Ignore };
                text.AddToClassList("rf-shop__row-text");
                var title = new Label(string.Format(Localization.UI_SHOP_EDITING, listing.Shelf + 1, name));
                title.AddToClassList("rf-shop__row-title");
                var line = new Label(string.Format(Localization.UI_SHOP_PRICE, listing.PricePerItem.ToString("N0")) + "   "
                    + string.Format(Localization.UI_SHOP_LISTING_STATS, listing.Amount.ToString("N0"), listing.Sold.ToString("N0"), listing.CoinsEarned.ToString("N0")) + "   "
                    + string.Format(Localization.UI_SHOP_TIME_LEFT, TimeLeft(listing.Expires))
                    + (listing.Access != StreamerShopAccess.Everyone ? "   " + Localization.UI_SHOP_CLAN_ONLY : ""));
                line.AddToClassList("rf-shop__row-line");
                text.Add(title);
                text.Add(line);
                row.Add(text);

                var edit = new Button(() => { shelf = listing.Shelf; editing = listing; selected = null; LoadEdit(listing); SelectPage(Page.Sell); }) { text = Localization.UI_SHOP_EDIT };
                edit.AddToClassList("rf-button");
                var down = new Button(() => TakeDown(listing)) { text = Localization.UI_SHOP_TAKE_DOWN };
                down.AddToClassList("rf-button");
                down.SetEnabled(!busy);
                row.Add(edit);
                row.Add(down);
                listingsList.Add(row);
            }
        }

        // ---- Sales ----------------------------------------------------------------------------------

        private async void LoadSales()
        {
            salesList.Clear();
            salesTotals.text = Localization.UI_SHOP_LOADING;
            try
            {
                var report = await gameManager.RavenNest.Shop.GetReportAsync();
                if (report == null || page != Page.Sales) return;
                salesTotals.text = string.Format(Localization.UI_SHOP_SALES_TOTALS, report.TotalCoins.ToString("N0"), report.TotalItems.ToString("N0"), report.CoinsLast24Hours.ToString("N0"));
                salesList.Clear();
                if (report.Sales.Count == 0)
                {
                    salesList.Add(Note(Localization.UI_SHOP_NO_SALES));
                    return;
                }

                foreach (var sale in report.Sales)
                {
                    var row = new VisualElement();
                    row.AddToClassList("rf-shop__row");
                    var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("rf-shop__row-icon");
                    LoadIcon(icon, gameManager.Items.Get(sale.ItemId)?.Name ?? sale.ItemName);
                    row.Add(icon);
                    var text = new Label(string.Format(Localization.UI_SHOP_SALE_ROW, sale.BuyerName, sale.ItemName, sale.Price.ToString("N0"), sale.LoyaltyPoints))
                    { pickingMode = PickingMode.Ignore };
                    text.AddToClassList("rf-shop__row-text");
                    row.Add(text);
                    var time = new Label(sale.Time.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)) { pickingMode = PickingMode.Ignore };
                    time.AddToClassList("rf-shop__row-time");
                    row.Add(time);
                    salesList.Add(row);
                }
            }
            catch (Exception exc)
            {
                salesTotals.text = string.Format(Localization.UI_SHOP_ERROR, exc.Message);
            }
        }

        // ---- Helpers --------------------------------------------------------------------------------

        private List<int> FreeShelves()
        {
            var shop = TavernShop.Latest;
            var result = new List<int>();
            if (shop == null) return result;
            for (var i = 0; i < shop.Shelves; ++i)
            {
                if (shop.Listings == null || !shop.Listings.Any(x => x.Shelf == i)) result.Add(i);
            }
            return result;
        }

        private int FirstFreeShelf()
        {
            var free = FreeShelves();
            return free.Count > 0 ? free[0] : -1;
        }

        private string DisplayName(StreamerShopSellableItem item) =>
            !string.IsNullOrEmpty(item.Name) ? item.Name : item.ItemName ?? "?";

        private static bool Same(StreamerShopSellableItem a, StreamerShopSellableItem b) =>
            a.ItemId == b.ItemId && a.Tag == b.Tag && a.Enchantment == b.Enchantment && a.Name == b.Name && a.Flags == b.Flags && a.TransmogrificationId == b.TransmogrificationId;

        private static string Short(long amount) =>
            amount >= 1_000_000 ? (amount / 1_000_000) + "M" : amount >= 10_000 ? (amount / 1_000) + "k" : amount.ToString(CultureInfo.InvariantCulture);

        private static string Hours(int h) =>
            h < 24 ? string.Format(Localization.UI_SHOP_HOURS, h) : string.Format(Localization.UI_SHOP_DAYS, h / 24);

        private static string TimeLeft(DateTime expires)
        {
            var left = expires - DateTime.UtcNow;
            if (left <= TimeSpan.Zero) return "0";
            if (left.TotalDays >= 1) return string.Format(Localization.UI_SHOP_DAYS, (int)left.TotalDays);
            if (left.TotalHours >= 1) return string.Format(Localization.UI_SHOP_HOURS, (int)left.TotalHours);
            return Math.Max(1, (int)left.TotalMinutes) + " min";
        }

        private void Status(string text, bool error = false)
        {
            statusLabel.text = text ?? "";
            statusLabel.EnableInClassList("rf-shop__status--error", error);
        }

        private static void LoadIcon(VisualElement target, string itemName)
        {
            ItemIcons.Get(itemName, texture =>
            {
                // set even before the element is on screen: a cached icon calls back at once
                if (texture) target.style.backgroundImage = new StyleBackground(texture);
            });
        }

        private VisualElement ItemHeader(string itemName, string title, params string[] lines)
        {
            var header = new VisualElement();
            header.AddToClassList("rf-shop__item-header");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-shop__item-icon");
            LoadIcon(icon, itemName);
            header.Add(icon);
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-shop__item-text");
            var titleLabel = new Label(title) { pickingMode = PickingMode.Ignore };
            titleLabel.AddToClassList("rf-shop__item-title");
            text.Add(titleLabel);
            // a label per line: this font sets broken lines almost on top of each other
            foreach (var line in lines)
            {
                var sub = new Label(line) { pickingMode = PickingMode.Ignore };
                sub.AddToClassList("rf-shop__item-sub");
                text.Add(sub);
            }
            header.Add(text);
            return header;
        }

        private static Label Heading(string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-shop__heading");
            return label;
        }

        private static Label Note(string text, string extraClass = null)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-shop__note");
            if (extraClass != null) label.AddToClassList(extraClass);
            return label;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-shop__chips");
            return row;
        }

        private static Button Chip(string text, bool active, Action clicked)
        {
            var chip = new Button(clicked) { text = text };
            chip.AddToClassList("rf-shop__chip");
            chip.EnableInClassList("rf-shop__chip--active", active);
            return chip;
        }

        private static TextField NumberField(long value, Action<long> changed)
        {
            var field = new TextField { value = value.ToString(CultureInfo.InvariantCulture) };
            field.AddToClassList("rf-shop__field");
            field.RegisterValueChangedCallback(e =>
            {
                var digits = new string((e.newValue ?? "").Where(char.IsDigit).ToArray());
                if (digits != e.newValue) field.SetValueWithoutNotify(digits);
                changed(long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0);
            });
            return field;
        }

        private static void SetPlaceholder(TextField field, string text)
        {
            if (field == null) return;
            field.textEdition.placeholder = text;
            field.textEdition.hidePlaceholderOnFocus = true;
        }
    }
}
