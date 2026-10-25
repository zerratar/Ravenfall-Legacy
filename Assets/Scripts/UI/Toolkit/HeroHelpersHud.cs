using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Chat's part on hero mode's HUD (HeroHelpers, docs/hero-mode-design.md 7c): on the right the top helpers, their
    /// damage, healing, revives and cheers; under the wave line a short feed of what chat just did (a cheer, a heal, a
    /// taunt, a revive), each line fading after a few seconds; and the cheers' damage bonus by the style.
    /// </summary>
    public sealed class HeroHelpersHud
    {
        private const int Shown = 5;
        private const float FeedSeconds = 6f;

        private readonly VisualElement panel;
        private readonly List<(VisualElement Row, Label Name, Label Line)> rows = new List<(VisualElement, Label, Label)>();
        private readonly VisualElement feed;
        private readonly List<Label> feedLines = new List<Label>();
        private readonly Label cheer;
        private float nextRefresh;

        public HeroHelpersHud(VisualElement root, VisualElement styleBox)
        {
            panel = new VisualElement { pickingMode = PickingMode.Ignore };
            panel.AddToClassList("rf-hero-helpers");
            var title = new Label(Localization.UI_HERO_HELPERS) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-hero-helpers__title");
            panel.Add(title);
            for (var i = 0; i < Shown; i++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("rf-hero-helpers__row");
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-hero-helpers__name");
                var line = new Label { pickingMode = PickingMode.Ignore };
                line.AddToClassList("rf-hero-helpers__line");
                row.Add(name);
                row.Add(line);
                panel.Add(row);
                rows.Add((row, name, line));
            }
            root.Add(panel);

            feed = new VisualElement { pickingMode = PickingMode.Ignore };
            feed.AddToClassList("rf-hero-feed");
            for (var i = 0; i < 5; i++)
            {
                var l = new Label { pickingMode = PickingMode.Ignore };
                l.AddToClassList("rf-hero-feed__line");
                feed.Add(l);
                feedLines.Add(l);
            }
            root.Add(feed);

            cheer = new Label { pickingMode = PickingMode.Ignore };
            cheer.AddToClassList("rf-hero-style__cheer");
            styleBox.Add(cheer);
        }

        public void Update(HeroController hero)
        {
            var stacks = hero.CheerStacks;
            cheer.style.display = stacks > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (stacks > 0) cheer.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_CHEER_BONUS, Mathf.RoundToInt((hero.CheerBonus - 1f) * 100f));

            // the feed: newest at the bottom, fading out
            var lines = HeroHelpers.Feed;
            var shown = 0;
            for (var i = 0; i < lines.Count && shown < feedLines.Count; i++)
            {
                var age = Time.time - lines[i].At;
                if (age > FeedSeconds) continue;
                var label = feedLines[shown++];
                label.text = lines[i].Text;
                label.style.opacity = Mathf.Clamp01((FeedSeconds - age) / 1.5f);
                label.style.display = DisplayStyle.Flex;
            }
            for (var i = shown; i < feedLines.Count; i++) feedLines[i].style.display = DisplayStyle.None;

            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + 0.5f;
            var r = 0;
            foreach (var h in HeroHelpers.Top(Shown))
            {
                var (row, name, line) = rows[r++];
                row.style.display = DisplayStyle.Flex;
                name.text = h.Name;
                line.text = Describe(h);
            }
            for (var i = r; i < rows.Count; i++) rows[i].Row.style.display = DisplayStyle.None;
            panel.style.display = r > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string Describe(HeroHelpers.Helper h)
        {
            var parts = new List<string>();
            if (h.Damage > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_HELPER_DAMAGE, HudFormat.Compact(h.Damage)));
            if (h.Healing > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_HELPER_HEALING, HudFormat.Compact(h.Healing)));
            if (h.Revives > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_HELPER_REVIVES, h.Revives));
            if (h.Cheers > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_HELPER_CHEERS, h.Cheers));
            if (h.Taunts > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_HELPER_TAUNTS, h.Taunts));
            return string.Join(", ", parts);
        }
    }
}
