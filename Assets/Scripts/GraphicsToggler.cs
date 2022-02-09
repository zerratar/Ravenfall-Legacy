using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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
    private bool graphicsEnabled = true;
    private HashSet<int> ignoreList = new HashSet<int>();

    private readonly SemaphoreSlim mutex = new SemaphoreSlim(1);

    public bool IsGraphicsEnabled => graphicsEnabled;

    // Start is called before the first frame update
    void Start()
    {
        this.skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
        this.meshRenderers = FindObjectsOfType<MeshRenderer>();
        this.previousTargetFramerate = Application.targetFrameRate;

        if (Application.isBatchMode)
        {
            //ToggleAllGraphics();
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 2;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isBatchMode)
        {
            mutex.Wait(TimeSpan.FromMilliseconds(10));
            return;
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleAllGraphics();
        }

        if (!graphicsEnabled)
        {
            //uiCamera.enabled = Time.frameCount % 90 == 0;
            //System.Threading.Thread.Sleep(5);

            mutex.Wait(TimeSpan.FromMilliseconds(10));
        }
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
            mutex.Release();
            uiCamera.enabled = false;
            Application.targetFrameRate = previousTargetFramerate;
            QualitySettings.vSyncCount = previousVsyncCount;
        }
        else
        {
            uiCamera.enabled = true;
            previousTargetFramerate = Application.targetFrameRate;
            previousVsyncCount = QualitySettings.vSyncCount;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 2;
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
