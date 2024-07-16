using RavenNest.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameMath;

public class EnemyObserveDialog : MonoBehaviour
{
    [SerializeField] private StatObserver attack;
    [SerializeField] private StatObserver defense;
    [SerializeField] private StatObserver strength;

    [SerializeField] private AttributeStats armor;
    [SerializeField] private AttributeStats melee;

    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI lblHealth;
    [SerializeField] private TextMeshProUGUI lblName;
    [SerializeField] private TextMeshProUGUI lblCombatLevel;

    [SerializeField] private TextMeshProUGUI lblDeathCount;
    [SerializeField] private TextMeshProUGUI lblKillCount;


    private EnemyController target;
    private int lastDeathCount;
    private int lastPlayerKill;
    public bool IsVisible { get; private set; }
    public void Awake()
    {
        Close();
    }

    public void ShowDialog(EnemyController ec)
    {
        this.target = ec;

#if UNITY_EDITOR
        UnityEngine.Debug.Log("Show Enemy Observe Dialog: " + target.Name);
#endif

        this.UpdateUI();
        this.gameObject.SetActive(true);
        IsVisible = true;
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
        IsVisible = false;
    }

    private void Update()
    {
        if (!target) return;
        // update health
        if (healthBar)
        {
            healthBar.fillAmount = target.Stats.Health.CurrentValue / target.Stats.Health.Level; // don't use max level here.
            lblHealth.text = target.Stats.Health.CurrentValue + " HP";
        }

        if (lastDeathCount != target.DeathCount)
        {
            lblDeathCount.text = target.DeathCount.ToString();
            lastDeathCount = target.DeathCount;
        }

        if (lastPlayerKill != target.PlayerKillCount)
        {
            lblKillCount.text = target.PlayerKillCount.ToString();
            lastPlayerKill = target.PlayerKillCount;
        }
    }
    private void UpdateUI()
    {
        attack.Observe(target.Stats.Attack);
        defense.Observe(target.Stats.Defense);
        strength.Observe(target.Stats.Strength);

        lblCombatLevel.text = "Combat Level: <b>" + target.Stats.CombatLevel + "</b>";
        lblName.text = target.Name;

        var eqStats = target.EquipmentStats;
        armor.Text = (eqStats.ArmorPowerBonus > 0 ? "<color=green>" : "") + eqStats.BaseArmorPower.ToString();
        melee.Text = (eqStats.WeaponBonus > 0 ? "<color=green>" : "") + eqStats.BaseWeaponAim + "\n" + eqStats.BaseWeaponPower;
    }
}