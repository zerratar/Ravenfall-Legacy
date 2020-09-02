using System;
using System.Collections;
using PathCreation;
using PathCreation.Examples;
using UnityEngine;

public class PathSelector : MonoBehaviour
{
    [SerializeField] private FerryController ferryController;

    // Start is called before the first frame update
    [SerializeField] private PathCreator[] paths;
    [SerializeField] private PathFollower pathFollower;
    [SerializeField] private float stopBetweenPaths;

    [SerializeField] private float movementSpeed = 9f;

    [Range(0f, 1f)] [SerializeField] private float speedChangeAtPercent = 0.1f;

    //[SerializeField] private float slowdownPercent = 0.98f;
    [SerializeField] private float minSpeedPercent = 0.02f;

    private float[] pathDuration;
    private float[] pathStart;

    private int pathIndex = 0;
    private bool changingPath;
    private float leaveTimer;
    private GameManager gameManager;

    public int PathIndex => pathIndex;
    public float CurrentSpeed => pathFollower.speed;
    public float CurrentPathETA
    {
        get
        {
            var duration = pathDuration[pathIndex];
            if (duration <= 0f)
            {
                var diff = (pathFollower.Length - pathFollower.Distance);
                if (pathFollower.speed <= 0 || diff <= 0) return 0;
                return diff / pathFollower.speed;
            }

            var start = pathStart[pathIndex];
            if (start <= 0f)
            {
                return duration;
            }

            return duration - (Time.time - start);
        }
    }

    public float CurrentLeaveETA => leaveTimer;

    public float GetProgress() => pathFollower.GetProgress();

    private void Awake()
    {
        pathDuration = new float[paths.Length];
        pathStart = new float[paths.Length];
        pathStart[pathIndex] = Time.time;
        this.gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        var speed = movementSpeed;
        var stop = stopBetweenPaths;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space) && (gameManager.Permissions?.IsAdministrator).GetValueOrDefault())
        {
            speed = 500;
            stop = 3;
        }

        if (leaveTimer > 0f) leaveTimer -= Time.deltaTime;
        if (leaveTimer < 0) leaveTimer = 0f;
        if (!pathFollower) return;
        if (paths == null || paths.Length == 0) return;

        if (pathFollower.IsPathCompleted && !changingPath)
        {
            StartCoroutine(ChangePath(stop));
        }
        else if (!pathFollower.IsPathCompleted)
        {
            ferryController.state = FerryState.Moving;
            var distance = pathFollower.GetProgress();

            if (distance < speedChangeAtPercent)
            {
                // going from zero to hero
                var percent = Mathf.Min(Mathf.Max(0.01f, distance / speedChangeAtPercent), 1f);
                pathFollower.speed = speed * percent;
                ferryController.SetMovementEffect(percent);
            }
            else
            {
                var target = 1f - speedChangeAtPercent;
                if (distance >= target)
                {
                    // hero to zero Kappa
                    var percent = (1f - distance) / speedChangeAtPercent;
                    var slowdown = Mathf.Max(minSpeedPercent, percent);
                    pathFollower.speed = speed * slowdown;
                    ferryController.SetMovementEffect(slowdown);
                }
                else
                {
                    pathFollower.speed = speed;
                    ferryController.SetMovementEffect(1f);
                }
            }
        }
    }

    private IEnumerator ChangePath(float stop)
    {
        var pathTime = Time.time - pathStart[pathIndex];
        pathDuration[pathIndex] = pathTime;
        changingPath = true;
        ferryController.state = FerryState.Docked;
        leaveTimer = stop;
        yield return new WaitForSeconds(stop);
        pathIndex = (pathIndex + 1) % paths.Length;
        pathFollower.ResetPath();
        pathFollower.pathCreator = paths[pathIndex];
        changingPath = false;
        pathStart[pathIndex] = Time.time;
    }
}
