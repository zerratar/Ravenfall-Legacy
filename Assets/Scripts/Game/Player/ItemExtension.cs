using RavenNest.Models;

public static class ItemExtension
{
    public static int GetTotalStats(this Item item)
    {
        return item.Level + item.ArmorPower + item.WeaponAim + item.WeaponPower;
    }
}
