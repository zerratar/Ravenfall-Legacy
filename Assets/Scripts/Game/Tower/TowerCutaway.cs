using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Indoor floors seen from the game's camera (docs/tower-design.md section 22): the camera orbits the player it
///     follows from above, so a wall between it and that player would hide the fight. Each wall piece
///     (<see cref="TowerWallPiece"/>) that stands between the camera and what it looks at is cut down to its low stub
///     (its full height keeps its shadow), and stands full again once the camera has turned past it: the far walls
///     always stand, with their windows, banners and torches. Nothing to set: it costs a few dot products per wall.
/// </summary>
public sealed class TowerCutaway : MonoBehaviour
{
    /// <summary>How far to the sides of the line of sight a wall is still cut, metres (the fight round the player).</summary>
    public float Margin = 7f;

    /// <summary>The camera farther than this from the floor (another place): every wall stands.</summary>
    public float Reach = 260f;

    private TowerWallPiece[] pieces = new TowerWallPiece[0];
    private Camera cam;
    private OrbitCamera orbit;

    private void OnEnable()
    {
        pieces = GetComponentsInChildren<TowerWallPiece>(true);
    }

    private void OnDisable()
    {
        foreach (var p in pieces) if (p) p.SetCut(false);
    }

    private void LateUpdate()
    {
        if (!cam || !cam.isActiveAndEnabled)
        {
            cam = Camera.main;
            orbit = cam ? cam.GetComponent<OrbitCamera>() : null;
        }
        if (!cam) return;
        var eye = cam.transform.position;
        var near = (eye - transform.position).sqrMagnitude < Reach * Reach;
        var focus = near ? Focus(eye) : eye;
        for (var i = 0; i < pieces.Length; i++)
        {
            var p = pieces[i];
            if (p) p.SetCut(near && Between(p, eye, focus));
        }
    }

    /// <summary>What the camera looks at: the player the orbit camera follows, else where its view meets the floor.</summary>
    private Vector3 Focus(Vector3 eye)
    {
        if (orbit && orbit.isActiveAndEnabled && orbit.TargetTransform) return orbit.TargetTransform.position + Vector3.up;
        var forward = cam.transform.forward;
        var floorY = transform.position.y;
        if (forward.y < -0.08f)
        {
            var d = (eye.y - floorY - 1f) / -forward.y;
            return eye + forward * Mathf.Min(d, 45f);
        }
        return eye + forward * 25f;
    }

    /// <summary>The wall's plane parts the camera from the focus, and the sight line crosses it near the wall.</summary>
    private bool Between(TowerWallPiece p, Vector3 eye, Vector3 focus)
    {
        var t = p.transform;
        var n = t.forward;
        n.y = 0f;
        if (n.sqrMagnitude < 0.01f) return false;
        n.Normalize();
        var c = t.position;
        var de = Vector3.Dot(eye - c, n);
        var df = Vector3.Dot(focus - c, n);
        // the focus must be clearly on the other side (a player brushing past a wall does not cut it)
        if (de * df >= 0f || Mathf.Abs(df) < 0.4f) return false;
        var cross = focus + (eye - focus) * (df / (df - de));
        var along = Vector3.Dot(cross - c, t.right);
        return Mathf.Abs(along) <= p.HalfLength + Margin;
    }
}
