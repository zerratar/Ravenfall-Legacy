using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Rendering;
using System.Collections.Generic;

public class StripURPShaders : IPreprocessShaders
{
    // If you have multiple IPreprocessShaders, set order to control the sequence.
    public int callbackOrder => 0;

    // List the shaders you want to strip from the build
    private static readonly HashSet<string> shadersToStrip = new HashSet<string>
    {
        //"Universal Render Pipeline/Particles/Lit",
        //"Universal Render Pipeline/Simple Lit",
        //"Universal Render Pipeline/Lit",
    };

    public void OnProcessShader(
        Shader shader,
        ShaderSnippetData snippet,
        IList<ShaderCompilerData> shaderCompilerData
    )
    {
        // If this shader is one of the shaders we never want to build,
        // clear out all its shader variants so it won't be included.
        if (shadersToStrip.Contains(shader.name))
        {
            shaderCompilerData.Clear();
            // Optionally, log each time you strip a shader:
            Debug.Log($"Stripped shader: {shader.name}");
        }
    }
}