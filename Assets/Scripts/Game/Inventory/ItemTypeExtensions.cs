using RavenNest.Models;

public static class ItemTypeExtensions
{
    public static int GetAnimation(this Item item)
    {
        if (item == null) return 0;
        if (item.Name.IndexOf("katana", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 5;
        }

        return item.Type.GetAnimation();
    }
    public static int GetAnimation(this GameInventoryItem item) => GetAnimation(item.Item);
    public static int GetAnimation(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedStaff: return 4;
            case ItemType.TwoHandedBow: return 3;
            case ItemType.TwoHandedSword: return 2;
            case ItemType.OneHandedMace:
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
        if (item.Name.IndexOf("katana", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 5;
        }

        return item.Type.GetAnimationCount();
    }

    public static int GetAnimationCount(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedStaff:
            case ItemType.TwoHandedBow: return 0;
            case ItemType.TwoHandedSword: return 3;
            case ItemType.OneHandedMace:
            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 2;
        }

        return 0;
    }
}