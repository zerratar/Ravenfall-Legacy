using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Renders a building prefab once into a small texture, for the build mode cards.
/// </summary>
/// <remarks>
/// The old building dialog gave every building its own live camera rendering continuously into a
/// shared render texture, which costs a camera per card for as long as the dialog exists and runs
/// out of textures past the fixed set. Here each prefab is placed far below the world, framed from
/// its bounds, rendered once, copied into a texture and removed. The result is cached per prefab.
/// The render waits a frame after the building is placed: the GPU resident drawer takes in a new
/// renderer at the start of a frame, so a building rendered the frame it is made is not drawn at all.
/// </remarks>
public static class BuildingPreview
{
    private static readonly Dictionary<GameObject, Texture2D> cache = new Dictionary<GameObject, Texture2D>();
    private static readonly Dictionary<GameObject, List<Action<Texture2D>>> waiting = new Dictionary<GameObject, List<Action<Texture2D>>>();
    private static Runner runner;

    /// <summary>Far enough below the world that nothing else is in frame.</summary>
    private static readonly Vector3 Stage = new Vector3(0f, -5000f, 0f);

    // Each render gets its own spot on the stage. The GPU resident drawer draws a removed object for one more
    // frame, so on one shared spot every picture also showed the building rendered before it: a small tree's
    // picture had a large tree's ghost behind it, which looked like a crown cut off at the edge.
    private const float StageSpacing = 400f;
    private const int StageSpots = 16;
    private static int nextSpot;

    /// <summary>
    /// The building's picture, given to <paramref name="ready"/>: at once when it has been made before, otherwise a
    /// couple of frames from now (null if it could not be made).
    /// </summary>
    public static void Get(GameObject prefab, Action<Texture2D> ready, int size = 256)
    {
        if (!prefab)
        {
            ready?.Invoke(null);
            return;
        }

        if (cache.TryGetValue(prefab, out var cached) && cached)
        {
            ready?.Invoke(cached);
            return;
        }

        if (waiting.TryGetValue(prefab, out var callbacks))
        {
            if (ready != null) callbacks.Add(ready);
            return;
        }

        waiting[prefab] = new List<Action<Texture2D>>();
        if (ready != null) waiting[prefab].Add(ready);
        if (!runner)
        {
            var go = new GameObject("Building Previews") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }
        runner.StartCoroutine(Render(prefab, size));
    }

    private static IEnumerator Render(GameObject prefab, int size)
    {
        GameObject instance = null;
        Texture2D texture = null;
        try
        {
            var spot = Stage + new Vector3(StageSpacing * (nextSpot++ % StageSpots), 0f, 0f);
            instance = Object.Instantiate(prefab, spot, Quaternion.identity);

            // Logic on the building has no business running for a snapshot.
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Building preview failed for " + prefab.name + ": " + exc.Message);
        }

        // a frame for the renderers to be taken in before they are drawn
        yield return null;

        if (instance)
        {
            texture = Snapshot(instance, prefab, size);
            Object.Destroy(instance);
        }

        cache[prefab] = texture;
        if (waiting.TryGetValue(prefab, out var callbacks))
        {
            waiting.Remove(prefab);
            foreach (var ready in callbacks)
            {
                try { ready(texture); }
                catch (Exception exc) { Shinobytes.Debug.LogWarning("Building preview could not be shown: " + exc.Message); }
            }
        }
    }

    private static Texture2D Snapshot(GameObject instance, GameObject prefab, int size)
    {
        GameObject cameraObject = null;
        RenderTexture target = null;
        var previousActive = RenderTexture.active;

        try
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            cameraObject = new GameObject("Building Preview Camera");
            var cam = cameraObject.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            // Three quarter view from the front right and above, like the village camera.
            var radius = bounds.extents.magnitude;
            var distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            var direction = Quaternion.Euler(28f, -35f, 0f) * Vector3.back;
            cam.transform.position = bounds.center + direction * distance;
            cam.transform.LookAt(bounds.center);

            target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 4;
            cam.targetTexture = target;

            // Daylight for the picture, whatever the time in the world: at night the trees and bushes came out as
            // black shapes (the houses only showed for their lit windows). Only for this one render.
            var lightObject = new GameObject("Building Preview Light");
            var keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.96f, 0.88f);
            keyLight.intensity = 1.3f;
            keyLight.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
            var sun = RenderSettings.sun;
            var ambientMode = RenderSettings.ambientMode;
            var ambientLight = RenderSettings.ambientLight;
            var fog = RenderSettings.fog;
            try
            {
                RenderSettings.sun = keyLight;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.56f, 0.6f);
                RenderSettings.fog = false;
                cam.Render();
            }
            finally
            {
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                RenderSettings.fog = fog;
                Object.Destroy(lightObject);
            }

            RenderTexture.active = target;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = prefab.name + " Preview" };
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();
            return texture;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Building preview failed for " + prefab.name + ": " + exc.Message);
            return null;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (target) RenderTexture.ReleaseTemporary(target);
            if (cameraObject) Object.Destroy(cameraObject);
        }
    }

    /// <summary>Only there to run the renders a frame apart.</summary>
    private sealed class Runner : MonoBehaviour { }
}
