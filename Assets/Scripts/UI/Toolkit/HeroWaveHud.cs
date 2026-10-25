using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A wave run on screen (WaveRun, docs/hero-mode-design.md 7b), part of hero mode's HUD: at the top the wave and
    /// how many are left (or the count to the next wave) and a boss's health; in the middle, while the hero is down,
    /// the count and how chat gets the hero back up; after the run, how far it got, for a while.
    /// </summary>
    public sealed class HeroWaveHud
    {
        private const float ResultSeconds = 10f;

        private readonly VisualElement top;
        private readonly Label wave;
        private readonly Label sub;
        private readonly VisualElement boss;
        private readonly Label bossName;
        private readonly VisualElement bossFill;
        private readonly VisualElement downed;
        private readonly Label downedCount;
        private readonly Label downedHelp;
        private readonly VisualElement result;
        private readonly Label resultTitle;
        private readonly Label resultLine;
        private readonly Label resultBest;

        public HeroWaveHud(VisualElement root)
        {
            top = Box("rf-wave");
            wave = Text(top, "rf-wave__title");
            sub = Text(top, "rf-wave__sub");
            boss = Box("rf-wave-boss");
            bossName = Text(boss, "rf-wave-boss__name");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("rf-wave-boss__bar");
            bossFill = new VisualElement { pickingMode = PickingMode.Ignore };
            bossFill.AddToClassList("rf-wave-boss__fill");
            track.Add(bossFill);
            boss.Add(track);
            top.Add(boss);

            downed = Box("rf-wave-down");
            Text(downed, "rf-wave-down__title").text = Localization.UI_WAVE_DOWN;
            downedCount = Text(downed, "rf-wave-down__count");
            downedHelp = Text(downed, "rf-wave-down__help");

            result = Box("rf-wave-result");
            resultTitle = Text(result, "rf-wave-result__title");
            resultLine = Text(result, "rf-wave-result__line");
            resultBest = Text(result, "rf-wave-result__best");

            root.Add(top);
            root.Add(downed);
            root.Add(result);
            Hide();
        }

        private static VisualElement Box(string className)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);
            return box;
        }

        private static Label Text(VisualElement parent, string className)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        public void Hide()
        {
            top.style.display = DisplayStyle.None;
            downed.style.display = DisplayStyle.None;
            result.style.display = DisplayStyle.None;
        }

        public void Update()
        {
            var run = WaveRun.Current;
            var last = WaveRun.Last;
            ShowResult(!run && last != null && Time.time - last.EndedAt < ResultSeconds && !last.Abandoned ? last : null);
            if (!run)
            {
                top.style.display = DisplayStyle.None;
                downed.style.display = DisplayStyle.None;
                return;
            }

            top.style.display = DisplayStyle.Flex;
            var left = Mathf.Max(0, Mathf.CeilToInt(run.PhaseEndsAt - Time.time));
            switch (run.State)
            {
                case WaveRun.Phase.Gathering:
                    wave.text = Localization.UI_WAVE_GET_READY;
                    sub.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_FIRST_IN, left);
                    break;
                case WaveRun.Phase.Breather:
                    wave.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_HELD, run.Cleared);
                    sub.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_NEXT_IN, run.Wave + 1, left);
                    break;
                default:
                    wave.text = run.Goal > 0
                        ? string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_OF, run.Wave, run.Goal)
                        : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_N, run.Wave);
                    sub.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_LEFT, run.Left, run.Kills);
                    break;
            }

            var b = run.Boss;
            boss.style.display = b && !b.Stats.IsDead ? DisplayStyle.Flex : DisplayStyle.None;
            if (b && !b.Stats.IsDead)
            {
                bossName.text = run.BossName;
                var h = b.Stats.Health;
                bossFill.style.width = new Length(h.MaxLevel > 0 ? Mathf.Clamp01(h.CurrentValue / (float)h.MaxLevel) * 100f : 0f, LengthUnit.Percent);
            }

            downed.style.display = run.Downed ? DisplayStyle.Flex : DisplayStyle.None;
            if (run.Downed)
            {
                downedCount.text = Mathf.Max(0, Mathf.CeilToInt(run.DownedUntil - Time.time)).ToString(CultureInfo.InvariantCulture);
                downedHelp.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_DOWN_HELP, run.Votes, run.VotesNeeded)
                    + (run.HealerProgress > 0f ? "\n" + string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_DOWN_HEALER, Mathf.RoundToInt(run.HealerProgress * 100f)) : string.Empty);
            }
        }

        private void ShowResult(WaveRun.Result run)
        {
            result.style.display = run != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (run == null) return;
            var reached = run.Goal > 0 && run.Cleared >= run.Goal;
            resultTitle.text = reached ? Localization.UI_WAVE_RESULT_HELD : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT, run.Cleared);
            var seconds = Mathf.FloorToInt(run.EndedAt - run.StartedAt);
            resultLine.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_LINE, run.Cleared, run.Kills, seconds / 60, seconds % 60);
            resultBest.text = run.NewBest ? Localization.UI_WAVE_RESULT_NEW_BEST : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_BEST, run.Best);
        }
    }
}
