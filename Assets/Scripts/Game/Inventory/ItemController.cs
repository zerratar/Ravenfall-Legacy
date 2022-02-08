using System;
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
    public ItemMaterial Material;

    public int MaleModelID;
    public int FemaleModelID;

    public int[] AdditionalIndex;

    public string GenericPrefabPath;
    public string MalePrefabPath;
    public string FemalePrefabPath;
    public bool IsGenericModel;

    [SerializeField] private float pickupRadius = 3f;

    public GameInventoryItem Definition;

    private DropEventManager dropEventManager;
    private bool pickable;

    public int Compare(RavenNest.Models.Item item)
    {
        var stats1 = this.GetTotalStats();
        var stats2 = item.GetTotalStats();
        return stats1 - stats2;
    }
    //public void FixedUpdate()
    //{
    //    if (Time.frameCount % 30 == 0 && transform.localPosition.x != 0)
    //    {
    //        transform.localPosition = Vector3.zero;
    //        transform.localRotation = Quaternion.identity;
    //    }
    //}
    internal void EnablePickup(DropEventManager dropEventManager)
    {
        this.dropEventManager = dropEventManager;
        pickable = true;
        var pickupCollider = gameObject.AddComponent<SphereCollider>();
        pickupCollider.radius = pickupRadius;
        pickupCollider.isTrigger = true;
    }

    public ItemController Create(RavenNest.Models.Item item, bool useMalePrefab)
    {
        return Create(new GameInventoryItem(null, new InventoryItem { Id = Guid.NewGuid(), ItemId = item.Id }, item), useMalePrefab);
    }

    public ItemController Create(GameInventoryItem item, bool useMalePrefab)
    {
        Definition = item;

        gameObject.name = item.InventoryItem.Name ?? item.Item.Name;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Id = item.InventoryItem.Id;
        ItemId = item.Item.Id;
        Name = item.InventoryItem.Name ?? item.Item.Name;
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
        Material = item.Item.Material;


        if (!string.IsNullOrEmpty(item.Item.FemaleModelId))
        {
            if (item.Item.FemaleModelId.Contains(","))
            {
                var indices = item.Item.FemaleModelId.Split(',');
                FemaleModelID = int.Parse(indices[0]);
                AdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
            }
            else
            {
                FemaleModelID = int.Parse(item.Item.FemaleModelId);
            }
        }
        else
        {
            FemaleModelID = -1;
        }

        if (!string.IsNullOrEmpty(item.Item.MaleModelId))
        {
            if (item.Item.MaleModelId.Contains(","))
            {
                var indices = item.Item.MaleModelId.Split(',');
                MaleModelID = int.Parse(indices[0]);
                AdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
            }
            else
            {
                MaleModelID = int.Parse(item.Item.MaleModelId);
            }
        }
        else
        {
            MaleModelID = -1;
        }

        GenericPrefabPath = item.Item.GenericPrefab;
        MalePrefabPath = item.Item.MalePrefab;
        FemalePrefabPath = item.Item.FemalePrefab;
        IsGenericModel = item.Item.IsGenericModel.GetValueOrDefault() || Category == ItemCategory.Pet || !string.IsNullOrEmpty(GenericPrefabPath);

        if (!prefab)
        {
            var path = IsGenericModel
                ? GenericPrefabPath
                : useMalePrefab
                    ? MalePrefabPath
                    : FemalePrefabPath;

            if (string.IsNullOrEmpty(path))
            {
                return this;
            }

            prefab = Resources.Load<GameObject>(path);
            if (!prefab)
            {
                //Debug.LogError(this.name + " failed to load prefab: " + path);
                return this;
            }

            model = Instantiate(prefab, transform) as GameObject;            
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        return this;
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