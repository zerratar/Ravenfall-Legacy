using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A wave run on screen (WaveRun, docs/hero-mode-design.md), part of hero mode's HUD, redesigned 2026-11-02 after
    /// the user found the old bare text "really sad": one solid column at the top centre with the wave plate (a big
    /// wave badge, the main line, a bar that fills as the wave falls or drains to the next one, where they come from)
    /// and the wave track under it (the clock, pips for the waves round this one with the bosses marked, the next boss
    /// or the best), then as they apply the boss's panel, chat's boon vote, the leave warning and the downed panel; after
    /// the run a result card where the numbers count up. Banners mark the moments: a wave starts, a boss arrives, a wave
    /// is held, the boss falls, a new best, back up.
    /// </summary>
    public sealed class HeroWaveHud
    {
        private const float ResultSeconds = 15f;
        private const int Pips = 9;

        // ---- The column ----
        private readonly VisualElement column, plate, track;
        private readonly VisualElement badge, badgeFlash;
        private readonly Label badgeCap, badgeNum, badgeOf;
        private readonly Label titleIcon, title;
        private readonly Label countPre, countIcon, countNum, countPost;
        private readonly VisualElement barFill;
        private readonly Label fromIcon, fromText;
        private readonly VisualElement from;
        private readonly Label clock;
        private readonly (VisualElement Pip, Label Icon)[] pips = new (VisualElement, Label)[Pips + 3];
        private readonly VisualElement info;
        private readonly Label infoIcon, infoText;

        private readonly VisualElement boss;
        private readonly Label bossName, bossTagIcon, bossTagText, bossPct;
        private readonly VisualElement bossTag, bossGhost, bossFill, bossShine, tick50, tick25;

        private readonly VisualElement boons;
        private readonly Label boonsHint;
        private readonly (VisualElement Row, Label Icon, Label Name, Label Effect, VisualElement Fill, Label Votes)[] boonRows = new (VisualElement, Label, Label, Label, VisualElement, Label)[3];

        private readonly VisualElement leave, leaveDrain;
        private readonly Label leaveMain, leaveSub, leaveIcon;

        private readonly VisualElement down, downDrain, downPips, healer, healerFill;
        private readonly Label downNum, downVotes, healerPct;
        private readonly VisualElement downFrame;

        private readonly VisualElement result, resultHeader, resultBest, resultChips, resultRank, resultTitle, resultChest, resultTimer;
        private readonly Label resultIcon, resultHeading, resultBestMain, resultBestSub, resultRankText, resultTitleText, resultChestName, resultChestWhat;
        private readonly Label[] statNums = new Label[3];

        // ---- Banners ----
        private readonly VisualElement banner, bannerBand, bannerMain;
        private readonly Label bannerCap, bannerText, bannerSub, bannerIconL, bannerIconR;
        private readonly Queue<Moment> moments = new Queue<Moment>();
        private Moment showing;
        private float shownAt, nextMomentAt;

        // ---- What the HUD keeps track of itself ----
        private WaveRun lastRun;
        private int seenWave = -1, killsAtStart, waveTotal, seenKills;
        private WaveRun.Phase seenState = WaveRun.Phase.Over;
        private float phaseTotal = 1f, downTotal = 1f, leaveAt = -1f, resultAt = -1f;
        private bool wasDowned, bossSeen, bossDownThisWave, newBestShown, bossBannerShown;
        private int seenPhase;
        private string bossNameThisWave;
        private float shownProgress, ghostShare = 1f, ghostWaitUntil, lastBossShare = 1f;
        private int lastCountdown = -1;
        private readonly int[] lastVotes = new int[3];
        private WaveRun.Result shownResult;
        private readonly List<Tween> tweens = new List<Tween>();

        private sealed class Moment
        {
            public string Style;   // "", boss, held, gold, up
            public string Cap, Text, Sub, Icon;
            public float Hold;
        }

        private struct Tween
        {
            public float Start, Duration;
            public Action<float> Apply;
        }

        private static readonly Color BossBorder = new Color32(120, 30, 26, 255);
        private static readonly Color BossHot = new Color32(255, 90, 60, 255);

        public HeroWaveHud(VisualElement root)
        {
            column = Box(root, "rf-wave");

            // the plate: badge, then the title row, the bar and where they come from
            plate = Box(column, "rf-wave__plate");
            badge = Box(plate, "rf-wave__badge");
            badgeFlash = Box(badge, "rf-wave__badge-flash");
            badgeCap = Text(badge, "rf-wave__badge-cap");
            badgeNum = Text(badge, "rf-wave__badge-num");
            badgeOf = Text(badge, "rf-wave__badge-of");
            var body = Box(plate, "rf-wave__body");
            var head = Box(body, "rf-wave__head");
            var titleRow = Box(head, "rf-wave__title-row");
            titleIcon = Icon(titleRow, "rf-wave__title-icon", "");
            title = Text(titleRow, "rf-wave__title");
            var count = Box(head, "rf-wave__count");
            countPre = Text(count, "rf-wave__count-cap");
            countIcon = Icon(count, "rf-wave__count-icon", "");
            countNum = Text(count, "rf-wave__count-num");
            countPost = Text(count, "rf-wave__count-cap");
            var bar = Box(body, "rf-wave__bar");
            barFill = Box(bar, "rf-wave__fill");
            from = Box(body, "rf-wave__from");
            fromIcon = Icon(from, "rf-wave__from-icon", "");
            fromText = Text(from, "rf-wave__from-text");

            // the track: the clock, the pips, the next boss or the best
            track = Box(column, "rf-wave-track");
            var clockBox = Box(track, "rf-wave-track__clock");
            Icon(clockBox, "rf-wave-track__clock-icon", "");
            clock = Text(clockBox, "rf-wave-track__clock-text");
            var pipRow = Box(track, "rf-wave-track__pips");
            for (var i = 0; i < pips.Length; i++)
            {
                var pip = Box(pipRow, "rf-wave-pip");
                pips[i] = (pip, Icon(pip, "rf-wave-pip__icon", ""));
            }
            info = Box(track, "rf-wave-track__info");
            infoIcon = Icon(info, "rf-wave-track__info-icon", "");
            infoText = Text(info, "rf-wave-track__info-text");

            // the boss
            boss = Box(column, "rf-wave-boss");
            var bossHead = Box(boss, "rf-wave-boss__head");
            Icon(bossHead, "rf-wave-boss__icon", "");
            bossName = Text(bossHead, "rf-wave-boss__name");
            bossTag = Box(bossHead, "rf-wave-boss__tag");
            bossTagIcon = Icon(bossTag, "rf-wave-boss__tag-icon", "");
            bossTagText = Text(bossTag, "rf-wave-boss__tag-text");
            bossPct = Text(bossHead, "rf-wave-boss__pct");
            var bossBar = Box(boss, "rf-wave-boss__bar");
            bossGhost = Box(bossBar, "rf-wave-boss__ghost");
            bossFill = Box(bossBar, "rf-wave-boss__fill");
            bossShine = Box(bossBar, "rf-wave-boss__shine");
            tick50 = Box(bossBar, "rf-wave-boss__tick");
            tick50.style.left = new Length(50f, LengthUnit.Percent);
            tick25 = Box(bossBar, "rf-wave-boss__tick");
            tick25.style.left = new Length(25f, LengthUnit.Percent);

            // chat's boon vote
            boons = Box(column, "rf-wave-boons");
            var boonsHead = Box(boons, "rf-wave-boons__head");
            Text(boonsHead, "rf-wave-boons__title").text = Localization.UI_WAVE_BOON_VOTE;
            boonsHint = Text(boonsHead, "rf-wave-boons__hint");
            for (var i = 0; i < boonRows.Length; i++)
            {
                var row = Box(boons, "rf-wave-boons__row");
                Text(row, "rf-wave-boons__cmd").text = "!hero " + (i + 1);
                var icon = Icon(row, "rf-wave-boons__icon", "");
                var name = Text(row, "rf-wave-boons__name");
                var effect = Text(row, "rf-wave-boons__effect");
                var vb = Box(row, "rf-wave-boons__bar");
                var fill = Box(vb, "rf-wave-boons__fill");
                var votes = Text(row, "rf-wave-boons__votes");
                boonRows[i] = (row, icon, name, effect, fill, votes);
            }

            // the leave warning (Esc once)
            leave = Box(column, "rf-wave-leave");
            Text(leave, "rf-wave-leave__key").text = "Esc";
            var leaveTexts = Box(leave, "rf-wave-leave__texts");
            leaveMain = Text(leaveTexts, "rf-wave-leave__main");
            leaveSub = Text(leaveTexts, "rf-wave-leave__sub");
            leaveIcon = Icon(leave, "rf-wave-leave__icon", "");
            leaveDrain = Box(leave, "rf-wave-leave__drain");

            // downed
            down = Box(column, "rf-wave-down");
            var downHead = Box(down, "rf-wave-down__head");
            Icon(downHead, "rf-wave-down__icon", "");
            Text(downHead, "rf-wave-down__title").text = Localization.UI_WAVE_DOWN;
            var downCount = Box(downHead, "rf-wave-down__count");
            downNum = Text(downCount, "rf-wave-down__num");
            Text(downCount, "rf-wave-down__cap").text = Localization.UI_WAVE_DOWN_SECONDS;
            var drain = Box(down, "rf-wave-down__drain");
            downDrain = Box(drain, "rf-wave-down__drain-fill");
            var revive = Box(down, "rf-wave-down__revive");
            Text(revive, "rf-wave-down__label").text = Localization.UI_WAVE_DOWN_HELP;
            Text(revive, "rf-wave-down__cmd").text = Localization.UI_WAVE_DOWN_CMD;
            Box(revive, "rf-wave__spacer");
            downPips = Box(revive, "rf-wave-down__pips");
            downVotes = Text(revive, "rf-wave-down__votes");
            healer = Box(down, "rf-wave-down__healer");
            Icon(healer, "rf-wave-down__healer-icon", "");
            Text(healer, "rf-wave-down__healer-text").text = Localization.UI_WAVE_DOWN_HEALER;
            var hb = Box(healer, "rf-wave-down__healer-bar");
            healerFill = Box(hb, "rf-wave-down__healer-fill");
            healerPct = Text(healer, "rf-wave-down__healer-pct");

            // the result card
            result = Box(column, "rf-wave-result");
            resultHeader = Box(result, "rf-wave-result__header");
            resultIcon = Icon(resultHeader, "rf-wave-result__icon", "");
            resultHeading = Text(resultHeader, "rf-wave-result__title");
            var resultBody = Box(result, "rf-wave-result__body");
            var stats = Box(resultBody, "rf-wave-result__stats");
            var caps = new[] { Localization.UI_WAVE_STAT_WAVES, Localization.UI_WAVE_STAT_FALLEN, Localization.UI_WAVE_STAT_TIME };
            for (var i = 0; i < 3; i++)
            {
                var stat = Box(stats, "rf-wave-result__stat");
                statNums[i] = Text(stat, "rf-wave-result__stat-num");
                Text(stat, "rf-wave-result__stat-cap").text = caps[i];
            }
            resultBest = Box(resultBody, "rf-wave-result__best");
            Icon(resultBest, "rf-wave-result__best-icon", "");
            resultBestMain = Text(resultBest, "rf-wave-result__best-main");
            resultBestSub = Text(resultBest, "rf-wave-result__best-sub");
            Icon(resultBest, "rf-wave-result__best-icon", "");
            resultChips = Box(resultBody, "rf-wave-result__chips");
            resultRank = Box(resultChips, "rf-wave-result__chip");
            Icon(resultRank, "rf-wave-result__chip-icon", "");
            resultRankText = Text(resultRank, "rf-wave-result__chip-text");
            resultTitle = Box(resultChips, "rf-wave-result__chip");
            Icon(resultTitle, "rf-wave-result__chip-icon", "");
            resultTitleText = Text(resultTitle, "rf-wave-result__chip-text");
            resultChest = Box(resultBody, "rf-wave-result__chest");
            Icon(resultChest, "rf-wave-result__chest-icon", "");
            resultChestName = Text(resultChest, "rf-wave-result__chest-name");
            resultChestWhat = Text(resultChest, "rf-wave-result__chest-what");
            resultTimer = Box(result, "rf-wave-result__timer");

            // the banner and the downed frame, over everything else
            banner = Box(root, "rf-wave-banner");
            bannerBand = Box(banner, "rf-wave-banner__band");
            bannerCap = Text(bannerBand, "rf-wave-banner__cap");
            bannerMain = Box(bannerBand, "rf-wave-banner__main");
            bannerIconL = Icon(bannerMain, "rf-wave-banner__icon", "");
            bannerText = Text(bannerMain, "rf-wave-banner__text");
            bannerIconR = Icon(bannerMain, "rf-wave-banner__icon", "");
            bannerSub = Text(bannerBand, "rf-wave-banner__sub");
            downFrame = Box(root, "rf-wave-down-frame");

            Hide();
        }

        // ---- Building ----

        private static VisualElement Box(VisualElement parent, string className)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);
            parent.Add(box);
            return box;
        }

        private static Label Text(VisualElement parent, string className)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        private static Label Icon(VisualElement parent, string className, string glyph)
        {
            var label = Text(parent, className);
            label.AddToClassList("rf-icon");
            label.text = glyph;
            return label;
        }

        private static void Show(VisualElement e, bool on)
        {
            var d = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (e.style.display != d) e.style.display = d;
        }

        private static void Width(VisualElement e, float share) => e.style.width = new Length(Mathf.Clamp01(share) * 100f, LengthUnit.Percent);

        private static string Number(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60).ToString(CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public void Hide()
        {
            Show(column, false);
            Show(banner, false);
            Show(downFrame, false);
        }

        // ---- Every frame ----

        public void Update()
        {
            var now = Time.unscaledTime;
            RunTweens(now);
            UpdateBanner(now);

            var run = WaveRun.Current;
            if (run != lastRun) Reset(run);
            var last = WaveRun.Last;
            var result = !run && last != null && Time.time - last.EndedAt < ResultSeconds && !last.Abandoned ? last : null;
            Show(column, run || result != null);
            ShowResult(result, now);
            // the run's own parts only while it runs: after it, the result card alone
            var running = (bool)run;
            Show(plate, running);
            Show(track, running);
            if (!running)
            {
                Show(boss, false);
                Show(boons, false);
                Show(leave, false);
                Show(down, false);
                Show(downFrame, false);
                return;
            }

            Track(run, now);
            Plate(run, now);
            TrackPips(run);
            Boss(run, now);
            Boons(run, now);
            Leave(run, now);
            Downed(run, now);
        }

        private void Reset(WaveRun run)
        {
            lastRun = run;
            seenWave = -1;
            seenState = WaveRun.Phase.Over;
            wasDowned = false;
            newBestShown = false;
            bossSeen = bossDownThisWave = bossBannerShown = false;
            seenPhase = 0;
            moments.Clear();
            if (run) shownResult = null;
        }

        /// <summary>Notices what changed since the last frame: a new wave, a phase, kills, the boss, downed, a new best.</summary>
        private void Track(WaveRun run, float now)
        {
            if (run.State != seenState)
            {
                var was = seenState;
                seenState = run.State;
                phaseTotal = Mathf.Max(0.01f, run.PhaseEndsAt - Time.time);
                lastCountdown = -1;
                if (run.State == WaveRun.Phase.Breather && run.Cleared > 0 && was == WaveRun.Phase.Fighting)
                {
                    if (bossDownThisWave)
                        Queue("gold", Localization.UI_WAVE_BANNER_BOSS_DOWN_CAP, string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BANNER_BOSS_DOWN, bossNameThisWave), Localization.UI_WAVE_POTIONS, "", 1.8f);
                    else
                        Queue("held", Localization.UI_WAVE_BANNER_HELD_CAP, string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BANNER_HELD, run.Cleared), Localization.UI_WAVE_BANNER_HELD_SUB, "", 1.2f);
                    if (!newBestShown && run.Best > 0 && run.Cleared > run.Best)
                    {
                        newBestShown = true;
                        Queue("gold", string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BANNER_BEST_CAP, run.Cleared), Localization.UI_WAVE_BANNER_BEST, null, "", 1.4f);
                    }
                }
            }

            if (run.Wave != seenWave && run.State == WaveRun.Phase.Fighting)
            {
                seenWave = run.Wave;
                killsAtStart = run.Kills;
                waveTotal = Mathf.Max(1, run.Left);
                bossSeen = bossDownThisWave = bossBannerShown = false;
                seenPhase = 0;
                Flash(badgeFlash);
                Pop(badgeNum, 1.35f, 0.25f);
                var bossWave = run.Wave % 5 == 0;
                if (!bossWave)
                {
                    var final = run.Goal > 0 && run.Wave == run.Goal;
                    Queue("", final ? Localization.UI_WAVE_BANNER_CAP : Localization.UI_WAVE_BANNER_CAP,
                        final ? Localization.UI_WAVE_BANNER_FINAL : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BANNER, run.Wave),
                        run.ComingFrom, "", 1.2f);
                }
            }
            waveTotal = Mathf.Max(waveTotal, run.Kills - killsAtStart + run.Left);

            // the boss's banner waits for the boss itself (its name)
            var b = run.Boss;
            if (b && !bossBannerShown && run.State == WaveRun.Phase.Fighting)
            {
                bossBannerShown = true;
                bossSeen = true;
                bossNameThisWave = run.BossName;
                ghostShare = lastBossShare = 1f;
                Queue("boss", string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BANNER_BOSS_CAP, run.Wave), run.BossName, null, "", 2f);
                var game = GameManager.Instance;
                if (game && game.Camera && game.Camera.HeroView) game.Camera.HeroView.Shake(0.3f, 0.4f);
            }
            if (bossSeen && !b && run.State == WaveRun.Phase.Fighting) bossDownThisWave = true;

            if (run.Kills > seenKills) Pop(countNum, 1.2f, 0.12f);
            seenKills = run.Kills;

            if (run.Downed && !wasDowned)
            {
                downTotal = Mathf.Max(0.01f, run.DownedUntil - Time.time);
                Pop(down, 1.15f, 0.2f);
            }
            if (!run.Downed && wasDowned && run.State != WaveRun.Phase.Over)
                Queue("up", null, Localization.UI_WAVE_BANNER_UP, null, "", 1f);
            wasDowned = run.Downed;

            if (run.LeaveAsked && leaveAt < 0f)
            {
                leaveAt = now;
                Slide(leave);
                if (!run.CanRetreat) Shake(leave);
            }
            if (!run.LeaveAsked) leaveAt = -1f;
        }

        private void Plate(WaveRun run, float now)
        {
            var ready = run.State == WaveRun.Phase.Gathering || (run.State == WaveRun.Phase.Breather && run.Cleared <= 0);
            var held = run.State == WaveRun.Phase.Breather && run.Cleared > 0;
            var bossWave = !ready && !held && run.Wave % 5 == 0;
            badge.EnableInClassList("rf-wave__badge--ready", ready);
            badge.EnableInClassList("rf-wave__badge--held", held);
            badge.EnableInClassList("rf-wave__badge--boss", bossWave);
            badgeCap.text = held ? Localization.UI_WAVE_CAP_HELD : bossWave ? Localization.UI_WAVE_CAP_BOSS : Localization.UI_WAVE_CAP;
            badgeNum.text = (ready ? Mathf.Max(1, run.Wave + 1) : held ? run.Cleared : run.Wave).ToString(CultureInfo.InvariantCulture);
            Show(badgeOf, run.Goal > 0);
            if (run.Goal > 0) badgeOf.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_OF_SHORT, run.Goal);

            var left = run.PhaseEndsAt - Time.time;
            var countdown = Mathf.Max(0, Mathf.CeilToInt(left));
            title.EnableInClassList("rf-wave__title--last", false);
            title.EnableInClassList("rf-wave__title--held", held);
            Show(titleIcon, held);
            if (ready || held)
            {
                title.text = ready ? Localization.UI_WAVE_GET_READY : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_HELD, run.Cleared);
                Show(countPre, true);
                Show(countIcon, false);
                Show(countPost, false);
                countPre.text = ready ? Localization.UI_WAVE_FIRST_IN : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_NEXT_IN, run.Wave + 1);
                countNum.text = countdown.ToString(CultureInfo.InvariantCulture);
                var late = left <= 3f;
                countNum.EnableInClassList("rf-wave__count-num--hot", late);
                if (late && countdown != lastCountdown && countdown > 0) Pop(countNum, 1.3f, 0.3f);
                lastCountdown = countdown;
                barFill.EnableInClassList("rf-wave__fill--time", !late);
                barFill.EnableInClassList("rf-wave__fill--late", late);
                Width(barFill, left / phaseTotal);
                shownProgress = 0f;
            }
            else
            {
                var last = run.Left == 1;
                title.text = last ? Localization.UI_WAVE_LAST_ONE : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_LEFT, run.Left);
                title.EnableInClassList("rf-wave__title--last", last);
                Show(countPre, false);
                Show(countIcon, true);
                Show(countPost, true);
                countNum.text = Number(run.Kills);
                countNum.EnableInClassList("rf-wave__count-num--hot", false);
                countPost.text = Localization.UI_WAVE_FALLEN;
                barFill.EnableInClassList("rf-wave__fill--time", false);
                barFill.EnableInClassList("rf-wave__fill--late", false);
                var target = waveTotal > 0 ? (run.Kills - killsAtStart) / (float)waveTotal : 0f;
                shownProgress = Mathf.MoveTowards(shownProgress, target, Time.unscaledDeltaTime * 2.5f);
                Width(barFill, shownProgress);
            }

            // where the next ones come from, or the boss's warning a breather before it
            var bossNext = held && (run.Wave + 1) % 5 == 0;
            from.EnableInClassList("rf-wave__from--boss", bossNext);
            fromIcon.text = bossNext ? "" : "";
            fromText.text = bossNext ? Localization.UI_WAVE_BOSS_NEXT : run.ComingFrom;
            clock.text = Clock(Time.time - run.StartedAt);
        }

        /// <summary>The track: pips for the waves round this one (all of them for a short goal), the bosses marked.</summary>
        private void TrackPips(WaveRun run)
        {
            var current = run.State == WaveRun.Phase.Fighting ? run.Wave : run.Wave + 1;
            var all = run.Goal > 0 && run.Goal <= 12;
            var first = all ? 1 : Mathf.Max(1, current - 3);
            var count = all ? run.Goal : Pips;
            for (var i = 0; i < pips.Length; i++)
            {
                var (pip, icon) = pips[i];
                var on = i < count;
                Show(pip, on);
                if (!on) continue;
                var n = first + i;
                var boss = n % 5 == 0;
                var goal = all && n == run.Goal;
                pip.EnableInClassList("rf-wave-pip--done", n < current);
                pip.EnableInClassList("rf-wave-pip--now", n == current);
                pip.EnableInClassList("rf-wave-pip--boss", boss || goal);
                pip.EnableInClassList("rf-wave-pip--best", run.Best > 0 && n == run.Best);
                Show(icon, boss || goal);
                icon.text = goal ? "" : "";
            }

            // the next boss when it is close, else the best (a new best once it is passed)
            var toBoss = (5 - current % 5) % 5;
            var newBest = run.Best > 0 && run.Cleared > run.Best;
            info.EnableInClassList("rf-wave-track__info--newbest", newBest);
            info.EnableInClassList("rf-wave-track__info--boss", !newBest && toBoss > 0 && toBoss <= 3);
            if (newBest)
            {
                Show(infoIcon, true);
                infoIcon.text = "";
                infoText.text = Localization.UI_WAVE_TRACK_NEW_BEST;
            }
            else if (toBoss > 0 && toBoss <= 3)
            {
                Show(infoIcon, true);
                infoIcon.text = "";
                infoText.text = toBoss == 1 ? Localization.UI_WAVE_TRACK_BOSS_NEXT : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_TRACK_BOSS_IN, toBoss);
            }
            else
            {
                Show(infoIcon, run.Best > 0);
                infoIcon.text = "";
                infoText.text = run.Best > 0 ? string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_TRACK_BEST, run.Best) : string.Empty;
            }
        }

        private void Boss(WaveRun run, float now)
        {
            var b = run.Boss;
            var on = b && !b.Stats.IsDead;
            Show(boss, on);
            if (!on) return;
            bossName.text = run.BossName;
            var h = b.Stats.Health;
            var share = h.MaxLevel > 0 ? Mathf.Clamp01(h.CurrentValue / (float)h.MaxLevel) : 0f;
            bossPct.text = Mathf.CeilToInt(share * 100f).ToString(CultureInfo.InvariantCulture) + "%";
            Width(bossFill, share);
            Width(bossShine, share);
            // the ghost of what the last blows took waits a moment, then catches up
            if (share < lastBossShare - 0.0001f) ghostWaitUntil = now + 0.4f;
            lastBossShare = share;
            if (now >= ghostWaitUntil) ghostShare = Mathf.MoveTowards(ghostShare, share, Time.unscaledDeltaTime * 1.5f);
            ghostShare = Mathf.Max(ghostShare, share);
            Width(bossGhost, ghostShare);
            tick50.EnableInClassList("rf-wave-boss__tick--passed", share <= 0.5f);
            tick25.EnableInClassList("rf-wave-boss__tick--passed", share <= 0.25f);

            var phase = run.BossPhase;
            Show(bossTag, phase > 0);
            bossTag.EnableInClassList("rf-wave-boss__tag--adds", phase == 1);
            bossTag.EnableInClassList("rf-wave-boss__tag--enraged", phase == 2);
            bossTagIcon.text = phase == 2 ? "" : "";
            bossTagText.text = phase == 2 ? Localization.UI_WAVE_BOSS_ENRAGED_TAG : Localization.UI_WAVE_BOSS_ADDS_TAG;
            if (phase != seenPhase) { seenPhase = phase; if (phase > 0) Pop(bossTag, 1.6f, 0.25f); }
            bossFill.EnableInClassList("rf-wave-boss__fill--enraged", phase == 2);
            // enraged: the border pulses
            var c = phase == 2 ? Color.Lerp(BossBorder, BossHot, 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f / 0.8f)) : BossBorder;
            boss.style.borderTopColor = boss.style.borderBottomColor = boss.style.borderLeftColor = boss.style.borderRightColor = c;
        }

        private void Boons(WaveRun run, float now)
        {
            var offer = run.State == WaveRun.Phase.Breather ? run.Offer : null;
            // after the held banner, not under it
            var on = offer != null && (showing == null || showing.Style != "held");
            Show(boons, on);
            if (!on) return;
            var total = 0;
            var most = 0;
            for (var i = 0; i < offer.Length; i++) { var v = run.BoonVotes(i); total += v; most = Mathf.Max(most, v); }
            boonsHint.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_BOON_VOTES, total);
            for (var i = 0; i < boonRows.Length; i++)
            {
                var (row, icon, name, effect, fill, votes) = boonRows[i];
                var shown = i < offer.Length;
                Show(row, shown);
                if (!shown) continue;
                var count = run.BoonVotes(i);
                var text = offer[i].Name();
                var cut = text.IndexOf(" (", StringComparison.Ordinal);
                name.text = cut > 0 ? text.Substring(0, cut) : text;
                effect.text = cut > 0 ? text.Substring(cut + 2).TrimEnd(')') : string.Empty;
                icon.text = BoonGlyph(offer[i].Key);
                votes.text = count.ToString(CultureInfo.InvariantCulture);
                if (count > lastVotes[i]) Pop(votes, 1.35f, 0.2f);
                lastVotes[i] = count;
                Width(fill, total > 0 ? count / (float)total : 0f);
                row.EnableInClassList("rf-wave-boons__row--lead", most > 0 && count == most);
            }
        }

        private static string BoonGlyph(string key)
        {
            switch (key)
            {
                case "steel": return "";
                case "wells": return "";
                case "hide": return "";
                case "wind": return "";
                case "glass": return "";
                case "swift": return "";
                case "blood": return "";
                default: return "";
            }
        }

        private void Leave(WaveRun run, float now)
        {
            Show(leave, run.LeaveAsked);
            if (!run.LeaveAsked) return;
            var retreat = run.CanRetreat;
            leave.EnableInClassList("rf-wave-leave--abandon", !retreat);
            leaveMain.text = retreat ? Localization.UI_WAVE_RETREAT_ASK : Localization.UI_WAVE_LEAVE_ASK;
            leaveSub.text = retreat ? Localization.UI_WAVE_RETREAT_SUB : Localization.UI_WAVE_LEAVE_SUB;
            leaveIcon.text = retreat ? "" : "";
            Width(leaveDrain, 1f - (now - leaveAt) / WaveRun.LeaveAskSeconds);
        }

        private void Downed(WaveRun run, float now)
        {
            Show(down, run.Downed);
            Show(downFrame, run.Downed);
            if (!run.Downed) return;
            var left = run.DownedUntil - Time.time;
            var count = Mathf.Max(0, Mathf.CeilToInt(left));
            if (left <= 5f && count != lastCountdown && count > 0) Pop(downNum, 1.3f, 0.3f);
            lastCountdown = count;
            downNum.text = count.ToString(CultureInfo.InvariantCulture);
            Width(downDrain, left / downTotal);
            // one pip per vote needed
            while (downPips.childCount < run.VotesNeeded) Box(downPips, "rf-wave-down__pip");
            for (var i = 0; i < downPips.childCount; i++)
            {
                var pip = downPips[i];
                Show(pip, i < run.VotesNeeded);
                var lit = i < run.Votes;
                if (lit && !pip.ClassListContains("rf-wave-down__pip--on")) Pop(pip, 1.4f, 0.2f);
                pip.EnableInClassList("rf-wave-down__pip--on", lit);
            }
            downVotes.text = run.Votes.ToString(CultureInfo.InvariantCulture) + " / " + run.VotesNeeded.ToString(CultureInfo.InvariantCulture);
            Show(healer, run.HealerProgress > 0f);
            Width(healerFill, run.HealerProgress);
            healerPct.text = Mathf.RoundToInt(Mathf.Clamp01(run.HealerProgress) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
            // the screen's red frame pulses
            downFrame.style.opacity = 0.25f + 0.45f * (0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f));
        }

        // ---- The result ----

        private void ShowResult(WaveRun.Result run, float now)
        {
            Show(result, run != null);
            if (run == null) { shownResult = null; return; }
            if (run != shownResult)
            {
                shownResult = run;
                resultAt = now;
                Pop(resultBest, 1.15f, 0.25f, 0.9f);
            }
            var reached = run.Goal > 0 && run.Cleared >= run.Goal;
            resultHeader.EnableInClassList("rf-wave-result__header--held", reached);
            resultHeader.EnableInClassList("rf-wave-result__header--retreat", !reached && run.Retreated);
            resultHeader.EnableInClassList("rf-wave-result__header--broke", !reached && !run.Retreated);
            resultIcon.text = reached ? "" : run.Retreated ? "" : "";
            resultHeading.text = reached ? Localization.UI_WAVE_RESULT_HELD
                : run.Retreated ? string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_RETREAT, run.Cleared)
                : run.Cleared <= 0 ? Localization.UI_WAVE_RESULT_FIRST
                : run.Cleared == 1 ? Localization.UI_WAVE_RESULT_ONE
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT, run.Cleared);

            // the numbers count up, one after the other
            var age = now - resultAt;
            var seconds = run.EndedAt - run.StartedAt;
            statNums[0].text = Number(Mathf.RoundToInt(run.Cleared * Ease(age / 0.8f)));
            statNums[1].text = Number(Mathf.RoundToInt(run.Kills * Ease((age - 0.15f) / 0.8f)));
            statNums[2].text = Clock(seconds * Ease((age - 0.3f) / 0.8f));

            resultBest.EnableInClassList("rf-wave-result__best--plain", !run.NewBest);
            resultBestMain.text = run.NewBest ? Localization.UI_WAVE_BANNER_BEST
                : run.Best == 1 ? Localization.UI_WAVE_RESULT_BEST_ONE
                : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_BEST, run.Best);
            Show(resultBestSub, run.NewBest);
            resultBestSub.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_NEW_BEST_SUB, run.Cleared);
            foreach (var child in resultBest.Children()) if (child.ClassListContains("rf-wave-result__best-icon")) Show(child, run.NewBest);

            Show(resultRank, run.Rank > 0);
            resultRankText.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_RANK, run.Rank);
            var titles = run.NewTitles != null && run.NewTitles.Length > 0;
            Show(resultTitle, titles);
            if (titles) resultTitleText.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_TITLE, string.Join(", ", Array.ConvertAll(run.NewTitles, QuestChat.TitleText)));
            Show(resultChips, run.Rank > 0 || titles);

            // the chest: what it held, or how many waves it takes
            var chest = run.ChestItems > 0 || run.Coins > 0;
            resultChest.EnableInClassList("rf-wave-result__chest--none", !chest && run.Cleared < 5);
            Show(resultChest, chest || run.Cleared < 5);
            resultChestName.text = chest ? Localization.UI_WAVE_RESULT_CHEST_NAME : Localization.UI_WAVE_RESULT_NO_CHEST;
            resultChestWhat.text = run.ChestItems > 0
                ? (run.ChestItems == 1 ? Localization.UI_WAVE_RESULT_ITEM_ONE : string.Format(CultureInfo.InvariantCulture, Localization.UI_WAVE_RESULT_ITEMS, run.ChestItems))
                : run.Coins > 0 ? Number(run.Coins) + " " + Localization.UI_WAVE_RESULT_COINS_WORD : string.Empty;
            Width(resultTimer, 1f - (Time.time - run.EndedAt) / ResultSeconds);
        }

        // ---- Banners ----

        private void Queue(string style, string cap, string text, string sub, string icon, float hold)
        {
            moments.Enqueue(new Moment { Style = style, Cap = cap, Text = text, Sub = sub, Icon = icon, Hold = hold });
        }

        private const float BannerIn = 0.28f, BannerOut = 0.25f;

        private void UpdateBanner(float now)
        {
            if (showing == null)
            {
                if (moments.Count == 0 || now < nextMomentAt) { Show(banner, false); return; }
                showing = moments.Dequeue();
                shownAt = now;
                foreach (var kind in new[] { "boss", "held", "gold", "up" }) banner.EnableInClassList("rf-wave-banner--" + kind, showing.Style == kind);
                Show(bannerCap, !string.IsNullOrEmpty(showing.Cap));
                bannerCap.text = showing.Cap ?? string.Empty;
                bannerText.text = showing.Text ?? string.Empty;
                Show(bannerSub, !string.IsNullOrEmpty(showing.Sub));
                bannerSub.text = showing.Sub ?? string.Empty;
                bannerIconL.text = bannerIconR.text = showing.Icon ?? string.Empty;
                Show(banner, true);
            }

            var age = now - shownAt;
            var end = BannerIn + showing.Hold;
            // the band unrolls, the words land, hold, then lift away
            var band = Ease(age / 0.16f);
            bannerBand.style.scale = new Scale(new Vector3(Mathf.Max(0.001f, band), 1f, 1f));
            var land = Mathf.Clamp01((age - 0.06f) / 0.22f);
            var s = Mathf.LerpUnclamped(1.5f, 1f, EaseBack(land));
            bannerMain.style.scale = new Scale(new Vector3(s, s, 1f));
            var opacity = Mathf.Clamp01(land * 1.5f);
            var lift = 0f;
            if (age > end)
            {
                var t = Mathf.Clamp01((age - end) / BannerOut);
                opacity *= 1f - t;
                lift = -16f * t;
                if (t >= 1f)
                {
                    showing = null;
                    nextMomentAt = now + 0.3f;
                    Show(banner, false);
                    return;
                }
            }
            banner.style.opacity = opacity;
            banner.style.translate = new Translate(0f, lift);
        }

        // ---- Little animations ----

        private void RunTweens(float now)
        {
            for (var i = tweens.Count - 1; i >= 0; i--)
            {
                var t = tweens[i];
                var p = (now - t.Start) / t.Duration;
                if (p < 0f) continue;
                t.Apply(Mathf.Clamp01(p));
                if (p >= 1f) tweens.RemoveAt(i);
            }
        }

        /// <summary>A quick scale from big back to normal (a number that changed, a tag that appeared).</summary>
        private void Pop(VisualElement e, float from, float seconds, float delay = 0f)
        {
            tweens.Add(new Tween
            {
                Start = Time.unscaledTime + delay,
                Duration = seconds,
                Apply = p => { var s = Mathf.Lerp(from, 1f, Ease(p)); e.style.scale = new Scale(new Vector3(s, s, 1f)); },
            });
        }

        /// <summary>The badge flashes cream and back to its own colour (a new wave): a cream cover fading out.</summary>
        private void Flash(VisualElement cover)
        {
            tweens.Add(new Tween
            {
                Start = Time.unscaledTime,
                Duration = 0.3f,
                Apply = p => cover.style.opacity = 1f - Ease(p),
            });
        }

        private void Slide(VisualElement e)
        {
            tweens.Add(new Tween
            {
                Start = Time.unscaledTime,
                Duration = 0.15f,
                Apply = p => { e.style.translate = new Translate(0f, -12f * (1f - Ease(p))); e.style.opacity = p; },
            });
        }

        private void Shake(VisualElement e)
        {
            tweens.Add(new Tween
            {
                Start = Time.unscaledTime,
                Duration = 0.25f,
                Apply = p => e.style.translate = new Translate(p >= 1f ? 0f : 6f * Mathf.Sin(p * Mathf.PI * 6f), 0f),
            });
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        private static float EaseBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
