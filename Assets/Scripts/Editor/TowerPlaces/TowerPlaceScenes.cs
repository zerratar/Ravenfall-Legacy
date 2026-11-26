using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     Opening, framing, saving and closing tower places for Ravenfall > Tower Places. A place opens beside the open
///     scenes (MainWorld stays open, unsaved and untouched) and becomes the active scene while it is open, so what is
///     dragged into the Scene view lands in the place and not in MainWorld. Every tower scene is built on the same spot,
///     so one is open at a time: the one before is closed first (asking to save it when it has changes). Only tower
///     scenes are ever saved or closed here.
/// </summary>
internal static class TowerPlaceScenes
{
    private const string ActiveBefore = "Ravenfall.TowerPlaces.ActiveBefore";

    internal static bool IsTowerScene(Scene s) => s.IsValid() && !string.IsNullOrEmpty(s.path) && s.path.StartsWith(TowerPlaceCatalog.SceneFolder + "/");

    internal static List<Scene> OpenTowerScenes()
    {
        var list = new List<Scene>();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && IsTowerScene(s)) list.Add(s);
        }
        return list;
    }

    internal static Scene Loaded(string path)
    {
        var s = SceneManager.GetSceneByPath(path);
        return s.IsValid() && s.isLoaded ? s : default;
    }

    internal static TowerPlace PlaceIn(Scene s)
    {
        if (!s.IsValid() || !s.isLoaded) return null;
        foreach (var root in s.GetRootGameObjects())
        {
            var p = root.GetComponent<TowerPlace>();
            if (p) return p;
        }
        return null;
    }

    /// <summary>Opens a tower scene beside the open ones (closing another tower scene first); false when it could not.</summary>
    internal static bool Open(string path, out Scene scene, out string error)
    {
        scene = default;
        error = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { error = "Stop play mode first: places open in edit mode."; return false; }
        if (!File.Exists(path)) { error = "There is no scene at " + path + "."; return false; }
        var already = Loaded(path);
        if (already.IsValid())
        {
            scene = already;
            MakeActive(scene);
            return true;
        }
        foreach (var other in OpenTowerScenes())
        {
            if (other.isDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex("Tower Places",
                    other.name + " has changes that are not saved. Save them before opening " + Path.GetFileNameWithoutExtension(path) + "?\n\nTower scenes are built on the same spot, so one is open at a time.",
                    "Save", "Cancel", "Don't save");
                if (choice == 1) { error = "Canceled."; return false; }
                if (choice == 0 && !Save(other, out error)) return false;
            }
            if (!Close(other)) { error = "Could not close " + other.name + "."; return false; }
        }
        // a lone empty untitled scene gives way; anything else stays open
        var single = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(SceneManager.GetSceneAt(0).path) && !SceneManager.GetSceneAt(0).isDirty;
        var active = SceneManager.GetActiveScene();
        if (!single && active.IsValid() && !IsTowerScene(active)) SessionState.SetString(ActiveBefore, active.path);
        scene = EditorSceneManager.OpenScene(path, single ? OpenSceneMode.Single : OpenSceneMode.Additive);
        if (!scene.IsValid()) { error = "Unity could not open " + path + "."; return false; }
        MakeActive(scene);
        TowerPlaceMarkers.MarkStale();
        return true;
    }

    private static void MakeActive(Scene scene)
    {
        if (SceneManager.GetActiveScene() != scene) SceneManager.SetActiveScene(scene);
    }

    /// <summary>Closes a tower scene (never another), the scene that was active before it active again.</summary>
    internal static bool Close(Scene scene)
    {
        if (!IsTowerScene(scene)) return false;
        if (SceneManager.GetActiveScene() == scene)
        {
            var before = SceneManager.GetSceneByPath(SessionState.GetString(ActiveBefore, ""));
            if (!before.IsValid() || !before.isLoaded)
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s != scene && s.isLoaded) { before = s; break; }
                }
            }
            if (before.IsValid() && before.isLoaded) SceneManager.SetActiveScene(before);
        }
        if (SceneManager.sceneCount <= 1)
        {
            // the only scene: an empty one takes its place
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }
        var ok = EditorSceneManager.CloseScene(scene, true);
        TowerPlaceMarkers.MarkStale();
        return ok;
    }

    /// <summary>Saves one tower scene (never another); its change list is written as it saves (TowerPlaceEdits).</summary>
    internal static bool Save(Scene scene, out string error)
    {
        error = null;
        if (!IsTowerScene(scene)) { error = "Only tower scenes are saved here."; return false; }
        if (EditorApplication.isPlaying) { error = "Stop play mode first."; return false; }
        if (!EditorSceneManager.SaveScene(scene)) { error = "Unity could not save " + scene.name + "."; return false; }
        return true;
    }

    /// <summary>The Scene view looks at a place from above its arrival, toward its middle.</summary>
    internal static void Frame(TowerPlace place, Vector3? at = null, float size = -1f)
    {
        var view = SceneView.lastActiveSceneView;
        if (!view)
        {
            view = EditorWindow.GetWindow<SceneView>();
        }
        if (!view || !place) return;
        var centre = place.Centre;
        var spawn = place.Spawn;
        var dir = centre - spawn; dir.y = 0f;
        var yaw = dir.sqrMagnitude > 1f ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;
        var target = at ?? Vector3.Lerp(spawn, centre, 0.5f);
        if (size <= 0f) size = Mathf.Clamp(place.Radius * 0.5f, 25f, 90f);
        view.in2DMode = false;
        view.LookAt(target, Quaternion.Euler(48f, yaw, 0f), size, false, false);
        view.Repaint();
    }

    /// <summary>The Scene view looks at one point closely.</summary>
    internal static void FrameAt(Vector3 point, float size = 12f)
    {
        var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
        if (!view) return;
        var rotation = view.rotation;
        if (Vector3.Angle(rotation * Vector3.forward, Vector3.down) > 80f) rotation = Quaternion.Euler(45f, rotation.eulerAngles.y, 0f);
        view.LookAt(point, rotation, size, false, false);
        view.Repaint();
    }
}
