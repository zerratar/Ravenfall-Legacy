using System;
using UnityEngine;

public class TavernHandler : MonoBehaviour
{
    [SerializeField] private FadeInOut fadeToBlack;
    [SerializeField] private GameCamera gameCamera;
    [SerializeField] private SphereCollider enterCollider;
    [SerializeField] private SphereCollider exitCollider;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform enterPoint;

    [SerializeField] private GameObject tavern;

    [SerializeField] private GameObject defaultTheme;
    [SerializeField] private GameObject halloweenTheme;
    [SerializeField] private GameObject christmasTheme;
    [SerializeField] private GameObject birthdayTheme;

    [SerializeField] private TicTacToe ticTacToeGame;
    [SerializeField] private PetRacingGame petRacingGame;

    [SerializeField] private ITavernGame[] games;

    [SerializeField] private bool maintenanceMode = true;

    private bool insideTavern;
    private GameObject activeTheme;
    private ITavernGame activeGame;

    private float leaveTimer;

    [Header("Diagnostics")]
    [Tooltip("Logs the camera's distance to the tavern triggers, at most once a second. Temporary.")]
    [SerializeField] private bool logProximity = true;
    private float nextProximityLog;
    private float lastLoggedDistance = float.MinValue;

    public TicTacToe TicTacToe => ticTacToeGame;
    public PetRacingGame PetRacing => petRacingGame;
    public ITavernGame ActiveGame => activeGame;
    public bool IsActivated => insideTavern;
    public bool MaintenanceMode => maintenanceMode;
    public bool CanRedeemItems;

    private DayNightCycle dayNightCycle;
    private float previousTimeOfDay;

    // Start is called before the first frame update
    void Start()
    {
        dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        if (!gameCamera) gameCamera = FindAnyObjectByType<GameCamera>();
        fadeToBlack.FadeHalfWay = OnFadeHalfway;

        tavern.SetActive(false);
        defaultTheme.SetActive(false);
        halloweenTheme.SetActive(false);
        christmasTheme.SetActive(false);
    }

    private void OnFadeHalfway()
    {
        if (!insideTavern)
        {
            insideTavern = true;
            tavern.SetActive(true);
            SetActiveTheme();
            gameCamera.transform.position = enterPoint.position;
            gameCamera.transform.rotation = enterPoint.rotation;
            gameCamera.ForceFreeCamera();
        }
        else
        {
            tavern.SetActive(false);
            gameCamera.transform.position = exitPoint.position;
            gameCamera.transform.rotation = exitPoint.rotation;
            gameCamera.ReleaseFreeCamera();
            insideTavern = false;
            UnfreezeDayTime();
        }
    }

    private void SetActiveTheme()
    {
        var dateNow = DateTime.Now;
        switch (dateNow.Month)
        {
            case 10:

                SetHalloweenTheme();
                //SetTheme(dateNow.Day == 8 ? birthdayTheme : halloweenTheme);
                break;
            case 11:
                if (dateNow.Year == 2021) // Special year. As halloween was late, we allow it to exist for whole of november too.
                    SetHalloweenTheme();
                break;
            case 12:
                SetTheme(christmasTheme);
                break;
            case 1:
                if (dateNow.Year == 2022) // Special year. As christmas was late, we allow it to exist for whole of january too.
                    SetTheme(christmasTheme);
                break;
            default:
                SetTheme(defaultTheme);
                break;
        }
    }

    private void UnfreezeDayTime()
    {
        dayNightCycle.SetTimeOfDay(previousTimeOfDay, 0);
    }
    private void FreezeDayTimeAt(float time)
    {
        this.previousTimeOfDay = dayNightCycle.TotalTime;
        dayNightCycle.SetTimeOfDay(time, float.MaxValue);
    }
    private void SetHalloweenTheme()
    {
        SetTheme(halloweenTheme);
        FreezeDayTimeAt(DayNightCycle.TimeOfDay_Night);
    }

    private void SetTheme(GameObject theme)
    {
        if (activeTheme) activeTheme.SetActive(false);
        activeTheme = theme;
        activeTheme.SetActive(true);
    }


    /// <summary>
    /// World space centre of a sphere collider.
    /// </summary>
    /// <remarks>
    /// This used to be written as <c>transform.position + collider.center</c>, which is only
    /// correct when the collider's transform has no rotation and no scale. The tavern's transform
    /// is rotated -90 degrees on Y, so a centre offset of (0.52, 0.49, 2.58) was being applied
    /// along the wrong axes and put the trigger point about 2.6 units away from where it is drawn
    /// in the editor. TransformPoint applies the rotation and scale the same way the physics system
    /// does.
    /// </remarks>
    private static Vector3 WorldCenter(SphereCollider collider)
    {
        return collider.transform.TransformPoint(collider.center);
    }

    /// <summary>
    /// World space radius of a sphere collider. <see cref="SphereCollider.radius"/> is in local
    /// space, so a scaled transform makes the raw value disagree with the sphere actually drawn.
    /// Unity scales a sphere collider by the largest axis, so this matches it.
    /// </summary>
    private static float WorldRadius(SphereCollider collider)
    {
        var scale = collider.transform.lossyScale;
        var largest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        return collider.radius * largest;
    }

    /// <summary>
    /// Reports how far the camera is from a tavern trigger, at most once a second and only when the
    /// distance has actually moved. Temporary, to find out whether the tavern is failing because
    /// proximity is never detected or because the fade never runs.
    /// </summary>
    private void LogProximity(string which, float distance, float radius)
    {
        if (!logProximity)
        {
            return;
        }

        if (Time.unscaledTime < nextProximityLog && Mathf.Abs(distance - lastLoggedDistance) < 1f)
        {
            return;
        }

        nextProximityLog = Time.unscaledTime + 1f;
        lastLoggedDistance = distance;
        Shinobytes.Debug.Log($"[Tavern] {which}: distance {distance:F1} of radius {radius:F1}"
            + $", inside={insideTavern}, fadeActive={(fadeToBlack ? fadeToBlack.FadeActive.ToString() : "no fade component")}");
    }

    // Update is called once per frame
    void Update()
    {
        if (maintenanceMode)
            return;

        if (!insideTavern)
        {
            var enterTavernPoint = WorldCenter(enterCollider);
            var dist = Vector3.Distance(gameCamera.transform.position, enterTavernPoint);
            LogProximity("enter", dist, WorldRadius(enterCollider));
            if (dist <= WorldRadius(enterCollider))
            {
                if (!fadeToBlack.FadeActive)
                {
                    fadeToBlack.StartFade();
                }

                leaveTimer = 2f;
            }
        }
        else
        {
            leaveTimer -= Time.deltaTime;
            if (leaveTimer > 0)
            {
                return;
            }

            var exitTavernPoint = WorldCenter(exitCollider);
            var dist = Vector3.Distance(gameCamera.transform.position, exitTavernPoint);
            LogProximity("exit", dist, WorldRadius(exitCollider));
            if (dist >= WorldRadius(exitCollider))
            {
                if (!fadeToBlack.FadeActive)
                {
                    fadeToBlack.StartFade();
                }
            }
        }
    }
}
