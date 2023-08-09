using RavenNest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class EquipmentSlot : MonoBehaviour
{
    private static readonly ConcurrentDictionary<Guid, Sprite> loadedItemImages
        = new ConcurrentDictionary<Guid, Sprite>();

    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private UnityEngine.UI.Image itemImage;

    [SerializeField] private Tooltip tooltip;

    internal void Start()
    {
        ClearItem();
    }

    internal void SetItem(GameInventoryItem item)
    {
        if (item == null)
        {
            ClearItem();
            return;
        }

        tooltip.Enable();
        iconImage.gameObject.SetActive(false);

        if (!loadedItemImages.TryGetValue(item.Item.Id, out var sprite))
        {
            if (!ExternalResources.TryGetSprite(item.Item.Id.ToString(), out sprite))
            {
                sprite = UnityEngine.Resources.Load<Sprite>("Items/" + item.Item.Id);
            }

            loadedItemImages[item.Item.Id] = sprite;
        }

        if (sprite)
        {
            itemImage.gameObject.SetActive(true);
        }

        itemImage.sprite = sprite;
        tooltip.Title = item.InventoryItem.Name ?? item.Item.Name;
        tooltip.Body = GenerateItemTooltipContent(item);
    }

    private string GenerateItemTooltipContent(GameInventoryItem item)
    {
        if (item.Item.Type == ItemType.Pet)
        {
            return "Aww! What a cute little pet!";
        }

        StringBuilder sb = new StringBuilder();
        var stats = item.GetItemStats();
        sb.Append("<mspace=20>");
        foreach (var s in stats)
        {
            var bonus = "";
            if (s.Enchantment != null)
            {
                if (s.Bonus > 0)
                {
                    bonus = " <color=green>(+" + s.Bonus + ")</color>";
                }
                else if (s.Bonus < 0)
                {
                    bonus = " <color=red>(-" + s.Bonus + ")</color>";
                }
            }

            sb.AppendLine(s.Name.PadRight(14, ' ') + " <b>" + s.Value + bonus + "</b>");
        }

        if (item.Enchantments != null && item.Enchantments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<color=green><b>Enchantments</b>");
            foreach (var e in item.Enchantments)
            {
                sb.AppendLine(e.Name.PadRight(14, ' ') + (" <b>+" + (e.ValueType == AttributeValueType.Percent ? ((int)(e.Value * 100)) + "%" : e.Value + "") + "</b>").PadLeft(11, ' '));

            }
        }
        sb.Append("</mspace>");
        return sb.ToString();
    }

    private void ClearItem()
    {
        iconImage.gameObject.SetActive(true);
        itemImage.gameObject.SetActive(false);
        tooltip.Disable();
    }
}
