using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One of the Mangrove island's monkeys (docs/banana-island.md): it ambles about its home on the island's navmesh, sits,
/// beats its chest now and then, and runs when it has snatched something (MonkeyTroop's monkey business). The model is
/// a stand-in (the Low Poly Animated Animals gorilla, small and brown) until a real monkey is found; its animator has
/// isWalking, isRunning and isChestHit.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class Monkey : MonoBehaviour
{
    public const float WalkSpeed = 1.4f;
    public const float RunSpeed = 6.5f;

    /// <summary>Its name in the feed ("Gary the monkey stole ..."), given by the troop.</summary>
    [System.NonSerialized] public string MonkeyName;
    [System.NonSerialized] public Vector3 Home;
    [System.NonSerialized] public float Roam = 7f;

    /// <summary>How much lower than its home it ambles: no deeper into the lagoon than that.</summary>
    private const float WadeDepth = 0.25f;

    /// <summary>Busy with the troop's business (running off with something): it does not wander meanwhile.</summary>
    public bool Busy { get; private set; }

    private static readonly int Walking = Animator.StringToHash("isWalking");
    private static readonly int Running = Animator.StringToHash("isRunning");
    private static readonly int ChestHit = Animator.StringToHash("isChestHit");

    private NavMeshAgent agent;
    private Animator animator;
    private float nextThought;
    private float chestUntil;
    private int areaMask = NavMesh.AllAreas;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        agent.speed = WalkSpeed;
        agent.angularSpeed = 540f;
        agent.acceleration = 20f;
        agent.stoppingDistance = 0.3f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    private void Start()
    {
        areaMask = NavMeshHelper.AreaMaskAt(transform.position);
        agent.areaMask = areaMask;
        nextThought = Time.time + Random.Range(0.5f, 6f);
    }

    /// <summary>Runs to a place (to whoever it takes something from, or away with it).</summary>
    public bool RunTo(Vector3 position)
    {
        Busy = true;
        chestUntil = 0f;
        agent.speed = RunSpeed;
        return Go(position);
    }

    /// <summary>Done running: back to its ambling.</summary>
    public void Settle()
    {
        Busy = false;
        agent.speed = WalkSpeed;
        nextThought = Time.time + Random.Range(1f, 3f);
    }

    /// <summary>Beats its chest for a few seconds: pleased with itself, or showing off to someone who clicked it.</summary>
    public void BeatChest(float seconds = 3.8f)
    {
        if (agent.isOnNavMesh) agent.ResetPath();
        chestUntil = Time.time + seconds;
    }

    public bool Arrived => !agent.isOnNavMesh || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.4f);

    public Vector3 Position => transform.position;

    /// <summary>Somewhere near its home, not down in the water.</summary>
    private void Amble()
    {
        var at = Home + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1f, Roam);
        if (!NavMesh.SamplePosition(at, out var hit, 3f, areaMask) || hit.position.y < Home.y - WadeDepth) return;
        agent.SetDestination(hit.position);
    }

    private bool Go(Vector3 position)
    {
        if (!agent.isOnNavMesh) return false;
        if (!NavMesh.SamplePosition(position, out var hit, 4f, areaMask)) return false;
        return agent.SetDestination(hit.position);
    }

    private void Update()
    {
        var moving = agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.05f;
        var beating = Time.time < chestUntil;
        animator.SetBool(ChestHit, beating && !moving);
        animator.SetBool(Running, moving && agent.speed > WalkSpeed + 0.5f);
        animator.SetBool(Walking, moving);

        if (Busy || beating || Time.time < nextThought) return;
        nextThought = Time.time + Random.Range(4f, 14f);
        var roll = Random.value;
        if (roll < 0.15f) BeatChest();
        else if (roll < 0.75f) Amble();
        // otherwise it sits a while where it is
    }
}
