using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class TreeController : TaskObject
{
    private readonly ConcurrentDictionary<string, PlayerController> woodCutters
        = new ConcurrentDictionary<string, PlayerController>();

    private static GameObject stumpObject;

    [SerializeField] private GameObject[] trees;
    [SerializeField] private GameObject stump;
    [SerializeField] private float respawnTimeSeconds = 15f;
    [SerializeField] private int health = 4;
    [SerializeField] private int maxHealth = 4;

    public int Level = 1;

    public double Resource => 1;

    public IReadOnlyList<PlayerController> WoodCutters => woodCutters.Values.ToList();

    public bool IsStump => health <= 0;

    public Vector3 Position;
    public IslandController Island;

    [ReadOnly]
    public float MaxActionDistance = 5f;

    private float maxRespawnTimeSeconds;
    private float minRespawnTimeSeconds;
    private bool respawnTimeLocked;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    void Start()
    {
        maxRespawnTimeSeconds = respawnTimeSeconds;
        minRespawnTimeSeconds = 1f;
        Position = this.transform.position;
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }

    void Awake()
    {
        this.Island = GetComponentInParent<IslandController>();
        if (stump) stump.SetActive(false);
        foreach (var tree in trees) tree.SetActive(false);
        ActivateRandomTree();
    }

    public override void Poll()
    {
    }

    public void DecreaseRespawnTime()
    {
        if (respawnTimeSeconds <= minRespawnTimeSeconds || respawnTimeLocked)
        {
            return;
        }

        respawnTimeSeconds = Mathf.Clamp(respawnTimeSeconds - 1f, minRespawnTimeSeconds, maxRespawnTimeSeconds);
        respawnTimeLocked = true;
    }

    public void IncreaseRespawnTime()
    {
        if (respawnTimeSeconds >= maxRespawnTimeSeconds || respawnTimeLocked)
        {
            return;
        }

        respawnTimeSeconds = Mathf.Clamp(respawnTimeSeconds + 1f, minRespawnTimeSeconds, maxRespawnTimeSeconds);
        respawnTimeLocked = true;
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTimeSeconds);
        woodCutters.Clear();
        ActivateRandomTree();
        respawnTimeLocked = false;
    }

    private void ActivateRandomTree()
    {
        try
        {
            if (trees.Length == 0) return;
            if (stump) stump.SetActive(false);
            trees[Random.Range(0, trees.Length)].SetActive(true);
            health = maxHealth;
        }
        catch (System.Exception exception)
        {
            Shinobytes.Debug.LogError("TreeController.ActivateRandomTree: " + exception.ToString());
        }
    }

    private void CutDown()
    {
        foreach (var tree in trees)
        {
            tree.SetActive(false);
        }

        if (stump)
        {
            stump.SetActive(true);
        }

        StartCoroutine(Respawn());
    }

    public void AddWoodcutter(PlayerController player)
    {
        woodCutters[player.PlayerName] = player;
    }

    public bool DoDamage(PlayerController player, int amount)
    {
        if (health <= 0)
        {
            return false;
        }

        AddWoodcutter(player);

        health -= amount;
        if (health <= 0)
        {
            CutDown();
            return true;
        }

        return false;
    }

    public void Create()
    {
        this.Island = GetComponentInParent<IslandController>();

        if (trees == null || trees.Length == 0)
        {
            var t = new List<GameObject>();
            for (var i = 0; i < this.transform.childCount; ++i)
            {
                var child = this.transform.GetChild(i);
                if (child.name.ToLower() == "stump")
                {
                    continue;
                }

                t.Add(child.gameObject);
            }

            this.trees = t.ToArray();
        }

        if (!stump)
        {
            // create stump
            // easiest way? find another tree with a stump. instantiate it.

            var chunk = this.gameObject.GetComponentInParent<Chunk>();
            if (chunk)
            {
                var otherTrees = chunk.GetComponentsInChildren<TreeController>();
                foreach (var t in otherTrees)
                {
                    if (t.stump)
                    {
                        stumpObject = t.stump;
                        break;
                    }
                }
            }
            else
            {
                if (!stumpObject)
                {
                    var otherTrees = GameObject.FindObjectsByType<TreeController>(FindObjectsSortMode.None);
                    foreach (var t in otherTrees)
                    {
                        if (t.stump)
                        {
                            stumpObject = t.stump;
                            break;
                        }
                    }
                }
            }

            var lp = stumpObject.transform.localPosition;
            var lr = stumpObject.transform.localRotation;
            var ls = stumpObject.transform.localScale;

            stump = GameObject.Instantiate(stumpObject, this.transform);
            stump.transform.localScale = ls;
            stump.transform.SetLocalPositionAndRotation(lp, lr);
        }

        this.gameObject.EnsureComponent<SphereCollider>(x =>
        {
            x.isTrigger = true;
            x.radius = 3f;
        });

        // no need to assign difficulty, we will just use a fixed one for now.
        this.health = 30;
        this.maxHealth = 30;
        this.Level = 1000;
        this.MaxActionDistance = 6f;
        this.respawnTimeSeconds = 20;
    }
}