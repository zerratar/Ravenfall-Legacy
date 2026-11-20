using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A frozen pond's ice (the Glacier's pond, Rimeholt's fishing pond): a disc of <see cref="Radius"/> laid flat at this
/// object, drawn with the Frozen Water shader (Assets/World/Water/FrozenWater.shader), clear ice over deep water with
/// cracks, frost and bubbles. Set it a little over the pond's flat bed and a little wider than the pond: where the
/// shore rises the terrain covers its rim, and the shader fades it into the snow. <see cref="Holes"/> are holes cut in
/// the ice (ice fishing), in this object's space, x and z with the radius in y.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class FrozenPond : MonoBehaviour
{
    private const int MaxHoles = 8;
    private static readonly int HolesId = Shader.PropertyToID("_Holes");
    private static readonly int HoleCountId = Shader.PropertyToID("_HoleCount");

    public float Radius = 12f;
    [Tooltip("Holes in the ice: x and z in this object's space, the radius in y.")]
    public List<Vector3> Holes = new List<Vector3>();

    private Mesh mesh;
    private MaterialPropertyBlock block;
    private readonly Vector4[] holes = new Vector4[MaxHoles];

    private void OnEnable()
    {
        Build();
        Apply();
    }

    private void OnDisable()
    {
        GrassRenderer.Remove(this);
        if (mesh) DestroyImmediate(mesh);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        Build();
        Apply();
    }
#endif

    /// <summary>The disc: rings out from the middle, uv the position over the radius.</summary>
    private void Build()
    {
        const int Segments = 72, Rings = 6;
        if (!mesh) mesh = new Mesh { name = "Frozen Pond", hideFlags = HideFlags.DontSave };
        var verts = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { Vector2.zero };
        var tris = new List<int>();
        for (var ring = 1; ring <= Rings; ring++)
        {
            var f = ring / (float)Rings;
            for (var s = 0; s < Segments; s++)
            {
                var a = s / (float)Segments * Mathf.PI * 2f;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * f;
                verts.Add(new Vector3(u.x * Radius, 0f, u.y * Radius));
                uvs.Add(u);
            }
        }
        for (var s = 0; s < Segments; s++) tris.AddRange(new[] { 0, 1 + (s + 1) % Segments, 1 + s });
        for (var ring = 1; ring < Rings; ring++)
        {
            var inner = 1 + (ring - 1) * Segments;
            var outer = 1 + ring * Segments;
            for (var s = 0; s < Segments; s++)
            {
                var s1 = (s + 1) % Segments;
                tris.AddRange(new[] { inner + s, inner + s1, outer + s, outer + s, inner + s1, outer + s1 });
            }
        }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    /// <summary>The holes to the shader, in world space.</summary>
    private void Apply()
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        block ??= new MaterialPropertyBlock();
        var count = Mathf.Min(MaxHoles, Holes.Count);
        var scale = Mathf.Abs(transform.lossyScale.x);
        for (var i = 0; i < MaxHoles; i++)
        {
            if (i >= count) { holes[i] = Vector4.zero; continue; }
            var h = Holes[i];
            var w = transform.TransformPoint(new Vector3(h.x, 0f, h.z));
            holes[i] = new Vector4(w.x, w.z, h.y * scale, 0f);
        }
        // no grass through the ice: a round keep-out as wide as the pond (a road of one point)
        var c = transform.position;
        GrassRenderer.SetRoad(this, new[] { new Vector2(c.x, c.z), new Vector2(c.x + 0.01f, c.z) }, Radius * 0.9f * Mathf.Abs(transform.lossyScale.x) * 2f, null);
        block.SetVectorArray(HolesId, holes);
        block.SetFloat(HoleCountId, count);
        renderer.SetPropertyBlock(block);
    }
}
