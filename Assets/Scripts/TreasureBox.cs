using RavenNest.Models;
using Shinobytes.Linq;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class TreasureBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private VisualEffect smokeEffect;
    [SerializeField] private VisualEffect coinEffect;
    [SerializeField] private SyntyPlayerAppearance playerAppereance;

    [SerializeField] private ChestObject[] chests;
    [SerializeField] private float smokeEffectDuration = 1f;
    [SerializeField] private float destroySpawnedItemsAfter = 1.25f;


    private float dirForce = 0.75f;
    private float force = 7.5f;

    private float intervalBetweenItemSpawn = 0.3f;

    private float smokeEffectTimer = 0;
    private ChestObject seletedChest;
    private long coinsAmount;
    private IReadOnlyList<Item> items;

    private List<GameObject> instantiatedItems = new List<GameObject>();
    private Dictionary<string, GameObject> loadedItems = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Awake()
    {
        smokeEffect.Stop();

        coinEffect.Stop();
        coinEffect.resetSeedOnPlay = true;

        if (!animator) animator = GetComponent<Animator>();
        SelectRandomChest();
    }
    private void Update()
    {
        if (smokeEffectTimer > 0)
        {
            smokeEffectTimer -= Time.deltaTime;
            if (smokeEffectTimer < 0)
            {
                smokeEffect.Stop();
            }
        }
    }

    private void SelectRandomChest()
    {
        if (chests == null || chests.Length == 0)
        {
            AssignChests();
        }

        var identity = Quaternion.identity;
        foreach (var chest in chests)
        {
            chest.Chest.SetActive(false);
            chest.Lid.transform.localRotation = identity;
        }

        seletedChest = chests.Random();
        seletedChest.Chest.SetActive(true);
    }

    public void Open(long coins, IReadOnlyList<Item> items)
    {
        this.coinsAmount = coins;
        this.items = items;

        SelectRandomChest();
        animator.Rebind();
        animator.Update(0f);
        animator.SetTrigger("Open");

        // TODO:
        // spawn items here, deactivate the items
        // then activate and play the effect as SpawnTreasureEffects is called.
    }

    public void PlaySmokeEffect()
    {
        smokeEffect.resetSeedOnPlay = true;
        smokeEffect.playRate = 1f;
        smokeEffect.Play();
        smokeEffectTimer = smokeEffectDuration;
    }

    public void SpawnTreasureEffects()
    {
        UnityEngine.Debug.Log("Start Coin Effect");
        coinEffect.Play();

        StartCoroutine(SpawnItems());
    }

    private IEnumerator SpawnItems()
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (!string.IsNullOrEmpty(item.GenericPrefab))
            {
                InstantiateItem(item, item.GenericPrefab);
            }
            else if (!string.IsNullOrEmpty(item.FemalePrefab))
            {
                InstantiateItem(item, item.FemalePrefab);
            }
            else if (!string.IsNullOrEmpty(item.MalePrefab))
            {
                InstantiateItem(item, item.MalePrefab);
            }
            else
            {
                // 
                if (playerAppereance && item != null)
                {
                    var syntyItem = playerAppereance.Get(item.Type, item.Material, item.MaleModelId);
                    if (syntyItem)
                    {
                        syntyItem.transform.SetParent(transform);
                        syntyItem.name = item.Name;
                        syntyItem.AddComponent<ItemHolder>().Item = item;
                        ReplaceSkinnedMeshRenderer(syntyItem);
                        AddRigidbody(syntyItem);
                        if (destroySpawnedItemsAfter > 0)
                            Destroy(syntyItem, destroySpawnedItemsAfter);
                    }
                }
            }

            yield return new WaitForSeconds(intervalBetweenItemSpawn);
        }
    }

    private void ReplaceSkinnedMeshRenderer(GameObject obj)
    {
        var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skinnedMeshRenderers.Length == 0) return;
        foreach (var sr in skinnedMeshRenderers)
        {
            sr.bones = null;
            // replace skinned mesh renderers with meshfilter and meshrenderer
            var targetObj = sr.gameObject;
            var mesh = sr.sharedMesh;
            var material = sr.materials;

            Destroy(sr);

            var meshFilter = targetObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = targetObj.AddComponent<MeshRenderer>();
            meshRenderer.materials = material;
        }
        obj.transform.localScale = Vector3.one * 0.01f;
    }

    private void AddRigidbody(GameObject obj)
    {
        obj.transform.localPosition = Vector3.up * 0.25f;
        var rb = obj.GetComponent<Rigidbody>();
        if (!rb) rb = obj.AddComponent<Rigidbody>();
        obj.transform.localRotation = UnityEngine.Random.rotation;
        // add an impulse to shoot up in a direction
        // but with a slight random x and z
        var x = UnityEngine.Random.Range(-dirForce, dirForce);
        var y = force;
        var z = UnityEngine.Random.Range(-dirForce, dirForce);
        var forceVector = new Vector3(x, y, z);
        rb.AddForce(forceVector, ForceMode.Impulse);
    }

    public void EndTreasureEffects()
    {
        UnityEngine.Debug.Log("Stop Coin Effect");
        coinEffect.Stop();
    }

    private void InstantiateItem(Item item, string prefab)
    {
        if (!loadedItems.TryGetValue(prefab, out var itemObj))
        {
            itemObj = loadedItems[prefab] = UnityEngine.Resources.Load<GameObject>(prefab);
        }

        InstantiateItem(item, itemObj);
    }

    private void InstantiateItem(Item item, UnityEngine.GameObject itemObj)
    {
        var i = Instantiate(itemObj, transform);
        i.name = item.Name;
        i.AddComponent<ItemHolder>().Item = item;
        AddRigidbody(i);
        if (destroySpawnedItemsAfter > 0)
            Destroy(i, destroySpawnedItemsAfter);
        //this.instantiatedItems.Add(i);
    }

    [Button("Play Animation")]
    private void PlayOpenAnimation()
    {
        var repo = JsonBasedItemRepository.Instance;
        if (repo == null)
        {
            repo = new JsonBasedItemRepository();
            repo.Load();
        }

        var items = repo.GetItems();
        var drops = items.TakeRandom(UnityEngine.Random.Range(3, 10));
        Open(100_000, drops);
    }

    [Button("Assign Chests")]
    private void AssignChests()
    {
        var c = new List<ChestObject>();
        foreach (Transform child in this.transform)
        {
            if (child.name.Contains("chest", StringComparison.OrdinalIgnoreCase))
            {
                c.Add(new ChestObject
                {
                    Chest = child.gameObject,
                    Lid = child.Find("Lid").gameObject
                });
            }
        }
        chests = c.ToArray();
    }

}

[Serializable]
public class ChestObject
{
    public GameObject Chest;
    public GameObject Lid;
}
