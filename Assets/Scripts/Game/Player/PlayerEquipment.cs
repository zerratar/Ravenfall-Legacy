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

    [SerializeField] private ItemController shield;
    [SerializeField] private ItemController weapon;
    [SerializeField] private ItemController staff;
    [SerializeField] private ItemController bow;

    private GameManager gameManager;

    private List<ItemController> equippedObjects;

    private PlayerController player;
    private IPlayerAppearance appearance;

    private GameObject woodcuttingHatchet;
    private GameObject miningPickaxe;
    private GameObject hammer;
    private GameObject fishingRod;
    private GameObject rake;

    public IReadOnlyList<ItemController> EquippedItems => equippedObjects;
    public bool HasShield => !!shield;

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
        if (shield) shield.gameObject.SetActive(false);
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
            bow = Instantiate(bowPrefab, appearance.OffHandTransform)
                    .GetComponent<ItemController>();

        if (bow)
        {
            bow.transform.SetParent(appearance.OffHandTransform);
            bow.gameObject.SetActive(true);
        }
    }

    private void ShowStaff()
    {
        HideEquipments();

        if (!staff && staffPrefab)
            staff = Instantiate(staffPrefab, appearance.MainHandTransform)
                        .GetComponent<ItemController>();

        if (staff)
        {
            staff.transform.SetParent(appearance.MainHandTransform);
            staff.gameObject.SetActive(true);
        }
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

            case AttackType.Healing:
            case AttackType.Magic:
                ShowStaff();
                return;
            default:
                var showShield = !!shield;
                if (weapon)
                {
                    weapon.gameObject.SetActive(true);
                    showShield &= IsOneHandedWeapon(weapon);
                }
                if (showShield)
                {
                    shield.gameObject.SetActive(true);
                }
                return;
        }
    }

    public bool IsOneHandedWeapon(ItemController weapon)
    {
        return weapon.Type == ItemType.OneHandedAxe ||
                        weapon.Type == ItemType.OneHandedSword ||
                        weapon.Type == ItemType.OneHandedMace;
    }

    public void EquipAll(IReadOnlyList<RavenNest.Models.Item> inventoryEquippedItems)
    {
        HideWeapon();
        appearance.UpdateAppearance();

        foreach (var item in inventoryEquippedItems)
        {
            Equip(item);
        }

        appearance.Optimize();

        HideWeapon();

        if (player.TrainingMagic)
            ShowWeapon(AttackType.Magic);

        if (player.TrainingRanged)
            ShowWeapon(AttackType.Ranged);

        if (player.TrainingMelee)
            ShowWeapon(AttackType.Melee);
    }

    public void UpdateAppearance()
    {
        appearance.UpdateAppearance();

        appearance.Optimize();
    }

    public bool DestroyArmorMesh()
    {
        var cm = appearance.GetCombinedMesh();

        if (cm)
        {
            for (var i = 0; i < cm.childCount; ++i)
            {
                var c = cm.GetChild(i);
                if (c)
                {
                    var smr = c.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        if (!smr.material.name.ToLower().Contains("fantasyhero"))
                        {
                            DestroyImmediate(c.gameObject);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public void Unequip(Guid id)
    {
        var equipped = equippedObjects.FirstOrDefault(x => x.Id == id);
        var removed = false;
        if (equipped != null)
        {
            appearance.Unequip(equipped);
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

        if (itemController.Type == ItemType.Shield)
        {
            SetShield(itemController);
        }

        if (itemController.Category == ItemCategory.Weapon)
        {
            SetWeapon(itemController);
        }
    }

    public void SetShield(ItemController item)
    {
        shield = item;
    }

    public void SetWeapon(ItemController item)
    {
        if (item.Type == ItemType.TwoHandedBow)
            bow = item;
        else if (item.Type == ItemType.TwoHandedStaff)
            staff = item;
        else
        {
            weapon = item;
        }
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