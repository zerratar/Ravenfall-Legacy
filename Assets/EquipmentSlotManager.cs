using UnityEngine;
using RavenNest.Models;

public class EquipmentSlotManager : MonoBehaviour
{
    [SerializeField] private EquipmentSlot amuletSlot;
    [SerializeField] private EquipmentSlot ringSlot;
    [SerializeField] private EquipmentSlot petSlot;
    [SerializeField] private EquipmentSlot helmetSlot;
    [SerializeField] private EquipmentSlot chestSlot;
    [SerializeField] private EquipmentSlot glovesSlot;
    [SerializeField] private EquipmentSlot leggingsSlot;
    [SerializeField] private EquipmentSlot bootsSlot;
    [SerializeField] private EquipmentSlot swordSlot;
    [SerializeField] private EquipmentSlot staffSlot;
    [SerializeField] private EquipmentSlot shieldSlot;
    [SerializeField] private EquipmentSlot bowSlot;

    internal void Observe(PlayerController player)
    {
        if (!player)
            return;

        var amulet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Amulet);
        amuletSlot.SetItem(amulet);

        var ring = player.Inventory.GetEquipmentOfCategory(ItemCategory.Ring);
        ringSlot.SetItem(ring);

        var pet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        petSlot.SetItem(pet);

        var helmet = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Helm);
        helmetSlot.SetItem(helmet);

        var chest = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Chest);
        chestSlot.SetItem(chest);

        var gloves = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Gloves);
        glovesSlot.SetItem(gloves);

        var leggings = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Leggings);
        leggingsSlot.SetItem(leggings);

        var boots = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Boots);
        bootsSlot.SetItem(boots);

        var sword0 = player.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword);
        var sword1 = player.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.OneHandedSword);

        if (sword0 != null)
            swordSlot.SetItem(sword0);
        else
            swordSlot.SetItem(sword1);

        var shield = player.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
        shieldSlot.SetItem(shield);

        var staff = player.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedStaff);
        staffSlot.SetItem(staff);

        var bow = player.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedBow);
        bowSlot.SetItem(bow);
    }
}
