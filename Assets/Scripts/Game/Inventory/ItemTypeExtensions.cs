using RavenNest.Models;
using System.Runtime.CompilerServices;

public static class ItemTypeExtensions
{
    public static float GetAnimationLength(this GameInventoryItem item) => GetAnimationLength(item.Item);
    public static float GetAnimationLength(this Item item)
    {
        if (item == null) return 0;
        if (IsKatana(item.Name)) return 1.2f;
        return item.Type.GetAnimationLength();
    }

    private static float GetAnimationLength(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedSpear: return 1.2f;
            case ItemType.TwoHandedAxe: return 1.6f;
            // casting magic: 1f
            // attacking with staff: 1.2f
            // casting buff: 1.3f
            case ItemType.TwoHandedStaff: return 1f; // this is for casting magic though. 
            case ItemType.TwoHandedBow: return 0.8f;
            case ItemType.TwoHandedSword: return 1.2f;

            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 0.8f;
        }

        return 1.5f;
    }

    public static int GetAnimation(this GameInventoryItem item) => GetAnimation(item.Item);
    public static int GetAnimation(this Item item)
    {
        if (item == null) return 0;
        if (IsKatana(item.Name)) return 5;

        return item.Type.GetAnimation();
    }


    private static int GetAnimation(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedAxe: return 8;
            case ItemType.TwoHandedSpear: return 7;
            // healing: 6
            // katana: 5
            // casting magic: 4
            case ItemType.TwoHandedStaff: return 4;
            case ItemType.TwoHandedBow: return 3;
            case ItemType.TwoHandedSword: return 2;
            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 1;
        }

        return 0;
    }



    public static int GetAnimationCount(this GameInventoryItem item)
    {
        return item.Item.GetAnimationCount();
    }

    public static int GetAnimationCount(this Item item)
    {
        if (item == null) return 0;
        if (IsKatana(item.Name)) return 5;

        return item.Type.GetAnimationCount();
    }

    public static int GetAnimationCount(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedStaff:
            case ItemType.TwoHandedBow: return 0;
            case ItemType.TwoHandedSword: return 3;
            case ItemType.TwoHandedAxe: return 3;
            case ItemType.TwoHandedSpear: return 3;

            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 2;
        }

        return 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsKatana(string name)
    {
        return name.IndexOf("katana", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}