using System;
using System.Linq;
using RavenNest.SDK.Endpoints;
using Shinobytes.UI;
using UnityEngine;

/// <summary>
/// The streamer's shelves in the tavern's redeemables room: the streamer puts items of their own up for
/// coins that go to the village treasury, and viewers buy them one at a time with !redeem. Keeps the
/// shop as the server last sent it, shows it on the shelves (<see cref="ShopShelf"/>), and opens the
/// shop window when the streamer clicks a shelf.
/// </summary>
/// <remarks>
/// The tavern is switched off while nobody is in it, so the last shop the server sent is kept in
/// <see cref="Latest"/> and shown when the shelves come on; it is fetched again on entering the tavern
/// and once a minute while there. See docs/streamer-shop-design.md.
/// </remarks>
public sealed class TavernShop : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ShopShelf[] shelves = Array.Empty<ShopShelf>();
    [SerializeField] private TMPro.TextMeshPro sign;

    private const float RefreshSeconds = 60f;

    private static readonly RaycastHit[] hits = new RaycastHit[32];
    private float nextRefresh;
    private bool refreshing;

    public static TavernShop Instance { get; private set; }

    /// <summary>The shop as the server last sent it, kept while the tavern is closed.</summary>
    public static StreamerShopInfo Latest { get; private set; }

    /// <summary>Raised whenever the shop changes, for the window.</summary>
    public static event Action Changed;

    /// <summary>From the server's StreamerShopUpdated event, or after anything the window did.</summary>
    public static void Apply(StreamerShopInfo shop)
    {
        if (shop == null) return;
        Latest = shop;
        if (Instance && Instance.isActiveAndEnabled) Instance.ShowShelves();
        Changed?.Invoke();
    }

    private void Awake()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
    }

    private void OnEnable()
    {
        Instance = this;
        // not bold: the label font is already thickened for the light wall, and bold on top smears it
        if (sign) sign.text = Localization.UI_SHOP_SIGN + "\n<size=55%>" + Localization.UI_SHOP_SIGN_HINT + "</size>";
        if (Latest != null) ShowShelves();
        else foreach (var shelf in shelves) if (shelf) shelf.Show(null, false, gameManager);
        nextRefresh = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        if (Input.GetMouseButtonDown(0) && !HudPointer.IsOverHud() && !UIUtility.IsPointerOverUIElement() && !DraggableWindow.IsPointerOverWindow)
        {
            var shelf = ShelfUnderMouse();
            if (shelf && ShopWindow.Instance) ShopWindow.Instance.OpenShelf(shelf.Index);
        }
    }

    /// <summary>Asks the server for the shop again.</summary>
    public async void Refresh()
    {
        if (refreshing || gameManager == null || gameManager.RavenNest == null || !gameManager.RavenNest.Authenticated) return;
        refreshing = true;
        try
        {
            Apply(await gameManager.RavenNest.Shop.GetAsync());
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The tavern shelves could not be loaded: " + exc.Message);
        }
        finally
        {
            refreshing = false;
        }
    }

    private void ShowShelves()
    {
        var shop = Latest;
        foreach (var shelf in shelves)
        {
            if (!shelf) continue;
            var listing = shop.Listings?.FirstOrDefault(x => x.Shelf == shelf.Index);
            shelf.Show(listing, shelf.Index < shop.Shelves, gameManager);
        }
    }

    private ShopShelf ShelfUnderMouse()
    {
        var cam = Camera.main;
        if (!cam) return null;
        var count = Physics.RaycastNonAlloc(cam.ScreenPointToRay(Input.mousePosition), hits, 60f, ~0, QueryTriggerInteraction.Collide);
        var nearest = -1;
        for (var i = 0; i < count; ++i)
        {
            if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
        }

        return nearest >= 0 ? hits[nearest].collider.GetComponentInParent<ShopShelf>() : null;
    }
}
