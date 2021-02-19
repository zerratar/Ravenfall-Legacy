using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    public void Start()
    {
    }

    internal bool ToggleVisibility()
    {
        var newState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(newState);
        return newState;
    }

    internal void Hide()
    {
        inventoryPanel.SetActive(false);
    }
}
