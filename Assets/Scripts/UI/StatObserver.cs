using TMPro;
using UnityEngine;

public class StatObserver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameProgressBar bar;

    private int? lastCurrentValue;
    private double? lastExperience;
    private SkillStat observedStat;

    // Start is called before the first frame update
    void Start()
    {
        if (!bar) bar = GetComponentInChildren<GameProgressBar>();
    }

    // Update is called once per frame
    void Update()
    {
        if (observedStat == null || !label)
        {
            return;
        }

        if (lastCurrentValue != null && lastCurrentValue == observedStat.CurrentValue &&
            lastExperience != null && lastExperience == observedStat.Experience)
        {
            return;
        }

        var value = observedStat.CurrentValue;
        var max = observedStat.Level;
        var nextLevel = observedStat.Level + 1;

        label.text = value == max ? $"{value}" : value > max ? $"<color=#00ff00>{value}" : $"<color=#ff0000>{value}";
        var thisLevelExp = observedStat.Experience;
        var nextLevelExp = GameMath.ExperienceForLevel(nextLevel);

        bar.Progress = thisLevelExp > 0 && nextLevelExp > 0 ? (float)(thisLevelExp / nextLevelExp) : 0;

        lastCurrentValue = observedStat.CurrentValue;
        lastExperience = observedStat.Experience;
    }

    public void Observe(SkillStat stat)
    {
        observedStat = stat;
    }
}
