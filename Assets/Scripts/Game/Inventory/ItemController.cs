using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;
using UnityEngine;
using Resources = UnityEngine.Resources;

public class ItemController : MonoBehaviour
{
    private GameObject prefab;
    private GameObject model;

    public Guid Id;
    public Guid ItemId;

    public string Name;
    public int Level;
    public int WeaponAim;
    public int WeaponPower;
    public int MagicPower;
    public int MagicAim;
    public int RangedPower;
    public int RangedAim;
    public int ArmorPower;
    public int RequiredAttackLevel;
    public int RequiredDefenseLevel;
    public int RequiredRangedLevel;
    public int RequiredMagicLevel;
    public int RequiredSlayerLevel;

    public ItemCategory Category;
    public ItemType Type;

    public Item Skin { get; set; }
    public Item Item { get; private set; }
    public ItemManager ItemManager { get; private set; }

    public ItemMaterial Material;

    public int MaleModelID;
    public int FemaleModelID;

    public int[] MaleAdditionalIndex;
    public int[] FemaleAdditionalIndex;

    public string GenericPrefabPath;
    public string MalePrefabPath;
    public string FemalePrefabPath;
    public bool IsGenericModel;

    [SerializeField] private float pickupRadius = 3f;

    public bool UseMalePrefab { get; private set; }

    public GameInventoryItem Definition;

    private DropEventManager dropEventManager;
    private bool pickable;
    private Transform _transform;

    public int Compare(RavenNest.Models.Item item)
    {
        var stats1 = this.GetTotalStats();
        var stats2 = item.GetTotalStats();
        return stats1 - stats2;
    }

    public void Awake()
    {
        this.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        this._transform = this.transform;
    }
    public void Start()
    {
        this.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    internal void EnablePickup(DropEventManager dropEventManager)
    {
        this.dropEventManager = dropEventManager;
        pickable = true;
        var pickupCollider = gameObject.AddComponent<SphereCollider>();
        pickupCollider.radius = pickupRadius;
        pickupCollider.isTrigger = true;
    }

    public ItemController Create(ItemManager itemManager, RavenNest.Models.Item item, bool useMalePrefab)
    {
        return Create(itemManager, new GameInventoryItem(null, new InventoryItem { Id = Guid.NewGuid(), ItemId = item.Id }, item), useMalePrefab);
    }

    public ItemController Create(ItemManager itemManager, GameInventoryItem item, bool useMalePrefab)
    {
        this.ItemManager = itemManager;
        UseMalePrefab = useMalePrefab;
        Definition = item;

        item.Controller = this;

        gameObject.name = item.InventoryItem.Name ?? item.Item.Name;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        Item = item.Item;

        Id = item.InventoryItem.Id;
        ItemId = item.ItemId;
        Name = item.Name;
        Level = item.Item.Level;

        WeaponAim = item.Item.WeaponAim;
        WeaponPower = item.Item.WeaponPower;
        ArmorPower = item.Item.ArmorPower;

        MagicAim = item.Item.MagicAim;
        MagicPower = item.Item.MagicPower;

        RangedPower = item.Item.RangedPower;
        RangedAim = item.Item.RangedAim;

        RequiredAttackLevel = item.Item.RequiredAttackLevel;
        RequiredDefenseLevel = item.Item.RequiredDefenseLevel;
        RequiredRangedLevel = item.Item.RequiredRangedLevel;
        RequiredMagicLevel = item.Item.RequiredMagicLevel;
        RequiredSlayerLevel = item.Item.RequiredSlayerLevel;

        Category = item.Item.Category;
        Type = item.Item.Type;

        this.Skin = itemManager.Get(item.TransmogrificationId);
        if (Skin != null)
        {
            SetItemAppearance(itemManager, useMalePrefab, Skin);
        }
        else
        {
            SetItemAppearance(itemManager, useMalePrefab, Item);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        return this;
    }

    public void UpdateAppearance()
    {
        if (ItemManager == null)
        {
            return;
        }

        if (Skin != null)
        {
            SetItemAppearance(ItemManager, UseMalePrefab, Skin);
        }
        else if (Item != null)
        {
            SetItemAppearance(ItemManager, UseMalePrefab, Item);
        }
    }

    private void SetItemAppearance(ItemManager itemManager, bool useMalePrefab, Item item)
    {
        var indices = itemManager.GetModelIndices(item.Id);

        Material = item.Material;

        MaleModelID = indices.Item1.Item1;
        MaleAdditionalIndex = indices.Item1.Item2;

        FemaleModelID = indices.Item2.Item1;
        FemaleAdditionalIndex = indices.Item2.Item2;

        GenericPrefabPath = item.GenericPrefab;
        MalePrefabPath = item.MalePrefab;
        FemalePrefabPath = item.FemalePrefab;
        IsGenericModel = item.IsGenericModel || Category == ItemCategory.Pet || !string.IsNullOrEmpty(GenericPrefabPath);

        if (!prefab)
        {
            var path = IsGenericModel
                ? GenericPrefabPath
                : useMalePrefab
                    ? MalePrefabPath
                    : FemalePrefabPath;

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!itemManager.TryGetPrefab(path, out var prefab))
            {
                return;
            }

            if (prefab)
            {
                model = Instantiate(prefab, transform);
            }
            else
            {
                Debug.LogError("Failed to load prefab: " + path);
            }
        }
    }
    public void CleanupModel()
    {
        if (model != null)
        {
            GameObject.Destroy(model);
            model = null;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!pickable)
        {
            return;
        }

        var player = other.GetComponent<PlayerController>();
        if (!player || !player.ItemDropEventActive)
        {
            return;
        }

        if (!dropEventManager) return;

        if (player.PickupItemById(ItemId))
        {
            dropEventManager.RemoveDropItem(this);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
}