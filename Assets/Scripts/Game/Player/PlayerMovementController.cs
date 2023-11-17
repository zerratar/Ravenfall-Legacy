using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private PlayerAnimationController playerAnimations;

    [HideInInspector] public Vector3 Position;

    public float IdleTime;
    public float MovementTime;
    public bool IsMoving;

    public Vector3 Destination;

    private Vector3 lastPosition;

    /// <summary>
    /// Used to always have a slight offset to target destinations.
    /// </summary>
    private Vector3 positionJitter;
    private float defaultSpeed;
    private float defaultAngularSpeed;
    private MovementLockState movementLockState;
    private bool firstUnlock = true;
    public float MovementSpeedMultiplier = 1;


    private NavMeshPath currentPath;

    //private NavigationAgent navAgentState;

    public enum MovementLockState
    {
        None,
        Locked,
        Unlocked
    }


    void Start()
    {
        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
        SetupNavigationAgent();
    }


    // Update is called once per frame
    void Update()
    {
        Position = this.transform.position;

        if (defaultSpeed > 0 && MovementSpeedMultiplier > 0)
        {
            this.navMeshAgent.speed = MovementSpeedMultiplier * defaultSpeed;
        }

        if (defaultAngularSpeed > 0 && MovementSpeedMultiplier > 0)
        {
            this.navMeshAgent.angularSpeed = MovementSpeedMultiplier * defaultAngularSpeed;
        }
    }

    internal void UpdateMovement()
    {
        IsMoving = (movementLockState == MovementLockState.Unlocked) && MovementTime > 0.05f && GetAgentVelocity().sqrMagnitude >= 0.01f;

        if (!IsMoving && this.playerAnimations.IsMoving)
        {
            this.playerAnimations.StopMoving();
        }
        else if (IsMoving && !this.playerAnimations.IsMoving)
        {
            this.playerAnimations.StartMoving();
        }
    }

    private void SetupNavigationAgent()
    {
        var x = UnityEngine.Random.Range(-0.5f, 0.5f);
        var z = UnityEngine.Random.Range(-0.5f, 0.5f);
        this.positionJitter = new Vector3(x, 0, z);
        this.defaultSpeed = this.navMeshAgent.speed;
        this.defaultAngularSpeed = this.navMeshAgent.angularSpeed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Vector3 JitterTranslate(Vector3 position)
    {
        return position + positionJitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Vector3 JitterTranslateSlow(object obj)
    {
        return (((obj as Transform) ?? (obj as MonoBehaviour)?.transform)?.position ?? Position) + positionJitter;
    }

    internal Vector3 GetAgentVelocity()
    {
        return navMeshAgent.velocity;
    }

    internal void UpdateIdle(bool onFerry)
    {
        var a = Position;
        var b = lastPosition;
        var x = a.x - b.x;
        var y = a.y - b.y;
        var z = a.z - b.z;

        if (onFerry || System.Math.Sqrt(x * x + y * y + z * z) < 0.01f)
        {
            IdleTime += GameTime.deltaTime;
            MovementTime = 0;
        }
        else
        {
            IdleTime = 0;
            MovementTime += GameTime.deltaTime;
        }

        lastPosition = Position;
    }

    internal void ResetTimers()
    {
        MovementTime = 0;
        IdleTime = 0;
    }

    internal bool SetDestination(Vector3 pos)
    {
        Unlock();

        if (!navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (Destination == pos)
        {
            return true;
        }

        if (currentPath == null)
            currentPath = new NavMeshPath();

        if (navMeshAgent.CalculatePath(pos, currentPath) && currentPath.status == NavMeshPathStatus.PathComplete)
        {
            Destination = pos;
            navMeshAgent.SetPath(currentPath);
            return true;
        }

        if (currentPath.status == NavMeshPathStatus.PathInvalid)
        {
            // could be good to log this, maybe even target of the player to determine what object has invalid paths.
        }

        return false;
    }

    internal void Lock()
    {
        var currentPosition = Position;

        //agent.Stop();
        var agent = this.navMeshAgent;
        agent.velocity = Vector3.zero;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(currentPosition);
            agent.isStopped = true;
            agent.ResetPath();
        }
        agent.enabled = false;

        if (playerAnimations)
            playerAnimations.StopMoving();

        IsMoving = false;
        MovementTime = 0;
        Destination = currentPosition;

        this.movementLockState = MovementLockState.Locked;
    }

    internal void Unlock(bool adjustToNavmesh = false)
    {
        navMeshAgent.enabled = true;
        if (adjustToNavmesh && !navMeshAgent.isOnNavMesh)
        {
            AdjustPlayerPositionToNavmesh();
        }

        movementLockState = MovementLockState.Unlocked;
    }

    internal void SetPosition(Vector3 position, bool adjustToNavmesh, bool unlock)
    {
        SetPosition(position, adjustToNavmesh);
        if (unlock) Unlock();
    }

    internal void SetPosition(Vector3 position, bool adjustToNavmesh = true)
    {
        var agent = navMeshAgent;
        if (agent.enabled)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }

        Position = position;

        if (adjustToNavmesh)
            AdjustPlayerPositionToNavmesh();
    }

    public void AdjustPlayerPositionToNavmesh()
    {
        NavMeshHit closestHit;
        if (NavMesh.SamplePosition(gameObject.transform.position, out closestHit, 500f, NavMesh.AllAreas))
        {
            var pos = closestHit.position;
            var agent = navMeshAgent;
            if (agent)
            {
                agent.Warp(pos);
            }

            gameObject.transform.position = pos;
        }
    }

    internal void SetAvoidancePriority(int v)
    {
        if (navMeshAgent)
            navMeshAgent.avoidancePriority = v;
    }
}

