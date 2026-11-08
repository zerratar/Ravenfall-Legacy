using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps "Draw Instanced" off on terrains drawn with the Ravenfall/Terrain shader: checked once a second in the
/// editor, before a scene is saved, when entering play mode and in builds.
/// </summary>
/// <remarks>
/// Ravenfall/Terrain has no terrain instancing path. Drawn instanced, a terrain sends flat patches the shader never
/// lifts by the heightmap, so the islands vanish under the sea and long triangles flicker out from (0, 0, 0).
/// On Unity 6000.7.0b7 setting Terrain.drawInstanced on one terrain sets it on every loaded terrain, whatever their
/// Grouping IDs (loading a scene saved with it on does not). On 2026-11-08 the tower floor build scripts set it on
/// their new terrains while MainWorld was open, and all of MainWorld's terrains followed.
/// </remarks>
[InitializeOnLoad]
public class TerrainInstancingGuard : IProcessSceneWithReport
{
    private const string ShaderName = "Ravenfall/Terrain";
    private static double nextCheck;

    public int callbackOrder => 0;

    static TerrainInstancingGuard()
    {
        EditorSceneManager.sceneSaving += (scene, path) => Fix(scene, " before saving");
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1.0;
            foreach (var terrain in Terrain.activeTerrains) FixTerrain(terrain, "");
        };
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        Fix(scene, "");
    }

    private static void Fix(Scene scene, string when)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                FixTerrain(terrain, when);
    }

    private static void FixTerrain(Terrain terrain, string when)
    {
        if (!terrain || !terrain.drawInstanced) return;
        var material = terrain.materialTemplate;
        if (!material || !material.shader || material.shader.name != ShaderName) return;

        // this turns it off on every loaded terrain (see the remarks)
        terrain.drawInstanced = false;
        Debug.LogWarning($"TerrainInstancingGuard: '{terrain.name}' in {terrain.gameObject.scene.name} had Draw Instanced on, "
            + $"which {ShaderName} cannot draw; turned it off{when}. Setting it on any terrain sets it on all loaded terrains.", terrain);
    }
}
