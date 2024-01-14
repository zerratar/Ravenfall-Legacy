using System;
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

    public bool HasIncompletePath;
    public NavMeshPathStatus PathStatus;

    public Vector3 Destination;

    private Vector3 lastPosition;

    private float defaultSpeed;
    private float defaultAngularSpeed;
    private MovementLockState movementLockState;
    private bool firstUnlock = true;
    public float MovementSpeedMultiplier = 1;


    public NavMeshPath CurrentPath;
    private float last_speed;
    private float last_angularSpeed;
    private Transform _transform;


    //private NavigationAgent navAgentState;

    public enum MovementLockState
    {
        None,
        Locked,
        Unlocked
    }


    void Start()
    {
        this._transform = this.transform;
        if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();
        if (!playerAnimations) playerAnimations = GetComponent<PlayerAnimationController>();
        SetupNavigationAgent();
    }


    // Update is called once per frame
    void Update()
    {
        Position = _transform.position;

        if (defaultSpeed > 0 && MovementSpeedMultiplier > 0)
        {
            var speed = MovementSpeedMultiplier * defaultSpeed;
            if (last_speed != speed)
            {
                this.navMeshAgent.speed = speed;
                last_speed = speed;
            }
        }

        if (defaultAngularSpeed > 0 && MovementSpeedMultiplier > 0)
        {
            var angularSpeed = MovementSpeedMultiplier * defaultAngularSpeed;
            if (last_angularSpeed != angularSpeed)
            {
                this.navMeshAgent.angularSpeed = angularSpeed;
                last_angularSpeed = angularSpeed;
            }
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
        this.defaultSpeed = this.navMeshAgent.speed;
        this.defaultAngularSpeed = this.navMeshAgent.angularSpeed;
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

    internal bool SetDestination(Vector3 pos, bool adjustToNavMesh)
    {
        if (Destination == pos && navMeshAgent.destination == pos)
        {
            return true;
        }

        Unlock(adjustToNavMesh);

        if (!navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (CurrentPath == null)
            CurrentPath = new NavMeshPath();

        Destination = pos;
        HasIncompletePath = false;

        navMeshAgent.CalculatePath(pos, CurrentPath);

        if (CurrentPath.status != NavMeshPathStatus.PathComplete)
        {
            this.HasIncompletePath = true;
        }

        this.PathStatus = CurrentPath.status;
        navMeshAgent.SetPath(CurrentPath);

        return !HasIncompletePath;
    }

    public void Lock()
    {
        //agent.Stop();
        var agent = this.navMeshAgent;

        if (!agent.enabled)
        {
            return;
        }

        var currentPosition = Position;
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

    public void Unlock(bool adjustToNavmesh = false)
    {
        if (adjustToNavmesh && !navMeshAgent.isOnNavMesh)
        {
            AdjustPlayerPositionToNavmesh();
        }

        navMeshAgent.enabled = true;
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
        if (navMeshAgent.isOnNavMesh) return;
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

    public void SetAvoidancePriority(int v)
    {
        if (navMeshAgent)
            navMeshAgent.avoidancePriority = v;
    }

    public void EnableLocalAvoidance()
    {
        try
        {
            if (navMeshAgent)
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.Log(this.gameObject?.name + ": navMeshAgent has not been initialized properly before calling EnableLocalAvoidance: " + exc);
        }
    }

    public void DisableLocalAvoidance()
    {
        try
        {
            if (navMeshAgent)
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.Log(this.gameObject?.name + ": navMeshAgent has not been initialized properly before calling DisableLocalAvoidance: " + exc);
        }
    }
}

