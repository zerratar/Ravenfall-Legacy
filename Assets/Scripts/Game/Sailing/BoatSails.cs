using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// A ship's sails taken in and set again (docs/own-boats.md, "Sails in port"). Pirate Realm's masts carry each sail
/// twice: set ("..._Down_01", skinned, with a blend shape that raises it) and furled ("..._Tied_01"); the jibs and the
/// spanker come the same way as whole pieces, the furled one beside the set one ("..._Tied_01"). Taking in raises the
/// set sails on their blend shape, then shows the furled ones in their place; setting them runs the other way. The
/// furled sails wear the look's sailcloth without the kit's wind (<see cref="Still"/>): they are lashed to their yards.
/// </summary>
public sealed class BoatSails : MonoBehaviour
{
    /// <summary>How long taking in or setting the sails takes.</summary>
    private const float Seconds = 2.5f;
    /// <summary>How far along taking in the furled sails replace the raised ones (and the jibs and spanker swap).</summary>
    private const float SwapAt = 0.75f;

    private struct Pair
    {
        public Renderer Set;
        public Renderer Furled;
        /// <summary>The set sail's raising blend shape; null for the jibs and the spanker, which only swap.</summary>
        public SkinnedMeshRenderer Skinned;
    }

    private static readonly Regex TiedSuffix = new Regex(@"_Tied(_\d+)?$");
    private static readonly Dictionary<Material, Material> still = new Dictionary<Material, Material>();

    private readonly List<Pair> pairs = new List<Pair>();
    private float amount; // 0 set .. 1 furled
    private bool furl;

    public bool Furled => furl;

    /// <summary>
    /// Finds a rebuilt ship's sails, keeping how far in they were. Run before the look's sails are painted: the furled
    /// ones are switched on (their renderers hidden) so they get the look too.
    /// </summary>
    public static BoatSails Collect(GameObject boat, Transform model)
    {
        var sails = boat.GetComponent<BoatSails>();
        if (!sails) sails = boat.AddComponent<BoatSails>();
        sails.pairs.Clear();
        foreach (var furled in model.GetComponentsInChildren<Renderer>(true))
        {
            if (!furled.name.Contains("_Tied") || !furled.transform.parent) continue;
            var parent = furled.transform.parent;
            // the sails on the yards: "_Sail_Tied_01" beside "_Sail_Down_01"; the jibs and the spanker: "X_Tied_01" beside "X"
            var set = Sibling(parent, furled.name.Replace("_Tied", "_Down")) ?? Sibling(parent, TiedSuffix.Replace(furled.name, ""));
            if (!set || set == furled) continue;
            furled.gameObject.SetActive(true);
            var skinned = set as SkinnedMeshRenderer;
            sails.pairs.Add(new Pair
            {
                Set = set,
                Furled = furled,
                Skinned = skinned && skinned.sharedMesh && skinned.sharedMesh.blendShapeCount > 0 ? skinned : null,
            });
        }
        sails.Show();
        return sails;
    }

    private static Renderer Sibling(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; ++i)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child.GetComponent<Renderer>();
        }
        return null;
    }

    /// <summary>The look's sailcloth for a furled sail: the same, with the kit's wind turned off (made once per cloth).</summary>
    public static Material Still(Material sails)
    {
        if (!sails) return null;
        if (still.TryGetValue(sails, out var copy) && copy) return copy;
        copy = new Material(sails) { name = sails.name + " (still)" };
        foreach (var wind in new[] { "_ENABLE_NORMAL_MAP_WIND", "_Use_Vertex_Color_Wind", "_Enable_Breeze", "_Enable_Light_Wind", "_Enable_Strong_Wind", "_Breeze_Strength", "_Light_Wind_Strength", "_Strong_Wind_Strength" })
            if (copy.HasProperty(wind)) copy.SetFloat(wind, 0f);
        copy.DisableKeyword("_ENABLE_NORMAL_MAP_WIND");
        still[sails] = copy;
        return copy;
    }

    /// <summary>Takes the sails in (true) or sets them (false), over a couple of seconds.</summary>
    public void Furl(bool furled) => furl = furled;

    /// <summary>At once, as when the ship comes alongside a dock to be boarded.</summary>
    public void Snap(bool furled)
    {
        furl = furled;
        amount = furled ? 1f : 0f;
        Show();
    }

    private void Update()
    {
        var target = furl ? 1f : 0f;
        if (amount == target) return;
        amount = Mathf.MoveTowards(amount, target, GameTime.deltaTime / Seconds);
        Show();
    }

    private void Show()
    {
        var raised = Mathf.Clamp01(amount / SwapAt) * 100f;
        var swapped = amount >= SwapAt;
        foreach (var p in pairs)
        {
            if (!p.Set || !p.Furled) continue;
            if (p.Skinned) p.Skinned.SetBlendShapeWeight(0, raised);
            p.Set.enabled = !swapped;
            p.Furled.enabled = swapped;
        }
    }
}
