using UnityEngine;

/// <summary>
/// Grass on a terrain: put it on the terrain (or point it at one) with a <see cref="GrassProfile"/>, and
/// <see cref="GrassRenderer"/> grows grass around the camera wherever the terrain's layers say so, in the editor
/// as well as in play mode. Nothing per blade is saved; the same ground always grows the same grass.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GrassField : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private GrassProfile profile;

    public Terrain Terrain => terrain ? terrain : GetComponent<Terrain>();

    public GrassProfile Profile
    {
        get => profile;
        set
        {
            profile = value;
            GrassRenderer.Regrow();
        }
    }

    private void OnEnable()
    {
        GrassRenderer.Register(this);
        TerrainCallbacks.heightmapChanged += HeightsChanged;
        TerrainCallbacks.textureChanged += TexturesChanged;
    }

    private void OnDisable()
    {
        TerrainCallbacks.heightmapChanged -= HeightsChanged;
        TerrainCallbacks.textureChanged -= TexturesChanged;
        GrassRenderer.Unregister(this);
    }

    // the ground was reshaped or repainted (village build mode, the terrain tools): grow that part again
    private void HeightsChanged(Terrain changed, RectInt heightRegion, bool synched)
    {
        if (changed == Terrain && synched) GrassRenderer.GroundChanged(this);
    }

    private void TexturesChanged(Terrain changed, string textureName, RectInt texelRegion, bool synched)
    {
        if (changed == Terrain && synched && textureName == TerrainData.AlphamapTextureName) GrassRenderer.GroundChanged(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        GrassRenderer.Regrow();
    }
#endif
}
