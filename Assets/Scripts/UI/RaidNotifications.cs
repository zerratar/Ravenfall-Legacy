using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaidNotifications : MonoBehaviour
{
    [SerializeField] private GameObject raidBossAppeared;
    [SerializeField] private GameObject raidBossHud;
    [SerializeField] private GameProgressBar raidBossHealth;
    [SerializeField] private TextMeshProUGUI raidTimer;
    [SerializeField] private Image comeJoinImage;

    [SerializeField] private TextMeshProUGUI lbLRaidBossLevel;

    private AudioSource audioSource;

    public float volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private void Start()
    {
        if (!raidTimer) raidTimer = GetComponentInChildren<TextMeshProUGUI>();
        if (!comeJoinImage) comeJoinImage = raidBossHud.transform.Find("Image").GetComponent<Image>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        audioSource.volume = PlayerPrefs.GetFloat("RaidHornVolume", 1f);

        HideRaidInfo();
    }

    public void HideRaidInfo()
    {
        if (raidBossAppeared) raidBossAppeared.SetActive(false);
        if (raidBossHealth) raidBossHud.SetActive(false);
    }

    public void HideRaidJoinInfo()
    {
        comeJoinImage.enabled = false;
        raidTimer.rectTransform.localPosition = new Vector3(0, 15f);
    }

    private void ShowRaidJoinInfo()
    {
        comeJoinImage.enabled = true;
        raidTimer.rectTransform.localPosition = new Vector3(0, 120);
    }

    public void ShowRaidBossAppeared()
    {
        if (!raidBossAppeared)
        {
            Debug.LogError("No Raid Boss Message set on Raid Notifications");
            return;
        }

        if (audioSource) audioSource.Play();

        raidBossAppeared.SetActive(true);
        raidBossHud.SetActive(true);


        ShowRaidJoinInfo();

        var zoomAnimation = raidBossAppeared.GetComponent<ZoomAnimationUI>();
        if (zoomAnimation)
        {
            zoomAnimation.Reset();
        }

        var autoHide = raidBossAppeared.GetComponent<AutoHideUI>();
        if (autoHide)
        {
            autoHide.Reset();
        }
    }

    public void SetHealthBarValue(float proc)
    {
        raidBossHealth.progress = proc;
    }

    public void UpdateRaidTimer(float timeoutTimer)
    {
        if (!raidTimer)
        {
            return;
        }

        raidTimer.text = $"{Mathf.FloorToInt(timeoutTimer)} seconds left";
    }

    public void SetRaidBossLevel(int combatLevel)
    {
        if (lbLRaidBossLevel) lbLRaidBossLevel.text = $"Lv: <b>{combatLevel}";
    }
}
