using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Hero mode's loot on screen (RavebridgeLoot): the name of each drop on the ground in its rarity's colour, over it,
    /// as the action RPGs show them; and what the hero just picked up, rising from the hero and fading ("+12 coins",
    /// "Goblin ear"). Labels are pooled and projected from the world each frame, as the combat numbers are
    /// (FloatingTextView). HeroHudView owns it; clicks pass through to the world, where a click on a drop walks to it.
    /// </summary>
    public sealed class LootLabels
    {
        private const float ShowWithin = 45f;
        private const float PopupSeconds = 1.4f;
        private const float PopupRise = 46f;

        private sealed class Popup { public Label Label; public PlayerController Player; public float Age; public float Offset; }

        private readonly VisualElement layer;
        private readonly List<Label> pool = new List<Label>();
        private readonly Dictionary<LootDrop, Label> shown = new Dictionary<LootDrop, Label>();
        private readonly List<LootDrop> gone = new List<LootDrop>();
        private readonly List<Popup> popups = new List<Popup>();
        private float lastPopup;
        private int popupsInARow;

        public LootLabels(VisualElement parent)
        {
            layer = new VisualElement { name = "loot-labels", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("rf-loot-layer");
            parent.Add(layer);
            RavebridgeLoot.Picked += OnPicked;
        }

        public void Remove()
        {
            RavebridgeLoot.Picked -= OnPicked;
            layer.RemoveFromHierarchy();
        }

        private void OnPicked(PlayerController player, RavebridgeLoot.Item item, long amount)
        {
            var hero = HeroController.Active;
            if (!hero || hero.Player != player) return;
            var label = Take();
            label.AddToClassList("rf-loot-popup");
            label.text = item == null ? string.Format(Localization.UI_LOOT_PICKED_COINS, amount) : (amount > 1 ? "+" + amount + " " + item.Name : item.Name);
            label.style.color = item == null ? RavebridgeLoot.CoinColor : RavebridgeLoot.ColorOf(item.Grade);
            // several at once stack up instead of covering each other
            popupsInARow = Time.unscaledTime - lastPopup < 0.35f ? popupsInARow + 1 : 0;
            lastPopup = Time.unscaledTime;
            popups.Add(new Popup { Label = label, Player = player, Offset = popupsInARow * 22f });
        }

        private Label Take()
        {
            Label label;
            if (pool.Count > 0)
            {
                label = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
            }
            else
            {
                label = new Label { pickingMode = PickingMode.Ignore };
                layer.Add(label);
            }
            label.ClearClassList();
            label.AddToClassList("rf-loot-label");
            label.style.display = DisplayStyle.Flex;
            label.style.opacity = 1f;
            return label;
        }

        private void Give(Label label)
        {
            label.style.display = DisplayStyle.None;
            pool.Add(label);
        }

        public void Update()
        {
            var cam = Camera.main;
            var panel = layer.panel;
            if (!cam || panel == null) return;
            var camPos = cam.transform.position;

            // the drops on the ground
            foreach (var drop in LootDrop.All)
            {
                if (!drop) continue;
                var world = drop.LabelPoint;
                var near = (world - camPos).sqrMagnitude < ShowWithin * ShowWithin;
                var viewport = cam.WorldToViewportPoint(world);
                var visible = near && viewport.z > 0f && viewport.x > -0.05f && viewport.x < 1.05f && viewport.y > -0.05f && viewport.y < 1.05f;
                shown.TryGetValue(drop, out var label);
                if (!visible)
                {
                    if (label != null) { Give(label); shown.Remove(drop); }
                    continue;
                }
                if (label == null)
                {
                    label = Take();
                    label.text = drop.Label;
                    label.style.color = drop.Color;
                    label.EnableInClassList("rf-loot-label--rare", drop.Item != null && drop.Item.Grade >= RavebridgeLoot.Rarity.Rare);
                    shown[drop] = label;
                }
                Place(label, panel, world, cam, 0f);
            }
            gone.Clear();
            foreach (var pair in shown) if (!pair.Key) gone.Add(pair.Key);
            foreach (var drop in gone) { Give(shown[drop]); shown.Remove(drop); }

            // what was just picked up
            for (var i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.Age += Time.unscaledDeltaTime;
                if (p.Age >= PopupSeconds || !p.Player)
                {
                    Give(p.Label);
                    popups.RemoveAt(i);
                    continue;
                }
                var t = p.Age / PopupSeconds;
                var ease = 1f - (1f - t) * (1f - t);
                Place(p.Label, panel, p.Player.Position + Vector3.up * 2.3f, cam, -PopupRise * ease - p.Offset);
                p.Label.style.opacity = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            }
        }

        /// <summary>A label centred over a point in the world, its bottom on it, raised by some pixels.</summary>
        private static void Place(Label label, IPanel panel, Vector3 world, Camera cam, float raise)
        {
            var point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
            var width = label.resolvedStyle.width;
            var height = label.resolvedStyle.height;
            if (float.IsNaN(width)) width = 0f;
            if (float.IsNaN(height)) height = 0f;
            label.style.translate = new Translate(point.x - width * 0.5f, point.y - height + raise);
        }

        /// <summary>Clears every label (hero mode ended).</summary>
        public void Clear()
        {
            foreach (var pair in shown) Give(pair.Value);
            shown.Clear();
            foreach (var p in popups) Give(p.Label);
            popups.Clear();
        }
    }
}
