﻿using System;
using UnityEngine;

public class TownHallController : MonoBehaviour
{
    [SerializeField] private TownHallResource resources;
    [SerializeField] private TownHallManager manager;
    [SerializeField] private BoxCollider hitCollider;
    [SerializeField] private Transform infoPos;

    private GameManager gameManager;
    private MeshRenderer meshRenderer;
    private int instanceID;
    private TownHallInfoManager ui;

    public Transform InfoTransform => infoPos;

    // Start is called before the first frame update
    void Start()
    {
        if (!ui) ui = FindAnyObjectByType<TownHallInfoManager>();
        if (!manager) manager = FindAnyObjectByType<TownHallManager>();
        if (!resources) resources = GetComponentInChildren<TownHallResource>();
        if (!hitCollider) hitCollider = GetComponent<BoxCollider>();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

        this.meshRenderer = GetComponentInChildren<MeshRenderer>();
        this.instanceID = hitCollider.GetInstanceID();
    }

    public void SetTownHallResourceController(TownHallResource resx)
    {
        this.resources = resx;
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (!meshRenderer.isVisible || !gameManager || gameManager == null || manager == null || !manager)
            {
                return;
            }

            if (gameManager && (gameManager.RavenNest == null || !gameManager.RavenNest.Authenticated))
            {
                return;
            }

            if (hitCollider && Input.GetMouseButtonUp(0))
            {
                var activeCamera = Camera.main;
                if (!activeCamera || activeCamera == null)
                {
                    return;
                }

                var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
                //var result = Physics.RaycastAll(ray, 5000);
                var result = Physics.RaycastAll(ray, 5000);
                foreach (var res in result)
                {
                    if (res.collider.GetInstanceID() == instanceID)
                    {
                        manager.OpenVillageDialog();
                        return;
                    }
                }
            }
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("TownHallController.Update: " + exc.ToString());
        }
    }

    internal void ResourcesUpdated()
    {
        if (!resources) resources = GetComponentInChildren<TownHallResource>();
        if (resources) resources.ResourcesUpdated(manager);
        if (ui) ui.MakeDirty();
    }
}
