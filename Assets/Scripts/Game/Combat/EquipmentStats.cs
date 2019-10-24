using System;

[Serializable]
public class EquipmentStats : IComparable
{
    public int ArmorPower;
    public int WeaponAim;
    public int WeaponPower;

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is EquipmentStats eq)
        {
            return (ArmorPower + WeaponAim + WeaponPower) -
                   (eq.ArmorPower + eq.WeaponAim + eq.WeaponPower);
        }
        return 0;
    }
}