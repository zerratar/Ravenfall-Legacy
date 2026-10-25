using UnityEngine;

/// <summary>
/// The ground a building covers: a rectangle, turned with the building. Placement used circles
/// around each building and a square drawn from the widest side, so the outline did not match the
/// building and long buildings kept each other further apart than they looked. Every check, the
/// drawn outline and the rings round buildings in the way now use this one shape.
/// </summary>
public struct Footprint
{
    /// <summary>Half the width (x) and depth (y) of the rectangle, in metres.</summary>
    public Vector2 Half;
    /// <summary>Where the rectangle's middle sits from the building's origin, unturned.</summary>
    public Vector2 Offset;

    public Footprint(Vector2 half, Vector2 offset)
    {
        Half = half;
        Offset = offset;
    }

    public static readonly Footprint Default = new Footprint(new Vector2(2.5f, 2.5f), Vector2.zero);

    /// <summary>The same rectangle, larger on every side by <paramref name="margin"/>.</summary>
    public Footprint Grown(float margin) => new Footprint(Half + new Vector2(margin, margin), Offset);

    /// <summary>The middle of the rectangle for a building standing at <paramref name="position"/>.</summary>
    public Vector3 Centre(Vector3 position, float rotation)
    {
        return position + Quaternion.Euler(0f, rotation, 0f) * new Vector3(Offset.x, 0f, Offset.y);
    }

    /// <summary>The four corners, in order round the rectangle.</summary>
    public Vector3[] Corners(Vector3 position, float rotation)
    {
        var turn = Quaternion.Euler(0f, rotation, 0f);
        var c = Centre(position, rotation);
        return new[]
        {
            c + turn * new Vector3(-Half.x, 0f, -Half.y),
            c + turn * new Vector3(-Half.x, 0f, Half.y),
            c + turn * new Vector3(Half.x, 0f, Half.y),
            c + turn * new Vector3(Half.x, 0f, -Half.y),
        };
    }

    /// <summary>
    /// Measured from the colliders, or the renderers when there are none to go by, with the object
    /// turned straight while measuring so a turned building does not grow its rectangle. No side is
    /// shorter than <paramref name="minHalf"/> from the middle (a metre for buildings; decor is smaller).
    /// </summary>
    public static Footprint Measure(GameObject building, bool useRenderers = false, float minHalf = 1f)
    {
        if (!building) return Default;

        var transform = building.transform;
        var rotation = transform.rotation;
        transform.rotation = Quaternion.identity;
        // collider bounds follow the transform only once physics hears of the change
        Physics.SyncTransforms();
        try
        {
            Bounds bounds;
            if (!TryBounds(building, useRenderers, out bounds))
            {
                return Default;
            }

            var offset = bounds.center - transform.position;
            var half = new Vector2(Mathf.Max(minHalf, bounds.extents.x), Mathf.Max(minHalf, bounds.extents.z));
            return new Footprint(half, new Vector2(offset.x, offset.z));
        }
        finally
        {
            transform.rotation = rotation;
            Physics.SyncTransforms();
        }
    }

    private static bool TryBounds(GameObject building, bool useRenderers, out Bounds bounds)
    {
        bounds = default;
        var found = false;
        if (!useRenderers)
        {
            foreach (var collider in building.GetComponentsInChildren<Collider>())
            {
                // trigger volumes (pick areas, auras) say nothing about the walls
                if (collider.isTrigger || !collider.enabled) continue;
                if (!found) { bounds = collider.bounds; found = true; }
                else bounds.Encapsulate(collider.bounds);
            }
        }

        if (found) return true;

        foreach (var renderer in building.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        return found;
    }

    /// <summary>Whether the point (x, z) lies inside the rectangle.</summary>
    public bool Contains(Vector3 position, float rotation, Vector2 point)
    {
        var centre = Centre(position, rotation);
        var local = Quaternion.Euler(0f, -rotation, 0f) * new Vector3(point.x - centre.x, 0f, point.y - centre.z);
        return Mathf.Abs(local.x) <= Half.x && Mathf.Abs(local.z) <= Half.y;
    }

    /// <summary>Whether two turned rectangles overlap, by the separating axis test.</summary>
    public static bool Overlaps(Footprint a, Vector3 positionA, float rotationA, Footprint b, Vector3 positionB, float rotationB)
    {
        var ca = a.Corners(positionA, rotationA);
        var cb = b.Corners(positionB, rotationB);
        return !Separated(ca, cb, rotationA) && !Separated(ca, cb, rotationB);
    }

    // The two axes of a rectangle turned by rotation; separated on either means no overlap.
    private static bool Separated(Vector3[] a, Vector3[] b, float rotation)
    {
        var turn = Quaternion.Euler(0f, rotation, 0f);
        return Gap(a, b, turn * Vector3.right) || Gap(a, b, turn * Vector3.forward);
    }

    private static bool Gap(Vector3[] a, Vector3[] b, Vector3 axis)
    {
        Project(a, axis, out var minA, out var maxA);
        Project(b, axis, out var minB, out var maxB);
        return maxA <= minB || maxB <= minA;
    }

    private static void Project(Vector3[] corners, Vector3 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (var corner in corners)
        {
            var d = corner.x * axis.x + corner.z * axis.z;
            if (d < min) min = d;
            if (d > max) max = d;
        }
    }
}
