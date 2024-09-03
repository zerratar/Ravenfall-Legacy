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
    private Transform internalTransform;
    private bool hasInternalTransform;
    private int navmeshErrorCount;

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
    public void Poll()
    {
        if (!hasInternalTransform)
        {
            internalTransform = transform;
            hasInternalTransform = true;
        }

        Position = internalTransform.position;
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

    internal bool SetDestination(Vector3 pos)
    {
        if (Destination == pos && navMeshAgent.destination == pos)
        {
            return true;
        }

        Unlock();

        if (!navMeshAgent.isActiveAndEnabled
            // || !navMeshAgent.isOnNavMesh
            )
        {
            return true;
        }

        if (CurrentPath == null)
            CurrentPath = new NavMeshPath();

        HasIncompletePath = false;

        if (navMeshAgent.isOnNavMesh && navMeshAgent.CalculatePath(pos, CurrentPath))
        {
            navmeshErrorCount = 0;
            Destination = pos;

            if (CurrentPath.status != NavMeshPathStatus.PathComplete)
            {
                this.HasIncompletePath = true;
            }

            navMeshAgent.SetPath(CurrentPath);
        }

        this.PathStatus = CurrentPath.status;

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

    public void Unlock()
    {
        if (!navMeshAgent.isOnNavMesh)
        {
            AdjustPlayerPositionToNavmesh(0.5f);
        }

        navMeshAgent.enabled = true;
        movementLockState = MovementLockState.Unlocked;
    }

    internal void SetPosition(Vector3 position)
    {
        SetPositionImpl(position);
        AdjustPlayerPositionToNavmesh(0.5f);
    }

    private bool SetPositionImpl(Vector3 position)
    {
        var success = true;
        var agent = navMeshAgent;
        if (agent.enabled)
        {
            success = agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }

        Position = position;
        return success;
    }

    public void AdjustPlayerPositionToNavmesh(float maxDistance = 20f)
    {
        if (navMeshAgent.isOnNavMesh) return;
        var currentPosition = gameObject.transform.position;

        if (NavMeshHelper.SamplePosition(currentPosition, maxDistance, out var pos))
        {
            if (!SetPositionImpl(pos))
            {
#if DEBUG
                Shinobytes.Debug.LogError(name + " could not be placed on the navmesh. Perhaps try nudging the player slightly.");
#endif
                var nudge = UnityEngine.Random.insideUnitSphere * 1f;
                nudge.y = 0;
                SetPositionImpl(currentPosition + nudge);
            }
        }
#if DEBUG
        else
        {
            Shinobytes.Debug.LogError("Could not find a valid position on the navmesh for " + name + " at " + currentPosition);
        }
#endif
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

