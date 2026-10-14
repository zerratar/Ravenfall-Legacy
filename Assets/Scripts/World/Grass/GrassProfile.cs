using System;
using UnityEngine;

/// <summary>
/// What grass grows where: how grassy each terrain layer is and what colour it has, and the kinds of grass (the
/// generated blade clumps, and Synty tufts and flowers as accents). A <see cref="GrassField"/> on the terrain uses
/// it; <see cref="GrassRenderer"/> grows and draws the grass.
/// </summary>
/// <remarks>
/// The layer colours are the terrain textures' average colours, read once by the field's inspector (the textures
/// are not readable while the game runs), so the grass takes the colour of the ground it stands on.
/// </remarks>
[CreateAssetMenu(menuName = "Ravenfall/Grass Profile", fileName = "Grass Profile")]
public class GrassProfile : ScriptableObject
{
    /// <summary>A terrain layer: how much grass grows on it and the colour it gives the grass.</summary>
    [Serializable]
    public class Layer
    {
        public TerrainLayer terrainLayer;
        [Tooltip("How much grass grows where the ground is all this layer; 0 for none.")]
        [Range(0f, 1f)] public float density = 1f;
        [Tooltip("The layer's colour, read from its texture.")]
        public Color colour = new Color(0.36f, 0.5f, 0.22f);
    }

    /// <summary>A kind of grass: a mesh and material, how many grow per square metre, and how big.</summary>
    [Serializable]
    public class Kind
    {
        public string name = "Blades";
        [Tooltip("The mesh; leave empty for a generated clump of blades.")]
        public Mesh mesh;
        [Tooltip("Blades in a generated clump.")]
        [Range(1, 16)] public int blades = 7;
        [Tooltip("Width of a generated blade at its root, in metres at size 1.")]
        public float bladeWidth = 0.05f;
        public Material material;
        [Tooltip("How many grow on a square metre of ground at full density.")]
        [Min(0f)] public float perSquareMetre = 5f;
        [Tooltip("Size of each, picked at random; a generated clump is 1 m tall at size 1.")]
        public Vector2 size = new Vector2(0.3f, 0.55f);
        [Tooltip("Only where the ground is at least this grassy: accents in the lush middle of a meadow, not at its edge.")]
        [Range(0f, 1f)] public float minDensity;
        public bool enabled = true;
    }

    public Layer[] layers = Array.Empty<Layer>();
    public Kind[] kinds = Array.Empty<Kind>();

    [Header("Where")]
    [Tooltip("Nothing grows below this height (the sea).")]
    public float waterLevel = -3f;
    [Tooltip("Ground steeper than this, in degrees, has no grass.")]
    [Range(0f, 90f)] public float maxSlope = 40f;

    [Header("Colour")]
    [Tooltip("Multiplied into the ground's colour. White keeps the ground's own colour, so snow grass is as white as the snow.")]
    public Color tint = Color.white;
    [Tooltip("How much the brightness changes from one clump to the next.")]
    [Range(0f, 0.3f)] public float colourJitter = 0.08f;
    [Tooltip("How strong the broad patches are, some lighter and some drier, that keep a meadow from being one flat colour.")]
    [Range(0f, 0.6f)] public float patches = 0.25f;

    /// <summary>Raised when the profile changes in the editor, so the grass grows again.</summary>
    public static event Action<GrassProfile> Changed;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Changed?.Invoke(this);
    }
#endif

    /// <summary>The layer settings for a terrain layer, or null when the profile has none.</summary>
    public Layer LayerFor(TerrainLayer terrainLayer)
    {
        foreach (var layer in layers) if (layer != null && layer.terrainLayer == terrainLayer) return layer;
        return null;
    }
}
