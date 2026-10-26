using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Hero mode's HUD (docs/hero-mode-design.md): a health orb bottom left and a mana orb bottom right (a glass sphere
    /// of liquid that waves, sloshes when hit and shows what a hit took, drawn by the Hero Orb shader into a small
    /// render texture each frame, the glass see-through and its light added over what is behind, with the label under
    /// it, after the user's reference), the skill
    /// bar between them (the style with Tab to switch, four skills with their keys, cooldown sweeps, mana costs and unlock levels, the
    /// dodge and the potion), and on the left the party: who is on Ravebridge, with their health. While the hero plays
    /// the usual HUD steps aside (a class on the documents, as their views set their own display).
    /// </summary>
    public class HeroHudView : MonoBehaviour
    {
        private const string HeroModeClass = "rf-hero-mode";
        private const int PartyShown = 12;

        private VisualElement root;
        private Orb health, mana;
        private Label healthText, manaText, styleIcon, styleName;
        private VisualElement styleBox;
        private float styleFlashUntil;
        private readonly Slot[] slots = new Slot[6];
        private VisualElement party;
        private Label partyTitle;
        private readonly List<PartyRow> partyRows = new List<PartyRow>();
        private float nextParty;
        private HeroInventoryWindow inventory;
        private HeroWaveHud waves;
        private HeroHelpersHud helpers;
        private bool bound;


        /// <summary>One orb: its texture, its material, and the level shown (eased), where it was, and its slosh.</summary>
        private sealed class Orb
        {
            public VisualElement Sphere;
            public RenderTexture Texture;
            public Material Material;
            public float Shown = 1f;
            public float Trail = 1f;
            public float TrailHoldUntil;
            public float Slosh;
            public float SloshSpeed;
            public bool Started;
        }

        private sealed class Slot
        {
            public VisualElement Box;
            public VisualElement Sweep;
            public Label Name;
            public Label Note;
            public float FlashUntil;
        }

        private sealed class PartyRow
        {
            public VisualElement Box;
            public Label Name;
            public VisualElement Fill;
        }

        private void OnEnable()
        {
            bound = false;
            HeroController.Changed += Changed;
            HeroController.Refused += Flash;
            HeroController.InventoryPressed += ToggleInventory;
        }

        private void OnDisable()
        {
            HeroController.Changed -= Changed;
            HeroController.Refused -= Flash;
            HeroController.InventoryPressed -= ToggleInventory;
            SetHeroMode(false);
            bound = false;
        }

        // ---- Building -------------------------------------------------------------------------------------

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            root?.RemoveFromHierarchy();
            root = new VisualElement { name = "hero-hud", pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-hero");
            ReleaseOrbs();
            health = MakeOrb(new Color(0.9f, 0.02f, 0.02f), new Color(0.18f, 0.0f, 0.005f), new Color(1f, 0.22f, 0.1f));
            mana = MakeOrb(new Color(0.06f, 0.25f, 1f), new Color(0.0f, 0.02f, 0.2f), new Color(0.3f, 0.65f, 1f));
            root.Add(OrbElement("rf-hero-orb--health", health, Localization.UI_HERO_HEALTH, out healthText));
            root.Add(OrbElement("rf-hero-orb--mana", mana, Localization.UI_HERO_MANA, out manaText));

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("rf-hero-bar");
            // the style: its icon and name, Tab to switch
            styleBox = new VisualElement { pickingMode = PickingMode.Position };
            styleBox.AddToClassList("rf-hero-style");
            styleIcon = new Label { pickingMode = PickingMode.Ignore };
            styleIcon.AddToClassList("rf-icon");
            styleIcon.AddToClassList("rf-hero-style__icon");
            styleName = new Label { pickingMode = PickingMode.Ignore };
            styleName.AddToClassList("rf-hero-style__name");
            var tab = new Label("Tab") { pickingMode = PickingMode.Ignore };
            tab.AddToClassList("rf-hero-style__key");
            styleBox.Add(styleIcon);
            styleBox.Add(styleName);
            styleBox.Add(tab);
            bar.Add(styleBox);
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
            // the shopkeeper's buffs on now, over the bar
            buffs = new VisualElement { pickingMode = PickingMode.Ignore };
            buffs.AddToClassList("rf-hero-buffs");
            bar.Insert(0, buffs);
            root.Add(bar);

            party = new VisualElement { pickingMode = PickingMode.Ignore };
            party.AddToClassList("rf-hero-party");
            partyTitle = new Label { pickingMode = PickingMode.Ignore };
            partyTitle.AddToClassList("rf-hero-party__title");
            party.Add(partyTitle);
            partyRows.Clear();
            for (var i = 0; i < PartyShown; i++)
            {
                var r = new PartyRow { Box = new VisualElement { pickingMode = PickingMode.Ignore } };
                r.Box.AddToClassList("rf-hero-party__row");
                r.Name = new Label { pickingMode = PickingMode.Ignore };
                r.Name.AddToClassList("rf-hero-party__name");
                var track = new VisualElement { pickingMode = PickingMode.Ignore };
                track.AddToClassList("rf-hero-party__bar");
                r.Fill = new VisualElement { pickingMode = PickingMode.Ignore };
                r.Fill.AddToClassList("rf-hero-party__fill");
                track.Add(r.Fill);
                r.Box.Add(r.Name);
                r.Box.Add(track);
                party.Add(r.Box);
                partyRows.Add(r);
            }
            root.Add(party);

            waves = new HeroWaveHud(root);
            helpers = new HeroHelpersHud(root, styleBox);
            loot?.Remove();
            loot = new LootLabels(root);
            root.style.display = DisplayStyle.None;
            layer.Add(root);
            inventory?.Remove();
            inventory = new HeroInventoryWindow(layer);
            bound = true;
            Changed();
        }

        private LootLabels loot;
        private VisualElement buffs;
        private float nextBuffs;

        private void DrawBuffs(PlayerController player)
        {
            if (buffs == null) return;
            buffs.Clear();
            foreach (var (entry, left) in RavebridgeShop.ActiveBuffs(player))
            {
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("rf-hero-buff");
                var icon = new Label(entry.Glyph) { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-icon");
                icon.AddToClassList("rf-hero-buff__icon");
                var time = new Label(Mathf.FloorToInt(left / 60f) + ":" + Mathf.FloorToInt(left % 60f).ToString("00", CultureInfo.InvariantCulture)) { pickingMode = PickingMode.Ignore };
                time.AddToClassList("rf-hero-buff__time");
                chip.Add(icon);
                chip.Add(time);
                buffs.Add(chip);
            }
        }

        private const int OrbTexture = 256;

        private static Orb MakeOrb(Color liquid, Color deep, Color glow)
        {
            var orb = new Orb();
            var template = Resources.Load<Material>("HeroOrb");
            if (!template) return orb;
            orb.Material = new Material(template) { hideFlags = HideFlags.DontSave };
            orb.Material.SetColor("_Color", liquid);
            orb.Material.SetColor("_Deep", deep);
            orb.Material.SetColor("_Glow", glow);
            orb.Texture = new RenderTexture(OrbTexture, OrbTexture, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { name = "Hero orb", hideFlags = HideFlags.DontSave };
            orb.Texture.Create();
            return orb;
        }

        /// <summary>An orb: the sphere, the number over it, the label under it.</summary>
        private static VisualElement OrbElement(string kind, Orb orb, string label, out Label text)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("rf-hero-orb");
            box.AddToClassList(kind);
            orb.Sphere = new VisualElement { pickingMode = PickingMode.Ignore };
            orb.Sphere.AddToClassList("rf-hero-orb__sphere");
            if (orb.Texture) orb.Sphere.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(orb.Texture));
            // premultiplied blending (a UI Shader Graph, Assets/UI/Shaders/HeroOrbLight): the orb's light adds over what is
            // behind it, and the empty glass lets it through
            var blend = Resources.Load<Material>("HeroOrbLight");
            if (blend) orb.Sphere.style.unityMaterial = new StyleMaterialDefinition(blend);
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-hero-orb__text");
            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rf-hero-orb__label");
            box.Add(orb.Sphere);
            box.Add(text);
            box.Add(name);
            return box;
        }

        private void ReleaseOrbs()
        {
            foreach (var orb in new[] { health, mana })
            {
                if (orb == null) continue;
                if (orb.Texture) { orb.Texture.Release(); Destroy(orb.Texture); }
                if (orb.Material) Destroy(orb.Material);
            }
            health = mana = null;
        }

        private void OnDestroy() => ReleaseOrbs();

        private int orbFrame;

        /// <summary>
        /// The level shown eases to the real one; a drop leaves a trail that waits a moment and then follows; the
        /// liquid rocks by how fast the level moved, like a spring. Drawn every frame (every third in potato mode).
        /// </summary>
        private static void DrawOrb(Orb orb, float share, bool draw)
        {
            if (orb == null) return;
            if (!orb.Started) { orb.Shown = orb.Trail = share; orb.Started = true; }
            var dt = Time.unscaledDeltaTime;
            var before = orb.Shown;
            orb.Shown = Mathf.Lerp(orb.Shown, share, 1f - Mathf.Exp(-8f * dt));
            if (share > orb.Trail) orb.Trail = share;
            else if (orb.Trail - share > 0.01f && orb.TrailHoldUntil <= 0f) orb.TrailHoldUntil = Time.time + 0.6f;
            if (orb.TrailHoldUntil > 0f && Time.time > orb.TrailHoldUntil)
            {
                orb.Trail = Mathf.MoveTowards(orb.Trail, share, dt * 0.5f);
                if (orb.Trail - share <= 0.001f) orb.TrailHoldUntil = 0f;
            }
            var moved = dt > 0f ? (orb.Shown - before) / dt : 0f;
            orb.SloshSpeed += (-orb.Slosh * 40f - orb.SloshSpeed * 4f) * dt + moved * 2.5f;
            orb.Slosh = Mathf.Clamp(orb.Slosh + orb.SloshSpeed * dt, -1f, 1f);
            if (!draw || !orb.Material || !orb.Texture) return;
            orb.Material.SetFloat("_Fill", orb.Shown);
            orb.Material.SetFloat("_Trail", orb.Trail);
            orb.Material.SetFloat("_Slosh", orb.Slosh);
            orb.Material.SetFloat("_Clock", Time.unscaledTime);
            Graphics.Blit(null, orb.Texture, orb.Material);
            orb.Sphere?.MarkDirtyRepaint();
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

        // ---- The orbs' art, drawn once ---------------------------------------------------------------------

        // ---- Showing ----------------------------------------------------------------------------------------

        private void Changed()
        {
            var hero = HeroController.Active;
            SetHeroMode(hero);
            if (!bound) return;
            root.style.display = hero ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hero) { inventory?.Close(); return; }
            styleIcon.text = HeroSkill.StyleIcon(hero.Style);
            styleName.text = hero.StyleName + "  " + hero.StyleLevel.ToString(CultureInfo.InvariantCulture);
            styleBox.tooltip = Localization.UI_HERO_STYLE_SWITCH;
            foreach (HeroStyle st in System.Enum.GetValues(typeof(HeroStyle))) styleBox.EnableInClassList("rf-hero-style--" + st.ToString().ToLowerInvariant(), st == hero.Style);
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
            nextParty = 0f;
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

        private void ToggleInventory()
        {
            var hero = HeroController.Active;
            if (bound && hero && inventory != null) inventory.Toggle(hero.Player);
        }

        private void Flash(int slot, string why)
        {
            if (slot == -3) { styleFlashUntil = Time.time + 0.3f; return; }
            var index = slot >= 0 ? slot : slot == -1 ? 4 : 5;
            if (index < slots.Length && slots[index] != null) slots[index].FlashUntil = Time.time + 0.3f;
        }

        private void Update()
        {
            if (bound && root.panel == null) bound = false;
            if (!bound) TryBind();
            var hero = HeroController.Active;
            if (!bound || !hero || !hero.Player) return;
            inventory?.Update(hero.Player);
            loot?.Update();
            if (Time.unscaledTime >= nextBuffs)
            {
                nextBuffs = Time.unscaledTime + 0.5f;
                DrawBuffs(hero.Player);
            }
            waves?.Update();
            helpers?.Update(hero);
            if (inventory != null && inventory.IsOpen && Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping)
            {
                EscapeKey.Claim();
                inventory.Close();
            }

            var life = hero.Player.Stats.Health;
            var share = life.MaxLevel > 0 ? Mathf.Clamp01(life.CurrentValue / (float)life.MaxLevel) : 0f;
            var game = GameManager.Instance;
            var draw = !(game && game.PotatoMode) || ++orbFrame % 3 == 0;
            DrawOrb(health, share, draw);
            DrawOrb(mana, hero.Mana / hero.MaxMana, draw);
            healthText.text = ((int)life.CurrentValue).ToString(CultureInfo.InvariantCulture) + " / " + ((int)life.MaxLevel).ToString(CultureInfo.InvariantCulture);
            manaText.text = Mathf.FloorToInt(hero.Mana).ToString(CultureInfo.InvariantCulture) + " / " + Mathf.RoundToInt(hero.MaxMana).ToString(CultureInfo.InvariantCulture);

            styleBox.EnableInClassList("rf-hero-style--flash", Time.time < styleFlashUntil);
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
            Show(slots[4], false, dodge / hero.RollCooldown, dodge > 0f ? dodge.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty, false);
            // the hero's own potions, and the healing draughts in the Ravebridge bag behind them
            var spare = RavebridgeLoot.PotionsInBag(hero.Player);
            Show(slots[5], hero.Potions <= 0 && spare <= 0, 0f, hero.Potions + "/" + hero.MaxPotions + (spare > 0 ? " +" + spare : string.Empty), false);

            if (Time.time >= nextParty)
            {
                nextParty = Time.time + 0.5f;
                UpdateParty(hero);
            }
        }

        /// <summary>Who is on Ravebridge besides the hero, and how they are doing.</summary>
        private void UpdateParty(HeroController hero)
        {
            var game = GameManager.Instance;
            var shown = 0;
            var total = 0;
            if (game && game.Players != null)
            {
                foreach (var p in game.Players.GetAllPlayers())
                {
                    if (!p || !p.InRavebridge || p == hero.Player) continue;
                    total++;
                    if (shown >= partyRows.Count) continue;
                    var row = partyRows[shown++];
                    row.Box.style.display = DisplayStyle.Flex;
                    row.Name.text = p.Name;
                    var h = p.Stats.Health;
                    var share = h.MaxLevel > 0 ? Mathf.Clamp01(h.CurrentValue / (float)h.MaxLevel) : 0f;
                    row.Fill.style.width = new Length(share * 100f, LengthUnit.Percent);
                    row.Box.EnableInClassList("rf-hero-party__row--down", p.Stats.IsDead);
                }
            }
            for (var i = shown; i < partyRows.Count; i++) partyRows[i].Box.style.display = DisplayStyle.None;
            partyTitle.text = string.Format(CultureInfo.InvariantCulture, total > shown ? Localization.UI_HERO_PARTY_MORE : Localization.UI_HERO_PARTY, total, total - shown);
            party.style.display = total > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
