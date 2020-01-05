using TMPro;
using UnityEngine;

public class StatObserver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameProgressBar bar;

    private int lastCurrentValue;
    private decimal lastExperience;
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

        if (lastCurrentValue == observedStat.CurrentValue &&
            lastExperience == observedStat.Experience)
        {
            return;
        }

        var value = observedStat.CurrentValue;
        var max = observedStat.Level;
        var nextLevel = observedStat.Level + 1;

        label.text = value == max ? $"{value}" : value > max ? $"<color=#00ff00>{value}" : $"<color=#ff0000>{value}";
        var thisLevelExp = GameMath.LevelToExperience(observedStat.Level);
        var nextLevelExp = GameMath.LevelToExperience(nextLevel);

        var now = observedStat.Experience - thisLevelExp;
        var next = nextLevelExp - thisLevelExp;
        bar.Progress = (float)(now / next);

        lastCurrentValue = observedStat.CurrentValue;
        lastExperience = observedStat.Experience;
    }

    public void Observe(SkillStat stat)
    {
        observedStat = stat;
    }
}
