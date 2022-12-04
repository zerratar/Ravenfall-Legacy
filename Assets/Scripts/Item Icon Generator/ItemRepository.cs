using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RavenNest.Models;
using UnityEditor;
using UnityEditor.Experimental;
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

    public bool UseLegacyScreenshots = true;
    public bool AutoNextItem = true;

    public Texture2D[] AssetPreviews;
    private List<Texture2D> assetPreviews = new List<Texture2D>();
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.antiAliasing = 0;

        var itemsRepo = @"C:\git\Ravenfall Legacy\Data\Repositories\items.json";

        System.Net.WebClient cl = new System.Net.WebClient();
        try
        {
            cl.DownloadFile("https://www.ravenfall.stream/api/items", itemsRepo);
            Shinobytes.Debug.Log("Downloaded new items repo");
        }
        catch { }

        var json = System.IO.File.ReadAllText(itemsRepo);

        //var json = System.IO.File.ReadAllText(@"C:\git\Ravenfall Legacy\Data\Repositories\items.json");
        items = JsonConvert.DeserializeObject<Item[]>(json);

        if (renderCameras == null || renderCameras.Length == 0)
        {
            renderCameras = FindObjectsOfType<ItemRenderer>();
        }



        //foreach (var item in renderCameras)
        //{
        //    item.gameObject.SetActive(false);
        //}

        StartCoroutine(SpawnItems());
    }

    private IEnumerator SpawnItems()
    {

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.GenericPrefab))
            {
                yield return StartCoroutine(SavePrefabTexture(item, item.GenericPrefab));
            }
            else if (!string.IsNullOrEmpty(item.FemalePrefab))
            {
                yield return StartCoroutine(SavePrefabTexture(item, item.FemalePrefab));

            }
            else if (!string.IsNullOrEmpty(item.MalePrefab))
            {
                yield return StartCoroutine(SavePrefabTexture(item, item.MalePrefab));
                //#if UNITY_EDITOR
                //                SaveTexture(AssetPreview.GetAssetPreview(itemObj), "C:\\Item Icons\\" + item.Id + ".png");
                //#endif
                //InstantiateItem(item, itemObj);
            }
            else
            {
                if (playerAppereance && item != null)
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
        }
        AssetPreviews = assetPreviews.ToArray();
        yield return DeactivatePlayerAppearance();
    }

    private IEnumerator SavePrefabTexture(Item item, string prefab)
    {
        var itemObj = UnityEngine.Resources.Load<GameObject>(prefab);
        if (!itemObj)
        {
            Shinobytes.Debug.LogError(prefab + " does not exist!");
            yield break;
        }

        if (UseLegacyScreenshots)
        {
            InstantiateItem(item, itemObj);
            yield break;
        }
#if UNITY_EDITOR
        var prev = AssetPreview.GetMiniThumbnail(itemObj);

        //var prevT = (Texture)prev;
        AssetPreview.SetPreviewTextureCacheSize(1024);

        if (!prev.isReadable)
        {
            //prev = Texture2D.CreateExternalTexture(prevT.width, prevT.height, TextureFormat.RGBA32, false, false, prevT.GetNativeTexturePtr());
            //var pixels = ((Texture2D)prevT).GetPixels(0, 0, prev.width, prev.height, 0);

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(itemObj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Shinobytes.Debug.Log("Asset Path: " + assetPath);
                var icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
                if (icon != null)
                {
                    prev = icon;
                }
            }

            if (!prev.isReadable || prev.width == 0)
            {
                Shinobytes.Debug.LogWarning("MiniThumbnail for " + itemObj.name + " is not readable. Using GetAssetPreview");
                var instanceId = itemObj.GetInstanceID();

                do
                {
                    yield return new WaitForSeconds(0.01f);
                    prev = AssetPreview.GetAssetPreview(itemObj);
                } while (AssetPreview.IsLoadingAssetPreview(instanceId) || prev == null);

            }
        }

        assetPreviews.Add(prev);

        SaveTexture(prev, "C:\\Item Icons\\" + item.Id + ".png");
        //InstantiateItem(item, itemObj);

#endif
    }

    private void InstantiateItem(Item item, UnityEngine.GameObject itemObj)
    {
        var i = Instantiate(itemObj, transform);
        i.name = item.Name;
        i.AddComponent<ItemHolder>().Item = item;
    }

    private IEnumerator DeactivatePlayerAppearance()
    {
        yield return new WaitForSeconds(0.5f);
        if (playerAppereance)
        {
            playerAppereance.gameObject.SetActive(false);
        }
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
        var item = transform.GetComponent<ItemHolder>().Item;
        var camera = renderCameras.FirstOrDefault(x => x.Item == transform.name);
        var isGenericItem = !string.IsNullOrEmpty(item.GenericPrefab);

        if (!camera)
        {
            camera = renderCameras.FirstOrDefault(x => (x.IsGenericItem ? isGenericItem : true) && x.Type == item.Type && string.IsNullOrEmpty(x.Item));
        }

        if (!camera)
        {
            camera = renderCameras.FirstOrDefault(x => (x.IsGenericItem ? isGenericItem : true) && x.Category == item.Category && string.IsNullOrEmpty(x.Item));
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


        Shinobytes.Debug.Log("Taking snap of " + item.Name + " using " + newCamera.name);


        newCamera.gameObject.SetActive(true);
        oldCamera = newCamera;

        var targetTexture = new RenderTexture(512, 512, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        targetTexture.name = item.Name;

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

        //if (rt.sRGB)
        //{
        //    Shinobytes.Debug.Log("WUP WUP! sRGB!");
        //}
        //else
        //{
        //    Shinobytes.Debug.LogError("No sRGB :<");
        //}

        RenderTexture.active = rt;
        //tex.alphaIsTransparency = true;

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(pngOutPath, tex.EncodeToPNG());
        RenderTexture.active = oldRT;

        if (AutoNextItem && activeItemIndex < transform.childCount - 1)
        {
            ActivateNextItem();
        }
    }

    private void SaveTexture(Texture2D tex, string pngOutPath)
    {
        if (tex == null)
        {
            Shinobytes.Debug.LogError("Saving " + pngOutPath + " failed. Texture not ready");
            return;
        }

        var isNew = !System.IO.File.Exists(pngOutPath);

        try
        {
            Shinobytes.Debug.Log("Saving " + pngOutPath + ", " + tex.name + ", " + tex.width + "x" + tex.height);
            var pngData = tex.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                Shinobytes.Debug.LogError("Saving " + pngOutPath + " failed. EncodeToPNG returned empty data");
                return;
            }

            System.IO.File.WriteAllBytes(pngOutPath, pngData);
            if (isNew)
            {
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pngOutPath), "new", System.IO.Path.GetFileName(pngOutPath)), pngData);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(pngOutPath + " could not be saved. " + exc.Message);
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