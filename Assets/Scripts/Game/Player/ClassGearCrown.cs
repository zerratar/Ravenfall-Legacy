using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The crown (head attachment 7, Chr_HelmetAttachment_08) in a gear line's own metal and gem. The class gear puts it on
/// several lines' high tiers: the Mage's Wizard Hat and Hood from tier 6, the Healer's tier 6 Cowl and Cap, the
/// Melee's Coif and the Dark Knight's Horned Helm from tier 6, the Monk's from tier 6 and the Tank's tier 8 helms. A
/// part takes the colors of the piece's tier material, and with the Mage's and the Monk's tiers it came out the same
/// gold and white crown as the Tank's Aegis: side by side it read like a bug (the owner, 2026-11-27).
///
/// What the shader paints on the crown: the band <c>_Color_Metal_Primary</c>, its rims and points
/// <c>_Color_Metal_Secondary</c>, the gem in front <c>_Color_Secondary</c>. A line in the table below wears its crown
/// in those colors on every tier; the other lines keep their tier's own colors, which already are their own (the
/// Healer's Saint green, the Melee's bronze to black and red, the Dark Knight's blackened steel) or the gold the crown
/// is meant to be (the Tank). The website's figure has the same table (RavenNest wwwroot/js/rf-character-gear.js,
/// CROWNS): change both together.
/// </summary>
public static class ClassGearCrown
{
    /// <summary>The crown's index among the head attachments.</summary>
    public const int Attachment = 7;

    private struct Palette
    {
        public Color Band, Rims, Gem;
        public Palette(string band, string rims, string gem)
        {
            ColorUtility.TryParseHtmlString(band, out Band);
            ColorUtility.TryParseHtmlString(rims, out Rims);
            ColorUtility.TryParseHtmlString(gem, out Gem);
        }
    }

    private static readonly Dictionary<ItemLine, Palette> palettes = new Dictionary<ItemLine, Palette>
    {
        // silver, the gem the mint green of the Starcaller robe's glow
        { ItemLine.Mage, new Palette("#9EA8B6", "#E2E7EE", "#6CF0A4") },
        // antique bronze with a carnelian: the line's ochre, saffron and earth, not the Tank's gold
        { ItemLine.Monk, new Palette("#A8773F", "#6E4A26", "#E0642A") },
    };

    private static readonly int BandId = Shader.PropertyToID("_Color_Metal_Primary");
    private static readonly int RimsId = Shader.PropertyToID("_Color_Metal_Secondary");
    private static readonly int GemId = Shader.PropertyToID("_Color_Secondary");

    private static readonly Dictionary<Material, Material> crowns = new Dictionary<Material, Material>();

    /// <summary>
    /// The material a head attachment is worn in: <paramref name="tierMaterial"/> (the head piece's), or for the crown
    /// of a line in the table a copy of it in that line's crown colors (one copy per tier material, kept).
    /// </summary>
    public static Material For(int attachment, Material tierMaterial, ItemMaterial material)
    {
        if (attachment != Attachment || !tierMaterial) return tierMaterial;
        if (!ItemLines.IsClassGear(material) || !palettes.TryGetValue(ItemLines.Of(material), out var palette)) return tierMaterial;
        if (crowns.TryGetValue(tierMaterial, out var crown) && crown) return crown;

        crown = new Material(tierMaterial) { name = tierMaterial.name + " (Crown)" };
        crown.SetColor(BandId, palette.Band);
        crown.SetColor(RimsId, palette.Rims);
        crown.SetColor(GemId, palette.Gem);
        crowns[tierMaterial] = crown;
        return crown;
    }
}
