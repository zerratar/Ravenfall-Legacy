using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Item icons made with <see cref="ImageGen"/>, in the style of the icons the game has: a new item's icon drawn after a
/// few existing icons, and a variant of an existing icon (cooked, burned, another metal). The result is shrunk to the
/// icons' 256 x 256, put in Assets/Resources/Items (not versioned) as a sprite, and copied to the website's item
/// pictures in the RavenNest repo when it is next to this one. The full size picture stays in Temp/ImageGen to look at.
/// </summary>
public static class IconForge
{
    public const string ItemsFolder = "Assets/Resources/Items";
    public const int Size = 256;

    /// <summary>Icons that show the house style well, used when none are given.</summary>
    public static readonly string[] DefaultStyle = { "raw-swordfish", "dragon-helmet", "red-wine" };

    private static readonly string[] WebsiteFolders =
    {
        "../RavenNest/src/RavenNest.Blazor/wwwroot/imgs/items",
        "../RavenNest/Publish/wwwroot/imgs/items",
    };

    private const string Style =
        "Match the reference images exactly in style: a hand painted game inventory icon, rich colours with soft painted " +
        "shading and highlights, a thin dark outline, shown at a slight angle, centred and filling most of the square, " +
        "no text, no border, no ground shadow, on a fully transparent background.";

    /// <summary>The file name an item's icon has (as the game looks it up): lower case, spaces to dashes, no apostrophes.</summary>
    public static string Slug(string itemName) => itemName.Replace(" ", "-").Replace("'", "").ToLowerInvariant();

    public static string IconPath(string slug) => ItemsFolder + "/" + slug + ".png";

    /// <summary>A new item's icon, drawn after the style icons (slugs of existing icons; three at most is plenty).</summary>
    public static ImageGen.Job NewIcon(string itemName, string description, string[] styleIcons = null, ImageGen.Model model = ImageGen.Model.Flare, Action<ImageGen.Job> then = null)
    {
        var slug = Slug(itemName);
        var refs = (styleIcons ?? DefaultStyle).Select(IconPath).Where(File.Exists).ToArray();
        var prompt = "A single item icon of \"" + itemName + "\"" + (string.IsNullOrWhiteSpace(description) ? "" : ": " + description.Trim()) +
                     ". The reference images are other icons from the same game, only for their style; draw the new item, not them. " + Style;
        var options = new ImageGen.Options { Model = model };
        var temp = "Temp/ImageGen/" + slug + ".png";
        return refs.Length > 0
            ? ImageGen.Edit(prompt, refs, temp, options, null, job => { Install(job, slug); then?.Invoke(job); })
            : ImageGen.Generate(prompt, temp, options, job => { Install(job, slug); then?.Invoke(job); });
    }

    /// <summary>
    /// An existing icon changed: "cooked: grilled golden brown with char marks", "burned: blackened and smoking",
    /// "made of gold". The framing, angle, outline and style stay. Sunburst keeps closest to the original.
    /// </summary>
    public static ImageGen.Job Variant(string fromSlug, string toItemName, string change, ImageGen.Model model = ImageGen.Model.Sunburst, Action<ImageGen.Job> then = null)
    {
        var slug = Slug(toItemName);
        var from = IconPath(fromSlug);
        var prompt = "The same icon as the image, with one change: " + change.Trim() + ". Keep everything else exactly as it " +
                     "is: the same item, framing, angle, size in the square, thin dark outline and hand painted style, on a " +
                     "fully transparent background. No text.";
        return ImageGen.Edit(prompt, new[] { from }, "Temp/ImageGen/" + slug + ".png", new ImageGen.Options { Model = model }, null,
            job => { Install(job, slug); then?.Invoke(job); });
    }

    /// <summary>Shrinks a finished picture to an icon, saves it as the item's sprite and copies it to the website.</summary>
    public static void Install(ImageGen.Job job, string slug)
    {
        if (job.State != ImageGen.JobState.Done || !File.Exists(job.OutPath)) return;
        var png = Shrink(File.ReadAllBytes(job.OutPath), Size);
        Directory.CreateDirectory(ItemsFolder);
        var path = IconPath(slug);
        File.WriteAllBytes(path, png);
        foreach (var folder in WebsiteFolders)
        {
            if (Directory.Exists(folder)) File.WriteAllBytes(Path.Combine(folder, slug + ".png"), png);
        }
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Debug.Log("Image generator: icon " + slug + " made (" + job.Id + ")");
    }

    /// <summary>
    /// Down to size by averaging blocks of pixels, weighted by their alpha so transparent edges do not darken; the
    /// picture is cropped to a centred square first.
    /// </summary>
    public static byte[] Shrink(byte[] png, int size)
    {
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(png);
        var side = Math.Min(src.width, src.height);
        int ox = (src.width - side) / 2, oy = (src.height - side) / 2;
        var pixels = src.GetPixels32();
        var outPixels = new Color32[size * size];
        var scale = side / (float)size;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                int x0 = ox + (int)(x * scale), x1 = ox + Math.Max((int)((x + 1) * scale), (int)(x * scale) + 1);
                int y0 = oy + (int)(y * scale), y1 = oy + Math.Max((int)((y + 1) * scale), (int)(y * scale) + 1);
                double r = 0, g = 0, b = 0, a = 0; var n = 0;
                for (var sy = y0; sy < y1; sy++)
                {
                    for (var sx = x0; sx < x1; sx++)
                    {
                        var p = pixels[sy * src.width + sx];
                        var w = p.a / 255.0;
                        r += p.r * w; g += p.g * w; b += p.b * w; a += p.a; n++;
                    }
                }
                var total = a / 255.0;
                outPixels[y * size + x] = total > 0
                    ? new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), (byte)(a / n))
                    : new Color32(0, 0, 0, 0);
            }
        }
        var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
        dst.SetPixels32(outPixels);
        dst.Apply();
        var bytes = dst.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(src);
        UnityEngine.Object.DestroyImmediate(dst);
        return bytes;
    }

    /// <summary>The catalog's items (Data/Repositories/items.json) that have no icon in Assets/Resources/Items yet.</summary>
    public static List<string> MissingIcons()
    {
        var missing = new List<string>();
        var json = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText("Data/Repositories/items.json"));
        foreach (var item in json)
        {
            var name = (string)item["name"];
            if (!string.IsNullOrEmpty(name) && !File.Exists(IconPath(Slug(name)))) missing.Add(name);
        }
        return missing;
    }
}
