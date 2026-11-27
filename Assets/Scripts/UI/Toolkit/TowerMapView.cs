using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The floor map of the tower (docs/tower-worlds-proposal.md section 13): a floor as a world. Its town in the
    /// middle, the floor's areas to both sides of it (the courtyard, the marsh, the crypt), and each area's parts as
    /// numbered tiles: lit when the stream has cleared them, gold where the climb is, a question mark where nobody
    /// has been, with how many players are on each part right now. A section of the tower panel
    /// (<see cref="TowerHudView"/>), opened from its Map button or with !tower map: never a window of its own, so it
    /// stacks with nothing.
    /// </summary>
    /// <remarks>
    /// Everything is drawn from RavenNest.Combat.Tower (<c>AreasOf</c>), so a floor with any number of areas and parts
    /// works: the areas go in turn to the left and the right column, and an area's tiles wrap. It is built again only
    /// when what it shows changes (a part cleared, a player moving), which <see cref="Refresh"/> tells from a short
    /// text of its state.
    /// </remarks>
    public sealed class TowerMapView
    {
        public readonly VisualElement Root;

        private readonly Label title, bonus, hint;
        private readonly Button previous, next;
        private readonly VisualElement body, left, right, middle, lines;
        private readonly List<(VisualElement Card, bool Reached)> cards = new List<(VisualElement, bool)>();
        private VisualElement townBadge;
        private string shown;

        /// <summary>The floor the map shows.</summary>
        public int Floor { get; private set; }

        /// <param name="browse">The arrows: one floor down (-1) or up (+1).</param>
        public TowerMapView(Action<int> browse)
        {
            Root = Box("rf-tmap");
            Root.name = "tower-map";

            var top = Box("rf-tmap__top");
            previous = Arrow("", () => browse(-1));
            title = Text("rf-tmap__title");
            next = Arrow("", () => browse(1));
            top.Add(previous);
            top.Add(title);
            top.Add(next);
            // what the floor's parts pay above the island (5% a floor)
            bonus = Text("rf-tmap__bonus");
            top.Add(bonus);
            top.Add(Box("rf-raid__spacer"));
            hint = Text("rf-tmap__hint");
            top.Add(hint);
            Root.Add(top);

            body = Box("rf-tmap__body");
            lines = Box("rf-tmap__lines");
            lines.generateVisualContent += DrawLines;
            left = Box("rf-tmap__column");
            middle = Box("rf-tmap__middle");
            right = Box("rf-tmap__column");
            body.Add(lines);
            body.Add(left);
            body.Add(middle);
            body.Add(right);
            body.RegisterCallback<GeometryChangedEvent>(_ => lines.MarkDirtyRepaint());
            Root.Add(body);

            // what the tiles mean, under the map
            var legend = Box("rf-tmap__legends");
            legend.Add(Legend("rf-tmap__tile--cleared", "1", Localization.UI_TOWER_MAP_CLEARED));
            legend.Add(Legend("rf-tmap__tile--climb", "2", Localization.UI_TOWER_MAP_CLIMB));
            legend.Add(Legend("rf-tmap__tile--hidden", "?", Localization.UI_TOWER_MAP_HIDDEN));
            var here = Box("rf-tmap__legend");
            var hereCount = Box("rf-tmap__count", "rf-tmap__count--legend");
            hereCount.Add(Icon("\uf0c0", "rf-tmap__count-icon"));
            here.Add(hereCount);
            var hereText = Text("rf-tmap__legend-text");
            hereText.text = Localization.UI_TOWER_MAP_PLAYERS;
            here.Add(hereText);
            legend.Add(here);
            var crown = Box("rf-tmap__legend");
            crown.Add(Icon("\uf521", "rf-tmap__legend-crown"));
            var crownText = Text("rf-tmap__legend-text");
            crownText.text = Localization.UI_TOWER_MAP_FLOOR_BOSS;
            crown.Add(crownText);
            legend.Add(crown);
            Root.Add(legend);
        }

        /// <summary>Shows a floor as the stream has it now; builds the map again only when something on it changed.</summary>
        public void Refresh(TowerManager tower, int floor)
        {
            var info = Tower.FloorInfo(floor);
            if (!tower || info == null) return;
            var stream = tower.StreamFloor;
            var allCleared = tower.AllCleared;
            var areas = Tower.AreasOf(floor);
            var town = Tower.Towns.Count > 0 ? TownOf(floor) : null;
            var inTown = town != null && TowerTown.Loaded && TowerTown.Loaded.Town == town ? tower.TownPlayers().Count : 0;
            var reachedFloors = TowerWords.Floor(stream);

            // what the map shows, as a text: built again only when it differs
            var state = new StringBuilder();
            state.Append(floor).Append('|').Append(stream).Append('|').Append(allCleared ? 1 : 0).Append('|').Append(inTown).Append('|').Append(reachedFloors);
            foreach (var area in areas)
                foreach (var step in area.Steps) state.Append('|').Append(tower.CountOnPart(step));
            var text = state.ToString();
            if (text == shown) return;
            shown = text;
            Floor = floor;

            title.text = string.Format(Localization.UI_TOWER_MAP_TITLE, floor, info.Name);
            bonus.text = string.Format(Localization.UI_TOWER_MAP_EXP, Mathf.RoundToInt((float)(Tower.ExpBonus(floor) * 100)).ToString(CultureInfo.InvariantCulture));
            previous.SetEnabled(floor > 1);
            next.SetEnabled(floor < reachedFloors);
            // with one floor reached there is nothing to page through
            previous.style.display = reachedFloors > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            next.style.display = reachedFloors > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            left.Clear();
            right.Clear();
            middle.Clear();
            cards.Clear();

            // the town in the middle
            townBadge = Box("rf-tmap__town");
            townBadge.Add(Icon("", "rf-tmap__town-icon"));
            var townName = Text("rf-tmap__town-name");
            townName.text = town != null ? town.Name : Localization.UI_TOWER_MAP_TOWN;
            townBadge.Add(townName);
            if (inTown > 0) townBadge.Add(Count(inTown));
            middle.Add(townBadge);

            // its areas, in turn to the left and the right of it
            var example = 0;
            for (var i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                var reached = area.FirstStep <= stream;
                var card = Box("rf-tmap__area");
                card.EnableInClassList("rf-tmap__area--hidden", !reached);
                var name = Text("rf-tmap__area-name");
                name.text = reached ? area.Name : Localization.UI_TOWER_MAP_UNKNOWN;
                card.Add(name);
                var tiles = Box("rf-tmap__tiles");
                TowerFloorDefinition climb = null;
                foreach (var step in area.Steps)
                {
                    var cleared = step < stream || allCleared;
                    var current = step == stream && !allCleared;
                    var part = TowerWords.Part(step);
                    var tile = Box("rf-tmap__tile");
                    var number = Text("rf-tmap__tile-number");
                    if (cleared || current)
                    {
                        number.text = part.ToString(CultureInfo.InvariantCulture);
                        tile.AddToClassList(current ? "rf-tmap__tile--climb" : "rf-tmap__tile--cleared");
                        if (cleared && example == 0) example = step;
                        if (current) climb = Tower.Get(step);
                    }
                    else
                    {
                        number.text = "?";
                        tile.AddToClassList("rf-tmap__tile--hidden");
                    }
                    tile.Add(number);
                    // the floor boss's part wears a crown once it is reached (which part that is stays unknown until then)
                    if ((cleared || current) && Tower.IsFloorBoss(step)) tile.Add(Icon("", "rf-tmap__tile-crown"));
                    var on = tower.CountOnPart(step);
                    if (on > 0) tile.Add(Count(on));
                    tiles.Add(tile);
                }
                card.Add(tiles);
                if (climb != null)
                {
                    var line = Text("rf-tmap__area-climb");
                    line.text = string.Format(Localization.UI_TOWER_MAP_CLIMB_LINE, climb.Part, climb.Name);
                    card.Add(line);
                }
                (i % 2 == 0 ? left : right).Add(card);
                cards.Add((card, reached));
            }

            // how to go to one of the lit parts (one that is cleared, as the example)
            hint.text = example > 0 ? string.Format(Localization.UI_TOWER_MAP_HINT, Tower.Short(example)) : "";
            hint.style.display = example > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            lines.MarkDirtyRepaint();
        }

        /// <summary>The map is built again on the next <see cref="Refresh"/> (the language changed, the panel was rebuilt).</summary>
        public void Forget() => shown = null;

        private static TowerTownDefinition TownOf(int floor)
        {
            foreach (var t in Tower.Towns) if (t.Band == floor) return t;
            return null;
        }

        // the paths from the town to each area: lit to the ones reached, faint dashes to the others
        private void DrawLines(MeshGenerationContext context)
        {
            if (townBadge == null || townBadge.panel == null) return;
            var painter = context.painter2D;
            var from = lines.WorldToLocal(townBadge.worldBound.center);
            foreach (var (card, reached) in cards)
            {
                if (card.panel == null) continue;
                var box = card.worldBound;
                var to = lines.WorldToLocal(box.center);
                painter.lineWidth = reached ? 5f : 4f;
                painter.strokeColor = reached ? new Color(0.5f, 0.69f, 0.41f, 1f) : new Color(0.35f, 0.32f, 0.27f, 1f);
                painter.lineCap = LineCap.Round;
                var length = Vector2.Distance(from, to);
                if (length < 1f) continue;
                if (reached)
                {
                    painter.BeginPath();
                    painter.MoveTo(from);
                    painter.LineTo(to);
                    painter.Stroke();
                    continue;
                }
                var direction = (to - from) / length;
                for (var at = 0f; at < length; at += 26f)
                {
                    painter.BeginPath();
                    painter.MoveTo(from + direction * at);
                    painter.LineTo(from + direction * Mathf.Min(length, at + 12f));
                    painter.Stroke();
                }
            }
        }

        private static VisualElement Count(int players)
        {
            var count = Box("rf-tmap__count");
            count.Add(Icon("", "rf-tmap__count-icon"));
            var number = Text("rf-tmap__count-number");
            number.text = players.ToString(CultureInfo.InvariantCulture);
            count.Add(number);
            return count;
        }

        private static VisualElement Legend(string tileClass, string glyph, string text)
        {
            var legend = Box("rf-tmap__legend");
            var tile = Box("rf-tmap__tile", "rf-tmap__tile--legend", tileClass);
            var mark = Text("rf-tmap__tile-number");
            mark.text = glyph;
            tile.Add(mark);
            legend.Add(tile);
            var label = Text("rf-tmap__legend-text");
            label.text = text;
            legend.Add(label);
            return legend;
        }

        private static Button Arrow(string glyph, Action clicked)
        {
            var button = new Button(clicked) { text = "" };
            button.AddToClassList("rf-tmap__arrow");
            button.Add(Icon(glyph, "rf-tmap__arrow-icon"));
            return button;
        }

        private static Label Icon(string glyph, string cls)
        {
            var icon = Text("rf-icon", cls);
            icon.text = glyph;
            return icon;
        }

        private static VisualElement Box(params string[] classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) e.AddToClassList(c);
            return e;
        }

        private static Label Text(params string[] classes)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }
    }
}
