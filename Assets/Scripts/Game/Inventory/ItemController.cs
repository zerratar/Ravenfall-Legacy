using System;
using RavenNest.Models;
using UnityEngine;
using Resources = UnityEngine.Resources;

public class ItemController : MonoBehaviour
{
    private GameObject prefab;
    private GameObject model;

    public Guid Id;
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
    public string MaleModelID;
    public string FemaleModelID;

    public string GenericPrefabPath;
    public string MalePrefabPath;
    public string FemalePrefabPath;
    public bool IsGenericModel;

    [SerializeField] private float pickupRadius = 3f;

    private RavenNest.Models.Item definition;
    private DropEventManager dropEventManager;
    private bool pickable;

    public int Compare(RavenNest.Models.Item item)
    {
        var stats1 = WeaponAim + WeaponPower + ArmorPower;
        var stats2 = item.WeaponAim + item.WeaponPower + item.ArmorPower;
        return stats1 - stats2;
    }

    public void Update()
    {
        if (transform.localPosition.x != 0)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

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
        definition = item;

        gameObject.name = item.Name;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Id = item.Id;
        Name = item.Name;
        Level = item.Level;

        WeaponAim = item.WeaponAim;
        WeaponPower = item.WeaponPower;
        ArmorPower = item.ArmorPower;

        MagicAim = item.MagicAim;
        MagicPower = item.MagicPower;

        RangedPower = item.RangedPower;
        RangedAim = item.RangedAim;

        RequiredAttackLevel = item.RequiredAttackLevel;
        RequiredDefenseLevel = item.RequiredDefenseLevel;
        RequiredRangedLevel = item.RequiredRangedLevel;
        RequiredMagicLevel = item.RequiredMagicLevel;
        RequiredSlayerLevel = item.RequiredSlayerLevel;

        Category = item.Category;
        Type = item.Type;
        Material = item.Material;
        FemaleModelID = item.FemaleModelId;
        MaleModelID = item.MaleModelId;
        GenericPrefabPath = item.GenericPrefab;
        MalePrefabPath = item.MalePrefab;
        FemalePrefabPath = item.FemalePrefab;
        IsGenericModel = item.IsGenericModel.GetValueOrDefault();

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

        if (player.PickupEventItem(Id))
        {
            dropEventManager.RemoveDropItem(this);
            Destroy(gameObject);
        }
    }
}