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

    private RavenNest.Models.Item definition;
    private DropEventManager dropEventManager;
    private bool pickable;

    public int Compare(RavenNest.Models.Item item)
    {
        var stats1 = this.GetTotalStats();
        var stats2 = item.GetTotalStats();
        return stats1 - stats2;
    }
    public void FixedUpdate()
    {
        if (Time.frameCount % 30 == 0 && transform.localPosition.x != 0)
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


        if (!string.IsNullOrEmpty(item.FemaleModelId))
        {
            if (item.FemaleModelId.Contains(","))
            {
                var indices = item.FemaleModelId.Split(',');
                FemaleModelID = int.Parse(indices[0]);
                AdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
            }
            else
            {
                FemaleModelID = int.Parse(item.FemaleModelId);
            }
        }
        else
        {
            FemaleModelID = -1;
        }

        if (!string.IsNullOrEmpty(item.MaleModelId))
        {
            if (item.MaleModelId.Contains(","))
            {
                var indices = item.MaleModelId.Split(',');
                MaleModelID = int.Parse(indices[0]);
                AdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
            }
            else
            {
                MaleModelID = int.Parse(item.MaleModelId);
            }
        }
        else
        {
            MaleModelID = -1;
        }

        GenericPrefabPath = item.GenericPrefab;
        MalePrefabPath = item.MalePrefab;
        FemalePrefabPath = item.FemalePrefab;
        IsGenericModel = item.IsGenericModel.GetValueOrDefault() || Category == ItemCategory.Pet || !string.IsNullOrEmpty(GenericPrefabPath);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

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
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
}