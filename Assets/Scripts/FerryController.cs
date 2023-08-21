﻿using Assets.Scripts;
using System;
using Shinobytes.Linq;
using UnityEngine;

public enum FerryState
{
    Docked,
    Moving,
}

public enum WaypointType
{
    Dock,
    Normal
}

public class FerryController : MonoBehaviour
{
    [SerializeField] private GameObject movementEffect;
    [SerializeField] private float boyance = 0.0333f;
    [SerializeField] private float wave = 0.333f;
    [SerializeField] private float bobEffect = 0.33f;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private PathSelector pathSelector;
    [SerializeField] private FerryUI ui;

    public FerryState state = FerryState.Moving;

    private IslandController island;

    private Vector3 destination;
    private ParticleSystem movementParticleSystem;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MinMaxCurve rateOverTime;
    private float posY;

    private bool isVisible = true;
    private StreamLabel ferryStateLabel;

    public IslandController Island => island;
    public int PathIndex => pathSelector.PathIndex;
    public float CaptainSpeedAdjustment { get; private set; }

    public PlayerController Captain { get; private set; }


    private RaycastHit[] raycastHits = new RaycastHit[16];


    // Use this for initialization

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        movementParticleSystem = movementEffect.GetComponent<ParticleSystem>();
        pathSelector = gameObject.GetComponent<PathSelector>();
        emission = movementParticleSystem.emission;
        rateOverTime = emission.rateOverTime;
        posY = transform.position.y;

        RegisterStreamLabels();
    }

    private void RegisterStreamLabels()
    {
        this.ferryStateLabel = gameManager.StreamLabels.Register("ferry-state", () =>
        {
            if (state == FerryState.Docked)
            {
                return "Currently docked at " + this.Island?.Identifier;
            }

            var destination = "";

            if (PathIndex == 0)
            {
                destination = "Away";
            }

            if (PathIndex == 1)
            {
                destination = "Ironhill";
            }

            if (PathIndex == 2)
            {
                destination = "Kyo";
            }

            if (PathIndex == 3)
            {
                destination = "Heim";
            }

            if (PathIndex == 4)
            {
                destination = "Home";
            }

            if (string.IsNullOrEmpty(destination))
            {
                return "Currently sailing";
            }

            return "Currently sailing towards " + destination;
        });
    }

    public bool Docked => state == FerryState.Docked;

    private void OnBecameVisible()
    {
        this.isVisible = true;
    }

    private void OnBecameInvisible()
    {
        this.isVisible = false;
    }
    // Update is called once per frame
    private void LateUpdate()
    {
        if (GameCache.IsAwaitingGameRestore || !isVisible) return;
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        var pos = transform.position;
        var euler = transform.rotation.eulerAngles;
        var z = Mathf.Sin(Time.time) * (wave * 2f) - wave;
        var y = Mathf.Sin(Time.time) * (boyance * 2f) - boyance;
        var x = Mathf.Sin(Time.time) * (bobEffect * 2f) - bobEffect;

        transform.rotation = Quaternion.Euler(x, euler.y, z);
        transform.position = new Vector3(pos.x, posY + y, pos.z);
        movementEffect.SetActive(state == FerryState.Moving);
    }

    public void IslandEnter(IslandController island)
    {
        this.island = island;
    }

    public void IslandExit()
    {
        island = null;
    }

    public bool IsCaptainPosition(Transform transform)
    {
        return transform && (transform == playerPositions[0] || transform.position == playerPositions[0].position);
    }

    public Transform GetNextPlayerPoint(bool includeCaptainPosition = true)
    {
        if (includeCaptainPosition)
        {
            var captainPosition = playerPositions[0];
            if (captainPosition.childCount == 0) return captainPosition;
        }

        return playerPositions
            .Skip(1)
            .OrderBy(x => x.transform.childCount + UnityEngine.Random.value)
            //.ThenBy(x => UnityEngine.Random.value)
            .FirstOrDefault();
    }

    internal void SetState(FerryState newState)
    {
        this.state = newState;

        if (state == FerryState.Docked)
        {
            var newIsland = gameManager.Islands.FindIsland(this.transform.position);
            if (newIsland)
            {
                IslandEnter(newIsland);
            }
        }
        else
        {
            IslandExit();
        }

        if (ferryStateLabel != null)
        {
            ferryStateLabel.Update();
        }
    }

    private void Update()
    {
        if (!isVisible) return;
        
        if (Captain && playerPositions[0].childCount == 0)
        {
            SetCaptain(null);
        }

        // don't use it right now.
        return;

        try
        {
            if (!gameManager || gameManager == null || ui == null || !ui)
            {
                return;
            }

            if (gameManager && (gameManager.RavenNest == null || !gameManager.RavenNest.Authenticated))
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                var activeCamera = Camera.main;
                if (!activeCamera || activeCamera == null)
                {
                    return;
                }

                var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
                var hitCount = Physics.RaycastNonAlloc(ray, raycastHits);
                for(var i = 0; i < hitCount; ++i)
                {
                    var hit = raycastHits[i];

                    if (hit.collider.CompareTag("Ferry"))
                    {
                        ui.ShowDialog();
                        return;
                    }
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError(exc.ToString());
        }
    }

    public void SetMovementEffect(float v)
    {
        emission.rateOverTime = rateOverTime.constant * v;
    }

    internal void AssignBestCaptain()
    {
        var players = GetComponentsInChildren<PlayerController>();
        if (players.Length == 0) return;
        var nextCaptain = players
            .OrderByDescending(x => x.Stats.Sailing.MaxLevel)
            .FirstOrDefault();
        if (nextCaptain != null)
        {
            PromoteToCaptain(nextCaptain);
        }
    }

    private void PromoteToCaptain(PlayerController nextCaptain)
    {
        nextCaptain.transform.SetParent(playerPositions[0]);
        SetCaptain(nextCaptain);
    }

    internal void SetCaptain(PlayerController newCaptain)
    {
        if (newCaptain != null)
        {
            CaptainSpeedAdjustment = newCaptain.Stats.Sailing.MaxLevel;
        }
        else
        {
            CaptainSpeedAdjustment = 0;
        }

        this.Captain = newCaptain;
    }
}
