using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class TownHouseManager : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private TownHouseSlot[] slots;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private BuildSlotIconsManager buildSlotIcons;
    [SerializeField] private TownHouse[] buildableTownHouses;

    private TownHouseSlot selectedHouseSlot;

    public TownHouse[] TownHouses => buildableTownHouses;

    void Start()
    {
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

    public static bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on Unity UI element.
    public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }
        return false;
    }
    ///Gets all event systen raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
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

    private bool HandleHouseSlotClick(RaycastHit hit)
    {
        var townHouseSlot = hit.collider.GetComponent<TownHouseSlot>();
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
        for (var i = 0; i < slots.Length; ++i)
        {
            SetActiveFast(slots[i].gameObject, i < count);
        }
    }

    internal void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
    {
        SetSlotCount(houses.Count);

        for (var i = 0; i < houses.Count; ++i)
        {
            var house = houses[i];
            var prefab = buildableTownHouses.FirstOrDefault(x => x.Type == house.Type);
            slots[i].SetHouse(house, prefab);
        }
    }

    internal void SetHouse(TownHouseSlot slot, int type)
    {
        var house = buildableTownHouses.FirstOrDefault(x => x.Type == type);
        slot.SetHouse(new VillageHouseInfo()
        {
            Slot = Array.IndexOf(slots, slot),
            Type = type
        }, house);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetActiveFast(GameObject gameObject, bool value)
    {
        if (gameObject.activeSelf != value)
        {
            gameObject.SetActive(value);
        }
    }
}
