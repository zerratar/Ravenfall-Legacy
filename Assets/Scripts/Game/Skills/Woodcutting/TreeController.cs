using Assets.Scripts;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeController : MonoBehaviour
{
    private readonly ConcurrentDictionary<string, PlayerController> woodCutters
        = new ConcurrentDictionary<string, PlayerController>();

    [SerializeField] private GameObject[] trees;
    [SerializeField] private GameObject stump;
    [SerializeField] private float respawnTimeSeconds = 15f;

    [SerializeField] private int health = 4;
    [SerializeField] private int maxHealth = 4;

    [SerializeField] private float treeShakeTime = 1f;
    [SerializeField] private float treeShakeTimer = 0;
    //[SerializeField] private float treeShakeRange = 2f;

    //private Quaternion startRotation;

    public int Level = 1;

    //public double Experience => GameMath.GetWoodcuttingExperience(Level);

    public double Resource => 1;

    public IReadOnlyList<PlayerController> WoodCutters => woodCutters.Values.ToList();

    public bool IsStump => health <= 0;

    public IslandController Island { get; private set; }

    // Start is called before the first frame update
    [ReadOnly]
    public float MaxActionDistance = 5f;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    void Start()
    {
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }

        //startRotation = transform.rotation;
    }

    void Awake()
    {

        this.Island = GetComponentInParent<IslandController>();

        if (stump) stump.SetActive(false);
        foreach (var tree in trees) tree.SetActive(false);
        ActivateRandomTree();
    }


    //// Update is called once per frame
    //void Update()
    //{
    //    if (GameCache.IsAwaitingGameRestore) return;
    //    if (treeShakeTimer > 0)
    //    {
    //        treeShakeTimer -= Time.deltaTime;
    //        transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-treeShakeRange / 2f, treeShakeRange / 2f));
    //        if (treeShakeTimer <= 0)
    //        {
    //            transform.rotation = startRotation;
    //        }
    //    }
    //}

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTimeSeconds);
        woodCutters.Clear();
        ActivateRandomTree();
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
            Shinobytes.Debug.LogError(exception.ToString());
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

        woodCutters[player.PlayerName] = player;

        health -= amount;
        if (health <= 0)
        {
            CutDown();
            return true;
        }

        if (treeShakeTimer <= 0f)
        {
            treeShakeTimer = treeShakeTime;
        }

        return false;
    }
}