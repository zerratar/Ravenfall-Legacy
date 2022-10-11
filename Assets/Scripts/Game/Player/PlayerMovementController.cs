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

    public Vector3 NextDestination;
    public Vector3 LastDestination;

    private Vector3 lastPosition;

    /// <summary>
    /// Used to always have a slight offset to target destinations.
    /// </summary>
    private Vector3 positionJitter;
    private MovementLockState movementLockState;
    private bool firstUnlock = true;
    private NavigationAgent navAgentState;

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
        return GetNavigationAgent().Velocity;
    }

    internal void UpdateIdle(bool onFerry)
    {
        if (onFerry || (Position - lastPosition).magnitude < 0.01f)
        {
            IdleTime += Time.deltaTime;
            MovementTime = 0;
        }
        else
        {
            IdleTime = 0;
            MovementTime += Time.deltaTime;
        }

        lastPosition = Position;
    }

    internal void ResetTimers()
    {
        MovementTime = 0;
        IdleTime = 0;
    }

    internal void SetDestination(Vector3 position)
    {
        Unlock();

        playerAnimations.StartMoving();
        NextDestination = Vector3.zero;

        var agent = GetNavigationAgent();

        if ((!agent.HasPath || Vector3.Distance(position, LastDestination) >= 1
               || Vector3.Distance(agent.Destination, position) >= 10) && agent.ActiveAndEnabled && agent.OnNavMesh && !agent.PathPending)
        {
            LastDestination = position;

            const float jitterRange = 2f;

            position += positionJitter + new Vector3(UnityEngine.Random.Range(-jitterRange, jitterRange), 0, UnityEngine.Random.Range(-jitterRange, jitterRange));

            agent.SetDestination(position);
        }
    }

    internal void Lock()
    {
        var agent = GetNavigationAgent();
        //if (movementLockState == MovementLockState.Unlocked && !agent.Enabled)
        //{
        //    return;
        //}

        var currentPosition = Position;

        agent.Stop();

        if (playerAnimations)
            playerAnimations.StopMoving();

        IsMoving = false;
        MovementTime = 0;
        NextDestination = currentPosition;

        this.movementLockState = MovementLockState.Locked;
    }

    internal void Unlock()
    {
        //if (movementLockState == MovementLockState.Unlocked)
        //{
        //    return;
        //}

        var agent = GetNavigationAgent();
        if (!agent.OnNavMesh && firstUnlock)
        {
            AdjustPlayerPositionToNavmesh();
            firstUnlock = false;
        }

        agent.Enable();

        movementLockState = MovementLockState.Unlocked;
    }

    internal void SetPosition(Vector3 position, bool adjustToNavmesh, bool unlock)
    {
        SetPosition(position, adjustToNavmesh);
        if (unlock) Unlock();
    }

    internal void SetPosition(Vector3 position, bool adjustToNavmesh = true)
    {
        var agent = GetNavigationAgent();
        if (agent.Enabled)
        {
            agent.Teleport(position);
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
            var agent = GetNavigationAgent();
            if (agent.Enabled)
            {
                agent.Teleport(pos);
            }
            else
            {
                gameObject.transform.position = pos;
            }
        }
    }

    internal NavigationAgent GetNavigationAgent()
    {
        if (navAgentState == null)
        {
            return navAgentState = new NavigationAgent(Position, navMeshAgent);
        }
        return navAgentState.Update(Position);
    }

    internal void SetAvoidancePriority(int v)
    {
        if (navMeshAgent)
            navMeshAgent.avoidancePriority = v;
    }
}

public class NavigationAgent
{
    public bool Enabled;
    public bool OnNavMesh;
    public bool HasPath;
    public bool PathPending;
    public bool ReachedDestination;

    public bool ActiveAndEnabled;
    public Vector3 Destination;
    public Vector3 Velocity;

    private NavMeshAgent agent;

    private Vector3 currentPosition;

    public NavigationAgent(Vector3 currentPosition, NavMeshAgent agent)
    {
        this.currentPosition = currentPosition;
        this.agent = agent;
        Update(currentPosition);
    }

    internal NavigationAgent Update(Vector3 currentPosition)
    {
        this.currentPosition = currentPosition;

        Enabled = agent && agent.enabled;
        Destination = agent.destination;
        Velocity = agent.velocity;
        OnNavMesh = agent.isOnNavMesh;
        HasPath = agent.hasPath;
        PathPending = agent.pathPending;
        ActiveAndEnabled = agent.isActiveAndEnabled;
        ReachedDestination = agent.isOnNavMesh ? agent.remainingDistance > 1f : true;
        return this;
    }

    internal void Enable()
    {
        agent.enabled = true;
        if (agent.isOnNavMesh)
            agent.isStopped = false;
    }

    public void Stop()
    {
        var agent = this.agent;
        agent.velocity = Vector3.zero;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(currentPosition);
            agent.isStopped = true;
            agent.ResetPath();
        }
        agent.enabled = false;
    }

    public void Teleport(Vector3 position)
    {
        agent.Warp(position);
    }

    internal void SetDestination(Vector3 position)
    {
        agent.SetDestination(position);
    }
}

