using UnityEngine;

/// <summary>
///     A place's own sky (tower band 3, the Storm Spire): the world's sunny sky does not belong over a storm, and a place
///     only sets the fog and the light (TowerManager). While the camera is at the place, a dome round the camera hides the
///     world's sky: <see cref="Zenith"/> overhead, fading to <see cref="Horizon"/> (the place's fog colour, so it meets the
///     haze) at the horizon. Cheap: one mesh, no lights, no fog on it.
/// </summary>
public sealed class TowerSky : MonoBehaviour
{
    public Color Zenith = new Color(0.20f, 0.22f, 0.29f);
    public Color Horizon = new Color(0.66f, 0.70f, 0.78f);

    /// <summary>How far round the camera the dome is (inside its far clip).</summary>
    public float DomeRadius = 900f;

    /// <summary>The camera's reach that shows the dome.</summary>
    public float CameraRadius = 260f;

    private GameObject dome;
    private static Mesh mesh;

    private void LateUpdate()
    {
        var cam = Camera.main;
        var show = cam && (cam.transform.position - transform.position).sqrMagnitude <= CameraRadius * CameraRadius;
        if (!show)
        {
            if (dome && dome.activeSelf) dome.SetActive(false);
            return;
        }
        if (!dome) Make();
        if (!dome.activeSelf) dome.SetActive(true);
        dome.transform.position = cam.transform.position;
        dome.transform.localScale = Vector3.one * DomeRadius;
    }

    private void OnDestroy()
    {
        if (dome) Destroy(dome);
    }

    private void Make()
    {
        if (!mesh) mesh = Dome();
        dome = new GameObject("Tower Sky") { hideFlags = HideFlags.DontSave };
        dome.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = dome.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // vertex colours carry the gradient: the particles' unlit shader multiplies them in and takes no fog here
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!shader) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader) { name = "Tower Sky", renderQueue = 1500 };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.DisableKeyword("_FOG");
        renderer.sharedMaterial = material;
        // the colours live in the mesh, shared by every dome: write this place's into a copy
        var own = Instantiate(mesh);
        var colors = new Color[own.vertexCount];
        var vertices = own.vertices;
        for (var i = 0; i < vertices.Length; i++)
        {
            var up = Mathf.Clamp01(vertices[i].y);
            colors[i] = Color.Lerp(Horizon, Zenith, Mathf.Pow(up, 0.55f));
        }
        own.colors = colors;
        dome.GetComponent<MeshFilter>().sharedMesh = own;
    }

    /// <summary>A unit sphere seen from inside, finer near the horizon where the colour changes most.</summary>
    private static Mesh Dome()
    {
        const int Rings = 24, Segments = 48;
        var vertices = new Vector3[(Rings + 1) * (Segments + 1)];
        for (var r = 0; r <= Rings; r++)
        {
            // from the bottom (-90) to the top (+90)
            var lat = Mathf.Lerp(-90f, 90f, r / (float)Rings) * Mathf.Deg2Rad;
            for (var s = 0; s <= Segments; s++)
            {
                var lon = s / (float)Segments * Mathf.PI * 2f;
                vertices[r * (Segments + 1) + s] = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
            }
        }
        var triangles = new int[Rings * Segments * 6];
        var t = 0;
        for (var r = 0; r < Rings; r++)
            for (var s = 0; s < Segments; s++)
            {
                var a = r * (Segments + 1) + s;
                var b = a + Segments + 1;
                // wound to face inward
                triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = a + 1; triangles[t++] = b + 1;
            }
        var m = new Mesh { name = "Tower Sky Dome", vertices = vertices, triangles = triangles };
        m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        return m;
    }
}
