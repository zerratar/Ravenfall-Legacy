using UnityEngine;

/// <summary>
/// Keeps grass off a rectangle of ground while it is enabled: a house, a stall, a plaza that has no village plot of its
/// own (the tower towns' buildings). The rectangle is the box collider on the same object when there is one, else
/// <see cref="HalfSize"/>, centred on the object and turned with it.
/// </summary>
[ExecuteAlways]
public sealed class GrassKeepOut : MonoBehaviour
{
    [Tooltip("Half the size along the object's own x and z, used when there is no box collider.")]
    public Vector2 HalfSize = new Vector2(2f, 2f);

    private void OnEnable()
    {
        var centre = transform.position;
        var half = HalfSize;
        if (TryGetComponent<BoxCollider>(out var box))
        {
            centre = transform.TransformPoint(box.center);
            var scale = transform.lossyScale;
            half = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.z * Mathf.Abs(scale.z)) * 0.5f;
        }
        GrassRenderer.SetArea(this, centre, half, transform.eulerAngles.y);
    }

    private void OnDisable()
    {
        GrassRenderer.Remove(this);
    }
}
