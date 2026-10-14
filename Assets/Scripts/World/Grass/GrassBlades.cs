using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A clump of grass blades as a mesh, 1 m tall: thin tapered strips standing round a point, each leaning and
/// curving its own way. Made in code, so no asset has to be kept for it; the same settings always give the same
/// clump. Far away a clump is drawn with half the blades, wider, of one segment each.
/// </summary>
public static class GrassBlades
{
    public static Mesh Build(int blades, float width, int seed, int segments = 3)
    {
        segments = Mathf.Max(1, segments);
        var random = new System.Random(seed);
        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (var b = 0; b < blades; ++b)
        {
            // where it stands in the clump, how tall, and how far it leans: outwards from the middle, as a tuft
            // fans out, so its blades show their faces when seen from above as well as from the side
            var angle = (b + Next(-0.3f, 0.3f)) / blades * Mathf.PI * 2f;
            var radius = Next(0.02f, 0.16f);
            var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var root = outward * radius;
            var turn = Next(-0.6f, 0.6f);
            var leanDirection = new Vector3(outward.x * Mathf.Cos(turn) - outward.z * Mathf.Sin(turn), 0f, outward.x * Mathf.Sin(turn) + outward.z * Mathf.Cos(turn));
            var across = new Vector3(-leanDirection.z, 0f, leanDirection.x);
            var height = Next(0.55f, 1f);
            var lean = Next(0.25f, 0.65f) * (1.1f - height * 0.4f);
            var bladeWidth = width * Next(0.7f, 1.2f);

            var first = vertices.Count;
            for (var s = 0; s <= segments; ++s)
            {
                var t = s / (float)segments;
                // a curve that leans more towards the tip
                var centre = root + leanDirection * (lean * t * t) + Vector3.up * (height * t);
                var halfWidth = bladeWidth * 0.5f * (1f - t * 0.85f);
                var normal = Vector3.Cross(across, (leanDirection * (2f * lean * t) + Vector3.up * height).normalized).normalized;
                if (normal.y < 0f) normal = -normal;
                if (s < segments)
                {
                    vertices.Add(centre - across * halfWidth);
                    vertices.Add(centre + across * halfWidth);
                    normals.Add(normal);
                    normals.Add(normal);
                    uvs.Add(new Vector2(0f, t));
                    uvs.Add(new Vector2(1f, t));
                }
                else
                {
                    vertices.Add(centre);
                    normals.Add(normal);
                    uvs.Add(new Vector2(0.5f, 1f));
                }
            }

            for (var s = 0; s < segments - 1; ++s)
            {
                var a = first + s * 2;
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var last = first + (segments - 1) * 2;
            triangles.AddRange(new[] { last, last + 2, last + 1 });
        }

        var mesh = new Mesh { name = "Grass Blades " + blades };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.hideFlags = HideFlags.DontSave;
        return mesh;
    }
}
