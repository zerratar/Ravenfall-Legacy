using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TownHouseManager : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TownHouseSlot[] slots;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private BuildSlotIconsManager buildSlotIcons;
    [SerializeField] private TownHouse[] buildableTownHouses;
    [SerializeField] private TownHallInfoManager info;

    private TownHouseSlot selectedHouseSlot;
    private int activeSlotCount;

    public TownHouse[] TownHouses => buildableTownHouses;
    
    public int SlotCount => activeSlotCount;
    public void EnsureAssignPlayerRows(int count)
    {
        buildSlotIcons.AssignPlayerDialog.EnsureAssingPlayerRows(count);
    }

    void Start()
    {
        if (!info) info = FindObjectOfType<TownHallInfoManager>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        slots = GetComponentsInChildren<TownHouseSlot>();
        SetSlotCount(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUIElement())
                return;

            var ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray);
            if (hits.Length == 0)
                return;

            //if (!Physics.Raycast(ray, out var hit))
            //    return;

            foreach (var hit in hits)
            {
                if (HandleHouseButtonClick(hit))
                {
                    return;
                }
            }

            var hitSlot = false;
            foreach (var hit in hits)
            {
                hitSlot = hitSlot || HandleHouseSlotClick(hit);
            }

            if (!hitSlot)
            {
                if (selectedHouseSlot)
                    selectedHouseSlot.Deselect(defaultMaterial);

                buildSlotIcons.Hide();
            }
        }
    }

    private bool HandleHouseButtonClick(RaycastHit hit)
    {
        var button = hit.collider.GetComponent<UIButton>();
        if (button)
        {
            if (button.Click != null)
            {
                button.Click.Invoke();
            }

            return true;
        }
        return false;
    }

    internal bool IsHouseOwner(PlayerController player)
    {
        return slots.Any(x => x.OwnerUserId == player?.UserId);
    }

    internal void SetOwner(TownHouseSlot activeSlot, PlayerController owner)
    {
        if (slots == null || slots.Length == 0)
            return;

        if (!activeSlot || activeSlot == null)
            return;

        if (owner != null && owner)
        {
            var existingHouse = slots.FirstOrDefault(x => x.OwnerUserId == owner.UserId);
            if (existingHouse != null && existingHouse)
            {
                existingHouse.RemoveOwner();
            }

            activeSlot.SetOwner(owner.UserId);
            return;
        }

        activeSlot.RemoveOwner();
    }

    private bool HandleHouseSlotClick(RaycastHit hit)
    {
        var townHouseController = hit.collider.GetComponent<TownHouseController>();
        var townHouseSlot = hit.collider.GetComponent<TownHouseSlot>();

        if (townHouseController)
            townHouseSlot = townHouseController.GetComponentInParent<TownHouseSlot>();

        if (!townHouseSlot)
            return false;

        if (selectedHouseSlot)
            selectedHouseSlot.Deselect(defaultMaterial);

        selectedHouseSlot = townHouseSlot;
        selectedHouseSlot.Select(selectedMaterial);
        buildSlotIcons.Show(selectedHouseSlot);
        return true;
    }

    internal void SetSlotCount(int count)
    {
        this.activeSlotCount = count;
        for (var i = 0; i < slots.Length; ++i)
        {
            SetActiveFast(slots[i].gameObject, i < count);
        }
    }

    internal void InvalidateOwnershipOfHouses()
    {
        foreach (var slot in slots)
        {
            slot.InvalidateOwner();
        }

        gameManager.UpdateVillageBoostText();
    }

    internal void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
    {
        SetSlotCount(houses.Count);
        for (var i = 0; i < houses.Count; ++i)
        {
            var house = houses[i];
            var prefab = buildableTownHouses.FirstOrDefault(x => x.Type == (TownHouseSlotType)house.Type);
            slots[i].SetHouse(house, prefab);
        }

        if (slots != null && slots.Length > 0)
        {
            info.UsedSlots = slots.Count(x => x != null && x.SlotType != TownHouseSlotType.Empty && x.SlotType != TownHouseSlotType.Undefined);
        }
    }

    internal void SetHouse(TownHouseSlot slot, TownHouseSlotType type)
    {
        if (slot == null || !slot)
            return;

        if (type == TownHouseSlotType.Empty)
        {
            SetHouseType(slot, TownHouseSlotType.Empty, null);
            return;
        }

        if (buildableTownHouses == null || buildableTownHouses.Length == 0)
            return;

        var house = buildableTownHouses.FirstOrDefault(x => x.Type == type);
        if (house == null || !house)
            return;

        SetHouseType(slot, type, house);
    }

    private void SetHouseType(TownHouseSlot slot, TownHouseSlotType type, TownHouse house)
    {
        try
        {
            slot.SetHouse(new VillageHouseInfo()
            {
                Slot = slot.Slot,
                Type = (int)type
            }, house);

            if (slots != null && slots.Length > 0)
            {
                info.UsedSlots = slots.Count(x => x != null && x.SlotType != TownHouseSlotType.Empty && x.SlotType != TownHouseSlotType.Undefined);
            }
        }
        catch (Exception exc)
        {
            GameManager.LogError(exc.ToString());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetActiveFast(GameObject gameObject, bool value)
    {
        if (gameObject.activeSelf != value)
        {
            gameObject.SetActive(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    ///Returns 'true' if we touched or hovering on Unity UI element.
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI") &&
                curRaysastResult.gameObject.tag == "BuildingDialog")
            {
                return true;
            }
        }
        return false;
    }

    ///Gets all event systen raycast results of current mouse or touch position.
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
