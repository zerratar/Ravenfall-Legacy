using Cysharp.Threading.Tasks.Triggers;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class PathSelector : MonoBehaviour
{
    [SerializeField] private FerryController ferryController;
    [SerializeField] private SplineContainer[] splines;

    // the mangrove island's stop, between Eldara and Home, made only where the island is unlocked (StoryLog);
    // the paths are numbered after the loop's: 7 there, 8 back home
    [SerializeField] private SplineContainer toMangrove;
    [SerializeField] private SplineContainer fromMangrove;
    public const int EldaraPath = 5, HomePath = 6, ToMangrovePath = 7, FromMangrovePath = 8;

    // a stop another scene adds to the ring (the pier at the foot of the world tower, WorldTower): after one of the
    // ring's paths the ferry sails "to" and then "from", which stands in for the ring's next path; only while
    // "wanted" says someone is bound there or waiting there, so nobody else's crossing is made longer
    public const int ExtraToPath = 9, ExtraFromPath = 10;
    private SplineContainer extraTo, extraFrom;
    private Spline extraToSpline, extraFromSpline;
    private int extraAfter = -1;
    private int extraResume;
    private System.Func<bool> extraWanted;

    /// <summary>The island at the added stop, and where the ferry is bound when it leaves it; null with no stop.</summary>
    public string ExtraStopName { get; private set; }
    public string ExtraNextName { get; private set; }

    public void RegisterStop(int afterPath, SplineContainer to, SplineContainer from, string name, string next, System.Func<bool> wanted)
    {
        if (!to || !from || splines == null || splines.Length == 0) return;
        to.Spline.Warmup();
        from.Spline.Warmup();
        extraTo = to;
        extraFrom = from;
        extraToSpline = to.Spline;
        extraFromSpline = from.Spline;
        extraAfter = afterPath;
        extraResume = (afterPath + 2) % splines.Length;
        extraWanted = wanted;
        ExtraStopName = name;
        ExtraNextName = next;
    }

    /// <summary>The stop goes with its scene; a leg the ferry is on is sailed to its end.</summary>
    public void UnregisterStop(SplineContainer to)
    {
        if (extraTo != to) return;
        extraAfter = -1;
        extraWanted = null;
    }
    [SerializeField] private float stopBetweenPaths = 8f;
    [SerializeField] private float movementSpeed = 9f;
    private int pathIndex = 0;
    private bool isMoving = false;
    private bool isPlaying;
    private float elapsed = 0f;
    private float pathLength;
    private float duration = 0f;
    private Spline currentSpline;
    private float stopTimer = 0f;

    private float devSpeedBoost = 0f;

    public void UpdateFerrySpeed()
    {
        var oldDuration = duration;
        var effectBoost = Mathf.Max(1, ferryController.GetFerryBoostEffect());
        // the captain's level fills a curve towards four times the slowest speed (SailingSpeeds), no longer 20 + level
        var adjustedSpeed = SailingSpeeds.Ferry(movementSpeed, (int)ferryController.CaptainSpeedAdjustment) * effectBoost + devSpeedBoost;
        duration = Mathf.Max(1f, (2f * pathLength) / adjustedSpeed);
        if (oldDuration != duration && oldDuration > 0f)
        {
            // Adjust elapsed so ferry doesn't teleport
            elapsed = (elapsed / oldDuration) * duration;
        }
    }

    private void Start()
    {
        for (int i = 0; i < splines.Length; i++)
        {
            var container = splines[i];
            container.Spline.Warmup();
        }
        if (toMangrove) toMangrove.Spline.Warmup();
        if (fromMangrove) fromMangrove.Spline.Warmup();
        StartNewPath();
    }

    private void Update()
    {
        var prevDevSpeedBoost = devSpeedBoost;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space))
        {
            devSpeedBoost = 1000f;
        }
        else
        {
            devSpeedBoost = 0f;
        }

        if (prevDevSpeedBoost != devSpeedBoost)
        {
            UpdateFerrySpeed();
        }

        if (isMoving)
        {
            elapsed += GameTime.deltaTime;
            float t = EaseInOut(elapsed / duration);

            currentSpline.Evaluate(t, out var position, out var tangent, out var upVector);
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, upVector);
            var effectBoost = Mathf.Max(1, ferryController.GetFerryBoostEffect());
            ferryController.SetMovementEffect(Mathf.Clamp01((SailingSpeeds.Ferry(movementSpeed, (int)ferryController.CaptainSpeedAdjustment) * effectBoost) / movementSpeed));

            if (elapsed >= duration)
            {
                isMoving = false;
                ferryController.SetState(FerryState.Docked);
                ferryController.SetMovementEffect(0);

                var sbp = stopBetweenPaths;
                // adjust the timer to be variable to the distance of the players that wish to embark on the island.
                // if no players are going to embark, then we can have 500ms to allow for disembark events to go through.
                var longestDist = 0f;
                var dockedIsland = ferryController.Island;
                if (dockedIsland != null)
                {
                    var fp = this.transform.position;
                    foreach (var player in dockedIsland.GetPlayers())
                    {
                        if (player.ferryHandler.Embarking)
                        {
                            var d = Vector3.Distance(player.Position, fp);
                            if (d > longestDist)
                            {
                                longestDist = d;
                            }
                        }
                    }
                }

                if (longestDist > 0)
                {
                    sbp = Mathf.Max(Mathf.Min(stopBetweenPaths, longestDist), 0.5f);
                }
                else
                {
                    sbp = 0.5f;
                }

                stopTimer = sbp;
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
        pathIndex = NextPath(pathIndex);
        currentSpline = pathIndex == ToMangrovePath ? toMangrove.Spline : pathIndex == FromMangrovePath ? fromMangrove.Spline
            : pathIndex == ExtraToPath ? extraToSpline : pathIndex == ExtraFromPath ? extraFromSpline : splines[pathIndex].Spline;
        pathLength = currentSpline.GetLength();
        UpdateFerrySpeed();
        elapsed = 0f;
        isMoving = true;
        ferryController.SetState(FerryState.Moving);
    }
    /// <summary>The loop, with the detour to the mangrove island after Eldara where it is open.</summary>
    private int NextPath(int current)
    {
        if (current == EldaraPath && toMangrove && fromMangrove && MangroveOpen()) return ToMangrovePath;
        if (current == ToMangrovePath) return FromMangrovePath;
        if (current == FromMangrovePath) return 0;
        if (current == extraAfter && extraAfter >= 0 && extraToSpline != null && extraFromSpline != null && (extraWanted == null || extraWanted())) return ExtraToPath;
        if (current == ExtraToPath) return ExtraFromPath;
        if (current == ExtraFromPath) return extraResume;
        return (current + 1) % splines.Length;
    }

    private static bool MangroveOpen()
    {
        var game = GameManager.Instance;
        var island = game && game.Islands != null ? game.Islands.Get(IslandController.MangroveIsland) : null;
        return island && !island.Locked;
    }

    private float EaseInOut(float t)
    {
        return Mathf.Clamp01(t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2);
    }

    public int PathIndex => pathIndex;
    public bool IsMoving => isMoving;
}
