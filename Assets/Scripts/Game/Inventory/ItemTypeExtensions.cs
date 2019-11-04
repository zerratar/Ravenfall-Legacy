using RavenNest.Models;

public static class ItemTypeExtensions
{  
    public static int GetAnimation(this ItemType type)
    {
        switch (type)
        {            
            case ItemType.TwoHandedSword: return 2;
            case ItemType.OneHandedMace:
            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 1;
        }

        return 0;
    }  

    public static int GetAnimationCount(this ItemType type)
    {
        switch (type)
        {
            case ItemType.TwoHandedSword: return 3;
            case ItemType.OneHandedMace:
            case ItemType.OneHandedAxe:
            case ItemType.OneHandedSword: return 2;
        }

        return 0;
    }
}