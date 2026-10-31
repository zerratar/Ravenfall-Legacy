using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The quest window's pictures (Assets/World/Quests/Resources/QuestUI, drawn with the Image Generator by
    /// tools/quests/quest-ui-pictures.cs): a scene per stream quest kind for the offer cards, the stories' subjects, the
    /// empty states and the small reward icons. A picture that is missing shows a Font Awesome glyph in its place, so a
    /// new quest kind or story still draws.
    /// </summary>
    public static class QuestArt
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!cache.TryGetValue(name, out var texture))
            {
                texture = UnityEngine.Resources.Load<Texture2D>("QuestUI/" + name);
                cache[name] = texture;
            }
            return texture;
        }

        /// <summary>A picture box (sized by its classes, space separated), or the glyph centred in it when the picture is missing.</summary>
        public static VisualElement Picture(string name, string glyph, string classNames)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var className in classNames.Split(' ')) box.AddToClassList(className);
            var texture = Load(name);
            if (texture != null)
            {
                box.style.backgroundImage = new StyleBackground(texture);
                return box;
            }

            box.AddToClassList("rf-qart--missing");
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-qart__glyph");
            box.Add(icon);
            return box;
        }

        /// <summary>A small icon beside a number: the picture, or its glyph.</summary>
        public static VisualElement Icon(string name, string glyph)
        {
            var texture = Load(name);
            if (texture != null)
            {
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("rf-qart-icon");
                image.style.backgroundImage = new StyleBackground(texture);
                return image;
            }

            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-qart-icon");
            icon.AddToClassList("rf-qart-icon--glyph");
            return icon;
        }

        public static VisualElement Coin() => Icon("icon-coin", "\uf51e");

        /// <summary>The picture for what a quest asks (a stream quest's or a streamer's): gather by skill, or the objective.</summary>
        public static string QuestPicture(string objective, int skill)
        {
            switch (objective)
            {
                case "gather":
                    switch ((Skill)skill)
                    {
                        case Skill.Woodcutting: return "quest-wood";
                        case Skill.Mining: return "quest-ore";
                        case Skill.Fishing: return "quest-fish";
                        case Skill.Farming: return "quest-wheat";
                        default: return "quest-herbs";
                    }
                case "kill": return "quest-kill";
                case "make":
                case "craft":
                case "cook":
                case "brew": return "quest-make";
                case "levels": return "quest-levels";
                case "raid": return "quest-raid";
                case "dungeon": return "quest-dungeon";
                case "hero_wave": return "quest-hero";
                default: return null;
            }
        }

        public static string QuestGlyph(string objective)
        {
            switch (objective)
            {
                case "gather": return "\uf6e3";
                case "kill": return "\uf71c";
                case "levels": return "\uf062";
                case "raid": return "\uf6d5";
                case "dungeon": return "\uf6d9";
                default: return "\uf024";
            }
        }

        /// <summary>The icon for a stream quest's resource reward: 1 wood, 2 ore, 3 fish, 4 wheat.</summary>
        public static VisualElement Resource(int resource)
        {
            switch (resource)
            {
                case 1: return Icon("icon-wood", "\uf06c");
                case 2: return Icon("icon-ore", "\uf3a5");
                case 3: return Icon("icon-fish", "\uf578");
                default: return Icon("icon-wheat", "\uf72c");
            }
        }

        public static string StoryPicture(string key) => "story-" + key;

        // ---- Pieces the three tabs share -------------------------------------------------------------------

        /// <summary>A tab's heading: a big line and a dim one under it.</summary>
        public static void Heading(VisualElement content, string title, string sub)
        {
            content.Add(Text(title, "rf-qheading"));
            if (!string.IsNullOrEmpty(sub)) content.Add(Text(sub, "rf-qheading__sub"));
        }

        public static Label Glyph(string glyph, string className)
        {
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList(className);
            return icon;
        }

        /// <summary>A thin upright line between two things in a row.</summary>
        public static VisualElement Divider()
        {
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("rf-qdivider");
            return line;
        }

        /// <summary>A glyph and a short text, as "(clock) 120 min"; the text is the rf-qchip__text label.</summary>
        public static VisualElement Chip(string glyph, string text)
        {
            var chip = new VisualElement { pickingMode = PickingMode.Ignore };
            chip.AddToClassList("rf-qchip");
            chip.Add(Glyph(glyph, "rf-qchip__icon"));
            chip.Add(Text(text, "rf-qchip__text"));
            return chip;
        }

        /// <summary>An icon and a number beside it.</summary>
        public static VisualElement Amount(VisualElement icon, string amount)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-qamount");
            row.Add(icon);
            row.Add(Text(amount, "rf-qamount__text"));
            return row;
        }

        /// <summary>Reward columns with a small capital label over each and a line between them.</summary>
        public static VisualElement Rewards(params (string Label, VisualElement Row)[] columns)
        {
            var rewards = new VisualElement { pickingMode = PickingMode.Ignore };
            rewards.AddToClassList("rf-qrewards");
            for (var i = 0; i < columns.Length; ++i)
            {
                if (i > 0) rewards.Add(Divider());
                var column = new VisualElement { pickingMode = PickingMode.Ignore };
                column.AddToClassList("rf-qrewards__column");
                column.Add(Text(columns[i].Label.ToUpperInvariant(), "rf-qlabel"));
                column.Add(columns[i].Row);
                rewards.Add(column);
            }
            return rewards;
        }

        public static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
