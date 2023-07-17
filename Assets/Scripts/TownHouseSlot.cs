﻿using RavenNest.Models;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class TownHouseSlot : MonoBehaviour
{
    [SerializeField] private GameObject mud;
    [SerializeField] private GameManager gameManager;

    private PlayerManager playerManager;
    private MeshRenderer meshRenderer;
    private TownHouse townHouseSource;

    public TownHouseController House;
    public Skills PlayerSkills { get; private set; }
    public string PlayerName { get; private set; }
    public string OwnerPlatformId { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public PlayerController Player { get; private set; }
    public TownHouseSlotType SlotType { get; set; }
    public int Slot { get; set; }
    public bool Selected { get; private set; }
    public float IconOffset => townHouseSource?.IconOffset ?? 0;


    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!mud) mud = transform.Find("Mud").gameObject;
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!playerManager) playerManager = FindObjectOfType<PlayerManager>();
        if (!meshRenderer) meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void SetHouse(VillageHouseInfo houseInfo, TownHouse townHouse, bool updateExpBonus = true)
    {
        if (SlotType == (TownHouseSlotType)houseInfo.Type && OwnerUserId == houseInfo.OwnerUserId && updateExpBonus)
        {
            UpdateExpBonus();
            return;
        }

        OwnerUserId = houseInfo.OwnerUserId;
        Player = houseInfo.OwnerUserId != null ? playerManager.GetPlayerByUserId(houseInfo.OwnerUserId.Value) : null;

        if (Player)
        {
            PlayerSkills = Player.Stats;
            PlayerName = Player.Name;
        }

        SlotType = (TownHouseSlotType)houseInfo.Type;
        Slot = houseInfo.Slot;
        townHouseSource = townHouse;

        try
        {
            // if we have a hut,
            // check if the hut changed. If it did, we want to destroy and instantiate the new model.
            if (House)
            {
                House.Slot = this;

                if (townHouse != null && House.TownHouse.Name == townHouse.Name)
                {
                    return;
                }

                Destroy(House.gameObject);
                House = null;
                mud.SetActive(true);
            }

            // if the townhouse was null, or the type is now empty. Return out
            if (!townHouse || SlotType == TownHouseSlotType.Empty)
            {
                return;
            }

            var houseObject = Instantiate(townHouse.Prefab, this.transform);
            if (houseObject)
            {
                House = houseObject.GetComponent<TownHouseController>();
                House.TownHouse = townHouse;
                House.Slot = this;
                mud.SetActive(false);
            }
        }
        finally
        {
            UpdateExpBonus(updateExpBonus);
        }
    }

    public void UpdateExpBonus(bool notify = true)
    {
        float bonus = 0;
        try
        {
            var playerSkills = this.PlayerSkills;
            if (Player != null && Player)
            {
                playerSkills = Player.Stats;
            }

            if (playerSkills != null)
            {
                var existingSkill = GameMath.GetSkillByHouseType(playerSkills, SlotType);
                bonus = GameMath.CalculateHouseExpBonus(existingSkill);
                if (notify)
                {
                    gameManager?.Village?.SetBonus(Slot, SlotType, bonus);
                }
                else
                {
                    gameManager?.Village?.SetBonusWithoutNotify(Slot, SlotType, bonus);
                }
                return;
            }

            if (notify)
            {
                gameManager?.Village?.SetBonus(Slot, SlotType, 0);
            }
            else
            {
                gameManager?.Village?.SetBonusWithoutNotify(Slot, SlotType, 0);
            }

        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }

    public void InvalidateOwner()
    {
        Player = OwnerUserId != null ? playerManager.GetPlayerByUserId(OwnerUserId.Value) : null;
        if (Player)
        {
            PlayerSkills = Player.Stats;
            PlayerName = Player.Name;
        }
        else
        {
            PlayerSkills = null;
        }

        if (OwnerUserId == null)
        {
            PlayerSkills = null;
            PlayerName = null;
        }

        UpdateExpBonus();
    }

    internal void SetOwner(Guid? userId)
    {
        this.OwnerUserId = userId;
        InvalidateOwner();
    }

    internal void RemoveOwner()
    {
        this.OwnerUserId = null;
        InvalidateOwner();
    }

    public void Deselect(Material defaultMaterial)
    {
        Selected = false;
        meshRenderer.material = defaultMaterial;
    }

    public void Select(Material selectedMaterial)
    {
        Selected = true;
        meshRenderer.material = selectedMaterial;
    }
}
