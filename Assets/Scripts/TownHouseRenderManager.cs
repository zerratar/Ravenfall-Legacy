using System.Collections.Generic;
using UnityEngine;

public class TownHouseRenderManager : MonoBehaviour
{
    [SerializeField] private float offset = 10;
    [SerializeField] private GameObject townHouseRendererPrefab;
    [SerializeField] private Vector3 rendererOffset = new Vector3(0, 0, 14.3f);

    private List<TownHouseRenderer> houseRenderers = new List<TownHouseRenderer>();
    private List<GameObject> instantiated = new List<GameObject>();

    private void FixedUpdate()
    {
        foreach (var renderer in houseRenderers)
        {
            renderer.transform.localPosition = rendererOffset + renderer.TownHouse.CameraOffset;
        }
    }

    internal RenderTexture CreateHouseRender(TownHouse townHouse)
    {
        var houseObject = Instantiate(townHouse.Prefab, transform);

        houseObject.transform.localPosition = instantiated.Count * offset * Vector3.left;

        var houseRenderer = Instantiate(townHouseRendererPrefab, houseObject.transform);
        var renderer = houseRenderer.GetComponent<TownHouseRenderer>();
        houseRenderer.transform.localPosition = rendererOffset;

        houseRenderers.Add(renderer);
        instantiated.Add(houseObject);

        return renderer.PrepareRenderTexture(townHouse, instantiated.Count);
    }
}
