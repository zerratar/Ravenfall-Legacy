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

    public TicTacToe TicTacToe => ticTacToeGame;
    public PetRacingGame PetRacing => petRacingGame;
    public ITavernGame ActiveGame => activeGame;

    public bool IsActivated => insideTavern;
    public bool MaintenanceMode => maintenanceMode;
    public bool CanRedeemItems;
    // Start is called before the first frame update
    void Start()
    {
        if (!gameCamera) gameCamera = FindObjectOfType<GameCamera>();
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
        }
    }

    private void SetActiveTheme()
    {
        var dateNow = DateTime.Now;
        switch (dateNow.Month)
        {
            case 10:
                SetTheme(dateNow.Day == 8 ? birthdayTheme : halloweenTheme);
                break;
            case 12:
                SetTheme(christmasTheme);
                break;
            default:
                SetTheme(defaultTheme);
                break;
        }
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
            var enterTavernPoint = this.transform.position + enterCollider.center;
            if (Vector3.Distance(gameCamera.transform.position, enterTavernPoint) <= enterCollider.radius)
            {
                if (!fadeToBlack.FadeActive)
                {
                    fadeToBlack.StartFade();
                }
            }
        }
        else
        {
            var exitTavernPoint = this.transform.position + exitCollider.center;
            if (Vector3.Distance(gameCamera.transform.position, exitTavernPoint) >= exitCollider.radius)
            {
                if (!fadeToBlack.FadeActive)
                {
                    fadeToBlack.StartFade();
                }
            }
        }
    }
}
