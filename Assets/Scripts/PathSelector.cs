using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class PathSelector : MonoBehaviour
{
    [SerializeField] private FerryController ferryController;
    [SerializeField] private SplineContainer[] splines;
    [SerializeField] private float stopBetweenPaths = 8f;
    [SerializeField] private float movementSpeed = 9f;
    private int pathIndex = 0;
    private bool isMoving = false;
    private bool isPlaying;
    private float elapsed = 0f;
    private float pathLength;
    private float originalDuration;
    private float duration = 0f;
    private Spline currentSpline;
    private float stopTimer = 0f;

    private void Start()
    {
        for (int i = 0; i < splines.Length; i++)
        {
            var container = splines[i];
            container.Spline.Warmup();
        }
        StartNewPath();
    }

    private void Update()
    {
        if (isMoving)
        {
            elapsed += GameTime.deltaTime;
            float t = EaseInOut(elapsed / duration);

            currentSpline.Evaluate(t, out var position, out var tangent, out var upVector);
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, upVector);
            ferryController.SetMovementEffect(Mathf.Clamp01((movementSpeed + ferryController.CaptainSpeedAdjustment) / movementSpeed));

            if (elapsed >= duration)
            {
                isMoving = false;
                ferryController.SetState(FerryState.Docked);
                ferryController.SetMovementEffect(0);
                stopTimer = stopBetweenPaths;
            }
        }
        else if (stopTimer > 0)
        {
            stopTimer -= GameTime.deltaTime;
            if (stopTimer <= 0)
            {
                StartNewPath();
            }
        }
    }

    private void StartNewPath()
    {
        pathIndex = (pathIndex + 1) % splines.Length;
        currentSpline = splines[pathIndex].Spline;
        float length = currentSpline.GetLength();
        float adjustedSpeed = movementSpeed + ferryController.CaptainSpeedAdjustment;

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space))
        {
            adjustedSpeed += 1000;
        }
#endif
        pathLength = length;
        originalDuration = Mathf.Max(1f, (2f * length) / adjustedSpeed);
        duration = originalDuration;

        elapsed = 0f;
        isMoving = true;
        ferryController.SetState(FerryState.Moving);
    }
    private float EaseInOut(float t)
    {
        return Mathf.Clamp01(t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2);
    }

    public int PathIndex => pathIndex;
    public bool IsMoving => isMoving;
}
