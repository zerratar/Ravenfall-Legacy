﻿using System;
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
    private ItemManager itemManager;
    private List<ItemController> equippedObjects;

    private PlayerController player;
    private SyntyPlayerAppearance appearance;

    private GameObject woodcuttingHatchet;
    private GameObject miningPickaxe;
    private GameObject hammer;
    private GameObject fishingRod;
    private GameObject rake;
    private float appearanceRefreshTimeout;
    private AttackType lastShownWeapon;
    private bool woodcuttingHatchetVisible;
    private bool miningPickaxeVisible;
    private bool fishingRodVisible;
    private bool rakeVisible;
    private bool hammerVisible;
    private bool staffVisible;
    private bool bowVisible;
    private bool shieldVisible;
    private bool weaponVisible;

    //private bool staffVisible;
    //private bool bowVisible;
    //private bool shieldVisible;
    //private bool weaponVisible;
    //private bool rakeVisible;
    //private bool hammerVisible;
    //private bool woodcuttingHatchetVisible;
    //private bool miningPickaxeVisible;
    //private bool fishingRodVisible;

    public IReadOnlyList<ItemController> EquippedItems => equippedObjects;
    public bool HasShield => !!shield;

    private void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        itemManager = gameManager?.Items ?? FindAnyObjectByType<ItemManager>();
        equippedObjects = new List<ItemController>();
        appearance = GetComponent<SyntyPlayerAppearance>();
        player = GetComponent<PlayerController>();
    }

    public void HideEquipments(bool forceHide = false)
    {
        HideRake(forceHide);
        HideWeapon(forceHide);
        HideHatchet(forceHide);
        HidePickAxe(forceHide);
        HideHammer(forceHide);
        HideFishingRod(forceHide);
        lastShownWeapon = AttackType.None;
    }

    public void HideHatchet(bool forceHide = false)
    {
        if (forceHide || woodcuttingHatchetVisible)
        {
            if (woodcuttingHatchet) woodcuttingHatchet.SetActive(false);
            woodcuttingHatchetVisible = false;
        }
    }

    public void HidePickAxe(bool forceHide = false)
    {
        if (forceHide || miningPickaxeVisible)
        {
            if (miningPickaxe) miningPickaxe.SetActive(false);
            miningPickaxeVisible = false;
        }
    }

    public void HideFishingRod(bool forceHide = false)
    {
        if (forceHide || fishingRodVisible)
        {
            if (fishingRod) fishingRod.SetActive(false);
            fishingRodVisible = false;
        }
    }

    public void HideRake(bool forceHide = false)
    {
        if (forceHide || rakeVisible)
        {
            if (rake) rake.SetActive(false);
            rakeVisible = false;
        }
    }

    public void HideWeapon(bool forceHide = false)
    {
        if (forceHide || weaponVisible)
        {
            if (weapon) weapon.gameObject.SetActive(false);
            weaponVisible = false;
        }

        if (forceHide || shieldVisible)
        {
            if (shield) shield.gameObject.SetActive(false);
            shieldVisible = false;
        }
        if (forceHide || bowVisible)
        {
            if (bow) bow.gameObject.SetActive(false);
            bowVisible = false;
        }
        if (forceHide || staffVisible)
        {
            if (staff) staff.gameObject.SetActive(false);
            staffVisible = false;
        }
    }

    public void HideHammer(bool forceHide = false)
    {
        if (forceHide || hammerVisible)
        {
            if (hammer) hammer.SetActive(false);
            hammerVisible = false;
        }
    }


    private void ShowBow()
    {
        try
        {
            if (bowVisible || lastShownWeapon == AttackType.Ranged)
            {
                return;
            }

            HideEquipments();

            if (!bow && bowPrefab)
            {
                bow = Instantiate(bowPrefab, appearance.OffHandTransform)
                        .GetComponent<ItemController>();
            }

            if (bow)
            {
                bow.transform.SetParent(appearance.OffHandTransform);
                bow.gameObject.SetActive(true);
                bowVisible = true;
            }
        }
        finally
        {
            lastShownWeapon = AttackType.Ranged;
        }
    }

    private void ShowStaff()
    {
        try
        {
            if (staffVisible || lastShownWeapon == AttackType.Magic)
            {
                return;
            }

            HideEquipments();

            if (!staff && staffPrefab)
            {
                staff = Instantiate(staffPrefab, appearance.MainHandTransform)
                            .GetComponent<ItemController>();
            }

            if (staff)
            {
                staff.transform.SetParent(appearance.MainHandTransform);
                staff.gameObject.SetActive(true);
                staffVisible = true;
            }
        }
        finally
        {
            lastShownWeapon = AttackType.Magic;
        }
    }

    public void ShowFishingRod()
    {
        if (fishingRodVisible)
            return;

        HideEquipments();

        if (!fishingRod && fishingRodPrefab)
            fishingRod = Instantiate(fishingRodPrefab, appearance.MainHandTransform);

        if (fishingRod)
        {
            fishingRod.SetActive(true);
            fishingRodVisible = true;
        }
    }

    public void ShowPickAxe()
    {
        if (miningPickaxeVisible)
            return;

        HideEquipments();

        if (!miningPickaxe && miningPickaxePrefab)
            miningPickaxe = Instantiate(miningPickaxePrefab, appearance.MainHandTransform);

        if (miningPickaxe)
        {
            miningPickaxe.SetActive(true);
            miningPickaxeVisible = true;
        }
    }

    public void ShowHammer()
    {
        if (hammerVisible)
            return;

        HideEquipments();

        if (!hammer && hammerPrefab)
            hammer = Instantiate(hammerPrefab, appearance.MainHandTransform);
        if (hammer)
        {
            hammer.SetActive(true);
            hammerVisible = true;
        }
        //hammerVisible = true;
    }

    public void ShowHatchet()
    {
        if (woodcuttingHatchetVisible)
            return;

        HideEquipments();

        if (!woodcuttingHatchet && woodcuttingHatchetPrefab)
            woodcuttingHatchet = Instantiate(woodcuttingHatchetPrefab, appearance.MainHandTransform);

        if (woodcuttingHatchet)
        {
            woodcuttingHatchet.SetActive(true);
            woodcuttingHatchetVisible = true;
        }
    }

    public void ShowRake()
    {
        if (rakeVisible) return;

        HideEquipments();
        if (!rake && rakePrefab) rake = Instantiate(rakePrefab, appearance.MainHandTransform);
        if (rake)
        {
            rake.SetActive(true);
            rakeVisible = true;
        }
        //rakeVisible = true;
    }

    public void ShowWeapon(AttackType type)
    {
        try
        {
            //if (lastShownWeapon == type)
            //{
            //    return;
            //}

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
                        weaponVisible = true;
                        showShield &= IsOneHandedWeapon(weapon);
                    }
                    if (showShield)
                    {
                        shield.gameObject.SetActive(true);
                        shieldVisible = true;
                    }
                    return;
            }
        }
        finally
        {
            lastShownWeapon = type;
        }
    }

    public bool IsOneHandedWeapon(ItemController weapon)
    {
        return weapon.Type == ItemType.OneHandedAxe || weapon.Type == ItemType.OneHandedSword;
    }

    public void EquipAll(IReadOnlyList<GameInventoryItem> inventoryEquippedItems)
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

        else if (player.TrainingRanged)
            ShowWeapon(AttackType.Ranged);

        else if (player.TrainingMelee)
            ShowWeapon(AttackType.Melee);
    }

    public void UpdateAppearance()
    {
        appearance.UpdateAppearance();

        foreach (var e in EquippedItems)
        {
            appearance.Equip(e);
        }

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

    public void Unequip(GameInventoryItem item, bool rebuildMeshIfNecessary = false)
    {
        var equipped = equippedObjects.FirstOrDefault(x => x.Id == item.InstanceId);

        if (equipped == null)
        {
            // fallback to old search
            equipped = equippedObjects.FirstOrDefault(x => x.ItemId == item.ItemId);
        }

        var needMeshRebuild = string.IsNullOrEmpty(item.Item.GenericPrefab) && item.Item.Category == ItemCategory.Armor;
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
            Shinobytes.Debug.LogError($"{player.PlayerName} is trying to unequip item but item did not exist. {item.Name}");
        }

        if (rebuildMeshIfNecessary && needMeshRebuild)
        {
            UpdateAppearance();
        }
    }

    public void Equip(GameInventoryItem item, bool updateAppearance = true)
    {
        if (!baseItemPrefab)
        {
            Shinobytes.Debug.LogError("BaseItemPrefab not set on player!! Unable to create item");
            return;
        }

        var existingItemController = equippedObjects.FirstOrDefault(x => x.ItemId == item.Item.Id);
        if (existingItemController != null)
        {
            if (existingItemController)
            {
                Destroy(existingItemController.gameObject);
            }

            equippedObjects.Remove(existingItemController);
        }

        var itemController = itemManager.Create(item, player.Appearance.Gender == Gender.Male);
        itemController.transform.SetParent(transform);
        itemController.gameObject.layer = player.gameObject.layer;

        equippedObjects.Add(itemController);

        if (updateAppearance)
        {
            appearance.Equip(itemController);
        }

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
        DestroyItemObjectIfNotSame(shield, item);
        shield = item;
        shieldVisible = true;
    }

    public void SetWeapon(ItemController item)
    {
        if (item.Type == ItemType.TwoHandedBow)
        {
            DestroyItemObjectIfNotSame(bow, item);
            bow = item;
            item.gameObject.SetActive(true);
            bowVisible = true;
        }
        else if (item.Type == ItemType.TwoHandedStaff)
        {
            DestroyItemObjectIfNotSame(staff, item);
            staff = item;
            item.gameObject.SetActive(true);
            staffVisible = true;
        }
        else
        {
            DestroyItemObjectIfNotSame(weapon, item);
            weapon = item;
            item.gameObject.SetActive(true);
            weaponVisible = true;
        }
    }

    private void DestroyItemObjectIfNotSame(ItemController obj, ItemController other)
    {
        if (obj && obj.GetInstanceID() != other.GetInstanceID())
        {
            Destroy(obj.gameObject);
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