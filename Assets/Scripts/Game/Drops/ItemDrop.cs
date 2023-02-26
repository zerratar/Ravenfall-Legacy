using System;
using UnityEngine;

[Serializable]
public class ItemDrop
{
    public string ItemID;
    public string ItemName;

    [Range(0.0001f, 1f)]
    public float DropChance;

    public bool Unique;

    public RavenNest.Models.Item Item;
}
