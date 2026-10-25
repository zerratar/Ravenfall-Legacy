using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Hero mode's HUD (docs/hero-mode-design.md, 7a): a health orb bottom left, a mana orb bottom right, and the skill
    /// bar between them (four skills with their keys, cooldown sweeps, mana costs and the level that unlocks them; the
    /// dodge and the potion). While the hero plays the usual HUD steps aside: the town bar, the shortcuts and the
    /// stream quest bar (hidden through a class on the documents, as their views set their own display).
    /// </summary>
    public class HeroHudView : MonoBehaviour
    {
        private const string HeroModeClass = "rf-hero-mode";

        private VisualElement root;
        private VisualElement healthFill, manaFill;
        private Label healthText, manaText, style;
        private readonly Slot[] slots = new Slot[6];
        private bool bound;

        private sealed class Slot
        {
            public VisualElement Box;
            public VisualElement Sweep;
            public Label Name;
            public Label Note;
            public float FlashUntil;
        }

        private void OnEnable()
        {
            bound = false;
            HeroController.Changed += Changed;
            HeroController.Refused += Flash;
        }

        private void OnDisable()
        {
            HeroController.Changed -= Changed;
            HeroController.Refused -= Flash;
            SetHeroMode(false);
            bound = false;
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            root?.RemoveFromHierarchy();
            root = new VisualElement { name = "hero-hud", pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-hero");
            root.Add(Orb("rf-hero-orb--health", out healthFill, out healthText));
            root.Add(Orb("rf-hero-orb--mana", out manaFill, out manaText));

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rf-hero-bar");
            style = new Label { pickingMode = PickingMode.Ignore };
            style.AddToClassList("rf-hero-bar__style");
            bar.Add(style);
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-hero-bar__row");
            for (var i = 0; i < slots.Length; i++)
            {
                var key = i < 4 ? (i + 1).ToString(CultureInfo.InvariantCulture) : i == 4 ? "Space" : "Q";
                slots[i] = MakeSlot(key);
                if (i == 4) slots[i].Box.AddToClassList("rf-hero-slot--gap");
                row.Add(slots[i].Box);
            }
            bar.Add(row);
            root.Add(bar);
            root.style.display = DisplayStyle.None;
            layer.Add(root);
            bound = true;
            Changed();
        }

        private static VisualElement Orb(string kind, out VisualElement fill, out Label text)
        {
            var orb = new VisualElement { pickingMode = PickingMode.Ignore };
            orb.AddToClassList("rf-hero-orb");
            orb.AddToClassList(kind);
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("rf-hero-orb__fill");
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-hero-orb__text");
            orb.Add(fill);
            orb.Add(text);
            return orb;
        }

        private static Slot MakeSlot(string key)
        {
            var s = new Slot { Box = new VisualElement { pickingMode = PickingMode.Position } };
            s.Box.AddToClassList("rf-hero-slot");
            s.Name = new Label { pickingMode = PickingMode.Ignore };
            s.Name.AddToClassList("rf-icon");
            s.Name.AddToClassList("rf-hero-slot__icon");
            s.Sweep = new VisualElement { pickingMode = PickingMode.Ignore };
            s.Sweep.AddToClassList("rf-hero-slot__sweep");
            var k = new Label(key) { pickingMode = PickingMode.Ignore };
            k.AddToClassList("rf-hero-slot__key");
            s.Note = new Label { pickingMode = PickingMode.Ignore };
            s.Note.AddToClassList("rf-hero-slot__note");
            s.Box.Add(s.Name);
            s.Box.Add(s.Sweep);
            s.Box.Add(k);
            s.Box.Add(s.Note);
            return s;
        }

        private void Changed()
        {
            var hero = HeroController.Active;
            SetHeroMode(hero);
            if (!bound) return;
            root.style.display = hero ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hero) return;
            style.text = Localization.UI_HERO_STYLE_MELEE;
            for (var i = 0; i < 4; i++)
            {
                var skill = i < hero.Kit.Count ? hero.Kit[i] : null;
                slots[i].Name.text = skill?.Icon ?? string.Empty;
                slots[i].Box.tooltip = skill != null ? skill.Name() + "\n" + skill.Description() : string.Empty;
            }
            slots[4].Name.text = "";
            slots[4].Box.tooltip = Localization.UI_HERO_DODGE;
            slots[5].Name.text = "";
            slots[5].Box.tooltip = Localization.UI_HERO_POTION;
        }

        /// <summary>The usual HUD steps aside while the hero plays.</summary>
        private static void SetHeroMode(bool on)
        {
            foreach (var doc in FindObjectsByType<UIDocument>())
            {
                var r = doc.rootVisualElement;
                if (r != null) r.EnableInClassList(HeroModeClass, on);
            }
        }

        private void Flash(int slot, string why)
        {
            var index = slot >= 0 ? slot : slot == -1 ? 4 : 5;
            if (index < slots.Length && slots[index] != null) slots[index].FlashUntil = Time.time + 0.3f;
        }

        private void Update()
        {
            if (bound && root.panel == null) bound = false;
            if (!bound) TryBind();
            var hero = HeroController.Active;
            if (!bound || !hero || !hero.Player) return;

            var health = hero.Player.Stats.Health;
            var share = health.MaxLevel > 0 ? Mathf.Clamp01(health.CurrentValue / (float)health.MaxLevel) : 0f;
            healthFill.style.height = new Length(share * 100f, LengthUnit.Percent);
            healthText.text = health.CurrentValue + " / " + health.MaxLevel;
            manaFill.style.height = new Length(hero.Mana / hero.MaxMana * 100f, LengthUnit.Percent);
            manaText.text = Mathf.FloorToInt(hero.Mana) + " / " + Mathf.FloorToInt(hero.MaxMana);

            var level = hero.StyleLevel;
            for (var i = 0; i < 4; i++)
            {
                var skill = i < hero.Kit.Count ? hero.Kit[i] : null;
                if (skill == null) continue;
                var locked = level < skill.UnlockLevel;
                var left = Mathf.Max(0f, hero.ReadyAt(i) - Time.time);
                Show(slots[i], locked, left / skill.Cooldown, locked ? string.Format(CultureInfo.InvariantCulture, Localization.UI_HERO_LOCKED, skill.UnlockLevel) : left > 0f ? left.ToString("0", CultureInfo.InvariantCulture) : Mathf.RoundToInt(skill.Mana).ToString(CultureInfo.InvariantCulture), !locked && hero.Mana < skill.Mana);
            }
            var dodge = Mathf.Max(0f, hero.DodgeReadyAt - Time.time);
            Show(slots[4], false, dodge / HeroController.DodgeCooldown, dodge > 0f ? dodge.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty, false);
            Show(slots[5], hero.Potions <= 0, 0f, hero.Potions + "/" + hero.MaxPotions, false);
        }

        private static void Show(Slot s, bool locked, float sweep, string note, bool lowMana)
        {
            s.Box.EnableInClassList("rf-hero-slot--locked", locked);
            s.Box.EnableInClassList("rf-hero-slot--low", lowMana);
            s.Box.EnableInClassList("rf-hero-slot--flash", Time.time < s.FlashUntil);
            s.Sweep.style.height = new Length(Mathf.Clamp01(sweep) * 100f, LengthUnit.Percent);
            s.Note.text = note;
        }
    }
}
