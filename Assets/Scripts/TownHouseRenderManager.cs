using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The buildings shown in the building selection dialog: a copy of each, set apart from the world, with a camera
/// that takes its picture. The copies are hidden and the cameras off except while <see cref="RenderAll"/> takes the
/// pictures, when the dialog opens; nothing of it is drawn or culled the rest of the time.
/// </summary>
public class TownHouseRenderManager : MonoBehaviour
{
    [SerializeField] private float offset = 10;
    [SerializeField] private GameObject townHouseRendererPrefab;
    [SerializeField] private Vector3 rendererOffset = new Vector3(0, 0, 14.3f);

    private List<TownHouseRenderer> houseRenderers = new List<TownHouseRenderer>();
    private List<GameObject> instantiated = new List<GameObject>();

    internal RenderTexture CreateHouseRender(TownHouse townHouse)
    {
        var houseObject = Instantiate(townHouse.Prefab, transform);

        houseObject.transform.localPosition = instantiated.Count * offset * Vector3.left;

        var houseRenderer = Instantiate(townHouseRendererPrefab, houseObject.transform);
        var renderer = houseRenderer.GetComponent<TownHouseRenderer>();
        houseRenderers.Add(renderer);
        instantiated.Add(houseObject);

        var texture = renderer.PrepareRenderTexture(townHouse, instantiated.Count);
        houseRenderer.transform.localPosition = rendererOffset + (townHouse ? townHouse.CameraOffset : Vector3.zero);
        houseObject.SetActive(false);
        return texture;
    }

    /// <summary>Takes every building's picture: the copies shown for the moment it takes, then hidden again.</summary>
    public void RenderAll()
    {
        foreach (var house in instantiated) if (house) house.SetActive(true);
        foreach (var renderer in houseRenderers) if (renderer) renderer.Render();
        foreach (var house in instantiated) if (house) house.SetActive(false);
    }
}
