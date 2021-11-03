using System.Linq;
using UnityEngine;

public class AttributeStatsManager : MonoBehaviour
{
    [SerializeField] private AttributeStats armor;
    [SerializeField] private AttributeStats melee;
    [SerializeField] private AttributeStats ranged;
    [SerializeField] private AttributeStats magic;

    [SerializeField] private AttributeStats exp;
    [SerializeField] private AttributeStats raid;
    [SerializeField] private AttributeStats dungeon;

    private PlayerController observedPlayer;
    private float nextUpdate = 0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!observedPlayer)
        {
            return;
        }

        if (nextUpdate > 0f)
        {
            nextUpdate -= Time.deltaTime;
            if (nextUpdate <= 0f)
            {
                UpdateTexts(observedPlayer);
                nextUpdate = 1f;
            }
        }

        // in case we want to update every frame..
    }

    internal void Observe(PlayerController player)
    {
        observedPlayer = player;
        if (!player)
            return;

        UpdateTexts(player);
    }

    private void UpdateTexts(PlayerController player)
    {
        var scrolls = player.Inventory.GetInventoryItemsOfCategory(RavenNest.Models.ItemCategory.Scroll);
        var e = scrolls?.FirstOrDefault(x => x.Item.Name.ToLower().Contains("exp"));
        exp.Text = ((long)(e?.Amount ?? 0d)).ToString();
        var r = scrolls?.FirstOrDefault(x => x.Item.Name.ToLower().Contains("raid"));
        raid.Text = ((long)(r?.Amount ?? 0d)).ToString();
        var d = scrolls?.FirstOrDefault(x => x.Item.Name.ToLower().Contains("dungeon"));
        dungeon.Text = ((long)(d?.Amount ?? 0d)).ToString();

        armor.Text = observedPlayer.EquipmentStats.ArmorPower.ToString();
        melee.Text = observedPlayer.EquipmentStats.WeaponAim + "\n" + observedPlayer.EquipmentStats.WeaponPower;
        ranged.Text = observedPlayer.EquipmentStats.RangedAim + "\n" + observedPlayer.EquipmentStats.RangedPower;
        magic.Text = observedPlayer.EquipmentStats.MagicAim + "\n" + observedPlayer.EquipmentStats.MagicPower;
    }
}
