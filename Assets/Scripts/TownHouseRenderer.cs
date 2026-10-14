using UnityEngine;

/// <summary>
/// The camera that takes a building's picture for the building selection dialog. It does not run by itself: the
/// picture is taken when the dialog opens (<see cref="Render"/>), since the buildings stand still. A camera left on
/// would draw the whole pipeline again every frame for each building, culling and shadows included.
/// </summary>
public class TownHouseRenderer : MonoBehaviour
{
    [SerializeField] private Camera camera;

    private TownHouse townHouse;
    private RenderTexture renderTexture;

    public TownHouse TownHouse => townHouse;

    internal RenderTexture PrepareRenderTexture(TownHouse townHouse, int renderIndex)
    {
        this.townHouse = townHouse;
        renderTexture = Resources.Load<RenderTexture>($"BuildingRenderTextures/Building{renderIndex}");
        camera.targetTexture = renderTexture;
        camera.enabled = false;
        return renderTexture;
    }

    /// <summary>Takes the building's picture into its render texture.</summary>
    public void Render()
    {
        if (camera && camera.targetTexture) camera.Render();
    }
}
