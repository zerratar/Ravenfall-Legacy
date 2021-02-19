using System;

[Serializable]
public class EquipmentStats : IComparable
{
    public int ArmorPower;
    public int WeaponAim;
    public int WeaponPower;
    public int MagicPower;
    public int MagicAim;
    public int RangedPower;
    public int RangedAim;

    public static EquipmentStats Zero => new EquipmentStats();

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is EquipmentStats eq)
        {
            return (ArmorPower + WeaponAim + WeaponPower + MagicPower + MagicAim + RangedPower + RangedAim) -
                   (eq.ArmorPower + eq.WeaponAim + eq.WeaponPower + eq.MagicPower + eq.MagicAim + eq.RangedPower + eq.RangedAim);
        }
        return 0;
    }

    public static EquipmentStats operator *(EquipmentStats stats, float num)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            ArmorPower = (int)(stats.ArmorPower * num),
            WeaponAim = (int)(stats.WeaponAim * num),
            WeaponPower = (int)(stats.WeaponPower * num),
            MagicPower = (int)(stats.MagicPower * num),
            MagicAim = (int)(stats.MagicAim * num),
            RangedPower = (int)(stats.RangedPower * num),
            RangedAim = (int)(stats.RangedAim * num)
        };
    }
}