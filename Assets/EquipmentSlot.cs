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

    internal void SetItem(Item item)
    {
        if (item == null)
        {
            ClearItem();
            return;
        }

        tooltip.Enable();
        iconImage.gameObject.SetActive(false);

        if (!loadedItemImages.TryGetValue(item.Id, out var sprite))
        {
            sprite = UnityEngine.Resources.Load<Sprite>("Items/" + item.Id);
            loadedItemImages[item.Id] = sprite;
        }

        if (sprite)
        {
            itemImage.gameObject.SetActive(true);
        }

        itemImage.sprite = sprite;
        tooltip.Title = item.Name;
        tooltip.Body = GenerateItemTooltipContent(item);
    }

    private string GenerateItemTooltipContent(Item item)
    {
        if (item.Type == ItemType.Pet)
        {
            return "Aww! What a cute little pet!";
        }

        StringBuilder sb = new StringBuilder();
        var stats = GetItemStats(item);

        foreach (var s in stats)
            sb.AppendLine(s.Name + "\t" + s.Value);

        return sb.ToString();
    }

    private void ClearItem()
    {
        iconImage.gameObject.SetActive(true);
        itemImage.gameObject.SetActive(false);
        tooltip.Disable();
    }

    private IReadOnlyList<ItemStat> GetItemStats(Item i)
    {
        var stats = new List<ItemStat>();
        if (i.WeaponAim > 0) stats.Add(new ItemStat("Weapon Aim", i.WeaponAim));
        if (i.WeaponPower > 0) stats.Add(new ItemStat("Weapon Power", i.WeaponPower));
        if (i.RangedAim > 0) stats.Add(new ItemStat("Ranged Aim", i.RangedAim));
        if (i.RangedPower > 0) stats.Add(new ItemStat("Ranged Power", i.RangedPower));
        if (i.MagicAim > 0) stats.Add(new ItemStat("Magic Aim", i.MagicAim));
        if (i.MagicPower > 0) stats.Add(new ItemStat("Magic Power", i.MagicPower));
        if (i.ArmorPower > 0) stats.Add(new ItemStat("Armor", i.ArmorPower));
        return stats;
    }

    private class ItemStat
    {
        public ItemStat() { }
        public ItemStat(string name, int value)
        {
            this.Name = name;
            this.Value = value;
        }
        public string Name { get; set; }
        public int Value { get; set; }
    }

}
