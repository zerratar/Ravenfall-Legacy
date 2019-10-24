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
}