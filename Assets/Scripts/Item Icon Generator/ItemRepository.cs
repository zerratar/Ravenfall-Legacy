using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using RavenNest.Models;
using UnityEngine;

public class ItemRepository : MonoBehaviour
{
    [SerializeField] private SyntyPlayerAppearance playerAppereance;

    [SerializeField] private ItemRenderer[] renderCameras;

    //[SerializeField] private ItemRenderer irHead;
    //[SerializeField] private ItemRenderer irChest;
    //[SerializeField] private ItemRenderer irChest;

    //[SerializeField] private ItemRenderer irMedium;
    //[SerializeField] private ItemRenderer irLow;
    //[SerializeField] private ItemRenderer irWeapon;
    //[SerializeField] private ItemRenderer irPet;

    private Item[] items;
    private bool itemsSpawned;
    private int activeItemIndex = -1;
    private ItemRenderer oldCamera;

    // Start is called before the first frame update
    void Start()
    {
        var json = System.IO.File.ReadAllText(@"C:\git\Ravenfall-Legacy\Data\Repositories\items.json");
        items = JsonConvert.DeserializeObject<Item[]>(json);

        if (renderCameras == null || renderCameras.Length == 0)
        {
            renderCameras = FindObjectsOfType<ItemRenderer>();
        }

        foreach (var item in renderCameras)
        {
            item.gameObject.SetActive(false);
        }

        SpawnItems();
    }

    private void SpawnItems()
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.GenericPrefab))
            {
                var itemObj = UnityEngine.Resources.Load<GameObject>(item.GenericPrefab);
                InstantiateItem(item, itemObj);
            }
            else if (!string.IsNullOrEmpty(item.FemalePrefab))
            {
                Debug.Log("Instantiate female prefab: " + item.FemalePrefab);
                var itemObj = UnityEngine.Resources.Load<GameObject>(item.GenericPrefab);
                InstantiateItem(item, itemObj);
            }
            else if (!string.IsNullOrEmpty(item.MalePrefab))
            {
                Debug.Log("Instantiate male prefab: " + item.MalePrefab);
                var itemObj = UnityEngine.Resources.Load<GameObject>(item.GenericPrefab);

                //AssetPreview.GetAssetPreview

                InstantiateItem(item, itemObj);
            }
            else
            {
                var syntyItem = playerAppereance.Get(item.Type, item.Material, item.MaleModelId);
                if (syntyItem)
                {
                    syntyItem.transform.SetParent(transform);
                    syntyItem.name = item.Name;
                    syntyItem.AddComponent<ItemHolder>().Item = item;
                }
            }
        }

        StartCoroutine(DeactivatePlayerAppearance());
    }

    private void InstantiateItem(Item item, GameObject itemObj)
    {
        var i = Instantiate(itemObj, transform);
        i.name = item.Name;
        i.AddComponent<ItemHolder>().Item = item;
    }

    private IEnumerator DeactivatePlayerAppearance()
    {
        yield return new WaitForSeconds(0.5f);

        playerAppereance.gameObject.SetActive(false);
        itemsSpawned = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!itemsSpawned) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateNextItem();
        }
    }

    private void ActivateNextItem()
    {
        if (activeItemIndex == -1)
        {
            for (var i = 1; i < this.transform.childCount; ++i)
            {
                this.transform.GetChild(i).gameObject.SetActive(false);
            }

            activeItemIndex = 0;

            SetCameraTarget(transform.GetChild(activeItemIndex));

            return;
        }

        transform.GetChild(activeItemIndex).gameObject.SetActive(false);
        activeItemIndex = (activeItemIndex + 1) % transform.childCount;
        var nextItem = transform.GetChild(activeItemIndex);
        nextItem.gameObject.SetActive(true);
        SetCameraTarget(nextItem);
    }

    private void SetCameraTarget(Transform transform)
    {
        Debug.Log("setting itemRenderer parent to " + transform.name);

        var item = transform.GetComponent<ItemHolder>().Item;
        var camera = renderCameras.FirstOrDefault(x => x.Item == transform.name);

        if (!camera)
        {
            camera = renderCameras.FirstOrDefault(x => x.Type == item.Type && string.IsNullOrEmpty(x.Item));
        }

        if (!camera)
        {
            camera = renderCameras.FirstOrDefault(x => x.Category == item.Category && string.IsNullOrEmpty(x.Item));
        }

        if (camera)
        {
            SnapPicture(camera, item);
        }
    }

    private void SnapPicture(ItemRenderer newCamera, Item item)
    {
        if (oldCamera)
        {
            oldCamera.gameObject.SetActive(false);
        }

        newCamera.gameObject.SetActive(true);
        oldCamera = newCamera;

        var targetTexture = new RenderTexture(512, 512, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var camera = newCamera.GetComponent<Camera>();
        camera.targetTexture = targetTexture;

        StartCoroutine(SaveRenderTexture(targetTexture, "C:\\Item Icons\\" + item.Id + ".png"));
    }

    public IEnumerator SaveRenderTexture(RenderTexture rt, string pngOutPath)
    {
        //yield return new WaitForSeconds(0.1f);

        yield return null;

        var oldRT = RenderTexture.active;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);

        if (rt.sRGB)
        {
            Debug.Log("WUP WUP! sRGB!");
        }
        else
        {
            Debug.LogError("No sRGB :<");
        }

        RenderTexture.active = rt;
        //tex.alphaIsTransparency = true;

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(pngOutPath, tex.EncodeToPNG());
        RenderTexture.active = oldRT;

        if (activeItemIndex < transform.childCount - 1)
        {
            ActivateNextItem();
        }
    }

    private static Vector3 GetCenterPoint(Bounds[] bounds)
    {
        Vector3 targetLookatPosition;
        var minX = bounds.Min(x => x.min.x);
        var minY = bounds.Min(x => x.min.y);
        var minZ = bounds.Min(x => x.min.z);

        var maxX = bounds.Max(x => x.max.x);
        var maxY = bounds.Max(x => x.max.y);
        var maxZ = bounds.Max(x => x.max.z);

        var centerX = (maxX - minX) / 2f;
        var centerY = (maxY - minY) / 2f;
        var centerZ = (maxZ - minZ) / 2f;

        targetLookatPosition = new Vector3(centerX, centerY, centerZ);
        return targetLookatPosition;
    }
}


public class ItemHolder : MonoBehaviour
{
    public Item Item { get; internal set; }
}

public class Test2
{

}

public class Test
{

    public static implicit operator bool(Test obj)
    {
        return obj != null;
    }

    public static implicit operator Test2(Test obj)
    {
        return new Test2();
    }
    public static explicit operator string(Test obj)
    {
        return "";
    }
}