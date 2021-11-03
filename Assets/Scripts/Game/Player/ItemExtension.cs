using RavenNest.Models;

public static class ItemExtension
{
    public static int GetTotalStats(this ItemController item)
    {
        return item.Level
            + item.WeaponPower
            + item.WeaponAim
            + item.ArmorPower
            + item.MagicAim
            + item.MagicPower
            + item.RangedAim
            + item.RangedPower;
    }
    public static int GetTotalStats(this Item item)
    {
        return item.Level
            + item.WeaponPower
            + item.WeaponAim
            + item.ArmorPower
            + item.MagicAim
            + item.MagicPower
            + item.RangedAim
            + item.RangedPower;
    }
}
