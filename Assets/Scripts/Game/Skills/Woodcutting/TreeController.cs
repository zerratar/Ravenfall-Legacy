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
    [SerializeField] private float treeShakeTimer = 1f;
    [SerializeField] private float treeShakeRange = 2f;

    private Quaternion startRotation;

    public int Level = 1;

    public decimal Experience => GameMath.GetWoodcuttingExperience(Level);

    public double Resource => 1;

    public IReadOnlyList<PlayerController> WoodCutters => woodCutters.Values.ToList();

    public bool IsStump => health <= 0;

    // Start is called before the first frame update
    void Start()
    {
        startRotation = transform.rotation;
    }


    void Awake()
    {
        if (stump) stump.SetActive(false);
        foreach (var tree in trees) tree.SetActive(false);
        ActivateRandomTree();
    }


    // Update is called once per frame
    void Update()
    {
        if (treeShakeTimer > 0)
        {
            treeShakeTimer -= Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-treeShakeRange / 2f, treeShakeRange / 2f));
            if (treeShakeTimer <= 0)
            {
                transform.rotation = startRotation;
            }
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTimeSeconds);
        woodCutters.Clear();
        ActivateRandomTree();
    }

    private void ActivateRandomTree()
    {
        if (trees.Length == 0) return;
        if (stump) stump.SetActive(false);
        var index = Mathf.FloorToInt(UnityEngine.Random.value * trees.Length);
        trees[index].SetActive(true);
        health = maxHealth;
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