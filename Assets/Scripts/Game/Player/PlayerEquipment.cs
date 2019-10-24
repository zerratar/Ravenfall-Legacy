using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RavenNest.Models;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private GameObject staffPrefab;
    [SerializeField] private GameObject bowPrefab;
    [SerializeField] private GameObject woodcuttingHatchetPrefab;
    [SerializeField] private GameObject miningPickaxePrefab;
    [SerializeField] private GameObject fishingRodPrefab;
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject rakePrefab;
    [SerializeField] private GameObject baseItemPrefab;

    private GameManager gameManager;

    private List<ItemController> equippedObjects;

    private PlayerController player;
    private IPlayerAppearance appearance;
    private ItemController weapon;
    private GameObject staff;
    private GameObject bow;

    private GameObject woodcuttingHatchet;
    private GameObject miningPickaxe;
    private GameObject hammer;
    private GameObject fishingRod;
    private GameObject rake;

    public IReadOnlyList<ItemController> EquippedItems => equippedObjects;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        equippedObjects = new List<ItemController>();
        appearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>() ?? GetComponent<PlayerAppearance>();
        player = GetComponent<PlayerController>();
    }

    public void HideHatchet()
    {
        if (woodcuttingHatchet) woodcuttingHatchet.SetActive(false);
    }

    public void HidePickAxe()
    {
        if (miningPickaxe) miningPickaxe.SetActive(false);
    }

    public void HideFishingRod()
    {
        if (fishingRod) fishingRod.SetActive(false);
    }

    public void HideRake()
    {
        if (rake) rake.SetActive(false);
    }

    public void HideWeapon()
    {
        if (weapon) weapon.gameObject.SetActive(false);
        if (bow) bow.gameObject.SetActive(false);
        if (staff) staff.gameObject.SetActive(false);
    }

    public void HideHammer()
    {
        if (hammer) hammer.SetActive(false);
    }

    public void HideEquipments()
    {
        HideRake();
        HideWeapon();
        HideHatchet();
        HidePickAxe();
        HideHammer();
        HideFishingRod();
    }
    private void ShowBow()
    {
        HideEquipments();

        if (!bow && bowPrefab)
            bow = Instantiate(bowPrefab, appearance.OffHandTransform);

        if (bow) bow.gameObject.SetActive(true);
    }

    private void ShowStaff()
    {
        HideEquipments();

        if (!staff && staffPrefab)
            staff = Instantiate(staffPrefab, appearance.MainHandTransform);

        if (staff) staff.gameObject.SetActive(true);
    }

    public void ShowFishingRod()
    {
        HideEquipments();

        if (!fishingRod && fishingRodPrefab)
            fishingRod = Instantiate(fishingRodPrefab, appearance.MainHandTransform);

        if (fishingRod)
            fishingRod.SetActive(true);
    }

    public void ShowPickAxe()
    {
        HideEquipments();

        if (!miningPickaxe && miningPickaxePrefab)
            miningPickaxe = Instantiate(miningPickaxePrefab, appearance.MainHandTransform);

        if (miningPickaxe)
            miningPickaxe.SetActive(true);
    }

    public void ShowHammer()
    {
        HideEquipments();

        if (!hammer && hammerPrefab)
            hammer = Instantiate(hammerPrefab, appearance.MainHandTransform);

        if (hammer)
            hammer.SetActive(true);
    }

    public void ShowHatchet()
    {
        HideEquipments();

        if (!woodcuttingHatchet && woodcuttingHatchetPrefab)
            woodcuttingHatchet = Instantiate(woodcuttingHatchetPrefab, appearance.MainHandTransform);

        if (woodcuttingHatchet)
            woodcuttingHatchet.SetActive(true);
    }

    public void ShowRake()
    {
        HideEquipments();
        if (!rake && rakePrefab) rake = Instantiate(rakePrefab, appearance.MainHandTransform);
        if (rake) rake.SetActive(true);
    }

    public void ShowWeapon(AttackType type)
    {
        HideEquipments();

        switch (type)
        {
            case AttackType.Ranged:
                ShowBow();
                return;

            case AttackType.Magic:
                ShowStaff();
                return;
            default:
                if (weapon) weapon.gameObject.SetActive(true);
                return;
        }
    }

    public void EquipAll(IReadOnlyList<RavenNest.Models.Item> inventoryEquippedItems)
    {
        appearance.UpdateAppearance();

        foreach (var item in inventoryEquippedItems)
        {
            Equip(item);
        }

        appearance.Optimize();
    }

    public void Unequip(Guid id)
    {
        var equipped = equippedObjects.FirstOrDefault(x => x.Id == id);
        var removed = false;
        if (equipped != null)
        {
            appearance.UnEquip(equipped);
            removed = equippedObjects.Remove(equipped);
        }

        if (equipped)
        {
            Destroy(equipped.gameObject);
        }

        if (!removed)
        {
            Debug.LogError($"{player.PlayerName} is trying to unequip item but item did not exist. ID {id}");
        }
    }

    public void Equip(RavenNest.Models.Item item)
    {
        if (!baseItemPrefab)
        {
            Debug.LogError("BaseItemPrefab not set on player!! Unable to create item");
            return;
        }

        var existingItemController = equippedObjects.FirstOrDefault(x => x.Id == item.Id);
        if (existingItemController != null && existingItemController)
        {
            Destroy(existingItemController.gameObject);
            equippedObjects.Remove(existingItemController);
        }

        var itemController = gameManager.Items.Create(item, player.Appearance.Gender == Gender.Male);
        itemController.transform.SetParent(transform);
        itemController.gameObject.layer = player.gameObject.layer;

        equippedObjects.Add(itemController);

        appearance.Equip(itemController);

        if (itemController.Category == ItemCategory.Weapon)
        {
            SetWeapon(itemController);
        }
    }


    public void SetWeapon(ItemController item)
    {
        weapon = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAppearanceItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Gloves:
            case ItemType.Chest:
            case ItemType.Leggings:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }

}