using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tessera;
using UnityEngine;

public class GatherController : MonoBehaviour
{
    private readonly ConcurrentDictionary<Guid, PlayerController> gatherers
        = new ConcurrentDictionary<Guid, PlayerController>();

    //public int Level = 1;
    //public double Experience => GameMath.GetFishingExperience(Level);

    [SerializeField] private float respawnTimeSeconds = 20f;
    [SerializeField] private GameObject gatherObj;

    public int Resource => 1;

    public float MaxActionDistance = 5;

    public bool IsDepleted { get; set; }
    public bool PlayKneelingAnimation = true;

    public IslandController Island;

    public IReadOnlyList<PlayerController> Gatherers => gatherers.Values.ToList();

    void Start()
    {
        if (!gatherObj)
        {
            gatherObj = this.transform.GetChild(0).gameObject;
        }

        this.Island = GetComponentInParent<IslandController>();
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }
    public void AddGatherer(PlayerController player)
    {
        gatherers[player.Id] = player;
    }

    public bool Gather(PlayerController player)
    {
        if (IsDepleted) return false;
        AddGatherer(player);
        Gather();
        return true;
    }

    private void Gather()
    {
        IsDepleted = true;
        gatherObj.SetActive(false);
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTimeSeconds);
        gatherers.Clear();
        gatherObj.SetActive(true);
        IsDepleted = false;
    }

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    public void Create(GameObject targetGatheringObject)
    {
        this.gatherObj = targetGatheringObject;

        this.MaxActionDistance = 6f;
        this.respawnTimeSeconds = 20;
        this.PlayKneelingAnimation = true;
        this.Island = GetComponentInParent<IslandController>();
        this.gameObject.EnsureComponent<SphereCollider>(x =>
        {
            x.isTrigger = true;
            x.radius = 3f;
        });

        PlacementUtility.PlaceOnGround(this.gameObject);
    }
}