using UnityEngine;

public class TownHouseRenderer : MonoBehaviour
{
    [SerializeField] private Camera camera;

    private TownHouse townHouse;
    private RenderTexture renderTexture;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    internal RenderTexture PrepareRenderTexture(TownHouse townHouse, int renderIndex)
    {
        this.townHouse = townHouse;
        renderTexture = Resources.Load<RenderTexture>($"BuildingRenderTextures/Building{renderIndex}");
        camera.targetTexture = renderTexture;

        if (townHouse)
            transform.position = startPosition + townHouse.CameraOffset;

        return renderTexture;
    }
}
