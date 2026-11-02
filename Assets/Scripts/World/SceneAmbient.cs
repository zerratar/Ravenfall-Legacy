using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The scene's ambient light (RenderSettings.ambientProbe, which the day and night cycle sets) as a global for shaders
/// that light themselves: _RavenfallSH, seven vectors as unity_SHAr to unity_SHC. The Medium and High quality pipelines
/// use probe volumes, with nothing baked, so a shader without the probe volume variants reads empty per-object
/// ambient: the terrain's slopes facing away from the sun went black (Home's shore, playtest 2026-11-02). Set before
/// every frame, in the editor too.
/// </summary>
public static class SceneAmbient
{
    private static readonly int Id = Shader.PropertyToID("_RavenfallSH");
    private static readonly Vector4[] values = new Vector4[7];
    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        RenderPipelineManager.beginContextRendering += (_, __) => Set();
    }

    private static void Set()
    {
        var sh = new SHCoefficients(RenderSettings.ambientProbe);
        values[0] = sh.SHAr;
        values[1] = sh.SHAg;
        values[2] = sh.SHAb;
        values[3] = sh.SHBr;
        values[4] = sh.SHBg;
        values[5] = sh.SHBb;
        values[6] = sh.SHC;
        Shader.SetGlobalVectorArray(Id, values);
    }
}
