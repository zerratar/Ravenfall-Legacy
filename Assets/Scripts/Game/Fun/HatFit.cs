using UnityEngine;

/// <summary>
///     Where the April 1st party hat or traffic cone sits on this enemy or pet (SillySurprises.Hat). Without one the
///     hat goes on a guess: the head bone, or the top front of the body for models without one (cows), sized by height.
///     Add it to the prefab, press "Show hat preview" in the inspector, move, turn and scale the preview hat until it
///     sits right, then press "Take values from preview".
/// </summary>
[DisallowMultipleComponent]
public sealed class HatFit : MonoBehaviour
{
    [Tooltip("The bone the hat follows (the head, or the neck joint on animals). Empty: this object.")]
    public Transform Anchor;

    [Tooltip("The hat's position in the anchor's space.")]
    public Vector3 Offset = new Vector3(0f, 0.15f, 0f);

    [Tooltip("The hat's rotation in the anchor's space.")]
    public Vector3 Rotation;

    [Tooltip("The hat's size: 1 is the size of the hat on a grown up.")]
    public float Scale = 1f;

    [Tooltip("This one never gets a hat.")]
    public bool NoHat;

    public Transform Parent => Anchor ? Anchor : transform;

    /// <summary>Puts a hat (a copy of a template, of world size templateScale at Scale 1) where this fit says.</summary>
    public void Place(Transform hat, Vector3 templateScale)
    {
        var parent = Parent;
        hat.SetParent(parent, false);
        hat.localPosition = Offset;
        hat.localRotation = Quaternion.Euler(Rotation);
        var p = parent.lossyScale;
        hat.localScale = new Vector3(
            templateScale.x * Scale / NonZero(p.x),
            templateScale.y * Scale / NonZero(p.y),
            templateScale.z * Scale / NonZero(p.z));
    }

    /// <summary>Reads a hat placed by hand back into this fit.</summary>
    public void TakeFrom(Transform hat, Vector3 templateScale)
    {
        var parent = Parent;
        Offset = parent.InverseTransformPoint(hat.position);
        Rotation = (Quaternion.Inverse(parent.rotation) * hat.rotation).eulerAngles;
        Scale = hat.lossyScale.y / NonZero(templateScale.y);
    }

    private static float NonZero(float v) => Mathf.Abs(v) < 0.0001f ? 1f : v;
}
