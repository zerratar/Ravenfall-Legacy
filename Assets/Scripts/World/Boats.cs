using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The boats moored about the islands ride the sea's waves (<see cref="Ocean.Float"/>), lifting and leaning as the swell
/// goes under them. With the weather and ambient effects setting, and still in potato mode (<see cref="Ambience"/>).
/// The ferry rides them too, on its own (<see cref="FerryController"/>).
/// </summary>
/// <remarks>
/// Found by their kit names when play starts, and only those afloat: the hull down at the sea's level with water, not
/// ground, under it (a boat drawn up on a beach stays put). Each is moved from where it was placed, so it never drifts.
/// </remarks>
public sealed class Boats : MonoBehaviour
{
    /// <summary>How much of the water's lift and lean a moored boat takes; its moorings hold it some.</summary>
    private const float Ride = 0.8f;

    private readonly List<Moored> moored = new List<Moored>();
    private bool still;

    private struct Moored
    {
        public Transform Boat;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Along, Across; // the hull's length and width, in its own axes
        public float Length, Width;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isPlaying) return;
        var go = new GameObject("Boats") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<Boats>();
    }

    private void Start()
    {
        var ocean = FindAnyObjectByType<Ocean>();
        if (!ocean) return;
        var sea = ocean.transform.position.y;
        var terrain = Terrain.activeTerrain;
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
        {
            var name = renderer.name;
            if (!name.StartsWith("SM_Veh_Boat_01") && !name.StartsWith("SM_Veh_FishingBoat")) continue;
            if (renderer.GetComponentInParent<FerryController>()) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;

            var bounds = renderer.bounds;
            var ground = terrain ? terrain.SampleHeight(bounds.center) + terrain.transform.position.y : sea - 10f;
            if (bounds.min.y > sea + 0.5f || bounds.max.y < sea || ground > sea - 0.2f) continue;

            var size = Vector3.Scale(filter.sharedMesh.bounds.size, renderer.transform.lossyScale);
            var alongZ = size.z >= size.x;
            var t = renderer.transform;
            moored.Add(new Moored
            {
                Boat = t,
                Position = t.position,
                Rotation = t.rotation,
                Along = alongZ ? Vector3.forward : Vector3.right,
                Across = alongZ ? Vector3.right : Vector3.forward,
                Length = alongZ ? size.z : size.x,
                Width = alongZ ? size.x : size.z,
            });
        }
    }

    private void Update()
    {
        var on = Ambience.Enabled && !Ambience.Suppressed;
        if (!on)
        {
            // back where they were put, once
            if (still) return;
            still = true;
            foreach (var m in moored) if (m.Boat) m.Boat.SetPositionAndRotation(m.Position, m.Rotation);
            return;
        }
        still = false;

        foreach (var m in moored)
        {
            if (!m.Boat) continue;
            if (!Ocean.Float(m.Position, m.Rotation * m.Along, m.Rotation * m.Across, m.Length, m.Width, out var heave, out var up)) continue;
            var lean = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, up, Ride));
            m.Boat.SetPositionAndRotation(m.Position + Vector3.up * (heave * Ride), lean * m.Rotation);
        }
    }
}
