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


    [Tooltip("Optional UI Toolkit target frame. When assigned, it shows the enemy and this uGUI dialog stays hidden. Clear it to bring the old dialog back.")]
    [SerializeField] private Shinobytes.UI.EnemyDetailsWindow uiToolkitWindow;

    private EnemyController target;
    private int lastDeathCount;
    private int lastPlayerKill;
    public bool IsVisible { get; private set; }
    private bool closeHooked;

    public void Awake()
    {
        Close();
    }

    public void ShowDialog(EnemyController ec)
    {
        this.target = ec;

        if (uiToolkitWindow)
        {
            // Hooked here rather than in Awake: with the window in use this object is never
            // activated, so Awake may never run.
            if (!closeHooked)
            {
                uiToolkitWindow.CloseRequested += Close;
                closeHooked = true;
            }

            uiToolkitWindow.Show(ec);
            IsVisible = true;
            return;
        }

#if UNITY_EDITOR
        UnityEngine.Debug.Log("Show Enemy Observe Dialog: " + target.Name);
#endif

        this.UpdateUI();
        this.gameObject.SetActive(true);
        IsVisible = true;
    }

    public void Close()
    {
        if (uiToolkitWindow) uiToolkitWindow.Hide();
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
        lblName.text = target.Name.Replace("_", "");

        var eqStats = target.EquipmentStats;
        armor.Text = (eqStats.ArmorPowerBonus > 0 ? "<color=green>" : "") + eqStats.BaseArmorPower.ToString();
        melee.Text = (eqStats.WeaponBonus > 0 ? "<color=green>" : "") + eqStats.BaseWeaponAim + "\n" + eqStats.BaseWeaponPower;
    }
}