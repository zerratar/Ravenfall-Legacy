using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class GraphicsToggler : MonoBehaviour
{
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera[] cameras;

    [SerializeField] private Canvas canvas2d;

    [SerializeField] private GameObject ui2d;
    [SerializeField] private GameObject ui3d;
    [SerializeField] private GameObject graphy;
    [SerializeField] private GameObject blackScreen;

    public bool DisableCameras = true;
    public bool DisableSkinnedRenderers;
    public bool DisableRenderers;

    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private MeshRenderer[] meshRenderers;
    private int previousTargetFramerate;
    private int previousVsyncCount;
    public bool graphicsEnabled = true;
    private int renderFrameInterval = 10;
    private int targetFramesPerSeconds = 6;
    private HashSet<int> ignoreList = new HashSet<int>();

    //private readonly SemaphoreSlim mutex = new SemaphoreSlim(1);
    public static bool GraphicsEnabled = true;
    public static SimulationMode SimulationMode = SimulationMode.FixedUpdate;

    public bool IsGraphicsEnabled => graphicsEnabled;

    public static void EnablePhysics()
    {
        SimulationMode = SimulationMode.FixedUpdate;
        Physics.simulationMode = SimulationMode;
    }

    public static void DisablePhysics()
    {
        SimulationMode = SimulationMode.Script;
        Physics.simulationMode = SimulationMode;
    }

    // Start is called before the first frame update
    void Start()
    {
        this.skinnedMeshRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        this.meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        this.previousTargetFramerate = Application.targetFrameRate;

        if (Application.isBatchMode)
        {
            //ToggleAllGraphics();            
            ReduceRenderTarget();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isBatchMode)
        {
            GraphicsEnabled = false;

            //mutex.Wait(TimeSpan.FromMilliseconds(10));
            return;
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleAllGraphics();
        }

        GraphicsEnabled = graphicsEnabled;
    }

    private void ToggleAllGraphics()
    {
        StartCoroutine(ToggleGraphicsImpl());
    }

    private IEnumerator ToggleGraphicsImpl()
    {
        if (graphicsEnabled)
        {
            this.blackScreen.SetActive(true);
            this.ui2d.SetActive(false);
            this.ui3d.SetActive(false);
            this.graphy.SetActive(false);

            canvas2d.worldCamera = uiCamera;
        }
        else
        {
            this.blackScreen.SetActive(false);
            this.ui2d.SetActive(true);
            this.ui3d.SetActive(true);
            this.graphy.SetActive(true);

            canvas2d.worldCamera = mainCamera;
        }

        yield return null;

        graphicsEnabled = !graphicsEnabled;

        if (graphicsEnabled)
        {
            uiCamera.enabled = false;

            RestoreRenderTarget();
        }
        else
        {
            uiCamera.enabled = true;
            ReduceRenderTarget();
        }

        yield return null;

        if (DisableCameras)
        {
            ToggleBehaviours(cameras);
        }
        if (DisableSkinnedRenderers)
        {
            ToggleRenderers(skinnedMeshRenderers);
        }
        if (DisableRenderers)
        {
            ToggleRenderers(meshRenderers);
        }
        yield return null;

    }

    private void RestoreRenderTarget()
    {
        //Physics.autoSimulation = true;
        Physics.simulationMode = SimulationMode;
        OnDemandRendering.renderFrameInterval = 0;
        Application.targetFrameRate = previousTargetFramerate;
        QualitySettings.vSyncCount = previousVsyncCount;
    }

    private void ReduceRenderTarget()
    {
        Physics.simulationMode = SimulationMode.Script;
        OnDemandRendering.renderFrameInterval = renderFrameInterval;
        previousTargetFramerate = Application.targetFrameRate;
        previousVsyncCount = QualitySettings.vSyncCount;
        Application.targetFrameRate = targetFramesPerSeconds;
        QualitySettings.vSyncCount = 2;
    }

    private void ToggleBehaviours(Behaviour[] renderer)
    {
        for (var i = 0; i < renderer.Length; ++i)
        {
            if (ignoreList.Contains(renderer[i].GetInstanceID()))
            {
                continue;
            }

            var enabled = renderer[i].enabled;
            if (graphicsEnabled == enabled)
            {
                this.ignoreList.Add(renderer[i].GetInstanceID());
                continue;
            }

            renderer[i].enabled = graphicsEnabled;
        }
    }
    private void ToggleRenderers(Renderer[] renderer)
    {
        for (var i = 0; i < renderer.Length; ++i)
        {
            if (ignoreList.Contains(renderer[i].GetInstanceID()))
            {
                continue;
            }

            var enabled = renderer[i].enabled;
            if (graphicsEnabled == enabled)
            {
                this.ignoreList.Add(renderer[i].GetInstanceID());
                continue;
            }
            renderer[i].enabled = graphicsEnabled;
        }
    }
}
