using Assets.Scripts;
using System;
using System.Linq;
using UnityEngine;

public enum FerryState
{
    Docked,
    Moving,
}

public enum WaypointType
{
    Dock,
    Normal
}

public class FerryController : MonoBehaviour
{
    [SerializeField] private GameObject movementEffect;
    [SerializeField] private float boyance = 0.0333f;
    [SerializeField] private float wave = 0.333f;
    [SerializeField] private float bobEffect = 0.33f;

    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private PathSelector pathSelector;

    public FerryState state = FerryState.Moving;

    private IslandController island;

    private Vector3 destination;
    private ParticleSystem movementParticleSystem;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MinMaxCurve rateOverTime;
    private float posY;

    public IslandController Island => island;
    public float GetProgress() => pathSelector.GetProgress();
    public int PathIndex => pathSelector.PathIndex;
    public float CurrentSpeed => pathSelector.CurrentSpeed;
    public float CurrentPathETA => pathSelector.CurrentPathETA;
    public float CurrentLeaveETA => pathSelector.CurrentLeaveETA;

    // Use this for initialization
    void Start()
    {
        movementParticleSystem = movementEffect.GetComponent<ParticleSystem>();
        pathSelector = gameObject.GetComponent<PathSelector>();
        emission = movementParticleSystem.emission;
        rateOverTime = emission.rateOverTime;
        posY = transform.position.y;
    }

    public bool Docked => state == FerryState.Docked;


    // Update is called once per frame
    private void LateUpdate()
    {
        if (GameCache.Instance.IsAwaitingGameRestore) return;
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        var pos = transform.position;
        var euler = transform.rotation.eulerAngles;
        var z = Mathf.Sin(Time.time) * (wave * 2f) - wave;
        var y = Mathf.Sin(Time.time) * (boyance * 2f) - boyance;
        var x = Mathf.Sin(Time.time) * (bobEffect * 2f) - bobEffect;

        transform.rotation = Quaternion.Euler(x, euler.y, z);
        transform.position = new Vector3(pos.x, posY + y, pos.z);
        movementEffect.SetActive(state == FerryState.Moving);
    }

    public void IslandEnter(IslandController island)
    {
        this.island = island;
    }

    public void IslandExit()
    {
        island = null;
    }

    public bool IsCaptainPosition(Transform transform)
    {
        return transform && (transform == playerPositions[0] || transform.position == playerPositions[0].position);
    }

    public Transform GetNextPlayerPoint()
    {
        var captainPosition = playerPositions[0];
        if (captainPosition.childCount == 0) return captainPosition;

        return playerPositions
            .Skip(1)
            .OrderBy(x => x.transform.childCount)
            .ThenBy(x => UnityEngine.Random.value)
            .FirstOrDefault();
    }

    public void SetMovementEffect(float v)
    {
        emission.rateOverTime = rateOverTime.constant * v;
    }
}
