﻿using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;

public class CraftingStation : MonoBehaviour
{
    public int Level = 1;
    public double ExpPerResource => 10;

    public IslandController Island { get; private set; }

    [HideInInspector] public bool IsInvalid;

    public CraftingStationType StationType = CraftingStationType.Crafting;

    public float ExpMultiplier = 1f;

    [ReadOnly]
    public float MaxActionDistance = 5;


    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }
    private float invalidTimer;
    private void Update()
    {
        if (IsInvalid)
        {
            invalidTimer += GameTime.deltaTime;
            if (invalidTimer >= 20)
            {
                invalidTimer = 0f;
                IsInvalid = false;
            }
        }
    }
    void Start()
    {
        this.Island = GetComponentInParent<IslandController>();
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }
    //public double GetExperience(PlayerController playerController)
    //{
    //    ExpMultiplier = Mathf.Min(1f, ExpMultiplier);
    //    var resx = playerController.Stats;
    //    var level = StationType == CraftingStationType.Cooking ? resx.Cooking.Level : resx.Crafting.Level;
    //    var a = Mathf.FloorToInt(level / 10f) + 1;
    //    var b = Mathf.FloorToInt(level / 10f) + 1;
    //    return (ExpPerResource * a + ExpPerResource * b) * ExpMultiplier;
    //}

    public bool Craft(PlayerController player)
    {
        return true;
    }
}

public enum CraftingStationType
{
    Crafting,
    Cooking,
    Brewing
}
