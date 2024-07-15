using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GatherController : TaskObject
{
    private readonly ConcurrentDictionary<Guid, PlayerController> gatherers
        = new ConcurrentDictionary<Guid, PlayerController>();

    [SerializeField] private float respawnTimeSeconds = 20f;
    private bool respawnTimeLocked;
    [SerializeField] private GameObject gatherObj;

    private float maxRespawnTimeSeconds;
    private float minRespawnTimeSeconds;

    public float MaxActionDistance = 5;

    public bool IsDepleted { get; set; }
    public bool PlayKneelingAnimation = true;

    public IslandController Island;

    public IReadOnlyList<PlayerController> Gatherers => gatherers.Values.ToList();

    [HideInInspector] public bool IsInvalid;

    void Start()
    {
        maxRespawnTimeSeconds = respawnTimeSeconds;
        minRespawnTimeSeconds = 1f;

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
    private float invalidTimer;
    public override void Poll()
    {
        if (IsInvalid)
        {
            invalidTimer += GameTime.deltaTime;
            if (invalidTimer >= 20)
            {
                invalidTimer = 0f;
                IsInvalid = false;
            }
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
        gatherers.Clear();
        gatherObj.SetActive(true);
        IsDepleted = false;
        respawnTimeLocked = false;
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