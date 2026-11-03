using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The regatta's results (the user's mockup, 2026-11-03), in the middle of the screen when a race is over: a podium
    /// with each boat's picture, the rest of the order, cards for the standouts (most hits taken, most pickups, the
    /// biggest comeback, who woke the Kraken), the viewers who meddled and the prize pot; confetti falling over it. It
    /// stays <see cref="ShowSeconds"/> seconds, or until clicked. MangroveView puts it on the HUD's layer.
    /// </summary>
    public sealed class RegattaResultsPanel
    {
        private const float ShowSeconds = 15f;
        private const float ConfettiSeconds = 5f;
        private const int ConfettiPieces = 48;

        private const string Trophy = "", Crown = "", Burst = "", Bottle = "", Rising = "",
            Octopus = "", Skull = "", Coins = "";

        private static readonly Color[] ConfettiColours =
        {
            new Color32(236, 190, 52, 255), new Color32(230, 92, 70, 255), new Color32(80, 190, 230, 255),
            new Color32(120, 210, 110, 255), new Color32(230, 120, 200, 255), new Color32(245, 236, 214, 255),
        };

        private readonly VisualElement root;
        private readonly VisualElement panel;
        private readonly VisualElement confettiLayer;
        private readonly List<(VisualElement Piece, Vector2 At, Vector2 Velocity, float Spin, float Turn)> confetti = new List<(VisualElement, Vector2, Vector2, float, float)>();
        private float hideAt = -1f, shownAt;

        public RegattaResultsPanel(VisualElement layer)
        {
            root = new VisualElement { name = "regatta-results", pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-mres-wrap");
            confettiLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            confettiLayer.AddToClassList("rf-mres-confetti");
            panel = new VisualElement();
            panel.AddToClassList("rf-mres");
            panel.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => Hide());
            root.Add(panel);
            root.Add(confettiLayer);
            root.style.display = DisplayStyle.None;
            layer.Add(root);
        }

        public bool Attached => root.panel != null;

        public void Remove() => root.RemoveFromHierarchy();

        public void Show(RegattaResult result)
        {
            panel.Clear();
            Build(result);
            root.style.display = DisplayStyle.Flex;
            root.RemoveFromClassList("rf-mres-wrap--shown");
            root.schedule.Execute(() => root.AddToClassList("rf-mres-wrap--shown"));
            shownAt = Time.unscaledTime;
            hideAt = shownAt + ShowSeconds;
            StartConfetti();
        }

        public void Hide()
        {
            if (hideAt < 0f) return;
            hideAt = -1f;
            root.RemoveFromClassList("rf-mres-wrap--shown");
            root.schedule.Execute(() => { if (hideAt < 0f) root.style.display = DisplayStyle.None; }).ExecuteLater(400);
        }

        /// <summary>Every frame: the confetti falls, the panel goes when its time is up; hidden in build mode.</summary>
        public void Update(bool building)
        {
            if (hideAt < 0f) return;
            if (building || Time.unscaledTime >= hideAt) { Hide(); return; }
            Fall(Time.unscaledDeltaTime);
        }

        // ---- Building ----------------------------------------------------------------------------------

        private void Build(RegattaResult result)
        {
            var head = Row("rf-mres__head");
            head.Add(Icon(Trophy, "rf-mres__trophy"));
            var titles = new VisualElement { pickingMode = PickingMode.Ignore };
            titles.Add(Text(Localization.UI_REGATTA_RESULTS, "rf-mres__title", "rf-font-eb"));
            var line = string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_RESULTS_LINE, result.Laps, result.Boats);
            if (result.WinnerTime >= 0f) line += "   |   " + Regatta.Clock(result.WinnerTime);
            titles.Add(Text(line, "rf-mres__subtitle"));
            head.Add(titles);
            panel.Add(head);

            // the podium: second, first (tallest, crowned), third
            var podium = Row("rf-mres__podium");
            var first = result.Places.Count > 0 ? result.Places[0] : null;
            var second = result.Places.Count > 1 && !result.Places[1].Out ? result.Places[1] : null;
            var third = result.Places.Count > 2 && !result.Places[2].Out ? result.Places[2] : null;
            if (second != null) podium.Add(Step(second, "silver"));
            if (first != null) podium.Add(Step(first, "gold"));
            if (third != null) podium.Add(Step(third, "bronze"));
            panel.Add(podium);

            // the rest of the order
            if (result.Places.Count > 3)
            {
                var list = new VisualElement { pickingMode = PickingMode.Ignore };
                list.AddToClassList("rf-mres__list");
                for (var i = 3; i < result.Places.Count; ++i)
                {
                    var p = result.Places[i];
                    var row = Row("rf-mres__row");
                    row.Add(Text(p.Out ? "-" : Regatta.Ordinal(p.Number), "rf-mres__place", "rf-font-b"));
                    row.Add(Text(p.Name, "rf-mres__name", "rf-font-b"));
                    row.Add(Text(TimeText(p), p.Time >= 0f ? "rf-mres__time" : "rf-mres__time--none"));
                    list.Add(row);
                }
                panel.Add(list);
            }

            // the standouts, only those there are
            var cards = Row("rf-mres__cards");
            if (!string.IsNullOrEmpty(result.MostHits.Name))
                cards.Add(Card(Burst, "rf-mres__card-icon--hit", Localization.UI_REGATTA_STAT_HITS, result.MostHits.Name, "x" + result.MostHits.Count));
            if (!string.IsNullOrEmpty(result.MostPickups.Name))
                cards.Add(Card(Bottle, "rf-mres__card-icon--pick", Localization.UI_REGATTA_STAT_PICKUPS, result.MostPickups.Name, "x" + result.MostPickups.Count));
            if (!string.IsNullOrEmpty(result.Comeback.Name))
                cards.Add(Card(Rising, "rf-mres__card-icon--rise", Localization.UI_REGATTA_STAT_COMEBACK, result.Comeback.Name,
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_STAT_COMEBACK_VALUE, Regatta.Ordinal(result.Comeback.From), Regatta.Ordinal(result.Comeback.To))));
            if (!string.IsNullOrEmpty(result.KrakenBy))
                cards.Add(Card(Octopus, "rf-mres__card-icon--kraken", Localization.UI_REGATTA_STAT_KRAKEN, result.KrakenBy, null));
            if (cards.childCount > 0) panel.Add(cards);

            // the viewers who meddled, and the pot their coins filled
            if (result.Saboteurs.Count > 0 || result.Pot > 0)
            {
                var foot = Row("rf-mres__foot");
                if (result.Saboteurs.Count > 0)
                {
                    var box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("rf-mres__saboteurs");
                    var title = Row("rf-mres__sab-title");
                    title.Add(Icon(Skull, "rf-mres__sab-icon"));
                    title.Add(Text(Localization.UI_REGATTA_SABOTEURS, "rf-mres__sab-heading", "rf-font-b"));
                    box.Add(title);
                    var names = Row("rf-mres__sab-list");
                    for (var i = 0; i < result.Saboteurs.Count && i < 3; ++i)
                    {
                        var s = result.Saboteurs[i];
                        var entry = new VisualElement { pickingMode = PickingMode.Ignore };
                        entry.AddToClassList("rf-mres__sab");
                        if (i > 0) entry.AddToClassList("rf-mres__sab--more");
                        entry.Add(Text(s.Name, "rf-mres__sab-name", "rf-font-b"));
                        entry.Add(Text(Meddled(s), "rf-mres__sab-what"));
                        names.Add(entry);
                    }
                    box.Add(names);
                    foot.Add(box);
                }
                if (result.Pot > 0)
                {
                    var pot = Row("rf-mres__pot");
                    pot.Add(Icon(Coins, "rf-mres__pot-icon"));
                    var words = new VisualElement { pickingMode = PickingMode.Ignore };
                    words.Add(Text(Localization.UI_REGATTA_POT_TITLE, "rf-mres__pot-title"));
                    words.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_POT_COINS, result.Pot.ToString("N0", CultureInfo.InvariantCulture)), "rf-mres__pot-coins", "rf-font-eb"));
                    words.Add(Text(Localization.UI_REGATTA_POT_TO, "rf-mres__pot-title"));
                    pot.Add(words);
                    foot.Add(pot);
                }
                panel.Add(foot);
            }
        }

        private VisualElement Step(RegattaResult.Place p, string medal)
        {
            var step = new VisualElement { pickingMode = PickingMode.Ignore };
            step.AddToClassList("rf-mres__step");
            step.AddToClassList("rf-mres__step--" + medal);
            if (medal == "gold") step.Add(Icon(Crown, "rf-mres__crown"));
            step.Add(Text(Regatta.Ordinal(p.Number), "rf-mres__step-place", "rf-font-eb"));
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("rf-mres__boat");
            SetBoatPicture(picture, p.Boat);
            step.Add(picture);
            step.Add(Text(p.Name, "rf-mres__step-name", "rf-font-b"));
            var detail = string.IsNullOrEmpty(p.Boat) ? TimeText(p) : p.Boat + "   " + TimeText(p);
            step.Add(Text(detail, "rf-mres__step-detail"));
            return step;
        }

        /// <summary>The boat's own item picture: the game's (Resources/Items), else the website's.</summary>
        private static void SetBoatPicture(VisualElement picture, string boat)
        {
            var slug = ItemIcons.Slug(boat);
            if (slug == null) return;
            var local = Resources.Load<Texture2D>("Items/" + slug);
            if (local) { picture.style.backgroundImage = new StyleBackground(local); return; }
            ItemIcons.Get(boat, texture => { if (texture) picture.style.backgroundImage = new StyleBackground(texture); });
        }

        private static VisualElement Card(string glyph, string iconClass, string title, string name, string value)
        {
            var card = Row("rf-mres__card");
            card.Add(Icon(glyph, "rf-mres__card-icon", iconClass));
            var words = new VisualElement { pickingMode = PickingMode.Ignore };
            words.style.flexShrink = 1;
            words.Add(Text(title, "rf-mres__card-title"));
            words.Add(Text(name, "rf-mres__card-name", "rf-font-b"));
            if (!string.IsNullOrEmpty(value)) words.Add(Text(value, "rf-mres__card-value", "rf-font-b"));
            card.Add(words);
            return card;
        }

        private static string TimeText(RegattaResult.Place p) =>
            p.Out ? Localization.UI_REGATTA_RESULTS_OUT : p.Time >= 0f ? Regatta.Clock(p.Time) : Localization.UI_REGATTA_RESULTS_DNF;

        /// <summary>"3 kegs, 1 wind", or "2 winds on Bot36850" when it all went at one racer.</summary>
        private static string Meddled(RegattaResult.Saboteur s)
        {
            var parts = new List<string>();
            if (s.Kegs > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, s.Kegs == 1 ? Localization.UI_REGATTA_SAB_KEG : Localization.UI_REGATTA_SAB_KEGS, s.Kegs));
            if (s.Winds > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, s.Winds == 1 ? Localization.UI_REGATTA_SAB_WIND : Localization.UI_REGATTA_SAB_WINDS, s.Winds));
            var what = string.Join(", ", parts);
            return string.IsNullOrEmpty(s.OnlyTarget) ? what : string.Format(CultureInfo.InvariantCulture, Localization.UI_REGATTA_SAB_ON, what, s.OnlyTarget);
        }

        private static VisualElement Row(string cls)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList(cls);
            return row;
        }

        private static Label Text(string text, params string[] classes)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) label.AddToClassList(c);
            return label;
        }

        private static Label Icon(string glyph, params string[] classes)
        {
            var label = Text(glyph, classes);
            label.AddToClassList("rf-icon");
            return label;
        }

        // ---- Confetti ----------------------------------------------------------------------------------

        private void StartConfetti()
        {
            foreach (var c in confetti) c.Piece.RemoveFromHierarchy();
            confetti.Clear();
            for (var i = 0; i < ConfettiPieces; ++i)
            {
                var piece = new VisualElement { pickingMode = PickingMode.Ignore };
                piece.AddToClassList("rf-mres-confetti__piece");
                piece.style.backgroundColor = ConfettiColours[i % ConfettiColours.Length];
                var wide = Random.value < 0.5f;
                piece.style.width = wide ? 12f : 7f;
                piece.style.height = wide ? 7f : 12f;
                confettiLayer.Add(piece);
                // from above the panel, spread across it, some later than others
                var at = new Vector2(Random.Range(0.05f, 0.95f), Random.Range(-0.6f, -0.02f));
                var velocity = new Vector2(Random.Range(-0.04f, 0.04f), Random.Range(0.18f, 0.32f));
                confetti.Add((piece, at, velocity, Random.Range(0f, 360f), Random.Range(-240f, 240f)));
            }
        }

        private void Fall(float dt)
        {
            if (confetti.Count == 0) return;
            var size = confettiLayer.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f) return;
            var age = Time.unscaledTime - shownAt;
            var fade = Mathf.Clamp01((ConfettiSeconds - age) / 1.2f);
            for (var i = confetti.Count - 1; i >= 0; --i)
            {
                var c = confetti[i];
                c.At += c.Velocity * dt;
                c.At.x += Mathf.Sin((age + i) * 2.3f) * 0.02f * dt;
                c.Spin += c.Turn * dt;
                if (fade <= 0f || c.At.y > 1.1f)
                {
                    c.Piece.RemoveFromHierarchy();
                    confetti.RemoveAt(i);
                    continue;
                }
                c.Piece.style.left = c.At.x * size.x;
                c.Piece.style.top = c.At.y * size.y;
                c.Piece.style.rotate = new Rotate(c.Spin);
                c.Piece.style.opacity = fade;
                confetti[i] = c;
            }
        }
    }
}
