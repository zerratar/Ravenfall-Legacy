using System;
using RavenNest.SDK.Endpoints;
using TMPro;
using UnityEngine;

/// <summary>
/// One of the tavern's shelves for the streamer's own items (see <see cref="TavernShop"/>): the item on
/// it, its model when it has one of its own (pets and the like) or its icon on a card, and a label with
/// the shelf number, name, price, how many are left and who may buy. Empty shelves say so; shelves the
/// village has not unlocked yet are hidden.
/// </summary>
public sealed class ShopShelf : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private TextMeshPro label;
    [SerializeField] private Transform anchor;

    // the largest the item may be on the shelf, in metres
    private const float MaxSize = 0.5f;

    private GameObject display;
    private Guid shownItem;
    private Material cardMaterial;

    public int Index => index;
    public StreamerShopListingInfo Listing { get; private set; }

    public void Show(StreamerShopListingInfo listing, bool unlocked, GameManager game)
    {
        if (gameObject.activeSelf != unlocked) gameObject.SetActive(unlocked);
        if (!unlocked) return;

        Listing = listing;
        var item = listing != null && game ? game.Items.Get(listing.ItemId) : null;
        if (label) label.text = Text(listing, item);

        var itemId = listing != null ? listing.ItemId : Guid.Empty;
        if (itemId == shownItem && (display || itemId == Guid.Empty)) return;
        shownItem = itemId;
        if (display) Destroy(display);
        display = null;
        if (item != null) display = MakeDisplay(item, listing);
    }

    private string Text(StreamerShopListingInfo listing, RavenNest.Models.Item item)
    {
        var number = "<size=70%>" + string.Format(Localization.UI_SHOP_SHELF_NUMBER, index + 1) + "</size>\n";
        if (listing == null)
        {
            return number + "<color=#6b5b4b>" + Localization.UI_SHOP_SHELF_EMPTY + "</color>";
        }

        var name = !string.IsNullOrEmpty(listing.Name) ? listing.Name : item?.Name ?? "?";
        var text = number + name + "\n<size=82%><color=#8A4512>" + string.Format(Localization.UI_SHOP_PRICE, listing.PricePerItem.ToString("N0"))
            + "</color>  " + string.Format(Localization.UI_SHOP_LEFT, listing.Amount) + "</size>";
        if (listing.Access != StreamerShopAccess.Everyone) text += "\n<size=70%><color=#5b3f8a>" + Localization.UI_SHOP_CLAN_ONLY + "</color></size>";
        return text;
    }

    private GameObject MakeDisplay(RavenNest.Models.Item item, StreamerShopListingInfo listing)
    {
        var holder = new GameObject("Item");
        holder.transform.SetParent(anchor ? anchor : transform, false);

        var prefab = !string.IsNullOrEmpty(item.GenericPrefab) ? Resources.Load<GameObject>(item.GenericPrefab) : null;
        if (prefab)
        {
            var model = Instantiate(prefab, holder.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            Fit(model);
            return holder;
        }

        // everything else shows its icon on a small card leaning on the shelf
        var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(card.GetComponent<Collider>());
        card.name = "Icon";
        card.transform.SetParent(holder.transform, false);
        card.transform.localScale = Vector3.one * 0.42f;
        card.transform.localPosition = new Vector3(0f, 0.21f, 0f);
        card.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
        if (!cardMaterial) cardMaterial = new Material(Shader.Find("Sprites/Default"));
        var renderer = card.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = cardMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        card.SetActive(false);
        var wanted = listing.ItemId;
        ItemIcons.Get(item.Name, texture =>
        {
            if (!card || shownItem != wanted || !texture) return;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", texture);
            renderer.SetPropertyBlock(block);
            card.SetActive(true);
        });
        return holder;
    }

    /// <summary>Scales a model down to fit the shelf and stands it on the anchor.</summary>
    private static void Fit(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (size > MaxSize) model.transform.localScale *= MaxSize / size;

        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var parent = model.transform.parent;
        model.transform.position += new Vector3(parent.position.x - bounds.center.x, parent.position.y - bounds.min.y, parent.position.z - bounds.center.z);
    }

    private void OnDestroy()
    {
        if (cardMaterial) Destroy(cardMaterial);
    }
}
