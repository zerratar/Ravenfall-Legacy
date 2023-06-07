using System;

[Serializable]
public class EquipmentStats : IComparable
{
    public int BaseArmorPower;
    public int BaseWeaponAim;
    public int BaseWeaponPower;
    public int BaseMagicPower;
    public int BaseMagicAim;
    public int BaseRangedPower;
    public int BaseRangedAim;
    public int BaseHealingPower;

    public int ArmorPowerBonus;
    public int WeaponAimBonus;
    public int WeaponPowerBonus;
    public int MagicPowerBonus;
    public int MagicAimBonus;
    public int RangedPowerBonus;
    public int RangedAimBonus;
    public int HealingPowerBonus;

    public int WeaponBonus => WeaponAimBonus + WeaponPowerBonus;
    public int RangedBonus => RangedAimBonus + RangedPowerBonus;
    public int MagicBonus => MagicAimBonus + MagicPowerBonus;

    public int ArmorPower => BaseArmorPower + ArmorPowerBonus;
    public int WeaponAim => BaseWeaponAim + WeaponAimBonus;
    public int WeaponPower => BaseWeaponPower + WeaponPowerBonus;
    public int MagicAim => BaseMagicAim + MagicAimBonus;
    public int MagicPower => BaseMagicPower + MagicPowerBonus;
    public int RangedAim => BaseRangedAim + RangedAimBonus;
    public int RangedPower => BaseRangedPower + RangedPowerBonus;
    public int HealingPower => BaseHealingPower + HealingPowerBonus;

    public static readonly EquipmentStats Zero = new EquipmentStats();

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is EquipmentStats eq)
        {
            return (ArmorPower + WeaponAim + WeaponPower + MagicPower + MagicAim + RangedPower + RangedAim + HealingPower) -
                   (eq.ArmorPower + eq.WeaponAim + eq.WeaponPower + eq.MagicPower + eq.MagicAim + eq.RangedPower + eq.RangedAim + eq.HealingPower);
        }
        return 0;
    }

    public static EquipmentStats operator *(EquipmentStats stats, float num)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower * num),
            BaseWeaponAim = (int)(stats.BaseWeaponAim * num),
            BaseWeaponPower = (int)(stats.BaseWeaponPower * num),
            BaseMagicPower = (int)(stats.BaseMagicPower * num),
            BaseMagicAim = (int)(stats.BaseMagicAim * num),
            BaseRangedPower = (int)(stats.BaseRangedPower * num),
            BaseRangedAim = (int)(stats.BaseRangedAim * num),
            BaseHealingPower = (int)(stats.BaseHealingPower * num),
        };
    }
}