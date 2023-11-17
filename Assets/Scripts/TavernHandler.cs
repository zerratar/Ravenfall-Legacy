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

    // Update is called once per frame
    void Update()
    {
        if (maintenanceMode)
            return;

        if (!insideTavern)
        {
            var enterTavernPoint = this.enterCollider.transform.position + enterCollider.center;
            var dist = Vector3.Distance(gameCamera.transform.position, enterTavernPoint);
            if (dist <= enterCollider.radius)
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

            var exitTavernPoint = this.exitCollider.transform.position + exitCollider.center;
            var dist = Vector3.Distance(gameCamera.transform.position, exitTavernPoint);
            if (dist >= exitCollider.radius)
            {
                if (!fadeToBlack.FadeActive)
                {
                    fadeToBlack.StartFade();
                }
            }
        }
    }
}
